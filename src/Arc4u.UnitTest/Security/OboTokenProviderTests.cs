using System.Net;
using System.Text;
using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProviders;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static Arc4u.UnitTest.Security.SignOutTestSupport;

namespace Arc4u.UnitTest.Security;

/// <summary>The token acquisition of <see cref="AzureADOboTokenProvider"/>: the cache, the response of the token endpoint and its errors.</summary>
[Trait("Category", "CI")]
public class OboTokenProviderTests
{
    private static readonly Uri TokenEndpoint = new("https://login.example.com/tenant/oauth2/v2.0/token");
    private readonly string _userToken = CreateJwt(("oid", "user-1"), ("aud", "front-app"));
    private readonly Mock<ICache> _cache = new();

    [Fact]
    public async Task A_Cached_Opaque_Token_Is_Returned_Without_A_Request_Should()
    {
        _cache.Setup(c => c.GetAsync<TokenInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new TokenInfo("access_token", "opaque-token", DateTime.UtcNow.AddHours(1)));
        var handler = new StubHandler(Json("{}"));

        var result = await CreateSut(handler).GetTokenAsync(Settings(), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("opaque-token");
        result.Value.TokenType.Should().Be("Bearer");
        handler.Called.Should().BeFalse();
    }

    [Fact]
    public async Task A_Cached_Token_Expiring_Within_A_Minute_Is_Requested_Again_Should()
    {
        _cache.Setup(c => c.GetAsync<TokenInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(new TokenInfo("Bearer", "old-token", DateTime.UtcNow.AddSeconds(30)));
        var handler = new StubHandler(Json("""{"access_token":"new-token","expires_in":3600}"""));

        var result = await CreateSut(handler).GetTokenAsync(Settings(), null);

        result.Value.Token.Should().Be("new-token");
        handler.Called.Should().BeTrue();
        handler.Body.Should().Contain("grant_type=urn%3Aietf%3Aparams%3Aoauth%3Agrant-type%3Ajwt-bearer");
    }

    [Fact]
    public async Task A_New_Token_Is_Cached_For_Its_Lifetime_Should()
    {
        var handler = new StubHandler(Json("""{"access_token":"new-token","expires_in":3600}"""));

        var result = await CreateSut(handler).GetTokenAsync(Settings(), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresOnUtc.Should().BeCloseTo(DateTime.UtcNow.AddHours(1), TimeSpan.FromSeconds(10));
        _cache.Verify(c => c.PutAsync(It.Is<string>(k => k.StartsWith("_Obo_front-app_")),
                                      It.Is<TimeSpan>(t => t > TimeSpan.FromMinutes(59) && t <= TimeSpan.FromHours(1)),
                                      It.Is<TokenInfo>(t => t.Token == "new-token"), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task An_Expires_In_Given_As_A_String_Is_Read_Should()
    {
        var handler = new StubHandler(Json("""{"access_token":"new-token","expires_in":"3599"}"""));

        var result = await CreateSut(handler).GetTokenAsync(Settings(), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresOnUtc.Should().BeCloseTo(DateTime.UtcNow.AddSeconds(3599), TimeSpan.FromSeconds(10));
    }

    [Theory]
    [InlineData("9223372036854775807")]
    [InlineData("\"9223372036854775807\"")]
    public async Task An_Unreasonable_Expires_In_Is_Limited_Instead_Of_Throwing_Should(string expiresIn)
    {
        var handler = new StubHandler(Json($$"""{"access_token":"new-token","expires_in":{{expiresIn}}}"""));

        var result = await CreateSut(handler).GetTokenAsync(Settings(), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresOnUtc.Should().BeCloseTo(DateTime.UtcNow.AddDays(365), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task A_Negative_Expires_In_Is_Returned_But_Not_Cached_Should()
    {
        var handler = new StubHandler(Json("""{"access_token":"new-token","expires_in":-10}"""));

        var result = await CreateSut(handler).GetTokenAsync(Settings(), null);

        result.IsSuccess.Should().BeTrue();
        VerifyNothingCached();
    }

    [Fact]
    public async Task Without_Expires_In_The_Expiration_Of_A_Jwt_Is_Used_Should()
    {
        var expires = DateTime.UtcNow.AddMinutes(30);
        var jwt = CreateJwt(expires, ("aud", "api-y"));
        var handler = new StubHandler(Json($$"""{"access_token":"{{jwt}}"}"""));

        var result = await CreateSut(handler).GetTokenAsync(Settings(), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.ExpiresOnUtc.Should().BeCloseTo(expires, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task An_Opaque_Token_Without_Expires_In_Is_Returned_But_Not_Cached_Should()
    {
        var handler = new StubHandler(Json("""{"access_token":"opaque-token"}"""));

        var result = await CreateSut(handler).GetTokenAsync(Settings(), null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be("opaque-token");
        VerifyNothingCached();
    }

    [Fact]
    public async Task A_Token_With_An_Expires_In_Of_Zero_Is_Returned_But_Not_Cached_Should()
    {
        var handler = new StubHandler(Json("""{"access_token":"new-token","expires_in":0}"""));

        var result = await CreateSut(handler).GetTokenAsync(Settings(), null);

        result.IsSuccess.Should().BeTrue();
        VerifyNothingCached();
    }

    [Fact]
    public async Task A_Token_Valid_For_At_Most_The_Expiration_Margin_Is_Returned_But_Not_Cached_Should()
    {
        // A cached token is used only when it is valid for more than a minute: this one would never be read.
        var handler = new StubHandler(Json("""{"access_token":"new-token","expires_in":45}"""));

        var result = await CreateSut(handler).GetTokenAsync(Settings(), null);

        result.IsSuccess.Should().BeTrue();
        VerifyNothingCached();
    }

    [Theory]
    [InlineData("""{"expires_in":3600}""")]
    [InlineData("""{"access_token":"","expires_in":3600}""")]
    [InlineData("""{"access_token":null,"expires_in":3600}""")]
    [InlineData("not json")]
    public async Task A_Response_Without_An_Access_Token_Fails_And_Is_Not_Cached_Should(string body)
    {
        var handler = new StubHandler(Json(body));

        var result = await CreateSut(handler).GetTokenAsync(Settings(), null);

        result.IsFailed.Should().BeTrue();
        VerifyNothingCached();
    }

    [Fact]
    public async Task An_Error_Status_Code_Fails_Without_Throwing_Should()
    {
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("""{"error":"invalid_grant"}""", Encoding.UTF8, "application/json")
        });

        var result = await CreateSut(handler).GetTokenAsync(Settings(), null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Message.Should().Contain("400");
        VerifyNothingCached();
    }

    [Theory]
    [InlineData(TokenKeys.ClientIdKey)]
    [InlineData(TokenKeys.ClientSecret)]
    [InlineData(TokenKeys.Scope)]
    public async Task A_Missing_Setting_Fails_Without_A_Request_Should(string missingKey)
    {
        var values = new Dictionary<string, string>(Settings().Values);
        values.Remove(missingKey);
        var handler = new StubHandler(Json("{}"));

        var result = await CreateSut(handler).GetTokenAsync(new SimpleKeyValueSettings(values), null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle().Which.Message.Should().Contain(missingKey);
        handler.Called.Should().BeFalse();
    }

    [Fact]
    public async Task The_HttpClient_Named_By_The_Settings_Is_Used_Should()
    {
        var handler = new StubHandler(Json("""{"access_token":"new-token","expires_in":3600}"""));
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("OboClient")).Returns(() => new HttpClient(handler));
        var values = new Dictionary<string, string>(Settings().Values) { [TokenKeys.HttpClientName] = "OboClient" };

        var result = await CreateSut(factory.Object).GetTokenAsync(new SimpleKeyValueSettings(values), null);

        result.IsSuccess.Should().BeTrue();
        factory.Verify(f => f.CreateClient("OboClient"), Times.Once);
        handler.RequestUri.Should().Be(TokenEndpoint);
    }

    private void VerifyNothingCached()
        => _cache.Verify(c => c.PutAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<It.IsAnyType>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);

    private static SimpleKeyValueSettings Settings()
        => new(new Dictionary<string, string>
        {
            [TokenKeys.ClientIdKey] = "front-app",
            [TokenKeys.ClientSecret] = "secret",
            [TokenKeys.Scope] = "api-y/.default"
        });

    private static HttpResponseMessage Json(string body)
        => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    // The default HttpClient of the factory sends the requests to the handler.
    private AzureADOboTokenProvider CreateSut(StubHandler handler)
    {
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler));
        return CreateSut(factory.Object);
    }

    private AzureADOboTokenProvider CreateSut(IHttpClientFactory httpClientFactory)
    {
        var cacheHelper = new Mock<ICacheHelper>();
        cacheHelper.Setup(h => h.GetCache()).Returns(_cache.Object);
        var authorities = new Mock<IOptionsMonitor<AuthorityOptions>>();
        authorities.Setup(a => a.Get("Default")).Returns(new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), TokenEndpoint, null, null));

        return new AzureADOboTokenProvider(new TokenRefreshInfo(),
                                           cacheHelper.Object,
                                           new Mock<IActivitySourceFactory>().Object,
                                           PrincipalWithToken(_userToken),
                                           authorities.Object,
                                           BuildContainer(_ => { }).GetRequiredService<ILogger<AzureADOboTokenProvider>>(),
                                           httpClientFactory: httpClientFactory);
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public bool Called { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Called = true;
            RequestUri = request.RequestUri;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }
    }
}
