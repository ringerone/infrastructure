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

