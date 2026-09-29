using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;

namespace Arc4u.Security.Cryptography;
/// <summary>
/// Loads X.509 certificates from a certificate store, from PEM files or from a configuration section.
/// </summary>
public interface IX509CertificateLoader
{
    /// <summary>
    /// Finds a certificate in a certificate store.
    /// </summary>
    /// <param name="certificateInfo">The description of the certificate to look for.</param>
    /// <returns>The certificate.</returns>
    public X509Certificate2 FindCertificate(CertificateInfo certificateInfo);

    /// <summary>
    /// Creates a certificate from PEM files.
    /// </summary>
    /// <param name="certificateInfo">The paths of the public certificate and of the private key.</param>
    /// <returns>The certificate, with its private key.</returns>
    public X509Certificate2 FindCertificate(CertificateFilePathInfo? certificateInfo);

    /// <summary>
    /// Reads a configuration section (see <see cref="CertificateStoreOrFileInfo"/>) and loads the certificate it describes.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The name of the section that describes the certificate.</param>
    /// <returns>The certificate, or <see langword="null"/> when the section does not exist.</returns>
    public X509Certificate2? FindCertificate(IConfiguration configuration, string sectionName);
}

/// <summary>
/// Extension methods for <see cref="IX509CertificateLoader"/>.
/// </summary>
public static class IX509CertificateLoaderExtensionMethods
{
    /// <summary>
    /// Loads the certificate described by a <see cref="CertificateStoreOrFileInfo"/>.
    /// </summary>
    /// <param name="x509CertificateLoader">The loader.</param>
    /// <param name="certificateInfo">The description of the certificate. The store is used first, then the files.</param>
    /// <returns>The certificate, or <see langword="null"/> when <paramref name="certificateInfo"/> is <see langword="null"/>.</returns>
    /// <exception cref="InvalidOperationException">Neither a store nor files are described.</exception>
    public static X509Certificate2? FindCertificate(this IX509CertificateLoader x509CertificateLoader, CertificateStoreOrFileInfo? certificateInfo)
    {
        // For this configuration, no decryption exists. Simply skip this provider.
        if (certificateInfo is null)
        {
            return null;
        }

        if (certificateInfo.Store is not null)
        {
            return x509CertificateLoader.FindCertificate(certificateInfo.Store);
        }
        else
        {
            if (certificateInfo.File is null)
            {
                throw new InvalidOperationException("No certificate information found in the configuration.");
            }
            return x509CertificateLoader.FindCertificate(certificateInfo.File);
        }
    }
}
