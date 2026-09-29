using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.AspNetCore.Blazor;
/// <summary>
/// Source-generated high-performance log messages of the <c>Arc4u.OAuth2.AspNetCore.Blazor</c> package, exposed as <see cref="ILogger"/> extension methods.
/// </summary>
public static partial class LoggerMessages
{
    /// <summary>
    /// Logs a Warning message (event id 9160): <c>No access token can be retrieved for the current user!</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9160, Level = LogLevel.Warning,
                   Message = "No access token can be retrieved for the current user!")]
    public static partial void LogNoAccessToken(this ILogger logger);

    /// <summary>
    /// Logs a Warning message (event id 9161): <c>The user is not identified!</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9161, Level = LogLevel.Warning,
                   Message = "The user is not identified!")]
    public static partial void LogUserNotIdentified(this ILogger logger);

    /// <summary>
    /// Logs an Error message (event id 9162): <c>Token provider for {ProviderId} is null</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="providerId">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9162, Level = LogLevel.Error,
               Message = "Token provider for {ProviderId} is null")]
    public static partial void LogTokenProviderIsNull(this ILogger logger, string providerId);
}
