# Arc4u.Caching.Dapr

The `Dapr` cache kind of Arc4u: a named cache stored in a Dapr state store.

## Install

```bash
dotnet add package Arc4u.Caching.Dapr --prerelease
```

## Usage

```csharp
builder.Services.AddILogger();
builder.Services.AddCacheContext(builder.Configuration);
builder.Services.AddKeyedTransient<ICache, DaprCache>(CacheContext.Dapr);
```

The `Caching` section declares the cache with `"Kind": "Dapr"` and `"Settings": { "Name": "<state store name>" }`. The
application needs a Dapr sidecar and a state store component with that name.

## Documentation

- Guide: [Dapr cache](https://arc4u-org.github.io/Arc4u/guides/caching/dapr.html)
- API reference: [Arc4u.Caching.Dapr](https://arc4u-org.github.io/Arc4u/api/Arc4u.Caching.Dapr.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
