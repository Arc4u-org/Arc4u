using System.Diagnostics;
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
    /// <summary>Gets the additional properties that are written with the next log entry of this logger.</summary>
    /// <remarks>The properties are cleared once the entry is written.</remarks>
    public Dictionary<string, object?> AdditionalFields { get; }

    /// <summary>Sets a value indicating whether the stack trace is added to the next log entry; reset once the entry is written.</summary>
    public bool IncludeStackTrace { set; }

    /// <summary>Returns a logger that writes its entries with the category, the calling member and optionally the source context type given.</summary>
    /// <remarks>The returned logger has its own additional fields, so a logger shared between callers (or threads) is never modified by an entry being prepared.</remarks>
    /// <param name="category">The message category, see <see cref="MessageCategory"/>.</param>
    /// <param name="caller">The name of the calling member.</param>
    /// <param name="realType">The type to report as the source context instead of <typeparamref name="T"/>, or <see langword="null"/> to keep <typeparamref name="T"/>.</param>
    /// <returns>The logger wrapper to use for the entry.</returns>
    ILoggerWrapper<T> SetContext(string category, string caller = "", Type? realType = null);

}

/// <summary>
/// Transient implementation of <see cref="ILoggerWrapper{T}"/> that decorates the <see cref="ILogger{TCategoryName}"/> created by the <see cref="ILoggerFactory"/>.
/// The <see cref="IAddPropertiesToLog"/> it uses depends on where it is resolved: in a scope (an HTTP or gRPC request, a <c>CreateScope()</c>), the provider
/// registered under the key <c>"Scoped"</c>, which may depend on scoped services; from the root (a singleton and its dependencies), the provider registered
/// under the key <c>"Transient"</c>, which must not.
/// </summary>
/// <typeparam name="T">The category type of the logger.</typeparam>
public sealed class LoggerWrapper<T> : LoggerBaseWrapper<T>
{
    /// <summary>Initializes a new instance of the <see cref="LoggerWrapper{T}"/> class.</summary>
    /// <param name="loggerFactory">The factory used to create the underlying logger.</param>
    /// <param name="serviceProvider">The provider the logger is resolved from, used to choose the <c>"Scoped"</c> or <c>"Transient"</c> <see cref="IAddPropertiesToLog"/>.</param>
    /// <param name="appConfig">The application configuration, used to obtain the application name.</param>
    public LoggerWrapper(ILoggerFactory loggerFactory, IServiceProvider serviceProvider, IOptionsMonitor<ApplicationConfig> appConfig)
        : base(loggerFactory, GetPropertiesToLog(serviceProvider), appConfig)
    {
    }

    private static IAddPropertiesToLog GetPropertiesToLog(IServiceProvider serviceProvider)
    {
        // Without AddILogger (the wrapper registered by hand), the scope cannot be detected: use the "Transient" provider, as from the root.
        var inScope = serviceProvider.GetService<RootServiceProvider>()?.IsScope(serviceProvider) == true;

        if (inScope && serviceProvider.GetKeyedService<IAddPropertiesToLog>("Scoped") is { } scopedProperties)
        {
            return scopedProperties;
        }

        return serviceProvider.GetRequiredKeyedService<IAddPropertiesToLog>("Transient");
    }
}

/// <summary>
/// Base implementation of <see cref="ILoggerWrapper{T}"/>. It enriches every log entry with the Arc4u properties (see <see cref="LoggingConstants"/>)
/// and the properties supplied by <see cref="IAddPropertiesToLog"/> before forwarding it to the underlying logger.
/// </summary>
/// <remarks>
/// <see cref="SetContext"/> (used by <c>Technical()</c>, <c>Business()</c>, <c>Monitoring()</c>, ...) returns a new logger dedicated to one entry: the properties
/// added to it are written with its next entry only, and they are never shared with other callers of this instance.
/// </remarks>
/// <typeparam name="T">The category type of the logger.</typeparam>
public abstract class LoggerBaseWrapper<T> : ILoggerWrapper<T>
{
    private readonly ILogger _logger;
    private readonly Dictionary<string, object?> _additionalFields = [];
    private string _caller = string.Empty;
    private bool _disposed;
    private readonly IAddPropertiesToLog? _addPropertiesToLog;
    private readonly ApplicationConfig _applicationConfig;

    /// <inheritdoc/>
    public Dictionary<string, object?> AdditionalFields => _additionalFields;
    /// <inheritdoc/>
    public bool IncludeStackTrace { private get; set; }

    private static int ProcessId
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
        _addPropertiesToLog = addPropertiesToLog;
        _applicationConfig = appConfig.CurrentValue ?? new();

