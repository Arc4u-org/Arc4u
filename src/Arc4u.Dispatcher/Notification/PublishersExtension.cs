namespace Arc4u.Dispatcher.Notification;

/// <summary>Extension methods to publish a notification to all the registered handlers, either concurrently or one after the other.</summary>
public static class PublishersExtension
{
    /// <summary>
    /// Call each <see cref="INotificationHandler{T}"/> concurrently: the handlers are started one after the other on the calling thread, then awaited together.
    /// A handler that throws before returning its task prevents the following handlers from starting.
    /// </summary>
    /// <typeparam name="T">The type of the object notified</typeparam>
    /// <param name="notifier">The collection of handlers to call.</param>
    /// <param name="param">The notified object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when all the handlers have completed.</returns>
    /// <exception cref="Exception">The first exception raised by a handler is rethrown when the task is awaited.</exception>
    public static async Task PublishWhenAllAsync<T>(this INotificationHandlers<T> notifier, T param, CancellationToken cancellationToken)
    {
        await Task.WhenAll(notifier.Handlers.Select(handler => handler.HandleAsync(param, cancellationToken))
                                   .ToArray()).ConfigureAwait(false);
    }

    /// <summary>Calls every registered <see cref="INotificationHandler{T1, T2}"/> concurrently (all the handlers are started one after the other on the calling thread, then awaited together). A handler that throws before returning its task prevents the following handlers from starting.</summary>
    /// <typeparam name="T1">The type of the notification value number 1.</typeparam>
    /// <typeparam name="T2">The type of the notification value number 2.</typeparam>
    /// <param name="notifier">The collection of handlers to call.</param>
    /// <param name="param1">The notification value number 1.</param>
    /// <param name="param2">The notification value number 2.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when all the handlers have completed.</returns>
    /// <exception cref="Exception">The first exception raised by a handler is rethrown when the task is awaited.</exception>
    public static async Task PublishWhenAllAsync<T1, T2>(this INotificationHandlers<T1, T2> notifier, T1 param1, T2 param2, CancellationToken cancellationToken)
    {
        await Task.WhenAll(notifier.Handlers.Select(handler => handler.HandleAsync(param1, param2, cancellationToken))
                                   .ToArray()).ConfigureAwait(false);
    }

    /// <summary>Calls every registered <see cref="INotificationHandler{T1, T2, T3}"/> concurrently (all the handlers are started one after the other on the calling thread, then awaited together). A handler that throws before returning its task prevents the following handlers from starting.</summary>
    /// <typeparam name="T1">The type of the notification value number 1.</typeparam>
    /// <typeparam name="T2">The type of the notification value number 2.</typeparam>
    /// <typeparam name="T3">The type of the notification value number 3.</typeparam>
    /// <param name="notifier">The collection of handlers to call.</param>
    /// <param name="param1">The notification value number 1.</param>
    /// <param name="param2">The notification value number 2.</param>
    /// <param name="param3">The notification value number 3.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when all the handlers have completed.</returns>
    /// <exception cref="Exception">The first exception raised by a handler is rethrown when the task is awaited.</exception>
    public static async Task PublishWhenAllAsync<T1, T2, T3>(this INotificationHandlers<T1, T2, T3> notifier, T1 param1, T2 param2, T3 param3, CancellationToken cancellationToken)
    {
        await Task.WhenAll(notifier.Handlers.Select(handler => handler.HandleAsync(param1, param2, param3, cancellationToken))
                                   .ToArray()).ConfigureAwait(false);
    }

