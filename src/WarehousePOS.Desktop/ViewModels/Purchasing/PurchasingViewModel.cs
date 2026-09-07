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
    private readonly IPurchaseService   _purchaseService;
    private readonly IProductService    _productService;
    private readonly ISupplierService   _supplierService;
    private readonly INavigationService _nav;
    private readonly SessionContext    _session;

    private List<ProductDto> _allActiveProducts = [];
    private int? _selectedSupplierId;
    private string _notes = string.Empty;
    private string _selectedPaymentMethod = "Cash";
    private string _paidAmountText = "0.00";
    private string _paymentDetails = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;
    private bool _isPaidAmountUserModified;

    public ObservableCollection<SupplierDto> Suppliers          { get; } = [];
    public ObservableCollection<ProductDto>  AvailableProducts  { get; } = [];
    public ObservableCollection<PurchasingItemRowViewModel> LineItems { get; } = [];
    public ObservableCollection<string> PaymentMethods         { get; } = ["Cash", "Cheque", "Bank Transfer", "Credit / Unpaid"];

    public int? SelectedSupplierId
    {
        get => _selectedSupplierId;
        set
        {
            if (SetField(ref _selectedSupplierId, value))
            {
                FilterProductsForSelectedSupplier();
                OnPropertyChanged(nameof(SupplierError));
            }
        }
    }

    public string Notes
    {
        get => _notes;
        set => SetField(ref _notes, value);
    }

    public string SelectedPaymentMethod
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

    public string? SupplierError   => (!SelectedSupplierId.HasValue || SelectedSupplierId.Value <= 0) ? "Please select a supplier." : null;
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

    public decimal TotalOrderCost => LineItems.Sum(i => i.TotalCost);
    public int TotalPaidQuantity => LineItems.Sum(i => i.Quantity);
    public int TotalFreeQuantity => LineItems.Sum(i => i.FreeQuantity);
    public int TotalItemsCount => LineItems.Count;

    public event Action? CreateNewProductRequested;

    public RelayCommand AddLineItemCommand       { get; }
    public RelayCommand CreateNewProductCommand  { get; }
    public RelayCommand SaveAndReceiveCommand    { get; }
    public RelayCommand ResetOrderCommand        { get; }

    public PurchasingViewModel(
        IPurchaseService purchaseService,
        IProductService productService,
        ISupplierService supplierService,
        INavigationService nav,
        SessionContext session)
    {
        _purchaseService = purchaseService;
        _productService  = productService;
        _supplierService = supplierService;
        _nav             = nav;
        _session         = session;

        AddLineItemCommand      = new RelayCommand(AddLineItem);
        CreateNewProductCommand = new RelayCommand(() => CreateNewProductRequested?.Invoke());
        SaveAndReceiveCommand   = new RelayCommand(async () => await SaveAndReceiveAsync(), () => !IsBusy);
        ResetOrderCommand       = new RelayCommand(ResetOrder);

        LineItems.CollectionChanged += (_, _) => UpdateOrderSummary();
    }

    public async Task LoadAsync()
    {
        ErrorMessage = string.Empty;
        var suppliers = await _supplierService.GetActiveAsync();
        Suppliers.Clear();
        foreach (var s in suppliers) Suppliers.Add(s);

        var products = await _productService.GetAllAsync();
        _allActiveProducts = products.Where(p => p.IsActive).ToList();

        if (SelectedSupplierId == null && Suppliers.Count > 0)
        {
            SelectedSupplierId = Suppliers[0].Id;
        }
        else
        {
            FilterProductsForSelectedSupplier();
        }

        if (LineItems.Count == 0 && AvailableProducts.Count > 0)
        {
            AddLineItem();
        }

        UpdateOrderSummary();
    }

    private void FilterProductsForSelectedSupplier()
    {
        var selectedSupplier = Suppliers.FirstOrDefault(s => s.Id == SelectedSupplierId);
        AvailableProducts.Clear();

        if (selectedSupplier is not null && !string.IsNullOrWhiteSpace(selectedSupplier.ProvidedProducts))
        {
            var tokens = selectedSupplier.ProvidedProducts
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var matchedProducts = _allActiveProducts
                .Where(p => tokens.Any(t => t.Equals(p.Name, StringComparison.OrdinalIgnoreCase) ||
                                            t.Equals(p.SKU, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (matchedProducts.Any())
            {
                foreach (var p in matchedProducts) AvailableProducts.Add(p);
            }
            else
            {
                foreach (var p in _allActiveProducts) AvailableProducts.Add(p);
            }
        }
        else
        {
            foreach (var p in _allActiveProducts) AvailableProducts.Add(p);
        }

        foreach (var item in LineItems)
        {
            if (item.Product is null || !AvailableProducts.Any(p => p.Id == item.Product.Id))
            {
                if (AvailableProducts.Count > 0)
                {
                    item.Product = AvailableProducts[0];
                }
            }
        }
    }

    public void AddLineItem()
    {
        var row = new PurchasingItemRowViewModel();
        if (AvailableProducts.Count > 0)
        {
            row.Product = AvailableProducts[0];
        }

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
        LineItems.Clear();
        Notes = string.Empty;
        PaymentDetails = string.Empty;
        SelectedPaymentMethod = "Cash";
        _isPaidAmountUserModified = false;
        ErrorMessage = string.Empty;
        if (AvailableProducts.Count > 0)
        {
            AddLineItem();
        }
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
                SelectedPaymentMethod,
                PaidAmount,
                string.IsNullOrWhiteSpace(PaymentDetails) ? null : PaymentDetails.Trim());

            var createdPurchase = await _purchaseService.CreateAsync(createReq);
            await _purchaseService.ConfirmAsync(createdPurchase.Id);
            await _purchaseService.ReceiveStockAsync(createdPurchase.Id);

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
