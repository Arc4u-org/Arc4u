#if NET10_0_OR_GREATER

using System.Security.Cryptography.X509Certificates;
using Arc4u.Configuration;
using Arc4u.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Security.CustomRootCA;

/// <summary>
/// Extension methods to register custom root certificate authorities (CA) that must be trusted, for example when calling a service
/// whose certificate is signed by a private CA. Each root CA is registered as a named <see cref="CARootOption"/>.
/// </summary>
public static class CustomRootCaExtensions
{
    static CustomRootCaExtensions()
    {
        CertificateOptionKeys = [];
    }

    /// <summary>
    /// Gets the names of the <see cref="CARootOption"/> registered with <c>AddCustomRootCA</c>.
    /// The names allow to enumerate the named options, which cannot otherwise be listed.
    /// </summary>
    public static List<string> CertificateOptionKeys { get; }

    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers a custom root certificate authority under a name.
        /// </summary>
        /// <param name="name">The name of the root CA.</param>
        /// <param name="configureOptions">An action that defines the CA. Exactly one of <see cref="CARootOption.CaPem"/>, <see cref="CARootOption.CaFilePath"/> or <see cref="CARootOption.Store"/> must be set.</param>
        /// <returns>The service collection, to chain calls.</returns>
        /// <exception cref="ConfigurationException">No CA source, or more than one, is defined.</exception>
        /// <example>
        /// <code language="csharp">
        /// services.AddCustomRootCA("MyCA", option => option.CaFilePath = "/certs/my-root-ca.pem");
        /// </code>
        /// </example>
        public IServiceCollection AddCustomRootCA(string name, Action<CARootOption> configureOptions)
        {
            var certificate = new CARootOption();
            configureOptions(certificate);

            if (string.IsNullOrWhiteSpace(certificate.CaFilePath) && string.IsNullOrWhiteSpace(certificate.CaPem) && certificate.Store is null)
            {
                throw new ConfigurationException($"The certificate {name} does not have a CA file or a CA PEM or a Store defined.");
            }

            // Check that the certificate has only one CA source.
            var definedSources = 0;
            if (!string.IsNullOrWhiteSpace(certificate.CaFilePath)) definedSources++;
            if (!string.IsNullOrWhiteSpace(certificate.CaPem)) definedSources++;
            if (certificate.Store is not null) definedSources++;
            if (definedSources > 1)
            {
                throw new ConfigurationException($"The certificate {name} has more than one CA source defined.");
            }

            CertificateOptionKeys.Add(name);
            services.Configure(name, configureOptions);
            return services;
        }

        /// <summary>
        /// Registers the custom root certificate authorities defined in a configuration section.
        /// The section contains one child per CA, named after the CA, with the properties of <see cref="CARootOption"/>.
        /// Nothing is registered (and no exception is thrown) when the section does not exist.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sectionName">The name of the section. The default is <c>CustomRootCA</c>.</param>
        /// <returns>The service collection, to chain calls.</returns>
        /// <exception cref="ConfigurationException">A CA defines no source, or more than one.</exception>
        /// <example>
        /// <code language="csharp">
        /// // appsettings.json:
        /// // "CustomRootCA": { "MyCA": { "CaFilePath": "/certs/my-root-ca.pem" } }
        /// services.AddCustomRootCA(configuration);
        /// </code>
        /// </example>
        public IServiceCollection AddCustomRootCA(IConfiguration configuration, string sectionName = "CustomRootCA")
        {
            var section = configuration.GetSection(sectionName);
            if (!section.Exists())
            {
                // Do not throw an exception if the section is not found.
                // So we can activate or not the feature depending on the configuration.
                return services;
            }
            // Read the collection of certificates from the configuration section
            var certificates = section.Get<Dictionary<string, CARootOption>>();

            if (certificates is null)
            {
                return services;
            }

            // Check that each certificate has 1 field.
            foreach (var certificate in certificates)
            {
                var option = new CARootOption
                {
                    CaFilePath = certificate.Value.CaFilePath,
                    CaPem = certificate.Value.CaPem,
                    Store = certificate.Value.Store
                };

                services.AddCustomRootCA(certificate.Key, FillCustomRootCa);
                continue;

                void FillCustomRootCa(CARootOption fillerOption)
                {
                    fillerOption.CaFilePath = option.CaFilePath;
                    fillerOption.CaPem = option.CaPem;
                    fillerOption.Store = option.Store;
                }
            }

            return services;
        }
    }

    extension(CARootOption option)
    {
        /// <summary>
        /// Gets the root certificate described by the option.
        /// </summary>
        /// <param name="certificateLoader">The loader used when the certificate is located in a certificate store.</param>
        /// <returns>The certificate, or <see langword="null"/> when the option defines no source.</returns>
        public X509Certificate2? GetCertificate(IX509CertificateLoader certificateLoader)
        {
            if (!string.IsNullOrWhiteSpace(option.CaPem)) return X509Certificate2.CreateFromPem(option.CaPem);

            if (!string.IsNullOrWhiteSpace(option.CaFilePath)) return X509Certificate2.CreateFromPem(File.ReadAllText(option.CaFilePath));

            return option.Store is not null ? certificateLoader.FindCertificate(option.Store) : null;
        }
    }
}

#endif
