
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2
{
    /// <summary>
/// Source-generated high-performance log messages of the <c>Arc4u.OAuth2.AspNetCore.Authentication</c> package, exposed as <see cref="ILogger"/> extension methods.
/// </summary>
public static partial class LoggerMessages
    {
        /// <summary>
        /// Logs an Error message (event id 9120): <c>No settings or application context is defined with {ResolvingName}.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="resolvingName">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9120, Level = LogLevel.Error,
            Message = "No settings or application context is defined with {ResolvingName}.")]
        public static partial void LogResolvingIssueSettingsByName(this ILogger logger, string resolvingName);

        /// <summary>
        /// Logs a Trace message (event id 9121): <c>{DelegateName} JwtHttpHandler is called.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="delegateName">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9121, Level = LogLevel.Trace,
            Message = "{DelegateName} JwtHttpHandler is called.")]
        public static partial void LogHttpHandlerIsCalled(this ILogger logger, string delegateName);

        /// <summary>
        /// Logs a Trace message (event id 9123): <c>No authentication type for {DelegateName}, Check next Delegate Handler.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="delegateName">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9123, Level = LogLevel.Trace,
            Message = "No authentication type for {DelegateName}, Check next Delegate Handler.")]
        public static partial void LogNoAuthenticationTypeCallNextHttpHandler(this ILogger logger, string delegateName);

        /// <summary>
        /// Logs a Trace message (event id 9124): <c>Different authentication type for {DelegateName}, Check next Delegate Handler.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="delegateName">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9124, Level = LogLevel.Trace,
            Message = "Different authentication type for {DelegateName}, Check next Delegate Handler.")]
        public static partial void LogDifferentAuthenticationTypeCallNextHttpHandler(this ILogger logger, string delegateName);

        /// <summary>
        /// Logs a Trace message (event id 9125): <c>An authorization header already exist for handler {DelegateName}, Check next Delegate Handler.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="delegateName">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9125, Level = LogLevel.Trace,
            Message = "An authorization header already exist for handler {DelegateName}, Check next Delegate Handler.")]
        public static partial void LogAlreadyHasAnAuthorizationHeaderCallNextHttpHandler(this ILogger logger, string delegateName);

        /// <summary>
        /// Logs a Trace message (event id 9126): <c>{DelegateName}, Get the authentication Token Provider.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="delegateName">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9126, Level = LogLevel.Trace,
            Message = "{DelegateName}, Get the authentication Token Provider.")]
        public static partial void LogGetTheTokenProvider(this ILogger logger, string delegateName);

        /// <summary>
        /// Logs a Trace message (event id 9127): <c>{DelegateName}, No authentication Token Provider.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="delegateName">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9127, Level = LogLevel.Trace,
            Message = "{DelegateName}, No authentication Token Provider.")]
        public static partial void LogNoTokenProvider(this ILogger logger, string delegateName);

        /// <summary>
        /// Logs a Trace message (event id 9128): <c>Requesting an authentication token.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        [LoggerMessage(EventId = 9128, Level = LogLevel.Trace,
            Message = "Requesting an authentication token.")]
        public static partial void LogRequestingToken(this ILogger logger);

        /// <summary>
        /// Logs a Trace message (event id 9129): <c>Token is expired! Next Hanlder will be called.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        [LoggerMessage(EventId = 9129, Level = LogLevel.Trace,
            Message = "Token is expired! Next Hanlder will be called.")]
        public static partial void LogTokenIsExpired(this ILogger logger);

        /// <summary>
        /// Logs a Trace message (event id 9130): <c>Remove any Bearer token attached.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        [LoggerMessage(EventId = 9130, Level = LogLevel.Trace,
            Message = "Remove any Bearer token attached.")]
        public static partial void LogRemoveAnyBearer(this ILogger logger);

        /// <summary>
        /// Logs a Trace message (event id 9131): <c>Add the {Scheme} token to provide authentication evidence.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="scheme">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9131, Level = LogLevel.Trace,
            Message = "Add the {Scheme} token to provide authentication evidence.")]
        public static partial void LogAddSchentoToken(this ILogger logger, string scheme);

        /// <summary>
        /// Logs a Trace message (event id 9147): <c>Add the current culture to the request: {Culture}.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="culture">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9147, Level = LogLevel.Trace,
            Message = "Add the current culture to the request: {Culture}.")]
        public static partial void LogUseCurrentCulture(this ILogger logger, string culture);

        /// <summary>
        /// Logs an Information message (event id 9132): <c>Refresh the access token. Expired since {TimeExpired}.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="timeExpired">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9132, Level = LogLevel.Information,
            Message = "Refresh the access token. Expired since {TimeExpired}.")]
        public static partial void LogAccessTokenIsExpired(this ILogger logger, TimeSpan timeExpired);

        /// <summary>
        /// Logs an Information message (event id 9132): <c>Refresh the access token. Will expire in {TimeExpired}.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="timeExpired">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9132, Level = LogLevel.Information,
            Message = "Refresh the access token. Will expire in {TimeExpired}.")]
        public static partial void LogAccessTokenIsExpiring(this ILogger logger, TimeSpan timeExpired);

        /// <summary>
        /// Logs a Debug message (event id 9133): <c>Extract token from the cookie cache.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        [LoggerMessage(EventId = 9133, Level = LogLevel.Debug,
            Message = "Extract token from the cookie cache.")]
        public static partial void LogExtractCookieFromTokenCache(this ILogger logger);

        /// <summary>
        /// Logs an Error message (event id 9134): <c>No TokenRefreshInfo found in the service provider.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        [LoggerMessage(EventId = 9134, Level = LogLevel.Error,
            Message = "No TokenRefreshInfo found in the service provider.")]
        public static partial void LogNoTokenRefreshInfo(this ILogger logger);

        /// <summary>
        /// Logs an Error message (event id 9135): <c>No TokenRefreshInfo found in the service provider. Reject the principal and sign out the user.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        [LoggerMessage(EventId = 9135, Level = LogLevel.Error,
            Message = "No TokenRefreshInfo found in the service provider. Reject the principal and sign out the user.")]
        public static partial void LogCantRefreshToken(this ILogger logger);

        /// <summary>
        /// Logs a Debug message (event id 9136): <c>Cache with name {CacheName} is used for the Authentication tickets.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="cacheName">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9136, Level = LogLevel.Debug,
            Message = "Cache with name {CacheName} is used for the Authentication tickets.")]
        public static partial void LogCacheNameUsed(this ILogger logger, string cacheName);

        /// <summary>
        /// Logs a Warning message (event id 9137): <c>Cache with name {CacheName} doesn't exist, fallback on the default one for the Authentication tickets!</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="cacheName">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9137, Level = LogLevel.Warning,
            Message = "Cache with name {CacheName} doesn't exist, fallback on the default one for the Authentication tickets!")]
        public static partial void LogNoCacheExist(this ILogger logger, string cacheName);

        /// <summary>
        /// Logs a Debug message (event id 9138): <c>Authentication ticket with key {Key} has been deleted.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="key">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9138, Level = LogLevel.Debug,
            Message = "Authentication ticket with key {Key} has been deleted.")]
        public static partial void LogDeleteAuthenticationTicket(this ILogger logger, string key);

        /// <summary>
        /// Logs a Debug message (event id 9139): <c>Authentication ticket with key {Key} has been created with validity period of {period}.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="key">The value substituted in the message.</param>
        /// <param name="period">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9139, Level = LogLevel.Debug,
            Message = "Authentication ticket with key {Key} has been created with validity period of {period}.")]
        public static partial void LogCreateAuthenticationTicket(this ILogger logger, string key, TimeSpan period);

        /// <summary>
        /// Logs a Debug message (event id 9140): <c>Authentication ticket with key {Key} has been created.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="key">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9140, Level = LogLevel.Debug,
            Message = "Authentication ticket with key {Key} has been created.")]
        public static partial void LogAuthenticationTicketCreated(this ILogger logger, string key);

        /// <summary>
        /// Logs an Error message (event id 9141): <c>Authentication ticket from the cache is null!</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        [LoggerMessage(EventId = 9141, Level = LogLevel.Error,
            Message = "Authentication ticket from the cache is null!")]
        public static partial void LogAuthenticationTicketIsNull(this ILogger logger);

        /// <summary>
        /// Logs an Error message (event id 9142): <c>No Authentication ticket from the cache.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        [LoggerMessage(EventId = 9142, Level = LogLevel.Error,
            Message = "No Authentication ticket from the cache.")]
        public static partial void LogNoAuthenticationTicketFromCache(this ILogger logger);

        /// <summary>
        /// Logs a Debug message (event id 9143): <c>Remove ticket with key: {Key}.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="key">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9143, Level = LogLevel.Debug,
            Message = "Remove ticket with key: {Key}.")]
        public static partial void LogRemoveAuthenticationTicket(this ILogger logger, string key);

        /// <summary>
        /// Logs a Debug message (event id 9144): <c>Renew ticket with key: {Key} on path: {FullPath}.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="key">The value substituted in the message.</param>
        /// <param name="fullPath">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9144, Level = LogLevel.Debug,
            Message = "Renew ticket with key: {Key} on path: {FullPath}.")]
        public static partial void LogRenewAuthenticationTicket(this ILogger logger, string key, string fullPath);

        /// <summary>
        /// Logs a Debug message (event id 9145): <c>Get ticket with key: {Key} on path: {FullPath}.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="key">The value substituted in the message.</param>
        /// <param name="fullPath">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9145, Level = LogLevel.Debug,
            Message = "Get ticket with key: {Key} on path: {FullPath}.")]
        public static partial void LogGetAuthenticationTicket(this ILogger logger, string key, string fullPath);

        /// <summary>
        /// Logs an Error message (event id 9146): <c>{FullPath} doesn't exist, can't retrieve an authentication ticket.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="fullPath">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9146, Level = LogLevel.Error,
            Message = "{FullPath} doesn't exist, can't retrieve an authentication ticket.")]
        public static partial void LogNoFileExistForAuthenticationTicket(this ILogger logger, string fullPath);

        /// <summary>
        /// Logs an Error message (event id 9147): <c>Create ticket with key: {Key} on path: {FullPath}.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="key">The value substituted in the message.</param>
        /// <param name="fullPath">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9147, Level = LogLevel.Error,
            Message = "Create ticket with key: {Key} on path: {FullPath}.")]
        public static partial void LogCreateAuthenticationTicketOnFile(this ILogger logger, string key, string fullPath);

        /// <summary>
        /// Logs an Error message (event id 9148): <c>Getting the Access token with Obo failed. {ReasonPhrase}</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="reasonPhrase">The value substituted in the message.</param>
        [LoggerMessage(EventId = 9148, Level = LogLevel.Error,
            Message = "Getting the Access token with Obo failed. {ReasonPhrase}")]
        public static partial void LogOboFailedReasonWithPII(this ILogger logger, string reasonPhrase);

        /// <summary>
        /// Logs an Error message (event id 9149): <c>Getting the Access token with Obo failed. Enable PII to have more info.</c>
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        [LoggerMessage(EventId = 9149, Level = LogLevel.Error,
            Message = "Getting the Access token with Obo failed. Enable PII to have more info.")]
        public static partial void LogOboFailedWithNoReasonNoPII(this ILogger logger);
    }
}
