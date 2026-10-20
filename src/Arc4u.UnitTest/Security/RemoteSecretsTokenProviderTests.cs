using System.Text;
using Arc4u.Configuration;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProvider;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class RemoteSecretsTokenProviderTests
{
    private readonly Fixture _fixture;

    public RemoteSecretsTokenProviderTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public async Task RemoteSecrets_TokenProvider_Should()
    {
        // arrange
        var options = _fixture.Create<RemoteSecretSettingsOptions>();

        var settings = new SimpleKeyValueSettings(new Dictionary<string, string>
        {
            { TokenKeys.ProviderIdKey, RemoteClientSecretTokenProvider.ProviderName },
            { TokenKeys.ClientSecret, options.ClientSecret },
            { TokenKeys.ClientSecretHeader, options.HeaderKey }
        });

        // act
        var sut = _fixture.Create<RemoteClientSecretTokenProvider>();
        var tokenResult = await sut.GetTokenAsync(settings, null);

        // assert
        tokenResult.Should().NotBeNull();
        tokenResult.IsSuccess.Should().BeTrue();
        var token = tokenResult.Value;
        token.Should().NotBeNull();
        token!.Token.Should().Be(options.ClientSecret);
        token.TokenType.Should().Be(options.HeaderKey);
    }

    [Theory]
    [InlineData("Basic")]
    [InlineData("basic")]
    public async Task RemoteSecrets_TokenProvider_With_Basic_And_Clear_Secret_Should_Encode_Base64(string headerKey)
    {
        // arrange
        const string clearSecret = "user:p@ssw0rd";

        var settings = new SimpleKeyValueSettings(new Dictionary<string, string>
        {
            { TokenKeys.ProviderIdKey, RemoteClientSecretTokenProvider.ProviderName },
            { TokenKeys.ClientSecret, clearSecret },
            { TokenKeys.ClientSecretHeader, headerKey }
        });

        // act
        var sut = _fixture.Create<RemoteClientSecretTokenProvider>();
        var tokenResult = await sut.GetTokenAsync(settings, null);

        // assert
        tokenResult.IsSuccess.Should().BeTrue();
        tokenResult.Value.Token.Should().Be(Convert.ToBase64String(Encoding.UTF8.GetBytes(clearSecret)));
        tokenResult.Value.TokenType.Should().Be(headerKey);
    }

    [Fact]
    public async Task RemoteSecrets_TokenProvider_With_Basic_And_Base64_Secret_Should_Not_Encode_Twice()
    {
        // arrange
        var encodedSecret = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:p@ssw0rd"));

        var settings = new SimpleKeyValueSettings(new Dictionary<string, string>
        {
            { TokenKeys.ProviderIdKey, RemoteClientSecretTokenProvider.ProviderName },
            { TokenKeys.ClientSecret, encodedSecret },
            { TokenKeys.ClientSecretHeader, "Basic" }
        });

        // act
        var sut = _fixture.Create<RemoteClientSecretTokenProvider>();
        var tokenResult = await sut.GetTokenAsync(settings, null);

        // assert
        tokenResult.IsSuccess.Should().BeTrue();
        tokenResult.Value.Token.Should().Be(encodedSecret);
    }

    [Fact]
    public async Task RemoteSecrets_TokenProvider_With_Custom_Header_Should_Not_Encode()
    {
        // arrange
        const string clearSecret = "user:p@ssw0rd";

        var settings = new SimpleKeyValueSettings(new Dictionary<string, string>
        {
            { TokenKeys.ProviderIdKey, RemoteClientSecretTokenProvider.ProviderName },
            { TokenKeys.ClientSecret, clearSecret },
            { TokenKeys.ClientSecretHeader, "SecretKey" }
        });

        // act
        var sut = _fixture.Create<RemoteClientSecretTokenProvider>();
        var tokenResult = await sut.GetTokenAsync(settings, null);

        // assert
        tokenResult.IsSuccess.Should().BeTrue();
        tokenResult.Value.Token.Should().Be(clearSecret);
    }

    [Fact]
    public async Task RemoteSecrets_TokenProvider_With_No_Settings_Should()
    {
        // arrange

        // act
        var sut = _fixture.Create<RemoteClientSecretTokenProvider>();
        var exception =
            await Record.ExceptionAsync(async () => await sut.GetTokenAsync(null, null).ConfigureAwait(false));

        // assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ArgumentNullException>();
    }

    [Fact]
    public async Task RemoteSecrets_TokenProvider_With_No_ClientSecret_Should()
    {
        // arrange
        var options = _fixture.Create<RemoteSecretSettingsOptions>();

        var settings = new SimpleKeyValueSettings(new Dictionary<string, string>
        {
            { TokenKeys.ProviderIdKey, RemoteClientSecretTokenProvider.ProviderName },
            { TokenKeys.ClientSecretHeader, options.HeaderKey }
        });

        // act
        var sut = _fixture.Create<RemoteClientSecretTokenProvider>();
        var exception =
            await Record.ExceptionAsync(async () => await sut.GetTokenAsync(settings, null).ConfigureAwait(false));

        // assert
        exception.Should().NotBeNull();
        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public async Task RemoteSecrets_TokenProvider_With_No_HeaderKey_Should()
    {
        // arrange
        var options = _fixture.Create<RemoteSecretSettingsOptions>();

        var settings = new SimpleKeyValueSettings(new Dictionary<string, string>
        {
            { TokenKeys.ProviderIdKey, RemoteClientSecretTokenProvider.ProviderName },
            { TokenKeys.ClientSecret, options.ClientSecret }
        });

        // act
        var sut = _fixture.Create<RemoteClientSecretTokenProvider>();
        var result = await sut.GetTokenAsync(settings, null);

        // assert
        result.IsFailed.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Client secret Header is missing. Cannot process the request.");
    }
}
