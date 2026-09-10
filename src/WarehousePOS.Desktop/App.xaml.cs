using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using WarehousePOS.Application;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels.Auth;
using WarehousePOS.Desktop.ViewModels.Expenses;
using WarehousePOS.Desktop.ViewModels.Products;
using WarehousePOS.Desktop.ViewModels.Purchasing;
using WarehousePOS.Desktop.ViewModels.Reports;
using WarehousePOS.Desktop.ViewModels.Sales;
using WarehousePOS.Desktop.ViewModels.Settings;
using WarehousePOS.Desktop.ViewModels.Suppliers;
using WarehousePOS.Desktop.ViewModels.Users;
using WarehousePOS.Desktop.Views.Auth;
using WarehousePOS.Desktop.Views.Expenses;
using WarehousePOS.Desktop.Views.Products;
using WarehousePOS.Desktop.Views.Purchasing;
using WarehousePOS.Desktop.Views.Reports;
using WarehousePOS.Desktop.Views.Sales;
using WarehousePOS.Desktop.Views.Settings;
using WarehousePOS.Desktop.Views.Suppliers;
using WarehousePOS.Desktop.Views.Users;
using WarehousePOS.Infrastructure;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private IServiceScope? _loginScope;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // ── Global Exception Handlers ─────────────────────────────
        // Catch ANY unhandled exception on the UI thread
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        // Catch unhandled exceptions on background threads
        AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

        try
        {
            // ── Data directories ─────────────────────────────────
            DirectoryManager.EnsureDirectoriesExist();

            string databasePath = DirectoryManager.GetDatabasePath();
            string logsPath     = DirectoryManager.GetLogFilePath();

            // ── Serilog ───────────────────────────────────────────
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(logsPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
#if DEBUG
                .WriteTo.Console()
#endif
                .CreateLogger();

            Log.Information("WarehousePOS starting up...");

            // ── DI Host ───────────────────────────────────────────
            _host = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices(services =>
                {
                    services.AddApplicationServices();
                    services.AddInfrastructureServices(databasePath);

                    // Desktop services
                    services.AddSingleton<SessionContext>();
                    services.AddSingleton<INavigationService, NavigationService>();

                    // ── ViewModels ────────────────────────────────
                    // Use AddScoped so that each resolved scope gets its own ViewModel
                    // instance with a properly scoped DbContext — prevents EF Core
                    // "cannot resolve Scoped service from root provider" errors.
                    services.AddScoped<LoginViewModel>();
                    services.AddScoped<PosViewModel>();
                    services.AddScoped<ProductListViewModel>();
                    services.AddScoped<ProductFormViewModel>();
                    services.AddScoped<PurchasingViewModel>();
                    services.AddScoped<CategoryManagementViewModel>();
                    services.AddScoped<SupplierListViewModel>();
                    services.AddScoped<SupplierFormViewModel>();
                    services.AddScoped<CustomerListViewModel>();
                    services.AddScoped<CustomerFormViewModel>();
                    services.AddScoped<CustomerPurchasedItemsViewModel>();
                    services.AddScoped<ReportsViewModel>();
                    services.AddScoped<StoreSettingsViewModel>();
                    services.AddScoped<ExpenseListViewModel>();
                    services.AddScoped<UserManagementViewModel>();

                    // ── Views (Pages) ─────────────────────────────
                    services.AddScoped<PosView>();
                    services.AddScoped<ProductListView>();
                    services.AddScoped<PurchasingView>();
                    services.AddScoped<CategoryManagementView>();
                    services.AddScoped<SupplierListView>();
                    services.AddScoped<CustomerListView>();
                    services.AddScoped<CustomerPurchasedItemsView>();
                    services.AddScoped<ReportsView>();
                    services.AddScoped<StoreSettingsView>();
                    services.AddScoped<ExpenseListView>();
                    services.AddScoped<UserManagementView>();

                    // ── Windows ───────────────────────────────────
                    // LoginWindow uses a dedicated scope (one-shot, disposed after login).
                    // MainWindow is Singleton — it lives for the whole session.
                    services.AddTransient<LoginWindow>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            // ── Register navigation routes ────────────────────────
            NavigationService.Register<PosViewModel,                        Views.Sales.PosView>();
            NavigationService.Register<ProductListViewModel,                Views.Products.ProductListView>();
            NavigationService.Register<PurchasingViewModel,                 Views.Purchasing.PurchasingView>();
            NavigationService.Register<CategoryManagementViewModel,         Views.Products.CategoryManagementView>();
            NavigationService.Register<SupplierListViewModel,               Views.Suppliers.SupplierListView>();
            NavigationService.Register<CustomerListViewModel,               Views.Sales.CustomerListView>();
            NavigationService.Register<CustomerPurchasedItemsViewModel,     Views.Sales.CustomerPurchasedItemsView>();
            NavigationService.Register<ReportsViewModel,                    Views.Reports.ReportsView>();
            NavigationService.Register<StoreSettingsViewModel,              Views.Settings.StoreSettingsView>();
            NavigationService.Register<ExpenseListViewModel,                Views.Expenses.ExpenseListView>();
            NavigationService.Register<UserManagementViewModel,              Views.Users.UserManagementView>();
            NavigationService.RegisterAdminOnly<ReportsViewModel>();
            NavigationService.RegisterAdminOnly<ExpenseListViewModel>();
            NavigationService.RegisterAdminOnly<UserManagementViewModel>();

            await _host.StartAsync();

            // ── Database: create schema + seed ───────────────────
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Log.Information("Initialising database at {Path}", databasePath);
                await DbInitializer.InitializeAsync(db);
                Log.Information("Database initialised successfully.");
            }

            // Keep the application alive while the same top-level window transitions
            // from login content to the main shell.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // ── Show Login and transition in-place ───────────────

            bool loggedIn;
            _loginScope = _host.Services.CreateScope();
            {
                var loginWindow = _loginScope.ServiceProvider.GetRequiredService<LoginWindow>();
                loginWindow.Show();
                loggedIn = await loginWindow.WaitForLoginAsync();

                if (loggedIn)
                {
                    Log.Information("Login successful. Transitioning to MainWindow shell...");
                    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
                    mainWindow.InitializeShell();
                    loginWindow.Content = mainWindow.TakeShellContent();
                    loginWindow.Title = "Warehouse POS";
                    MainWindow = loginWindow;
                }
            }

            if (!loggedIn)
            {
                Log.Information("Login cancelled or failed. Shutting down.");
                Shutdown();
                return;
            }

            // The login window is now the main shell window, so closing it terminates the app.
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            // Write crash to a fallback file (Serilog may not have been initialized yet)
            var crashLogPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "WarehousePOS", "Logs", "startup-crash.log");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(crashLogPath)!);
                File.AppendAllText(crashLogPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] STARTUP CRASH:{Environment.NewLine}" +
                    $"{ex}{Environment.NewLine}{Environment.NewLine}");
            }
            catch { /* best-effort */ }

            // Also log to Serilog if it initialised before the crash
            try { Log.Fatal(ex, "Fatal startup error"); Log.CloseAndFlush(); } catch { }

            // Show a visible error dialog — crash is never silent
            MessageBox.Show(
                $"WarehousePOS failed to start.{Environment.NewLine}{Environment.NewLine}" +
                $"Error: {ex.Message}{Environment.NewLine}{Environment.NewLine}" +
                $"A detailed crash report has been saved to:{Environment.NewLine}{crashLogPath}",
                "WarehousePOS — Startup Failed",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(1);
        }
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_host is not null)
            {
                var backupService = _host.Services.GetService<WarehousePOS.Application.Common.IBackupService>();
                var cloudService = _host.Services.GetService<WarehousePOS.Application.Common.ICloudBackupService>();
                if (backupService is not null)
                {
                    Log.Information("Creating automatic daily shutdown backup...");
                    var localZip = await backupService.CreateBackupAsync();
                    if (cloudService is not null && await cloudService.IsConnectedAsync())
                    {
                        Log.Information("Uploading automatic shutdown backup to Google Drive...");
                        await cloudService.UploadBackupAsync(localZip);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to complete automatic shutdown backup");
        }

        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        _loginScope?.Dispose();
        _loginScope = null;
        Log.Information("WarehousePOS shut down.");
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    // ── Global fallback handlers ──────────────────────────────────────

    private static DateTime _lastErrorDialogTime = DateTime.MinValue;
    private static string? _lastErrorMessage;
    private static bool _isShowingErrorDialog;

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs ex)
    {
        try { Log.Fatal(ex.Exception, "Unhandled UI thread exception"); Log.CloseAndFlush(); } catch { }

        // Mark this event handled before opening a modal dialog. MessageBox.Show
        // runs a nested dispatcher loop, so delaying this until after it closes
        // can allow the same exception path to raise additional dialogs.
        ex.Handled = true;

        // MessageBox.Show starts a nested dispatcher loop. Further failures from
        // the same failed view must not create another modal error dialog.
        if (_isShowingErrorDialog)
        {
            ex.Handled = true;
            return;
        }

        var baseException = ex.Exception.GetBaseException();
        string displayMessage = baseException != null && baseException != ex.Exception
            ? $"{ex.Exception.Message}{Environment.NewLine}Details: {baseException.Message}"
            : ex.Exception.Message;

        var now = DateTime.UtcNow;
        if (_lastErrorMessage == displayMessage && (now - _lastErrorDialogTime).TotalSeconds < 2)
        {
            ex.Handled = true;
            return;
        }

        _lastErrorDialogTime = now;
        _lastErrorMessage = displayMessage;

        try
        {
            _isShowingErrorDialog = true;
            MessageBox.Show(
                $"An unexpected error occurred:{Environment.NewLine}{Environment.NewLine}{displayMessage}",
                "WarehousePOS — Unexpected Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            _isShowingErrorDialog = false;
            ex.Handled = true;
        }
    }

    private static void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs ex)
    {
        try
        {
            Log.Fatal(ex.ExceptionObject as Exception, "Unhandled background thread exception");
            Log.CloseAndFlush();
        }
        catch { }
    }
}
