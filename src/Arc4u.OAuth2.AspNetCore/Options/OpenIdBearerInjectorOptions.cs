
namespace Arc4u.OAuth2.Options;

/// <summary>
/// Configures the OpenID bearer injector middleware: the names under which the token settings and the token provider are registered.
/// </summary>
public class OpenIdBearerInjectorOptions
{
    /// <summary>
    /// Gets or sets the name of the named <c>SimpleKeyValueSettings</c> that describes the on-behalf-of token request.
    /// The default is <c>Obo_for_OpenId</c>. When these settings are empty, the token is requested with the OpenID settings instead.
    /// </summary>
    public string OnBehalfOfOpenIdSettingsKey { get; set; } = "Obo_for_OpenId";

    /// <summary>
    /// Gets or sets the key of the <see cref="Arc4u.OAuth2.Token.ITokenProvider"/> that creates an on-behalf-of token. The default is <c>Obo</c>.
    /// </summary>
    public string OboProviderKey { get; set; } = "Obo";

    /// <summary>
    /// Gets or sets the name of the <see cref="Arc4u.Configuration.SimpleKeyValueSettings"/> holding the OpenID settings. The default is <c>Cookies</c> (<see cref="Constants.CookiesAuthenticationType"/>).
    /// </summary>
    public string OpenIdSettingsKey { get; set; } = Constants.CookiesAuthenticationType;
}

