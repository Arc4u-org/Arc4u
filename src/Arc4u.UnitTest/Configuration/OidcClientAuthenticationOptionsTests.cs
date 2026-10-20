using Arc4u.Configuration;
using Arc4u.IdentityModel.Claims;
using Arc4u.OAuth2.Client.Authentication.Options;
using Arc4u.OAuth2.Client.Authentication.TokenProvider;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using AutoFixture;
using AutoFixture.AutoMoq;
using Duende.IdentityModel.Client;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Configuration;

[Trait("Category", "CI")]
public class OidcClientAuthenticationOptionsTests
{
    public OidcClientAuthenticationOptionsTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    private readonly Fixture _fixture;

    [Fact]
    public void OidcClientAuthenticationOptions_From_Config_Should()
    {
        var oidcOptions = _fixture.Create<OidcClientSettingsOption>();
        var authority = _fixture.Build<AuthorityOptions>().With(p => p.Url, new Uri("http://localhost:3000/")).Create();
        oidcOptions.Scopes = _fixture.CreateMany<string>(4).ToList();
        var scopes = oidcOptions.Scopes.ToArray();
        var services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:DefaultAuthority:Url"] = authority.Url.ToString(),
                    ["Authentication:CallbackPath"] = "/signin-oidc",
                    ["Authentication:LoadProfile"] = true.ToString(),
                    ["Authentication:ClaimsIdentifierOptions:0"] = "sub",
                    ["Authentication:ForceRefreshTimeoutTimeSpan"] = "00:00:00",
                    ["Authentication:RefreshTokenLifetime"] = TimeSpan.FromDays(21).ToString(),
                    ["Authentication:OidcClient.Settings:ClientId"] = oidcOptions.ClientId,
                    ["Authentication:OidcClient.Settings:Scopes:0"] = scopes[0],
                    ["Authentication:OidcClient.Settings:Scopes:1"] = scopes[1],
                    ["Authentication:OidcClient.Settings:Scopes:2"] = scopes[2],
                    ["Authentication:OidcClient.Settings:Scopes:3"] = scopes[3],
                    ["Authentication:OidcClient.Settings:ProviderId"] = oidcOptions.ProviderId,
                    ["Authentication:ApiExtraContextAuthenticationSection:AuthorizationEndpointSectionPath"] = "Authentication:OidcClient.Settings2:AuthorizationEndpoint",
                    ["Authentication:ApiExtraContextAuthenticationSection:TokenEndpointSectionPath"] = "Authentication:OidcClient.Settings2:TokenEndpoint",
                    ["Authentication:OidcClient.Settings2:AuthorizationEndpoint:audience"] = "http://arc4u/aud",
                    ["Authentication:OidcClient.Settings2:AuthorizationEndpoint:resource"] = "http://arc4u/res",
                    ["Authentication:OidcClient.Settings2:TokenEndpoint:audience"] = "http://guidance/aud",
                    ["Authentication:OidcClient.Settings2:TokenEndpoint:resource"] = "http://guidance/res",
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        var mockBrowser = new Mock<IBrowser>();

        services.AddOidcClientAuthentication(mockBrowser.Object, NullLoggerFactory.Instance, configuration);

        var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<OidcClientOptions>();

        Assert.NotNull(options);
        Assert.Equal(authority.Url.ToString(), options.Authority);
        Assert.True(options.LoadProfile);
        Assert.Equal("/signin-oidc", options.RedirectUri);
        Assert.Equal("/", options.PostLogoutRedirectUri);
        Assert.Equal(oidcOptions.ClientId, options.ClientId);
        Assert.Equal(string.Join(' ', scopes), options.Scope);
        Assert.IsType<DiscoveryPolicy>(options.Policy.Discovery);
        Assert.False(options.Policy.Discovery.RequireHttps);

        var oidcAuthenticationOptions = serviceProvider.GetService<IOptionsMonitor<OidcClientAuthenticationOptions>>();
        Assert.NotNull(oidcAuthenticationOptions?.CurrentValue);
        Assert.Equal(TimeSpan.FromDays(21), oidcAuthenticationOptions?.CurrentValue?.RefreshTokenLifetime);
        Assert.Equal(TimeSpan.Zero, oidcAuthenticationOptions?.CurrentValue?.ForceRefreshTimeoutTimeSpan);

        var option = serviceProvider.GetService<IOptionsMonitor<ApiExtraContextAuthenticationOption>>();

        Assert.NotNull(option);
        var parameters = option.CurrentValue;

        Assert.NotNull(parameters);
        Assert.NotNull(parameters.AuthorizationParameters);
        Assert.NotEmpty(parameters.AuthorizationParameters.Values);
        Assert.Equal(2, parameters.AuthorizationParameters.Values.Count);
        Assert.Equal("http://arc4u/aud", parameters.AuthorizationParameters.Values["audience"]);
        Assert.Equal("http://arc4u/res", parameters.AuthorizationParameters.Values["resource"]);
        Assert.NotNull(parameters.TokenParameters);
        Assert.NotEmpty(parameters.TokenParameters.Values);
        Assert.Equal(2, parameters.TokenParameters.Values.Count);
        Assert.Equal("http://guidance/aud", parameters.TokenParameters.Values["audience"]);
        Assert.Equal("http://guidance/res", parameters.TokenParameters.Values["resource"]);

        var settingsMonitor = serviceProvider.GetService<IOptionsMonitor<SimpleKeyValueSettings>>();
        Assert.NotNull(settingsMonitor);
        var settings = settingsMonitor.Get("OidcClient");
        Assert.NotNull(settings);
        Assert.Equal(oidcOptions.ClientId, settings.Values[TokenKeys.ClientIdKey]);
        Assert.Equal(string.Join(' ', oidcOptions.Scopes), settings.Values[TokenKeys.Scope]);
        Assert.Equal(oidcOptions.ProviderId, settings.Values[TokenKeys.ProviderIdKey]);

    }

