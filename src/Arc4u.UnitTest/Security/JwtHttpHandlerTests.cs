using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.Diagnostics;
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
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;
using Authorization = Arc4u.Security.Principal.Authorization;

namespace Arc4u.UnitTest.Security;

public class JwtHandlerToTest(
    IServiceProvider serviceProvider,
    ILogger<JwtHandlerToTest> logger,
    IOptionsMonitor<SimpleKeyValueSettings> keyValuesSettingsOption,
    string resolvingName) : JwtHttpHandler<JwtHandlerToTest>(serviceProvider, logger, keyValuesSettingsOption.Get(resolvingName))
{
}

/// <summary>
///     This test will control the different scenario defined for the usage of the JWtHttpHandler.
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
///     7) Chain 2 Handlers: OAuth2Bearer + Cookies and the access token returned is the OAuth2 one
///     8) Chain 2 Handlers: OAuth2Bearer + Cookies and the access token returned is the Cookies one
///     9) Chain 3 Handlers: Oauth2Bearer + Cookies + Inject but inject is not occuring because a OAuth2Bearer is already
///     available.
/// </summary>
[Trait("Category", "CI")]
public class JwtHttpHandlerTests
{
    private readonly Fixture _fixture;

    public JwtHttpHandlerTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    // Scenario 1
    public async Task Jwt_With_OAuth2_And_Principal_With_OIDC_Token_Should()
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

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddDefaultAuthority(configuration);
        services.ConfigureOpenIdSettings(configuration, "Authentication:OpenId.Settings");
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();
        services.AddScoped<TokenRefreshInfo>();

        var mockILoggerFactory = new Mock<ILoggerFactory>();
        mockILoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        services.AddSingleton(mockILoggerFactory.Object);
        services.AddTransient(typeof(ILogger<>), typeof(LoggerWrapper<>));
        services.AddKeyedTransient<IAddPropertiesToLog, NullLoggerProperties>("Transient");

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        var mockTokenRefresh = _fixture.Freeze<Mock<ITokenRefreshProvider>>();
        // Register the different TokenProvider and CredentialTokenProviders.

        services.AddKeyedTransient<ITokenProvider, OidcTokenProvider>(OidcTokenProvider.ProviderName);
        services.AddSingleton(mockHttpContextAccessor.Object);
        services.AddSingleton<ITokenRefreshProvider>(mockTokenRefresh!.Object);
        var container = services.BuildServiceProvider();

        var scopedServiceAccessor = container.GetRequiredService<IScopedServiceProviderAccessor>();


        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = container.CreateScope();

        scopedServiceAccessor!.ServiceProvider = scopedContainer.ServiceProvider;

        var tokenRefresh = scopedContainer.ServiceProvider.GetRequiredService<TokenRefreshInfo>();
        tokenRefresh.RefreshToken =
            new TokenInfo("refresh_token", Guid.NewGuid().ToString(), DateTime.UtcNow.AddHours(1));
        tokenRefresh.AccessToken = new TokenInfo("access_token", accessToken);

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

        // Define the end handler that will simulate the call to the endpoint.
        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Verifiable();

