using System.Collections.Concurrent;
using System.Diagnostics;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDBDriver = MongoDB.Driver;
using Infrastructure.DataAccess;
using Infrastructure.MultiTenancy;
using Infrastructure.Telemetry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.DataAccess.MongoDB;

/// <summary>
/// MongoDB implementation of IDataAccess
/// Abstracts MongoDB-specific details following Liskov Substitution Principle
/// </summary>
public class MongoDbDataAccess : IDataAccess
{
    private readonly MongoDBDriver.IMongoDatabase _database;
    private readonly ILogger<MongoDbDataAccess> _logger;
    private readonly IActivitySourceFactory _activitySourceFactory;
    private readonly ActivitySource _activitySource;

    public MongoDbDataAccess(
        MongoDBDriver.IMongoDatabase database,
        ILogger<MongoDbDataAccess> logger,
        IActivitySourceFactory activitySourceFactory)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySourceFactory = activitySourceFactory ?? throw new ArgumentNullException(nameof(activitySourceFactory));
        _activitySource = _activitySourceFactory.GetActivitySource("Infrastructure.DataAccess.MongoDB");
    }

    public async Task<T?> GetByIdAsync<T>(string id, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = _activitySource.StartActivity("MongoDB.GetById");
        activity?.SetTag("db.operation", "get_by_id");
        activity?.SetTag("db.collection", GetCollectionName<T>());
        activity?.SetTag("db.id", id);

        try
        {
            var collection = GetCollection<T>();
            var filter = MongoDBDriver.Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
            var result = await collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
            
            activity?.SetTag("db.result", result != null ? "found" : "not_found");
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error getting entity by ID {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<T>> FindAsync<T>(FilterDefinition<T> filter, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = _activitySource.StartActivity("MongoDB.Find");
        activity?.SetTag("db.operation", "find");
        activity?.SetTag("db.collection", GetCollectionName<T>());

        try
        {
            var collection = GetCollection<T>();
            var dbFilter = filter.ToDatabaseFilter();
            var mongoFilter = dbFilter is MongoDBDriver.FilterDefinition<T> mongoDbFilter 
                ? mongoDbFilter 
                : MongoDBDriver.Builders<T>.Filter.Empty;
            var results = await collection.Find(mongoFilter).ToListAsync(cancellationToken);
            
            activity?.SetTag("db.result_count", results.Count);
            return results;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error finding entities");
            throw;
        }
    }

    public async Task<T?> FindOneAsync<T>(FilterDefinition<T> filter, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = _activitySource.StartActivity("MongoDB.FindOne");
        activity?.SetTag("db.operation", "find_one");
        activity?.SetTag("db.collection", GetCollectionName<T>());

        try
        {
            var collection = GetCollection<T>();
            var dbFilter = filter.ToDatabaseFilter();
            var mongoFilter = dbFilter is MongoDBDriver.FilterDefinition<T> mongoDbFilter 
                ? mongoDbFilter 
                : MongoDBDriver.Builders<T>.Filter.Empty;
            var result = await collection.Find(mongoFilter).FirstOrDefaultAsync(cancellationToken);
            
            activity?.SetTag("db.result", result != null ? "found" : "not_found");
            return result;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error finding one entity");
            throw;
        }
    }

    public async Task<string> InsertAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = _activitySource.StartActivity("MongoDB.Insert");
        activity?.SetTag("db.operation", "insert");
        activity?.SetTag("db.collection", GetCollectionName<T>());

        try
        {
            var collection = GetCollection<T>();
            await collection.InsertOneAsync(entity, cancellationToken: cancellationToken);
            
            var id = ExtractId(entity);
            activity?.SetTag("db.inserted_id", id);
            
            return id;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error inserting entity");
            throw;
        }
    }

    public async Task<IEnumerable<string>> InsertManyAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = _activitySource.StartActivity("MongoDB.InsertMany");
        activity?.SetTag("db.operation", "insert_many");
        activity?.SetTag("db.collection", GetCollectionName<T>());

        try
        {
            var collection = GetCollection<T>();
            var entityList = entities.ToList();
            await collection.InsertManyAsync(entityList, cancellationToken: cancellationToken);
            
            var ids = entityList.Select(ExtractId).ToList();
            activity?.SetTag("db.inserted_count", ids.Count);
            
            return ids;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error inserting multiple entities");
            throw;
        }
    }

    public async Task<bool> UpdateAsync<T>(string id, T entity, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = _activitySource.StartActivity("MongoDB.Update");
        activity?.SetTag("db.operation", "update");
        activity?.SetTag("db.collection", GetCollectionName<T>());
        activity?.SetTag("db.id", id);

        try
        {
            var collection = GetCollection<T>();
            var filter = MongoDBDriver.Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
            var result = await collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);
            
            activity?.SetTag("db.modified_count", result.ModifiedCount);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error updating entity {Id}", id);
            throw;
        }
    }

    public async Task<bool> UpdateAsync<T>(string id, UpdateDefinition<T> update, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = _activitySource.StartActivity("MongoDB.Update");
        activity?.SetTag("db.operation", "update");
        activity?.SetTag("db.collection", GetCollectionName<T>());
        activity?.SetTag("db.id", id);

        try
        {
            var collection = GetCollection<T>();
            var filter = MongoDBDriver.Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
            var dbUpdate = update.ToDatabaseUpdate();
            var mongoUpdate = dbUpdate is MongoDBDriver.UpdateDefinition<T> mongoDbUpdate 
                ? mongoDbUpdate 
                : MongoDBDriver.Builders<T>.Update.Set("_id", ObjectId.Parse(id));
            var result = await collection.UpdateOneAsync(filter, mongoUpdate, cancellationToken: cancellationToken);
            
            activity?.SetTag("db.modified_count", result.ModifiedCount);
            return result.ModifiedCount > 0;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error updating entity {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync<T>(string id, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = _activitySource.StartActivity("MongoDB.Delete");
        activity?.SetTag("db.operation", "delete");
        activity?.SetTag("db.collection", GetCollectionName<T>());
        activity?.SetTag("db.id", id);

        try
        {
            var collection = GetCollection<T>();
            var filter = MongoDBDriver.Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
            var result = await collection.DeleteOneAsync(filter, cancellationToken);
            
            activity?.SetTag("db.deleted_count", result.DeletedCount);
            return result.DeletedCount > 0;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error deleting entity {Id}", id);
            throw;
        }
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(QueryDefinition<T> query, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = _activitySource.StartActivity("MongoDB.Query");
        activity?.SetTag("db.operation", "query");
        activity?.SetTag("db.collection", GetCollectionName<T>());

        try
        {
            var collection = GetCollection<T>();
            var dbQuery = query.ToDatabaseQuery();
            // This would need to be implemented based on your query abstraction
            _logger.LogWarning("Custom query execution not fully implemented");
            return Array.Empty<T>();
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error executing query");
            throw;
        }
    }

    public async Task<long> CountAsync<T>(FilterDefinition<T> filter, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = _activitySource.StartActivity("MongoDB.Count");
        activity?.SetTag("db.operation", "count");
        activity?.SetTag("db.collection", GetCollectionName<T>());

        try
        {
            var collection = GetCollection<T>();
            var dbFilter = filter.ToDatabaseFilter();
            var mongoFilter = dbFilter is MongoDBDriver.FilterDefinition<T> mongoDbFilter 
                ? mongoDbFilter 
                : MongoDBDriver.Builders<T>.Filter.Empty;
            var count = await collection.CountDocumentsAsync(mongoFilter, cancellationToken: cancellationToken);
            
            activity?.SetTag("db.count", count);
            return count;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error counting entities");
            throw;
        }
    }

    public async Task<bool> ExistsAsync<T>(string id, CancellationToken cancellationToken = default) where T : class
    {
        using var activity = _activitySource.StartActivity("MongoDB.Exists");
        activity?.SetTag("db.operation", "exists");
        activity?.SetTag("db.collection", GetCollectionName<T>());
        activity?.SetTag("db.id", id);

        try
        {
            var collection = GetCollection<T>();
            var filter = MongoDBDriver.Builders<T>.Filter.Eq("_id", ObjectId.Parse(id));
            var count = await collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
            
            var exists = count > 0;
            activity?.SetTag("db.exists", exists);
            return exists;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Error checking entity existence {Id}", id);
            throw;
        }
    }

    private MongoDBDriver.IMongoCollection<T> GetCollection<T>()
    {
        var collectionName = GetCollectionName<T>();
        return _database.GetCollection<T>(collectionName);
    }

    private string GetCollectionName<T>()
    {
        // Use type name as collection name, or implement custom naming strategy
        return typeof(T).Name;
    }

    private string ExtractId<T>(T entity)
    {
        var idProperty = typeof(T).GetProperty("Id") ?? typeof(T).GetProperty("_id");
        if (idProperty != null)
        {
            var value = idProperty.GetValue(entity);
            return value?.ToString() ?? string.Empty;
        }
        
        return string.Empty;
    }
}

/// <summary>
/// MongoDB-specific filter definition
/// </summary>
public class MongoDbFilterDefinition<T> : FilterDefinition<T>
{
    private readonly MongoDBDriver.FilterDefinition<T> _filter;

    public MongoDbFilterDefinition(MongoDBDriver.FilterDefinition<T> filter)
    {
        _filter = filter ?? throw new ArgumentNullException(nameof(filter));
    }

    public override object ToDatabaseFilter()
    {
        return _filter;
    }
}

/// <summary>
/// MongoDB-specific update definition
/// </summary>
public class MongoDbUpdateDefinition<T> : UpdateDefinition<T>
{
    private readonly MongoDBDriver.UpdateDefinition<T> _update;

    public MongoDbUpdateDefinition(MongoDBDriver.UpdateDefinition<T> update)
    {
        _update = update ?? throw new ArgumentNullException(nameof(update));
    }

    public override object ToDatabaseUpdate()
    {
        return _update;
    }
}

/// <summary>
/// Factory for creating MongoDB data access instances per tenant
/// </summary>
public class MongoDbDataAccessFactory : IDataAccessFactory
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<MongoDbDataAccessFactory> _logger;
    private readonly IActivitySourceFactory _activitySourceFactory;
    private readonly DataAccessOptions _options;
    private readonly ConcurrentDictionary<string, IDataAccess> _dataAccessCache = new();

    public MongoDbDataAccessFactory(
        ITenantContextAccessor tenantContextAccessor,
        ILogger<MongoDbDataAccessFactory> logger,
        IActivitySourceFactory activitySourceFactory,
        DataAccessOptions options)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySourceFactory = activitySourceFactory ?? throw new ArgumentNullException(nameof(activitySourceFactory));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<IDataAccess> GetDataAccessAsync(CancellationToken cancellationToken = default)
    {
        var tenant = _tenantContextAccessor.CurrentTenant;
        if (tenant == null)
            throw new InvalidOperationException("No tenant context available");

        return await GetDataAccessAsync(tenant, cancellationToken);
    }

    public async Task<IDataAccess> GetDataAccessAsync(TenantContext tenant, CancellationToken cancellationToken = default)
    {
        if (tenant == null)
            throw new ArgumentNullException(nameof(tenant));

        var cacheKey = $"{tenant.TenantId}:{tenant.DatabaseName ?? _options.DefaultDatabaseName}";
        
        return _dataAccessCache.GetOrAdd(cacheKey, _ =>
        {
            var connectionString = tenant.DatabaseConnectionString ?? _options.DefaultConnectionString;
            if (string.IsNullOrEmpty(connectionString))
                throw new InvalidOperationException("Database connection string is not configured");

            var databaseName = tenant.DatabaseName ?? _options.DefaultDatabaseName;
            if (string.IsNullOrEmpty(databaseName))
                throw new InvalidOperationException("Database name is not configured");

            var client = new MongoDBDriver.MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);

            var loggerFactory = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Information));
            var logger = loggerFactory.CreateLogger<MongoDbDataAccess>();

            return new MongoDbDataAccess(database, logger, _activitySourceFactory);
        });
    }
}

/// <summary>
/// Extension methods for registering MongoDB data access
/// </summary>
public static class MongoDbDataAccessExtensions
{
    public static IServiceCollection AddMongoDbDataAccess(
        this IServiceCollection services,
        Action<DataAccessOptions>? configureOptions = null)
    {
        var options = new DataAccessOptions();
        configureOptions?.Invoke(options);
        
        services.AddSingleton(options);
        services.AddSingleton<IDataAccessFactory, MongoDbDataAccessFactory>();
        
        return services;
    }
}

