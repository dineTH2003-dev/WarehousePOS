using Microsoft.Extensions.DependencyInjection;

namespace WarehousePOS.Application;

/// <summary>
/// Extension method to register all Application layer services into the DI container.
/// Called from WarehousePOS.Desktop during startup.
/// </summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register use-case services here as they are implemented.
        // Example:
        // services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
