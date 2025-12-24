namespace Infrastructure.MultiTenancy;

/// <summary>
/// Implementation of ITenantContextAccessor using AsyncLocal for thread-safe context propagation
/// </summary>
public class TenantContextAccessor : ITenantContextAccessor
{
    private static readonly AsyncLocal<TenantContext?> _currentTenant = new();

    public TenantContext? CurrentTenant
    {
        get => _currentTenant.Value;
        set => _currentTenant.Value = value;
    }

    public bool HasTenant => CurrentTenant != null;

    public void SetTenant(string tenantId, string? tenantName = null)
    {
        CurrentTenant = new TenantContext
        {
            TenantId = tenantId,
            TenantName = tenantName
        };
    }

    public void SetTenant(TenantContext tenant)
    {
        if (tenant == null)
            throw new ArgumentNullException(nameof(tenant));
        
        if (string.IsNullOrWhiteSpace(tenant.TenantId))
            throw new ArgumentException("TenantId cannot be null or empty", nameof(tenant));

        CurrentTenant = tenant;
    }

    public void ClearTenant()
    {
        CurrentTenant = null;
    }
}

