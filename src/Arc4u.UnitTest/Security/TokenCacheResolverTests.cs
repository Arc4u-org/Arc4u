using Arc4u.Caching;
using Arc4u.Dependency;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Security;

[Trait("Category", "CI")]
public class TokenCacheResolverTests
{
    private readonly Mock<ICacheHelper> _cacheHelper = new();
    private readonly LogCapture _logs = new();

    public TokenCacheResolverTests()
    {
        _cacheHelper.Setup(h => h.GetCache()).Returns(new Mock<ICache>().Object);
        _cacheHelper.Setup(h => h.GetCache(It.IsAny<string?>())).Returns(new Mock<ICache>().Object);
    }

    private ServiceProvider BuildContainer()
    {
        var services = new ServiceCollection();
        services.AddTokenCache(o => o.CacheName = "Default");
        services.AddNamedTokenCache("Obo1", "Redis", TimeSpan.FromMinutes(5));
        services.AddSingleton(_cacheHelper.Object);
        services.AddLogging(builder => builder.AddProvider(_logs));
        services.AddILogger();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void AddTokenCache_Registers_The_Resolver_As_A_Singleton_Should()
    {
        using var container = BuildContainer();

        container.GetRequiredService<TokenCacheResolver>().Should().BeSameAs(container.GetRequiredService<TokenCacheResolver>());
    }

    [Fact]
    public void An_Unregistered_Name_Uses_The_Default_Options_And_Is_Reported_Once_Should()
    {
        using var container = BuildContainer();
        var sut = container.GetRequiredService<TokenCacheResolver>();

        for (var request = 0; request < 3; request++)
        {
            sut.Resolve("Typo").MaxTime.Should().Be(new TokenCacheOptions().MaxTime);
            sut.Resolve("Obo1").MaxTime.Should().Be(TimeSpan.FromMinutes(5));
        }

        // Event 9193: LogUnregisteredTokenCacheOptions.
        _logs.Warnings.Where(w => w.EventId.Id == 9193).Select(w => w.Message).Should().ContainSingle().Which.Should().Contain("Typo");
    }

    [Fact]
    public void A_Name_Bound_With_BindConfiguration_Is_Registered_Should()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Caches:Reports:CacheName"] = "Redis",
            ["Caches:Reports:MaxTime"] = "00:10:00"
        }).Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddTokenCache(o => o.CacheName = "Default");
        services.AddOptions<TokenCacheOptions>("Reports").BindConfiguration("Caches:Reports");
        services.AddSingleton(_cacheHelper.Object);
        services.AddLogging(builder => builder.AddProvider(_logs));
        services.AddILogger();
        using var container = services.BuildServiceProvider();

        var resolved = container.GetRequiredService<TokenCacheResolver>().Resolve("Reports");

        resolved.MaxTime.Should().Be(TimeSpan.FromMinutes(10));
        resolved.CacheName.Should().Be("Redis");
        _logs.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void ConfigureAll_Does_Not_Make_An_Unregistered_Name_Registered_Should()
    {
        var services = new ServiceCollection();
        services.AddTokenCache(o => o.CacheName = "Default");
        services.ConfigureAll<TokenCacheOptions>(o => o.MaxTime = TimeSpan.FromMinutes(20));
        services.AddSingleton(_cacheHelper.Object);
        services.AddLogging(builder => builder.AddProvider(_logs));
        services.AddILogger();
        using var container = services.BuildServiceProvider();
        var sut = container.GetRequiredService<TokenCacheResolver>();

        // The typo uses the default options, and is reported.
        var resolved = sut.Resolve("Typo");

        resolved.CacheName.Should().BeNull();
        _logs.Warnings.Where(w => w.EventId.Id == 9193).Should().ContainSingle();
    }

    [Fact]
    public async Task An_Unregistered_Name_Is_Reported_Once_When_Requests_Race_Should()
    {
        using var container = BuildContainer();
        var sut = container.GetRequiredService<TokenCacheResolver>();

        await Task.WhenAll(Enumerable.Range(0, 32).Select(_ => Task.Run(() => sut.Resolve("Typo"))));

        _logs.Warnings.Where(w => w.EventId.Id == 9193).Should().ContainSingle();
    }

    [Fact]
    public void ApplicationCache_Resolves_Without_AddTokenCache_When_The_Client_Tokens_Are_Registered_Should()
    {
        // A JWT API without an Authentication:TokenCache section: AddTokenCache is not called, the default cache is used.
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var services = new ServiceCollection();
        services.AddClientTokens(configuration);
        services.AddOnBehalfOf(configuration);
        services.AddSingleton(_cacheHelper.Object);
        services.AddSingleton<ITokenCache, ApplicationCache>();
        services.AddLogging(builder => builder.AddProvider(_logs));
        services.AddILogger();
        using var container = services.BuildServiceProvider();

        container.GetRequiredService<ITokenCache>().Should().BeOfType<ApplicationCache>();
        services.Count(d => d.ServiceType == typeof(TokenCacheResolver)).Should().Be(1);
    }

    [Fact]
    public void An_Api_Without_A_Token_Cache_Passes_The_Container_Validation_Should()
    {
        // No ICacheHelper is registered: the resolver registered by AddClientTokens and AddOnBehalfOf is never used.
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var services = new ServiceCollection();
        services.AddClientTokens(configuration);
        services.AddOnBehalfOf(configuration);

        var exception = Record.Exception(() => services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }));

        exception.Should().BeNull();
    }

    [Fact]
    public void Named_Options_Using_The_Token_Cache_Read_It_With_GetCache_Should()
    {
        // Only the token cache is set up, as in a mock of ICacheHelper written for the default options.
        var tokenCache = new Mock<ICache>().Object;
        var cacheHelper = new Mock<ICacheHelper>();
        cacheHelper.Setup(h => h.GetCache()).Returns(tokenCache);
        var services = new ServiceCollection();
        services.AddTokenCache(o => o.CacheName = "Default");
        services.AddNamedTokenCache("OwnMaxTime", cacheName: null, TimeSpan.FromMinutes(5));
        services.AddNamedTokenCache("SameCache", "Default", TimeSpan.FromMinutes(7));
        services.AddSingleton(cacheHelper.Object);
        services.AddLogging(builder => builder.AddProvider(_logs));
        services.AddILogger();
        using var container = services.BuildServiceProvider();
        var sut = container.GetRequiredService<TokenCacheResolver>();

        sut.Resolve("OwnMaxTime").Should().Be(new ResolvedTokenCache(tokenCache, TimeSpan.FromMinutes(5)));
        sut.Resolve("SameCache").Should().Be(new ResolvedTokenCache(tokenCache, TimeSpan.FromMinutes(7)));
        cacheHelper.Verify(h => h.GetCache(It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public void The_Cache_Of_The_Default_Options_Has_No_Name_Should()
    {
        using var container = BuildContainer();
        var sut = container.GetRequiredService<TokenCacheResolver>();

        sut.Resolve(null).CacheName.Should().BeNull();
        sut.Resolve("Obo1").CacheName.Should().Be("Redis");
    }

    private sealed class LogCapture : ILoggerProvider, ILogger
    {
        public List<(EventId EventId, string Message)> Warnings { get; } = [];

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning)
            {
                lock (Warnings)
                {
                    Warnings.Add((eventId, formatter(state, exception)));
                }
            }
        }

        public void Dispose() { }
    }
}
