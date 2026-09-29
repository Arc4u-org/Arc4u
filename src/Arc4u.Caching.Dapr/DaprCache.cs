using System.Globalization;
using Arc4u.Configuration.Dapr;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Dapr.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.Caching.Dapr;

/// <summary>
/// An <see cref="ICache"/> backed by a Dapr state store, resolved with the kind <c>Dapr</c>. The values are serialized by the Dapr client (not by an
/// <see cref="Arc4u.Serializer.IObjectSerialization"/>). The name of the state store comes from the <see cref="DaprCacheOption"/> named after the store.
/// </summary>
[Export("Dapr", typeof(ICache))]
public sealed class DaprCache : ICache
{
    /// <summary>Initializes a new instance of the <see cref="DaprCache"/> class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The named options of the Dapr caches.</param>
    public DaprCache(ILogger<DaprCache> logger, IOptionsMonitor<DaprCacheOption> options)
    {
        _logger = logger;
        _options = options;
    }

    private readonly ILogger<DaprCache> _logger;
    private readonly IOptionsMonitor<DaprCacheOption> _options;

    private DaprClient? _daprClient;
    private string _storeName = string.Empty;

    /// <summary>Disposes the Dapr client.</summary>
    public void Dispose()
    {
        _daprClient?.Dispose();
    }

    /// <summary>Gets the value stored in the state store for the key.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <returns>The value, or the default of <typeparamref name="TValue"/> when the key does not exist.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    public TValue? Get<TValue>(string key)
    {
        if (_daprClient is null)
        {
            throw new CacheNotInitializedException();
        }

        return _daprClient.GetStateAsync<TValue>(_storeName, key).GetAwaiter().GetResult();
    }

    /// <summary>Asynchronously gets the value stored in the state store for the key.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="cancellation">A token to cancel the operation.</param>
    /// <returns>The value, or the default of <typeparamref name="TValue"/> when the key does not exist.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    public async Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellation = default)
    {
        if (_daprClient is null)
        {
            throw new CacheNotInitializedException();
        }

        return await _daprClient.GetStateAsync<TValue>(_storeName, key, cancellationToken: cancellation).ConfigureAwait(false);
    }

    /// <summary>Creates the Dapr client and reads the name of the state store from the <see cref="DaprCacheOption"/> named <paramref name="store"/>. Calling it again on an initialized cache only logs a warning.</summary>
    /// <param name="store">The name of the cache, as declared in the configuration.</param>
    /// <exception cref="ArgumentException"><paramref name="store"/> is empty.</exception>
    public void Initialize(string store)
    {
        lock (_logger)
        {
            if (_daprClient is not null)
            {
                _logger.Technical().LogCacheIsAlreadyInitialized(store);
            }
            else
            {
                if (string.IsNullOrEmpty(store))
                {
                    NotInitializedReason = "When initializing the Dapr cache, the value of the store cannot be an empty string.";
                    throw new ArgumentException(NotInitializedReason, nameof(store));
                }

                try
                {
                    var config = _options.Get(store);

                    _storeName = config.Name ?? throw new NullReferenceException("There is no name defined in the configuration for the Dapr section!");
                    _daprClient = new DaprClientBuilder().Build();
                    _logger.Technical().LogCacheIsInitialized(store);
                }
                catch (Exception ex)
                {
                    NotInitializedReason = $"Dapr Cache {store} is not initialized. With exception: {ex.Message}";

                    throw;
                }

            }
        }
    }

    /// <summary>Saves the value in the state store, without expiration.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="value">The value to save.</param>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    public void Put<T>(string key, T value)
    {
        CheckIfInitialized();

        _daprClient!.SaveStateAsync(_storeName, key, value).GetAwaiter().GetResult();
    }

