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
    /// The url to redirect to the authority. If not set, the current url is used.
    /// </summary>
    public string RedirectUrlForAuthority { get; set; } = string.Empty;
}

