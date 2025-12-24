namespace Infrastructure.Logging;

/// <summary>
/// Logger interface for OIDC operations.
/// </summary>
public interface IOidcLogger
{
    void LogError(string message, Exception? exception = null);
    void LogWarning(string message);
    void LogInformation(string message);
    void LogDebug(string message);
    void LogOidcOperation(string operation, string message, Dictionary<string, object>? attributes = null);
}

