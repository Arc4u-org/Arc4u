using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arc4u.Caching;
using Arc4u.Caching.Dapr;
using Arc4u.Caching.Memory;
using Arc4u.Configuration.Dapr;
using Arc4u.Configuration.Memory;
using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Arc4u.Serializer;
using AwesomeAssertions;
using Dapr.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arc4u.UnitTest.Caching;

/// <summary>
/// The behaviour every <see cref="ICache"/> must share, whatever the provider (issue #243).
/// The memory cache runs for real; the Dapr cache runs on a mocked <see cref="DaprClient"/>.
/// </summary>
[Trait("Category", "CI")]
public class CacheContractTests
{
    private static MemoryCache CreateMemoryCache(long sizeLimitInMB = 10, IObjectSerialization? serializer = null)
    {
        IServiceCollection services = new ServiceCollection();
        services.AddMemoryCache("Store", o => o.SizeLimitInMB = sizeLimitInMB);
        if (serializer is null)
        {
            services.AddTransient<IObjectSerialization, JsonSerialization>();
        }
        else
        {
            services.AddSingleton(serializer);
        }

        var serviceProvider = services.BuildServiceProvider();

        var logger = new Mock<ILoggerWrapper<MemoryCache>>();
        logger.Setup(m => m.SetContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Type?>())).Returns(logger.Object);

