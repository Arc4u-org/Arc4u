# Arc4u

[![Build](https://github.com/Arc4u-org/Arc4u/actions/workflows/BuildPreview.yml/badge.svg?branch=develop%2F9.0.0)](https://github.com/Arc4u-org/Arc4u/actions/workflows/BuildPreview.yml)
[![NuGet](https://img.shields.io/nuget/vpre/Arc4u.Core?label=nuget)](https://www.nuget.org/packages/Arc4u.Core)
[![License](https://img.shields.io/github/license/Arc4u-org/Arc4u)](LICENSE)

Arc4u is a set of NuGet packages for .NET 10 and .NET 11 that handles common enterprise
concerns so you do not have to build them again: dependency injection, configuration,
logging, authentication, caching, results, gRPC and data access. It selects technologies
from the .NET ecosystem and adds what applications in an enterprise usually need on top of
them. It has been used in production for many years.

Arc4u 9 is in preview on the `develop/9.0.0` branch. Install it with `--prerelease`.

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

## Packages

Each package ID below is the name you pass to `dotnet add package`. Each area has a guide on
the documentation site.

| Area | Packages | Guide |
|---|---|---|
| Foundation | `Arc4u`, `Arc4u.Core`, `Arc4u.Threading` | [Concepts](https://arc4u-org.github.io/Arc4u/concepts/) |
| Dependency injection | `Arc4u.Dependency`, `Arc4u.Dependency.Tool` | [Guide](https://arc4u-org.github.io/Arc4u/guides/dependency-injection/) |
| Configuration | `Arc4u.Configuration`, `Arc4u.Configuration.Decryptor`, `Arc4u.Configuration.Store`, `Arc4u.Configuration.Store.EfCore` | [Guide](https://arc4u-org.github.io/Arc4u/guides/configuration/) |
| Diagnostics | `Arc4u.Diagnostics`, `Arc4u.Diagnostics.Serilog`, `Arc4u.Diagnostics.Serilog.Sinks.RealmDb` | [Guide](https://arc4u-org.github.io/Arc4u/guides/diagnostics/) |
| Server authentication | `Arc4u.OAuth2`, `Arc4u.OAuth2.AspNetCore`, `Arc4u.OAuth2.AspNetCore.Authentication`, `Arc4u.Authorization` | [Guide](https://arc4u-org.github.io/Arc4u/guides/authentication-server/) |
| Client authentication | `Arc4u.OAuth2.Client`, `Arc4u.OAuth2.Client.Authentication`, `Arc4u.OAuth2.Blazor`, `Arc4u.OAuth2.AspNetCore.Blazor` | [Guide](https://arc4u-org.github.io/Arc4u/guides/authentication-client/) |
| Caching and serialization | `Arc4u.Caching`, `Arc4u.Caching.Memory`, `Arc4u.Caching.Redis`, `Arc4u.Caching.SqlServer`, `Arc4u.Caching.Dapr`, `Arc4u.Serializer`, `Arc4u.Serializer.JSon` | [Guide](https://arc4u-org.github.io/Arc4u/guides/caching/) |
| Results | `Arc4u.Results`, `Arc4u.AspNetCore.Results`, `Arc4u.FluentValidation` | [Guide](https://arc4u-org.github.io/Arc4u/guides/results/) |
| gRPC and versioning | `Arc4u.gRPC`, `Arc4u.AspNetCore.gRpc`, `Arc4u.AspNetCore.Versioning` | [Guide](https://arc4u-org.github.io/Arc4u/guides/grpc-versioning/) |
| Data access | `Arc4u.Data`, `Arc4u.EfCore`, `Arc4u.MongoDB`, `Arc4u.OData` | [Guide](https://arc4u-org.github.io/Arc4u/guides/data/) |
| Dispatcher | `Arc4u.Dispatcher` | [Guide](https://arc4u-org.github.io/Arc4u/guides/dispatcher/) |

Some projects in `src/` are not part of the 9.x package set. The
[Package support](https://arc4u-org.github.io/Arc4u/guides/package-support.html) page lists
which packages ship and which do not.

## Documentation

- [Documentation site](https://arc4u-org.github.io/Arc4u/): getting started, concepts, guides, API reference and migration from 8.x.
- [Migrate from 8.x to 9](https://arc4u-org.github.io/Arc4u/migration/8x-to-9.html)
- [Release notes](https://arc4u-org.github.io/Arc4u/releases/)
- The site sources are in [`docs/`](docs/).

## Arc4u.Guidance

[Arc4u.Guidance](https://github.com/GFlisch/Arc4u.Guidance.Doc) is a commercial Visual Studio
extension that generates a .NET solution of micro-services preconfigured with Arc4u. Arc4u
does not require it. See its [documentation repository](https://github.com/GFlisch/Arc4u.Guidance.Doc).

## Contributing

Bug reports, fixes and documentation improvements are welcome. Read
[CONTRIBUTING.md](CONTRIBUTING.md) for how to build, test and propose a change, and use the
[issue templates](https://github.com/Arc4u-org/Arc4u/issues/new/choose) to report a problem.

## License

Arc4u is licensed under the [Apache License 2.0](LICENSE).
