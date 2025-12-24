using Infrastructure.Configuration;
using Infrastructure.DataAccess;
using Infrastructure.DataAccess.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Infrastructure.Configuration.Database;

/// <summary>
/// MongoDB implementation of IConfigurationRepository
/// Follows Liskov Substitution Principle - can replace any IConfigurationRepository
/// </summary>
public class MongoDbConfigurationRepository : IConfigurationRepository
{
    private readonly IDataAccess _dataAccess;
    private const string CollectionName = "Configurations";

    public MongoDbConfigurationRepository(IDataAccessFactory dataAccessFactory)
    {
        _dataAccess = dataAccessFactory.GetDataAccessAsync().GetAwaiter().GetResult();
    }

    public async Task<ConfigurationEntry?> GetConfigurationAsync(string key, ConfigurationScope scope, string? scopeIdentifier, CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(key, scope, scopeIdentifier);
        var document = await _dataAccess.FindOneAsync<ConfigurationDocument>(filter, cancellationToken);
        return document?.ToConfigurationEntry();
    }

    public async Task<IEnumerable<ConfigurationEntry>> GetAllConfigurationsAsync(Dictionary<ConfigurationScope, string?> scopeIdentifiers, CancellationToken cancellationToken = default)
    {
        // For GetAll, we want to return all configurations regardless of scope
        // The scope filtering and resolution happens at the service layer
        var allFilter = new MongoDbFilterDefinition<ConfigurationDocument>(
            Builders<ConfigurationDocument>.Filter.Empty);
        var allDocuments = await _dataAccess.FindAsync<ConfigurationDocument>(allFilter, cancellationToken);
        return allDocuments.Select(d => d.ToConfigurationEntry());
    }

