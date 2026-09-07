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

    public SupplierListView(
        SupplierListViewModel vm,
        SupplierFormViewModel formVm,
        ProductFormViewModel productFormVm,
        ICategoryService categoryService)
    {
        InitializeComponent();
        _vm = vm;
        _formVm = formVm;
        _productFormVm = productFormVm;
        _categoryService = categoryService;
        DataContext = vm;
        vm.EditRequested += OnEditRequested;
    }

    public async Task InitAsync() => await _vm.LoadAsync();

    private async void OnEditRequested(SupplierDto? dto)
    {
        await _formVm.LoadAsync(dto);
        var dialog = new SupplierFormView(_formVm, _productFormVm, _categoryService) { Owner = System.Windows.Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) await _vm.LoadAsync();
    }
}
