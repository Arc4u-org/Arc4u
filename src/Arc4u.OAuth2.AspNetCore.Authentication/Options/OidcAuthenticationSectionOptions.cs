using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Arc4u.OAuth2.Options;

    /// <summary>
    /// The configuration layout of the OpenID Connect authentication. It is bound from the root <c>Authentication</c> section (or the section name given to
    /// <c>AddOidcAuthentication</c>).
    /// The <c>*Path</c> properties are the paths of the other configuration sections read during registration; each one has a default that can be overridden.
    /// </summary>
    public class OidcAuthenticationSectionOptions
    {
        /// <summary>Gets or sets the default authority (identity provider). Required.</summary>
        public AuthorityOptions DefaultAuthority { get; set; } = default!;

        /// <summary>Gets or sets the name of the authentication cookie. Required: use a name specific to the application, for example <c>.MyApp.Cookies</c>.</summary>
        public string CookieName { get; set; } = default!;

        /// <summary>Gets or sets a value indicating whether the issuer of the access token must be the default authority. The default is <see langword="true"/>. This value is not copied to <see cref="OidcAuthenticationOptions"/> by the configuration based registration.</summary>
        public bool ValidateAuthority { get; set; } = true;

        /// <summary>Gets or sets the path of the section holding the <see cref="OpenIdSettingsOption"/>. The default is <c>Authentication:OpenId.Settings</c>.</summary>
        public string OpenIdSettingsSectionPath { get; set; } = "Authentication:OpenId.Settings";

        /// <summary>Gets or sets the path of the section holding the claim types that identify a user. The default is <c>Authentication:ClaimsIdentifier</c>.</summary>
        public string ClaimsIdentifierSectionPath { get; set; } = "Authentication:ClaimsIdentifier";

        /// <summary>Gets or sets the path of the section describing the certificate used to protect the data protection keys. The default is <c>Authentication:DataProtection:EncryptionCertificate</c>.</summary>
        public string CertificateSectionPath { get; set; } = "Authentication:DataProtection:EncryptionCertificate";

        /// <summary>Gets or sets the path of the section holding the <see cref="Arc4u.OAuth2.TicketStore.CacheTicketStoreOptions"/> of the ticket store. The default is <c>Authentication:AuthenticationCacheTicketStore</c>.</summary>
        public string AuthenticationCacheTicketStorePath { get; set; } = "Authentication:AuthenticationCacheTicketStore";

        /// <summary>Gets or sets the path of the section holding the <see cref="Arc4u.OAuth2.DataProtection.CacheStoreOption"/> in which the data protection keys are stored. The default is <c>Authentication:DataProtection:CacheStore</c>.</summary>
        public string DataProtectionSectionPath { get; set; } = "Authentication:DataProtection:CacheStore";

        /// <summary>Gets or sets the lifetime of the data protection keys. The default is 365 days.</summary>
        public TimeSpan DefaultKeyLifetime { get; set; } = TimeSpan.FromDays(365);

        /// <summary>Gets or sets the configuration key whose value is the application name used by data protection. The default is <c>Application.configuration:ApplicationName</c>.</summary>
        public string ApplicationNameSectionPath { get; set; } = "Application.configuration:ApplicationName";

        /// <summary>Gets or sets the path of the section holding the <see cref="TokenCacheOptions"/>. The default is <c>Authentication:TokenCache</c>.</summary>
        public string TokenCacheSectionPath { get; set; } = "Authentication:TokenCache";

        /// <summary>Gets or sets the path of the section holding the domain mappings. The default is <c>Authentication:DomainsMapping</c>.</summary>
        public string DomainMappingsSectionPath { get; set; } = "Authentication:DomainsMapping";

        /// <summary>Gets or sets the path of the section holding the <see cref="ClaimsFillerOptions"/>. The default is <c>Authentication:ClaimsMiddleWare:ClaimsFiller</c>.</summary>
        public string ClaimsFillerSectionPath { get; set; } = "Authentication:ClaimsMiddleWare:ClaimsFiller";

        /// <summary>Gets or sets the remaining lifetime under which the access token is refreshed. The default is 5 minutes.</summary>
        public TimeSpan ForceRefreshTimeoutTimeSpan { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Gets or sets the assumed lifetime of a refresh token, which is also the maximum lifetime of the authentication cookie. The default is 90 days.</summary>
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(90);

        /// <summary>Gets or sets the path the identity provider redirects to after the sign-in. The default is <c>/signin-oidc</c>.</summary>
        public string CallbackPath { get; set; } = "/signin-oidc";

        /// <summary>Gets or sets the path of the section describing an optional certificate used as issuer signing key to validate tokens.</summary>
        public string? CertSecurityKeyPath { get; set; } = default!;

        /// <summary>
        /// For the other OIDC => ResponseType = OpenIdConnectResponseType.CodeIdTokenToken;
        /// For AzureAD, AzureB2C and Adfs => ResponseType = OpenIdConnectResponseType.Code;
        /// </summary>
        public string ResponseType { get; set; } = OpenIdConnectResponseType.Code;

        /// <summary>
        /// Time to live of the authentication ticket.
        /// Default is 7 days.
        /// </summary>
        public TimeSpan AuthenticationTicketTtl { get; set; } = TimeSpan.FromDays(7);

        /// <summary>
        /// By default the audience is validated. It is always better to do
        /// On Keycloak audience doesn't exist by default, so it is needed to disable it or add it.
        /// </summary>
        public bool ValidateAudience { get; set; } = true;

        /// <summary>
        /// Define the claim type used to identify the name of the user.
        /// </summary>
        public string NameClaimType { get; set; } = "name";

        /// <summary>
        /// Define the claim type used to identify the role of the user.
        /// </summary>
        public string RoleClaimType { get; set; } = "role";

        /// <summary>Gets or sets the name of the <see cref="OpenIdConnectRedirectBehavior"/> used to redirect to the authority: <c>RedirectGet</c> (the default) or <c>FormPost</c>.</summary>
        public string AuthenticationMethod { get; set; } = nameof(OpenIdConnectRedirectBehavior.RedirectGet);
    }
