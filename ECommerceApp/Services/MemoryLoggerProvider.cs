using ECommerceApp.Models.ViewModels;

namespace ECommerceApp.Services;

public class MemoryLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new MemoryLogger(categoryName);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}

public class MemoryLogger : ILogger
{
    private readonly string _categoryName;

    public MemoryLogger(string categoryName)
    {
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        // Sadece Warning ve üzeri seviyedeki logları yakala (Information hariç)
        return logLevel >= LogLevel.Warning;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);

        var logEntry = new LogEntryVM
        {
            Timestamp = DateTime.Now,
            Level = logLevel.ToString(),
            Message = message,
            Exception = exception?.ToString(),
            Source = _categoryName,
            Category = _categoryName
        };

        LogService.AddLogEntry(logEntry);
    }
}

