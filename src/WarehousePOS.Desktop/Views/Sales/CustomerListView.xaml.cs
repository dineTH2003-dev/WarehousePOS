using System.Windows;
using System.Windows.Controls;
using WarehousePOS.Application.Sales;
using WarehousePOS.Desktop.ViewModels.Sales;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class CustomerListView : Page
{
    private readonly CustomerListViewModel _vm;
    private readonly CustomerFormViewModel _formVm;

    public CustomerListView(CustomerListViewModel vm, CustomerFormViewModel formVm)
    {
        InitializeComponent();
        _vm = vm;
        _formVm = formVm;
        DataContext = vm;
        vm.EditRequested += OnEditRequested;
    }

    public async Task InitAsync() => await _vm.LoadAsync();

    private async void OnEditRequested(CustomerDto? dto)
    {
        _formVm.Load(dto);
        var dialog = new CustomerFormView(_formVm) { Owner = System.Windows.Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) await _vm.LoadAsync();
    }
}
