
using Microsoft.Extensions.Logging;

namespace Arc4u.Caching.Memory;
/// <summary>Source-generated log messages of the memory cache.</summary>
public static partial class LoggerMessages
{
    /// <summary>Logs (event 9030, Warning) that the memory cache is already initialized.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="store">The name of the cache.</param>
    [LoggerMessage(EventId = 9030, Level = LogLevel.Warning,
                   Message = "Memory Cache {store} is already initialized.")]
    public static partial void LogCacheIsAlreadyInitialized(this ILogger logger, string store);

    /// <summary>Logs (event 9031, Information) that the memory cache is initialized.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="store">The name of the cache.</param>
    [LoggerMessage(EventId = 9031, Level = LogLevel.Information,
                  Message = "Memory caching for state store {store} is initialized.")]
    public static partial void LogCacheIsInitialized(this ILogger logger, string store);

    /// <summary>Logs (event 9032, Warning) the size limit of the memory cache, when it is not positive.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="store">The name of the cache.</param>
    /// <param name="sizeLimitInMB">The configured size limit.</param>
    [LoggerMessage(EventId = 9032, Level = LogLevel.Warning,
               Message = "The size limit for the {Store} cache is {SizeLimitInMB}.")]
    public static partial void LogCacheSizeLimit(this ILogger logger, string store, long sizeLimitInMB);
}
