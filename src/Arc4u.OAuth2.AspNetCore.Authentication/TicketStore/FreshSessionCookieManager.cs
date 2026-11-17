using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.TicketStore;

/// <summary>
/// An <see cref="ICookieManager"/> that hides the authentication cookie from the cookie handler on the OpenID Connect callback
/// (a <c>POST</c> to <see cref="RemoteAuthenticationOptions.CallbackPath"/> of the <see cref="OpenIdConnectDefaults.AuthenticationScheme"/> scheme),
/// so the sign-in that follows always stores the ticket under a new session key.
/// </summary>
/// <remarks>
/// <para>
/// When a session store is used, <see cref="CookieAuthenticationHandler"/> does not create a new session on sign-in if the request already carries a
/// cookie pointing to a session that still exists: it renews that session key with the new ticket. A cookie planted in the victim's browser
/// (from a sibling subdomain, or from the same origin through XSS) would then receive the victim's session, and the attacker, who holds the same
/// cookie, would be signed in as the victim (session fixation). Hiding the incoming cookie on the callback makes the handler call
/// <see cref="ITicketStore.StoreAsync(Microsoft.AspNetCore.Authentication.AuthenticationTicket)"/> instead.
/// </para>
/// <para>
/// Only reading the request cookie is affected, and only on the callback: writing and deleting the cookie, and every other request, are
/// delegated unchanged to the inner manager. The OpenID Connect correlation and nonce cookies are not managed by this instance and are not affected.
/// The previous session, if any, is not removed: it expires with its time to live.
/// </para>
/// </remarks>
public sealed class FreshSessionCookieManager : ICookieManager
{
    /// <summary>Initializes a new instance of the <see cref="FreshSessionCookieManager"/> class.</summary>
    /// <param name="inner">The manager the cookies are read from and written to, usually a <see cref="ChunkingCookieManager"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="inner"/> is <see langword="null"/>.</exception>
    public FreshSessionCookieManager(ICookieManager inner)
    {
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>Gets the manager the cookies are read from and written to.</summary>
    public ICookieManager Inner { get; }

    /// <inheritdoc/>
    /// <remarks>Returns <see langword="null"/> on the OpenID Connect callback, so the cookie handler starts a new session.</remarks>
    public string? GetRequestCookie(HttpContext context, string key)
    {
        ArgumentNullException.ThrowIfNull(context);

        return IsSignInCallback(context) ? null : Inner.GetRequestCookie(context, key);
    }

    /// <inheritdoc/>
    public void AppendResponseCookie(HttpContext context, string key, string? value, CookieOptions options)
        => Inner.AppendResponseCookie(context, key, value, options);

    /// <inheritdoc/>
    public void DeleteCookie(HttpContext context, string key, CookieOptions options)
        => Inner.DeleteCookie(context, key, options);

    // The callback path is read at request time, so a value changed after AddOidcAuthentication (PostConfigure) is honoured.
    private static bool IsSignInCallback(HttpContext context)
    {
        if (!HttpMethods.IsPost(context.Request.Method))
        {
            return false;
        }

        var callbackPath = context.RequestServices?.GetService<IOptionsMonitor<OpenIdConnectOptions>>()?
            .Get(OpenIdConnectDefaults.AuthenticationScheme).CallbackPath ?? default;

        return callbackPath.HasValue && context.Request.Path.Equals(callbackPath, StringComparison.OrdinalIgnoreCase);
    }
}
