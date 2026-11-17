using System.Security.Claims;
using System.Security.Principal;
using Arc4u.Blazor;
using Arc4u.OAuth2;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Arc4uClaimTypes = Arc4u.IdentityModel.Claims.ClaimTypes;

namespace Arc4u.UnitTest.Blazor;

/// <summary>
/// A Blazor circuit stops acting as the user once the sign-in it started with has ended (sign-out in another tab, expired session):
/// it checks the cookie's session store periodically, and when the state turns anonymous it removes the user's principal and tokens
/// from the circuit and from the request scope of its SignalR connection, which the JwtHttpHandler and the gRPC interceptor read
/// (Arc4u.Portal #702).
/// </summary>
[Trait("Category", "CI")]
public sealed class CircuitRevalidationTests : IDisposable
{
    private readonly Mock<IClaimAuthorizationFiller> _authorizationFiller = new();
    private readonly Mock<IClaimProfileFiller> _profileFiller = new();
    private readonly SessionKeyTicketStoreTests.InMemoryTicketStore _tickets = new();
    private readonly SessionKeyTicketStore _sessionStore;

    // The request scope of the SignalR connection: filled by the claims transformation and the cookie events when it opened.
    private readonly ServiceProvider _connectionRoot;
    private readonly IServiceScope _connectionScope;

    // The circuit's scope.
    private readonly ServiceProvider _circuitRoot;
    private readonly IServiceScope _circuitScope;

    public CircuitRevalidationTests()
    {
        _authorizationFiller.Setup(f => f.GetAuthorization(It.IsAny<IIdentity>())).Returns(new Arc4u.Security.Principal.Authorization());
        _profileFiller.Setup(f => f.GetProfile(It.IsAny<IIdentity>())).Returns(UserProfile.Empty);
        _sessionStore = new SessionKeyTicketStore(_tickets);

        _connectionRoot = ScopedUserServices().BuildServiceProvider();
        _connectionScope = _connectionRoot.CreateScope();

        var accessor = new Mock<IScopedServiceProviderAccessor>();
        accessor.SetupGet(a => a.ServiceProvider).Returns(() => _connectionScope.ServiceProvider);
        var circuitServices = ScopedUserServices();
        circuitServices.AddSingleton(accessor.Object);
        _circuitRoot = circuitServices.BuildServiceProvider();
        _circuitScope = _circuitRoot.CreateScope();
    }

    public void Dispose()
    {
        _circuitScope.Dispose();
        _circuitRoot.Dispose();
        _connectionScope.Dispose();
        _connectionRoot.Dispose();
    }

    private IApplicationContext CircuitContext => _circuitScope.ServiceProvider.GetRequiredService<IApplicationContext>();

    private IApplicationContext ConnectionContext => _connectionScope.ServiceProvider.GetRequiredService<IApplicationContext>();

    private TokenRefreshInfo ConnectionTokens => _connectionScope.ServiceProvider.GetRequiredService<TokenRefreshInfo>();

