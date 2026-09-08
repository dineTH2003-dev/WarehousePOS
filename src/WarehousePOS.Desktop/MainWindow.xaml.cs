using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels.Expenses;
using WarehousePOS.Desktop.ViewModels.Products;
using WarehousePOS.Desktop.ViewModels.Purchasing;
using WarehousePOS.Desktop.ViewModels.Reports;
using WarehousePOS.Desktop.ViewModels.Sales;
using WarehousePOS.Desktop.ViewModels.Settings;
using WarehousePOS.Desktop.ViewModels.Suppliers;
using WarehousePOS.Desktop.ViewModels.Users;

namespace WarehousePOS.Desktop;

/// <summary>
/// MainWindow shell — hosts the sidebar nav and the main content Frame.
/// All content lives in Pages navigated via INavigationService.
/// </summary>
public partial class MainWindow : Window
{
    private readonly INavigationService _nav;
    private readonly SessionContext _session;
    private readonly HashSet<object> _initializedPages = [];
    private bool _shellInitialized;

    public MainWindow(INavigationService nav, SessionContext session)
    {
        InitializeComponent();
        _nav = nav;
        _session = session;

        // Wire the navigation service to the Frame inside this window
        if (_nav is Services.NavigationService ns)
            ns.SetFrame(MainFrame);

        // Fix: use Frame.Navigated to call InitAsync AFTER the page is actually
        // loaded into the frame — Frame.Navigate() is asynchronous and the
        // Content property is null immediately after the call returns.
        MainFrame.Navigated += OnFrameNavigated;

        Loaded += (_, _) => InitializeShell();
    }

    public object? TakeShellContent()
    {
        var content = Content;
        Content = null;
        return content;
    }

    public void InitializeShell()
    {
        if (_shellInitialized)
            return;

        _shellInitialized = true;

        if (_session.IsLoggedIn)
        {
            UserLabel.Text = $"{_session.CurrentUser.FullName} ({_session.CurrentUser.Role})";
            BtnReports.Visibility = _session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnExpenses.Visibility = _session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            BtnUserManagement.Visibility = _session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
        }

        // Navigate to POS as the default landing page
        NavigateTo<PosViewModel>();
    }

    // Called by WPF after Frame.Navigate() has fully committed — Content is populated here.
    private async void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        try
        {
            var page = e.Content;
            if (page is null) return;

            bool isFirstLoad = _initializedPages.Add(page);

            if (page is Views.Sales.PosView posView)
            {
                if (isFirstLoad)
                    await posView.InitAsync();
            }
            else if (page is Views.Products.ProductListView productView)
            {
                if (isFirstLoad || ProductListViewModel.PendingOpenAddProduct)
                    await productView.InitAsync();
            }
            else if (page is Views.Purchasing.PurchasingView purchasingView)
            {
                if (isFirstLoad)
                    await purchasingView.InitAsync();
            }
            else if (page is Views.Products.CategoryManagementView catView)
            {
                if (isFirstLoad)
                    await catView.InitAsync();
            }
            else if (page is Views.Suppliers.SupplierListView supplierView)
            {
                if (isFirstLoad)
                    await supplierView.InitAsync();
            }
            else if (page is Views.Sales.CustomerListView customerView)
            {
                if (isFirstLoad)
                    await customerView.InitAsync();
            }
            else if (page is Views.Sales.CustomerPurchasedItemsView purchasedView)
            {
                if (isFirstLoad || CustomerPurchasedItemsViewModel.PendingCustomer is not null)
                    await purchasedView.InitAsync();
            }
            else if (page is Views.Reports.ReportsView reportsView)
            {
                if (isFirstLoad)
                    await reportsView.InitAsync();
            }
            else if (page is Views.Expenses.ExpenseListView expenseView)
            {
                if (isFirstLoad)
                    await expenseView.InitAsync();
            }
            else if (page is Views.Settings.StoreSettingsView settingsView)
            {
                if (isFirstLoad)
                    await settingsView.InitAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to load page data:{Environment.NewLine}{ex.Message}",
                "WarehousePOS — Navigation Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    // ── Helper: navigate using the cached navigation service ─────────────
    private void NavigateTo<TViewModel>() where TViewModel : class
    {
        _nav.NavigateTo<TViewModel>();
    }

    // ── Sidebar button handlers ───────────────────────────────────────────

    private void BtnPos_Click(object sender, RoutedEventArgs e)
        => NavigateTo<PosViewModel>();

    private void BtnProducts_Click(object sender, RoutedEventArgs e)
        => NavigateTo<ProductListViewModel>();

    private void BtnPurchasing_Click(object sender, RoutedEventArgs e)
        => NavigateTo<PurchasingViewModel>();

    private void BtnCategories_Click(object sender, RoutedEventArgs e)
        => NavigateTo<CategoryManagementViewModel>();

    private void BtnSuppliers_Click(object sender, RoutedEventArgs e)
        => NavigateTo<SupplierListViewModel>();

    private void BtnCustomers_Click(object sender, RoutedEventArgs e)
        => NavigateTo<CustomerListViewModel>();

    private void BtnReports_Click(object sender, RoutedEventArgs e)
        => NavigateToAuthorized<ReportsViewModel>();

    private void BtnExpenses_Click(object sender, RoutedEventArgs e)
        => NavigateToAuthorized<ExpenseListViewModel>();

    private void BtnUserManagement_Click(object sender, RoutedEventArgs e)
        => NavigateToAuthorized<UserManagementViewModel>();

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
        => NavigateTo<StoreSettingsViewModel>();

    private void NavigateToAuthorized<TViewModel>() where TViewModel : class
    {
        if (!_session.IsAdmin)
        {
            MessageBox.Show("Only an Admin can access this feature.", "Access denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        NavigateTo<TViewModel>();
    }

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        _nav.ClearCache();
        _initializedPages.Clear();
        _session.Clear();
        var processPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(processPath))
        {
            System.Diagnostics.Process.Start(processPath);
        }
        System.Windows.Application.Current.Shutdown();
    }
}
