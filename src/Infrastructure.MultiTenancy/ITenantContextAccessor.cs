namespace Infrastructure.MultiTenancy;

/// <summary>
/// Service for managing tenant context in the current execution context
/// Uses AsyncLocal to maintain tenant context across async operations
/// </summary>
public interface ITenantContextAccessor
{
    TenantContext? CurrentTenant { get; set; }
    void SetTenant(string tenantId, string? tenantName = null);
    void SetTenant(TenantContext tenant);
    void ClearTenant();
    bool HasTenant { get; }
}

