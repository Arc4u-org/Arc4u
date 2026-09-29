
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.AspNetCore;
/// <summary>
/// Source-generated high-performance log messages of the <c>Arc4u.OAuth2.AspNetCore</c> package, exposed as <see cref="ILogger"/> extension methods.
/// </summary>
public static partial class LoggerMessages
{
    /// <summary>
    /// Logs an Information message (event id 9100): <c>Time to complete method call</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9100, Level = LogLevel.Information,
                   Message = "Time to complete method call")]
    public static partial void TimeToCompleteMethodCall(this ILogger logger);

    /// <summary>
    /// Logs a Debug message (event id 9101): <c>Thread UI Culture is set to {UICultureName}</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="uICultureName">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9101, Level = LogLevel.Debug,
                   Message = "Thread UI Culture is set to {UICultureName}")]
    public static partial void LogThreadCultureName(this ILogger logger, string uICultureName);

    /// <summary>
    /// Logs a Debug message (event id 9102): <c>Username receives is: {UserName}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="userName">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9102, Level = LogLevel.Debug,
                   Message = "Username receives is: {UserName}.")]
    public static partial void LogUserName(this ILogger logger, string userName);

    /// <summary>
    /// Logs a Debug message (event id 9103): <c>Change user name receivde from {UserName} to {NewUserName}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="userName">The value substituted in the message.</param>
    /// <param name="newUserName">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9103, Level = LogLevel.Debug,
                   Message = "Change user name receivde from {UserName} to {NewUserName}.")]
    public static partial void LogChangeUserName(this ILogger logger, string userName, string newUserName);

    /// <summary>
    /// Logs a Debug message (event id 9104): <c>Force an OpenId connection.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9104, Level = LogLevel.Debug,
                   Message = "Force an OpenId connection.")]
    public static partial void LogForceOpenIdConnect(this ILogger logger);

    /// <summary>
    /// Logs an Error message (event id 9104): <c>No claim type found equal to: {IdentifierOptions} in the current identity.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="identifierOptions">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9104, Level = LogLevel.Error,
                   Message = "No claim type found equal to: {IdentifierOptions} in the current identity.")]
    public static partial void LogNoClaimTypeFound(this ILogger logger, string identifierOptions);

    /// <summary>
    /// Logs a Debug message (event id 9105): <c>Claim Type id used to identify the user is {ClaimTypeId}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="claimTypeId">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9105, Level = LogLevel.Debug,
                   Message = "Claim Type id used to identify the user is {ClaimTypeId}.")]
    public static partial void LogClaimTypeIdFound(this ILogger logger, string claimTypeId);

    /// <summary>
    /// Logs a Warning message (event id 9106): <c>Loading extra claims needs an identity!</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9106, Level = LogLevel.Warning,
                   Message = "Loading extra claims needs an identity!")]
    public static partial void LogNoIdentity(this ILogger logger);

    /// <summary>
    /// Logs an Error message (event id 9107): <c>Basic authentication is not well formed!</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9107, Level = LogLevel.Error,
               Message = "Basic authentication is not well formed!")]
    public static partial void LogBasicAuthentityBadFormat(this ILogger logger);

    /// <summary>
    /// Logs a Trace message (event id 9108): <c>Create the principal.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9108, Level = LogLevel.Trace,
           Message = "Create the principal.")]
    public static partial void LogPrincipalCreation(this ILogger logger);

    /// <summary>
    /// Logs a Trace message (event id 9109): <c>Check if the cache contains a token for {TokenCacheKey}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="tokenCacheKey">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9109, Level = LogLevel.Trace,
       Message = "Check if the cache contains a token for {TokenCacheKey}.")]
    public static partial void LogCheckContainsKeyInCache(this ILogger logger, string tokenCacheKey);

    /// <summary>
    /// Logs a Trace message (event id 9110): <c>Token loaded from the cache for {TokenCacheKey}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="tokenCacheKey">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9110, Level = LogLevel.Trace,
        Message = "Token loaded from the cache for {TokenCacheKey}.")]
    public static partial void LogTokenLoadedFromCache(this ILogger logger, string tokenCacheKey);

    /// <summary>
    /// Logs a Trace message (event id 9111): <c>Token loaded from the cache is expired {TokenCacheKey}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="tokenCacheKey">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9111, Level = LogLevel.Trace,
        Message = "Token loaded from the cache is expired {TokenCacheKey}.")]
    public static partial void LogTokenExpiredLoadedFromCache(this ILogger logger, string tokenCacheKey);

    /// <summary>
    /// Logs a Trace message (event id 9112): <c>Contact the Service Token Provider to create an access token for {TokenCacheKey}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="tokenCacheKey">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9112, Level = LogLevel.Trace,
        Message = "Contact the Service Token Provider to create an access token for {TokenCacheKey}.")]
    public static partial void LogCallSTS(this ILogger logger, string tokenCacheKey);

    /// <summary>
    /// Logs a Trace message (event id 9113): <c>Save the token in the cache for {TokenCacheKey}, will expire at {ExpiresOnUtc} Utc.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="tokenCacheKey">The value substituted in the message.</param>
    /// <param name="expiresOnUtc">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9113, Level = LogLevel.Trace,
        Message = "Save the token in the cache for {TokenCacheKey}, will expire at {ExpiresOnUtc} Utc.")]
    public static partial void LogSaveTokenInCache(this ILogger logger, string tokenCacheKey, DateTime expiresOnUtc);

    /// <summary>
    /// Logs a Warning message (event id 9114): <c>No cache is defined. STS is called for every call!</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9114, Level = LogLevel.Warning,
        Message = "No cache is defined. STS is called for every call!")]
    public static partial void LogNoCachePerformance(this ILogger logger);

    /// <summary>
    /// Logs an Error message (event id 9115): <c>No token provider found for {CredentialTokenProviderProviderName}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="credentialTokenProviderProviderName">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9115, Level = LogLevel.Error,
    Message = "No token provider found for {CredentialTokenProviderProviderName}.")]
    public static partial void LogNoTokenProvider(this ILogger logger, string credentialTokenProviderProviderName);

    /// <summary>
    /// Logs a Trace message (event id 9116): <c>Creating an authentication context for the request.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9116, Level = LogLevel.Trace,
    Message = "Creating an authentication context for the request.")]
    public static partial void LogCreateAuthenticationContext(this ILogger logger);

    /// <summary>
    /// Logs an Error message (event id 9117): <c>Requesting a client_credentials token failed with {StatusCode}: {ResponseBody}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="statusCode">The value substituted in the message.</param>
    /// <param name="responseBody">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9117, Level = LogLevel.Error,
    Message = "Requesting a client_credentials token failed with {StatusCode}: {ResponseBody}.")]
    public static partial void LogClientCredentialsTokenError(this ILogger logger, string statusCode, string responseBody);
}
