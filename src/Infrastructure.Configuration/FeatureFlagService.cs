using System.Diagnostics;
using Infrastructure.MultiTenancy;
using Infrastructure.Telemetry;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Configuration;

/// <summary>
/// Feature flag service with hierarchical resolution and percentage rollouts
/// Follows Single Responsibility Principle - only handles feature flag evaluation
/// </summary>
public class FeatureFlagService : IFeatureFlagService
{
    private readonly IFeatureFlagRepository _repository;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<FeatureFlagService> _logger;
    private readonly IActivitySourceFactory _activitySourceFactory;
    private readonly IMemoryCache? _cache;
    private readonly ActivitySource _activitySource;
    private readonly string _environment;
    private readonly string? _region;
    private readonly TimeSpan _cacheExpiration;

    public FeatureFlagService(
        IFeatureFlagRepository repository,
        ITenantContextAccessor tenantContextAccessor,
        ILogger<FeatureFlagService> logger,
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
        _activitySource = _activitySourceFactory.GetActivitySource("Infrastructure.FeatureFlags");
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _region = region;
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromMinutes(5);
    }

    public bool IsEnabled(string featureName)
    {
        var context = CreateContext();
        return IsEnabled(featureName, context);
    }

    public bool IsEnabled(string featureName, FeatureFlagContext context)
    {
        using var activity = _activitySource.StartActivity("CheckFeatureFlag");
        activity?.SetTag("feature.name", featureName);
        activity?.SetTag("feature.tenant", context.TenantId ?? "none");

        try
        {
            // Check cache first
            var cacheKey = $"feature:{featureName}:{context.TenantId}:{context.UserId}";
            if (_cache?.TryGetValue(cacheKey, out bool? cachedValue) == true && cachedValue.HasValue)
            {
                activity?.SetTag("feature.enabled", cachedValue.Value);
                activity?.SetTag("feature.source", "cache");
                return cachedValue.Value;
            }

            // Resolve feature flag hierarchically
            var flag = ResolveFeatureFlag(featureName, context).GetAwaiter().GetResult();
            
            if (flag == null)
            {
                activity?.SetTag("feature.enabled", false);
                activity?.SetTag("feature.reason", "not_found");
                return false;
            }

            // Check if base flag is enabled
            if (!flag.Enabled)
            {
                activity?.SetTag("feature.enabled", false);
                activity?.SetTag("feature.reason", "disabled");
                return false;
            }

            // Check targeting rules
            if (flag.Rules.Any() && !EvaluateRules(flag.Rules, context))
            {
                activity?.SetTag("feature.enabled", false);
                activity?.SetTag("feature.reason", "rules_not_met");
                return false;
            }

            // Check percentage rollout
            var enabled = CheckRollout(flag, context);
            
            // Cache the result
            if (_cache != null)
            {
                _cache.Set(cacheKey, enabled, _cacheExpiration);
            }
            
            activity?.SetTag("feature.enabled", enabled);
            activity?.SetTag("feature.rollout_percentage", flag.RolloutPercentage);
            activity?.SetTag("feature.scope", flag.Scope.ToString());
            activity?.SetTag("feature.source", "repository");
            
            _logger.LogDebug(
                "Feature flag {FeatureName} = {Enabled} (Scope: {Scope}, Rollout: {Rollout}%)",
                featureName, enabled, flag.Scope, flag.RolloutPercentage);

            return enabled;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogWarning(ex, "Error checking feature flag {FeatureName}", featureName);
            return false;
        }
    }

    public string? GetVariant(string featureName, string? defaultValue = null)
    {
        var context = CreateContext();
        var flag = ResolveFeatureFlag(featureName, context).GetAwaiter().GetResult();
        return flag?.Variant ?? defaultValue;
    }

    public Dictionary<string, bool> GetAllFlags()
    {
        var context = CreateContext();
        var flags = _repository.GetAllFeatureFlagsAsync(GetScopeIdentifiers(context)).GetAwaiter().GetResult();
        
        var result = new Dictionary<string, bool>();
        foreach (var flag in flags)
        {
            // Resolve each flag (most specific wins)
            if (!result.ContainsKey(flag.Name))
            {
                result[flag.Name] = IsEnabled(flag.Name, context);
            }
        }
        
        return result;
    }

