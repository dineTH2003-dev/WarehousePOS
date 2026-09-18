using System.Collections.ObjectModel;
using WarehousePOS.Application.Products;
using WarehousePOS.Application.Purchasing;
using WarehousePOS.Application.Suppliers;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels;
using WarehousePOS.Desktop.ViewModels.Products;

namespace WarehousePOS.Desktop.ViewModels.Purchasing;

public sealed class PurchasingViewModel : ViewModelBase
{
    private readonly IPurchaseService _purchaseService;
    private readonly IProductService _productService;
    private readonly ISupplierService _supplierService;
    private readonly ISupplierEntitlementService _entitlementService;
    private readonly INavigationService _nav;
    private readonly SessionContext _session;

    private List<ProductDto> _allActiveProducts = [];
    private int? _selectedSupplierId;
    private string _notes = string.Empty;
    private string? _selectedPaymentMethod;
    private string _paidAmountText = "0.00";
    private string _paymentDetails = string.Empty;
    private bool _isBusy;
    private bool _isPaidAmountUserModified;
    private bool _showAllProducts;
    private decimal _discountAmount;
    private string _discountAmountText = "0.00";
    private string _errorMessage = string.Empty;

    public ObservableCollection<SupplierDto> Suppliers { get; } = [];
    public ObservableCollection<ProductDto> AvailableProducts { get; } = [];
    public ObservableCollection<PurchasingItemRowViewModel> LineItems { get; } = [];
    public ObservableCollection<SupplierProductEntitlementDto> Entitlements { get; } = [];
    public ObservableCollection<string> PaymentMethods { get; } = ["Cash", "Cheque", "Bank Transfer", "Credit / Unpaid"];

    public bool ShowAllProducts
    {
        get => _showAllProducts;
        set
        {
            if (SetField(ref _showAllProducts, value))
            {
                FilterProductsForSelectedSupplier();
            }
        }
    }

    public string DiscountAmountText
    {
        get => _discountAmountText;
        set
        {
            if (SetField(ref _discountAmountText, value))
            {
                if (decimal.TryParse(value, out var d) && d >= 0)
                {
                    _discountAmount = d;
                }
                else
                {
                    _discountAmount = 0;
                }
                OnPropertyChanged(nameof(DiscountAmount));
                UpdateOrderSummary();
            }
        }
    }

    public decimal DiscountAmount => _discountAmount;

    public int? SelectedSupplierId
    {
        get => _selectedSupplierId;
        set
        {
            if (SetField(ref _selectedSupplierId, value))
            {
                FilterProductsForSelectedSupplier();
                OnPropertyChanged(nameof(SupplierError));
                _ = LoadEntitlementsForSelectedSupplierAsync();
            }
        }
    }

    public string Notes
    {
        get => _notes;
        set => SetField(ref _notes, value);
    }

