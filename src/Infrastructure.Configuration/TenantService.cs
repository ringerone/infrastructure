using Infrastructure.MultiTenancy;

namespace Infrastructure.Configuration;

/// <summary>
/// Service implementation for managing tenants
/// </summary>
public class TenantService : ITenantService
{
    private readonly ITenantRepository _repository;

    public TenantService(ITenantRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Tenant?> GetTenantAsync(string tenantIdentifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
            throw new ArgumentException("Tenant identifier cannot be null or empty", nameof(tenantIdentifier));

        return await _repository.GetTenantAsync(tenantIdentifier, cancellationToken);
    }

    public async Task<(IEnumerable<Tenant> Items, long TotalCount)> GetAllTenantsPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        TenantStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        return await _repository.GetAllTenantsPagedAsync(
            pageNumber,
            pageSize,
            searchTerm,
            statusFilter,
            cancellationToken);
    }

    public async Task SetTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        if (tenant == null)
            throw new ArgumentNullException(nameof(tenant));

        if (string.IsNullOrWhiteSpace(tenant.TenantIdentifier))
            throw new ArgumentException("Tenant identifier cannot be null or empty", nameof(tenant));

        if (string.IsNullOrWhiteSpace(tenant.Name))
            throw new ArgumentException("Tenant name cannot be null or empty", nameof(tenant));

        await _repository.SetTenantAsync(tenant, cancellationToken);
    }

    public async Task DeleteTenantAsync(string tenantIdentifier, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tenantIdentifier))
            throw new ArgumentException("Tenant identifier cannot be null or empty", nameof(tenantIdentifier));

        await _repository.DeleteTenantAsync(tenantIdentifier, cancellationToken);
    }
}

