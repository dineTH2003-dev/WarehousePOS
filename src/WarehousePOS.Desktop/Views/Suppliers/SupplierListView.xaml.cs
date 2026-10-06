using System.Windows.Controls;
using WarehousePOS.Application.Products;
using WarehousePOS.Application.Suppliers;
using WarehousePOS.Desktop.ViewModels.Products;
using WarehousePOS.Desktop.ViewModels.Suppliers;

namespace WarehousePOS.Desktop.Views.Suppliers;

public partial class SupplierListView : Page
{
    private readonly SupplierListViewModel _vm;
    private readonly SupplierFormViewModel _formVm;
    private readonly ProductFormViewModel _productFormVm;
    private readonly ICategoryService _categoryService;
    private readonly ISupplierEntitlementService _entitlementService;
    private readonly IProductService _productService;
    private readonly ISupplierImportService _importService;

    public SupplierListView(
        SupplierListViewModel vm,
        SupplierFormViewModel formVm,
        ProductFormViewModel productFormVm,
        ICategoryService categoryService,
        ISupplierEntitlementService entitlementService,
        IProductService productService,
        ISupplierImportService importService)
    {
        InitializeComponent();
        _vm = vm;
        _formVm = formVm;
        _productFormVm = productFormVm;
        _categoryService = categoryService;
        _entitlementService = entitlementService;
        _productService = productService;
        _importService = importService;
        DataContext = vm;
        vm.EditRequested += OnEditRequested;
        vm.RecordEntitlementRequested += OnRecordEntitlementRequested;
        vm.ImportRequested += OnImportRequested;
    }

    public async Task InitAsync() => await _vm.LoadAsync();

    private async void OnImportRequested()
    {
        var window = System.Windows.Window.GetWindow(this);
        var dialog = new ImportSuppliersDialog(_importService)
        {
            Owner = window
        };
        var result = dialog.ShowDialog();
        if (result == true || dialog.HasImportedAny)
        {
            await _vm.LoadAsync();
        }
    }

    private async void OnRecordEntitlementRequested(SupplierDto supplier)
    {
        var products = await _productService.GetAllAsync();
        var activeProducts = products.Where(p => p.IsActive).ToList();
        var window = System.Windows.Window.GetWindow(this);
        var entitlementVm = new SupplierEntitlementFormViewModel(_entitlementService, supplier.Id, supplier.Name, activeProducts);
        var dialog = new SupplierEntitlementFormView(entitlementVm) { Owner = window };
        dialog.ShowDialog();
    }

    private async void OnEditRequested(SupplierDto? dto)
    {
        await _formVm.LoadAsync(dto);
        var dialog = new SupplierFormView(_formVm, _productFormVm, _categoryService) { Owner = System.Windows.Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) await _vm.LoadAsync();
    }
}
