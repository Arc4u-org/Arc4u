using System.Security.Claims;
using Arc4u.Dependency.Attribute;
using Arc4u.Security.Principal;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Arc4u.Blazor;

/// <summary>
/// Server (Interactive Server / SignalR circuit) counterpart of the WebAssembly
/// <c>Arc4u.Blazor.AppPrincipalFromAuthenticationState</c> used on the client: it sets the user's <see cref="AppPrincipal"/>
/// in the circuit, and ends the circuit's sign-in when the user's sign-in has ended.
/// </summary>
/// <remarks>
/// <para>
/// On the server, <c>AppPrincipalTransform</c> (an <c>IClaimsTransformation</c>) runs in the HTTP request scope during
/// <c>UseAuthentication()</c>, so the <see cref="IApplicationContext"/> it populates belongs to that request scope. The circuit uses its
/// own DI scope, whose <see cref="IApplicationContext"/> is empty. This provider (which the Blazor endpoint seeds from
/// <c>HttpContext.User</c>) rebuilds the <see cref="AppPrincipal"/> whenever the authentication state is read, and on every
/// <see cref="AuthenticationStateProvider.AuthenticationStateChanged"/> (a reconnection with another user or without a user), and sets it on
/// the scope's <see cref="IApplicationContext"/>.
/// </para>
/// <para>
/// Every <see cref="CircuitRevalidationOptions.Interval"/> (two minutes by default) it also asks the <see cref="ISignInValidator"/> whether
/// the sign-in of an authenticated circuit still exists: by default <see cref="SessionStoreSignInValidator"/>, which checks the cookie's
/// session store. When the sign-in has ended (sign-out in another tab, expired session), or the check throws, Blazor moves the circuit to an
/// anonymous state. An anonymous visitor is never checked.
/// </para>
/// <para>
/// Whenever the state turns anonymous, the user is removed: the principal of the circuit, and the principal and tokens
/// (<c>TokenRefreshInfo</c>) of the SignalR connection's request scope, which the <c>JwtHttpHandler</c> and the gRPC interceptor read.
/// Every operation the stale page starts from then on finds no user, and no API is called with the user's token. Components that read the
/// authentication state (<c>AuthorizeView</c>, <c>[Authorize]</c> pages rendered interactively) render their anonymous content.
/// </para>
/// <para>
/// Register it with <c>AddAppPrincipalCircuitRevalidation</c>: that call also wraps the cookie's session store in a
/// <see cref="SessionKeyTicketStore"/>, without which the principals carry no session key and the default validator cannot check anything
/// (it then logs a warning once, and the circuits keep their user until they reconnect). Set
/// <see cref="CircuitRevalidationOptions.Enabled"/> to <see langword="false"/> to turn the check off.
/// </para>
/// </remarks>
[Export(typeof(AuthenticationStateProvider)), Scoped]
public sealed partial class AppPrincipalServerAuthenticationStateProvider : RevalidatingServerAuthenticationStateProvider
{
    private readonly IClaimProfileFiller _claimsFiller;
    private readonly IClaimAuthorizationFiller _authorizationFiller;
    private readonly IApplicationContext _applicationContext;
    private readonly IServiceProvider? _scopeServices;
    private readonly ISignInValidator? _signInValidator;
    private readonly TimeSpan _interval;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppPrincipalServerAuthenticationStateProvider"/> class without revalidation and
    /// without access to the scope's services: the circuit keeps its user until it reconnects. Prefer the constructors used by the
    /// dependency injection.
    /// </summary>
    /// <param name="claimsFiller">The filler that builds the user profile from the claims.</param>
    /// <param name="authorizationFiller">The filler that builds the authorization from the claims.</param>
    /// <param name="applicationContext">The circuit-scoped application context that receives the principal.</param>
    public AppPrincipalServerAuthenticationStateProvider(
        IClaimProfileFiller claimsFiller,
        IClaimAuthorizationFiller authorizationFiller,
        IApplicationContext applicationContext)
        : this(NullLoggerFactory.Instance, claimsFiller, authorizationFiller, applicationContext, null, Options.Create(new CircuitRevalidationOptions { Enabled = false }), null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppPrincipalServerAuthenticationStateProvider"/> class with the <see cref="ISignInValidator"/>
    /// and the <see cref="CircuitRevalidationOptions"/> registered in <paramref name="scopeServices"/>; when no validator is registered, a
    /// <see cref="SessionStoreSignInValidator"/> is used, and the default options when none are configured.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="claimsFiller">The filler that builds the user profile from the claims.</param>
    /// <param name="authorizationFiller">The filler that builds the authorization from the claims.</param>
    /// <param name="applicationContext">The scoped application context that receives the principal.</param>
    /// <param name="scopeServices">The services of the scope (circuit or request).</param>
    public AppPrincipalServerAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IClaimProfileFiller claimsFiller,
        IClaimAuthorizationFiller authorizationFiller,
        IApplicationContext applicationContext,
        IServiceProvider scopeServices)
        : this(
            loggerFactory,
            claimsFiller,
            authorizationFiller,
            applicationContext,
            DefaultValidator(loggerFactory, scopeServices),
            scopeServices?.GetService<IOptions<CircuitRevalidationOptions>>() ?? Options.Create(new CircuitRevalidationOptions()),
            scopeServices)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="AppPrincipalServerAuthenticationStateProvider"/> class.</summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="claimsFiller">The filler that builds the user profile from the claims.</param>
    /// <param name="authorizationFiller">The filler that builds the authorization from the claims.</param>
    /// <param name="applicationContext">The scoped application context that receives the principal.</param>
    /// <param name="signInValidator">Tells whether the sign-in still exists; <see langword="null"/> turns the revalidation off.</param>
    /// <param name="options">The revalidation options.</param>
    /// <param name="scopeServices">The services of the scope (circuit or request), used to remove the user's tokens, and the user of the connection's request scope, when the state turns anonymous.</param>
    /// <exception cref="ArgumentOutOfRangeException">The revalidation is enabled and <see cref="CircuitRevalidationOptions.Interval"/> is not more than zero and at most <see cref="CircuitRevalidationOptions.MaxInterval"/>.</exception>
    public AppPrincipalServerAuthenticationStateProvider(
        ILoggerFactory loggerFactory,
        IClaimProfileFiller claimsFiller,
        IClaimAuthorizationFiller authorizationFiller,
        IApplicationContext applicationContext,
        ISignInValidator? signInValidator,
        IOptions<CircuitRevalidationOptions> options,
        IServiceProvider? scopeServices)
        : base(loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)))
    {
        _claimsFiller = claimsFiller ?? throw new ArgumentNullException(nameof(claimsFiller));
        _authorizationFiller = authorizationFiller ?? throw new ArgumentNullException(nameof(authorizationFiller));
        _applicationContext = applicationContext ?? throw new ArgumentNullException(nameof(applicationContext));
        _scopeServices = scopeServices;
        _logger = loggerFactory.CreateLogger<AppPrincipalServerAuthenticationStateProvider>();

        var revalidation = (options ?? throw new ArgumentNullException(nameof(options))).Value;
        if (revalidation.Enabled && signInValidator is not null)
        {
            if (revalidation.Interval <= TimeSpan.Zero || revalidation.Interval > CircuitRevalidationOptions.MaxInterval)
            {
                throw new ArgumentOutOfRangeException(nameof(options), revalidation.Interval,
                    $"{nameof(CircuitRevalidationOptions)}.{nameof(CircuitRevalidationOptions.Interval)} must be more than zero and at most {CircuitRevalidationOptions.MaxInterval}.");
            }

            _signInValidator = signInValidator;
            _interval = revalidation.Interval;
        }
        else
        {
            // No check: the loop of the base class only wakes up once a day and always keeps the user.
            _signInValidator = null;
            _interval = CircuitRevalidationOptions.MaxInterval;
        }

        AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    /// <inheritdoc/>
    protected override TimeSpan RevalidationInterval => _interval;

    /// <summary>Gets the authentication state and, for an authenticated user, sets the <see cref="AppPrincipal"/> in the scope's <see cref="IApplicationContext"/>. For an anonymous user, the user is removed.</summary>
    /// <returns>The authentication state, whose user is the <see cref="AppPrincipal"/> when the user is authenticated.</returns>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // The base provider returns the state seeded from HttpContext.User when the circuit was established.
        var state = await base.GetAuthenticationStateAsync().ConfigureAwait(false);

        // Same construction as the WASM AppPrincipalFromAuthenticationState, reusing the identity
        // already carried by the state (it keeps its original authentication type and claims).
        var principal = ApplicationContextInitializer.SetPrincipal(
            state.User.Identity as ClaimsIdentity,
            _authorizationFiller,
            _claimsFiller,
            _applicationContext,
            _scopeServices);

        // Anonymous user: the context is cleared and the framework state is returned untouched.
        return principal is null ? state : new AuthenticationState(principal);
    }

    /// <inheritdoc/>
    protected override async Task<bool> ValidateAuthenticationStateAsync(AuthenticationState authenticationState, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(authenticationState);

        if (_signInValidator is null)
        {
            return true;
        }

        var isAlive = await _signInValidator.IsSignInAliveAsync(authenticationState.User, cancellationToken).ConfigureAwait(false);
        if (!isAlive)
        {
            // Removed here as well as on the state change that follows, so nothing acts as the user between the two.
            ApplicationContextInitializer.ClearUser(_applicationContext, _scopeServices);
            LogSignInEnded(_logger);
        }

        return isAlive;
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            AuthenticationStateChanged -= OnAuthenticationStateChanged;
        }

        base.Dispose(disposing);
    }

    // The registered validator, otherwise the session store check, built from the services every ASP.NET Core application has.
    private static ISignInValidator? DefaultValidator(ILoggerFactory loggerFactory, IServiceProvider? scopeServices)
    {
        if (scopeServices is null)
        {
            return null;
        }

        if (scopeServices.GetService<ISignInValidator>() is { } registered)
        {
            return registered;
        }

        if (scopeServices.GetService<IOptionsMonitor<CookieAuthenticationOptions>>() is not { } cookieOptions)
        {
            return null;
        }

        var options = scopeServices.GetService<IOptions<CircuitRevalidationOptions>>() ?? Options.Create(new CircuitRevalidationOptions());
        return new SessionStoreSignInValidator(cookieOptions, options, (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<SessionStoreSignInValidator>());
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
        => _ = ApplicationContextInitializer.SetPrincipalAsync(task, _authorizationFiller, _claimsFiller, _applicationContext, _scopeServices);

    [LoggerMessage(EventId = 9300, Level = LogLevel.Information,
        Message = "The sign-in behind this circuit has ended; the circuit is now anonymous.")]
    private static partial void LogSignInEnded(ILogger logger);
}