    [Fact]
    public void AddOidcClientAuthentication_Should_Register_The_Services_Once()
    {
        var oidcOptions = _fixture.Create<OidcClientSettingsOption>();
        var authority = _fixture.Build<AuthorityOptions>().With(p => p.Url, new Uri("http://localhost:3000/")).Create();
        var services = new ServiceCollection();

        services.AddOidcClientAuthentication(new Mock<IBrowser>().Object, NullLoggerFactory.Instance, config =>
        {
            config.DefaultAuthority = authority;
            config.ClaimsIdentifierOptions = identifier => identifier.Add("sub");
            config.OidcClientSettingsOption = oidc =>
            {
                oidc.ClientId = oidcOptions.ClientId;
                oidc.Scopes = oidcOptions.Scopes;
                oidc.ProviderId = oidcOptions.ProviderId;
            };
        });

        Assert.Single(services, d => d.ServiceType == typeof(TokenRefreshInfo));
        Assert.Single(services, d => d.ServiceType == typeof(SignOutTokenProviderKeys));
        Assert.Equal(["sub"], services.BuildServiceProvider().GetRequiredService<IOptions<ClaimsIdentifierOption>>().Value);
    }

    [Fact]
    public void AddOidcClientAuthentication_Without_ClaimsIdentifierOptions_Should_Use_The_Object_Identifier_Claim_Types()
    {
        var oidcOptions = _fixture.Create<OidcClientSettingsOption>();
        var authority = _fixture.Build<AuthorityOptions>().With(p => p.Url, new Uri("http://localhost:3000/")).Create();
        var services = new ServiceCollection();

        services.AddOidcClientAuthentication(new Mock<IBrowser>().Object, NullLoggerFactory.Instance, config =>
        {
            config.DefaultAuthority = authority;
            config.OidcClientSettingsOption = oidc =>
            {
                oidc.ClientId = oidcOptions.ClientId;
                oidc.Scopes = oidcOptions.Scopes;
                oidc.ProviderId = oidcOptions.ProviderId;
            };
        });

        Assert.Equal([ClaimTypes.ObjectIdentifier, ClaimTypes.OID], services.BuildServiceProvider().GetRequiredService<IOptions<ClaimsIdentifierOption>>().Value);
    }

