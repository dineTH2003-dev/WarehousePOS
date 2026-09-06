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

    private int? _selectedSupplierId;
    private string _notes = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public ObservableCollection<SupplierDto> Suppliers          { get; } = [];
    public ObservableCollection<ProductDto>  AvailableProducts  { get; } = [];
    public ObservableCollection<PurchasingItemRowViewModel> LineItems { get; } = [];

    public int? SelectedSupplierId
    {
        get => _selectedSupplierId;
        set => SetField(ref _selectedSupplierId, value);
    }

    public string Notes
    {
        get => _notes;
        set => SetField(ref _notes, value);
    }

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
        AvailableProducts.Clear();
        foreach (var p in products.Where(p => p.IsActive)) AvailableProducts.Add(p);

        if (SelectedSupplierId == null && Suppliers.Count > 0)
        {
            SelectedSupplierId = Suppliers[0].Id;
        }

        if (LineItems.Count == 0 && AvailableProducts.Count > 0)
        {
            AddLineItem();
        }

        UpdateOrderSummary();
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
    }

    private void ResetOrder()
    {
        LineItems.Clear();
        Notes = string.Empty;
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
                i.WholesalePrice)).ToList();

            var currentUserId = _session.IsLoggedIn ? _session.CurrentUser.UserId : 1;

            var createReq = new CreatePurchaseRequest(
                SelectedSupplierId.Value,
                currentUserId,
                string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
                itemRequests);

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
