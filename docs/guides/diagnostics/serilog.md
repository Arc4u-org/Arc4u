---
description: "Send Arc4u log entries to Serilog sinks, format them, split them by category and hide the user identity."
---
# Serilog

Arc4u sends its log entries to whatever `ILoggerFactory` providers you configure, and [Serilog](https://serilog.net/) is the provider the Arc4u packages are built and tested with. This page shows how to connect Serilog, what an Arc4u entry looks like to a Serilog sink, how to configure sinks in `appsettings.json`, and what `Arc4u.Diagnostics.Serilog` adds. It builds on the [fluent logging API](index.md) and on the [design principles](../../concepts/design-principles.md) of Arc4u.

## Connect Serilog

Add the Arc4u logger, then let Serilog take over `Microsoft.Extensions.Logging` with `UseSerilog` (package `Serilog.AspNetCore`). Everything the Arc4u wrapper writes goes to the sinks you configure.

```csharp
// Program.cs
using Arc4u.Dependency;
using Arc4u.Diagnostics.Formatter;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddILogger();

builder.Host.UseSerilog((context, configuration) => configuration
    .MinimumLevel.Information()
    .WriteTo.Console(new SimpleTextFormatter()));

var app = builder.Build();
app.Run();
```

Without `SimpleTextFormatter`, `Arc4u.Diagnostics.Serilog` is not needed for the connection to work; the package provides the formatter and the other types described below.

## What a sink receives

The Arc4u wrapper passes the properties as the log state, so Serilog turns each of them into a property of the `LogEvent`. Here is a Monitoring entry, written by a fresh logger to a sink that prints Serilog's `JsonFormatter`:

```json
{"Timestamp":"2026-09-29T15:19:02.4535660+02:00","Level":"Information","MessageTemplate":"Cpu sample","Properties":{"Cpu":12.5,"Method":"<Main>$","SourceContext":"Program","Category":"Monitoring","Application":"Chk","Tid":1,"Pid":85565}}
```

`Category` is a string (`Technical`, `Business` or `Monitoring`). See [Properties written on every entry](index.md#properties-written-on-every-entry) for the other names.

## Configure sinks in appsettings.json

Serilog reads its configuration from the `Serilog` section with `ReadFrom.Configuration` (package `Serilog.Settings.Configuration`, included in `Serilog.AspNetCore`):

```csharp
using Arc4u.Dependency;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddILogger();

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration));
```

The section below creates one sub-logger per destination, so each destination has its own minimum level and filters. The root `MinimumLevel` is the lowest level any sub-logger can receive.

```json
{
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.File",
      "Serilog.Sinks.Seq",
      "Serilog.Expressions"
    ],
    "MinimumLevel": {
      "Default": "Verbose",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Logger",
        "Args": {
          "configureLogger": {
            "MinimumLevel": "Information",
            "Filter": [
              {
                "Name": "ByExcluding",
                "Args": { "expression": "Category = 'Monitoring'" }
              }
            ],
            "WriteTo": [
              {
                "Name": "Console",
                "Args": {
                  "formatter": "Arc4u.Diagnostics.Formatter.SimpleTextFormatter, Arc4u.Diagnostics.Serilog"
                }
              }
            ]
          }
        }
      },
      {
        "Name": "Logger",
        "Args": {
          "configureLogger": {
            "MinimumLevel": "Debug",
            "WriteTo": [
              {
                "Name": "Seq",
                "Args": { "serverUrl": "http://localhost:5341" }
              }
            ]
          }
        }
      },
      {
        "Name": "Logger",
        "Args": {
          "configureLogger": {
            "MinimumLevel": "Debug",
            "WriteTo": [
              {
                "Name": "File",
                "Args": {
                  "path": "logs/log-.txt",
                  "rollingInterval": "Day",
                  "formatter": "Arc4u.Diagnostics.Formatter.SimpleTextFormatter, Arc4u.Diagnostics.Serilog"
                }
              }
            ]
          }
        }
      }
    ]
  }
}
```

- **Console**: Information and above, without the Monitoring entries, formatted by `SimpleTextFormatter`. In a container or a Kubernetes pod the console is what `kubectl logs <pod>` shows. Raise its `MinimumLevel` to `Warning` and the operators only see the entries that need attention, while Seq keeps everything.
- **Seq**: Debug and above, with all categories, so you can also watch the Monitoring entries (for example to spot a memory leak).
- **File**: a rolling text file, for machines where no log server is available. Prefer a log server to files.

The `"Using"` entries are Serilog assembly names: add the sink packages you use (`Serilog.Sinks.Seq`, `Serilog.Expressions`, ...) to the project. The `formatter` argument is an assembly-qualified type name, which is how a JSON configuration selects `SimpleTextFormatter`.

With that configuration, the console shows the Technical and Business entries and skips the Monitoring one, while the file receives all of them:

```text
29/09/2026 14:52:23,038  Information   Technical   25688  1   Program - <Main>$ - technical entry -
29/09/2026 14:52:23,050  Information   Technical   25688  1   Program - <Main>$ - business entry - {"OrderId":42}
```

## Enrichers

A Serilog enricher adds a property to every event, whatever wrote it. Add them in the `Enrich` array of the `Serilog` section (in the section above, next to `"MinimumLevel"`); the enricher packages are listed in `"Using"` like the sinks:

```json
{
  "Serilog": {
    "Using": [ "Serilog.Enrichers.Environment" ],
    "Enrich": [ "WithMachineName" ],
    "Properties": { "Environment": "Production" }
  }
}
```

`WithMachineName` (package `Serilog.Enrichers.Environment`) adds `MachineName`, and `Properties` adds a fixed property. Serilog enrichers run for entries written through the Arc4u logger and for entries written by libraries that use plain `ILogger`. The Arc4u standard properties (`Application`, `Pid`, `Tid`, `SourceContext`, `Method`, `Category`) are only added by the Arc4u logger, so use an enricher for a property that must be on every entry. An enricher does not replace a property with the same name that the event already has.

## Text output format

`SimpleTextFormatter` writes one line per entry. The fixed columns are padded, so that the lines of a console or a file line up:

| Order | Content |
|---|---|
| 1 | Timestamp, `dd/MM/yyyy HH:mm:ss,fff` |
| 2 | Level: `Trace`, `Debug`, `Information`, `Warning`, `Error` or `Critical` |
| 3 | Category (see the known issue below) |
| 4 | `Identity` |
| 5, 6 | `Pid` and `Tid` (`-1` when the entry did not come from the Arc4u logger) |
| 7 | `ActivityId` |
| 8 | `SourceContext`, then `Method`, then the message, separated by ` - ` |
| last | The other properties as one JSON object |

An exception is written on the following lines, one line per exception and inner exception (`|---Type : message`), followed by the stack trace when `AddStackTrace()` was used. The formatter never throws: an error while formatting is swallowed so that logging cannot fail the application.

`JsonPropertiesFormatter` (used by the formatter) writes a list of Serilog properties as a single JSON object; you can use it in your own sinks.

## Filter by category

To route Monitoring entries to their own sink, or to keep them out of one, filter on the `Category` string with a [Serilog.Expressions](https://github.com/serilog/serilog-expressions) filter. The JSON above already does it with `ByExcluding`. The same in code (package `Serilog.Expressions`):

```csharp
using Arc4u.Diagnostics.Formatter;
using Serilog;

public static class CategoryRouting
{
    public static LoggerConfiguration Configure(LoggerConfiguration configuration) => configuration
        .WriteTo.Logger(l => l
            .Filter.ByExcluding("Category = 'Monitoring'")
            .WriteTo.Console(new SimpleTextFormatter()))
        .WriteTo.Logger(l => l
            .Filter.ByIncludingOnly("Category = 'Monitoring'")
            .WriteTo.File("monitoring-.txt", rollingInterval: RollingInterval.Day));
}
```

> [!WARNING]
> Known issue: `WriteTo.CategoryFilter(...)` (the `CategoryFilterSink` of `Arc4u.Diagnostics.Serilog`) forwards nothing. The logger writes `Category` as a string and the sink reads it as a number, so no event matches and the wrapped sink never receives an entry. Use the expression filter above until this is fixed.
>
> The same mismatch is why an old filter such as `Category = 4` no longer selects the Monitoring entries: compare with the string `'Monitoring'`.

## Hide the user identity

`WriteTo.Anonymizer(sink)` wraps a sink and removes the `Identity` property from the events before the sink receives them. It takes a Serilog sink instance (a `Serilog.Core.Logger` is one), so build the target with its own `LoggerConfiguration`:

```csharp
using Arc4u.Diagnostics.Formatter;
using Serilog;

public static class Anonymized
{
    public static LoggerConfiguration Configure(LoggerConfiguration configuration)
    {
        var anonymous = new LoggerConfiguration()
            .WriteTo.Console(new SimpleTextFormatter())
            .CreateLogger();

        return configuration
            .WriteTo.Console(new SimpleTextFormatter())
            .WriteTo.Anonymizer(anonymous);
    }
}
```

With a provider that sets `Identity` to `alice`, the first sink prints `alice` in the identity column and the anonymized sink prints an empty column:

```text
29/09/2026 14:53:47,920  Information   Technical      alice           28853  1   Program - <Main>$ - hello - {"Tenant":"contoso"}
29/09/2026 14:53:47,920  Information   Technical                      28853  1   Program - <Main>$ - hello - {"Tenant":"contoso"}
```

Only the `Identity` property is removed. Any other property that holds personal data (for example one added with `Add`) is still written.

## Read the Arc4u properties in your own sink

`Helper.ExtractEventInfo(LogEvent)` (namespace `Arc4u.Diagnostics`) returns a tuple with the standard properties of an event and the list of all the other properties. `Helper.ToMessageType` converts a `LogEventLevel` to a `LogLevel`. `SimpleTextFormatter`, `MemoryLogDB` and `RealmDB` use them.

```csharp
using Arc4u.Diagnostics;
using Serilog.Core;
using Serilog.Events;

public class ClassAndMethodSink : ILogEventSink
{
    public void Emit(LogEvent logEvent)
    {
        var info = Helper.ExtractEventInfo(logEvent);

        Console.WriteLine($"{info.ClassType}.{info.MethodName}: {logEvent.RenderMessage()} ({info.Properties.Count} other properties)");
    }
}
```

The `Category` element of the tuple is affected by the [known issue](index.md#the-category-is-always-technical): it is `Technical` for every event. Read `logEvent.Properties["Category"]` (a `ScalarValue` holding a string) when you need the category.

## Write your own Serilog writer

`SerilogWriter` (namespace `Arc4u.Diagnostics.Serilog`) is a base class for a component that owns a Serilog logger. Derive from it and override `Configure`. `Initialize()` creates the logger with the minimum level `Verbose` and the log context enricher, then calls `Configure` once; `Logger` returns the created logger and `Dispose()` disposes it. Arc4u does not call `Initialize()` for you.

```csharp
using Arc4u.Diagnostics.Formatter;
using Arc4u.Diagnostics.Serilog;
using Serilog;

public class ConsoleWriter : SerilogWriter
{
    public override void Configure(LoggerConfiguration configurator)
    {
        configurator.WriteTo.Console(new SimpleTextFormatter());
    }
}
```

## Troubleshooting

### Nothing is written to the console or the file

Check, in this order: `UseSerilog` is called on the host; the sink package is referenced and listed in `"Using"`; the sub-logger `MinimumLevel` is not higher than the level you log; a `Filter` does not exclude the entry. Serilog writes sink failures to its self log: add `Serilog.Debugging.SelfLog.Enable(Console.Error)` before you build the logger when a sink stays silent.

### The Category column always shows Technical, and CategoryFilter forwards nothing

Known issue, described in [Filter by category](#filter-by-category) and in the [fluent API troubleshooting](index.md#the-category-is-always-technical).

### Properties from an earlier entry appear on a later one

Known issue, described in [Properties added with Add stay on later entries](index.md#properties-added-with-add-stay-on-later-entries).

## See also

- [Diagnostics and logging](index.md)
- [Client-side log storage](frontend-logging.md)
- <xref:Arc4u.Diagnostics.Serilog> in the API reference