    /// <summary>Calls every registered <see cref="INotificationHandler{T1, T2, T3, T4}"/> concurrently (all the handlers are started one after the other on the calling thread, then awaited together). A handler that throws before returning its task prevents the following handlers from starting.</summary>
    /// <typeparam name="T1">The type of the notification value number 1.</typeparam>
    /// <typeparam name="T2">The type of the notification value number 2.</typeparam>
    /// <typeparam name="T3">The type of the notification value number 3.</typeparam>
    /// <typeparam name="T4">The type of the notification value number 4.</typeparam>
    /// <param name="notifier">The collection of handlers to call.</param>
    /// <param name="param1">The notification value number 1.</param>
    /// <param name="param2">The notification value number 2.</param>
    /// <param name="param3">The notification value number 3.</param>
    /// <param name="param4">The notification value number 4.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when all the handlers have completed.</returns>
    /// <exception cref="Exception">The first exception raised by a handler is rethrown when the task is awaited.</exception>
    public static async Task PublishWhenAllAsync<T1, T2, T3, T4>(this INotificationHandlers<T1, T2, T3, T4> notifier, T1 param1, T2 param2, T3 param3, T4 param4, CancellationToken cancellationToken)
    {
        await Task.WhenAll(notifier.Handlers.Select(handler => handler.HandleAsync(param1, param2, param3, param4, cancellationToken))
                                   .ToArray()).ConfigureAwait(false);
    }

    /// <summary>Calls every registered <see cref="INotificationHandler{T1, T2, T3, T4, T5}"/> concurrently (all the handlers are started one after the other on the calling thread, then awaited together). A handler that throws before returning its task prevents the following handlers from starting.</summary>
    /// <typeparam name="T1">The type of the notification value number 1.</typeparam>
    /// <typeparam name="T2">The type of the notification value number 2.</typeparam>
    /// <typeparam name="T3">The type of the notification value number 3.</typeparam>
    /// <typeparam name="T4">The type of the notification value number 4.</typeparam>
    /// <typeparam name="T5">The type of the notification value number 5.</typeparam>
    /// <param name="notifier">The collection of handlers to call.</param>
    /// <param name="param1">The notification value number 1.</param>
    /// <param name="param2">The notification value number 2.</param>
    /// <param name="param3">The notification value number 3.</param>
    /// <param name="param4">The notification value number 4.</param>
    /// <param name="param5">The notification value number 5.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when all the handlers have completed.</returns>
    /// <exception cref="Exception">The first exception raised by a handler is rethrown when the task is awaited.</exception>
    public static async Task PublishWhenAllAsync<T1, T2, T3, T4, T5>(this INotificationHandlers<T1, T2, T3, T4, T5> notifier, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, CancellationToken cancellationToken)
    {
        await Task.WhenAll(notifier.Handlers.Select(handler => handler.HandleAsync(param1, param2, param3, param4, param5, cancellationToken))
                                   .ToArray()).ConfigureAwait(false);
    }

