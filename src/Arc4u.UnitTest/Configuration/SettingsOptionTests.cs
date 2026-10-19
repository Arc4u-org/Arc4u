using System.Text;
using Arc4u.Configuration;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Arc4u.UnitTest;

[Trait("Category", "CI")]
public class SettingsOptionTests
{
    /// <summary>
    ///     This simulates a configuration section with some non-string values
    /// </summary>
    private const string _json = @"{
    ""OAuth2.Settings"": {
        ""ProviderId"": ""Oidc"",
        ""AuthenticationType"": ""OAuth2Bearer"",
        ""Object"": ""True"",
        ""Complex"": [1, 2, 3]
        }
}";

    private readonly Fixture _fixture;

    public SettingsOptionTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void SimpleKeyValueShould()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(_json)))
            .Build();

        var section = configuration.GetSection("OAuth2.Settings");
        section.Exists().Should().BeTrue();

        var dic = section.GetChildren().ToDictionary(x => x.Key, x => x.Value);

        void options(SimpleKeyValueSettings settings)
        {
            foreach (var kv in dic!)
            {
                settings.Add(kv.Key, kv.Value!);
            }
        }

        IServiceCollection services = new ServiceCollection();

        services.Configure<SimpleKeyValueSettings>("OAuth2", options);

        var serviceProvider = services.BuildServiceProvider();

        var sut = serviceProvider.GetService<IOptionsMonitor<SimpleKeyValueSettings>>()!.Get("OAuth2");

        sut.Values.Count.Should().Be(4);
        sut.Values["ProviderId"].Should().Be("Oidc");
        sut.Values["AuthenticationType"].Should().Be("OAuth2Bearer");
        sut.Values["Object"].Should().Be("True");
        sut.Values["Complex"].Should().BeNull();
    }

    [Fact]
    public void ApplicationConfigFromConfigurationShould()
    {
        // Arrange
        IServiceCollection services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Application.Configuration:ApplicationName"] = "Arc4u.UnitTest",
                    ["Application.Configuration:Environment:Name"] = "Development",
                    ["Application.Configuration:Environment:LoggingName"] = "Arc4u.UnitTest",
                    ["Application.Configuration:Environment:TimeZone"] = "Romance Standard Time"
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        services.AddApplicationConfig(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // act
        var sut = serviceProvider.GetRequiredService<IOptionsMonitor<ApplicationConfig>>();

        // assert;

        sut.Should().NotBeNull();
        sut.CurrentValue.ApplicationName.Should().Be("Arc4u.UnitTest");
        sut.CurrentValue.Environment.Name.Should().Be("Development");
        sut.CurrentValue.Environment.LoggingName.Should().Be("Arc4u.UnitTest");
        sut.CurrentValue.Environment.TimeZone.Should().Be("Romance Standard Time");
    }

    [Fact]
    public void ApplicationConfigFromMissingSectionShouldThrowConfigurationException()
    {
        IServiceCollection services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        var act = () => services.AddApplicationConfig(configuration);

        act.Should().Throw<ConfigurationException>().WithMessage("*Application.Configuration*");
    }

    [Fact]
    public void ApplicationConfigWithEmptyValuesShouldListEveryMissingValue()
    {
        IServiceCollection services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Application.Configuration:ApplicationName"] = "Arc4u.UnitTest" })
            .Build();

        var act = () => services.AddApplicationConfig(configuration);

        act.Should().Throw<ConfigurationException>()
           .Where(e => !e.Message.Contains("Application name") && e.Message.Contains("initialization") && e.Message.Contains("time zone"));
    }
}
