using System.Security.Claims;

namespace Arc4u.OAuth2;

/// <summary>
/// Generates the key under which the data of an identity is stored in the cache.
/// </summary>
public interface ICacheKeyGenerator
{
    /// <summary>Gets the cache key used to store the claims of the identity.</summary>
    /// <param name="identity">The identity for which the key is generated.</param>
    /// <returns>The cache key.</returns>
    string GetClaimsKey(ClaimsIdentity identity);
}
