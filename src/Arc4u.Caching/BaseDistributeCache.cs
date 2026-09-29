using System.Diagnostics;
using Arc4u.Dependency;
using Arc4u.Diagnostics;
using Arc4u.Serializer;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Arc4u.Caching;

/// <summary>
/// Base class of the <see cref="ICache"/> implementations built on top of a Microsoft <see cref="IDistributedCache"/> (memory, Redis, SQL Server).
/// Values are serialized with an <see cref="IObjectSerialization"/> before being stored, and each operation is traced with an Arc4u <see cref="ActivitySource"/> when an
/// <see cref="IActivitySourceFactory"/> is registered. A derived class must set <see cref="DistributeCache"/> and <see cref="SerializerFactory"/> and
/// set <see cref="IsInitialized"/> in its <see cref="Initialize(string)"/> override; every data operation throws a <see cref="CacheNotInitializedException"/> until then.
/// </summary>
/// <typeparam name="T">The type of the derived cache, used as the category of the logger.</typeparam>
public abstract class BaseDistributeCache<T> : ICache
{
    private bool disposed;

    /// <summary>The entry options used by the <c>Put</c> overloads that have no timeout. By default no expiration is defined.</summary>
    protected DistributedCacheEntryOptions DefaultOption = new();
    private IObjectSerialization? _serializerFactory;
    private readonly ILogger<T> _logger;

    /// <summary>Gets or sets the underlying distributed cache.</summary>
    protected IDistributedCache? DistributeCache { get; set; }

    /// <summary>The lock used by derived classes to make <see cref="Initialize(string)"/> thread safe.</summary>
    protected readonly object _lock = new();
    /// <summary>Gets or sets a value indicating whether the cache is initialized and can be used.</summary>
    protected bool IsInitialized { get; set; }

    // The reason why the cache is not initialized, this will be used when an exception is thrown.
    /// <summary>Gets or sets the reason why the cache is not initialized; it is the message of the <see cref="CacheNotInitializedException"/> thrown by the operations.</summary>
    protected string NotInitializedReason { get; set; } = string.Empty;

    /// <summary>The service provider used to resolve the serializer.</summary>
    protected readonly IServiceProvider _container;

    /// <summary>Gets the service provider used to resolve the serializer.</summary>
    protected IServiceProvider Container => _container;
    private readonly ActivitySource? _activitySource;

    /// <summary>Initializes a new instance of the <see cref="BaseDistributeCache{T}"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="container">The service provider, used to resolve the <see cref="IObjectSerialization"/> and the optional <see cref="IActivitySourceFactory"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> or <paramref name="container"/> is <see langword="null"/>.</exception>
    protected BaseDistributeCache(ILogger<T> logger, IServiceProvider container)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(container);

        _container = container;
        _logger = logger;

