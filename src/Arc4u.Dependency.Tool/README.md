# Arc4u.Dependency.Tool

Source generators that turn the `[Export]`, `[Shared]` and `[Scoped]` attributes of `Arc4u.Dependency` into `IServiceCollection` registrations at build time, with no reflection at startup.

## Install

```bash
dotnet add package Arc4u.Dependency --prerelease
dotnet add package Arc4u.Dependency.Tool --prerelease
```

## Usage

```csharp
using Arc4u.Dependency;

// Registers the [Export] classes of this project (assembly Contoso.Api).
builder.Services.RegisterApiTypes();
// Registers the types listed in Application.Dependency:RegisterTypes of Configs/appsettings.json.
builder.Services.RegisterTypes();
```

`RegisterTypes` is generated only when the project declares `<AdditionalFiles Include="Configs/appsettings.json" />`.

## Documentation

- Guide: [Dependency injection](https://arc4u-org.github.io/Arc4u/guides/dependency-injection/)
- Generated code: [Source generator reference](https://arc4u-org.github.io/Arc4u/guides/dependency-injection/source-generators.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
