using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace Arc4u.Diagnostics;

/// <summary>Default logging helpers shared by Arc4u components.</summary>
public static partial class LoggerDefaults
{
    private static readonly Action<ILogger, string, Exception?> __LogExceptionCallback =
            LoggerMessage.Define<string>(LogLevel.Error, 
                                         new EventId(9000, nameof(LogException)), "Exception: {Message}" , 
                                         new LogDefineOptions() { SkipEnabledCheck = true });

    /// <summary>
    /// Logs an exception at <see cref="LogLevel.Error"/> level with event id 9000 and the message <c>Exception: {Message}</c>.
    /// Nothing is written when the Error level is disabled. If the logger is an <see cref="ILoggerCallerMember"/>, the caller name is recorded.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="exception">The exception to log.</param>
    /// <param name="caller">The name of the calling member; filled in by the compiler.</param>
    public static void LogException(this ILogger logger, Exception exception, [CallerMemberName] string caller = "")
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            if (logger is ILoggerCallerMember loggerTypeInfo)
            {
                loggerTypeInfo.CallerMemberName(caller);
            }
            __LogExceptionCallback(logger, exception.Message, exception);
        }
    }
}
