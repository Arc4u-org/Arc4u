namespace Arc4u.Configuration.Store;

using Internals;

/// <summary>
/// A section antity represents a configuration section named <see cref="Key"/> and its value as persisted in some store.
/// The user of this type can only modify the value, not create new configuration sections or delete them: sections are specified by the application.
/// </summary>
public sealed class SectionEntity
{
    /// <summary>
    /// Creates an empty entity. Used by the configuration provider and by object mappers such as EF Core.
    /// </summary>
    public SectionEntity()
    {
    }

    /// <summary>
    /// Creates an entity from its persisted representation, typically in a custom <see cref="ISectionStore"/> that reads its own storage.
    /// </summary>
    /// <param name="key">The name of the section.</param>
    /// <param name="value">The persisted Json representation of the value, exactly as found in <see cref="Value"/> of an entity received in <see cref="ISectionStore.Add"/>.</param>
    public SectionEntity(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        Key = key;
        Value = value;
    }

    /// <summary>
    /// The name of the section holding the value.
    /// This is determined at initialization time and cannot be changed
    /// </summary>
    public string Key { get; internal set; } = default!;

    /// <summary>
    /// A Json representation of the value, wrapped in a <see cref="ValueHolder{TValue}"/>
    /// </summary>
    public string Value { get; internal set; } = default!;

    /// <summary>
    /// Return the value associated with the section.
    /// It is assumed that the caller knows the correct type.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <returns></returns>
    public TValue? GetValue<TValue>()
    {
        return ValueHolder<TValue>.Deserialize(Value);
    }

    /// <summary>
    /// Set the value associated with the section. 
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="value"></param>
    public void SetValue<TValue>(TValue? value)
    {
        // check for multiple wrapping... it should never occur
        if (value is IValueHolder)
        {
            throw new ArgumentException($"The parameter is not expected to implement {typeof(IValueHolder).Name}", nameof(value));
        }

        Value = ValueHolder.Create(value).Serialize();
    }

    /// <summary>
    /// Internal method to set the value directoy using the internal <see cref="IValueHolder"/> interface
    /// </summary>
    /// <param name="valueHolder"></param>
    internal void SetValue(IValueHolder valueHolder)
    {
        Value = valueHolder.Serialize();
    }
}
