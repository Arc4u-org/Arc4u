
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
}
