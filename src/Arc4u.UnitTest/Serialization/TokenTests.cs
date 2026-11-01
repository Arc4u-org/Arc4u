using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Arc4u.Caching;
using Arc4u.Caching.Memory;
using Arc4u.Configuration.Memory;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Token;
using Arc4u.Serializer;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Serialization;

[Trait("Category", "CI")]
public class TokenTests
{
    private readonly Fixture _fixture;

    public TokenTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void AccessTokenValidityShould()
    {
        // act
        var sut = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")], DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1));

        // assert
        (sut.ValidTo > DateTime.UtcNow.AddMinutes(-5)).Should().BeTrue();
    }

    [Fact]
    public void AccessTokenValidityShouldNot()
    {
        // act
        var sut = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")], DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddMinutes(-10));

        // assert
        (sut.ValidTo > DateTime.UtcNow.AddMinutes(-5)).Should().BeFalse();
    }

    [Fact]
    public void AccessTokenSerializationJsonShouldNot()
    {
        // Arrange
        var jwt = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")], DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddMinutes(-10));

        var tokenInfo = new TokenInfo("Bearer", jwt.EncodedPayload, DateTime.UtcNow);
        var storeName = "store name";

        IServiceCollection services = new ServiceCollection();

        services.AddTransient<ICache, MemoryCache>();
        services.AddMemoryCache(storeName, options => options.SizeLimitInMB = 10);
        services.AddTransient<IObjectSerialization, JsonSerialization>();

        var mockILoggerFactory = new Mock<ILoggerFactory>();
        mockILoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        services.AddSingleton(mockILoggerFactory.Object);
        services.AddTransient(typeof(ILogger<>), typeof(LoggerWrapper<>));
        services.AddKeyedTransient<IAddPropertiesToLog, NullLoggerProperties>("Transient");

        var serviceProvider = services.BuildServiceProvider();

        var sut = serviceProvider.GetRequiredService<ICache>();
        sut.Initialize(storeName);

        sut.Put("key", tokenInfo);

        // act
        var cachedToken = sut.Get<TokenInfo>("key");

        // assert
        cachedToken.Should().NotBeNull();
        cachedToken!.Token.Should().Be(jwt.EncodedPayload);
        cachedToken.TokenType.Should().Be(tokenInfo.TokenType);
        cachedToken.ExpiresOnUtc.Should().Be(tokenInfo.ExpiresOnUtc);
    }

    [Fact]
    public void AccessTokenSerializationProtobufShouldNot()
    {
        // Arrange
        var jwt = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")], DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddMinutes(-10));

        var tokenInfo = new TokenInfo("Bearer", jwt.EncodedPayload, DateTime.UtcNow);

        var storeName = "store name";
        IServiceCollection services = new ServiceCollection();

        services.AddTransient<ICache, MemoryCache>();
        services.AddMemoryCache(storeName, options => options.SizeLimitInMB = 10);
        services.AddTransient<IObjectSerialization, JsonSerialization>();

        var mockILoggerFactory = new Mock<ILoggerFactory>();
        mockILoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        services.AddSingleton(mockILoggerFactory.Object);
        services.AddTransient(typeof(ILogger<>), typeof(LoggerWrapper<>));
        services.AddKeyedTransient<IAddPropertiesToLog, NullLoggerProperties>("Transient");

        var serviceProvider = services.BuildServiceProvider();

        var sut = serviceProvider.GetRequiredService<ICache>();
        sut.Initialize(storeName);

        sut.Put("key", tokenInfo);

        // act
        var cachedToken = sut.Get<TokenInfo>("key");

        // assert
        cachedToken.Should().NotBeNull();
        cachedToken!.Token.Should().Be(jwt.EncodedPayload);
        cachedToken.TokenType.Should().Be(tokenInfo.TokenType);
        cachedToken.ExpiresOnUtc.Should().Be(tokenInfo.ExpiresOnUtc);
    }
}