    public async Task SetFeatureFlagAsync(FeatureFlag flag, CancellationToken cancellationToken = default)
    {
        using var activity = _activitySource.StartActivity("SetFeatureFlag");
        activity?.SetTag("feature.name", flag.Name);

        try
        {
            flag.UpdatedAt = DateTime.UtcNow;
            await _repository.SetFeatureFlagAsync(flag, cancellationToken);

            // Invalidate cache
            if (_cache != null)
            {
                _cache.Remove($"feature:{flag.Name}");
            }

            activity?.SetTag("feature.success", true);
            _logger.LogInformation("Feature flag set: {FeatureName} = {Enabled}", flag.Name, flag.Enabled);
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogError(ex, "Failed to set feature flag {FeatureName}", flag.Name);
            throw;
        }
    }

    private async Task<FeatureFlag?> ResolveFeatureFlag(string featureName, FeatureFlagContext context)
    {
        // Try in order of specificity
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
            var identifier = GetScopeIdentifier(scope, context);
            var flag = await _repository.GetFeatureFlagAsync(featureName, scope, identifier);
            if (flag != null)
            {
                return flag;
            }
        }

        return null;
    }

    private bool CheckRollout(FeatureFlag flag, FeatureFlagContext context)
    {
        if (flag.RolloutPercentage >= 100)
            return true;

        if (flag.RolloutPercentage <= 0)
            return false;

        // Use a consistent hash of context to ensure same user/tenant always gets same result
        var hashInput = $"{flag.Name}:{context.TenantId}:{context.UserId}:{context.Region}";
        var hash = Math.Abs(hashInput.GetHashCode());
        var percentage = (hash % 100) + 1; // 1-100

        return percentage <= flag.RolloutPercentage;
    }

    private bool EvaluateRules(List<FeatureFlagRule> rules, FeatureFlagContext context)
    {
        foreach (var rule in rules)
        {
            if (!context.Attributes.TryGetValue(rule.Attribute, out var attributeValue))
            {
                // Check tenant/user context
                attributeValue = rule.Attribute.ToLower() switch
                {
                    "tenantid" or "tenant_id" => context.TenantId,
                    "userid" or "user_id" => context.UserId,
                    "region" => context.Region,
                    "environment" => context.Environment,
                    _ => null
                };
            }

            if (attributeValue == null)
                return false;

            var matches = rule.Operator.ToLower() switch
            {
                "equals" => attributeValue.Equals(rule.Value, StringComparison.OrdinalIgnoreCase),
                "contains" => attributeValue.Contains(rule.Value, StringComparison.OrdinalIgnoreCase),
                "startswith" => attributeValue.StartsWith(rule.Value, StringComparison.OrdinalIgnoreCase),
                "endswith" => attributeValue.EndsWith(rule.Value, StringComparison.OrdinalIgnoreCase),
                _ => false
            };

            if (!matches)
                return false;
        }

        return true;
    }

    private FeatureFlagContext CreateContext()
    {
        return new FeatureFlagContext
        {
            TenantId = _tenantContextAccessor.CurrentTenant?.TenantId,
            Region = _region,
            Environment = _environment
        };
    }

    private Dictionary<ConfigurationScope, string?> GetScopeIdentifiers(FeatureFlagContext context)
    {
        return new Dictionary<ConfigurationScope, string?>
        {
            [ConfigurationScope.Environment] = context.Environment,
            [ConfigurationScope.Region] = context.Region,
            [ConfigurationScope.Tenant] = context.TenantId,
            [ConfigurationScope.User] = context.UserId
        };
    }

    private string? GetScopeIdentifier(ConfigurationScope scope, FeatureFlagContext context)
    {
        return scope switch
        {
            ConfigurationScope.Environment => context.Environment,
            ConfigurationScope.Region => context.Region,
            ConfigurationScope.Tenant => context.TenantId,
            ConfigurationScope.User => context.UserId,
            ConfigurationScope.Global => null,
            _ => null
        };
    }
}