    /// <summary>Calls every registered <see cref="INotificationHandler{T}"/> sequentially, in registration order, waiting for each one before calling the next.</summary>
    /// <typeparam name="T">The type of the notified object.</typeparam>
    /// <param name="notifier">The collection of handlers to call.</param>
    /// <param name="param">The notified object.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when all the handlers have completed.</returns>
    /// <exception cref="Exception">An exception raised by a handler is propagated and the remaining handlers are not called.</exception>
    public static async Task PublishForEachAsync<T>(this INotificationHandlers<T> notifier, T param, CancellationToken cancellationToken)
    {
        foreach (var handler in notifier.Handlers)
        {
            await handler.HandleAsync(param, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Calls every registered <see cref="INotificationHandler{T1, T2}"/> sequentially, in registration order, waiting for each one before calling the next.</summary>
    /// <typeparam name="T1">The type of the notification value number 1.</typeparam>
    /// <typeparam name="T2">The type of the notification value number 2.</typeparam>
    /// <param name="notifier">The collection of handlers to call.</param>
    /// <param name="param1">The notification value number 1.</param>
    /// <param name="param2">The notification value number 2.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when all the handlers have completed.</returns>
    /// <exception cref="Exception">An exception raised by a handler is propagated and the remaining handlers are not called.</exception>
    public static async Task PublishForEachAsync<T1, T2>(this INotificationHandlers<T1, T2> notifier, T1 param1, T2 param2, CancellationToken cancellationToken)
    {
        foreach (var handler in notifier.Handlers)
        {
            await handler.HandleAsync(param1, param2, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Calls every registered <see cref="INotificationHandler{T1, T2, T3}"/> sequentially, in registration order, waiting for each one before calling the next.</summary>
    /// <typeparam name="T1">The type of the notification value number 1.</typeparam>
    /// <typeparam name="T2">The type of the notification value number 2.</typeparam>
    /// <typeparam name="T3">The type of the notification value number 3.</typeparam>
    /// <param name="notifier">The collection of handlers to call.</param>
    /// <param name="param1">The notification value number 1.</param>
    /// <param name="param2">The notification value number 2.</param>
    /// <param name="param3">The notification value number 3.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when all the handlers have completed.</returns>
    /// <exception cref="Exception">An exception raised by a handler is propagated and the remaining handlers are not called.</exception>
    public static async Task PublishForEachAsync<T1, T2, T3>(this INotificationHandlers<T1, T2, T3> notifier, T1 param1, T2 param2, T3 param3, CancellationToken cancellationToken)
    {
        foreach (var handler in notifier.Handlers)
        {
            await handler.HandleAsync(param1, param2, param3, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Calls every registered <see cref="INotificationHandler{T1, T2, T3, T4}"/> sequentially, in registration order, waiting for each one before calling the next.</summary>
    /// <typeparam name="T1">The type of the notification value number 1.</typeparam>
    /// <typeparam name="T2">The type of the notification value number 2.</typeparam>
    /// <typeparam name="T3">The type of the notification value number 3.</typeparam>
    /// <typeparam name="T4">The type of the notification value number 4.</typeparam>
    /// <param name="notifier">The collection of handlers to call.</param>
    /// <param name="param1">The notification value number 1.</param>
    /// <param name="param2">The notification value number 2.</param>
    /// <param name="param3">The notification value number 3.</param>
    /// <param name="param4">The notification value number 4.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when all the handlers have completed.</returns>
    /// <exception cref="Exception">An exception raised by a handler is propagated and the remaining handlers are not called.</exception>
    public static async Task PublishForEachAsync<T1, T2, T3, T4>(this INotificationHandlers<T1, T2, T3, T4> notifier, T1 param1, T2 param2, T3 param3, T4 param4, CancellationToken cancellationToken)
    {
        foreach (var handler in notifier.Handlers)
        {
            await handler.HandleAsync(param1, param2, param3, param4, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>Calls every registered <see cref="INotificationHandler{T1, T2, T3, T4, T5}"/> sequentially, in registration order, waiting for each one before calling the next.</summary>
    /// <typeparam name="T1">The type of the notification value number 1.</typeparam>
    /// <typeparam name="T2">The type of the notification value number 2.</typeparam>
    /// <typeparam name="T3">The type of the notification value number 3.</typeparam>
    /// <typeparam name="T4">The type of the notification value number 4.</typeparam>
    /// <typeparam name="T5">The type of the notification value number 5.</typeparam>
    /// <param name="notifier">The collection of handlers to call.</param>
    /// <param name="param1">The notification value number 1.</param>
    /// <param name="param2">The notification value number 2.</param>
    /// <param name="param3">The notification value number 3.</param>
    /// <param name="param4">The notification value number 4.</param>
    /// <param name="param5">The notification value number 5.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that completes when all the handlers have completed.</returns>
    /// <exception cref="Exception">An exception raised by a handler is propagated and the remaining handlers are not called.</exception>
    public static async Task PublishForEachAsync<T1, T2, T3, T4, T5>(this INotificationHandlers<T1, T2, T3, T4, T5> notifier, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, CancellationToken cancellationToken)
    {
        foreach (var handler in notifier.Handlers)
        {
            await handler.HandleAsync(param1, param2, param3, param4, param5, cancellationToken).ConfigureAwait(false);
        }
    }
}
