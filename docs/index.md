---
description: "Arc4u is a set of NuGet packages for .NET 10 and .NET 11 that handles common enterprise concerns: dependency injection, configuration, logging, authentication, caching, results, gRPC and data access."
---
# Arc4u

Arc4u is a framework that eases the development of .NET applications. It selects a
number of technologies from the .NET ecosystem and packages them so that you can add
common enterprise concerns, such as authentication, logging or caching, without
reinventing them. The framework has been in use for many years and is open source.

This site documents Arc4u 9, the `develop/9.0.0` line. It targets `net10.0` and
`net11.0` and ships as a set of NuGet packages named `Arc4u.*`, one per feature area.

## Where to start

| Section | Read it to |
|---|---|
| [Getting started](getting-started/index.md) | Install Arc4u and build a first application step by step. |
| [Concepts](concepts/index.md) | Understand the architecture and design principles behind Arc4u. |
| [Guides](guides/index.md) | Set up and use one feature area: dependency injection, configuration, logging, authentication, caching, results, gRPC, data access. |
| [API reference](api/index.md) | Look up a type or member, generated from the source code. |
| [Migration](migration/8x-to-9.md) | Upgrade an application from Arc4u 8.x to 9. |
| [Releases](releases/index.md) | See what changed in each release. |

## Feature areas

- [Dependency injection](guides/dependency-injection/index.md): attribute-based registration and source generators.
- [Configuration](guides/configuration/index.md): options, encrypted settings and configuration stores.
- [Diagnostics and logging](guides/diagnostics/index.md): structured logging with Serilog.
- [Server authentication](guides/authentication-server/index.md): securing APIs and web apps with OAuth2 and OpenID Connect.
- [Client authentication and Blazor](guides/authentication-client/index.md): calling protected APIs, including from Blazor.
- [Caching](guides/caching/index.md): memory, Redis, SQL Server and Dapr caches.
- [Results and errors](guides/results/index.md): the result pattern and ProblemDetails.
- [gRPC and API versioning](guides/grpc-versioning/index.md): gRPC services and versioned REST APIs.
- [Data access](guides/data/index.md): Entity Framework Core, MongoDB and OData.
- [Dispatcher](guides/dispatcher/index.md): dispatching notifications to handlers.

The [package support](guides/package-support.md) page lists every package and its status.

## Get involved

Arc4u is developed on [GitHub](https://github.com/Arc4u-org/Arc4u). To report a problem
or contribute code or documentation, read
[CONTRIBUTING.md](https://github.com/Arc4u-org/Arc4u/blob/develop/9.0.0/CONTRIBUTING.md).
