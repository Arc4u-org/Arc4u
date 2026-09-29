namespace Arc4u.OAuth2.Options
{
    /// <summary>The configuration layout of the hybrid authentication: the <see cref="OidcAuthenticationSectionOptions"/> plus the paths of the JWT bearer and Basic authentication sections.</summary>
    public class HybridAuthenticationSectionOptions : OidcAuthenticationSectionOptions
    {
        /// <summary>Gets or sets the path of the section holding the <see cref="OAuth2SettingsOption"/>. The default is <c>Authentication:OAuth2.Settings</c>.</summary>
        public string OAuth2SettingsSectionPath { get; set; } = "Authentication:OAuth2.Settings";

        /// <summary>Gets or sets a key for the OAuth2 settings. The default is <see cref="Constants.BearerAuthenticationType"/> (<c>OAuth2</c>). It is not read by the registration code.</summary>
        public string OAuth2SettingsKey { get; set; } = Constants.BearerAuthenticationType;

        /// <summary>Gets or sets the path of the optional section holding the Basic authentication settings. The default is <c>Authentication:Basic</c>. The section may be absent.</summary>
        public string BasicAuthenticationSectionPath { get; set; } = "Authentication:Basic";

    }
}

