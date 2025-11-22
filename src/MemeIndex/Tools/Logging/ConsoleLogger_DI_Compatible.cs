using MS_LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace MemeIndex.Tools.Logging;

public class ConsoleLogger_DI_Compatible (string category) : ILogger
{
    public IDisposable? BeginScope<TState>
        (TState state) where TState : notnull => null;

    public bool IsEnabled
        (MS_LogLevel logLevel) => true;

    public void Log<TState>
    (
        MS_LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    )
    {
        var message = formatter(state, exception);
        ConsoleLogger.Log($"{category} [{eventId}]", message, ConvertLogLevel(logLevel));
    }

    private static LogLevel ConvertLogLevel(MS_LogLevel logLevel) => logLevel switch
    {
        MS_LogLevel.Trace       => LogLevel.Debug,
        MS_LogLevel.Debug       => LogLevel.Debug,
        MS_LogLevel.Information => LogLevel.Info,
        MS_LogLevel.Warning     => LogLevel.Warn,
        MS_LogLevel.Error       => LogLevel.Error,
        MS_LogLevel.Critical    => LogLevel.Error,
        _                       => LogLevel.Info,
    };
}

public class ConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger
        (string categoryName) => new ConsoleLogger_DI_Compatible(categoryName);

    public void Dispose() { }
}