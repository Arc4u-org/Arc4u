#if NET10_0_OR_GREATER

using Microsoft.Extensions.Logging;

namespace Arc4u.AspNetCore.gRpc;

/// <summary>
/// Source generated logging messages (events 10100 to 10106) used when configuring and validating a custom root certificate for gRPC.
/// </summary>
public static partial class LoggerMessages
{
    /// <summary>
    /// Logs at debug level the option key used to find the custom root certificate.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="key">The option key.</param>
    [LoggerMessage(EventId = 10100, Level = LogLevel.Debug,
                   Message = "Using custom root certificate option key: {Key}.")]
    public static partial void LogUsingCustomRootCertificateOptionKey(this ILogger logger, string key);

    /// <summary>
    /// Logs at debug level that a custom root certificate was loaded.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="certificateFriendlyName">The friendly name (or subject) of the certificate.</param>
    [LoggerMessage(EventId = 10101, Level = LogLevel.Debug,
                   Message = "Loaded custom root certificate: {CertificateFriendlyName}.")]
    public static partial void LogLoadedCustomRootCertificate(this ILogger logger, string certificateFriendlyName);

    /// <summary>
    /// Logs at warning level that no custom root certificate could be loaded.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 10102, Level = LogLevel.Warning,
                   Message = "No valid CA certificates loaded for custom root trust.")]
    public static partial void LogNoValidCACertificatesLoaded(this ILogger logger);

    /// <summary>
    /// Logs at error level that loading the custom root certificate failed.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="ex">The exception raised.</param>
    /// <param name="certificateOptionKey">The option key used, if any.</param>
    [LoggerMessage(EventId = 10103, Level = LogLevel.Error,
                   Message = "Failed to load internal CA certificate from {CertificateOptionKey}.")]
    public static partial void LogFailedToLoadCACertificate(this ILogger logger, Exception ex, string? certificateOptionKey);

    /// <summary>
    /// Logs at warning level that no certificate chain was available for validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 10104, Level = LogLevel.Warning,
                   Message = "Failed to build certificate chain.")]
    public static partial void LogFailedToBuildCertificateChain(this ILogger logger);

    /// <summary>
    /// Logs at warning level that the server certificate was not provided for validation.
    /// </summary>
    /// <param name="logger">The logger.</param>
    [LoggerMessage(EventId = 10105, Level = LogLevel.Warning,
                   Message = "Failed to get remote certificate.")]
    public static partial void LogFailedToGetRemoteCertificate(this ILogger logger);

    /// <summary>
    /// Logs at warning level that a server certificate does not chain to a custom root certificate.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="subject">The subject of the server certificate.</param>
    /// <param name="chainStatus">The chain status information.</param>
    [LoggerMessage(EventId = 10106, Level = LogLevel.Warning,
                   Message = "Certificate validation failed for {Subject}. Chain status: {ChainStatus}.")]
    public static partial void LogCertificateValidationFailed(this ILogger logger, string? subject, string chainStatus);
}

#endif
