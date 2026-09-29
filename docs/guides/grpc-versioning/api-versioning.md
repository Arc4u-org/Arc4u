---
description: "Version Arc4u REST endpoints with AddServiceApiVersioning: three ways to give a version, a mandatory version and reported versions."
---
# API versioning

Every Arc4u service that exposes REST endpoints versions them the same way. `Arc4u.AspNetCore.Versioning`
is a thin wrapper around [Asp.Versioning](https://github.com/dotnet/aspnet-api-versioning) that fixes
the readers, the header names, the default version and the rule on what happens when a caller gives
no version. Versioning the interface layer is a core idea of Arc4u's layering, described in the
[architecture concept](../../concepts/architecture.md).

## What it solves

Without a convention, each service picks its own header name and its own fallback, and a client cannot
rely on any of them. `AddServiceApiVersioning()` gives all services the same behavior:

- a client picks the version in the URL, a header or the query string, whichever fits;
- a caller that gives no version is rejected instead of being silently routed to v1, so that
  retiring a version later is a controlled operation and not a surprise for clients that never
  said which version they were written against;
- the supported and deprecated versions are reported in every response;
- the API explorer documents the real route (`interface/v1/environment`) and not the template
  (`interface/v{version}/environment`).

Arc4u deliberately leaves the rest to Asp.Versioning: declaring versions, deprecation, conventions and
OpenAPI generation.

## Packages involved

| Package | Use it for |
|---|---|
| `Arc4u.AspNetCore.Versioning` | `AddServiceApiVersioning()`, `ApiVersionDefault` and the header and query string names. References `Asp.Versioning.Http` and `Asp.Versioning.Mvc.ApiExplorer`. |

## Install

```bash
dotnet add package Arc4u.AspNetCore.Versioning --prerelease
```

> [!NOTE]
> The package is not published on NuGet.org yet and targets `net10.0` only. It is flagged as not AOT
> compatible because MVC and the API explorer it depends on are not trimmable yet.

## Configuration

The package has no configuration section. `AddServiceApiVersioning()` sets these options:

| Option | Value | Effect |
|---|---|---|
| `DefaultApiVersion` | `ApiVersionDefault.V1` (version 1) | The version the routing considers the default. |
| `AssumeDefaultVersionWhenUnspecified` | `false` | A request without a version is rejected. |
| `ReportApiVersions` | `true` | Responses carry the `api-supported-versions` and `api-deprecated-versions` headers. |
| `ApiVersionReader` | URL segment, `x-api-version` header and `api-version` query string, combined | Where the version is read from. |
| API explorer `SubstituteApiVersionInUrl` | `true` | Documented routes contain the version instead of `{version}`. |

### Code

```csharp
// Program.cs
using Arc4u.AspNetCore.Versioning;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddServiceApiVersioning();

var app = builder.Build();
app.Run();
```

`ApiVersionDefault.V1` is a static `ApiVersion(1)`. Use it wherever you name the first version in code.

## Common scenarios

### Give the version as a caller

The three readers are combined, so a client uses the one that suits it:

| Source | Example |
|---|---|
| URL segment | `GET /interface/v1/environment` |
| Header `x-api-version` | `x-api-version: 1` |
| Query string `api-version` | `GET /interface/environment?api-version=1` |

The header and query string names are constants. Use them in a typed `HttpClient`, a test or a
gateway configuration instead of repeating the strings:

```csharp
using Arc4u.AspNetCore.Versioning;

public static class ClientSample
{
    public static HttpRequestMessage Request()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/interface/environment");
        request.Headers.Add(ApiVersioningExtensions.VersionHeader, "1");   // x-api-version
        return request;                                                    // VersionQueryString is "api-version"
    }
}
```

### Version a minimal API

Declare the versions in a version set and attach it to the endpoint. The URL-segment reader needs the
`{version:apiVersion}` route constraint:

```csharp
using Arc4u.AspNetCore.Versioning;
using Asp.Versioning;

public static class MinimalApiSample
{
    public static void Map(WebApplication app)
    {
        var versionSet = app.NewApiVersionSet()
                            .HasApiVersion(ApiVersionDefault.V1)
                            .Build();

        app.MapGet("/interface/v{version:apiVersion}/environment", () => Results.Ok())
           .WithApiVersionSet(versionSet);
    }
}
```

### Version a controller

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[ApiVersion("1.0")]
[Route("interface/v{version:apiVersion}/[controller]")]
public class EnvironmentController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
```

Controllers need `builder.Services.AddControllers()` and `app.MapControllers()` as usual.

### Publish a second version and deprecate the first

Map one endpoint per version with `MapToApiVersion`, and mark the version to retire on the
version set or with `Deprecated = true` on a controller:

```csharp
using Arc4u.AspNetCore.Versioning;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

public static class TwoVersionsSample
{
    public static void Map(WebApplication app)
    {
        var v2 = new ApiVersion(2);
        var versionSet = app.NewApiVersionSet()
                            .HasDeprecatedApiVersion(ApiVersionDefault.V1)
                            .HasApiVersion(v2)
                            .Build();

        app.MapGet("/interface/v{version:apiVersion}/environment", () => Results.Ok("v1"))
           .WithApiVersionSet(versionSet)
           .MapToApiVersion(ApiVersionDefault.V1);

        app.MapGet("/interface/v{version:apiVersion}/environment", () => Results.Ok("v2"))
           .WithApiVersionSet(versionSet)
           .MapToApiVersion(v2);
    }
}

[ApiController]
[ApiVersion("1.0")]
[ApiVersion("0.9", Deprecated = true)]
[Route("interface/v{version:apiVersion}/[controller]")]
public class LegacyController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
```

Each response then carries the versions the resource supports, for example
`api-supported-versions: 1.0` and `api-deprecated-versions: 0.9` for the controller. A client can log
or monitor these headers to detect that the version it uses is deprecated before it is removed. The
text of the header follows how the version was declared (`1.0` for `[ApiVersion("1.0")]`, `1, 2` for
`ApiVersion(1)` and `ApiVersion(2)`).

### Get one OpenAPI document per version

The API explorer describes each version through `IApiVersionDescriptionProvider`:

```csharp
using Asp.Versioning.ApiExplorer;

public static class ExplorerSample
{
    public static void List(WebApplication app)
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

        foreach (var description in provider.ApiVersionDescriptions)
        {
            Console.WriteLine($"{description.GroupName} {description.ApiVersion} deprecated: {description.IsDeprecated}");
        }
    }
}
```

By default `GroupName` is the bare version: `1` for version 1. To get `v1`, set the group name
format of the explorer:

```csharp
using Arc4u.AspNetCore.Versioning;
using Asp.Versioning.ApiExplorer;

