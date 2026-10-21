using System.Text.Json.Serialization;
using Arc4u.Configuration.Store;
using Arc4u.Dependency;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Arc4u.UnitTest.Configuration;

[Trait("Category", "CI")]
public class SectionStoreTests
{
    #region Monitor and logging

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SectionStoreService_Should_Start_And_Stop_With_Or_Without_AddILogger(bool addILogger)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (addILogger)
        {
            services.AddILogger();
        }
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddSectionStoreConfiguration(options => options.Add("Feature", 1)).Build());
        services.AddSectionStoreService(TimeSpan.FromMilliseconds(10));

        await using var serviceProvider = services.BuildServiceProvider();
        var hostedService = serviceProvider.GetServices<IHostedService>().Should().ContainSingle().Subject;

        var start = () => hostedService.StartAsync(CancellationToken.None);
        await start.Should().NotThrowAsync();

        var stop = () => hostedService.StopAsync(CancellationToken.None);
        await stop.Should().NotThrowAsync();
    }

    #endregion

    #region SectionEntity

    [Fact]
    public void SectionEntity_Constructor_Should_Rebuild_A_Persisted_Entity()
    {
        var original = new SectionEntity();
        original.SetValue(new FeatureOptions { Enabled = true, Limit = 42 });

        var rebuilt = new SectionEntity("Feature", original.Value);

        rebuilt.Key.Should().Be("Feature");
        rebuilt.Value.Should().Be(original.Value);
        rebuilt.GetValue<FeatureOptions>().Should().BeEquivalentTo(new FeatureOptions { Enabled = true, Limit = 42 });
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void SectionEntity_Constructor_Should_Reject_An_Invalid_Key(string? invalidKey)
    {
        var act = () => new SectionEntity(invalidKey!, "{}");

        act.Should().Throw<ArgumentException>().WithParameterName("key");
    }

    [Fact]
    public void SectionEntity_Constructor_Should_Reject_A_Null_Value()
    {
        var act = () => new SectionEntity("Feature", null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    [Fact]
    public void Custom_SectionStore_Should_Serve_The_Configuration()
    {
        var store = new DictionarySectionStore();
        var configuration = new ConfigurationBuilder()
            .AddSectionStoreConfiguration(options => options.Add("Feature", new FeatureOptions { Enabled = true, Limit = 5 }))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ISectionStore>(store);
        services.AddSectionStoreService();
        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.UseSectionStoreConfiguration();

        // the initial value is persisted as raw strings by the custom store...
        store.Rows.Should().ContainKey("Feature");

        // ... and a value changed in the store is read back through entities built with the public constructor.
        var changed = new SectionEntity();
        changed.SetValue(new FeatureOptions { Enabled = false, Limit = 7 });
        store.Rows["Feature"] = changed.Value;
        configuration.Reload();

        configuration.GetSection("Feature").Get<FeatureOptions>().Should().BeEquivalentTo(new FeatureOptions { Enabled = false, Limit = 7 });
    }

    [Fact]
    public void AddFromConfiguration_Should_Persist_The_Section_Read_By_The_Binding_Function()
    {
        var store = new DictionarySectionStore();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Feature:Enabled"] = "true", ["Feature:Limit"] = "5" })
            .AddSectionStoreConfiguration(options => options.AddFromConfiguration("Feature", section => section.Get<FeatureOptions>()))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ISectionStore>(store);
        services.AddSectionStoreService();
        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.UseSectionStoreConfiguration();

        store.Rows.Should().ContainKey("Feature");
        configuration.GetSection("Feature").Get<FeatureOptions>().Should().BeEquivalentTo(new FeatureOptions { Enabled = true, Limit = 5 });
    }

    [Fact]
    public void Add_Should_Read_The_Section_From_A_ConfigurationManager()
    {
        // WebApplicationBuilder.Configuration is a ConfigurationManager.
        var configuration = new ConfigurationManager();
        configuration.AddInMemoryCollection(new Dictionary<string, string?> { ["Feature:Enabled"] = "true", ["Feature:Limit"] = "5" });
        configuration.AddSectionStoreConfiguration(options => options.Add<FeatureOptions>("Feature"));

        configuration.GetSection("Feature").Get<FeatureOptions>().Should().BeEquivalentTo(new FeatureOptions { Enabled = true, Limit = 5 });
    }

    #endregion

    #region JsonTypeInfo overloads

    [Fact]
    public void SectionEntity_With_JsonTypeInfo_Should_Read_The_Persisted_Format()
    {
        // The format persisted by the previous versions: the value wrapped in a "Value" property.
        var entity = new SectionEntity("Feature", """{"Value":{"Enabled":true,"Limit":42}}""");

        entity.GetValue(SectionStoreTestsJsonContext.Default.FeatureOptions).Should().BeEquivalentTo(new FeatureOptions { Enabled = true, Limit = 42 });
    }

    [Fact]
    public void SectionEntity_With_JsonTypeInfo_Should_Write_The_Same_Format_As_Reflection()
    {
        var value = new FeatureOptions { Enabled = true, Limit = 42 };
        var withReflection = new SectionEntity();
        withReflection.SetValue(value);
        var withTypeInfo = new SectionEntity();
        withTypeInfo.SetValue(value, SectionStoreTestsJsonContext.Default.FeatureOptions);

        withTypeInfo.Value.Should().Be("""{"Value":{"Enabled":true,"Limit":42}}""");
        withTypeInfo.Value.Should().Be(withReflection.Value);
    }

    [Fact]
    public void SectionEntity_With_JsonTypeInfo_Should_Round_Trip_A_Primitive_And_Null()
    {
        var entity = new SectionEntity();

        entity.SetValue(12, SectionStoreTestsJsonContext.Default.Int32);
        entity.Value.Should().Be("""{"Value":12}""");
        entity.GetValue(SectionStoreTestsJsonContext.Default.Int32).Should().Be(12);

        entity.SetValue(null, SectionStoreTestsJsonContext.Default.FeatureOptions);
        entity.Value.Should().Be("""{"Value":null}""");
        entity.GetValue(SectionStoreTestsJsonContext.Default.FeatureOptions).Should().BeNull();
    }

    [Fact]
    public void AddFromConfiguration_With_JsonTypeInfo_Should_Persist_The_Section()
    {
        var store = new DictionarySectionStore();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Feature:Enabled"] = "true", ["Feature:Limit"] = "5" })
            .AddSectionStoreConfiguration(options => options
                .AddFromConfiguration("Feature", section => section.Get<FeatureOptions>(), SectionStoreTestsJsonContext.Default.FeatureOptions)
                .Add("Count", 3, SectionStoreTestsJsonContext.Default.Int32))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton<ISectionStore>(store);
        services.AddSectionStoreService();
        using var serviceProvider = services.BuildServiceProvider();

        serviceProvider.UseSectionStoreConfiguration();

        store.Rows["Feature"].Should().Be("""{"Value":{"Enabled":true,"Limit":5}}""");
        configuration.GetSection("Feature").Get<FeatureOptions>().Should().BeEquivalentTo(new FeatureOptions { Enabled = true, Limit = 5 });
        configuration["Count"].Should().Be("3");
    }

    [Fact]
    public async Task ISectionStoreExtensions_With_JsonTypeInfo_Should_Round_Trip_The_Value()
    {
        var store = new DictionarySectionStore();
        store.Rows["Feature"] = """{"Value":{"Enabled":true,"Limit":1}}""";
        var typeInfo = SectionStoreTestsJsonContext.Default.FeatureOptions;

        (await store.TrySetValueAsync("Feature", new FeatureOptions { Enabled = false, Limit = 7 }, typeInfo, CancellationToken.None)).Should().BeTrue();
        var (valid, value) = await store.TryGetValueAsync("Feature", typeInfo, CancellationToken.None);

        valid.Should().BeTrue();
        value.Should().BeEquivalentTo(new FeatureOptions { Enabled = false, Limit = 7 });
        (await store.TryGetValueAsync("Missing", typeInfo, CancellationToken.None)).Valid.Should().BeFalse();
    }

    #endregion

    #region DbContextSectionStore.Add

    [Fact]
    public void DbContextSectionStore_Add_Should_Insert_The_Sections()
    {
        using var serviceProvider = BuildEfServiceProvider(new FailingSaveChangesInterceptor { Fail = false });
        using var scope = serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISectionStore>();

        store.Add([CreateEntity("A", 1), CreateEntity("B", 2)]);

        store.GetAll().Select(entity => entity.Key).Should().BeEquivalentTo("A", "B");
        scope.ServiceProvider.GetRequiredService<TestDbContext>().ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public void DbContextSectionStore_Add_Should_Surface_An_Insert_Failure()
    {
        using var serviceProvider = BuildEfServiceProvider(new FailingSaveChangesInterceptor { Fail = true });
        using var scope = serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISectionStore>();

        var act = () => store.Add([CreateEntity("A", 1)]);

        act.Should().Throw<DbUpdateException>();
        // the failed insert must not linger in the context and be retried by a later SaveChanges.
        scope.ServiceProvider.GetRequiredService<TestDbContext>().ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public void DbContextSectionStore_Add_Should_Tolerate_A_Concurrent_Insert_Of_All_Keys()
    {
        var interceptor = new FailingSaveChangesInterceptor { Fail = false };
        using var serviceProvider = BuildEfServiceProvider(interceptor);
        Seed(serviceProvider, "A", "B");
        interceptor.Fail = true;

        using var scope = serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISectionStore>();

        var act = () => store.Add([CreateEntity("A", 1), CreateEntity("B", 2)]);

        act.Should().NotThrow();
        scope.ServiceProvider.GetRequiredService<TestDbContext>().ChangeTracker.Entries().Should().BeEmpty();
    }

    [Fact]
    public void DbContextSectionStore_Add_Should_Surface_The_Failure_When_Only_Some_Keys_Exist()
    {
        var interceptor = new FailingSaveChangesInterceptor { Fail = false };
        using var serviceProvider = BuildEfServiceProvider(interceptor);
        Seed(serviceProvider, "A");
        interceptor.Fail = true;

        using var scope = serviceProvider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISectionStore>();

        var act = () => store.Add([CreateEntity("A", 1), CreateEntity("B", 2)]);

        act.Should().Throw<DbUpdateException>();
    }

    [Fact]
    public void UseSectionStoreConfiguration_Should_Fail_When_The_Initial_Insert_Fails()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddSectionStoreConfiguration(options => options.Add("Feature", 1)).Build());
        AddEfStore(services, new FailingSaveChangesInterceptor { Fail = true });
        services.AddSectionStoreService();
        using var serviceProvider = services.BuildServiceProvider();

        var act = () => serviceProvider.UseSectionStoreConfiguration();

        act.Should().Throw<DbUpdateException>();
    }

    #endregion

    #region ResetAsync

    [Fact]
    public async Task Reload_After_ResetAsync_Should_Publish_The_Initial_Data()
    {
        var configuration = new ConfigurationBuilder()
            .AddSectionStoreConfiguration(options => options.Add("Feature", new FeatureOptions { Enabled = true, Limit = 5 }))
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        AddEfStore(services, new FailingSaveChangesInterceptor { Fail = false });
        services.AddSectionStoreService();
        using var serviceProvider = services.BuildServiceProvider();
        serviceProvider.UseSectionStoreConfiguration();

        using (var scope = serviceProvider.CreateScope())
        {
            var store = scope.ServiceProvider.GetRequiredService<ISectionStore>();
            (await store.TrySetValueAsync("Feature", new FeatureOptions { Enabled = false, Limit = 7 }, CancellationToken.None)).Should().BeTrue();
            configuration.Reload();
            configuration.GetSection("Feature").Get<FeatureOptions>().Should().BeEquivalentTo(new FeatureOptions { Enabled = false, Limit = 7 });

            await store.ResetAsync(CancellationToken.None);
        }

        // the first read after the reset re-seeds the store and must publish the initial data, not an empty set.
        configuration.Reload();

        configuration.GetSection("Feature").Get<FeatureOptions>().Should().BeEquivalentTo(new FeatureOptions { Enabled = true, Limit = 5 });
    }

    #endregion

    #region Extension class names

    [Fact]
    public void ServiceCollectionExtensions_Should_Have_Distinct_Full_Names()
    {
        // Before the fix, both assemblies declared Arc4u.Configuration.Store.ServiceCollectionExtensions: naming it here did not compile (CS0433).
        typeof(Arc4u.Configuration.Store.ServiceCollectionExtensions).Assembly.GetName().Name.Should().Be("Arc4u.Configuration.Store");
        typeof(DbContextSectionStoreServiceCollectionExtensions).Assembly.GetName().Name.Should().Be("Arc4u.Configuration.Store.EFCore");
    }

    #endregion

    #region Helpers

    public sealed class FeatureOptions
    {
        public bool Enabled { get; set; }
        public int Limit { get; set; }
    }

    private static SectionEntity CreateEntity<TValue>(string key, TValue value)
    {
        var entity = new SectionEntity(key, "null");
        entity.SetValue(value);
        return entity;
    }

    private static ServiceProvider BuildEfServiceProvider(FailingSaveChangesInterceptor interceptor)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        AddEfStore(services, interceptor);
        return services.BuildServiceProvider();
    }

    private static void AddEfStore(IServiceCollection services, FailingSaveChangesInterceptor interceptor)
    {
        var databaseName = Guid.NewGuid().ToString();
        services.AddDbContext<TestDbContext>(options => options.UseInMemoryDatabase(databaseName).AddInterceptors(interceptor));
        services.AddDbContextSectionStore<TestDbContext>();
    }

    private static void Seed(IServiceProvider serviceProvider, params string[] keys)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        context.Set<SectionEntity>().AddRange(keys.Select(key => CreateEntity(key, 0)));
        context.SaveChanges();
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SectionEntity>().Configure();
        }
    }

    /// <summary>
    /// Simulates a database error (constraint, permission...) on SaveChanges, after the store has tracked the entities.
    /// </summary>
    private sealed class FailingSaveChangesInterceptor : SaveChangesInterceptor
    {
        public bool Fail { get; set; }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            return Fail ? throw new DbUpdateException("Simulated insert failure.") : result;
        }
    }

    /// <summary>
    /// A custom store that keeps raw strings, as a non-EF store (file, key/value store...) would.
    /// </summary>
    private sealed class DictionarySectionStore : ISectionStore
    {
        public Dictionary<string, string> Rows { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void Add(IEnumerable<SectionEntity> entities)
        {
            foreach (var entity in entities)
            {
                Rows.Add(entity.Key, entity.Value);
            }
        }

        public List<SectionEntity> GetAll() => Rows.Select(row => new SectionEntity(row.Key, row.Value)).ToList();

        public Task<SectionEntity?> GetAsync(string key, CancellationToken cancellationToken) =>
            Task.FromResult(Rows.TryGetValue(key, out var value) ? new SectionEntity(key, value) : null);

        public Task UpdateAsync(IEnumerable<SectionEntity> entities, CancellationToken cancellationToken)
        {
            foreach (var entity in entities)
            {
                Rows[entity.Key] = entity.Value;
            }
            return Task.CompletedTask;
        }

        public Task ResetAsync(CancellationToken cancellationToken)
        {
            Rows.Clear();
            return Task.CompletedTask;
        }
    }

    #endregion
}

[JsonSerializable(typeof(SectionStoreTests.FeatureOptions))]
[JsonSerializable(typeof(int))]
internal sealed partial class SectionStoreTestsJsonContext : JsonSerializerContext
{
}
