namespace Arc4u.OAuth2.DataProtection
{
    /// <summary>Describes the cache in which the data protection keys are persisted.</summary>
    public class CacheStoreOption
    {
        /// <summary>Gets or sets the key under which the keys are stored. Required.</summary>
        public string CacheKey { get; set; } = default!;

        /// <summary>Gets or sets the name of the cache. When empty, the default cache is used.</summary>
        public string? CacheName { get; set; }
    }
}
