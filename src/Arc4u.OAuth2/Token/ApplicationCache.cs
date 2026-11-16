using Arc4u.Caching;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Arc4u.OAuth2.Options;
using Microsoft.Extensions.Logging;

namespace Arc4u.OAuth2.Token;

/// <summary>
/// An <see cref="ITokenCache"/> that stores the tokens in the cache designated by <see cref="TokenCacheOptions.CacheName"/>
/// (the default cache when the name is empty or unknown). The keys are lower-cased and suffixed with <c>_TokenCache</c>.
/// </summary>
/// <remarks>
/// The cache and the <see cref="TokenCacheOptions.MaxTime"/> are given by the <see cref="TokenCacheResolver"/>: the <c>optionsName</c> given to each method selects
/// the named options (for example the key of an <c>Authentication:ClientTokens</c> entry), and <see langword="null"/> selects the default ones.
/// <para>
/// A name is used only when options are registered under it (see <see cref="TokenCacheResolver"/>): by <c>AddNamedTokenCache</c>, <c>AddSharedTokenCache</c>,
/// <c>AddClientTokens</c>, <c>AddOnBehalfOf</c> or <c>services.Configure&lt;TokenCacheOptions&gt;(name, …)</c>. Any other name, for example a typo, uses the default options
/// and a warning is logged once.
/// </para>
/// </remarks>
/// <param name="logger">The logger.</param>
/// <param name="resolver">Resolves the cache and the maximum time of the default or named token cache options.</param>
[Export(typeof(ITokenCache)), Shared]
public class ApplicationCache(ILogger logger, TokenCacheResolver resolver) : ITokenCache
{
    private readonly TokenCacheResolver _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));

    /// <summary>
    /// Removes the entry stored for the key. The claims cached by the claims filler are stored under another key and are not removed.
    /// </summary>
    /// <param name="key">The key of the token.</param>
    /// <param name="optionsName">The name of the <see cref="TokenCacheOptions"/> to use, or <see langword="null"/> for the default ones.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the entry is removed.</returns>
    public async ValueTask DeleteItemAsync(string key, string? optionsName = null, CancellationToken cancellationToken = default)
    {
        logger.Technical<ApplicationCache>().LogDeleteInTokenCache(key);
        await GetCache(optionsName, out _).RemoveAsync(ApplicationCache.GetKey(key), cancellationToken).ConfigureAwait(false);
        logger.Technical<ApplicationCache>().LogDeletedInTokenCache(key);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The entry is evicted after <see cref="TokenCacheOptions.MaxTime"/>. A <see cref="TokenInfo"/> is evicted at its <see cref="TokenInfo.ExpiresOnUtc"/> when it comes first;
    /// an already expired <see cref="TokenInfo"/> is not stored and the previous entry for the key is removed.
    /// </remarks>
    public async ValueTask PutAsync<T>(string key, T data, string? optionsName = null, CancellationToken cancellationToken = default)
    {
        if (data is null)
        {
            logger.Technical<ApplicationCache>().LogNullTokenData(key);
            return;
        }

        var cache = GetCache(optionsName, out var timeout);

        if (data is TokenInfo tokenInfo)
        {
            var remaining = tokenInfo.ExpiresOnUtc - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                logger.Technical<ApplicationCache>().LogExpiredTokenNotCached(key);
                await cache.RemoveAsync(ApplicationCache.GetKey(key), cancellationToken).ConfigureAwait(false);
                return;
            }

            if (remaining < timeout)
            {
                timeout = remaining;
            }
        }

        logger.Technical<ApplicationCache>().LogAddingInTokenCache(key);
        await cache.PutAsync(ApplicationCache.GetKey(key), timeout, data, cancellation: cancellationToken).ConfigureAwait(false);
        logger.Technical<ApplicationCache>().LogAddedInTokenCache(key);
    }

    /// <inheritdoc/>
    public async ValueTask<T?> GetAsync<T>(string key, string? optionsName = null, CancellationToken cancellationToken = default)
    {
        logger.Technical<ApplicationCache>().LogGetDataTokenCache(key);
        var data = await GetCache(optionsName, out _).GetAsync<T>(ApplicationCache.GetKey(key), cancellationToken).ConfigureAwait(false);

        if (data is null)
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

    private ICache GetCache(string? optionsName, out TimeSpan maxTime)
    {
        var resolved = _resolver.Resolve(optionsName);
        maxTime = resolved.MaxTime;
        return resolved.Cache;
    }

    private static string GetKey(string id)
    {
        return (id + "_TokenCache").ToLowerInvariant();
    }

}
