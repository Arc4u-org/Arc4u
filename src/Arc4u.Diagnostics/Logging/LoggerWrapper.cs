using System.Reflection;
using Arc4u.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.Diagnostics;

/// <summary>
/// An <see cref="ILogger{TCategoryName}"/> that lets callers attach extra properties (see <see cref="LoggerWrapperExtensions"/>) and a message category
/// to the log entry before it is written.
/// </summary>
/// <typeparam name="T">The category type of the logger.</typeparam>
public interface ILoggerWrapper<T> : ILogger<T>, ILoggerCallerMember
{
    /// <summary>Gets the additional properties that are written with the log entries of this logger.</summary>
    /// <remarks>Known issue: properties are not cleared after an entry is written; they persist on later entries written by the same logger instance, whose lifetime follows its consumer.</remarks>
    public Dictionary<string, object?> AdditionalFields { get; }

    /// <summary>Sets a value indicating whether the stack trace is added to the log entry.</summary>
    public bool IncludeStackTrace { set; }

    /// <summary>Sets the category, the calling member and optionally the source context type used by the log entries of this logger.</summary>
    /// <remarks>The values stay set until <see cref="SetContext"/> is called again on the same instance (the source context type is only replaced when <paramref name="realType"/> is given).</remarks>
    /// <param name="category">The message category, see <see cref="MessageCategory"/>.</param>
    /// <param name="caller">The name of the calling member.</param>
    /// <param name="realType">The type to report as the source context instead of <typeparamref name="T"/>, or <see langword="null"/> to keep <typeparamref name="T"/>.</param>
    /// <returns>This logger wrapper.</returns>
    ILoggerWrapper<T> SetContext(string category, string caller = "", Type? realType = null);

}

/// <summary>An <see cref="ILoggerWrapper{T}"/> registered with a scoped lifetime, so the additional properties are shared during the lifetime of the scope (for example an HTTP request).</summary>
/// <typeparam name="T">The category type of the logger.</typeparam>
public interface IScopedLogger<T> : ILoggerWrapper<T>
{

}

/// <summary>Transient implementation of <see cref="ILoggerWrapper{T}"/> that decorates the <see cref="ILogger{TCategoryName}"/> created by the <see cref="ILoggerFactory"/>.</summary>
/// <typeparam name="T">The category type of the logger.</typeparam>
public sealed class LoggerWrapper<T> : LoggerBaseWrapper<T>
{
    /// <summary>Initializes a new instance of the <see cref="LoggerWrapper{T}"/> class.</summary>
    /// <param name="loggerFactory">The factory used to create the underlying logger.</param>
    /// <param name="addPropertiesToLog">The provider (keyed <c>"Transient"</c>) of the additional properties.</param>
    /// <param name="appConfig">The application configuration, used to obtain the application name.</param>
    public LoggerWrapper(ILoggerFactory loggerFactory, [FromKeyedServices("Transient")] IAddPropertiesToLog addPropertiesToLog, IOptionsMonitor<ApplicationConfig> appConfig) : base(loggerFactory, addPropertiesToLog, appConfig)   
    {
        
    }
}

/// <summary>Scoped implementation of <see cref="IScopedLogger{T}"/> that decorates the <see cref="ILogger{TCategoryName}"/> created by the <see cref="ILoggerFactory"/>.</summary>
/// <typeparam name="T">The category type of the logger.</typeparam>
public sealed class ScopedLoggerWrapper<T> : LoggerBaseWrapper<T>
{
    /// <summary>Initializes a new instance of the <see cref="ScopedLoggerWrapper{T}"/> class.</summary>
    /// <param name="loggerFactory">The factory used to create the underlying logger.</param>
    /// <param name="addPropertiesToLog">The provider (keyed <c>"Scoped"</c>) of the additional properties.</param>
    /// <param name="appConfig">The application configuration, used to obtain the application name.</param>
    public ScopedLoggerWrapper(ILoggerFactory loggerFactory, [FromKeyedServices("Scoped")] IAddPropertiesToLog addPropertiesToLog, IOptionsMonitor<ApplicationConfig> appConfig) : base(loggerFactory, addPropertiesToLog, appConfig)
    {

    }
}

/// <summary>
/// Base implementation of <see cref="IScopedLogger{T}"/>. It enriches every log entry with the Arc4u properties (see <see cref="LoggingConstants"/>)
/// and the properties supplied by <see cref="IAddPropertiesToLog"/> before forwarding it to the underlying logger.
/// </summary>
/// <typeparam name="T">The category type of the logger.</typeparam>
public abstract class LoggerBaseWrapper<T> : IScopedLogger<T>
{
    internal readonly ILogger _logger;
    private string _category;
    private readonly Dictionary<string, object?> _additionalFields = [];
    private Type _contextType;
    private string _caller = string.Empty;
    private bool _disposed;
    private readonly IAddPropertiesToLog? _addPropertiesToLog;
    private readonly ApplicationConfig _applicationConfig;

    /// <inheritdoc/>
    public Dictionary<string, object?> AdditionalFields => _additionalFields;
    /// <inheritdoc/>
    public bool IncludeStackTrace { private get; set; }

