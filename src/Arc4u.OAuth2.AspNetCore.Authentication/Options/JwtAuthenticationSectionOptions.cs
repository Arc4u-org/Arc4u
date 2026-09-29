namespace Arc4u.OAuth2.Options
{
    /// <summary>
    /// The configuration layout of the JWT bearer authentication. It is bound from the root <c>Authentication</c> section (or the section name given to
    /// <c>AddJwtAuthentication</c>).
    /// The <c>*Path</c> properties are the paths of the other configuration sections read during registration.
    /// </summary>
    public class JwtAuthenticationSectionOptions
    {
        /// <summary>Gets or sets the default authority that issues the tokens. Required.</summary>
        public AuthorityOptions DefaultAuthority { get; set; } = default!;
        /// <summary>Gets or sets the path of the section holding the <see cref="OAuth2SettingsOption"/>. The default is <c>Authentication:OAuth2.Settings</c>.</summary>
        public string OAuth2SettingsSectionPath { get; set; } = "Authentication:OAuth2.Settings";

        /// <summary>Gets or sets a value indicating whether the authority must be validated. The default is <see langword="true"/>. It is not read by the JWT bearer configuration.</summary>
        public bool ValidateAuthority { get; set; } = true;

        /// <summary>Gets or sets the path of the section describing an optional certificate used as issuer signing key to validate tokens.</summary>
        public string? CertSecurityKeyPath { get; set; } = default!;

        /// <summary>Gets or sets the path of the section holding the claim types that identify a user. The default is <c>Authentication:ClaimsIdentifier</c>.</summary>
        public string ClaimsIdentifierSectionPath { get; set; } = "Authentication:ClaimsIdentifier";

        /// <summary>Gets or sets the path of the section holding the <see cref="ClaimsFillerOptions"/>. The default is <c>Authentication:ClaimsMiddleWare:ClaimsFiller</c>.</summary>
        public string ClaimsFillerSectionPath { get; set; } = "Authentication:ClaimsMiddleWare:ClaimsFiller";

        /// <summary>Gets or sets the path of the section holding the <see cref="TokenCacheOptions"/>. The default is <c>Authentication:TokenCache</c>.</summary>
        public string TokenCacheSectionPath { get; set; } = "Authentication:TokenCache";

        /// <summary>Gets or sets the path of the section holding the client tokens (see <c>ClientTokenSettingsOptions</c>). The default is <c>Authentication:ClientTokens</c>.</summary>
        public string ClientTokensSectionPath { get; set; } = "Authentication:ClientTokens";

        /// <summary>Gets or sets the path of the section holding the remote secrets. The default is <c>Authentication:RemoteSecrets</c>.</summary>
        public string RemoteSecretSectionPath { get; set; } = "Authentication:RemoteSecrets";

        /// <summary>Gets or sets the path of the section holding the domain mappings. The default is <c>Authentication:DomainsMapping</c>.</summary>
        public string DomainMappingsSectionPath { get; set; } = "Authentication:DomainsMapping";

        /// <summary>
        /// By default the audience is validated. It is always better to do
        /// On Keycloak audience doesn't exist by default, so it is needed to disable it.
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

    }
}
