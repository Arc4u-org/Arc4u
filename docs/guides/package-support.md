---
description: "Every Arc4u package with its support status in 9.x, its target frameworks, its latest NuGet version and the replacement for the packages that are deprecated or removed."
---
# Package support

This page lists every package of the Arc4u framework, whether it is supported in 9.x, the frameworks it targets and, for the packages that are not supported, the replacement. Use it to check that a package you depend on is still maintained, and to find the replacement when it is not.

## How to read the tables

| Status | Meaning |
|---|---|
| **Supported** | Built from `src/Arc4u.slnx` on `develop/9.0.0`, published to NuGet.org and maintained. |
| **Deprecated** | Published on NuGet.org for 8.x, no longer maintained, and being removed from the repository. It has no 9.x build. Use the replacement. |
| **Not shipped in 9.x** | The source folder still exists but is not part of `src/Arc4u.slnx`, so no 9.x package is built. |
| **Removed** | The package and its source no longer exist in 9.x. Older versions stay on NuGet.org. |

- The package name is the `<PackageId>` of the `.csproj`, which can differ from the folder name (`Arc4u.Caching.SqlServer` lives in `src/Arc4u.Caching.Sql`).
- Target frameworks are those of the current `develop/9.0.0` build, which is the next preview, read from each `.csproj` (`src/Directory.Build.props` does not set a target framework). The published `9.0.0-preview37` and earlier previews target `net8.0`, `net9.0` and `net10.0` (`Arc4u.Dependency` also targets `netstandard2.0`; `Arc4u.Dependency.Tool` targets only `netstandard2.0`), not `net11.0`.
- All supported packages have `9.0.0-preview37` as their latest version on NuGet.org (checked on 2026-09-29), except `Arc4u.AspNetCore.Versioning`, which is not on NuGet.org yet. The `Last version` column of the last table shows the latest *listed* version on that date.
- Arc4u 9 is in preview: install with `--prerelease`, as in `dotnet add package Arc4u.Caching --prerelease`. See [Versioning and package naming](../concepts/versioning.md).

## Supported packages

### Foundation

These packages are used by many of the others and have no guide of their own. See [Concepts](../concepts/index.md) and the [API reference](../api/index.md).

| Package | Status | Target frameworks | Guide |
|---|---|---|---|
| `Arc4u` | Supported | `net10.0`, `net11.0` | [Concepts](../concepts/index.md) |
| `Arc4u.Core` | Supported | `net10.0`, `net11.0` | [Concepts](../concepts/index.md) |
| `Arc4u.Threading` | Supported | `net10.0`, `net11.0` | [Concepts](../concepts/index.md) |

### Dependency injection

| Package | Status | Target frameworks | Guide |
|---|---|---|---|
| `Arc4u.Dependency` | Supported | `netstandard2.0`, `net10.0`, `net11.0` | [Dependency injection](dependency-injection/index.md) |
| `Arc4u.Dependency.Tool` | Supported | `netstandard2.0` (Roslyn source generator) | [Dependency injection](dependency-injection/index.md) |

### Configuration

| Package | Status | Target frameworks | Guide |
|---|---|---|---|
| `Arc4u.Configuration` | Supported | `net10.0`, `net11.0` | [Configuration](configuration/index.md) |
| `Arc4u.Configuration.Decryptor` | Supported | `net10.0`, `net11.0` | [Configuration](configuration/index.md) |
| `Arc4u.Configuration.Store` | Supported | `net10.0`, `net11.0` | [Configuration](configuration/index.md) |
| `Arc4u.Configuration.Store.EfCore` | Supported | `net10.0`, `net11.0` | [Configuration](configuration/index.md) |

### Diagnostics and logging

| Package | Status | Target frameworks | Guide |
|---|---|---|---|
| `Arc4u.Diagnostics` | Supported | `net10.0`, `net11.0` | [Diagnostics and logging](diagnostics/index.md) |
| `Arc4u.Diagnostics.Serilog` | Supported | `net10.0`, `net11.0` | [Diagnostics and logging](diagnostics/index.md) |
| `Arc4u.Diagnostics.Serilog.Sinks.RealmDb` | Supported | `net10.0`, `net11.0` | [Diagnostics and logging](diagnostics/index.md) |

### Server authentication

