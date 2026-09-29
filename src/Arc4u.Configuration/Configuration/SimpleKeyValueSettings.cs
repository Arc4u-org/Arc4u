namespace Arc4u.Configuration;

/// <summary>
/// A simple in-memory <see cref="IKeyValueSettings"/>. Instances created without a dictionary, from tuples or with
/// <see cref="CreateFrom(IKeyValueSettings)"/> use case-insensitive keys.
/// Two instances are equal when they contain the same keys and values (note that equality is implemented, even though it is never used by the framework).
/// </summary>
public class SimpleKeyValueSettings : IKeyValueSettings, IEquatable<SimpleKeyValueSettings>
{
    /// <summary>
    /// Initializes a new, empty instance with case-insensitive keys.
    /// </summary>
    public SimpleKeyValueSettings()
    {
        _keyValues = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Initializes a new instance that wraps the specified dictionary (it is not copied).
    /// </summary>
    /// <param name="keyValues">The dictionary holding the settings.</param>
    public SimpleKeyValueSettings(Dictionary<string, string> keyValues)
    {
        _keyValues = keyValues;
    }

    /// <summary>
    /// Initializes a new instance with a copy of the specified settings. The copy uses the default (case-sensitive) key comparer.
    /// </summary>
    /// <param name="keyValues">The settings to copy.</param>
    public SimpleKeyValueSettings(IReadOnlyDictionary<string, string> keyValues)
    {
        _keyValues = keyValues.ToDictionary();
    }

    /// <summary>
    /// Initializes a new instance from key/value tuples, with case-insensitive keys.
    /// </summary>
    /// <param name="keyValues">The settings.</param>
    /// <exception cref="ArgumentException">Two tuples have the same key (ignoring case).</exception>
    public SimpleKeyValueSettings(params (string key, string value)[] keyValues)
    {
        _keyValues = keyValues.Select(x => (x.key, x.value)).ToDictionary(x => x.key, x => x.value, StringComparer.OrdinalIgnoreCase);
    }
    /// <summary>
    /// Creates a new instance, with case-insensitive keys, containing a copy of the values of another settings object.
    /// </summary>
    /// <param name="source">The settings to copy.</param>
    /// <returns>A new <see cref="SimpleKeyValueSettings"/>.</returns>
    public static SimpleKeyValueSettings CreateFrom(IKeyValueSettings source)
    {
        var keyValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var keyPair in source.Values)
        {
            keyValues.Add(keyPair.Key, keyPair.Value);
        }

        return new SimpleKeyValueSettings(keyValues);
    }

    /// <summary>
    /// Adds a setting.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    /// <exception cref="ArgumentException">The key already exists.</exception>
    public void Add(string key, string value)
    {
        _keyValues.Add(key, value);
    }

    /// <summary>
    /// Adds a setting only when the value is neither <see langword="null"/>, empty nor white space.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value, which may be <see langword="null"/>.</param>
    /// <exception cref="ArgumentException">The value is provided and the key already exists.</exception>
    public void AddifNotNullOrEmpty(string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            _keyValues.Add(key, value!);
        }
    }
    private readonly Dictionary<string, string> _keyValues;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, string> Values => _keyValues;

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = 0;
        foreach (var value in Values)
        {
            hash ^= value.GetHashCode();
        }

        return hash;
    }

    /// <summary>
    /// Determines whether another instance contains the same number of settings with the same keys and values.
    /// </summary>
    /// <param name="other">The instance to compare with.</param>
    /// <returns><see langword="true"/> if both contain the same settings; otherwise <see langword="false"/>.</returns>
    public bool Equals(SimpleKeyValueSettings? other)
    {
        if (other != null && other._keyValues.Count == _keyValues.Count)
        {
            foreach (var keyValue in _keyValues)
            {
                if (!other._keyValues.TryGetValue(keyValue.Key, out var value) || value != keyValue.Value)
                {
                    return false;
                }
            }

            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is SimpleKeyValueSettings other && Equals(other);
    }
}
