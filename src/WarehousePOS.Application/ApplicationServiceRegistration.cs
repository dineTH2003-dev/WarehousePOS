using Microsoft.Extensions.DependencyInjection;
using WarehousePOS.Application.Authentication;
using WarehousePOS.Application.Common;

namespace WarehousePOS.Application;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}
