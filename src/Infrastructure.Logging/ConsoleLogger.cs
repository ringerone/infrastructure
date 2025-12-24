using Microsoft.Extensions.Logging;

namespace Infrastructure.Logging;

/// <summary>
/// Console-based logger implementation.
/// </summary>
public class ConsoleLogger : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var level = logLevel switch
        {
            LogLevel.Error => "ERROR",
            LogLevel.Warning => "WARN",
            LogLevel.Information => "INFO",
            LogLevel.Debug => "DEBUG",
            LogLevel.Trace => "TRACE",
            _ => "INFO"
        };

        Console.WriteLine($"[{level}] {timestamp} - {message}");
        
        if (exception != null)
        {
            Console.WriteLine($"Exception: {exception}");
        }
    }
}

/// <summary>
/// Console logger factory for creating console loggers.
/// </summary>
public class ConsoleLoggerFactory : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider)
    {
        // No-op for console logger
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleLogger();
    }

    public void Dispose()
    {
        // No-op
    }
}


