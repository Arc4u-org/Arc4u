namespace Arc4u.Dispatcher.Notification;

/// <summary>
/// Interface to return the registered <see cref="INotificationHandler{T}"/> in the DI.
/// </summary>
/// <typeparam name="T">The type of the object.</typeparam>
public interface INotificationHandlers<T>
{
    /// <summary>Gets the registered handlers.</summary>
    IEnumerable<INotificationHandler<T>> Handlers { get; }
}

/// <summary>Gives access to all the <see cref="INotificationHandler{T1, T2}"/> registered in the dependency injection container.</summary>
/// <typeparam name="T1">The type of the notification value number 1.</typeparam>
/// <typeparam name="T2">The type of the notification value number 2.</typeparam>
public interface INotificationHandlers<T1, T2>
{
    /// <summary>Gets the registered handlers.</summary>
    IEnumerable<INotificationHandler<T1, T2>> Handlers { get; }
}

/// <summary>Gives access to all the <see cref="INotificationHandler{T1, T2, T3}"/> registered in the dependency injection container.</summary>
/// <typeparam name="T1">The type of the notification value number 1.</typeparam>
/// <typeparam name="T2">The type of the notification value number 2.</typeparam>
/// <typeparam name="T3">The type of the notification value number 3.</typeparam>
public interface INotificationHandlers<T1, T2, T3>
{
    /// <summary>Gets the registered handlers.</summary>
    IEnumerable<INotificationHandler<T1, T2, T3>> Handlers { get; }
}

/// <summary>Gives access to all the <see cref="INotificationHandler{T1, T2, T3, T4}"/> registered in the dependency injection container.</summary>
/// <typeparam name="T1">The type of the notification value number 1.</typeparam>
/// <typeparam name="T2">The type of the notification value number 2.</typeparam>
/// <typeparam name="T3">The type of the notification value number 3.</typeparam>
/// <typeparam name="T4">The type of the notification value number 4.</typeparam>
public interface INotificationHandlers<T1, T2, T3, T4>
{
    /// <summary>Gets the registered handlers.</summary>
    IEnumerable<INotificationHandler<T1, T2, T3, T4>> Handlers { get; }
}

/// <summary>Gives access to all the <see cref="INotificationHandler{T1, T2, T3, T4, T5}"/> registered in the dependency injection container.</summary>
/// <typeparam name="T1">The type of the notification value number 1.</typeparam>
/// <typeparam name="T2">The type of the notification value number 2.</typeparam>
/// <typeparam name="T3">The type of the notification value number 3.</typeparam>
/// <typeparam name="T4">The type of the notification value number 4.</typeparam>
/// <typeparam name="T5">The type of the notification value number 5.</typeparam>
public interface INotificationHandlers<T1, T2, T3, T4, T5>
{
    /// <summary>Gets the registered handlers.</summary>
    IEnumerable<INotificationHandler<T1, T2, T3, T4, T5>> Handlers { get; }
}
