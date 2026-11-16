using Arc4u.OAuth2.TokenProviders;

namespace Arc4u.OAuth2.Options
{
    /// <summary>
    /// The settings of an on-behalf-of token request. The entries of the <c>Authentication:OnBehalfOf</c> section are bound to this class;
    /// each one is registered as a <see cref="Arc4u.Configuration.SimpleKeyValueSettings"/> named as the entry.
    /// </summary>
    public class OnBehalfOfSettingsOptions
    {
        /// <summary>Gets or sets the key of the token provider that performs the request. The default is <see cref="AzureADOboTokenProvider.ProviderName"/> (<c>Obo</c>).</summary>
        public string ProviderId { get; set; } = AzureADOboTokenProvider.ProviderName;
        /// <summary>Gets or sets the client id of the application. Required.</summary>
        public string ClientId { get; set; } = default!;
        /// <summary>Gets or sets the scopes requested for the downstream API. At least one is required.</summary>
        public List<string> Scopes { get; set; } = [];
        /// <summary>Gets or sets the client secret of the application. Required.</summary>
        public string ClientSecret { get; set; } = default!;
        /// <summary>Gets or sets the authentication type of the resulting identity. The default is <see cref="Constants.InjectAuthenticationType"/>.</summary>
        public string AuthenticationType { get; set; } = Constants.InjectAuthenticationType;

        /// <summary>
        /// Gets or sets the name of the <see cref="HttpClient"/> of the <see cref="IHttpClientFactory"/> that calls the token endpoint, for example to configure
        /// a proxy or the Windows authentication of the token endpoint. When empty, the default client of the factory is used (a shared client when no factory is registered).
        /// </summary>
        public string? HttpClientName { get; set; }

        /// <summary>
        /// Gets or sets the cache in which the tokens of this setting are stored and for how long. When present, it is registered as named
        /// <see cref="TokenCacheOptions"/> under the setting name.
        /// In the configuration, a value that is not given (<c>CacheName</c> or <c>MaxTime</c>) is taken from the <c>Authentication:OnBehalfOfCache</c> section when it exists,
        /// otherwise from the default <see cref="TokenCacheOptions"/> (<c>Authentication:TokenCache</c>); when absent, the setting uses the
        /// <c>Authentication:OnBehalfOfCache</c> section, or the default options when there is none.
        /// From code (<c>AddOnBehalfOfSettings</c>), an empty <c>CacheName</c> takes the one of the default options, the <c>MaxTime</c> of the instance is used, and the
        /// <c>Authentication:OnBehalfOfCache</c> section does not apply.
        /// </summary>
        public TokenCacheOptions? TokenCache { get; set; }

    }
}
