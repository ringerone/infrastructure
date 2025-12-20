using System.Collections.Concurrent;
using System.Diagnostics;

namespace Infrastructure.Telemetry;

/// <summary>
/// Factory interface for creating ActivitySource instances
/// Follows Interface Segregation Principle - single responsibility
/// </summary>
public interface IActivitySourceFactory
{
    /// <summary>
    /// Gets or creates an ActivitySource with the specified name
    /// </summary>
    ActivitySource GetActivitySource(string name, string? version = null);
}

/// <summary>
/// Factory implementation for creating ActivitySource instances
/// </summary>
public class ActivitySourceFactory : IActivitySourceFactory
{
    private readonly ConcurrentDictionary<string, ActivitySource> _activitySources = new();

    public ActivitySource GetActivitySource(string name, string? version = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("ActivitySource name cannot be null or empty", nameof(name));

        var key = version != null ? $"{name}:{version}" : name;
        
        return _activitySources.GetOrAdd(key, _ => new ActivitySource(name, version ?? "1.0.0"));
    }
}

