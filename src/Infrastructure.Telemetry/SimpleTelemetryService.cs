using System.Diagnostics;

namespace Infrastructure.Telemetry;

/// <summary>
/// Simplified OpenTelemetry service for OIDC operations.
/// </summary>
public class SimpleTelemetryService : ITelemetryService
{
    private static readonly ActivitySource ActivitySource = new("Oidc.Server", "1.0.0");

    public Activity? StartActivity(string name, string operation = "oidc.operation")
    {
        var activity = ActivitySource.StartActivity(name);
        if (activity != null)
        {
            activity.SetTag("operation", operation);
            activity.SetTag("service.name", "oidc-server");
            activity.SetTag("service.version", "1.0.0");
        }
        return activity;
    }

    public void AddTag(string key, object? value)
    {
        Activity.Current?.SetTag(key, value);
    }

    public void AddEvent(string name, params (string key, object? value)[] attributes)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            var tags = new ActivityTagsCollection();
            foreach (var attr in attributes)
            {
                tags.Add(attr.key, attr.value);
            }
            activity.AddEvent(new ActivityEvent(name, tags: tags));
        }
    }

    public void RecordException(Exception exception)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
            // Add exception details as tags
            activity.SetTag("exception.type", exception.GetType().Name);
            activity.SetTag("exception.message", exception.Message);
            activity.SetTag("exception.stacktrace", exception.StackTrace);
        }
    }

    public void SetStatus(ActivityStatusCode status, string? description = null)
    {
        Activity.Current?.SetStatus(status, description);
    }

    public void RecordMetric(string name, double value, params (string key, object? value)[] tags)
    {
        var tagString = string.Join(", ", tags.Select(t => $"{t.key}={t.value}"));
        Console.WriteLine($"[METRIC] {name} = {value}, Tags: {tagString}");
    }

    public void IncrementCounter(string name, params (string key, object? value)[] tags)
    {
        var tagString = string.Join(", ", tags.Select(t => $"{t.key}={t.value}"));
        Console.WriteLine($"[COUNTER] {name}++, Tags: {tagString}");
    }

    public static void RecordDuration(string operation, double durationSeconds, params (string key, object? value)[] tags)
    {
        var tagString = string.Join(", ", tags.Select(t => $"{t.key}={t.value}"));
        Console.WriteLine($"[DURATION] {operation} = {durationSeconds:F3}s, Tags: {tagString}");
    }
}

