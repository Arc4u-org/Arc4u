# Arc4u.Dispatcher

Publish a notification to every handler registered for it in the dependency injection container, in parallel or one after the other.

## Install

```bash
dotnet add package Arc4u.Dispatcher --prerelease
```

## Usage

```csharp
using Arc4u.Dispatcher.Notification;

services.AddNotificationHandlersAsScoped();
services.AddScoped<INotificationHandler<OrderPlaced>, SendConfirmationEmail>();

// Later, in a scope:
var notifier = provider.GetRequiredService<INotificationHandlers<OrderPlaced>>();
await notifier.PublishWhenAllAsync(new OrderPlaced(42), cancellationToken);
```

`OrderPlaced` is your notification type and `SendConfirmationEmail` implements `INotificationHandler<OrderPlaced>`.

## Documentation

- Guide: [Dispatcher](https://arc4u-org.github.io/Arc4u/guides/dispatcher/)
- API reference: [Arc4u.Dispatcher.Notification](https://arc4u-org.github.io/Arc4u/api/Arc4u.Dispatcher.Notification.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
