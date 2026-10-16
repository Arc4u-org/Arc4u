using System.Security.Claims;
using Arc4u.Dependency;
using Arc4u.OAuth2.Events;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

/// <summary>
/// The cookie events store the access token in the <see cref="ClaimsIdentity.BootstrapContext"/> of the identity, like the bearer events do (issue #90).
/// </summary>
[Trait("Category", "CI")]
public class StandardCookieEventsTests
{
    [Fact]
    public async Task ValidatePrincipal_With_A_Valid_Token_Should_Set_The_BootstrapContext()
    {
        var accessToken = CreateJwt(DateTime.UtcNow.AddHours(1));
        var refreshProvider = new Mock<ITokenRefreshProvider>();
        var (sut, context, _) = Create(accessToken, refreshProvider);

        await sut.ValidatePrincipal(context);

        context.Principal.Should().NotBeNull();
        ((ClaimsIdentity)context.Principal!.Identity!).BootstrapContext.Should().Be(accessToken);
        context.ShouldRenew.Should().BeFalse();
        refreshProvider.Verify(p => p.RefreshTokenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidatePrincipal_With_An_Expiring_Token_Should_Set_The_Refreshed_Token_In_The_BootstrapContext()
    {
        var accessToken = CreateJwt(DateTime.UtcNow.AddMinutes(1));
        var refreshedToken = CreateJwt(DateTime.UtcNow.AddHours(1));
        var refreshProvider = new Mock<ITokenRefreshProvider>();
        var (sut, context, tokens) = Create(accessToken, refreshProvider);
        refreshProvider.Setup(p => p.RefreshTokenAsync(It.IsAny<CancellationToken>()))
                       .Callback(() =>
                       {
                           tokens.AccessToken = new TokenInfo("access_token", refreshedToken, DateTime.UtcNow.AddHours(1));
                           tokens.RefreshToken = new TokenInfo("refresh_token", "new-refresh", DateTime.UtcNow.AddHours(8));
                       })
                       .ReturnsAsync(() => tokens);

        await sut.ValidatePrincipal(context);

        context.Principal.Should().NotBeNull();
        ((ClaimsIdentity)context.Principal!.Identity!).BootstrapContext.Should().Be(refreshedToken);
        context.ShouldRenew.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePrincipal_When_The_Refresh_Fails_Should_Reject_The_Principal_Without_Setting_The_BootstrapContext()
    {
        var accessToken = CreateJwt(DateTime.UtcNow.AddMinutes(1));
        var refreshProvider = new Mock<ITokenRefreshProvider>();
        refreshProvider.Setup(p => p.RefreshTokenAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException("refresh failed"));
        var (sut, context, _) = Create(accessToken, refreshProvider);
        var identity = (ClaimsIdentity)context.Principal!.Identity!;

        await sut.ValidatePrincipal(context);

        context.Principal.Should().BeNull();
        identity.BootstrapContext.Should().BeNull();
    }

    private static (StandardCookieEvents Events, CookieValidatePrincipalContext Context, TokenRefreshInfo Tokens) Create(string accessToken, Mock<ITokenRefreshProvider> refreshProvider)
    {
        var tokens = new TokenRefreshInfo();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddILogger();
        services.AddSingleton(tokens);
        services.AddSingleton(Mock.Of<IAuthenticationService>());
        var serviceProvider = services.BuildServiceProvider();

        var properties = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) };
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = accessToken },
            new AuthenticationToken { Name = "refresh_token", Value = "refresh" },
        ]);

        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim("name", "John")], CookieAuthenticationDefaults.AuthenticationScheme));
        var scheme = new AuthenticationScheme(CookieAuthenticationDefaults.AuthenticationScheme, null, typeof(CookieAuthenticationHandler));
        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var context = new CookieValidatePrincipalContext(httpContext, scheme, new CookieAuthenticationOptions(),
                                                         new AuthenticationTicket(principal, properties, scheme.Name));

        var sut = new StandardCookieEvents(serviceProvider,
                                           serviceProvider.GetRequiredService<ILogger<StandardCookieEvents>>(),
                                           Options.Create(new OidcAuthenticationOptions { ForceRefreshTimeoutTimeSpan = TimeSpan.FromMinutes(5) }),
                                           refreshProvider.Object);

        return (sut, context, tokens);
    }

    private static string CreateJwt(DateTime expires)
        => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            NotBefore = DateTime.UtcNow.AddHours(-1),
            IssuedAt = DateTime.UtcNow.AddHours(-1),
            Expires = expires,
        });
}