    internal static int ProcessId
    {
        get
        {
            try
            {
                return System.Environment.ProcessId;
            }
            catch (PlatformNotSupportedException)
            {
                return -1;
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LoggerBaseWrapper{T}"/> class; created by the <see cref="IServiceProvider"/>.
    /// When the application configuration has no logging name, the name of the entry assembly is used.
    /// </summary>
    /// <param name="loggerFactory">The factory used to create the underlying logger.</param>
    /// <param name="addPropertiesToLog">The provider of the additional properties added to every log entry.</param>
    /// <param name="appConfig">The application configuration, used to obtain the application name.</param>
    public LoggerBaseWrapper(ILoggerFactory loggerFactory, IAddPropertiesToLog addPropertiesToLog, IOptionsMonitor<ApplicationConfig> appConfig)
    {
        _logger = loggerFactory.CreateLogger<T>();
        _category = nameof(MessageCategory.Technical);
        _contextType = typeof(T);
        _addPropertiesToLog = addPropertiesToLog;
        _applicationConfig = appConfig.CurrentValue ?? new();

        if (string.IsNullOrWhiteSpace(_applicationConfig.Environment.LoggingName))
        {
            _applicationConfig.Environment.LoggingName = ApplicationName;
        }
    }

    /// <inheritdoc/>
    public  ILoggerWrapper<T> SetContext(string category, string caller = "", Type? realType = null)
    {
        _caller = caller;
        _category = category;
        if (realType != null)
        {
            _contextType = realType;
        }
        return this;
    }

    private static string ApplicationName => Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown App";

    private void Log(LogLevel level, string? message, Exception? exception = null)
    {
        ThrowIfDisposed();

        if (!IsEnabled(level))
        {
            return;
        }

        try
        {
            var properties = AddAdditionalProperties();

            if (IncludeStackTrace)
            {
                properties.AddIfNotExist(LoggingConstants.Stacktrace, exception?.StackTrace ?? System.Environment.StackTrace);
            }

            properties.AddIfNotExist(LoggingConstants.MethodName, _caller);
            properties.AddIfNotExist(LoggingConstants.Class, _contextType?.FullName ?? nameof(_contextType));
            properties.AddIfNotExist(LoggingConstants.Category, _category);
            properties.AddIfNotExist(LoggingConstants.Application, _applicationConfig.Environment.LoggingName);
            properties.AddIfNotExist(LoggingConstants.ThreadId, System.Environment.CurrentManagedThreadId);
            properties.AddIfNotExist(LoggingConstants.ProcessId, ProcessId);

            _logger.Log(level, 0, properties, exception, (state, ex) => message ?? "");

            if (exception is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                {
                    _logger.Log(level, 0, properties, innerException, (state, ex) => message ?? "");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log message with property providers and eventId");
        }
    }

    internal void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>Marks the wrapper as disposed; any later attempt to log throws an <see cref="ObjectDisposedException"/>.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    private Dictionary<string, object?> AddAdditionalProperties()
    {
        var properties = new Dictionary<string, object?>(AdditionalFields);
        try
        { 
            var definedProperties = _addPropertiesToLog?.GetProperties();
            if (definedProperties != null)
            {
                foreach (var property in definedProperties)
                {
                    if (property.Value != null)
                    {
                        properties.AddIfNotExist(property.Key, property.Value);
                    }
                }
            }
            return properties;
        }
        catch (Exception ex)
        {
            Log(LogLevel.Error, "Error getting property providers. Logging without additional properties.", ex);
            return new Dictionary<string, object?>(AdditionalFields);
        }
    }

    /// <summary>
    /// Writes a log entry. When <paramref name="state"/> is a sequence of key/value pairs, the pairs are added to <see cref="AdditionalFields"/> first.
    /// The entry is enriched with the Arc4u properties and forwarded to the underlying logger; for an <see cref="AggregateException"/> the flattened inner exceptions are logged as well.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    /// <param name="logLevel">The severity of the entry.</param>
    /// <param name="eventId">The event id (not used by the wrapper).</param>
    /// <param name="state">The state to log.</param>
    /// <param name="exception">The exception related to the entry, if any.</param>
    /// <param name="formatter">Creates the message from the state and the exception.</param>
    /// <exception cref="ObjectDisposedException">The wrapper has been disposed.</exception>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (state is IEnumerable<KeyValuePair<string, object>> pairs)
        {
            foreach (var pair in pairs)
            {
                AdditionalFields.AddOrReplace(pair.Key, pair.Value);
            }
        }

        Log(logLevel, formatter(state, exception), exception);
    }

    /// <inheritdoc/>
    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    /// <summary>
    /// Create a disposable scope that can be used to push properties into the scope.
    /// This is not really intended to be used by the end user.
    /// As LoggerWrapper is a wrapper around ILogger, this method must be implemented.
    /// </summary>
    /// <typeparam name="TState">The type of the scope state.</typeparam>
    /// <param name="state">The scope state.</param>
    /// <returns>A disposable that ends the scope, or <see langword="null"/>.</returns>
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);

    /// <summary>
    /// Sets the name of the calling member written in the <see cref="LoggingConstants.MethodName"/> property.
    /// </summary>
    /// <param name="caller">The name of the calling member.</param>
    public void CallerMemberName(string caller)
    {
        _caller = caller;
    }
}

