using System.Security.Cryptography.X509Certificates;
using System.Text;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arc4u.gRPC.ChannelCertificate;

/// <summary>
/// Provides, and caches by host name, the PEM encoded certificate of gRPC servers, typically to trust a private root certificate on a channel.
/// </summary>
/// <remarks>Registered as a shared (singleton) service through the <c>Export</c> attribute. The certificate is fetched with an <see cref="IRootCertificateExtractor"/>.</remarks>
[Export, Shared]
public class RootPemCertificates
{
    /// <summary>
    /// Creates the collection.
    /// </summary>
    /// <param name="certificateExtractor">The service used to fetch server certificates.</param>
    /// <param name="logger">The logger for technical messages.</param>
    public RootPemCertificates(IRootCertificateExtractor certificateExtractor, ILogger<RootPemCertificates> logger)
    {
        _certificateExtractor = certificateExtractor;
        _pemsCollections = new Dictionary<string, string>();
        _logger = logger;
    }

    readonly IRootCertificateExtractor _certificateExtractor;
    readonly ILogger<RootPemCertificates> _logger;
    static readonly object _lock = new();

    /// <summary>
    /// Gets the PEM certificates already retrieved, keyed by host name.
    /// </summary>
    public IReadOnlyDictionary<string, string> Pems => _pemsCollections;

    private readonly Dictionary<string, string> _pemsCollections;

    /// <summary>
    /// Gets the PEM encoded certificate of the server behind <paramref name="rootUri"/>, fetching and caching it by host on first use.
    /// </summary>
    /// <param name="rootUri">The https URL of the server.</param>
    /// <returns>The certificate in PEM format.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rootUri"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">No usable certificate could be obtained for the URI (the cause is logged).</exception>
    public string GetPemFor(Uri rootUri)
    {
        ArgumentNullException.ThrowIfNull(rootUri);

        if (_pemsCollections.TryGetValue(rootUri.Host, out var pem))
        {
            return pem;
        }

        try
        {
            var certificate = _certificateExtractor.FetchCertificateFor(rootUri);

            if (certificate is null)
            {
                _logger.Technical().LogCertificateUriError(rootUri.ToString());
                throw new KeyNotFoundException(rootUri.ToString());
            }

            _logger.Technical().LogCertificateUsed(certificate.Subject);

            pem = ExportToPem(certificate);

            if (string.IsNullOrWhiteSpace(pem))
            {
                _logger.Technical().LogEmptyPemCertificate(certificate.Subject);
                throw new KeyNotFoundException(rootUri.ToString());
            }

            lock (_lock)
            {
                _pemsCollections.Add(rootUri.Host, pem);
            }

            return pem;
        }
        catch (Exception ex)
        {
            _logger.Technical().LogException(ex);

            throw new KeyNotFoundException(rootUri.ToString());
        }
    }

    /// <summary>
    /// Export a certificate to a PEM format string
    /// </summary>
    /// <param name="cert">The certificate to export</param>
    /// <returns>A PEM encoded string</returns>
    public static string ExportToPem(X509Certificate2 cert)
    {
        var builder = new StringBuilder();

        try
        {
            builder.AppendLine("-----BEGIN CERTIFICATE-----");
            builder.AppendLine(Convert.ToBase64String(cert.Export(X509ContentType.Cert), Base64FormattingOptions.InsertLineBreaks));
            builder.AppendLine("-----END CERTIFICATE-----");

        }
        catch (Exception)
        {
        }

        return builder.ToString();
    }
}
