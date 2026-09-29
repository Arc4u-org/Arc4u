
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2;
/// <summary>
/// Source-generated high-performance log messages of the <c>Arc4u.OAuth2</c> package, exposed as <see cref="ILogger"/> extension methods.
/// </summary>
public static partial class LoggerMessages
{
    /// <summary>
    /// Logs a Trace message (event id 9080): <c>The token cache is {cacheName}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="cacheName">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9080, Level = LogLevel.Trace,
                   Message = "The token cache is {cacheName}.")]
    public static partial void LogTokenCacheName(this ILogger logger, string cacheName);

    /// <summary>
    /// Logs a Trace message (event id 9081): <c>The token cache is using the default cache defined in the config.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9081, Level = LogLevel.Trace,
                   Message = "The token cache is using the default cache defined in the config.")]
    public static partial void LogDefaultTokenCache(this ILogger logger);

    /// <summary>
    /// Logs a Trace message (event id 9082): <c>Requesting an authentication token.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9082, Level = LogLevel.Trace,
               Message = "Requesting an authentication token.")]
    public static partial void LogRequestingAuthenticationToken(this ILogger logger);

    /// <summary>
    /// Logs an Error message (event id 9083): <c>Requesting an authentication token.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9083, Level = LogLevel.Error,
           Message = "Requesting an authentication token.")]
    public static partial void LogNoToken(this ILogger logger);

    /// <summary>
    /// Logs a Trace message (event id 9084): <c>Skip fetching claims, no setting found for authentication type {AuthenticationType}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="authenticationType">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9084, Level = LogLevel.Trace,
           Message = "Skip fetching claims, no setting found for authentication type {AuthenticationType}.")]
    public static partial void LogSkipFetchingClaims(this ILogger logger, string authenticationType);

    /// <summary>
    /// Logs an Error message (event id 9085): <c>Token endpoint for {Upn} returned {ResponseStatusCode}: {LoggedResponseBody}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="upn">The value substituted in the message.</param>
    /// <param name="responseStatusCode">The value substituted in the message.</param>
    /// <param name="loggedResponseBody">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9085, Level = LogLevel.Error,
           Message = "Token endpoint for {Upn} returned {ResponseStatusCode}: {LoggedResponseBody}.")]
    public static partial void LogCredentialToken(this ILogger logger, string upn, string responseStatusCode, string loggedResponseBody);

    /// <summary>
    /// Logs a Debug message (event id 9086): <c>Creating an authentication context for the request.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9086, Level = LogLevel.Debug,
       Message = "Creating an authentication context for the request.")]
    public static partial void LogCreatingAuthenticationContext(this ILogger logger);

    /// <summary>
    /// Logs a Debug message (event id 9087): <c>Call STS: {Authority} for user: {Upn}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="authority">The value substituted in the message.</param>
    /// <param name="upn">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9087, Level = LogLevel.Debug,
       Message = "Call STS: {Authority} for user: {Upn}.")]
    public static partial void LogStsAndUser(this ILogger logger, string authority, string upn);

    /// <summary>
    /// Logs a Debug message (event id 9088): <c>Get token endpoint</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9088, Level = LogLevel.Debug,
       Message = "Get token endpoint")]
    public static partial void LogGetEndpoint(this ILogger logger);

    /// <summary>
    /// Logs a Debug message (event id 9089): <c>Token is received for user {UserUpn}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="userUpn">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9089, Level = LogLevel.Debug,
               Message = "Token is received for user {UserUpn}.")]
    public static partial void LogTokenReceived(this ILogger logger, string userUpn);

    /// <summary>
    /// Logs a Debug message (event id 9090): <c>Access token will expire at {TokenExpirationDateUtc} utc.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="tokenExpirationDateUtc">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9090, Level = LogLevel.Debug,
           Message = "Access token will expire at {TokenExpirationDateUtc} utc.")]
    public static partial void LogTokenExpiration(this ILogger logger, DateTime tokenExpirationDateUtc);

    /// <summary>
    /// Logs a Warning message (event id 9091): <c>Geting all data from the token cache is not implemented.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9091, Level = LogLevel.Warning,
        Message = "Geting all data from the token cache is not implemented.")]
    public static partial void LogTokenCacheNotImplemented(this ILogger logger);

    /// <summary>
    /// Logs a Trace message (event id 9092): <c>Deleting information from the token cache for the id: {TokenKey}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="tokenKey">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9092, Level = LogLevel.Trace,
        Message = "Deleting information from the token cache for the id: {TokenKey}.")]
    public static partial void LogDeleteInTokenCache(this ILogger logger, string tokenKey);

    /// <summary>
    /// Logs a Trace message (event id 9093): <c>Deleted information from the token cache for the id: {TokenKey}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="tokenKey">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9093, Level = LogLevel.Trace,
        Message = "Deleted information from the token cache for the id: {TokenKey}.")]
    public static partial void LogDeletedInTokenCache(this ILogger logger, string tokenKey);

    /// <summary>
    /// Logs a Warning message (event id 9094): <c>A null token data information was provided to the cache, with an id: {TokenKey}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="tokenKey">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9094, Level = LogLevel.Warning,
        Message = "A null token data information was provided to the cache, with an id: {TokenKey}.")]
    public static partial void LogNullTokenData(this ILogger logger, string tokenKey);

    /// <summary>
    /// Logs a Trace message (event id 9095): <c>Adding token data information to the cache with id: {TokenKey}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="tokenKey">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9095, Level = LogLevel.Trace,
    Message = "Adding token data information to the cache with id: {TokenKey}.")]
    public static partial void LogAddingInTokenCache(this ILogger logger, string tokenKey);

    /// <summary>
    /// Logs a Trace message (event id 9096): <c>Added token data information to the cache with id: {TokenKey}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="tokenKey">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9096, Level = LogLevel.Trace,
        Message = "Added token data information to the cache with id: {TokenKey}.")]
    public static partial void LogAddedInTokenCache(this ILogger logger, string tokenKey);

    /// <summary>
    /// Logs a Trace message (event id 9097): <c>Retrieve token information for user: {TokenKey}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="tokenKey">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9097, Level = LogLevel.Trace,
        Message = "Retrieve token information for user: {TokenKey}.")]
    public static partial void LogGetDataTokenCache(this ILogger logger, string tokenKey);

    /// <summary>
    /// Logs a Warning message (event id 9098): <c>The data in cache is null for user: {TokenKey}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="tokenKey">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9098, Level = LogLevel.Warning,
        Message = "The data in cache is null for user: {TokenKey}.")]
    public static partial void LogGetNullDataTokenCache(this ILogger logger, string tokenKey);