    public async Task<(IEnumerable<ConfigurationEntry> Items, long TotalCount)> GetAllConfigurationsPagedAsync(
        Dictionary<ConfigurationScope, string?> scopeIdentifiers,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        ConfigurationScope? scopeFilter = null,
        string? scopeIdentifierFilter = null,
        CancellationToken cancellationToken = default)
    {
        var builder = Builders<ConfigurationDocument>.Filter;
        var filter = builder.Empty;

        // Add search filter if provided
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchFilter = builder.Or(
                builder.Regex(x => x.Key, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                builder.Regex(x => x.ScopeIdentifier, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
            );
            filter = builder.And(filter, searchFilter);
        }

        // Add scope filter if provided
        if (scopeFilter.HasValue)
        {
            filter = builder.And(filter, builder.Eq(x => x.Scope, scopeFilter.Value));
        }

        // Add scope identifier filter if provided (only filter if explicitly provided)
        if (!string.IsNullOrEmpty(scopeIdentifierFilter))
        {
            filter = builder.And(filter, builder.Eq(x => x.ScopeIdentifier, scopeIdentifierFilter));
        }
        // Note: If scope is specified but scopeIdentifier is not provided, show ALL entries for that scope
        // (regardless of scopeIdentifier value - this allows users to see all entries for a scope)

        var mongoFilter = new MongoDbFilterDefinition<ConfigurationDocument>(filter);
        
        // Get paginated results using IDataAccess
        var (documents, totalCount) = await _dataAccess.FindPagedAsync<ConfigurationDocument>(
            mongoFilter,
            pageNumber,
            pageSize,
            cancellationToken);

        var items = documents.Select(d => d.ToConfigurationEntry());
        return (items, totalCount);
    }

    public async Task SetConfigurationAsync(ConfigurationEntry entry, CancellationToken cancellationToken = default)
    {
        // First try to find by key only (for editing - allows scope changes)
        var nameOnlyFilter = new MongoDbFilterDefinition<ConfigurationDocument>(
            Builders<ConfigurationDocument>.Filter.Eq(x => x.Key, entry.Key));
        var existing = await _dataAccess.FindOneAsync<ConfigurationDocument>(nameOnlyFilter, cancellationToken);

        if (existing != null)
        {
            // Update existing document (even if scope/scopeIdentifier changed)
            var document = ConfigurationDocument.FromConfigurationEntry(entry, existing.Id);
            document.CreatedAt = existing.CreatedAt; // Preserve original creation date
            document.UpdatedAt = DateTime.UtcNow;
            await _dataAccess.UpdateAsync<ConfigurationDocument>(existing.Id, document, cancellationToken);
        }
        else
        {
            // Check if a document with same key+scope+scopeIdentifier exists (for duplicate prevention)
            var exactFilter = BuildFilter(entry.Key, entry.Scope, entry.ScopeIdentifier);
            var exactMatch = await _dataAccess.FindOneAsync<ConfigurationDocument>(exactFilter, cancellationToken);
            
            if (exactMatch != null)
            {
                // Update existing document with exact match
                var document = ConfigurationDocument.FromConfigurationEntry(entry, exactMatch.Id);
                document.CreatedAt = exactMatch.CreatedAt;
                document.UpdatedAt = DateTime.UtcNow;
                await _dataAccess.UpdateAsync<ConfigurationDocument>(exactMatch.Id, document, cancellationToken);
            }
            else
            {
                // Insert new document - MongoDB will auto-generate _id
                var document = ConfigurationDocument.FromConfigurationEntry(entry, null);
                document.CreatedAt = DateTime.UtcNow;
                await _dataAccess.InsertAsync(document, cancellationToken);
            }
        }
    }

    public async Task DeleteConfigurationAsync(string key, ConfigurationScope scope, string? scopeIdentifier, CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(key, scope, scopeIdentifier);
        var existing = await _dataAccess.FindOneAsync<ConfigurationDocument>(filter, cancellationToken);
        
        if (existing != null)
        {
            await _dataAccess.DeleteAsync<ConfigurationDocument>(existing.Id, cancellationToken);
        }
    }

    private MongoDbFilterDefinition<ConfigurationDocument> BuildFilter(string? key, ConfigurationScope scope, string? scopeIdentifier)
    {
        var builder = Builders<ConfigurationDocument>.Filter;
        var filters = new List<MongoDB.Driver.FilterDefinition<ConfigurationDocument>>();

        if (!string.IsNullOrEmpty(key))
        {
            filters.Add(builder.Eq(x => x.Key, key));
        }

        filters.Add(builder.Eq(x => x.Scope, scope));

        if (scopeIdentifier != null)
        {
            filters.Add(builder.Eq(x => x.ScopeIdentifier, scopeIdentifier));
        }
        else
        {
            filters.Add(builder.Eq(x => x.ScopeIdentifier, (string?)null));
        }

        return new MongoDbFilterDefinition<ConfigurationDocument>(builder.And(filters));
    }
}

/// <summary>
/// MongoDB implementation of IFeatureFlagRepository
/// </summary>
public class MongoDbFeatureFlagRepository : IFeatureFlagRepository
{
    private readonly IDataAccess _dataAccess;
    private const string CollectionName = "FeatureFlags";

    public MongoDbFeatureFlagRepository(IDataAccessFactory dataAccessFactory)
    {
        _dataAccess = dataAccessFactory.GetDataAccessAsync().GetAwaiter().GetResult();
    }

    public async Task<FeatureFlag?> GetFeatureFlagAsync(string name, ConfigurationScope scope, string? scopeIdentifier, CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(name, scope, scopeIdentifier);
        var document = await _dataAccess.FindOneAsync<FeatureFlagDocument>(filter, cancellationToken);
        return document?.ToFeatureFlag();
    }

    public async Task<IEnumerable<FeatureFlag>> GetAllFeatureFlagsAsync(Dictionary<ConfigurationScope, string?> scopeIdentifiers, CancellationToken cancellationToken = default)
    {
        // Return all feature flags, filtering and hierarchical resolution is done in the service layer
        var allFilter = new MongoDbFilterDefinition<FeatureFlagDocument>(
            Builders<FeatureFlagDocument>.Filter.Empty);
        var allDocuments = await _dataAccess.FindAsync<FeatureFlagDocument>(allFilter, cancellationToken);
        return allDocuments.Select(d => d.ToFeatureFlag());
    }

