using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Infrastructure.Telemetry;

/// <summary>
/// Factory interface for creating Meter instances
/// Follows Interface Segregation Principle - single responsibility
/// </summary>
public interface IMeterFactory
{
    /// <summary>
    /// Gets or creates a Meter with the specified name
    /// </summary>
    Meter GetMeter(string name, string? version = null);
}

/// <summary>
/// Factory implementation for creating Meter instances
/// </summary>
public class MeterFactory : IMeterFactory
{
    private readonly ConcurrentDictionary<string, Meter> _meters = new();

    public Meter GetMeter(string name, string? version = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Meter name cannot be null or empty", nameof(name));

        var key = version != null ? $"{name}:{version}" : name;
        
        return _meters.GetOrAdd(key, _ => new Meter(name, version ?? "1.0.0"));
    }
}

