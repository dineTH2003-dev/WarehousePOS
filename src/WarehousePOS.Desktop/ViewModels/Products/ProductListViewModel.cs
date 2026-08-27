using System.Collections.ObjectModel;
using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.ViewModels;

namespace WarehousePOS.Desktop.ViewModels.Products;

public sealed class ProductListViewModel : ViewModelBase
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    private ObservableCollection<ProductDto> _products = [];
    private ObservableCollection<CategoryDto> _categories = [];
    private ProductDto? _selectedProduct;
    private string _searchText = string.Empty;
    private int? _filterCategoryId;
    private bool _showInactive;

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

    // Raised to tell the view to open the form
    public event Action<ProductDto?>? EditRequested;

    public RelayCommand AddCommand     { get; }
    public RelayCommand<ProductDto> EditCommand    { get; }
    public RelayCommand<ProductDto> ToggleActiveCommand { get; }
    public RelayCommand RefreshCommand { get; }

    public ProductListViewModel(IProductService productService, ICategoryService categoryService)
    {
        _productService  = productService;
        _categoryService = categoryService;
        AddCommand       = new RelayCommand(() => EditRequested?.Invoke(null));
        EditCommand      = new RelayCommand<ProductDto>(dto => EditRequested?.Invoke(dto));
        ToggleActiveCommand = new RelayCommand<ProductDto>(async dto => await ToggleActiveAsync(dto));
        RefreshCommand   = new RelayCommand(async () => await LoadAsync());
    }

    public async Task LoadAsync()
    {
        var cats = await _categoryService.GetActiveAsync();
        Categories = new ObservableCollection<CategoryDto>(cats);
        await ApplyFilterAsync();
    }

    private async Task ApplyFilterAsync()
    {
        IReadOnlyList<ProductDto> result;

        if (!string.IsNullOrWhiteSpace(SearchText))
            result = await _productService.SearchAsync(SearchText);
        else if (FilterCategoryId.HasValue)
            result = await _productService.GetByCategoryAsync(FilterCategoryId.Value);
        else
            result = await _productService.GetAllAsync();

        if (!ShowInactive)
            result = result.Where(p => p.IsActive).ToList();

        Products = new ObservableCollection<ProductDto>(result);
    }

    private async Task ToggleActiveAsync(ProductDto? dto)
    {
        if (dto is null) return;
        if (dto.IsActive) await _productService.DeactivateAsync(dto.Id);
        else await _productService.ActivateAsync(dto.Id);
        await ApplyFilterAsync();
    }
}
