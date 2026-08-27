using Microsoft.Extensions.DependencyInjection;
using WarehousePOS.Application.Authentication;
using WarehousePOS.Application.Products;

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

        return services;
    }
}