        if (string.IsNullOrWhiteSpace(_applicationConfig.Environment.LoggingName))
        {
            _applicationConfig.Environment.LoggingName = ApplicationName;
        }
    }

    /// <inheritdoc/>
    /// <remarks>This instance is not modified: the returned logger carries the context and its own additional fields.</remarks>
    public ILoggerWrapper<T> SetContext(string category, string caller = "", Type? realType = null) =>
        new LogEntry(this, category, caller, realType ?? typeof(T));

    private static string ApplicationName => Assembly.GetEntryAssembly()?.GetName().Name ?? "Unknown App";

    private void Write<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter,
                               string category, string caller, Type contextType, Dictionary<string, object?> fields, bool includeStackTrace)
    {
        ThrowIfDisposed();

        if (!IsEnabled(level))
        {
            return;
        }

        try
        {
            // The properties of the entry: a copy, so the fields of the caller are never modified by the entry itself.
            var properties = new Dictionary<string, object?>(fields);

            if (state is IEnumerable<KeyValuePair<string, object>> pairs)
            {
                foreach (var pair in pairs)
                {
                    properties.AddOrReplace(pair.Key, pair.Value);
                }
            }

            AddPropertiesToLog(properties);

            if (includeStackTrace)
            {
                properties.AddIfNotExist(LoggingConstants.Stacktrace, exception?.StackTrace ?? System.Environment.StackTrace);
            }

            properties.AddIfNotExist(LoggingConstants.MethodName, caller);
            properties.AddIfNotExist(LoggingConstants.Class, contextType.FullName ?? contextType.Name);
            properties.AddIfNotExist(LoggingConstants.Category, category);
            properties.AddIfNotExist(LoggingConstants.Application, _applicationConfig.Environment.LoggingName);
            properties.AddIfNotExist(LoggingConstants.ThreadId, System.Environment.CurrentManagedThreadId);
            properties.AddIfNotExist(LoggingConstants.ProcessId, ProcessId);
            if (Activity.Current is { } activity)
            {
                properties.AddIfNotExist(LoggingConstants.ActivityId, activity.TraceId.ToString());
            }

            var message = formatter(state, exception);

            _logger.Log(level, eventId, properties, exception, (_, _) => message ?? "");

            if (exception is AggregateException aggregateException)
            {
                foreach (var innerException in aggregateException.Flatten().InnerExceptions)
                {
                    _logger.Log(level, eventId, properties, innerException, (_, _) => message ?? "");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log message with property providers and eventId");
        }
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>Marks the wrapper as disposed; any later attempt to log throws an <see cref="ObjectDisposedException"/>.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
    }

    private void AddPropertiesToLog(Dictionary<string, object?> properties)
    {
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting property providers. Logging without additional properties.");
        }
    }

    /// <summary>
    /// Writes a log entry in the <see cref="MessageCategory.Technical"/> category. When <paramref name="state"/> is a sequence of key/value pairs, the pairs are written as properties of this entry.
    /// The entry is enriched with the Arc4u properties and forwarded to the underlying logger; for an <see cref="AggregateException"/> the flattened inner exceptions are logged as well.
    /// The <see cref="AdditionalFields"/>, the stack trace request and the calling member are reset once the entry is written.
    /// </summary>
    /// <typeparam name="TState">The type of the state.</typeparam>
    /// <param name="logLevel">The severity of the entry.</param>
    /// <param name="eventId">The event id, forwarded to the underlying logger.</param>
    /// <param name="state">The state to log.</param>
    /// <param name="exception">The exception related to the entry, if any.</param>
    /// <param name="formatter">Creates the message from the state and the exception.</param>
    /// <exception cref="ObjectDisposedException">The wrapper has been disposed.</exception>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        try
        {
            Write(logLevel, eventId, state, exception, formatter, nameof(MessageCategory.Technical), _caller, typeof(T), _additionalFields, IncludeStackTrace);
        }
        finally
        {
            _additionalFields.Clear();
            IncludeStackTrace = false;
            _caller = string.Empty;
        }
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
    /// Sets the name of the calling member written in the <see cref="LoggingConstants.MethodName"/> property of the next entry.
    /// </summary>
    /// <param name="caller">The name of the calling member.</param>
    public void CallerMemberName(string caller)
    {
        _caller = caller;
    }

    /// <summary>
    /// The logger returned by <see cref="SetContext"/>: it holds the category, the calling member, the source context type and the additional fields
    /// of one entry, and writes through its parent. The additional fields and the stack trace request are reset once an entry is written.
    /// </summary>
    private sealed class LogEntry(LoggerBaseWrapper<T> parent, string category, string caller, Type contextType) : ILoggerWrapper<T>
    {
        private string _caller = caller;

        public Dictionary<string, object?> AdditionalFields { get; } = [];

        public bool IncludeStackTrace { private get; set; }

        public ILoggerWrapper<T> SetContext(string category, string caller = "", Type? realType = null) =>
            new LogEntry(parent, category, caller, realType ?? contextType);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            try
            {
                parent.Write(logLevel, eventId, state, exception, formatter, category, _caller, contextType, AdditionalFields, IncludeStackTrace);
            }
            finally
            {
                AdditionalFields.Clear();
                IncludeStackTrace = false;
            }
        }

        public bool IsEnabled(LogLevel logLevel) => parent.IsEnabled(logLevel);

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => parent.BeginScope(state);

        public void CallerMemberName(string caller) => _caller = caller;
    }
}
