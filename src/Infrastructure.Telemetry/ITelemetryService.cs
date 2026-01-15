using System.Diagnostics;

namespace Infrastructure.Telemetry;

/// <summary>
/// Service for OpenTelemetry tracing and metrics in OIDC operations.
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Creates a new activity for tracing OIDC operations.
    /// </summary>
    Activity? StartActivity(string name, string operation = "oidc.operation");

    /// <summary>
    /// Adds a tag to the current activity.
    /// </summary>
    void AddTag(string key, object? value);

    /// <summary>
    /// Adds an event to the current activity.
    /// </summary>
    void AddEvent(string name, params (string key, object? value)[] attributes);

    /// <summary>
    /// Records an exception in the current activity.
    /// </summary>
    void RecordException(Exception exception);

    /// <summary>
    /// Sets the status of the current activity.
    /// </summary>
    void SetStatus(ActivityStatusCode status, string? description = null);

    /// <summary>
    /// Records a metric for OIDC operations.
    /// </summary>
    void RecordMetric(string name, double value, params (string key, object? value)[] tags);

    /// <summary>
    /// Increments a counter metric.
    /// </summary>
    void IncrementCounter(string name, params (string key, object? value)[] tags);
}

