---
description: "Attach the token of a token provider to HttpClient requests and gRPC calls with the Arc4u handlers and interceptor."
---
# HttpClient and gRPC clients

Arc4u attaches tokens with a `DelegatingHandler` for `HttpClient` and an `Interceptor` for gRPC
clients. You add them to the clients you register with `IHttpClientFactory` or the gRPC client
factory, and give each one the named settings that select the
[token provider](token-providers.md).

| Handler | Package | Use it in |
|---|---|---|
| <xref:Arc4u.OAuth2.Token.JwtHttpHandler`1> | `Arc4u.OAuth2.AspNetCore.Authentication` | ASP.NET Core services and web apps. |
| <xref:Arc4u.gRPC.Interceptors.OAuth2Interceptor`1> | `Arc4u.gRPC` | gRPC clients, in services and client apps. |
| <xref:Arc4u.OAuth2.Client.Authentication.Token.JwtHttpHandler`1> | `Arc4u.OAuth2.Client.Authentication` | Desktop and mobile apps. See [Desktop and mobile clients](desktop.md). |
| <xref:Arc4u.Blazor.Handlers.JwtHttpHandler> | `Arc4u.OAuth2.Blazor` | Blazor WebAssembly apps. See [Blazor](blazor.md). |

## Services the handlers need

The server-side handler and the interceptor resolve these services. The
[server authentication](../authentication-server/index.md) registration adds some of them, and
the Arc4u source generator registers the exported ones (see
[Dependency injection](../dependency-injection/index.md)).

| Service | Implementation | Why |
|---|---|---|
| `ILogger<T>` (Arc4u) | `AddILogger()` from `Arc4u.Diagnostics` | The handlers log through the Arc4u logger. With the plain .NET logger, the first call throws `InvalidOperationException: Bad Arc4u usage.` See [Diagnostics](../diagnostics/index.md). |
| <xref:Arc4u.OAuth2.IScopedServiceProviderAccessor> | <xref:Arc4u.OAuth2.AspNetCore.ScopedServiceProviderAccessor> (singleton), with `AddHttpContextAccessor()` | Gives the handler the services of the current request, so it reads the user of that request. |
| `IApplicationContext` | `ApplicationInstanceContext` (scoped in a service) | Holds the current user. |
| The token providers | See [Token providers](token-providers.md) | Keyed by their `ProviderId`. |
| <xref:Arc4u.OAuth2.Token.ITokenCache> | <xref:Arc4u.OAuth2.Token.ApplicationCache> with <xref:Arc4u.OAuth2.Token.CacheHelper> | The client credentials and user name and password providers keep their tokens in it; they fail to resolve without it. `ApplicationCache` stores them in the Arc4u cache named by `AddTokenCache` (section `Authentication:TokenCache`). The [shortest setup](index.md#code) shows the complete registration with a memory cache. |

## Add the handler to an HttpClient

Build a <xref:Arc4u.OAuth2.Token.JwtHttpHandler`1> with the service provider, a logger and the
named settings, in `AddHttpMessageHandler`. The type parameter only sets the logger category.

```csharp
// Program.cs
using Arc4u.Configuration;
using Arc4u.OAuth2.Token;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ... token providers and their settings: see Token providers.

builder.Services.AddHttpClient<InventoryClient>(client => client.BaseAddress = new Uri("https://inventory.example.com/"))
    .AddHttpMessageHandler(sp => new JwtHttpHandler<InventoryClient>(
        sp,
        sp.GetRequiredService<ILogger<InventoryClient>>(),
        sp.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>().Get("Inventory")));

var app = builder.Build();
app.Run();

public sealed class InventoryClient(HttpClient httpClient)
{
    public Task<string> GetStockAsync(string sku, CancellationToken cancellationToken)
        => httpClient.GetStringAsync($"stock/{sku}", cancellationToken);
}
```

`"Inventory"` is the settings name: the entry name under `Authentication:ClientTokens`,
`Authentication:OnBehalfOf` or `Authentication:RemoteSecrets`, or `OAuth2` and `Cookies` for the
user's own token.

### Chain several handlers

One client can serve several kinds of callers, for example an API called both by users (bearer
token) and by a web app (cookie), with a technical account as a fallback. Add one handler per
settings: the first handler that attaches a token wins, because the next ones skip a request
that already has an `Authorization` header.

```csharp
builder.Services.AddHttpClient<InventoryClient>(client => client.BaseAddress = new Uri("https://inventory.example.com/"))
    .AddHttpMessageHandler(sp => CreateHandler(sp, "OAuth2"))
    .AddHttpMessageHandler(sp => CreateHandler(sp, "Cookies"))
    .AddHttpMessageHandler(sp => CreateHandler(sp, "Inventory"));

static JwtHttpHandler<InventoryClient> CreateHandler(IServiceProvider sp, string settingsName)
    => new(sp,
           sp.GetRequiredService<ILogger<InventoryClient>>(),
           sp.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>().Get(settingsName));
```

A handler whose token provider sends a custom header (a remote secret with a `HeaderKey` other
than `Basic`) does not set `Authorization`, so the next handlers still run.

### How the handlers decide

