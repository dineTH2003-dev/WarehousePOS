using System.Windows.Controls;
using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.ViewModels.Products;

namespace WarehousePOS.Desktop.Views.Products;

public partial class ProductListView : Page
{
    private readonly ProductListViewModel _vm;
    private readonly ProductFormViewModel _formVm;
    private readonly ICategoryService     _categoryService;

    public ProductListView(
        ProductListViewModel vm,
        ProductFormViewModel formVm,
        ICategoryService categoryService)
    {
        InitializeComponent();
        _vm = vm;
        _formVm = formVm;
        _categoryService = categoryService;
        DataContext = vm;
        vm.EditRequested += OnEditRequested;
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
}
