using System.Security.Claims;
using Arc4u.OAuth2;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Arc4uClaimTypes = Arc4u.IdentityModel.Claims.ClaimTypes;

namespace Arc4u.Blazor;

/// <summary>
/// Checks the sign-in against the session store of the authentication cookie: the sign-in exists as long as the store still holds the
/// ticket whose key the principal carries (the <see cref="Arc4uClaimTypes.SessionKey"/> claim, stamped by <see cref="SessionKeyTicketStore"/>,
/// which <c>AddAppPrincipalCircuitRevalidation</c> wraps around the cookie's session store).
/// </summary>
/// <remarks>
/// <para>
/// Sign-out removes the ticket from the store, and so does its expiry, so the check follows both. The ticket belongs to that sign-in alone:
/// a sign-out ends the circuits of that browser session, not the other sessions of the same user.
/// </para>
/// <para>
/// The sign-in is ended when the store no longer holds the ticket, when the ticket has expired, or when it belongs to another user. When the
/// principal carries no session key or the cookie has no session store (the whole ticket is then in the cookie, and the server keeps
/// nothing to check against), the sign-in is considered alive: there is nothing to check, and the user keeps the circuit until it reconnects.
/// </para>
/// </remarks>
public sealed partial class SessionStoreSignInValidator : ISignInValidator
{
    private readonly IOptionsMonitor<CookieAuthenticationOptions> _cookieOptions;
    private readonly CircuitRevalidationOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SessionStoreSignInValidator> _logger;
    private int _cannotBeCheckedWarned;

    /// <summary>Initializes a new instance of the <see cref="SessionStoreSignInValidator"/> class.</summary>
    /// <param name="cookieOptions">The cookie options, giving the session store.</param>
    /// <param name="options">The circuit revalidation options, giving the cookie scheme.</param>
    /// <param name="logger">The logger.</param>
    public SessionStoreSignInValidator(
        IOptionsMonitor<CookieAuthenticationOptions> cookieOptions,
        IOptions<CircuitRevalidationOptions> options,
        ILogger<SessionStoreSignInValidator> logger)
        : this(cookieOptions, options, logger, TimeProvider.System)
    {
    }

    internal SessionStoreSignInValidator(
        IOptionsMonitor<CookieAuthenticationOptions> cookieOptions,
        IOptions<CircuitRevalidationOptions> options,
        ILogger<SessionStoreSignInValidator> logger,
        TimeProvider timeProvider)
    {
        _cookieOptions = cookieOptions ?? throw new ArgumentNullException(nameof(cookieOptions));
        _options = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <inheritdoc/>
    public async Task<bool> IsSignInAliveAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Identity is not { IsAuthenticated: true })
        {
            return false;
        }

        var sessionKey = user.FindFirst(Arc4uClaimTypes.SessionKey)?.Value;
        var sessionStore = _cookieOptions.Get(_options.CookieScheme).SessionStore;
        if (string.IsNullOrEmpty(sessionKey) || sessionStore is null)
        {
            // Warned once (a missing AddAppPrincipalCircuitRevalidation, or a cookie without session store), then at the Debug level.
            if (Interlocked.Exchange(ref _cannotBeCheckedWarned, 1) == 0)
            {
                LogCannotBeCheckedWarning(_logger, _options.CookieScheme);
            }
            else
            {
                LogCannotBeChecked(_logger, _options.CookieScheme);
            }

            return true;
        }

        var ticket = await sessionStore.RetrieveAsync(sessionKey, cancellationToken).ConfigureAwait(false);
        if (ticket is null)
        {
            return false;
        }

        if (ticket.Properties.ExpiresUtc is { } expiresUtc && expiresUtc < _timeProvider.GetUtcNow())
        {
            return false;
        }

        // A session key pointing at another user's ticket proves nothing about this sign-in.
        return ClaimsPrincipalSubject.IsSameSubject(ticket.Principal, user);
    }

    [LoggerMessage(EventId = 9303, Level = LogLevel.Warning,
        Message = "The sign-in of the circuit cannot be checked: the principal carries no session key or the {CookieScheme} cookie has no session store. Call AddAppPrincipalCircuitRevalidation; circuits keep their user until they reconnect.")]
    private static partial void LogCannotBeCheckedWarning(ILogger logger, string cookieScheme);

    [LoggerMessage(EventId = 9301, Level = LogLevel.Debug,
        Message = "The sign-in of the circuit cannot be checked: the principal carries no session key or the {CookieScheme} cookie has no session store.")]
    private static partial void LogCannotBeChecked(ILogger logger, string cookieScheme);
}
