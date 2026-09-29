
using Microsoft.Extensions.Logging;

namespace Arc4u.Caching.Sql;
/// <summary>Source-generated log messages of the SQL Server cache.</summary>
public static partial class LoggerMessages
{
    /// <summary>Logs (event 9050, Warning) that the SQL Server cache is already initialized.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="store">The name of the cache.</param>
    [LoggerMessage(EventId = 9050, Level = LogLevel.Warning,
                   Message = "Sql Cache {store} is already initialized.")]
    public static partial void LogCacheIsAlreadyInitialized(this ILogger logger, string store);

    /// <summary>Logs (event 9051, Information) that the SQL Server cache is initialized.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="store">The name of the cache.</param>
    [LoggerMessage(EventId = 9051, Level = LogLevel.Information,
                  Message = "Sql caching for dapr state store {store} is initialized.")]
    public static partial void LogCacheIsInitialized(this ILogger logger, string store);
}
