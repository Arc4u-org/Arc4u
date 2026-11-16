using System.Net;
using System.Text;
using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProviders;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static Arc4u.UnitTest.Security.SignOutTestSupport;

namespace Arc4u.UnitTest.Security;

/// <summary>
/// Named <see cref="TokenCacheOptions"/> for the on-behalf-of settings: their registration, and <see cref="AzureADOboTokenProvider"/> storing the tokens in the named cache
/// while the index of the user stays in the default token cache, so the sign-out removes each token from its own cache.
/// </summary>
[Trait("Category", "CI")]
public class OboNamedTokenCacheTests
{
    private static readonly Uri TokenEndpoint = new("https://login.example.com/tenant/oauth2/v2.0/token");
    private readonly string _userToken = CreateJwt(("oid", "user-1"), ("aud", "front-app"));
    private readonly Mock<ICache> _namedCache = new();
    private readonly Mock<ICache> _defaultCache;
    private readonly Dictionary<string, OboTokenCacheIndex> _indexes;

    public OboNamedTokenCacheTests()
    {
        (_defaultCache, _indexes) = CreateIndexCache();
    }

    [Fact]
    public void AddOnBehalfOf_With_A_TokenCache_Registers_Named_TokenCacheOptions_Should()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:TokenCache:CacheName"] = "Default",
            ["Authentication:TokenCache:MaxTime"] = "00:40:00",
            ["Authentication:OnBehalfOf:Obo1:ClientId"] = "front-app",
            ["Authentication:OnBehalfOf:Obo1:ClientSecret"] = "secret",
            ["Authentication:OnBehalfOf:Obo1:Scopes:0"] = "api-y/.default",
            ["Authentication:OnBehalfOf:Obo1:TokenCache:CacheName"] = "Redis",
            ["Authentication:OnBehalfOf:Obo2:ClientId"] = "front-app",
            ["Authentication:OnBehalfOf:Obo2:ClientSecret"] = "secret",
            ["Authentication:OnBehalfOf:Obo2:Scopes:0"] = "api-z/.default",
        }).Build();

        var services = new ServiceCollection();
        services.AddTokenCache(configuration);
        services.AddOnBehalfOf(configuration);
        var container = services.BuildServiceProvider();

        var settings = container.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();
        var tokenCacheOptions = container.GetRequiredService<IOptionsMonitor<TokenCacheOptions>>();

        settings.Get("Obo1").Values[TokenKeys.TokenCacheOptionsName].Should().Be("Obo1");
        tokenCacheOptions.Get("Obo1").CacheName.Should().Be("Redis");
        // MaxTime is not in the TokenCache section: the default one is used.
        tokenCacheOptions.Get("Obo1").MaxTime.Should().Be(TimeSpan.FromMinutes(40));

        settings.Get("Obo2").Values.ContainsKey(TokenKeys.TokenCacheOptionsName).Should().BeFalse();
    }

    [Fact]
    public void OnBehalfOfCache_Is_The_Fallback_Of_Every_Setting_Should()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:TokenCache:CacheName"] = "Default",
            ["Authentication:TokenCache:MaxTime"] = "00:40:00",
            ["Authentication:OnBehalfOfCache:CacheName"] = "Shared",
            ["Authentication:OnBehalfOf:Obo1:ClientId"] = "front-app",
            ["Authentication:OnBehalfOf:Obo1:ClientSecret"] = "secret",
            ["Authentication:OnBehalfOf:Obo1:Scopes:0"] = "api-y/.default",
            ["Authentication:OnBehalfOf:Obo2:ClientId"] = "front-app",
            ["Authentication:OnBehalfOf:Obo2:ClientSecret"] = "secret",
            ["Authentication:OnBehalfOf:Obo2:Scopes:0"] = "api-z/.default",
            ["Authentication:OnBehalfOf:Obo2:TokenCache:MaxTime"] = "00:05:00",
        }).Build();

        var services = new ServiceCollection();
        services.AddTokenCache(configuration);
        services.AddOnBehalfOf(configuration);
        var container = services.BuildServiceProvider();

        var settings = container.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();
        var tokenCacheOptions = container.GetRequiredService<IOptionsMonitor<TokenCacheOptions>>();

        // The shared section takes MaxTime from the default options.
        var shared = tokenCacheOptions.Get(OnBehalfOfAuthenticationExtensions.TokenCacheOptionsName);
        shared.CacheName.Should().Be("Shared");
        shared.MaxTime.Should().Be(TimeSpan.FromMinutes(40));

        // A setting without its own TokenCache uses the shared one.
        settings.Get("Obo1").Values[TokenKeys.TokenCacheOptionsName].Should().Be(OnBehalfOfAuthenticationExtensions.TokenCacheOptionsName);

        // A setting with its own TokenCache takes the missing values from the shared one.
        settings.Get("Obo2").Values[TokenKeys.TokenCacheOptionsName].Should().Be("Obo2");
        tokenCacheOptions.Get("Obo2").CacheName.Should().Be("Shared");
        tokenCacheOptions.Get("Obo2").MaxTime.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Fact]
    public void OnBehalfOfCache_With_A_Setting_Named_Like_It_Should_Throw()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:OnBehalfOfCache:CacheName"] = "Shared",
            ["Authentication:OnBehalfOf:onbehalfofcache:ClientId"] = "front-app",
            ["Authentication:OnBehalfOf:onbehalfofcache:ClientSecret"] = "secret",
            ["Authentication:OnBehalfOf:onbehalfofcache:Scopes:0"] = "api-y/.default",
        }).Build();

        var exception = Record.Exception(() => new ServiceCollection().AddOnBehalfOf(configuration));

        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void OnBehalfOfCache_Does_Not_Apply_To_Settings_From_Code_Should()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:OnBehalfOfCache:CacheName"] = "Shared",
        }).Build();

        var services = new ServiceCollection();
        services.AddOnBehalfOf(configuration);
        services.AddOnBehalfOfSettings(o =>
        {
            o.ClientId = "front-app";
            o.ClientSecret = "secret";
            o.Scopes.Add("api-y/.default");
        }, "FromCode");
        var container = services.BuildServiceProvider();

        container.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>().Get("FromCode")
                 .Values.ContainsKey(TokenKeys.TokenCacheOptionsName).Should().BeFalse();
    }

    [Fact]
    public void AddOnBehalfOfSettings_With_A_TokenCache_From_Code_Registers_Named_TokenCacheOptions_Should()
    {
        var services = new ServiceCollection();
        services.AddTokenCache(o => { o.CacheName = "Default"; o.MaxTime = TimeSpan.FromMinutes(40); });
        services.AddOnBehalfOfSettings(o =>
        {
            o.ClientId = "front-app";
            o.ClientSecret = "secret";
            o.Scopes.Add("api-y/.default");
            o.TokenCache = new TokenCacheOptions { MaxTime = TimeSpan.FromMinutes(10) };
        }, "Downstream");
        var container = services.BuildServiceProvider();

        var named = container.GetRequiredService<IOptionsMonitor<TokenCacheOptions>>().Get("Downstream");

        // An empty CacheName takes the one of the default options; MaxTime comes from the instance.
        named.CacheName.Should().Be("Default");
        named.MaxTime.Should().Be(TimeSpan.FromMinutes(10));
        container.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>().Get("Downstream")
                 .Values[TokenKeys.TokenCacheOptionsName].Should().Be("Downstream");
    }

    [Fact]
    public void AddOnBehalfOf_With_A_Zero_TokenCache_MaxTime_Should_Throw()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:OnBehalfOf:Obo1:ClientId"] = "front-app",
            ["Authentication:OnBehalfOf:Obo1:ClientSecret"] = "secret",
            ["Authentication:OnBehalfOf:Obo1:Scopes:0"] = "api-y/.default",
            ["Authentication:OnBehalfOf:Obo1:TokenCache:MaxTime"] = "00:00:00",
        }).Build();

        var exception = Record.Exception(() => new ServiceCollection().AddOnBehalfOf(configuration));

        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public async Task A_Token_Of_Named_Options_Is_Stored_In_Their_Cache_And_Indexed_In_The_Default_Cache_Should()
    {
        var handler = new StubHandler("""{"access_token":"new-token","expires_in":3600}""");
        var sut = CreateSut(handler);

        var result = await sut.GetTokenAsync(Settings("Obo1"), null);

        result.IsSuccess.Should().BeTrue();
        // The token is kept at most the MaxTime of the named options (5 minutes), not its lifetime of 1 hour.
        _namedCache.Verify(c => c.PutAsync(It.Is<string>(k => k.StartsWith("_Obo_front-app_")), TimeSpan.FromMinutes(5),
                                           It.Is<TokenInfo>(t => t.Token == "new-token"), false, It.IsAny<CancellationToken>()), Times.Once);
        _defaultCache.Verify(c => c.PutAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TokenInfo>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);

        var index = _indexes.Values.Should().ContainSingle().Subject;
        var key = index.Keys.Keys.Should().ContainSingle().Subject;
        index.GetCacheName(key).Should().Be("Redis");
        index.Keys[key].Should().BeCloseTo(DateTime.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task A_MaxTime_Under_The_Expiration_Margin_Still_Caches_The_Token_Should()
    {
        // The token is valid for an hour: a MaxTime of 30 seconds only shortens its time in the cache.
        var handler = new StubHandler("""{"access_token":"new-token","expires_in":3600}""");

        await CreateSut(handler, TimeSpan.FromSeconds(30)).GetTokenAsync(Settings("Obo1"), null);

        _namedCache.Verify(c => c.PutAsync(It.IsAny<string>(), TimeSpan.FromSeconds(30), It.Is<TokenInfo>(t => t.Token == "new-token"), false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_Token_Of_Named_Options_Is_Read_From_Their_Cache_Should()
    {
        _namedCache.Setup(c => c.GetAsync<TokenInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                   .ReturnsAsync(new TokenInfo("Bearer", "cached-token", DateTime.UtcNow.AddHours(1)));
        var handler = new StubHandler("{}");

        var result = await CreateSut(handler).GetTokenAsync(Settings("Obo1"), null);

        result.Value.Token.Should().Be("cached-token");
        handler.Called.Should().BeFalse();
    }

    [Fact]
    public async Task A_Token_Without_Named_Options_Stays_In_The_Default_Cache_With_The_Default_MaxTime_Should()
    {
        var handler = new StubHandler("""{"access_token":"new-token","expires_in":3600}""");

        await CreateSut(handler).GetTokenAsync(Settings(optionsName: null), null);

        _defaultCache.Verify(c => c.PutAsync(It.IsAny<string>(), TimeSpan.FromMinutes(50), It.Is<TokenInfo>(t => t.Token == "new-token"), false, It.IsAny<CancellationToken>()), Times.Once);
        _namedCache.VerifyNoOtherCalls();
        // The index of a token in the default cache keeps the format of the previous versions.
        _indexes.Values.Single().CacheNames.Should().BeNull();
    }

    [Fact]
    public async Task An_Unregistered_OptionsName_Uses_The_Default_Cache_Should()
    {
        var handler = new StubHandler("""{"access_token":"new-token","expires_in":3600}""");

        await CreateSut(handler).GetTokenAsync(Settings("Typo"), null);

        _defaultCache.Verify(c => c.PutAsync(It.IsAny<string>(), TimeSpan.FromMinutes(50), It.IsAny<TokenInfo>(), false, It.IsAny<CancellationToken>()), Times.Once);
        _namedCache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SignOut_Removes_Each_Token_From_Its_Cache_Should()
    {
        var sut = CreateSut(new StubHandler("{}"));
        var expires = DateTime.UtcNow.AddHours(1);
        await sut.AddToIndexAsync(_defaultCache.Object, _userToken, "_Obo_named", expires, cacheName: "Redis");
        await sut.AddToIndexAsync(_defaultCache.Object, _userToken, "_Obo_default", expires);
        _indexes.Values.Single().Keys.Should().HaveCount(2);

        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        _namedCache.Verify(c => c.RemoveAsync("_Obo_named", It.IsAny<CancellationToken>()), Times.Once);
        _defaultCache.Verify(c => c.RemoveAsync("_Obo_default", It.IsAny<CancellationToken>()), Times.Once);
        _defaultCache.Verify(c => c.RemoveAsync("_Obo_named", It.IsAny<CancellationToken>()), Times.Never);
        _indexes.Should().BeEmpty();
    }

    [Fact]
    public async Task SignOut_Keeps_The_CacheName_Of_A_Token_That_Cannot_Be_Removed_Should()
    {
        _namedCache.Setup(c => c.RemoveAsync("_Obo_named", It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("cache down"));
        var sut = CreateSut(new StubHandler("{}"));
        var expires = DateTime.UtcNow.AddHours(1);
        await sut.AddToIndexAsync(_defaultCache.Object, _userToken, "_Obo_named", expires, cacheName: "Redis");
        await sut.AddToIndexAsync(_defaultCache.Object, _userToken, "_Obo_default", expires);

        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        // The next sign-out removes it from the right cache.
        var index = _indexes.Values.Should().ContainSingle().Subject;
        index.Keys.Keys.Should().Equal("_Obo_named");
        index.GetCacheName("_Obo_named").Should().Be("Redis");
    }

    [Fact]
    public async Task SignOut_Removes_A_Token_From_Its_Cache_After_Its_Options_Are_Removed_Should()
    {
        // The token was cached in Redis by options that no longer exist after a redeploy: the sign-out uses the recorded cache name.
        var sut = CreateSut(new StubHandler("{}"));
        await sut.AddToIndexAsync(_defaultCache.Object, _userToken, "_Obo_named", DateTime.UtcNow.AddHours(1), cacheName: "Redis");

        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        _namedCache.Verify(c => c.RemoveAsync("_Obo_named", It.IsAny<CancellationToken>()), Times.Once);
        _defaultCache.Verify(c => c.RemoveAsync("_Obo_named", It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Settings_With_The_Same_Client_And_Scope_But_Another_Cache_Have_Their_Own_Key_Should()
    {
        var handler = new StubHandler("""{"access_token":"new-token","expires_in":3600}""");
        var sut = CreateSut(handler);

        await sut.GetTokenAsync(Settings("Obo1"), null);
        await sut.GetTokenAsync(Settings(optionsName: null), null);

        // Both tokens are in the index, each with its cache: the sign-out removes both.
        var index = _indexes.Values.Should().ContainSingle().Subject;
        index.Keys.Should().HaveCount(2);
        index.CacheNames.Should().ContainSingle().Which.Value.Should().Be("Redis");
    }

    [Fact]
    public async Task An_Index_Of_A_Previous_Version_Is_Read_With_The_Default_Cache_Should()
    {
        var sut = CreateSut(new StubHandler("{}"));
        await sut.AddToIndexAsync(_defaultCache.Object, _userToken, "_Obo_old", DateTime.UtcNow.AddHours(1));
        var indexKey = _indexes.Keys.Single();
        // An index stored by a previous version has no CacheNames.
        _indexes[indexKey] = new OboTokenCacheIndex(_indexes[indexKey].Keys);

        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        _defaultCache.Verify(c => c.RemoveAsync("_Obo_old", It.IsAny<CancellationToken>()), Times.Once);
        _namedCache.VerifyNoOtherCalls();
    }

    private static SimpleKeyValueSettings Settings(string? optionsName)
    {
        var values = new Dictionary<string, string>
        {
            [TokenKeys.ClientIdKey] = "front-app",
            [TokenKeys.ClientSecret] = "secret",
            [TokenKeys.Scope] = "api-y/.default"
        };
        if (optionsName is not null)
        {
            values[TokenKeys.TokenCacheOptionsName] = optionsName;
        }
        return new SimpleKeyValueSettings(values);
    }

    private AzureADOboTokenProvider CreateSut(StubHandler handler, TimeSpan? namedMaxTime = null)
    {
        var services = new ServiceCollection();
        services.AddTokenCache(o => { o.CacheName = "Default"; o.MaxTime = TimeSpan.FromMinutes(50); });
        services.AddNamedTokenCache("Obo1", "Redis", namedMaxTime ?? TimeSpan.FromMinutes(5));
        var cacheHelper = new Mock<ICacheHelper>();
        cacheHelper.Setup(h => h.GetCache()).Returns(_defaultCache.Object);
        cacheHelper.Setup(h => h.GetCache("Redis")).Returns(_namedCache.Object);
        var options = services.BuildServiceProvider();
        var resolver = new TokenCacheResolver(cacheHelper.Object,
                                              options.GetRequiredService<IOptionsMonitor<TokenCacheOptions>>(),
                                              options.GetServices<IConfigureOptions<TokenCacheOptions>>(),
                                              BuildContainer(_ => { }).GetRequiredService<ILogger<TokenCacheResolver>>());
        var authorities = new Mock<IOptionsMonitor<AuthorityOptions>>();
        authorities.Setup(a => a.Get("Default")).Returns(new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), TokenEndpoint, null, null));
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler));

        return new AzureADOboTokenProvider(new TokenRefreshInfo(),
                                           cacheHelper.Object,
                                           new Mock<IActivitySourceFactory>().Object,
                                           PrincipalWithToken(_userToken),
                                           authorities.Object,
                                           BuildContainer(_ => { }).GetRequiredService<ILogger<AzureADOboTokenProvider>>(),
                                           httpClientFactory: httpClientFactory.Object,
                                           tokenCacheResolver: resolver);
    }

    // A cache mock that keeps the OBO indexes in a dictionary.
    private static (Mock<ICache> Cache, Dictionary<string, OboTokenCacheIndex> Entries) CreateIndexCache()
    {
        var entries = new Dictionary<string, OboTokenCacheIndex>();
        var cache = new Mock<ICache>();
        cache.Setup(c => c.GetAsync<OboTokenCacheIndex>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string key, CancellationToken _) => entries.TryGetValue(key, out var index) ? index : null);
        cache.Setup(c => c.PutAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<OboTokenCacheIndex>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
             .Callback((string key, TimeSpan _, OboTokenCacheIndex index, bool _, CancellationToken _) => entries[key] = index)
             .Returns(Task.CompletedTask);
        cache.Setup(c => c.RemoveAsync(It.Is<string>(key => key.StartsWith("_Obo_Index_")), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string key, CancellationToken _) => entries.Remove(key));

        return (cache, entries);
    }

    private sealed class StubHandler(string body) : HttpMessageHandler
    {
        public bool Called { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Called = true;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }
}
