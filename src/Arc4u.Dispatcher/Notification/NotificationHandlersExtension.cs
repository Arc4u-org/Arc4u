using Microsoft.Extensions.DependencyInjection;

namespace Arc4u.Dispatcher.Notification;
/// <summary>
/// Extension method to register the notification handler collections (<see cref="INotificationHandlers{T}"/> and its overloads with up to five type arguments).
/// </summary>
/// <example>
/// Registration:
/// <code language="csharp">
/// services.AddNotificationHandlersAsScoped();
///
/// // Register the handlers.
/// services.AddScoped&lt;INotificationHandler&lt;Order&gt;, AuditOrderHandler&gt;();
/// </code>
/// Publishing:
/// <code language="csharp">
/// public class OrderService(INotificationHandlers&lt;Order&gt; handlers)
/// {
///     public Task PlaceAsync(Order order, CancellationToken cancellationToken)
///         =&gt; handlers.PublishWhenAllAsync(order, cancellationToken);
/// }
/// </code>
/// </example>
public static class NotificationHandlersExtension
{
    /// <summary>
    /// Register notification handlers to used to publish notifications
    /// </summary>
    /// <param name="services"><see cref="IServiceCollection"/></param>
    /// <param name="lifetime"><see cref="ServiceLifetime"/>. Singleton is registered ad Scoped</param>
    public static void AddNotificationHandlersAsScoped(this IServiceCollection services, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        switch (lifetime)
        {
            case ServiceLifetime.Transient:
                services.AddTransient(typeof(INotificationHandlers<>), typeof(NotificationHandlers<>));
                services.AddTransient(typeof(INotificationHandlers<,>), typeof(NotificationHandlers<,>));
                services.AddTransient(typeof(INotificationHandlers<,,>), typeof(NotificationHandlers<,,>));
                services.AddTransient(typeof(INotificationHandlers<,,,>), typeof(NotificationHandlers<,,,>));
                services.AddTransient(typeof(INotificationHandlers<,,,,>), typeof(NotificationHandlers<,,,,>));
                break;
            default:
                services.AddScoped(typeof(INotificationHandlers<>), typeof(NotificationHandlers<>));
                services.AddScoped(typeof(INotificationHandlers<,>), typeof(NotificationHandlers<,>));
                services.AddScoped(typeof(INotificationHandlers<,,>), typeof(NotificationHandlers<,,>));
                services.AddScoped(typeof(INotificationHandlers<,,,>), typeof(NotificationHandlers<,,,>));
                services.AddScoped(typeof(INotificationHandlers<,,,,>), typeof(NotificationHandlers<,,,,>));
                break;
        }
    }
}
