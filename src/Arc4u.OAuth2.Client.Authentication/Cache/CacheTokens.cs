using System.Globalization;
using System.Text.Json;
using Arc4u.Caching;
using Arc4u.Configuration;
using Arc4u.Dependency.Attribute;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Client.Authentication.Cache;

/// <summary>
/// An <see cref="ISecureCache"/> that persists each value as a JSON file in the <c>OAuth2</c> folder of the local application data of the user. The file name is built from the logging name and the
/// environment name of the application configuration and the key. The expiration arguments are not supported.
/// </summary>
[Export(typeof(ISecureCache)), Shared]
public sealed class CacheTokens : ISecureCache
{
    /// <summary>Initializes a new instance of the <see cref="CacheTokens"/> class and creates the cache folder when it does not exist.</summary>
    /// <param name="config">The application configuration, giving the logging name and the environment name.</param>
    public CacheTokens(IOptions<ApplicationConfig> config)
    {
        // Create a cache file from the application name in the config file + environment.
        var path = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "OAuth2");

        _cacheFilePath = Path.Combine(path, string.Format(CultureInfo.CurrentCulture, "{0}_{1}_", config.Value.Environment.LoggingName, config.Value.Environment.Name));

        if (Directory.Exists(path) == false)
        {
            Directory.CreateDirectory(path);
        }
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

#if NET9_0_OR_GREATER
        static readonly Lock FileLock = new();
#else
    static readonly object FileLock = new();
#endif
    private readonly string _cacheFilePath;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private bool _disposed;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            _disposed = true;
        }
    }

    /// <summary>Gets the value stored under a key.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <returns>The value, or the default of <typeparamref name="TValue"/> when the file does not exist.</returns>
    /// <exception cref="KeyNotFoundException">The file contains a <c>null</c> value.</exception>
    public TValue Get<TValue>(string key)
    {
        var path = ComputePath(key);

        if (!File.Exists(path))
        {
            return default!;
        }

        lock (FileLock)
        {
            var result = JsonSerializer.Deserialize<TValue>(File.ReadAllText(path));

            return result ?? throw new KeyNotFoundException();
        }
    }

    private string ComputePath(string key)
    {
        return $"{_cacheFilePath}{key.Trim()}.json";
    }

    /// <summary>Does nothing.</summary>
    /// <param name="store">The store name, which is ignored.</param>
    public void Initialize(string store)
    {
    }

    /// <inheritdoc/>
    public bool Remove(string key)
    {
        try
        {
            lock (FileLock)
            {
                var path = ComputePath(key);

                File.Delete(path);

                return true;
            }
        }
        catch (Exception)
        {
            return false;
        }

    }

    /// <inheritdoc/>
    public bool TryGetValue<TValue>(string key, out TValue value)
    {
        try
        {
            value = Get<TValue>(key);
            return true;
        }
        catch (Exception)
        {
            value = default!;
            return false;
        }
    }

    /// <inheritdoc/>
    public void Put<T>(string key, T value)
    {
        var path = ComputePath(key);

        var json = JsonSerializer.Serialize(value, _jsonSerializerOptions);

        File.WriteAllText(path, json);
    }

    /// <inheritdoc/>
    public async Task PutAsync<T>(string key, T value, CancellationToken cancellation = default)
    {
        var path = ComputePath(key);

        var json = JsonSerializer.Serialize(value, _jsonSerializerOptions);

        await File.WriteAllTextAsync(path, json, cancellation).ConfigureAwait(true);
    }

    /// <summary>Not supported.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="timeout">The expiration time.</param>
    /// <param name="value">The value.</param>
    /// <param name="isSlided">The sliding expiration flag.</param>
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public void Put<T>(string key, TimeSpan timeout, T value, bool isSlided = false)
    {
        throw new NotImplementedException();
    }

    /// <summary>Not supported.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="timeout">The expiration time.</param>
    /// <param name="value">The value.</param>
    /// <param name="isSlided">The sliding expiration flag.</param>
    /// <param name="cancellation">A token to cancel the operation.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public Task PutAsync<T>(string key, TimeSpan timeout, T value, bool isSlided = false, CancellationToken cancellation = default)
    {
        throw new NotImplementedException();
    }

    /// <summary>Gets the value stored under a key.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="cancellation">A token to cancel the operation.</param>
    /// <returns>The value, or the default of <typeparamref name="TValue"/> when the file does not exist.</returns>
    /// <exception cref="KeyNotFoundException">The file contains a <c>null</c> value.</exception>
    public async Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellation = default)
    {
        var path = ComputePath(key);

        if (!File.Exists(path))
        {
            return default;
        }

        // Lock to avoid concurrent access to the same file.
        // Do not perform async I/O inside the lock: copy bytes synchronously, then deserialize asynchronously.
        byte[] bytes;
        lock (FileLock)
        {
#pragma warning disable CA1849
            bytes = File.ReadAllBytes(path);
#pragma warning restore CA1849
        }

        using var ms = new MemoryStream(bytes, writable: false);
        var result = await JsonSerializer.DeserializeAsync<TValue>(ms, _jsonSerializerOptions, cancellation).ConfigureAwait(false);

        return result ?? throw new KeyNotFoundException();
    }

    /// <summary>Not supported.</summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="key">The key of the value.</param>
    /// <param name="cancellation">A token to cancel the operation.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public Task<TValue?> TryGetValueAsync<TValue>(string key, CancellationToken cancellation = default)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<bool> RemoveAsync(string key, CancellationToken cancellation = default)
    {
        return Task.FromResult(Remove(key));
    }
}