    public async Task<(IEnumerable<FeatureFlag> Items, long TotalCount)> GetAllFeatureFlagsPagedAsync(
        Dictionary<ConfigurationScope, string?> scopeIdentifiers,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        ConfigurationScope? scopeFilter = null,
        string? scopeIdentifierFilter = null,
        CancellationToken cancellationToken = default)
    {
        var builder = Builders<FeatureFlagDocument>.Filter;
        var filter = builder.Empty;

        // Add search filter if provided
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchFilter = builder.Or(
                builder.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                builder.Regex(x => x.ScopeIdentifier, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                builder.Regex(x => x.Variant, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
            );
            filter = builder.And(filter, searchFilter);
        }

        // Add scope filter if provided
        if (scopeFilter.HasValue)
        {
            filter = builder.And(filter, builder.Eq(x => x.Scope, scopeFilter.Value));
        }

        // Add scope identifier filter if provided (only filter if explicitly provided)
        if (!string.IsNullOrEmpty(scopeIdentifierFilter))
        {
            filter = builder.And(filter, builder.Eq(x => x.ScopeIdentifier, scopeIdentifierFilter));
        }
        // Note: If scope is specified but scopeIdentifier is not provided, show ALL entries for that scope
        // (regardless of scopeIdentifier value - this allows users to see all entries for a scope)

        var mongoFilter = new MongoDbFilterDefinition<FeatureFlagDocument>(filter);
        
        // Get paginated results using IDataAccess
        var (documents, totalCount) = await _dataAccess.FindPagedAsync<FeatureFlagDocument>(
            mongoFilter,
            pageNumber,
            pageSize,
            cancellationToken);

        var items = documents.Select(d => d.ToFeatureFlag());
        return (items, totalCount);
    }

    public async Task SetFeatureFlagAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
    {
        // First try to find by name only (for editing - allows scope changes)
        var nameOnlyFilter = new MongoDbFilterDefinition<FeatureFlagDocument>(
            Builders<FeatureFlagDocument>.Filter.Eq(x => x.Name, flag.Name));
        var existing = await _dataAccess.FindOneAsync<FeatureFlagDocument>(nameOnlyFilter, cancellationToken);

        if (existing != null)
        {
            // Update existing document (even if scope/scopeIdentifier changed)
            var document = FeatureFlagDocument.FromFeatureFlag(flag, existing.Id);
            document.CreatedAt = existing.CreatedAt; // Preserve original creation date
            document.UpdatedAt = DateTime.UtcNow;
            await _dataAccess.UpdateAsync<FeatureFlagDocument>(existing.Id, document, cancellationToken);
        }
        else
        {
            // Check if a document with same name+scope+scopeIdentifier exists (for duplicate prevention)
            var exactFilter = BuildFilter(flag.Name, flag.Scope, flag.ScopeIdentifier);
            var exactMatch = await _dataAccess.FindOneAsync<FeatureFlagDocument>(exactFilter, cancellationToken);
            
            if (exactMatch != null)
            {
                // Update existing document with exact match
                var document = FeatureFlagDocument.FromFeatureFlag(flag, exactMatch.Id);
                document.CreatedAt = exactMatch.CreatedAt;
                document.UpdatedAt = DateTime.UtcNow;
                await _dataAccess.UpdateAsync<FeatureFlagDocument>(exactMatch.Id, document, cancellationToken);
            }
            else
            {
                // Insert new document - MongoDB will auto-generate _id
                var document = FeatureFlagDocument.FromFeatureFlag(flag, null);
                document.CreatedAt = DateTime.UtcNow;
                await _dataAccess.InsertAsync(document, cancellationToken);
            }
        }
    }

    public async Task DeleteFeatureFlagAsync(string name, ConfigurationScope scope, string? scopeIdentifier, CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(name, scope, scopeIdentifier);
        var existing = await _dataAccess.FindOneAsync<FeatureFlagDocument>(filter, cancellationToken);
        
        if (existing != null)
        {
            await _dataAccess.DeleteAsync<FeatureFlagDocument>(existing.Id, cancellationToken);
        }
    }

    private MongoDbFilterDefinition<FeatureFlagDocument> BuildFilter(string? name, ConfigurationScope scope, string? scopeIdentifier)
    {
        var builder = Builders<FeatureFlagDocument>.Filter;
        var filters = new List<MongoDB.Driver.FilterDefinition<FeatureFlagDocument>>();

        if (!string.IsNullOrEmpty(name))
        {
            filters.Add(builder.Eq(x => x.Name, name));
        }

        filters.Add(builder.Eq(x => x.Scope, scope));

        if (scopeIdentifier != null)
        {
            filters.Add(builder.Eq(x => x.ScopeIdentifier, scopeIdentifier));
        }
        else
        {
            filters.Add(builder.Eq(x => x.ScopeIdentifier, (string?)null));
        }

        return new MongoDbFilterDefinition<FeatureFlagDocument>(builder.And(filters));
    }
}

/// <summary>
/// MongoDB implementation of ITenantRepository
/// </summary>
public class MongoDbTenantRepository : ITenantRepository
{
    private readonly IDataAccess _dataAccess;

