using Microsoft.Extensions.DependencyInjection;

namespace WarehousePOS.Infrastructure;

/// <summary>
/// Extension method to register all Infrastructure layer services into the DI container.
/// Called from WarehousePOS.Desktop during startup.
/// </summary>
public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string databasePath)
    {
        // Register EF Core + SQLite
        // services.AddDbContext<AppDbContext>(options =>
        //     options.UseSqlite($"Data Source={databasePath}"));

        // Register repositories
        // services.AddScoped<IProductRepository, ProductRepository>();

        // Register printer service
        // services.AddSingleton<IPrinterService, EpsonPrinterService>();

        // Register backup service
        // services.AddSingleton<IBackupService, BackupService>();

        return services;
    }
}
