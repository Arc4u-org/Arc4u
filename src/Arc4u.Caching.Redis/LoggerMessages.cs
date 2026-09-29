
using Microsoft.Extensions.Logging;

namespace Arc4u.Caching.Redis;
/// <summary>Source-generated log messages of the Redis caches.</summary>
public static partial class LoggerMessages
{
    /// <summary>Logs (event 9040, Warning) that the Redis cache is already initialized.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="store">The name of the cache.</param>
    [LoggerMessage(EventId = 9040, Level = LogLevel.Warning,
                   Message = "Redis Cache {store} is already initialized.")]
    public static partial void LogCacheIsAlreadyInitialized(this ILogger logger, string store);

    /// <summary>Logs (event 9041, Information) that the Redis cache is initialized.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="store">The name of the cache.</param>
    [LoggerMessage(EventId = 9041, Level = LogLevel.Information,
                  Message = "Redis caching {store} is initialized.")]
    public static partial void LogCacheIsInitialized(this ILogger logger, string store);
}
