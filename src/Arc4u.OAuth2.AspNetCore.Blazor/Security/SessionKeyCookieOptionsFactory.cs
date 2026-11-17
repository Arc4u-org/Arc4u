using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Arc4u.Blazor;

/// <summary>
/// Builds the cookie options as usual, then wraps the session store of the circuit revalidation's cookie scheme in a
/// <see cref="SessionKeyTicketStore"/>, so the principals read from it carry their session key.
/// </summary>
/// <remarks>
/// The wrap runs after every configure and post-configure step, whatever their registration order, so
/// <c>AddAppPrincipalCircuitRevalidation</c> can be called before or after <c>AddOidcAuthentication</c>. It is not an
/// <see cref="IPostConfigureOptions{TOptions}"/> on purpose: Arc4u registers its cookie configuration with <c>TryAddSingleton</c>, so any
/// post-configure step of the cookie options registered before it would silently replace it (and with it the session store and the cookie
/// hardening).
/// </remarks>
internal sealed partial class SessionKeyCookieOptionsFactory : IOptionsFactory<CookieAuthenticationOptions>
{
    private readonly OptionsFactory<CookieAuthenticationOptions> _inner;
    private readonly IOptionsMonitor<CircuitRevalidationOptions> _revalidation;
    private readonly ILogger _logger;

    public SessionKeyCookieOptionsFactory(
        IEnumerable<IConfigureOptions<CookieAuthenticationOptions>> setups,
        IEnumerable<IPostConfigureOptions<CookieAuthenticationOptions>> postConfigures,
        IEnumerable<IValidateOptions<CookieAuthenticationOptions>> validations,
        IOptionsMonitor<CircuitRevalidationOptions> revalidation,
        ILoggerFactory? loggerFactory = null)
    {
        _inner = new OptionsFactory<CookieAuthenticationOptions>(setups, postConfigures, validations);
        _revalidation = revalidation ?? throw new ArgumentNullException(nameof(revalidation));
        _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SessionKeyCookieOptionsFactory>();
    }

    public CookieAuthenticationOptions Create(string name)
    {
        var options = _inner.Create(name);

        var revalidation = _revalidation.CurrentValue;
        if (!revalidation.Enabled || !string.Equals(name, revalidation.CookieScheme, StringComparison.Ordinal))
        {
            return options;
        }

        if (options.SessionStore is null)
        {
            LogNoSessionStore(_logger, name);
            return options;
        }

        SessionKeyTicketStore.Wrap(options);
        return options;
    }

    [LoggerMessage(EventId = 9302, Level = LogLevel.Warning,
        Message = "The {CookieScheme} cookie has no session store: the circuit revalidation cannot check the sign-ins, and circuits keep their user until they reconnect.")]
    private static partial void LogNoSessionStore(ILogger logger, string cookieScheme);
}
