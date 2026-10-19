using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Arc4u.gRPC.Interceptors;
using Arc4u.OAuth2;
using Arc4u.OAuth2.AspNetCore;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Security.Principal;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProvider;
using Arc4u.OAuth2.TokenProvider.Scenarios;
using Arc4u.OAuth2.TokenProviders;
using Arc4u.Security.Principal;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Serilog;
using Xunit;
using Xunit.Abstractions;
using static Grpc.Core.Interceptors.Interceptor;

namespace Arc4u.UnitTest.Security;

/// <summary>
///     This test will control the different scenario defined for the usage of the GRpcinterceptor.
///     The handler can be used in the following case:
///     1) Retrieve the bearer token when used with a user principal authenticated via an OpenId Connect scenario):
///     AuthenticationType is OpenId.
///     2) Retrieve the bearer token when used with a user principal authenticated in an Api call: AuthenticationType is
///     OAuth2Bearer.
///     3) By using a CLientSecret definition (username:password) and injecting the bearer token retrieve from a call to
///     the authority provider: Authentication type is Inject.
///     4) By injecting an encrypted username:password in the header of the http request : RemoteSecret token provider and
///     AuthenticationType is Inject.
///     5) By injecting a Basic authorization header during the http request: RemoteSecret, header key is Basic and
///     AuthenticationType is Inject.
///     6) Have an on behalf of scenario based on the scenario 1 or 2.
/// </summary>


public class InterceptorTest : OAuth2Interceptor<InterceptorTest>
{
    public InterceptorTest(IServiceProvider containerResolve, ILogger<InterceptorTest> logger,
        IOptionsMonitor<SimpleKeyValueSettings> keyValuesSettingsOption, string settingsName) : base(containerResolve,
        logger, keyValuesSettingsOption.Get(settingsName))
    {
    }
}

[Trait("Category", "CI")]
public class GRpcInterceptorTests
{
    private readonly Fixture _fixture;
    private readonly ITestOutputHelper _output;

    public GRpcInterceptorTests(ITestOutputHelper output)
    {
        _output = output;

        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }


