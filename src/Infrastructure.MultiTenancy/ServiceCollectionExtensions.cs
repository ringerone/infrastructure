using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.MultiTenancy;

/// <summary>
/// Extension methods for registering telemetry services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ITelemetryService with SimpleTelemetryService implementation
    /// </summary>
    public static IServiceCollection AddInfrastructureMultiTenancy(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));


        services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();

        return services;
    }
}

