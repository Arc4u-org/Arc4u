using Arc4u.Configuration;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;

namespace Arc4u.OAuth2.TokenProvider.Scenarios;

/// <summary>
/// <c>grant_type=client_credentials</c> scenario: a ClientId + ClientSecret pair. Any other key in
/// <see cref="ClientTokenSettingsOptions.Settings"/> (e.g. the Logto.io <c>resource</c> indicator)
/// is not claimed by this scenario and is forwarded to the token endpoint as an extra parameter.
/// </summary>
public sealed class ClientCredentialsScenario : IClientTokenScenario
{
    /// <summary>The discriminator under which this scenario is registered.</summary>
    public const string Name = "ClientCredentials";

    /// <summary>The token provider (ProviderId) that consumes settings produced by this scenario.</summary>
    public const string ProviderName = ClientCredentialsTokenProvider.ProviderName;

    private const string ClientId = "ClientId";
    private const string ClientSecret = "ClientSecret";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> KnownKeys { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ClientId, ClientSecret };

    /// <inheritdoc/>
    public void Validate(string optionKey, ClientTokenSettingsOptions options)
    {
        new ClientTokenValidation(optionKey)
            .Require(options.Settings, ClientId)
            .Require(options.Settings, ClientSecret)
            .ThrowIfInvalid();
    }

    /// <inheritdoc/>
    public void WriteTo(string optionKey, ClientTokenSettingsOptions options, SimpleKeyValueSettings settings)
    {
        settings.Add(TokenKeys.ProviderIdKey, ProviderName);
        ClientTokenScenarioHelper.WriteCommon(options, settings, optionKey);

        settings.Add(TokenKeys.ClientIdKey, options.Settings[ClientId]);
        settings.Add(TokenKeys.ClientSecret, options.Settings[ClientSecret]);
    }
}
