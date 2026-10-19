using Arc4u.Configuration;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProviders;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

/// <summary>
/// The access and refresh tokens of <see cref="TokenRefreshInfo"/> are null when the user is not logged in yet (issue #161).
/// </summary>
[Trait("Category", "CI")]
public class TokenRefreshInfoNullabilityTests
{
    private static OidcTokenProvider CreateOidcProvider(TokenRefreshInfo tokens, Mock<ITokenRefreshProvider> refreshProvider)
        => new(NullLogger<OidcTokenProvider>.Instance, tokens, Options.Create(new OidcAuthenticationOptions()), refreshProvider.Object);

    [Fact]
    public async Task OidcTokenProvider_Without_Tokens_Should_Fail_Without_Refreshing()
    {
        var refreshProvider = new Mock<ITokenRefreshProvider>();
        var sut = CreateOidcProvider(new TokenRefreshInfo(), refreshProvider);

        var result = await sut.GetTokenAsync(new SimpleKeyValueSettings(), null);

        result.IsFailed.Should().BeTrue();
        refreshProvider.Verify(p => p.RefreshTokenAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OidcTokenProvider_Without_AccessToken_Should_Refresh_With_RefreshToken()
    {
        var refreshed = new TokenRefreshInfo
        {
            AccessToken = new TokenInfo("access_token", "new-access", DateTime.UtcNow.AddHours(1)),
            RefreshToken = new TokenInfo("refresh_token", "new-refresh", DateTime.UtcNow.AddHours(8)),
        };
        var refreshProvider = new Mock<ITokenRefreshProvider>();
        refreshProvider.Setup(p => p.RefreshTokenAsync(It.IsAny<CancellationToken>())).ReturnsAsync(refreshed);
        var sut = CreateOidcProvider(new TokenRefreshInfo { RefreshToken = new TokenInfo("refresh_token", "refresh", DateTime.UtcNow.AddHours(8)) }, refreshProvider);

        var result = await sut.GetTokenAsync(new SimpleKeyValueSettings(), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("new-access");
    }

    [Fact]
    public async Task RefreshTokenProvider_Without_RefreshToken_Should_Throw_InvalidOperationException()
    {
        var sut = new RefreshTokenProvider(new TokenRefreshInfo(),
                                           Mock.Of<IOptionsMonitor<OpenIdConnectOptions>>(),
                                           Options.Create(new OidcAuthenticationOptions()),
                                           Mock.Of<IActivitySourceFactory>(),
                                           Mock.Of<IOptionsMonitor<ApiExtraContextAuthenticationOption>>(),
                                           NullLogger<RefreshTokenProvider>.Instance);

        var act = () => sut.RefreshTokenAsync(CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
