using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;
using Arc4u.OAuth2.Events;
using Arc4u.OAuth2.Options;
using Arc4u.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using X509CertificateLoader = Arc4u.Security.Cryptography.X509CertificateLoader;

namespace Arc4u.OAuth2.Extensions
{
    public static partial class AuthenticationExtensions
    {
        private static void ConfigureJwtBearerOptions(IServiceCollection services, JwtBearerOptions option,
            HybridAuthenticationOptions hybridOptions, OAuth2SettingsOption oauth2Options, SecurityKey? securityKey)
        {
            ArgumentNullException.ThrowIfNull(hybridOptions.DefaultAuthority.MetaDataAddress);

            option.RequireHttpsMetadata =
                hybridOptions.DefaultAuthority.MetaDataAddress.Scheme.Equals(Uri.UriSchemeHttps,
                    StringComparison.InvariantCultureIgnoreCase);
            option.Authority = oauth2Options.Authority is null
                ? hybridOptions.DefaultAuthority.Url.ToString()
                : oauth2Options.Authority.Url.ToString();
            option.MetadataAddress = hybridOptions.DefaultAuthority.MetaDataAddress.ToString();
            option.SaveToken = true;
            option.MapInboundClaims = false;
            option.TokenValidationParameters.NameClaimType = hybridOptions.NameClaimType;
            option.TokenValidationParameters.RoleClaimType = hybridOptions.RoleClaimType;
            option.TokenValidationParameters.SaveSigninToken = false;
            option.TokenValidationParameters.AuthenticationType = Constants.BearerAuthenticationType;
            option.TokenValidationParameters.ValidateIssuer = false;
            option.TokenValidationParameters.ValidateAudience = oauth2Options.ValidateAudience;
            option.TokenValidationParameters.ValidAudiences = oauth2Options.Audiences;
            if (securityKey is not null)
            {
                option.TokenValidationParameters.IssuerSigningKey = securityKey;
            }

            option.EventsType = typeof(JwtBearerEvents);
        }


        /// <summary>
        /// This extension is used on a API only scenario.
        /// Until the yarp is there and support the Oidc scenario. We don't use this on the Yarp project.
        /// </summary>
        /// <param name="services">The collection ued to define the dependencies</param>
        /// <param name="configuration"></param>
        /// <param name="authenticationOptions">Custom options</param>
        /// <returns></returns>
        public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services,
            IConfiguration configuration, Action<JwtAuthenticationOptions> authenticationOptions)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(authenticationOptions);

            var options = new JwtAuthenticationOptions();
            authenticationOptions(options);

            if (options.OAuth2SettingsOptions is null)
            {
                throw new ArgumentNullException(nameof(authenticationOptions),
                    $"{nameof(options.OAuth2SettingsOptions)} is empty");
            }

            var oauth2Options = new OAuth2SettingsOption();
            options.OAuth2SettingsOptions(oauth2Options);

            ArgumentNullException.ThrowIfNull(options.DefaultAuthority.GetMetaDataAddress());
            ArgumentNullException.ThrowIfNull(options.DefaultAuthority.MetaDataAddress);

            services.ConfigureOAuth2Settings(options.OAuth2SettingsOptions);
            services.AddClaimsIdentifier(options.ClaimsIdentifierOptions);
            services.TryAddTransient<JwtBearerEvents, StandardBearerEvents>();
            services.AddAuthorizationCore();
            services.AddHttpContextAccessor();
            services.AddDefaultAuthority(auth =>
            {
                auth.SetData(options.DefaultAuthority.Url, options.DefaultAuthority.TokenEndpoint,
                    options.DefaultAuthority.Issuer, options.DefaultAuthority.MetaDataAddress);
            });

            var authenticationBuilder =
                services.AddAuthentication(auth =>
                    {
                        auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                        auth.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
                        auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    })
                    .AddJwtBearer(option =>
                    {
                        SecurityKey? securityKey = options.CertSecurityKey is not null
                            ? new X509SecurityKey(options.CertSecurityKey)
                            : null;

                        option.RequireHttpsMetadata =
                            options.DefaultAuthority.MetaDataAddress.Scheme.Equals(Uri.UriSchemeHttps,
                                StringComparison.InvariantCultureIgnoreCase);
                        option.Authority = oauth2Options.Authority is null
                            ? options.DefaultAuthority.Url.ToString()
                            : oauth2Options.Authority.Url.ToString();
                        option.MetadataAddress = options.DefaultAuthority.MetaDataAddress.ToString();
                        option.SaveToken = true;
                        option.MapInboundClaims = false;
                        option.TokenValidationParameters.SaveSigninToken = false;
                        option.TokenValidationParameters.AuthenticationType = Constants.BearerAuthenticationType;
                        option.TokenValidationParameters.ValidateIssuer = false;
                        option.TokenValidationParameters.ValidateAudience = oauth2Options.ValidateAudience;
                        option.TokenValidationParameters.ValidAudiences = oauth2Options.Audiences;
                        option.EventsType = typeof(JwtBearerEvents);
                        if (securityKey is not null)
                        {
                            option.TokenValidationParameters.IssuerSigningKey = securityKey;
                        }
                    });

