---
description: "Check the prerequisites, pick the packages you need and add them to a .NET 10 or .NET 11 project."
---
# Install Arc4u

Arc4u is a set of NuGet packages. You add the packages for the feature areas you need to a .NET
project. This page lists the prerequisites, helps you choose the packages and adds them. It takes
about five minutes.

## Prerequisites

- The .NET 10 SDK or later. Check with:

  ```bash
  dotnet --version
  ```

  Arc4u 9 targets `net10.0` and `net11.0`. The `9.0.0-preview` packages on NuGet include
  `net10.0` assemblies, so a .NET 10 SDK is enough to use them.
- A .NET project to add Arc4u to. [Build your first Arc4u app](first-app.md) starts from
  `dotnet new web`.

> [!NOTE]
> To build Arc4u itself, or to run the samples in the repository (they reference the source
> projects), you need the exact SDK pinned in
> [`src/global.json`](https://github.com/Arc4u-org/Arc4u/blob/develop/9.0.0/src/global.json).
> [CONTRIBUTING.md](https://github.com/Arc4u-org/Arc4u/blob/develop/9.0.0/CONTRIBUTING.md) explains
> how to install it.

## Step 1: Choose your packages

Each feature area has its own packages, and you only add the ones you use. Arc4u packages
reference the other Arc4u packages they need, so NuGet brings those in for you.

| You want to | Packages | Guide |
|---|---|---|
| Register services with attributes | `Arc4u.Dependency`, `Arc4u.Dependency.Tool` | [Dependency injection](../guides/dependency-injection/index.md) |
| Bind application settings, decrypt secrets, use a configuration store | `Arc4u.Configuration`, `Arc4u.Configuration.Decryptor`, `Arc4u.Configuration.Store`, `Arc4u.Configuration.Store.EfCore` | [Configuration](../guides/configuration/index.md) |
| Log with categories and Serilog | `Arc4u.Diagnostics`, `Arc4u.Diagnostics.Serilog` | [Diagnostics and logging](../guides/diagnostics/index.md) |
| Protect an API or a web app with OAuth2 or OpenID Connect, and authorize operations | `Arc4u.OAuth2.AspNetCore.Authentication`, `Arc4u.Authorization` | [Server authentication](../guides/authentication-server/index.md) |
| Call protected APIs, including from Blazor | `Arc4u.OAuth2.Client`, `Arc4u.OAuth2.Client.Authentication`, `Arc4u.OAuth2.Blazor`, `Arc4u.OAuth2.AspNetCore.Blazor` | [Client authentication and Blazor](../guides/authentication-client/index.md) |
| Cache data in memory, Redis, SQL Server or Dapr | `Arc4u.Caching.Memory`, `Arc4u.Caching.Redis`, `Arc4u.Caching.SqlServer`, `Arc4u.Caching.Dapr` | [Caching](../guides/caching/index.md) |
| Return results and ProblemDetails | `Arc4u.Results`, `Arc4u.AspNetCore.Results`, `Arc4u.FluentValidation` | [Results and errors](../guides/results/index.md) |
| Expose gRPC services and versioned APIs | `Arc4u.gRPC`, `Arc4u.AspNetCore.gRpc` | [gRPC and API versioning](../guides/grpc-versioning/index.md) |
| Access data with Entity Framework Core, MongoDB or OData | `Arc4u.EfCore`, `Arc4u.MongoDB`, `Arc4u.OData` | [Data access](../guides/data/index.md) |
| Dispatch notifications to handlers | `Arc4u.Dispatcher` | [Dispatcher](../guides/dispatcher/index.md) |

The [Package support](../guides/package-support.md) page lists every package, its status and
its target frameworks.

For the [first app](first-app.md), you need `Arc4u.Dependency`, `Arc4u.Dependency.Tool`,
`Arc4u.Configuration`, `Arc4u.Diagnostics` and `Arc4u.OAuth2.AspNetCore.Authentication`.

## Step 2: Add the packages

From the folder that contains your `.csproj`, add each package. Arc4u 9 is published as
`9.0.0-previewNN` versions only, so add `--prerelease`; without it, `dotnet add package` looks for
a stable version and fails.

```bash
dotnet add package Arc4u.Dependency --prerelease
dotnet add package Arc4u.Dependency.Tool --prerelease
dotnet add package Arc4u.Configuration --prerelease
dotnet add package Arc4u.Diagnostics --prerelease
dotnet add package Arc4u.OAuth2.AspNetCore.Authentication --prerelease
```

Keep every Arc4u package of an application on the same version.

`Arc4u.Dependency.Tool` is a source generator: it runs when you compile and is not deployed with
your application. `dotnet add package` marks it as a development dependency, so your project file
contains:

```xml
<PackageReference Include="Arc4u.Dependency.Tool" Version="9.0.0-preview37">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

## Step 3: Check the installation

List the packages of the project:

```bash
dotnet list package
```

The output shows the Arc4u packages with the version NuGet resolved (the preview number changes
with each release):

```text
Project 'GettingStarted' has the following package references
   [net10.0]:
   Top-level Package                             Requested         Resolved
   > Arc4u.Configuration                         9.0.0-preview37   9.0.0-preview37
   > Arc4u.Dependency                            9.0.0-preview37   9.0.0-preview37
   > Arc4u.Dependency.Tool                       9.0.0-preview37   9.0.0-preview37
   > Arc4u.Diagnostics                           9.0.0-preview37   9.0.0-preview37
   > Arc4u.OAuth2.AspNetCore.Authentication      9.0.0-preview37   9.0.0-preview37
```

Then build the project with `dotnet build`. It ends with `Build succeeded`.

## Next steps

- [Build your first Arc4u app](first-app.md): use these packages in a small API with dependency
  injection, configuration, logging and authentication.
- [Concepts](../concepts/index.md): the architecture and design principles behind Arc4u.
- [Migrate from Arc4u 8.x to 9](../migration/8x-to-9.md): package renames and breaking changes if
  you upgrade an existing application.
