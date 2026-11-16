using Arc4u.Configuration;
using Arc4u.OAuth2;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProvider;
using Arc4u.OAuth2.TokenProvider.Scenarios;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Arc4u.UnitTest;

[Trait("Category", "CI")]
public class ClientTokensOptionsTests
{
    private readonly Fixture _fixture;

    public ClientTokensOptionsTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void UserPassword_Scenario_Should()
    {
        var clientId = _fixture.Create<string>();
        var user = _fixture.Create<string>();
        var password = _fixture.Create<string>();
        var authUrl = _fixture.Create<Uri>().ToString();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokens:Client1:Scenario"] = UserPasswordScenario.Name,
                    ["Authentication:ClientTokens:Client1:Authority:Url"] = authUrl,
                    ["Authentication:ClientTokens:Client1:Settings:ClientId"] = clientId,
                    ["Authentication:ClientTokens:Client1:Settings:User"] = user,
                    ["Authentication:ClientTokens:Client1:Settings:Password"] = password
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        IServiceCollection services = new ServiceCollection();
        services.AddClientTokens(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var sut = serviceProvider.GetService<IOptionsMonitor<SimpleKeyValueSettings>>()!.Get("Client1");

        sut.Should().NotBeNull();
        sut.Values[TokenKeys.ProviderIdKey].Should().Be(CredentialTokenCacheTokenProvider.ProviderName);
        sut.Values[TokenKeys.ClientIdKey].Should().Be(clientId);
        sut.Values[TokenKeys.AuthorityKey].Should().Be("Client1");
        sut.Values[TokenKeys.Scope].Should().Be(Constants.OpenIdScope);
        sut.Values["User"].Should().Be(user);
        sut.Values["Password"].Should().Be(password);
        sut.Values.ContainsKey("Credential").Should().BeFalse();
        sut.Values.ContainsKey(TokenKeys.ExtraParameters).Should().BeFalse();

        var sutAuthority = serviceProvider.GetService<IOptionsMonitor<AuthorityOptions>>()!.Get("Client1");
        sutAuthority.Should().NotBeNull();
        sutAuthority.Url.Should().Be(authUrl);
    }

    [Fact]
    public void ClientToken_With_TokenCache_Registers_Named_TokenCacheOptions_Should()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:TokenCache:CacheName"] = "Default",
                    ["Authentication:TokenCache:MaxTime"] = "00:40:00",
                    ["Authentication:ClientTokens:Client1:Scenario"] = ClientCredentialsScenario.Name,
                    ["Authentication:ClientTokens:Client1:Settings:ClientId"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client1:Settings:ClientSecret"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client1:TokenCache:CacheName"] = "ClientCache",
                    ["Authentication:ClientTokens:Client2:Scenario"] = ClientCredentialsScenario.Name,
                    ["Authentication:ClientTokens:Client2:Settings:ClientId"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client2:Settings:ClientSecret"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client2:TokenCache:MaxTime"] = "00:05:00",
                    ["Authentication:ClientTokens:Client3:Scenario"] = ClientCredentialsScenario.Name,
                    ["Authentication:ClientTokens:Client3:Settings:ClientId"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client3:Settings:ClientSecret"] = _fixture.Create<string>(),
                }).Build();

        IServiceCollection services = new ServiceCollection();
        services.AddTokenCache(config);
        services.AddClientTokens(config);

        var serviceProvider = services.BuildServiceProvider();
        var settings = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();
        var tokenCacheOptions = serviceProvider.GetRequiredService<IOptionsMonitor<TokenCacheOptions>>();

        // Only the given value overrides the default TokenCacheOptions.
        settings.Get("Client1").Values[TokenKeys.TokenCacheOptionsName].Should().Be("Client1");
        tokenCacheOptions.Get("Client1").CacheName.Should().Be("ClientCache");
        tokenCacheOptions.Get("Client1").MaxTime.Should().Be(TimeSpan.FromMinutes(40));

        settings.Get("Client2").Values[TokenKeys.TokenCacheOptionsName].Should().Be("Client2");
        tokenCacheOptions.Get("Client2").CacheName.Should().Be("Default");
        tokenCacheOptions.Get("Client2").MaxTime.Should().Be(TimeSpan.FromMinutes(5));

