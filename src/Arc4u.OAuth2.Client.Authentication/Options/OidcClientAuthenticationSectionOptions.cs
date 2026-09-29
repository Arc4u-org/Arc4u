
using Arc4u.OAuth2.Options;

namespace Arc4u.OAuth2.Client.Authentication.Options;

    /// <summary>
    /// The configuration layout of the OpenID Connect authentication of a client application. It is bound from the <c>Authentication</c> section (or the section name given to
    /// <c>AddOidcClientAuthentication</c>). The <c>*Path</c> properties are the paths of the other configuration sections read during registration.
    /// </summary>
    public class OidcClientAuthenticationSectionOptions
    {
        /// <summary>Gets or sets the authority (identity provider). Required.</summary>
        public AuthorityOptions DefaultAuthority { get; set; } = new AuthorityOptions();

        /// <summary>Gets or sets a value indicating whether the authority must be validated. The default is <see langword="true"/>. It is not currently read by the client authentication.</summary>
        public bool ValidateAuthority { get; set; } = true;

        /// <summary>Gets or sets the path of the section holding the <see cref="OidcClientSettingsOption"/>. The default is <c>Authentication:OidcClient.Settings</c>.</summary>
        public string OidcClientIdSettingsSectionPath { get; set; } = "Authentication:OidcClient.Settings";

        /// <summary>
        /// Gets or sets the paths of the sections holding the extra authorization and token request parameters. The defaults are <c>Authentication:OidcClient.Settings:AuthorizationEndpoint</c>
        /// and <c>Authentication:OidcClient.Settings:TokenEndpoint</c>.
        /// </summary>
        public ApiExtraContextAuthenticationSectionOption ApiExtraContextAuthenticationSection { get; set; } = new ApiExtraContextAuthenticationSectionOption
        {
            AuthorizationEndpointSectionPath = "Authentication:OidcClient.Settings:AuthorizationEndpoint",
            TokenEndpointSectionPath = "Authentication:OidcClient.Settings:TokenEndpoint"
        };
        /// <summary>Gets or sets the path of the section holding the claim types that identify a user. The default is <c>Authentication:ClaimsIdentifier</c>.</summary>
        public string ClaimsIdentifierSectionPath { get; set; } = "Authentication:ClaimsIdentifier";

        /// <summary>Gets or sets the configuration key holding the application name. The default is <c>Application.configuration:ApplicationName</c>. It is not read by the client authentication registration.</summary>
        public string ApplicationNameSectionPath { get; set; } = "Application.configuration:ApplicationName";

        /// <summary>Gets or sets the path of the section holding the domain mappings. The default is <c>Authentication:DomainsMapping</c>.</summary>
        public string DomainMappingsSectionPath { get; set; } = "Authentication:DomainsMapping";

        /// <summary>Gets or sets the path of the section holding the <see cref="ClaimsFillerOptions"/>. The default is <c>Authentication:ClaimsMiddleWare:ClaimsFiller</c>.</summary>
        public string ClaimsFillerSectionPath { get; set; } = "Authentication:ClaimsMiddleWare:ClaimsFiller";

        /// <summary>Gets or sets the remaining lifetime under which the access token is refreshed. The default is 5 minutes. It is not currently read by the client authentication.</summary>
        public TimeSpan ForceRefreshTimeoutTimeSpan { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Gets or sets the assumed lifetime of a refresh token. The default is 90 days. It is not currently read by the client authentication, the token provider assumes 90 days.</summary>
        public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(90);

        /// <summary>Gets or sets the tolerated clock difference when tokens are validated. The default is 5 minutes. The configuration based registration does not copy this value: the default of <see cref="OidcClientAuthenticationOptions.ClockSkew"/> applies.</summary>
        public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>Gets or sets the redirect uri sent to the authority after the sign-in. The default is <c>/</c>.</summary>
        public string CallbackPath { get; set; } = "/";
        /// <summary>Gets or sets the uri the authority redirects to after the sign-out. The default is <c>/</c>.</summary>
        public string PostLogoutRedirectUri { get; set; } = "/";

        /// <summary>Gets or sets a value indicating whether the profile of the user is loaded from the user info endpoint. The default is <see langword="false"/>.</summary>
        public bool LoadProfile { get; set; }
        /// <summary>
        /// Gets or sets the claim type holding the name of the user. The default is <c>name</c>. It is not currently read by the client authentication.
        /// </summary>
        public string NameClaimType { get; set; } = "name";

        /// <summary>
        /// Gets or sets the claim type holding the roles of the user. The default is <c>role</c>. It is not currently read by the client authentication.
        /// </summary>
        public string RoleClaimType { get; set; } = "role";

    }
