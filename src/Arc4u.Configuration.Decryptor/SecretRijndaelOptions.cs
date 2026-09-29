using Arc4u.Security;

namespace Arc4u.Configuration.Decryptor;
/// <summary>
/// Options of the Rijndael based configuration decryption (see <see cref="SecretDecryptorConfiguratorExtensions.AddRijndaelDecryptorConfiguration(Microsoft.Extensions.Configuration.IConfigurationBuilder)"/>).
/// </summary>
public class SecretRijndaelOptions
{
    /// <summary>
    /// A prefix used to detect encrypted values in a configuration.
    /// </summary>
    public string Prefix { get; set; } = SecretRijndaelConfigurationSource.PrefixDefault;

    /// <summary>
    /// A section define in a configuration provider containing the <see cref="CertificateInfo"/> keys.
    /// </summary>
    public string SecretSectionName { get; set; } = SecretRijndaelConfigurationSource.SecretSectionNameDefault;

    /// <summary>
    /// The config used to decrypt => Key and IV.
    /// </summary>
    public CypherCodecConfig? RijnDael { get; set; }

}
