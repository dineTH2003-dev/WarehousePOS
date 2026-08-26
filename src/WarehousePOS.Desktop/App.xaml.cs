using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Windows;
using WarehousePOS.Application;
using WarehousePOS.Infrastructure;

namespace WarehousePOS.Desktop;

/// <summary>
/// Application entry point and DI host configuration.
/// This is the composition root — the only place where Infrastructure is wired up.
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        // Resolve data directory (stored outside Program Files)
        string appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "WarehousePOS");

        string databasePath = Path.Combine(appDataPath, "Data", "WarehousePOS.db");
        string logsPath     = Path.Combine(appDataPath, "Logs", "application.log");
        string backupsPath  = Path.Combine(appDataPath, "Backups");

        // Ensure directories exist
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(logsPath)!);
        Directory.CreateDirectory(backupsPath);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logsPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
#if DEBUG
            .WriteTo.Console()
#endif
            .CreateLogger();

        _host = Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureServices(services =>
            {
                // Register application layers
                services.AddApplicationServices();
                services.AddInfrastructureServices(databasePath);

                // Register ViewModels
                // services.AddTransient<MainViewModel>();
                // services.AddTransient<LoginViewModel>();

                // Register main window
                services.AddSingleton<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        // Show the main window via DI
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
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
