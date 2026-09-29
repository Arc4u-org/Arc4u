---
description: "Keep the Arc4u log entries of a client application in memory or in a Realm database and read them back to display or export them."
---
# Client-side log storage

A server application sends its logs to a log server or to the console. A client application (a desktop or mobile app) often has neither, and it needs to keep its own logs to show them on a diagnostics screen or to let the user export them. The package `Arc4u.Diagnostics.Serilog.Sinks.RealmDb` provides two [Serilog](serilog.md) sinks for that, one that keeps the entries in memory and one that stores them in a Realm database, and an `ILogStore` to read and clear them. They receive the same entries as any other sink of the [fluent logging API](index.md).

> [!NOTE]
> Realm was used by the mobile and desktop clients of earlier Arc4u versions. The package still builds for `net10.0` and `net11.0`, but check the [package support matrix](../package-support.md) before you start a new project with it. For server applications use the console, file or log server sinks described in [Serilog](serilog.md).

## Install

```bash
dotnet add package Arc4u.Diagnostics.Serilog.Sinks.RealmDb --prerelease
```

The package references `Arc4u.Diagnostics.Serilog`, `Arc4u`, `Realm` and `Serilog.Sinks.PeriodicBatching`. You also need a Serilog host integration such as `Serilog.AspNetCore` or `Serilog.Extensions.Hosting`.

## Configuration

These sinks have no configuration section. The only setting is the Realm database, which is a static factory:

| Member | Type | Default | Description |
|---|---|---|---|
| `RealmDBExtension.DefaultConfig` | `Func<RealmConfiguration>` | file `loggingDB.realm`, `SchemaVersion = 1` | Creates the Realm configuration used by `WriteTo.RealmDB()` and by `AddRealmDBLog()`. Assign it before you call either. |

Both sinks batch the entries: at most 50 entries per batch, a batch every 500 ms (the first entry is written at once), and at most 10,000 entries waiting in the queue.

## Common scenarios

### Keep the entries in memory

`WriteTo.MemoryLogDB()` (namespace `Arc4u.Diagnostics.Serilog.Sinks.Memory`) stores each entry as a `LogMessage` in the static list `MemoryLogDbSink.LogMessages`. `AddMemoryLogDB()` registers that list and an `ILogStore` over it as singletons.

```csharp
// Program.cs
using Arc4u.Dependency;
using Arc4u.Diagnostics.Serilog.Sinks;
using Arc4u.Diagnostics.Serilog.Sinks.Memory;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddILogger();
builder.Services.AddMemoryLogDB();

builder.Host.UseSerilog((context, configuration) => configuration
    .MinimumLevel.Information()
    .WriteTo.MemoryLogDB());

var app = builder.Build();

app.MapGet("/logs", (ILogStore store) => store.GetLogs("", 0, 50));

app.Run();
```

Logging `Technical().Add("OrderId", 42).LogInformation("Order Shipped")` and reading the store back gives one `LogMessage`:

```text
2026-09-29T14:55:29.3585780+02:00 Information Technical Order Shipped {"OrderId":42}
```

The columns are `Timestamp`, `MessageType`, `MessageCategory`, `Message` and `Properties`.

> [!WARNING]
> The list has no size limit and no eviction. It grows for the life of the process until you call `ILogStore.RemoveAll()`. It is also a plain `List<T>`: the batching sink adds entries on a background thread, so a `GetLogs` call that enumerates the list at the same time can throw `Collection was modified`. Catch the exception and retry, or read the store at moments when little is logged.

### Store the entries in a Realm database

`WriteTo.RealmDB()` (namespace `Serilog`) writes the entries to a Realm database as `LogDBMessage` objects. `AddRealmDBLog()` registers the `RealmConfiguration` and an `ILogStore` (`RealmLoggingDbCtx`) as singletons. The second one opens the database when it is first resolved.

```csharp
using Arc4u.Diagnostics.Serilog.Sinks.RealmDb;
using Realms;
using Serilog;

public static class RealmLogging
{
    public static void Register(WebApplicationBuilder builder, string path)
    {
        // Assign the factory before RealmDB() and AddRealmDBLog(), which both call it.
        RealmDBExtension.DefaultConfig = () => new RealmConfiguration(path) { SchemaVersion = 1 };

        builder.Services.AddRealmDBLog();

        builder.Host.UseSerilog((context, configuration) => configuration
            .MinimumLevel.Information()
            .WriteTo.RealmDB());
    }
}
```

Reading back an entry logged with `realmLogger.Information("realm {A}", 1)` gives:

```text
REALM Information Technical realm 1 {"A":1}
```

### Read and clear the entries

Inject `ILogStore` (namespace `Arc4u.Diagnostics.Serilog.Sinks`).

| Member | Description |
|---|---|
| `GetLogs(criteria, skip, take)` | Returns a page of `LogMessage`. `criteria` is text that the message must contain; an empty value applies no filter. |
| `RemoveAll()` | Deletes all the stored entries. |

The memory store returns the entries in the order they were written. The Realm store returns the most recent first. A `LogMessage` has the properties `Timestamp`, `MessageCategory`, `Message`, `MessageType` (the `LogLevel` name), `ClassType`, `MethodName`, `ThreadId`, `ProcessId`, `Application`, `Identity`, `ActivityId`, `Stacktrace` and `Properties` (the non-standard properties as a JSON object).

```csharp
using Arc4u.Diagnostics.Serilog.Sinks;

public class LogPage(ILogStore store)
{
    public List<LogMessage> Latest() => store.GetLogs("", 0, 50);

    public void Clear() => store.RemoveAll();
}
```

## Extensibility points

`ILogStore` and the sinks are independent of each other. Register your own `ILogStore` to read from another store, or write your own Serilog sink with the [Arc4u helper](serilog.md#read-the-arc4u-properties-in-your-own-sink).

## Troubleshooting

### The category of every stored entry is Technical

Known issue. `MemoryLogDB` and `RealmDB` read the category with the same helper as `SimpleTextFormatter`, which falls back to `Technical` because the logger writes the category as a string. See [The category is always Technical](index.md#the-category-is-always-technical).

### GetLogs with a criteria returns nothing on the memory store

Known issue. `MemoryLogStore` lower-cases the criteria and then searches the message with a case-sensitive comparison, so a message that contains a capital letter in the searched text never matches: with the message `Order Shipped`, the criteria `order`, `shipped` and `Shipped` all return no entry. Leave the criteria empty and filter the result yourself. The Realm store compares in lower case on both sides.

### Realm accessed from incorrect thread

Known issue. `RealmLoggingDbCtx` opens its `Realm` in the constructor and is registered as a singleton, and a Realm instance can only be used on the thread that opened it. `GetLogs` and `RemoveAll` called from another thread throw `RealmException: Realm accessed from incorrect thread`. This is what happens in ASP.NET Core, where requests run on thread-pool threads, so the Realm store is not usable from a web endpoint as in the memory example.

The supported pattern is a client application whose reads happen on one thread: resolve `ILogStore` on that thread (for example the UI thread of a desktop application) and call it from there only. The Realm sink that writes the entries opens its own instance on its writer thread and is not affected. In a web application, use the memory store or a server-side sink instead.

### A Realm page holds fewer entries than requested

With a criteria, `RealmLoggingDbCtx.GetLogs` skips `skip` entries, then looks at the next `take` entries and returns those that match. A page can therefore hold fewer than `take` entries even when more entries match further on.

## See also

- [Diagnostics and logging](index.md)
- [Serilog](serilog.md)
- <xref:Arc4u.Diagnostics.Serilog.Sinks.RealmDb> in the API reference
