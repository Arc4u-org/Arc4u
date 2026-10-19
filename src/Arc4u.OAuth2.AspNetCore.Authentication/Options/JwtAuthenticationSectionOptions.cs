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

        /// <summary>
        /// Gets or sets a value indicating whether the issuer of the token must be validated. It is copied to <see cref="JwtAuthenticationOptions.ValidateAuthority"/>. The default is <see langword="true"/>.
        /// When <see langword="true"/>, the issuer must be <see cref="AuthorityOptions.Issuer"/> of the default authority when it is set, or the issuer read from its metadata otherwise.
        /// </summary>
        public bool ValidateAuthority { get; set; } = true;

        /// <summary>
        /// Gets or sets the claim type holding the name of the user (<c>User.Identity.Name</c>). It is copied to <see cref="JwtAuthenticationOptions.NameClaimType"/>. The default is <c>name</c>.
        /// </summary>
        public string NameClaimType { get; set; } = "name";

        /// <summary>
        /// Gets or sets the claim type holding the roles of the user. It is copied to <see cref="JwtAuthenticationOptions.RoleClaimType"/>. The default is <c>role</c>.
        /// </summary>
        public string RoleClaimType { get; set; } = "role";

        /// <summary>Gets or sets the path of the section describing an optional certificate used as issuer signing key to validate tokens.</summary>
        public string? CertSecurityKeyPath { get; set; } = default!;

        /// <summary>Gets or sets the path of the section holding the claim types that identify a user. The default is <c>Authentication:ClaimsIdentifier</c>.</summary>
        public string ClaimsIdentifierSectionPath { get; set; } = "Authentication:ClaimsIdentifier";

        /// <summary>Gets or sets the path of the section holding the <see cref="ClaimsFillerOptions"/>. The default is <c>Authentication:ClaimsMiddleWare:ClaimsFiller</c>.</summary>
        public string ClaimsFillerSectionPath { get; set; } = "Authentication:ClaimsMiddleWare:ClaimsFiller";

        /// <summary>Gets or sets the path of the section holding the <see cref="TokenCacheOptions"/>. The default is <c>Authentication:TokenCache</c>.
        /// The section is optional: when it does not exist, the token cache options keep their defaults (the default cache is used).</summary>
        public string TokenCacheSectionPath { get; set; } = "Authentication:TokenCache";

        /// <summary>Gets or sets the path of the section holding the client tokens (see <c>ClientTokenSettingsOptions</c>). The default is <c>Authentication:ClientTokens</c>.</summary>
        public string ClientTokensSectionPath { get; set; } = "Authentication:ClientTokens";

        /// <summary>Gets or sets the path of the section holding the remote secrets. The default is <c>Authentication:RemoteSecrets</c>.</summary>
        public string RemoteSecretSectionPath { get; set; } = "Authentication:RemoteSecrets";

        /// <summary>Gets or sets the path of the section holding the domain mappings. The default is <c>Authentication:DomainsMapping</c>.</summary>
        public string DomainMappingsSectionPath { get; set; } = "Authentication:DomainsMapping";
    }
}
