using System.Security.Cryptography.X509Certificates;
using Arc4u.Dependency.Attribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Arc4u.Diagnostics;

namespace Arc4u.Security.Cryptography;

/// <summary>
/// The default <see cref="IX509CertificateLoader"/>: it searches the certificate stores of the machine or loads PEM files.
/// </summary>
[Export(typeof(IX509CertificateLoader))]
public class X509CertificateLoader : IX509CertificateLoader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="X509CertificateLoader"/> class.
    /// </summary>
    /// <param name="logger">An optional logger used to trace missing files.</param>
    public X509CertificateLoader(ILogger<X509CertificateLoader>? logger)
    {
        _logger = logger;
    }

    private readonly ILogger<X509CertificateLoader>? _logger;
    /// <summary>
    /// Find a certificate in the system: Certificate store for Windows or Keychain...
    /// </summary>
    /// <param name="find">The certificate criteria to look for...</param>
    /// <param name="findType"><see cref="X509FindType"/>, default is FindBySubjectName</param>
    /// <param name="location"><see cref="StoreLocation"/>, default is LocalMachine</param>
    /// <param name="name"><see cref="StoreName"/>, default is My</param>
    /// <returns>The <see cref="X509Certificate2"/></returns>
    /// <exception cref="KeyNotFoundException">No certificate is found!</exception>
    protected X509Certificate2 FindCertificate(string find, X509FindType findType = X509FindType.FindBySubjectName, StoreLocation location = StoreLocation.LocalMachine, StoreName name = StoreName.My)
    {
        var certificateStore = new X509Store(name, location);

        try
        {
            certificateStore.Open(OpenFlags.ReadOnly);

            var certificates = certificateStore.Certificates.Find(findType, find, false);

            return certificates.Count > 0 ? certificates[0] : throw new KeyNotFoundException("No certificate found for the given criteria.");
        }
        finally
        {
            certificateStore.Close();
        }
    }

    /// <summary>
    /// Just a wrapper class to encapsultate the search of a certificate in the system based on the OS.
    /// </summary>
    /// <param name="certificateInfo"></param>
    /// <returns></returns>
    public X509Certificate2 FindCertificate(CertificateInfo certificateInfo)
    {
        if (certificateInfo.Name is null)
        {
            throw new InvalidOperationException("Certificate name cannot be null.");
        }

        var certificate = FindCertificate(
                            certificateInfo.Name,
                            certificateInfo.FindType,
                            certificateInfo.Location,
                            certificateInfo.StoreName);

        return certificate;
    }

    /// <summary>
    /// Read the current section and identify if the section contains a CertificateStore entry.
    /// If yes, the Certificate will be retrieve based on the <see cref="CertificateInfo"/> object.
    /// If no CertificateStore section exists, the File is checked and a certificate will be created bqsed
    /// on the pem files (private and public keys).
    /// </summary>
    /// <param name="configuration"><see cref="IConfiguration"/></param>
    /// <param name="sectionName">The name of the section if the <see cref="IConfiguration"/></param>
    /// <returns></returns>
    public X509Certificate2? FindCertificate(IConfiguration configuration, string sectionName)
    {
        var certificate = configuration.GetSection(sectionName).Get<CertificateStoreOrFileInfo>();

        return this.FindCertificate(certificate);
    }

    /// <inheritdoc/>
    /// <exception cref="ArgumentNullException"><paramref name="certificateFilePathInfo"/> is <see langword="null"/>.</exception>
    /// <exception cref="FileNotFoundException">The public certificate file or the private key file does not exist.</exception>
    public X509Certificate2 FindCertificate(CertificateFilePathInfo? certificateFilePathInfo)
    {
        ArgumentNullException.ThrowIfNull(certificateFilePathInfo);

        if (!File.Exists(certificateFilePathInfo.Cert))
        {
            _logger?.Technical().LogError($"Public key file doesn't exist.");
            throw new FileNotFoundException("Public key file doesn't exist.", certificateFilePathInfo.Cert);
        }

        if (!File.Exists(certificateFilePathInfo.Key))
        {
            _logger?.Technical().LogError($"Private key file doesn't exist.");
            throw new FileNotFoundException("Private key file doesn't exist.", certificateFilePathInfo.Key);
        }

        return X509Certificate2.CreateFromPemFile(certificateFilePathInfo.Cert, certificateFilePathInfo.Key);
    }
}
