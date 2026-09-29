---
description: "Register services with the [Export], [Shared] and [Scoped] attributes and the Arc4u source generators, including keyed services."
---
# Dependency injection

Arc4u uses the standard .NET dependency injection container (`IServiceCollection` and
`IServiceProvider`). The `Arc4u.Dependency` package adds attributes that declare a registration
on the class itself, and the `Arc4u.Dependency.Tool` source generators turn those attributes into
registration code when you build. Read this guide if you write the business services of an Arc4u
application, or if you register Arc4u implementations such as caches. The
[design principles](../../concepts/design-principles.md) explain why Arc4u builds on dependency
injection.

## What it solves

A business application has hundreds of services. Registering each one with `AddScoped` or
`AddSingleton` in `Program.cs` puts the lifetime far from the class, and the list is easy to get
out of sync. With Arc4u you mark the class with `[Export]` and, optionally, `[Shared]` or
`[Scoped]`. At build time a source generator writes an extension method on `IServiceCollection`
that registers every marked class of the project. A second generator registers types from other
assemblies, such as Arc4u's own cache implementations, from a list in a JSON file.

Because the registrations are generated when you compile:

- nothing scans assemblies or loads types by name at startup, which keeps the application
  compatible with trimming and Native AOT;
- a mistake such as a contract the class does not implement is a build error, not a runtime error.

