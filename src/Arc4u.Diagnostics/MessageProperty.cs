namespace Arc4u.Diagnostics;

/// <summary>Extension methods to add entries to a dictionary of log properties.</summary>
public static class MessagePropertyEx
{
    /// <summary>Adds the property only when <paramref name="value"/> is not <see langword="null"/> and the key is not already present.</summary>
    /// <param name="properties">The dictionary of properties.</param>
    /// <param name="key">The property name.</param>
    /// <param name="value">The property value.</param>
    public static void AddIfNotExist(this IDictionary<string, object?> properties, string key, object value)
    {
        if (value == null || properties.ContainsKey(key))
        {
            return;
        }

        properties[key] = value;
    }

    /// <summary>Sets the property, replacing any existing value for the key.</summary>
    /// <param name="properties">The dictionary of properties.</param>
    /// <param name="key">The property name.</param>
    /// <param name="value">The property value.</param>
    public static void AddOrReplace(this IDictionary<string, object?> properties, string key, object value)
    {
        properties[key] = value;
    }
}
