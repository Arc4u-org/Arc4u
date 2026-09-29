using Microsoft.Extensions.Configuration;

namespace Arc4u.Configuration.Decryptor;

/// <summary>
/// Represent the <see cref="IConfigurationProvider"/> collection as an <see cref="IConfigurationSource"/> for decryption based on the Rijndael algorythm.
/// </summary>
public class SecretRijndaelConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// The default prefix (<c>Decrypt:</c>) that marks a configuration value as encrypted.
    /// </summary>
    public const string PrefixDefault = "Decrypt:";
    /// <summary>
    /// The default name (<c>EncryptionRijndael</c>) of the configuration section that contains the Rijndael <c>Key</c> and <c>IV</c> (both base64 encoded).
    /// </summary>
    public const string SecretSectionNameDefault = "EncryptionRijndael";

    /// <summary>
    /// Create a <see cref="IConfigurationSource"/> using the defaults.
    /// The Rijndael configuration is fetched from the previous providers.
    /// </summary>
    /// <param name="options"></param>
    public SecretRijndaelConfigurationSource(SecretRijndaelOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = new SecretRijndaelOptions
        {
            Prefix = options.Prefix ?? PrefixDefault,
            SecretSectionName = options.SecretSectionName ?? SecretSectionNameDefault,
            RijnDael = options.RijnDael,
        };
    }

    private readonly SecretRijndaelOptions _options;

    /// <summary>
    /// Builds the <see cref="SecretRijndaelConfigurationProvider"/> for this source.
    /// </summary>
    /// <param name="builder">The <see cref="IConfigurationBuilder"/>.</param>
    /// <returns>A <see cref="SecretRijndaelConfigurationProvider"/></returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new SecretRijndaelConfigurationProvider(_options, GetSources(builder));
    }

    private IList<IConfigurationSource> GetSources(IConfigurationBuilder builder)
    {
        var sources = builder.Sources.ToList();
        var index = sources.IndexOf(this);
        return sources.Take(index).Where(s => s.GetType() != GetType()).ToList();
    }
}
