using System.Net;
using System.Text;
using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.OAuth2.Client.Authentication.Options;
using Arc4u.OAuth2.Client.Authentication.TokenProvider;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using AwesomeAssertions;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

/// <summary>
/// The client token provider reads the refresh threshold and the refresh token lifetime from the options, and sends the extra parameters on the right channel (issue #233).
/// </summary>
[Trait("Category", "CI")]
public class OidcClientIdentityModelTokenProviderTests
{
    private const string TokenEndpoint = "https://login.example.com/token";
    private const string AuthorizeEndpoint = "https://login.example.com/authorize";

    private sealed class TokenEndpointHandler : HttpMessageHandler
    {
        public List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestBodies.Add(await request.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"access_token":"new-access","refresh_token":"new-refresh","token_type":"Bearer","expires_in":3600}""", Encoding.UTF8, "application/json")
            };
        }
    }

    private static (OidcClientIdentityModelTokenProvider Provider, TokenRefreshInfo Tokens, TokenEndpointHandler Handler, Mock<IBrowser> Browser) Create(TokenInfo? accessToken, TokenInfo? refreshToken, TimeSpan forceRefresh, TimeSpan refreshTokenLifetime)
    {
        var handler = new TokenEndpointHandler();
        var browser = new Mock<IBrowser>();
        browser.Setup(b => b.InvokeAsync(It.IsAny<BrowserOptions>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new BrowserResult { ResultType = BrowserResultType.UserCancel });

        var oidcOptions = new OidcClientOptions
        {
            Authority = "https://login.example.com",
            ClientId = "client",
            Scope = "openid offline_access",
            RedirectUri = "myapp://callback",
            Browser = browser.Object,
            BackchannelHandler = handler,
            ProviderInformation = new ProviderInformation
            {
                IssuerName = "https://login.example.com",
                AuthorizeEndpoint = AuthorizeEndpoint,
                TokenEndpoint = TokenEndpoint,
                KeySet = new Duende.IdentityModel.Jwk.JsonWebKeySet()
            }
        };

        var tokens = new TokenRefreshInfo { AccessToken = accessToken, RefreshToken = refreshToken };

        var extraContext = new ApiExtraContextAuthenticationOption
        {
            AuthorizationParameters = new SimpleKeyValueSettings(("front", "authorize-value")),
            TokenParameters = new SimpleKeyValueSettings(("back", "token-value"))
        };
        var extraMonitor = Mock.Of<IOptionsMonitor<ApiExtraContextAuthenticationOption>>(m => m.CurrentValue == extraContext);

        var authenticationOptions = new OidcClientAuthenticationOptions { ForceRefreshTimeoutTimeSpan = forceRefresh, RefreshTokenLifetime = refreshTokenLifetime };
        var authenticationMonitor = Mock.Of<IOptionsMonitor<OidcClientAuthenticationOptions>>(m => m.CurrentValue == authenticationOptions);

        var provider = new OidcClientIdentityModelTokenProvider(oidcOptions, tokens, Mock.Of<ISecureCache>(), extraMonitor, authenticationMonitor);

        return (provider, tokens, handler, browser);
    }

    [Fact]
    public async Task Access_Token_Outside_The_Refresh_Threshold_Should_Be_Returned()
    {
        var accessToken = new TokenInfo("Bearer", "access", DateTime.UtcNow.AddMinutes(10));
        var (sut, _, handler, _) = Create(accessToken, new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddDays(1)), TimeSpan.FromMinutes(5), TimeSpan.FromDays(90));

        var result = await sut.GetTokenAsync(null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("access");
        handler.RequestBodies.Should().BeEmpty();
    }

    [Fact]
    public async Task Access_Token_Inside_The_Refresh_Threshold_Should_Be_Refreshed_With_The_Token_Parameters_Only()
    {
        var accessToken = new TokenInfo("Bearer", "access", DateTime.UtcNow.AddMinutes(2));
        var (sut, tokens, handler, _) = Create(accessToken, new TokenInfo("Bearer", "refresh", DateTime.UtcNow.AddDays(1)), TimeSpan.FromMinutes(5), TimeSpan.FromDays(14));

        var result = await sut.GetTokenAsync(null, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("new-access");
        handler.RequestBodies.Should().ContainSingle();
        handler.RequestBodies[0].Should().Contain("grant_type=refresh_token").And.Contain("back=token-value").And.NotContain("front=");
        tokens.RefreshToken!.Token.Should().Be("new-refresh");
        tokens.RefreshToken.ExpiresOnUtc.Should().BeCloseTo(DateTime.UtcNow.AddDays(14), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Login_Should_Send_The_Authorization_Parameters_On_The_Front_Channel()
    {
        var (sut, _, handler, browser) = Create(null, null, TimeSpan.FromMinutes(5), TimeSpan.FromDays(90));

        var result = await sut.GetTokenAsync(null, null);

        result.IsFailed.Should().BeTrue();
        browser.Verify(b => b.InvokeAsync(It.Is<BrowserOptions>(o => o.StartUrl.StartsWith(AuthorizeEndpoint, StringComparison.Ordinal)
                                                                     && o.StartUrl.Contains("front=authorize-value")
                                                                     && !o.StartUrl.Contains("back=")),
                                          It.IsAny<CancellationToken>()), Times.Once);
        handler.RequestBodies.Should().BeEmpty();
    }
}
