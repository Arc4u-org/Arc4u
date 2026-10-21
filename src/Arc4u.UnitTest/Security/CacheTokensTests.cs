using Arc4u.Configuration;
using Arc4u.IdentityModel.Claims;
using Arc4u.OAuth2.Client.Authentication.Cache;
using Arc4u.OAuth2.Token;
using AwesomeAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public sealed class CacheTokensTests : IDisposable
{
    private readonly CacheTokens _cache;
    private readonly string _key = Guid.NewGuid().ToString("N");

    public CacheTokensTests()
    {
        var config = new ApplicationConfig { Environment = { LoggingName = nameof(CacheTokensTests), Name = "Test" } };
        _cache = new CacheTokens(Options.Create(config));
    }

    public void Dispose()
    {
        _cache.Remove(_key);
        _cache.Dispose();
    }

    [Fact]
    public async Task TokenRefreshInfoShouldRoundTrip()
    {
        var expiresOn = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var tokens = new TokenRefreshInfo
        {
            AccessToken = new TokenInfo("Bearer", "access", expiresOn),
            RefreshToken = new TokenInfo("Refresh", "refresh", expiresOn),
            IdentityToken = "id"
        };

        await _cache.PutAsync(_key, tokens);
        var result = await _cache.GetAsync<TokenRefreshInfo>(_key);

        result.Should().NotBeNull();
        result!.AccessToken!.Token.Should().Be("access");
        result.AccessToken.TokenType.Should().Be("Bearer");
        result.AccessToken.ExpiresOnUtc.Should().Be(expiresOn);
        result.RefreshToken!.Token.Should().Be("refresh");
        result.IdentityToken.Should().Be("id");
    }

    [Fact]
    public void TokenInfoShouldRoundTrip()
    {
        var expiresOn = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);

        _cache.Put(_key, new TokenInfo("Bearer", "access", expiresOn));

        _cache.TryGetValue<TokenInfo>(_key, out var result).Should().BeTrue();
        result.Token.Should().Be("access");
        result.ExpiresOnUtc.Should().Be(expiresOn);
    }

    [Fact]
    public void ClaimsShouldRoundTrip()
    {
        _cache.Put(_key, new List<ClaimDto> { new("name", "John"), new("role", "admin") });

        var result = _cache.Get<List<ClaimDto>>(_key);

        result.Should().HaveCount(2);
        result[1].ClaimType.Should().Be("role");
        result[1].Value.Should().Be("admin");
    }

    [Fact]
    public void StringShouldRoundTrip()
    {
        _cache.Put(_key, "user@arc4u.net");

        _cache.Get<string>(_key).Should().Be("user@arc4u.net");
    }

    [Fact]
    public void TypeOutsideTheContextShouldUseReflectionWhenEnabled()
    {
        _cache.Put(_key, new Dictionary<string, int> { ["a"] = 1 });

        _cache.Get<Dictionary<string, int>>(_key).Should().ContainKey("a").WhoseValue.Should().Be(1);
    }
}
