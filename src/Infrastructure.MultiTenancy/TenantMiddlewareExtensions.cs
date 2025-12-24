using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.MultiTenancy;

/// <summary>
/// Extension methods for registering tenant middleware
/// </summary>
public static class TenantMiddlewareExtensions
{
    public static IApplicationBuilder UseTenantContext(this IApplicationBuilder app)
    {
        return app.UseMiddleware<TenantMiddleware>();
    }

    public static IServiceCollection AddMultiTenancy(this IServiceCollection services)
    {
        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped<ITenantResolver, DefaultTenantResolver>();
        return services;
    }

    public static IServiceCollection AddMultiTenancy<TTenantResolver>(this IServiceCollection services) 
        where TTenantResolver : class, ITenantResolver
    {
        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        services.AddScoped<ITenantResolver, TTenantResolver>();
        return services;
    }
}

