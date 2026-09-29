using Arc4u.OAuth2.TokenProviders;

namespace Arc4u.OAuth2.Options
{
    /// <summary>
    /// The settings of the OpenID Connect client. Bound from the <c>Authentication:OpenId.Settings</c> section by default.
    /// They are registered as the <see cref="Arc4u.Configuration.SimpleKeyValueSettings"/> named <c>Cookies</c> (see <see cref="Arc4u.OAuth2.Extensions.OpenIdSettingsExtension"/>).
    /// </summary>
    public class OpenIdSettingsOption
    {
        /// <summary>Gets or sets the key of the token provider that gets the token of the user. The default is <c>Oidc</c>.</summary>
        public string ProviderId { get; set; } = OidcTokenProvider.ProviderName;

        /// <summary>
        /// If null default authority is used!
        /// </summary>
        public AuthorityOptions? Authority { get; set; } = default!;

        /// <summary>Gets or sets the client id registered in the identity provider. Required.</summary>
        public string ClientId { get; set; } = default!;

        /// <summary>Gets or sets the client secret registered in the identity provider.</summary>
        public string ClientSecret { get; set; } = default!;

        /// <summary>Gets or sets the accepted audiences of the access token. At least one is required when <see cref="ValidateAudience"/> is <see langword="true"/>.</summary>
        public List<string> Audiences { get; set; } = [];

        /// <summary>Gets or sets the scopes requested to the identity provider. At least one is required.</summary>
        public List<string> Scopes { get; set; } = [];

        /// <summary>Gets or sets a value indicating whether the audience of the access token is validated. The default is <see langword="true"/>. Set it to <see langword="false"/> for identity providers, like Keycloak, that do not issue an audience by default.</summary>
        public bool ValidateAudience { get; set; } = true;
    }
}
