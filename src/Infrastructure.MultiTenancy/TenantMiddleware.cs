using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Infrastructure.MultiTenancy;

/// <summary>
/// ASP.NET Core middleware to extract tenant identifier from HTTP requests
/// and propagate it through the application context
/// </summary>
public class TenantMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<TenantMiddleware> _logger;
    private readonly ITenantResolver _tenantResolver;

    public TenantMiddleware(
        RequestDelegate next,
        ITenantContextAccessor tenantContextAccessor,
        ILogger<TenantMiddleware> logger,
        ITenantResolver tenantResolver)
    {
        _next = next;
        _tenantContextAccessor = tenantContextAccessor;
        _logger = logger;
        _tenantResolver = tenantResolver;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract tenant ID from various sources
        var tenantId = ExtractTenantId(context);

        if (!string.IsNullOrEmpty(tenantId))
        {
            // Resolve full tenant context (including database connection)
            var tenantContext = await _tenantResolver.ResolveTenantAsync(tenantId, context.RequestAborted);
            
            if (tenantContext != null)
            {
                // Set tenant context for the current request
                _tenantContextAccessor.SetTenant(tenantContext);

                // Add tenant to logging scope so all logs include tenant ID
                using (_logger.BeginScope(new Dictionary<string, object> 
                { 
                    ["TenantId"] = tenantContext.TenantId,
                    ["TenantName"] = tenantContext.TenantName ?? string.Empty
                }))
                {
                    await _next(context);
                }
            }
            else
            {
                _logger.LogWarning("Tenant {TenantId} not found or not accessible", tenantId);
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsync("Tenant not found or access denied");
                return;
            }
        }
        else
        {
            // No tenant found - continue without tenant context
            await _next(context);
        }

        // Clear tenant context after request completes
        _tenantContextAccessor.ClearTenant();
    }

    /// <summary>
    /// Extracts tenant ID from HTTP request using multiple strategies
    /// </summary>
    private string? ExtractTenantId(HttpContext context)
    {
        // Strategy 1: Custom header (most common)
        var tenantId = context.Request.Headers["X-Tenant-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantId))
        {
            return tenantId;
        }

        // Strategy 2: From subdomain (e.g., tenant1.example.com)
        var host = context.Request.Host.Host;
        var parts = host.Split('.');
        if (parts.Length > 2)
        {
            var subdomain = parts[0];
            if (subdomain != "www" && subdomain != "api")
            {
                return subdomain;
            }
        }

        // Strategy 3: From path (e.g., /api/tenant1/orders)
        var pathSegments = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (pathSegments != null && pathSegments.Length > 1)
        {
            var potentialTenant = pathSegments[1];
            if (IsValidTenantId(potentialTenant))
            {
                return potentialTenant;
            }
        }

        // Strategy 4: From authenticated user's claims
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            tenantId = context.User.FindFirst("tenant_id")?.Value
                    ?? context.User.FindFirst("TenantId")?.Value
                    ?? context.User.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;
            
            if (!string.IsNullOrEmpty(tenantId))
            {
                return tenantId;
            }
        }

        // Strategy 5: Query parameter (least secure, but useful for testing)
        tenantId = context.Request.Query["tenantId"].FirstOrDefault();
        if (!string.IsNullOrEmpty(tenantId))
        {
            return tenantId;
        }

        return null;
    }

    private bool IsValidTenantId(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        // Basic validation - can be extended
        return value.Length > 0 && value.Length < 100;
    }
}

/// <summary>
/// Service for resolving tenant information including database connections
/// </summary>
public interface ITenantResolver
{
    Task<TenantContext?> ResolveTenantAsync(string tenantId, CancellationToken cancellationToken = default);
}

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

/// <summary>
/// Default tenant resolver - can be replaced with custom implementation
/// </summary>
public class DefaultTenantResolver : ITenantResolver
{
    private readonly ILogger<DefaultTenantResolver> _logger;

    public DefaultTenantResolver(ILogger<DefaultTenantResolver> logger)
    {
        _logger = logger;
    }

    public Task<TenantContext?> ResolveTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        // Default implementation - should be replaced with actual tenant store lookup
        _logger.LogWarning("Using default tenant resolver. Implement ITenantResolver for production use.");
        
        return Task.FromResult<TenantContext?>(new TenantContext
        {
            TenantId = tenantId,
            TenantName = tenantId
        });
    }
}

