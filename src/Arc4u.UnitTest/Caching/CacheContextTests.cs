using System.Globalization;
using Arc4u.Caching;
using Arc4u.Caching.Memory;
using Arc4u.Configuration;
using Arc4u.Configuration.Dapr;
using Arc4u.Configuration.Memory;
using Arc4u.Configuration.Redis;
using Arc4u.Configuration.Sql;
using Arc4u.Diagnostics;
using Arc4u.Serializer;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Arc4u.UnitTest;

[Trait("Category", "CI")]
public class CacheContextTests
{
    private readonly Fixture _fixture;

    public CacheContextTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void ConfigSettingsShouldBe()
    {
        var cache = new Arc4u.Configuration.Caching { Default = "Volatile" };

        cache.Caches.Add(new CachingCache { IsAutoStart = true, Kind = "Memory", Name = "Volatile" });

        var memorySettings = _fixture.Build<MemoryCacheOption>().With(m => m.CompactionPercentage, 0.8).Create();
        ;

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Caching:Default"] = cache.Default,
                    ["Caching:Caches:0:Name"] = cache.Caches[0].Name,
                    ["Caching:Caches:0:Kind"] = cache.Caches[0].Kind,
                    ["Caching:Caches:0:IsAutoStart"] = cache.Caches[0].IsAutoStart.ToString(),
                    ["Caching:Caches:0:Settings"] = cache.Caches[0].IsAutoStart.ToString(),
                    ["Caching:Caches:0:Settings:SizeLimitInMB"] =
                        memorySettings.SizeLimitInMB.ToString(CultureInfo.InvariantCulture),
                    ["Caching:Caches:0:Settings:CompactionPercentage"] =
                        memorySettings.CompactionPercentage.ToString(CultureInfo.InvariantCulture),
                    ["Caching:Caches:0:Settings:SerializerName"] = memorySettings.SerializerName
                }).Build();

        var configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        var bindedCaching = new Arc4u.Configuration.Caching();
        configuration.GetSection("Caching").Bind(bindedCaching);

        bindedCaching.Should().NotBeNull();
        bindedCaching.Default.Should().Be(cache.Default);

        IServiceCollection services = new ServiceCollection();

        services.AddMemoryCache("option1", configuration, "Caching:Caches:0:Settings");

        var serviceProvider = services.BuildServiceProvider();

        // act
        var sut = serviceProvider.GetService<IOptionsMonitor<MemoryCacheOption>>()!.Get("option1");

        // assert
        sut.CompactionPercentage.Should().Be(memorySettings.CompactionPercentage);
        sut.SizeLimitInMB.Should().Be(memorySettings.SizeLimitInMB);
        sut.SerializerName.Should().Be(memorySettings.SerializerName);
    }

    [Fact]
    public void RegisterOptionSettingsShould()
    {
        // arrange
        var cache = new Arc4u.Configuration.Caching { Default = "Volatile" };

        cache.Caches.Add(new CachingCache { IsAutoStart = false, Kind = CacheContext.Redis, Name = "Performance" });
        cache.Caches.Add(new CachingCache { IsAutoStart = false, Kind = CacheContext.Sql, Name = "Compromize" });
        cache.Caches.Add(new CachingCache { IsAutoStart = true, Kind = CacheContext.Memory, Name = "Volatile" });
        cache.Caches.Add(new CachingCache { IsAutoStart = false, Kind = CacheContext.Dapr, Name = "Diversisty" });

        var memorySettings = _fixture.Build<MemoryCacheOption>().With(m => m.CompactionPercentage, 0.2).Create();
        var redisSettings = _fixture.Create<RedisCacheOption>();
        var sqlSettings = _fixture.Create<SqlCacheOption>();
        var daprSettings = _fixture.Create<DaprCacheOption>();

        IServiceCollection services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Caching:Default"] = cache.Default,
                    ["Caching:Caches:0:Name"] = cache.Caches[0].Name,
                    ["Caching:Caches:0:Kind"] = cache.Caches[0].Kind,
                    ["Caching:Caches:0:IsAutoStart"] = cache.Caches[0].IsAutoStart.ToString(),
                    ["Caching:Caches:0:Settings:ConnectionString"] = redisSettings.ConnectionString,
                    ["Caching:Caches:0:Settings:InstanceName"] = redisSettings.InstanceName,
                    ["Caching:Caches:0:Settings:SerializerName"] = redisSettings.SerializerName,
                    ["Caching:Caches:1:Name"] = cache.Caches[1].Name,
                    ["Caching:Caches:1:Kind"] = cache.Caches[1].Kind,
                    ["Caching:Caches:1:IsAutoStart"] = cache.Caches[1].IsAutoStart.ToString(),
                    ["Caching:Caches:1:Settings:SchemaName"] = sqlSettings.SchemaName,
                    ["Caching:Caches:1:Settings:TableName"] = sqlSettings.TableName,
                    ["Caching:Caches:1:Settings:ConnectionString"] = sqlSettings.ConnectionString,
                    ["Caching:Caches:1:Settings:SerializerName"] = sqlSettings.SerializerName,
                    ["Caching:Caches:2:Name"] = cache.Caches[2].Name,
                    ["Caching:Caches:2:Kind"] = cache.Caches[2].Kind,
                    ["Caching:Caches:2:IsAutoStart"] = cache.Caches[2].IsAutoStart.ToString(),
                    ["Caching:Caches:2:Settings:SizeLimitInMB"] =
                        memorySettings.SizeLimitInMB.ToString(CultureInfo.InvariantCulture),
                    ["Caching:Caches:2:Settings:CompactionPercentage"] =
                        memorySettings.CompactionPercentage.ToString(CultureInfo.InvariantCulture),
                    ["Caching:Caches:2:Settings:SerializerName"] = memorySettings.SerializerName,
                    ["Caching:Caches:3:Name"] = cache.Caches[3].Name,
                    ["Caching:Caches:3:Kind"] = cache.Caches[3].Kind,
                    ["Caching:Caches:3:Settings:Name"] = daprSettings.Name,
                    ["Caching:Caches:3:IsAutoStart"] = cache.Caches[3].IsAutoStart.ToString()
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        services.AddCacheContext(configuration);
        services.AddSingleton(configuration);

        var serviceProvider = services.BuildServiceProvider();

        // act
        var sutRedis = serviceProvider.GetService<IOptionsMonitor<RedisCacheOption>>()?.Get("Performance");
        var sutSql = serviceProvider.GetService<IOptionsMonitor<SqlCacheOption>>()?.Get("Compromize");
        var sutMemory = serviceProvider.GetService<IOptionsMonitor<MemoryCacheOption>>()?.Get("Volatile");
        var sutDapr = serviceProvider.GetService<IOptionsMonitor<DaprCacheOption>>()?.Get("Diversisty");
        // assert
        // redis
        sutRedis.Should().NotBeNull();
        sutRedis!.InstanceName.Should().Be(redisSettings.InstanceName);
        sutRedis.ConnectionString.Should().Be(redisSettings.ConnectionString);
        sutRedis.SerializerName.Should().Be(redisSettings.SerializerName);
        // sql

        sutSql!.ConnectionString.Should().Be(sqlSettings.ConnectionString);
        sutSql.TableName.Should().Be(sqlSettings.TableName);
        sutSql.SchemaName.Should().Be(sqlSettings.SchemaName);
        sutSql.SerializerName.Should().Be(sqlSettings.SerializerName);
        // memory
        sutMemory.Should().NotBeNull();
        sutMemory!.SerializerName.Should().Be(memorySettings.SerializerName);
        sutMemory.SizeLimitInMB.Should().Be(memorySettings.SizeLimitInMB);
        sutMemory.CompactionPercentage.Should().Be(memorySettings.CompactionPercentage);
        // dapr
        sutDapr.Should().NotBeNull();
        sutDapr!.Name.Should().Be(daprSettings.Name);
    }

    [Fact]
    public void InitializeAndUseOfMemoryShould()
    {
        var cache = new Arc4u.Configuration.Caching { Default = "Volatile" };

        cache.Caches.Add(new CachingCache { IsAutoStart = true, Kind = CacheContext.Memory, Name = "Volatile" });

        var memorySettings = new MemoryCacheOption { SizeLimitInMB = 10 };

        IServiceCollection services = new ServiceCollection();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Caching:Default"] = cache.Default,
                    ["Caching:Caches:0:Name"] = cache.Caches[0].Name,
                    ["Caching:Caches:0:Kind"] = cache.Caches[0].Kind,
                    ["Caching:Caches:0:IsAutoStart"] = cache.Caches[0].IsAutoStart.ToString(),
                    ["Caching:Caches:0:Settings:SizeLimitInMB"] =
                        memorySettings.SizeLimitInMB.ToString(CultureInfo.InvariantCulture),
                    ["Caching:Caches:0:Settings:CompactionPercentage"] =
                        memorySettings.CompactionPercentage.ToString(CultureInfo.InvariantCulture)
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        services.AddCacheContext(configuration);
        services.AddTransient<IObjectSerialization, JsonSerialization>();
        services.AddKeyedTransient<ICache, MemoryCache>(CacheContext.Memory);

        var mockLoggerWrapper = new Mock<ILoggerWrapper<CacheContext>>();
        mockLoggerWrapper.Setup(m => m.SetContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Type?>()))
            .Returns(mockLoggerWrapper.Object);

        var mockLoggerObject = mockLoggerWrapper.As<ILogger<CacheContext>>().Object;

        var mockLoggerWrapperMemoryCache = new Mock<ILoggerWrapper<MemoryCache>>();
        mockLoggerWrapperMemoryCache.Setup(m => m.SetContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Type?>()))
            .Returns(mockLoggerWrapperMemoryCache.Object);

        services.AddTransient<ILogger<MemoryCache>>(_ => mockLoggerWrapperMemoryCache.Object);

        var serviceProvider = services.BuildServiceProvider();

        var mockIOptions = _fixture.Freeze<Mock<IOptionsMonitor<MemoryCacheOption>>>();
        mockIOptions.Setup(m => m.Get("Volatile"))
            .Returns(serviceProvider.GetService<IOptionsMonitor<MemoryCacheOption>>()!.Get("Volatile"));

        _fixture.Inject<ILogger<CacheContext>>(mockLoggerObject);
        _fixture.Inject(configuration);
        _fixture.Inject<IServiceProvider>(serviceProvider);

        var sut = _fixture.Create<CacheContext>();

        var cacheInstance = sut["Volatile"];

        cacheInstance.Put("key", "value");

        cacheInstance.Get<string>("key").Should().Be("value");
    }

    [Theory]
    [InlineData("memory", CacheContext.Memory)]
    [InlineData("REDIS", CacheContext.Redis)]
    [InlineData("redissentinel", CacheContext.RedisSentinel)]
    [InlineData("sql", CacheContext.Sql)]
    [InlineData("DaPr", CacheContext.Dapr)]
    [InlineData("MyKind", "MyKind")]
    public void NormalizeKindShould(string kind, string expected)
    {
        CacheContext.NormalizeKind(kind).Should().Be(expected);
    }

    [Fact]
    public void AddCacheContextWithCustomSectionAndLowerCaseKindShould()
    {
        // arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["MyCaching:Default"] = "Volatile",
                    ["MyCaching:Caches:0:Name"] = "Volatile",
                    ["MyCaching:Caches:0:Kind"] = "memory",
                    ["MyCaching:Caches:0:IsAutoStart"] = "true",
                    ["MyCaching:Caches:0:Settings:SizeLimitInMB"] = "10"
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));

        IServiceCollection services = new ServiceCollection();
        services.AddSingleton(configuration);
        services.AddCacheContext(configuration, "MyCaching");
        services.AddMemoryCacheKind();
        services.AddMemoryCacheKind();
        services.AddTransient<IObjectSerialization, JsonSerialization>();

        var mockLoggerWrapper = new Mock<ILoggerWrapper<CacheContext>>();
        mockLoggerWrapper.Setup(m => m.SetContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Type?>()))
            .Returns(mockLoggerWrapper.Object);
        services.AddTransient<ILogger<CacheContext>>(_ => mockLoggerWrapper.Object);

        var mockLoggerWrapperMemoryCache = new Mock<ILoggerWrapper<MemoryCache>>();
        mockLoggerWrapperMemoryCache.Setup(m => m.SetContext(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Type?>()))
            .Returns(mockLoggerWrapperMemoryCache.Object);
        services.AddTransient<ILogger<MemoryCache>>(_ => mockLoggerWrapperMemoryCache.Object);

        var serviceProvider = services.BuildServiceProvider();

        // act
        var sut = serviceProvider.GetRequiredService<ICacheContext>();

        // assert
        services.Count(s => s.ServiceType == typeof(ICache)).Should().Be(1);
        sut.Exist("Volatile").Should().BeTrue();

        sut.Default.Put("key", "value");
        sut.Default.Get<string>("key").Should().Be("value");
    }
}
