
using Microsoft.Extensions.Logging;

namespace Arc4u.Caching.Dapr;
/// <summary>Source-generated log messages of the Dapr cache.</summary>
public static partial class LoggerMessages
{
    /// <summary>Logs (event 9020, Warning) that the Dapr cache is already initialized.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="store">The name of the cache.</param>
    [LoggerMessage(EventId = 9020, Level = LogLevel.Warning,
                   Message = "Dapr caching for dapr state store {store} is already initialized.")]
    public static partial void LogCacheIsAlreadyInitialized(this ILogger logger, string store);

    /// <summary>Logs (event 9021, Information) that the Dapr cache is initialized.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="store">The name of the cache.</param>
    [LoggerMessage(EventId = 9021, Level = LogLevel.Information,
                  Message = "Dapr caching for dapr state store {store} is initialized.")]
    public static partial void LogCacheIsInitialized(this ILogger logger, string store);
}
