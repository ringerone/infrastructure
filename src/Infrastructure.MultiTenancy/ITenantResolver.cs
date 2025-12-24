namespace Infrastructure.MultiTenancy;

/// <summary>
/// Service for resolving tenant information including database connections
/// </summary>
public interface ITenantResolver
{
    Task<TenantContext?> ResolveTenantAsync(string tenantId, CancellationToken cancellationToken = default);
}

