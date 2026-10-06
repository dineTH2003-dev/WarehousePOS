using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WarehousePOS.Application.Common;
using WarehousePOS.Domain.Interfaces;
using WarehousePOS.Infrastructure.Persistence;
using WarehousePOS.Infrastructure.Repositories;
using WarehousePOS.Infrastructure.Security;

namespace WarehousePOS.Infrastructure;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        string databasePath)
    {
        // EF Core + SQLite
        var connectionStringBuilder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            DefaultTimeout = 10,
            ForeignKeys = true
        };
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionStringBuilder.ToString()));

        // Security
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IPurchaseRepository, PurchaseRepository>();
        services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<IStoreSettingRepository, StoreSettingRepository>();
        services.AddScoped<WarehousePOS.Domain.Common.IUnitOfWork, UnitOfWork>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ISupplierEntitlementRepository, SupplierProductEntitlementRepository>();

        // Printing & Hardware
        services.AddScoped<Application.Printing.IReceiptPrinter, Printing.EpsonLq310Printer>();

        // Backup
        services.AddSingleton<IBackupService>(sp =>
            new Backup.BackupService(databasePath, sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<Backup.BackupService>>()));
        services.AddSingleton<ICloudBackupService, Backup.GoogleDriveBackupService>();

        // Notifications (Brevo Email)
        services.AddHttpClient<Notifications.BrevoEmailService>();
        services.AddScoped<Application.Notifications.IEmailNotificationService>(sp => sp.GetRequiredService<Notifications.BrevoEmailService>());
        services.AddScoped<Application.Notifications.INotificationOrchestrator, Notifications.NotificationOrchestrator>();

        return services;
    }
}

