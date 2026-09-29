# Arc4u

[![NuGet](https://img.shields.io/nuget/vpre/Arc4u.Core?label=nuget)](https://www.nuget.org/packages/Arc4u.Core)
[![License](https://img.shields.io/github/license/Arc4u-org/Arc4u)](LICENSE)

Arc4u is a set of NuGet packages for .NET 10 and .NET 11 that handles common enterprise
concerns so you do not have to build them again: dependency injection, configuration,
logging, authentication, caching, results, gRPC and data access. It selects technologies
from the .NET ecosystem and adds what applications in an enterprise usually need on top of
them. It has been used in production for many years.

Arc4u 9 is in preview on the `develop/9.0.0` branch: the packages are published as `9.0.0-previewNN` and target `net10.0` and `net11.0`, so install them with `--prerelease`. See [Versioning](https://arc4u-org.github.io/Arc4u/concepts/versioning.html) for how versions and package names work.

## Quick start

```bash
dotnet add package Arc4u.Diagnostics --prerelease
```

`Arc4u.Diagnostics` extends `ILogger<T>` with categories (technical, business, monitoring)
and extra properties on each log entry:

```csharp
// Program.cs
using Arc4u.Dependency;
using Arc4u.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddILogger();

var app = builder.Build();
app.MapGet("/", (ILogger<Program> logger) =>
{
    logger.Technical().Add("path", "/").LogInformation("Request received");
    return "Hello";
});
app.Run();
```

Next: [Getting started](https://arc4u-org.github.io/Arc4u/getting-started/) walks through a first application.

## Packages

Each package ID below is the name you pass to `dotnet add package`. The area name links to its guide on the documentation site.

| Area (guide) | Package | Latest preview |
|---|---|---|
| [Foundation](https://arc4u-org.github.io/Arc4u/concepts/) | `Arc4u` | [![Arc4u](https://img.shields.io/nuget/vpre/Arc4u?label=)](https://www.nuget.org/packages/Arc4u) |
|  | `Arc4u.Core` | [![Arc4u.Core](https://img.shields.io/nuget/vpre/Arc4u.Core?label=)](https://www.nuget.org/packages/Arc4u.Core) |
|  | `Arc4u.Threading` | [![Arc4u.Threading](https://img.shields.io/nuget/vpre/Arc4u.Threading?label=)](https://www.nuget.org/packages/Arc4u.Threading) |
| [Dependency injection](https://arc4u-org.github.io/Arc4u/guides/dependency-injection/) | `Arc4u.Dependency` | [![Arc4u.Dependency](https://img.shields.io/nuget/vpre/Arc4u.Dependency?label=)](https://www.nuget.org/packages/Arc4u.Dependency) |
|  | `Arc4u.Dependency.Tool` | [![Arc4u.Dependency.Tool](https://img.shields.io/nuget/vpre/Arc4u.Dependency.Tool?label=)](https://www.nuget.org/packages/Arc4u.Dependency.Tool) |
| [Configuration](https://arc4u-org.github.io/Arc4u/guides/configuration/) | `Arc4u.Configuration` | [![Arc4u.Configuration](https://img.shields.io/nuget/vpre/Arc4u.Configuration?label=)](https://www.nuget.org/packages/Arc4u.Configuration) |
|  | `Arc4u.Configuration.Decryptor` | [![Arc4u.Configuration.Decryptor](https://img.shields.io/nuget/vpre/Arc4u.Configuration.Decryptor?label=)](https://www.nuget.org/packages/Arc4u.Configuration.Decryptor) |
|  | `Arc4u.Configuration.Store` | [![Arc4u.Configuration.Store](https://img.shields.io/nuget/vpre/Arc4u.Configuration.Store?label=)](https://www.nuget.org/packages/Arc4u.Configuration.Store) |
|  | `Arc4u.Configuration.Store.EfCore` | [![Arc4u.Configuration.Store.EfCore](https://img.shields.io/nuget/vpre/Arc4u.Configuration.Store.EfCore?label=)](https://www.nuget.org/packages/Arc4u.Configuration.Store.EfCore) |
| [Diagnostics](https://arc4u-org.github.io/Arc4u/guides/diagnostics/) | `Arc4u.Diagnostics` | [![Arc4u.Diagnostics](https://img.shields.io/nuget/vpre/Arc4u.Diagnostics?label=)](https://www.nuget.org/packages/Arc4u.Diagnostics) |
|  | `Arc4u.Diagnostics.Serilog` | [![Arc4u.Diagnostics.Serilog](https://img.shields.io/nuget/vpre/Arc4u.Diagnostics.Serilog?label=)](https://www.nuget.org/packages/Arc4u.Diagnostics.Serilog) |
|  | `Arc4u.Diagnostics.Serilog.Sinks.RealmDb` | [![Arc4u.Diagnostics.Serilog.Sinks.RealmDb](https://img.shields.io/nuget/vpre/Arc4u.Diagnostics.Serilog.Sinks.RealmDb?label=)](https://www.nuget.org/packages/Arc4u.Diagnostics.Serilog.Sinks.RealmDb) |
| [Server authentication](https://arc4u-org.github.io/Arc4u/guides/authentication-server/) | `Arc4u.OAuth2` | [![Arc4u.OAuth2](https://img.shields.io/nuget/vpre/Arc4u.OAuth2?label=)](https://www.nuget.org/packages/Arc4u.OAuth2) |
|  | `Arc4u.OAuth2.AspNetCore` | [![Arc4u.OAuth2.AspNetCore](https://img.shields.io/nuget/vpre/Arc4u.OAuth2.AspNetCore?label=)](https://www.nuget.org/packages/Arc4u.OAuth2.AspNetCore) |
|  | `Arc4u.OAuth2.AspNetCore.Authentication` | [![Arc4u.OAuth2.AspNetCore.Authentication](https://img.shields.io/nuget/vpre/Arc4u.OAuth2.AspNetCore.Authentication?label=)](https://www.nuget.org/packages/Arc4u.OAuth2.AspNetCore.Authentication) |
|  | `Arc4u.Authorization` | [![Arc4u.Authorization](https://img.shields.io/nuget/vpre/Arc4u.Authorization?label=)](https://www.nuget.org/packages/Arc4u.Authorization) |
| [Client authentication](https://arc4u-org.github.io/Arc4u/guides/authentication-client/) | `Arc4u.OAuth2.Client` | [![Arc4u.OAuth2.Client](https://img.shields.io/nuget/vpre/Arc4u.OAuth2.Client?label=)](https://www.nuget.org/packages/Arc4u.OAuth2.Client) |
|  | `Arc4u.OAuth2.Client.Authentication` | [![Arc4u.OAuth2.Client.Authentication](https://img.shields.io/nuget/vpre/Arc4u.OAuth2.Client.Authentication?label=)](https://www.nuget.org/packages/Arc4u.OAuth2.Client.Authentication) |
|  | `Arc4u.OAuth2.Blazor` | [![Arc4u.OAuth2.Blazor](https://img.shields.io/nuget/vpre/Arc4u.OAuth2.Blazor?label=)](https://www.nuget.org/packages/Arc4u.OAuth2.Blazor) |
|  | `Arc4u.OAuth2.AspNetCore.Blazor` | [![Arc4u.OAuth2.AspNetCore.Blazor](https://img.shields.io/nuget/vpre/Arc4u.OAuth2.AspNetCore.Blazor?label=)](https://www.nuget.org/packages/Arc4u.OAuth2.AspNetCore.Blazor) |
| [Caching and serialization](https://arc4u-org.github.io/Arc4u/guides/caching/) | `Arc4u.Caching` | [![Arc4u.Caching](https://img.shields.io/nuget/vpre/Arc4u.Caching?label=)](https://www.nuget.org/packages/Arc4u.Caching) |
|  | `Arc4u.Caching.Memory` | [![Arc4u.Caching.Memory](https://img.shields.io/nuget/vpre/Arc4u.Caching.Memory?label=)](https://www.nuget.org/packages/Arc4u.Caching.Memory) |
|  | `Arc4u.Caching.Redis` | [![Arc4u.Caching.Redis](https://img.shields.io/nuget/vpre/Arc4u.Caching.Redis?label=)](https://www.nuget.org/packages/Arc4u.Caching.Redis) |
|  | `Arc4u.Caching.SqlServer` | [![Arc4u.Caching.SqlServer](https://img.shields.io/nuget/vpre/Arc4u.Caching.SqlServer?label=)](https://www.nuget.org/packages/Arc4u.Caching.SqlServer) |
|  | `Arc4u.Caching.Dapr` | [![Arc4u.Caching.Dapr](https://img.shields.io/nuget/vpre/Arc4u.Caching.Dapr?label=)](https://www.nuget.org/packages/Arc4u.Caching.Dapr) |
|  | `Arc4u.Serializer` | [![Arc4u.Serializer](https://img.shields.io/nuget/vpre/Arc4u.Serializer?label=)](https://www.nuget.org/packages/Arc4u.Serializer) |
|  | `Arc4u.Serializer.JSon` | [![Arc4u.Serializer.JSon](https://img.shields.io/nuget/vpre/Arc4u.Serializer.JSon?label=)](https://www.nuget.org/packages/Arc4u.Serializer.JSon) |
| [Results](https://arc4u-org.github.io/Arc4u/guides/results/) | `Arc4u.Results` | [![Arc4u.Results](https://img.shields.io/nuget/vpre/Arc4u.Results?label=)](https://www.nuget.org/packages/Arc4u.Results) |
|  | `Arc4u.AspNetCore.Results` | [![Arc4u.AspNetCore.Results](https://img.shields.io/nuget/vpre/Arc4u.AspNetCore.Results?label=)](https://www.nuget.org/packages/Arc4u.AspNetCore.Results) |
|  | `Arc4u.FluentValidation` | [![Arc4u.FluentValidation](https://img.shields.io/nuget/vpre/Arc4u.FluentValidation?label=)](https://www.nuget.org/packages/Arc4u.FluentValidation) |
| [gRPC and versioning](https://arc4u-org.github.io/Arc4u/guides/grpc-versioning/) | `Arc4u.gRPC` | [![Arc4u.gRPC](https://img.shields.io/nuget/vpre/Arc4u.gRPC?label=)](https://www.nuget.org/packages/Arc4u.gRPC) |
|  | `Arc4u.AspNetCore.gRpc` | [![Arc4u.AspNetCore.gRpc](https://img.shields.io/nuget/vpre/Arc4u.AspNetCore.gRpc?label=)](https://www.nuget.org/packages/Arc4u.AspNetCore.gRpc) |
|  | `Arc4u.AspNetCore.Versioning` | not on NuGet yet |
| [Data access](https://arc4u-org.github.io/Arc4u/guides/data/) | `Arc4u.Data` | [![Arc4u.Data](https://img.shields.io/nuget/vpre/Arc4u.Data?label=)](https://www.nuget.org/packages/Arc4u.Data) |
|  | `Arc4u.EfCore` | [![Arc4u.EfCore](https://img.shields.io/nuget/vpre/Arc4u.EfCore?label=)](https://www.nuget.org/packages/Arc4u.EfCore) |
|  | `Arc4u.MongoDB` | [![Arc4u.MongoDB](https://img.shields.io/nuget/vpre/Arc4u.MongoDB?label=)](https://www.nuget.org/packages/Arc4u.MongoDB) |
|  | `Arc4u.OData` | [![Arc4u.OData](https://img.shields.io/nuget/vpre/Arc4u.OData?label=)](https://www.nuget.org/packages/Arc4u.OData) |
| [Dispatcher](https://arc4u-org.github.io/Arc4u/guides/dispatcher/) | `Arc4u.Dispatcher` | [![Arc4u.Dispatcher](https://img.shields.io/nuget/vpre/Arc4u.Dispatcher?label=)](https://www.nuget.org/packages/Arc4u.Dispatcher) |

Some projects in `src/` are not part of the 9.x package set. The
[Package support](https://arc4u-org.github.io/Arc4u/guides/package-support.html) page lists
which packages ship and which do not.

## Documentation

- [Documentation site](https://arc4u-org.github.io/Arc4u/): getting started, concepts, guides, API reference and migration from 8.x.
- [Migrate from 8.x to 9](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html)
- [Release notes](https://arc4u-org.github.io/Arc4u/releases/)
- The site sources are in [`docs/`](docs/).

## Arc4u.Guidance

[Arc4u.Guidance](https://marketplace.visualstudio.com/items?itemName=Arc4u.Guidance2022-2) is a commercial Visual Studio
extension that generates a .NET solution of micro-services preconfigured with Arc4u. Arc4u
does not require it. See its [documentation repository](https://github.com/GFlisch/Arc4u.Guidance.Doc).

## Contributing

Bug reports, fixes and documentation improvements are welcome. Read
[CONTRIBUTING.md](CONTRIBUTING.md) for how to build, test and propose a change, and use the
[issue templates](https://github.com/Arc4u-org/Arc4u/issues/new/choose) to report a problem.

## License

Arc4u is licensed under the [Apache License 2.0](LICENSE).
