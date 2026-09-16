using System.Collections.ObjectModel;
using System.Globalization;
using WarehousePOS.Application.Printing;
using WarehousePOS.Application.Products;
using WarehousePOS.Application.Sales;
using WarehousePOS.Application.Settings;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Desktop.ViewModels.Sales;

public sealed record PaymentMethodOption(PaymentMethod Method, string DisplayName);

public sealed class PosCartItem : ViewModelBase
{
    private int _quantity;
    private decimal _unitPrice;
    private decimal _discount;

    public ProductDto Product { get; }

    public int Quantity
    {
        get => _quantity;
        set
        {
            if (value < 1) value = 1;
            if (Product != null && value > Product.StockQuantity)
            {
                value = Product.StockQuantity;
            }
            if (SetField(ref _quantity, value))
            {
                OnPropertyChanged(nameof(LineTotal));
            }
        }
    }

    public decimal UnitPrice
    {
        get => _unitPrice;
        set { SetField(ref _unitPrice, value); OnPropertyChanged(nameof(LineTotal)); }
    }

    public decimal Discount
    {
        get => _discount;
        set { SetField(ref _discount, value); OnPropertyChanged(nameof(LineTotal)); }
    }

    public decimal LineTotal => (UnitPrice * Quantity) - Discount;

    public PosCartItem(ProductDto product, int quantity, decimal unitPrice)
    {
        Product   = product;
        _quantity  = quantity;
        _unitPrice = unitPrice;
    }
}

public sealed class PosViewModel : ViewModelBase
{
    private readonly IProductService  _productService;
    private readonly ICustomerService _customerService;
    private readonly ISaleService     _saleService;
    private readonly SessionContext   _sessionContext;
    private readonly IReceiptPrinter  _printer;
    private readonly IStoreSettingService _settingService;

    private SaleDto? _lastCompletedSale;

    private ObservableCollection<ProductDto>  _searchResults = [];
    private ObservableCollection<CustomerDto> _customers     = [];
    private ObservableCollection<CustomerDto> _filteredCustomers = [];
    private ObservableCollection<PosCartItem> _cartItems     = [];

    private string      _searchQuery         = string.Empty;
    private string      _customerSearchQuery = string.Empty;
    private bool        _isCustomerDropDownOpen;
    private CustomerDto? _selectedCustomer;
    private bool _isDiscountManuallyOverridden;
    private SaleType    _saleType       = SaleType.Retail;
    private decimal     _overallDiscount;
    private string      _overallDiscountText = string.Empty;
    private decimal     _amountPaid;
    private string      _amountPaidText = "0";
    private string      _errorMessage   = string.Empty;
    private bool        _isBusy;
    private string      _successMessage = string.Empty;
    private CancellationTokenSource? _successMessageCts;

    public ObservableCollection<ProductDto>  SearchResults => _searchResults;
    public ObservableCollection<CustomerDto> Customers     => _customers;
    public ObservableCollection<CustomerDto> FilteredCustomers => _filteredCustomers;
    public ObservableCollection<PosCartItem> CartItems     => _cartItems;

    public string SearchQuery
    {
        get => _searchQuery;
        set { SetField(ref _searchQuery, value); _ = PerformSearchAsync(); }
    }

    public bool IsCustomerDropDownOpen
    {
        get => _isCustomerDropDownOpen;
        set => SetField(ref _isCustomerDropDownOpen, value);
    }

    public string CustomerSearchQuery
    {
        get => _customerSearchQuery;
        set
        {
            if (SetField(ref _customerSearchQuery, value))
            {
                FilterCustomers();
            }
        }
    }

    private bool _isRefreshingCustomers;
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;

    public IReadOnlyList<PaymentMethodOption> PaymentMethodOptions { get; } = new List<PaymentMethodOption>
    {
        new(PaymentMethod.Cash, "Cash"),
        new(PaymentMethod.Card, "Credit / Debit Card"),
        new(PaymentMethod.Cheque, "Cheque"),
        new(PaymentMethod.BankTransfer, "Bank Transfer")
    };

