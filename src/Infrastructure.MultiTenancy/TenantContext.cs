namespace Infrastructure.MultiTenancy;

/// <summary>
/// Represents tenant context for multi-tenant applications
/// </summary>
public class TenantContext
{
    public string TenantId { get; set; } = string.Empty;
    public string? TenantName { get; set; }
    public Dictionary<string, string> AdditionalProperties { get; set; } = new();
    public string? DatabaseConnectionString { get; set; }
    public string? DatabaseName { get; set; }
}

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