    [Fact]
    // Scenario 1
    public void Jwt_With_Principal_With_OIDC_Token_Should()
    {
        // arrange
        // arrange the configuration to setup the Client secret.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:OpenId.Settings:ClientId"] = "aa17786b-e33c-41ec-81cc-6063610aedeb",
                    ["Authentication:OpenId.Settings:ClientSecret"] = "This is a secret",
                    ["Authentication:OpenId.Settings:Audiences:0"] = "urn://audience.com",
                    ["Authentication:OpenId.Settings:Scopes:0"] = "user.read",
                    ["Authentication:OpenId.Settings:Scopes:1"] = "user.write",
                    ["Authentication:DefaultAuthority:Url"] = "https://login.microsoft.com"
                }).Build();

        // Define an access token that will be used as the return of the call to the CredentialDirect token credential provider.
        var jwt = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")], DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(1));
        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        IConfiguration configuration = new ConfigurationRoot([.. config.Providers]);

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        var serilog = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.TestOutput(_output, formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(serilog));
        services.AddILogger();
        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddDefaultAuthority(configuration);
        services.ConfigureOpenIdSettings(configuration, "Authentication:OpenId.Settings");
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();
        services.AddScoped<TokenRefreshInfo>();

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        var mockTokenRefresh = _fixture.Freeze<Mock<ITokenRefreshProvider>>();
        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedTransient<ITokenProvider, OidcTokenProvider>(OidcTokenProvider.ProviderName);
        services.AddSingleton(mockTokenRefresh.Object);
        services.AddSingleton<IHttpContextAccessor>(mockHttpContextAccessor.Object);

        var serviceProvider = services.BuildServiceProvider();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = serviceProvider.CreateScope();
        var scopedServiceAccessor =
            scopedContainer.ServiceProvider.GetRequiredService<IScopedServiceProviderAccessor>();
        scopedServiceAccessor!.ServiceProvider = scopedContainer.ServiceProvider;

        var tokenRefresh = scopedContainer.ServiceProvider.GetRequiredService<TokenRefreshInfo>();
        tokenRefresh!.RefreshToken =
            new TokenInfo("refresh_token", Guid.NewGuid().ToString(), DateTime.UtcNow.AddHours(1));
        tokenRefresh!.AccessToken = new TokenInfo("access_token", accessToken);

        var principal =
            new AppPrincipal(new Arc4u.Security.Principal.Authorization(),
                new ClaimsIdentity(Constants.CookiesAuthenticationType) { BootstrapContext = accessToken }, "S-1-0-0")
            {
                Profile = UserProfile.Empty
            };

        // Define a Principal with no OAuth2Bearer token here => we test the injection.
        var appContext = scopedContainer.ServiceProvider.GetRequiredService<IApplicationContext>();
        appContext!.SetPrincipal(principal);

        var setingsOptions =
            scopedContainer.ServiceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();

        var mockMethod = _fixture.Freeze<Mock<Method<string, string>>>();

        var mockClientInterceptorContext =
            new ClientInterceptorContext<string, string>(mockMethod.Object, "host", new CallOptions([]));
        var mock = _fixture.Freeze<Mock<BlockingUnaryCallContinuation<string, string>>>();

        // Act
        var sut = new InterceptorTest(scopedContainer.ServiceProvider,
            scopedContainer.ServiceProvider.GetRequiredService<ILogger<InterceptorTest>>()!, setingsOptions!,
            Constants.CookiesAuthenticationType);

        sut.BlockingUnaryCall<string, string>("Test", mockClientInterceptorContext, mock.Object);

        // Assert
        mockClientInterceptorContext.Options.Headers.Should().NotBeNull();
        mockClientInterceptorContext.Options.Headers!.GetValue("authorization").Should().Be($"Bearer {accessToken}");
    }

    [Fact]
    // Scenario 2
    public void Jwt_With_Principal_With_OAuth2_Token_Should()
    {
        // arrange
        // arrange the configuration to setup the Client secret.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:OAuth2.Settings:Audiences:0"] = "urn://audience.com",
                    ["Authentication:OAuth2.Settings:Scopes"] = "user.read user.write",
                    ["Authentication:DefaultAuthority:Url"] = "https://login.microsoft.com"
                }).Build();

        // Define an access token that will be used as the return of the call to the CredentialDirect token credential provider.
        var jwt = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")], DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(1));
        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        var serilog = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.TestOutput(_output, formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(serilog));
        services.AddILogger();

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddDefaultAuthority(configuration);
        services.ConfigureOAuth2Settings(configuration, "Authentication:OAuth2.Settings");
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedTransient<ITokenProvider, BootstrapContextTokenProvider>("Bootstrap");
        services.AddSingleton(mockHttpContextAccessor.Object);

        var serviceProvider = services.BuildServiceProvider();

        var scopedServiceAccessor = serviceProvider.GetRequiredService<IScopedServiceProviderAccessor>();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = serviceProvider.CreateScope();

        scopedServiceAccessor!.ServiceProvider = scopedContainer.ServiceProvider;

        var principal =
            new AppPrincipal(new Arc4u.Security.Principal.Authorization(),
                new ClaimsIdentity(Constants.BearerAuthenticationType) { BootstrapContext = accessToken }, "S-1-0-0")
            {
                Profile = UserProfile.Empty
            };

        // Define a Principal with no OAuth2Bearer token here => we test the injection.
        var appContext = scopedContainer.ServiceProvider.GetRequiredService<IApplicationContext>();
        appContext!.SetPrincipal(principal);

        var setingsOptions =
            scopedContainer.ServiceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();

        var mockMethod = _fixture.Freeze<Mock<Method<string, string>>>();

        var mockClientInterceptorContext =
            new ClientInterceptorContext<string, string>(mockMethod.Object, "host", new CallOptions([]));
        var mock = _fixture.Freeze<Mock<BlockingUnaryCallContinuation<string, string>>>();

        // Act
        var sut = new InterceptorTest(scopedContainer.ServiceProvider,
            scopedContainer.ServiceProvider.GetRequiredService<ILogger<InterceptorTest>>()!, setingsOptions!,
            "OAuth2");

        sut.BlockingUnaryCall<string, string>("Test", mockClientInterceptorContext, mock.Object);

        // Assert
        mockClientInterceptorContext.Options.Headers.Should().NotBeNull();
        mockClientInterceptorContext.Options.Headers!.GetValue("authorization").Should().Be($"Bearer {accessToken}");
    }

    [Fact]
    // Scenario 3
    // When we inject. There is no need to have a principal!
    public void Jwt_With_ClientSecet_Should()
    {
        // arrange
        // arrange the configuration to setup the Client secret.
        var clientId = _fixture.Create<string>();
        var user = _fixture.Create<string>();
        var scopes = _fixture.Create<List<string>>();
        var configDic = new Dictionary<string, string?>
        {
            ["Authentication:ClientTokens:Client1:Scenario"] = UserPasswordScenario.Name,
            ["Authentication:ClientTokens:Client1:Settings:ClientId"] = clientId,
            ["Authentication:ClientTokens:Client1:Settings:User"] = user,
            ["Authentication:ClientTokens:Client1:Settings:Password"] = "password",
            ["Authentication:DefaultAuthority:Url"] = "https://login.microsoft.com"
        };
        foreach (var scope in scopes)
        {
            configDic.Add($"Authentication:ClientTokens:Client1:Scopes:{scopes.IndexOf(scope)}", scope);
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDic).Build();

        // Define an access token that will be used as the return of the call to the CredentialDirect token credential provider.
        var jwt = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")], DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(1));
        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        var serilog = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.TestOutput(_output, formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(serilog));
        services.AddILogger();

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddClientTokens(configuration);
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();
        services.AddDefaultAuthority(configuration);

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        // Mock the CredentialDiect (Calling the authorize endpoint based on a user and password!)
        var mockSecretTokenProvider = _fixture.Freeze<Mock<ICredentialTokenProvider>>();
        mockSecretTokenProvider
            .Setup(m => m.GetTokenAsync(It.IsAny<IKeyValueSettings>(), It.IsAny<CredentialsResult>()))
            .ReturnsAsync(new TokenInfo("Bearer", accessToken));

        // Mock the cache used by the Credential token provider.
        var mockTokenCache = _fixture.Freeze<Mock<ITokenCache>>();
        mockTokenCache.Setup(m => m.Get<TokenInfo?>(It.IsAny<string>())).Returns((TokenInfo?)null);
        mockTokenCache.Setup(m => m.Put(It.IsAny<string>(), It.IsAny<TokenInfo>()));

        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedSingleton<ICredentialTokenProvider>("CredentialDirect", mockSecretTokenProvider.Object);
        services.AddKeyedTransient<ITokenProvider, CredentialTokenCacheTokenProvider>("Credential");
        services.AddSingleton<ITokenCache>(mockTokenCache.Object);
        services.AddSingleton<IHttpContextAccessor>(mockHttpContextAccessor.Object);

        var serviceProvider = services.BuildServiceProvider();

        var scopedServiceAccessor = serviceProvider.GetRequiredService<IScopedServiceProviderAccessor>();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = serviceProvider.CreateScope();

        var principal =
            new AppPrincipal(new Arc4u.Security.Principal.Authorization(),
                new ClaimsIdentity(Constants.CookiesAuthenticationType) { BootstrapContext = accessToken }, "S-1-0-0")
            {
                Profile = UserProfile.Empty
            };

        // Define a Principal with no OAuth2Bearer token here => we test the injection.
        var appContext = scopedContainer.ServiceProvider.GetService<IApplicationContext>();
        appContext!.SetPrincipal(principal);

        scopedServiceAccessor!.ServiceProvider = scopedContainer.ServiceProvider;

        var settingsOptions = scopedContainer.ServiceProvider.GetService<IOptionsMonitor<SimpleKeyValueSettings>>();

        var mockMethod = _fixture.Freeze<Mock<Method<string, string>>>();

        var mockClientInterceptorContext =
            new ClientInterceptorContext<string, string>(mockMethod.Object, "host", new CallOptions([]));
        var mock = _fixture.Freeze<Mock<BlockingUnaryCallContinuation<string, string>>>();

        // Act
        var sut = new InterceptorTest(scopedContainer.ServiceProvider,
            scopedContainer.ServiceProvider.GetService<ILogger<InterceptorTest>>()!, settingsOptions!, "Client1");

        sut.BlockingUnaryCall<string, string>("Test", mockClientInterceptorContext, mock.Object);

        // Assert
        mockClientInterceptorContext.Options.Headers.Should().NotBeNull();
        mockClientInterceptorContext.Options.Headers!.GetValue("authorization").Should().Be($"Bearer {accessToken}");
    }

    [Fact]
    // Scenario 4
    // When we inject. There is no need to have a principal!
    public void Jwt_With_RemoteSecreInjected_With_Basic_Authorization_Should()
    {
        // arrange
        // arrange the configuration to setup the Client secret.
        var options = _fixture.Create<RemoteSecretSettingsOptions>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:RemoteSecrets:Remote1:ClientSecret"] = options.ClientSecret,
                    ["Authentication:RemoteSecrets:Remote1:HeaderKey"] = "Basic"
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        var serilog = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.TestOutput(_output, formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(serilog));
        services.AddILogger();

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddRemoteSecretsAuthentication(configuration);
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();

        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedTransient<ITokenProvider, RemoteClientSecretTokenProvider>(RemoteClientSecretTokenProvider
            .ProviderName);
        services.AddSingleton(mockHttpContextAccessor.Object);

        var serviceProvider = services.BuildServiceProvider();

        var scopedServiceAccessor = serviceProvider.GetRequiredService<IScopedServiceProviderAccessor>();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = serviceProvider.CreateScope();

        var principal =
            new AppPrincipal(new Arc4u.Security.Principal.Authorization(),
                new ClaimsIdentity(Constants.BearerAuthenticationType) { BootstrapContext = string.Empty }, "S-1-0-0")
            {
                Profile = UserProfile.Empty
            };

        // Define a Principal with no OAuth2Bearer token here => we test the injection.
        var appContext = scopedContainer.ServiceProvider.GetRequiredService<IApplicationContext>();
        appContext!.SetPrincipal(principal);

        scopedServiceAccessor!.ServiceProvider = scopedContainer.ServiceProvider;

        var setingsOptions =
            scopedContainer.ServiceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();

        var mockMethod = _fixture.Freeze<Mock<Method<string, string>>>();

        var mockClientInterceptorContext =
            new ClientInterceptorContext<string, string>(mockMethod.Object, "host", new CallOptions([]));
        var mock = _fixture.Freeze<Mock<BlockingUnaryCallContinuation<string, string>>>();

        // Act
        var sut = new InterceptorTest(scopedContainer.ServiceProvider,
            scopedContainer.ServiceProvider.GetRequiredService<ILogger<InterceptorTest>>()!, setingsOptions!,
            "Remote1");

        sut.BlockingUnaryCall<string, string>("Test", mockClientInterceptorContext, mock.Object);

        // Assert
        mockClientInterceptorContext.Options.Headers.Should().NotBeNull();
        mockClientInterceptorContext.Options.Headers!.GetValue("authorization").Should()
            .Be($"Basic {Convert.ToBase64String(Encoding.UTF8.GetBytes(options.ClientSecret))}");
    }

    [Fact]
    // Scenario 5
    // When we inject. There is no need to have a principal!
    public void Jwt_With_RemoteSecreInjected_Should()
    {
        // arrange
        // arrange the configuration to setup the Client secret.
        var options = _fixture.Create<RemoteSecretSettingsOptions>();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:RemoteSecrets:Remote1:ClientSecret"] = options.ClientSecret,
                    ["Authentication:RemoteSecrets:Remote1:HeaderKey"] = options.HeaderKey
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        var serilog = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.TestOutput(_output, formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(serilog));
        services.AddILogger();

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddRemoteSecretsAuthentication(configuration);
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();

        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedTransient<ITokenProvider, RemoteClientSecretTokenProvider>(RemoteClientSecretTokenProvider
            .ProviderName);
        services.AddSingleton(mockHttpContextAccessor.Object);

        var serviceProvider = services.BuildServiceProvider();

        var scopedServiceAccessor = serviceProvider.GetService<IScopedServiceProviderAccessor>();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = serviceProvider.CreateScope();

        var principal =
            new AppPrincipal(new Arc4u.Security.Principal.Authorization(),
                new ClaimsIdentity(Constants.BearerAuthenticationType) { BootstrapContext = string.Empty }, "S-1-0-0")
            {
                Profile = UserProfile.Empty
            };

        // Define a Principal with no OAuth2Bearer token here => we test the injection.
        var appContext = scopedContainer.ServiceProvider.GetRequiredService<IApplicationContext>();
        appContext!.SetPrincipal(principal);

        scopedServiceAccessor!.ServiceProvider = scopedContainer.ServiceProvider;

        var setingsOptions =
            scopedContainer.ServiceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();

        var mockMethod = _fixture.Freeze<Mock<Method<string, string>>>();

        var mockClientInterceptorContext =
            new ClientInterceptorContext<string, string>(mockMethod.Object, "host", new CallOptions([]));
        var mock = _fixture.Freeze<Mock<BlockingUnaryCallContinuation<string, string>>>();

        // Act
        var sut = new InterceptorTest(scopedContainer.ServiceProvider,
            scopedContainer.ServiceProvider.GetRequiredService<ILogger<InterceptorTest>>()!, setingsOptions!,
            "Remote1");

        sut.BlockingUnaryCall<string, string>("Test", mockClientInterceptorContext, mock.Object);

        // Assert
        mockClientInterceptorContext.Options.Headers.Should().NotBeNull();
        mockClientInterceptorContext.Options.Headers!.GetValue(options.HeaderKey).Should().Be(options.ClientSecret);
    }

    [Fact]
    // Scenario 6
    public void Jwt_With_OAuth2_And_Principal_With_Bearer_Token_And_On_Behalf_of_Should()
    {
        // arrange
        // arrange the configuration to setup the Client secret.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:OnBehalfOf:Obo:ClientId"] = "aa17786b-e33c-41ec-81cc-6063610aedeb",
                    ["Authentication:OnBehalfOf:Obo:ClientSecret"] = "This is a secret",
                    ["Authentication:OnBehalfOf:Obo:Scopes:0"] = "user.read",
                    ["Authentication:OnBehalfOf:Obo:Scopes:1"] = "user.write",
                    ["Authentication:DefaultAuthority:Url"] = "https://login.microsoft.com"
                }).Build();

        // Define an access token that will be used as the return of the call to the CredentialDirect token credential provider.
        var jwt = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")], DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(1));
        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        var serilog = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.TestOutput(_output, formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(serilog));
        services.AddILogger();

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddDefaultAuthority(configuration);
        services.AddOnBehalfOf(configuration);
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();
        services.AddScoped<TokenRefreshInfo>();

        var mockActivitySourceFactory = new Mock<IActivitySourceFactory>();
        mockActivitySourceFactory.Setup(m => m.Get("Arc4u", null)).Returns<string?>(default!);
        services.AddSingleton(mockActivitySourceFactory.Object);

        // Used the cache to return the access token in the Obo provider => avoid any call to the Authority!
        var mockCache = _fixture.Freeze<Mock<ICache>>();
        mockCache.Setup(m => m.GetAsync<TokenInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenInfo("Bearer", accessToken));

        var mockCacheHelper = _fixture.Freeze<Mock<ICacheHelper>>();
        mockCacheHelper.Setup(m => m.GetCache()).Returns(mockCache.Object);

        services.AddSingleton(mockCacheHelper.Object);

        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedTransient<ITokenProvider, AzureADOboTokenProvider>(AzureADOboTokenProvider.ProviderName);
        services.AddSingleton<IHttpContextAccessor>(mockHttpContextAccessor.Object);

        var serviceProvider = services.BuildServiceProvider();

        var scopedServiceAccessor = serviceProvider.GetRequiredService<IScopedServiceProviderAccessor>();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = serviceProvider.CreateScope();

        scopedServiceAccessor!.ServiceProvider = scopedContainer.ServiceProvider;

        var principal = new AppPrincipal(new Arc4u.Security.Principal.Authorization(),
            new ClaimsIdentity(Constants.BearerAuthenticationType) { BootstrapContext = accessToken }, "S-1-0-0");
        principal.Profile = UserProfile.Empty;
        // Define a Principal with no OAuth2Bearer token here => we test the injection.
        var appContext = scopedContainer.ServiceProvider.GetService<IApplicationContext>();
        appContext!.SetPrincipal(principal);

        var setingsOptions = scopedContainer.ServiceProvider.GetService<IOptionsMonitor<SimpleKeyValueSettings>>();

        var mockMethod = _fixture.Freeze<Mock<Method<string, string>>>();

        var mockClientInterceptorContext =
            new ClientInterceptorContext<string, string>(mockMethod.Object, "host", new CallOptions([]));
        var mock = _fixture.Freeze<Mock<BlockingUnaryCallContinuation<string, string>>>();

        // Act
        var sut = new InterceptorTest(scopedContainer.ServiceProvider,
            scopedContainer.ServiceProvider.GetService<ILogger<InterceptorTest>>()!, setingsOptions!, "Obo");

        sut.BlockingUnaryCall<string, string>("Test", mockClientInterceptorContext, mock.Object);

        // Assert
        mockClientInterceptorContext.Options.Headers.Should().NotBeNull();
        mockClientInterceptorContext.Options.Headers!.GetValue("authorization").Should().Be($"Bearer {accessToken}");
    }

    [Fact]
    // Scenario 7
    public void Jwt_With_Principal_With_OAuth2_Token_For_Client_Scenario_Should()
    {
        // arrange
        // arrange the configuration to setup the Client secret.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:OAuth2.Settings:Audiences:0"] = "urn://audience.com",
                    ["Authentication:OAuth2.Settings:Scopes"] = "user.read user.write",
                    ["Authentication:DefaultAuthority:Url"] = "https://login.microsoft.com"
                }).Build();

        // Define an access token that will be used as the return of the call to the CredentialDirect token credential provider.
        var jwt = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")], DateTime.UtcNow.AddHours(-1),
            DateTime.UtcNow.AddHours(1));
        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwt);

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        var serilog = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.TestOutput(_output, formatProvider: CultureInfo.InvariantCulture)
            .CreateLogger();

        services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(serilog));
        services.AddILogger();

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddDefaultAuthority(configuration);
        services.ConfigureOAuth2Settings(configuration, "Authentication:OAuth2.Settings");
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();

        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedTransient<ITokenProvider, BootstrapContextTokenProvider>(BootstrapContextTokenProvider.ProviderName);

        var serviceProvider = services.BuildServiceProvider();

        var principal =
            new AppPrincipal(new Arc4u.Security.Principal.Authorization(),
                new ClaimsIdentity(Constants.BearerAuthenticationType) { BootstrapContext = accessToken }, "S-1-0-0")
            {
                Profile = UserProfile.Empty
            };

        // Define a Principal with no OAuth2Bearer token here => we test the injection.
        var appContext = serviceProvider.GetRequiredService<IApplicationContext>();
        appContext!.SetPrincipal(principal);

        var setingsOptions = serviceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();

        var mockMethod = _fixture.Freeze<Mock<Method<string, string>>>();

        var mockClientInterceptorContext =
            new ClientInterceptorContext<string, string>(mockMethod.Object, "host", new CallOptions([]));
        var mock = _fixture.Freeze<Mock<BlockingUnaryCallContinuation<string, string>>>();

        // Act
        var sut = new InterceptorTest(serviceProvider,
            serviceProvider.GetRequiredService<ILogger<InterceptorTest>>()!, setingsOptions!, Constants.BearerAuthenticationType);

        sut.BlockingUnaryCall<string, string>("Test", mockClientInterceptorContext, mock.Object);

        // Assert
        mockClientInterceptorContext.Options.Headers.Should().NotBeNull();
        mockClientInterceptorContext.Options.Headers!.GetValue("authorization").Should().Be($"Bearer {accessToken}");
    }
}
