using System.Diagnostics.CodeAnalysis;
using Arc4u.Configuration;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.OAuth2.Extensions;
/// <summary>Registers the remote secrets: named <see cref="SimpleKeyValueSettings"/> consumed by the <c>RemoteSecret</c> token provider.</summary>
public static class RemoteSecretsExtension
{
    /// <summary>Registers one remote secret from code.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The action that configures the secret.</param>
    /// <param name="optionKey">The name under which the settings are registered.</param>
    /// <exception cref="ConfigurationException">A mandatory field (<c>HeaderKey</c>, <c>ClientSecret</c>, <c>ProviderId</c>, <c>AuthenticationType</c>) is empty.</exception>
    public static void AddRemoteSecretsAuthentication(this IServiceCollection services, Action<RemoteSecretSettingsOptions> options, [DisallowNull] string optionKey)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(optionKey))
        {
            throw new ArgumentNullException(nameof(optionKey));
        }

        services.Configure<SimpleKeyValueSettings>(optionKey, BuildRemoteSecretsSettings(options));
    }

    /// <summary>
    /// Registers the remote secrets of a configuration section: each child of the section is a <see cref="RemoteSecretSettingsOptions"/> registered under the name of the child.
    /// Nothing is registered when the section does not exist or is empty.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <param name="sectionName">The section holding the secrets. The default is <c>Authentication:RemoteSecrets</c>.</param>
    /// <exception cref="ConfigurationException">A mandatory field (<c>HeaderKey</c>, <c>ClientSecret</c>, <c>ProviderId</c>, <c>AuthenticationType</c>) is empty.</exception>
    /// <example>
    /// <code language="csharp">
    /// services.AddRemoteSecretsAuthentication(configuration);
    /// </code>
    /// </example>
    public static void AddRemoteSecretsAuthentication(this IServiceCollection services, [DisallowNull] IConfiguration configuration, [DisallowNull] string sectionName = "Authentication:RemoteSecrets")
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(sectionName);

        var section = configuration.GetSection(sectionName);

        if (section is null || !section.Exists())
        {
            return;
        }

        var remoteSecrets = section.Get<Dictionary<string, RemoteSecretSettingsOptions>>();

        if (remoteSecrets is null || !remoteSecrets.Any())
        {
            return;
        }

        foreach (var secret in remoteSecrets)
        {
            services.Configure<SimpleKeyValueSettings>(secret.Key, BuildRemoteSecretsSettings(secret.Value));
        }
    }

    private static Action<SimpleKeyValueSettings> BuildRemoteSecretsSettings(Action<RemoteSecretSettingsOptions> action)
    {
        var options = new RemoteSecretSettingsOptions();
        action(options);

        return BuildRemoteSecretsSettings(options);

    }

    private static Action<SimpleKeyValueSettings> BuildRemoteSecretsSettings(RemoteSecretSettingsOptions options)
    {
        // Check the settings!
        // options mandatory fields!
        string? configErrors = null;
        if (string.IsNullOrWhiteSpace(options.HeaderKey))
        {
            configErrors += "HeaaderKey in Remote Secret settings must be filled!" + System.Environment.NewLine;
        }

        if (string.IsNullOrWhiteSpace(options.ClientSecret))
        {
            configErrors += "ClientSecret in Remote Secret settings must be filled!" + System.Environment.NewLine;
        }

        if (string.IsNullOrWhiteSpace(options.ProviderId))
        {
            configErrors += "ProviderId in Remote Secret settings must be filled!" + System.Environment.NewLine;
        }

        if (string.IsNullOrWhiteSpace(options.AuthenticationType))
        {
            configErrors += "AuthenticationType in Remote Secret settings must be filled!" + System.Environment.NewLine;
        }

        if (configErrors is not null)
        {
            throw new ConfigurationException(configErrors);
        }

        // We map this to a IKeyValuesSettings dictionary.
        // The TokenProviders are based on 

        void Settings(SimpleKeyValueSettings settings)
        {
            settings.Add(TokenKeys.ProviderIdKey, options!.ProviderId);
            settings.Add(TokenKeys.ClientSecretHeader, options.HeaderKey);
            settings.Add(TokenKeys.ClientSecret, options.ClientSecret);
            settings.Add(TokenKeys.AuthenticationTypeKey, options.AuthenticationType);
        }

        return Settings;
    }

}
