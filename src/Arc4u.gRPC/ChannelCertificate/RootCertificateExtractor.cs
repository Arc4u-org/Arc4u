using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.OAuth2;
using Microsoft.Extensions.Logging;

namespace Arc4u.gRPC.ChannelCertificate;

/// <summary>
/// Default <see cref="IRootCertificateExtractor"/>: sends a request to the server and captures the certificate in the TLS validation callback.
/// </summary>
/// <remarks>
/// The certificate is only captured when the connection is negotiated: as the underlying <see cref="HttpClient"/> reuses its connections,
/// a later call for the same server may not trigger the callback and return <see langword="null"/>.
/// The extractor owns an <see cref="HttpClient"/> that is released by <see cref="Dispose"/>.
/// </remarks>
[Export(typeof(IRootCertificateExtractor))]
public class RootCertificateExtractor : IRootCertificateExtractor, IDisposable
{
    /// <summary>
    /// Creates the extractor.
    /// </summary>
    /// <param name="logger">The logger for technical messages.</param>
    public RootCertificateExtractor(ILogger<RootCertificateExtractor> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, ServerCertificateCustomValidationCallback = ServerCertificateValidationCallback });
    }

    private readonly ILogger<RootCertificateExtractor> _logger;
    private readonly HttpClient _httpClient;
    private static readonly HttpRequestOptionsKey<CertificateHolder> _key = new(nameof(CertificateHolder));

    private sealed class CertificateHolder
    {
        public X509Certificate2? Certificate { get; set; }
    }

    /// <summary>
    /// Sends a GET request to the URL and returns the server certificate captured during the handshake.
    /// </summary>
    /// <remarks>Any failure of the request is logged and results in <see langword="null"/>. The certificate is returned only when the server certificate is valid for the platform trust (no SSL policy error).</remarks>
    /// <param name="rootUrl">The https URL of the server.</param>
    /// <returns>The certificate, or <see langword="null"/> when it could not be obtained.</returns>
    /// <exception cref="ArgumentException">The scheme of <paramref name="rootUrl"/> is not https.</exception>
    public X509Certificate2? FetchCertificateFor(Uri rootUrl)
    {
        if (!rootUrl.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Secure https protocol is expected.");
        }

        try
        {
            var certificateHolder = new CertificateHolder();
            using var request = new HttpRequestMessage(HttpMethod.Get, rootUrl);

            request.Options.Set(_key, certificateHolder);
            using var response = _httpClient.Send(request);
            // Note that this will oonly work once: next calls will not trigger ServerCertificateCustomValidationCallback because _httpClient will cache the response.
            return certificateHolder.Certificate;
        }
        catch (Exception ex)
        {
            _logger.Technical().LogException(ex);
            return null;
        }
    }

    private bool ServerCertificateValidationCallback(HttpRequestMessage sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslPolicyErrors)
    {
        if (certificate is not null)
        {
            _logger.Technical().LogCertificateFeedBackReceived(certificate.Subject);

            if (sender.Options.TryGetValue(_key, out var certificateHolder))
            {
                certificateHolder.Certificate = new X509Certificate2(certificate);
            }
        }
        else
        {
            _logger.Technical().LogCertificateFeedBackWithNoCertificate();
        }

        return sslPolicyErrors == SslPolicyErrors.None;
    }

    /// <summary>
    /// Releases the <see cref="HttpClient"/> used to contact the servers.
    /// </summary>
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
