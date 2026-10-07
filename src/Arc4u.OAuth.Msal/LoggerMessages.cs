
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.AspNetCore;
public static partial class LoggerMessages
{
    [LoggerMessage(EventId = 9120, Level = LogLevel.Error,
                   Message = "No settings can be resolved with name {ResolvingName}.")]
    public static partial void LogResolvingIssueSettingsByName(this ILogger logger, string resolvingName);

    [LoggerMessage(EventId = 9121, Level = LogLevel.Trace,
               Message = "{DelegateName} JwtHttpHandler is called.")]
    public static partial void LogHttpHandlerIsCalled(this ILogger logger, string delegateName);

    [LoggerMessage(EventId = 9122, Level = LogLevel.Trace,
           Message = "{DelegateName}, Check next Delegate Handler.")]
    public static partial void LogCallNextHttpHandler(this ILogger logger, string delegateName);

    [LoggerMessage(EventId = 9123, Level = LogLevel.Trace,
       Message = "No authentication type for {DelegateName}, Check next Delegate Handler.")]
    public static partial void LogNoAuthenticationTypeCallNextHttpHandler(this ILogger logger, string delegateName);

    [LoggerMessage(EventId = 9124, Level = LogLevel.Trace,
        Message = "Different authentication type for {DelegateName}, Check next Delegate Handler.")]
    public static partial void LogDifferentAuthenticationTypeCallNextHttpHandler(this ILogger logger, string delegateName);

    [LoggerMessage(EventId = 9125, Level = LogLevel.Trace,
        Message = "An authorization header already exist for handler {DelegateName}, Check next Delegate Handler.")]
    public static partial void LogAlreadyHasAnAuthorizationHeaderCallNextHttpHandler(this ILogger logger, string delegateName);

    [LoggerMessage(EventId = 9126, Level = LogLevel.Trace,
        Message = "{DelegateName}, Get the authentication Token Provider.")]
    public static partial void LogGetTheTokenProvider(this ILogger logger, string delegateName);

    [LoggerMessage(EventId = 9127, Level = LogLevel.Trace,
        Message = "Call back-end service for authorization, endpoint = {Url}")]
    public static partial void LogCallBackendWithUrl(this ILogger logger, string url);

    [LoggerMessage(EventId = 9128, Level = LogLevel.Trace,
        Message = "Call back-end service {Url} succeeds.")]
    public static partial void LogCallBackendWithUrlSucceed(this ILogger logger, string url);

    [LoggerMessage(EventId = 9129, Level = LogLevel.Trace,
        Message = "{Claims} claim(s) received.")]
    public static partial void LogClaims(this ILogger logger, int claims);

    [LoggerMessage(EventId = 9130, Level = LogLevel.Error,
        Message = "Call service {Url} gives error status ${StatusCode}.")]
    public static partial void LogHttpStatusErrorCode(this ILogger logger, string url, int statusCode);

    [LoggerMessage(EventId = 9131, Level = LogLevel.Trace,
        Message = "No token provider is defined for {ProviderId}, Check next Delegate Handler.")]
    public static partial void LogNoTokenProviderDefined(this ILogger logger, string providerId);

    [LoggerMessage(EventId = 9132, Level = LogLevel.Trace,
        Message = "Requesting an authentication token.")]
    public static partial void LogRequestingAuthenticationToken(this ILogger logger);

    [LoggerMessage(EventId = 9133, Level = LogLevel.Trace,
        Message = "Token is expired! Next Hanlder will be called.")]
    public static partial void LogExpiredToken(this ILogger logger);

    [LoggerMessage(EventId = 9134, Level = LogLevel.Trace,
        Message = "Remove any Bearer token attached.")]
    public static partial void LogRemoveBearerToken(this ILogger logger);

    [LoggerMessage(EventId = 9135, Level = LogLevel.Trace,
        Message = "Add the {Scheme} token to provide authentication evidence.")]
    public static partial void LogAddSchemeToToken(this ILogger logger, string scheme);

    [LoggerMessage(EventId = 9137, Level = LogLevel.Trace,
       Message = "Add the current culture to the request: {CurrentCulture}.")]
    public static partial void LogAddCulture(this ILogger logger, string currentCulture);

    [LoggerMessage(EventId = 9138, Level = LogLevel.Error,
        Message = "No settings to retrieve the information to call the backend are defined.")]
    public static partial void LogNoTokenSettings(this ILogger logger);

    [LoggerMessage(EventId = 9139, Level = LogLevel.Debug,
   Message = "Skip fetching claims, no setting found for authentication type {AuthenticationType}.")]
    public static partial void LogSkipFillingClaims(this ILogger logger, string authenticationType);

    [LoggerMessage(EventId = 9140, Level = LogLevel.Error,
        Message = "A null identity was received. No Claims will be generated.")]
    public static partial void LogNullIdentity(this ILogger logger);

    [LoggerMessage(EventId = 9141, Level = LogLevel.Error,
    Message = "MsalUiRequiredException: {ErrorMessage}")]
    public static partial void LogMsalUiRequiredException(this ILogger logger, string errorMessage);
}
