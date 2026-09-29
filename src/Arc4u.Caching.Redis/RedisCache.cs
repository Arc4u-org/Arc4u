using System.Diagnostics.CodeAnalysis;
using Arc4u.Configuration.Redis;
using Arc4u.Dependency;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.Serializer;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchangeRedis = Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache;

namespace Arc4u.Caching.Redis;

/// <summary>
/// An <see cref="ICache"/> stored in a Redis server, resolved with the kind <c>Redis</c>.
/// The connection string and the instance name come from the <see cref="RedisCacheOption"/> named after the store.
/// </summary>
[Export("Redis", typeof(ICache))]
public class RedisCache : BaseDistributeCache<RedisCache>, ICache
{
    private string? Name { get; set; }

    private readonly ILogger<RedisCache> _logger;

    /// <summary>
    /// This is a named options => the configuration named instance for this instance will be taken during the intialize method. 
    /// </summary>
    private readonly IOptionsMonitor<RedisCacheOption> _options;

    /// <summary>Initializes a new instance of the <see cref="RedisCache"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="container">The service provider used to resolve the serializer.</param>
    /// <param name="options">The named options of the Redis caches.</param>
    public RedisCache(ILogger<RedisCache> logger, IServiceProvider container, IOptionsMonitor<RedisCacheOption> options) : base(logger, container)
    {
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Creates the underlying Redis cache from the options named <paramref name="store"/> and resolves the <see cref="IObjectSerialization"/>
    /// (the one registered with <see cref="RedisCacheOption.SerializerName"/> if any, otherwise the default one).
    /// When no serializer can be resolved the cache stays uninitialized and the operations throw <see cref="CacheNotInitializedException"/>.
    /// Calling it again on an initialized cache only logs a warning.
    /// </summary>
    /// <param name="store">The name of the cache, as declared in the configuration.</param>
    /// <exception cref="ArgumentException"><paramref name="store"/> is empty.</exception>
    public override void Initialize([DisallowNull] string store)
    {
        if (string.IsNullOrEmpty(store))
        {
            NotInitializedReason = "When initializing the Redis cache, the value of the store cannot be an empty string.";
            throw new ArgumentException(NotInitializedReason, nameof(store));
        }

        lock (_lock)
        {
            if (IsInitialized)
            {
                _logger.Technical().LogCacheIsAlreadyInitialized(store);
                return;
            }

            try
            {
                Name = store;

                var config = _options.Get(store);

                var redisOption = new RedisCacheOptions
                {
                    InstanceName = config.InstanceName,
                    Configuration = config.ConnectionString,
                };

                DistributeCache = new StackExchangeRedis(redisOption);

                if (!string.IsNullOrWhiteSpace(config.SerializerName))
                {
                    IsInitialized = Container.TryGetService<IObjectSerialization>(config.SerializerName!, out var serializerFactory);
                    if (IsInitialized)
                    {
                        SerializerFactory = serializerFactory!;
                    }
                }

                if (!IsInitialized)
                {
                    IsInitialized = Container.TryGetService<IObjectSerialization>(out var serializerFactory);
                    if (IsInitialized)
                    {
                        SerializerFactory = serializerFactory!;
                    }
                }

                if (!IsInitialized)
                {
                    NotInitializedReason = $"Redis Cache {store} is not initialized. An IObjectSerialization instance cannot be resolved via the Ioc.";

                    _logger.Technical().LogError(NotInitializedReason, store);

                    return;
                }

                _logger.Technical().LogCacheIsInitialized(store);
            }
            catch (Exception ex)
            {
                NotInitializedReason = $"Redis Cache {store} is not initialized. With exception: {ex.Message}";
                throw;
            }

        }
    }

    /// <summary>Returns the name of the cache given to <see cref="Initialize(string)"/>.</summary>
    /// <returns>The name of the cache.</returns>
    /// <exception cref="InvalidOperationException">The cache has not been initialized.</exception>
    public override string ToString() => Name ?? throw new InvalidOperationException("The 'Name' property must not be null.");
}
