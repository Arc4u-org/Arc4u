
using Microsoft.Extensions.Logging;

namespace Arc4u;
/// <summary>
/// Source-generated log messages used by the time zone and certificate handling of Arc4u.
/// </summary>
public static partial class LoggerMessages
{
    /// <summary>
    /// Logs that a time zone was not found.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="zone">The identifier of the time zone.</param>
    [LoggerMessage(EventId = 9100, Level = LogLevel.Warning,
                   Message = "Zone {Zone} is not found!")]
    public static partial void LogZoneNotFound(this ILogger logger, string zone);

    /// <summary>
    /// Logs (at trace level) the time zone that is about to be used.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="zone">The identifier of the time zone.</param>
    [LoggerMessage(EventId = 9101, Level = LogLevel.Trace,
                   Message = "Try to define the time zone to {Zone}.")]
    public static partial void LogTryToUseTimeZone(this ILogger logger, string zone);

    /// <summary>
    /// Logs that a certificate could not be retrieved from a URI.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="uri">The URI.</param>
    [LoggerMessage(EventId = 9102, Level = LogLevel.Error,
                   Message = "Certificate was not retrieved from the uri, {Uri}.")]
    public static partial void LogCertificateUriError(this ILogger logger, string uri);

    /// <summary>
    /// Logs (at trace level) the subject of the certificate that is used.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="certificateSubject">The subject of the certificate.</param>
    [LoggerMessage(EventId = 9103, Level = LogLevel.Trace,
                   Message = "Certificate used is {CertificateSubject}.")]
    public static partial void LogCertificateUsed(this ILogger logger, string certificateSubject);

    /// <summary>
    /// Logs that the PEM extraction of a certificate is empty.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="certificateSubject">The subject of the certificate.</param>
    [LoggerMessage(EventId = 9104, Level = LogLevel.Error,
                   Message = "Pem extraction for Certificate {CertificateSubject} is empty.")]
    public static partial void LogEmptyPemCertificate(this ILogger logger, string certificateSubject);
}