The container, scopes, resolution and keyed services are the ones of .NET. Arc4u 9 has no container
abstraction of its own: `IContainer` and `IContainerResolve` were removed
([IContainer replaced by keyed services](../../migration/8x-to-9.md#icontainer-replaced-by-keyed-services)).

The diagram shows what each generator reads and the method it writes.

```mermaid
flowchart LR
    A["Classes marked [Export]"] --> G1["DependencyToolGenerator"]
    J["Configs/appsettings.json"] --> G2["GenerateRegisteredTypes"]
    G1 --> M1["RegisterApiTypes(), one method per project"]
    G2 --> M2["RegisterTypes()"]
    M1 --> S["IServiceCollection"]
    M2 --> S
```

## Packages involved

| Package | Use it for |
|---|---|
| `Arc4u.Dependency` | The `[Export]`, `[Shared]` and `[Scoped]` attributes, and `TryGetService` helpers on `IServiceProvider`. |
| `Arc4u.Dependency.Tool` | The two source generators. A development dependency: it runs inside the compiler, and your code never calls it. |

Reference both packages in every project that contains `[Export]` classes. On `develop/9.0.0`,
`Arc4u.Dependency` targets `netstandard2.0`, `net10.0` and `net11.0` (the published
`9.0.0-preview37` targets `netstandard2.0`, `net8.0`, `net9.0` and `net10.0`). Its
`netstandard2.0` build contains only the attributes. On the other target frameworks it references
`Microsoft.Extensions.Hosting`, which brings the `Microsoft.Extensions.DependencyInjection` API that
the generated code calls.

`Arc4u.Dependency.ComponentModel`, which held the `IContainer` implementation, is not built or
shipped in Arc4u 9. See [Package support](../package-support.md).

## Install

```bash
dotnet add package Arc4u.Dependency --prerelease
dotnet add package Arc4u.Dependency.Tool --prerelease
```

Because `Arc4u.Dependency.Tool` is a development dependency, `dotnet add package` writes its
reference with `PrivateAssets` set to `all`, so it does not flow to the projects that reference
yours:

```xml
<PackageReference Include="Arc4u.Dependency.Tool" Version="<version>">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
```

## Configuration

Registering the classes of your own project needs no configuration. The `Application.Dependency`
section is only read by the second generator, to register types that live in other assemblies
(see [Register Arc4u implementations from other packages](#register-arc4u-implementations-from-other-packages)).

### Mark your classes

Put `[Export]` on each class to register. The contract type is the service type; without it the
class is registered as itself. Add `[Shared]` for a singleton or `[Scoped]` for a scoped service;
without either, the service is transient.

```csharp
// OrderService.cs, in the project Contoso.Api
using Arc4u.Dependency.Attribute;

namespace Contoso.Api;

public interface IClock
{
    DateTime UtcNow { get; }
}

public interface IOrderService
{
    Task<int> CountAsync();
}

[Export(typeof(IClock)), Shared]
public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}

[Export(typeof(IOrderService)), Scoped]
public class OrderService(IClock clock) : IOrderService
{
    public Task<int> CountAsync() => Task.FromResult(clock.UtcNow.Day);
}
```

### Call the generated method

The generator adds a method named `Register<Name>Types`, where `<Name>` is the last segment of the
assembly name: `Contoso.Api` gives `RegisterApiTypes`. The method is in the `Arc4u.Dependency`
namespace.

```csharp
// Program.cs
using Arc4u.Dependency;
using Contoso.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterApiTypes();

var app = builder.Build();
app.MapGet("/orders/count", async (IOrderService orders) => await orders.CountAsync());
app.Run();
```

`RegisterApiTypes` contains:

```csharp
services.AddSingleton<Contoso.Api.IClock, global::Contoso.Api.SystemClock>();
services.AddScoped<Contoso.Api.IOrderService, global::Contoso.Api.OrderService>();
```

### appsettings.json

The second generator reads the `Application.Dependency` section of the file
`Configs/appsettings.json` of your project, at build time, and writes a method named
`RegisterTypes`. The file must be declared as an additional file of the compilation:

```xml
<ItemGroup>
  <AdditionalFiles Include="Configs/appsettings.json" />
</ItemGroup>
```

```json
{
  "Application.Dependency": {
    "RegisterTypes": [
      "Arc4u.Caching.Memory.MemoryCache, Arc4u.Caching.Memory",
      "Arc4u.AppSettings, Arc4u.Configuration"
    ]
  }
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `Application.Dependency:RegisterTypes` | array of strings | empty | Types to register, each written `Namespace.Type, AssemblyName`. The assembly must be referenced by the project and the type must carry `[Export]`; entries that do not match are skipped. |

The file is parsed with strict JSON rules: no comments and no trailing commas. Key names are
case-sensitive. The 8.x keys `Assemblies` and `RejectedTypes` are no longer read. The
[source generator reference](source-generators.md#generateregisteredtypes) lists every matching rule.

## Common scenarios

### Choose a lifetime

| Attributes on the class | Generated registration |
|---|---|
| `[Export]` | `services.AddTransient<TClass>()` |
| `[Export(typeof(IService))]` | `services.AddTransient<IService, TClass>()` |
| `[Export(typeof(IService)), Shared]` | `services.AddSingleton<IService, TClass>()` |
| `[Export(typeof(IService)), Scoped]` | `services.AddScoped<IService, TClass>()` |
| `[Export("key", typeof(IService))]` | `services.AddKeyedTransient<IService, TClass>("key")` |
| `[Export("key")]` | `services.AddKeyedTransient<TClass>("key")` |

`[Shared]` and `[Scoped]` combine with every form of `[Export]` (for example
`[Export("key", typeof(IService)), Scoped]` gives `AddKeyedScoped`). Use one of them, not both.

### Register and resolve keyed services

Give the export a name to register several implementations of the same contract. The name becomes
the service key of a .NET keyed service.

```csharp
using Arc4u.Dependency.Attribute;

namespace Contoso.Api;

public interface IPaymentGateway
{
    string Name { get; }
}

[Export("Stripe", typeof(IPaymentGateway)), Shared]
public class StripeGateway : IPaymentGateway
{
    public string Name => "Stripe";
}

[Export("Adyen", typeof(IPaymentGateway)), Shared]
public class AdyenGateway : IPaymentGateway
{
    public string Name => "Adyen";
}
```

Inject a known key with <xref:Microsoft.Extensions.DependencyInjection.FromKeyedServicesAttribute>:

```csharp
using Arc4u.Dependency.Attribute;
using Microsoft.Extensions.DependencyInjection;

namespace Contoso.Api;

[Export, Scoped]
public class CheckoutService([FromKeyedServices("Stripe")] IPaymentGateway gateway)
{
    public string Pay() => gateway.Name;
}
```

When the key is only known at run time (for example read from a database), resolve it from
`IServiceProvider`. `GetRequiredKeyedService` throws when the key is not registered;
<xref:Arc4u.Dependency.ServiceProviderExtensions.TryGetService*> returns `false` instead.

```csharp
using Arc4u.Dependency;
using Microsoft.Extensions.DependencyInjection;

namespace Contoso.Api;

public class PaymentRouter(IServiceProvider services)
{
    public string Pay(string provider) =>
        services.TryGetService<IPaymentGateway>(provider, out var gateway)
            ? gateway!.Name
            : throw new ArgumentException($"Unknown payment provider {provider}.");
}
```

A keyed registration is only returned for its key: `GetService<IPaymentGateway>()` without a key
does not find `StripeGateway`.

> [!NOTE]
> `TryGetService` returns `false` for any exception raised while the service is created, not only
> for a missing registration. Use `GetRequiredKeyedService` when you want to see why a service
> cannot be built.

### Register Arc4u implementations from other packages

Some Arc4u implementations are registered only through their `[Export]` attribute. For example,
no `Add...` method registers `Arc4u.Caching.Memory.MemoryCache`: it is exported as
`[Export("Memory", typeof(ICache))]`, and the cache context resolves the `ICache` keyed by the
`Kind` of each configured cache (see [Caching](../caching/index.md)). List such types in `Configs/appsettings.json` (see
[appsettings.json](#appsettingsjson)) and call `RegisterTypes`:

```csharp
// Program.cs
using Arc4u.Dependency;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterApiTypes();
builder.Services.RegisterTypes();

var app = builder.Build();
app.Run();
```

For the two entries of the sample file, `RegisterTypes` contains:

```csharp
// Types registered by nuget package: Arc4u.Caching.Memory
services.AddKeyedTransient<Arc4u.Caching.ICache, Arc4u.Caching.Memory.MemoryCache>("Memory");
// Types registered by nuget package: Arc4u.Configuration
services.AddSingleton<Arc4u.IAppSettings, Arc4u.AppSettings>();
```

The lifetime and key come from the attributes compiled into the referenced assembly, exactly as for
your own classes. Any public, non-nested class with `[Export]` in a referenced assembly can be
listed.

### Register the services of several projects

Each project that references `Arc4u.Dependency.Tool` gets its own method, named after its assembly.
The host calls all of them:

```csharp
// Program.cs of Contoso.Api, which references Contoso.Billing.Business and Contoso.Billing.Data
using Arc4u.Dependency;

// ...
builder.Services.RegisterBusinessTypes();
builder.Services.RegisterDataTypes();
builder.Services.RegisterApiTypes();
builder.Services.RegisterTypes();
```

- Give each project a different last segment in its assembly name. Two referenced projects named
  `Contoso.Billing.Business` and `Contoso.Shipping.Business` both produce `RegisterBusinessTypes`,
  and the call is ambiguous (error `CS0121`).
- Declare `Configs/appsettings.json` as an additional file in one project only, usually the host,
  for the same reason: each such project produces a `RegisterTypes` method.
- The generated methods use `Add...`, not `TryAdd...`. A class registered by its own project and
  also listed in `RegisterTypes` is registered twice.

### Register types in a Blazor WebAssembly application

A Blazor WebAssembly project keeps its settings in `wwwroot/appsettings.json`. The generator also
reads that file and writes a method named `RegisterWwwTypes`:

```xml
<ItemGroup>
  <AdditionalFiles Include="wwwroot/appsettings.json" />
</ItemGroup>
```

```csharp
// Program.cs
using Arc4u.Dependency;

// ...
builder.Services.RegisterWwwTypes();
```

The section and the entry format are the same as for `Configs/appsettings.json`. A project can
declare both files; it then gets both methods.

### Inspect the generated code

Set `EmitCompilerGeneratedFiles` in the project file and build:

```xml
<PropertyGroup>
  <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
</PropertyGroup>
```

The files are written under `obj/<Configuration>/<TargetFramework>/generated/Arc4u.Dependency.Tool/`.
Set `CompilerGeneratedFilesOutputPath` to write them elsewhere. The
[source generator reference](source-generators.md) describes every file.

### Publish with trimming or Native AOT

The generated methods only contain generic `Add...` and `AddKeyed...` calls. Nothing is loaded by
name or discovered by reflection at startup, so the registrations need no trimming annotations. A
console application registered this way publishes with `PublishAot=true` without trimming or AOT
warnings from the generated code. Whether the rest of your application is AOT-compatible depends on
the other packages it uses: publish with `PublishAot=true` and read the `IL2xxx` and `IL3xxx`
warnings.

### Register what the attributes cannot express

The attributes cover one class and one contract. For anything else, use the `IServiceCollection`
methods of .NET next to the generated call: open generic types, factories, existing instances and
`TryAdd...` registrations.

```csharp
using Arc4u.Dependency;
using Contoso.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.RegisterApiTypes();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();
app.Run();
```

## Extensibility points

`Arc4u.Dependency` registers no service of its own. The generated methods are ordinary extension
methods, so you control what they register:

- To replace a service registered by a generated method, register your implementation after the
  call. When a service type has several registrations, the container returns the last one.
- To leave a class out, remove its `[Export]` attribute (or its entry in `RegisterTypes`).

```csharp
// Program.cs
using Arc4u.Dependency;
using Contoso.Api;

// ...
builder.Services.RegisterApiTypes();
builder.Services.AddSingleton<IClock, FixedClock>(); // replaces SystemClock for IClock
```

To find out what is registered, <xref:Arc4u.Dependency.ServiceCollectionExtension.GetImplementationType*>
returns the implementation type of the first registration whose service type is `T` (or whose
implementation derives from the class `T`), or `null`. It also returns `null` when that
registration is keyed, or uses a factory or an instance, because such registrations have no
implementation type.

## Troubleshooting

### 'IServiceCollection' does not contain a definition for 'RegisterTypes'

The `GenerateRegisteredTypes` generator did not find the file. Check that:

- the file is at `Configs/appsettings.json` (or `wwwroot/appsettings.json` for `RegisterWwwTypes`)
  relative to the project folder; an `appsettings.json` at the root of the project is not read;
- the file is declared with `<AdditionalFiles Include="Configs/appsettings.json" />`;
- at least one `.cs` file sits directly in the project folder (usually `Program.cs`). The generator
  locates `Configs` relative to the shortest folder path that contains source files;
- the build output has no `CS8785` warning (next section).

The same error for `Register<Name>Types` means that the file lacks `using Arc4u.Dependency;`, that
the project does not reference `Arc4u.Dependency.Tool`, or that the name does not match the last
segment of the assembly name.

### CS8785: Generator 'GenerateRegisteredTypes' failed to generate source

The file cannot be read as the expected JSON. The message says why:

- `JsonReaderException ... '/' is invalid after a value`: the file contains comments. Remove them.
- `JsonReaderException ... The JSON array contains a trailing comma at the end which is not
  supported in this mode`: the file contains a trailing comma. Remove it.
- `JsonException: The JSON value could not be converted to System.String. Path: $.RegisterTypes[0]`:
  the entries use the old object form (`{ "Type": "..." }`). Write each entry as a string.

### A type listed in RegisterTypes is not registered

The generator skips an entry without warning when:

- the assembly name after the comma does not match a referenced assembly file (`<Name>.dll`);
- the entry contains a `Version=` part that does not appear in the path of the referenced assembly.
  A four-part version such as `Version=9.0.0.0` never matches a NuGet folder such as
  `9.0.0-preview37`. Leave `Version=` out;
- the assembly name is an 8.x name such as `Arc4u.Standard.Configuration` (see
  [Package renames](../../migration/8x-to-9.md#package-renames));
- the type name is misspelled, the type is nested in another type, or it has no `[Export]`
  attribute.

Inspect the generated `GeneratedTypes.g.cs` file (see
[Inspect the generated code](#inspect-the-generated-code)).

### A class marked [Export] is not registered

> [!WARNING]
> Known issues of the `DependencyToolGenerator` on `develop/9.0.0`:
>
> - The attribute is recognized by its exact spelling `[Export(...)]`. A class marked
>   `[ExportAttribute(...)]` or `[Arc4u.Dependency.Attribute.Export(...)]` is silently skipped.
> - `[Export]` accepts several instances on a class, but only the first one is registered.
> - `[Export]` on a `record` is ignored.
> - A class with both `[Shared]` and `[Scoped]` is registered as scoped, but the same class listed
>   in `RegisterTypes` is registered as a singleton.

Write the attribute as `[Export(...)]` with `using Arc4u.Dependency.Attribute;`, and register the
other contracts of a multi-contract class yourself.

### Build errors in Dependencies.g.cs for a generic class

The generator does not support open generic types: `[Export(typeof(IRepository<>))]` on
`Repository<T>` produces code that does not compile (`CS7003`, `CS0246`). Remove the attribute and
register the type with `services.AddScoped(typeof(IRepository<>), typeof(Repository<>))`. Closed
generic contracts such as `[Export(typeof(IRepository<Order>))]` work.

### CS0121: The call is ambiguous between the following methods or properties

Two referenced projects generate a method with the same name. See
[Register the services of several projects](#register-the-services-of-several-projects).

### InvalidOperationException: No service for type 'X' has been registered

- The generated method that registers the service is not called in `Program.cs`.
- The service is registered with a key (`[Export("key", typeof(X))]`) and resolved without one.
  Resolve it with `[FromKeyedServices("key")]` or `GetRequiredKeyedService<X>("key")`. For a key
  that is not registered, the message is `No keyed service for type 'X' using key type
  'System.String' has been registered.`

## See also

- [Source generator reference](source-generators.md)
- [Migrate from IContainer and reflection-based registration](migrate-from-icontainer.md)
- [Migrate from Arc4u 8.x to 9](../../migration/8x-to-9.md#icontainer-replaced-by-keyed-services)
- [Caching](../caching/index.md)
- [Design principles](../../concepts/design-principles.md)
- [Dependency injection in .NET](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
- <xref:Arc4u.Dependency.Attribute> and <xref:Arc4u.Dependency> in the API reference
