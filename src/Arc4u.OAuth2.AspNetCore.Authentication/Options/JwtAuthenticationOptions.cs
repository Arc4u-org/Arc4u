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

        /// <summary>Gets or sets a value indicating whether the authority must be validated. The default is <see langword="true"/>. It is not read by the JWT bearer configuration.</summary>
        public bool ValidateAuthority { get; set; } = true;

        /// <summary>Gets or sets an optional certificate used as issuer signing key to validate tokens.</summary>
        public X509Certificate2? CertSecurityKey { get; set; } = default!;
    }
}
