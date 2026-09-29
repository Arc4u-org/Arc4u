using Arc4u.Configuration;

namespace Arc4u.OAuth2.Options;

/// <summary>The resolved settings used by the OpenID bearer injector middleware. They are filled from <see cref="OpenIdBearerInjectorOptions"/> by <see cref="PostConfigureOpenIdBearerInjectorSettings"/>.</summary>
public class OpenIdBearerInjectorSettingsOptions
{
    /// <summary>Gets or sets the settings of the on-behalf-of token request. Empty by default.</summary>
    public IKeyValueSettings OnBehalfOfOpenIdSettings { get; set; } = new SimpleKeyValueSettings();

    /// <summary>
    /// Gets or sets the key of the <see cref="Arc4u.OAuth2.Token.ITokenProvider"/> that creates an on-behalf-of token. The default is <c>Obo</c>.
    /// </summary>
    public string OboProviderKey { get; set; } = "Obo";

    /// <summary>
    /// Gets or sets the settings of the OpenID authentication (see <see cref="OpenIdBearerInjectorOptions.OpenIdSettingsKey"/>), empty by default.
    /// The middleware only acts on identities whose authentication type equals the <c>AuthenticationType</c> of these settings.
    /// </summary>
    public IKeyValueSettings OpenIdSettings { get; set; } = new SimpleKeyValueSettings();
}

