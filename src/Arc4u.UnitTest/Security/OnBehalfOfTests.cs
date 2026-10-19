using Arc4u.Configuration;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class OnBehalfOfTests
{
    private readonly Fixture _fixture;

    public OnBehalfOfTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void OnBehalfOfAuthenticationOptionsShould()
    {
        var settings = _fixture.Create<OnBehalfOfSettingsOptions>();

        var configDic = new Dictionary<string, string?>
        {
            ["Authentication:OnBehalfOf:Obo1:ClientId"] = settings.ClientId,
            ["Authentication:OnBehalfOf:Obo1:ClientSecret"] = settings.ClientSecret
        };
        settings.Scopes.ForEach(scope =>
            configDic.Add($"Authentication:OnBehalfOf:Obo1:Scopes:{settings.Scopes.IndexOf(scope)}", scope));
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDic).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        IServiceCollection services = new ServiceCollection();

        services.AddOnBehalfOf(configuration);

        // The token providers are resolved one by one at the sign-out.
        services.Should().ContainSingle(d => d.ServiceType == typeof(SignOutTokenProviderKeys));

        var app = services.BuildServiceProvider();

        var oboSettings = app.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>().Get("Obo1");

        oboSettings.Should().NotBeNull();
        oboSettings.Values[TokenKeys.ClientIdKey].Should().Be(settings.ClientId);
        oboSettings.Values[TokenKeys.ClientSecret].Should().Be(settings.ClientSecret);
        oboSettings.Values[TokenKeys.Scope].Should().Be(string.Join(' ', settings.Scopes));
    }
}
