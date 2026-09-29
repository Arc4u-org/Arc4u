using Arc4u.Configuration;

namespace Arc4u.OAuth2.Options;

/// <summary>The resolved settings used by the OpenID bearer injector middleware. They are filled from <see cref="OpenIdBearerInjectorOptions"/> by <see cref="PostConfigureOpenIdBearerInjectorSettings"/>.</summary>
public class OpenIdBearerInjectorSettingsOptions
{
    /// <summary>Gets or sets the settings of the on-behalf-of token request. Empty by default.</summary>
    public IKeyValueSettings OnBehalfOfOpenIdSettings { get; set; } = new SimpleKeyValueSettings();

    /// <summary>
    /// Which provider is used to create an On behal of token.
    /// </summary>
    public string OboProviderKey { get; set; } = "Obo";

    /// <summary>
    /// The OpenId KeyValues settings resolver name
    /// </summary>
    public IKeyValueSettings OpenIdSettings { get; set; } = new SimpleKeyValueSettings();
}

