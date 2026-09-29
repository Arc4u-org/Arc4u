using System.Security.Cryptography.X509Certificates;
using Arc4u.OAuth2.DataProtection;
using Arc4u.OAuth2.TicketStore;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Arc4u.OAuth2.Options;

    /// <summary>
    /// The options of the OpenID Connect authentication. They are given to
    /// <c>AddOidcAuthentication</c>
    /// or filled from the configuration by the configuration based overload. They are registered as <c>IOptions&lt;OidcAuthenticationOptions&gt;</c> and read by the cookie and OpenID Connect events.
    /// </summary>
    public class OidcAuthenticationOptions
    {
        /// <summary>Gets or sets the default authority (identity provider). Required.</summary>
        public AuthorityOptions DefaultAuthority { get; set; } = default!;

        /// <summary>Gets or sets the name of the authentication cookie. Use a name specific to the application.</summary>
        public string CookieName { get; set; } = default!;

        /// <summary>Gets or sets a value indicating whether the issuer of the access token received from the token endpoint must be the <see cref="DefaultAuthority"/>. The default is <see langword="true"/>.</summary>
        public bool ValidateAuthority { get; set; } = true;

        /// <summary>Gets or sets the action that fills the <see cref="OpenIdSettingsOption"/> (client id, secret, scopes, ...). Required.</summary>
        public Action<OpenIdSettingsOption> OpenIdSettingsOptions { get; set; } = default!;

        /// <summary>Gets or sets the action that fills the claim types identifying a user.</summary>
        public Action<ClaimsIdentifierOption> ClaimsIdentifierOptions { get; set; } = default!;

        /// <summary>Gets or sets the certificate that protects the data protection keys. Required.</summary>
        public X509Certificate2 DataProtectionCertificate { get; set; } = default!;

        /// <summary>Gets or sets the action that fills the <see cref="CacheTicketStoreOptions"/>. When set, the authentication tickets are stored in a cache instead of the cookie. Optional.</summary>
        public Action<CacheTicketStoreOptions> AuthenticationCacheTicketStoreOption { get; set; } = default!;

        /// <summary>Gets or sets the action that fills the <see cref="CacheStoreOption"/> describing the cache in which the data protection keys are persisted. Required.</summary>
        public Action<CacheStoreOption> DataProtectionCacheStoreOption { get; set; } = default!;

        /// <summary>Gets or sets the lifetime of the data protection keys. The default is 365 days.</summary>
        public TimeSpan DefaultKeyLifetime { get; set; } = TimeSpan.FromDays(365);

        /// <summary>Gets or sets the path the identity provider redirects to after the sign-in. The default is <c>/signin-oidc</c>.</summary>
        public string CallbackPath { get; set; } = "/signin-oidc";

        /// <summary>Gets or sets the application name used to isolate the data protection keys of the application.</summary>
        public string ApplicationName { get; set; } = default!;

        /// <summary>Gets or sets the remaining lifetime under which the access token is refreshed. The default is 5 minutes.</summary>
        public TimeSpan ForceRefreshTimeoutTimeSpan { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Gets or sets the assumed lifetime of a refresh token, which is also the maximum lifetime of the authentication cookie. The default is 90 days.</summary>
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(90);

        /// <summary>Gets or sets an optional certificate used as issuer signing key to validate tokens.</summary>
        public X509Certificate2? CertSecurityKey { get; set; } = default!;

        /// <summary>
        /// Gets or sets the OpenID Connect response type. The default is <see cref="OpenIdConnectResponseType.Code"/>.
        /// For AzureAD, AzureB2C and Adfs use <see cref="OpenIdConnectResponseType.Code"/>; for other identity providers <see cref="OpenIdConnectResponseType.CodeIdTokenToken"/> may be needed.
        /// </summary>
        public string ResponseType { get; set; } = OpenIdConnectResponseType.Code;

        /// <summary>
        /// Gets or sets the time to live of the authentication ticket. The cookie expires after the shorter of this value and <see cref="RefreshTokenLifetime"/>.
        /// The default is 7 days.
        /// </summary>
        public TimeSpan AuthenticationTicketTtl { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// Gets or sets a value indicating whether the audience of the access token received from the token endpoint must be one of the audiences of the OpenID settings.
        /// The default is <see langword="true"/>. Identity providers like Keycloak do not issue an audience by default: disable it or configure the audience in the identity provider.
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>
        /// Gets or sets the claim type holding the name of the user. The default is <c>name</c>.
        /// </summary>
        public string NameClaimType { get; set; } = "name";

        /// <summary>
        /// Gets or sets the claim type holding the roles of the user. The default is <c>role</c>.
        /// </summary>
        public string RoleClaimType { get; set; } = "role";

        /// <summary>Gets or sets how the request is sent to the authority. The default is <see cref="OpenIdConnectRedirectBehavior.RedirectGet"/>.</summary>
        public OpenIdConnectRedirectBehavior AuthenticationMethod { get; set; } = OpenIdConnectRedirectBehavior.RedirectGet;
    }
