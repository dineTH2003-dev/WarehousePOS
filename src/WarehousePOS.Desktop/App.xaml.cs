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
using WarehousePOS.Desktop.ViewModels.Reports;
using WarehousePOS.Desktop.ViewModels.Sales;
using WarehousePOS.Desktop.ViewModels.Settings;
using WarehousePOS.Desktop.ViewModels.Suppliers;
using WarehousePOS.Desktop.Views.Auth;
using WarehousePOS.Desktop.Views.Expenses;
using WarehousePOS.Desktop.Views.Products;
using WarehousePOS.Desktop.Views.Reports;
using WarehousePOS.Desktop.Views.Sales;
using WarehousePOS.Desktop.Views.Settings;
using WarehousePOS.Desktop.Views.Suppliers;
using WarehousePOS.Infrastructure;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _host;

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
                    services.AddTransient<LoginViewModel>();
                    services.AddTransient<PosViewModel>();
                    services.AddTransient<ProductListViewModel>();
                    services.AddTransient<ProductFormViewModel>();
                    services.AddTransient<CategoryManagementViewModel>();
                    services.AddTransient<SupplierListViewModel>();
                    services.AddTransient<SupplierFormViewModel>();
                    services.AddTransient<CustomerListViewModel>();
                    services.AddTransient<CustomerFormViewModel>();
                    services.AddTransient<ReportsViewModel>();
                    services.AddTransient<StoreSettingsViewModel>();
                    services.AddTransient<ExpenseListViewModel>();

                    // ── Views (Pages) ─────────────────────────────
                    services.AddTransient<PosView>();
                    services.AddTransient<ProductListView>();
                    services.AddTransient<CategoryManagementView>();
                    services.AddTransient<SupplierListView>();
                    services.AddTransient<CustomerListView>();
                    services.AddTransient<ReportsView>();
                    services.AddTransient<StoreSettingsView>();
                    services.AddTransient<ExpenseListView>();

                    // ── Windows ───────────────────────────────────
                    services.AddTransient<LoginWindow>();
                    services.AddSingleton<MainWindow>();
                })
                .Build();

            // ── Register navigation routes ────────────────────────
            NavigationService.Register<PosViewModel,                Views.Sales.PosView>();
            NavigationService.Register<ProductListViewModel,        Views.Products.ProductListView>();
            NavigationService.Register<CategoryManagementViewModel, Views.Products.CategoryManagementView>();
            NavigationService.Register<SupplierListViewModel,       Views.Suppliers.SupplierListView>();
            NavigationService.Register<CustomerListViewModel,       Views.Sales.CustomerListView>();
            NavigationService.Register<ReportsViewModel,            Views.Reports.ReportsView>();
            NavigationService.Register<StoreSettingsViewModel,      Views.Settings.StoreSettingsView>();
            NavigationService.Register<ExpenseListViewModel,        Views.Expenses.ExpenseListView>();


            await _host.StartAsync();

            // ── Database: create schema + seed ───────────────────
            using (var scope = _host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Log.Information("Initialising database at {Path}", databasePath);
                await DbInitializer.InitializeAsync(db);
                Log.Information("Database initialised successfully.");
            }

            // ── Show Login ────────────────────────────────────────
            var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
            bool? loggedIn = loginWindow.ShowDialog();

            if (loggedIn != true)
            {
                Shutdown();
                return;
            }

            // ── Show Main Window ──────────────────────────────────
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();

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
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        Log.Information("WarehousePOS shut down.");
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    // ── Global fallback handlers ──────────────────────────────────────

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs ex)
    {
        try { Log.Fatal(ex.Exception, "Unhandled UI thread exception"); Log.CloseAndFlush(); } catch { }
        MessageBox.Show(
            $"An unexpected error occurred:{Environment.NewLine}{Environment.NewLine}{ex.Exception.Message}",
            "WarehousePOS — Unexpected Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        ex.Handled = true;
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
