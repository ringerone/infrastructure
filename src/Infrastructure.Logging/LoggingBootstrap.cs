using Infrastructure.MultiTenancy;
using Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Diagnostics;

namespace Infrastructure.Logging;

/// <summary>
/// Bootstrap class for OpenTelemetry logging infrastructure
/// All wire-up code is self-contained here following Single Responsibility Principle
/// </summary>
public static class LoggingBootstrap
{
    /// <summary>
    /// Configures OpenTelemetry logging, tracing, and metrics
    /// </summary>
    public static void ConfigureOpenTelemetry(
        IServiceCollection services,
        LoggingOptions options)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        // Register tenant context accessor if not already registered
        if (!services.Any(s => s.ServiceType == typeof(ITenantContextAccessor)))
        {
            services.AddSingleton<ITenantContextAccessor, TenantContextAccessor>();
        }

        // Register ActivitySource and Meter factories
        services.AddSingleton<IActivitySourceFactory, ActivitySourceFactory>();
        services.AddSingleton<IMeterFactory, MeterFactory>();

        // Configure OpenTelemetry
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(options.ServiceName, serviceVersion: options.ServiceVersion)
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = options.Environment ?? "development"
                }))
            .WithTracing(tracing => ConfigureTracing(tracing, options, services))
            .WithMetrics(metrics => ConfigureMetrics(metrics, options))
            .WithLogging(logging => ConfigureLogging(logging, options, services));

        // Configure standard logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            // OpenTelemetry logging is configured above
        });
    }

    private static void ConfigureTracing(
        TracerProviderBuilder builder,
        LoggingOptions options,
        IServiceCollection services)
    {
        // Add tenant enricher if multi-tenancy is enabled
        if (options.EnableMultiTenancy)
        {
            builder.AddProcessor(serviceProvider => 
                new Infrastructure.Telemetry.TenantSpanEnricher(serviceProvider.GetRequiredService<ITenantContextAccessor>()));
        }

        // Add instrumentation
        builder.AddHttpClientInstrumentation();
        builder.AddAspNetCoreInstrumentation();
        builder.AddSource(options.ActivitySourceNames?.ToArray() ?? Array.Empty<string>());

        // Add exporters
        if (options.EnableConsoleExporter)
        {
            builder.AddConsoleExporter();
        }

        if (options.OtlpExporterOptions != null)
        {
            builder.AddOtlpExporter(opt =>
            {
                opt.Endpoint = options.OtlpExporterOptions.Endpoint;
                opt.Headers = options.OtlpExporterOptions.Headers;
            });
        }
    }

    private static void ConfigureMetrics(
        MeterProviderBuilder builder,
        LoggingOptions options)
    {
        builder.AddHttpClientInstrumentation();
        builder.AddAspNetCoreInstrumentation();
        builder.AddRuntimeInstrumentation();
        builder.AddMeter(options.MeterNames?.ToArray() ?? Array.Empty<string>());

        if (options.EnableConsoleExporter)
        {
            builder.AddConsoleExporter();
        }

        if (options.OtlpExporterOptions != null)
        {
            builder.AddOtlpExporter(opt =>
            {
                opt.Endpoint = options.OtlpExporterOptions.Endpoint;
                opt.Headers = options.OtlpExporterOptions.Headers;
            });
        }
    }

    private static void ConfigureLogging(
        LoggerProviderBuilder builder,
        LoggingOptions options,
        IServiceCollection services)
    {
        // Add tenant enricher if multi-tenancy is enabled
        if (options.EnableMultiTenancy)
        {
            builder.AddProcessor(serviceProvider => 
                new TenantLoggingEnricher(serviceProvider.GetRequiredService<ITenantContextAccessor>()));
        }

        if (options.EnableConsoleExporter)
        {
            builder.AddConsoleExporter();
        }

        if (options.OtlpExporterOptions != null)
        {
            builder.AddOtlpExporter(opt =>
            {
                opt.Endpoint = options.OtlpExporterOptions.Endpoint;
                opt.Headers = options.OtlpExporterOptions.Headers;
            });
        }
    }
}

/// <summary>
/// Options for configuring OpenTelemetry logging
/// </summary>
public class LoggingOptions
{
    public string ServiceName { get; set; } = "Application";
    public string? ServiceVersion { get; set; }
    public string? Environment { get; set; }
    public bool EnableMultiTenancy { get; set; } = false;
    public bool EnableConsoleExporter { get; set; } = true;
    public OtlpExporterOptions? OtlpExporterOptions { get; set; }
    public List<string>? ActivitySourceNames { get; set; }
    public List<string>? MeterNames { get; set; }
}

/// <summary>
/// OTLP exporter configuration
/// </summary>
public class OtlpExporterOptions
{
    public Uri Endpoint { get; set; } = new Uri("http://localhost:4317");
    public string? Headers { get; set; }
}

