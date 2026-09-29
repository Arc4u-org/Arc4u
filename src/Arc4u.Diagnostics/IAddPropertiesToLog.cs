namespace Arc4u.Diagnostics;

/// <summary>
/// Provides additional properties that the Arc4u logger adds to every log entry (for example the identity of the current user or a correlation id).
/// Implementations are registered as keyed services with the keys <c>"Scoped"</c> and <c>"Transient"</c>.
/// </summary>
public interface IAddPropertiesToLog
{
    /// <summary>Gets the properties to add to the log entry being written.</summary>
    /// <returns>A dictionary of property names and values. Entries with a <see langword="null"/> value are ignored by the logger.</returns>
    IDictionary<string, object> GetProperties();
}
