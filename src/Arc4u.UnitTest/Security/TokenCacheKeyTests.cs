using Arc4u.OAuth2.Token;
using AwesomeAssertions;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class TokenCacheKeyTests
{
    [Fact]
    public void Hash_Is_Stable_Across_Processes_Should()
    {
        // SHA-256 of "3:abc" computed independently: the value must never depend on the process.
        TokenCacheKey.Hash("abc").Should().Be("AAB5F9AE99B2E38FB462025C8F72F570C9C811705D2A4277DC855D7FA293FE97");
    }

    [Fact]
    public void Hash_Same_Parts_Give_Same_Hash_Should()
    {
        TokenCacheKey.Hash("a", "b").Should().Be(TokenCacheKey.Hash("a", "b"));
    }

    [Fact]
    public void Hash_Different_Splits_Give_Different_Hashes_Should()
    {
        TokenCacheKey.Hash("ab", "c").Should().NotBe(TokenCacheKey.Hash("a", "bc"));
    }

    [Fact]
    public void Hash_Null_Part_Is_Empty_Should()
    {
        TokenCacheKey.Hash(null, "a").Should().Be(TokenCacheKey.Hash(string.Empty, "a"));
    }
}
