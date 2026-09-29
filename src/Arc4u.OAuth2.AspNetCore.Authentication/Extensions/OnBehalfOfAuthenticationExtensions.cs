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

            if (string.IsNullOrWhiteSpace(optionKey))
            {
                throw new ArgumentNullException(optionKey);
            }

            services.Configure(optionKey, BuildSettings(options));
        }

        /// <summary>
        /// Registers the on-behalf-of settings of a configuration section. Each child of the section is a <see cref="OnBehalfOfSettingsOptions"/> registered under the name of the child.
        /// Nothing is registered when the section does not exist.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sectionName">The section to read. The default is <c>Authentication:OnBehalfOf</c>.</param>
        /// <exception cref="ConfigurationException">A setting has an empty client id, client secret, scopes or authentication type.</exception>
        public static void AddOnBehalfOf(this IServiceCollection services, [DisallowNull] IConfiguration configuration, [DisallowNull] string sectionName = "Authentication:OnBehalfOf")
        {
            ArgumentNullException.ThrowIfNull(configuration);
            ArgumentNullException.ThrowIfNull(services);

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

            foreach (var settingsOptions in options)
            {
                services.AddOnBehalfOfSettings(oboSettings =>
                {
                    oboSettings.ClientId = settingsOptions.Value.ClientId;
                    oboSettings.ClientSecret = settingsOptions.Value.ClientSecret;
                    oboSettings.Scopes = settingsOptions.Value.Scopes;
                    oboSettings.AuthenticationType = settingsOptions.Value.AuthenticationType;
                    oboSettings.ProviderId = settingsOptions.Value.ProviderId;
                }, settingsOptions.Key);
            }

        }

        private static Action<SimpleKeyValueSettings> BuildSettings(Action<OnBehalfOfSettingsOptions> options)
        {
            var validated = Validate(options);

            void Settings(SimpleKeyValueSettings settings)
            {
                settings.Add(TokenKeys.ClientIdKey, validated.ClientId);
                settings.Add(TokenKeys.Scope, string.Join(' ', validated.Scopes));
                settings.Add(TokenKeys.ClientSecret, validated.ClientSecret);
                settings.Add(TokenKeys.AuthenticationTypeKey, validated.AuthenticationType);
                settings.Add(TokenKeys.ProviderIdKey, validated.ProviderId);
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

            if (!extract.Scopes.Any())
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
