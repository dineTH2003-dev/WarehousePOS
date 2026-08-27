using System.Collections.ObjectModel;
using WarehousePOS.Application.Products;
using WarehousePOS.Application.Sales;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels;
using WarehousePOS.Domain.Enums;

namespace WarehousePOS.Desktop.ViewModels.Sales;

public sealed class PosCartItem : ViewModelBase
{
    private int _quantity;
    private decimal _unitPrice;
    private decimal _discount;

    public ProductDto Product { get; }

    public int Quantity
    {
        get => _quantity;
        set { SetField(ref _quantity, value); OnPropertyChanged(nameof(LineTotal)); }
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

    private ObservableCollection<ProductDto>  _searchResults = [];
    private ObservableCollection<CustomerDto> _customers     = [];
    private ObservableCollection<PosCartItem> _cartItems     = [];

    private string      _searchQuery    = string.Empty;
    private CustomerDto? _selectedCustomer;
    private SaleType    _saleType       = SaleType.Retail;
    private decimal     _overallDiscount;
    private decimal     _amountPaid;
    private string      _errorMessage   = string.Empty;
    private bool        _isBusy;
    private string      _successMessage = string.Empty;

    public ObservableCollection<ProductDto>  SearchResults => _searchResults;
    public ObservableCollection<CustomerDto> Customers     => _customers;
    public ObservableCollection<PosCartItem> CartItems     => _cartItems;

    public string SearchQuery
    {
        get => _searchQuery;
        set { SetField(ref _searchQuery, value); _ = PerformSearchAsync(); }
    }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            SetField(ref _selectedCustomer, value);
            if (value is not null)
                SaleType = value.Type; // Auto-set SaleType based on customer preference
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
        set { SetField(ref _overallDiscount, value); RecalculateTotals(); }
    }

    public decimal AmountPaid
    {
        get => _amountPaid;
        set { SetField(ref _amountPaid, value); OnPropertyChanged(nameof(ChangeAmount)); }
    }

    public decimal SubTotal    => _cartItems.Sum(i => i.LineTotal);
    public decimal TotalAmount => Math.Max(0, SubTotal - OverallDiscount);
    public decimal ChangeAmount=> Math.Max(0, AmountPaid - TotalAmount);

    public string ErrorMessage   { get => _errorMessage;   set { SetField(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool HasError         => !string.IsNullOrEmpty(ErrorMessage);
    public string SuccessMessage { get => _successMessage; set { SetField(ref _successMessage, value); OnPropertyChanged(nameof(HasSuccess)); } }
    public bool HasSuccess       => !string.IsNullOrEmpty(SuccessMessage);
    public bool IsBusy           { get => _isBusy;           set => SetField(ref _isBusy, value); }

    public RelayCommand<ProductDto> AddToCartCommand     { get; }
    public RelayCommand<PosCartItem> RemoveFromCartCommand{ get; }
    public RelayCommand ProcessSaleCommand               { get; }
    public RelayCommand ClearCartCommand                 { get; }

    public PosViewModel(
        IProductService productService,
        ICustomerService customerService,
        ISaleService saleService,
        SessionContext sessionContext)
    {
        _productService  = productService;
        _customerService = customerService;
        _saleService     = saleService;
        _sessionContext  = sessionContext;

        AddToCartCommand      = new RelayCommand<ProductDto>(AddToCart);
        RemoveFromCartCommand = new RelayCommand<PosCartItem>(RemoveFromCart);
        ProcessSaleCommand    = new RelayCommand(async () => await ProcessSaleAsync(), () => !IsBusy && _cartItems.Count > 0);
        ClearCartCommand      = new RelayCommand(ClearCart);
    }

    public async Task InitializeAsync()
    {
        var custs = await _customerService.GetActiveAsync();
        _customers.Clear();
        foreach (var c in custs) _customers.Add(c);

        await PerformSearchAsync();
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
        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(TotalAmount));
        OnPropertyChanged(nameof(ChangeAmount));
        ProcessSaleCommand.RaiseCanExecuteChanged();
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

        if (AmountPaid < TotalAmount)
        {
            ErrorMessage = $"Amount paid (Rs. {AmountPaid:N2}) must be at least total amount (Rs. {TotalAmount:N2}).";
            return;
        }

        IsBusy = true;
        try
        {
            var userId = _sessionContext.CurrentUser?.Id ?? 1;

            var items = _cartItems.Select(i => new CreateSaleItemRequest(
                i.Product.Id, i.Quantity, i.UnitPrice, i.Discount)).ToList();

            var req = new CreateSaleRequest(
                SaleType,
                userId,
                SelectedCustomer?.Id,
                OverallDiscount,
                AmountPaid,
                "POS Cash Transaction",
                items);

            var sale = await _saleService.ProcessSaleAsync(req);
            SuccessMessage = $"Sale #{sale.Id} completed! Change: Rs. {sale.Change:N2}";

            ClearCart();
            await PerformSearchAsync(); // Refresh product stock levels
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

    private void ClearCart()
    {
        _cartItems.Clear();
        OverallDiscount  = 0;
        AmountPaid       = 0;
        SelectedCustomer = null;
        RecalculateTotals();
    }
}
