using System.Windows;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels.Expenses;
using WarehousePOS.Desktop.ViewModels.Products;
using WarehousePOS.Desktop.ViewModels.Reports;
using WarehousePOS.Desktop.ViewModels.Sales;
using WarehousePOS.Desktop.ViewModels.Settings;
using WarehousePOS.Desktop.ViewModels.Suppliers;

namespace WarehousePOS.Desktop;

/// <summary>
/// MainWindow shell — hosts the sidebar nav and the main content Frame.
/// All content lives in Pages navigated via INavigationService.
/// </summary>
public partial class MainWindow : Window
{
    private readonly INavigationService _nav;
    private readonly SessionContext _session;

    public MainWindow(INavigationService nav, SessionContext session)
    {
        InitializeComponent();
        _nav = nav;
        _session = session;

        // Wire the navigation service to the Frame inside this window
        if (_nav is NavigationService ns)
            ns.SetFrame(MainFrame);

        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        if (_session.IsLoggedIn)
        {
            UserLabel.Text = $"{_session.CurrentUser.FullName} ({_session.CurrentUser.Role})";
        }

        // Navigate to POS (Point of Sale) as the default landing page
        _nav.NavigateTo<PosViewModel>();

        // Initialize the POS page data
        if (MainFrame.Content is Views.Sales.PosView posView)
            await posView.InitAsync();
    }

    private async void BtnPos_Click(object sender, RoutedEventArgs e)
    {
        _nav.NavigateTo<PosViewModel>();
        if (MainFrame.Content is Views.Sales.PosView view)
            await view.InitAsync();
    }

    private async void BtnProducts_Click(object sender, RoutedEventArgs e)
    {
        _nav.NavigateTo<ProductListViewModel>();
        if (MainFrame.Content is Views.Products.ProductListView view)
            await view.InitAsync();
    }

    private async void BtnSuppliers_Click(object sender, RoutedEventArgs e)
    {
        _nav.NavigateTo<SupplierListViewModel>();
        if (MainFrame.Content is Views.Suppliers.SupplierListView view)
            await view.InitAsync();
    }

    private async void BtnCustomers_Click(object sender, RoutedEventArgs e)
    {
        _nav.NavigateTo<CustomerListViewModel>();
        if (MainFrame.Content is Views.Sales.CustomerListView view)
            await view.InitAsync();
    }

    private async void BtnReports_Click(object sender, RoutedEventArgs e)
    {
        _nav.NavigateTo<ReportsViewModel>();
        if (MainFrame.Content is Views.Reports.ReportsView view)
            await view.InitAsync();
    }

    private async void BtnExpenses_Click(object sender, RoutedEventArgs e)
    {
        _nav.NavigateTo<ExpenseListViewModel>();
        if (MainFrame.Content is Views.Expenses.ExpenseListView view)
            await view.InitAsync();
    }

    private async void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        _nav.NavigateTo<StoreSettingsViewModel>();
        if (MainFrame.Content is Views.Settings.StoreSettingsView view)
            await view.InitAsync();
    }

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        _session.Clear();
        var processPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(processPath))
        {
            System.Diagnostics.Process.Start(processPath);
        }
        System.Windows.Application.Current.Shutdown();
    }
}

