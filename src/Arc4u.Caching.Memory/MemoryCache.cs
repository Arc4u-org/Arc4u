using Arc4u.Configuration.Memory;
using Arc4u.Dependency;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.Serializer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;

namespace Arc4u.Caching.Memory;

/// <summary>
/// An <see cref="ICache"/> stored in the memory of the process, resolved with the kind <c>Memory</c>. Values are serialized like in the other distributed caches.
/// The size limit and the compaction percentage come from the <see cref="MemoryCacheOption"/> named after the store.
/// </summary>
[Export("Memory", typeof(ICache))]
public class MemoryCache : BaseDistributeCache<MemoryCache>, ICache
{
    private string? Name { get; set; }

    private readonly ILogger<MemoryCache> _logger;
    /// <summary>
    /// This is a named options => the configuration named instance for this instance will be taken during the intialize method. 
    /// </summary>
    private readonly IOptionsMonitor<MemoryCacheOption> _options;

    /// <summary>Initializes a new instance of the <see cref="MemoryCache"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="container">The service provider used to resolve the serializer.</param>
    /// <param name="options">The named options of the memory caches.</param>
    public MemoryCache(ILogger<MemoryCache> logger, IServiceProvider container, IOptionsMonitor<MemoryCacheOption> options) : base(logger, container)
    {
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Creates the underlying memory cache from the options named <paramref name="store"/> and resolves the <see cref="IObjectSerialization"/>
    /// (the one registered with <see cref="MemoryCacheOption.SerializerName"/> if any, otherwise the default one).
    /// When no serializer can be resolved the cache stays uninitialized and the operations throw <see cref="CacheNotInitializedException"/>.
    /// Calling it again on an initialized cache only logs a warning.
    /// </summary>
    /// <param name="store">The name of the cache, as declared in the configuration.</param>
    /// <exception cref="ArgumentException"><paramref name="store"/> is empty.</exception>
    public override void Initialize([DisallowNull] string store)
    {
        if (string.IsNullOrEmpty(store))
        {
            NotInitializedReason = "When initializing the Memory cache, the value of the store cannot be an empty string.";
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

                if (config.SizeLimitInMB <= 0)
                {
                    _logger.LogCacheSizeLimit(store, config.SizeLimitInMB);
                }

                var option = new DistriOption(new MemoryDistributedCacheOptions
                {
                    CompactionPercentage = config.CompactionPercentage,
                    SizeLimit = config.SizeLimitInBytes
                });

                DistributeCache = new MemoryDistributedCache(option);

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
                    NotInitializedReason = $"Memory Cache {store} is not initialized. An IObjectSerialization instance cannot be resolved via the Ioc.";

                    _logger.Technical().LogError(NotInitializedReason, store);

                    return;
                }

                _logger.Technical().LogCacheIsInitialized(store);
            }
            catch (Exception ex)
            {
                NotInitializedReason = $"Memory Cache {store} is not initialized. With exception: {ex.Message}";
                throw;
            }
        }
    }

    /// <summary>Returns the name of the cache given to <see cref="Initialize(string)"/>.</summary>
    /// <returns>The name of the cache.</returns>
    /// <exception cref="InvalidOperationException">The cache has not been initialized.</exception>
    public override string ToString() => Name ?? throw new InvalidOperationException("The 'Name' property must not be null.");
}
