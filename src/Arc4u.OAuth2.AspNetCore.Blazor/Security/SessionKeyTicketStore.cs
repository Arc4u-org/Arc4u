using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Arc4uClaimTypes = Arc4u.IdentityModel.Claims.ClaimTypes;

namespace Arc4u.Blazor;

/// <summary>
/// Wraps the session store of the authentication cookie so the ticket it hands back carries its own session key as the
/// <see cref="Arc4uClaimTypes.SessionKey"/> claim.
/// </summary>
/// <remarks>
/// <para>
/// With a session store, the cookie holds only the session key and the ticket lives in the store. The cookie handler reads the key out of
/// the cookie but does not put it on the principal it returns, so nothing downstream of authentication (in particular a Blazor circuit,
/// which only sees the principal) can tell which session it belongs to. Stamping the key when the ticket is read lets that code ask the
/// store whether the session still exists: sign-out removes it, and so does its expiry. The key is stamped on every read, so the sessions
/// created before this store was added carry it too.
/// </para>
/// <para>
/// The claim is never persisted: it is removed from the ticket before it is stored or renewed, and a stale value is replaced on every read.
/// The stored ticket is never modified, as a copy is stamped. The claim must never be sent to a browser: see
/// <see cref="AuthenticationStateClaimsSerializer"/>.
/// </para>
/// <para>
/// <c>AddAppPrincipalCircuitRevalidation</c> wraps the session store of the cookie with it. Only applications with Blazor circuits get the
/// claim: an MVC or Razor Pages application has no use for it.
/// </para>
/// </remarks>
public sealed class SessionKeyTicketStore : ITicketStore
{
    private readonly ITicketStore _inner;

    /// <summary>Initializes a new instance of the <see cref="SessionKeyTicketStore"/> class.</summary>
    /// <param name="inner">The store that keeps the tickets.</param>
    /// <exception cref="ArgumentNullException"><paramref name="inner"/> is <see langword="null"/>.</exception>
    public SessionKeyTicketStore(ITicketStore inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>Gets the store this one wraps.</summary>
    public ITicketStore Inner => _inner;

    /// <summary>Wraps <paramref name="store"/>, unless it is already a <see cref="SessionKeyTicketStore"/>.</summary>
    /// <param name="store">The store to wrap.</param>
    /// <returns>A <see cref="SessionKeyTicketStore"/> over <paramref name="store"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> is <see langword="null"/>.</exception>
    public static SessionKeyTicketStore Wrap(ITicketStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        return store as SessionKeyTicketStore ?? new SessionKeyTicketStore(store);
    }

    /// <summary>Wraps the session store of <paramref name="options"/>, if it has one and it is not already wrapped.</summary>
    /// <param name="options">The cookie options.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static void Wrap(CookieAuthenticationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.SessionStore is { } store)
        {
            options.SessionStore = Wrap(store);
        }
    }

    /// <inheritdoc/>
    public Task<string> StoreAsync(AuthenticationTicket ticket) => _inner.StoreAsync(Strip(ticket));

    /// <inheritdoc/>
    public Task<string> StoreAsync(AuthenticationTicket ticket, CancellationToken cancellationToken) =>
        _inner.StoreAsync(Strip(ticket), cancellationToken);

    /// <inheritdoc/>
    public Task<string> StoreAsync(AuthenticationTicket ticket, HttpContext httpContext, CancellationToken cancellationToken) =>
        _inner.StoreAsync(Strip(ticket), httpContext, cancellationToken);

    /// <inheritdoc/>
    public Task RenewAsync(string key, AuthenticationTicket ticket) => _inner.RenewAsync(key, Strip(ticket));

    /// <inheritdoc/>
    public Task RenewAsync(string key, AuthenticationTicket ticket, CancellationToken cancellationToken) =>
        _inner.RenewAsync(key, Strip(ticket), cancellationToken);

    /// <inheritdoc/>
    public Task RenewAsync(string key, AuthenticationTicket ticket, HttpContext httpContext, CancellationToken cancellationToken) =>
        _inner.RenewAsync(key, Strip(ticket), httpContext, cancellationToken);

    /// <inheritdoc/>
    public async Task<AuthenticationTicket?> RetrieveAsync(string key) =>
        Stamp(await _inner.RetrieveAsync(key).ConfigureAwait(false), key);

    /// <inheritdoc/>
    public async Task<AuthenticationTicket?> RetrieveAsync(string key, CancellationToken cancellationToken) =>
        Stamp(await _inner.RetrieveAsync(key, cancellationToken).ConfigureAwait(false), key);

    /// <inheritdoc/>
    public async Task<AuthenticationTicket?> RetrieveAsync(string key, HttpContext httpContext, CancellationToken cancellationToken) =>
        Stamp(await _inner.RetrieveAsync(key, httpContext, cancellationToken).ConfigureAwait(false), key);

    /// <inheritdoc/>
    public Task RemoveAsync(string key) => _inner.RemoveAsync(key);

    /// <inheritdoc/>
    public Task RemoveAsync(string key, CancellationToken cancellationToken) => _inner.RemoveAsync(key, cancellationToken);

    /// <inheritdoc/>
    public Task RemoveAsync(string key, HttpContext httpContext, CancellationToken cancellationToken) =>
        _inner.RemoveAsync(key, httpContext, cancellationToken);

    // A copy of the ticket whose identities carry no session key claim; the ticket itself when none does.
    private static AuthenticationTicket Strip(AuthenticationTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        if (!ticket.Principal.HasClaim(c => c.Type == Arc4uClaimTypes.SessionKey))
        {
            return ticket;
        }

        var principal = Copy(ticket.Principal);
        return new AuthenticationTicket(principal, ticket.Properties, ticket.AuthenticationScheme);
    }

    // A copy of the ticket whose first identity carries the key as its only session key claim. The stored ticket is not modified.
    private static AuthenticationTicket? Stamp(AuthenticationTicket? ticket, string key)
    {
        if (ticket is null)
        {
            return null;
        }

        var principal = Copy(ticket.Principal);
        if (principal.Identities.FirstOrDefault() is not { } identity)
        {
            return ticket;
        }

        identity.AddClaim(new Claim(Arc4uClaimTypes.SessionKey, key));
        return new AuthenticationTicket(principal, ticket.Properties, ticket.AuthenticationScheme);
    }

    // ClaimsPrincipal.Clone shares the identities, so they are cloned one by one, without any session key claim.
    private static ClaimsPrincipal Copy(ClaimsPrincipal principal)
    {
        var copy = new ClaimsPrincipal(principal.Identities.Select(i => i.Clone()));
        foreach (var identity in copy.Identities)
        {
            foreach (var claim in identity.FindAll(Arc4uClaimTypes.SessionKey).ToList())
            {
                identity.RemoveClaim(claim);
            }
        }

        return copy;
    }
}
