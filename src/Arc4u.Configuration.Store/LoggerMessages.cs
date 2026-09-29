
using Microsoft.Extensions.Logging;

namespace Arc4u.Configuration.Store;

/// <summary>
/// Source-generated log messages used by the section store monitoring service.
/// </summary>
public static partial class LoggerMessages
{
    /// <summary>
    /// Logs that a service is starting.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceName">The name of the service.</param>
    [LoggerMessage(EventId = 9060, Level = LogLevel.Information,
                   Message = "{ServiceName} service starting.")]
    public static partial void LogServiceStarting(this ILogger logger, string serviceName);

    /// <summary>
    /// Logs that a service has started.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceName">The name of the service.</param>
    [LoggerMessage(EventId = 9061, Level = LogLevel.Information,
                   Message = "{ServiceName} service started.")]
    public static partial void LogServiceStarted(this ILogger logger, string serviceName);

    /// <summary>
    /// Logs that a service is stopping.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceName">The name of the service.</param>
    [LoggerMessage(EventId = 9062, Level = LogLevel.Information,
               Message = "{ServiceName} service stopping.")]
    public static partial void LogServiceStopping(this ILogger logger, string serviceName);

    /// <summary>
    /// Logs that a service has stopped.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="serviceName">The name of the service.</param>
    [LoggerMessage(EventId = 9063, Level = LogLevel.Information,
                   Message = "{ServiceName} service stopped.")]
    public static partial void LogServiceStopped(this ILogger logger, string serviceName);
}