            return authenticationBuilder;
        }

        /// <summary>
        /// Registers the JWT bearer authentication from the configuration. It is meant for an API only scenario.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="authenticationSectionName">The section bound to <see cref="JwtAuthenticationSectionOptions"/>. The default is <c>Authentication</c>.</param>
        /// <param name="certificateLoader">The loader of the certificates. When <see langword="null"/>, a default <c>X509CertificateLoader</c> is used.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>, to chain calls.</returns>
        /// <exception cref="InvalidOperationException">The section does not exist.</exception>
        /// <exception cref="MissingFieldException">The default authority or the OAuth2 settings section path is missing, or the certificate cannot be found.</exception>
        /// <remarks>
        /// <para>
        /// Besides the JWT bearer scheme, the method registers, from the sections named by <see cref="JwtAuthenticationSectionOptions"/>: the OAuth2 settings, the claim identifiers, the domain mappings, the token cache,
        /// the claims filler, the client tokens (<c>Authentication:ClientTokens</c>) and the remote secrets (<c>Authentication:RemoteSecrets</c>).
        /// </para>
        /// <para>A minimal configuration:</para>
        /// <code language="json">
        /// {
        ///   "Authentication": {
        ///     "DefaultAuthority": { "Url": "https://login.example.com/tenant/v2.0" },
        ///     "OAuth2.Settings": {
        ///       "Audiences": [ "api://my-api" ]
        ///     },
        ///     "TokenCache": { "CacheName": "Default" }
        ///   }
        /// }
        /// </code>
        /// </remarks>
        /// <example>
        /// <code language="csharp">
        /// builder.Services.AddJwtAuthentication(builder.Configuration);
        /// // ...
        /// app.UseAuthentication();
        /// app.UseAuthorization();
        /// </code>
        /// </example>
        public static AuthenticationBuilder AddJwtAuthentication(this IServiceCollection services,
            IConfiguration configuration, [DisallowNull] string authenticationSectionName = "Authentication",
            IX509CertificateLoader? certificateLoader = null)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(authenticationSectionName);

            var section = configuration.GetSection(authenticationSectionName) ??
                          throw new InvalidOperationException(
                              $"No section exists with name {authenticationSectionName} in the configuration providers for OAuth2 Connect authentication.");
            var settings = section.Get<JwtAuthenticationSectionOptions>() ??
                           throw new InvalidOperationException(
                               $"No section exists with name {authenticationSectionName} in the configuration providers for OAuth2 Connect authentication.");

            if (settings.DefaultAuthority is null)
            {
                throw new MissingFieldException("DefaultAuthority must be filled!");
            }

            if (string.IsNullOrWhiteSpace(settings.OAuth2SettingsSectionPath))
            {
                throw new MissingFieldException("We need a setting section to configure OAuth2.");
            }

            X509Certificate2? certSecurityKey;

            if (string.IsNullOrWhiteSpace(settings.CertSecurityKeyPath))
            {
                certSecurityKey = null;
            }
            else
            {
                // we only need a non-null loader in this case
                certificateLoader ??= new X509CertificateLoader(null);
                certSecurityKey = string.IsNullOrWhiteSpace(settings.CertSecurityKeyPath)
                    ? null
                    : certificateLoader.FindCertificate(configuration, settings.CertSecurityKeyPath) ??
                      throw new MissingFieldException(
                          $"No certificate was found based on the configuration section: {settings.CertSecurityKeyPath}.");
            }

            void JwtAuthenticationFiller(JwtAuthenticationOptions options)
            {
                options.DefaultAuthority = settings.DefaultAuthority;
                options.ValidateAuthority = settings.ValidateAuthority;
                options.OAuth2SettingsOptions =
                    OAuth2SettingsExtension.PrepareAction(configuration, settings.OAuth2SettingsSectionPath);
                options.CertSecurityKey = certSecurityKey;
                options.ClaimsIdentifierOptions =
                    ClaimsidentifierExtension.PrepareAction(configuration, settings.ClaimsIdentifierSectionPath);
            }

            services.AddDomainMapping(configuration, settings.DomainMappingsSectionPath);
            services.AddTokenCache(configuration, settings.TokenCacheSectionPath);
            services.AddClaimsFiller(configuration, settings.ClaimsFillerSectionPath);
            services.AddClientTokens(configuration, settings.ClientTokensSectionPath);
            services.AddRemoteSecretsAuthentication(configuration, settings.RemoteSecretSectionPath);

            return services.AddJwtAuthentication(configuration, JwtAuthenticationFiller);
        }
    }
}
