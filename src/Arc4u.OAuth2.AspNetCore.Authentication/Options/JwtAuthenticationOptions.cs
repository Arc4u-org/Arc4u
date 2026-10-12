using System.Security.Cryptography.X509Certificates;

namespace Arc4u.OAuth2.Options
{
    /// <summary>The options of the JWT bearer authentication, given to <c>AddJwtAuthentication</c>.</summary>
    public class JwtAuthenticationOptions
    {
        /// <summary>Gets or sets the default authority that issues the tokens. Its url is the authority and its metadata address is used to download the signing keys.</summary>
        public AuthorityOptions DefaultAuthority { get; set; } = new AuthorityOptions();
        /// <summary>Gets or sets the action that fills the <see cref="OAuth2SettingsOption"/> (audiences, scopes, ...). Required.</summary>
        public Action<OAuth2SettingsOption> OAuth2SettingsOptions { get; set; } = default!;
        /// <summary>Gets or sets the action that fills the claim types identifying a user.</summary>
        public Action<ClaimsIdentifierOption> ClaimsIdentifierOptions { get; set; } = default!;

        /// <summary>
        /// Gets or sets a value indicating whether the issuer of the token must be validated. The default is <see langword="true"/>.
        /// When <see langword="true"/>, the issuer must be <see cref="AuthorityOptions.Issuer"/> of the default authority when it is set, or the issuer read from its metadata otherwise.
        /// </summary>
        public bool ValidateAuthority { get; set; } = true;

        /// <summary>
        /// Gets or sets the claim type holding the name of the user (<c>User.Identity.Name</c>). The default is <c>name</c>.
        /// </summary>
        public string NameClaimType { get; set; } = "name";

        /// <summary>
        /// Gets or sets the claim type holding the roles of the user. The default is <c>role</c>.
        /// </summary>
        public string RoleClaimType { get; set; } = "role";

        /// <summary>Gets or sets an optional certificate used as issuer signing key to validate tokens.</summary>
        public X509Certificate2? CertSecurityKey { get; set; } = default!;
    }
}
