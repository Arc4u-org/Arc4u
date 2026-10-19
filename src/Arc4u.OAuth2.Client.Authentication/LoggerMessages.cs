using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Client.Authentication;

/// <summary>
/// Source-generated high-performance log messages of the <c>Arc4u.OAuth2.Client.Authentication</c> package, exposed as <see cref="ILogger"/> extension methods.
/// </summary>
public static partial class LoggerMessages
{
     /// <summary>
     /// Logs an Error message (event id 9180): <c>No settings for {resolvingName} is found.</c>
     /// </summary>
     /// <param name="logger">The logger to write to.</param>
     /// <param name="resolvingName">The value substituted in the message.</param>
     [LoggerMessage(EventId = 9180, Level = LogLevel.Error,
         Message = "No settings for {resolvingName} is found.")]
     public static partial void LogNoHttpHandlerSettingsFound(this ILogger logger, string resolvingName);

     /// <summary>
     /// Logs an Error message (event id 9181): <c>No ApplicationContext is registered in the DI container.</c>
     /// </summary>
     /// <param name="logger">The logger to write to.</param>
     [LoggerMessage(EventId = 9181, Level = LogLevel.Error,
         Message = "No ApplicationContext is registered in the DI container.")]
     public static partial void LogNoApplicationContextFound(this ILogger logger);

     /// <summary>
     /// Logs a Trace message (event id 9182): <c>{handlerName} HttpHandler is called.</c>
     /// </summary>
     /// <param name="logger">The logger to write to.</param>
     /// <param name="handlerName">The value substituted in the message.</param>
     [LoggerMessage(EventId = 9182, Level = LogLevel.Trace,
         Message = "{handlerName} HttpHandler is called.")]
     public static partial void LogHttpHandlerIsCalled(this ILogger logger, string handlerName);

     /// <summary>
     /// Logs a Debug message (event id 9183): <c>An authorization header already exist for handler {handlerName}, Check next Delegate Handler</c>
     /// </summary>
     /// <param name="logger">The logger to write to.</param>
     /// <param name="handlerName">The value substituted in the message.</param>
     [LoggerMessage(EventId = 9183, Level = LogLevel.Debug,
         Message = "An authorization header already exist for handler {handlerName}, Check next Delegate Handler")]
     public static partial void LogHasAlreadyAnAuthorizationHeader(this ILogger logger, string handlerName);

     /// <summary>
     /// Logs a Warning message (event id 9184): <c>No token provider is defined in the settings, Check next Delegate Handler</c>
     /// </summary>
     /// <param name="logger">The logger to write to.</param>
     [LoggerMessage(EventId = 9184, Level = LogLevel.Warning,
         Message = "No token provider is defined in the settings, Check next Delegate Handler")]
     public static partial void LogNoTokenProviderIsDefinedInSettings(this ILogger logger);

     /// <summary>
     /// Logs a Debug message (event id 9185): <c>Resolving instance of token provider {providerName}.</c>
     /// </summary>
     /// <param name="logger">The logger to write to.</param>
     /// <param name="providerName">The value substituted in the message.</param>
     [LoggerMessage(EventId = 9185, Level = LogLevel.Debug,
         Message = "Resolving instance of token provider {providerName}.")]
     public static partial void LogTokenProviderIsResolved(this ILogger logger, string providerName);

     /// <summary>
     /// Logs a Debug message (event id 9186): <c>Requesting an authentication token.</c>
     /// </summary>
     /// <param name="logger">The logger to write to.</param>
     [LoggerMessage(EventId = 9186, Level = LogLevel.Debug,
         Message = "Requesting an authentication token.")]
     public static partial void LogRequestingAToken(this ILogger logger);

     /// <summary>
     /// Logs an Error message (event id 9187): <c>Authentication token is not available with token provider {providerName}, Check next Delegate Handler.</c>
     /// </summary>
     /// <param name="logger">The logger to write to.</param>
     /// <param name="providerName">The value substituted in the message.</param>
     [LoggerMessage(EventId = 9187, Level = LogLevel.Error,
         Message = "Authentication token is not available with token provider {providerName}, Check next Delegate Handler.")]
     public static partial void LogNoAuthenticationTokenCanBeRetrieve(this ILogger logger, string providerName);

     /// <summary>
     /// Logs an Error message (event id 9188): <c>Token is expired! Next Hanlder will be called.</c>
     /// </summary>
     /// <param name="logger">The logger to write to.</param>
     [LoggerMessage(EventId = 9188, Level = LogLevel.Error,
         Message = "Token is expired! Next Hanlder will be called.")]
     public static partial void LogTokenIsExpired(this ILogger logger);

     /// <summary>
     /// Logs a Debug message (event id 9189): <c>Add the {scheme} token to provide authentication evidence.</c>
     /// </summary>
     /// <param name="logger">The logger to write to.</param>
     /// <param name="scheme">The value substituted in the message.</param>
     [LoggerMessage(EventId = 9189, Level = LogLevel.Debug,
         Message = "Add the {scheme} token to provide authentication evidence.")]
     public static partial void LogSchemeInfo(this ILogger logger, string scheme);


     /// <summary>
     /// Logs a Debug message (event id 9191): <c>Add the current culture to the request: {culture}.</c>
     /// </summary>
     /// <param name="logger">The logger to write to.</param>
     /// <param name="culture">The value substituted in the message.</param>
     [LoggerMessage(EventId = 9191, Level = LogLevel.Debug,
         Message = "Add the current culture to the request: {culture}.")]
     public static partial void LogCultureRequested(this ILogger logger, string culture);
}
