using Microsoft.Extensions.DependencyInjection;
using WarehousePOS.Application.Authentication;
using WarehousePOS.Application.Products;
using WarehousePOS.Application.Purchasing;
using WarehousePOS.Application.Suppliers;

namespace WarehousePOS.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Authentication
        services.AddScoped<IAuthService, AuthService>();

        // Products & Categories
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();

        // Suppliers
        services.AddScoped<ISupplierService, SupplierService>();

        // Purchasing & Inventory
        services.AddScoped<IPurchaseService, PurchaseService>();
        services.AddScoped<IInventoryService, InventoryService>();

        return services;
    }
}
