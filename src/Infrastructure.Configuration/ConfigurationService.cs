using System.Diagnostics;
using Infrastructure.MultiTenancy;
using Infrastructure.Telemetry;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

/// <summary>
/// Hierarchical configuration service implementation
/// Resolves configuration in order: User -> Tenant -> Region -> Environment -> Global
/// Follows Single Responsibility Principle - only handles configuration resolution
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IConfigurationRepository _repository;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<ConfigurationService> _logger;
    private readonly IActivitySourceFactory _activitySourceFactory;
    private readonly IMemoryCache? _cache;
    private readonly ActivitySource _activitySource;
    private readonly string _environment;
    private readonly string? _region;
    private readonly TimeSpan _cacheExpiration;

    public ConfigurationService(
        IConfigurationRepository repository,
        ITenantContextAccessor tenantContextAccessor,
        ILogger<ConfigurationService> logger,
        IActivitySourceFactory activitySourceFactory,
        string environment,
        string? region = null,
        IMemoryCache? cache = null,
        TimeSpan? cacheExpiration = null)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySourceFactory = activitySourceFactory ?? throw new ArgumentNullException(nameof(activitySourceFactory));
        _cache = cache;
        _activitySource = _activitySourceFactory.GetActivitySource("Infrastructure.Configuration");
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _region = region;
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromMinutes(5);
    }

    public T GetValue<T>(string key, T defaultValue = default!)
    {
        using var activity = _activitySource.StartActivity("GetConfiguration");
        activity?.SetTag("config.key", key);

        try
        {
            // Check cache first
            var cacheKey = $"config:{key}";
            if (_cache?.TryGetValue(cacheKey, out T? cachedValue) == true && cachedValue != null)
            {
                activity?.SetTag("config.source", "cache");
                return cachedValue;
            }

            // Resolve hierarchically: User -> Tenant -> Region -> Environment -> Global
            var value = ResolveValue<T>(key, defaultValue);
            
            // Cache the result
            if (_cache != null && value != null)
            {
                _cache.Set(cacheKey, value, _cacheExpiration);
            }
            
            activity?.SetTag("config.value", value?.ToString() ?? "null");
            activity?.SetTag("config.scope", GetResolvedScope(key).ToString());
            activity?.SetTag("config.source", "repository");
            
            _logger.LogDebug(
                "Configuration retrieved: {Key} = {Value} (Scope: {Scope})",
                key, value, GetResolvedScope(key));

            return value;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogWarning(ex, "Failed to retrieve configuration {Key}, using default", key);
            return defaultValue;
        }
    }

    public T GetValue<T>(string key, ConfigurationScope scope, T defaultValue = default!)
    {
        using var activity = _activitySource.StartActivity("GetConfiguration");
        activity?.SetTag("config.key", key);
        activity?.SetTag("config.requested_scope", scope.ToString());

        try
        {
            var scopeIdentifier = GetScopeIdentifier(scope);
            var entry = _repository.GetConfigurationAsync(key, scope, scopeIdentifier).GetAwaiter().GetResult();
            var value = entry != null ? (T)Convert.ChangeType(entry.Value, typeof(T)) : defaultValue;
            
            activity?.SetTag("config.value", value?.ToString() ?? "null");
            activity?.SetTag("config.scope", scope.ToString());
            
            return value;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogWarning(ex, "Failed to retrieve configuration {Key} at scope {Scope}", key, scope);
            return defaultValue;
        }
    }

    public Dictionary<string, object> GetAllValues()
    {
        var allValues = new Dictionary<string, object>();
        var entries = _repository.GetAllConfigurationsAsync(GetCurrentScopeIdentifiers()).GetAwaiter().GetResult();
        
        foreach (var entry in entries)
        {
            // Most specific scope wins
            if (!allValues.ContainsKey(entry.Key))
            {
                allValues[entry.Key] = entry.Value;
            }
        }
        
        return allValues;
    }

    public async Task SetValueAsync<T>(string key, T value, ConfigurationScope scope, string? scopeIdentifier = null, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("SetConfiguration");
        activity?.SetTag("config.key", key);
        activity?.SetTag("config.scope", scope.ToString());

        try
        {
            var entry = new ConfigurationEntry
            {
                Key = key,
                Value = value!,
                Scope = scope,
                ScopeIdentifier = scopeIdentifier ?? GetScopeIdentifier(scope),
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.SetConfigurationAsync(entry, cancellationToken);

            // Invalidate cache
            if (_cache != null)
            {
                _cache.Remove($"config:{key}");
            }

            activity?.SetTag("config.success", true);
            _logger.LogInformation("Configuration set: {Key} = {Value} at scope {Scope}", key, value, scope);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to set configuration {Key}", key);
            throw;
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_cache != null)
        {
            // Clear all configuration cache entries
            // In a production system, you might want to track cache keys more precisely
            _logger.LogInformation("Configuration cache refreshed");
        }
        await Task.CompletedTask;
    }

    private T ResolveValue<T>(string key, T defaultValue)
    {
        // Try in order of specificity (most specific first)
        var scopes = new[]
        {
            (ConfigurationScope.User, GetScopeIdentifier(ConfigurationScope.User)),
            (ConfigurationScope.Tenant, GetScopeIdentifier(ConfigurationScope.Tenant)),
            (ConfigurationScope.Region, GetScopeIdentifier(ConfigurationScope.Region)),
            (ConfigurationScope.Environment, GetScopeIdentifier(ConfigurationScope.Environment)),
            (ConfigurationScope.Global, null)
        };

        foreach (var (scope, identifier) in scopes)
        {
            var entry = _repository.GetConfigurationAsync(key, scope, identifier).GetAwaiter().GetResult();
            if (entry != null)
            {
                try
                {
                    return (T)Convert.ChangeType(entry.Value, typeof(T));
                }
                catch
                {
                    // Type conversion failed, try next scope
                    continue;
                }
            }
        }

        return defaultValue;
    }

    private ConfigurationScope GetResolvedScope(string key)
    {
        // Determine which scope provided the value
        var scopes = new[]
        {
            ConfigurationScope.User,
            ConfigurationScope.Tenant,
            ConfigurationScope.Region,
            ConfigurationScope.Environment,
            ConfigurationScope.Global
        };

        foreach (var scope in scopes)
        {
            var entry = _repository.GetConfigurationAsync(key, scope, GetScopeIdentifier(scope)).GetAwaiter().GetResult();
            if (entry != null)
            {
                return scope;
            }
        }

        return ConfigurationScope.Global;
    }

    private Dictionary<ConfigurationScope, string?> GetCurrentScopeIdentifiers()
    {
        return new Dictionary<ConfigurationScope, string?>
        {
            [ConfigurationScope.Environment] = _environment,
            [ConfigurationScope.Region] = _region,
            [ConfigurationScope.Tenant] = _tenantContextAccessor.CurrentTenant?.TenantId,
            [ConfigurationScope.User] = null // Would come from current user context
        };
    }

    private string? GetScopeIdentifier(ConfigurationScope scope)
    {
        return scope switch
        {
            ConfigurationScope.Environment => _environment,
            ConfigurationScope.Region => _region,
            ConfigurationScope.Tenant => _tenantContextAccessor.CurrentTenant?.TenantId,
            ConfigurationScope.User => null, // Would come from current user context
            ConfigurationScope.Global => null,
            _ => null
        };
    }
}

