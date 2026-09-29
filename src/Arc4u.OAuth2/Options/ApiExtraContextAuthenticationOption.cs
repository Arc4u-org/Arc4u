using Arc4u.Configuration;

namespace Arc4u.OAuth2.Options;

/// <summary>
/// Extra parameters added to the authorization and token requests sent to the identity provider.
/// </summary>
public class ApiExtraContextAuthenticationOption
{
    /// <summary>Gets or sets the key/value pairs added to the authorization endpoint request. Empty by default.</summary>
    public IKeyValueSettings AuthorizationParameters { get; set; } = new SimpleKeyValueSettings();

    /// <summary>Gets or sets the key/value pairs added to the token endpoint request. Empty by default.</summary>
    public IKeyValueSettings TokenParameters { get; set; } = new SimpleKeyValueSettings();
}
