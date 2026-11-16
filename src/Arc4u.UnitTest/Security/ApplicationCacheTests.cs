using Arc4u.Caching;
using Arc4u.Dependency;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class ApplicationCacheTests
{
    private readonly Mock<ICache> _defaultCache = new();
    private readonly Mock<ICache> _namedCache = new();

    private ApplicationCache BuildSut()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddILogger();
        services.Configure<TokenCacheOptions>(o =>
        {
            o.CacheName = "Default";
            o.MaxTime = TimeSpan.FromMinutes(50);
        });
        services.Configure<TokenCacheOptions>("Client1", o =>
        {
            o.CacheName = "Named";
            o.MaxTime = TimeSpan.FromMinutes(5);
        });
        var container = services.BuildServiceProvider();
        var monitor = container.GetRequiredService<IOptionsMonitor<TokenCacheOptions>>();

        var cacheHelper = new Mock<ICacheHelper>();
        cacheHelper.Setup(m => m.GetCache()).Returns(_defaultCache.Object);
        cacheHelper.Setup(m => m.GetCache("Named")).Returns(_namedCache.Object);

        var resolver = new TokenCacheResolver(cacheHelper.Object, monitor, container.GetServices<IConfigureOptions<TokenCacheOptions>>(),
                                              container.GetRequiredService<ILogger<TokenCacheResolver>>());

        return new ApplicationCache(container.GetRequiredService<ILogger>(), resolver);
    }

    [Fact]
    public async Task Put_Without_OptionsName_Uses_The_Default_Options_Should()
    {
        var sut = BuildSut();

        await sut.PutAsync("key", "value");

        _defaultCache.Verify(m => m.PutAsync("key_tokencache", TimeSpan.FromMinutes(50), "value", false, It.IsAny<CancellationToken>()), Times.Once);
        _namedCache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Put_With_OptionsName_Uses_The_Named_Options_Should()
    {
        var sut = BuildSut();

        await sut.PutAsync("key", "value", "Client1");

        _namedCache.Verify(m => m.PutAsync("key_tokencache", TimeSpan.FromMinutes(5), "value", false, It.IsAny<CancellationToken>()), Times.Once);
        _defaultCache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Put_A_Token_Expiring_Before_MaxTime_Uses_Its_Expiry_Should()
    {
        var sut = BuildSut();
        var token = new TokenInfo("Bearer", "token", DateTime.UtcNow.AddMinutes(10));

        await sut.PutAsync("key", token);

        _defaultCache.Verify(m => m.PutAsync("key_tokencache", It.Is<TimeSpan>(t => t > TimeSpan.FromMinutes(9) && t <= TimeSpan.FromMinutes(10)), token, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Put_A_Token_Expiring_After_MaxTime_Uses_MaxTime_Should()
    {
        var sut = BuildSut();
        var token = new TokenInfo("Bearer", "token", DateTime.UtcNow.AddHours(2));

        await sut.PutAsync("key", token, "Client1");

        _namedCache.Verify(m => m.PutAsync("key_tokencache", TimeSpan.FromMinutes(5), token, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Put_An_Expired_Token_Removes_The_Entry_Should()
    {
        var sut = BuildSut();
        var token = new TokenInfo("Bearer", "token", DateTime.UtcNow.AddMinutes(-1));

        await sut.PutAsync("key", token);

        _defaultCache.Verify(m => m.RemoveAsync("key_tokencache", It.IsAny<CancellationToken>()), Times.Once);
        _defaultCache.Verify(m => m.PutAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<TokenInfo>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Get_And_Delete_With_OptionsName_Use_The_Named_Cache_Should()
    {
        _namedCache.Setup(m => m.GetAsync<string>("key_tokencache", It.IsAny<CancellationToken>())).ReturnsAsync("value");
        var sut = BuildSut();

        (await sut.GetAsync<string>("key", "Client1")).Should().Be("value");
        await sut.DeleteItemAsync("key", "Client1");

        _namedCache.Verify(m => m.RemoveAsync("key_tokencache", It.IsAny<CancellationToken>()), Times.Once);
        _defaultCache.VerifyNoOtherCalls();
    }
}
