using Arc4u.MongoDB;
using Arc4u.MongoDB.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration of a MongoDB database context in the service collection.
/// </summary>
public static class MongoDbConnection
{
    /// <summary>
    /// Registers the database context and its <see cref="IMongoClientFactory{TContext}"/>, with the client settings read from a connection string.
    /// </summary>
    /// <remarks>
    /// The database name is the one of the connection string. There is one context per database, and its collections are mapped in <see cref="DbContext"/>.
    /// The settings are registered as named options, the name being the database name in lower case. The context is created once and registered as a singleton.
    /// </remarks>
    /// <typeparam name="TContext">The type of the database context.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration holding the connection string.</param>
    /// <param name="ConnectionStringKey">The name of the connection string in the <c>ConnectionStrings</c> section.</param>
    /// <example>
    /// <code>
    /// services.AddMongoDatabase&lt;ShopContext&gt;(configuration, "ShopDb");
    /// </code>
    /// </example>
    public static void AddMongoDatabase<TContext>(this IServiceCollection services, IConfiguration configuration, string ConnectionStringKey) where TContext : DbContext, new()
    {
        var ConnectionString = configuration.GetConnectionString(ConnectionStringKey);

        var mongoUrl = new MongoUrl(ConnectionString);

        services.Configure<MongoClientSettings>(mongoUrl.DatabaseName.ToLowerInvariant(), options =>
        {
            var c = MongoClientSettings.FromConnectionString(configuration.GetConnectionString(ConnectionStringKey));
            options.AllowInsecureTls = c.AllowInsecureTls;
            options.ApplicationName = c.ApplicationName;
            options.AutoEncryptionOptions = c.AutoEncryptionOptions;
            options.ClusterConfigurator = c.ClusterConfigurator;
            options.ConnectTimeout = c.ConnectTimeout;
            options.Credential = c.Credential;
            options.HeartbeatInterval = c.HeartbeatInterval;
            options.IPv6 = c.IPv6;
            options.LocalThreshold = c.LocalThreshold;
            options.MaxConnectionIdleTime = c.MaxConnectionIdleTime;
            options.MaxConnectionLifeTime = c.MaxConnectionLifeTime;
            options.MaxConnectionPoolSize = c.MaxConnectionPoolSize;
            options.MinConnectionPoolSize = c.MinConnectionPoolSize;
            options.ReadConcern = c.ReadConcern;
            options.ReadEncoding = c.ReadEncoding;
            options.ReadPreference = c.ReadPreference;
            options.ReplicaSetName = c.ReplicaSetName;
            options.RetryReads = c.RetryReads;
            options.RetryWrites = c.RetryWrites;
            options.Scheme = c.Scheme;
            options.Servers = c.Servers;
            options.ServerSelectionTimeout = c.ServerSelectionTimeout;
            options.SocketTimeout = c.SocketTimeout;
            options.SslSettings = c.SslSettings;
            options.UseTls = c.UseTls;
            options.WaitQueueTimeout = c.WaitQueueTimeout;
            options.WriteConcern = c.WriteConcern;
            options.WriteEncoding = c.WriteEncoding;
        });

        var contextBuilder = new DbContextBuilder(services, mongoUrl.DatabaseName);

        var dbContext = new TContext();

        services.TryAddSingleton(typeof(TContext), (provider) => dbContext);
        services.TryAddSingleton(typeof(IMongoClientFactory<TContext>), typeof(DefaultMongoClientFactory<TContext>));

        dbContext.Configure(contextBuilder);
    }

    /// <summary>
    /// Registers the database context and its <see cref="IMongoClientFactory{TContext}"/>, with the client settings given by a delegate.
    /// </summary>
    /// <remarks>The settings are registered as named options, the name being the database name in lower case. The context is created once and registered as a singleton.</remarks>
    /// <typeparam name="TContext">The type of the database context.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="databaseName">The name of the database.</param>
    /// <param name="options">Configures the <see cref="MongoClientSettings"/>.</param>
    /// <example>
    /// <code>
    /// services.AddMongoDatabase&lt;ShopContext&gt;("shop", settings =&gt; settings.Server = new MongoServerAddress("localhost", 27017));
    /// </code>
    /// </example>
    public static void AddMongoDatabase<TContext>(this IServiceCollection services, string databaseName, Action<MongoClientSettings> options) where TContext : DbContext, new()
    {
        services.Configure<MongoClientSettings>(databaseName.ToLowerInvariant(), options);

        var contextBuilder = new DbContextBuilder(services, databaseName);

        var dbContext = new TContext();

        services.TryAddSingleton(typeof(TContext), (provider) => dbContext);
        services.TryAddSingleton(typeof(IMongoClientFactory<TContext>), typeof(DefaultMongoClientFactory<TContext>));

        dbContext.Configure(contextBuilder);
    }
}
