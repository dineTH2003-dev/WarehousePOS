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
    private readonly WarehousePOS.Application.Common.IBackupService _backupService;
    private readonly WarehousePOS.Application.Common.ICloudBackupService _cloudService;
    private readonly HashSet<object> _initializedPages = [];
    private System.Windows.Threading.DispatcherTimer? _autoBackupTimer;

    public MainWindow(
        INavigationService nav,
        SessionContext session,
        WarehousePOS.Application.Common.IBackupService backupService,
        WarehousePOS.Application.Common.ICloudBackupService cloudService)
    {
        InitializeComponent();
        _nav = nav;
        _session = session;
        _backupService = backupService;
        _cloudService = cloudService;

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
            {
                UserLabel.Text = $"{_session.CurrentUser.FullName} ({_session.CurrentUser.Role})";
                BtnReports.Visibility = _session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
                BtnExpenses.Visibility = _session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
                BtnUserManagement.Visibility = _session.IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            }

            // Navigate to POS as the default landing page
            NavigateTo<PosViewModel>();

            // Start automated daily backup schedule
            StartAutoBackupTimer();
        };
    }

    private void StartAutoBackupTimer()
    {
        // Subscribe to cloud sync status changes to update the notification banner
        _cloudService.SyncStatusChanged += OnSyncStatusChanged;

        // Auto-reconnect listener: automatically sync when internet/network becomes available
        System.Net.NetworkInformation.NetworkChange.NetworkAddressChanged += (_, _) =>
        {
            Dispatcher.Invoke(async () => await CheckAndRunAutoBackupAsync());
        };

        // Check 15 seconds after app startup without blocking UI
        Task.Delay(15000).ContinueWith(_ => _ = CheckAndRunAutoBackupAsync());

        // Check periodically every hour while the app remains open
        _autoBackupTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromHours(1)
        };
        _autoBackupTimer.Tick += async (_, _) => await CheckAndRunAutoBackupAsync();
        _autoBackupTimer.Start();
    }

    private void OnSyncStatusChanged()
    {
        Dispatcher.Invoke(() =>
        {
            if (_cloudService.IsSyncPending)
            {
                CloudSyncNoticeBanner.Visibility = Visibility.Visible;
                CloudSyncNoticeText.Text = string.IsNullOrEmpty(_cloudService.PendingSyncReason)
                    ? "Internet offline: Local backup is saved safely on this PC. Cloud backup will automatically upload when internet reconnects."
                    : _cloudService.PendingSyncReason;
            }
            else
            {
                CloudSyncNoticeBanner.Visibility = Visibility.Collapsed;
            }
        });
    }

    private async Task CheckAndRunAutoBackupAsync()
    {
        try
        {
            var backups = _backupService.GetLocalBackups();
            bool needsBackup = backups.Count == 0 || (DateTime.UtcNow - backups[0].CreatedTimeUtc).TotalHours >= 24;

            string? zipPath = null;
            if (needsBackup)
            {
                zipPath = await _backupService.CreateBackupAsync();
            }
            else if (_cloudService.IsSyncPending && backups.Count > 0)
            {
                zipPath = backups[0].FilePath;
            }

            if (zipPath is not null && await _cloudService.IsConnectedAsync())
            {
                await _cloudService.UploadBackupAsync(zipPath);
            }
        }
        catch
        {
            // Silent catch on background thread — never disrupt the cashier or UI
        }
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
                if (isFirstLoad)
                    await productView.InitAsync();
                else if (ProductListViewModel.PendingOpenAddProduct)
                    productView.HandlePendingOpenAddProduct();
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
            else if (page is Views.Users.UserManagementView userView)
            {
                if (isFirstLoad)
                    await userView.InitAsync();
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
