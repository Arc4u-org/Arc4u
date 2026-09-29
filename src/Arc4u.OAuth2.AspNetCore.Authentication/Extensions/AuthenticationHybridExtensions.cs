using Arc4u.Configuration;
using Arc4u.OAuth2.Events;
using Arc4u.OAuth2.Middleware;
using Arc4u.OAuth2.Options;
using Arc4u.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Net.Http.Headers;

namespace Arc4u.OAuth2.Extensions
{
    public static partial class AuthenticationExtensions
    {
        /// <summary>
        /// Registers the hybrid authentication, from code: OpenID Connect with cookies for browser requests, and JWT bearer for requests that carry an <c>Authorization: Bearer</c> header.
        /// The <c>Arc4uScheme</c> policy scheme selects the JWT bearer scheme when the header starts with <c>Bearer </c> and the OpenID Connect scheme otherwise.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="authenticationOptions">The action that configures the <see cref="HybridAuthenticationOptions"/>. <see cref="HybridAuthenticationOptions.OAuth2SettingsOptions"/> is required in addition to the required <see cref="OidcAuthenticationOptions"/> members.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>, to chain calls.</returns>
        /// <exception cref="ArgumentNullException">An argument or a required option is <see langword="null"/>.</exception>
        public static AuthenticationBuilder AddHybridAuthentication(this IServiceCollection services,
            Action<HybridAuthenticationOptions> authenticationOptions)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(authenticationOptions);

            var oidcOptions = new HybridAuthenticationOptions();
            authenticationOptions(oidcOptions);

            ValidateHybridOptions(oidcOptions);

            services.Configure<OidcAuthenticationOptions>(options =>
            {
                options.DefaultAuthority = oidcOptions.DefaultAuthority!;
                options.CookieName = oidcOptions.CookieName;
                options.AuthenticationMethod = oidcOptions.AuthenticationMethod;
                options.AuthenticationCacheTicketStoreOption = oidcOptions.AuthenticationCacheTicketStoreOption!;
                options.OpenIdSettingsOptions = oidcOptions.OpenIdSettingsOptions!;
                options.DataProtectionCertificate = oidcOptions.DataProtectionCertificate;
                options.CallbackPath = oidcOptions.CallbackPath;
                options.DefaultKeyLifetime = oidcOptions.DefaultKeyLifetime;
                options.ApplicationName = oidcOptions.ApplicationName;
                options.ForceRefreshTimeoutTimeSpan = oidcOptions.ForceRefreshTimeoutTimeSpan;
                options.RefreshTokenLifetime = oidcOptions.RefreshTokenLifetime;
                options.CertSecurityKey = oidcOptions.CertSecurityKey;
                options.ResponseType = oidcOptions.ResponseType;
                options.AuthenticationTicketTtl = oidcOptions.AuthenticationTicketTtl;
                options.DataProtectionCacheStoreOption = oidcOptions.DataProtectionCacheStoreOption!;
                options.ClaimsIdentifierOptions = oidcOptions.ClaimsIdentifierOptions!;
                options.NameClaimType = oidcOptions.NameClaimType;
                options.RoleClaimType = oidcOptions.RoleClaimType;
            });
            var openIdOptions = ConfigureOidcServices(services, oidcOptions, out var securityKey);
            services.TryAddTransient<JwtBearerEvents, StandardBearerEvents>();

            services.ConfigureOAuth2Settings(oidcOptions.OAuth2SettingsOptions);

            var oauth2Options = new OAuth2SettingsOption();
            oidcOptions.OAuth2SettingsOptions(oauth2Options);

            var authenticationBuilder = services
                .AddAuthentication(auth =>
                {
                    auth.DefaultAuthenticateScheme = Constants.ChallengePolicyScheme;
                    auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    auth.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                })
                .AddPolicyScheme(Constants.ChallengePolicyScheme, "Authorization Bearer or OIDC", options =>
                {
                    options.ForwardDefaultSelector = context =>
                    {
                        var authHeader = context.Request.Headers[HeaderNames.Authorization].FirstOrDefault();
                        return authHeader?.StartsWith("Bearer ", StringComparison.Ordinal) == true ? JwtBearerDefaults.AuthenticationScheme : OpenIdConnectDefaults.AuthenticationScheme;
                    };
                })
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme,
                    options =>
                    {
                        ConfigureOpenIdConnectOptions(services, options, oidcOptions, openIdOptions, securityKey);
                    })
                .AddJwtBearer(option =>
                {
                    ConfigureJwtBearerOptions(services, option, oidcOptions, oauth2Options, securityKey);
                }).AddCookie();

            services.AddAuthenticationApiContext(_ => { });

