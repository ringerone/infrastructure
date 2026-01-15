using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Telemetry;

/// <summary>
/// Extension methods for registering telemetry services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ITelemetryService with SimpleTelemetryService implementation
    /// </summary>
    public static IServiceCollection AddInfrastructureTelemetry(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register ITelemetryService - simplified telemetry service
        // TODO: Migrate to Infrastructure.Telemetry.IActivitySourceFactory
        services.AddSingleton<ITelemetryService, SimpleTelemetryService>();

        return services;
    }
}

