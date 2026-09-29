namespace Arc4u.Caching;

/// <summary>
/// The interface defines the contract implemented by a cache.
/// </summary>
public interface ICache : IDisposable
{
    /// <summary>Initializes the cache. This must be called once every time a cache is created.</summary>
    /// <param name="store">The name of the cache, as declared in the configuration; it identifies the options and the store used.</param>
    void Initialize(string store);

    /// <summary>Adds or replaces a value in the cache, without a specific expiration.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key used to identify the value in the cache.</param>
    /// <param name="value">The value to save; it cannot be <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/> (or the default value of <typeparamref name="T"/>). This check is done by the distributed caches (memory, Redis, SQL), not by the Dapr cache.</exception>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    void Put<T>(string key, T value);

    /// <summary>Asynchronously adds or replaces a value in the cache, without a specific expiration.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key used to identify the value in the cache.</param>
    /// <param name="value">The value to save; it cannot be <see langword="null"/>.</param>
    /// <param name="cancellation">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A task that completes when the value is saved.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>. This check is done by the distributed caches (memory, Redis, SQL), not by the Dapr cache.</exception>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    Task PutAsync<T>(string key, T value, CancellationToken cancellation = default);

    /// <summary>Adds or replaces a value in the cache with an expiration.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key used to identify the value in the cache.</param>
    /// <param name="timeout">The period of validity of the value. When it expires, the value is removed from the cache.</param>
    /// <param name="value">The value to save; it cannot be <see langword="null"/>.</param>
    /// <param name="isSlided"><see langword="true"/> to restart the period each time the value is read (sliding expiration); <see langword="false"/> for an absolute expiration. Not every cache supports a sliding expiration (the Dapr cache throws <see cref="NotSupportedException"/>).</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>. This check is done by the distributed caches (memory, Redis, SQL), not by the Dapr cache.</exception>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    /// <exception cref="NotSupportedException"><paramref name="isSlided"/> is <see langword="true"/> and the cache does not support it.</exception>
    void Put<T>(string key, TimeSpan timeout, T value, bool isSlided = false);

    /// <summary>Asynchronously adds or replaces a value in the cache with an expiration.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key used to identify the value in the cache.</param>
    /// <param name="timeout">The period of validity of the value. When it expires, the value is removed from the cache.</param>
    /// <param name="value">The value to save; it cannot be <see langword="null"/>.</param>
    /// <param name="isSlided"><see langword="true"/> to restart the period each time the value is read (sliding expiration); <see langword="false"/> for an absolute expiration. Not every cache supports a sliding expiration (the Dapr cache throws <see cref="NotSupportedException"/>).</param>
    /// <param name="cancellation">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>A task that completes when the value is saved.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>. This check is done by the distributed caches (memory, Redis, SQL), not by the Dapr cache.</exception>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    /// <exception cref="NotSupportedException"><paramref name="isSlided"/> is <see langword="true"/> and the cache does not support it.</exception>
    Task PutAsync<T>(string key, TimeSpan timeout, T value, bool isSlided = false, CancellationToken cancellation = default);

    /// <summary>Gets the value stored for the key.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key used to identify the value in the cache.</param>
    /// <returns>The value, or the default of <typeparamref name="TValue"/> when no value exists for the key.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    /// <exception cref="DataCacheException">The cache engine or the deserialization failed. The distributed caches (memory, Redis, SQL) wrap the error in this exception; the Dapr cache lets the exception of the Dapr client through.</exception>
    TValue? Get<TValue>(string key);

    /// <summary>Asynchronously gets the value stored for the key.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key used to identify the value in the cache.</param>
    /// <param name="cancellation">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>The value, or the default of <typeparamref name="TValue"/> when no value exists for the key.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    /// <exception cref="DataCacheException">The cache engine or the deserialization failed. The distributed caches (memory, Redis, SQL) wrap the error in this exception; the Dapr cache lets the exception of the Dapr client through.</exception>
    Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellation = default);

    /// <summary>Tries to get the value stored for the key. Errors raised while reading or deserializing are not thrown.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key used to identify the value in the cache.</param>
    /// <param name="value">The value, or the default of <typeparamref name="TValue"/> when it is not found or could not be read.</param>
    /// <returns><see langword="true"/> when a value was read; otherwise <see langword="false"/>.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    bool TryGetValue<TValue>(string key, out TValue? value);

    /// <summary>Asynchronously tries to get the value stored for the key. Errors raised while reading or deserializing are not thrown.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key used to identify the value in the cache.</param>
    /// <param name="cancellation">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns>The value, or the default of <typeparamref name="TValue"/> when it is not found or could not be read.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    Task<TValue?> TryGetValueAsync<TValue>(string key, CancellationToken cancellation = default);

    /// <summary>Removes the value stored for the key.</summary>
    /// <param name="key">The key used to identify the value in the cache.</param>
    /// <returns><see langword="true"/> when the operation succeeded; <see langword="false"/> when the underlying cache raised an error.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    bool Remove(string key);

    /// <summary>Asynchronously removes the value stored for the key.</summary>
    /// <param name="key">The key used to identify the value in the cache.</param>
    /// <param name="cancellation">A <see cref="CancellationToken"/> to cancel the operation.</param>
    /// <returns><see langword="true"/> when the operation succeeded; <see langword="false"/> when the underlying cache raised an error.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    Task<bool> RemoveAsync(string key, CancellationToken cancellation = default);
}
