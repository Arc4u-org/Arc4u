using System.Security.Cryptography.X509Certificates;

namespace Arc4u.gRPC.ChannelCertificate;

/// <summary>
/// Retrieves the certificate a server presents during the TLS handshake.
/// </summary>
public interface IRootCertificateExtractor
{
    /// <summary>
    /// Fetches the server certificate of an https endpoint.
    /// </summary>
    /// <param name="rootUrl">The https URL of the server.</param>
    /// <returns>The certificate, or <see langword="null"/> when it could not be obtained.</returns>
    X509Certificate2? FetchCertificateFor(Uri rootUrl);
}
