namespace Arc4u.Diagnostics;

/// <summary>The exception thrown when a custom log property uses a key reserved by <see cref="LoggingConstants"/>.</summary>
public class ReservedLoggingKeyException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ReservedLoggingKeyException"/> class.</summary>
    /// <param name="key">The reserved key that was used; it is also the exception message.</param>
    public ReservedLoggingKeyException(string key) : base(key)
    {
    }
}
