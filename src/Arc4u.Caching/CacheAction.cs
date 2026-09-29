namespace Arc4u.Caching;

/// <summary>The actions that can be performed on a cache entry. The values can be combined.</summary>
[Flags]
public enum CacheAction
{
    /// <summary>An entry was added.</summary>
    Added = 1,
    /// <summary>An entry was removed.</summary>
    Removed = 2,
    /// <summary>An entry was updated.</summary>
    Updated = 4
}
