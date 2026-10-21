---
description: "How far each Arc4u package supports trimming and Native AOT, the APIs to use in a trimmed or AOT application, and how the Arc4u build checks it."
---
# Native AOT and trimming

Arc4u 9 can be used in an application published with `PublishAot=true` or `PublishTrimmed=true`. This page
tells you what Arc4u guarantees, which APIs to call so that your application publishes without
warnings, and which packages depend on libraries that are not AOT-compatible themselves.

## What Arc4u guarantees

- Every package except `Arc4u.AspNetCore.Versioning` and `Arc4u.Dependency.Tool` is marked
  `IsAotCompatible`. Its build fails on any trimming (`IL2xxx`), single-file (`IL3000`–`IL3005`) or
  AOT (`IL3050`–`IL3058`) analyzer warning.
- An API that needs reflection by design is marked `[RequiresUnreferencedCode]` and, when it builds
  code at run time, `[RequiresDynamicCode]`. In your application, the warning appears where you call
  it, not inside Arc4u, and the message names the API to use instead.
- Every pull request publishes an application that references all these packages with
  `PublishAot=true`, and fails on any warning in Arc4u code (see [How Arc4u checks it](#how-arc4u-checks-it)).

What Arc4u cannot guarantee is the behavior of the libraries it builds on. The
[package table](#package-status) lists the packages whose dependencies report their own warnings.

## APIs to use in a trimmed or AOT application

| Area | Reflection-based API (warns) | API to use instead |
|---|---|---|
| Dependency injection | None: the registrations are generated at compile time. | See [Publish with trimming or Native AOT](dependency-injection/index.md#publish-with-trimming-or-native-aot). |
| Cache serialization | `new JsonSerialization()`, `new JsonSerialization(options)` (and the compressed variants) | `new JsonSerialization(AppJsonContext.Default)`. See [Serialization](caching/serialization.md). |
| Dapr cache | `DaprCacheOption.JsonSerializerOptions` left `null` | Set `JsonSerializerOptions` with your `JsonSerializerContext`. See [Dapr](caching/dapr.md#native-aot-and-trimming). |
| Configuration store | `Add<T>(key)`, `Add(key, value)`, `AddFromConfiguration(key, bind)`, `SectionEntity.GetValue<T>()`, `SetValue(value)`, `TryGetValueAsync<T>`, `TrySetValueAsync` | The overloads that take a `JsonTypeInfo<T>`, with `AddFromConfiguration`. See [Configuration store](configuration/configuration-store.md). |
| Configuration from resources | `ConfigurationHelper.GetConfigurationFromResourceStream("Assembly, resource")` | `GetConfigurationFromResourceStream(typeof(Program).Assembly, "resource")` |
| Include graphs | `new Graph<T>(paths)`, `Graph<T>.Includes` setter, `Graph<T>.Parse` | `new Graph<T>()` and `Include(expression)`. See [Include graphs](data/graph.md). |
| EF Core includes | `GraphExtension` (`Include`, `ApplyReferences`, ...) | None: the includes are built with reflection. EF Core itself is not AOT-compatible. |

The token cache of desktop and mobile clients (`CacheTokens`, in `Arc4u.OAuth2.Client.Authentication`)
needs nothing from you: the tokens, claims and strings that Arc4u stores in it are serialized with
source-generated metadata. Another type stored in it throws a `NotSupportedException` in a Native AOT
application.

A source-generated context must declare every type you serialize with it:

```csharp
// AppJsonContext.cs
using System.Text.Json.Serialization;

[JsonSerializable(typeof(FeatureOptions))]
[JsonSerializable(typeof(Report))]
internal sealed partial class AppJsonContext : JsonSerializerContext;
```

## Package status

| Status | Meaning |
|---|---|
| **Clean** | Neither the package nor its dependencies report a trimming or AOT warning. |
| **Clean, dependency warns** | The package itself is clean, but a library it depends on reports warnings in its own code. Test your scenarios after publishing. |
| **Not supported** | The package is not AOT-compatible. |

| Package | Status | Note |
|---|---|---|
| `Arc4u`, `Arc4u.Core`, `Arc4u.Threading` | Clean | `Graph<T>` has reflection-based members, see above. |
| `Arc4u.Dependency` | Clean | |
| `Arc4u.Dependency.Tool` | Not applicable | Source generator, runs at build time only. |
| `Arc4u.Configuration`, `Arc4u.Configuration.Decryptor`, `Arc4u.Configuration.Store` | Clean | |
| `Arc4u.Configuration.Store.EfCore` | Clean, dependency warns | Entity Framework Core. |
| `Arc4u.Diagnostics`, `Arc4u.Diagnostics.Serilog` | Clean | |
| `Arc4u.Diagnostics.Serilog.Sinks.RealmDb` | Clean, dependency warns | Realm. |
| `Arc4u.OAuth2`, `Arc4u.OAuth2.AspNetCore`, `Arc4u.OAuth2.AspNetCore.Authentication`, `Arc4u.Authorization` | Clean | |
| `Arc4u.OAuth2.Client`, `Arc4u.OAuth2.Client.Authentication`, `Arc4u.OAuth2.AspNetCore.Blazor` | Clean | |
| `Arc4u.OAuth2.Blazor` | Clean, dependency warns | Blazor WebAssembly and JSInterop. A WebAssembly application is trimmed, not published with Native AOT. |
| `Arc4u.Caching`, `Arc4u.Caching.Memory`, `Arc4u.Caching.Redis` | Clean | |
| `Arc4u.Caching.SqlServer` | Clean, dependency warns | Microsoft.Data.SqlClient and System.Configuration.ConfigurationManager. |
| `Arc4u.Caching.Dapr` | Clean, dependency warns | Dapr.Client serializes with `JsonSerializer` and options without type information. It works when its options carry a `JsonSerializerContext`, but the trimmer cannot see it. |
| `Arc4u.Serializer` | Clean | |
| `Arc4u.Serializer.JSon` | Clean, dependency warns | Microsoft.IO.RecyclableMemoryStream, in its `EventSource` tracing. |
| `Arc4u.Results`, `Arc4u.AspNetCore.Results`, `Arc4u.FluentValidation` | Clean | |
| `Arc4u.gRPC`, `Arc4u.AspNetCore.gRpc` | Clean | |
| `Arc4u.AspNetCore.Versioning` | Not supported | MVC and the API explorer are not trimmable. |
| `Arc4u.EfCore` | Clean, dependency warns | Entity Framework Core. `GraphExtension` is reflection-based, see above. |
| `Arc4u.MongoDB` | Clean, dependency warns | MongoDB driver. |
| `Arc4u.OData` | Clean, dependency warns | Microsoft.OData and Microsoft.AspNetCore.OData. |
| `Arc4u.Dispatcher` | Clean | |

## Check your application

Publish it and read the warnings:

```bash
dotnet publish -c Release -p:PublishAot=true
```

A warning located in an Arc4u member is a bug: report it. A warning that points at an Arc4u API you
call names the API to use instead.

## How Arc4u checks it

- `src/Directory.Build.targets` turns the trimming, single-file and AOT analyzer warnings into errors
  for every project marked `IsAotCompatible`.
- `src/Samples/Arc4u.AotPublishTest` references every AOT-compatible package and roots each Arc4u
  assembly with `TrimmerRootAssembly`, so the AOT compiler analyzes all of their code and not only what
  the sample calls. Each third-party assembly reports its warnings as a single `IL2104` or `IL3053`, which
  is ignored; every warning in an Arc4u assembly is an error.
- The `Native AOT publish` workflow publishes this project on Linux on every pull request that changes
  `src/`, fails on any warning located in an Arc4u member, then runs the native binary.

To run the same check locally:

```bash
dotnet publish src/Samples/Arc4u.AotPublishTest -c Release
```
