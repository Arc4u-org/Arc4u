using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Arc4u.Configuration;
using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Events;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;
using Arc4u.Security.Cryptography;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.Kernel;
using AwesomeAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class HybridAuthenticationOptionsTests
{
    private readonly Fixture _fixture;

    public HybridAuthenticationOptionsTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
        _fixture.Customizations.Add(new X509Certificate2SpecimenBuilder());
    }

    [Fact]
    public void Empty_Oidc_Authentication_Should()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>()).Build();

        IConfiguration configuration = new ConfigurationRoot([.. config.Providers]);

        IServiceCollection services = new ServiceCollection();

        var exception = Record.Exception(() => services.AddHybridAuthentication(configuration));

        exception.Should().BeOfType<ConfigurationException>();
    }

    [Fact]
    public void Default_Oidc_Authentication_Value_Should()
    {
        var oidcSettings = _fixture.Create<OidcAuthenticationOptions>();
        var tokenCacheSettings = _fixture.Create<TokenCacheOptions>();
        var defaultAuthority = _fixture.Create<AuthorityOptions>();
        var oidcOptions = _fixture.Create<OpenIdSettingsOption>();

        var configDic = new Dictionary<string, string?>
        {
            { "Application.configuration:ApplicationName", "TestName" },
            { "Authentication:AuthenticationMethod", nameof(OpenIdConnectRedirectBehavior.FormPost) },
            { "Authentication:DefaultAuthority:RetryInterval", defaultAuthority.RetryInterval.ToString() },
            { "Authentication:DefaultAuthority:MetaDataAddress", defaultAuthority.MetaDataAddress!.ToString() },
            { "Authentication:DefaultAuthority:Url", defaultAuthority.Url.ToString() },
            { "Authentication:DefaultAuthority:TokenEndpoint", defaultAuthority.TokenEndpoint!.ToString() },
            { "Authentication:CookieName", oidcSettings.CookieName },
            { "Authentication:ApplicationName", oidcSettings.ApplicationName },
            { "Authentication:TokenCache:CacheName", tokenCacheSettings.CacheName },
            { "Authentication:ValidateAudience", false.ToString() },
            { "Authentication:ValidateAuthority", false.ToString() },
            { "Authentication:DataProtection:CacheStore:CacheKey", "TokenCacheKey" },
            { "Authentication:DataProtection:CacheStore:CacheName", "TokenCacheName" },

            // OpenId
            { "Authentication:OpenId.Settings:ClientId", oidcOptions.ClientId },
            { "Authentication:OpenId.Settings:ClientSecret", oidcOptions.ClientSecret }
        };

        // OpenId
        foreach (var audience in oidcOptions.Audiences)
        {
            configDic.Add($"Authentication:OpenId.Settings:Audiences:{oidcOptions.Audiences.IndexOf(audience)}",
                audience);
        }

        foreach (var scope in oidcOptions.Scopes)
        {
            configDic.Add($"Authentication:OpenId.Settings:Scopes:{oidcOptions.Scopes.IndexOf(scope)}", scope);
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDic).Build();

        IConfiguration configuration = new ConfigurationRoot([.. config.Providers]);

        IServiceCollection services = new ServiceCollection();

        var mockCertificateLoader = new Mock<IX509CertificateLoader>();
        mockCertificateLoader.Setup(loader => loader.FindCertificate(It.IsAny<IConfiguration>(), It.IsAny<string>()))
            .Returns(_fixture.Create<X509Certificate2>());

        services.AddOidcAuthentication(configuration, certificateLoader: mockCertificateLoader.Object);

        var app = services.BuildServiceProvider();

        var sut = app.GetRequiredService<IOptionsMonitor<OidcAuthenticationOptions>>().CurrentValue;

        sut.Should().NotBeNull();
        sut.AuthenticationMethod.Should().Be(OpenIdConnectRedirectBehavior.FormPost);
        sut.ValidateAudience.Should().BeFalse();
        sut.ValidateAuthority.Should().BeFalse();
    }

    [Fact]
    public void Default_Hybrid_Authentication_Value_Should()
    {
        var oidcSettings = _fixture.Create<HybridAuthenticationOptions>();
        var tokenCacheSettings = _fixture.Create<TokenCacheOptions>();
        var defaultAuthority = _fixture.Create<AuthorityOptions>();
        var auth2Options = _fixture.Create<OAuth2SettingsOption>();
        var oidcOptions = _fixture.Create<OpenIdSettingsOption>();

        var configDic = new Dictionary<string, string?>
        {
            { "Application.configuration:ApplicationName", "TestName" },
            { "Authentication:DefaultAuthority:RetryInterval", defaultAuthority.RetryInterval.ToString() },
            { "Authentication:DefaultAuthority:MetaDataAddress", defaultAuthority.MetaDataAddress!.ToString() },
            { "Authentication:DefaultAuthority:Url", defaultAuthority.Url.ToString() },
            { "Authentication:DefaultAuthority:TokenEndpoint", defaultAuthority.TokenEndpoint!.ToString() },
            { "Authentication:CookieName", oidcSettings.CookieName },
            { "Authentication:ApplicationName", oidcSettings.ApplicationName },
            { "Authentication:AuthenticationMethod", nameof(OpenIdConnectRedirectBehavior.FormPost) },
            { "Authentication:TokenCache:CacheName", tokenCacheSettings.CacheName },
            { "Authentication:ValidateAudience", false.ToString() },
            { "Authentication:ValidateAuthority", false.ToString() },
            { "Authentication:DataProtection:CacheStore:CacheKey", "TokenCacheKey" },
            { "Authentication:DataProtection:CacheStore:CacheName", "TokenCacheName" },

            // OpenId
            { "Authentication:OpenId.Settings:ClientId", oidcOptions.ClientId },
            { "Authentication:OpenId.Settings:ClientSecret", oidcOptions.ClientSecret }
        };

        // OAuth2
        foreach (var audience in auth2Options.Audiences)
        {
            configDic.Add($"Authentication:OAuth2.Settings:Audiences:{auth2Options.Audiences.IndexOf(audience)}",
                audience);
        }

        foreach (var scope in auth2Options.Scopes)
        {
            configDic.Add($"Authentication:OAuth2.Settings:Scopes:{auth2Options.Scopes.IndexOf(scope)}", scope);
        }

        // OpenId
        foreach (var audience in oidcOptions.Audiences)
        {
            configDic.Add($"Authentication:OpenId.Settings:Audiences:{oidcOptions.Audiences.IndexOf(audience)}",
                audience);
        }

        foreach (var scope in oidcOptions.Scopes)
        {
            configDic.Add($"Authentication:OpenId.Settings:Scopes:{oidcOptions.Scopes.IndexOf(scope)}", scope);
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configDic).Build();

        IConfiguration configuration = new ConfigurationRoot([.. config.Providers]);

        IServiceCollection services = new ServiceCollection();

        var mockCertificateLoader = new Mock<IX509CertificateLoader>();
        mockCertificateLoader.Setup(loader => loader.FindCertificate(It.IsAny<IConfiguration>(), It.IsAny<string>()))
            .Returns(_fixture.Create<X509Certificate2>());

        services.AddHybridAuthentication(configuration, certificateLoader: mockCertificateLoader.Object);

        var app = services.BuildServiceProvider();

        var sut = app.GetRequiredService<IOptionsMonitor<OidcAuthenticationOptions>>().CurrentValue;

        sut.Should().NotBeNull();
        sut.AuthenticationMethod.Should().Be(OpenIdConnectRedirectBehavior.FormPost);
        sut.ValidateAudience.Should().BeFalse();
        sut.ValidateAuthority.Should().BeFalse();

        app.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme)
            .TokenValidationParameters.ValidateIssuer.Should().BeFalse();
    }

    [Fact]
    public void Code_Hybrid_Authentication_Copies_Validate_Flags_Should()
    {
        var defaultAuthority = _fixture.Create<AuthorityOptions>();

        IServiceCollection services = new ServiceCollection();

        services.AddHybridAuthentication(options =>
        {
            options.DefaultAuthority = defaultAuthority;
            options.CookieName = "Cookies";
            options.ApplicationName = "TestName";
            options.DataProtectionCertificate = _fixture.Create<X509Certificate2>();
            options.DataProtectionCacheStoreOption = o => { o.CacheKey = "Key"; o.CacheName = "Name"; };
            options.ClaimsIdentifierOptions = o => o.Add("oid");
            options.OpenIdSettingsOptions = o =>
            {
                o.ClientId = "ClientId";
                o.ClientSecret = "Secret";
                o.Audiences = ["ClientId"];
                o.Scopes = ["openid"];
            };
            options.OAuth2SettingsOptions = o => o.Audiences = ["api"];
            options.ValidateAudience = false;
            options.ValidateAuthority = false;
        });

        var provider = services.BuildServiceProvider();
        var sut = provider.GetRequiredService<IOptionsMonitor<OidcAuthenticationOptions>>().CurrentValue;

        sut.ValidateAudience.Should().BeFalse();
        sut.ValidateAuthority.Should().BeFalse();

        provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme)
            .TokenValidationParameters.ValidateIssuer.Should().BeFalse();
    }

    [Fact]
    public void Code_Hybrid_Authentication_Validates_Bearer_Issuer_By_Default_Should()
    {
        var defaultAuthority = new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null,
            new Uri("https://login.example.com/tenant/v2.0"), null);

        IServiceCollection services = new ServiceCollection();

        services.AddHybridAuthentication(options =>
        {
            options.DefaultAuthority = defaultAuthority;
            options.CookieName = "Cookies";
            options.ApplicationName = "TestName";
            options.DataProtectionCertificate = _fixture.Create<X509Certificate2>();
            options.DataProtectionCacheStoreOption = o => { o.CacheKey = "Key"; o.CacheName = "Name"; };
            options.ClaimsIdentifierOptions = o => o.Add("oid");
            options.OpenIdSettingsOptions = o =>
            {
                o.ClientId = "ClientId";
                o.ClientSecret = "Secret";
                o.Audiences = ["ClientId"];
                o.Scopes = ["openid"];
            };
            options.OAuth2SettingsOptions = o => o.Audiences = ["api"];
        });

        var sut = services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;

        sut.ValidateIssuer.Should().BeTrue();
        sut.ValidIssuer.Should().Be(defaultAuthority.Issuer!.ToString());
    }

    [Fact]
    public async Task Token_Response_Without_Audiences_Key_Should_Not_Throw()
    {
        var authority = new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null, null, null);
        var oidcOptions = new OidcAuthenticationOptions { DefaultAuthority = authority }; // ValidateAudience is true.
        var openIdSettings = new SimpleKeyValueSettings(); // No Audiences key: OpenId.Settings:ValidateAudience is false.

        var oidcMonitor = Mock.Of<IOptionsMonitor<OidcAuthenticationOptions>>(m => m.CurrentValue == oidcOptions);
        var settingsMonitor = Mock.Of<IOptionsMonitor<SimpleKeyValueSettings>>(m => m.Get(It.IsAny<string>()) == openIdSettings);
        var extraMonitor = Mock.Of<IOptionsMonitor<ApiExtraContextAuthenticationOption>>(m => m.CurrentValue == new ApiExtraContextAuthenticationOption());

        var sut = new StandardOpenIdConnectEvents(NullLogger<StandardOpenIdConnectEvents>.Instance, oidcMonitor, settingsMonitor, extraMonitor);

        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer: authority.Url.AbsoluteUri, audience: "any"));
        var context = new TokenResponseReceivedContext(new DefaultHttpContext(),
            new AuthenticationScheme(OpenIdConnectDefaults.AuthenticationScheme, null, typeof(OpenIdConnectHandler)),
            new OpenIdConnectOptions(), new ClaimsPrincipal(), new AuthenticationProperties())
        {
            TokenEndpointResponse = new OpenIdConnectMessage { AccessToken = token }
        };

        await sut.TokenResponseReceived(context);

        context.Result.Should().BeNull();
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("false", false)]
    [InlineData("true", true)]
    public void Jwt_Authentication_ValidateAuthority_Should_Drive_Issuer_Validation(string? validateAuthority, bool expected)
    {
        const string issuer = "https://login.example.com/tenant/v2.0";
        var configDic = new Dictionary<string, string?>
        {
            { "Authentication:DefaultAuthority:Url", "https://login.example.com/tenant/v2.0" },
            { "Authentication:DefaultAuthority:Issuer", issuer },
            { "Authentication:TokenCache:CacheName", "Default" },
            { "Authentication:OAuth2.Settings:Audiences:0", "api" },
        };
        if (validateAuthority is not null)
        {
            configDic.Add("Authentication:ValidateAuthority", validateAuthority);
        }

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(configDic).Build();

        IServiceCollection services = new ServiceCollection();
        services.AddJwtAuthentication(configuration);

        var sut = services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;

        sut.ValidateIssuer.Should().Be(expected);
        if (expected)
        {
            sut.ValidIssuer.Should().Be(new Uri(issuer).ToString());
        }
    }

    [Theory]
    [InlineData("https://issuer.example.com/tenant/", null, "https://issuer.example.com/tenant/", true)]
    [InlineData("https://issuer.example.com/tenant/", null, "https://other.example.com/tenant/", false)]
    [InlineData(null, "https://metadata.example.com/tenant/", "https://metadata.example.com/tenant/", true)]
    [InlineData(null, "https://metadata.example.com/tenant/", "https://login.example.com/tenant/v2.0", false)]
    public async Task Token_Response_Issuer_Should_Match_Configured_Or_Metadata_Issuer(string? configuredIssuer, string? metadataIssuer, string tokenIssuer, bool accepted)
    {
        var authority = new AuthorityOptions(new Uri("https://login.example.com/tenant/v2.0"), null,
            configuredIssuer is null ? null : new Uri(configuredIssuer), null);
        var oidcOptions = new OidcAuthenticationOptions { DefaultAuthority = authority, ValidateAudience = false };

        var oidcMonitor = Mock.Of<IOptionsMonitor<OidcAuthenticationOptions>>(m => m.CurrentValue == oidcOptions);
        var settingsMonitor = Mock.Of<IOptionsMonitor<SimpleKeyValueSettings>>(m => m.Get(It.IsAny<string>()) == new SimpleKeyValueSettings());
        var extraMonitor = Mock.Of<IOptionsMonitor<ApiExtraContextAuthenticationOption>>(m => m.CurrentValue == new ApiExtraContextAuthenticationOption());

        var loggerWrapper = new Mock<ILoggerWrapper<StandardOpenIdConnectEvents>>();
        loggerWrapper.Setup(m => m.SetContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Type?>())).Returns(loggerWrapper.Object);
        var logger = loggerWrapper.As<ILogger<StandardOpenIdConnectEvents>>().Object;

        var sut = new StandardOpenIdConnectEvents(logger, oidcMonitor, settingsMonitor, extraMonitor);

        var handlerOptions = new OpenIdConnectOptions();
        if (metadataIssuer is not null)
        {
            handlerOptions.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration { Issuer = metadataIssuer };
        }

        var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer: tokenIssuer, audience: "any"));
        var context = new TokenResponseReceivedContext(new DefaultHttpContext(),
            new AuthenticationScheme(OpenIdConnectDefaults.AuthenticationScheme, null, typeof(OpenIdConnectHandler)),
            handlerOptions, new ClaimsPrincipal(), new AuthenticationProperties())
        {
            TokenEndpointResponse = new OpenIdConnectMessage { AccessToken = token }
        };

        await sut.TokenResponseReceived(context);

        if (accepted)
        {
            context.Result.Should().BeNull();
        }
        else
        {
            context.Result!.Failure!.Message.Should().Be("Invalid authority");
        }
    }

    [Fact]
    public void Test_GetType_From_ServiceCollection()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddTransient<StandardBearerEvents>();

        var type = services.GetImplementationType<JwtBearerEvents>();
        type.Should().Be(typeof(StandardBearerEvents));
    }

    public class X509Certificate2SpecimenBuilder : ISpecimenBuilder
    {
        public object Create(object request, ISpecimenContext context)
        {
            if (request is Type type && type == typeof(X509Certificate2))
            {
                // Create a self-signed certificate for testing purposes
                var ecdsa = ECDsa.Create();
                var req = new CertificateRequest("cn=TestCertificate", ecdsa, HashAlgorithmName.SHA256);
                var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1));
                return cert;
            }

            return new NoSpecimen();
        }
    }
}
