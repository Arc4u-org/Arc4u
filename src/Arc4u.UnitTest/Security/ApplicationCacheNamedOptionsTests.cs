using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.Dependency;
using Arc4u.OAuth2.Extensions;
using Arc4u.OAuth2.Options;
using Arc4u.OAuth2.Token;
using Arc4u.OAuth2.TokenProvider.Scenarios;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Environment = Arc4u.Configuration.Environment;

namespace Arc4u.UnitTest.Security;

/// <summary>
/// <see cref="ITokenCache"/> (<see cref="ApplicationCache"/> with the real <see cref="CacheHelper"/>) used with specific <see cref="TokenCacheOptions"/>:
/// the named options registered from the <c>Authentication:ClientTokens</c> entries and the <c>Authentication:ClientTokensCache</c> section.
/// </summary>
[Trait("Category", "CI")]
public class ApplicationCacheNamedOptionsTests
{
    private readonly Mock<ICacheContext> _cacheContext = new();
    private readonly Dictionary<string, FakeCache> _caches = new(StringComparer.Ordinal);

    public ApplicationCacheNamedOptionsTests()
    {
        foreach (var name in new[] { "Default", "Token", "Shared", "Redis", "Sql" })
        {
            var cache = new FakeCache();
            _caches[name] = cache;
            _cacheContext.Setup(c => c.Exist(name)).Returns(name != "Default");
            _cacheContext.Setup(c => c[name]).Returns(cache);
        }
        _cacheContext.Setup(c => c.Default).Returns(_caches["Default"]);
    }

    /// <summary>
    /// Default options: cache <c>Token</c>, 40 minutes.
    /// <c>Redis</c> entry: cache <c>Redis</c>, 5 minutes. <c>Sql</c> entry: cache <c>Sql</c>, MaxTime inherited.
    /// <c>Unknown</c> entry: a cache that does not exist, 15 minutes. <c>Plain</c> entry: no TokenCache.
    /// </summary>
    private (ITokenCache Sut, IServiceProvider Container) BuildSut(bool withSharedSection = false, Action<IServiceCollection, IConfiguration>? configure = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Authentication:TokenCache:CacheName"] = "Token",
            ["Authentication:TokenCache:MaxTime"] = "00:40:00",
        };
        AddEntry(values, "Redis", ("CacheName", "Redis"), ("MaxTime", "00:05:00"));
        AddEntry(values, "Sql", ("CacheName", "Sql"));
        AddEntry(values, "Unknown", ("CacheName", "DoesNotExist"), ("MaxTime", "00:15:00"));
        AddEntry(values, "Plain");
        if (withSharedSection)
        {
            values["Authentication:ClientTokensCache:CacheName"] = "Shared";
            values["Authentication:ClientTokensCache:MaxTime"] = "00:20:00";
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var fixture = new Fixture().Customize(new AutoMoqCustomization());

        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        services.AddILogger();
        services.AddApplicationConfig(config =>
        {
            config.ApplicationName = fixture.Create<string>();
            config.Environment = fixture.Create<Environment>();
        });
        services.AddTokenCache(configuration);
        services.AddClientTokens(configuration);
        configure?.Invoke(services, configuration);
        services.AddSingleton(_cacheContext.Object);
        services.AddSingleton<ICacheHelper, CacheHelper>();
        // The TokenCacheResolver is registered by AddTokenCache.
        services.AddSingleton<ITokenCache, ApplicationCache>();
        var container = services.BuildServiceProvider();

        return (container.GetRequiredService<ITokenCache>(), container);
    }

    private static void AddEntry(Dictionary<string, string?> values, string key, params (string Name, string Value)[] tokenCache)
    {
        values[$"Authentication:ClientTokens:{key}:Scenario"] = ClientCredentialsScenario.Name;
        values[$"Authentication:ClientTokens:{key}:Settings:ClientId"] = "client";
        values[$"Authentication:ClientTokens:{key}:Settings:ClientSecret"] = "secret";
        foreach (var (name, value) in tokenCache)
        {
            values[$"Authentication:ClientTokens:{key}:TokenCache:{name}"] = value;
        }
    }

    /// <summary>The name the token providers pass to the <see cref="ITokenCache"/> for the entry.</summary>
    private static string? OptionsNameOf(IServiceProvider container, string entry)
        => container.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>().Get(entry).Values
                    .TryGetValue(TokenKeys.TokenCacheOptionsName, out var name) ? name : null;

