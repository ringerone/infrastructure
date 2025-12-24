using Microsoft.Extensions.Logging;

namespace Infrastructure.Logging;

/// <summary>
/// Console-based logger implementation using Infrastructure.Logging.
/// </summary>
public class ConsoleOidcLogger : IOidcLogger
{
    private readonly ILogger _logger;

    public ConsoleOidcLogger()
    {
        var consoleLoggerFactory = new ConsoleLoggerFactory();
        _logger = consoleLoggerFactory.CreateLogger("Oidc.Core");
    }

    public void LogError(string message, Exception? exception = null)
    {
        if (exception != null)
        {
            _logger.LogError(exception, message);
        }
        else
        {
            _logger.LogError(message);
        }
    }

    public void LogWarning(string message)
    {
        _logger.LogWarning(message);
    }

    public void LogInformation(string message)
    {
        _logger.LogInformation(message);
    }

    public void LogDebug(string message)
    {
        _logger.LogDebug(message);
    }

    public void LogOidcOperation(string operation, string message, Dictionary<string, object>? attributes = null)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["Category"] = "oidc.operation"
        });

        if (attributes != null && attributes.Count > 0)
        {
            using var attributeScope = _logger.BeginScope(attributes);
            _logger.LogInformation("[OIDC] {Operation} - {Message}", operation, message);
        }
        else
        {
            _logger.LogInformation("[OIDC] {Operation} - {Message}", operation, message);
        }
    }
}

/// <summary>
/// Serilog-based logger implementation using standard ILogger.
/// </summary>
public class SerilogOidcLogger : IOidcLogger
{
    private readonly ILogger _logger;

    public SerilogOidcLogger(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void LogError(string message, Exception? exception = null)
    {
        if (exception != null)
        {
            _logger.LogError(exception, message);
        }
        else
        {
            _logger.LogError(message);
        }
    }

    public void LogWarning(string message)
    {
        _logger.LogWarning(message);
    }

    public void LogInformation(string message)
    {
        _logger.LogInformation(message);
    }

    public void LogDebug(string message)
    {
        _logger.LogDebug(message);
    }

    public void LogOidcOperation(string operation, string message, Dictionary<string, object>? attributes = null)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["Operation"] = operation,
            ["Category"] = "oidc.operation"
        });

        if (attributes != null && attributes.Count > 0)
        {
            using var attributeScope = _logger.BeginScope(attributes);
            _logger.LogInformation("[OIDC] {Operation} - {Message}", operation, message);
        }
        else
        {
            _logger.LogInformation("[OIDC] {Operation} - {Message}", operation, message);
        }
    }
}

