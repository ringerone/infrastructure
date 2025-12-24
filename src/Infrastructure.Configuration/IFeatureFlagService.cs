namespace Infrastructure.Configuration;

/// <summary>
/// Service for checking feature flags with hierarchical resolution
/// Supports percentage-based rollouts, A/B testing, and tenant-specific overrides
/// Follows Interface Segregation Principle - focused interface
/// </summary>
public interface IFeatureFlagService
{
    /// <summary>
    /// Checks if a feature flag is enabled for the current context
    /// </summary>
    bool IsEnabled(string featureName);

    /// <summary>
    /// Checks if a feature flag is enabled with explicit context
    /// </summary>
    bool IsEnabled(string featureName, FeatureFlagContext context);

    /// <summary>
    /// Gets the variant value for a feature flag (for A/B testing)
    /// </summary>
    string? GetVariant(string featureName, string? defaultValue = null);

    /// <summary>
    /// Gets all feature flags for the current context
    /// </summary>
    Dictionary<string, bool> GetAllFlags();

    /// <summary>
    /// Sets a feature flag
    /// </summary>
    Task SetFeatureFlagAsync(FeatureFlag flag, CancellationToken cancellationToken = default);
}

/// <summary>
/// Context for feature flag evaluation
/// </summary>
public class FeatureFlagContext
{
    public string? TenantId { get; set; }
    public string? UserId { get; set; }
    public string? Region { get; set; }
    public string? Environment { get; set; }
    public Dictionary<string, string> Attributes { get; set; } = new();
}

/// <summary>
/// Feature flag definition
/// </summary>
public class FeatureFlag
{
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public ConfigurationScope Scope { get; set; }
    public string? ScopeIdentifier { get; set; }
    
    // Percentage rollout (0-100)
    public int RolloutPercentage { get; set; } = 100;
    
    // Variant for A/B testing
    public string? Variant { get; set; }
    
    // Targeting rules
    public List<FeatureFlagRule> Rules { get; set; } = new();
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// Rule for feature flag targeting
/// </summary>
public class FeatureFlagRule
{
    public string Attribute { get; set; } = string.Empty;
    public string Operator { get; set; } = "equals"; // equals, contains, startsWith, etc.
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Repository interface for feature flag storage
/// Follows Dependency Inversion Principle - depends on abstraction
/// </summary>
public interface IFeatureFlagRepository
{
    Task<FeatureFlag?> GetFeatureFlagAsync(string name, ConfigurationScope scope, string? scopeIdentifier, CancellationToken cancellationToken = default);
    Task<IEnumerable<FeatureFlag>> GetAllFeatureFlagsAsync(Dictionary<ConfigurationScope, string?> scopeIdentifiers, CancellationToken cancellationToken = default);
    Task<(IEnumerable<FeatureFlag> Items, long TotalCount)> GetAllFeatureFlagsPagedAsync(
        Dictionary<ConfigurationScope, string?> scopeIdentifiers,
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        ConfigurationScope? scopeFilter = null,
        string? scopeIdentifierFilter = null,
        CancellationToken cancellationToken = default);
    Task SetFeatureFlagAsync(FeatureFlag flag, CancellationToken cancellationToken = default);
    Task DeleteFeatureFlagAsync(string name, ConfigurationScope scope, string? scopeIdentifier, CancellationToken cancellationToken = default);
}

