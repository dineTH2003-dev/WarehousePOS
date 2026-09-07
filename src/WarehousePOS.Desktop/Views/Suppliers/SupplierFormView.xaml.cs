using System.Windows;
using WarehousePOS.Application.Products;
using WarehousePOS.Desktop.ViewModels.Products;
using WarehousePOS.Desktop.ViewModels.Suppliers;
using WarehousePOS.Desktop.Views.Products;

namespace WarehousePOS.Desktop.Views.Suppliers;

public partial class SupplierFormView : Window
{
    private readonly SupplierFormViewModel _vm;
    private readonly ProductFormViewModel _productFormVm;
    private readonly ICategoryService _categoryService;

    public SupplierFormView(
        SupplierFormViewModel vm,
        ProductFormViewModel productFormVm,
        ICategoryService categoryService)
    {
        InitializeComponent();
        _vm = vm;
        _productFormVm = productFormVm;
        _categoryService = categoryService;
        DataContext = vm;

        vm.SaveCompleted += OnSaveCompleted;
        vm.CreateNewProductRequested += OnCreateNewProductRequested;

        Closed += (_, _) =>
        {
            vm.SaveCompleted -= OnSaveCompleted;
            vm.CreateNewProductRequested -= OnCreateNewProductRequested;
        };
    }

    private void OnSaveCompleted()
    {
        DialogResult = true;
    }

    private async void OnCreateNewProductRequested()
    {
        await _productFormVm.LoadAsync(null);
        var productDialog = new ProductFormView(_productFormVm, _categoryService) { Owner = this };
        if (productDialog.ShowDialog() == true)
        {
            // Create a ProductDto from saved details
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
