using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Arc4u.Configuration;
using Arc4u.OAuth2.DataProtection;
using Arc4u.OAuth2.Events;
using Arc4u.OAuth2.Middleware;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.TicketStore;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using X509CertificateLoader = Arc4u.Security.Cryptography.X509CertificateLoader;
using System.Linq;

namespace Arc4u.OAuth2.Extensions
{
    /// <summary>
    /// Extension methods that register the Arc4u authentication schemes: OpenID Connect with cookies
    /// (<c>AddOidcAuthentication</c>), JWT bearer (<c>AddJwtAuthentication</c>) and both together (<c>AddHybridAuthentication</c>).
    /// </summary>
    public static partial class AuthenticationExtensions
    {
        /// <summary>
        /// Registers the OpenID Connect authentication with cookies, from code.
        /// The default authenticate scheme is the <c>Arc4uScheme</c> policy scheme, which always forwards to OpenID Connect, and the user is signed in with a cookie.
        /// The ticket store (see <see cref="OidcAuthenticationOptions.AuthenticationCacheTicketStoreOption"/>), the data protection keys, the claim identifiers, the default authority and the OpenID settings are registered too.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="authenticationOptions">The action that configures the <see cref="OidcAuthenticationOptions"/>. <see cref="OidcAuthenticationOptions.OpenIdSettingsOptions"/>, <see cref="OidcAuthenticationOptions.DataProtectionCertificate"/> and <see cref="OidcAuthenticationOptions.DataProtectionCacheStoreOption"/> are required.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>, to chain calls.</returns>
        /// <exception cref="ArgumentNullException">An argument, <see cref="OidcAuthenticationOptions.OpenIdSettingsOptions"/> or <see cref="OidcAuthenticationOptions.DataProtectionCertificate"/> is <see langword="null"/>.</exception>
        /// <exception cref="NullReferenceException"><see cref="OidcAuthenticationOptions.DefaultAuthority"/> or <see cref="OidcAuthenticationOptions.DataProtectionCacheStoreOption"/> is <see langword="null"/>.</exception>
        /// <example>
        /// <code language="csharp">
        /// builder.Services.AddOidcAuthentication(options =>
        /// {
        ///     options.DefaultAuthority = new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null);
        ///     options.CookieName = ".MyApp.Cookies";
        ///     options.ApplicationName = "MyApp";
        ///     options.DataProtectionCertificate = certificate;
        ///     options.DataProtectionCacheStoreOption = o => { o.CacheKey = "DataProtection"; o.CacheName = "Default"; };
        ///     options.ClaimsIdentifierOptions = o => o.Add("oid");
        ///     options.OpenIdSettingsOptions = o =>
        ///     {
        ///         o.ClientId = "my-client-id";
        ///         o.ClientSecret = "my-client-secret";
        ///         o.Audiences.Add("my-client-id");
        ///         o.Scopes.Add("openid");
        ///     };
        /// });
        /// </code>
        /// </example>
        public static AuthenticationBuilder AddOidcAuthentication(this IServiceCollection services,
            Action<OidcAuthenticationOptions> authenticationOptions)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(authenticationOptions);

            var oidcOptions = new OidcAuthenticationOptions();
            authenticationOptions(oidcOptions);

            ValidateOidcOptions(oidcOptions);

            services.Configure(authenticationOptions);
            var openIdOptions = ConfigureOidcServices(services, oidcOptions, out var securityKey);

