namespace Arc4u.Security.CustomRootCA;

/// <summary>
/// The names of the <see cref="CARootOption"/> registered with <c>AddCustomRootCA</c> in a service collection.
/// The named options cannot otherwise be listed; the names are kept per service collection (and not in a static list) so that
/// several hosts in the same process (for example integration tests) do not see the root CAs of each other.
/// </summary>
public sealed class CARootOptionKeys
{
    /// <summary>
    /// Gets the names of the registered <see cref="CARootOption"/>, without duplicates.
    /// </summary>
    public List<string> Keys { get; } = [];
}
