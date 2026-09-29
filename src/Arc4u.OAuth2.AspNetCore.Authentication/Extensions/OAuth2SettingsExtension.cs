using System.Diagnostics.CodeAnalysis;
using Arc4u.Configuration;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.Extensions
{
    /// <summary>Registers the <see cref="OAuth2SettingsOption"/> as the key/value settings read by the token providers.</summary>
    public static class OAuth2SettingsExtension
    {
        /// <summary>Validates the OAuth2 settings and registers them as the named <see cref="SimpleKeyValueSettings"/> <paramref name="sectionKey"/>. When an authority is set, it is registered as the named authority <c>OAuth2</c>.</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="option">The action that configures the settings.</param>
        /// <param name="sectionKey">The name of the settings. The default is <see cref="Constants.BearerAuthenticationType"/> (<c>OAuth2</c>).</param>
        /// <returns>The settings registered.</returns>
        /// <exception cref="ConfigurationException">The provider id is empty, or no audience is given while <see cref="OAuth2SettingsOption.ValidateAudience"/> is <see langword="true"/>.</exception>
        public static SimpleKeyValueSettings ConfigureOAuth2Settings(this IServiceCollection services, Action<OAuth2SettingsOption> option, [DisallowNull] string sectionKey = Constants.BearerAuthenticationType)
        {
            ArgumentNullException.ThrowIfNull(sectionKey);

            var validate = new OAuth2SettingsOption();
            option(validate);
            string? configErrors = null;

            if (string.IsNullOrWhiteSpace(validate.ProviderId))
            {
                configErrors += "ProviderId field is not defined." + System.Environment.NewLine;
            }

            if (validate.ValidateAudience && !validate.Audiences.Any())
            {
                configErrors += "Audiences field is not defined." + System.Environment.NewLine;
            }

            if (configErrors is not null)
            {
                throw new ConfigurationException(configErrors);
            }

            // We map this to a IKeyValuesSettings dictionary.
            // The TokenProviders are based on

            void SettingsFiller(SimpleKeyValueSettings keyOptions)
            {
                keyOptions.Add(TokenKeys.ProviderIdKey, validate!.ProviderId);
                keyOptions.Add(TokenKeys.AuthenticationTypeKey, Constants.BearerAuthenticationType);
                //Optional => go to default.
                if (validate.Authority is not null)
                {
                    keyOptions.Add(TokenKeys.AuthorityKey, Constants.BearerAuthenticationType);
                    services.AddAuthority(options =>
                    {
                        options.SetData(validate.Authority.Url, validate.Authority.TokenEndpoint, validate.Authority.Issuer, validate.Authority.MetaDataAddress);
                    }, Constants.BearerAuthenticationType);
                }
                // Build the list of Audiences as a string.
                keyOptions.Add(TokenKeys.Audiences, string.Join(' ', validate.Audiences));
                keyOptions.Add(TokenKeys.Scope, string.Join(' ', validate.Scopes));

            }

            services.Configure<SimpleKeyValueSettings>(sectionKey, SettingsFiller);

            var settings = new SimpleKeyValueSettings();

            SettingsFiller(settings);

            return settings;

        }

        /// <summary>Reads the OAuth2 settings from a configuration section and registers them as the named <see cref="SimpleKeyValueSettings"/> <paramref name="sectionKey"/>.</summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="sectionName">The section holding the <see cref="OAuth2SettingsOption"/>, for example <c>Authentication:OAuth2.Settings</c>.</param>
        /// <param name="sectionKey">The name of the settings. The default is <see cref="Constants.BearerAuthenticationType"/> (<c>OAuth2</c>).</param>
        /// <returns>The settings registered.</returns>
        /// <exception cref="ConfigurationException">The provider id is empty, or no audience is given while <see cref="OAuth2SettingsOption.ValidateAudience"/> is <see langword="true"/>.</exception>
        public static SimpleKeyValueSettings ConfigureOAuth2Settings(this IServiceCollection services, IConfiguration configuration, [DisallowNull] string sectionName, [DisallowNull] string sectionKey = Constants.BearerAuthenticationType)
        {
            ArgumentNullException.ThrowIfNull(sectionKey);

            return ConfigureOAuth2Settings(services, PrepareAction(configuration, sectionName), sectionKey);
        }

        internal static Action<OAuth2SettingsOption> PrepareAction(IConfiguration configuration, [DisallowNull] string sectionName)
        {
            ArgumentNullException.ThrowIfNullOrWhiteSpace(sectionName);
            ArgumentNullException.ThrowIfNull(configuration);

            var settings = new OAuth2SettingsOption();
            var defaulValidateAudience = settings.ValidateAudience;

            var section = configuration.GetSection(sectionName);

            if (section is not null && section.Exists())
            {
                settings = section.Get<OAuth2SettingsOption>() ?? settings;

                if (section.GetChildren().Any(c => c.Key == nameof(OAuth2SettingsOption.ValidateAudience)))
                {
                    settings.ValidateAudience = section.GetValue<bool>(nameof(OAuth2SettingsOption.ValidateAudience));
                } else
                {
                    settings.ValidateAudience = defaulValidateAudience;
                }
            }

            void OptionFiller(OAuth2SettingsOption option)
            {
                option.Authority = settings.Authority;
                option.Audiences = settings.Audiences;
                option.ProviderId = settings.ProviderId;
                option.Scopes = settings.Scopes;
                option.ValidateAudience = settings.ValidateAudience;
            }

            return OptionFiller;
        }
    }
}
