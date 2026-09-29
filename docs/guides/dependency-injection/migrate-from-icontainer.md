---
description: "Replace the Arc4u 8.x IContainer, IContainerResolve and InitializeFromConfig with the generated registrations and .NET keyed services."
---
# Migrate from IContainer and reflection-based registration

Arc4u 8.x wrapped the .NET container in its own abstraction (`IContainer`, `IContainerRegistry`,
`IContainerResolve`, `DependencyContext`) and registered `[Export]` classes at startup by reflection
(`InitializeFromConfig`). Arc4u 9 removes that layer and relies on the keyed services added in
.NET 8, which replace resolution by name
([#140](https://github.com/Arc4u-org/Arc4u/issues/140)). This page shows how to move an application
to the source generators and keyed services. The full list of 8.x to 9 changes is in the
[migration guide](../../migration/8x-to-9.md#icontainer-replaced-by-keyed-services).

## What changed

| Arc4u 8.x | Arc4u 9 |
|---|---|
| `Arc4u.Standard.Dependency` | `Arc4u.Dependency`: the attributes and a few `IServiceProvider` and `IServiceCollection` helpers. |
| `Arc4u.Standard.Dependency.ComponentModel` (`ComponentModelContainer`) | Not shipped (see [Package support](../package-support.md)). Use `IServiceCollection` directly. |
| `InitializeFromConfig` scans the `Assemblies` and `RegisterTypes` of the `Application.Dependency` section at startup, with `Assembly.Load` and `Type.GetType`. | `Arc4u.Dependency.Tool` generates the registrations at build time: one `Register<Name>Types` method per project, and `RegisterTypes` for the types listed in `Configs/appsettings.json`. |
| `IContainerResolve.Resolve<T>(name)` | .NET keyed services: `[FromKeyedServices(name)]`, `GetKeyedService<T>(name)`. |
| `DependencyContext.Current` (static access to the container) | Constructor injection, or an injected `IServiceProvider`. |

The attributes did not change: `[Export]`, `[Shared]` and `[Scoped]` are still in the
`Arc4u.Dependency.Attribute` namespace and give the same lifetimes.

## Steps

1. Replace the package references: remove `Arc4u.Standard.Dependency` and
   `Arc4u.Standard.Dependency.ComponentModel`, then add `Arc4u.Dependency` and
   `Arc4u.Dependency.Tool` to every project that contains `[Export]` classes
   (see [Install](index.md#install)).
2. Replace the container setup in `Program.cs` with calls to the generated methods.

   **Before (8.x)**

   ```csharp
   using Arc4u.Dependency;
   using Arc4u.Dependency.ComponentModel;

   var builder = WebApplication.CreateBuilder(args);
   new ComponentModelContainer(builder.Services).InitializeFromConfig(builder.Configuration);
   ```

   **After (9)**

   ```csharp
   using Arc4u.Dependency;

   var builder = WebApplication.CreateBuilder(args);
   builder.Services.RegisterBusinessTypes(); // one call per project with [Export] classes
   builder.Services.RegisterApiTypes();
   builder.Services.RegisterTypes();         // types listed in Configs/appsettings.json
   ```

3. Move the `Application.Dependency` section:
   - Drop the `Assemblies` list. Each of your projects now registers its own classes through its
     `Register<Name>Types` method. Instead of `RejectedTypes`, remove `[Export]` from the classes
     you do not want registered.
   - Keep `RegisterTypes` for the types of other packages. Put the section in
     `Configs/appsettings.json` of the host project, declare that file as `AdditionalFiles`, and
     write each entry as a string without comments in the file (see
     [appsettings.json](index.md#appsettingsjson)).
4. Replace `IContainerResolve` with `IServiceProvider`, or better, inject the services themselves
   (see the table below).
5. Replace `IContainerRegistry` calls with the `IServiceCollection` methods of .NET (see the table
   below).

## Replace IContainerResolve

**Before (8.x)**

```csharp
public class CheckoutService
{
    private readonly IPaymentGateway _gateway;

    public CheckoutService(IContainerResolve container)
    {
        _gateway = container.Resolve<IPaymentGateway>("Stripe")!;
    }
}
```

**After (9)**

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

| `IContainerResolve` (8.x) | `IServiceProvider` (9) |
|---|---|
| `Resolve<T>()` | `GetService<T>()` |
| `Resolve<T>(name)` | `GetKeyedService<T>(name)` |
| `TryResolve<T>(out value)` | `TryGetService<T>(out value)` (Arc4u.Dependency) |
| `TryResolve<T>(name, out value)` | `TryGetService<T>(name, out value)` (Arc4u.Dependency) |
| `TryResolve(type, name, out value)` | `TryGetService(type, name, out value)` (Arc4u.Dependency) |
| `ResolveAll<T>()` | `GetServices<T>()` |
| `ResolveAll<T>(name)` | `GetKeyedServices<T>(name)` |
| `CreateScope()` | `CreateScope()`, then use `scope.ServiceProvider` |

The `TryGetService` methods are in the `Arc4u.Dependency` namespace
(<xref:Arc4u.Dependency.ServiceProviderExtensions>). Like `TryResolve`, they return `false` instead
of throwing, including when the service throws while it is created.

## Replace IContainerRegistry

| `IContainerRegistry` (8.x) | `IServiceCollection` (9) |
|---|---|
| `Register<TFrom, To>()` | `AddTransient<TFrom, To>()` |
| `RegisterScoped<TFrom, To>()` | `AddScoped<TFrom, To>()` |
| `RegisterSingleton<TFrom, To>()` | `AddSingleton<TFrom, To>()` |
| `Register<TFrom, To>(name)`, `RegisterScoped<TFrom, To>(name)`, `RegisterSingleton<TFrom, To>(name)` | `AddKeyedTransient<TFrom, To>(name)`, `AddKeyedScoped<TFrom, To>(name)`, `AddKeyedSingleton<TFrom, To>(name)` |
| `RegisterInstance<T>(instance)` | `AddSingleton<T>(instance)` |
| `RegisterInstance<T>(instance, name)` | `AddKeyedSingleton<T>(name, instance)` |
| `RegisterFactory<T>(factory)` | `AddTransient<T>(_ => factory())` |
| `RegisterScopedFactory<T>(factory)`, `RegisterSingletonFactory<T>(factory)` | `AddScoped<T>(_ => factory())`, `AddSingleton<T>(_ => factory())` |
| `CreateContainer()` | `builder.Build()` |

## Troubleshooting

### The type or namespace name 'IContainerResolve' could not be found

The type no longer exists. Inject the service itself, or `IServiceProvider`, as shown in
[Replace IContainerResolve](#replace-icontainerresolve).

### A service registered in 8.x is missing after the migration

- Its project does not reference `Arc4u.Dependency.Tool`, or `Program.cs` does not call the
  project's `Register<Name>Types` method.
- It came from the `Assemblies` list of `Application.Dependency`, which is no longer read.
- It was listed in `RegisterTypes` in the object form or with a `Version=`; see
  [Troubleshooting](index.md#troubleshooting) in the dependency injection guide.

## See also

- [Dependency injection](index.md)
- [Source generator reference](source-generators.md)
- [Migrate from Arc4u 8.x to 9](../../migration/8x-to-9.md#icontainer-replaced-by-keyed-services)
- [Dependency injection in .NET](https://learn.microsoft.com/dotnet/core/extensions/dependency-injection)