For each request, <xref:Arc4u.OAuth2.Token.JwtHttpHandler`1>:

1. Gets the services of the current request through `IScopedServiceProviderAccessor`: the scope
   set for the current async flow, otherwise the `HttpContext.RequestServices` of the current
   request, otherwise the service provider given to the constructor.
2. Sends the request unchanged when no `IApplicationContext` is registered, or when the settings
   have no `AuthenticationType` (for example when the settings name does not exist).
3. When `AuthenticationType` is not `Inject` and the current user has an identity, sends the
   request unchanged unless the settings value contains the identity's authentication type
   (case-insensitive).
4. Sends the request unchanged when it already has an `Authorization` header.
5. Resolves the keyed `ITokenProvider` named by `ProviderId`, and sends the request unchanged when
   there is none, when the provider returns a failed result or when the token is expired. Settings
   with an `AuthenticationType` but no `ProviderId` throw `KeyNotFoundException`.
6. Sets the header: with `Inject`, the scheme is the token type (`Bearer` and `Basic` go in
   `Authorization`, any other type is the name of a custom header); otherwise the scheme is
   `Bearer`.
7. Adds a `culture` header with the two-letter language of the user, when there is a user.

An exception thrown by the token provider is not caught: the request fails with it.

## Add the interceptor to a gRPC client

<xref:Arc4u.gRPC.Interceptors.OAuth2Interceptor`1> does the same for gRPC calls: it adds an
`authorization` metadata entry (or a custom one) and a `culture` entry. Register it with the gRPC
client factory (`Grpc.Net.ClientFactory` package):

```csharp
// Program.cs
using Arc4u.Configuration;
using Arc4u.gRPC.Interceptors;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ... token providers and their settings: see Token providers.

builder.Services.AddGrpcClient<Inventory.InventoryClient>(options => options.Address = new Uri("https://inventory.example.com"))
    .AddInterceptor(sp => new OAuth2Interceptor<Inventory.InventoryClient>(
        sp,
        sp.GetRequiredService<ILogger<Inventory.InventoryClient>>(),
        sp.GetRequiredService<IOptionsMonitor<SimpleKeyValueSettings>>().Get("Inventory")));

var app = builder.Build();
app.Run();
```

`Inventory.InventoryClient` is the client generated from your `.proto` file. The interceptor
differs from the HttpClient handler on these points:

- It adds nothing when the `IApplicationContext` has no user, even with `Inject` settings. A call
  made with an application identity (client credentials) must still run with a principal set.
- When `AuthenticationType` is not `Inject`, the settings value must be equal to the identity's
  authentication type (case-insensitive), not only contain it.
- It passes the current `ClaimsIdentity` to the token provider as `platformParameters`.
- It waits synchronously for the token provider, and logs and ignores the exceptions the provider
  throws: the call is then sent without a token.

Status codes and ProblemDetails returned by gRPC services are covered in
[gRPC and versioning](../grpc-versioning/index.md).

## Call an API outside a request

A background job (an `IHostedService`, a message handler) has no `HttpContext`. Create a scope,
give it to the accessor so that the handlers read the `IApplicationContext` of that scope, and set
a principal in it when the call needs one:

```csharp
using Arc4u.OAuth2;

public sealed class StockSynchronizer(IServiceScopeFactory scopeFactory, IScopedServiceProviderAccessor accessor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        accessor.ServiceProvider = scope.ServiceProvider;

        var client = scope.ServiceProvider.GetRequiredService<InventoryClient>();
        await client.GetStockAsync("sku-42", stoppingToken);
    }
}
```

The accessor keeps the scope for the rest of the async flow, even after the scope is disposed: set
it again for each new scope, otherwise the next call fails with `ObjectDisposedException`. Without
a scope, the HttpClient handler falls back to the service provider it was created with, while the
gRPC interceptor throws a `NullReferenceException` (known issue).

## Troubleshooting

### The downstream API returns 401 and the request has no Authorization header

- The settings name passed to the handler does not exist: `IOptionsMonitor.Get` then returns empty
  settings and the handler does nothing. Check the entry name in `appsettings.json`.
- The `AuthenticationType` of the settings does not match the caller (step 3 above). Use
  `Inject` for an application identity.
- The token provider is not registered under the `ProviderId` key, or it returned a failed
  result: the handler logs it in the `Technical` category.
- For gRPC, the `IApplicationContext` has no principal.

### The token is sent in an access_token header

The settings use the on-behalf-of provider with `AuthenticationType` `Inject`. Set
`AuthenticationType` to `OAuth2` or `Cookies`, see
[Call an API on behalf of the user](token-providers.md#call-an-api-on-behalf-of-the-user).

### NullReferenceException in the handler

- The `ProviderId` is the string `"null"` (<xref:Arc4u.OAuth2.Token.NullTokenProvider>): the
  provider returns a success without a token (known issue). The service `JwtHttpHandler<T>` and the
  desktop `JwtHttpHandler<T>` then throw `NullReferenceException`. `OAuth2Interceptor<T>` logs the
  exception and sends the call with the `culture` entry only, and the Blazor `JwtHttpHandler` logs
  it and sends no header. Do not add a handler to a client that must not send a token.
- gRPC call outside a request: see [Call an API outside a request](#call-an-api-outside-a-request).

## See also

- [Token providers](token-providers.md)
- [Blazor](blazor.md)
- [gRPC and versioning](../grpc-versioning/index.md)
- <xref:Arc4u.gRPC.Interceptors> in the API reference
