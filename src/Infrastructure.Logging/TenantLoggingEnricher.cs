using Infrastructure.MultiTenancy;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Infrastructure.Logging;

/// <summary>
/// OpenTelemetry log processor that enriches all logs with tenant context
/// This ensures tenant ID is included in all log records automatically
/// </summary>
public class TenantLoggingEnricher : BaseProcessor<LogRecord>
{
    private readonly ITenantContextAccessor _tenantContextAccessor;

    public TenantLoggingEnricher(ITenantContextAccessor tenantContextAccessor)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
    }

    public override void OnEnd(LogRecord data)
    {
        var tenant = _tenantContextAccessor.CurrentTenant;
        if (tenant != null)
        {
            // Add tenant ID to all log records using the Attributes property setter
            var attributes = new List<KeyValuePair<string, object?>>();
            
            // Copy existing attributes if any
            if (data.Attributes != null)
            {
                attributes.AddRange(data.Attributes);
            }
            
            // Add tenant attributes
            attributes.Add(new KeyValuePair<string, object?>("tenant.id", tenant.TenantId));
            
            if (!string.IsNullOrEmpty(tenant.TenantName))
            {
                attributes.Add(new KeyValuePair<string, object?>("tenant.name", tenant.TenantName));
            }
            
            // Set the new attributes list
            data.Attributes = attributes;
        }

        base.OnEnd(data);
    }
}