    public PaymentMethod SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set => SetField(ref _selectedPaymentMethod, value);
    }

    public bool IsRegisteredCustomer => SelectedCustomer is not null;

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (_isRefreshingCustomers) return;

            if (SetField(ref _selectedCustomer, value))
            {
                _isDiscountManuallyOverridden = false;
                IsCustomerDropDownOpen = false;
                if (value is not null)
                {
                    _customerSearchQuery = value.DisplayName;
                    OnPropertyChanged(nameof(CustomerSearchQuery));
                    SaleType = value.Type; // Auto-set SaleType based on customer preference
                    ApplyCustomerDiscountRate();
                }
                else
                {
                    _customerSearchQuery = string.Empty;
                    OnPropertyChanged(nameof(CustomerSearchQuery));
                    _overallDiscount = 0;
                    _overallDiscountText = string.Empty;
                    OnPropertyChanged(nameof(OverallDiscount));
                    OnPropertyChanged(nameof(OverallDiscountText));
                }
                OnPropertyChanged(nameof(IsRegisteredCustomer));
                RecalculateTotals();
            }
        }
    }

    public SaleType SaleType
    {
        get => _saleType;
        set
        {
            if (SetField(ref _saleType, value))
                UpdateCartPricesForSaleType();
        }
    }

    public decimal OverallDiscount
    {
        get => _overallDiscount;
        set
        {
            if (SetField(ref _overallDiscount, value))
            {
                _overallDiscountText = value == 0
                    ? string.Empty
                    : value.ToString("F2", CultureInfo.InvariantCulture);
                OnPropertyChanged(nameof(OverallDiscountText));
                RecalculateTotals();
            }
        }
    }

    public string OverallDiscountText
    {
        get => _overallDiscountText;
        set
        {
            if (!SetField(ref _overallDiscountText, value))
                return;

            _isDiscountManuallyOverridden = true;

            if (string.IsNullOrWhiteSpace(value))
            {
                _overallDiscount = 0;
            }
            else if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var discount) ||
                     decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out discount))
            {
                _overallDiscount = discount;
            }

            RecalculateTotals();
        }
    }

    public decimal AmountPaid
    {
        get => _amountPaid;
        set
        {
            if (SetField(ref _amountPaid, value))
            {
                _amountPaidText = value == 0 ? "0" : value.ToString(CultureInfo.InvariantCulture);
                OnPropertyChanged(nameof(AmountPaidText));
                RecalculateTotals();
            }
        }
    }

    public string AmountPaidText
    {
        get => _amountPaidText;
        set
        {
            if (!string.IsNullOrEmpty(value) && value.Length > 1 && value.StartsWith("0") && !value.StartsWith("0.") && !value.StartsWith("0,"))
            {
                value = value.TrimStart('0');
                if (string.IsNullOrEmpty(value)) value = "0";
            }

            if (!SetField(ref _amountPaidText, value))
                return;

            if (string.IsNullOrWhiteSpace(value))
            {
                _amountPaid = 0;
            }
            else if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var paid) ||
                     decimal.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out paid))
            {
                _amountPaid = Math.Max(0, paid);
            }
            else
            {
                _amountPaid = 0;
            }

            RecalculateTotals();
        }
    }

    public decimal SubTotal      => _cartItems.Sum(i => i.LineTotal);
    public decimal TotalAmount   => Math.Max(0, SubTotal - OverallDiscount);
    public decimal ChangeAmount  => Math.Max(0, AmountPaid - TotalAmount);
    public decimal UnpaidBalance => Math.Max(0, TotalAmount - AmountPaid);
    public bool IsDeficit        => AmountPaid < TotalAmount && _cartItems.Count > 0;
    public decimal Balance       => AmountPaid - TotalAmount;

    public string BalanceLabel =>
        IsDeficit
            ? (IsRegisteredCustomer ? "Credit Amount:" : "Deficit (Full Payment Required):")
            : "Change:";

    public string BalanceDisplay =>
        IsDeficit
            ? (IsRegisteredCustomer ? $"Rs. {UnpaidBalance:N2}" : $"-Rs. {UnpaidBalance:N2}")
            : $"Rs. {ChangeAmount:N2}";

    public string BalanceColor =>
        IsDeficit
            ? (IsRegisteredCustomer ? "#D97706" : "#DC2626")
            : "#16A34A";

    public string ErrorMessage   { get => _errorMessage;   set { SetField(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError         => !string.IsNullOrEmpty(ErrorMessage);
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

                    Task.Delay(3000, token).ContinueWith(t =>
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
    public bool HasSuccess       => !string.IsNullOrEmpty(SuccessMessage);
    public bool IsBusy           { get => _isBusy;           set => SetField(ref _isBusy, value); }
    public bool HasLastCompletedSale => _lastCompletedSale is not null;

    public RelayCommand<ProductDto> AddToCartCommand     { get; }
    public RelayCommand<PosCartItem> RemoveFromCartCommand{ get; }
    public RelayCommand ProcessSaleCommand               { get; }
    public RelayCommand ClearCartCommand                 { get; }
    public RelayCommand ClearCustomerSelectionCommand    { get; }
    public RelayCommand RePrintLastReceiptCommand        { get; }

    public PosViewModel(
        IProductService productService,
        ICustomerService customerService,
        ISaleService saleService,
        SessionContext sessionContext,
        IReceiptPrinter printer,
        IStoreSettingService settingService)
    {
        _productService  = productService;
        _customerService = customerService;
        _saleService     = saleService;
        _sessionContext  = sessionContext;
        _printer         = printer;
        _settingService  = settingService;

        AddToCartCommand              = new RelayCommand<ProductDto>(AddToCart);
        RemoveFromCartCommand         = new RelayCommand<PosCartItem>(RemoveFromCart);
        ProcessSaleCommand            = new RelayCommand(async () => await ProcessSaleAsync(), CanProcessSale);
        ClearCartCommand              = new RelayCommand(ClearCart);
        ClearCustomerSelectionCommand = new RelayCommand(ClearCustomerSelection);
        RePrintLastReceiptCommand     = new RelayCommand(async () => await RePrintLastReceiptAsync(), () => _lastCompletedSale is not null);
    }

    private bool CanProcessSale()
    {
        if (IsBusy || _cartItems.Count == 0)
            return false;

        // Unregistered walk-in customers MUST pay in full
        if (SelectedCustomer is null && AmountPaid < TotalAmount)
            return false;

        return true;
    }

    public async Task InitializeAsync()
    {
        var prevSelectedId = SelectedCustomer?.Id;
        var prevCustomerQuery = CustomerSearchQuery;
        var prevDiscountOverridden = _isDiscountManuallyOverridden;
        var prevDiscount = OverallDiscount;
        var prevDiscountText = OverallDiscountText;

        try
        {
            _isRefreshingCustomers = true;
            var custs = await _customerService.GetActiveAsync();
            _customers.Clear();
            foreach (var c in custs) _customers.Add(c);
            FilterCustomers();
        }
        finally
        {
            _isRefreshingCustomers = false;
        }

        if (prevSelectedId.HasValue)
        {
            var matchedCustomer = _customers.FirstOrDefault(c => c.Id == prevSelectedId.Value);
            if (matchedCustomer is not null)
            {
                _selectedCustomer = matchedCustomer;
                _customerSearchQuery = matchedCustomer.DisplayName;
                OnPropertyChanged(nameof(SelectedCustomer));
                OnPropertyChanged(nameof(CustomerSearchQuery));

                if (prevDiscountOverridden)
                {
                    _isDiscountManuallyOverridden = true;
                    _overallDiscount = prevDiscount;
                    _overallDiscountText = prevDiscountText;
                    OnPropertyChanged(nameof(OverallDiscount));
                    OnPropertyChanged(nameof(OverallDiscountText));
                }
                else
                {
                    ApplyCustomerDiscountRate();
                }
                IsCustomerDropDownOpen = false;
                RecalculateTotals();
            }
        }
        else
        {
            IsCustomerDropDownOpen = false;
        }

        await PerformSearchAsync();
    }

    private void FilterCustomers()
    {
        var query = CustomerSearchQuery?.Trim().ToLower() ?? string.Empty;
        FilteredCustomers.Clear();

        var matches = string.IsNullOrWhiteSpace(query)
            ? _customers
            : _customers.Where(c => c.Name.ToLower().Contains(query) || (c.Phone != null && c.Phone.Contains(query)));

        foreach (var c in matches)
        {
            FilteredCustomers.Add(c);
        }

        if (!string.IsNullOrWhiteSpace(query) && FilteredCustomers.Any() && SelectedCustomer is null)
        {
            IsCustomerDropDownOpen = true;
        }
        else
        {
            IsCustomerDropDownOpen = false;
        }
    }

    private void ClearCustomerSelection()
    {
        _customerSearchQuery = string.Empty;
        OnPropertyChanged(nameof(CustomerSearchQuery));
        SelectedCustomer = null;
        IsCustomerDropDownOpen = false;
        FilterCustomers();
    }

    private async Task PerformSearchAsync()
    {
        IReadOnlyList<ProductDto> results;
        if (string.IsNullOrWhiteSpace(SearchQuery))
            results = await _productService.GetAllAsync();
        else
            results = await _productService.SearchAsync(SearchQuery);

        _searchResults.Clear();
        foreach (var p in results.Where(x => x.IsActive))
            _searchResults.Add(p);
    }

    private void AddToCart(ProductDto? product)
    {
        if (product is null) return;

        var existing = _cartItems.FirstOrDefault(i => i.Product.Id == product.Id);
        if (existing is not null)
        {
            if (existing.Quantity + 1 > product.StockQuantity)
            {
                ErrorMessage = $"Insufficient stock for {product.Name}. Available: {product.StockQuantity}";
                return;
            }
            existing.Quantity++;
        }
        else
        {
            if (product.StockQuantity < 1)
            {
                ErrorMessage = $"{product.Name} is out of stock.";
                return;
            }

            decimal price = SaleType == SaleType.Wholesale ? product.WholesalePrice : product.RetailPrice;
            var item = new PosCartItem(product, 1, price);
            item.PropertyChanged += (_, _) => RecalculateTotals();
            _cartItems.Add(item);
        }

        ErrorMessage = string.Empty;
        RecalculateTotals();
    }

    private void RemoveFromCart(PosCartItem? item)
    {
        if (item is null) return;
        _cartItems.Remove(item);
        RecalculateTotals();
    }

    private void UpdateCartPricesForSaleType()
    {
        foreach (var item in _cartItems)
        {
            item.UnitPrice = SaleType == SaleType.Wholesale
                ? item.Product.WholesalePrice
                : item.Product.RetailPrice;
        }
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        if (!_isDiscountManuallyOverridden && SelectedCustomer is not null && SelectedCustomer.DiscountRate > 0m)
        {
            ApplyCustomerDiscountRate();
        }

        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(ChangeAmount));
        OnPropertyChanged(nameof(IsDeficit));
        OnPropertyChanged(nameof(Balance));
        OnPropertyChanged(nameof(BalanceLabel));
        OnPropertyChanged(nameof(BalanceDisplay));
        OnPropertyChanged(nameof(BalanceColor));
        ProcessSaleCommand.RaiseCanExecuteChanged();
    }

    private void ApplyCustomerDiscountRate()
    {
        if (SelectedCustomer is not null && SelectedCustomer.DiscountRate > 0m)
        {
            var calculatedDiscount = Math.Round(SubTotal * (SelectedCustomer.DiscountRate / 100m), 2);
            _overallDiscount = calculatedDiscount;
            _overallDiscountText = calculatedDiscount == 0 ? string.Empty : calculatedDiscount.ToString("F2", CultureInfo.InvariantCulture);
            OnPropertyChanged(nameof(OverallDiscount));
            OnPropertyChanged(nameof(OverallDiscountText));
        }
        else if (!_isDiscountManuallyOverridden)
        {
            _overallDiscount = 0;
            _overallDiscountText = string.Empty;
            OnPropertyChanged(nameof(OverallDiscount));
            OnPropertyChanged(nameof(OverallDiscountText));
        }
    }

    private async Task ProcessSaleAsync()
    {
        ErrorMessage   = string.Empty;
        SuccessMessage = string.Empty;

        if (!_cartItems.Any())
        {
            ErrorMessage = "Cart is empty.";
            return;
        }

        if (SelectedCustomer is null && AmountPaid < TotalAmount)
        {
            ErrorMessage = $"Unregistered walk-in customers cannot make credit purchases. Amount paid (Rs. {AmountPaid:N2}) must be at least total amount (Rs. {TotalAmount:N2}).";
            return;
        }

        IsBusy = true;
        try
        {
            var userId = _sessionContext.CurrentUser?.UserId ?? 1;

            var items = _cartItems.Select(i => new CreateSaleItemRequest(
                i.Product.Id, i.Quantity, i.UnitPrice, i.Discount)).ToList();

            var req = new CreateSaleRequest(
                SaleType,
                userId,
                SelectedCustomer?.Id,
                OverallDiscount,
                AmountPaid,
                $"POS {SelectedPaymentMethod} Transaction",
                items,
                SelectedPaymentMethod);

            var sale = await _saleService.ProcessSaleAsync(req);
            _lastCompletedSale = sale;
            OnPropertyChanged(nameof(HasLastCompletedSale));
            RePrintLastReceiptCommand.RaiseCanExecuteChanged();

            if (sale.AmountPaid < sale.TotalAmount && sale.CustomerId.HasValue)
            {
                decimal unpaid = sale.TotalAmount - sale.AmountPaid;
                SuccessMessage = $"Sale #{sale.Id} processed on credit! Outstanding: Rs. {unpaid:N2}";
            }
            else
            {
                SuccessMessage = $"Sale #{sale.Id} completed! Change: Rs. {sale.Change:N2}";
            }

            // Auto-print to Epson LQ-310 if enabled in settings
            try
            {
                var printerConfig = await _settingService.GetPrinterSettingsAsync();
                if (printerConfig.AutoPrintEnabled)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _printer.PrintReceiptAsync(sale);
                        }
                        catch
                        {
                            // Error is logged within printer service
                        }
                    });
                    SuccessMessage += " [Bill sent to printer]";
                }
            }
            catch
            {
                // Non-blocking fallback
            }

            ClearCart();
            await InitializeAsync(); // Refresh active customers & products
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RePrintLastReceiptAsync()
    {
        if (_lastCompletedSale is null) return;
        try
        {
            await _printer.PrintReceiptAsync(_lastCompletedSale);
            SuccessMessage = $"Re-printed bill for Sale #{_lastCompletedSale.Id} successfully!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to re-print bill: {ex.Message}";
        }
    }

    private void ClearCart()
    {
        _cartItems.Clear();
        _isDiscountManuallyOverridden = false;
        OverallDiscount  = 0;
        _amountPaid       = 0;
        _amountPaidText   = "0";
        SelectedPaymentMethod = PaymentMethod.Cash;
        OnPropertyChanged(nameof(AmountPaidText));
        SelectedCustomer = null;
        SearchQuery      = string.Empty;
        SaleType         = SaleType.Retail;
        ErrorMessage     = string.Empty;
        IsCustomerDropDownOpen = false;
        FilterCustomers();
        RecalculateTotals();
    }
}
