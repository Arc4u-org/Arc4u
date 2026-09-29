# Arc4u.Caching

Named caches declared in configuration and used through one interface, `ICache`, whatever the store behind them.
Add a provider package for the store (memory, Redis, SQL Server or Dapr).

## Install

```bash
dotnet add package Arc4u.Caching --prerelease
```

## Usage

```csharp
// Reads the "Caching" section of the configuration.
builder.Services.AddILogger();
builder.Services.AddCacheContext(builder.Configuration);

// Later, with an injected ICacheContext:
await cacheContext.Default.PutAsync("greeting", TimeSpan.FromMinutes(5), "hello");
var greeting = await cacheContext.Default.GetAsync<string>("greeting");
```

The providers and a serializer must also be registered: see the guide.

## Documentation

- Guide: [Caching](https://arc4u-org.github.io/Arc4u/guides/caching/)
- API reference: [Arc4u.Caching](https://arc4u-org.github.io/Arc4u/api/Arc4u.Caching.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
