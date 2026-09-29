namespace Arc4u.Caching;

/// <summary>
/// Exception used to encapsulate the error from the cache engine.
/// </summary>
public class DataCacheException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="DataCacheException"/> class.</summary>
    /// <param name="message">The message of the error raised by the cache engine.</param>
    public DataCacheException(string message) : base(message)
    {
    }
}
