using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Options;

/// <summary>The cookie settings shared by <see cref="ConfigureCookieWithTicketStoreAuthenticationOptions"/> and <see cref="ConfigureStandardCookieAuthenticationOptions"/>.</summary>
internal static class AuthenticationCookieDefaults
{
    /// <summary>The prefix that makes a browser accept the cookie only when it is secure, host-only and set for <c>Path=/</c>.</summary>
    internal const string HostPrefix = "__Host-";

    internal static void Apply(CookieAuthenticationOptions options, OidcAuthenticationOptions oidcOptions, ILogger logger)
    {
        options.Cookie.Name = oidcOptions.CookieName;
        options.SlidingExpiration = true;
        // Set the expiration time span to the minimum of AuthenticationTicketTTL and RefreshTokenLifetime
        // to avoid having a ticket that is expired but still valid.
        options.ExpireTimeSpan = oidcOptions.AuthenticationTicketTtl < oidcOptions.RefreshTokenLifetime
            ? oidcOptions.AuthenticationTicketTtl
            : oidcOptions.RefreshTokenLifetime;
        options.EventsType = typeof(CookieAuthenticationEvents);
        // we need this to persist the cookie and keep the user logged in.
        options.Cookie.IsEssential = true;
        // Need to set the same site to Lax to allow the cookie to be sent to the portal from the authority provider.
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.MaxAge = oidcOptions.RefreshTokenLifetime;
        // Host-only and for the whole site: what a __Host- cookie requires. Left unset, the path falls back to the request path base,
        // so a UsePathBase or a branch mapping would issue Path=/x, and browsers silently drop such a __Host- cookie.
        options.Cookie.Path = "/";
        options.Cookie.Domain = null;

        if (string.IsNullOrEmpty(oidcOptions.CookieName) || !oidcOptions.CookieName.StartsWith(HostPrefix, StringComparison.Ordinal))
        {
            logger.LogCookieWithoutHostPrefix(oidcOptions.CookieName ?? string.Empty);
        }
    }
}
