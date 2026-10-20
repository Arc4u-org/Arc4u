using System.Security.Claims;
using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Security;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProviders;
using Arc4u.Security.Principal;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using static Arc4u.UnitTest.Security.SignOutTestSupport;

namespace Arc4u.UnitTest.Security;

/// <summary>
/// The sign-out of <see cref="AzureADOboTokenProvider"/> and the index of the cached on-behalf-of tokens (issue #113),
/// and the user token it exchanges when the OpenID bearer injector replaced the <see cref="ClaimsIdentity.BootstrapContext"/> (issue #275).
/// </summary>
[Trait("Category", "CI")]
public class OboTokenProviderSignOutTests
{
    private readonly Fixture _fixture;

    public OboTokenProviderSignOutTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public async Task Obo_SignOut_Should_Remove_The_Tokens_Of_Every_Api_Of_The_User()
    {
        var userToken = _fixture.Create<string>();
        var authority = new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null);
        var indexKey = $"_Obo_Index_{TokenCacheKey.Hash(authority.Url.ToString(), userToken)}";
        var keys = new Dictionary<string, DateTime> { { "_Obo_api1_x", DateTime.UtcNow.AddHours(1) }, { "_Obo_api2_y", DateTime.UtcNow.AddHours(1) } };

        var cache = new Mock<ICache>();
        cache.Setup(c => c.GetAsync<OboTokenCacheIndex>(indexKey, It.IsAny<CancellationToken>())).ReturnsAsync(new OboTokenCacheIndex(keys));
        var sut = CreateObo(PrincipalWithToken(userToken), new TokenRefreshInfo(), cache, authority);

        // The settings of the principal do not identify an on-behalf-of API: every API of the user is signed out.
        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync("_Obo_api1_x", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("_Obo_api2_y", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync(indexKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Obo_Tokens_Obtained_Before_A_Refresh_Of_The_User_Token_Should_Be_Removed()
    {
        // Two access tokens of the same user (same oid), before and after a refresh.
        var before = CreateJwt(("oid", "user-1"), ("jti", "1"));
        var after = CreateJwt(("oid", "user-1"), ("jti", "2"));
        var (cache, entries) = CreateIndexCache();
        var authority = new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null);

        var sut = CreateObo(PrincipalWithToken(after), new TokenRefreshInfo(), cache, authority);
        await sut.AddToIndexAsync(cache.Object, before, "_Obo_api1", DateTime.UtcNow.AddHours(1));
        await sut.AddToIndexAsync(cache.Object, after, "_Obo_api2", DateTime.UtcNow.AddHours(1));

        entries.Should().ContainSingle().Which.Value.Keys.Keys.Should().BeEquivalentTo("_Obo_api1", "_Obo_api2");

        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync("_Obo_api1", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("_Obo_api2", It.IsAny<CancellationToken>()), Times.Once);
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Obo_Index_Should_Use_The_Registered_User_Object_Identifier()
    {
        // The tokens have no oid and their sub changes: the user is identified by the claim configured for the application (IUserObjectIdentifier), read from the token.
        var before = CreateJwt(("sub", "pairwise-1"), ("upn", "user@example.com"), ("jti", "1"));
        var after = CreateJwt(("sub", "pairwise-2"), ("upn", "user@example.com"), ("jti", "2"));
        var identity = new ClaimsIdentity([new Claim("other", "value")], "Bearer") { BootstrapContext = before };
        var userObjectIdentifier = new Mock<IUserObjectIdentifier>();
        userObjectIdentifier.Setup(u => u.Getidentifier(It.IsAny<ClaimsIdentity>())).Returns((ClaimsIdentity i) => i.FindFirst(ClaimTypes.Upn)?.Value);
        var (cache, entries) = CreateIndexCache();

        // The sign-out has no principal: the user token comes from the TokenRefreshInfo.
        var sut = CreateObo(null, new TokenRefreshInfo { AccessToken = new TokenInfo("Bearer", after, DateTime.UtcNow.AddHours(1)) }, cache, new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null), userObjectIdentifier.Object);

        await sut.AddToIndexAsync(cache.Object, before, "_Obo_api1", DateTime.UtcNow.AddHours(1), identity);
        await sut.AddToIndexAsync(cache.Object, after, "_Obo_api2", DateTime.UtcNow.AddHours(1), identity);
        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync("_Obo_api1", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("_Obo_api2", It.IsAny<CancellationToken>()), Times.Once);
        entries.Should().BeEmpty();
    }

    [Fact]
    public void Obo_Should_Be_Resolved_Without_A_User_Object_Identifier()
    {
        var container = BuildContainer(services =>
        {
            services.AddScoped<TokenRefreshInfo>();
            services.AddSingleton(new Mock<ICacheHelper>().Object);
            services.AddSingleton(new Mock<IActivitySourceFactory>().Object);
            services.AddSingleton(new Mock<IApplicationContext>().Object);
            services.AddSingleton(Mock.Of<IOptionsMonitor<AuthorityOptions>>(m => m.Get("Default") == new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null)));
            services.AddKeyedTransient<ITokenProvider, AzureADOboTokenProvider>(AzureADOboTokenProvider.ProviderName);
        });

        using var scope = container.CreateScope();

        scope.ServiceProvider.GetRequiredKeyedService<ITokenProvider>(AzureADOboTokenProvider.ProviderName).Should().BeOfType<AzureADOboTokenProvider>();
    }

    [Fact]
    public async Task Obo_Index_Should_Not_Be_Shared_By_Two_Applications()
    {
        // The same user signed in to two applications (aud) that share the cache.
        var appA = CreateJwt(("oid", "user-1"), ("aud", "app-a"));
        var appB = CreateJwt(("oid", "user-1"), ("aud", "app-b"));
        var (cache, entries) = CreateIndexCache();
        var sut = CreateObo(PrincipalWithToken(appA), new TokenRefreshInfo(), cache, new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null));
        await sut.AddToIndexAsync(cache.Object, appA, "_Obo_a", DateTime.UtcNow.AddHours(1));
        await sut.AddToIndexAsync(cache.Object, appB, "_Obo_b", DateTime.UtcNow.AddHours(1));

        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync("_Obo_a", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("_Obo_b", It.IsAny<CancellationToken>()), Times.Never);
        entries.Should().ContainSingle().Which.Value.Keys.Keys.Should().BeEquivalentTo("_Obo_b");
    }

