using System.Collections.Concurrent;
using Arc4u.Caching;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Arc4u.OAuth2.Token;

/// <summary>Default <see cref="ICacheHelper"/>: resolves the cache named by <see cref="TokenCacheOptions.CacheName"/>, and falls back to the default cache.</summary>
[Export(typeof(ICacheHelper)), Shared]
public class CacheHelper : ICacheHelper
{
    /// <summary>Initializes a new instance of the <see cref="CacheHelper"/> class.</summary>
    /// <param name="cacheContext">The context giving access to the registered caches.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="options">The token cache options.</param>
    /// <remarks>
    /// Only the default <see cref="TokenCacheOptions"/> are read, once, to resolve the token cache: <c>AddTokenCache</c> fixes them at startup, so
    /// <see cref="IOptions{TOptions}"/> is enough. The named options are resolved by <see cref="TokenCacheResolver"/>, which needs an <see cref="IOptionsMonitor{TOptions}"/>.
    /// </remarks>
    public CacheHelper(ICacheContext cacheContext, ILogger<CacheHelper> logger, IOptions<TokenCacheOptions> options)
    {
        _cacheContext = cacheContext;
        _logger = logger;
        _tokenCacheOptions = options.Value;

        try
        {
            _cache = GetDefaultAuthenticationTokenCache();
        }
        catch (Exception ex)
        {
            logger.Technical().LogException(ex);
            logger.Technical().LogDefaultTokenCache();

            _cache = cacheContext.Default;
        }

    }

    private readonly ICacheContext _cacheContext;
    private readonly ILogger<CacheHelper> _logger;
    private readonly TokenCacheOptions _tokenCacheOptions;

    // The named caches resolved by GetCache(string), by name.
    private readonly ConcurrentDictionary<string, ICache> _namedCaches = new(StringComparer.Ordinal);

    // The token cache: the cache named by TokenCacheOptions.CacheName (Authentication:TokenCache:CacheName) when one exists with this name,
    // otherwise the default cache of the cache context.
    private ICache GetDefaultAuthenticationTokenCache()
    {
        var cacheName = _tokenCacheOptions.CacheName;

        if (!string.IsNullOrWhiteSpace(cacheName) && _cacheContext.Exist(cacheName))
        {
            _logger.Technical().LogTokenCacheName(cacheName);

            return _cacheContext[cacheName];
        }

        // use the default cache if not able to resolve it by name.
        return _cacheContext.Default;
    }

    private readonly ICache _cache;

    /// <inheritdoc/>
    public ICache GetCache() => _cache;

    /// <inheritdoc/>
    /// <remarks>
    /// Each name is resolved once and the cache is kept. When the cache cannot be initialized, the error is logged and the default cache is used, as for the token cache.
    /// </remarks>
    public ICache GetCache(string? cacheName)
    {
        if (string.IsNullOrWhiteSpace(cacheName) || string.Equals(cacheName, _tokenCacheOptions.CacheName, StringComparison.Ordinal))
        {
            return _cache;
        }

        return _namedCaches.GetOrAdd(cacheName, ResolveNamedCache);
    }

    private ICache ResolveNamedCache(string cacheName)
    {
        try
        {
            if (_cacheContext.Exist(cacheName))
            {
                _logger.Technical().LogTokenCacheName(cacheName);
                return _cacheContext[cacheName];
            }
        }
        catch (Exception ex)
        {
            _logger.Technical().LogException(ex);
        }

        _logger.Technical().LogDefaultTokenCache();
        return _cacheContext.Default;
    }
}