    [Fact]
    public void OidcClientAuthenticationOptions_From_No_Config_Should()
    {
        var oidcOptions = _fixture.Create<OidcClientSettingsOption>();
        var authority = _fixture.Build<AuthorityOptions>().With(p => p.Url, new Uri("http://localhost:3000/")).Create();
        var services = new ServiceCollection();

        var mockBrowser = new Mock<IBrowser>();

        services.AddOidcClientAuthentication(mockBrowser.Object, NullLoggerFactory.Instance, config =>
        {
            config.DefaultAuthority = authority;
            config.CallbackPath = "/signin-oidc";
            config.PostLogoutRedirectUri = "/";
            config.LoadProfile = true;
            config.ClaimsIdentifierOptions = identifier => identifier.Add("sub");
            config.ForceRefreshTimeoutTimeSpan = TimeSpan.Zero;
            config.RefreshTokenLifetime = TimeSpan.FromDays(21);
            config.OidcClientSettingsOption = oidc =>
            {
                oidc.ClientId = oidcOptions.ClientId;
                oidc.Scopes = oidcOptions.Scopes;
                oidc.ProviderId = oidcOptions.ProviderId;
            };
        });
        services.AddAuthenticationApiContext(config =>
        {
            config.AuthorizationParameters = new SimpleKeyValueSettings(("audience", "http://arc4u/aud"),
                ("resource", "http://arc4u/res"));
            config.TokenParameters = new SimpleKeyValueSettings(("audience", "http://guidance/aud"),
                ("resource", "http://guidance/res"));
        });
        var serviceProvider = services.BuildServiceProvider();

        var options = serviceProvider.GetRequiredService<OidcClientOptions>();

        Assert.NotNull(options);
        Assert.Equal(authority.Url.ToString(), options.Authority);
        Assert.True(options.LoadProfile);
        Assert.Equal("/signin-oidc", options.RedirectUri);
        Assert.Equal("/", options.PostLogoutRedirectUri);
        Assert.Equal(oidcOptions.ClientId, options.ClientId);
        Assert.Equal(string.Join(' ', oidcOptions.Scopes), options.Scope);
        Assert.IsType<DiscoveryPolicy>(options.Policy.Discovery);
        Assert.False(options.Policy.Discovery.RequireHttps);

        var oidcAuthenticationOptions = serviceProvider.GetService<IOptionsMonitor<OidcClientAuthenticationOptions>>();
        Assert.NotNull(oidcAuthenticationOptions?.CurrentValue);
        Assert.Equal(TimeSpan.FromDays(21), oidcAuthenticationOptions?.CurrentValue?.RefreshTokenLifetime);
        Assert.Equal(TimeSpan.Zero, oidcAuthenticationOptions?.CurrentValue?.ForceRefreshTimeoutTimeSpan);

        var option = serviceProvider.GetService<IOptionsMonitor<ApiExtraContextAuthenticationOption>>();

        Assert.NotNull(option);
        var parameters = option.CurrentValue;

        Assert.NotNull(parameters);
        Assert.NotNull(parameters.AuthorizationParameters);
        Assert.NotEmpty(parameters.AuthorizationParameters.Values);
        Assert.Equal(2, parameters.AuthorizationParameters.Values.Count);
        Assert.Equal("http://arc4u/aud", parameters.AuthorizationParameters.Values["audience"]);
        Assert.Equal("http://arc4u/res", parameters.AuthorizationParameters.Values["resource"]);
        Assert.NotNull(parameters.TokenParameters);
        Assert.NotEmpty(parameters.TokenParameters.Values);
        Assert.Equal(2, parameters.TokenParameters.Values.Count);
        Assert.Equal("http://guidance/aud", parameters.TokenParameters.Values["audience"]);
        Assert.Equal("http://guidance/res", parameters.TokenParameters.Values["resource"]);

        var settingsMonitor = serviceProvider.GetService<IOptionsMonitor<SimpleKeyValueSettings>>();
        Assert.NotNull(settingsMonitor);
        var settings = settingsMonitor.Get("OidcClient");
        Assert.NotNull(settings);
        Assert.Equal(oidcOptions.ClientId, settings.Values[TokenKeys.ClientIdKey]);
        Assert.Equal(string.Join(' ', oidcOptions.Scopes), settings.Values[TokenKeys.Scope]);
        Assert.Equal(oidcOptions.ProviderId, settings.Values[TokenKeys.ProviderIdKey]);

    }
}