        // Act
        var sut = new JwtHandlerToTest(scopedContainer.ServiceProvider,
            scopedContainer.ServiceProvider.GetRequiredService<ILogger<JwtHandlerToTest>>()!, setingsOptions!,
            Constants.CookiesAuthenticationType) { InnerHandler = innerHandler.Object };

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");
        var invoker = new HttpMessageInvoker(sut);
        var response = await invoker.SendAsync(httpRequestMessage, new CancellationToken());

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        httpRequestMessage.Headers.Authorization.Should().NotBeNull();
        // The request must have a Bearer token injected in the authohirzation header.
        httpRequestMessage.Headers.Authorization!.Scheme.Should().Be("Bearer");
        httpRequestMessage.Headers.Authorization!.Parameter.Should().Be(accessToken);
    }

    [Fact]
    // Scenario 2
    public async Task Jwt_With_OAuth2_And_Principal_With_Bearer_Token_Should()
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

        IConfiguration configuration = new ConfigurationRoot([.. config.Providers]);

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddDefaultAuthority(configuration);
        services.ConfigureOAuth2Settings(configuration, "Authentication:OAuth2.Settings");
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();

        var mockILoggerFactory = new Mock<ILoggerFactory>();
        mockILoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        services.AddSingleton(mockILoggerFactory.Object);
        services.AddTransient(typeof(ILogger<>), typeof(LoggerWrapper<>));
        services.AddKeyedTransient<IAddPropertiesToLog, NullLoggerProperties>("Transient");

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedTransient<ITokenProvider, BootstrapContextTokenProvider>("Bootstrap");
        services.AddSingleton(mockHttpContextAccessor.Object);

        var container = services.BuildServiceProvider();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = container.CreateScope();
        var scopedServiceAccessor = container.GetRequiredService<IScopedServiceProviderAccessor>();
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

        // Define the end handler that will simulate the call to the endpoint.
        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Verifiable();

        // Act
        var sut = new JwtHandlerToTest(scopedContainer.ServiceProvider,
            scopedContainer.ServiceProvider.GetRequiredService<ILogger<JwtHandlerToTest>>()!, setingsOptions!, "OAuth2")
        {
            InnerHandler = innerHandler.Object
        };

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");
        var invoker = new HttpMessageInvoker(sut);
        var response = await invoker.SendAsync(httpRequestMessage, new CancellationToken());

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        httpRequestMessage.Headers.Authorization.Should().NotBeNull();
        // The request must have a Bearer token injected in the authohirzation header.
        httpRequestMessage.Headers.Authorization!.Scheme.Should().Be("Bearer");
        httpRequestMessage.Headers.Authorization!.Parameter.Should().Be(accessToken);
    }

    [Fact]
    // Scenario 3
    // When we inject. There is no need to have a principal!
    public async Task Jwt_With_ClientSecet_Should()
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

        IConfiguration configuration = new ConfigurationRoot([.. config.Providers]);

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddClientTokens(configuration);
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();
        services.AddDefaultAuthority(configuration);

        var mockILoggerFactory = new Mock<ILoggerFactory>();
        mockILoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        services.AddSingleton(mockILoggerFactory.Object);
        services.AddTransient(typeof(ILogger<>), typeof(LoggerWrapper<>));
        services.AddKeyedTransient<IAddPropertiesToLog, NullLoggerProperties>("Transient");

        // Mock the CredentialDiect (Calling the authorize endpoint based on a user and password!)
        var mockSecretTokenProvider = _fixture.Freeze<Mock<ICredentialTokenProvider>>();
        mockSecretTokenProvider
            .Setup(m => m.GetTokenAsync(It.IsAny<IKeyValueSettings>(), It.IsAny<CredentialsResult>()))
            .ReturnsAsync(new TokenInfo("Bearer", accessToken));

        // Mock the cache used by the Credential token provider.
        var mockTokenCache = _fixture.Freeze<Mock<ITokenCache>>();
        mockTokenCache.Setup(m => m.Get<TokenInfo>(It.IsAny<string>())).Returns(() => default);
        mockTokenCache.Setup(m => m.Put(It.IsAny<string>(), It.IsAny<TokenInfo>()));

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        // Register the different TokenProvider and CredentialTokenProviders.

        services.AddKeyedSingleton("CredentialDirect", mockSecretTokenProvider.Object);
        services.AddKeyedTransient<ITokenProvider, CredentialTokenCacheTokenProvider>("Credential");
        services.AddSingleton<IHttpContextAccessor>(mockHttpContextAccessor.Object);
        services.AddSingleton<ITokenCache>(mockTokenCache.Object);

        var container = services.BuildServiceProvider();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = container.CreateScope();
        var scopedServiceAccessor = container.GetRequiredService<IScopedServiceProviderAccessor>();
        scopedServiceAccessor!.ServiceProvider = scopedContainer.ServiceProvider;

        var setingsOptions =
            scopedContainer.ServiceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();

        // Define the end handler that will simulate the call to the endpoint.
        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Verifiable();

        // Act
        var sut = new JwtHandlerToTest(scopedContainer.ServiceProvider,
            scopedContainer.ServiceProvider.GetRequiredService<ILogger<JwtHandlerToTest>>(), setingsOptions!, "Client1")
        {
            InnerHandler = innerHandler.Object
        };

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");
        var invoker = new HttpMessageInvoker(sut);
        var response = await invoker.SendAsync(httpRequestMessage, new CancellationToken());

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        httpRequestMessage.Headers.Authorization.Should().NotBeNull();
        // The request must have a Bearer token injected in the authohirzation header.
        httpRequestMessage.Headers.Authorization!.Scheme.Should().Be("Bearer");
        httpRequestMessage.Headers.Authorization!.Parameter.Should().Be(accessToken);
    }

    [Fact]
    // Scenario 4
    // When we inject. There is no need to have a principal!
    public async Task Jwt_With_RemoteSecreInjected_With_Basic_Authorization_Should()
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

        IConfiguration configuration = new ConfigurationRoot([.. config.Providers]);

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddRemoteSecretsAuthentication(configuration);
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();

        var mockILoggerFactory = new Mock<ILoggerFactory>();
        mockILoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        services.AddSingleton(mockILoggerFactory.Object);
        services.AddTransient(typeof(ILogger<>), typeof(LoggerWrapper<>));
        services.AddKeyedTransient<IAddPropertiesToLog, NullLoggerProperties>("Transient");

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedTransient<ITokenProvider, RemoteClientSecretTokenProvider>(RemoteClientSecretTokenProvider
            .ProviderName);
        services.AddSingleton(mockHttpContextAccessor.Object);

        var container = services.BuildServiceProvider();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = container.CreateScope();
        var scopedServiceAccessor = container.GetRequiredService<IScopedServiceProviderAccessor>();
        scopedServiceAccessor!.ServiceProvider = scopedContainer.ServiceProvider;

        var setingsOptions =
            scopedContainer.ServiceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();

        // Define the end handler that will simulate the call to the endpoint.
        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Verifiable();

        // Act
        var sut = new JwtHandlerToTest(scopedContainer.ServiceProvider,
            scopedContainer.ServiceProvider.GetRequiredService<ILogger<JwtHandlerToTest>>()!, setingsOptions!, "Remote1")
        {
            InnerHandler = innerHandler.Object
        };

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");
        var invoker = new HttpMessageInvoker(sut);
        var response = await invoker.SendAsync(httpRequestMessage, new CancellationToken());

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        httpRequestMessage.Headers.Authorization.Should().NotBeNull();
        // The request must have a Bearer token injected in the authohirzation header.
        httpRequestMessage.Headers.Authorization!.Scheme.Should().Be("Basic");
        httpRequestMessage.Headers.Authorization!.Parameter.Should().Be(Convert.ToBase64String(Encoding.UTF8.GetBytes(options.ClientSecret)));
    }

    [Fact]
    // Scenario 5
    // When we inject. There is no need to have a principal!
    public async Task Jwt_With_RemoteSecreInjected_Should()
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

        IConfiguration configuration = new ConfigurationRoot([.. config.Providers]);

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddRemoteSecretsAuthentication(configuration);
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();

        var mockILoggerFactory = new Mock<ILoggerFactory>();
        mockILoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        services.AddSingleton(mockILoggerFactory.Object);
        services.AddTransient(typeof(ILogger<>), typeof(LoggerWrapper<>));
        services.AddKeyedTransient<IAddPropertiesToLog, NullLoggerProperties>("Transient");

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedTransient<ITokenProvider, RemoteClientSecretTokenProvider>(RemoteClientSecretTokenProvider
            .ProviderName);
        services.AddSingleton(mockHttpContextAccessor.Object);

        var container = services.BuildServiceProvider();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = container.CreateScope();
        var scopedServiceAccessor = container.GetRequiredService<IScopedServiceProviderAccessor>();
        scopedServiceAccessor!.ServiceProvider = scopedContainer.ServiceProvider;

        var setingsOptions =
            scopedContainer.ServiceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();

        // Define the end handler that will simulate the call to the endpoint.
        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Verifiable();

        // Act
        var sut = new JwtHandlerToTest(scopedContainer.ServiceProvider,
            scopedContainer.ServiceProvider.GetRequiredService<ILogger<JwtHandlerToTest>>(), setingsOptions!, "Remote1")
        {
            InnerHandler = innerHandler.Object
        };

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");
        var invoker = new HttpMessageInvoker(sut);
        var response = await invoker.SendAsync(httpRequestMessage, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        httpRequestMessage.Headers.Authorization.Should().BeNull();
        httpRequestMessage.Headers.Contains(options.HeaderKey).Should().BeTrue();
        httpRequestMessage.Headers.GetValues(options.HeaderKey).FirstOrDefault().Should().Be(options.ClientSecret);
    }

    [Fact]
    // Scenario 6
    public async Task Jwt_With_OAuth2_And_Principal_With_Bearer_Token_And_On_Behalf_of_Should()
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

        IConfiguration configuration = new ConfigurationRoot([.. config.Providers]);

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddDefaultAuthority(configuration);
        services.AddOnBehalfOf(configuration);
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();
        services.AddScoped<TokenRefreshInfo>();

        var mockILoggerFactory = new Mock<ILoggerFactory>();
        mockILoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        services.AddSingleton(mockILoggerFactory.Object);
        services.AddTransient(typeof(ILogger<>), typeof(LoggerWrapper<>));
        services.AddKeyedTransient<IAddPropertiesToLog, NullLoggerProperties>("Transient");

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        var mockActivitySourceFactory = new Mock<IActivitySourceFactory>();
        mockActivitySourceFactory.Setup(m => m.Get("Arc4u", null)).Returns<ActivitySource?>(default!);
        services.AddSingleton(mockActivitySourceFactory.Object);

        // Uses the cache to return the access token in the Obo provider => avoid any call to the Authority!
        // The cached entry has the type access_token, as written by previous versions: it must still be sent as a Bearer (issue #229).
        var mockCache = _fixture.Freeze<Mock<ICache>>();
        mockCache.Setup(m => m.GetAsync<TokenInfo>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenInfo("access_token", accessToken));

        var mockCacheHelper = _fixture.Freeze<Mock<ICacheHelper>>();
        mockCacheHelper.Setup(m => m.GetCache()).Returns(mockCache.Object);

        services.AddSingleton(mockCacheHelper.Object);

        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedTransient<ITokenProvider, AzureADOboTokenProvider>(AzureADOboTokenProvider.ProviderName);
        services.AddSingleton<IHttpContextAccessor>(mockHttpContextAccessor.Object);

        var container = services.BuildServiceProvider();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = container.CreateScope();
        var scopedServiceAccessor = container.GetRequiredService<IScopedServiceProviderAccessor>();
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

        // Define the end handler that will simulate the call to the endpoint.
        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Verifiable();

        // Act
        var sut = new JwtHandlerToTest(scopedContainer.ServiceProvider,
            scopedContainer.ServiceProvider.GetRequiredService<ILogger<JwtHandlerToTest>>(), setingsOptions!, "Obo")
        {
            InnerHandler = innerHandler.Object
        };

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");
        var invoker = new HttpMessageInvoker(sut);
        var response = await invoker.SendAsync(httpRequestMessage, new CancellationToken());

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        httpRequestMessage.Headers.Authorization.Should().NotBeNull();
        // The request must have a Bearer token injected in the authohirzation header.
        httpRequestMessage.Headers.Authorization!.Scheme.Should().Be("Bearer");
        httpRequestMessage.Headers.Authorization!.Parameter.Should().Be(accessToken);
    }

    [Fact]
    // Scenario 7
    public async Task Jwt_With_OAuth2_And_OIDC_With_OAuth2_As_The_Winner_Should()
    {
        // arrange
        // arrange the configuration to setup the Client secret.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:OAuth2.Settings:Audiences:0"] = "urn://audience.com",
                    ["Authentication:OAuth2.Settings:Scopes"] = "user.read user.write",
                    ["Authentication:OpenId.Settings:ClientId"] = "aa17786b-e33c-41ec-81cc-6063610aedeb",
                    ["Authentication:OpenId.Settings:ClientSecret"] = "This is a secret",
                    ["Authentication:OpenId.Settings:Audiences:0"] = "urn://audience.com",
                    ["Authentication:OpenId.Settings:Scopes:0"] = "user.read",
                    ["Authentication:OpenId.Settings:Scopes:1"] = "user.write",
                    ["Authentication:DefaultAuthority:Url"] = "https://login.microsoft.com"
                }).Build();

        // Define an access token that will be used as the return of the call to the CredentialDirect token credential provider.
        var jwtOAuth2 = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")],
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        var accessTokenOAuth2 = new JwtSecurityTokenHandler().WriteToken(jwtOAuth2);

        var jwtCookies = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")],
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        var accessTokenCookies = new JwtSecurityTokenHandler().WriteToken(jwtCookies);

        IConfiguration configuration = new ConfigurationRoot([.. config.Providers]);

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddDefaultAuthority(configuration);
        services.ConfigureOAuth2Settings(configuration, "Authentication:OAuth2.Settings");
        services.ConfigureOpenIdSettings(configuration, "Authentication:OpenId.Settings");
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();
        services.AddScoped<TokenRefreshInfo>();

        var mockILoggerFactory = new Mock<ILoggerFactory>();
        mockILoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        services.AddSingleton(mockILoggerFactory.Object);
        services.AddTransient(typeof(ILogger<>), typeof(LoggerWrapper<>));
        services.AddKeyedTransient<IAddPropertiesToLog, NullLoggerProperties>("Transient");

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedTransient<ITokenProvider, OidcTokenProvider>(OidcTokenProvider.ProviderName);
        services.AddKeyedTransient<ITokenProvider, BootstrapContextTokenProvider>(BootstrapContextTokenProvider
            .ProviderName);
        services.AddSingleton(mockHttpContextAccessor.Object);

        var container = services.BuildServiceProvider();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = container.CreateScope();
        var scopedServiceAccessor = container.GetRequiredService<IScopedServiceProviderAccessor>();
        scopedServiceAccessor!.ServiceProvider = scopedContainer.ServiceProvider;

        var tokenRefresh = scopedContainer.ServiceProvider.GetRequiredService<TokenRefreshInfo>();
        tokenRefresh!.RefreshToken =
            new TokenInfo("refresh_token", Guid.NewGuid().ToString(), DateTime.UtcNow.AddHours(1));
        tokenRefresh!.AccessToken = new TokenInfo("access_token", accessTokenCookies);

        var principal =
            new AppPrincipal(new Arc4u.Security.Principal.Authorization(),
                new ClaimsIdentity(Constants.BearerAuthenticationType) { BootstrapContext = accessTokenOAuth2 },
                "S-1-0-0") { Profile = UserProfile.Empty };

        // Define a Principal with no OAuth2Bearer token here => we test the injection.
        var appContext = scopedContainer.ServiceProvider.GetRequiredService<IApplicationContext>();
        appContext!.SetPrincipal(principal);

        var setingsOptions =
            scopedContainer.ServiceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();

        // Define the end handler that will simulate the call to the endpoint.
        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Verifiable();

        // Act
        var sut =
            new JwtHandlerToTest(scopedContainer.ServiceProvider,
                scopedContainer.ServiceProvider.GetRequiredService<ILogger<JwtHandlerToTest>>()!, setingsOptions!,
                Constants.BearerAuthenticationType)
            {
                InnerHandler =
                    new JwtHandlerToTest(scopedContainer.ServiceProvider,
                        scopedContainer.ServiceProvider.GetRequiredService<ILogger<JwtHandlerToTest>>()!, setingsOptions!,
                        Constants.CookiesAuthenticationType) { InnerHandler = innerHandler.Object }
            };

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");
        var invoker = new HttpMessageInvoker(sut);
        var response = await invoker.SendAsync(httpRequestMessage, new CancellationToken());

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        httpRequestMessage.Headers.Authorization.Should().NotBeNull();
        // The request must have a Bearer token injected in the authohirzation header.
        httpRequestMessage.Headers.Authorization!.Scheme.Should().Be("Bearer");
        httpRequestMessage.Headers.Authorization!.Parameter.Should().Be(accessTokenOAuth2);
    }

    [Fact]
    // Scenario 8
    public async Task Jwt_With_OAuth2_And_OIDC_With_OpenId_As_The_Winner_Should()
    {
        // arrange
        // arrange the configuration to setup the Client secret.
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:OAuth2.Settings:Audiences:0"] = "urn://audience.com",
                    ["Authentication:OAuth2.Settings:Scopes"] = "user.read user.write",
                    ["Authentication:OpenId.Settings:ClientId"] = "aa17786b-e33c-41ec-81cc-6063610aedeb",
                    ["Authentication:OpenId.Settings:ClientSecret"] = "This is a secret",
                    ["Authentication:OpenId.Settings:Audiences:0"] = "urn://audience.com",
                    ["Authentication:OpenId.Settings:Scopes:0"] = "user.read",
                    ["Authentication:OpenId.Settings:Scopes:1"] = "user.write",
                    ["Authentication:DefaultAuthority:Url"] = "https://login.microsoft.com"
                }).Build();

        // Define an access token that will be used as the return of the call to the CredentialDirect token credential provider.
        var jwtOAuth2 = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")],
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        var accessTokenOAuth2 = new JwtSecurityTokenHandler().WriteToken(jwtOAuth2);

        var jwtCookies = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")],
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        var accessTokenCookies = new JwtSecurityTokenHandler().WriteToken(jwtCookies);

        IConfiguration configuration = new ConfigurationRoot([.. config.Providers]);

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddDefaultAuthority(configuration);
        services.ConfigureOAuth2Settings(configuration, "Authentication:OAuth2.Settings");
        services.ConfigureOpenIdSettings(configuration, "Authentication:OpenId.Settings");
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();
        services.AddScoped<TokenRefreshInfo>();

        var mockILoggerFactory = new Mock<ILoggerFactory>();
        mockILoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        services.AddSingleton(mockILoggerFactory.Object);
        services.AddTransient(typeof(ILogger<>), typeof(LoggerWrapper<>));
        services.AddKeyedTransient<IAddPropertiesToLog, NullLoggerProperties>("Transient");

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        var mockTokenRefresh = _fixture.Freeze<Mock<ITokenRefreshProvider>>();

        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedTransient<ITokenProvider, OidcTokenProvider>(OidcTokenProvider.ProviderName);
        services.AddKeyedTransient<ITokenProvider, BootstrapContextTokenProvider>(BootstrapContextTokenProvider
            .ProviderName);
        services.AddSingleton(mockHttpContextAccessor.Object);
        services.AddSingleton<ITokenRefreshProvider>(mockTokenRefresh!.Object);

        var container = services.BuildServiceProvider();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = container.CreateScope();
        var scopedServiceAccessor = container.GetRequiredService<IScopedServiceProviderAccessor>();
        scopedServiceAccessor!.ServiceProvider = scopedContainer.ServiceProvider;

        var tokenRefresh = scopedContainer.ServiceProvider.GetRequiredService<TokenRefreshInfo>();
        tokenRefresh!.RefreshToken =
            new TokenInfo("refresh_token", Guid.NewGuid().ToString(), DateTime.UtcNow.AddHours(1));
        tokenRefresh!.AccessToken = new TokenInfo("access_token", accessTokenCookies);

        var principal =
            new AppPrincipal(new Arc4u.Security.Principal.Authorization(),
                new ClaimsIdentity(Constants.BearerAuthenticationType) { BootstrapContext = accessTokenOAuth2 },
                "S-1-0-0") { Profile = UserProfile.Empty };

        // Define a Principal with no OAuth2Bearer token here => we test the injection.
        var appContext = scopedContainer.ServiceProvider.GetRequiredService<IApplicationContext>();
        appContext!.SetPrincipal(principal);

        var setingsOptions =
            scopedContainer.ServiceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();

        // Define the end handler that will simulate the call to the endpoint.
        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Verifiable();

        // Act
        var sut =
            new JwtHandlerToTest(scopedContainer.ServiceProvider,
                scopedContainer.ServiceProvider.GetRequiredService<ILogger<JwtHandlerToTest>>()!, setingsOptions!,
                Constants.BearerAuthenticationType)
            {
                InnerHandler =
                    new JwtHandlerToTest(scopedContainer.ServiceProvider,
                        scopedContainer.ServiceProvider.GetRequiredService<ILogger<JwtHandlerToTest>>()!, setingsOptions!,
                        Constants.CookiesAuthenticationType) { InnerHandler = innerHandler.Object }
            };

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");
        var invoker = new HttpMessageInvoker(sut);
        var response = await invoker.SendAsync(httpRequestMessage, new CancellationToken());

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        httpRequestMessage.Headers.Authorization.Should().NotBeNull();
        // The request must have a Bearer token injected in the authohirzation header.
        httpRequestMessage.Headers.Authorization!.Scheme.Should().Be("Bearer");
        httpRequestMessage.Headers.Authorization!.Parameter.Should().Be(accessTokenCookies);
    }

    [Fact]
    // Scenario 9
    public async Task Jwt_With_OAuth2_And_OIDC_And_Inject_With_OAuth2_As_The_Winner_Should()
    {
        // arrange
        // arrange the configuration to setup the Client secret.
        var options = _fixture.Create<RemoteSecretSettingsOptions>();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Authentication:RemoteSecrets:Remote1:ClientSecret"] = options.ClientSecret,
                    ["Authentication:RemoteSecrets:Remote1:HeaderKey"] = "Basic",
                    ["Authentication:OAuth2.Settings:Audiences:0"] = "urn://audience.com",
                    ["Authentication:OAuth2.Settings:Scopes"] = "user.read user.write",
                    ["Authentication:OpenId.Settings:ClientId"] = "aa17786b-e33c-41ec-81cc-6063610aedeb",
                    ["Authentication:OpenId.Settings:ClientSecret"] = "This is a secret",
                    ["Authentication:OpenId.Settings:Audiences:0"] = "urn://audience.com",
                    ["Authentication:OpenId.Settings:Scopes:0"] = "user.read",
                    ["Authentication:OpenId.Settings:Scopes:1"] = "user.write",
                    ["Authentication:DefaultAuthority:Url"] = "https://login.microsoft.com"
                }).Build();

        // Define an access token that will be used as the return of the call to the CredentialDirect token credential provider.
        var jwtOAuth2 = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")],
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        var accessTokenOAuth2 = new JwtSecurityTokenHandler().WriteToken(jwtOAuth2);

        var jwtCookies = new JwtSecurityToken("issuer", "audience", [new Claim("key", "value")],
            DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        var accessTokenCookies = new JwtSecurityTokenHandler().WriteToken(jwtCookies);

        IConfiguration configuration = new ConfigurationRoot([.. config.Providers]);

        // Register the different services.
        IServiceCollection services = new ServiceCollection();

        services.AddSingleton<IScopedServiceProviderAccessor, ScopedServiceProviderAccessor>();
        services.AddDefaultAuthority(configuration);
        services.AddRemoteSecretsAuthentication(configuration);
        services.ConfigureOAuth2Settings(configuration, "Authentication:OAuth2.Settings");
        services.ConfigureOpenIdSettings(configuration, "Authentication:OpenId.Settings");
        services.AddScoped<IApplicationContext, ApplicationInstanceContext>();
        services.AddScoped<TokenRefreshInfo>();

        var mockILoggerFactory = new Mock<ILoggerFactory>();
        mockILoggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>()))
            .Returns(NullLogger.Instance);

        services.AddSingleton(mockILoggerFactory.Object);
        services.AddTransient(typeof(ILogger<>), typeof(LoggerWrapper<>));
        services.AddKeyedTransient<IAddPropertiesToLog, NullLoggerProperties>("Transient");

        var mockHttpContextAccessor = _fixture.Freeze<Mock<IHttpContextAccessor>>();
        mockHttpContextAccessor.SetupGet(x => x.HttpContext).Returns(() => null);

        // Register the different TokenProvider and CredentialTokenProviders.
        services.AddKeyedTransient<ITokenProvider, OidcTokenProvider>(OidcTokenProvider.ProviderName);
        services.AddKeyedTransient<ITokenProvider, BootstrapContextTokenProvider>(BootstrapContextTokenProvider
            .ProviderName);
        services.AddSingleton(mockHttpContextAccessor.Object);
        services.AddKeyedTransient<ITokenProvider, RemoteClientSecretTokenProvider>(RemoteClientSecretTokenProvider
            .ProviderName);

        var container = services.BuildServiceProvider();

        // Create a scope to be in the context majority of the time a business code is.
        using var scopedContainer = container.CreateScope();
        var scopedServiceAccessor = container.GetRequiredService<IScopedServiceProviderAccessor>();
        scopedServiceAccessor!.ServiceProvider = scopedContainer.ServiceProvider;

        var tokenRefresh = scopedContainer.ServiceProvider.GetRequiredService<TokenRefreshInfo>();
        tokenRefresh!.RefreshToken =
            new TokenInfo("refresh_token", Guid.NewGuid().ToString(), DateTime.UtcNow.AddHours(1));
        tokenRefresh!.AccessToken = new TokenInfo("access_token", accessTokenCookies);

        var principal =
            new AppPrincipal(new Arc4u.Security.Principal.Authorization(),
                new ClaimsIdentity(Constants.BearerAuthenticationType) { BootstrapContext = accessTokenOAuth2 },
                "S-1-0-0") { Profile = UserProfile.Empty };

        // Define a Principal with no OAuth2Bearer token here => we test the injection.
        var appContext = scopedContainer.ServiceProvider.GetRequiredService<IApplicationContext>();
        appContext!.SetPrincipal(principal);

        var setingsOptions =
            scopedContainer.ServiceProvider.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>();

        // Define the end handler that will simulate the call to the endpoint.
        var innerHandler = new Mock<HttpMessageHandler>();
        innerHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK))
            .Verifiable();

        // Act
        var sut =
            new JwtHandlerToTest(scopedContainer.ServiceProvider,
                scopedContainer.ServiceProvider.GetRequiredService<ILogger<JwtHandlerToTest>>()!, setingsOptions!,
                Constants.BearerAuthenticationType)
            {
                InnerHandler =
                    new JwtHandlerToTest(scopedContainer.ServiceProvider,
                        scopedContainer.ServiceProvider.GetRequiredService<ILogger<JwtHandlerToTest>>()!, setingsOptions!,
                        Constants.CookiesAuthenticationType)
                    {
                        InnerHandler =
                            new JwtHandlerToTest(scopedContainer.ServiceProvider,
                                scopedContainer.ServiceProvider.GetRequiredService<ILogger<JwtHandlerToTest>>()!,
                                setingsOptions!, "Remote1") { InnerHandler = innerHandler.Object }
                    }
            };

        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://example.com/");
        var invoker = new HttpMessageInvoker(sut);
        var response = await invoker.SendAsync(httpRequestMessage, new CancellationToken());

        // Assert
        response.Should().NotBeNull();
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        httpRequestMessage.Headers.Authorization.Should().NotBeNull();
        // The request must have a Bearer token injected in the authohirzation header.
        httpRequestMessage.Headers.Authorization!.Scheme.Should().Be("Bearer");
        httpRequestMessage.Headers.Authorization!.Parameter.Should().Be(accessTokenOAuth2);
    }
}
