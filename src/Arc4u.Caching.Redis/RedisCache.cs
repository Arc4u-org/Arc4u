using System.Diagnostics.CodeAnalysis;
using Arc4u.Configuration.Redis;
using Arc4u.Dependency.Attribute;
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
    public override void Initialize([DisallowNull] string store) =>
        InitializeCore(store, "Redis", s =>
        {
            Name = s;

            var config = _options.Get(s);

            var redisOption = new RedisCacheOptions
            {
                InstanceName = config.InstanceName,
                Configuration = config.ConnectionString,
            };

            return (new StackExchangeRedis(redisOption), config.SerializerName);
        });

    /// <summary>Returns the name of the cache given to <see cref="Initialize(string)"/>.</summary>
    /// <returns>The name of the cache (empty before initialization).</returns>
    public override string ToString() => Name ?? string.Empty;
}
