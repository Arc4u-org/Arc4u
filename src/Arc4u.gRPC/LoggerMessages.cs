
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2;
/// <summary>
/// Source generated logging messages (events 9070 to 9080) used by the gRPC interceptors and the certificate extractor.
/// </summary>
public static partial class LoggerMessages
{
    /// <summary>
    /// Logs at trace level that an authorization header already exists so no bearer token is added.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authenticationType">The authentication type of the settings.</param>
    [LoggerMessage(EventId = 9070, Level = LogLevel.Trace,
                   Message = "Authorization header found. Skip adding a bearer token for AuthenticationType: {AuthenticationType}.")]
    public static partial void LogSkipAddingBearerToken(this ILogger logger, string authenticationType);

    /// <summary>
    /// Logs at trace level that no application context is available.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="contextName">The name of the context looked for.</param>
    [LoggerMessage(EventId = 9071, Level = LogLevel.Trace,
                   Message = "No settings or application context is defined with {ContextName}, Check next Delegate Handler.")]
    public static partial void LogNoApplicationContextIsDefined(this ILogger logger, string contextName);

    /// <summary>
    /// Logs at trace level that no authentication type is defined in the settings.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="authenticationType">The name reported in the message.</param>
    [LoggerMessage(EventId = 9072, Level = LogLevel.Trace,
                   Message = "No authentication type for {AuthenticationType}, Check next Interceptor.")]
    public static partial void LogNoAuthenticationTypeIsDefined(this ILogger logger, string authenticationType);

    /// <summary>
    /// Logs at trace level that there is no user context.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 9073, Level = LogLevel.Trace,
                   Message = "No user context, Check next Interceptor.")]
    public static partial void LogNoUserContext(this ILogger logger);

    /// <summary>
    /// Logs at trace level that no token provider is defined.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tokenProvider">The name reported in the message.</param>
    [LoggerMessage(EventId = 9074, Level = LogLevel.Trace,
                   Message = "No token provider is defined for {TokenProvider}, Check next Interceptor.")]
    public static partial void LogNoTokenProviderIsDefined(this ILogger logger, string tokenProvider);

    /// <summary>
    /// Logs at trace level that the token provider did not provide a token.
    /// </summary>
    /// <remarks>The message text is the same as the one of <see cref="LogNoTokenProviderIsDefined(ILogger, string)"/>.</remarks>
    /// <param name="logger">The logger.</param>
    /// <param name="tokenProvider">The name reported in the message.</param>
    [LoggerMessage(EventId = 9075, Level = LogLevel.Trace,
                   Message = "No token provider is defined for {TokenProvider}, Check next Interceptor.")]
    public static partial void LogNoTokenIsProvided(this ILogger logger, string tokenProvider);

    /// <summary>
    /// Logs at trace level that the token is expired.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 9076, Level = LogLevel.Trace,
                   Message = "Token is expired! Next Interceptor will be called.")]
    public static partial void LogGrpcTokenIsExpired(this ILogger logger);

    /// <summary>
    /// Logs at trace level the scheme of the token added to the request.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="scheme">The authentication scheme.</param>
    [LoggerMessage(EventId = 9080, Level = LogLevel.Trace,
                   Message = "Add the {Scheme} token to provide authentication evidence.")]
    public static partial void LogAddSchemeToken(this ILogger logger, string scheme);

    /// <summary>
    /// Logs at trace level the culture added to the request.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="currentCulture">The culture name.</param>
    [LoggerMessage(EventId = 9077, Level = LogLevel.Trace,
                   Message = "Add the current culture to the request: {CurrentCulture}.")]
    public static partial void LogAddCurrentCulture(this ILogger logger, string currentCulture);

    /// <summary>
    /// Logs at trace level the subject of the certificate received by the validation callback.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="certificateSubject">The subject of the certificate.</param>
    [LoggerMessage(EventId = 9078, Level = LogLevel.Trace,
                   Message = "Certificate callback received with Subject = {CertificateSubject}.")]
    public static partial void LogCertificateFeedBackReceived(this ILogger logger, string certificateSubject);

    /// <summary>
    /// Logs at error level that the validation callback received no certificate.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 9079, Level = LogLevel.Error,
                  Message = "Certificate callback received with no certificate!")]
    public static partial void LogCertificateFeedBackWithNoCertificate(this ILogger logger);
}
