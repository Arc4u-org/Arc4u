# Arc4u.AspNetCore.Versioning

Api versioning for the Arc4u asp net core interface endpoints.

The package is a thin, opinionated wrapper around [Asp.Versioning](https://github.com/dotnet/aspnet-api-versioning).
It exists so that every Arc4u service exposes its versions the same way: same readers, same header names, same
default version and the same rule on what happens when the caller does not give one.

## Registration

```csharp
using Arc4u.AspNetCore.Versioning;

builder.Services.AddInterfaceApiVersioning();
```

That single call registers the api versioning and the api explorer. Nothing else has to be configured.

## How a version is given by the caller

`AddInterfaceApiVersioning` combines three readers, so a client can pick the one that fits the best:

| Source | Example |
| --- | --- |
| Url segment | `GET /interface/v1/Environment` |
| Header (`x-api-version`) | `x-api-version: 1` |
| Query string (`api-version`) | `GET /interface/Environment?api-version=1` |

The header and query string names are exposed as constants, use them instead of hardcoding the strings
(in a typed HttpClient, a test, a gateway configuration...):

```csharp
request.Headers.Add(ApiVersioningExtensions.VersionHeader, "1");        // x-api-version
// ApiVersioningExtensions.VersionQueryString                           // api-version
```

## A version is mandatory

`AssumeDefaultVersionWhenUnspecified` is set to `false` on purpose: a caller that does not give a version is
rejected (`400 Bad Request`, error code `ApiVersionUnspecified`) instead of being silently routed to v1.

The reason is the contract between a service and its clients: an implicit fallback means that the day a v2 is
published and v1 is retired, every client that never bothered to give a version breaks at once, without ever
having stated which version it was written against. Forcing the version makes the dependency explicit and the
retirement of a version a controlled operation.

`ApiVersionDefault.V1` is therefore only the version the routing considers as the default one, not a fallback for
lazy callers. Use it wherever a version has to be named in code:

```csharp
options.DefaultApiVersion = ApiVersionDefault.V1;  // ApiVersion(1)
```

## Versions reported to the caller

`ReportApiVersions` is enabled, so every response carries what the service supports:

```
api-supported-versions: 1.0
api-deprecated-versions: 0.9
```

A client can log or monitor those headers to detect that the version it uses has been deprecated, before it is
removed.

## Versioning an endpoint

Controllers, with the version in the url segment:

```csharp
[ApiController]
[ApiVersion("1.0")]
[Route("interface/v{version:apiVersion}/[controller]")]
public class EnvironmentController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
```

Minimal apis:

```csharp
var versionSet = app.NewApiVersionSet()
                    .HasApiVersion(ApiVersionDefault.V1)
                    .Build();

app.MapGet("/interface/v{version:apiVersion}/environment", () => Results.Ok())
   .WithApiVersionSet(versionSet);
```

## Api explorer and OpenApi

The api explorer is registered with `SubstituteApiVersionInUrl = true`, which means the generated documentation
shows the real route of a version, `interface/v1/Environment`, and not the route template
`interface/v{version}/Environment`. Each version is described as its own document by the
`IApiVersionDescriptionProvider` resolved from the container:

```csharp
var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

foreach (var description in provider.ApiVersionDescriptions)
{
    // description.GroupName => "v1"
}
```

## Remarks

- Target framework: net10.0.
- The package references `Asp.Versioning.Http` and `Asp.Versioning.Mvc.ApiExplorer` and the asp net core shared
  framework.
- The package is flagged as not AOT compatible: Mvc and the ApiExplorer it depends on are not trimmable yet.
