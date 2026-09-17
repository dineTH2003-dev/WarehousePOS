using System.Collections.ObjectModel;
using WarehousePOS.Application.Expenses;
using WarehousePOS.Application.Printing;
using WarehousePOS.Application.Sales;
using WarehousePOS.Application.Settings;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Desktop.ViewModels.Sales;

public sealed record StatusOption(SaleStatus? Status, string DisplayName);
public sealed record PaymentFilterOption(PaymentMethod? Method, string DisplayName);

public sealed class BillHistoryViewModel : ViewModelBase
{
    private readonly ISaleService _saleService;
    private readonly IExpenseService _expenseService;
    private readonly IReceiptPrinter _printer;
    private readonly SessionContext _session;
    private readonly IStoreSettingService _settingService;

    private DateTime? _fromDate = DateTime.Today.AddDays(-30);
    private DateTime? _toDate = DateTime.Today;
    private string _searchQuery = string.Empty;
    private StatusOption _selectedStatusOption;
    private PaymentFilterOption _selectedPaymentOption;
    private SaleDto? _selectedSale;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private CancellationTokenSource? _successMessageCts;

    private int _totalSalesCount;
    private decimal _totalRevenue;
    private decimal _totalOutstanding;

    public ObservableCollection<SaleDto> Sales { get; } = new();
    public ObservableCollection<ExpenseDto> SelectedSaleExpenses { get; } = new();

    public IReadOnlyList<StatusOption> StatusOptions { get; } = new List<StatusOption>
    {
        new(null, "All Statuses"),
        new(SaleStatus.Completed, "Completed"),
        new(SaleStatus.AdvancePaid, "Advance / Layaway"),
        new(SaleStatus.PartiallyReturned, "Partially Returned"),
        new(SaleStatus.Returned, "Returned"),
        new(SaleStatus.Cancelled, "Cancelled")
    };

    public IReadOnlyList<PaymentFilterOption> PaymentOptions { get; } = new List<PaymentFilterOption>
    {
        new(null, "All Payment Methods"),
        new(PaymentMethod.Cash, "Cash"),
        new(PaymentMethod.Card, "Card"),
        new(PaymentMethod.BankTransfer, "Bank Transfer"),
        new(PaymentMethod.Cheque, "Cheque"),
        new(PaymentMethod.Other, "Other")
    };

    public DateTime? FromDate
    {
        get => _fromDate;
        set => SetField(ref _fromDate, value);
    }

    public DateTime? ToDate
    {
        get => _toDate;
        set => SetField(ref _toDate, value);
    }

    public string SearchQuery
    {
        get => _searchQuery;
        set => SetField(ref _searchQuery, value);
    }

    public StatusOption SelectedStatusOption
    {
        get => _selectedStatusOption;
        set => SetField(ref _selectedStatusOption, value);
    }

    public PaymentFilterOption SelectedPaymentOption
    {
        get => _selectedPaymentOption;
        set => SetField(ref _selectedPaymentOption, value);
    }

    public SaleDto? SelectedSale
    {
        get => _selectedSale;
        set
        {
            if (SetField(ref _selectedSale, value))
            {
                OnPropertyChanged(nameof(HasSelectedSale));
                OnPropertyChanged(nameof(CanRecordPayment));
                OnPropertyChanged(nameof(CanProcessReturn));
                RePrintReceiptCommand.RaiseCanExecuteChanged();
                RecordPaymentCommand.RaiseCanExecuteChanged();
                ProcessReturnCommand.RaiseCanExecuteChanged();
                AddExpenseCommand.RaiseCanExecuteChanged();
                AdjustSaleCommand.RaiseCanExecuteChanged();
                _ = LoadSelectedSaleExpensesAsync();
            }
        }
    }

