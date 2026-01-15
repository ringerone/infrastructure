using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Logging;

/// <summary>
/// Extension methods for registering OIDC logging services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers IOidcLogger with Serilog-based implementation
    /// </summary>
    public static IServiceCollection AddInfrastructureLogging(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register IOidcLogger - using Serilog-based implementation
        // This wraps the standard ILogger<T> for OIDC-specific logging
        services.AddScoped<IOidcLogger>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SerilogLogger>>();
            return new SerilogLogger(logger);
        });

        return services;
    }
}

