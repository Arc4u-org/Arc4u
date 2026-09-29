using Arc4u.Caching;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Token;

/// <summary>
/// An <see cref="ITokenCache"/> for a local client application: the token is persisted in the <see cref="ISecureCache"/> and kept in memory for the next reads.
/// </summary>
[Export(typeof(ITokenCache)), Shared]
public class ApplicationLocalDataCache : ITokenCache
{
    private object? _token;
    private readonly ICache Cache;
    private readonly ILogger Logger;

    /// <summary>Initializes a new instance of the <see cref="ApplicationLocalDataCache"/> class.</summary>
    /// <param name="cache">The secure cache in which the token is persisted.</param>
    /// <param name="logger">The logger.</param>
    public ApplicationLocalDataCache(ISecureCache cache, ILogger logger)
    {
        Cache = cache;
        Logger = logger;
    }

    /// <summary>Removes the token from the secure cache and from the in-memory copy. Errors are ignored.</summary>
    /// <param name="key">The key of the token.</param>
    public void Clear(string key)
    {
        try
        {
            Cache.Remove(key);
            _token = null;
        }
        catch { }
    }

    /// <inheritdoc/>
    public void DeleteItem(string key)
    {
        Clear(key);
    }

    /// <inheritdoc/>
    public void Put<T>(string key, T data)
    {
        try
        {
            Cache.Put(key, data);
            _token = data;
        }
        catch (Exception ex)
        {
            Logger.Technical<ApplicationLocalDataCache>().LogException(ex);
        }

    }

    /// <inheritdoc/>
    public T? Get<T>(string key)
    {
        if (_token is T token)
        {
            return token;
        }

        Load<T>(key);

        return (T?)_token;
    }

    void Load<T>(string key)
    {
        _token = null;

        try
        {
            _token = Cache.Get<T>(key);
        }
        catch (Exception ex)
        {
            Logger.Technical<ApplicationLocalDataCache>().LogException(ex);
        }
    }

}
