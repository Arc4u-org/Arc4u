namespace Arc4u.Dispatcher.Notification;

/// <summary>
/// Will be used to implement a notification.
/// </summary>
/// <typeparam name="T">The type of the object notified.</typeparam>
public interface INotificationHandler<T>
{
    /// <summary>Handles the notification.</summary>
    /// <param name="entity">The notified object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the notification is handled.</returns>
    Task HandleAsync(T entity, CancellationToken cancellationToken);
}

/// <summary>Implemented to react to a notification carrying 2 values. Register the implementations in the dependency injection container; they are resolved through <see cref="INotificationHandlers{T1, T2}"/>.</summary>
/// <typeparam name="T1">The type of the notification value number 1.</typeparam>
/// <typeparam name="T2">The type of the notification value number 2.</typeparam>
public interface INotificationHandler<T1, T2>
{
    /// <summary>Handles the notification.</summary>
    /// <param name="param1">The notification value number 1.</param>
    /// <param name="param2">The notification value number 2.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the notification is handled.</returns>
    Task HandleAsync(T1 param1, T2 param2, CancellationToken cancellationToken);
}

/// <summary>Implemented to react to a notification carrying 3 values. Register the implementations in the dependency injection container; they are resolved through <see cref="INotificationHandlers{T1, T2, T3}"/>.</summary>
/// <typeparam name="T1">The type of the notification value number 1.</typeparam>
/// <typeparam name="T2">The type of the notification value number 2.</typeparam>
/// <typeparam name="T3">The type of the notification value number 3.</typeparam>
public interface INotificationHandler<T1, T2, T3>
{
    /// <summary>Handles the notification.</summary>
    /// <param name="param1">The notification value number 1.</param>
    /// <param name="param2">The notification value number 2.</param>
    /// <param name="param3">The notification value number 3.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the notification is handled.</returns>
    Task HandleAsync(T1 param1, T2 param2, T3 param3, CancellationToken cancellationToken);
}

/// <summary>Implemented to react to a notification carrying 4 values. Register the implementations in the dependency injection container; they are resolved through <see cref="INotificationHandlers{T1, T2, T3, T4}"/>.</summary>
/// <typeparam name="T1">The type of the notification value number 1.</typeparam>
/// <typeparam name="T2">The type of the notification value number 2.</typeparam>
/// <typeparam name="T3">The type of the notification value number 3.</typeparam>
/// <typeparam name="T4">The type of the notification value number 4.</typeparam>
public interface INotificationHandler<T1, T2, T3, T4>
{
    /// <summary>Handles the notification.</summary>
    /// <param name="param1">The notification value number 1.</param>
    /// <param name="param2">The notification value number 2.</param>
    /// <param name="param3">The notification value number 3.</param>
    /// <param name="param4">The notification value number 4.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the notification is handled.</returns>
    Task HandleAsync(T1 param1, T2 param2, T3 param3, T4 param4, CancellationToken cancellationToken);
}

/// <summary>Implemented to react to a notification carrying 5 values. Register the implementations in the dependency injection container; they are resolved through <see cref="INotificationHandlers{T1, T2, T3, T4, T5}"/>.</summary>
/// <typeparam name="T1">The type of the notification value number 1.</typeparam>
/// <typeparam name="T2">The type of the notification value number 2.</typeparam>
/// <typeparam name="T3">The type of the notification value number 3.</typeparam>
/// <typeparam name="T4">The type of the notification value number 4.</typeparam>
/// <typeparam name="T5">The type of the notification value number 5.</typeparam>
public interface INotificationHandler<T1, T2, T3, T4, T5>
{
    /// <summary>Handles the notification.</summary>
    /// <param name="param1">The notification value number 1.</param>
    /// <param name="param2">The notification value number 2.</param>
    /// <param name="param3">The notification value number 3.</param>
    /// <param name="param4">The notification value number 4.</param>
    /// <param name="param5">The notification value number 5.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when the notification is handled.</returns>
    Task HandleAsync(T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, CancellationToken cancellationToken);
}