    /// <summary>Saves the value in the state store with a time to live (the <c>ttlInSeconds</c> metadata). A sliding expiration is not supported.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="timeout">The time to live of the value.</param>
    /// <param name="value">The value to save.</param>
    /// <param name="isSlided">Must be <see langword="false"/>.</param>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    /// <exception cref="NotSupportedException"><paramref name="isSlided"/> is <see langword="true"/>.</exception>
    public void Put<T>(string key, TimeSpan timeout, T value, bool isSlided = false)
    {
        CheckIfInitialized();

        if (isSlided)
        {
            throw new NotSupportedException("Sliding is not supported in Dapr State.");
        }

        _daprClient!.SaveStateAsync(_storeName, key, value, metadata: new Dictionary<string, string> { { "ttlInSeconds", timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture) } }).GetAwaiter().GetResult();
    }

    /// <summary>Asynchronously saves the value in the state store, without expiration.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="value">The value to save.</param>
    /// <param name="cancellation">A token to cancel the operation.</param>
    /// <returns>A task that completes when the value is saved.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    public async Task PutAsync<T>(string key, T value, CancellationToken cancellation = default)
    {
        CheckIfInitialized();

        await _daprClient!.SaveStateAsync(_storeName, key, value, cancellationToken: cancellation).ConfigureAwait(false);
    }

    /// <summary>Asynchronously saves the value in the state store with a time to live (the <c>ttlInSeconds</c> metadata). A sliding expiration is not supported.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="timeout">The time to live of the value.</param>
    /// <param name="value">The value to save.</param>
    /// <param name="isSlided">Must be <see langword="false"/>.</param>
    /// <param name="cancellation">A token to cancel the operation.</param>
    /// <returns>A task that completes when the value is saved.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    /// <exception cref="NotSupportedException"><paramref name="isSlided"/> is <see langword="true"/>.</exception>
    public async Task PutAsync<T>(string key, TimeSpan timeout, T value, bool isSlided = false, CancellationToken cancellation = default)
    {
        CheckIfInitialized();

        if (isSlided)
        {
            throw new NotSupportedException("Sliding is not supported in Dapr State.");
        }

        await _daprClient!.SaveStateAsync(_storeName, key, value, metadata: new Dictionary<string, string> { { "ttlInSeconds", timeout.TotalSeconds.ToString(CultureInfo.InvariantCulture) } }, cancellationToken: cancellation).ConfigureAwait(false);
    }

    /// <summary>Deletes the value from the state store. Errors are logged and reported by the return value.</summary>
    /// <param name="key">The key of the value.</param>
    /// <returns><see langword="true"/> when the value was deleted; <see langword="false"/> when an error occurred.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    public bool Remove(string key)
    {
        CheckIfInitialized();

        try
        {
            _daprClient!.DeleteStateAsync(_storeName, key).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.Technical().LogException(ex);
            return false;
        }

        return true;
    }

    /// <summary>Asynchronously deletes the value from the state store. Errors are logged and reported by the return value.</summary>
    /// <param name="key">The key of the value.</param>
    /// <param name="cancellation">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> when the value was deleted; <see langword="false"/> when an error occurred.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    public async Task<bool> RemoveAsync(string key, CancellationToken cancellation = default)
    {
        CheckIfInitialized();

        try
        {
            await _daprClient!.DeleteStateAsync(_storeName, key, cancellationToken: cancellation).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.Technical().LogException(ex);
            return false;
        }

        return true;
    }

    /// <summary>Tries to read the value for the key. Errors raised while reading are swallowed.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="value">The value, or the default of <typeparamref name="TValue"/> when an error occurred.</param>
    /// <returns><see langword="true"/> when the read succeeded (the value can be the default if the key does not exist); <see langword="false"/> when an error occurred.</returns>
    /// <exception cref="CacheNotInitializedException">The cache is not initialized.</exception>
    public bool TryGetValue<TValue>(string key, out TValue? value)
    {
        CheckIfInitialized();

        try
        {
            value = Get<TValue>(key);
            return true;
        }
        catch (Exception)
        {
            value = default;
            return false;
        }
    }

    /// <summary>Asynchronously tries to read the value for the key. Errors raised while reading are swallowed.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="cancellation">A token to cancel the operation.</param>
    /// <returns>The value, or the default of <typeparamref name="TValue"/> when an error occurred.</returns>
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

    // The reason why the cache is not initialized, this will be used when an exception is thrown.
    string NotInitializedReason { get; set; } = string.Empty;

    void CheckIfInitialized()
    {
        if (_daprClient is null)
        {
            throw new CacheNotInitializedException(NotInitializedReason);
        }
    }

    /// <summary>Returns the name of the Dapr state store.</summary>
    /// <returns>The name of the state store (empty before initialization).</returns>
    public override string ToString() => _storeName ?? throw new InvalidOperationException("The Store name parameter must not be null.");
}
