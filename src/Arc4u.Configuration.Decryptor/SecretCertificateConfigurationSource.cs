using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using X509CertificateLoader = Arc4u.Security.Cryptography.X509CertificateLoader;

namespace Arc4u.Configuration.Decryptor;

/// <summary>
/// Represent the <see cref="IConfigurationProvider"/> collection as an <see cref="IConfigurationSource"/> for decryption based on an <see cref="X509Certificate2"/>.
/// </summary>
public class SecretCertificateConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// The default prefix (<c>Decrypt:</c>) that marks a configuration value as encrypted.
    /// </summary>
    public const string PrefixDefault = "Decrypt:";
    /// <summary>
    /// The default name (<c>EncryptionCertificate</c>) of the configuration section that describes the certificate used to decrypt the values.
    /// </summary>
    public const string SecretSectionNameDefault = "EncryptionCertificate";

    /// <summary>
    /// Configure the souce with the specific options.
    /// </summary>
    /// <param name="options"><see cref="SecretCertificateOptions"/></param>
    public SecretCertificateConfigurationSource(SecretCertificateOptions options)
    {
        _options = new SecretCertificateOptions
        {
            Prefix = options.Prefix ?? PrefixDefault,
            SecretSectionName = options.SecretSectionName ?? SecretSectionNameDefault,
            CertificateLoader = options.CertificateLoader ?? (options.Certificate is null ? new X509CertificateLoader(null) : null),
            Certificate = options.Certificate,
        };
    }

    private readonly SecretCertificateOptions _options;

    /// <summary>
    /// Builds the <see cref="IConfigurationProvider"/> for this source.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <returns>A <see cref="IConfigurationProvider"/></returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SecretConfigurationCertificateProvider(_options, GetSources(builder));
    }

    private IList<IConfigurationSource> GetSources(IConfigurationBuilder builder)
    {
        var sources = builder.Sources.ToList();
        var index = sources.IndexOf(this);
        return sources.Take(index).Where(s => s.GetType() != GetType()).ToList();
    }
}
