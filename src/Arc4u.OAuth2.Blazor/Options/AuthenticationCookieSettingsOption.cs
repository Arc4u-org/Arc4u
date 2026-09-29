namespace Arc4u.Blazor.Options;

/// <summary>
/// The settings of the cookie based authentication of a Blazor WebAssembly application: the application calls its Blazor server (SSR) backend, with the browser cookies, to get a token.
/// The options are read from the <c>Authentication:OAuth2.Settings</c> section by default and registered as the key/value settings named <c>OAuth2</c> (see <see cref="CookieAuthenticationExtensions"/>).
/// </summary>
public class AuthenticationCookieSettingsOption
{
    /// <summary>
    /// Gets or sets the name of the HttpClient, created by the <see cref="IHttpClientFactory"/>, used to call the authentication server. The default is <c>Authentication</c>.
    /// </summary>
    public string HttpClientName { get; set; } = "Authentication";

    /// <summary>
    /// Gets or sets the base uri of the Blazor SSR application. The default is <c>https://localhost</c>.
    /// </summary>
    public Uri BaseUri { get; set; } = new("https://localhost");

    /// <summary>
    /// Gets or sets the url, on the Blazor SSR application, called to get the token. The default is <c>/authentication/token</c>.
    /// </summary>
    public string TokenRequestUrl { get; set; } = "/authentication/token";

    /// <summary>
    /// Gets or sets the key of the token provider performing the authentication. The default is <c>Client</c> (<c>ClientTokenProvider</c>).
    /// </summary>
    public string ProviderId { get; set; } = "Client";
}

