using Microsoft.Extensions.Configuration;

namespace Arc4u.Configuration;

/// <summary>
/// Base class for <see cref="IKeyValueSettings"/> implementations that read all the children of a configuration section as string values.
/// </summary>
public abstract class KeyValueSettings : IKeyValueSettings
{
    private readonly Dictionary<string, string> _properties;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyValueSettings"/> class from a configuration section.
    /// When the section does not exist, the settings are empty.
    /// </summary>
    /// <param name="sectionName">The name of the section whose children are read.</param>
    /// <param name="configuration">The configuration that contains the section.</param>
    public KeyValueSettings(string sectionName, IConfiguration configuration)
    {
        _properties = configuration.GetSection(sectionName)?.GetChildren()?.ToDictionary(x => x.Key, x => x.Value!) ?? throw new ArgumentException($"Section {sectionName} does not exist or doesn't contain a usable value", nameof(sectionName));
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Values => _properties;
}
