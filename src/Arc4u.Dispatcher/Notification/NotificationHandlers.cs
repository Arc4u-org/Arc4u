using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Dispatcher.Notification;

/// <summary>
/// Implement <see cref="INotificationHandlers{T}"/> and fetch from the DI the <see cref="INotificationHandler{T}"/>.
/// </summary>
/// <typeparam name="T"></typeparam>
public class NotificationHandlers<T> : INotificationHandlers<T>
{
    /// <summary>Initializes a new instance of the <see cref="NotificationHandlers{T}"/> class and resolves all the registered handlers from <paramref name="serviceProvider"/>.</summary>
    /// <param name="serviceProvider">The service provider used to resolve the handlers.</param>
    public NotificationHandlers(IServiceProvider serviceProvider)
    {
        Handlers = serviceProvider.GetServices<INotificationHandler<T>>();
    }

    /// <inheritdoc/>
    public IEnumerable<INotificationHandler<T>> Handlers { get; }
}

/// <summary>Implements <see cref="INotificationHandlers{T1, T2}"/> and fetches the <see cref="INotificationHandler{T1, T2}"/> registered in the dependency injection container.</summary>
/// <typeparam name="T1">The type of the notification value number 1.</typeparam>
/// <typeparam name="T2">The type of the notification value number 2.</typeparam>
public class NotificationHandlers<T1, T2> : INotificationHandlers<T1, T2>
{
    /// <summary>Initializes a new instance of the <see cref="NotificationHandlers{T1, T2}"/> class and resolves all the registered handlers from <paramref name="serviceProvider"/>.</summary>
    /// <param name="serviceProvider">The service provider used to resolve the handlers.</param>
    public NotificationHandlers(IServiceProvider serviceProvider)
    {
        Handlers = serviceProvider.GetServices<INotificationHandler<T1, T2>>();
    }

    /// <inheritdoc/>
    public IEnumerable<INotificationHandler<T1, T2>> Handlers { get; }
}

/// <summary>Implements <see cref="INotificationHandlers{T1, T2, T3}"/> and fetches the <see cref="INotificationHandler{T1, T2, T3}"/> registered in the dependency injection container.</summary>
/// <typeparam name="T1">The type of the notification value number 1.</typeparam>
/// <typeparam name="T2">The type of the notification value number 2.</typeparam>
/// <typeparam name="T3">The type of the notification value number 3.</typeparam>
public class NotificationHandlers<T1, T2, T3> : INotificationHandlers<T1, T2, T3>
{
    /// <summary>Initializes a new instance of the <see cref="NotificationHandlers{T1, T2, T3}"/> class and resolves all the registered handlers from <paramref name="serviceProvider"/>.</summary>
    /// <param name="serviceProvider">The service provider used to resolve the handlers.</param>
    public NotificationHandlers(IServiceProvider serviceProvider)
    {
        Handlers = serviceProvider.GetServices<INotificationHandler<T1, T2, T3>>();
    }

    /// <inheritdoc/>
    public IEnumerable<INotificationHandler<T1, T2, T3>> Handlers { get; }
}

/// <summary>Implements <see cref="INotificationHandlers{T1, T2, T3, T4}"/> and fetches the <see cref="INotificationHandler{T1, T2, T3, T4}"/> registered in the dependency injection container.</summary>
/// <typeparam name="T1">The type of the notification value number 1.</typeparam>
/// <typeparam name="T2">The type of the notification value number 2.</typeparam>
/// <typeparam name="T3">The type of the notification value number 3.</typeparam>
/// <typeparam name="T4">The type of the notification value number 4.</typeparam>
public class NotificationHandlers<T1, T2, T3, T4> : INotificationHandlers<T1, T2, T3, T4>
{
    /// <summary>Initializes a new instance of the <see cref="NotificationHandlers{T1, T2, T3, T4}"/> class and resolves all the registered handlers from <paramref name="serviceProvider"/>.</summary>
    /// <param name="serviceProvider">The service provider used to resolve the handlers.</param>
    public NotificationHandlers(IServiceProvider serviceProvider)
    {
        Handlers = serviceProvider.GetServices<INotificationHandler<T1, T2, T3, T4>>();
    }

    /// <inheritdoc/>
    public IEnumerable<INotificationHandler<T1, T2, T3, T4>> Handlers { get; }
}

/// <summary>Implements <see cref="INotificationHandlers{T1, T2, T3, T4, T5}"/> and fetches the <see cref="INotificationHandler{T1, T2, T3, T4, T5}"/> registered in the dependency injection container.</summary>
/// <typeparam name="T1">The type of the notification value number 1.</typeparam>
/// <typeparam name="T2">The type of the notification value number 2.</typeparam>
/// <typeparam name="T3">The type of the notification value number 3.</typeparam>
/// <typeparam name="T4">The type of the notification value number 4.</typeparam>
/// <typeparam name="T5">The type of the notification value number 5.</typeparam>
public class NotificationHandlers<T1, T2, T3, T4, T5> : INotificationHandlers<T1, T2, T3, T4, T5>
{
    /// <summary>Initializes a new instance of the <see cref="NotificationHandlers{T1, T2, T3, T4, T5}"/> class and resolves all the registered handlers from <paramref name="serviceProvider"/>.</summary>
    /// <param name="serviceProvider">The service provider used to resolve the handlers.</param>
    public NotificationHandlers(IServiceProvider serviceProvider)
    {
        Handlers = serviceProvider.GetServices<INotificationHandler<T1, T2, T3, T4, T5>>();
    }

    /// <inheritdoc/>
    public IEnumerable<INotificationHandler<T1, T2, T3, T4, T5>> Handlers { get; }
}
