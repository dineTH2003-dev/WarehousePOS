using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
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
    private readonly IServiceScopeFactory _scopeFactory;

    // Tracks the current DI scope so it can be disposed when navigating away.
    private IServiceScope? _currentPageScope;

    public MainWindow(INavigationService nav, SessionContext session, IServiceScopeFactory scopeFactory)
    {
        InitializeComponent();
        _nav = nav;
        _session = session;
        _scopeFactory = scopeFactory;

        // Wire the navigation service to the Frame inside this window
        if (_nav is Services.NavigationService ns)
            ns.SetFrame(MainFrame);

        // Fix: use Frame.Navigated to call InitAsync AFTER the page is actually
        // loaded into the frame — Frame.Navigate() is asynchronous and the
        // Content property is null immediately after the call returns.
        MainFrame.Navigated += OnFrameNavigated;

        Loaded += (_, _) =>
        {
            if (_session.IsLoggedIn)
                UserLabel.Text = $"{_session.CurrentUser.FullName} ({_session.CurrentUser.Role})";

            // Navigate to POS as the default landing page
            NavigateTo<PosViewModel>();
        };
    }

    // Called by WPF after Frame.Navigate() has fully committed — Content is populated here.
    private async void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        try
        {
            if (e.Content is Views.Sales.PosView posView)
                await posView.InitAsync();
            else if (e.Content is Views.Products.ProductListView productView)
                await productView.InitAsync();
            else if (e.Content is Views.Products.CategoryManagementView catView)
                await catView.InitAsync();
            else if (e.Content is Views.Suppliers.SupplierListView supplierView)
                await supplierView.InitAsync();
            else if (e.Content is Views.Sales.CustomerListView customerView)
                await customerView.InitAsync();
            else if (e.Content is Views.Reports.ReportsView reportsView)
                await reportsView.InitAsync();
            else if (e.Content is Views.Expenses.ExpenseListView expenseView)
                await expenseView.InitAsync();
            else if (e.Content is Views.Settings.StoreSettingsView settingsView)
                await settingsView.InitAsync();
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

    // ── Helper: create a fresh DI scope and navigate ──────────────────────
    private void NavigateTo<TViewModel>() where TViewModel : class
    {
        // Dispose the previous page's scope to free its DbContext
        _currentPageScope?.Dispose();
        _currentPageScope = _scopeFactory.CreateScope();

        // Resolve the view from the new scope so it gets a fresh DbContext
        if (_nav is Services.NavigationService ns)
            ns.NavigateToScoped<TViewModel>(_currentPageScope.ServiceProvider);
        else
            _nav.NavigateTo<TViewModel>();
    }

    // ── Sidebar button handlers ───────────────────────────────────────────

    private void BtnPos_Click(object sender, RoutedEventArgs e)
        => NavigateTo<PosViewModel>();

    private void BtnProducts_Click(object sender, RoutedEventArgs e)
        => NavigateTo<ProductListViewModel>();

    private void BtnSuppliers_Click(object sender, RoutedEventArgs e)
        => NavigateTo<SupplierListViewModel>();

    private void BtnCustomers_Click(object sender, RoutedEventArgs e)
        => NavigateTo<CustomerListViewModel>();

    private void BtnReports_Click(object sender, RoutedEventArgs e)
        => NavigateTo<ReportsViewModel>();

    private void BtnExpenses_Click(object sender, RoutedEventArgs e)
        => NavigateTo<ExpenseListViewModel>();

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
        => NavigateTo<StoreSettingsViewModel>();

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        _currentPageScope?.Dispose();
        _session.Clear();
        var processPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
        if (!string.IsNullOrEmpty(processPath))
        {
            System.Diagnostics.Process.Start(processPath);
        }
        System.Windows.Application.Current.Shutdown();
    }
}