| Package | Status | Target frameworks | Guide |
|---|---|---|---|
| `Arc4u.OAuth2` | Supported | `net10.0`, `net11.0` | [Server authentication](authentication-server/index.md) |
| `Arc4u.OAuth2.AspNetCore` | Supported | `net10.0`, `net11.0` | [Server authentication](authentication-server/index.md) |
| `Arc4u.OAuth2.AspNetCore.Authentication` | Supported | `net10.0`, `net11.0` | [Server authentication](authentication-server/index.md) |
| `Arc4u.Authorization` | Supported | `net10.0`, `net11.0` | [Server authentication](authentication-server/index.md) |

### Client authentication and Blazor

| Package | Status | Target frameworks | Guide |
|---|---|---|---|
| `Arc4u.OAuth2.Client` | Supported | `net10.0`, `net11.0` | [Client authentication and Blazor](authentication-client/index.md) |
| `Arc4u.OAuth2.Client.Authentication` | Supported | `net10.0`, `net11.0` | [Client authentication and Blazor](authentication-client/index.md) |
| `Arc4u.OAuth2.Blazor` | Supported | `net10.0`, `net11.0` | [Client authentication and Blazor](authentication-client/index.md) |
| `Arc4u.OAuth2.AspNetCore.Blazor` | Supported | `net10.0`, `net11.0` | [Client authentication and Blazor](authentication-client/index.md) |

### Caching and serialization

| Package | Status | Target frameworks | Guide |
|---|---|---|---|
| `Arc4u.Caching` | Supported | `net10.0`, `net11.0` | [Caching](caching/index.md) |
| `Arc4u.Caching.Memory` | Supported | `net10.0`, `net11.0` | [Caching](caching/index.md) |
| `Arc4u.Caching.Redis` | Supported | `net10.0`, `net11.0` | [Caching](caching/index.md) |
| `Arc4u.Caching.SqlServer` | Supported | `net10.0`, `net11.0` | [Caching](caching/index.md) |
| `Arc4u.Caching.Dapr` | Supported | `net10.0`, `net11.0` | [Caching](caching/index.md) |
| `Arc4u.Serializer` | Supported | `net10.0`, `net11.0` | [Caching](caching/index.md) |
| `Arc4u.Serializer.JSon` | Supported | `net10.0`, `net11.0` | [Caching](caching/index.md) |

### Results and errors

| Package | Status | Target frameworks | Guide |
|---|---|---|---|
| `Arc4u.Results` | Supported | `net10.0`, `net11.0` | [Results and errors](results/index.md) |
| `Arc4u.AspNetCore.Results` | Supported | `net10.0`, `net11.0` | [Results and errors](results/index.md) |
| `Arc4u.FluentValidation` | Supported | `net10.0`, `net11.0` | [Results and errors](results/index.md) |

### gRPC and API versioning

| Package | Status | Target frameworks | Guide |
|---|---|---|---|
| `Arc4u.gRPC` | Supported | `net10.0`, `net11.0` | [gRPC and API versioning](grpc-versioning/index.md) |
| `Arc4u.AspNetCore.gRpc` | Supported | `net10.0`, `net11.0` | [gRPC and API versioning](grpc-versioning/index.md) |
| `Arc4u.AspNetCore.Versioning` | Supported | `net10.0` | [gRPC and API versioning](grpc-versioning/index.md) |

### Data access

| Package | Status | Target frameworks | Guide |
|---|---|---|---|
| `Arc4u.Data` | Supported | `net10.0`, `net11.0` | [Data access](data/index.md) |
| `Arc4u.EfCore` | Supported | `net10.0`, `net11.0` | [Data access](data/index.md) |
| `Arc4u.MongoDB` | Supported | `net10.0`, `net11.0` | [Data access](data/index.md) |
| `Arc4u.OData` | Supported | `net10.0`, `net11.0` | [Data access](data/index.md) |

### Dispatcher

| Package | Status | Target frameworks | Guide |
|---|---|---|---|
| `Arc4u.Dispatcher` | Supported | `net10.0`, `net11.0` | [Dispatcher](dispatcher/index.md) |

## Deprecated, not shipped and removed packages

None of these packages has a 9.x build.

