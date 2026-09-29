namespace Arc4u.Caching;

/// <summary>The exception thrown when a key is registered more than once.</summary>
public class KeyAlreadyRegisteredException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="KeyAlreadyRegisteredException"/> class.</summary>
    /// <param name="key">The key that is already registered; it is also the exception message.</param>
    public KeyAlreadyRegisteredException(string key) : base(key) { }
}
