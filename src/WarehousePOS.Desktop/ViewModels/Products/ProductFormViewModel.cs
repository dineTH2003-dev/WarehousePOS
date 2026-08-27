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
        SaveCommand   = new RelayCommand(async () => await SaveAsync(), () => !IsBusy);
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
            CategoryId         = existing.CategoryId;
            ReorderLevel       = existing.ReorderLevel;
        }
        else
        {
            _editingId = null;
            CategoryId = cats.FirstOrDefault()?.Id ?? 0;
        }

        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(SKU));
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Name))    { ErrorMessage = "Name is required.";     return; }
        if (!IsEditMode && string.IsNullOrWhiteSpace(_sku)) { ErrorMessage = "SKU is required."; return; }
        if (!decimal.TryParse(RetailPriceText,    out var retail))    { ErrorMessage = "Invalid retail price.";    return; }
        if (!decimal.TryParse(WholesalePriceText, out var wholesale)) { ErrorMessage = "Invalid wholesale price."; return; }
        if (CategoryId == 0) { ErrorMessage = "Please select a category."; return; }

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                await _productService.UpdateAsync(new UpdateProductRequest(
                    _editingId!.Value, Name, string.IsNullOrWhiteSpace(Barcode) ? null : Barcode,
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    retail, wholesale, CategoryId, ReorderLevel));
            }
            else
            {
                await _productService.CreateAsync(new CreateProductRequest(
                    Name, _sku.Trim(),
                    string.IsNullOrWhiteSpace(Barcode) ? null : Barcode,
                    string.IsNullOrWhiteSpace(Description) ? null : Description,
                    retail, wholesale, CategoryId, ReorderLevel));
            }
            SaveCompleted?.Invoke();
        }
        catch (Exception ex) { ErrorMessage = ex.Message; }
        finally { IsBusy = false; }
    }
}
