using System.Windows.Controls;
using WarehousePOS.Application.Sales;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels.Sales;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class CustomerListView : Page
{
    private readonly CustomerListViewModel _vm;
    private readonly CustomerFormViewModel _formVm;
    private readonly INavigationService _nav;

    public CustomerListView(CustomerListViewModel vm, CustomerFormViewModel formVm, INavigationService nav)
    {
        InitializeComponent();
        _vm = vm;
        _formVm = formVm;
        _nav = nav;
        DataContext = vm;
        vm.EditRequested += OnEditRequested;
        vm.PurchasedItemsRequested += OnPurchasedItemsRequested;
    }

    public async Task InitAsync() => await _vm.LoadAsync();

    private async void OnEditRequested(CustomerDto? dto)
    {
        _formVm.Load(dto);
        var dialog = new CustomerFormView(_formVm) { Owner = System.Windows.Window.GetWindow(this) };
        if (dialog.ShowDialog() == true) await _vm.LoadAsync();
    }

    private void OnPurchasedItemsRequested(CustomerDto dto)
    {
        CustomerPurchasedItemsViewModel.PendingCustomer = dto;
        _nav.NavigateTo<CustomerPurchasedItemsViewModel>();
    }
}
