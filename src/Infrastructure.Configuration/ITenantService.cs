namespace Infrastructure.Configuration;

/// <summary>
/// Service for managing tenants
/// Follows Interface Segregation Principle - focused interface
/// </summary>
public interface ITenantService
{
    /// <summary>
    /// Gets a tenant by identifier
    /// </summary>
    Task<Tenant?> GetTenantAsync(string tenantIdentifier, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all tenants with pagination and filtering
    /// </summary>
    Task<(IEnumerable<Tenant> Items, long TotalCount)> GetAllTenantsPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        TenantStatus? statusFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a tenant
    /// </summary>
    Task SetTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a tenant
    /// </summary>
    Task DeleteTenantAsync(string tenantIdentifier, CancellationToken cancellationToken = default);
}

/// <summary>
/// Tenant status enumeration
/// </summary>
public enum TenantStatus
{
    Active,
    Inactive,
    Pending,
    Suspended
}

/// <summary>
/// Represents a tenant entity
/// </summary>
public class Tenant
{
    public string TenantIdentifier { get; set; } = string.Empty; // Unique identifier (e.g., "acme-corp")
    public string Name { get; set; } = string.Empty; // Display name (e.g., "Acme Corporation")
    public TenantStatus Status { get; set; } = TenantStatus.Pending;
    public string? Comments { get; set; } // Internal notes
    public string? SalesTerms { get; set; } // Contract details, pricing, etc.
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? ContactName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Repository interface for tenant storage
/// Follows Dependency Inversion Principle - depends on abstraction
/// </summary>
public interface ITenantRepository
{
    Task<Tenant?> GetTenantAsync(string tenantIdentifier, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Tenant> Items, long TotalCount)> GetAllTenantsPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        TenantStatus? statusFilter = null,
        CancellationToken cancellationToken = default);
    Task SetTenantAsync(Tenant tenant, CancellationToken cancellationToken = default);
    Task DeleteTenantAsync(string tenantIdentifier, CancellationToken cancellationToken = default);
}

