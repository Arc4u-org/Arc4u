using Arc4u.MongoDB;
using Arc4u.MongoDB.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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
    /// <param name="connectionStringKey">The name of the connection string in the <c>ConnectionStrings</c> section.</param>
    /// <example>
    /// <code>
    /// services.AddMongoDatabase&lt;ShopContext&gt;(configuration, "ShopDb");
    /// </code>
    /// </example>
    public static void AddMongoDatabase<TContext>(this IServiceCollection services, IConfiguration configuration, string connectionStringKey) where TContext : DbContext, new()
    {
        var connectionString = configuration.GetConnectionString(connectionStringKey);

        var mongoUrl = new MongoUrl(connectionString);

        // The settings are created from the connection string by MongoClientSettingsFactory, so every option the driver parses is kept.
        GetConnectionStrings(services)[mongoUrl.DatabaseName.ToLowerInvariant()] = connectionString!;
        services.AddOptions();
        services.TryAddSingleton<IOptionsFactory<MongoClientSettings>, MongoClientSettingsFactory>();

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

    private static MongoConnectionStrings GetConnectionStrings(IServiceCollection services)
    {
        if (services.FirstOrDefault(d => d.ServiceType == typeof(MongoConnectionStrings))?.ImplementationInstance is MongoConnectionStrings connectionStrings)
        {
            return connectionStrings;
        }

        connectionStrings = new MongoConnectionStrings();
        services.AddSingleton(connectionStrings);
        return connectionStrings;
    }
}