    [Fact]
    public async Task Put_With_Specific_Options_Stores_In_Their_Cache_With_Their_MaxTime_Should()
    {
        var (sut, container) = BuildSut();

        await sut.PutAsync("key", "value", OptionsNameOf(container, "Redis"));

        _caches["Redis"].Timeouts["key_tokencache"].Should().Be(TimeSpan.FromMinutes(5));
        _caches["Token"].Entries.Should().BeEmpty();
        _caches["Default"].Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task Put_With_Specific_Options_Without_MaxTime_Inherits_The_Default_MaxTime_Should()
    {
        var (sut, container) = BuildSut();

        await sut.PutAsync("key", "value", OptionsNameOf(container, "Sql"));

        _caches["Sql"].Timeouts["key_tokencache"].Should().Be(TimeSpan.FromMinutes(40));
    }

    [Fact]
    public async Task Put_With_Specific_Options_Naming_An_Unknown_Cache_Uses_The_Default_Cache_And_Their_MaxTime_Should()
    {
        var (sut, container) = BuildSut();

        await sut.PutAsync("key", "value", OptionsNameOf(container, "Unknown"));

        _caches["Default"].Timeouts["key_tokencache"].Should().Be(TimeSpan.FromMinutes(15));
        _caches["Token"].Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task The_Same_Key_Is_Isolated_Between_Specific_Options_Should()
    {
        var (sut, container) = BuildSut();
        var redis = OptionsNameOf(container, "Redis");
        var sql = OptionsNameOf(container, "Sql");

        await sut.PutAsync("key", "redis-value", redis);
        await sut.PutAsync("key", "sql-value", sql);
        await sut.PutAsync("key", "default-value");

        (await sut.GetAsync<string>("key", redis)).Should().Be("redis-value");
        (await sut.GetAsync<string>("key", sql)).Should().Be("sql-value");
        (await sut.GetAsync<string>("key")).Should().Be("default-value");
    }

    [Fact]
    public async Task DeleteItem_With_Specific_Options_Removes_Only_Their_Entry_Should()
    {
        var (sut, container) = BuildSut();
        var redis = OptionsNameOf(container, "Redis");
        await sut.PutAsync("key", "redis-value", redis);
        await sut.PutAsync("key", "default-value");

        await sut.DeleteItemAsync("key", redis);

        (await sut.GetAsync<string>("key", redis)).Should().BeNull();
        (await sut.GetAsync<string>("key")).Should().Be("default-value");
    }

    [Fact]
    public async Task Put_A_Token_With_Specific_Options_Is_Capped_By_Their_MaxTime_Or_The_Token_Expiry_Should()
    {
        var (sut, container) = BuildSut();
        var redis = OptionsNameOf(container, "Redis");

        // Expires after the 5 minutes of the Redis options: MaxTime applies.
        await sut.PutAsync("long", new TokenInfo("Bearer", "t", DateTime.UtcNow.AddHours(1)), redis);
        // Expires before: the token expiry applies.
        await sut.PutAsync("short", new TokenInfo("Bearer", "t", DateTime.UtcNow.AddMinutes(2)), redis);

        _caches["Redis"].Timeouts["long_tokencache"].Should().Be(TimeSpan.FromMinutes(5));
        _caches["Redis"].Timeouts["short_tokencache"].Should().BeGreaterThan(TimeSpan.FromMinutes(1)).And.BeLessThanOrEqualTo(TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task Put_An_Expired_Token_With_Specific_Options_Removes_Their_Entry_Only_Should()
    {
        var (sut, container) = BuildSut();
        var redis = OptionsNameOf(container, "Redis");
        await sut.PutAsync("key", new TokenInfo("Bearer", "old", DateTime.UtcNow.AddHours(1)), redis);
        await sut.PutAsync("key", new TokenInfo("Bearer", "default", DateTime.UtcNow.AddHours(1)));

        await sut.PutAsync("key", new TokenInfo("Bearer", "expired", DateTime.UtcNow.AddMinutes(-1)), redis);

        (await sut.GetAsync<TokenInfo>("key", redis)).Should().BeNull();
        (await sut.GetAsync<TokenInfo>("key"))!.Token.Should().Be("default");
    }

    [Fact]
    public async Task An_Entry_Without_TokenCache_Uses_The_Shared_ClientTokensCache_Should()
    {
        var (sut, container) = BuildSut(withSharedSection: true);

        var optionsName = OptionsNameOf(container, "Plain");
        await sut.PutAsync("key", "value", optionsName);

        optionsName.Should().Be(ClientTokensExtension.TokenCacheOptionsName);
        _caches["Shared"].Timeouts["key_tokencache"].Should().Be(TimeSpan.FromMinutes(20));
        _caches["Token"].Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task An_Entry_Without_MaxTime_Inherits_The_Shared_MaxTime_Before_The_Default_One_Should()
    {
        var (sut, container) = BuildSut(withSharedSection: true);

        await sut.PutAsync("key", "value", OptionsNameOf(container, "Sql"));

        _caches["Sql"].Timeouts["key_tokencache"].Should().Be(TimeSpan.FromMinutes(20));
    }

    [Fact]
    public async Task An_Entry_Without_TokenCache_And_Without_Shared_Section_Uses_The_Default_Options_Should()
    {
        var (sut, container) = BuildSut();

        var optionsName = OptionsNameOf(container, "Plain");
        await sut.PutAsync("key", "value", optionsName);

        optionsName.Should().BeNull();
        _caches["Token"].Timeouts["key_tokencache"].Should().Be(TimeSpan.FromMinutes(40));
    }

    [Fact]
    public async Task An_Unregistered_OptionsName_Uses_The_Default_Options_Should()
    {
        var (sut, _) = BuildSut();

        // A typo: no TokenCacheOptions are registered under this name.
        await sut.PutAsync("key", "value", "Reddis");

        // Without the check, the class defaults (default cache, 50 minutes) would be used.
        _caches["Token"].Timeouts["key_tokencache"].Should().Be(TimeSpan.FromMinutes(40));
        _caches["Default"].Entries.Should().BeEmpty();
        (await sut.GetAsync<string>("key", "Reddis")).Should().Be("value");
        (await sut.GetAsync<string>("key")).Should().Be("value");
    }

    [Fact]
    public async Task Options_Registered_With_Configure_And_An_Action_Are_Used_Should()
    {
        var (sut, _) = BuildSut(configure: (services, _) =>
            services.Configure<TokenCacheOptions>("Custom", o =>
            {
                o.CacheName = "Redis";
                o.MaxTime = TimeSpan.FromMinutes(7);
            }));

        await sut.PutAsync("key", "value", "Custom");

        _caches["Redis"].Timeouts["key_tokencache"].Should().Be(TimeSpan.FromMinutes(7));
    }

    [Fact]
    public async Task Options_Registered_With_Configure_And_A_Section_Are_Used_Should()
    {
        var (sut, _) = BuildSut(configure: (services, _) =>
        {
            var section = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["CacheName"] = "Sql", ["MaxTime"] = "00:03:00" })
                .Build();
            services.Configure<TokenCacheOptions>("FromSection", section);
        });

        await sut.PutAsync("key", "value", "FromSection");

        _caches["Sql"].Timeouts["key_tokencache"].Should().Be(TimeSpan.FromMinutes(3));
    }

    [Fact]
    public async Task ConfigureAll_Is_Not_A_Registration_Of_A_Name_Should()
    {
        var (sut, _) = BuildSut(configure: (services, _) =>
            services.ConfigureAll<TokenCacheOptions>(o => o.MaxTime = TimeSpan.FromMinutes(1)));

        await sut.PutAsync("key", "value", "NotRegistered");

        // The default options are used (ConfigureAll also applies to them).
        _caches["Token"].Timeouts["key_tokencache"].Should().Be(TimeSpan.FromMinutes(1));
    }

    /// <summary>An in-memory <see cref="ICache"/> recording the timeout of each entry.</summary>
    private sealed class FakeCache : ICache
    {
        public Dictionary<string, object?> Entries { get; } = new(StringComparer.Ordinal);
        public Dictionary<string, TimeSpan> Timeouts { get; } = new(StringComparer.Ordinal);

        public void Put<T>(string key, TimeSpan timeout, T value, bool isSlided = false)
        {
            Entries[key] = value;
            Timeouts[key] = timeout;
        }

        public TValue? Get<TValue>(string key) => Entries.TryGetValue(key, out var value) ? (TValue?)value : default;

        public bool Remove(string key)
        {
            Timeouts.Remove(key);
            return Entries.Remove(key);
        }

        public void Put<T>(string key, T value) => Entries[key] = value;

        public bool TryGetValue<TValue>(string key, out TValue? value)
        {
            value = Get<TValue>(key);
            return Entries.ContainsKey(key);
        }

        public Task PutAsync<T>(string key, TimeSpan timeout, T value, bool isSlided = false, CancellationToken cancellation = default)
        {
            Put(key, timeout, value, isSlided);
            return Task.CompletedTask;
        }

        public Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellation = default) => Task.FromResult(Get<TValue>(key));

        public Task<bool> RemoveAsync(string key, CancellationToken cancellation = default) => Task.FromResult(Remove(key));

        // Not used by ApplicationCache.
        public void Initialize(string store) { }
        public Task PutAsync<T>(string key, T value, CancellationToken cancellation = default) => throw new NotSupportedException();
        public Task<TValue?> TryGetValueAsync<TValue>(string key, CancellationToken cancellation = default) => throw new NotSupportedException();
        public void Dispose() { }
    }
}