        var cache = new MemoryCache(logger.Object, serviceProvider, serviceProvider.GetRequiredService<IOptionsMonitor<MemoryCacheOption>>());
        cache.Initialize("Store");
        return cache;
    }

    private static DaprCache CreateDaprCache(DaprClient client)
    {
        var cache = new DaprCache(NullLogger<DaprCache>.Instance, Mock.Of<IOptionsMonitor<DaprCacheOption>>());

        // The Dapr client is built by Initialize from the sidecar environment; inject a mock instead.
        typeof(DaprCache).GetField("_daprClient", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(cache, client);
        typeof(DaprCache).GetField("_storeName", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(cache, "store");
        return cache;
    }

    private static Mock<DaprClient> CreateDaprClient()
    {
        var client = new Mock<DaprClient>();
        client.SetupGet(c => c.JsonSerializerOptions).Returns(new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return client;
    }

    [Fact]
    public async Task MemoryPutShouldStoreDefaultValuesWithEveryOverload()
    {
        using var cache = CreateMemoryCache();

        cache.Put("sync", 0);
        await cache.PutAsync("async", false);
        cache.Put("syncTimeout", TimeSpan.FromMinutes(1), 0);
        await cache.PutAsync("asyncTimeout", TimeSpan.FromMinutes(1), false);

        cache.TryGetValue<int>("sync", out var sync).Should().BeTrue();
        sync.Should().Be(0);
        cache.TryGetValue<bool>("async", out _).Should().BeTrue();
        cache.TryGetValue<int>("syncTimeout", out _).Should().BeTrue();
        cache.TryGetValue<bool>("asyncTimeout", out _).Should().BeTrue();
    }

    [Fact]
    public async Task MemoryPutShouldRejectNullWithEveryOverload()
    {
        using var cache = CreateMemoryCache();

        FluentActions.Invoking(() => cache.Put<string?>("k", null)).Should().Throw<ArgumentNullException>().WithParameterName("value");
        FluentActions.Invoking(() => cache.Put<string?>("k", TimeSpan.FromMinutes(1), null)).Should().Throw<ArgumentNullException>().WithParameterName("value");
        await FluentActions.Awaiting(() => cache.PutAsync<string?>("k", null)).Should().ThrowAsync<ArgumentNullException>().WithParameterName("value");
        await FluentActions.Awaiting(() => cache.PutAsync<string?>("k", TimeSpan.FromMinutes(1), null)).Should().ThrowAsync<ArgumentNullException>().WithParameterName("value");
    }

    [Fact]
    public void MemoryTryGetValueShouldReturnFalseForAMissingKey()
    {
        using var cache = CreateMemoryCache();

        cache.TryGetValue<int>("missing", out var number).Should().BeFalse();
        number.Should().Be(0);
        cache.TryGetValue<string>("missing", out var text).Should().BeFalse();
        text.Should().BeNull();
    }

    [Fact]
    public async Task MemoryPutShouldThrowWhenTheCacheIsFull()
    {
        using var cache = CreateMemoryCache(sizeLimitInMB: 1);
        cache.Put("k", "small");

        var tooLarge = new string('x', 2 * 1024 * 1024);

        FluentActions.Invoking(() => cache.Put("k", tooLarge)).Should().Throw<DataCacheException>().WithMessage("*size limit*");
        await FluentActions.Awaiting(() => cache.PutAsync("k", tooLarge)).Should().ThrowAsync<DataCacheException>().WithMessage("*size limit*");
    }

    [Fact]
    public async Task MemoryPutShouldWrapErrorsInDataCacheException()
    {
        var serializer = new Mock<IObjectSerialization>();
        var error = new InvalidOperationException("boom");
        serializer.Setup(s => s.Serialize(It.IsAny<string>())).Throws(error);
        using var cache = CreateMemoryCache(serializer: serializer.Object);

        FluentActions.Invoking(() => cache.Put("k", "v")).Should().Throw<DataCacheException>().WithInnerException<InvalidOperationException>();
        (await FluentActions.Awaiting(() => cache.PutAsync("k", TimeSpan.FromMinutes(1), "v")).Should().ThrowAsync<DataCacheException>())
            .Which.InnerException.Should().BeSameAs(error);
    }

    [Fact]
    public void DaprTryGetValueShouldReturnFalseForAMissingKey()
    {
        var client = CreateDaprClient();
        client.Setup(c => c.GetStateAsync<JsonElement>("store", "missing", It.IsAny<ConsistencyMode?>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(default(JsonElement));
        using var cache = CreateDaprCache(client.Object);

        cache.TryGetValue<int>("missing", out var number).Should().BeFalse();
        number.Should().Be(0);
        cache.TryGetValue<string>("missing", out var text).Should().BeFalse();
        text.Should().BeNull();
    }

    [Fact]
    public void DaprTryGetValueShouldReturnTrueForAStoredDefaultValue()
    {
        var client = CreateDaprClient();
        client.Setup(c => c.GetStateAsync<JsonElement>("store", "zero", It.IsAny<ConsistencyMode?>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(JsonDocument.Parse("0").RootElement);
        using var cache = CreateDaprCache(client.Object);

        cache.TryGetValue<int>("zero", out var number).Should().BeTrue();
        number.Should().Be(0);
        cache.Get<int>("zero").Should().Be(0);
    }

    [Fact]
    public async Task DaprPutShouldRejectNull()
    {
        using var cache = CreateDaprCache(CreateDaprClient().Object);

        FluentActions.Invoking(() => cache.Put<string?>("k", null)).Should().Throw<ArgumentNullException>().WithParameterName("value");
        await FluentActions.Awaiting(() => cache.PutAsync<string?>("k", TimeSpan.FromMinutes(1), null)).Should().ThrowAsync<ArgumentNullException>().WithParameterName("value");
    }

    [Fact]
    public async Task DaprShouldWrapErrorsInDataCacheException()
    {
        var client = CreateDaprClient();
        var error = new InvalidOperationException("sidecar down");
        client.Setup(c => c.SaveStateAsync("store", "k", It.IsAny<string>(), It.IsAny<StateOptions?>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(error);
        client.Setup(c => c.GetStateAsync<JsonElement>("store", "k", It.IsAny<ConsistencyMode?>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(error);
        using var cache = CreateDaprCache(client.Object);

        FluentActions.Invoking(() => cache.Put("k", "v")).Should().Throw<DataCacheException>().WithInnerException<InvalidOperationException>();
        await FluentActions.Awaiting(() => cache.PutAsync("k", "v")).Should().ThrowAsync<DataCacheException>();
        FluentActions.Invoking(() => cache.Get<string>("k")).Should().Throw<DataCacheException>();
        await FluentActions.Awaiting(() => cache.GetAsync<string>("k")).Should().ThrowAsync<DataCacheException>();
    }

    [Fact]
    public async Task DaprGetShouldUseTheMetadataOfTheClientOptions()
    {
        // Only the source-generated metadata is available: a type outside the context cannot be read.
        var client = new Mock<DaprClient>();
        client.SetupGet(c => c.JsonSerializerOptions).Returns(new JsonSerializerOptions { TypeInfoResolver = DaprTestJsonContext.Default });
        client.Setup(c => c.GetStateAsync<JsonElement>("store", "k", It.IsAny<ConsistencyMode?>(), It.IsAny<IReadOnlyDictionary<string, string>>(), It.IsAny<CancellationToken>()))
              .ReturnsAsync(JsonDocument.Parse("{\"Name\":\"a\",\"Count\":2}").RootElement);
        using var cache = CreateDaprCache(client.Object);

        (await cache.GetAsync<DaprTestValue>("k")).Should().Be(new DaprTestValue("a", 2));
        FluentActions.Invoking(() => cache.Get<Dictionary<string, int>>("k")).Should().Throw<DataCacheException>().WithInnerException<NotSupportedException>();
    }

    [Fact]
    public void DaprInitializeShouldGiveTheJsonOptionsToTheClient()
    {
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web) { TypeInfoResolver = DaprTestJsonContext.Default };
        var options = new Mock<IOptionsMonitor<DaprCacheOption>>();
        options.Setup(o => o.Get("Shared")).Returns(new DaprCacheOption { Name = "statestore", JsonSerializerOptions = jsonOptions });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddILogger();
        using var serviceProvider = services.BuildServiceProvider();
        using var cache = new DaprCache(serviceProvider.GetRequiredService<ILogger<DaprCache>>(), options.Object);

        cache.Initialize("Shared");

        var client = (DaprClient)typeof(DaprCache).GetField("_daprClient", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(cache)!;
        client.JsonSerializerOptions.PropertyNamingPolicy.Should().Be(JsonNamingPolicy.CamelCase);
        client.JsonSerializerOptions.GetTypeInfo(typeof(DaprTestValue)).Should().NotBeNull();
        // The JsonElement read first by the cache is added to the metadata of the application.
        client.JsonSerializerOptions.GetTypeInfo(typeof(JsonElement)).Should().NotBeNull();
    }

    [Fact]
    public void AddDaprCacheShouldKeepTheJsonOptions()
    {
        var jsonOptions = new JsonSerializerOptions { TypeInfoResolver = DaprTestJsonContext.Default };
        var services = new ServiceCollection();
        services.AddDaprCache("Shared", o =>
        {
            o.Name = "statestore";
            o.JsonSerializerOptions = jsonOptions;
        });

        var option = services.BuildServiceProvider().GetRequiredService<IOptionsMonitor<DaprCacheOption>>().Get("Shared");

        option.Name.Should().Be("statestore");
        option.JsonSerializerOptions.Should().BeSameAs(jsonOptions);
    }
}

public sealed record DaprTestValue(string Name, int Count);

[JsonSerializable(typeof(DaprTestValue))]
internal sealed partial class DaprTestJsonContext : JsonSerializerContext
{
}
