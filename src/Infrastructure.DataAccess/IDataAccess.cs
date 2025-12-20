using Infrastructure.MultiTenancy;

namespace Infrastructure.DataAccess;

/// <summary>
/// Abstract interface for data access operations
/// Abstracts database-specific implementations following Dependency Inversion Principle
/// </summary>
public interface IDataAccess
{
    /// <summary>
    /// Gets a single entity by ID
    /// </summary>
    Task<T?> GetByIdAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets entities matching a filter
    /// </summary>
    Task<IEnumerable<T>> FindAsync<T>(FilterDefinition<T> filter, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Gets a single entity matching a filter
    /// </summary>
    Task<T?> FindOneAsync<T>(FilterDefinition<T> filter, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Inserts a new entity
    /// </summary>
    Task<string> InsertAsync<T>(T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Inserts multiple entities
    /// </summary>
    Task<IEnumerable<string>> InsertManyAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Updates an existing entity
    /// </summary>
    Task<bool> UpdateAsync<T>(string id, T entity, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Updates an existing entity using update definition
    /// </summary>
    Task<bool> UpdateAsync<T>(string id, UpdateDefinition<T> update, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Deletes an entity by ID
    /// </summary>
    Task<bool> DeleteAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Executes a custom query
    /// </summary>
    Task<IEnumerable<T>> QueryAsync<T>(QueryDefinition<T> query, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Counts entities matching a filter
    /// </summary>
    Task<long> CountAsync<T>(FilterDefinition<T> filter, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Checks if an entity exists
    /// </summary>
    Task<bool> ExistsAsync<T>(string id, CancellationToken cancellationToken = default) where T : class;
}

/// <summary>
/// Abstract filter definition - database agnostic
/// </summary>
public abstract class FilterDefinition<T>
{
    public abstract object ToDatabaseFilter();
}

/// <summary>
/// Abstract update definition - database agnostic
/// </summary>
public abstract class UpdateDefinition<T>
{
    public abstract object ToDatabaseUpdate();
}

/// <summary>
/// Abstract query definition - database agnostic
/// </summary>
public abstract class QueryDefinition<T>
{
    public abstract object ToDatabaseQuery();
}

/// <summary>
/// Factory interface for creating data access instances per tenant
/// Follows Open/Closed Principle - can be extended for different databases
/// </summary>
public interface IDataAccessFactory
{
    /// <summary>
    /// Gets or creates a data access instance for the current tenant
    /// </summary>
    Task<IDataAccess> GetDataAccessAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets or creates a data access instance for a specific tenant
    /// </summary>
    Task<IDataAccess> GetDataAccessAsync(TenantContext tenant, CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for configuring data access
/// </summary>
public class DataAccessOptions
{
    public string? DefaultConnectionString { get; set; }
    public string? DefaultDatabaseName { get; set; }
    public int ConnectionPoolSize { get; set; } = 100;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
}

