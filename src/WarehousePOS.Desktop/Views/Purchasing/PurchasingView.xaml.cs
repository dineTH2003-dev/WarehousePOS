using System.Windows.Controls;
using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.ViewModels.Products;
using WarehousePOS.Desktop.ViewModels.Purchasing;
using WarehousePOS.Desktop.Views.Products;

namespace WarehousePOS.Desktop.Views.Purchasing;

public partial class PurchasingView : Page
{
    private readonly PurchasingViewModel _vm;
    private readonly ProductFormViewModel _productFormVm;
    private readonly ICategoryService _categoryService;

    public PurchasingView(
        PurchasingViewModel vm,
        ProductFormViewModel productFormVm,
        ICategoryService categoryService)
    {
        InitializeComponent();
        _vm = vm;
        _productFormVm = productFormVm;
        _categoryService = categoryService;
        DataContext = vm;

        vm.CreateNewProductRequested += OnCreateNewProductRequested;
    }

    public async Task InitAsync()
    {
        await _vm.LoadAsync();
    }

    private async void OnCreateNewProductRequested()
    {
        await _productFormVm.LoadAsync(null);
        var window = System.Windows.Window.GetWindow(this);
        var productDialog = new ProductFormView(_productFormVm, _categoryService) { Owner = window };

        if (productDialog.ShowDialog() == true)
        {
            var newProduct = new ProductDto(
                0,
                _productFormVm.Name,
                _productFormVm.SKU,
                _productFormVm.Barcode,
                _productFormVm.Description,
                0,
                0,
                0,
                _productFormVm.ReorderLevel,
                int.TryParse(_productFormVm.WarrantyYearsText, out var wy) ? wy : 0,
                int.TryParse(_productFormVm.WarrantyMonthsText, out var wm) ? wm : 0,
                int.TryParse(_productFormVm.WarrantyDaysText, out var wd) ? wd : 0,
                true,
                false,
                _productFormVm.CategoryId,
                "General");

            _vm.AddNewlyCreatedProduct(newProduct);
        }
    }
}
