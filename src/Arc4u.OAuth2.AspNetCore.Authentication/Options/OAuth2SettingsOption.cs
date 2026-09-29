namespace Arc4u.OAuth2.Options
{
    /// <summary>
    /// The settings used to validate the JWT bearer tokens received by the application and to obtain a token from them. Bound from the <c>Authentication:OAuth2.Settings</c> section by default.
    /// They are registered as the <see cref="Arc4u.Configuration.SimpleKeyValueSettings"/> named <c>OAuth2</c> (see <see cref="Arc4u.OAuth2.Extensions.OAuth2SettingsExtension"/>).
    /// </summary>
    public class OAuth2SettingsOption
    {
        /// <summary>Gets or sets the key of the token provider used to get the token of the caller. The default is <c>Bootstrap</c> (the token received in the request).</summary>
        public string ProviderId { get; set; } = "Bootstrap";

        /// <summary>Gets or sets the authority of the tokens. When <see langword="null"/>, the default authority is used.</summary>
        public AuthorityOptions? Authority { get; set; } = default!;

        /// <summary>Gets or sets the accepted audiences. At least one is required when <see cref="ValidateAudience"/> is <see langword="true"/>.</summary>
        public List<string> Audiences { get; set; } = [];

        // use for Obo scenario.
        /// <summary>Gets or sets the scopes, used by the on-behalf-of scenario.</summary>
        public List<string> Scopes { get; set; } = [];

        /// <summary>Gets or sets a value indicating whether the audience of the token is validated. The default is <see langword="true"/>. Set it to <see langword="false"/> for identity providers, like Keycloak, that do not issue an audience by default.</summary>
        public bool ValidateAudience { get; set; } = true;

    }
}
