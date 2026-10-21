using Microsoft.Extensions.Logging;

namespace Arc4u.Diagnostics.Monitoring;
/// <summary>Source-generated log messages used by the monitoring components.</summary>
public static partial class MonitoringMessages
{
    /// <summary>Logs the <c>Cpu &amp; Memory</c> message (event id 9001, Information level) with the properties added to the logger.</summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 9001,
                   Level = LogLevel.Information,
                   Message = "Cpu & Memory",
                   SkipEnabledCheck = true)]
    [Obsolete(MonitoringObsolete.Message)]
    public static partial void LogMonitoring(this ILogger logger);
}
