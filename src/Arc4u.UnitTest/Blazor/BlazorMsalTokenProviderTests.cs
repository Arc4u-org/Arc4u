using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arc4u.OAuth2.TokenProvider;
using Arc4u.Security.Principal;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Blazor;

[Trait("Category", "CI")]
public class BlazorMsalTokenProviderTests
{
    private const string LoginUrl = "authentication/login";

    private readonly Mock<IAccessTokenProvider> _accessTokenProvider = new();
    private readonly ApplicationInstanceContext _applicationContext = new();
    private readonly TestNavigationManager _navigation = new();
    private readonly Mock<IKeyValueSettings> _settings = new();

    private BlazorMsalTokenProvider CreateSut() => new(_accessTokenProvider.Object, _applicationContext, _navigation);

    private static string CreateAccessToken(DateTime validTo)
    {
        var jwt = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")], validTo.AddHours(-2), validTo);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private void SetPrincipalWithToken(string token)
    {
        var identity = new ClaimsIdentity("TestAuth") { BootstrapContext = token };
        _applicationContext.SetPrincipal(new AppPrincipal(new Arc4u.Security.Principal.Authorization(), identity, "S-1-0-0"));
    }

    private void SetupAccessTokenResult(AccessTokenResult result)
    {
        _accessTokenProvider.Setup(p => p.RequestAccessToken()).Returns(ValueTask.FromResult(result));
    }

    [Fact]
    public async Task ValidTokenOnPrincipalShouldBeReturnedWithoutRequestingMsal()
    {
        // arrange
        var accessToken = CreateAccessToken(DateTime.UtcNow.AddHours(1));
        SetPrincipalWithToken(accessToken);
        var sut = CreateSut();

        // act
        var result = await sut.GetTokenAsync(_settings.Object, null);

        // assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be(accessToken);
        _accessTokenProvider.Verify(p => p.RequestAccessToken(), Times.Never);
        _navigation.NavigatedUris.Should().BeEmpty();
    }

    [Fact]
    public async Task TokenFromMsalShouldBeReturnedWithoutRedirect()
    {
        // arrange
        var accessToken = CreateAccessToken(DateTime.UtcNow.AddHours(1));
        SetupAccessTokenResult(new AccessTokenResult(AccessTokenResultStatus.Success,
            new AccessToken { Value = accessToken, Expires = DateTimeOffset.UtcNow.AddHours(1) }, null, null));
        var sut = CreateSut();

        // act
        var result = await sut.GetTokenAsync(_settings.Object, null);

        // assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be(accessToken);
        _navigation.NavigatedUris.Should().BeEmpty();
    }

    [Fact]
    public async Task ExpiredTokenRequiringRedirectShouldClearPrincipalAndNavigateToLogin()
    {
        // arrange
        SetPrincipalWithToken(CreateAccessToken(DateTime.UtcNow.AddMinutes(-10)));
        var options = new InteractiveRequestOptions { Interaction = InteractionType.GetToken, ReturnUrl = "https://localhost/page" };
        SetupAccessTokenResult(new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect, null!, LoginUrl, options));
        var sut = CreateSut();

        // act
        var result = await sut.GetTokenAsync(_settings.Object, null);

        // assert
        result.IsFailed.Should().BeTrue();
        _applicationContext.Principal.Should().BeNull();
        _navigation.NavigatedUris.Should().ContainSingle().Which.Should().Be($"https://localhost/{LoginUrl}");
        _navigation.LastOptions!.Value.HistoryEntryState.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RedirectWithoutInteractionOptionsShouldNavigateToLogin()
    {
        // arrange
        SetPrincipalWithToken(CreateAccessToken(DateTime.UtcNow.AddMinutes(-10)));
        SetupAccessTokenResult(new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect, null!, LoginUrl, null));
        var sut = CreateSut();

        // act
        var result = await sut.GetTokenAsync(_settings.Object, null);

        // assert
        result.IsFailed.Should().BeTrue();
        _applicationContext.Principal.Should().BeNull();
        _navigation.NavigatedUris.Should().ContainSingle().Which.Should().Be($"https://localhost/{LoginUrl}");
    }

    [Fact]
    public async Task RedirectWithoutInteractiveRequestUrlShouldNotNavigate()
    {
        // arrange
        SetPrincipalWithToken(CreateAccessToken(DateTime.UtcNow.AddMinutes(-10)));
        SetupAccessTokenResult(new AccessTokenResult(AccessTokenResultStatus.RequiresRedirect, null!, null, null));
        var sut = CreateSut();

        // act
        var result = await sut.GetTokenAsync(_settings.Object, null);

        // assert
        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Message.Should().Be("No token found!");
        _applicationContext.Principal.Should().NotBeNull();
        _navigation.NavigatedUris.Should().BeEmpty();
    }

    [Fact]
    public async Task NullSettingsShouldThrow()
    {
        // arrange
        var sut = CreateSut();

        // act
        var exception = await Record.ExceptionAsync(async () => await sut.GetTokenAsync(null, null));

        // assert
        exception.Should().BeOfType<ArgumentNullException>();
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        public List<string> NavigatedUris { get; } = [];

        public NavigationOptions? LastOptions { get; private set; }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            NavigatedUris.Add(ToAbsoluteUri(uri).ToString());
            LastOptions = options;
        }
    }
}
