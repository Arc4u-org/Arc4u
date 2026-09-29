# Arc4u.Dependency

Attributes (`[Export]`, `[Shared]`, `[Scoped]`) that declare how a class is registered in the .NET dependency injection container, and `TryGetService` helpers on `IServiceProvider`.

## Install

```bash
dotnet add package Arc4u.Dependency --prerelease
dotnet add package Arc4u.Dependency.Tool --prerelease
```

## Usage

```csharp
// Program.cs of the project Contoso.Api
using Arc4u.Dependency;           // generated RegisterApiTypes
using Arc4u.Dependency.Attribute; // Export, Shared, Scoped

var builder = WebApplication.CreateBuilder(args);
builder.Services.RegisterApiTypes();

[Export(typeof(IClock)), Shared] // singleton; [Scoped] for scoped, neither for transient
public class SystemClock : IClock { public DateTime UtcNow => DateTime.UtcNow; }
```

`RegisterApiTypes` is generated at build time by `Arc4u.Dependency.Tool` from the `[Export]` classes of the project; its name comes from the last segment of the assembly name.

## Documentation

- Guide: [Dependency injection](https://arc4u-org.github.io/Arc4u/guides/dependency-injection/)
- API reference: [Arc4u.Dependency](https://arc4u-org.github.io/Arc4u/api/Arc4u.Dependency.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
