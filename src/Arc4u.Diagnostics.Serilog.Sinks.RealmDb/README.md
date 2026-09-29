# Arc4u.Diagnostics.Serilog.Sinks.RealmDb

Serilog sinks that keep the Arc4u log entries in memory or in a Realm database, and an `ILogStore` to read them back, for client applications that display their own logs.

## Install

```bash
dotnet add package Arc4u.Diagnostics.Serilog.Sinks.RealmDb --prerelease
```

## Usage

```csharp
builder.Services.AddMemoryLogDB();

builder.Host.UseSerilog((context, configuration) => configuration
    .WriteTo.MemoryLogDB());

// Later, inject ILogStore and call store.GetLogs("", 0, 50).
```

`AddMemoryLogDB` and `MemoryLogDB` are in `Arc4u.Diagnostics.Serilog.Sinks.Memory`; use `AddRealmDBLog` and `WriteTo.RealmDB()` to store the entries in Realm instead.

## Documentation

- Guide: [Client-side log storage](https://arc4u-org.github.io/Arc4u/guides/diagnostics/frontend-logging.html)
- API reference: [Arc4u.Diagnostics.Serilog.Sinks.RealmDb](https://arc4u-org.github.io/Arc4u/api/Arc4u.Diagnostics.Serilog.Sinks.RealmDb.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