    [Fact]
    public async Task Obo_Index_Should_Be_Shared_By_The_Clients_Of_The_Same_Application()
    {
        // An API (aud) called by two clients (azp) for the same user: one index, so one sign-out removes the tokens obtained through both.
        var spa = CreateJwt(("oid", "user-1"), ("aud", "api"), ("azp", "spa"));
        var mobile = CreateJwt(("oid", "user-1"), ("aud", "api"), ("azp", "mobile"));
        var (cache, entries) = CreateIndexCache();
        var sut = CreateObo(PrincipalWithToken(spa), new TokenRefreshInfo(), cache, new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null));
        await sut.AddToIndexAsync(cache.Object, spa, "_Obo_spa", DateTime.UtcNow.AddHours(1));
        await sut.AddToIndexAsync(cache.Object, mobile, "_Obo_mobile", DateTime.UtcNow.AddHours(1));

        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync("_Obo_spa", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("_Obo_mobile", It.IsAny<CancellationToken>()), Times.Once);
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Obo_Index_Without_Audience_Should_Use_The_Appid_Of_The_Token()
    {
        // v1 tokens without aud: the application is the appid claim.
        var appA = CreateJwt(("oid", "user-1"), ("appid", "app-a"));
        var appB = CreateJwt(("oid", "user-1"), ("appid", "app-b"));
        var (cache, entries) = CreateIndexCache();
        var sut = CreateObo(PrincipalWithToken(appA), new TokenRefreshInfo(), cache, new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null));
        await sut.AddToIndexAsync(cache.Object, appA, "_Obo_a", DateTime.UtcNow.AddHours(1));
        await sut.AddToIndexAsync(cache.Object, appB, "_Obo_b", DateTime.UtcNow.AddHours(1));

        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync("_Obo_b", It.IsAny<CancellationToken>()), Times.Never);
        entries.Should().ContainSingle().Which.Value.Keys.Keys.Should().BeEquivalentTo("_Obo_b");
    }

    [Fact]
    public async Task Obo_Index_With_Opaque_Tokens_Should_Use_The_Identity_Of_The_Principal()
    {
        // Opaque (not JWT) user tokens have no claims: the registered identifier reads the principal.
        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Upn, "user@example.com")], "Bearer") { BootstrapContext = "opaque-2" };
        var applicationContext = new Mock<IApplicationContext>();
        applicationContext.SetupGet(c => c.Principal).Returns(new AppPrincipal(new Arc4u.Security.Principal.Authorization(), identity, "S-1-0-0"));
        var userObjectIdentifier = new Mock<IUserObjectIdentifier>();
        userObjectIdentifier.Setup(u => u.Getidentifier(It.IsAny<ClaimsIdentity>())).Returns((ClaimsIdentity i) => i.FindFirst(ClaimTypes.Upn)?.Value);
        var (cache, entries) = CreateIndexCache();
        var sut = CreateObo(applicationContext.Object, new TokenRefreshInfo(), cache, new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null), userObjectIdentifier.Object);

        await sut.AddToIndexAsync(cache.Object, "opaque-1", "_Obo_api1", DateTime.UtcNow.AddHours(1), identity);
        await sut.AddToIndexAsync(cache.Object, "opaque-2", "_Obo_api2", DateTime.UtcNow.AddHours(1), identity);
        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync("_Obo_api1", It.IsAny<CancellationToken>()), Times.Once);
        cache.Verify(c => c.RemoveAsync("_Obo_api2", It.IsAny<CancellationToken>()), Times.Once);
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Obo_Index_Should_Fall_Back_To_The_Oid_When_The_Identifier_Fails()
    {
        var before = CreateJwt(("oid", "user-1"), ("jti", "1"));
        var after = CreateJwt(("oid", "user-1"), ("jti", "2"));
        var userObjectIdentifier = new Mock<IUserObjectIdentifier>();
        userObjectIdentifier.Setup(u => u.Getidentifier(It.IsAny<ClaimsIdentity>())).Throws(new InvalidOperationException("misconfigured"));
        var (cache, entries) = CreateIndexCache();
        var sut = CreateObo(PrincipalWithToken(after), new TokenRefreshInfo(), cache, new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null), userObjectIdentifier.Object);

        await sut.AddToIndexAsync(cache.Object, before, "_Obo_api1", DateTime.UtcNow.AddHours(1));
        await sut.AddToIndexAsync(cache.Object, after, "_Obo_api2", DateTime.UtcNow.AddHours(1));
        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync("_Obo_api1", It.IsAny<CancellationToken>()), Times.Once);
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Obo_SignOut_Should_Keep_In_The_Index_The_Keys_That_Cannot_Be_Removed()
    {
        var userToken = CreateJwt(("oid", "user-1"));
        var (cache, entries) = CreateIndexCache();
        cache.Setup(c => c.RemoveAsync("_Obo_api1", It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("cache down"));
        var sut = CreateObo(PrincipalWithToken(userToken), new TokenRefreshInfo(), cache, new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null));
        await sut.AddToIndexAsync(cache.Object, userToken, "_Obo_api1", DateTime.UtcNow.AddHours(1));
        await sut.AddToIndexAsync(cache.Object, userToken, "_Obo_api2", DateTime.UtcNow.AddHours(1));

        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync("_Obo_api2", It.IsAny<CancellationToken>()), Times.Once);
        entries.Should().ContainSingle().Which.Value.Keys.Keys.Should().BeEquivalentTo("_Obo_api1");
    }

    [Fact]
    public async Task Obo_SignOut_Without_User_Token_Should_Do_Nothing()
    {
        var cache = new Mock<ICache>();
        var sut = CreateObo(null, new TokenRefreshInfo(), cache, new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null));

        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // With UseOpenIdBearerInjector and on-behalf-of settings, the BootstrapContext of a cookie session holds the on-behalf-of token of the injector (audience api-x):
    // the user token is the one of the cookie. The token is found in the cache only when it was obtained with the cookie token (no request is sent).
    [Fact]
    public async Task Obo_In_A_Cookie_Session_Should_Exchange_The_Cookie_Token_And_Not_The_BootstrapContext()
    {
        var userToken = CreateJwt(("oid", "user-1"), ("aud", "front-app"));
        var injectedToken = CreateJwt(("oid", "user-1"), ("aud", "api-x"));
        var apiYToken = CreateJwt(("oid", "user-1"), ("aud", "api-y"));
        var authority = UnreachableAuthority();
        var settings = OboSettings("api-y/.default");
        var cache = new Mock<ICache>();
        cache.Setup(c => c.GetAsync<TokenInfo>(OboCacheKey(authority, settings, userToken), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new TokenInfo("Bearer", apiYToken, DateTime.UtcNow.AddHours(1)));
        var sut = CreateObo(PrincipalWithToken(injectedToken), new TokenRefreshInfo { AccessToken = new TokenInfo("access_token", userToken, DateTime.UtcNow.AddHours(1)) }, cache, authority);

        var result = await sut.GetTokenAsync(settings, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be(apiYToken);
    }

    [Fact]
    public async Task Obo_Without_A_Cookie_Session_Should_Exchange_The_BootstrapContext()
    {
        var bearerToken = CreateJwt(("oid", "user-1"), ("aud", "api"));
        var downstreamToken = CreateJwt(("oid", "user-1"), ("aud", "api-y"));
        var authority = UnreachableAuthority();
        var settings = OboSettings("api-y/.default");
        var cache = new Mock<ICache>();
        cache.Setup(c => c.GetAsync<TokenInfo>(OboCacheKey(authority, settings, bearerToken), It.IsAny<CancellationToken>()))
             .ReturnsAsync(new TokenInfo("Bearer", downstreamToken, DateTime.UtcNow.AddHours(1)));
        var sut = CreateObo(PrincipalWithToken(bearerToken), new TokenRefreshInfo(), cache, authority);

        var result = await sut.GetTokenAsync(settings, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Token.Should().Be(downstreamToken);
    }

    [Fact]
    public async Task Obo_SignOut_In_A_Cookie_Session_Should_Remove_The_Token_Of_The_Injector()
    {
        var userToken = CreateJwt(("oid", "user-1"), ("aud", "front-app"));
        var injectedToken = CreateJwt(("oid", "user-1"), ("aud", "api-x"));
        var (cache, entries) = CreateIndexCache();
        var sut = CreateObo(PrincipalWithToken(injectedToken), new TokenRefreshInfo { AccessToken = new TokenInfo("access_token", userToken, DateTime.UtcNow.AddHours(1)) }, cache, UnreachableAuthority());
        // The injector obtained its token with the cookie token, so it is in the index of the front-app.
        await sut.AddToIndexAsync(cache.Object, userToken, "_Obo_api_x", DateTime.UtcNow.AddHours(1));

        await sut.SignOutAsync(new SimpleKeyValueSettings(), CancellationToken.None);

        cache.Verify(c => c.RemoveAsync("_Obo_api_x", It.IsAny<CancellationToken>()), Times.Once);
        entries.Should().BeEmpty();
    }

    // No token request can succeed: a test reaching the token endpoint fails.
    private static AuthorityOptions UnreachableAuthority()
        => new(new Uri("https://login.example.com/tenant/v2.0"), new Uri("http://127.0.0.1:1/token"), null, null);

    private static SimpleKeyValueSettings OboSettings(string scope)
        => new(new Dictionary<string, string> { [TokenKeys.ClientIdKey] = "front-app", [TokenKeys.ClientSecret] = "secret", [TokenKeys.Scope] = scope });

    // The cache key of an on-behalf-of token obtained with the given user token.
    private static string OboCacheKey(AuthorityOptions authority, IKeyValueSettings settings, string userToken)
        => $"_Obo_{settings.Values[TokenKeys.ClientIdKey]}_{TokenCacheKey.Hash(authority.Url.ToString(), userToken, settings.Values[TokenKeys.Scope])}";

    private static AzureADOboTokenProvider CreateObo(IApplicationContext? applicationContext, TokenRefreshInfo tokens, Mock<ICache> cache, AuthorityOptions authority, IUserObjectIdentifier? userObjectIdentifier = null)
    {
        var cacheHelper = new Mock<ICacheHelper>();
        cacheHelper.Setup(h => h.GetCache()).Returns(cache.Object);
        var authorities = new Mock<IOptionsMonitor<AuthorityOptions>>();
        authorities.Setup(a => a.Get("Default")).Returns(authority);

        return new AzureADOboTokenProvider(tokens, cacheHelper.Object, new Mock<IActivitySourceFactory>().Object, applicationContext ?? new Mock<IApplicationContext>().Object, authorities.Object, BuildContainer(_ => { }).GetRequiredService<ILogger<AzureADOboTokenProvider>>(), userObjectIdentifier);
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
}
