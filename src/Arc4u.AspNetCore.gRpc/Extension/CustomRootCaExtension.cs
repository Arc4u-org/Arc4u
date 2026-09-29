#if NET10_0_OR_GREATER

using System.Security.Cryptography.X509Certificates;
using Arc4u.Diagnostics;
using Arc4u.Security.Cryptography;
using Arc4u.Security.CustomRootCA;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.AspNetCore.gRpc;

/// <summary>
/// Extension for an <see cref="IHttpClientBuilder"/> that trusts private root certificates, for a gRPC client calling servers signed by an internal CA.
/// </summary>
public static class CustomRootCaExtension
{
    extension(IHttpClientBuilder builder)
    {
        /// <summary>
        /// Configures the primary <see cref="SocketsHttpHandler"/> of the client so server certificates are also accepted when they chain to a configured custom root certificate.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The root certificates come from the <c>CARootOption</c> named <paramref name="certificateOptionKey"/>, or from all the registered custom root CA option keys when it is null or white space.
        /// They are loaded once, when the handler is created, through the registered <c>IX509CertificateLoader</c>.
        /// </para>
        /// <para>
        /// The custom trust is only consulted when the default validation reports an error; the revocation check is disabled for that validation.
        /// When no certificate can be loaded, a warning is logged and the default validation is kept.
        /// </para>
        /// </remarks>
        /// <param name="certificateOptionKey">The name of the CA root option to use; <see langword="null"/> to use all the registered ones.</param>
        /// <returns>The <see cref="IHttpClientBuilder"/>, to chain calls.</returns>
        /// <example>
        /// <code>
        /// using Arc4u.AspNetCore.gRpc;
        ///
        /// services.AddHttpClient("grpc").ConfigureLocalCaCertificateForGrpc("InternalCA");
        /// </code>
        /// </example>
        public IHttpClientBuilder ConfigureLocalCaCertificateForGrpc(string? certificateOptionKey = null)
        {
            return builder.ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var handler = new SocketsHttpHandler();
                var logger = sp.GetRequiredService<ILogger<SocketsHttpHandler>>();
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
                        logger.Technical().LogNoValidCACertificatesLoaded();
                    }
                }
                catch (Exception ex)
                {
                    logger.Technical().LogFailedToLoadCACertificate(ex, certificateOptionKey);
                }

                // Only configure callback if we have certificates (OPTIMIZATION)
                if (customRootCertificates.Count > 0)
                {
                    handler.SslOptions.RemoteCertificateValidationCallback = (_, cert, chain, sslPolicyErrors) =>
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

                        // Cast to X509Certificate2 (RemoteCertificateValidationCallback receives X509Certificate)
                        var cert2 = cert as X509Certificate2 ?? new X509Certificate2(cert);
                        var isValid = chain.Build(cert2);

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