        if (container.TryGetService<IActivitySourceFactory>(out var activitySourceFactory))
        {
            _activitySource = activitySourceFactory!.GetArc4u();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Disposes the underlying distributed cache when it is disposable.</summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>; <see langword="false"/> when called from a finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (!disposing)
            {
                return;
            }
            if (DistributeCache is IDisposable disposable)
            {
                disposable.Dispose();
            }

            disposed = true;
        }
    }

    /// <summary>Gets the value stored for the key and deserializes it.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <returns>The value, or the default of <typeparamref name="TValue"/> when the key does not exist.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    /// <exception cref="DataCacheException">The cache or the deserialization failed; the message is the one of the original exception.</exception>
    public TValue? Get<TValue>(string key)
    {
        CheckIfInitialized();

        try
        {
            byte[]? blob = [];
            using var activity = _activitySource?.StartActivity("Get from cache.", ActivityKind.Producer);
            activity?.SetTag("cacheKey", key);

            blob = DistributeCache?.Get(key);
            if (null == blob)
            {
                return default;
            }

            using var serializerActivity = _activitySource?.StartActivity("Deserialize.", ActivityKind.Producer);
            return SerializerFactory.Deserialize<TValue>(blob);
        }
        catch (Exception ex)
        {
            throw new DataCacheException(ex.Message);
        }
    }

    /// <summary>Asynchronously gets the value stored for the key and deserializes it.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="cancellation">A token to cancel the operation.</param>
    /// <returns>The value, or the default of <typeparamref name="TValue"/> when the key does not exist.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    /// <exception cref="DataCacheException">The cache or the deserialization failed; the message is the one of the original exception.</exception>
    public async Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellation = default)
    {
        CheckIfInitialized();

        try
        {
            byte[]? blob = null;
            using var activity = _activitySource?.StartActivity("Get from cache.", ActivityKind.Producer);
            activity?.SetTag("cacheKey", key);

            blob = DistributeCache is null ? null : await DistributeCache.GetAsync(key, cancellation).ConfigureAwait(false);
            if (null == blob)
            {
                return default;
            }

            using var serializerActivity = _activitySource?.StartActivity("Deserialize.", ActivityKind.Producer);
            return SerializerFactory.Deserialize<TValue>(blob);

        }
        catch (Exception ex)
        {
            throw new DataCacheException(ex.Message);
        }
    }

    /// <inheritdoc/>
    public virtual void Initialize(string store) { }

    /// <summary>Gets or sets the serializer used to convert the values to and from bytes.</summary>
    /// <exception cref="NullReferenceException">The getter is used before a serializer has been set.</exception>
    protected IObjectSerialization SerializerFactory
    {
        get
        {
            if (null == _serializerFactory)
            {
                _logger.Technical().LogError("A strong dependency on IObjectSerialization exists in Arc4u and distribute caching!");
            }

            return _serializerFactory ?? throw new NullReferenceException("No serializer exists!");
        }
        set => _serializerFactory = value;
    }

    /// <summary>Serializes the value and stores it with the <see cref="DefaultOption"/> (no expiration by default).</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="value">The value to store; it cannot be <see langword="null"/> or the default of its type.</param>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/> or the default value of <typeparamref name="TValue"/>.</exception>
    public void Put<TValue>(string key, TValue value)
    {
        CheckIfInitialized();

        if (EqualityComparer<TValue>.Default.Equals(value, default))
        {
            throw new ArgumentNullException(nameof(value));
        }

        using var activity = _activitySource?.StartActivity("Put to cache.", ActivityKind.Producer);
        byte[] blob = [];

        using (var serializerActivity = _activitySource?.StartActivity("Serialize.", ActivityKind.Producer))
        {
            blob = SerializerFactory.Serialize<TValue>(value);
        }

        activity?.SetTag("cacheKey", key);
        DistributeCache?.Set(key, blob, DefaultOption);
    }

    /// <summary>Asynchronously serializes the value and stores it with the <see cref="DefaultOption"/> (no expiration by default).</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="value">The value to store; it cannot be <see langword="null"/>.</param>
    /// <param name="cancellation">A token to cancel the operation.</param>
    /// <returns>A task that completes when the value is stored.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public async Task PutAsync<TValue>(string key, TValue value, CancellationToken cancellation = default)
    {
        CheckIfInitialized();

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        using var activity = _activitySource?.StartActivity("Put to cache.", ActivityKind.Producer);
        byte[] blob = [];

        using (var serializerActivity = _activitySource?.StartActivity("Serialize.", ActivityKind.Producer))
        {
            blob = SerializerFactory.Serialize<TValue>(value);
        }

        activity?.SetTag("cacheKey", key);

        if (null != DistributeCache)
        {
            await DistributeCache.SetAsync(key, blob, DefaultOption, cancellation).ConfigureAwait(false);
        }
    }

    /// <summary>Serializes the value and stores it with an absolute or a sliding expiration.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="timeout">The validity period of the value.</param>
    /// <param name="value">The value to store; it cannot be <see langword="null"/>.</param>
    /// <param name="isSlided"><see langword="true"/> to use a sliding expiration (the period restarts each time the value is read); <see langword="false"/> for an absolute expiration relative to now.</param>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public void Put<TValue>(string key, TimeSpan timeout, TValue value, bool isSlided = false)
    {
        CheckIfInitialized();

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        byte[] blob = [];

        using var activity = _activitySource?.StartActivity("Put to cache.", ActivityKind.Producer);
        using (var serializerActivity = _activitySource?.StartActivity("Serialize.", ActivityKind.Producer))
        {
            blob = SerializerFactory.Serialize<TValue>(value);
        }

        var dceo = new DistributedCacheEntryOptions();
        if (isSlided)
        {
            dceo.SetSlidingExpiration(timeout);
        }
        else
        {
            dceo.SetAbsoluteExpiration(timeout);
        }

        activity?.SetTag("cacheKey", key);

        DistributeCache?.Set(key, blob, dceo);
    }

    /// <summary>Asynchronously serializes the value and stores it with an absolute or a sliding expiration.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="timeout">The validity period of the value.</param>
    /// <param name="value">The value to store; it cannot be <see langword="null"/>.</param>
    /// <param name="isSlided"><see langword="true"/> to use a sliding expiration (the period restarts each time the value is read); <see langword="false"/> for an absolute expiration relative to now.</param>
    /// <param name="cancellation">A token to cancel the operation.</param>
    /// <returns>A task that completes when the value is stored.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public async Task PutAsync<TValue>(string key, TimeSpan timeout, TValue value, bool isSlided = false, CancellationToken cancellation = default)
    {
        CheckIfInitialized();

        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        byte[] blob = [];

        using var activity = _activitySource?.StartActivity("Put to cache.", ActivityKind.Producer);

        using (var serializerActivity = _activitySource?.StartActivity("Serialize.", ActivityKind.Producer))
        {
            blob = SerializerFactory.Serialize<TValue>(value);
        }

        var dceo = new DistributedCacheEntryOptions();
        if (isSlided)
        {
            dceo.SetSlidingExpiration(timeout);
        }
        else
        {
            dceo.SetAbsoluteExpiration(timeout);
        }

        activity?.SetTag("cacheKey", key);

        if (null != DistributeCache)
        {
            await DistributeCache.SetAsync(key, blob, dceo, cancellation).ConfigureAwait(false);
        }
    }

    /// <summary>Removes the value stored for the key.</summary>
    /// <param name="key">The key of the value.</param>
    /// <returns><see langword="true"/> when the operation succeeded; <see langword="false"/> when the underlying cache raised an error.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    public bool Remove(string key)
    {
        using var activity = _activitySource?.StartActivity("Remove from cache.", ActivityKind.Producer);

        activity?.SetTag("cacheKey", key);

        CheckIfInitialized();

        try
        {
            DistributeCache?.Remove(key);
            return true;
        }
        catch (Exception)
        {
            return false;
        }

    }
    /// <summary>Asynchronously removes the value stored for the key.</summary>
    /// <param name="key">The key of the value.</param>
    /// <param name="cancellation">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the operation succeeded; <see langword="false"/> when the underlying cache raised an error.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    public async Task<bool> RemoveAsync(string key, CancellationToken cancellation = default)
    {
        using var activity = _activitySource?.StartActivity("Remove from cache.", ActivityKind.Producer);

        activity?.SetTag("cacheKey", key);
        CheckIfInitialized();

        try
        {
            if (null != DistributeCache)
            {
                await DistributeCache.RemoveAsync(key, cancellation).ConfigureAwait(false);
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>Tries to get the value stored for the key. Errors raised while reading or deserializing are swallowed.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="value">The value, or the default of <typeparamref name="TValue"/> when it is not found or an error occurred.</param>
    /// <returns><see langword="true"/> when a non-null value was read; otherwise <see langword="false"/>.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    public bool TryGetValue<TValue>(string key, out TValue? value)
    {
        CheckIfInitialized();

        try
        {
            value = Get<TValue>(key);
            return null != value;
        }
        catch (Exception)
        {
            value = default;
            return false;
        }

    }

    /// <summary>Asynchronously tries to get the value stored for the key. Errors raised while reading or deserializing are swallowed.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="cancellation">A token to cancel the operation.</param>
    /// <returns>The value, or the default of <typeparamref name="TValue"/> when it is not found or an error occurred.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    public async Task<TValue?> TryGetValueAsync<TValue>(string key, CancellationToken cancellation = default)
    {
        CheckIfInitialized();

        try
        {
            return await GetAsync<TValue>(key, cancellation).ConfigureAwait(false);
        }
        catch (Exception)
        {
            return default;
        }
    }

    /// <summary>Throws when the cache is not initialized.</summary>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized; the message is <see cref="NotInitializedReason"/>.</exception>
    protected void CheckIfInitialized()
    {
        if (!IsInitialized)
        {
            throw new CacheNotInitializedException(NotInitializedReason);
        }
    }

    /// <summary>Returns the name of the type <typeparamref name="T"/>.</summary>
    /// <returns>The name of <typeparamref name="T"/>.</returns>
    public override string ToString()
    {
        return typeof(T).Name;
    }
}

