using Arc4u.Caching;
using Arc4u.Dependency.Attribute;
using Arc4u.OAuth2.Token;

namespace Arc4u.OAuth2.Security.Principal;

/// <summary>
/// An <see cref="ISecureCache"/> that delegates to the token cache configured through <see cref="Arc4u.OAuth2.Options.TokenCacheOptions"/> (see <see cref="ICacheHelper"/>).
/// </summary>
[Export(typeof(ISecureCache)), Shared]
public class ServerPrincipalCache : ISecureCache
{
    /// <summary>Initializes a new instance of the <see cref="ServerPrincipalCache"/> class.</summary>
    /// <param name="cacheHelper">The helper giving access to the token cache.</param>
    public ServerPrincipalCache(ICacheHelper cacheHelper)
    {
        _cache = cacheHelper.GetCache();
    }

    private ICache? _cache;
    private bool disposed;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>Releases the reference to the underlying cache.</summary>
    /// <param name="disposing"><see langword="true"/> when called from <see cref="Dispose()"/>.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                _cache = null;
                disposed = true;
            }
        }
    }

    /// <inheritdoc/>
    public TValue? Get<TValue>(string key)
    {
        if (_cache is null)
        {
            return default;
        }
        return _cache.Get<TValue>(key);
    }

    /// <inheritdoc/>
    public async Task<TValue?> GetAsync<TValue>(string key, CancellationToken cancellation = default)
    {
        if (_cache is null)
        {
            return default;
        }
        return await _cache.GetAsync<TValue>(key, cancellation).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void Initialize(string store)
    {
    }

    /// <inheritdoc/>
    public void Put<T>(string key, T value)
    {
        _cache?.Put(key, value);
    }

    /// <inheritdoc/>
    public void Put<T>(string key, TimeSpan timeout, T value, bool isSlided = false)
    {
        _cache?.Put(key, timeout, value, isSlided);
    }

    /// <inheritdoc/>
    public async Task PutAsync<T>(string key, T value, CancellationToken cancellation = default)
    {
        if (_cache is null)
        {
            return;
        }
        await _cache.PutAsync(key, value, cancellation).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task PutAsync<T>(string key, TimeSpan timeout, T value, bool isSlided = false, CancellationToken cancellation = default)
    {
        if (_cache is null)
        {
            return;
        }
        await _cache.PutAsync(key, timeout, value, isSlided, cancellation).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public bool Remove(string key)
    {
        return _cache?.Remove(key) ?? false;
    }

    /// <inheritdoc/>
    public async Task<bool> RemoveAsync(string key, CancellationToken cancellation = default)
    {
        return _cache is null ? false : await _cache.RemoveAsync(key, cancellation).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public bool TryGetValue<TValue>(string key, out TValue? value)
    {
        if (_cache is null)
        {
            value = default;
            return false;
        }

        return _cache.TryGetValue(key, out value);
    }

    /// <inheritdoc/>
    public async Task<TValue?> TryGetValueAsync<TValue>(string key, CancellationToken cancellation = default)
    {
        if (_cache is null)
        {
            return default!;
        }

        return await _cache.TryGetValueAsync<TValue>(key, cancellation).ConfigureAwait(false);
    }
}
