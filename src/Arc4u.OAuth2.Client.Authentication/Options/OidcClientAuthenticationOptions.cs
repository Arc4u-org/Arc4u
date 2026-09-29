
using Arc4u.OAuth2.Options;

namespace Arc4u.OAuth2.Client.Authentication.Options;

    /// <summary>
    /// The options of the OpenID Connect authentication of a client (desktop or mobile) application, given to
    /// <see cref="Arc4u.OAuth2.Extensions.AuthenticationExtensions"/> <c>AddOidcClientAuthentication</c> or filled from the configuration by its configuration based overload.
    /// </summary>
    public class OidcClientAuthenticationOptions
    {
        /// <summary>Gets or sets the authority (identity provider). Required.</summary>
        public AuthorityOptions DefaultAuthority { get; set; } = default!;

        /// <summary>Gets or sets a value indicating whether the authority must be validated. The default is <see langword="true"/>. It is not currently read by the client authentication.</summary>
        public bool ValidateAuthority { get; set; } = true;

        /// <summary>Gets or sets the action that fills the <see cref="Options.OidcClientSettingsOption"/> (client id and scopes). Required.</summary>
        public Action<OidcClientSettingsOption>? OidcClientSettingsOption { get; set; }

        /// <summary>Gets or sets the action that fills the claim types identifying a user.</summary>
        public Action<ClaimsIdentifierOption>? ClaimsIdentifierOptions { get; set; }

        /// <summary>Gets or sets the redirect uri sent to the authority after the sign-in. The default is <c>/</c>.</summary>
        public string CallbackPath { get; set; } = "/";
        /// <summary>Gets or sets the uri the authority redirects to after the sign-out. The default is <c>/</c>.</summary>
        public string PostLogoutRedirectUri { get; set; } = "/";

        /// <summary>Gets or sets a value indicating whether the profile of the user is loaded from the user info endpoint. The default is <see langword="false"/>.</summary>
        public bool LoadProfile { get; set; }
        /// <summary>Gets or sets the remaining lifetime under which the access token is refreshed. The default is 5 minutes. It is not currently read by the client authentication.</summary>
        public TimeSpan ForceRefreshTimeoutTimeSpan { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Gets or sets the assumed lifetime of a refresh token. The default is 90 days. It is not currently read by the client authentication, the token provider assumes 90 days.</summary>
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(90);

        /// <summary>Gets or sets the tolerated clock difference when tokens are validated. The default is 5 minutes.</summary>
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Define the claim type used to identify the name of the user.
        /// </summary>
        public string NameClaimType { get; set; } = "name";

        /// <summary>
        /// Define the claim type used to identify the role of the user.
        /// </summary>
        public string RoleClaimType { get; set; } = "role";
    }
