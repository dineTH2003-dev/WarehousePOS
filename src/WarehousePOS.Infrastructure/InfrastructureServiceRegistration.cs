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
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={databasePath}"));

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

        return services;
    }
}

