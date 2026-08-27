using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Windows;
using WarehousePOS.Application;
using WarehousePOS.Desktop.Services;
using WarehousePOS.Desktop.ViewModels.Auth;
using WarehousePOS.Desktop.Views.Auth;
using WarehousePOS.Infrastructure;
using WarehousePOS.Infrastructure.Persistence;

namespace WarehousePOS.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // ── Data directories ─────────────────────────────────────
        DirectoryManager.EnsureDirectoriesExist();

        string databasePath = DirectoryManager.GetDatabasePath();
        string logsPath     = DirectoryManager.GetLogFilePath();

        // ── Serilog ───────────────────────────────────────────────
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logsPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
#if DEBUG
            .WriteTo.Console()
#endif
            .CreateLogger();

        // ── DI Host ───────────────────────────────────────────────
        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                services.AddApplicationServices();
                services.AddInfrastructureServices(databasePath);

                // Desktop services
                services.AddSingleton<SessionContext>();
                services.AddSingleton<INavigationService, NavigationService>();

                // ViewModels
                services.AddTransient<LoginViewModel>();

                // Windows
                services.AddTransient<LoginWindow>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // ── Database: migrate + seed ───────────────────────────────
        using (var scope = _host.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await DbInitializer.InitializeAsync(db);
        }

        // ── Show Login ────────────────────────────────────────────
        var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
        bool? loggedIn = loginWindow.ShowDialog();

        if (loggedIn != true)
        {
            Shutdown();
            return;
        }

        // ── Show Main Window ──────────────────────────────────────
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
