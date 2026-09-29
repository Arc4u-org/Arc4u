namespace Arc4u.OAuth2.Token;

/// <summary>The keys of the key/value settings that configure the token providers.</summary>
public class TokenKeys
{
    /// <summary>
    /// Name for an Adal token provider.
    /// </summary>
    public const string ProviderIdKey = "ProviderId";

    /// <summary>
    /// The application unique Id used to identify in the STS the application.
    /// </summary>
    public const string ServiceApplicationIdKey = "ServiceApplicationId";

    /// <summary>
    /// The STS authority
    /// </summary>
    public const string AuthorityKey = "Authority";

    /// <summary>
    /// The ClientId used to identify the client definition in the sts.
    /// </summary>
    public const string ClientIdKey = "ClientId";

    /// <summary>
    /// The token will be used to identify the caller from a sevice => this is the root of the uri.
    /// </summary>
    public const string RootServiceUrlKey = "RootServiceUrl";

    /// <summary>
    /// The url registered in the sts.
    /// </summary>
    public const string RedirectUrl = "RedirectUrl";

    /// <summary>
    /// Application key used to certify the caller (User or Application).
    /// </summary>
    public const string ApplicationKey = "ApplicationKey";

    /// <summary>
    /// The certificate name or friendly name.
    /// </summary>
    public const string CertificateName = "CertificateName";

    /// <summary>
    /// The Sts provider
    /// </summary>
    public const string InstanceKey = "Instance";

    /// <summary>
    /// The identity provider id.
    /// </summary>
    public const string TenantIdKey = "TenantId";

    /// <summary>
    /// The certificate type.
    /// </summary>
    public const string FindType = "FindType";

    /// <summary>
    /// The certificate store location (Current user or Machine).
    /// </summary>
    public const string StoreLocation = "StoreLocation";

    /// <summary>
    /// The certificate folder where the certificate is stored.
    /// </summary>
    public const string StoreName = "StoreName";

    /// <summary>
    /// The shared key used to authenticate a process.
    /// </summary>
    public const string ClientSecret = "ClientSecret";

    /// <summary>
    /// Key used to store a user and password in a secure cache.
    /// </summary>
    public const string PasswordStoreKey = "PasswordStoreKey";

    /// <summary>
    /// Determine the type of the authentication (OAuth2Bearer or Cookies or ...)
    /// </summary>
    public const string AuthenticationTypeKey = "AuthenticationType";

    /// <summary>
    /// Header to use when injecting the client secret.
    /// </summary>
    public const string ClientSecretHeader = "HeaderKey";

    /// <summary>
    /// Scopes defined to identify the rigth(s) to a sts to access the requested resource.
    /// </summary>
    public const string Scopes = "Scopes";

    /// <summary>
    /// Define the Scope when used as a client calling the sts.
    /// </summary>
    public const string Scope = "Scope";

    /// <summary>
    /// A string containing the audiences separated by a comma.
    /// </summary>
    public const string Audiences = "Audiences";

    /// <summary>
    /// A string containing the audience when used as a client calling the sts.
    /// </summary>
    public const string Audience = "Audience";

    /// <summary>
    /// Open bag of extra request parameters (form-url-encoded) forwarded to the token endpoint,
    /// e.g. the Logto.io 'resource' or 'audience' parameters.
    /// </summary>
    public const string ExtraParameters = "ExtraParameters";

    /// <summary>
    /// Http factory name <see cref="IHttpClientFactory"/> given in settings to create an <see cref="HttpClient"/>
    /// </summary>
    public const string HttpClientName = "HttpClientName";

    /// <summary>
    /// Url used to contact an external token identity provider.
    /// </summary>
    public const string TokenRequestUrl = "TokenRequestUrl";
}
