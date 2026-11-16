using System.Diagnostics.CodeAnalysis;
using Arc4u.Configuration;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.Extensions
{
    /// <summary>Registers the on-behalf-of settings: named <see cref="SimpleKeyValueSettings"/> read by the <c>Obo</c> token provider.</summary>
    public static class OnBehalfOfAuthenticationExtensions
    {
        /// <summary>
        /// The name of the <see cref="TokenCacheOptions"/> registered from the <c>Authentication:OnBehalfOfCache</c> section: the token cache of every on-behalf-of setting
        /// read by <see cref="AddOnBehalfOf"/>. No setting can use this name as its key when the section exists.
        /// </summary>
        public const string TokenCacheOptionsName = "OnBehalfOfCache";

        /// <summary>Registers one on-behalf-of setting from code.</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="options">The action that configures the setting.</param>
        /// <param name="optionKey">The name under which the settings are registered.</param>
        /// <exception cref="ArgumentNullException">An argument is <see langword="null"/> or empty.</exception>
        /// <exception cref="ConfigurationException">The client id, the client secret, the scopes or the authentication type is empty.</exception>
        /// <example>
        /// <code language="csharp">
        /// builder.Services.AddOnBehalfOfSettings(o =>
        /// {
        ///     o.ClientId = "my-client-id";
        ///     o.ClientSecret = "my-client-secret";
        ///     o.Scopes.Add("api://downstream-api/.default");
        /// }, "DownstreamApi");
        /// </code>
        /// </example>
        public static void AddOnBehalfOfSettings(this IServiceCollection services, Action<OnBehalfOfSettingsOptions> options, [DisallowNull] string optionKey)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(options);

            services.AddTokenProvidersSignOut();
            services.AddTokenCacheResolver();

            if (string.IsNullOrWhiteSpace(optionKey))
            {
                throw new ArgumentNullException(optionKey);
            }

            var validated = Validate(options);

            // From code, the instance gives both values; an empty CacheName takes the one of the default options.
            if (validated.TokenCache is not null)
            {
                services.AddNamedTokenCache(optionKey, validated.TokenCache.CacheName, validated.TokenCache.MaxTime);
            }

            services.Configure(optionKey, BuildSettings(validated, validated.TokenCache is null ? null : optionKey));
        }

        /// <summary>
        /// Registers the on-behalf-of settings of a configuration section. Each child of the section is a <see cref="OnBehalfOfSettingsOptions"/> registered under the name of the child.
        /// Nothing is registered when the section does not exist.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sectionName">The section to read. The default is <c>Authentication:OnBehalfOf</c>.</param>
        /// <param name="tokenCacheSectionName">
        /// The optional section holding the <see cref="TokenCacheOptions"/> shared by every setting. The default is <c>Authentication:OnBehalfOfCache</c>; an empty name turns it off.
        /// The token cache of a setting is resolved value by value: the setting's own <c>TokenCache</c>, then this section, then the default <see cref="TokenCacheOptions"/>
        /// (<c>Authentication:TokenCache</c>).
        /// </param>
        /// <exception cref="ConfigurationException">
        /// A setting has an empty client id, client secret, scopes or authentication type; a <c>MaxTime</c> of a token cache is not greater than zero;
        /// or a setting is named <see cref="TokenCacheOptionsName"/> while the token cache section exists.
        /// </exception>
        public static void AddOnBehalfOf(this IServiceCollection services, [DisallowNull] IConfiguration configuration, [DisallowNull] string sectionName = "Authentication:OnBehalfOf",
                                         [DisallowNull] string tokenCacheSectionName = "Authentication:OnBehalfOfCache")
        {
            ArgumentNullException.ThrowIfNull(tokenCacheSectionName);
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(services);

            services.AddTokenProvidersSignOut();
            services.AddTokenCacheResolver();

            if (string.IsNullOrWhiteSpace(sectionName))
            {
                // No OnBehalfOf are needed into the application!
                return;
            }

            var section = configuration.GetSection(sectionName);

            if (section is null || !section.Exists())
            {
                // No OnBehalfOf are needed into the application!
                return;
            }

            var options = section.Get<Dictionary<string, OnBehalfOfSettingsOptions>>();
            if (null == options)
            {
                return;
            }

            // The token cache shared by every setting, when the section exists.
            var sharedTokenCacheName = services.AddSharedTokenCache(configuration, tokenCacheSectionName, TokenCacheOptionsName, options.Keys);

            foreach (var settingsOptions in options)
            {
                var validated = Validate(oboSettings =>
                {
                    oboSettings.ClientId = settingsOptions.Value.ClientId;
                    oboSettings.ClientSecret = settingsOptions.Value.ClientSecret;
                    oboSettings.Scopes = settingsOptions.Value.Scopes;
                    oboSettings.AuthenticationType = settingsOptions.Value.AuthenticationType;
                    oboSettings.ProviderId = settingsOptions.Value.ProviderId;
                    oboSettings.TokenCache = settingsOptions.Value.TokenCache;
                    oboSettings.HttpClientName = settingsOptions.Value.HttpClientName;
                });

                // From the configuration, a value that is not in the TokenCache section is taken from the shared section, then from the default options.
                // A setting without its own TokenCache uses the shared one (or the default options when there is none).
                var tokenCacheOptionsName = sharedTokenCacheName;
                if (validated.TokenCache is not null)
                {
                    services.AddNamedTokenCache(settingsOptions.Key, section.GetSection(settingsOptions.Key).GetSection(nameof(OnBehalfOfSettingsOptions.TokenCache)), sharedTokenCacheName);
                    tokenCacheOptionsName = settingsOptions.Key;
                }

                services.Configure(settingsOptions.Key, BuildSettings(validated, tokenCacheOptionsName));
            }

        }

        private static Action<SimpleKeyValueSettings> BuildSettings(OnBehalfOfSettingsOptions validated, string? tokenCacheOptionsName)
        {
            void Settings(SimpleKeyValueSettings settings)
            {
                settings.Add(TokenKeys.ClientIdKey, validated.ClientId);
                settings.Add(TokenKeys.Scope, string.Join(' ', validated.Scopes));
                settings.Add(TokenKeys.ClientSecret, validated.ClientSecret);
                settings.Add(TokenKeys.AuthenticationTypeKey, validated.AuthenticationType);
                settings.Add(TokenKeys.ProviderIdKey, validated.ProviderId);
                settings.AddifNotNullOrEmpty(TokenKeys.HttpClientName, validated.HttpClientName);
                // The token provider passes this name to the token cache so the tokens of this setting use its TokenCacheOptions.
                if (tokenCacheOptionsName is not null)
                {
                    settings.Add(TokenKeys.TokenCacheOptionsName, tokenCacheOptionsName);
                }
            }

            return Settings;
        }

        private static OnBehalfOfSettingsOptions Validate(Action<OnBehalfOfSettingsOptions> options)
        {
            // validation.
            var extract = new OnBehalfOfSettingsOptions();
            options(extract);

            var configErrors = string.Empty;
            if (string.IsNullOrWhiteSpace(extract.ClientId))
            {
                configErrors += "ClientId field is not defined." + System.Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(extract.ClientSecret))
            {
                configErrors += "ClientSecret field is not defined." + System.Environment.NewLine;
            }

            if (extract.Scopes.Count == 0)
            {
                configErrors += "Scope field is not defined." + System.Environment.NewLine;
            }

            if (string.IsNullOrWhiteSpace(extract.AuthenticationType))
            {
                configErrors += "AuthenticationType field is not defined." + System.Environment.NewLine;
            }

            if (!string.IsNullOrWhiteSpace(configErrors))
            {
                throw new ConfigurationException(configErrors);
            }

            return extract;
        }
    }
}