        // No TokenCache section: the default TokenCacheOptions are used.
        settings.Get("Client3").Values.ContainsKey(TokenKeys.TokenCacheOptionsName).Should().BeFalse();
    }

    [Fact]
    public void ClientTokensCache_Is_The_Fallback_Of_Every_Entry_Should()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:TokenCache:CacheName"] = "Default",
                    ["Authentication:TokenCache:MaxTime"] = "00:40:00",
                    ["Authentication:ClientTokensCache:CacheName"] = "Shared",
                    ["Authentication:ClientTokens:Client1:Scenario"] = ClientCredentialsScenario.Name,
                    ["Authentication:ClientTokens:Client1:Settings:ClientId"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client1:Settings:ClientSecret"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client2:Scenario"] = ClientCredentialsScenario.Name,
                    ["Authentication:ClientTokens:Client2:Settings:ClientId"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client2:Settings:ClientSecret"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client2:TokenCache:MaxTime"] = "00:05:00",
                }).Build();

        IServiceCollection services = new ServiceCollection();
        services.AddTokenCache(config);
        services.AddClientTokens(config);
        // A second call (another composition path) registers nothing twice.
        services.AddClientTokens(config);

        var serviceProvider = services.BuildServiceProvider();
        var settings = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();
        var tokenCacheOptions = serviceProvider.GetRequiredService<IOptionsMonitor<TokenCacheOptions>>();

        // The shared section falls back on the default TokenCacheOptions for MaxTime.
        var shared = tokenCacheOptions.Get(ClientTokensExtension.TokenCacheOptionsName);
        shared.CacheName.Should().Be("Shared");
        shared.MaxTime.Should().Be(TimeSpan.FromMinutes(40));

        // An entry without its own TokenCache uses the shared one.
        settings.Get("Client1").Values[TokenKeys.TokenCacheOptionsName].Should().Be(ClientTokensExtension.TokenCacheOptionsName);

        // An entry with its own TokenCache falls back on the shared one, then on the default.
        settings.Get("Client2").Values[TokenKeys.TokenCacheOptionsName].Should().Be("Client2");
        tokenCacheOptions.Get("Client2").CacheName.Should().Be("Shared");
        tokenCacheOptions.Get("Client2").MaxTime.Should().Be(TimeSpan.FromMinutes(5));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void An_Empty_TokenCache_Section_Name_Turns_The_Shared_Section_Off_Should(string tokenCacheSectionName)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokensCache:CacheName"] = "Shared",
                    ["Authentication:ClientTokens:Client1:Scenario"] = ClientCredentialsScenario.Name,
                    ["Authentication:ClientTokens:Client1:Settings:ClientId"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client1:Settings:ClientSecret"] = _fixture.Create<string>(),
                }).Build();

        IServiceCollection services = new ServiceCollection();
        services.AddClientTokens(config, tokenCacheSectionName: tokenCacheSectionName);

        // The entry uses the default options: no TokenCacheOptions name is passed to the cache.
        var settings = services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();
        settings.Get("Client1").Values.ContainsKey(TokenKeys.TokenCacheOptionsName).Should().BeFalse();
    }

    [Fact]
    public void ClientTokensCache_With_An_Entry_Named_Like_It_Should_Throw()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokensCache:CacheName"] = "Shared",
                    ["Authentication:ClientTokens:clienttokenscache:Scenario"] = ClientCredentialsScenario.Name,
                    ["Authentication:ClientTokens:clienttokenscache:Settings:ClientId"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:clienttokenscache:Settings:ClientSecret"] = _fixture.Create<string>(),
                }).Build();

        IServiceCollection services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddClientTokens(config));

        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void ClientToken_With_A_Zero_TokenCache_MaxTime_Should_Throw()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokens:Client1:Scenario"] = ClientCredentialsScenario.Name,
                    ["Authentication:ClientTokens:Client1:Settings:ClientId"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client1:Settings:ClientSecret"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client1:TokenCache:MaxTime"] = "00:00:00",
                }).Build();

        IServiceCollection services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddClientTokens(config));

        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void ClientCredentials_Scenario_With_ExtraParameters_Should()
    {
        var clientId = _fixture.Create<string>();
        var clientSecret = _fixture.Create<string>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokens:Logto:Scenario"] = ClientCredentialsScenario.Name,
                    ["Authentication:ClientTokens:Logto:Settings:ClientId"] = clientId,
                    ["Authentication:ClientTokens:Logto:Settings:ClientSecret"] = clientSecret,
                    ["Authentication:ClientTokens:Logto:Settings:resource"] = "https://api.example.com/path",
                    ["Authentication:ClientTokens:Logto:Settings:audience"] = "logto"
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        IServiceCollection services = new ServiceCollection();
        services.AddClientTokens(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var sut = serviceProvider.GetService<IOptionsMonitor<SimpleKeyValueSettings>>()!.Get("Logto");

        sut.Should().NotBeNull();
        sut.Values[TokenKeys.ProviderIdKey].Should().Be(ClientCredentialsScenario.ProviderName);
        sut.Values[TokenKeys.ClientIdKey].Should().Be(clientId);
        sut.Values[TokenKeys.ClientSecret].Should().Be(clientSecret);
        sut.Values.ContainsKey(TokenKeys.AuthorityKey).Should().BeFalse();

        // Unclaimed Settings keys flow through as extra parameters.
        var extra = ExtraParametersEncoder.Decode(sut.Values[TokenKeys.ExtraParameters]).ToDictionary(p => p.Key, p => p.Value);
        extra.Should().HaveCount(2);
        extra["resource"].Should().Be("https://api.example.com/path");
        extra["audience"].Should().Be("logto");

        // Check a second call is not filling twice the settings parameters.
        using var scope = serviceProvider.CreateScope();
        sut = scope.ServiceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>().Get("Logto");
        sut.Should().NotBeNull();
        sut.Values[TokenKeys.ProviderIdKey].Should().Be(ClientCredentialsScenario.ProviderName);
        sut.Values[TokenKeys.ClientIdKey].Should().Be(clientId);
        sut.Values[TokenKeys.ClientSecret].Should().Be(clientSecret);
        sut.Values.ContainsKey(TokenKeys.AuthorityKey).Should().BeFalse();

        // Unclaimed Settings keys flow through as extra parameters.
        extra = ExtraParametersEncoder.Decode(sut.Values[TokenKeys.ExtraParameters]).ToDictionary(p => p.Key, p => p.Value);
        extra.Should().HaveCount(2);
        extra["resource"].Should().Be("https://api.example.com/path");
        extra["audience"].Should().Be("logto");
    }

    [Fact]
    public void Missing_Scenario_Discriminator_Should_Throw()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokens:Client1:Settings:ClientId"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client1:Settings:User"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client1:Settings:Password"] = _fixture.Create<string>()
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddClientTokens(configuration));

        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void Unknown_Scenario_Discriminator_Should_Throw()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokens:Client1:Scenario"] = "DoesNotExist",
                    ["Authentication:ClientTokens:Client1:Settings:ClientId"] = _fixture.Create<string>()
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddClientTokens(configuration));

        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void ClientCredentials_Missing_ClientSecret_Should_Throw()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokens:Logto:Scenario"] = ClientCredentialsScenario.Name,
                    ["Authentication:ClientTokens:Logto:Settings:ClientId"] = _fixture.Create<string>()
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddClientTokens(configuration));

        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void Basic_Scenario_Decodes_Credential_Should()
    {
        var clientId = _fixture.Create<string>();
        var user = _fixture.Create<string>();
        var password = _fixture.Create<string>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokens:Client1:Scenario"] = BasicScenario.Name,
                    ["Authentication:ClientTokens:Client1:Settings:ClientId"] = clientId,
                    ["Authentication:ClientTokens:Client1:Settings:Credential"] = $"{user}:{password}"
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        IServiceCollection services = new ServiceCollection();
        services.AddClientTokens(configuration);

        var serviceProvider = services.BuildServiceProvider();

        var sut = serviceProvider.GetService<IOptionsMonitor<SimpleKeyValueSettings>>()!.Get("Client1");

        sut.Should().NotBeNull();
        // The credential pair is split into User/Password at configuration time and targets the cache provider.
        sut.Values[TokenKeys.ProviderIdKey].Should().Be(CredentialTokenCacheTokenProvider.ProviderName);
        sut.Values[TokenKeys.ClientIdKey].Should().Be(clientId);
        sut.Values["User"].Should().Be(user);
        sut.Values["Password"].Should().Be(password);
        sut.Values.ContainsKey("Credential").Should().BeFalse();
        sut.Values.ContainsKey(TokenKeys.ExtraParameters).Should().BeFalse();
    }

    [Fact]
    public void Basic_Missing_Credential_Should_Throw()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokens:Client1:Scenario"] = BasicScenario.Name,
                    ["Authentication:ClientTokens:Client1:Settings:ClientId"] = _fixture.Create<string>()
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddClientTokens(configuration));

        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void Basic_Credential_Without_Pair_Format_Should_Throw()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokens:Client1:Scenario"] = BasicScenario.Name,
                    ["Authentication:ClientTokens:Client1:Settings:ClientId"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client1:Settings:Credential"] = "no-colon-here"
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddClientTokens(configuration));

        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void AddClientTokens_Called_Twice_Should_Register_Entry_Once()
    {
        var clientId = _fixture.Create<string>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokens:Client1:Scenario"] = ClientCredentialsScenario.Name,
                    ["Authentication:ClientTokens:Client1:Settings:ClientId"] = clientId,
                    ["Authentication:ClientTokens:Client1:Settings:ClientSecret"] = _fixture.Create<string>()
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        // Mimic two composition paths registering the same client tokens.
        services.AddClientTokens(configuration);
        services.AddClientTokens(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // Resolving the options must not throw a duplicate-key exception (WriteTo runs once).
        var sut = serviceProvider.GetService<IOptionsMonitor<SimpleKeyValueSettings>>()!.Get("Client1");

        sut.Should().NotBeNull();
        sut.Values[TokenKeys.ClientIdKey].Should().Be(clientId);
    }

    [Fact]
    public void UserPassword_Password_Without_User_Should_Throw()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokens:Client1:Scenario"] = UserPasswordScenario.Name,
                    ["Authentication:ClientTokens:Client1:Settings:ClientId"] = _fixture.Create<string>(),
                    ["Authentication:ClientTokens:Client1:Settings:Password"] = _fixture.Create<string>()
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddClientTokens(configuration));

        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void Custom_Scenario_Can_Be_Registered_Should()
    {
        var clientId = _fixture.Create<string>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:ClientTokens:Client1:Scenario"] = "Custom",
                    ["Authentication:ClientTokens:Client1:Settings:ClientId"] = clientId
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        try
        {
            ClientTokensExtension.SetScenarioResolver(discriminator => discriminator switch
            {
                "Custom" => new CustomScenario(),
                _ => ClientTokensExtension.DefaultScenario(discriminator)
            });

            services.AddClientTokens(configuration);

            var serviceProvider = services.BuildServiceProvider();
            var sut = serviceProvider.GetService<IOptionsMonitor<SimpleKeyValueSettings>>()!.Get("Client1");

            sut.Values[TokenKeys.ProviderIdKey].Should().Be("Custom");
            sut.Values[TokenKeys.ClientIdKey].Should().Be(clientId);
        }
        finally
        {
            // Restore the default resolver so the global static does not leak into other tests.
            ClientTokensExtension.SetScenarioResolver(ClientTokensExtension.DefaultScenario);
        }
    }

    [Fact]
    public void No_ClientTokens_Section_Should()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection([]).Build();
        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        services.AddClientTokens(configuration);

        var serviceProvider = services.BuildServiceProvider();
        serviceProvider.GetService<IOptionsMonitor<SimpleKeyValueSettings>>().Should().BeNull();
    }

    private sealed class CustomScenario : IClientTokenScenario
    {
        public IReadOnlyCollection<string> KnownKeys { get; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ClientId" };

        public void Validate(string optionKey, ClientTokenSettingsOptions options)
        {
            if (!options.Settings.TryGetValue("ClientId", out var clientId) || string.IsNullOrWhiteSpace(clientId))
            {
                throw new ConfigurationException($"[{optionKey}] ClientId must be filled.");
            }
        }

        public void WriteTo(string optionKey, ClientTokenSettingsOptions options, SimpleKeyValueSettings settings)
        {
            settings.Add(TokenKeys.ProviderIdKey, "Custom");
            settings.Add(TokenKeys.ClientIdKey, options.Settings["ClientId"]);
        }
    }
}