    public bool HasSelectedSale => SelectedSale is not null;
    public bool CanRecordPayment => SelectedSale is not null && (SelectedSale.UnpaidAmount > 0 || SelectedSale.Status == SaleStatus.AdvancePaid);
    public bool CanProcessReturn => SelectedSale is not null && SelectedSale.Status != SaleStatus.Cancelled;
    public bool IsAdmin => _session.IsAdmin;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetField(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetField(ref _errorMessage, value))
                OnPropertyChanged(nameof(HasError));
        }
    }
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public string SuccessMessage
    {
        get => _successMessage;
        set
        {
            if (SetField(ref _successMessage, value))
            {
                OnPropertyChanged(nameof(HasSuccess));
                if (!string.IsNullOrEmpty(value))
                {
                    _successMessageCts?.Cancel();
                    _successMessageCts = new CancellationTokenSource();
                    var token = _successMessageCts.Token;
                    Task.Delay(3500, token).ContinueWith(t =>
                    {
                        if (!t.IsCanceled && !token.IsCancellationRequested)
                        {
                            System.Windows.Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                if (!token.IsCancellationRequested && _successMessage == value)
                                {
                                    SuccessMessage = string.Empty;
                                }
                            });
                        }
                    }, TaskScheduler.Default);
                }
            }
        }
    }
    public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

    public int TotalSalesCount
    {
        get => _totalSalesCount;
        private set => SetField(ref _totalSalesCount, value);
    }

    public decimal TotalRevenue
    {
        get => _totalRevenue;
        private set => SetField(ref _totalRevenue, value);
    }

    public decimal TotalOutstanding
    {
        get => _totalOutstanding;
        private set => SetField(ref _totalOutstanding, value);
    }

    public RelayCommand SearchCommand { get; }
    public RelayCommand ResetFiltersCommand { get; }
    public RelayCommand RePrintReceiptCommand { get; }
    public RelayCommand RecordPaymentCommand { get; }
    public RelayCommand ProcessReturnCommand { get; }
    public RelayCommand AddExpenseCommand { get; }
    public RelayCommand AdjustSaleCommand { get; }

    public BillHistoryViewModel(
        ISaleService saleService,
        IExpenseService expenseService,
        IReceiptPrinter printer,
        SessionContext session,
        IStoreSettingService settingService)
    {
        _saleService = saleService;
        _expenseService = expenseService;
        _printer = printer;
        _session = session;
        _settingService = settingService;

        _selectedStatusOption = StatusOptions[0];
        _selectedPaymentOption = PaymentOptions[0];

        SearchCommand = new RelayCommand(async () => await SearchAsync());
        ResetFiltersCommand = new RelayCommand(async () => await ResetFiltersAsync());
        RePrintReceiptCommand = new RelayCommand(async () => await RePrintReceiptAsync(), () => SelectedSale is not null);
        RecordPaymentCommand = new RelayCommand(async () => await RecordPaymentAsync(), () => CanRecordPayment);
        ProcessReturnCommand = new RelayCommand(async () => await ProcessReturnAsync(), () => CanProcessReturn);
        AddExpenseCommand = new RelayCommand(async () => await AddExpenseAsync(), () => SelectedSale is not null);
        AdjustSaleCommand = new RelayCommand(async () => await AdjustSaleAsync(), () => SelectedSale is not null && IsAdmin);
    }

    public async Task InitializeAsync()
    {
        await SearchAsync();
    }

    public async Task SearchAsync()
    {
        ErrorMessage = string.Empty;
        IsLoading = true;
        try
        {
            var criteria = new SaleSearchCriteria(
                FromDate: FromDate.HasValue ? DateTime.SpecifyKind(FromDate.Value.Date, DateTimeKind.Utc) : null,
                ToDate: ToDate.HasValue ? DateTime.SpecifyKind(ToDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc) : null,
                SearchTerm: string.IsNullOrWhiteSpace(SearchQuery) ? null : SearchQuery.Trim(),
                Status: SelectedStatusOption.Status,
                PaymentMethod: SelectedPaymentOption.Method);

            var results = await _saleService.SearchSalesAsync(criteria);

            Sales.Clear();
            decimal revenue = 0;
            decimal outstanding = 0;

            foreach (var sale in results)
            {
                Sales.Add(sale);
                if (sale.Status != SaleStatus.Cancelled)
                {
                    revenue += sale.AmountPaid;
                    outstanding += sale.UnpaidAmount;
                }
            }

            TotalSalesCount = Sales.Count;
            TotalRevenue = revenue;
            TotalOutstanding = outstanding;

            if (SelectedSale is not null)
            {
                var refreshed = Sales.FirstOrDefault(s => s.Id == SelectedSale.Id);
                SelectedSale = refreshed;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to search bills: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task ResetFiltersAsync()
    {
        FromDate = DateTime.Today.AddDays(-30);
        ToDate = DateTime.Today;
        SearchQuery = string.Empty;
        SelectedStatusOption = StatusOptions[0];
        SelectedPaymentOption = PaymentOptions[0];
        await SearchAsync();
    }

    public async Task LoadSelectedSaleExpensesAsync()
    {
        SelectedSaleExpenses.Clear();
        if (SelectedSale is null) return;

        try
        {
            var expenses = await _expenseService.GetBySaleIdAsync(SelectedSale.Id);
            foreach (var exp in expenses)
            {
                SelectedSaleExpenses.Add(exp);
            }
        }
        catch
        {
            // Non-critical
        }
    }

    public async Task RePrintReceiptAsync()
    {
        if (SelectedSale is null) return;

        try
        {
            await _printer.PrintReceiptAsync(SelectedSale);
            SuccessMessage = $"Bill #{SelectedSale.Id} sent to printer successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to print bill: {ex.Message}";
        }
    }

    public async Task RecordPaymentAsync()
    {
        if (SelectedSale is null) return;

        var dialog = new Views.Sales.RecordPaymentDialog(SelectedSale)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.PaymentRequest is not null)
        {
            IsLoading = true;
            try
            {
                var req = dialog.PaymentRequest with
                {
                    CashierUserId = _session.CurrentUser?.UserId ?? 1
                };

                var updated = await _saleService.RecordPaymentAsync(req);
                SuccessMessage = $"Payment of Rs. {req.Amount:N2} recorded for Invoice #{updated.Id}! Remaining: Rs. {updated.UnpaidAmount:N2}";
                await SearchAsync();
                SelectedSale = updated;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to record payment: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public async Task ProcessReturnAsync()
    {
        if (SelectedSale is null) return;

        var dialog = new Views.Sales.SalesReturnDialog(SelectedSale)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.ReturnItems.Any())
        {
            IsLoading = true;
            try
            {
                var req = new ProcessSaleReturnRequest(
                    SelectedSale.Id,
                    _session.CurrentUser?.UserId ?? 1,
                    dialog.ReturnItems,
                    dialog.RefundCash,
                    dialog.ReturnReason);

                var updated = await _saleService.ProcessReturnAsync(req);
                SuccessMessage = $"Return processed successfully for Invoice #{updated.Id}!";
                await SearchAsync();
                SelectedSale = updated;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to process return: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public async Task AddExpenseAsync()
    {
        if (SelectedSale is null) return;

        var categories = await _expenseService.GetCategoriesAsync();
        var dialog = new Views.Sales.AddSaleExpenseDialog(SelectedSale.Id, categories)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.CreatedExpense is not null)
        {
            IsLoading = true;
            try
            {
                var req = dialog.CreatedExpense with
                {
                    RecordedByUserId = _session.CurrentUser?.UserId ?? 1
                };

                await _expenseService.CreateAsync(req);
                SuccessMessage = $"Extra outcome / expense of Rs. {req.Amount:N2} recorded for Invoice #{SelectedSale.Id}!";
                await LoadSelectedSaleExpensesAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to add expense: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }

    public async Task AdjustSaleAsync()
    {
        if (SelectedSale is null || !IsAdmin) return;

        var dialog = new Views.Sales.AdjustSaleDialog(SelectedSale)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true && dialog.AdjustmentRequest is not null)
        {
            IsLoading = true;
            try
            {
                var req = dialog.AdjustmentRequest with
                {
                    AdminUserId = _session.CurrentUser?.UserId ?? 1
                };

                var updated = await _saleService.AdjustSaleAsync(req);
                SuccessMessage = $"Invoice #{updated.Id} details amended successfully!";
                await SearchAsync();
                SelectedSale = updated;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Failed to amend bill: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
