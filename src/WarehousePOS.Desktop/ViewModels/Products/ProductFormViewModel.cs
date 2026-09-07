using System.Collections.ObjectModel;
using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Products;

public sealed class ProductFormViewModel : ViewModelBase
{
    private readonly IProductService  _productService;
    private readonly ICategoryService _categoryService;
    private readonly SessionContext   _session;

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
    private bool   _isDuplicate;
    private CancellationTokenSource? _validationCts;

    public ObservableCollection<CategoryDto> Categories { get; } = [];

    public string Name
    {
        get => _name;
        set
        {
            if (SetField(ref _name, value))
            {
                _ = ValidateUniquenessAsync();
            }
        }
    }

    public string SKU
    {
        get => _sku;
        set
        {
            if (SetField(ref _sku, value))
            {
                _ = ValidateUniquenessAsync();
            }
        }
    }

    public string Barcode           { get => _barcode;           set => SetField(ref _barcode, value); }
    public string Description       { get => _description;       set => SetField(ref _description, value); }
    public string RetailPriceText   { get => _retailPriceText;   set => SetField(ref _retailPriceText, value); }
    public string WholesalePriceText{ get => _wholesalePriceText;set => SetField(ref _wholesalePriceText, value); }
    public string StockQuantityText { get => _stockQuantityText; set { if (SetField(ref _stockQuantityText, value)) RefreshStockValidation(); } }
    public string CategoryIdText    { get => _categoryId.ToString(); }
    public int    CategoryId        { get => _categoryId;         set { if (SetField(ref _categoryId, value)) OnPropertyChanged(nameof(CategoryError)); } }
    public int    ReorderLevel      { get => _reorderLevel;       set { if (SetField(ref _reorderLevel, value)) OnPropertyChanged(nameof(ReorderLevelError)); } }

    public string? NameError         => string.IsNullOrWhiteSpace(Name) ? "Product Name is required." : (_isDuplicate && ErrorMessage == "This product already exists." ? ErrorMessage : null);
    public string? SkuError          => string.IsNullOrWhiteSpace(SKU) ? "SKU is required." : (_isDuplicate && ErrorMessage.StartsWith("This SKU") ? ErrorMessage : null);
    public string? CategoryError     => CategoryId == 0 ? "Please select a category." : null;
    public string? ReorderLevelError => ReorderLevel < 0 ? "Reorder level must be 0 or greater." : null;

    public string ErrorMessage      { get => _errorMessage;       set { SetField(ref _errorMessage, value); OnPropertyChanged(nameof(HasError)); } }
    public bool   HasError          => !string.IsNullOrEmpty(ErrorMessage);
    public bool   IsBusy            { get => _isBusy;             set { SetField(ref _isBusy, value); SaveCommand.RaiseCanExecuteChanged(); OnPropertyChanged(nameof(CanSave)); } }
    public bool   IsEditMode        => _editingId.HasValue;
    public string Title             => IsEditMode ? "Edit Product" : "New Product";
    public bool   IsAdmin           => _session.IsAdmin;
    public bool   CanSave           => !IsBusy && IsStockQuantityValid() && !_isDuplicate;

    public event Action? SaveCompleted;
    public event Action? AddCategoryRequested;
    public event Action? ManageCategoriesRequested;

    public RelayCommand SaveCommand             { get; }
    public RelayCommand CancelCommand           { get; }
    public RelayCommand AddCategoryCommand      { get; }
    public RelayCommand ManageCategoriesCommand { get; }

    public ProductFormViewModel(IProductService productService, ICategoryService categoryService, SessionContext session)
    {
        _productService  = productService;
        _categoryService = categoryService;
        _session         = session;

        SaveCommand             = new RelayCommand(async () => await SaveAsync(), () => CanSave);
        CancelCommand           = new RelayCommand(() => SaveCompleted?.Invoke());
        AddCategoryCommand      = new RelayCommand(() => AddCategoryRequested?.Invoke());
        ManageCategoriesCommand = new RelayCommand(() => ManageCategoriesRequested?.Invoke());
    }

    public async Task RefreshCategoriesAsync(int? selectCategoryId = null)
    {
        var cats = await _categoryService.GetActiveAsync();
        Categories.Clear();
        foreach (var c in cats) Categories.Add(c);

        if (selectCategoryId.HasValue && Categories.Any(c => c.Id == selectCategoryId.Value))
        {
            CategoryId = selectCategoryId.Value;
        }
        else if (CategoryId == 0 && Categories.Count > 0)
        {
            CategoryId = Categories[0].Id;
        }
    }

