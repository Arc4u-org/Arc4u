
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Client;
/// <summary>
/// Source-generated high-performance log messages of the <c>Arc4u.OAuth2.Client</c> package, exposed as <see cref="ILogger"/> extension methods.
/// </summary>
public static partial class LoggerMessages
{
    /// <summary>
    /// Logs a Trace message (event id 9170): <c>Null token provider is invoked.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9170, Level = LogLevel.Trace,
                   Message = "Null token provider is invoked.")]
    public static partial void LogCallNullTokenProvider(this ILogger logger);

    /// <summary>
    /// Logs a Trace message (event id 9171): <c>Null token provider signout is invoked.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9171, Level = LogLevel.Trace,
               Message = "Null token provider signout is invoked.")]
    public static partial void LogCallSignOutNullTokenProvider(this ILogger logger);

}
