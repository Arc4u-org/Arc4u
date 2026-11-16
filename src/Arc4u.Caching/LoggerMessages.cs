
using Microsoft.Extensions.Logging;

namespace Arc4u.Caching;
/// <summary>Source-generated log messages of the cache context.</summary>
public static partial class LoggerMessages
{
    /// <summary>Logs (event 9010, Trace) that a cache of the given kind and name has been instantiated.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cacheKind">The kind of the cache.</param>
    /// <param name="cacheName">The name of the cache.</param>
    [LoggerMessage(EventId = 9010, Level = LogLevel.Trace,
                   Message = "Instantiate a new instance of a cache with a kind of {cacheKind} and named {cacheName}.")]
    public static partial void LogNewCache(this ILogger logger, string cacheKind, string cacheName);

    /// <summary>Logs (event 9011, Information) that a cache of the given kind and name has been registered to be initialized later.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cacheKind">The kind of the cache.</param>
    /// <param name="cacheName">The name of the cache.</param>
    [LoggerMessage(EventId = 9011, Level = LogLevel.Information,
                  Message = "Register a cache with a kind of {CacheKind} and named {CacheName} for later.")]
    public static partial void LogRegisterNewCache(this ILogger logger, string cacheKind, string cacheName);

    /// <summary>Logs (event 9012, Error) that no <see cref="ICache"/> can be resolved for the given kind.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cacheKind">The kind of the cache.</param>
    [LoggerMessage(EventId = 9012, Level = LogLevel.Error,
                  Message = "Cannot resolve an ICache instance with the name: {CacheKind}")]
    public static partial void LogCacheKindIssue(this ILogger logger, string cacheKind);

    /// <summary>Logs (event 9013, Warning) that a distributed cache is already initialized.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cacheKind">The kind of the cache.</param>
    /// <param name="store">The name of the cache.</param>
    [LoggerMessage(EventId = 9013, Level = LogLevel.Warning,
                   Message = "{CacheKind} Cache {Store} is already initialized.")]
    public static partial void LogCacheIsAlreadyInitialized(this ILogger logger, string cacheKind, string store);

    /// <summary>Logs (event 9014, Information) that a distributed cache is initialized.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cacheKind">The kind of the cache.</param>
    /// <param name="store">The name of the cache.</param>
    [LoggerMessage(EventId = 9014, Level = LogLevel.Information,
                   Message = "{CacheKind} caching for store {Store} is initialized.")]
    public static partial void LogCacheIsInitialized(this ILogger logger, string cacheKind, string store);

    /// <summary>Logs (event 9015, Error) that no <see cref="Arc4u.Serializer.IObjectSerialization"/> can be resolved for a distributed cache.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="cacheKind">The kind of the cache.</param>
    /// <param name="store">The name of the cache.</param>
    [LoggerMessage(EventId = 9015, Level = LogLevel.Error,
                   Message = "{CacheKind} Cache {Store} is not initialized. An IObjectSerialization instance cannot be resolved via the Ioc.")]
    public static partial void LogSerializerNotResolved(this ILogger logger, string cacheKind, string store);
}
