using Arc4u.Configuration;
using Arc4u.OAuth2;
using Arc4u.OAuth2.Middleware;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Arc4u.UnitTest;

[Trait("Category", "CI")]
public class BasicSettingsOptionsTests
{
    private readonly Fixture _fixture;

    public BasicSettingsOptionsTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void Basic_Standard_Should()
    {
        var options = _fixture.Create<BasicSettingsOptions>();
        var _default = new BasicSettingsOptions();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["Authentication:Basic:Settings:ClientId"] = options.ClientId })
            .Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        IServiceCollection services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddBasicAuthenticationSettings(configuration));

        exception.Should().NotBeNull();
        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void Basic_Standard_No_Section_Should_Throw_An_Exception()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                []).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        IServiceCollection services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddBasicAuthenticationSettings(configuration));

        exception.Should().NotBeNull();
        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void Basic_Standard_No_Section_Should()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                []).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        IServiceCollection services = new ServiceCollection();

        services.AddBasicAuthenticationSettings(configuration, throwExceptionIfSectionDoesntExist: false);

        var serviceProvider = services.BuildServiceProvider();

        // act
        var sut = serviceProvider.GetService<IOptions<BasicAuthenticationSettingsOptions>>();

        sut.Should().BeNull();
    }

    [Fact]
    public void Custom_Standard_Should()
    {
        var options = _fixture.Create<BasicSettingsOptions>();

        var configDic = new Dictionary<string, string?>
        {
            ["Authentication:Basic:Settings:ClientId"] = options.ClientId,
            ["Authentication:Basic:Settings:ProviderId"] = options.ProviderId,
            ["Authentication:Basic:Settings:AuthenticationType"] = options.AuthenticationType,
            ["Authentication:Basic:Settings:ClientSecret"] = options.ClientSecret
        };
        foreach (var scope in options.Scopes)
        {
            configDic.Add($"Authentication:Basic:Settings:Scopes:{options.Scopes.IndexOf(scope)}", scope);
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDic).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        IServiceCollection services = new ServiceCollection();

        services.AddBasicAuthenticationSettings(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // act
        var sut = serviceProvider.GetService<IOptions<BasicAuthenticationSettingsOptions>>()!.Value;

        sut.Should().NotBeNull();
        sut.BasicSettings.Values[TokenKeys.ClientIdKey].Should().Be(options.ClientId);
        sut.BasicSettings.Values[TokenKeys.ProviderIdKey].Should().Be(options.ProviderId);
        sut.BasicSettings.Values[TokenKeys.AuthenticationTypeKey].Should().Be(options.AuthenticationType);
        sut.BasicSettings.Values[TokenKeys.Scope].Should().Be(string.Join(' ', options.Scopes));
        sut.BasicSettings.Values[TokenKeys.ClientSecret].Should().Be(options.ClientSecret);
    }

    [Fact]
    public void Basic_Settings_Default_Upn_Should()
    {
        var options = _fixture.Create<BasicSettingsOptions>();

        var configDic = new Dictionary<string, string?>
        {
            ["Authentication:Basic:Settings:ClientId"] = options.ClientId,
            ["Authentication:Basic:Settings:ProviderId"] = options.ProviderId,
            ["Authentication:Basic:Settings:AuthenticationType"] = options.AuthenticationType,
            ["Authentication:Basic:DefaultUpn"] = "@arc4u.net"
        };
        foreach (var scope in options.Scopes)
        {
            configDic.Add($"Authentication:Basic:Settings:Scopes:{options.Scopes.IndexOf(scope)}", scope);
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDic).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        IServiceCollection services = new ServiceCollection();

        services.AddBasicAuthenticationSettings(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // act
        var sut = serviceProvider.GetService<IOptions<BasicAuthenticationSettingsOptions>>()!.Value;

        sut.Should().NotBeNull();
        sut.DefaultUpn.Should().Be("@arc4u.net");
        sut.BasicSettings.Values.ContainsKey(TokenKeys.ClientSecret).Should().BeFalse();
    }

    [Fact]
    public void BasicCertificateShould()
    {
        var options = _fixture.Create<BasicSettingsOptions>();

        var configDic = new Dictionary<string, string?>
        {
            ["Authentication:Basic:Settings:ClientId"] = options.ClientId,
            ["Authentication:Basic:Settings:ProviderId"] = options.ProviderId,
            ["Authentication:Basic:Settings:AuthenticationType"] = options.AuthenticationType,
            ["Authentication:Basic:Certificates:Cert1:File:Cert"] = @"./Configs/cert.pem",
            ["Authentication:Basic:Certificates:Cert1:File:Key"] = @"./Configs/key.pem"
        };
        foreach (var scope in options.Scopes)
        {
            configDic.Add($"Authentication:Basic:Settings:Scopes:{options.Scopes.IndexOf(scope)}", scope);
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDic).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        IServiceCollection services = new ServiceCollection();

        services.AddBasicAuthenticationSettings(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // act
        var sut = serviceProvider.GetService<IOptions<BasicAuthenticationSettingsOptions>>()!.Value;

        sut.Should().NotBeNull();
        sut.CertificateHeaderOptions.Should().NotBeNull();
        sut.CertificateHeaderOptions.Should().NotBeEmpty();
        sut.CertificateHeaderOptions.First().Key.Should().Be("Cert1");
    }
}
