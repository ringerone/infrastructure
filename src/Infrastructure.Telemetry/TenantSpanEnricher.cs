using System.Diagnostics;
using Infrastructure.MultiTenancy;
using OpenTelemetry;

namespace Infrastructure.Telemetry;

/// <summary>
/// OpenTelemetry span processor that enriches all spans with tenant context
/// This ensures tenant ID is included in all spans automatically
/// </summary>
public class TenantSpanEnricher : BaseProcessor<Activity>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public TenantSpanEnricher(ITenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    public override void OnEnd(Activity data)
    {
        var tenant = _tenantContextAccessor.CurrentTenant;
        if (tenant != null)
        {
            // Add tenant ID to all spans
            data.SetTag("tenant.id", tenant.TenantId);
            
            if (!string.IsNullOrEmpty(tenant.TenantName))
            {
                data.SetTag("tenant.name", tenant.TenantName);
            }

            // Add tenant ID to baggage for propagation across service boundaries
            Baggage.SetBaggage("tenant.id", tenant.TenantId);
            
            if (!string.IsNullOrEmpty(tenant.TenantName))
            {
                Baggage.SetBaggage("tenant.name", tenant.TenantName);
            }
        }

        base.OnEnd(data);
    }
}

/// <summary>
/// Extension methods for adding tenant context to activities
/// </summary>
public static class TenantContextExtensions
{
    /// <summary>
    /// Adds tenant context to the current activity
    /// This ensures tenant ID is included in all spans automatically
    /// </summary>
    public static Activity? AddTenantContext(this Activity? activity, TenantContext? tenant)
    {
        if (activity == null || tenant == null)
            return activity;

        // Add tenant ID as a standard tag (will appear in all spans)
        activity.SetTag("tenant.id", tenant.TenantId);
        
        if (!string.IsNullOrEmpty(tenant.TenantName))
        {
            activity.SetTag("tenant.name", tenant.TenantName);
        }

        // Add tenant ID to baggage for propagation across service boundaries
        Baggage.SetBaggage("tenant.id", tenant.TenantId);
        
        if (!string.IsNullOrEmpty(tenant.TenantName))
        {
            Baggage.SetBaggage("tenant.name", tenant.TenantName);
        }

        return activity;
    }

    /// <summary>
    /// Gets tenant context from baggage (useful when receiving requests from other services)
    /// </summary>
    public static TenantContext? GetTenantFromBaggage()
    {
        var tenantId = Baggage.GetBaggage("tenant.id");
        if (string.IsNullOrEmpty(tenantId))
            return null;

        return new TenantContext
        {
            TenantId = tenantId,
            TenantName = Baggage.GetBaggage("tenant.name")
        };
    }
}

