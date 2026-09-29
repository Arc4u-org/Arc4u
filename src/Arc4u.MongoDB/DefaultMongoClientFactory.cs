using Arc4u.MongoDB.Exceptions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Arc4u.MongoDB;

/// <summary>
/// Default <see cref="IMongoClientFactory{TContext}"/>: creates the client lazily, once, from the named <see cref="MongoClientSettings"/> of the database.
/// </summary>
/// <typeparam name="TContext">The type of the database context.</typeparam>
public class DefaultMongoClientFactory<TContext> : IMongoClientFactory<TContext> where TContext : DbContext
{
    /// <summary>
    /// Creates the factory.
    /// </summary>
    /// <param name="clientSettings">The named client settings; the name is the database name in lower case.</param>
    /// <param name="serviceProvider">Used to resolve the registered <typeparamref name="TContext"/>.</param>
    /// <exception cref="InvalidOperationException"><typeparamref name="TContext"/> is not registered.</exception>
    public DefaultMongoClientFactory(IOptionsMonitor<MongoClientSettings> clientSettings, IServiceProvider serviceProvider)
    {
        _clientSettings = clientSettings;
        _mongoContext = (TContext?)serviceProvider.GetService(typeof(TContext)) ?? throw new InvalidOperationException($"No registration exist for type {typeof(TContext).Name}");
    }

    IMongoDatabase? _database;
    IMongoClient? _client;
    private static readonly object _locker = new object();
    readonly IOptionsMonitor<MongoClientSettings> _clientSettings;
    readonly TContext _mongoContext;

    /// <inheritdoc/>
    /// <remarks>The client is created once from the named <see cref="MongoClientSettings"/> of the database. When no settings were registered under that name, the options system returns default settings, so the client silently targets <c>localhost:27017</c>.</remarks>
    public IMongoClient CreateClient()
    {
        if (null != _client)
        {
            return _client;
        }

        // one creation at a time => block here only. Few calls will arrive here.
        lock (_locker)
        {
            if (null != _client)
            {
                return _client;
            }

            var settings = _clientSettings.Get(_mongoContext.DatabaseName.ToLowerInvariant());
            if (null != settings)
            {
                _client = new MongoClient(settings);
                return _client;

            }
            throw new MongoClientException($"No mongo client settings defined for key {_mongoContext.DatabaseName}");
        }

    }

    private IMongoDatabase GetDatabase()
    {
        if (null != _database)
        {
            return _database;
        }

        // one creation at a time => block here only. Few calls will arrive here.
        lock (_locker)
        {
            if (null != _database)
            {
                return _database;
            }

            var client = CreateClient();

            _database = client.GetDatabase(_mongoContext.DatabaseName);
            return _database;
        }
    }

    /// <inheritdoc/>
    public IMongoCollection<TEntity> GetCollection<TEntity>(string collectionName)
    {
        var db = GetDatabase();

        return db.GetCollection<TEntity>(collectionName);
    }

    /// <inheritdoc/>
    /// <exception cref="TypeNotMappedToCollectionException"><typeparamref name="TEntity"/> is not mapped to a collection.</exception>
    /// <exception cref="TypeMappedToMoreThanOneCollectionException{TEntity}"><typeparamref name="TEntity"/> is mapped to several collections.</exception>
    public IMongoCollection<TEntity> GetCollection<TEntity>()
    {
        var type = typeof(TEntity);
        if (!_mongoContext.EntityCollectionTypes.TryGetValue(type, out var collectionNames))
        {
            throw new TypeNotMappedToCollectionException();
        }

        if (collectionNames.Count != 1)
        {
            throw new TypeMappedToMoreThanOneCollectionException<TEntity>(collectionNames.Count);
        }

        var db = GetDatabase();

        return db.GetCollection<TEntity>(collectionNames[0]);
    }
}
