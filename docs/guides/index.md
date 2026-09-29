---
description: "One guide per Arc4u feature area, with the packages each one covers."
---
# Guides

Each guide covers one feature area: the problem it solves, the packages involved,
how to install and configure them, common scenarios, what you can replace through
dependency injection and how to troubleshoot it. The [Concepts](../concepts/index.md)
section explains the ideas the guides build on, and the [API reference](../api/index.md)
documents every public type.

| Guide | Packages |
|---|---|
| [Dependency injection](dependency-injection/index.md) | `Arc4u.Dependency`, `Arc4u.Dependency.Tool` |
| [Configuration](configuration/index.md) | `Arc4u.Configuration`, `Arc4u.Configuration.Decryptor`, `Arc4u.Configuration.Store`, `Arc4u.Configuration.Store.EFCore` |
| [Diagnostics and logging](diagnostics/index.md) | `Arc4u.Diagnostics`, `Arc4u.Diagnostics.Serilog`, `Arc4u.Diagnostics.Serilog.Sinks.RealmDb` |
| [Server authentication](authentication-server/index.md) | `Arc4u.OAuth2`, `Arc4u.OAuth2.AspNetCore`, `Arc4u.OAuth2.AspNetCore.Authentication`, `Arc4u.Authorization` |
| [Client authentication and Blazor](authentication-client/index.md) | `Arc4u.OAuth2.Client`, `Arc4u.OAuth2.Client.Authentication`, `Arc4u.OAuth2.Blazor`, `Arc4u.OAuth2.AspNetCore.Blazor` |
| [Caching](caching/index.md) | `Arc4u.Caching`, `Arc4u.Caching.Memory`, `Arc4u.Caching.Redis`, `Arc4u.Caching.Sql`, `Arc4u.Caching.Dapr`, `Arc4u.Serializer`, `Arc4u.Serializer.JSon` |
| [Results and errors](results/index.md) | `Arc4u.Results`, `Arc4u.AspNetCore.Results`, `Arc4u.FluentValidation` |
| [gRPC and API versioning](grpc-versioning/index.md) | `Arc4u.gRPC`, `Arc4u.AspNetCore.gRpc`, `Arc4u.AspNetCore.Versioning` |
| [Data access](data/index.md) | `Arc4u.Data`, `Arc4u.EfCore`, `Arc4u.MongoDB`, `Arc4u.OData` |
| [Dispatcher](dispatcher/index.md) | `Arc4u.Dispatcher` |

The foundation packages `Arc4u`, `Arc4u.Core` and `Arc4u.Threading` are used by
most of the packages above and have no guide of their own: see
[Concepts](../concepts/index.md) and the [API reference](../api/index.md).

The [package support](package-support.md) page lists every package with its
support status and target frameworks, including the packages that are not shipped in 9.x
(`Arc4u.Dependency.ComponentModel`, `Arc4u.OAuth2.Msal` and the removed NServiceBus and
Prism.DI.Wpf packages).