            var authenticationBuilder = services.AddAuthentication(auth =>
                {
                    auth.DefaultAuthenticateScheme = Arc4u.OAuth2.Constants.ChallengePolicyScheme;
                    auth.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                    auth.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddPolicyScheme(Arc4u.OAuth2.Constants.ChallengePolicyScheme, "Authorization OIDC", options =>
                {
                    options.ForwardDefaultSelector = context => OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme,
                    options =>
                    {
                        ConfigureOpenIdConnectOptions(services, options, oidcOptions, openIdOptions, securityKey);
                    }).AddCookie();

            services.AddAuthenticationApiContext(_ => { });

            return authenticationBuilder;
        }

        private static OpenIdSettingsOption ConfigureOidcServices(IServiceCollection services,
            OidcAuthenticationOptions oidcOptions, out SecurityKey? securityKey)
        {
            if (oidcOptions.AuthenticationCacheTicketStoreOption is not null)
            {
                services.AddCacheTicketStore(oidcOptions.AuthenticationCacheTicketStoreOption);
                services.TryAddSingleton(typeof(IPostConfigureOptions<CookieAuthenticationOptions>),
                    typeof(ConfigureCookieWithTicketStoreAuthenticationOptions));
            }
            else
            {
                services.TryAddSingleton(typeof(IPostConfigureOptions<CookieAuthenticationOptions>),
                    typeof(ConfigureStandardCookieAuthenticationOptions));
            }

            services.AddDataProtection()
                .PersistKeysToCache(oidcOptions.DataProtectionCacheStoreOption)
                .ProtectKeysWithCertificate(oidcOptions.DataProtectionCertificate)
                .SetApplicationName(oidcOptions.ApplicationName)
                .SetDefaultKeyLifetime(oidcOptions.DefaultKeyLifetime);

            services.AddClaimsIdentifier(oidcOptions.ClaimsIdentifierOptions);
            services.AddScoped<TokenRefreshInfo>();
            services.AddAuthorizationCore();
            services.AddHttpContextAccessor();
            services.TryAddTransient<CookieAuthenticationEvents, StandardCookieEvents>();
            services.TryAddTransient<OpenIdConnectEvents, StandardOpenIdConnectEvents>();

            services.AddDefaultAuthority(options =>
            {
                options.SetData(oidcOptions.DefaultAuthority.Url, oidcOptions.DefaultAuthority.TokenEndpoint,
                    oidcOptions.DefaultAuthority.Issuer, oidcOptions.DefaultAuthority.MetaDataAddress);
            });

            services.ConfigureOpenIdSettings(oidcOptions.OpenIdSettingsOptions);

            var openIdOptions = new OpenIdSettingsOption();
            oidcOptions.OpenIdSettingsOptions(openIdOptions);

            securityKey = oidcOptions.CertSecurityKey is not null
                ? new X509SecurityKey(oidcOptions.CertSecurityKey)
                : null;

            return openIdOptions;
        }

        /// <summary>
        /// Registers the OpenID Connect authentication with cookies from the configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="authenticationSectionName">The section bound to <see cref="OidcAuthenticationSectionOptions"/>. The default is <c>Authentication</c>.</param>
        /// <param name="certificateLoader">The loader of the certificates. When <see langword="null"/>, a default <c>X509CertificateLoader</c> is used.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>, to chain calls.</returns>
        /// <exception cref="ConfigurationException">The section does not exist or a mandatory value (default authority, cookie name, claim types, section paths, response type, authentication method) is missing.</exception>
        /// <exception cref="MissingFieldException">A certificate cannot be found from its configuration section, or the OpenID settings have no client id, no scope, or no audience while <c>ValidateAudience</c> is <see langword="true"/>.</exception>
        /// <remarks>
        /// <para>
        /// Besides the authentication schemes, the method registers, from the sections named by <see cref="OidcAuthenticationSectionOptions"/>: the OpenID settings, the domain mappings, the token cache, the claims filler,
        /// the on-behalf-of settings (<c>Authentication:OnBehalfOf</c>), the claim identifiers, the data protection keys (protected by the certificate of <c>Authentication:DataProtection:EncryptionCertificate</c> and stored in the cache described by
        /// <c>Authentication:DataProtection:CacheStore</c>), the ticket store (<c>Authentication:AuthenticationCacheTicketStore</c>) and the bearer injector.
        /// </para>
        /// <para>A minimal configuration:</para>
        /// <code language="json">
        /// {
        ///   "Application.configuration": { "ApplicationName": "MyApp" },
        ///   "Authentication": {
        ///     "DefaultAuthority": { "Url": "https://login.example.com/tenant/v2.0" },
        ///     "CookieName": ".MyApp.Cookies",
        ///     "OpenId.Settings": {
        ///       "ClientId": "my-client-id",
        ///       "ClientSecret": "my-client-secret",
        ///       "Audiences": [ "my-client-id" ],
        ///       "Scopes": [ "openid", "profile" ]
        ///     },
        ///     "DataProtection": {
        ///       "EncryptionCertificate": { "Store": { "Name": "MyAppCertificate" } },
        ///       "CacheStore": { "CacheKey": "DataProtection", "CacheName": "Default" }
        ///     },
        ///     "TokenCache": { "CacheName": "Default" }
        ///   }
        /// }
        /// </code>
        /// </remarks>
        /// <example>
        /// <code language="csharp">
        /// builder.Services.AddOidcAuthentication(builder.Configuration);
        /// // ...
        /// app.UseAuthentication();
        /// app.UseAuthorization();
        /// </code>
        /// </example>
        public static AuthenticationBuilder AddOidcAuthentication(this IServiceCollection services,
            IConfiguration configuration, [DisallowNull] string authenticationSectionName = "Authentication",
            IX509CertificateLoader? certificateLoader = null)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(authenticationSectionName);

            var section = configuration.GetSection(authenticationSectionName);

            if (!section.Exists())
            {
                throw new ConfigurationException(
                    $"No section exists with name {authenticationSectionName} in the configuration providers for OpenId Connect authentication.");
            }

            var settings = new OidcAuthenticationSectionOptions();
            section.Bind(settings);

            var configErrors = ValidateOidcConfiguration(settings);

            if (configErrors is not null)
            {
                throw new ConfigurationException(configErrors);
            }

            var (dataProtectionCertificate, certSecurityKey, ticketStoreAction)  = PrepareOidcAuthenticationDependencies(services, configuration, certificateLoader, settings);

            // Duplicate code.
            void OidcAuthenticationFiller(OidcAuthenticationOptions options)
            {
                PopulateFromSection(options, settings, configuration, dataProtectionCertificate, certSecurityKey, ticketStoreAction);
            }

            services.AddAuthenticationApiContext(configuration);

            return services.AddOidcAuthentication(OidcAuthenticationFiller);
        }

        private static (X509Certificate2? dataProtectionCertificate, X509Certificate2? certSecurityKey, Action<CacheTicketStoreOptions>? ticketStoreAction) PrepareOidcAuthenticationDependencies(IServiceCollection services, IConfiguration configuration,
            IX509CertificateLoader? certificateLoader, OidcAuthenticationSectionOptions settings)
        {
            certificateLoader ??= new X509CertificateLoader(null);
            var certSecurityKey = string.IsNullOrWhiteSpace(settings.CertSecurityKeyPath)
                ? null
                : certificateLoader.FindCertificate(configuration, settings.CertSecurityKeyPath) ??
                  throw new MissingFieldException(
                      $"No certificate was found based on the configuration section: {settings.CertSecurityKeyPath}.");

            var cert = certificateLoader.FindCertificate(configuration, settings.CertificateSectionPath) ??
                       throw new MissingFieldException(
                           $"No certificate was found based on the configuration section: {settings.CertificateSectionPath}.");

            var ticketStoreAction =
                CacheTicketStoreExtension.PrepareAction(configuration, settings.AuthenticationCacheTicketStorePath);

            services.AddDomainMapping(configuration, settings.DomainMappingsSectionPath);
            services.AddOnBehalfOf(configuration);
            services.AddTokenCache(configuration, settings.TokenCacheSectionPath);
            services.AddClaimsFiller(configuration, settings.ClaimsFillerSectionPath);
            services.AddOpenIdBearerInjector();

            return (cert, certSecurityKey, ticketStoreAction);
        }

        private static void PopulateFromSection(
            OidcAuthenticationOptions options,
            OidcAuthenticationSectionOptions settings,
            IConfiguration configuration,
            X509Certificate2? dataProtectionCertificate,
            X509Certificate2? certSecurityKey,
            Action<CacheTicketStoreOptions>? ticketStoreAction)
        {
            options.DefaultAuthority = settings.DefaultAuthority!;
            options.CookieName = settings.CookieName;
            options.AuthenticationMethod = Enum.Parse<OpenIdConnectRedirectBehavior>(settings.AuthenticationMethod);
            options.AuthenticationCacheTicketStoreOption = ticketStoreAction!;
            options.OpenIdSettingsOptions =
                OpenIdSettingsExtension.PrepareAction(configuration, settings.OpenIdSettingsSectionPath);
            options.DataProtectionCertificate = dataProtectionCertificate;
            options.CallbackPath = settings.CallbackPath;
            options.DefaultKeyLifetime = settings.DefaultKeyLifetime;
            options.ApplicationName = configuration[settings.ApplicationNameSectionPath]!;
            options.ForceRefreshTimeoutTimeSpan = settings.ForceRefreshTimeoutTimeSpan;
            options.RefreshTokenLifetime = settings.RefreshTokenLifetime;
            options.CertSecurityKey = certSecurityKey;
            options.ResponseType = settings.ResponseType;
            options.AuthenticationTicketTtl = settings.AuthenticationTicketTtl;
            options.DataProtectionCacheStoreOption =
                CacheStoreExtension.PrepareAction(configuration, settings.DataProtectionSectionPath);
            options.ClaimsIdentifierOptions =
                ClaimsidentifierExtension.PrepareAction(configuration, settings.ClaimsIdentifierSectionPath);
            options.NameClaimType = settings.NameClaimType;
            options.RoleClaimType = settings.RoleClaimType;
        }

        private static string? ValidateOidcConfiguration(OidcAuthenticationSectionOptions settings)
        {
            string? configErrors = null;

            if (settings.DefaultAuthority is null)
            {
                configErrors += "DefaultAuthority must be filled!" + System.Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(settings.CookieName))
            {
                configErrors += "We need a cookie name defined specifically for your services." +
                                System.Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(settings.NameClaimType))
            {
                configErrors += "We need a name claim type defined specifically for your services." +
                                System.Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(settings.RoleClaimType))
            {
                configErrors += "We need a role claim type defined specifically for your services." +
                                System.Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(settings.OpenIdSettingsSectionPath))
            {
                configErrors += "We need a setting section to configure the OpenId Connect." +
                                System.Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(settings.CertificateSectionPath))
            {
                configErrors +=
                    "We need a setting section to specify the certificate to protect your sensitive information." +
                    System.Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(settings.DataProtectionSectionPath))
            {
                configErrors += "We need a setting section to configure the DataProtection cache store." +
                                System.Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(settings.TokenCacheSectionPath))
            {
                configErrors += "We need a setting section to configure the TokenCacheOptions." +
                                System.Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(settings.ClaimsIdentifierSectionPath))
            {
                configErrors += "We need a setting section to specify the claims used to identify a user." +
                                System.Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(settings.ClaimsFillerSectionPath))
            {
                configErrors += "We need a setting section to configure the ClaimsFillerOptions." +
                                System.Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(settings.DomainMappingsSectionPath))
            {
                configErrors += "We need a setting section to configure the DomainsMapping." +
                                System.Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(settings.ResponseType))
            {
                configErrors += "A ResponseType is mandatory to define the OpenId Connect protocol.";
            }

            if (!new [] {nameof(OpenIdConnectRedirectBehavior.FormPost), nameof(OpenIdConnectRedirectBehavior.RedirectGet)}.AsEnumerable().Contains(settings.AuthenticationMethod, StringComparer.InvariantCulture))
            {
                configErrors += "The AuthenticationMethod should be either FormPost or RedirectGet.";
            }

            return configErrors;
        }

        private static void ValidateOidcOptions(OidcAuthenticationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options.OpenIdSettingsOptions);
            ArgumentNullException.ThrowIfNull(options.DataProtectionCertificate);
            ArgumentNullException.ThrowIfNull(options.DefaultAuthority.GetMetaDataAddress());
        }

