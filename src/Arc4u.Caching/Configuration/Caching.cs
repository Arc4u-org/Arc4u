
namespace Arc4u.Configuration;

/// <summary>The <c>Caching</c> configuration section: the name of the default cache and the list of declared caches.</summary>
public class Caching
{
    /// <summary>Initializes a new instance of the <see cref="Caching"/> class with no cache and no default.</summary>
    public Caching()
    {
        Caches = [];
        Default = string.Empty;
    }

    /// <summary>Gets or sets the name of the default cache, see <see cref="Arc4u.Caching.ICacheContext.Default"/>.</summary>
    public string Default { get; set; }

    /// <summary>Gets or sets the declared caches.</summary>
    public List<CachingCache> Caches { get; set; }
}
