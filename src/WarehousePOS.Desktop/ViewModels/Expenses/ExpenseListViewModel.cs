using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Media;
using WarehousePOS.Application.Expenses;
using WarehousePOS.Application.Reports;
using WarehousePOS.Desktop.Services;

namespace WarehousePOS.Desktop.ViewModels.Expenses;

public sealed class CategoryFilterItem
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class CategoryAnalyticsItemViewModel
{
    public int CategoryId { get; init; }
    public string CategoryName { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string TotalAmountFormatted => $"Rs. {TotalAmount:N2}";
    public double Percentage { get; init; }
    public string PercentageFormatted => $"{Percentage:F0}%";
    public int Count { get; init; }
    public Brush ColorBrush { get; init; } = Brushes.Gray;
}

public sealed class MonthlyTrendBarViewModel
{
    public string MonthLabel { get; init; } = string.Empty;
    public decimal TotalAmount { get; init; }
    public string TotalAmountFormatted => $"Rs. {TotalAmount:N2}";
    public int TransactionCount { get; init; }
    public double BarHeight { get; init; } // Normalized height for visual bar
    public bool IsActiveMonth { get; init; }
    public string TooltipText => $"{MonthLabel}\nExpenses: Rs. {TotalAmount:N2}\nTransactions: {TransactionCount}";
}

public sealed class ExpenseListViewModel : ViewModelBase
{
    private readonly IExpenseService _expenseService;
    private readonly SessionContext _sessionContext;

    // Filters
    private DateRangePreset _selectedDatePreset = DateRangePreset.ThisMonth;
    private DateTime _fromDate = new(DateTime.Today.Year, DateTime.Today.Month, 1);
    private DateTime _toDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1).AddDays(-1);
    private int _selectedCategoryFilterId = 0;
    private string _searchTerm = string.Empty;
    private string _filterValidationError = string.Empty;

    // Data Collections
    private ObservableCollection<ExpenseDto> _expenses = [];
    private ObservableCollection<ExpenseCategoryDto> _formCategories = [];
    private ObservableCollection<CategoryFilterItem> _filterCategories = [];
    private ObservableCollection<CategoryAnalyticsItemViewModel> _categoryBreakdownItems = [];
    private ObservableCollection<MonthlyTrendBarViewModel> _monthlyTrendItems = [];

    // Selected Item & Modals
    private ExpenseDto? _selectedExpense;
    private bool _isDetailDialogOpen;
    private bool _isDeleteConfirmOpen;
    private ExpenseDto? _expenseToDelete;

    // Form Inputs & State
    private bool _isEditing;
    private int? _editingExpenseId;
    private DateTime _formExpenseDate = DateTime.Today;
    private int _formCategoryId;
    private string _formAmountText = string.Empty;
    private string _formDescription = string.Empty;
    private string _formReferenceNo = string.Empty;
    private string _formAmountError = string.Empty;
    private string _formErrorMessage = string.Empty;
    private string _successMessage = string.Empty;
    private bool _isBusy;

    // KPIs
    private decimal _totalExpenses;
    private int _expenseCount;
    private decimal _averageExpense;
    private string _highestCategoryName = "N/A";
    private string _highestCategorySubtext = "0% of total expenses";
    private string _trendText = "─ 0% vs last month";
    private string _insightText = "Keep track of all business expenses to improve cost control and profitability.";

    public ObservableCollection<ExpenseDto> Expenses => _expenses;
    public ObservableCollection<ExpenseCategoryDto> FormCategories => _formCategories;
    public ObservableCollection<CategoryFilterItem> FilterCategories => _filterCategories;
    public ObservableCollection<CategoryAnalyticsItemViewModel> CategoryBreakdownItems => _categoryBreakdownItems;
    public ObservableCollection<MonthlyTrendBarViewModel> MonthlyTrendItems => _monthlyTrendItems;

    // Filtering Properties
    public DateRangePreset SelectedDatePreset
    {
        get => _selectedDatePreset;
        set
        {
            if (SetField(ref _selectedDatePreset, value))
            {
                OnDatePresetChanged(value);
                OnPropertyChanged(nameof(IsCustomDateRange));
            }
        }
    }

    public bool IsCustomDateRange => SelectedDatePreset == DateRangePreset.Custom;

    public DateTime FromDate
    {
        get => _fromDate;
        set
        {
            if (SetField(ref _fromDate, value))
            {
                ValidateDateRange();
            }
        }
    }

    public DateTime ToDate
    {
        get => _toDate;
        set
        {
            if (SetField(ref _toDate, value))
            {
                ValidateDateRange();
            }
        }
    }

    public int SelectedCategoryFilterId
    {
        get => _selectedCategoryFilterId;
        set
        {
            if (SetField(ref _selectedCategoryFilterId, value))
            {
                _ = ApplyFilterAsync();
            }
        }
    }

    public string SearchTerm
    {
        get => _searchTerm;
        set
        {
            if (SetField(ref _searchTerm, value))
            {
                _ = ApplyFilterAsync();
            }
        }
    }

    public string FilterValidationError
    {
        get => _filterValidationError;
        set
        {
            if (SetField(ref _filterValidationError, value))
                OnPropertyChanged(nameof(HasFilterValidationError));
        }
    }

    public bool HasFilterValidationError => !string.IsNullOrEmpty(FilterValidationError);

    // KPI Properties
    public decimal TotalExpenses
    {
        get => _totalExpenses;
        private set => SetField(ref _totalExpenses, value);
    }

    public string TotalExpensesFormatted => $"Rs. {TotalExpenses:N2}";

    public int ExpenseCount
    {
        get => _expenseCount;
        private set
        {
            if (SetField(ref _expenseCount, value))
                OnPropertyChanged(nameof(RecordCountSummary));
        }
    }

    public decimal AverageExpense
    {
        get => _averageExpense;
        private set => SetField(ref _averageExpense, value);
    }

    public string AverageExpenseFormatted => $"Rs. {AverageExpense:N2}";

    public string HighestCategoryName
    {
        get => _highestCategoryName;
        private set => SetField(ref _highestCategoryName, value);
    }

    public string HighestCategorySubtext
    {
        get => _highestCategorySubtext;
        private set => SetField(ref _highestCategorySubtext, value);
    }

    public string TrendText
    {
        get => _trendText;
        private set => SetField(ref _trendText, value);
    }

    public string InsightText
    {
        get => _insightText;
        private set => SetField(ref _insightText, value);
    }

    public string RecordCountSummary => $"Showing 1 to {Expenses.Count} of {ExpenseCount} entries";

    // Form Properties
    public bool IsEditing
    {
        get => _isEditing;
        private set
        {
            if (SetField(ref _isEditing, value))
            {
                OnPropertyChanged(nameof(FormTitle));
                OnPropertyChanged(nameof(FormSubtitle));
                OnPropertyChanged(nameof(FormSubmitButtonText));
            }
        }
    }

    public string FormTitle => IsEditing ? "Edit Expense Entry" : "+ Record New Expense";
    public string FormSubtitle => IsEditing ? "Update existing operational expense details" : "Add a new operational expense entry";
    public string FormSubmitButtonText => IsEditing ? "Update Expense Entry" : "Save Expense Entry";

    public DateTime FormExpenseDate
    {
        get => _formExpenseDate;
        set => SetField(ref _formExpenseDate, value);
    }

    public int FormCategoryId
    {
        get => _formCategoryId;
        set
        {
            if (SetField(ref _formCategoryId, value))
            {
                ClearFormErrors();
                SaveExpenseCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string FormAmountText
    {
        get => _formAmountText;
        set
        {
            if (SetField(ref _formAmountText, value))
            {
                ValidateAmountInput(value);
                ClearFormErrors();
                SaveExpenseCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string FormDescription
    {
        get => _formDescription;
        set
        {
            if (SetField(ref _formDescription, value))
            {
                ClearFormErrors();
                SaveExpenseCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string FormReferenceNo
    {
        get => _formReferenceNo;
        set => SetField(ref _formReferenceNo, value);
    }

    public string FormAmountError
    {
        get => _formAmountError;
        set
        {
            if (SetField(ref _formAmountError, value))
                OnPropertyChanged(nameof(HasFormAmountError));
        }
    }

    public bool HasFormAmountError => !string.IsNullOrEmpty(FormAmountError);

    public string FormErrorMessage
    {
        get => _formErrorMessage;
        set
        {
            if (SetField(ref _formErrorMessage, value))
                OnPropertyChanged(nameof(HasFormErrorMessage));
        }
    }

    public bool HasFormErrorMessage => !string.IsNullOrEmpty(FormErrorMessage);

    public string SuccessMessage
    {
        get => _successMessage;
        set
        {
            if (SetField(ref _successMessage, value))
                OnPropertyChanged(nameof(HasSuccessMessage));
        }
    }

    public bool HasSuccessMessage => !string.IsNullOrEmpty(SuccessMessage);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                SaveExpenseCommand.RaiseCanExecuteChanged();
                ApplyFilterCommand.RaiseCanExecuteChanged();
            }
        }
    }

    // Modal / Selection Properties
    public ExpenseDto? SelectedExpense
    {
        get => _selectedExpense;
        set => SetField(ref _selectedExpense, value);
    }

    public bool IsDetailDialogOpen
    {
        get => _isDetailDialogOpen;
        set => SetField(ref _isDetailDialogOpen, value);
    }

    public bool IsDeleteConfirmOpen
    {
        get => _isDeleteConfirmOpen;
        set => SetField(ref _isDeleteConfirmOpen, value);
    }

    public ExpenseDto? ExpenseToDelete
    {
        get => _expenseToDelete;
        set => SetField(ref _expenseToDelete, value);
    }

    // Commands
    public RelayCommand ApplyFilterCommand { get; }
    public RelayCommand ClearFilterCommand { get; }
    public RelayCommand SaveExpenseCommand { get; }
    public RelayCommand CancelEditCommand { get; }
    public RelayCommand<ExpenseDto> OpenEditCommand { get; }
    public RelayCommand<ExpenseDto> ViewDetailsCommand { get; }
    public RelayCommand CloseDetailsCommand { get; }
    public RelayCommand<ExpenseDto> PromptDeleteCommand { get; }
    public RelayCommand ConfirmDeleteCommand { get; }
    public RelayCommand CancelDeleteCommand { get; }
    public RelayCommand RefreshCommand { get; }
    public RelayCommand QuickThisMonthCommand { get; }
    public RelayCommand QuickLast3MonthsCommand { get; }

    public ExpenseListViewModel(IExpenseService expenseService, SessionContext sessionContext)
    {
        if (!sessionContext.IsAdmin)
            throw new UnauthorizedAccessException("Only an Admin can access Expenses.");

        _expenseService = expenseService;
        _sessionContext = sessionContext;

        ApplyFilterCommand = new RelayCommand(async () => await ApplyFilterAsync(), () => !HasFilterValidationError && !IsBusy);
        ClearFilterCommand = new RelayCommand(async () => await ResetFiltersAsync());
        SaveExpenseCommand = new RelayCommand(async () => await SaveExpenseAsync(), () => CanSaveExpense());
        CancelEditCommand = new RelayCommand(ResetForm);

        OpenEditCommand = new RelayCommand<ExpenseDto>(StartEditExpense);
        ViewDetailsCommand = new RelayCommand<ExpenseDto>(OpenExpenseDetails);
        CloseDetailsCommand = new RelayCommand(() => IsDetailDialogOpen = false);

        PromptDeleteCommand = new RelayCommand<ExpenseDto>(PromptDeleteExpense);
        ConfirmDeleteCommand = new RelayCommand(async () => await ExecuteDeleteAsync());
        CancelDeleteCommand = new RelayCommand(() => IsDeleteConfirmOpen = false);

        RefreshCommand = new RelayCommand(async () => await LoadDataAsync());
        QuickThisMonthCommand = new RelayCommand(() => SelectedDatePreset = DateRangePreset.ThisMonth);
        QuickLast3MonthsCommand = new RelayCommand(() => SelectedDatePreset = DateRangePreset.Last30Days);
    }

    public async Task LoadDataAsync()
    {
        EnsureAdmin();
        IsBusy = true;
        FilterValidationError = string.Empty;

        try
        {
            // Load Categories
            var categories = await _expenseService.GetCategoriesAsync();

            _formCategories.Clear();
            foreach (var c in categories.Where(x => x.IsActive))
                _formCategories.Add(c);

            _filterCategories.Clear();
            _filterCategories.Add(new CategoryFilterItem { Id = 0, Name = "All Categories" });
            foreach (var c in categories)
                _filterCategories.Add(new CategoryFilterItem { Id = c.Id, Name = c.Name });

            if (_formCategories.Count > 0 && (_formCategoryId <= 0 || !_formCategories.Any(c => c.Id == _formCategoryId)))
            {
                FormCategoryId = _formCategories[0].Id;
            }

            await ApplyFilterAsync();
        }
        catch (Exception ex)
        {
            FormErrorMessage = $"Failed to load expenses: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ApplyFilterAsync()
    {
        if (HasFilterValidationError) return;

        IsBusy = true;
        try
        {
            int? catId = SelectedCategoryFilterId > 0 ? SelectedCategoryFilterId : null;
            var filterReq = new ExpenseFilterRequest(SelectedDatePreset, FromDate, ToDate, catId, SearchTerm);

            var analytics = await _expenseService.GetAnalyticsAsync(filterReq);

            TotalExpenses = analytics.TotalExpenses;
            ExpenseCount = analytics.ExpenseCount;
            AverageExpense = analytics.AverageExpense;
            HighestCategoryName = analytics.HighestCategoryName;
            HighestCategorySubtext = analytics.HighestCategoryAmount > 0
                ? $"{analytics.HighestCategoryPercentage:F0}% of total expenses"
                : "0% of total expenses";

            if (analytics.PercentageChangeVsPreviousPeriod.HasValue)
            {
                var pct = analytics.PercentageChangeVsPreviousPeriod.Value;
                TrendText = pct >= 0 ? $"↑ +{pct:F1}% vs previous period" : $"↓ {pct:F1}% vs previous period";
            }
            else
            {
                TrendText = "─ 0% vs previous period";
            }

            // Update Expense List
            _expenses.Clear();
            foreach (var item in analytics.FilteredExpenses)
                _expenses.Add(item);

            // Update Category Breakdown Visual Items
            _categoryBreakdownItems.Clear();
            var brushConverter = new BrushConverter();
            foreach (var catDto in analytics.CategoryBreakdown)
            {
                var colorHex = GetCategoryHexColor(catDto.CategoryName);
                var brush = (Brush)brushConverter.ConvertFromString(colorHex)!;

                _categoryBreakdownItems.Add(new CategoryAnalyticsItemViewModel
                {
                    CategoryId = catDto.CategoryId,
                    CategoryName = catDto.CategoryName,
                    TotalAmount = catDto.TotalAmount,
                    Percentage = catDto.PercentageOfTotal,
                    Count = catDto.Count,
                    ColorBrush = brush
                });
            }

            // Update Monthly Trend Bar Items
            _monthlyTrendItems.Clear();
            var maxVal = analytics.MonthlyTrend.Max(m => m.TotalExpenses);
            if (maxVal <= 0) maxVal = 1m;

            var currentMonth = DateTime.Today.Month;

            foreach (var mDto in analytics.MonthlyTrend)
            {
                double heightRatio = (double)(mDto.TotalExpenses / maxVal) * 120.0;
                if (heightRatio < 4.0 && mDto.TotalExpenses > 0) heightRatio = 6.0;

                _monthlyTrendItems.Add(new MonthlyTrendBarViewModel
                {
                    MonthLabel = mDto.MonthLabel,
                    TotalAmount = mDto.TotalExpenses,
                    TransactionCount = mDto.TransactionCount,
                    BarHeight = heightRatio,
                    IsActiveMonth = mDto.Month == currentMonth
                });
            }

            // Update Dynamic Insight Text
            if (analytics.HighestCategoryAmount > 0)
            {
                InsightText = $"{HighestCategoryName} represents {HighestCategorySubtext} for the selected period.";
            }
            else
            {
                InsightText = "Keep track of all business expenses to improve cost control and profitability.";
            }

            OnPropertyChanged(nameof(TotalExpensesFormatted));
            OnPropertyChanged(nameof(AverageExpenseFormatted));
            OnPropertyChanged(nameof(RecordCountSummary));
        }
        catch (Exception ex)
        {
            FormErrorMessage = $"Filter error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnDatePresetChanged(DateRangePreset preset)
    {
        var today = DateTime.Today;
        switch (preset)
        {
            case DateRangePreset.Today:
                _fromDate = today;
                _toDate = today;
                break;
            case DateRangePreset.Yesterday:
                _fromDate = today.AddDays(-1);
                _toDate = today.AddDays(-1);
                break;
            case DateRangePreset.Last7Days:
                _fromDate = today.AddDays(-6);
                _toDate = today;
                break;
            case DateRangePreset.Last30Days:
                _fromDate = today.AddDays(-29);
                _toDate = today;
                break;
            case DateRangePreset.ThisMonth:
                _fromDate = new DateTime(today.Year, today.Month, 1);
                _toDate = _fromDate.AddMonths(1).AddDays(-1);
                break;
            case DateRangePreset.AllTime:
                _fromDate = new DateTime(2000, 1, 1);
                _toDate = today;
                break;
            case DateRangePreset.Custom:
                // Retain current From/To
                break;
        }

        OnPropertyChanged(nameof(FromDate));
        OnPropertyChanged(nameof(ToDate));
        ValidateDateRange();

        if (!HasFilterValidationError)
        {
            _ = ApplyFilterAsync();
        }
    }

    private void ValidateDateRange()
    {
        if (FromDate.Date > ToDate.Date)
        {
            FilterValidationError = "Start date cannot be later than end date.";
        }
        else
        {
            FilterValidationError = string.Empty;
        }
        ApplyFilterCommand.RaiseCanExecuteChanged();
    }

    private async Task ResetFiltersAsync()
    {
        _selectedDatePreset = DateRangePreset.ThisMonth;
        var today = DateTime.Today;
        _fromDate = new DateTime(today.Year, today.Month, 1);
        _toDate = _fromDate.AddMonths(1).AddDays(-1);
        _selectedCategoryFilterId = 0;
        _searchTerm = string.Empty;
        _filterValidationError = string.Empty;

        OnPropertyChanged(nameof(SelectedDatePreset));
        OnPropertyChanged(nameof(FromDate));
        OnPropertyChanged(nameof(ToDate));
        OnPropertyChanged(nameof(SelectedCategoryFilterId));
        OnPropertyChanged(nameof(SearchTerm));
        OnPropertyChanged(nameof(FilterValidationError));

        await ApplyFilterAsync();
    }

    private void ValidateAmountInput(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            FormAmountError = string.Empty;
            return;
        }

        var trimmed = text.Trim();
        if (!Regex.IsMatch(trimmed, @"^[0-9]+(\.[0-9]+)?$"))
        {
            FormAmountError = "Amount contains invalid characters. Use positive digits only (e.g. 1500.00).";
            return;
        }

        if (!decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount) &&
            !decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out amount))
        {
            FormAmountError = "Enter a valid numeric amount.";
            return;
        }

        if (amount <= 0)
        {
            FormAmountError = "Amount must be greater than 0.";
            return;
        }

        var dotIndex = trimmed.IndexOf('.');
        if (dotIndex >= 0 && trimmed.Length - dotIndex - 1 > 2)
        {
            FormAmountError = "Amount cannot have more than 2 decimal places.";
            return;
        }

        FormAmountError = string.Empty;
    }

    public bool CanSaveExpense()
    {
        if (IsBusy) return false;
        if (FormCategoryId <= 0) return false;
        if (string.IsNullOrWhiteSpace(FormAmountText)) return false;
        if (HasFormAmountError) return false;
        if (!TryParsePositiveAmount(FormAmountText, out _)) return false;
        if (string.IsNullOrWhiteSpace(FormDescription)) return false;

        return true;
    }

    private static bool TryParsePositiveAmount(string text, out decimal amount)
    {
        amount = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var trimmed = text.Trim();

        if (!Regex.IsMatch(trimmed, @"^[0-9]+(\.[0-9]{1,2})?$"))
            return false;

        if (!decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) &&
            !decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.CurrentCulture, out amount))
        {
            return false;
        }

        return amount > 0;
    }

    private async Task SaveExpenseAsync()
    {
        EnsureAdmin();
        FormErrorMessage = string.Empty;
        SuccessMessage = string.Empty;

        if (FormCategoryId <= 0)
        {
            FormErrorMessage = "Please select an expense category.";
            return;
        }

        if (!TryParsePositiveAmount(FormAmountText, out var amount))
        {
            ValidateAmountInput(FormAmountText);
            FormErrorMessage = string.IsNullOrEmpty(FormAmountError)
                ? "Enter a valid positive expense amount (e.g. 1500.00)."
                : FormAmountError;
            return;
        }

        if (string.IsNullOrWhiteSpace(FormDescription))
        {
            FormErrorMessage = "Description is required.";
            return;
        }

        IsBusy = true;
        try
        {
            var userId = _sessionContext.CurrentUser?.UserId ?? 1;

            if (IsEditing && _editingExpenseId.HasValue)
            {
                var updateReq = new UpdateExpenseRequest(
                    FormCategoryId,
                    amount,
                    FormDescription.Trim(),
                    FormExpenseDate,
                    string.IsNullOrWhiteSpace(FormReferenceNo) ? null : FormReferenceNo.Trim());

                await _expenseService.UpdateAsync(_editingExpenseId.Value, updateReq);
                SuccessMessage = "Expense recorded successfully.";
            }
            else
            {
                var createReq = new CreateExpenseRequest(
                    FormCategoryId,
                    amount,
                    FormDescription.Trim(),
                    userId,
                    FormExpenseDate,
                    string.IsNullOrWhiteSpace(FormReferenceNo) ? null : FormReferenceNo.Trim());

                await _expenseService.CreateAsync(createReq);
                SuccessMessage = "Expense recorded successfully.";
            }

            ResetForm();
            await ApplyFilterAsync();
        }
        catch (Exception ex)
        {
            FormErrorMessage = $"Unable to save expense: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void StartEditExpense(ExpenseDto? item)
    {
        if (item == null) return;

        IsEditing = true;
        _editingExpenseId = item.Id;
        FormExpenseDate = item.ExpenseDate;
        FormCategoryId = item.CategoryId;
        FormAmountText = item.Amount.ToString("F2", CultureInfo.InvariantCulture);
        FormDescription = item.Description;
        FormReferenceNo = item.ReferenceNo ?? string.Empty;
        ClearFormErrors();
    }

    private void OpenExpenseDetails(ExpenseDto? item)
    {
        if (item == null) return;
        SelectedExpense = item;
        IsDetailDialogOpen = true;
    }

    private void PromptDeleteExpense(ExpenseDto? item)
    {
        if (item == null) return;
        ExpenseToDelete = item;
        IsDeleteConfirmOpen = true;
    }

    private async Task ExecuteDeleteAsync()
    {
        if (ExpenseToDelete == null) return;

        IsBusy = true;
        try
        {
            await _expenseService.DeleteAsync(ExpenseToDelete.Id);
            IsDeleteConfirmOpen = false;
            ExpenseToDelete = null;
            SuccessMessage = "Expense deleted successfully.";
            await ApplyFilterAsync();
        }
        catch (Exception ex)
        {
            FormErrorMessage = $"Unable to delete expense: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetForm()
    {
        IsEditing = false;
        _editingExpenseId = null;
        FormExpenseDate = DateTime.Today;
        FormAmountText = string.Empty;
        FormAmountError = string.Empty;
        FormDescription = string.Empty;
        FormReferenceNo = string.Empty;
        FormErrorMessage = string.Empty;

        if (_formCategories.Count > 0)
            FormCategoryId = _formCategories[0].Id;
    }

    private void ClearFormErrors()
    {
        FormErrorMessage = string.Empty;
        SuccessMessage = string.Empty;
    }

    private void EnsureAdmin()
    {
        if (!_sessionContext.IsAdmin)
            throw new UnauthorizedAccessException("Only an Admin can access Expenses.");
    }

    private static string GetCategoryHexColor(string catName) => catName?.Trim() switch
    {
        "Transport & Fuel"      => "#3B82F6",
        "Maintenance & Repairs" => "#10B981",
        "Utility Bills"         => "#F59E0B",
        "Rent & Lease"          => "#8B5CF6",
        "Office Supplies"       => "#EF4444",
        "Wages & Salaries"      => "#14B8A6",
        "Miscellaneous"         => "#64748B",
        _                       => "#6366F1"
    };
}