| Package | Status | Last version on NuGet | Replacement | More |
|---|---|---|---|---|
| `Arc4u.Standard.NServiceBus` | Deprecated | `8.3.2` | Use Dapr pub/sub directly. Arc4u has no Dapr messaging package. | [NServiceBus replaced by Dapr pub/sub](../migration/8x-to-9.md#nservicebus-replaced-by-dapr-pubsub) |
| `Arc4u.Standard.NServiceBus.Core` | Deprecated | `8.3.2` | Use Dapr pub/sub directly. | [NServiceBus replaced by Dapr pub/sub](../migration/8x-to-9.md#nservicebus-replaced-by-dapr-pubsub) |
| `Arc4u.Standard.NServiceBus.RabbitMQ` | Deprecated | `8.3.2` | Use Dapr pub/sub directly. | [NServiceBus replaced by Dapr pub/sub](../migration/8x-to-9.md#nservicebus-replaced-by-dapr-pubsub) |
| `Arc4u.Prism.DI.Wpf` | Deprecated | `8.3.2` | Use `Prism.DryIoc`, Prism's own DryIoc container package. | [Prism.DI.Wpf replaced by Prism.DryIoc](../migration/8x-to-9.md#prismdiwpf-replaced-by-prismdryioc) |
| `Arc4u.Dependency.ComponentModel` (8.x ID `Arc4u.Standard.Dependency.ComponentModel`) | Not shipped in 9.x | `8.3.2` | `Arc4u.Dependency`, which registers services on `IServiceCollection`. | [Dependency injection](dependency-injection/index.md) |
| `Arc4u.OAuth2.Msal` (8.x ID `Arc4u.Standard.OAuth2.Msal`, folder `src/Arc4u.OAuth.Msal`) | Not shipped in 9.x | `8.3.2` | None in Arc4u 9. | [MSAL status](../migration/8x-to-9.md#msal-status) |
| `Arc4u.Standard.OAuth2.AspNetCore.Api` | Removed | `8.3.2` | None. The project was deleted from 9.x. For server authentication see `Arc4u.OAuth2.AspNetCore`. | [Server authentication](authentication-server/index.md) |
| `Arc4u.Standard.OAuth2.Adal`, `Arc4u.Standard.OAuth2.AspNetCore.Adal` | Removed | `6.0.6.1-preview02`, `8.3.0-preview01` | None. ADAL is retired by Microsoft. | [ADAL and Protobuf removed](../migration/8x-to-9.md#adal-and-protobuf-removed) |
| `Arc4u.Standard.Serializer.Protobuf`, `Arc4u.Standard.Serializer.ProtobufV2` | Removed | `8.3.0-preview01` | `Arc4u.Serializer.JSon`, the only serializer in 9. | [ADAL and Protobuf removed](../migration/8x-to-9.md#adal-and-protobuf-removed) |
| `Arc4u.Standard.Diagnostics.TraceListeners` | Removed | `8.3.0-preview02` | None. See the migration guide. | [TraceListeners removed](../migration/8x-to-9.md#tracelisteners-removed) |
| `Arc4u.Standard.OAuth2.AspNetCore.Msal`, `Arc4u.Standard.Dependency.ComponentModel.Container`, `Arc4u.Standard.Dependency.Composition` | Removed | `6.0.14.3-preview43`, `6.0.14.3-preview43`, `5.0.17.3` | None. | [Migrate from 8.x to 9](../migration/8x-to-9.md) |
| `Arc4u.Standard.KubeMQ`, `Arc4u.Standard.KubeMQ.AspNetCore` | Removed | `6.0.14.3-preview43` (an unlisted `9.9.9.9` placeholder also exists) | None. | [Migrate from 8.x to 9](../migration/8x-to-9.md) |

The Xamarin (`Arc4u.Xamarin.*`) and `Arc4u.Windows.Mvvm` packages on NuGet.org come from Arc4u 5 only and are not part of Arc4u 9. The tools `Arc4u.Cyphertool` and `Arc4u.Encryptor` and the `Arc4u.LicManager.*` packages published under the same owner are not part of the framework and are not covered here.

## Renamed packages

Every other 8.x package named `Arc4u.Standard.<Name>` continues as `Arc4u.<Name>` in 9, for example `Arc4u.Standard.Dispatcher` is now `Arc4u.Dispatcher`. Two cases differ from a plain prefix change: the base package `Arc4u.Standard` is now `Arc4u`, and the old `Arc4u.Standard.Caching.Sql` (6.x) is now `Arc4u.Caching.SqlServer`. The full table is in [Package renames](../migration/8x-to-9.md#package-renames).

## See also

- [Migrate from 8.x to 9](../migration/8x-to-9.md)
- [Versioning and package naming](../concepts/versioning.md)
- [Guides](index.md)
