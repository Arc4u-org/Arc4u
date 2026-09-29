namespace Arc4u.OAuth2.Options;
/// <summary>
/// Defines the cache in which the tokens are stored and for how long.
/// </summary>
public class TokenCacheOptions
{
    /// <summary>Gets or sets the time after which a cached token is evicted. The default is 50 minutes.</summary>
    public TimeSpan MaxTime { get; set; } = TimeSpan.FromMinutes(50);

    /// <summary>Gets or sets the name of the cache (registered in the <c>ICacheContext</c>) that stores the tokens. When empty or unknown, the default cache is used.</summary>
    public string CacheName { get; set; } = default!;
}
