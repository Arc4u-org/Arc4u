#if NET10_0_OR_GREATER

using System.Security.Cryptography.X509Certificates;
using Arc4u.Diagnostics;
using Arc4u.Security.Cryptography;
using Arc4u.Security.CustomRootCA;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Extensions;

/// <summary>Extensions on <see cref="IHttpClientBuilder"/> to trust custom root certificate authorities.</summary>
public static class CustomRootCaExtension
{
    /// <summary>Extensions on the <see cref="IHttpClientBuilder"/> <paramref name="builder"/>.</summary>
    /// <param name="builder">The HTTP client builder.</param>
    extension(IHttpClientBuilder builder)
    {
        /// <summary>
        /// Configures the primary handler of the HTTP client to validate server certificates against the custom root CA certificates
        /// registered as <c>CARootOption</c> options. When the standard validation fails, the chain is rebuilt with the custom certificates as the only trust store (revocation is not checked).
        /// </summary>
        /// <param name="certificateOptionKey">The name of the <c>CARootOption</c> to use, or <see langword="null"/> to use all the registered root CA options.</param>
        /// <returns>The builder, to chain calls.</returns>
        public IHttpClientBuilder ConfigureLocalCaCertificate(string? certificateOptionKey = null)
        {
            return builder.ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var handler = new HttpClientHandler();
                var logger = sp.GetRequiredService<ILogger<HttpClientHandler>>();
                var x509Loader = sp.GetRequiredService<IX509CertificateLoader>();

                var options = new List<CARootOption>();
                var caRootOptions = sp.GetRequiredService<IOptionsMonitor<CARootOption>>();
                if (!string.IsNullOrWhiteSpace(certificateOptionKey))
                {
                    logger.Technical().LogUsingCustomRootCertificateOptionKey(certificateOptionKey);
                    options.Add(caRootOptions.Get(certificateOptionKey));
                }
                else
                {
                    options.AddRange(CustomRootCaExtensions.CertificateOptionKeys.Select(key => caRootOptions.Get(key)));
                }

                // Load certificates once during handler creation (OPTIMIZATION)
                var customRootCertificates = new List<X509Certificate2>();
                try
                {
                    foreach (var caOption in options)
                    {
                        var certificate = caOption.GetCertificate(x509Loader);
                        if (certificate is not null)
                        {
                            logger.Technical().LogLoadedCustomRootCertificate(certificate.FriendlyName ?? certificate.Subject);
                            customRootCertificates.Add(certificate);
                        }
                    }

                    if (customRootCertificates.Count == 0)
                    {
                        logger.Technical().LogNoValidCaCertificatesLoaded();
                    }
                }
                catch (Exception ex)
                {
                    logger.Technical().LogFailedToLoadCaCertificate(ex, certificateOptionKey);
                }

                // Only configure callback if we have certificates (OPTIMIZATION)
                if (customRootCertificates.Count > 0)
                {
                    handler.ServerCertificateCustomValidationCallback = (_, cert, chain, sslPolicyErrors) =>
                    {
                        // If no errors, accept immediately
                        if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                            return true;

                        if (chain is null)
                        {
                            logger.Technical().LogFailedToBuildCertificateChain();
                            return false;
                        }

                        if (cert is null)
                        {
                            logger.Technical().LogFailedToGetRemoteCertificate();
                            return false;
                        }
                        // Configure chain to use custom root trust
                        chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                        foreach (var certificate in customRootCertificates)
                        {
                            chain.ChainPolicy.CustomTrustStore.Add(certificate);
                        }

                        var isValid = chain.Build(cert!);

                        if (!isValid)
                        {
                            logger.Technical().LogCertificateValidationFailed(
                                cert?.Subject,
                                string.Join(", ", chain.ChainStatus.Select(s => s.StatusInformation)));
                        }

                        return isValid;
                    };
                }

                return handler;
            });
        }
    }
}

#endif
