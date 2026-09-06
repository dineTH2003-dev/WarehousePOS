using System.Collections.ObjectModel;
using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Products;

public sealed class ProductListViewModel : ViewModelBase
{
    private readonly IProductService   _productService;
    private readonly ICategoryService  _categoryService;
    private readonly INavigationService _nav;
    private readonly SessionContext    _session;

    public static bool PendingOpenAddProduct { get; set; }

    private ObservableCollection<ProductDto> _products = [];
    private ObservableCollection<CategoryDto> _categories = [];
    private ProductDto? _selectedProduct;
    private string _searchText = string.Empty;
    private int? _filterCategoryId;
    private bool _showInactive;
    private bool _showLowStockOnly;
    private int _totalProductsCount;
    private int _activeCategoriesCount;
    private int _lowStockCount;

    public ObservableCollection<ProductDto> Products   { get => _products;   private set => SetField(ref _products, value); }
    public ObservableCollection<CategoryDto> Categories { get => _categories; private set => SetField(ref _categories, value); }

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set => SetField(ref _selectedProduct, value);
    }

    public string SearchText
    {
        get => _searchText;
        set { SetField(ref _searchText, value); _ = ApplyFilterAsync(); }
    }

    public int? FilterCategoryId
    {
        get => _filterCategoryId;
        set { SetField(ref _filterCategoryId, value); _ = ApplyFilterAsync(); }
    }

    public bool ShowInactive
    {
        get => _showInactive;
        set { SetField(ref _showInactive, value); _ = ApplyFilterAsync(); }
    }

    public bool ShowLowStockOnly
    {
        get => _showLowStockOnly;
        set { SetField(ref _showLowStockOnly, value); _ = ApplyFilterAsync(); }
    }

    public int TotalProductsCount    { get => _totalProductsCount;    private set => SetField(ref _totalProductsCount, value); }
    public int ActiveCategoriesCount { get => _activeCategoriesCount; private set => SetField(ref _activeCategoriesCount, value); }
    public int LowStockCount         { get => _lowStockCount;         private set => SetField(ref _lowStockCount, value); }

    public bool IsAdmin => _session.IsAdmin;

    // Raised to tell the view to open the form
    public event Action<ProductDto?>? EditRequested;

    public RelayCommand AddCommand              { get; }
    public RelayCommand<ProductDto> EditCommand { get; }
    public RelayCommand<ProductDto> ToggleActiveCommand { get; }
    public RelayCommand RefreshCommand          { get; }
    public RelayCommand ManageCategoriesCommand { get; }
    public RelayCommand ClearFiltersCommand     { get; }

    public ProductListViewModel(
        IProductService productService,
        ICategoryService categoryService,
        INavigationService nav,
        SessionContext session)
    {
        _productService  = productService;
        _categoryService = categoryService;
        _nav             = nav;
        _session         = session;

        AddCommand              = new RelayCommand(() => EditRequested?.Invoke(null));
        EditCommand             = new RelayCommand<ProductDto>(dto => EditRequested?.Invoke(dto));
        ToggleActiveCommand     = new RelayCommand<ProductDto>(async dto => await ToggleActiveAsync(dto));
        RefreshCommand          = new RelayCommand(async () => await LoadAsync());
        ManageCategoriesCommand = new RelayCommand(() => _nav.NavigateTo<CategoryManagementViewModel>());
        ClearFiltersCommand     = new RelayCommand(ClearFilters);
    }

    public async Task LoadAsync()
    {
        var cats = await _categoryService.GetActiveAsync();
        var catList = new List<CategoryDto> { new CategoryDto(0, "All Categories", null, true, 0) };
        catList.AddRange(cats);
        Categories = new ObservableCollection<CategoryDto>(catList);
        ActiveCategoriesCount = cats.Count;

        await ApplyFilterAsync();
    }

    private async Task ApplyFilterAsync()
    {
        IReadOnlyList<ProductDto> result;

        if (!string.IsNullOrWhiteSpace(SearchText))
            result = await _productService.SearchAsync(SearchText);
        else if (FilterCategoryId.HasValue && FilterCategoryId.Value > 0)
            result = await _productService.GetByCategoryAsync(FilterCategoryId.Value);
        else
            result = await _productService.GetAllAsync();

        var allProds = await _productService.GetAllAsync();
        TotalProductsCount = allProds.Count(p => p.IsActive);
        LowStockCount = allProds.Count(p => p.IsActive && p.IsLowStock);

        if (!ShowInactive)
            result = result.Where(p => p.IsActive).ToList();

        if (ShowLowStockOnly)
            result = result.Where(p => p.IsLowStock).ToList();

        Products = new ObservableCollection<ProductDto>(result);
    }

    private void ClearFilters()
    {
        _searchText = string.Empty;
        _filterCategoryId = 0;
        _showInactive = false;
        _showLowStockOnly = false;
        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(FilterCategoryId));
        OnPropertyChanged(nameof(ShowInactive));
        OnPropertyChanged(nameof(ShowLowStockOnly));
        _ = ApplyFilterAsync();
    }

    private async Task ToggleActiveAsync(ProductDto? dto)
    {
        if (dto is null) return;
        if (dto.IsActive) await _productService.DeactivateAsync(dto.Id);
        else await _productService.ActivateAsync(dto.Id);
        await ApplyFilterAsync();
    }
}