#if NET10_0_OR_GREATER
    // Custom Root CA logging messages
    /// <summary>
    /// Logs a Debug message (event id 9100): <c>Using custom root certificate option key: {Key}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="key">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9100, Level = LogLevel.Debug,
                   Message = "Using custom root certificate option key: {Key}.")]
    public static partial void LogUsingCustomRootCertificateOptionKey(this ILogger logger, string key);

    /// <summary>
    /// Logs a Debug message (event id 9101): <c>Loaded custom root certificate: {CertificateFriendlyName}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="certificateFriendlyName">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9101, Level = LogLevel.Debug,
                   Message = "Loaded custom root certificate: {CertificateFriendlyName}.")]
    public static partial void LogLoadedCustomRootCertificate(this ILogger logger, string certificateFriendlyName);

    /// <summary>
    /// Logs a Warning message (event id 9102): <c>No valid CA certificates loaded for custom root trust.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9102, Level = LogLevel.Warning,
                   Message = "No valid CA certificates loaded for custom root trust.")]
    public static partial void LogNoValidCaCertificatesLoaded(this ILogger logger);

    /// <summary>
    /// Logs an Error message (event id 9103): <c>Failed to load internal CA certificate from {CertificateOptionKey}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="ex">The value substituted in the message.</param>
    /// <param name="certificateOptionKey">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9103, Level = LogLevel.Error,
                   Message = "Failed to load internal CA certificate from {CertificateOptionKey}.")]
    public static partial void LogFailedToLoadCaCertificate(this ILogger logger, Exception ex, string? certificateOptionKey);

    /// <summary>
    /// Logs a Warning message (event id 9104): <c>Failed to build certificate chain.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9104, Level = LogLevel.Warning,
                   Message = "Failed to build certificate chain.")]
    public static partial void LogFailedToBuildCertificateChain(this ILogger logger);

    /// <summary>
    /// Logs a Warning message (event id 9105): <c>Certificate validation failed for {Subject}. Chain status: {ChainStatus}.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    /// <param name="subject">The value substituted in the message.</param>
    /// <param name="chainStatus">The value substituted in the message.</param>
    [LoggerMessage(EventId = 9105, Level = LogLevel.Warning,
                   Message = "Certificate validation failed for {Subject}. Chain status: {ChainStatus}.")]
    public static partial void LogCertificateValidationFailed(this ILogger logger, string? subject, string chainStatus);

    /// <summary>
    /// Logs a Warning message (event id 9106): <c>Failed to get remote certificate.</c>
    /// </summary>
    /// <param name="logger">The logger to write to.</param>
    [LoggerMessage(EventId = 9106, Level = LogLevel.Warning,
        Message = "Failed to get remote certificate.")]
    public static partial void LogFailedToGetRemoteCertificate(this ILogger logger);

#endif
}
