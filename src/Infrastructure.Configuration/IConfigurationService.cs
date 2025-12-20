namespace Infrastructure.Configuration;

/// <summary>
/// Service for retrieving configuration values with hierarchical resolution
/// Hierarchy: User -> Tenant -> Region -> Environment -> Global (most specific wins)
/// Follows Interface Segregation Principle - focused interface
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// Gets a configuration value with hierarchical resolution
    /// </summary>
    /// <typeparam name="T">Type of the configuration value</typeparam>
    /// <param name="key">Configuration key</param>
    /// <param name="defaultValue">Default value if not found</param>
    /// <returns>Configuration value or default</returns>
    T GetValue<T>(string key, T defaultValue = default!);

    /// <summary>
    /// Gets a configuration value with explicit hierarchy override
    /// </summary>
    T GetValue<T>(string key, ConfigurationScope scope, T defaultValue = default!);

    /// <summary>
    /// Gets all configuration values for the current context
    /// </summary>
    Dictionary<string, object> GetAllValues();

    /// <summary>
    /// Sets a configuration value at a specific scope
    /// </summary>
    Task SetValueAsync<T>(string key, T value, ConfigurationScope scope, string? scopeIdentifier = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes the configuration cache
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines the scope/level for configuration resolution
/// </summary>
public enum ConfigurationScope
{
    Global,        // Application-wide defaults
    Environment,    // dev, staging, prod
    Region,        // us-east, eu-west, etc.
    Tenant,        // Tenant-specific overrides
    User           // User-specific overrides (most specific)
}

/// <summary>
/// Represents a configuration entry with its scope
/// </summary>
public class ConfigurationEntry
{
    public string Key { get; set; } = string.Empty;
    public object Value { get; set; } = default!;
    public ConfigurationScope Scope { get; set; }
    public string? ScopeIdentifier { get; set; } // e.g., tenant ID, user ID, region name
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Repository interface for configuration storage
/// Follows Dependency Inversion Principle - depends on abstraction
/// </summary>
public interface IConfigurationRepository
{
    Task<ConfigurationEntry?> GetConfigurationAsync(string key, ConfigurationScope scope, string? scopeIdentifier, CancellationToken cancellationToken = default);
    Task<IEnumerable<ConfigurationEntry>> GetAllConfigurationsAsync(Dictionary<ConfigurationScope, string?> scopeIdentifiers, CancellationToken cancellationToken = default);
    Task SetConfigurationAsync(ConfigurationEntry entry, CancellationToken cancellationToken = default);
    Task DeleteConfigurationAsync(string key, ConfigurationScope scope, string? scopeIdentifier, CancellationToken cancellationToken = default);
}

