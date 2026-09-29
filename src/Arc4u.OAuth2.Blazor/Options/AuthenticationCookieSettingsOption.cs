namespace Arc4u.Blazor.Options;

/// <summary>
/// The settings of the cookie based authentication of a Blazor WebAssembly application: the application calls its Blazor server (SSR) backend, with the browser cookies, to get a token.
/// The options are read from the <c>Authentication:OAuth2.Settings</c> section by default and registered as the key/value settings named <c>OAuth2</c> (see <see cref="CookieAuthenticationExtensions"/>).
/// </summary>
public class AuthenticationCookieSettingsOption
{
    /// <summary>
    /// The name of the HttpClient used to call the authentication server, from an instance of IHttpClientFactory.
    /// </summary>
    public string HttpClientName { get; set; } = "Authentication";

    /// <summary>
    /// The base uri of the Blazor SSR application.
    /// </summary>
    public Uri BaseUri { get; set; } = new("https://localhost");

    /// <summary>
    /// The url on the Blazor SSR application to call to get the token.
    /// </summary>
    public string TokenRequestUrl { get; set; } = "/authentication/token";

    /// <summary>
    /// The id of the provider performing the authentication.
    /// </summary>
    public string ProviderId { get; set; } = "Client";
}

