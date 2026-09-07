using System.Windows.Controls;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels.Sales;

namespace WarehousePOS.Desktop.Views.Sales;

public partial class CustomerPurchasedItemsView : Page
{
    private readonly CustomerPurchasedItemsViewModel _vm;
    private readonly INavigationService _nav;

    public CustomerPurchasedItemsView(CustomerPurchasedItemsViewModel vm, INavigationService nav)
    {
        InitializeComponent();
        _vm = vm;
        _nav = nav;
        DataContext = vm;
        vm.BackRequested += () => _nav.NavigateTo<CustomerListViewModel>();
    }

    public async Task InitAsync()
    {
        if (CustomerPurchasedItemsViewModel.PendingCustomer is not null)
        {
            var customer = CustomerPurchasedItemsViewModel.PendingCustomer;
            CustomerPurchasedItemsViewModel.PendingCustomer = null;
            await _vm.LoadAsync(customer);
        }
    }
}
