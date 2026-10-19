using Microsoft.Extensions.Caching.Distributed;

namespace Arc4u.Caching.Memory;

/// <summary>
/// Wraps a <see cref="MemoryDistributedCache"/> so that a value rejected because the size limit is reached is reported.
/// The Microsoft memory cache does not add an entry that does not fit (and removes the previous value of the key), without any error.
/// </summary>
sealed class CapacityCheckedDistributedCache : IDistributedCache
{
    public CapacityCheckedDistributedCache(MemoryDistributedCache inner, string store)
    {
        _inner = inner;
        _store = store;
    }

    readonly MemoryDistributedCache _inner;
    readonly string _store;

    public byte[]? Get(string key) => _inner.Get(key);

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => _inner.GetAsync(key, token);

    public void Refresh(string key) => _inner.Refresh(key);

    public Task RefreshAsync(string key, CancellationToken token = default) => _inner.RefreshAsync(key, token);

    public void Remove(string key) => _inner.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default) => _inner.RemoveAsync(key, token);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        _inner.Set(key, value, options);
        CheckStored(key, value);
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
    {
        await _inner.SetAsync(key, value, options, token).ConfigureAwait(false);
        CheckStored(key, value);
    }

    // The capacity check is done synchronously by Set: when the entry is rejected, the key has no value right after.
    void CheckStored(string key, byte[] value)
    {
        if (_inner.Get(key) is null)
        {
            throw new InvalidOperationException($"The value of {value.Length} bytes for the key '{key}' was not stored: the size limit of the Memory cache {_store} is reached.");
        }
    }
}
