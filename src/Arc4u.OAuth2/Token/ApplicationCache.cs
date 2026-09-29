using Arc4u.Caching;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Token;

/// <summary>
/// An <see cref="ITokenCache"/> that stores the tokens in the cache designated by <see cref="TokenCacheOptions.CacheName"/>
/// (the default cache when the name is empty or unknown). The keys are lower-cased and suffixed with <c>_TokenCache</c>.
/// </summary>
[Export(typeof(ITokenCache)), Shared]
public class ApplicationCache(ICacheHelper cacheHelper, ILogger logger, IOptions<TokenCacheOptions> options) : ITokenCache
{
    private readonly ICache _cache = cacheHelper.GetCache();
    private readonly TokenCacheOptions _tokenCacheOptions = options.Value;

    /// <summary>
    /// Removes the entry stored for the key. The claims cached by the claims filler are stored under another key and are not removed.
    /// </summary>
    /// <param name="key">The key of the token.</param>
    public void DeleteItem(string key)
    {
        logger.Technical<ApplicationCache>().LogDeleteInTokenCache(key);
        _cache.Remove(ApplicationCache.GetKey(key));
        logger.Technical<ApplicationCache>().LogDeletedInTokenCache(key);
    }

    /// <inheritdoc/>
    public void Put<T>(string key, T data)
    {
        if (null == data)
        {
            logger.Technical<ApplicationCache>().LogNullTokenData(key);
            return;
        }

        logger.Technical<ApplicationCache>().LogAddingInTokenCache(key);
        _cache.Put(ApplicationCache.GetKey(key), _tokenCacheOptions.MaxTime, data);
        logger.Technical<ApplicationCache>().LogAddedInTokenCache(key);
    }

    /// <inheritdoc/>
    public T? Get<T>(string key)
    {
        logger.Technical<ApplicationCache>().LogGetDataTokenCache(key);
        var data = _cache.Get<T>(ApplicationCache.GetKey(key));

        if (null == data)
        {
            logger.Technical<ApplicationCache>().LogGetNullDataTokenCache(key);
        }

        return data;
    }

    /// <summary>Not supported.</summary>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotImplementedException">Always thrown.</exception>
    public IEnumerable<byte[]> GetAll()
    {
        logger.Technical<ApplicationCache>().LogTokenCacheNotImplemented();

        throw new NotImplementedException();
    }

    private static string GetKey(string id)
    {
        return (id + "_TokenCache").ToLowerInvariant();
    }

}