            return authenticationBuilder;
        }

        /// <summary>
        /// Registers the hybrid authentication from the configuration. It does what
        /// <see cref="AddOidcAuthentication(IServiceCollection, IConfiguration, string, IX509CertificateLoader?)"/> does and, in addition, reads the JWT bearer settings from the section
        /// of <see cref="HybridAuthenticationSectionOptions.OAuth2SettingsSectionPath"/> (<c>Authentication:OAuth2.Settings</c> by default) and the optional Basic authentication settings from
        /// <see cref="HybridAuthenticationSectionOptions.BasicAuthenticationSectionPath"/> (<c>Authentication:Basic</c> by default).
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="authenticationSectionName">The section bound to <see cref="HybridAuthenticationSectionOptions"/>. The default is <c>Authentication</c>.</param>
        /// <param name="certificateLoader">The loader of the certificates. When <see langword="null"/>, a default <c>X509CertificateLoader</c> is used.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>, to chain calls.</returns>
        /// <exception cref="ConfigurationException">The section does not exist, a mandatory value is missing, or the OAuth2 settings have no audience while <c>ValidateAudience</c> is <see langword="true"/> (the default).</exception>
        /// <exception cref="MissingFieldException">A certificate cannot be found from its configuration section, or the OpenID settings have no client id, no scope, or no audience while <c>ValidateAudience</c> is <see langword="true"/>.</exception>
        /// <remarks>
        /// <para>
        /// In addition to the configuration required by <c>AddOidcAuthentication</c>, the OAuth2 settings (<c>Authentication:OAuth2.Settings</c>) are mandatory and must contain at least one audience,
        /// unless <c>ValidateAudience</c> is <see langword="false"/> in that section. A minimal configuration:
        /// </para>
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
        ///     "OAuth2.Settings": { "Audiences": [ "api://my-api" ] },
        ///     "DataProtection": {
        ///       "EncryptionCertificate": { "Store": { "Name": "MyAppCertificate" } },
        ///       "CacheStore": { "CacheKey": "DataProtection", "CacheName": "Default" }
        ///     },
        ///     "TokenCache": { "CacheName": "Default" }
        ///   }
        /// }
        /// </code>
        /// <para>
        /// Known issue: the <c>ValidateAudience</c> and <c>ValidateAuthority</c> values of <see cref="OidcAuthenticationOptions"/> are not copied by the hybrid registrations and stay <see langword="true"/>.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code language="csharp">
        /// builder.Services.AddHybridAuthentication(builder.Configuration);
        /// // ...
        /// app.UseAuthentication();
        /// app.UseAuthorization();
        /// </code>
        /// </example>
        public static AuthenticationBuilder AddHybridAuthentication(this IServiceCollection services,
            IConfiguration configuration, string authenticationSectionName = "Authentication",
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

            var settings = new HybridAuthenticationSectionOptions();
            section.Bind(settings);

            var configErrors = ValidateHybridConfiguration(settings);

            if (configErrors is not null)
            {
                throw new ConfigurationException(configErrors);
            }

            var (dataProtectionCertificate, certSecurityKey, ticketStoreAction)  = PrepareOidcAuthenticationDependencies(services, configuration, certificateLoader, settings);

            services.AddBasicAuthenticationSettings(configuration, settings.BasicAuthenticationSectionPath,
                certificateLoader, throwExceptionIfSectionDoesntExist: false);

            void HybridAuthenticationFiller(HybridAuthenticationOptions options)
            {
                options.OAuth2SettingsOptions =
                    OAuth2SettingsExtension.PrepareAction(configuration, settings.OAuth2SettingsSectionPath);

                OidcAuthenticationFiller(options);
            }

            void OidcAuthenticationFiller(OidcAuthenticationOptions options)
            {
                PopulateFromSection(options, settings, configuration, dataProtectionCertificate, certSecurityKey, ticketStoreAction);
            }

            services.AddAuthenticationApiContext(configuration);

            return services.AddHybridAuthentication(HybridAuthenticationFiller);
        }
        private static string? ValidateHybridConfiguration(HybridAuthenticationSectionOptions settings)
        {
            string? configErrors = null;

            if (string.IsNullOrWhiteSpace(settings.OAuth2SettingsSectionPath))
            {
                configErrors += "We need a setting section to configure OAuth2." + System.Environment.NewLine;
            }
            var oidcConfigErrors = ValidateOidcConfiguration(settings);

            return (configErrors is null && oidcConfigErrors is null) ? null : configErrors + oidcConfigErrors;
        }
        private static void ValidateHybridOptions(HybridAuthenticationOptions options)
        {
            ArgumentNullException.ThrowIfNull(options.OAuth2SettingsOptions);

            ValidateOidcOptions(options);
        }
    }
}
