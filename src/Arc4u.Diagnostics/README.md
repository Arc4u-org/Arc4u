# Arc4u.Diagnostics

Fluent logging API on top of `Microsoft.Extensions.Logging`: log entries get a category (Technical, Business or Monitoring), standard properties and your own properties. Also provides process monitoring and the `ActivitySource` factory used for OpenTelemetry traces.

## Install

```bash
dotnet add package Arc4u.Diagnostics --prerelease
```

## Usage

```csharp
builder.Services.AddILogger();

// In a class that receives ILogger<OrderService>:
logger.Business().Add("OrderId", 42).LogInformation("Order shipped");
```

## Documentation

- Guide: [Diagnostics and logging](https://arc4u-org.github.io/Arc4u/guides/diagnostics/)
- API reference: [Arc4u.Diagnostics](https://arc4u-org.github.io/Arc4u/api/Arc4u.Diagnostics.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
