using Arc4u.Configuration;
using Arc4u.Diagnostics;
using Arc4u.OAuth2;
using Arc4u.OAuth2.Extensions;
using Arc4u.Security.Principal;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AspNetCoreJwtHttpHandler = Arc4u.OAuth2.Token.JwtHttpHandler<Arc4u.UnitTest.Security.NamedSettingsTests>;
using GrpcOAuth2Interceptor = Arc4u.gRPC.Interceptors.OAuth2Interceptor<Arc4u.UnitTest.Security.NamedSettingsTests>;
using BlazorJwtHttpHandler = Arc4u.Blazor.Handlers.JwtHttpHandler;
using ClientJwtHttpHandler = Arc4u.OAuth2.Client.Authentication.Token.JwtHttpHandler<Arc4u.UnitTest.Security.NamedSettingsTests>;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class NamedSettingsTests
{
    [Fact]
    public void Named_Options_Are_Resolved()
    {
        var services = new ServiceCollection();
        services.Configure<SimpleKeyValueSettings>("OidcClient", o => o.Add("ProviderId", "Oidc"));
        using var sp = services.BuildServiceProvider();

        sp.TryGetNamedSettings("OidcClient", out var settings).Should().BeTrue();
        settings!.Values["ProviderId"].Should().Be("Oidc");
    }

    [Fact]
    public void Keyed_Settings_Are_Ignored()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddKeyedSingleton<IKeyValueSettings>("OAuth2", new SimpleKeyValueSettings(new Dictionary<string, string> { ["ProviderId"] = "Keyed" }));
        using var sp = services.BuildServiceProvider();

        sp.TryGetNamedSettings("OAuth2", out var settings).Should().BeFalse();
        settings.Should().BeNull();
    }

    [Fact]
    public void Unknown_Name_Is_Not_Found()
    {
        var services = new ServiceCollection();
        services.Configure<SimpleKeyValueSettings>("OidcClient", o => o.Add("ProviderId", "Oidc"));
        using var sp = services.BuildServiceProvider();

        sp.TryGetNamedSettings("Unknown", out var settings).Should().BeFalse();
        settings.Should().BeNull();
    }

    [Fact]
    public void Client_JwtHttpHandler_Resolves_Named_Options()
    {
        var services = new ServiceCollection();
        services.Configure<SimpleKeyValueSettings>("OidcClient", o => o.Add("ProviderId", "Oidc"));
        using var sp = BuildHandlerServices(services);

        var act = () => new ClientJwtHttpHandler(sp, sp.GetRequiredService<ILogger<NamedSettingsTests>>(), "OidcClient");

        act.Should().NotThrow();
    }

    [Fact]
    public void Client_JwtHttpHandler_Throws_For_Unknown_Name()
    {
        var services = new ServiceCollection();
        using var sp = BuildHandlerServices(services);

        var act = () => new ClientJwtHttpHandler(sp, sp.GetRequiredService<ILogger<NamedSettingsTests>>(), "Unknown");

        act.Should().Throw<ConfigurationException>();
    }

    [Fact]
    public void AspNetCore_JwtHttpHandler_Resolves_Named_Options()
    {
        var services = new ServiceCollection();
        services.Configure<SimpleKeyValueSettings>("OAuth2", o => o.Add("ProviderId", "Bootstrap"));
        services.AddSingleton(new Mock<IScopedServiceProviderAccessor>().Object);
        using var sp = BuildHandlerServices(services);

        var act = () => new AspNetCoreJwtHttpHandler(sp, sp.GetRequiredService<ILogger<NamedSettingsTests>>(), "OAuth2");

        act.Should().NotThrow();
    }

    [Fact]
    public void AspNetCore_JwtHttpHandler_Throws_For_Unknown_Name()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new Mock<IScopedServiceProviderAccessor>().Object);
        using var sp = BuildHandlerServices(services);

        var act = () => new AspNetCoreJwtHttpHandler(sp, sp.GetRequiredService<ILogger<NamedSettingsTests>>(), "Unknown");

        act.Should().Throw<ConfigurationException>();
    }

    [Fact]
    public void Blazor_JwtHttpHandler_Resolves_Named_Options()
    {
        var services = new ServiceCollection();
        services.Configure<SimpleKeyValueSettings>("OAuth2", o => o.Add("ProviderId", "Blazor"));
        using var sp = BuildHandlerServices(services);

        var act = () => new BlazorJwtHttpHandler(sp, sp.GetRequiredService<ILogger<BlazorJwtHttpHandler>>(), "OAuth2");

        act.Should().NotThrow();
    }

    [Fact]
    public void Blazor_JwtHttpHandler_Throws_For_Unknown_Name()
    {
        var services = new ServiceCollection();
        using var sp = BuildHandlerServices(services);

        var act = () => new BlazorJwtHttpHandler(sp, sp.GetRequiredService<ILogger<BlazorJwtHttpHandler>>(), "Unknown");

        act.Should().Throw<ConfigurationException>();
    }

    [Fact]
    public void OAuth2Interceptor_Resolves_Named_Options()
    {
        var services = new ServiceCollection();
        services.Configure<SimpleKeyValueSettings>("OAuth2", o => o.Add("ProviderId", "Bootstrap"));
        using var sp = BuildHandlerServices(services);

        var act = () => new GrpcOAuth2Interceptor(sp, sp.GetRequiredService<ILogger<NamedSettingsTests>>(), "OAuth2");

        act.Should().NotThrow();
    }

    [Fact]
    public void OAuth2Interceptor_Throws_For_Unknown_Name()
    {
        var services = new ServiceCollection();
        using var sp = BuildHandlerServices(services);

        var act = () => new GrpcOAuth2Interceptor(sp, sp.GetRequiredService<ILogger<NamedSettingsTests>>(), "Unknown");

        act.Should().Throw<ConfigurationException>();
    }

    private static ServiceProvider BuildHandlerServices(ServiceCollection services)
    {
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory.Setup(m => m.CreateLogger(It.IsAny<string>())).Returns(NullLogger.Instance);

        services.AddOptions();
        services.AddSingleton(loggerFactory.Object);
        services.AddTransient(typeof(ILogger<>), typeof(LoggerWrapper<>));
        services.AddKeyedTransient<IAddPropertiesToLog, NullLoggerProperties>("Transient");
        services.AddSingleton(new Mock<IApplicationContext>().Object);

        return services.BuildServiceProvider();
    }
}
