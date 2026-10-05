using System.Security.Claims;
using Arc4u.Caching;
using Arc4u.OAuth2.Security.Principal;
using Arc4u.OAuth2.Token;
using Arc4u.Security.Principal;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class ServerPrincipalCacheTests
{
    private readonly Mock<ICache> _cache = new();
    private readonly ServerPrincipalCache _sut;

    public ServerPrincipalCacheTests()
    {
        var cacheHelper = new Mock<ICacheHelper>();
        cacheHelper.Setup(h => h.GetCache()).Returns(_cache.Object);
        _sut = new ServerPrincipalCache(cacheHelper.Object);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Put_With_Timeout_Should_Cache_The_Value_And_The_Sliding_Flag(bool isSlided)
    {
        var principal = new AppPrincipal(new global::Arc4u.Security.Principal.Authorization(), new ClaimsIdentity(), "S-1-0-0");
        var timeout = TimeSpan.FromMinutes(1);

        _sut.Put("key", timeout, principal, isSlided);

        _cache.Verify(c => c.Put("key", timeout, principal, isSlided), Times.Once);
        _cache.Verify(c => c.Put(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }
}
