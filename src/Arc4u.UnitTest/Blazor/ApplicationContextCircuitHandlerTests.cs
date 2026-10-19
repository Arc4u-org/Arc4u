using System.Security.Claims;
using System.Security.Principal;
using Arc4u.Blazor;
using Arc4u.Security.Principal;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Blazor;

/// <summary>
/// The principal of an Interactive Server circuit (issue #171): it is set in the circuit-scoped <see cref="IApplicationContext"/> when the
/// circuit connects and follows the changes of the circuit's authentication state, such as a reconnection with another user or without a user.
/// </summary>
[Trait("Category", "CI")]
public class ApplicationContextCircuitHandlerTests
{
    private readonly Mock<IClaimAuthorizationFiller> _authorizationFiller = new();
    private readonly Mock<IClaimProfileFiller> _profileFiller = new();
    private readonly ApplicationInstanceContext _applicationContext = new();

    public ApplicationContextCircuitHandlerTests()
    {
        _authorizationFiller.Setup(f => f.GetAuthorization(It.IsAny<IIdentity>())).Returns(new Arc4u.Security.Principal.Authorization());
        _profileFiller.Setup(f => f.GetProfile(It.IsAny<IIdentity>())).Returns(UserProfile.Empty);
    }

    [Fact]
    public async Task OnConnectionUpAsync_Should_Set_The_Principal_Of_The_Authenticated_User()
    {
        var provider = ProviderWith(User("alice"));
        using var sut = CreateHandler(provider);

        await sut.OnConnectionUpAsync(null!, CancellationToken.None);

        _applicationContext.Principal.Should().NotBeNull();
        _applicationContext.Principal!.Identity!.Name.Should().Be("alice");
    }

    [Fact]
    public async Task OnConnectionUpAsync_With_An_Anonymous_User_Should_Clear_The_Principal()
    {
        var provider = ProviderWith(Anonymous());
        using var sut = CreateHandler(provider);
        _applicationContext.SetPrincipal(new AppPrincipal(new Arc4u.Security.Principal.Authorization(), User("alice").Identity!, "S-1-0-0"));

        await sut.OnConnectionUpAsync(null!, CancellationToken.None);

        _applicationContext.Principal.Should().BeNull();
    }

    [Fact]
    public async Task When_The_Circuit_Reconnects_Without_A_User_Should_Clear_The_Principal()
    {
        // On a reconnection, Blazor runs the OnConnectionUpAsync handlers with the state of the previous connection,
        // then sets the user of the new connection (ComponentHub.ConnectCircuit calls SetCircuitUser after ConnectAsync).
        var provider = ProviderWith(User("alice"));
        using var sut = CreateHandler(provider);
        await sut.OnConnectionUpAsync(null!, CancellationToken.None);

        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(Anonymous())));

        _applicationContext.Principal.Should().BeNull();
    }

    [Fact]
    public async Task When_The_Circuit_Reconnects_With_Another_User_Should_Set_The_Principal_Of_That_User()
    {
        var provider = ProviderWith(User("alice"));
        using var sut = CreateHandler(provider);
        await sut.OnConnectionUpAsync(null!, CancellationToken.None);

        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(User("bob"))));

        _applicationContext.Principal!.Identity!.Name.Should().Be("bob");
    }

    [Fact]
    public async Task When_The_New_State_Cannot_Be_Read_Should_Clear_The_Principal()
    {
        var provider = ProviderWith(User("alice"));
        using var sut = CreateHandler(provider);
        await sut.OnConnectionUpAsync(null!, CancellationToken.None);

        provider.SetAuthenticationState(Task.FromException<AuthenticationState>(new InvalidOperationException("no state")));

        _applicationContext.Principal.Should().BeNull();
    }

    [Fact]
    public async Task After_Dispose_Should_No_Longer_Follow_The_State()
    {
        var provider = ProviderWith(User("alice"));
        var sut = CreateHandler(provider);
        await sut.OnConnectionUpAsync(null!, CancellationToken.None);

        sut.Dispose();
        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(Anonymous())));

        _applicationContext.Principal!.Identity!.Name.Should().Be("alice");
    }

    [Fact]
    public async Task Provider_GetAuthenticationStateAsync_Should_Set_The_Principal_Of_The_Authenticated_User()
    {
        var sut = new AppPrincipalServerAuthenticationStateProvider(_profileFiller.Object, _authorizationFiller.Object, _applicationContext);
        sut.SetAuthenticationState(Task.FromResult(new AuthenticationState(User("alice"))));

        var state = await sut.GetAuthenticationStateAsync();

        state.User.Should().BeOfType<AppPrincipal>();
        _applicationContext.Principal.Should().BeSameAs(state.User);
    }

    [Fact]
    public async Task Provider_GetAuthenticationStateAsync_With_An_Anonymous_User_Should_Clear_The_Principal()
    {
        var sut = new AppPrincipalServerAuthenticationStateProvider(_profileFiller.Object, _authorizationFiller.Object, _applicationContext);
        _applicationContext.SetPrincipal(new AppPrincipal(new Arc4u.Security.Principal.Authorization(), User("alice").Identity!, "S-1-0-0"));
        sut.SetAuthenticationState(Task.FromResult(new AuthenticationState(Anonymous())));

        var state = await sut.GetAuthenticationStateAsync();

        state.User.Identity!.IsAuthenticated.Should().BeFalse();
        _applicationContext.Principal.Should().BeNull();
    }

    [Fact]
    public async Task Provider_When_The_State_Changes_Should_Follow_It_Without_Being_Read_Again()
    {
        var sut = new AppPrincipalServerAuthenticationStateProvider(_profileFiller.Object, _authorizationFiller.Object, _applicationContext);
        sut.SetAuthenticationState(Task.FromResult(new AuthenticationState(User("alice"))));
        await sut.GetAuthenticationStateAsync();

        sut.SetAuthenticationState(Task.FromResult(new AuthenticationState(User("bob"))));
        _applicationContext.Principal!.Identity!.Name.Should().Be("bob");

        sut.SetAuthenticationState(Task.FromResult(new AuthenticationState(Anonymous())));
        _applicationContext.Principal.Should().BeNull();
    }

    private ApplicationContextCircuitHandler CreateHandler(AuthenticationStateProvider provider)
        => new(provider, _authorizationFiller.Object, _profileFiller.Object, _applicationContext);

    private static ServerAuthenticationStateProvider ProviderWith(ClaimsPrincipal user)
    {
        var provider = new ServerAuthenticationStateProvider();
        provider.SetAuthenticationState(Task.FromResult(new AuthenticationState(user)));
        return provider;
    }

    private static ClaimsPrincipal User(string name)
        => new(new ClaimsIdentity([new Claim(ClaimTypes.Name, name)], "Cookies"));

    private static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());
}
