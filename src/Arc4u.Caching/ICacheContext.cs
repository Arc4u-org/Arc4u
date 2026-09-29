namespace Arc4u.Caching;
/// <summary>Gives access to the caches declared in the <c>Caching</c> configuration section.</summary>
public interface ICacheContext
{
    /// <summary>Gets the cache with the given name, initializing it on first use when it is not flagged as auto start.</summary>
    /// <param name="cacheName">The name of the cache, as defined in the configuration.</param>
    /// <returns>The cache.</returns>
    /// <exception cref="InvalidOperationException">No cache is configured with this name.</exception>
    ICache this[string cacheName] { get; }

    /// <summary>Gets the default cache, i.e. the cache named by the <c>Default</c> entry of the <c>Caching</c> configuration section.</summary>
    /// <exception cref="InvalidOperationException">No cache is configured with the default name.</exception>
    ICache Default { get; }
    // CachingPrincipal Principal { get; set; }

    /// <summary>Determines whether a cache with this name is declared in the configuration, whether it is already initialized or not.</summary>
    /// <param name="cacheName">The name of the cache.</param>
    /// <returns><see langword="true"/> when the cache exists; otherwise <see langword="false"/>.</returns>
    bool Exist(string cacheName);
}
