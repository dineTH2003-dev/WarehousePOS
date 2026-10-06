using System.Collections.ObjectModel;
using System.Windows.Input;
using WarehousePOS.Application.Products;
using WarehousePOS.Application.Suppliers;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Suppliers;

public sealed class SupplierEntitlementFormViewModel : ViewModelBase
{
    private readonly ISupplierEntitlementService _entitlementService;
    private readonly int _supplierId;
    private readonly string _supplierName;

    private ProductDto? _selectedProduct;
    private string _nature = "Receiving Free Item";
    private string _quantityText = "0";
    private string _valueText = "0.00";
    private DateTime _eventDate = DateTime.Today;
    private bool _hasNextEntitlementDate;
    private DateTime _nextEntitlementDate = DateTime.Today.AddMonths(1);
    private string? _specialNotes;

    private string? _productError;
    private string? _natureError;
    private string? _quantityError;
    private string? _valueError;
    private string? _errorMessage;
    private bool _hasError;

    public SupplierEntitlementFormViewModel(
        ISupplierEntitlementService entitlementService,
        int supplierId,
        string supplierName,
        IEnumerable<ProductDto> availableProducts)
    {
        _entitlementService = entitlementService ?? throw new ArgumentNullException(nameof(entitlementService));
        _supplierId = supplierId;
        _supplierName = supplierName;
        AvailableProducts = new ObservableCollection<ProductDto>(availableProducts);

        if (AvailableProducts.Count > 0)
        {
            SelectedProduct = AvailableProducts[0];
        }

        NatureOptions = new List<string>
        {
            "Receiving Free Item",
            "Obtaining Discount",
            "Making Purchase",
            "Special Entitlement"
        };

        SaveCommand = new RelayCommand(async () => await SaveAsync(), CanSave);
        CancelCommand = new RelayCommand(() => CloseRequested?.Invoke(this, false));
    }

    public string Title => $"Record Entitlement / Benefit - {_supplierName}";
    public ObservableCollection<ProductDto> AvailableProducts { get; }
    public List<string> NatureOptions { get; }

    public event EventHandler<bool>? CloseRequested;

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetField(ref _selectedProduct, value))
            {
                ProductError = null;
            }
        }
    }

    public string Nature
    {
        get => _nature;
        set
        {
            if (SetField(ref _nature, value))
            {
                NatureError = null;
                OnPropertyChanged(nameof(IsQuantityRequired));
                OnPropertyChanged(nameof(IsValueRequired));
            }
        }
    }

    public string QuantityText
    {
        get => _quantityText;
        set
        {
            if (SetField(ref _quantityText, value))
            {
                QuantityError = null;
            }
        }
    }

    public string ValueText
    {
        get => _valueText;
        set
        {
            if (SetField(ref _valueText, value))
            {
                ValueError = null;
            }
        }
    }

    public DateTime EventDate
    {
        get => _eventDate;
        set => SetField(ref _eventDate, value);
    }

    public bool HasNextEntitlementDate
    {
        get => _hasNextEntitlementDate;
        set => SetField(ref _hasNextEntitlementDate, value);
    }

    public DateTime NextEntitlementDate
    {
        get => _nextEntitlementDate;
        set => SetField(ref _nextEntitlementDate, value);
    }

    public string? SpecialNotes
    {
        get => _specialNotes;
        set => SetField(ref _specialNotes, value);
    }

    public bool IsQuantityRequired => Nature == "Receiving Free Item" || Nature == "Making Purchase";
    public bool IsValueRequired => Nature == "Obtaining Discount";

    public string? ProductError
    {
        get => _productError;
        set => SetField(ref _productError, value);
    }

    public string? NatureError
    {
        get => _natureError;
        set => SetField(ref _natureError, value);
    }

    public string? QuantityError
    {
        get => _quantityError;
        set => SetField(ref _quantityError, value);
    }

    public string? ValueError
    {
        get => _valueError;
        set => SetField(ref _valueError, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public bool HasError
    {
        get => _hasError;
        set => SetField(ref _hasError, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanSave() => SelectedProduct is not null && !string.IsNullOrWhiteSpace(Nature);

    private async Task SaveAsync()
    {
        ClearErrors();

        if (SelectedProduct is null)
        {
            ProductError = "Please select a product.";
            HasError = true;
            ErrorMessage = "Product selection is required.";
            return;
        }

        if (string.IsNullOrWhiteSpace(Nature))
        {
            NatureError = "Please select or enter the nature/status.";
            HasError = true;
            ErrorMessage = "Status/Nature is required.";
            return;
        }

        int? qty = null;
        if (int.TryParse(QuantityText, out var parsedQty) && parsedQty > 0)
        {
            qty = parsedQty;
        }

        decimal? val = null;
        if (decimal.TryParse(ValueText, out var parsedVal) && parsedVal > 0)
        {
            val = parsedVal;
        }

        if (IsQuantityRequired && (!qty.HasValue || qty.Value <= 0))
        {
            QuantityError = "Please enter a valid quantity greater than 0.";
            HasError = true;
            ErrorMessage = "Quantity is required for this entitlement nature.";
            return;
        }

        if (IsValueRequired && (!val.HasValue || val.Value <= 0))
        {
            ValueError = "Please enter a valid discount amount greater than 0.";
            HasError = true;
            ErrorMessage = "Monetary discount value is required for this entitlement nature.";
            return;
        }

        try
        {
            var dto = new CreateSupplierEntitlementDto(
                SupplierId: _supplierId,
                ProductId: SelectedProduct.Id,
                Nature: Nature,
                EventDate: EventDate.Date,
                Quantity: qty,
                Value: val,
                NextEntitlementDate: HasNextEntitlementDate ? NextEntitlementDate.Date : null,
                SpecialNotes: SpecialNotes
            );

            await _entitlementService.CreateEntitlementAsync(dto);
            CloseRequested?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
        }
    }

    private void ClearErrors()
    {
        ProductError = null;
        NatureError = null;
        QuantityError = null;
        ValueError = null;
        ErrorMessage = null;
        HasError = false;
    }
}