    [Fact]
    public async Task A_circuit_whose_session_was_removed_turns_anonymous_and_drops_the_user_everywhere()
    {
        // Arrange
        var user = await SignInAsync("alice");
        SignInConnection(user);
        using var sut = CreateProvider(TimeSpan.FromMilliseconds(20));
        var signedOut = WaitForAnonymousState(sut);
        sut.SetAuthenticationState(Task.FromResult(new AuthenticationState(user)));
        await sut.GetAuthenticationStateAsync();
        CircuitContext.Principal.Should().NotBeNull();

        // Act: sign-out in another tab.
        await _sessionStore.RemoveAsync(user.FindFirst(Arc4uClaimTypes.SessionKey)!.Value);
        await signedOut.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        CircuitContext.Principal.Should().BeNull();
        ConnectionContext.Principal.Should().BeNull();
        ConnectionTokens.AccessToken.Should().BeNull();
        ConnectionTokens.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task A_circuit_whose_session_still_exists_keeps_its_user()
    {
        // Arrange
        var user = await SignInAsync("alice");
        using var sut = CreateProvider(TimeSpan.FromMilliseconds(20));
        var signedOut = WaitForAnonymousState(sut);
        sut.SetAuthenticationState(Task.FromResult(new AuthenticationState(user)));

        // Act
        var completed = await Task.WhenAny(signedOut, Task.Delay(300));

        // Assert
        completed.Should().NotBeSameAs(signedOut);
        (await sut.GetAuthenticationStateAsync()).User.Should().BeOfType<AppPrincipal>();
    }

    [Fact]
    public async Task The_registration_revalidates_even_when_called_before_the_cookie_configuration()
    {
        // Arrange: AddAppPrincipalCircuitRevalidation first, then the cookie configuration registered the way AddOidcAuthentication does it
        // (TryAddSingleton of a post-configure step): it must still apply, and the session store it sets must still be wrapped.
        var services = AppServices();
        services.AddAppPrincipalCircuitRevalidation(o => o.Interval = TimeSpan.FromMilliseconds(20));
        services.TryAddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>>(
            new PostConfigureOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, o => o.SessionStore = _tickets));
        using var root = services.BuildServiceProvider();
        using var scope = root.CreateScope();

        var cookieOptions = root.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        cookieOptions.SessionStore.Should().BeOfType<SessionKeyTicketStore>().Which.Inner.Should().BeSameAs(_tickets);

        var key = await cookieOptions.SessionStore!.StoreAsync(new AuthenticationTicket(User("alice"), CookieAuthenticationDefaults.AuthenticationScheme));
        var user = (await cookieOptions.SessionStore.RetrieveAsync(key))!.Principal;
        var sut = (AppPrincipalServerAuthenticationStateProvider)scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
        var signedOut = WaitForAnonymousState(sut);
        sut.SetAuthenticationState(Task.FromResult(new AuthenticationState(user)));

        // Act
        await cookieOptions.SessionStore.RemoveAsync(key);
        await signedOut.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        scope.ServiceProvider.GetRequiredService<IApplicationContext>().Principal.Should().BeNull();
    }

    [Fact]
    public async Task Without_the_registration_the_session_store_is_not_wrapped_and_the_circuit_keeps_its_user()
    {
        // Arrange: an existing registration of the provider alone, as an MVC or Razor Pages application would never call the method either.
        var services = AppServices();
        services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, o => o.SessionStore = _tickets);
        services.Configure<CircuitRevalidationOptions>(o => o.Interval = TimeSpan.FromMilliseconds(20));
        services.AddScoped<AuthenticationStateProvider, AppPrincipalServerAuthenticationStateProvider>();
        using var root = services.BuildServiceProvider();
        using var scope = root.CreateScope();

