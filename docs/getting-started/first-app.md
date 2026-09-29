---
description: "Build a minimal ASP.NET Core API with Arc4u dependency injection, configuration, logging and one authenticated endpoint."
---
# Build your first Arc4u app

In this tutorial you build a minimal ASP.NET Core API with two endpoints: `GET /hello/{name}`,
open to anyone, and `GET /me`, which requires a JWT bearer token. Along the way you use four
Arc4u features:

- **Dependency injection**: a service registered with the `[Export]` attribute and a source generator.
- **Configuration**: application settings bound and validated at startup.
- **Logging**: the Arc4u logger, which adds a category and context to each entry, on top of Serilog.
- **Authentication**: JWT bearer validation that rejects unauthenticated calls with a JSON error body.

It takes about 15 minutes. You do not need an identity provider: without a token, `/me` answers
`401`, which is what you check at the end.
[Call the protected endpoint with a real token](#call-the-protected-endpoint-with-a-real-token)
shows how to connect one.

The finished application is also in the repository, in
[`samples/GettingStarted`](https://github.com/Arc4u-org/Arc4u/tree/develop/9.0.0/samples/GettingStarted);
see [Run the finished sample](#run-the-finished-sample).

## Prerequisites

- The .NET 10 SDK or later (`dotnet --version`).
- `curl`, or any other HTTP client.
- No prior Arc4u knowledge. [Install Arc4u](index.md) describes the packages in more detail;
  this tutorial repeats the commands you need.

## Step 1: Create the project

Create an empty ASP.NET Core project and move into its folder:

```bash
dotnet new web -n GettingStarted
cd GettingStarted
```

Keep the name `GettingStarted`: the source generator names the registration method after the
assembly (see [Step 5](#step-5-write-programcs)).

## Step 2: Add the packages

```bash
dotnet add package Arc4u.Dependency --prerelease
dotnet add package Arc4u.Dependency.Tool --prerelease
dotnet add package Arc4u.Configuration --prerelease
dotnet add package Arc4u.Diagnostics --prerelease
dotnet add package Arc4u.OAuth2.AspNetCore.Authentication --prerelease
dotnet add package Serilog.AspNetCore
```

| Package | Used for |
|---|---|
| `Arc4u.Dependency` | The `[Export]`, `[Scoped]` and `[Shared]` attributes. |
| `Arc4u.Dependency.Tool` | The source generator that turns those attributes into registrations. |
| `Arc4u.Configuration` | `AddApplicationConfig`, which binds the application settings. |
| `Arc4u.Diagnostics` | The Arc4u logger (`AddILogger`, `Technical()`, `Business()`). |
| `Arc4u.OAuth2.AspNetCore.Authentication` | `AddJwtAuthentication`, the JWT bearer setup. |
| `Serilog.AspNetCore` | Serilog and its console sink, which write the log entries. |

`dotnet list package` now shows the six packages.

## Step 3: Configure the application

Replace the content of `appsettings.json` with:

```json
{
  "Urls": "http://localhost:5080",
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning"
      }
    }
  },
  "Application.Configuration": {
    "ApplicationName": "GettingStarted",
    "Environment": {
      "Name": "Local",
      "LoggingName": "GettingStarted",
      "TimeZone": "UTC"
    }
  },
  "Authentication": {
    "DefaultAuthority": {
      "Url": "https://login.example.com/realms/getting-started"
    },
    "OAuth2.Settings": {
      "Audiences": [ "getting-started-api" ]
    },
    "TokenCache": {
      "CacheName": "Volatile"
    }
  }
}
```

| Key | Read by | Description |
|---|---|---|
| `Urls` | ASP.NET Core | The address the application listens on. It takes precedence over the random port in `Properties/launchSettings.json`. |
| `Serilog:MinimumLevel` | Serilog | Minimum log levels: `Information`, and `Warning` for ASP.NET Core's own entries. |
| `Application.Configuration` | `AddApplicationConfig` | The application name and environment. The four values are required: startup fails with a `ConfigurationException` that lists the missing ones. `Environment:LoggingName` is written in every log entry as `Application`. |
| `Authentication:DefaultAuthority:Url` | `AddJwtAuthentication` | The identity provider that issues the tokens. The placeholder is enough to start: the application contacts the identity provider only to validate a token. |
| `Authentication:OAuth2.Settings:Audiences` | `AddJwtAuthentication` | The values accepted in the `aud` claim of a token. |
| `Authentication:TokenCache:CacheName` | `AddJwtAuthentication` | The name of the Arc4u cache that stores tokens. It must be set, even though this application never uses it. |

`Application.Configuration` and `OAuth2.Settings` are single keys that contain a dot, not nested
sections.

## Step 4: Add a service

Create `Services/GreetingService.cs`:

```csharp
using Arc4u.Configuration;
using Arc4u.Dependency.Attribute;
using Arc4u.Diagnostics;
using Microsoft.Extensions.Options;

namespace GettingStarted.Services;

public interface IGreetingService
{
    string Greet(string name);
}

[Export(typeof(IGreetingService)), Scoped]
public sealed class GreetingService(ILogger<GreetingService> logger, IOptions<ApplicationConfig> config) : IGreetingService
{
    public string Greet(string name)
    {
        logger.Business().LogInformation("Greeting {Name}", name);

        return $"Hello {name}, from {config.Value.ApplicationName} ({config.Value.Environment.Name}).";
    }
}
```

- `[Export(typeof(IGreetingService))]` registers `GreetingService` as the implementation of
  `IGreetingService`. `[Scoped]` gives it a scoped lifetime; use `[Shared]` for a singleton, or no
  lifetime attribute for a transient.
- `IOptions<ApplicationConfig>` gives the values of the `Application.Configuration` section.
- `logger.Business()` marks the entry with the `Business` category. `Technical()` and
  `Monitoring()` are the other categories.

## Step 5: Write Program.cs

Replace the content of `Program.cs` with:

```csharp
using System.Security.Claims;
using Arc4u.Configuration;
using Arc4u.Dependency;
using Arc4u.OAuth2.Extensions;
using GettingStarted.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog writes the log events; the Arc4u logger adds Category, SourceContext, Method and Application to each one.
builder.Services.AddSerilog(logger => logger
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Category,-10} {SourceContext}: {Message:lj}{NewLine}{Exception}"));
builder.Services.AddILogger();

// Binds the Application.Configuration section to IOptions<ApplicationConfig>.
builder.Services.AddApplicationConfig(builder.Configuration);

// Generated at compile time by Arc4u.Dependency.Tool from the [Export] attributes of this assembly.
builder.Services.RegisterGettingStartedTypes();

// Validates JWT bearer tokens issued by the authority in the Authentication section.
builder.Services.AddJwtAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

// Errors without a body (404, unhandled exceptions, ...) become ProblemDetails responses.
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/hello/{name}", (string name, IGreetingService greetings) => greetings.Greet(name));

app.MapGet("/me", (ClaimsPrincipal user) => user.Claims.Select(claim => new { claim.Type, claim.Value }))
   .RequireAuthorization();

app.Run();
```

What each part does:

**Logging.** `AddSerilog` makes Serilog write every log entry, here to the console.
<xref:Arc4u.Dependency.ServicesRegistrationExtension.AddILogger*> replaces the `ILogger<T>`
that ASP.NET Core injects with the Arc4u logger. The Arc4u logger adds `Category`,
`SourceContext` (the class), `Method` and `Application` to each entry, and provides
`Technical()`, `Business()` and `Monitoring()`. Those methods throw
`InvalidOperationException: Bad Arc4u usage.` when `AddILogger` is missing, and
`AddJwtAuthentication` logs through them, so call `AddILogger` in every Arc4u application.

**Configuration.** <xref:Arc4u.Configuration.ConfigurationHelper.AddApplicationConfig*> reads the
`Application.Configuration` section, checks that the four values are set, and registers
`IOptions<ApplicationConfig>`. Pass `sectionName` to read another section.

**Dependency injection.** `Arc4u.Dependency.Tool` scans the classes of your project for
`[Export]` when you compile and generates one extension method, `Register<Name>Types`, in the
`Arc4u.Dependency` namespace. `<Name>` is the last dot-separated part of the assembly name:
`GettingStarted` gives `RegisterGettingStartedTypes`, and `Contoso.Orders.Api` would give
`RegisterApiTypes`. For this project, the generated method contains:

```csharp
services.AddScoped<GettingStarted.Services.IGreetingService, global::GettingStarted.Services.GreetingService>();
```

**Authentication.** `AddJwtAuthentication` reads the `Authentication` section and registers the
ASP.NET Core JWT bearer handler as the default scheme, with Arc4u's `StandardBearerEvents`. When a
request to a protected endpoint has no valid token, `StandardBearerEvents` answers `401` with a
JSON body, and it logs why a token was rejected. `AddAuthorization` and `RequireAuthorization()` are the standard ASP.NET Core calls
that protect `/me`.

**Errors.** `AddProblemDetails`, `UseExceptionHandler` and `UseStatusCodePages` turn other errors,
such as an unknown route, into [ProblemDetails](../concepts/glossary.md#problemdetails) responses.

## Run and verify

Start the application:

```bash
dotnet run
```

The console shows where it listens:

```text
[15:04:54 INF]            Microsoft.Hosting.Lifetime: Now listening on: http://localhost:5080
[15:04:54 INF]            Microsoft.Hosting.Lifetime: Application started. Press Ctrl+C to shut down.
```

In a second terminal, call the anonymous endpoint:

```bash
curl -i http://localhost:5080/hello/Ada
```

```http
HTTP/1.1 200 OK
Content-Type: text/plain; charset=utf-8

Hello Ada, from GettingStarted (Local).
```

The name and environment come from `Application.Configuration`, and `IGreetingService` is resolved
through the generated registration. The application console shows the entry written by
`logger.Business()`, with its category and class:

```text
[15:04:55 INF] Business   GettingStarted.Services.GreetingService: Greeting Ada
```

Now call the protected endpoint without a token:

```bash
curl -i http://localhost:5080/me
```

```http
HTTP/1.1 401 Unauthorized
Content-Type: application/json

{"status":403}
```

With a token that is not a valid JWT, the body also names the error:

```bash
curl -i -H "Authorization: Bearer not-a-jwt" http://localhost:5080/me
```

```http
HTTP/1.1 401 Unauthorized
Content-Type: application/json

{"title":"invalid_token","status":403,"detail":""}
```

and the application logs the reason with the `Technical` category:

```text
[15:04:55 ERR] Technical  Arc4u.OAuth2.Events.StandardBearerEvents: Exception: IDX14100: JWT is not well formed, there are no dots (.).
```

> [!WARNING]
> Known issue: the JSON body of these `401` responses contains `"status":403`, and its content type
> is `application/json` rather than `application/problem+json`. Use the HTTP status code of the
> response, not the `status` field of the body.

You now have a running Arc4u application. Stop it with <kbd>Ctrl</kbd>+<kbd>C</kbd>.

## Call the protected endpoint with a real token

To get a `200` from `/me`, point the application to your identity provider (Microsoft Entra ID,
Keycloak, ADFS, or any OpenID Connect provider) and send one of its access tokens.

1. Set `Authentication:DefaultAuthority:Url` to the authority URL of your identity provider. The
   application downloads the signing keys from `<Url>/.well-known/openid-configuration`; set
   `Authentication:DefaultAuthority:MetaDataAddress` if your provider publishes its metadata
   elsewhere. HTTPS is required to download the metadata, unless the metadata address itself
   starts with `http://`, which is only suitable for a local test provider.
2. Set `Authentication:OAuth2.Settings:Audiences` to the value of the `aud` claim in the access
   tokens your provider issues for this API. For Microsoft Entra ID, it is the application
   (client) ID or the Application ID URI of the API registration, depending on the token version.
3. Restart the application, get an access token from your provider (for example with the client
   credentials flow), and call:

   ```bash
   curl -H "Authorization: Bearer <access-token>" http://localhost:5080/me
   ```

   The response lists the claims of the token, for example:

   ```json
   [{"type":"iss","value":"https://<authority>"},{"type":"aud","value":"getting-started-api"},{"type":"sub","value":"1234"},{"type":"name","value":"Ada Lovelace"},{"type":"iat","value":"1790679780"},{"type":"nbf","value":"1790679780"},{"type":"exp","value":"1790690580"}]
   ```

You can also override the settings with environment variables instead of editing
`appsettings.json` (`__` replaces `:`), for example in a Linux or macOS shell:

```bash
Authentication__DefaultAuthority__Url=https://<your-provider>/realms/<realm> dotnet run
```

With a token that is refused, `/me` still answers `401`, and the `detail` field of the body gives
the reason:

| Token | Response |
|---|---|
| Expired | `401` with an `x-token-expired` response header, and `detail` "The token expired on ..." |
| Wrong audience | `401` with `detail` "The audience '...' is invalid" |

> [!NOTE]
> `AddJwtAuthentication` validates the signature (with the keys of the authority), the lifetime
> and the audience of the token. It does not validate the `iss` claim. Some providers, such as
> Keycloak with its default settings, issue access tokens without an audience for your API: add
> one on the provider side rather than turning audience validation off. The
> [Server authentication](../guides/authentication-server/index.md) guide covers the other
> settings.

## Run the finished sample

The repository contains this application in
[`samples/GettingStarted`](https://github.com/Arc4u-org/Arc4u/tree/develop/9.0.0/samples/GettingStarted).
It references the Arc4u source projects instead of the NuGet packages, so it needs the exact SDK
pinned in [`src/global.json`](https://github.com/Arc4u-org/Arc4u/blob/develop/9.0.0/src/global.json)
(the Arc4u projects also target .NET 11) and the ASP.NET Core 10 runtime.

```bash
git clone --branch develop/9.0.0 https://github.com/Arc4u-org/Arc4u.git
cd Arc4u/samples/GettingStarted
dotnet run
```

Then call the endpoints as in [Run and verify](#run-and-verify).

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| `'IServiceCollection' does not contain a definition for 'RegisterGettingStartedTypes'` | The generator did not run or the name differs. Check that `Arc4u.Dependency.Tool` is referenced, and use the last part of your assembly name (`Register<Name>Types`). |
| `InvalidOperationException: Bad Arc4u usage.` | `AddILogger()` is not called. |
| `ConfigurationException: TokenCacheOptions.CacheName is not defined in the configuration file.` | `Authentication:TokenCache:CacheName` is missing. |
| `ConfigurationException: Audiences field is not defined.` | `Authentication:OAuth2.Settings:Audiences` is missing or empty. |
| `MissingFieldException: DefaultAuthority must be filled!` | `Authentication:DefaultAuthority` is missing. |
| `InvalidOperationException: No section exists with name Authentication ...` | The `Authentication` section is missing. |
| `ConfigurationException: Application environment time zone is not defined ...` (or name, logging name) | A value of `Application.Configuration` is missing. |

## Next steps

Each part of this application has a guide:

- [Dependency injection](../guides/dependency-injection/index.md): `[Export]`, lifetimes, named
  services and the source generators.
- [Configuration](../guides/configuration/index.md): application settings, encrypted secrets and
  configuration stores.
- [Diagnostics and logging](../guides/diagnostics/index.md): log categories, properties and Serilog
  sinks.
- [Server authentication](../guides/authentication-server/index.md): JWT bearer, OpenID Connect and
  the authentication settings.

The other feature areas:

- [Client authentication and Blazor](../guides/authentication-client/index.md): call protected APIs.
- [Caching](../guides/caching/index.md): memory, Redis, SQL Server and Dapr caches.
- [Results and errors](../guides/results/index.md): the result pattern and ProblemDetails.
- [gRPC and API versioning](../guides/grpc-versioning/index.md): gRPC services and versioned APIs.
- [Data access](../guides/data/index.md): Entity Framework Core, MongoDB and OData.
- [Dispatcher](../guides/dispatcher/index.md): dispatch notifications to handlers.

To understand the ideas behind Arc4u, read the [Concepts](../concepts/index.md).
