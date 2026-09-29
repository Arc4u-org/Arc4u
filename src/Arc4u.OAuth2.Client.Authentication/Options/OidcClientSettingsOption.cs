using Arc4u.OAuth2.Client.Authentication.TokenProvider;

namespace Arc4u.OAuth2.Client.Authentication.Options;

/// <summary>
/// The settings of the OpenID Connect client. Bound from the <c>Authentication:OidcClient.Settings</c> section by default and registered as the
/// <see cref="Arc4u.Configuration.SimpleKeyValueSettings"/> named <c>OidcClient</c>.
/// </summary>
public class OidcClientSettingsOption
{
    /// <summary>Gets or sets the key of the token provider. The default is <c>OidcClientIdentityModel</c>.</summary>
    public string ProviderId { get; set; } = OidcClientIdentityModelTokenProvider.TokenProviderName;

    /// <summary>Gets or sets the client id registered in the identity provider. Required.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the scopes requested to the identity provider. At least one is required.</summary>
    public List<string> Scopes { get; set; } = [];
}
