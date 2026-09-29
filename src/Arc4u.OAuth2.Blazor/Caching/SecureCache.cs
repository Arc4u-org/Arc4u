using Arc4u.Caching;
using Arc4u.Dependency.Attribute;

namespace Arc4u.Blazor.Caching;

/// <summary>
/// Provides a secure cache implementation.
/// </summary>
/// <remarks>
/// The values are kept in memory, in the browser session of the application. The expiration arguments (<c>timeout</c> and <c>isSlided</c>) are ignored,
/// and a value added under a key that already exists does not replace the existing one.
/// </remarks>
[Export(typeof(ISecureCache)), Shared]
public class SecureCache : ISecureCache
{
    private Dictionary<string, object> _repository = [];

    /// <summary>
    /// Disposes the cache object. There is nothing to release.
    /// </summary>
    public void Dispose()
    {
    }

    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <returns>The value associated with the specified key.</returns>
    /// <exception cref="KeyNotFoundException">There is no value for the key.</exception>
    public TValue Get<TValue>(string key)
    {
        return (TValue)_repository[key];
    }

    /// <summary>
    /// Asynchronously gets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="cancellation">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the value associated with the specified key.</returns>
    /// <exception cref="KeyNotFoundException">There is no value for the key.</exception>
    public Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellation = default)
    {
        return Task.FromResult((TValue?)_repository[key]);
    }

    /// <summary>
    /// Empties the cache. The store name is not used.
    /// </summary>
    /// <param name="store">The store name, which is ignored.</param>
    public void Initialize(string store)
    {
        _repository = [];
    }

    /// <summary>
    /// Adds the specified key and value to the cache. Nothing is done when the key already exists.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    public void Put<T>(string key, T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        _repository.TryAdd(key, value);
    }

    /// <summary>
    /// Adds the specified key and value to the cache. The expiration arguments are ignored and nothing is done when the key already exists.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="timeout">The expiration time, which is ignored.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <param name="isSlided">The sliding expiration flag, which is ignored.</param>
    public void Put<T>(string key, TimeSpan timeout, T value, bool isSlided = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        _repository.TryAdd(key, value);
    }

    /// <summary>
    /// Asynchronously adds the specified key and value to the cache. Nothing is done when the key already exists.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <param name="cancellation">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PutAsync<T>(string key, T value, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        _repository.TryAdd(key, value);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Asynchronously adds the specified key and value to the cache. The expiration arguments are ignored and nothing is done when the key already exists.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="timeout">The expiration time, which is ignored.</param>
    /// <param name="value">The value of the element to add.</param>
    /// <param name="isSlided">The sliding expiration flag, which is ignored.</param>
    /// <param name="cancellation">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task PutAsync<T>(string key, TimeSpan timeout, T value, bool isSlided = false, CancellationToken cancellation = default)
    {
        ArgumentNullException.ThrowIfNull(value);

        _repository.TryAdd(key, value);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes the value with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <returns>True if the element is successfully found and removed; otherwise, false.</returns>
    public bool Remove(string key)
    {
        return _repository.Remove(key);
    }

    /// <summary>
    /// Asynchronously removes the value with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <param name="cancellation">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean indicating whether the element was successfully found and removed.</returns>
    public Task<bool> RemoveAsync(string key, CancellationToken cancellation = default)
    {
        return Task.FromResult(_repository.Remove(key));
    }

    /// <summary>
    /// Tries to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="value">When this method returns, contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns>True if the cache contains an element with the specified key; otherwise, false.</returns>
    public bool TryGetValue<TValue>(string key, out TValue? value)
    {
        var result = _repository.TryGetValue(key, out var objectValue);

        value = (TValue?)objectValue;

        return result;
    }

    /// <summary>
    /// Asynchronously tries to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="cancellation">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the value associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</returns>
    public Task<TValue?> TryGetValueAsync<TValue>(string key, CancellationToken cancellation = default)
    {
        _repository.TryGetValue(key, out var objectValue);

        return Task.FromResult((TValue?)objectValue);
    }
}
