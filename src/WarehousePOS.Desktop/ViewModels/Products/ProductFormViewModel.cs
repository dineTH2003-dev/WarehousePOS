using System.Collections.ObjectModel;
using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Products;

public sealed class ProductFormViewModel : ViewModelBase
{
    private readonly IProductService  _productService;
    private readonly ICategoryService _categoryService;

    private int? _editingId;
    private string _name        = string.Empty;
    private string _sku         = string.Empty;
    private string _barcode     = string.Empty;
    private string _description = string.Empty;
    private string _retailPriceText    = "0.00";
    private string _wholesalePriceText = "0.00";
    private string _stockQuantityText = "0";
    private int    _categoryId;
    private int    _reorderLevel = 5;
    private string _errorMessage = string.Empty;
    private bool   _isBusy;

    public ObservableCollection<CategoryDto> Categories { get; } = [];

    public string Name              { get => _name;              set => SetField(ref _name, value); }
    public string SKU               { get => _sku;               set => SetField(ref _sku, value); }
    public string Barcode           { get => _barcode;           set => SetField(ref _barcode, value); }
    public string Description       { get => _description;       set => SetField(ref _description, value); }
    public string RetailPriceText   { get => _retailPriceText;   set => SetField(ref _retailPriceText, value); }
    public string WholesalePriceText{ get => _wholesalePriceText;set => SetField(ref _wholesalePriceText, value); }
    public string StockQuantityText { get => _stockQuantityText; set { if (SetField(ref _stockQuantityText, value)) RefreshStockValidation(); } }
    public int    CategoryId        { get => _categoryId;         set => SetField(ref _categoryId, value); }
    public int    ReorderLevel      { get => _reorderLevel;       set => SetField(ref _reorderLevel, value); }
    public string ErrorMessage      { get => _errorMessage;       set { SetField(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool   HasError          => !string.IsNullOrEmpty(ErrorMessage);
    public bool   IsBusy            { get => _isBusy;             set => SetField(ref _isBusy, value); }
    public bool   IsEditMode        => _editingId.HasValue;
    public string Title             => IsEditMode ? "Edit Product" : "New Product";

    public event Action? SaveCompleted;

    public RelayCommand SaveCommand   { get; }
    public RelayCommand CancelCommand { get; }

    public ProductFormViewModel(IProductService productService, ICategoryService categoryService)
    {
        _productService  = productService;
        _categoryService = categoryService;
        SaveCommand   = new RelayCommand(async () => await SaveAsync(), () => !IsBusy && IsStockQuantityValid());
        CancelCommand = new RelayCommand(() => SaveCompleted?.Invoke());
    }

    public async Task LoadAsync(ProductDto? existing = null)
    {
        var cats = await _categoryService.GetActiveAsync();
        Categories.Clear();
        foreach (var c in cats) Categories.Add(c);

        if (existing is not null)
        {
            _editingId         = existing.Id;
            Name               = existing.Name;
            _sku               = existing.SKU;   // SKU is not editable after creation
            Barcode            = existing.Barcode ?? string.Empty;
            Description        = existing.Description ?? string.Empty;
            RetailPriceText    = existing.RetailPrice.ToString("F2");
            WholesalePriceText = existing.WholesalePrice.ToString("F2");
            StockQuantityText = existing.StockQuantity.ToString();
            CategoryId         = existing.CategoryId;
            ReorderLevel       = existing.ReorderLevel;
        }
        else
        {
            _editingId = null;
            Name = string.Empty;
            SKU = string.Empty;
            Barcode = string.Empty;
            Description = string.Empty;
            RetailPriceText = "0.00";
            WholesalePriceText = "0.00";
            StockQuantityText = "0";
            CategoryId = cats.FirstOrDefault()?.Id ?? 0;
            ReorderLevel = 5;
            ErrorMessage = string.Empty;
        }

        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(Title));
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Name))    { ErrorMessage = "Name is required.";     return; }
        if (!IsEditMode && string.IsNullOrWhiteSpace(_sku)) { ErrorMessage = "SKU is required."; return; }
        if (!decimal.TryParse(RetailPriceText,    out var retail))    { ErrorMessage = "Invalid retail price.";    return; }
        if (!decimal.TryParse(WholesalePriceText, out var wholesale)) { ErrorMessage = "Invalid wholesale price."; return; }
        if (!TryParseStockQuantity(out var stockQuantity))
        { ErrorMessage = "Stock quantity must be a non-negative whole number."; return; }
        if (CategoryId == 0) { ErrorMessage = "Please select a category."; return; }

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                await _productService.UpdateAsync(new UpdateProductRequest(
                    _editingId!.Value, Name, string.IsNullOrWhiteSpace(Barcode) ? null : Barcode,
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    retail, wholesale, CategoryId, ReorderLevel, stockQuantity));
            }
            else
            {
                await _productService.CreateAsync(new CreateProductRequest(
                    Name, _sku.Trim(),
                    string.IsNullOrWhiteSpace(Barcode) ? null : Barcode,
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    retail, wholesale, CategoryId, ReorderLevel, stockQuantity));
            }
            SaveCompleted?.Invoke();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }

    private void RefreshStockValidation()
    {
        if (!IsStockQuantityValid())
            ErrorMessage = "Stock quantity must be a non-negative whole number.";
        else if (ErrorMessage == "Stock quantity must be a non-negative whole number.")
            ErrorMessage = string.Empty;

        SaveCommand.RaiseCanExecuteChanged();
    }

    private bool IsStockQuantityValid() =>
        TryParseStockQuantity(out _);

    private bool TryParseStockQuantity(out int stockQuantity)
    {
        stockQuantity = 0;
        return !string.IsNullOrEmpty(StockQuantityText) &&
               StockQuantityText.All(character => character is >= '0' and <= '9') &&
               int.TryParse(StockQuantityText, out stockQuantity);
    }
}
