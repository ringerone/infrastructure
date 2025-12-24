using Microsoft.Extensions.Logging;

namespace Infrastructure.MultiTenancy;

/// <summary>
/// Default tenant resolver - retrieves tenant from tenant store via ITenantService
/// </summary>
public class DefaultTenantResolver : ITenantResolver
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<DefaultTenantResolver> _logger;

    public DefaultTenantResolver(
        ITenantService tenantService,
        ILogger<DefaultTenantResolver> logger)
    {
        _tenantService = tenantService ?? throw new ArgumentNullException(nameof(tenantService));
        _logger = logger;
    }

    public async Task<TenantContext?> ResolveTenantAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            _logger.LogWarning("Tenant ID is null or empty");
            return null;
        }

        try
        {
            // Retrieve tenant from tenant store
            var tenant = await _tenantService.GetTenantAsync(tenantId, cancellationToken);

            if (tenant == null)
            {
                _logger.LogWarning("Tenant {TenantId} not found in tenant store", tenantId);
                return null;
            }

            // Only allow active tenants
            if (tenant.Status != TenantStatus.Active)
            {
                _logger.LogWarning("Tenant {TenantId} is not active. Current status: {Status}", tenantId, tenant.Status);
                return null;
            }

            // Map Tenant to TenantContext
            var tenantContext = new TenantContext
            {
                TenantId = tenant.TenantIdentifier,
                TenantName = tenant.Name,
                AdditionalProperties = new Dictionary<string, string>
                {
                    ["Status"] = tenant.Status.ToString(),
                    ["ContactEmail"] = tenant.ContactEmail ?? string.Empty,
                    ["ContactName"] = tenant.ContactName ?? string.Empty,
                    ["ContactPhone"] = tenant.ContactPhone ?? string.Empty
                }
            };

            _logger.LogDebug("Successfully resolved tenant {TenantId} ({TenantName})", tenant.TenantIdentifier, tenant.Name);
            return tenantContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant {TenantId} from tenant store", tenantId);
            return null;
        }
    }
}

