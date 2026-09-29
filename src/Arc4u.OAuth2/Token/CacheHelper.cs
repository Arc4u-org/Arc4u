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
    public CacheHelper(ICacheContext cacheContext, ILogger<CacheHelper> logger, IOptions<TokenCacheOptions> options)
    {
        _cacheContext = cacheContext;
        _logger = logger;
        _tokenCacheOptions = options.Value;

        try
        {
            _cache = GetCacheFromConfig();
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

    /// return the cache based on:
    /// 1) a cache exists with the Principal.CacheName
    /// 2) default.
    private ICache GetCacheFromConfig()
    {
        var cacheName = _tokenCacheOptions.CacheName;

        if (!string.IsNullOrWhiteSpace(cacheName) && _cacheContext.Exist(cacheName))
        {
            _logger.Technical().LogTokenCacheName(cacheName);

            return _cacheContext[cacheName];
        }

        return _cacheContext.Default;
    }

    private readonly ICache _cache;

    /// <inheritdoc/>
    public ICache GetCache() => _cache;
}