    public string? SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set => SetField(ref _selectedPaymentMethod, value);
    }

    public string PaidAmountText
    {
        get => _paidAmountText;
        set
        {
            if (SetField(ref _paidAmountText, value))
            {
                _isPaidAmountUserModified = true;
                OnPropertyChanged(nameof(PaidAmount));
                OnPropertyChanged(nameof(RemainingBalance));
                OnPropertyChanged(nameof(PaidAmountError));
            }
        }
    }

    public string? SupplierError => (!SelectedSupplierId.HasValue || SelectedSupplierId.Value <= 0) ? "Please select a supplier." : null;
    public string? PaidAmountError => (!decimal.TryParse(PaidAmountText, out var val) || val < 0) ? "Paid amount must be a valid number." : null;

    public string PaymentDetails
    {
        get => _paymentDetails;
        set => SetField(ref _paymentDetails, value);
    }

    public decimal PaidAmount => decimal.TryParse(PaidAmountText, out var val) ? Math.Max(0, val) : 0;
    public decimal RemainingBalance => Math.Max(0, TotalOrderCost - PaidAmount);

    public string ErrorMessage
    {
        get => _errorMessage;
        set { SetField(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool IsBusy
    {
        get => _isBusy;
        set { SetField(ref _isBusy, value); SaveAndReceiveCommand.RaiseCanExecuteChanged(); }
    }

    public decimal GrossTotal => LineItems.Sum(i => i.TotalCost);
    public decimal TotalOrderCost => Math.Max(0, GrossTotal - DiscountAmount);
    public int TotalPaidQuantity => LineItems.Sum(i => i.Quantity);
    public int TotalFreeQuantity => LineItems.Sum(i => i.FreeQuantity);
    public int TotalItemsCount => LineItems.Count;

    public event Action? CreateNewProductRequested;
    public event Action? CreateNewSupplierRequested;
    public event Action<int, string, IEnumerable<ProductDto>>? RecordEntitlementRequested;

    public RelayCommand AddLineItemCommand { get; }
    public RelayCommand CreateNewProductCommand { get; }
    public RelayCommand CreateNewSupplierCommand { get; }
    public RelayCommand SaveAndReceiveCommand { get; }
    public RelayCommand ResetOrderCommand { get; }
    public RelayCommand RecordEntitlementCommand { get; }

    public PurchasingViewModel(
        IPurchaseService purchaseService,
        IProductService productService,
        ISupplierService supplierService,
        ISupplierEntitlementService entitlementService,
        INavigationService nav,
        SessionContext session)
    {
        _purchaseService = purchaseService;
        _productService = productService;
        _supplierService = supplierService;
        _entitlementService = entitlementService;
        _nav = nav;
        _session = session;

        AddLineItemCommand = new RelayCommand(AddLineItem);
        CreateNewProductCommand = new RelayCommand(() => CreateNewProductRequested?.Invoke());
        CreateNewSupplierCommand = new RelayCommand(() => CreateNewSupplierRequested?.Invoke());
        SaveAndReceiveCommand = new RelayCommand(async () => await SaveAndReceiveAsync(), () => !IsBusy);
        ResetOrderCommand = new RelayCommand(ResetOrder);
        RecordEntitlementCommand = new RelayCommand(OnRecordEntitlement);

        LineItems.CollectionChanged += (_, _) => UpdateOrderSummary();
    }

    public async Task LoadAsync()
    {
        ErrorMessage = string.Empty;
        var currentSupplierId = SelectedSupplierId;

        var suppliers = await _supplierService.GetActiveAsync();
        Suppliers.Clear();
        foreach (var s in suppliers) Suppliers.Add(s);

        var products = await _productService.GetAllAsync();
        _allActiveProducts = products.Where(p => p.IsActive).ToList();

        if (currentSupplierId.HasValue && Suppliers.Any(s => s.Id == currentSupplierId.Value))
        {
            _selectedSupplierId = currentSupplierId.Value;
            OnPropertyChanged(nameof(SelectedSupplierId));
            FilterProductsForSelectedSupplier();
            await LoadEntitlementsForSelectedSupplierAsync();
        }
        else
        {
            _selectedSupplierId = null;
            OnPropertyChanged(nameof(SelectedSupplierId));
            FilterProductsForSelectedSupplier();
            Entitlements.Clear();
        }

        if (LineItems.Count == 0)
        {
            AddLineItem();
        }

        UpdateOrderSummary();
    }

    public async Task LoadEntitlementsForSelectedSupplierAsync()
    {
        Entitlements.Clear();
        if (!SelectedSupplierId.HasValue || SelectedSupplierId.Value <= 0)
            return;

        try
        {
            var records = await _entitlementService.GetEntitlementsBySupplierAsync(SelectedSupplierId.Value);
            foreach (var r in records)
            {
                Entitlements.Add(r);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading supplier entitlements: {ex.Message}");
        }
    }

    private void OnRecordEntitlement()
    {
        if (!SelectedSupplierId.HasValue || SelectedSupplierId.Value <= 0)
        {
            ErrorMessage = "Please select a supplier first before recording entitlements.";
            return;
        }

        var supplier = Suppliers.FirstOrDefault(s => s.Id == SelectedSupplierId.Value);
        var supplierName = supplier?.Name ?? "Selected Supplier";

        // Pass available products (or all active products if list empty)
        IEnumerable<ProductDto> productsToPass = AvailableProducts.Count > 0 ? AvailableProducts : _allActiveProducts;
        RecordEntitlementRequested?.Invoke(SelectedSupplierId.Value, supplierName, productsToPass);
    }

    private void FilterProductsForSelectedSupplier()
    {
        var selectedSupplier = Suppliers.FirstOrDefault(s => s.Id == SelectedSupplierId);
        AvailableProducts.Clear();

        List<ProductDto> matchedProducts = [];
        if (!_showAllProducts && selectedSupplier is not null && !string.IsNullOrWhiteSpace(selectedSupplier.ProvidedProducts))
        {
            var tokens = selectedSupplier.ProvidedProducts
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            matchedProducts = _allActiveProducts
                .Where(p => tokens.Any(t => t.Equals(p.Name, StringComparison.OrdinalIgnoreCase) ||
                                            t.Equals(p.SKU, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        // Fallback: If "Show All Products" is selected, or no products matched, or supplier has no configured products, show all active products!
        if (_showAllProducts || matchedProducts.Count == 0)
        {
            matchedProducts = _allActiveProducts;
        }

        foreach (var p in matchedProducts) AvailableProducts.Add(p);

        foreach (var item in LineItems)
        {
            if (item.Product is not null && !AvailableProducts.Any(p => p.Id == item.Product.Id))
            {
                item.Product = null;
            }
        }
    }

    public void AddLineItem()
    {
        var row = new PurchasingItemRowViewModel();

        row.RemoveRequested += OnRowRemoveRequested;
        row.PropertyChanged += (_, _) => UpdateOrderSummary();
        LineItems.Add(row);
        UpdateOrderSummary();
    }

    public void AddNewlyCreatedProduct(ProductDto newProduct)
    {
        if (!_allActiveProducts.Any(p => p.Id == newProduct.Id))
            _allActiveProducts.Add(newProduct);

        if (!AvailableProducts.Any(p => p.Id == newProduct.Id))
            AvailableProducts.Add(newProduct);

        var row = new PurchasingItemRowViewModel
        {
            Product = newProduct
        };
        row.RemoveRequested += OnRowRemoveRequested;
        row.PropertyChanged += (_, _) => UpdateOrderSummary();
        LineItems.Add(row);
        UpdateOrderSummary();
    }

    private void OnRowRemoveRequested(PurchasingItemRowViewModel row)
    {
        row.RemoveRequested -= OnRowRemoveRequested;
        LineItems.Remove(row);
        UpdateOrderSummary();
    }

    private void UpdateOrderSummary()
    {
        OnPropertyChanged(nameof(GrossTotal));
        OnPropertyChanged(nameof(TotalOrderCost));
        OnPropertyChanged(nameof(TotalPaidQuantity));
        OnPropertyChanged(nameof(TotalFreeQuantity));
        OnPropertyChanged(nameof(TotalItemsCount));

        if (!_isPaidAmountUserModified)
        {
            _paidAmountText = TotalOrderCost.ToString("F2");
            OnPropertyChanged(nameof(PaidAmountText));
        }

        OnPropertyChanged(nameof(PaidAmount));
        OnPropertyChanged(nameof(RemainingBalance));
    }

    private void ResetOrder()
    {
        _selectedSupplierId = null;
        OnPropertyChanged(nameof(SelectedSupplierId));
        OnPropertyChanged(nameof(SupplierError));

        _selectedPaymentMethod = null;
        OnPropertyChanged(nameof(SelectedPaymentMethod));

        _showAllProducts = false;
        OnPropertyChanged(nameof(ShowAllProducts));

        _discountAmount = 0;
        _discountAmountText = "0.00";
        OnPropertyChanged(nameof(DiscountAmountText));
        OnPropertyChanged(nameof(DiscountAmount));
        OnPropertyChanged(nameof(GrossTotal));

        Notes = string.Empty;
        PaymentDetails = string.Empty;
        _paidAmountText = "0.00";
        OnPropertyChanged(nameof(PaidAmountText));
        OnPropertyChanged(nameof(PaidAmount));
        OnPropertyChanged(nameof(RemainingBalance));

        _isPaidAmountUserModified = false;
        ErrorMessage = string.Empty;

        LineItems.Clear();
        Entitlements.Clear();
        FilterProductsForSelectedSupplier();

        AddLineItem();
        UpdateOrderSummary();
    }

    private async Task SaveAndReceiveAsync()
    {
        ErrorMessage = string.Empty;

        if (!SelectedSupplierId.HasValue || SelectedSupplierId.Value <= 0)
        {
            ErrorMessage = "Please select a supplier.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedPaymentMethod))
        {
            ErrorMessage = "Please select a payment method.";
            return;
        }

        if (LineItems.Count == 0)
        {
            ErrorMessage = "Please add at least one product line item.";
            return;
        }

        var validItems = LineItems.Where(i => i.Product is not null && i.TotalQuantity > 0).ToList();
        if (validItems.Count == 0)
        {
            ErrorMessage = "All line items must have a valid product and quantity greater than zero.";
            return;
        }

        if (validItems.Any(i => i.Quantity > 0 && i.UnitCost <= 0))
        {
            ErrorMessage = "Unit cost must be entered for all paid purchase items.";
            return;
        }

        IsBusy = true;
        try
        {
            var itemRequests = validItems.Select(i => new CreatePurchaseItemRequest(
                i.Product!.Id,
                i.Quantity,
                i.UnitCost,
                i.FreeQuantity,
                i.RetailPrice,
                i.WholesalePrice,
                i.ClaimedQuantityReceived)).ToList();

            var currentUserId = _session.IsLoggedIn ? _session.CurrentUser.UserId : 1;

            var createReq = new CreatePurchaseRequest(
                SelectedSupplierId.Value,
                currentUserId,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                itemRequests,
                SelectedPaymentMethod!,
                PaidAmount,
                string.IsNullOrWhiteSpace(PaymentDetails) ? null : PaymentDetails.Trim(),
                DiscountAmount);

            var createdPurchase = await _purchaseService.CreateAsync(createReq);
            await _purchaseService.ConfirmAsync(createdPurchase.Id);
            await _purchaseService.ReceiveStockAsync(createdPurchase.Id);

            // Auto-record purchase and free item entitlements for the ledger
            foreach (var item in validItems)
            {
                try
                {
                    if (item.Quantity > 0)
                    {
                        await _entitlementService.CreateEntitlementAsync(new CreateSupplierEntitlementDto(
                            SupplierId: SelectedSupplierId.Value,
                            ProductId: item.Product!.Id,
                            Nature: "Making Purchase",
                            EventDate: DateTime.Today,
                            Quantity: item.Quantity,
                            Value: item.TotalCost,
                            NextEntitlementDate: null,
                            SpecialNotes: $"Auto-recorded from Stock Purchase Order #{createdPurchase.Id}"
                        ));
                    }

                    if (item.FreeQuantity > 0)
                    {
                        await _entitlementService.CreateEntitlementAsync(new CreateSupplierEntitlementDto(
                            SupplierId: SelectedSupplierId.Value,
                            ProductId: item.Product!.Id,
                            Nature: "Receiving Free Item",
                            EventDate: DateTime.Today,
                            Quantity: item.FreeQuantity,
                            Value: null,
                            NextEntitlementDate: null,
                            SpecialNotes: $"Auto-recorded from Stock Purchase Order #{createdPurchase.Id}"
                        ));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error auto-recording entitlement: {ex.Message}");
                }
            }

            ResetOrder();
            _nav.NavigateTo<ProductListViewModel>();
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
}
