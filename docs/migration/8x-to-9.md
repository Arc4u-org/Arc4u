---
description: "Every breaking change between Arc4u 8.x and 9, with the code before and after."
---
# Migrate from Arc4u 8.x to 9

This guide is for teams that run an application on Arc4u 8.x (the last version is 8.3.2) and
want to move it to Arc4u 9. It lists every breaking change with the 8.x code, the Arc4u 9 code
and the steps to follow. Work through the sections in order: the first three (packages, target
framework, JSON) apply to every application, the others only if you use the feature.

The Arc4u 9 code in this guide is the code on the `develop/9.0.0` branch. For the complete list of
changes per version, see the [changelog](../releases/index.md).

## Before you start

- [ ] Install the .NET 10 SDK or later. Arc4u 9 targets `net10.0` and `net11.0` only (see
  [Target frameworks](#target-frameworks)).
- [ ] Upgrade to Arc4u 8.3.2 first and fix its build warnings. Several APIs removed in 9 are
  already marked `[Obsolete]` in 8.x and the warning tells you what to use instead.
- [ ] Work on a branch, and back up the configuration files (`appsettings*.json`) and the
  Kubernetes or deployment manifests that hold Arc4u settings. Several configuration keys change.
- [ ] Read the [changelog](../releases/index.md) entry for 9.0.0.
- [ ] Plan to clear distributed caches (Redis, SQL Server, Dapr state) when you deploy if you
  used the Protobuf serializer (see [ADAL and Protobuf removed](#adal-and-protobuf-removed)).

> [!NOTE]
> Arc4u 9 is in preview: install the packages with `--prerelease`. The last preview on NuGet,
> `9.0.0-preview37`, was built before some of the changes described here: it targets `net8.0`,
> `net9.0` and `net10.0`, and it does not contain the
> [authentication settings refactoring](#authentication-settings-refactoring) yet.

## Package renames

The `.Standard` part of the package names referred to .NET Standard. Arc4u 9 no longer targets
.NET Standard ([#134](https://github.com/Arc4u-org/Arc4u/issues/134)), so every
`Arc4u.Standard.*` package is published under a new ID without `.Standard`. The namespaces do not
change: Arc4u 8.x already used `Arc4u.*` namespaces, so most `using` directives stay as they are.

**Before (8.x)**

```xml
<ItemGroup>
  <PackageReference Include="Arc4u.Standard.Caching.Memory" Version="8.3.2" />
  <PackageReference Include="Arc4u.Standard.OAuth2.AspNetCore.Authentication" Version="8.3.2" />
</ItemGroup>
```

**After (9)**

```xml
<ItemGroup>
  <PackageReference Include="Arc4u.Caching.Memory" Version="9.0.0-preview37" />
  <PackageReference Include="Arc4u.OAuth2.AspNetCore.Authentication" Version="9.0.0-preview37" />
</ItemGroup>
```

Steps:

1. Replace each package ID with the new one from the table below. The old IDs stop at 8.3.2
   and have no 9 version.
2. Keep the same version for every Arc4u package: the packages depend on each other.
3. The assembly names change with the package IDs (`Arc4u.Standard.Caching.Memory.dll` becomes
   `Arc4u.Caching.Memory.dll`). Update every configuration value that names an Arc4u assembly, for
   example the assembly-qualified type names in the `Application.Dependency` section
   (`"Arc4u.Caching.Memory.MemoryCache, Arc4u.Standard.Caching.Memory"` becomes
   `"Arc4u.Caching.Memory.MemoryCache, Arc4u.Caching.Memory"`).
4. In a Blazor application, update the static web asset paths: `_content/Arc4u.Standard.OAuth2.Blazor/`
   becomes `_content/Arc4u.OAuth2.Blazor/`.

| Old package | New package |
|---|---|
| `Arc4u.Standard` | `Arc4u` |
| `Arc4u.Standard.AspNetCore.gRpc` | `Arc4u.AspNetCore.gRpc` |
| `Arc4u.Standard.Authorization` | `Arc4u.Authorization` |
| `Arc4u.Standard.Caching` | `Arc4u.Caching` |
| `Arc4u.Standard.Caching.Dapr` | `Arc4u.Caching.Dapr` |
| `Arc4u.Standard.Caching.Memory` | `Arc4u.Caching.Memory` |
| `Arc4u.Standard.Caching.Redis` | `Arc4u.Caching.Redis` |
| `Arc4u.Standard.Caching.SqlServer` | `Arc4u.Caching.SqlServer` |
| `Arc4u.Standard.Configuration` | `Arc4u.Configuration` |
| `Arc4u.Standard.Configuration.Decryptor` | `Arc4u.Configuration.Decryptor` |
| `Arc4u.Standard.Configuration.Store` | `Arc4u.Configuration.Store` |
| `Arc4u.Standard.Core` | `Arc4u.Core` |
| `Arc4u.Standard.Data` | `Arc4u.Data` |
| `Arc4u.Standard.Dependency` | `Arc4u.Dependency` |
| `Arc4u.Standard.Diagnostics` | `Arc4u.Diagnostics` |
| `Arc4u.Standard.Diagnostics.Serilog` | `Arc4u.Diagnostics.Serilog` |
| `Arc4u.Standard.Diagnostics.Serilog.Sinks.RealmDb` | `Arc4u.Diagnostics.Serilog.Sinks.RealmDb` |
| `Arc4u.Standard.Dispatcher` | `Arc4u.Dispatcher` |
| `Arc4u.Standard.EfCore` | `Arc4u.EfCore` |
| `Arc4u.Standard.FluentValidation` | `Arc4u.FluentValidation` |
| `Arc4u.Standard.gRPC` | `Arc4u.gRPC` |
| `Arc4u.Standard.MongoDB` | `Arc4u.MongoDB` |
| `Arc4u.Standard.OAuth2` | `Arc4u.OAuth2` |
| `Arc4u.Standard.OAuth2.AspNetCore` | `Arc4u.OAuth2.AspNetCore` |
| `Arc4u.Standard.OAuth2.AspNetCore.Authentication` | `Arc4u.OAuth2.AspNetCore.Authentication` |
| `Arc4u.Standard.OAuth2.AspNetCore.Blazor` | `Arc4u.OAuth2.AspNetCore.Blazor` |
| `Arc4u.Standard.OAuth2.Blazor` | `Arc4u.OAuth2.Blazor` |
| `Arc4u.Standard.OAuth2.Client` | `Arc4u.OAuth2.Client` |
| `Arc4u.Standard.OData` | `Arc4u.OData` |
| `Arc4u.Standard.Results` | `Arc4u.Results` |
| `Arc4u.Standard.Serializer` | `Arc4u.Serializer` |
| `Arc4u.Standard.Serializer.JSon` | `Arc4u.Serializer.JSon` |
| `Arc4u.Standard.Threading` | `Arc4u.Threading` |

`Arc4u.AspNetCore.Results` and `Arc4u.Configuration.Store.EfCore` keep their names. The
following 8.x packages have no Arc4u 9 version:

| 8.x package | What to do |
|---|---|
| `Arc4u.Standard.Dependency.ComponentModel` | [IContainer replaced by keyed services](#icontainer-replaced-by-keyed-services) |
| `Arc4u.Standard.OAuth2.AspNetCore.Adal`, `Arc4u.Standard.Serializer.Protobuf`, `Arc4u.Standard.Serializer.ProtobufV2` | [ADAL and Protobuf removed](#adal-and-protobuf-removed) |
| `Arc4u.Standard.Diagnostics.TraceListeners` | [TraceListeners removed](#tracelisteners-removed) |
| `Arc4u.Standard.OAuth2.Msal` | [MSAL status](#msal-status) |
| `Arc4u.Standard.NServiceBus`, `Arc4u.Standard.NServiceBus.Core`, `Arc4u.Standard.NServiceBus.RabbitMQ` | [NServiceBus replaced by Dapr pub/sub](#nservicebus-replaced-by-dapr-pubsub) |
| `Arc4u.Prism.DI.Wpf` | [Prism.DI.Wpf replaced by Prism.DryIoc](#prismdiwpf-replaced-by-prismdryioc) |
| `Arc4u.Standard.OAuth2.AspNetCore.Api` | [Other removed APIs](#other-removed-apis) |

Arc4u 9 also adds packages that have no 8.x equivalent: `Arc4u.Dependency.Tool` (source
generators for dependency registration), `Arc4u.OAuth2.Client.Authentication` (OpenID Connect
for desktop and mobile clients) and `Arc4u.AspNetCore.Versioning` (not published on NuGet yet).
The [package support matrix](../guides/package-support.md) lists the status of every package.

> [!WARNING]
> Known issue: `BlazorController` (`Arc4u.OAuth2.AspNetCore.Blazor`) still redirects to
> `_content/Arc4u.Standard.OAuth2.Blazor/GetToken.html`, which no longer exists after the rename,
> so that redirect returns 404.

## Target frameworks

Arc4u 8.3.x targeted `netstandard2.0`, `net8.0` and `net9.0` (ASP.NET Core packages: `net8.0`
and `net9.0`). Arc4u 9 drops .NET Standard and the .NET versions that reach end of support
([#134](https://github.com/Arc4u-org/Arc4u/issues/134)). It targets:

| Packages | Target frameworks |
|---|---|
| All Arc4u 9 packages, except the ones below | `net10.0`, `net11.0` |
| `Arc4u.Dependency` | `netstandard2.0`, `net10.0`, `net11.0` |
| `Arc4u.Dependency.Tool` | `netstandard2.0` (a Roslyn source generator, it runs inside the compiler) |
| `Arc4u.AspNetCore.Versioning` | `net10.0` |

**Before (8.x)**

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>
```

**After (9)**

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
</PropertyGroup>
```

Steps:

1. Set `TargetFramework` to `net10.0` (or `net11.0`) in every project that references Arc4u,
   including class libraries that targeted `netstandard2.0` to share code with Arc4u 8.x.
2. Update the other Microsoft packages of the project (`Microsoft.AspNetCore.*`,
   `Microsoft.Extensions.*`, Entity Framework Core) to the matching major version.
3. Update the base image of your containers (for example `mcr.microsoft.com/dotnet/aspnet:10.0`).

An application that must stay on .NET 8 or .NET 9 stays on Arc4u 8.3.2.

## Newtonsoft.Json replaced by System.Text.Json

Arc4u 9 no longer depends on Newtonsoft.Json ([#137](https://github.com/Arc4u-org/Arc4u/issues/137)).
In 8.x, `Arc4u.Standard.Dependency` referenced `Newtonsoft.Json`, so every Arc4u application could
use it without referencing it. Arc4u types that were serialized with Newtonsoft.Json attributes,
such as `UserProfile`, now use System.Text.Json attributes. The string helpers that Arc4u added to
`DataContractSerializer`, `DataContractJsonSerializer` and `XmlSerializer` (`WriteObject(graph, out string)`,
`ReadObject(string)`, `Serialize`, `Deserialize`) are removed as well.

**Before (8.x)**

```csharp
using Newtonsoft.Json;

public static class OrderJson
{
    // Compiles without a Newtonsoft.Json reference: Arc4u.Standard.Dependency brings it.
    public static string Serialize(int orderId) => JsonConvert.SerializeObject(new { OrderId = orderId });
}
```

**After (9)**

```csharp
using System.Text.Json;

public static class OrderJson
{
    public static string Serialize(int orderId) => JsonSerializer.Serialize(new { OrderId = orderId });
}
```

Steps:

1. Build the solution. Errors `CS0246` on `Newtonsoft.Json` types show the code that relied on the
   Arc4u reference.
2. Either port that code to System.Text.Json (see Microsoft's
   [migration guide](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/migrate-from-newtonsoft)),
   or add an explicit reference: `dotnet add package Newtonsoft.Json`.
3. If you stored Arc4u objects serialized with Newtonsoft.Json (for example in a cache), check that
   System.Text.Json reads them, or clear them.

## ADAL and Protobuf removed

Arc4u 9 removes the packages built on deprecated libraries
([#130](https://github.com/Arc4u-org/Arc4u/issues/130)):

- `Arc4u.Standard.OAuth2.AspNetCore.Adal` used ADAL (`Microsoft.IdentityModel.Clients.ActiveDirectory`),
  which Microsoft no longer supports. It targeted `net6.0` only and its last version is 8.2.1.
- `Arc4u.Standard.Serializer.Protobuf` and `Arc4u.Standard.Serializer.ProtobufV2` provided
  `IObjectSerialization` implementations based on protobuf-net. Their last version is 8.2.1 and
  `ProtoBufSerialization` is marked `[Obsolete("Use Arc4u.Serializer.JSon instead.")]`.

**Before (8.x)**

```csharp
using Arc4u.OAuth2.Extensions;
using Arc4u.Serializer;

// Arc4u.Standard.Serializer.ProtobufV2
builder.Services.AddSingleton<IObjectSerialization, ProtoBufSerialization>();

// Arc4u.Standard.OAuth2.AspNetCore.Adal
var adalOptions = new AdalAuthenticationOptions
{
    // ...
};
builder.Services.AddOpenIdBearerAuthentication(adalOptions);
```

**After (9)**

```csharp
using Arc4u.OAuth2.Extensions;
using Arc4u.Serializer;

// Arc4u.Serializer.JSon
builder.Services.AddSingleton<IObjectSerialization>(_ => new JsonSerialization());

// Arc4u.OAuth2.AspNetCore.Authentication: OpenID Connect and JWT bearer, read from the Authentication section
builder.Services.AddHybridAuthentication(builder.Configuration);
```

Steps:

1. Replace the ADAL registration with the Arc4u 9 authentication of the
   [server authentication guide](../guides/authentication-server/index.md): `AddHybridAuthentication`
   for a web application that signs users in and calls APIs, `AddJwtAuthentication` for an API.
   It works with Microsoft Entra ID, ADFS and other OpenID Connect providers.
2. Register `JsonSerialization` wherever you registered `ProtoBufSerialization`. To keep a named
   serializer that a cache selects with `SerializerName`, register it as a keyed service
   (`AddKeyedSingleton<IObjectSerialization>("<name>", ...)`).
3. Clear the distributed caches that stored Protobuf data, or give the caches new names, before the
   new version starts: the JSON serializer cannot read entries written by the Protobuf serializer.

## TraceListeners removed

`Arc4u.Standard.Diagnostics.TraceListeners` wrote Arc4u logs to `System.Diagnostics` trace
listeners ([#133](https://github.com/Arc4u-org/Arc4u/issues/133)). It targeted `netstandard2.0`,
its last version is 8.2.1 and `TraceLoggerProvider` is marked `[Obsolete("Use Serilog")]`.
Arc4u 9 logs through `Microsoft.Extensions.Logging`: send the logs to Serilog (with
`Arc4u.Diagnostics.Serilog`) or OpenTelemetry.

**Before (8.x)**

```csharp
using Arc4u.Diagnostics;

builder.Logging.AddProvider(new TraceLoggerProvider());
```

**After (9)**

```csharp
using Serilog;

// Packages: Serilog.Extensions.Hosting, Serilog.Settings.Configuration
builder.Services.AddSerilog((services, loggerConfiguration) =>
    loggerConfiguration.ReadFrom.Configuration(builder.Configuration));
```

Steps:

1. Remove the `Arc4u.Standard.Diagnostics.TraceListeners` package and the `TraceLoggerProvider`
   registration, and the trace listener configuration in `app.config` or `web.config`.
2. Configure Serilog or OpenTelemetry as described in the [diagnostics guide](../guides/diagnostics/index.md).

## MSAL status

Arc4u 9 does not ship MSAL support ([#135](https://github.com/Arc4u-org/Arc4u/issues/135)).
`Arc4u.Standard.OAuth2.Msal` (`MsalTokenProvider` and `PublicClientApp`, for desktop clients) has
no 9 package: its last version is 8.3.2. The source code is still in the repository
(`src/Arc4u.OAuth.Msal`), but it is not part of the solution and is not built or published.

| 8.x client | Arc4u 9 |
|---|---|
| Desktop or mobile client with `MsalTokenProvider` | `Arc4u.OAuth2.Client.Authentication`: OpenID Connect with `Duende.IdentityModel.OidcClient` |
| Blazor WebAssembly with `BlazorMsalTokenProvider` | `BlazorMsalTokenProvider` is still in `Arc4u.OAuth2.Blazor` |

**Before (8.x)**

```csharp
using Arc4u.Dependency.ComponentModel;
using Arc4u.OAuth2.Msal.TokenProvider.Client;
using Arc4u.OAuth2.Token;
using Microsoft.Identity.Client;

public static class ClientAuthentication
{
    public static void Configure(IServiceCollection services)
    {
        var publicClient = PublicClientApplicationBuilder.Create("<client-id>")
                                                         .WithAuthority("https://login.microsoftonline.com/<tenant-id>")
                                                         .WithDefaultRedirectUri()
                                                         .Build();

        var container = new ComponentModelContainer(services);
        container.RegisterInstance(new PublicClientApp { PublicClient = publicClient });
        container.Register<ITokenProvider, MsalTokenProvider>(MsalTokenProvider.ProviderName);
    }
}
```

**After (9)**

```csharp
using Arc4u.OAuth2.Extensions;
using Duende.IdentityModel.OidcClient.Browser;

public static class ClientAuthentication
{
    // browser: your IBrowser implementation that shows the identity provider's sign-in page.
    public static void Configure(IServiceCollection services, IConfiguration configuration,
                                 IBrowser browser, ILoggerFactory loggerFactory)
    {
        services.AddOidcClientAuthentication(browser, loggerFactory, configuration);
    }
}
```

Steps:

1. Replace `Arc4u.Standard.OAuth2.Msal` with `Arc4u.OAuth2.Client.Authentication`.
2. Implement `IBrowser` for your UI technology and move the client settings to the `Authentication`
   section, as described in the [client authentication guide](../guides/authentication-client/index.md).

> [!WARNING]
> Known issue: `BlazorTokenProvider` and `BlazorMsalTokenProvider` are both registered under the
> provider name `blazor`, so only one of them can be resolved by name in the same application.

## NServiceBus replaced by Dapr pub/sub

`Arc4u.Standard.NServiceBus`, `Arc4u.Standard.NServiceBus.Core` and
`Arc4u.Standard.NServiceBus.RabbitMQ` are deprecated and removed in Arc4u 9
([#135](https://github.com/Arc4u-org/Arc4u/issues/135), [#189](https://github.com/Arc4u-org/Arc4u/issues/189)).
They targeted `net8.0` only and depended on NServiceBus 7.8. Arc4u 9 has no messaging package:
use [Dapr pub/sub](https://docs.dapr.io/developing-applications/building-blocks/pubsub/howto-publish-subscribe/)
directly with the `Dapr.AspNetCore` package. The broker (RabbitMQ, Azure Service Bus, Kafka and
others) is then a Dapr component that you change without touching the code.

| NServiceBus with Arc4u 8.x | Dapr |
|---|---|
| `HandleMessageBase<T>.Handle(T)` | An endpoint with a `[Topic]` attribute that receives the event |
| `MessagesToPublish.Add(event)` | `DaprClient.PublishEventAsync(pubsubName, topic, event)` |
| `SenderEndpointConfigBase` and `ReceiverEndpointConfigurationBase` (transport and routing) | A pub/sub component file (YAML) |
| Command sent with `MessagesToPublish.Add(command)` | A dedicated topic, or Dapr service invocation |

**Before (8.x)**

```csharp
using Arc4u.NServiceBus;

public sealed record OrderPlaced(int OrderId);
public sealed record InvoiceRequested(int OrderId);

public sealed class OrderPlacedHandler : HandleMessageBase<OrderPlaced>
{
    public override Task Handle(OrderPlaced message)
    {
        // ... business work
        MessagesToPublish.Add(new InvoiceRequested(message.OrderId));
        return Task.CompletedTask;
    }
}
```

**After (9)**

Publish an event from any service with `DaprClient` (register it with `builder.Services.AddDaprClient()`):

```csharp
using Dapr.Client;

public sealed record OrderPlaced(int OrderId);

public sealed class OrderPublisher(DaprClient daprClient)
{
    public Task PublishAsync(OrderPlaced message, CancellationToken cancellationToken)
        => daprClient.PublishEventAsync("pubsub", "order-placed", message, cancellationToken);
}
```

Subscribe in the receiving service:

```csharp
// Program.cs
using Dapr;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDaprClient();

var app = builder.Build();
app.UseCloudEvents();
app.MapSubscribeHandler();

app.MapPost("/orders/placed", [Topic("pubsub", "order-placed")] (OrderPlaced message) =>
{
    // ... business work
    return Results.Ok();
});

await app.RunAsync();
```

Declare the broker as a Dapr component named `pubsub`, for example RabbitMQ:

```yaml
apiVersion: dapr.io/v1alpha1
kind: Component
metadata:
  name: pubsub
spec:
  type: pubsub.rabbitmq
  version: v1
  metadata:
  - name: connectionString
    value: "amqp://localhost:5672"
```

Steps:

1. Run each service with a Dapr sidecar and add the pub/sub component (see the
   [RabbitMQ component reference](https://docs.dapr.io/reference/components-reference/supported-pubsub/setup-rabbitmq/)).
2. Add `Dapr.AspNetCore` to the services, and replace each `HandleMessageBase<T>` with a subscribed
   endpoint and each `MessagesToPublish.Add` with `PublishEventAsync`.
3. `MessagesScope` published the collected messages when the unit of work completed. With Dapr, call
   `PublishEventAsync` after your data is saved. If the publish must be part of the database
   transaction, look at the Dapr [outbox pattern](https://docs.dapr.io/developing-applications/building-blocks/state-management/howto-outbox/).
4. Remove the NServiceBus packages and endpoint configuration classes.

## Prism.DI.Wpf replaced by Prism.DryIoc

`Arc4u.Prism.DI.Wpf` adapted Prism 7.2 to the Arc4u `IContainer`. It is deprecated and removed in
Arc4u 9 ([#189](https://github.com/Arc4u-org/Arc4u/issues/189)): it targeted `net8.0-windows` and
`net9.0-windows`, and `IContainer` no longer exists in Arc4u 9 (see
[IContainer replaced by keyed services](#icontainer-replaced-by-keyed-services)). Use Prism's own
DryIoc container, `Prism.DryIoc`.

**Before (8.x)**

```csharp
// App.xaml.cs
using System.Windows;
using Arc4u.Dependency;
using Arc4u.Dependency.ComponentModel;
using Prism.DI;
using Prism.DI.Ioc;
using Prism.Ioc;

public partial class App : PrismApplication
{
    protected override IContainerExtension CreateContainerExtension()
        => new AppContainerExtension(new ComponentModelContainer());

    protected override Window CreateShell() => Container.Resolve<MainWindow>();

    protected override void RegisterTypes(Prism.Ioc.IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<IOrderService, OrderService>();
    }
}

// Your subclass of the abstract DIContainerExtension.
public sealed class AppContainerExtension(IContainer container) : DIContainerExtension(container)
{
    public override void FinalizeExtension() => Container.CreateContainer();
}
```

**After (9)**

```csharp
// App.xaml.cs
using System.Windows;
using Arc4u.Dependency;
using DryIoc.Microsoft.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Prism.DryIoc;
using Prism.Ioc;

public partial class App : PrismApplication
{
    protected override Window CreateShell() => Container.Resolve<MainWindow>();

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.Register<IOrderService, OrderService>();

        // Arc4u 9 registers its services in an IServiceCollection: copy them into DryIoc.
        var services = new ServiceCollection();
        services.AddILogger();
        containerRegistry.GetContainer().Populate(services);
    }
}
```

Steps:

1. Replace `Arc4u.Prism.DI.Wpf` with `Prism.DryIoc`, and add `DryIoc.Microsoft.DependencyInjection`
   if you register Arc4u services with their `IServiceCollection` extension methods.
2. Derive `App` from `Prism.DryIoc.PrismApplication` and delete your `DIContainerExtension` subclass.
3. Prism 9 moved some types to new namespaces (for example the regions); follow the Prism upgrade
   notes for the version you choose.

> [!IMPORTANT]
> `Prism.DryIoc` 9 requires a Prism license: the package asks you to accept the Prism Community
> License or the Prism Commercial License, depending on the size of your organization. Read the
> terms on [prismlibrary.com](https://prismlibrary.com/) before you upgrade.

## Message and Messages replaced by ProblemDetails

Arc4u 8.x reported errors with `Message` objects collected in `Messages` and thrown in an
`AppException`. Arc4u 9 removes `Message`, `Messages`, `MessageType`, `LocalizedMessage` and
`AppException` ([#142](https://github.com/Arc4u-org/Arc4u/issues/142)). Methods return a FluentResults
`Result` instead, and the errors become a
[ProblemDetails](../concepts/glossary.md#problemdetails) response (HTTP) or an `RpcException` (gRPC).

**Before (8.x)**

```csharp
using Arc4u.ServiceModel;
using Microsoft.AspNetCore.Mvc;

public sealed record Order(int Id, int Quantity);

public sealed class OrderService(ILogger<OrderService> logger)
{
    public Order Create(Order order)
    {
        var messages = new Messages();
        if (order.Quantity <= 0)
        {
            messages.Add(new Message(MessageCategory.Business, MessageType.Error, "The quantity must be positive."));
        }

        messages.LogAndThrowIfNecessary(logger); // throws an AppException when an error was added

        // ...
        return order;
    }
}

[ApiController]
[Route("orders")]
public sealed class OrdersController(OrderService orderService) : ControllerBase
{
    [HttpPost]
    public ActionResult<Order> Create(Order order) => Ok(orderService.Create(order));
}
```

**After (9)**

```csharp
using Arc4u.AspNetCore.Results;
using Arc4u.Results.Validation;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

public sealed record Order(int Id, int Quantity);

public sealed class OrderService
{
    public Result<Order> Create(Order order)
    {
        if (order.Quantity <= 0)
        {
            return Result.Fail(ValidationError.Create("The quantity must be positive.")
                                              .WithCode("Order.Quantity"));
        }

        // ...
        return Result.Ok(order);
    }
}

[ApiController]
[Route("orders")]
public sealed class OrdersController(OrderService orderService) : ControllerBase
{
    [HttpPost]
    public ActionResult<Order> Create(Order order) => orderService.Create(order).ToActionOkResult();
}
```

In 8.x, the `ManageExceptionsFilter` turned an `AppException` into a response that contained the
business messages. In Arc4u 9 the filter only handles unexpected exceptions (403 for
`UnauthorizedAccessException`, 500 for any other exception); a `ValidationError` becomes a 422
validation problem details response through `ToActionOkResult`.

| 8.x | Arc4u 9 |
|---|---|
| `Messages`, `Message` with `MessageType.Error` | `Result.Fail(...)` with a `ValidationError` (expected, 422) or a `ProblemDetailError` (with your status code) |
| `throw new AppException(...)` | Return a failed `Result`; throw only for unexpected errors |
| `Messages.LogAndThrowIfNecessary(logger)` | `result.LogIfFailed()` and return the result |
| `PersistEntity.TryValidate()` returning `Messages` | `PersistEntity.TryValidate()` returning `Result` |
| `ValidateAll<T>(logger)` throwing | `ValidateAll<T>(logger)` returning `Result` |
| `ITokenProvider.GetTokenAsync` returning `TokenInfo?` | Returns `Result<TokenInfo>` |
| `IAppPrincipalFactory.CreatePrincipalAsync(Messages messages, ...)` | `CreatePrincipalAsync(...)` without `Messages`, returns `Result<AppPrincipal>` |
| `Arc4u.ServiceModel.MessageCategory` | `Arc4u.Diagnostics.MessageCategory` |

Steps:

1. Change the business methods to return `Result` or `Result<T>` and replace each `Message` with a
   `ValidationError` or a `ProblemDetailError`.
2. In controllers and minimal APIs, convert the result with the `Arc4u.AspNetCore.Results`
   extension methods; in gRPC services, use `ToRpcException()` from `Arc4u.AspNetCore.gRpc`. The
   [results guide](../guides/results/index.md) shows every method and status code.
3. Update your own `ITokenProvider` and `IAppPrincipalFactory` implementations to return a `Result`.
4. Clients that read the list of `Message` from a 8.x response must read a ProblemDetails body instead.

## IContainer replaced by keyed services

Arc4u 8.x resolved services by name through its own container abstraction: `IContainer`,
`IContainerRegistry`, `IContainerResolve`, `ComponentModelContainer` (package
`Arc4u.Standard.Dependency.ComponentModel`), `DependencyContext` and `ContainerContext`. Arc4u 9
removes them and uses the keyed services of `Microsoft.Extensions.DependencyInjection`: the 8.x name
becomes the service key ([#140](https://github.com/Arc4u-org/Arc4u/issues/140)). The `[Export]`,
`[Shared]` and `[Scoped]` attributes stay in `Arc4u.Dependency`.

**Before (8.x)**

```csharp
using Arc4u.Dependency;
using Arc4u.Dependency.ComponentModel;

public interface IPaymentGateway
{
    Task PayAsync(decimal amount);
}

public sealed class CardPaymentGateway : IPaymentGateway
{
    public Task PayAsync(decimal amount) => Task.CompletedTask;
}

public static class Registration
{
    public static void Register(WebApplicationBuilder builder)
    {
        // Registers the types listed in Application.Dependency, then a named service.
        var container = new ComponentModelContainer(builder.Services).InitializeFromConfig(builder.Configuration);
        container.Register<IPaymentGateway, CardPaymentGateway>("card");
    }
}

public sealed class CheckoutService(IContainerResolve container)
{
    public Task CheckoutAsync(decimal amount) => container.Resolve<IPaymentGateway>("card")!.PayAsync(amount);
}
```

**After (9)**

```csharp
using Arc4u.Caching;
using Arc4u.Caching.Memory;

public interface IPaymentGateway
{
    Task PayAsync(decimal amount);
}

public sealed class CardPaymentGateway : IPaymentGateway
{
    public Task PayAsync(decimal amount) => Task.CompletedTask;
}

public static class Registration
{
    public static void Register(WebApplicationBuilder builder)
    {
        builder.Services.AddKeyedTransient<IPaymentGateway, CardPaymentGateway>("card");

        // Was "Arc4u.Caching.Memory.MemoryCache, Arc4u.Standard.Caching.Memory" in Application.Dependency.
        builder.Services.AddKeyedTransient<ICache, MemoryCache>("Memory");
    }
}

public sealed class CheckoutService([FromKeyedServices("card")] IPaymentGateway gateway)
{
    public Task CheckoutAsync(decimal amount) => gateway.PayAsync(amount);
}
```

| 8.x | Arc4u 9 |
|---|---|
| `container.Register<IFoo, Foo>()`, `RegisterScoped`, `RegisterSingleton` | `services.AddTransient<IFoo, Foo>()`, `AddScoped`, `AddSingleton` |
| `container.Register<IFoo, Foo>("name")` | `services.AddKeyedTransient<IFoo, Foo>("name")` (and `AddKeyedScoped`, `AddKeyedSingleton`) |
| `container.RegisterInstance(instance, "name")` | `services.AddKeyedSingleton<IFoo>("name", instance)` |
| `IContainerResolve` injected in a class | `IServiceProvider`, or constructor parameters |
| `Resolve<T>("name")` | `GetRequiredKeyedService<T>("name")`, or a `[FromKeyedServices("name")]` parameter |
| `TryResolve<T>("name", out var value)` | `TryGetService<T>("name", out var value)` (`Arc4u.Dependency`) |
| `ResolveAll<T>()`, `ResolveAll<T>("name")` | `GetServices<T>()`, `GetKeyedServices<T>("name")` |
| `CreateScope()` on the container | `IServiceProvider.CreateScope()` |
| `InitializeFromConfig(configuration)` reading `Application.Dependency` at run time | Explicit registrations, or the `Arc4u.Dependency.Tool` source generator |
| `GetCacheContext(this IContainerResolve)`, `InitializeTimeZoneContext(this IContainerResolve)` | Same methods on `IServiceProvider` |

Steps:

1. Remove `Arc4u.Standard.Dependency.ComponentModel` and every `ComponentModelContainer`,
   `InitializeFromConfig` and `DependencyContext` call.
2. Register each named type as a keyed service. Arc4u types that 8.x found through the
   `Application.Dependency` section must be registered too: `AddCacheContext` resolves a cache
   implementation by the key of its `Kind` (`Memory`, `Redis`, `Sql`, `Dapr`), so without the
   keyed `ICache` registration shown above the cache is not created.
3. To keep registering types from `[Export]` attributes and from `Application.Dependency`, add the
   `Arc4u.Dependency.Tool` generator: it generates the registrations at compile time. The
   [dependency injection guide](../guides/dependency-injection/index.md) explains the setup.
4. Replace `IContainerResolve` with `IServiceProvider` or with constructor injection.

## Authentication settings refactoring

Arc4u 9 changes how the server authentication is registered and configured
(`Arc4u.OAuth2.AspNetCore.Authentication`). The settings that 8.x let you name or point to by
configuration are now fixed, so fewer keys are needed.

### Registration methods

In 8.x, `AddOidcAuthentication` registered OpenID Connect, cookies **and** JWT bearer. In Arc4u 9 that
combination is `AddHybridAuthentication`, and `AddOidcAuthentication` registers OpenID Connect and
cookies only, without JWT bearer. `AddJwtAuthentication` (API only) keeps its name.

**Before (8.x)**

```csharp
// Program.cs
using Arc4u.OAuth2.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOidcAuthentication(builder.Configuration);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
await app.RunAsync();
```

**After (9)**

```csharp
// Program.cs
using Arc4u.OAuth2.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHybridAuthentication(builder.Configuration);

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
await app.RunAsync();
```

Use `AddOidcAuthentication` only when the application never receives bearer tokens. When you
configure the options in code instead of in `appsettings.json`, the OAuth2 settings moved to
`HybridAuthenticationOptions.OAuth2SettingsOptions`, and `Certificate` is renamed
`DataProtectionCertificate`.

### Configuration keys

The configuration binder ignores unknown keys, so the removed keys do not break the startup, but
they have no effect any more. Remove them to avoid confusion.

| Section | Key | Change in Arc4u 9 |
|---|---|---|
| `Authentication` | `OpenIdSettingsKey`, `OAuth2SettingsKey` | No longer used: the settings names are fixed (see [Settings names](#settings-names)). |
| `Authentication` | `JwtBearerEventsType`, `CookieAuthenticationEventsType`, `OpenIdConnectEventsType` | Removed: register your events class in the service collection (see [Custom events](#custom-events)). |
| `Authentication` | `CookiesConfigureOptionsType` | Removed. |
| `Authentication` | `NameClaimType` | Added. Default `name`. |
| `Authentication` | `RoleClaimType` | Added. Default `role`. |
| `Authentication` | `AuthenticationMethod` | Added. `RedirectGet` (default) or `FormPost`. |
| `Authentication:OpenId.Settings` | `AuthenticationType` | Removed: always `Cookies`. |
| `Authentication:OAuth2.Settings` | `AuthenticationType` | Removed: always `OAuth2`. |
| `Authentication:ClaimsMiddleWare:ClaimsFiller` | `SettingsKeys` | Removed. |
| `Authentication` (JWT) | `ClientSecretSectionPath` (section `Authentication:ClientSecrets`) | Replaced by `ClientTokensSectionPath` (section `Authentication:ClientTokens`), see [Client secrets replaced by client tokens](#client-secrets-replaced-by-client-tokens). |

**Before (8.x)**

```json
{
  "Authentication": {
    "OpenId.Settings": {
      "ClientId": "<client-id>",
      "ClientSecret": "<client-secret>",
      "AuthenticationType": "Cookies",
      "Audiences": [ "<client-id>" ],
      "Scopes": [ "openid", "offline_access" ]
    },
    "OAuth2.Settings": {
      "AuthenticationType": "OAuth2Bearer",
      "Audiences": [ "<api-audience>" ]
    },
    "ClaimsMiddleWare": {
      "ClaimsFiller": {
        "LoadClaimsFromClaimsFillerProvider": true,
        "SettingsKeys": [ "OAuth2" ]
      }
    }
  }
}
```

**After (9)**

```json
{
  "Authentication": {
    "OpenId.Settings": {
      "ClientId": "<client-id>",
      "ClientSecret": "<client-secret>",
      "Audiences": [ "<client-id>" ],
      "Scopes": [ "openid", "offline_access" ]
    },
    "OAuth2.Settings": {
      "Audiences": [ "<api-audience>" ]
    },
    "ClaimsMiddleWare": {
      "ClaimsFiller": {
        "LoadClaimsFromClaimsFillerProvider": true
      }
    }
  }
}
```

The other keys of the `Authentication` section (`DefaultAuthority`, `CookieName`, `DataProtection`,
`TokenCache` and so on) are unchanged; the [server authentication guide](../guides/authentication-server/index.md)
documents all of them.

### Settings names

Arc4u registers the OpenID Connect and OAuth2 settings as named `SimpleKeyValueSettings`. The names
changed, and so did the `AuthenticationType` of the identities that JWT bearer authentication creates:

| Settings | 8.x name | Arc4u 9 name |
|---|---|---|
| OpenID Connect (`OpenId.Settings`) | `OpenId` | `Cookies` (`Constants.CookiesAuthenticationType`) |
| OAuth2 (`OAuth2.Settings`) | `OAuth2` | `OAuth2` (`Constants.BearerAuthenticationType`) |
| `AuthenticationType` of a bearer identity | `OAuth2Bearer` | `OAuth2` |

`JwtHttpHandler` also became generic: the type parameter is the category of its logger.

**Before (8.x)**

```csharp
using Arc4u.Configuration;
using Arc4u.OAuth2.Token;
using Microsoft.Extensions.Options;

public sealed class OrdersApiHandler(IServiceProvider serviceProvider,
                                     ILogger<JwtHttpHandler> logger,
                                     IOptionsMonitor<SimpleKeyValueSettings> settings)
    : JwtHttpHandler(serviceProvider, logger, settings.Get("OpenId"));
```

**After (9)**

```csharp
using Arc4u.Configuration;
using Arc4u.OAuth2;
using Arc4u.OAuth2.Token;
using Microsoft.Extensions.Options;

public sealed class OrdersApiHandler(IServiceProvider serviceProvider,
                                     ILogger<OrdersApiHandler> logger,
                                     IOptionsMonitor<SimpleKeyValueSettings> settings)
    : JwtHttpHandler<OrdersApiHandler>(serviceProvider, logger, settings.Get(Constants.CookiesAuthenticationType));
```

Search your code for `"OpenId"`, `Constants.OpenIdOptionsName`, `Constants.OAuth2OptionsName` and
`"OAuth2Bearer"` and replace them with the Arc4u 9 names.

### Custom claims filler

`IClaimsFiller.GetAsync` no longer receives the settings and the parameter object.

**Before (8.x)**

```csharp
using System.Security.Principal;
using Arc4u;
using Arc4u.IdentityModel.Claims;
using Arc4u.Security.Principal;

public sealed class DatabaseClaimsFiller : IClaimsFiller
{
    public Task<IEnumerable<ClaimDto>> GetAsync(IIdentity identity, IEnumerable<IKeyValueSettings> settings, object? parameter)
    {
        IEnumerable<ClaimDto> claims = [new ClaimDto("department", "sales")];
        return Task.FromResult(claims);
    }
}
```

**After (9)**

```csharp
using System.Security.Principal;
using Arc4u.IdentityModel.Claims;
using Arc4u.Security.Principal;

public sealed class DatabaseClaimsFiller : IClaimsFiller
{
    public Task<IEnumerable<ClaimDto>> GetAsync(IIdentity identity)
    {
        IEnumerable<ClaimDto> claims = [new ClaimDto("department", "sales")];
        return Task.FromResult(claims);
    }
}
```

### Custom events

In 8.x you named your events classes in configuration (`JwtBearerEventsType` and the others). In
Arc4u 9 the registration methods resolve `JwtBearerEvents`, `CookieAuthenticationEvents` and
`OpenIdConnectEvents` from the service collection, and register the Arc4u classes only if you have
not registered your own. Register yours before the `Add…Authentication` call:

```csharp
using Arc4u.OAuth2.Events;
using Arc4u.OAuth2.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;

builder.Services.AddTransient<JwtBearerEvents, AuditBearerEvents>();
builder.Services.AddHybridAuthentication(builder.Configuration);
```

```csharp
using Arc4u.OAuth2.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;

public sealed class AuditBearerEvents(ILogger<StandardBearerEvents> logger) : StandardBearerEvents(logger)
{
    public override Task TokenValidated(TokenValidatedContext context)
    {
        // ... your code
        return base.TokenValidated(context);
    }
}
```

Steps:

1. Replace `AddOidcAuthentication` with `AddHybridAuthentication` if the application also accepts
   bearer tokens.
2. Remove the keys listed in the table above and register your events classes in code.
3. Replace the `OpenId` settings name with `Cookies`, and `OAuth2Bearer` with `OAuth2`.
4. Update your `IClaimsFiller` implementations and your `JwtHttpHandler` subclasses.

> [!WARNING]
> Known issue: the configuration overloads of `AddOidcAuthentication` and `AddHybridAuthentication`
> do not apply `ValidateAudience` and `ValidateAuthority` from the `Authentication` section, and
> setting `ValidateAudience` to `false` in `OpenId.Settings` makes the sign-in fail with a
> `KeyNotFoundException` on `Audiences`.

## Client secrets replaced by client tokens

`AddJwtAuthentication` registered the credentials a service uses to call other services from the
`Authentication:ClientSecrets` section. Arc4u 9 reads the `Authentication:ClientTokens` section
instead. Each entry names a `Scenario`, and the credentials move to a `Settings` dictionary. The
entry name (`Backend` below) is still the name of the settings to pass to a `JwtHttpHandler`.

**Before (8.x)**

```json
{
  "Authentication": {
    "ClientSecrets": {
      "Backend": {
        "ClientId": "<client-id>",
        "User": "<service-account>",
        "Password": "<password>",
        "Scopes": [ "openid" ]
      }
    }
  }
}
```

**After (9)**

```json
{
  "Authentication": {
    "ClientTokens": {
      "Backend": {
        "Scenario": "UserPassword",
        "Scopes": [ "openid" ],
        "Settings": {
          "ClientId": "<client-id>",
          "User": "<service-account>",
          "Password": "<password>"
        }
      }
    }
  }
}
```

| 8.x entry | Arc4u 9 `Scenario` | Keys in `Settings` |
|---|---|---|
| `User` and `Password` | `UserPassword` | `ClientId`, `User`, `Password`, optional `ClientSecret` |
| `Credential` (`user:password`) | `Basic` | `ClientId`, `Credential`, optional `ClientSecret` |
| (not available) | `ClientCredentials` | `ClientId`, `ClientSecret` |

`Authority`, `Scopes` and `AuthenticationType` stay at the level of the entry. `ProviderId` and
`BasicProviderId` are removed: the scenario selects the token provider.

## Fluent logging API

The Arc4u logging extensions were rewritten for performance
([#150](https://github.com/Arc4u-org/Arc4u/pull/150)). `Technical()`, `Business()` and
`Monitoring()` still exist on `ILogger<T>`, but they return an `ILoggerWrapper<T>`, which is an
`ILogger<T>`: you add properties first and then log with the standard `Log…` methods. There is no
`Log()` call at the end any more.

**Before (8.x)**

```csharp
using Arc4u.Diagnostics;

public sealed class InvoiceService(ILogger<InvoiceService> logger)
{
    public void Send(int invoiceId)
    {
        logger.Technical().Information("Sending invoice {InvoiceId}", invoiceId).Add("InvoiceId", invoiceId).Log();
        logger.Business().Warning("Invoice {InvoiceId} is overdue", invoiceId).Log();
    }
}
```

**After (9)**

```csharp
using Arc4u.Diagnostics;

public sealed class InvoiceService(ILogger<InvoiceService> logger)
{
    public void Send(int invoiceId)
    {
        logger.Technical().Add("InvoiceId", invoiceId).LogInformation("Sending invoice {InvoiceId}", invoiceId);
        logger.Business().LogWarning("Invoice {InvoiceId} is overdue", invoiceId);
    }
}
```

Steps:

1. Call `builder.Services.AddILogger()` (`AddApplicationContext()` calls it too). Without it,
   `Technical()`, `Business()` and `Monitoring()` throw `InvalidOperationException("Bad Arc4u usage.")`,
   because the injected `ILogger<T>` is not the Arc4u wrapper.
2. Rewrite each call: move `Add…` before the log call, replace `Information`, `Warning`, `Error`,
   `Fatal`, `Debug` with `LogInformation`, `LogWarning`, `LogError`, `LogCritical`, `LogDebug`, and
   drop `.Log()`.
3. Replace `logger.Technical().From<T>()` on a non-generic `ILogger` with `logger.Technical<T>()`.
4. `LoggerContext` is removed: use `ILogger.BeginScope` or an `IAddPropertiesToLog` implementation to
   add properties to every entry. The [diagnostics guide](../guides/diagnostics/index.md) covers both.

> [!WARNING]
> Known issue: the properties added with `Add` are not cleared after the entry is written. They are
> added to every later entry written by the same `ILogger<T>` instance, which can leak data between
> requests when that instance is shared.

## Result extension methods

The FluentResults extension methods of `Arc4u.Results` changed in two ways:

- `OnFailed` callbacks receive an `IReadOnlyCollection<IError>` instead of a `List<IError>`.
- Passing an asynchronous lambda to a synchronous method (`OnSuccess`, `OnFailed`, `OnSuccessNull`,
  `OnSuccessNotNull`) is now a compile error (`CS0619`). In 8.x the lambda ran as `async void`: nothing
  awaited it and its exceptions were lost. Use the `…Async` methods and await them.

**Before (8.x)**

```csharp
using Arc4u.Results;
using FluentResults;

public interface INotifier
{
    Task NotifyAsync();
}

public static class OrderNotifications
{
    public static Result Notify(Result result, INotifier notifier, ILogger logger)
        => result.OnSuccess(async () => await notifier.NotifyAsync())
                 .OnFailed(errors => errors.ForEach(error => logger.LogWarning("{Message}", error.Message)));
}
```

**After (9)**

```csharp
using Arc4u.Results;
using FluentResults;

public interface INotifier
{
    Task NotifyAsync();
}

public static class OrderNotifications
{
    public static Task<Result> NotifyAsync(Result result, INotifier notifier, ILogger logger)
        => result.OnSuccessAsync(() => notifier.NotifyAsync())
                 .OnFailed(errors =>
                 {
                     foreach (var error in errors)
                     {
                         logger.LogWarning("{Message}", error.Message);
                     }
                 });
}
```

The FluentValidation helpers moved as well: `ValidateWithResult` and `ValidateWithResultAsync` are
now in `Arc4u.FluentValidation` (namespace `Arc4u.Validation`), next to the rules `IsInsert`,
`IsUpdate`, `IsDelete`, `IsNone`, `IsDateOnly` and `IsUtcDateTime`, and `ToFluentResultErrors` is
renamed `ToResultErrors`.

## Other API changes

**Before (8.x)**

```csharp
using Arc4u.Authorization;
using Arc4u.OAuth2.Middleware;
using Arc4u.Security.Principal;

public static class Security
{
    public static void Register(WebApplicationBuilder builder, Operation[] operations)
    {
        builder.Services.AddScopedOperationsPolicy(["Admin"], operations,
                                                   options => options.InvokeHandlersAfterFailure = false);
    }

    public static void Use(WebApplication app)
    {
        app.UseForceOfOpenId(options => options.ForceAuthenticationForPaths.Add("/swagger*"));
    }
}
```

**After (9)**

```csharp
using Arc4u.Authorization;
using Arc4u.OAuth2.Middleware;
using Arc4u.Security.Principal;

public static class Security
{
    public static void Register(WebApplicationBuilder builder, Operation[] operations)
    {
        builder.Services.AddScopedOperationsPolicy(["Admin"], operations)
                        .ConfigureAuthorization(options => options.InvokeHandlersAfterFailure = false);

        builder.Services.AddForceOpenId(options => options.ForceAuthenticationForPaths.Add("/swagger*"));
    }

    public static void Use(WebApplication app)
    {
        app.UseForceOpenId();
    }
}
```

| 8.x | Arc4u 9 |
|---|---|
| `app.UseForceOfOpenId(options)` | `services.AddForceOpenId(options)` (or `AddForceOpenId(configuration)`, section `Authentication:ClaimsMiddleWare:ForceOpenId`) and `app.UseForceOpenId()` |
| `AddScopedOperationsPolicy(scopes, operations, authorizationOptions)` | `AddScopedOperationsPolicy(scopes, operations)` returns a `PoliciesBuilder`; call `ConfigureAuthorization(...)` on it |
| `ScopedOperationsRequirement`, `ScopedOperationsHandler` | `AllScopedOperationsRequirement`, `AllScopedOperationsHandler` (and `AnyScopedOperation…` for "any of") |
| `AuthorityOptions.SetData(url, tokenEndpoint, metadataAddress)` | `SetData(url, tokenEndpoint, issuer, metadataAddress)` |
| `Arc4u.Caching.SecureCache` | `Arc4u.Blazor.Caching.SecureCache` |
| `Arc4u.FluentValidation.DefaultValidatorExtensions` | `Arc4u.Validation.DefaultValidatorExtensions` |
| `Arc4u.OAuth2.Configuration.ClaimsIdentifierOption` | `Arc4u.OAuth2.Options.ClaimsIdentifierOption` |
| `Arc4u.OAuth2.Middleware.OpenIdBearerInjectorOptions`, `OpenIdBearerInjectorSettingsOptions` | Namespace `Arc4u.OAuth2.Options` |
| `Arc4u.OAuth2.AspNetCore.Options.ValidateResourceRightMiddlewareOptions` | `Arc4u.OAuth2.Options.ValidateResourceRightMiddlewareOptions` |
| `Arc4u.OAuth2.Security.Principal.AppServicePrincipalFactory` | `Arc4u.OAuth2.AppServicePrincipalFactory` |
| `Arc4u.Utils.Enum<T>`, `EnumUtil` | The generic methods of `System.Enum` (`Enum.GetValues<T>()`, `Enum.TryParse<T>()`) |

### Other removed APIs

| 8.x | Replacement |
|---|---|
| `Arc4u.Standard.OAuth2.AspNetCore.Api`: `ServiceAspectAttribute`, `OperationCheckAttribute` (already obsolete in 8.x) | ASP.NET Core authorization policies from `AddScopedOperationsPolicy` (`[Authorize("<operation>")]`), `ManageExceptionsFilter` and `SetCultureActionFilter` |
| `SecretBasicExtension.AddSecretAuthentication`, `CredentialSecretTokenProvider` | `AddClientTokens` (see [Client secrets replaced by client tokens](#client-secrets-replaced-by-client-tokens)) |
| `LoggerContext`, `CommonLoggerProperties`, `LoggerBase` | See [Fluent logging API](#fluent-logging-api) |
| `RealmLoggingDbCtx.CreateMapping()` (AutoMapper) | Removed with the AutoMapper dependency |
