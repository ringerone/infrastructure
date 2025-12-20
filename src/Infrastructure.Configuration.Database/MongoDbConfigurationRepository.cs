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
        // Build filter for all scopes
        var filters = scopeIdentifiers.Select(kvp => BuildFilter(null, kvp.Key, kvp.Value));
        // Combine filters with OR
        var combinedFilter = new MongoDbFilterDefinition<ConfigurationDocument>(
            Builders<ConfigurationDocument>.Filter.Or(filters.Select(f => (MongoDB.Driver.FilterDefinition<ConfigurationDocument>)f.ToDatabaseFilter())));

        var documents = await _dataAccess.FindAsync<ConfigurationDocument>(combinedFilter, cancellationToken);
        return documents.Select(d => d.ToConfigurationEntry());
    }

    public async Task SetConfigurationAsync(ConfigurationEntry entry, CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(entry.Key, entry.Scope, entry.ScopeIdentifier);
        var existing = await _dataAccess.FindOneAsync<ConfigurationDocument>(filter, cancellationToken);

        var document = ConfigurationDocument.FromConfigurationEntry(entry, existing?.Id);

        if (existing != null)
        {
            await _dataAccess.UpdateAsync<ConfigurationDocument>(existing.Id, document, cancellationToken);
        }
        else
        {
            await _dataAccess.InsertAsync(document, cancellationToken);
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
        var filters = scopeIdentifiers.Select(kvp => BuildFilter(null, kvp.Key, kvp.Value));
        var combinedFilter = new MongoDbFilterDefinition<FeatureFlagDocument>(
            Builders<FeatureFlagDocument>.Filter.Or(filters.Select(f => (MongoDB.Driver.FilterDefinition<FeatureFlagDocument>)f.ToDatabaseFilter())));

        var documents = await _dataAccess.FindAsync<FeatureFlagDocument>(combinedFilter, cancellationToken);
        return documents.Select(d => d.ToFeatureFlag());
    }

    public async Task SetFeatureFlagAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
    {
        var filter = BuildFilter(flag.Name, flag.Scope, flag.ScopeIdentifier);
        var existing = await _dataAccess.FindOneAsync<FeatureFlagDocument>(filter, cancellationToken);

        var document = FeatureFlagDocument.FromFeatureFlag(flag, existing?.Id);

        if (existing != null)
        {
            await _dataAccess.UpdateAsync<FeatureFlagDocument>(existing.Id, document, cancellationToken);
        }
        else
        {
            await _dataAccess.InsertAsync(document, cancellationToken);
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

