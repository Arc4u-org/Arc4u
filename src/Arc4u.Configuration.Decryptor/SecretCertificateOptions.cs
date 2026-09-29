using System.Security.Cryptography.X509Certificates;
using Arc4u.Security.Cryptography;

namespace Arc4u.Configuration.Decryptor;
/// <summary>
/// Options of the certificate based configuration decryption (see <see cref="SecretDecryptorConfiguratorExtensions.AddCertificateDecryptorConfiguration(Microsoft.Extensions.Configuration.IConfigurationBuilder)"/>).
/// </summary>
public class SecretCertificateOptions
{
    /// <summary>
    /// A prefix used to detect encrypted values in a configuration.
    /// </summary>
    public string Prefix { get; set; } = SecretCertificateConfigurationSource.PrefixDefault;

    /// <summary>
    /// A section define in a configuration provider containing the "CertificateInfo" keys.
    /// </summary>
    public string SecretSectionName { get; set; } = SecretCertificateConfigurationSource.SecretSectionNameDefault;

    /// <summary>
    /// A certificate to use instead of reading this from a configuration provider.
    /// </summary>
    public X509Certificate2? Certificate { get; set; }

    /// <summary>
    /// Specify the CertificateLoader class to load the certificate => using the SecretSectionName.
    /// </summary>
    public IX509CertificateLoader? CertificateLoader { get; set; }
}
