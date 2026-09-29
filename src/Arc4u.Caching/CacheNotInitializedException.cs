namespace Arc4u.Caching;

/// <summary>The exception thrown when a cache is used before it has been successfully initialized.</summary>
public class CacheNotInitializedException : Exception
{
    const string defaultMessage = "The cache used is not initialized!";
    /// <summary>Initializes a new instance of the <see cref="CacheNotInitializedException"/> class with a default message.</summary>
    public CacheNotInitializedException() : base(defaultMessage) { }

    /// <summary>Initializes a new instance of the <see cref="CacheNotInitializedException"/> class with a message.</summary>
    /// <param name="message">The message that describes why the cache is not initialized.</param>
    public CacheNotInitializedException(string message) : base(message ?? defaultMessage) { }
}
