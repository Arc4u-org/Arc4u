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

    }
}
