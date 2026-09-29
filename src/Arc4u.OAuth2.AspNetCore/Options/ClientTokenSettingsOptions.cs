namespace Arc4u.OAuth2.Options;

/// <summary>
/// Unified configuration entry for the <c>Authentication:ClientTokens</c> section.
/// <para>
/// Carries only the fields common to every client token acquisition scenario. The
/// <see cref="Scenario"/> property is the discriminator that selects the
/// <see cref="Arc4u.OAuth2.TokenProvider.Scenarios.IClientTokenScenario"/> responsible for validating this
/// entry and projecting it into a <see cref="Arc4u.Configuration.SimpleKeyValueSettings"/>.
/// </para>
/// <para>
/// All scenario-specific values (ClientId, ClientSecret, User, Password, Credential, …) live in the
/// open <see cref="Settings"/> bag. The selected scenario claims the keys it consumes; every
/// remaining key is forwarded verbatim to the token endpoint as an extra request parameter.
/// </para>
/// </summary>
public class ClientTokenSettingsOptions
{
    /// <summary>
    /// Discriminator. The key resolving an <see cref="Arc4u.OAuth2.TokenProvider.Scenarios.IClientTokenScenario"/>
    /// (e.g. "UserPassword", "ClientCredentials").
    /// </summary>
    public string Scenario { get; set; } = default!;

    /// <summary>
    /// The authentication type (OAuth2Bearer, Cookies, Inject, …). Defaults to <see cref="Constants.InjectAuthenticationType"/>.
    /// </summary>
    public string AuthenticationType { get; set; } = Constants.InjectAuthenticationType;

    /// <summary>
    /// The STS authority. When present, it is registered under the entry key and referenced by the produced settings.
    /// </summary>
    public AuthorityOptions? Authority { get; set; }

    /// <summary>
    /// The scopes requested. Defaults to <c>openid</c> when none is provided.
    /// </summary>
    public List<string> Scopes { get; set; } = [];

    /// <summary>
    /// Open bag of scenario-specific values. The selected scenario consumes the keys it knows
    /// (see <see cref="Arc4u.OAuth2.TokenProvider.Scenarios.IClientTokenScenario.KnownKeys"/>); any other key is
    /// forwarded to the token endpoint as an extra request parameter.
    /// </summary>
    public Dictionary<string, string> Settings { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
