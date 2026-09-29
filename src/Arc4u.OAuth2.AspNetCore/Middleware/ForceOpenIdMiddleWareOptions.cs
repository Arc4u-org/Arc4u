namespace Arc4u.OAuth2.Middleware;

/// <summary>Configures the <see cref="ForceOpenIdMiddleWare"/>.</summary>
public class ForceOpenIdMiddleWareOptions
{
    /// <summary>
    /// Gets or sets the paths for which an unauthenticated request is redirected to the OpenID Connect login. A path is matched from the start of the request path, ignoring the case;
    /// <c>*</c> is a wildcard matching any characters, for example <c>/swagger*</c>. When the list is empty, the middleware does nothing.
    /// </summary>
    public List<string> ForceAuthenticationForPaths { get; set; } = [];

    /// <summary>
    /// Gets or sets the absolute base url (scheme, host and port) of the application as seen by the users, for example when it runs behind a reverse proxy.
    /// After the login the user is redirected to the requested path and query string on this base url. If not set (or not an absolute url), the url of the request is used.
    /// </summary>
    public string RedirectUrlForAuthority { get; set; } = string.Empty;
}

