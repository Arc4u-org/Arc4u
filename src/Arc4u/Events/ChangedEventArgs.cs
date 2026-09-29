namespace Arc4u.Events;

/// <summary>
/// Event data of an event raised when a value changes. It carries the previous and the new value.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
public class ChangedEventArgs<TValue> : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChangedEventArgs{TValue}"/> class.
    /// </summary>
    /// <param name="oldValue">The value before the change.</param>
    /// <param name="newValue">The value after the change.</param>
    public ChangedEventArgs(TValue oldValue, TValue newValue)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    /// <summary>
    /// Gets the value before the change.
    /// </summary>
    public TValue OldValue { get; }
    /// <summary>
    /// Gets the value after the change.
    /// </summary>
    public TValue NewValue { get; }
}
