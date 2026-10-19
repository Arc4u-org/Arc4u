using Microsoft.Extensions.Configuration;

namespace Arc4u.Configuration.Decryptor;

/// <summary>
/// Extension methods on <see cref="IConfigurationBuilder"/> that add a provider decrypting the values of the previously added providers.
/// A value is decrypted when it starts with the prefix (<c>Decrypt:</c> by default); the decrypted values are exposed by the added provider under the same keys.
/// The decryption provider must therefore be added after the providers holding the encrypted values and the key material.
/// </summary>
/// <example>
/// <code language="csharp">
/// var builder = new ConfigurationBuilder()
///     .AddJsonFile("appsettings.json")
///     .AddCertificateDecryptorConfiguration();
///
/// // appsettings.json contains the certificate description and the encrypted value:
/// // "EncryptionCertificate": { "Store": { "Name": "MyCertificate", "Location": "LocalMachine" } },
/// // "ConnectionStrings": { "Db": "Decrypt:AbC..." }
/// var configuration = builder.Build();
/// </code>
/// </example>
public static class SecretDecryptorConfiguratorExtensions
{
    /// <summary>
    /// Adds a provider that decrypts, with a certificate, the values starting with the default prefix.
    /// The certificate is described by the default <c>EncryptionCertificate</c> section found in the previously added providers.
    /// </summary>
    /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddCertificateDecryptorConfiguration(this IConfigurationBuilder configurationBuilder)
    {
        PreviousProviders.Capture(configurationBuilder);
        configurationBuilder.Add(new SecretCertificateConfigurationSource(new SecretCertificateOptions()));
        return configurationBuilder;
    }

    /// <summary>
    /// Adds a provider that decrypts, with a certificate, the values starting with the configured prefix.
    /// </summary>
    /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="options">An action that configures the <see cref="SecretCertificateOptions"/>.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddCertificateDecryptorConfiguration(this IConfigurationBuilder configurationBuilder, Action<SecretCertificateOptions> options)
    {
        var config = new SecretCertificateOptions();
        options(config);

        PreviousProviders.Capture(configurationBuilder);
        configurationBuilder.Add(new SecretCertificateConfigurationSource(config));
        return configurationBuilder;
    }

    /// <summary>
    /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values based on the default values defined in the <see cref="SecretRijndaelOptions"/>.
    /// </summary>
    /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddRijndaelDecryptorConfiguration(this IConfigurationBuilder configurationBuilder)
    {
        PreviousProviders.Capture(configurationBuilder);
        configurationBuilder.Add(new SecretRijndaelConfigurationSource(new SecretRijndaelOptions()));
        return configurationBuilder;
    }

    /// <summary>
    /// Adds an <see cref="IConfigurationProvider"/> that reads configuration values based on the values defined in the <see cref="SecretRijndaelOptions"/>.
    /// </summary>
    /// <param name="configurationBuilder">The <see cref="IConfigurationBuilder"/> to add to.</param>
    /// <param name="options">The <see cref="SecretRijndaelOptions"/> parameters.</param>
    /// <returns>The <see cref="IConfigurationBuilder"/>.</returns>
    public static IConfigurationBuilder AddRijndaelDecryptorConfiguration(this IConfigurationBuilder configurationBuilder, Action<SecretRijndaelOptions> options)
    {
        var config = new SecretRijndaelOptions();
        options(config);

        PreviousProviders.Capture(configurationBuilder);
        configurationBuilder.Add(new SecretRijndaelConfigurationSource(config));
        return configurationBuilder;
    }
}
