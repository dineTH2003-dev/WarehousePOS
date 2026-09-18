using System.Windows.Controls;
using WarehousePOS.Application.Products;
using WarehousePOS.Application.Suppliers;
using WarehousePOS.Desktop.ViewModels.Products;
using WarehousePOS.Desktop.ViewModels.Purchasing;
using WarehousePOS.Desktop.ViewModels.Suppliers;
using WarehousePOS.Desktop.Views.Products;
using WarehousePOS.Desktop.Views.Suppliers;

namespace WarehousePOS.Desktop.Views.Purchasing;

public partial class PurchasingView : Page
{
    private readonly PurchasingViewModel _vm;
    private readonly ProductFormViewModel _productFormVm;
    private readonly SupplierFormViewModel _supplierFormVm;
    private readonly ICategoryService _categoryService;
    private readonly ISupplierEntitlementService _entitlementService;

    public PurchasingView(
        PurchasingViewModel vm,
        ProductFormViewModel productFormVm,
        SupplierFormViewModel supplierFormVm,
        ICategoryService categoryService,
        ISupplierEntitlementService entitlementService)
    {
        InitializeComponent();
        _vm = vm;
        _productFormVm = productFormVm;
        _supplierFormVm = supplierFormVm;
        _categoryService = categoryService;
        _entitlementService = entitlementService;
        DataContext = vm;

        vm.CreateNewProductRequested += OnCreateNewProductRequested;
        vm.CreateNewSupplierRequested += OnCreateNewSupplierRequested;
        vm.RecordEntitlementRequested += OnRecordEntitlementRequested;
    }

    public async Task InitAsync()
    {
        await _vm.LoadAsync();
    }

    private async void OnRecordEntitlementRequested(int supplierId, string supplierName, IEnumerable<ProductDto> products)
    {
        var window = System.Windows.Window.GetWindow(this);
        var formVm = new SupplierEntitlementFormViewModel(_entitlementService, supplierId, supplierName, products);
        var dialog = new SupplierEntitlementFormView(formVm) { Owner = window };

        if (dialog.ShowDialog() == true)
        {
            await _vm.LoadEntitlementsForSelectedSupplierAsync();
        }
    }

    private async void OnCreateNewSupplierRequested()
    {
        await _supplierFormVm.LoadAsync(null);
        var window = System.Windows.Window.GetWindow(this);
        var supplierDialog = new SupplierFormView(_supplierFormVm, _productFormVm, _categoryService) { Owner = window };

        if (supplierDialog.ShowDialog() == true)
        {
            await _vm.LoadAsync();
        }
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
                0,
                true,
                false,
                _productFormVm.CategoryId,
                "General");

            _vm.AddNewlyCreatedProduct(newProduct);
        }
    }
}
