
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

        /// <summary>Gets or sets the action that fills the <see cref="Options.OidcClientSettingsOption"/> (client id and scopes). Required.</summary>
        public Action<OidcClientSettingsOption>? OidcClientSettingsOption { get; set; }

        /// <summary>Gets or sets the action that fills the claim types identifying a user. When <see langword="null"/>, the object identifier claim types (<c>ObjectIdentifier</c> and <c>OID</c> of <c>ClaimTypes</c>) are used.</summary>
        public Action<ClaimsIdentifierOption>? ClaimsIdentifierOptions { get; set; }

        /// <summary>Gets or sets the redirect uri sent to the authority after the sign-in. The default is <c>/</c>.</summary>
        public string CallbackPath { get; set; } = "/";
        /// <summary>Gets or sets the uri the authority redirects to after the sign-out. The default is <c>/</c>.</summary>
        public string PostLogoutRedirectUri { get; set; } = "/";

        /// <summary>Gets or sets a value indicating whether the profile of the user is loaded from the user info endpoint. The default is <see langword="false"/>.</summary>
        public bool LoadProfile { get; set; }
        /// <summary>Gets or sets the remaining lifetime under which the access token is refreshed. The default is 5 minutes. The access token is renewed with the refresh token once it expires in less than this value.</summary>
        public TimeSpan ForceRefreshTimeoutTimeSpan { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Gets or sets the assumed lifetime of a refresh token. The default is 90 days. A refresh token older than this value is not used: the user logs in again. Set it to the lifetime configured in the identity provider.</summary>
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(90);

        /// <summary>Gets or sets the tolerated clock difference when tokens are validated. The default is 5 minutes.</summary>
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the longest time an interactive login (in the browser) can take. The default is 5 minutes. The token calls are made one at a time while the user logs in:
        /// a call that waits longer than this value fails, and a login that does not complete in time is cancelled.
        /// </summary>
        public TimeSpan LoginTimeout { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Gets or sets the longest time a read, write or removal of the tokens in the secure cache can take during a sign-out or a token write. The default is 30 seconds.</summary>
        public TimeSpan SecureCacheTimeout { get; set; } = TimeSpan.FromSeconds(30);
    }
