using Arc4u.Caching;
using Arc4u.OAuth2.Token;
using AwesomeAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class ApplicationLocalDataCacheTests
{
    private readonly Mock<ISecureCache> _secureCache = new();

    [Fact]
    public async Task Each_Key_Keeps_Its_Own_Token_Should()
    {
        var sut = new ApplicationLocalDataCache(_secureCache.Object, NullLogger.Instance);
        var tokenA = new TokenInfo("Bearer", "token-a", DateTime.UtcNow.AddHours(1));
        var tokenB = new TokenInfo("Bearer", "token-b", DateTime.UtcNow.AddHours(1));

        await sut.PutAsync("a", tokenA);
        await sut.PutAsync("b", tokenB);

        (await sut.GetAsync<TokenInfo>("a")).Should().BeSameAs(tokenA);
        (await sut.GetAsync<TokenInfo>("b")).Should().BeSameAs(tokenB);
        // Served from memory: the secure cache is not read.
        _secureCache.Verify(c => c.GetAsync<TokenInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task A_Key_Without_Token_Does_Not_Get_The_Token_Of_Another_Key_Should()
    {
        var sut = new ApplicationLocalDataCache(_secureCache.Object, NullLogger.Instance);
        await sut.PutAsync("a", new TokenInfo("Bearer", "token-a", DateTime.UtcNow.AddHours(1)));

        (await sut.GetAsync<TokenInfo>("b")).Should().BeNull();
        _secureCache.Verify(c => c.GetAsync<TokenInfo>("b", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteItem_Removes_Only_Its_Key_Should()
    {
        var sut = new ApplicationLocalDataCache(_secureCache.Object, NullLogger.Instance);
        var tokenB = new TokenInfo("Bearer", "token-b", DateTime.UtcNow.AddHours(1));
        await sut.PutAsync("a", new TokenInfo("Bearer", "token-a", DateTime.UtcNow.AddHours(1)));
        await sut.PutAsync("b", tokenB);

        await sut.DeleteItemAsync("a");

        (await sut.GetAsync<TokenInfo>("a")).Should().BeNull();
        (await sut.GetAsync<TokenInfo>("b")).Should().BeSameAs(tokenB);
        _secureCache.Verify(c => c.RemoveAsync("a", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task A_Token_Read_From_The_Secure_Cache_Is_Kept_In_Memory_Should()
    {
        var token = new TokenInfo("Bearer", "persisted", DateTime.UtcNow.AddHours(1));
        _secureCache.Setup(c => c.GetAsync<TokenInfo>("a", It.IsAny<CancellationToken>())).ReturnsAsync(token);
        var sut = new ApplicationLocalDataCache(_secureCache.Object, NullLogger.Instance);

        (await sut.GetAsync<TokenInfo>("a")).Should().BeSameAs(token);
        (await sut.GetAsync<TokenInfo>("a")).Should().BeSameAs(token);

        _secureCache.Verify(c => c.GetAsync<TokenInfo>("a", It.IsAny<CancellationToken>()), Times.Once);
    }
}
