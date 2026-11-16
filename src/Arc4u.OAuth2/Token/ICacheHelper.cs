using Arc4u.Caching;

namespace Arc4u.OAuth2.Token;
/// <summary>Gives access to the cache used to store the tokens.</summary>
public interface ICacheHelper
{
    /// <summary>Gets the cache used to store the tokens.</summary>
    /// <returns>The token cache.</returns>
    ICache GetCache();

    /// <summary>Gets the cache registered under <paramref name="cacheName"/>.</summary>
    /// <param name="cacheName">The name of the cache. When it is empty, the token cache (see <see cref="GetCache()"/>) is returned.</param>
    /// <returns>The named cache, or the default cache when no cache is registered under <paramref name="cacheName"/>.</returns>
    ICache GetCache(string? cacheName);
}
