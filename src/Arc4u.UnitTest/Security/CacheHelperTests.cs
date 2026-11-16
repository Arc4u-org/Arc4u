using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.Dependency;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Environment = Arc4u.Configuration.Environment;

namespace Arc4u.UnitTest.Security;

/// <summary>Tests of <see cref="CacheHelper.GetCache(string?)"/>: the cache named by a specific <see cref="TokenCacheOptions.CacheName"/>.</summary>
[Trait("Category", "CI")]
public class CacheHelperTests
{
    private readonly Mock<ICacheContext> _cacheContext = new();
    private readonly Mock<ICache> _defaultCache = new();
    private readonly Mock<ICache> _tokenCache = new();
    private readonly Mock<ICache> _redisCache = new();

    public CacheHelperTests()
    {
        _cacheContext.Setup(c => c.Default).Returns(_defaultCache.Object);
        _cacheContext.Setup(c => c.Exist("Token")).Returns(true);
        _cacheContext.Setup(c => c["Token"]).Returns(_tokenCache.Object);
        _cacheContext.Setup(c => c.Exist("Redis")).Returns(true);
        _cacheContext.Setup(c => c["Redis"]).Returns(_redisCache.Object);
    }

    private CacheHelper BuildSut(string? defaultCacheName)
    {
        var fixture = new Fixture().Customize(new AutoMoqCustomization());

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddILogger();
        services.AddApplicationConfig(config =>
        {
            config.ApplicationName = fixture.Create<string>();
            config.Environment = fixture.Create<Environment>();
        });
        var container = services.BuildServiceProvider();

        return new CacheHelper(_cacheContext.Object,
                               container.GetRequiredService<ILogger<CacheHelper>>(),
                               Options.Create(new TokenCacheOptions { CacheName = defaultCacheName! }));
    }

    [Fact]
    public void GetCache_With_A_Specific_CacheName_Returns_That_Cache_Should()
    {
        var sut = BuildSut("Token");

        sut.GetCache("Redis").Should().BeSameAs(_redisCache.Object);
        // The default token cache is not changed by the lookup.
        sut.GetCache().Should().BeSameAs(_tokenCache.Object);
    }

    [Fact]
    public void GetCache_With_A_Specific_CacheName_When_The_Default_Options_Have_None_Should()
    {
        var sut = BuildSut(null);

        sut.GetCache().Should().BeSameAs(_defaultCache.Object);
        sut.GetCache("Redis").Should().BeSameAs(_redisCache.Object);
    }

    [Fact]
    public void GetCache_With_An_Unknown_CacheName_Returns_The_Default_Cache_Should()
    {
        _cacheContext.Setup(c => c.Exist("Unknown")).Returns(false);
        var sut = BuildSut("Token");

        // The documented fallback is the default cache of the cache context, not the token cache.
        sut.GetCache("Unknown").Should().BeSameAs(_defaultCache.Object);
        _cacheContext.Verify(c => c["Unknown"], Times.Never);
    }

    [Fact]
    public void GetCache_With_A_Cache_That_Cannot_Be_Initialized_Returns_The_Default_Cache_Should()
    {
        _cacheContext.Setup(c => c.Exist("Broken")).Returns(true);
        _cacheContext.Setup(c => c["Broken"]).Throws(new InvalidOperationException("Redis is unreachable"));
        var sut = BuildSut("Token");

        sut.GetCache("Broken").Should().BeSameAs(_defaultCache.Object);
    }

    [Fact]
    public void GetCache_Resolves_A_CacheName_Once_Should()
    {
        var sut = BuildSut("Token");
        _cacheContext.Invocations.Clear();

        sut.GetCache("Redis");
        sut.GetCache("Redis");

        _cacheContext.Verify(c => c["Redis"], Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetCache_Without_A_CacheName_Returns_The_Token_Cache_Should(string? cacheName)
    {
        var sut = BuildSut("Token");

        sut.GetCache(cacheName).Should().BeSameAs(_tokenCache.Object);
    }

    [Fact]
    public void GetCache_With_The_CacheName_Of_The_Default_Options_Reuses_The_Token_Cache_Should()
    {
        var sut = BuildSut("Token");
        _cacheContext.Invocations.Clear();

        sut.GetCache("Token").Should().BeSameAs(_tokenCache.Object);
        // Resolved once in the constructor: no new lookup in the cache context.
        _cacheContext.Verify(c => c.Exist(It.IsAny<string>()), Times.Never);
        _cacheContext.Verify(c => c[It.IsAny<string>()], Times.Never);
    }

    [Fact]
    public void GetCache_Resolves_Each_Specific_CacheName_Independently_Should()
    {
        var otherCache = new Mock<ICache>();
        _cacheContext.Setup(c => c.Exist("Sql")).Returns(true);
        _cacheContext.Setup(c => c["Sql"]).Returns(otherCache.Object);
        var sut = BuildSut("Token");

        sut.GetCache("Redis").Should().BeSameAs(_redisCache.Object);
        sut.GetCache("Sql").Should().BeSameAs(otherCache.Object);
    }
}