    public async Task LoadAsync(ProductDto? existing = null)
    {
        _validationCts?.Cancel();
        _isDuplicate = false;

        var cats = await _categoryService.GetActiveAsync();
        Categories.Clear();
        foreach (var c in cats) Categories.Add(c);

        if (existing is not null)
        {
            _editingId         = existing.Id;
            _name              = existing.Name;
            _sku               = existing.SKU;   // SKU is not editable after creation
            Barcode            = existing.Barcode ?? string.Empty;
            Description        = existing.Description ?? string.Empty;
            RetailPriceText    = existing.RetailPrice.ToString("F2");
            WholesalePriceText = existing.WholesalePrice.ToString("F2");
            StockQuantityText  = existing.StockQuantity.ToString();
            CategoryId         = existing.CategoryId;
            ReorderLevel       = existing.ReorderLevel;
            ErrorMessage       = string.Empty;
        }
        else
        {
            _editingId = null;
            _name = string.Empty;
            _sku = string.Empty;
            Barcode = string.Empty;
            Description = string.Empty;
            RetailPriceText = "0.00";
            WholesalePriceText = "0.00";
            StockQuantityText = "0";
            CategoryId = cats.FirstOrDefault()?.Id ?? 0;
            ReorderLevel = 5;
            ErrorMessage = string.Empty;
        }

        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(SKU));
        OnPropertyChanged(nameof(IsEditMode));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(CanSave));
        SaveCommand.RaiseCanExecuteChanged();
    }

    private async Task ValidateUniquenessAsync()
    {
        _validationCts?.Cancel();
        var cts = new CancellationTokenSource();
        _validationCts = cts;

        try
        {
            await Task.Delay(150, cts.Token);

            var skuToCheck = SKU?.Trim();
            var nameToCheck = Name?.Trim();

            if (!string.IsNullOrEmpty(skuToCheck))
            {
                var skuExists = await _productService.ExistsBySkuAsync(skuToCheck, _editingId, cts.Token);
                if (skuExists)
                {
                    _isDuplicate = true;
                    ErrorMessage = $"This SKU '{skuToCheck}' already exists.";
                    OnPropertyChanged(nameof(SkuError));
                    SaveCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(CanSave));
                    return;
                }
            }

            if (!string.IsNullOrEmpty(nameToCheck))
            {
                var nameExists = await _productService.ExistsByNameAsync(nameToCheck, _editingId, cts.Token);
                if (nameExists)
                {
                    _isDuplicate = true;
                    ErrorMessage = "This product already exists.";
                    OnPropertyChanged(nameof(NameError));
                    SaveCommand.RaiseCanExecuteChanged();
                    OnPropertyChanged(nameof(CanSave));
                    return;
                }
            }

            if (_isDuplicate)
            {
                _isDuplicate = false;
                if (ErrorMessage.StartsWith("This SKU") || ErrorMessage == "This product already exists.")
                {
                    ErrorMessage = string.Empty;
                }
                OnPropertyChanged(nameof(NameError));
                OnPropertyChanged(nameof(SkuError));
                SaveCommand.RaiseCanExecuteChanged();
                OnPropertyChanged(nameof(CanSave));
            }
        }
        catch (OperationCanceledException)
        {
            // Newer keystroke arrived
        }
        catch
        {
            // Ignore transient query errors during typing
        }
    }

    private async Task SaveAsync()
    {
        ErrorMessage = string.Empty;
        if (string.IsNullOrWhiteSpace(Name)) { ErrorMessage = "Name is required."; return; }
        if (string.IsNullOrWhiteSpace(_sku)) { ErrorMessage = "SKU is required.";  return; }
        if (!decimal.TryParse(RetailPriceText,    out var retail))    { ErrorMessage = "Invalid retail price.";    return; }
        if (!decimal.TryParse(WholesalePriceText, out var wholesale)) { ErrorMessage = "Invalid wholesale price."; return; }
        if (!TryParseStockQuantity(out var stockQuantity))
        { ErrorMessage = "Stock quantity must be a non-negative whole number."; return; }
        if (CategoryId == 0) { ErrorMessage = "Please select a category."; return; }

        if (await _productService.ExistsBySkuAsync(_sku.Trim(), _editingId))
        {
            _isDuplicate = true;
            ErrorMessage = $"This SKU '{_sku.Trim()}' already exists.";
            SaveCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(CanSave));
            return;
        }

        if (await _productService.ExistsByNameAsync(Name.Trim(), _editingId))
        {
            _isDuplicate = true;
            ErrorMessage = "This product already exists.";
            SaveCommand.RaiseCanExecuteChanged();
            OnPropertyChanged(nameof(CanSave));
            return;
        }

        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                await _productService.UpdateAsync(new UpdateProductRequest(
                    _editingId!.Value, Name.Trim(), _sku.Trim(),
                    string.IsNullOrWhiteSpace(Barcode) ? null : Barcode.Trim(),
                    string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                    retail, wholesale, CategoryId, ReorderLevel, stockQuantity));
            }
            else
            {
                await _productService.CreateAsync(new CreateProductRequest(
                    Name.Trim(), _sku.Trim(),
                    string.IsNullOrWhiteSpace(Barcode) ? null : Barcode.Trim(),
                    string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
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
        OnPropertyChanged(nameof(CanSave));
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
