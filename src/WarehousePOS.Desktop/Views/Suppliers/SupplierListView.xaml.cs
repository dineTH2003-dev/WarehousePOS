using System.Windows.Controls;
using WarehousePOS.Application.Suppliers;
using WarehousePOS.Desktop.ViewModels.Suppliers;

namespace WarehousePOS.Desktop.Views.Suppliers;

public partial class SupplierListView : Page
{
    private readonly SupplierListViewModel _vm;
    private readonly SupplierFormViewModel _formVm;

    public SupplierListView(SupplierListViewModel vm, SupplierFormViewModel formVm)
    {
        InitializeComponent();
        _vm = vm;
        _formVm = formVm;
        DataContext = vm;
        vm.EditRequested += OnEditRequested;
    }

    public async Task InitAsync() => await _vm.LoadAsync();

    private async void OnEditRequested(SupplierDto? dto)
    {
        _formVm.Load(dto);
        var dialog = new SupplierFormView(_formVm) { Owner = System.Windows.Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) await _vm.LoadAsync();
    }
}
