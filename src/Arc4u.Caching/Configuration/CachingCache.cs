namespace Arc4u.Configuration;

/// <summary>One entry of the <c>Caches</c> array of the <c>Caching</c> configuration section.</summary>
public class CachingCache
{
    /// <summary>Gets or sets the name used to retrieve the cache from the <see cref="Arc4u.Caching.ICacheContext"/>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the kind of cache: <c>Memory</c>, <c>Redis</c>, <c>RedisSentinel</c>, <c>Sql</c> or <c>Dapr</c>.</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the cache is created and initialized when the <see cref="Arc4u.Caching.ICacheContext"/> is created; otherwise it is initialized on first use.</summary>
    public bool IsAutoStart { get; set; }
}