    public MongoDbTenantRepository(IDataAccessFactory dataAccessFactory)
    {
        _dataAccess = dataAccessFactory.GetDataAccessAsync().GetAwaiter().GetResult();
    }

    public async Task<Tenant?> GetTenantAsync(string tenantIdentifier, CancellationToken cancellationToken = default)
    {
        var filter = new MongoDbFilterDefinition<TenantDocument>(
            Builders<TenantDocument>.Filter.Eq(x => x.TenantIdentifier, tenantIdentifier));
        var document = await _dataAccess.FindOneAsync<TenantDocument>(filter, cancellationToken);
        return document?.ToTenant();
    }

    public async Task<(IEnumerable<Tenant> Items, long TotalCount)> GetAllTenantsPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        TenantStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var builder = Builders<TenantDocument>.Filter;
        var filters = new List<MongoDB.Driver.FilterDefinition<TenantDocument>>();

        // Add search filter if provided
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchFilter = builder.Or(
                builder.Regex(x => x.TenantIdentifier, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                builder.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                builder.Regex(x => x.ContactEmail, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i")),
                builder.Regex(x => x.ContactName, new MongoDB.Bson.BsonRegularExpression(searchTerm, "i"))
            );
            filters.Add(searchFilter);
        }

        // Add status filter if provided
        if (statusFilter.HasValue)
        {
            filters.Add(builder.Eq(x => x.Status, statusFilter.Value));
        }

        var combinedFilter = filters.Any() ? builder.And(filters) : builder.Empty;
        var mongoFilter = new MongoDbFilterDefinition<TenantDocument>(combinedFilter);

        // Get paginated results using IDataAccess
        var (documents, totalCount) = await _dataAccess.FindPagedAsync<TenantDocument>(
            mongoFilter,
            pageNumber,
            pageSize,
            cancellationToken);

        var items = documents.Select(d => d.ToTenant());
        return (items, totalCount);
    }

    public async Task SetTenantAsync(Tenant tenant, CancellationToken cancellationToken = default)
    {
        // First try to find by tenant identifier
        var identifierFilter = new MongoDbFilterDefinition<TenantDocument>(
            Builders<TenantDocument>.Filter.Eq(x => x.TenantIdentifier, tenant.TenantIdentifier));
        var existing = await _dataAccess.FindOneAsync<TenantDocument>(identifierFilter, cancellationToken);

        if (existing != null)
        {
            // Update existing document
            var document = TenantDocument.FromTenant(tenant, existing.Id);
            document.CreatedAt = existing.CreatedAt; // Preserve original creation date
            document.UpdatedAt = DateTime.UtcNow;
            await _dataAccess.UpdateAsync<TenantDocument>(existing.Id, document, cancellationToken);
        }
        else
        {
            // Insert new document - MongoDB will auto-generate _id
            var document = TenantDocument.FromTenant(tenant, null);
            document.CreatedAt = DateTime.UtcNow;
            await _dataAccess.InsertAsync(document, cancellationToken);
        }
    }

    public async Task DeleteTenantAsync(string tenantIdentifier, CancellationToken cancellationToken = default)
    {
        var filter = new MongoDbFilterDefinition<TenantDocument>(
            Builders<TenantDocument>.Filter.Eq(x => x.TenantIdentifier, tenantIdentifier));
        var existing = await _dataAccess.FindOneAsync<TenantDocument>(filter, cancellationToken);

        if (existing != null)
        {
            await _dataAccess.DeleteAsync<TenantDocument>(existing.Id, cancellationToken);
        }
    }
}

