namespace Arc4u.Configuration.Store.Internals;

/// <summary>
/// A non-generic interface expressing the need for serialization.
/// This is defined to expose non-generic operations on a <see cref="ValueHolder{TValue}"/> easily.
/// </summary>
interface IValueHolder
{
    string Serialize();
}