public static class GroupNameSample
{
    public static void Configure(IServiceCollection services)
    {
        services.AddServiceApiVersioning();
        services.Configure<ApiExplorerOptions>(options => options.GroupNameFormat = "'v'VVV");
    }
}
```

## Extensibility points

`AddServiceApiVersioning()` only registers options, so you can change any of them with a
`Configure` call placed after it. Options set later win:

```csharp
using Arc4u.AspNetCore.Versioning;
using Asp.Versioning;

public static class OverrideSample
{
    public static void Configure(IServiceCollection services)
    {
        services.AddServiceApiVersioning();
        services.Configure<ApiVersioningOptions>(options => options.ReportApiVersions = false);
    }
}
```

> [!WARNING]
> Setting `AssumeDefaultVersionWhenUnspecified` back to `true` removes the guarantee that motivates
> the package: every client that never gave a version breaks the day version 1 is retired.

## Troubleshooting

### A request answers 400 and the body is empty

A request that reaches a versioned endpoint without a version, or with a version the endpoint does not
declare, answers `400 Bad Request`. The body only contains the error when ASP.NET Core can write
a ProblemDetails: call `builder.Services.AddProblemDetails()`. The body then has the code
`ApiVersionUnspecified` or `UnsupportedApiVersion`:

```json
{
  "type": "https://docs.api-versioning.org/problems#unspecified",
  "title": "Unspecified API version",
  "status": 400,
  "detail": "An API version is required, but was not specified.",
  "code": "ApiVersionUnspecified"
}
```

### A request answers 404 instead of 400

When the version is a segment of the route (`/interface/v{version:apiVersion}/environment`), a URL with
no version segment or with a version that no endpoint declares (`/interface/v3/...`) matches no
route, and the answer is `404`. The `400` answers above happen when the route matches without the
version, for example when the version comes from the header or the query string.

## See also

- [gRPC and API versioning](index.md)
- [Architecture](../../concepts/architecture.md)
- [Asp.Versioning documentation](https://github.com/dotnet/aspnet-api-versioning/wiki)
- <xref:Arc4u.AspNetCore.Versioning> in the API reference