        var cookieOptions = root.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);
        cookieOptions.SessionStore.Should().BeSameAs(_tickets);

        var key = await _tickets.StoreAsync(new AuthenticationTicket(User("alice"), CookieAuthenticationDefaults.AuthenticationScheme));
        var user = (await _tickets.RetrieveAsync(key))!.Principal;
        user.HasClaim(c => c.Type == Arc4uClaimTypes.SessionKey).Should().BeFalse();
        var sut = (AppPrincipalServerAuthenticationStateProvider)scope.ServiceProvider.GetRequiredService<AuthenticationStateProvider>();
        var signedOut = WaitForAnonymousState(sut);
        sut.SetAuthenticationState(Task.FromResult(new AuthenticationState(user)));
        await _tickets.RemoveAsync(key);

        // Act
        var completed = await Task.WhenAny(signedOut, Task.Delay(300));

        // Assert
        completed.Should().NotBeSameAs(signedOut);
    }

    [Fact]
    public void A_cookie_without_session_store_is_left_as_it_is()
    {
        // Arrange
        var services = AppServices();
        services.AddAppPrincipalCircuitRevalidation();
        using var root = services.BuildServiceProvider();

        // Act
        var cookieOptions = root.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>().Get(CookieAuthenticationDefaults.AuthenticationScheme);

        // Assert
        cookieOptions.SessionStore.Should().BeNull();
    }

    private ServiceCollection AppServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_authorizationFiller.Object);
        services.AddSingleton(_profileFiller.Object);
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();
        return services;
    }

    [Fact]
    public async Task A_disabled_revalidation_keeps_the_user_of_an_ended_sign_in()
    {
        // Arrange
        var user = await SignInAsync("alice");
        using var sut = CreateProvider(TimeSpan.FromMilliseconds(20), enabled: false);
        var signedOut = WaitForAnonymousState(sut);
        sut.SetAuthenticationState(Task.FromResult(new AuthenticationState(user)));
        await _sessionStore.RemoveAsync(user.FindFirst(Arc4uClaimTypes.SessionKey)!.Value);

        // Act
        var completed = await Task.WhenAny(signedOut, Task.Delay(300));

        // Assert
        completed.Should().NotBeSameAs(signedOut);
    }

    [Fact]
    public void An_enabled_revalidation_with_an_interval_out_of_range_is_refused_by_the_provider()
    {
        // Act
        var act = () => CreateProvider(TimeSpan.Zero);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task A_check_that_throws_turns_the_circuit_anonymous()
    {
        // Arrange
        var validator = new Mock<ISignInValidator>();
        validator.Setup(v => v.IsSignInAliveAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("store down"));
        SignInConnection(User("alice"));
        using var sut = CreateProvider(TimeSpan.FromMilliseconds(20), validator.Object);
        var signedOut = WaitForAnonymousState(sut);

        // Act
        sut.SetAuthenticationState(Task.FromResult(new AuthenticationState(User("alice"))));
        await signedOut.WaitAsync(TimeSpan.FromSeconds(10));

        // Assert
        CircuitContext.Principal.Should().BeNull();
        ConnectionContext.Principal.Should().BeNull();
    }

    [Fact]
    public async Task A_reconnection_without_a_user_drops_the_user_of_the_connection_too()
    {
        // Arrange: without revalidation, the provider still clears the connection's scope.
        SignInConnection(User("alice"));
        using var sut = CreateProvider(TimeSpan.FromMinutes(2), enabled: false);
        sut.SetAuthenticationState(Task.FromResult(new AuthenticationState(User("alice"))));
        await sut.GetAuthenticationStateAsync();

        // Act
        sut.SetAuthenticationState(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));

        // Assert
        CircuitContext.Principal.Should().BeNull();
        ConnectionContext.Principal.Should().BeNull();
        ConnectionTokens.RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task The_circuit_handler_drops_the_user_of_the_connection_when_the_state_turns_anonymous()
    {
        // Arrange
        SignInConnection(User("alice"));
        var provider = new Microsoft.AspNetCore.Components.Server.ServerAuthenticationStateProvider();
        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(User("alice"))));
        using var sut = new ApplicationContextCircuitHandler(provider, _authorizationFiller.Object, _profileFiller.Object, CircuitContext, _circuitScope.ServiceProvider);
        await sut.OnConnectionUpAsync(null!, CancellationToken.None);

        // Act
        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));

        // Assert
        ConnectionContext.Principal.Should().BeNull();
        ConnectionTokens.AccessToken.Should().BeNull();
    }

    [Fact]
    public async Task The_validator_accepts_a_live_session()
    {
        // Arrange
        var user = await SignInAsync("alice");

        // Act
        var alive = await CreateValidator().IsSignInAliveAsync(user, CancellationToken.None);

        // Assert
        alive.Should().BeTrue();
    }

    [Fact]
    public async Task The_validator_refuses_an_expired_ticket()
    {
        // Arrange
        var user = await SignInAsync("alice", expiresUtc: DateTimeOffset.UtcNow.AddMinutes(-1));

        // Act
        var alive = await CreateValidator().IsSignInAliveAsync(user, CancellationToken.None);

        // Assert
        alive.Should().BeFalse();
    }

    [Fact]
    public async Task The_validator_refuses_a_session_key_pointing_at_another_users_ticket()
    {
        // Arrange
        var victim = await SignInAsync("victim");
        var attacker = User("attacker", (Arc4uClaimTypes.SessionKey, victim.FindFirst(Arc4uClaimTypes.SessionKey)!.Value));

        // Act
        var alive = await CreateValidator().IsSignInAliveAsync(attacker, CancellationToken.None);

        // Assert
        alive.Should().BeFalse();
    }

    [Fact]
    public async Task The_validator_refuses_an_anonymous_user()
    {
        // Act
        var alive = await CreateValidator().IsSignInAliveAsync(new ClaimsPrincipal(new ClaimsIdentity()), CancellationToken.None);

        // Assert
        alive.Should().BeFalse();
    }

    [Fact]
    public async Task The_validator_keeps_a_sign_in_it_cannot_check()
    {
        // Arrange: no session store, so the whole ticket is in the cookie and the server keeps nothing to check against.
        var validator = CreateValidator(new CookieAuthenticationOptions());

        // Act
        var alive = await validator.IsSignInAliveAsync(User("alice"), CancellationToken.None);

        // Assert
        alive.Should().BeTrue();
    }

    [Fact]
    public void The_registration_replaces_the_provider_and_adds_the_validator_and_circuit_handler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<AuthenticationStateProvider, Microsoft.AspNetCore.Components.Server.ServerAuthenticationStateProvider>();

        // Act
        services.AddAppPrincipalCircuitRevalidation(o => o.Interval = TimeSpan.FromMinutes(5));

        // Assert
        services.Where(d => d.ServiceType == typeof(AuthenticationStateProvider)).Select(d => d.ImplementationType)
            .Should().Equal(typeof(AppPrincipalServerAuthenticationStateProvider));
        services.Should().Contain(d => d.ServiceType == typeof(ISignInValidator) && d.ImplementationType == typeof(SessionStoreSignInValidator));
        services.Should().Contain(d => d.ServiceType == typeof(CircuitHandler) && d.ImplementationType == typeof(ApplicationContextCircuitHandler));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(2 * 24 * 60)]
    public void An_interval_out_of_range_is_refused(int minutes)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddAppPrincipalCircuitRevalidation(o => o.Interval = TimeSpan.FromMinutes(minutes));
        using var provider = services.BuildServiceProvider();

        // Act
        var act = () => provider.GetRequiredService<IOptions<CircuitRevalidationOptions>>().Value;

        // Assert
        act.Should().Throw<OptionsValidationException>();
    }

    private static ServiceCollection ScopedUserServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();
        services.AddScoped<TokenRefreshInfo>();
        return services;
    }

    // Signs the user in: the ticket goes to the session store and the principal comes back from it, carrying its session key.
    private async Task<ClaimsPrincipal> SignInAsync(string name, DateTimeOffset? expiresUtc = null)
    {
        var properties = new AuthenticationProperties { ExpiresUtc = expiresUtc ?? DateTimeOffset.UtcNow.AddHours(1) };
        var key = await _sessionStore.StoreAsync(new AuthenticationTicket(User(name), properties, CookieAuthenticationDefaults.AuthenticationScheme));
        return (await _sessionStore.RetrieveAsync(key))!.Principal;
    }

    private void SignInConnection(ClaimsPrincipal user)
    {
        ConnectionContext.SetPrincipal(new AppPrincipal(new Arc4u.Security.Principal.Authorization(), user.Identity!, "S-1-0-0"));
        ConnectionTokens.AccessToken = new TokenInfo("access_token", "access", DateTime.UtcNow.AddHours(1));
        ConnectionTokens.RefreshToken = new TokenInfo("refresh_token", "refresh", DateTime.UtcNow.AddDays(1));
    }

    private SessionStoreSignInValidator CreateValidator(CookieAuthenticationOptions? cookieOptions = null)
    {
        var monitor = Mock.Of<IOptionsMonitor<CookieAuthenticationOptions>>(m =>
            m.Get(CookieAuthenticationDefaults.AuthenticationScheme) == (cookieOptions ?? new CookieAuthenticationOptions { SessionStore = _sessionStore }));
        return new SessionStoreSignInValidator(monitor, Options.Create(new CircuitRevalidationOptions()), NullLogger<SessionStoreSignInValidator>.Instance);
    }

    private AppPrincipalServerAuthenticationStateProvider CreateProvider(TimeSpan interval, ISignInValidator? validator = null, bool enabled = true)
        => new(
            NullLoggerFactory.Instance,
            _profileFiller.Object,
            _authorizationFiller.Object,
            CircuitContext,
            validator ?? CreateValidator(),
            Options.Create(new CircuitRevalidationOptions { Interval = interval, Enabled = enabled }),
            _circuitScope.ServiceProvider);

    private static Task WaitForAnonymousState(AuthenticationStateProvider provider)
    {
        var anonymous = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        provider.AuthenticationStateChanged += async task =>
        {
            if ((await task).User.Identity?.IsAuthenticated != true)
            {
                anonymous.TrySetResult();
            }
        };
        return anonymous.Task;
    }

    private static ClaimsPrincipal User(string name, params (string Type, string Value)[] claims)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name), new Claim("sub", name), .. claims.Select(c => new Claim(c.Type, c.Value))], "Cookies"));
}
