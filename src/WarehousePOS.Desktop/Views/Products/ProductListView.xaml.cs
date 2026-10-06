using System.Windows.Controls;
using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels.Products;

namespace WarehousePOS.Desktop.Views.Products;

public partial class ProductListView : Page
{
    private readonly ProductListViewModel _vm;
    private readonly ProductFormViewModel _formVm;
    private readonly ICategoryService     _categoryService;
    private readonly IProductImportService _importService;
    private readonly SessionContext       _session;

    public ProductListView(
        ProductListViewModel vm,
        ProductFormViewModel formVm,
        ICategoryService categoryService,
        IProductImportService importService,
        SessionContext session)
    {
        InitializeComponent();
        _vm = vm;
        _formVm = formVm;
        _categoryService = categoryService;
        _importService = importService;
        _session = session;
        DataContext = vm;
        vm.EditRequested += OnEditRequested;
        vm.ImportRequested += OnImportRequested;
    }

    public async Task InitAsync()
    {
        await _vm.LoadAsync();
        HandlePendingOpenAddProduct();
    }

    public void HandlePendingOpenAddProduct()
    {
        if (ProductListViewModel.PendingOpenAddProduct)
        {
            ProductListViewModel.PendingOpenAddProduct = false;
            OnEditRequested(null);
        }
    }

    private async void OnEditRequested(ProductDto? dto)
    {
        await _formVm.LoadAsync(dto);
        var dialog = new ProductFormView(_formVm, _categoryService) { Owner = System.Windows.Window.GetWindow(this) };
        var result = dialog.ShowDialog();
        if (result == true)
        {
            await _vm.LoadAsync();
        }
        else if (dialog.NavigateToCategoriesRequested)
        {
            _vm.ManageCategoriesCommand.Execute(null);
        }
    }

    private async void OnImportRequested()
    {
        var window = System.Windows.Window.GetWindow(this);
        var dialog = new ImportProductsDialog(_importService, _session.CurrentUser?.UserId ?? 1)
        {
            Owner = window
        };
        var result = dialog.ShowDialog();
        if (result == true || dialog.HasImportedAny)
        {
            await _vm.LoadAsync();
        }
    }
}
