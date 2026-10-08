using Arc4u.MongoDB;
using Arc4u.MongoDB.Configuration;
using Arc4u.MongoDB.Exceptions;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Compression;
using Xunit;

namespace Arc4u.UnitTest.Database;

[Trait("Category", "CI")]
public class MongoDBTests
{
    private readonly Fixture _fixture;

    public MongoDBTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void Test_MongoDB_Single_Server_Should()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:mongo"] = "mongodb://localhost:27017/DB1" })
            .Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        services.AddMongoDatabase<DatabaseDbContext>(configuration, "mongo");

        var app = services.BuildServiceProvider();

        var factory = app.GetService<IMongoClientFactory<DatabaseDbContext>>();
        var client = factory!.CreateClient();

        client.Settings.Server.Host.Should().Be("localhost");
        client.Settings.Server.Port.Should().Be(27017);
        client.Settings.Servers.Should().HaveCount(1);
    }

    [Fact]
    public void Test_MongoDB_Cluster_Server_Should()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:mongo"] = "mongodb://localhost:27017,localhost:26000/DB2"
                }).Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        services.AddMongoDatabase<DatabaseDbContext>(configuration, "mongo");

        var app = services.BuildServiceProvider();

        var factory = app.GetService<IMongoClientFactory<DatabaseDbContext>>();
        var client = factory!.CreateClient();

        client.Settings.Servers.Should().HaveCount(2);
        client.Settings.Servers.First().Host.Should().Be("localhost");
        client.Settings.Servers.First().Port.Should().Be(27017);

        client.Settings.Servers.Last().Host.Should().Be("localhost");
        client.Settings.Servers.Last().Port.Should().Be(26000);
    }

    [Fact]
    public void Test_MongoDB_ContextBuilder_For_A_Specific_Collection_Should()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:mongo"] = "mongodb://localhost:27017/DB1" })
            .Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        services.AddMongoDatabase<DatabaseDbContext>(configuration, "mongo");

        var app = services.BuildServiceProvider();

        var factory = app.GetService<IMongoClientFactory<DatabaseDbContext>>();
        var client = factory!.GetCollection<Contract>("Contracts");
        client.Should().NotBeNull();
    }

    [Fact]
    public void Test_MongoDB_ContextBuilder_For_A_Non_Specific_Collection_Should_Fail()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:mongo"] = "mongodb://localhost:27017/DB1" })
            .Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        services.AddMongoDatabase<DatabaseDbContext>(configuration, "mongo");

        var app = services.BuildServiceProvider();

        var factory = app.GetService<IMongoClientFactory<DatabaseDbContext>>();

        var exception = Record.Exception(() => factory!.GetCollection<Contract>());

        exception.Should().BeOfType<TypeMappedToMoreThanOneCollectionException<Contract>>();
    }

    [Fact]
    public void Test_MongoDB_ContextBuilder_For_A_Non_Mapped_Type_Should_Fail()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:mongo"] = "mongodb://localhost:27017/DB1" })
            .Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        services.AddMongoDatabase<DatabaseDbContext>(configuration, "mongo");

        var app = services.BuildServiceProvider();

        var factory = app.GetService<IMongoClientFactory<DatabaseDbContext>>();

        var exception = Record.Exception(() => factory!.GetCollection<NotMapped>());

        exception.Should().BeOfType<TypeNotMappedToCollectionException>();
    }

    [Fact]
    public void Test_MongoDB_ConnectionString_Options_Should_Be_Applied()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:mongo"] = "mongodb://localhost:27017/DB1?directConnection=true&compressors=zlib&maxConnecting=5&appName=Arc4u" })
            .Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        services.AddMongoDatabase<DatabaseDbContext>(configuration, "mongo");

        var app = services.BuildServiceProvider();

        var factory = app.GetService<IMongoClientFactory<DatabaseDbContext>>();
        var client = factory!.CreateClient();

        client.Settings.DirectConnection.Should().BeTrue();
        client.Settings.Compressors.Should().ContainSingle(c => c.Type == CompressorType.Zlib);
        client.Settings.MaxConnecting.Should().Be(5);
        client.Settings.ApplicationName.Should().Be("Arc4u");
    }

    [Fact]
    public void Test_MongoDB_LoadBalanced_Option_Should_Be_Applied()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:mongo"] = "mongodb://localhost:27017/DB1?loadBalanced=true" })
            .Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        services.AddMongoDatabase<DatabaseDbContext>(configuration, "mongo");

        var app = services.BuildServiceProvider();

        var factory = app.GetService<IMongoClientFactory<DatabaseDbContext>>();
        var client = factory!.CreateClient();

        client.Settings.LoadBalanced.Should().BeTrue();
    }

    [Fact]
    public void Test_MongoDB_ConnectionString_With_Configure_Should_Apply_Both()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?> { ["ConnectionStrings:mongo"] = "mongodb://myserver:27017/DB1?maxConnecting=5" })
            .Build();

        IConfiguration configuration = new ConfigurationRoot(new List<IConfigurationProvider>(config.Providers));
        IServiceCollection services = new ServiceCollection();

        services.AddMongoDatabase<DatabaseDbContext>(configuration, "mongo");
        services.Configure<MongoClientSettings>("db1", settings => settings.ApplicationName = "Arc4u");

        var app = services.BuildServiceProvider();

        var factory = app.GetService<IMongoClientFactory<DatabaseDbContext>>();
        var client = factory!.CreateClient();

        client.Settings.Server.Host.Should().Be("myserver");
        client.Settings.MaxConnecting.Should().Be(5);
        client.Settings.ApplicationName.Should().Be("Arc4u");
    }

    [Fact]
    public void Test_MongoDB_Without_Settings_Should_Fail()
    {
        IServiceCollection services = new ServiceCollection();

        services.AddMongoDatabase<DatabaseDbContext>("DB1", settings => settings.Server = new MongoServerAddress("myserver", 27017));
        services.RemoveAll<IConfigureOptions<MongoClientSettings>>();

        var app = services.BuildServiceProvider();

        var factory = app.GetService<IMongoClientFactory<DatabaseDbContext>>();

        var exception = Record.Exception(() => factory!.CreateClient());

        exception.Should().BeOfType<MongoClientException>();
    }

    private sealed class Contract
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }

    private sealed class Company
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }

    private sealed class NotMapped
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
    }

    private sealed class DatabaseDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextBuilder context)
        {
            context.MapCollection("Contracts").With<Contract>();
            context.MapCollection("Contracts").With<Company>();
            context.MapCollection("Companies").With<Contract>();
        }
    }
}
