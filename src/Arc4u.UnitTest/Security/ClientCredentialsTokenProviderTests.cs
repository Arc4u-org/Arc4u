using System.Net;
using System.Text;
using Arc4u.Configuration;
using Arc4u.Dependency;
using Arc4u.OAuth2;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProvider;
using Arc4u.OAuth2.TokenProvider.Scenarios;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class ClientCredentialsTokenProviderTests
{
    private readonly Fixture _fixture;
    private static readonly Uri TokenEndpoint = new("https://authdev.arc4u.net/oidc/token");

    public ClientCredentialsTokenProviderTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public async Task GetToken_Uses_Basic_Auth_And_Posts_ClientCredentials_Should()
    {
        // arrange
        var clientId = _fixture.Create<string>();
        var clientSecret = _fixture.Create<string>();
        var scope = "access.application";
        var accessToken = _fixture.Create<string>();

        var settings = BuildSettings(clientId, clientSecret, scope,
            extra: [new("resource", "http://guidance/api")]);

        var handler = new StubHandler(Ok(accessToken, 3600));
        var (cache, _) = MockCache(cached: null);
        var sut = BuildSut(handler, cache);

        // act
        var result = await sut.GetTokenAsync(settings, null);

        // assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TokenType.Should().Be("Bearer");
        result.Value.Token.Should().Be(accessToken);
        result.Value.ExpiresOnUtc.Should().BeCloseTo(DateTime.UtcNow.AddSeconds(3600), TimeSpan.FromMinutes(1));

        handler.RequestUri.Should().Be(TokenEndpoint);
        // client_secret_basic: credentials in the Authorization header, not the body.
        handler.AuthorizationScheme.Should().Be("Basic");
        handler.AuthorizationParameter.Should()
            .Be(Convert.ToBase64String(Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}")));
        handler.Body.Should().Contain("grant_type=client_credentials");
        handler.Body.Should().Contain("scope=access.application");
        handler.Body.Should().Contain("resource=http%3A%2F%2Fguidance%2Fapi");
        handler.Body.Should().NotContain("client_secret");
    }

    [Fact]
    public async Task GetToken_Stores_Token_In_Cache_Should()
    {
        // arrange
        var settings = BuildSettings(_fixture.Create<string>(), _fixture.Create<string>(), Constants.OpenIdScope, []);
        var handler = new StubHandler(Ok(_fixture.Create<string>(), 3600));
        var (cache, mockCache) = MockCache(cached: null);
        var sut = BuildSut(handler, cache);

        // act
        var result = await sut.GetTokenAsync(settings, null);

        // assert
        result.IsSuccess.Should().BeTrue();
        mockCache.Verify(m => m.Put(It.IsAny<string>(), It.IsAny<TokenInfo>()), Times.Once);
    }

    [Fact]
    public async Task GetToken_Returns_Cached_Token_Without_Calling_Sts_Should()
    {
        // arrange
        var cachedToken = new TokenInfo("Bearer", _fixture.Create<string>(), DateTime.UtcNow.AddHours(1));
        var settings = BuildSettings(_fixture.Create<string>(), _fixture.Create<string>(), Constants.OpenIdScope, []);
        var handler = new StubHandler(Ok(_fixture.Create<string>(), 3600));
        var (cache, _) = MockCache(cached: cachedToken);
        var sut = BuildSut(handler, cache);

        // act
        var result = await sut.GetTokenAsync(settings, null);

        // assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(cachedToken);
        handler.Called.Should().BeFalse();
    }

    [Fact]
    public async Task GetToken_Cache_Key_Depends_On_Extra_Parameters_Should()
    {
        // arrange
        var clientId = _fixture.Create<string>();
        var clientSecret = _fixture.Create<string>();

        async Task<string> CacheKeyFor(params KeyValuePair<string, string>[] extra)
        {
            var (cache, mockCache) = MockCache(cached: null);
            var sut = BuildSut(new StubHandler(Ok(_fixture.Create<string>(), 3600)), cache);
            await sut.GetTokenAsync(BuildSettings(clientId, clientSecret, Constants.OpenIdScope, extra), null);
            return (string)mockCache.Invocations.Single(i => i.Method.Name == nameof(ITokenCache.Put)).Arguments[0];
        }

        // act
        var keyApi1 = await CacheKeyFor(KeyValuePair.Create("resource", "http://api1"));
        var keyApi1Again = await CacheKeyFor(KeyValuePair.Create("resource", "http://api1"));
        var keyApi2 = await CacheKeyFor(KeyValuePair.Create("resource", "http://api2"));
        var keyOrderA = await CacheKeyFor(KeyValuePair.Create("resource", "http://api1"), KeyValuePair.Create("audience", "x"));
        var keyOrderB = await CacheKeyFor(KeyValuePair.Create("audience", "x"), KeyValuePair.Create("resource", "http://api1"));

        // assert
        keyApi1.Should().Be(keyApi1Again);
        keyApi1.Should().NotBe(keyApi2);
        keyOrderA.Should().Be(keyOrderB);
        keyApi1.Should().NotContain(clientSecret);
    }

    [Fact]
    public async Task GetToken_With_Missing_ClientSecret_Should_Fail()
    {
        // arrange
        var settings = new SimpleKeyValueSettings(new Dictionary<string, string>
        {
            { TokenKeys.ClientIdKey, _fixture.Create<string>() },
            { TokenKeys.Scope, Constants.OpenIdScope }
        });
        var handler = new StubHandler(Ok(_fixture.Create<string>(), 3600));
        var (cache, _) = MockCache(cached: null);
        var sut = BuildSut(handler, cache);

        // act
        var result = await sut.GetTokenAsync(settings, null);

        // assert
        result.IsFailed.Should().BeTrue();
        handler.Called.Should().BeFalse();
    }

    [Fact]
    public async Task GetToken_With_No_Settings_Should_Throw()
    {
        var handler = new StubHandler(Ok(_fixture.Create<string>(), 3600));
        var (cache, _) = MockCache(cached: null);
        var sut = BuildSut(handler, cache);

        var exception = await Record.ExceptionAsync(async () => await sut.GetTokenAsync(null, null).ConfigureAwait(false));

        exception.Should().BeOfType<ArgumentNullException>();
    }

    private SimpleKeyValueSettings BuildSettings(string clientId, string clientSecret, string scope, IEnumerable<KeyValuePair<string, string>> extra)
    {
        var values = new Dictionary<string, string>
        {
            { TokenKeys.ClientIdKey, clientId },
            { TokenKeys.ClientSecret, clientSecret },
            { TokenKeys.Scope, scope }
        };
        var extraList = extra.ToList();
        if (extraList.Count > 0)
        {
            values.Add(TokenKeys.ExtraParameters, ExtraParametersEncoder.Encode(extraList));
        }
        return new SimpleKeyValueSettings(values);
    }

    private static (ITokenCache Cache, Mock<ITokenCache> Mock) MockCache(TokenInfo? cached)
    {
        var mock = new Mock<ITokenCache>();
        mock.Setup(m => m.Get<TokenInfo>(It.IsAny<string>())).Returns(cached!);
        return (mock.Object, mock);
    }

    private ClientCredentialsTokenProvider BuildSut(StubHandler handler, ITokenCache cache)
    {
        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(m => m.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler));

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddILogger();
        services.AddDefaultAuthority(options => options.SetData(new Uri("https://authdev.arc4u.net"), TokenEndpoint, null, null));
        var container = services.BuildServiceProvider();

        return new ClientCredentialsTokenProvider(
            mockFactory.Object,
            cache,
            container.GetRequiredService<IOptionsMonitor<AuthorityOptions>>(),
            container.GetRequiredService<ILogger<ClientCredentialsTokenProvider>>());
    }

    private static HttpResponseMessage Ok(string accessToken, int expiresIn)
    {
        var json = $"{{\"access_token\":\"{accessToken}\",\"token_type\":\"Bearer\",\"expires_in\":{expiresIn}}}";
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public bool Called { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? AuthorizationScheme { get; private set; }
        public string? AuthorizationParameter { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Called = true;
            RequestUri = request.RequestUri;
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            AuthorizationParameter = request.Headers.Authorization?.Parameter;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return response;
        }
    }
}
