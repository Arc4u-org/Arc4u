namespace Arc4u.OAuth2.Options
{
    /// <summary>
    /// The options of the hybrid authentication: the <see cref="OidcAuthenticationOptions"/> of the browser (OpenID Connect and cookie) authentication plus the settings used to validate JWT bearer tokens.
    /// </summary>
    public class HybridAuthenticationOptions : OidcAuthenticationOptions
    {
        /// <summary>Gets or sets the action that fills the <see cref="OAuth2SettingsOption"/> used to validate the JWT bearer tokens. Required.</summary>
        public Action<OAuth2SettingsOption> OAuth2SettingsOptions { get; set; } = default!;
    }
}