        private static void ConfigureOpenIdConnectOptions(IServiceCollection services, OpenIdConnectOptions options,
            OidcAuthenticationOptions hybridOptions, OpenIdSettingsOption openIdOptions, SecurityKey? securityKey)
        {
            ArgumentNullException.ThrowIfNull(hybridOptions.DefaultAuthority.GetMetaDataAddress());

            options.UsePkce = true;
            options.UseTokenLifetime = false;
            options.SaveTokens = false;
            options.Authority = openIdOptions.Authority is null
                ? hybridOptions.DefaultAuthority.Url.ToString()
                : openIdOptions.Authority.Url.ToString();
            options.RequireHttpsMetadata =
                hybridOptions.DefaultAuthority.MetaDataAddress.Scheme.Equals(Uri.UriSchemeHttps,
                    StringComparison.InvariantCultureIgnoreCase);
            options.MetadataAddress = hybridOptions.DefaultAuthority.MetaDataAddress.ToString();
            options.ResponseType = hybridOptions.ResponseType;
            options.CallbackPath = hybridOptions.CallbackPath;
            options.Scope.Clear();
            openIdOptions.Scopes.ForEach(options.Scope.Add);
            options.ClientId = openIdOptions.ClientId;
            options.ClientSecret = openIdOptions.ClientSecret;
            options.GetClaimsFromUserInfoEndpoint = false;
            options.MapInboundClaims = false;
            options.TokenValidationParameters.NameClaimType = hybridOptions.NameClaimType;
            options.TokenValidationParameters.RoleClaimType = hybridOptions.RoleClaimType;
            options.TokenValidationParameters.SaveSigninToken = false;
            options.TokenValidationParameters.AuthenticationType = Constants.CookiesAuthenticationType;
            options.TokenValidationParameters.ValidateAudience = openIdOptions.ValidateAudience;
            options.TokenValidationParameters.ValidAudiences = openIdOptions.Audiences;
            if (securityKey is not null)
            {
                options.TokenValidationParameters.IssuerSigningKey = securityKey;
            }

            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.SaveTokens = true;
            options.AuthenticationMethod = hybridOptions.AuthenticationMethod;
            options.ResponseMode = OpenIdConnectResponseMode.FormPost;
            options.EventsType = typeof(OpenIdConnectEvents);
        }
    }
}
