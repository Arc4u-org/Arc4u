using Arc4u.Dependency;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arc4u.Caching;

/// <summary>
/// CacheContext is a helper class allowing to easily access the caches defined in the caching configuration section.
/// </summary>
[Export(typeof(ICacheContext)), Shared]
public class CacheContext : ICacheContext
{
    // Constant used to resolve the ICache interface to retrieve the MemoryCache implementation.
    /// <summary>The kind of cache implemented by the memory cache (<c>Arc4u.Caching.Memory</c>).</summary>
    public const string Memory = "Memory";

    // Constant used to resolve the ICache interface to retrieve the Sql implementation.
    /// <summary>The kind of cache implemented by the SQL Server cache (<c>Arc4u.Caching.Sql</c>).</summary>
    public const string Sql = "Sql";

    // Constant used to resolve the ICache interface to retrieve the Redis implementation.
    /// <summary>The kind of cache implemented by the Redis cache (<c>Arc4u.Caching.Redis</c>).</summary>
    public const string Redis = "Redis";

    // Constant used to resolve the ICache interface to retrieve the Redis Sentinel implementation.
    /// <summary>The kind of cache implemented by the Redis Sentinel cache (<c>Arc4u.Caching.Redis</c>).</summary>
    public const string RedisSentinel = "RedisSentinel";

    /// <summary>The kind of cache implemented by the Dapr state store cache (<c>Arc4u.Caching.Dapr</c>).</summary>
    public const string Dapr = "Dapr";

    private Dictionary<string, ICache> _caches = [];
    private Dictionary<string, string> _uninitializedCaches = [];
    private string _cacheConfigName = string.Empty;

    private static readonly object _lock = new();

    private readonly IServiceProvider _dependency;
    private readonly ILogger<CacheContext> _logger;

    /// <summary>
    /// Initialise the cache following the caching config section.
    /// The caches flagged <c>IsAutoStart</c> are created and initialized immediately; the others are registered and initialized on their first use.
    /// </summary>
    /// <param name="configuration">The configuration that contains the <c>Caching</c> section.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="dependency">The service provider used to resolve the <see cref="ICache"/> implementations by kind.</param>
    public CacheContext(IConfiguration configuration, ILogger<CacheContext> logger, IServiceProvider dependency)
        : this(configuration, logger, dependency, DefaultSectionName)
    {
    }

    /// <summary>
    /// Initialise the cache following the given caching config section.
    /// The caches flagged <c>IsAutoStart</c> are created and initialized immediately; the others are registered and initialized on their first use.
    /// </summary>
    /// <param name="configuration">The configuration that contains the caching section.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="dependency">The service provider used to resolve the <see cref="ICache"/> implementations by kind.</param>
    /// <param name="sectionName">The name of the caching configuration section.</param>
    public CacheContext(IConfiguration configuration, ILogger<CacheContext> logger, IServiceProvider dependency, string sectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        _logger = logger;
        _dependency = dependency;
        InitializeFromConfig(configuration, sectionName);
    }

    /// <summary>The default name of the caching configuration section.</summary>
    public const string DefaultSectionName = "Caching";

    /// <summary>
    /// Returns the canonical kind (<see cref="Memory"/>, <see cref="Redis"/>, <see cref="RedisSentinel"/>, <see cref="Sql"/> or <see cref="Dapr"/>) matching <paramref name="kind"/> case-insensitively.
    /// Any other kind is returned unchanged, so custom kinds are resolved with the exact key they are registered with.
    /// </summary>
    /// <param name="kind">The kind read from the configuration.</param>
    /// <returns>The key used to resolve the keyed <see cref="ICache"/> service.</returns>
    public static string NormalizeKind(string kind)
    {
        foreach (var known in KnownKinds)
        {
            if (string.Equals(known, kind, StringComparison.OrdinalIgnoreCase))
            {
                return known;
            }
        }

        return kind;
    }

    private static readonly string[] KnownKinds = [Memory, Redis, RedisSentinel, Sql, Dapr];

    /// <summary>
    /// Accessor to retrieve the default cache defined in the caching config section of the config file.
    /// </summary>
    /// <exception cref="InvalidOperationException">No cache is configured with the default name.</exception>
    public ICache Default
    {
        get { return this[_cacheConfigName]; }
    }

    private void InitializeFromConfig(IConfiguration configuration, string sectionName)
    {
        lock (_lock)
        {
            if (null == configuration)
            {
                return;
            }

            var config = new Configuration.Caching();
            configuration.GetSection(sectionName).Bind(config);

            if (null != config.Default && !string.IsNullOrWhiteSpace(config.Default))
            {
                _cacheConfigName = config.Default;

                if (null != config.Caches)
                {
                    // retrieve the caches and start if asked!
                    foreach (var cacheConfig in config.Caches)
                    {
                        var kind = NormalizeKind(cacheConfig.Kind);

                        if (cacheConfig.IsAutoStart)
                        {
                            if (_dependency.TryGetService<ICache>(kind, out var cache))
                            {
                                cache!.Initialize(cacheConfig.Name);

                                _caches.Add(cacheConfig.Name, cache);

                                _logger.Technical().LogNewCache(kind, cacheConfig.Name);
                            }
                            else
                            {
                                _logger.Technical().LogCacheKindIssue(kind);
                            }
                        }
                        else
                        {
                            _uninitializedCaches.Add(cacheConfig.Name, kind);
                            _logger.Technical().LogRegisterNewCache(kind, cacheConfig.Name);
                        }
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool Exist(string cacheName)
    {
        return _caches.ContainsKey(cacheName) || _uninitializedCaches.ContainsKey(cacheName);
    }

    /// <summary>
    /// Indexer to retrieve the <see cref="ICache"/> implementation based on the caching config section. The key is the cache name.
    /// </summary>
    /// <param name="cacheName">The name of the cache, as declared in the configuration.</param>
    /// <returns>An <see cref="ICache"/> implementation defined in the cache config section.</returns>
    /// <exception cref="InvalidOperationException">Throw the exception when the cache name does not exist in the caching config section.</exception>
    public ICache this[string cacheName]
    {
        get
        {
            // look inside the collection if the cache is initialized.
            if (_caches.TryGetValue(cacheName, out var value))
            {
                return value;
            }

            // look if the cache is not yet started and start it.
            lock (_lock)
            {
                // another thread may have initialized the cache while this one was waiting for the lock.
                if (_caches.TryGetValue(cacheName, out value))
                {
                    return value;
                }

                if (_uninitializedCaches.TryGetValue(cacheName, out var cacheKind))
                {
                    try
                    {
                        if (_dependency.TryGetService<ICache>(cacheKind, out var cache))
                        {
                            cache!.Initialize(cacheName);

                            // thread-safe update of the _cache and _uninitializedCaches is required because they can be accessed by other threads outside the lock
                            // (both in calls to this[string] as in Exists(string))
                            // To avoid locks, we use a copy+atomic exchange method. Since we are not dealing with a large number of items, this is still efficient.
                            var caches = new Dictionary<string, ICache>(_caches);
                            var uninitializedCaches = new Dictionary<string, string>(_uninitializedCaches);
                            caches.Add(cacheName, cache);
                            uninitializedCaches.Remove(cacheName);

                            // atomic exchange of object state.
                            Interlocked.Exchange(ref _caches, caches);
                            Interlocked.Exchange(ref _uninitializedCaches, uninitializedCaches);

                            return cache;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Technical().LogException(ex);
                    }
                }
            }

            throw new InvalidOperationException($"There is no cache configured with the name {cacheName}.");
        }
    }
}
