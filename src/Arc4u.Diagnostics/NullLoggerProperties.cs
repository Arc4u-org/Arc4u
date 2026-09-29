namespace Arc4u.Diagnostics;

/// <summary>
/// The NullLoggerProperties is used when no properties are added to the log.
/// It is a good practice to use this class when no properties are added to the log like in unit testing.
/// </summary>
public class NullLoggerProperties : IAddPropertiesToLog
{
    /// <inheritdoc/>
    public IDictionary<string, object> GetProperties()
    {
        return new Dictionary<string, object>();
    }
}
