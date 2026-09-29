using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Arc4u.Diagnostics;

/// <summary>
/// Helper class to unit test the Arc4u logging.
/// </summary>
/// <typeparam name="T"></typeparam>
public class NullLoggerWrapper<T> : ILoggerWrapper<T>
{
    /// <summary>Gets the shared instance.</summary>
    public static readonly NullLoggerWrapper<T> Instance = new();

    private readonly ILogger<T> _logger = NullLogger<T>.Instance;


    /// <inheritdoc/>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _logger.Log(logLevel, eventId, state, exception, formatter);
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel)
    {
        return _logger.IsEnabled(logLevel);
    }

    /// <inheritdoc/>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return _logger.BeginScope(state);
    }

    /// <inheritdoc/>
    public void CallerMemberName(string caller)
    {
        // No implementation.
    }

    /// <inheritdoc/>
    public Dictionary<string, object?> AdditionalFields { get; } = new();
    /// <inheritdoc/>
    public bool IncludeStackTrace { get; set; }
    /// <inheritdoc/>
    public ILoggerWrapper<T> SetContext(string category, string caller = "", Type? realType = null)
    {
        return this;
    }
}
