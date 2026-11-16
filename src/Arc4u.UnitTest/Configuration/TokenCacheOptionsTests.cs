using Arc4u.Configuration;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class TokenCacheOptionsTests
{
    private readonly Fixture _fixture;

    public TokenCacheOptionsTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void TockeCacheOption_From_Action_Should()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddTokenCache(options => { options.CacheName = "test"; });

        var servicePovider = services.BuildServiceProvider();

        var options = servicePovider.GetRequiredService<IOptions<TokenCacheOptions>>();

        var sut = options.Value;

        sut.CacheName.Should().Be("test");
        sut.MaxTime.Should().Be(TimeSpan.FromMinutes(50));
    }

    [Fact]
    public void TockeCacheOption_From_Config_Should()
    {
        IServiceCollection services = new ServiceCollection();

        var options = _fixture.Create<TokenCacheOptions>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:TokenCache:CacheName"] = options.CacheName,
                    ["Authentication:TokenCache:MaxTime"] = options.MaxTime.ToString()
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        services.AddTokenCache(configuration);

        var servicePovider = services.BuildServiceProvider();

        var sutOptions = servicePovider.GetRequiredService<IOptions<TokenCacheOptions>>();

        var sut = sutOptions.Value;

        sut.CacheName.Should().Be(options.CacheName);
        sut.MaxTime.Should().Be(options.MaxTime);
    }

    [Fact]
    public void TockeCacheOption_From_Config_With_Default_MaxTime_Should()
    {
        IServiceCollection services = new ServiceCollection();

        var options = _fixture.Build<TokenCacheOptions>().With(p => p.CacheName, _fixture.Create<string>()).Create();
        var defaultOptions = new TokenCacheOptions();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["Authentication:TokenCache:CacheName"] = options.CacheName })
            .Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        services.AddTokenCache(configuration);

        var servicePovider = services.BuildServiceProvider();

        var sutOptions = servicePovider.GetRequiredService<IOptions<TokenCacheOptions>>();

        var sut = sutOptions.Value;

        sut.CacheName.Should().Be(options.CacheName);
        sut.MaxTime.Should().Be(defaultOptions.MaxTime);
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-00:10:00")]
    public void TockeCacheOption_From_Config_With_MaxTime_To_Zero_Exception_Should(string maxTime)
    {
        IServiceCollection services = new ServiceCollection();

        var options = _fixture.Create<TokenCacheOptions>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:TokenCache:CacheName"] = options.CacheName,
                    ["Authentication:TokenCache:MaxTime"] = maxTime
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        var exception = Record.Exception(() => services.AddTokenCache(configuration));

        exception.Should().NotBeNull();
        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void TockeCacheOption_From_Config_With_No_CacheName_Exception_Should()
    {
        IServiceCollection services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:TokenCache:CacheName"] = "",
                    ["Authentication:TokenCache:MaxTime"] = TimeSpan.FromMinutes(20).ToString()
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        var exception = Record.Exception(() => services.AddTokenCache(configuration));

        exception.Should().NotBeNull();
        exception.Should().BeOfType<ConfigurationException>();
    }
}
