namespace Arc4u.OAuth2;

/// <summary>
/// Constants shared by the Arc4u OAuth2 authentication packages (authentication types, scheme names and default scopes).
/// </summary>
public class Constants
{
    /// <summary>The default name of the authentication cookie (<c>.Arc4u.Cookies</c>).</summary>
    public const string CookieName = ".Arc4u.Cookies";

    /// <summary>The name of the policy scheme (<c>Arc4uScheme</c>) that selects the JWT, cookie or OpenID Connect handler for each request.</summary>
    public const string ChallengePolicyScheme = "Arc4uScheme";

    /// <summary>The authentication type (<c>OAuth2</c>) given to an identity built from a bearer token.</summary>
    public const string BearerAuthenticationType = "OAuth2";

    /// <summary>The authentication type (<c>Cookies</c>) given to an identity built from an authentication cookie.</summary>
    public const string CookiesAuthenticationType = "Cookies";

    //public const string OAuth2OptionsName = "OAuth2";

    //public const string OpenIdOptionsName = "OpenId";

    /// <summary>The authentication type (<c>Inject</c>) given to an identity built by the bearer injector middleware.</summary>
    public const string InjectAuthenticationType = "Inject";

    /// <summary>The default OpenID Connect scope (<c>openid</c>) requested when no scope is configured.</summary>
    public const string OpenIdScope = "openid";
}
