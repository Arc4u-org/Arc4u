# Arc4u.Caching.Memory

The `Memory` cache kind of Arc4u: a named cache stored in the memory of the process.

## Install

```bash
dotnet add package Arc4u.Caching.Memory --prerelease
```

## Usage

```csharp
builder.Services.AddILogger();
builder.Services.AddCacheContext(builder.Configuration);
builder.Services.AddSingleton<IObjectSerialization, JsonSerialization>();
builder.Services.AddKeyedTransient<ICache, MemoryCache>(CacheContext.Memory);
```

The `Caching` section declares the cache with `"Kind": "Memory"` and a `Settings` section with `SizeLimitInMB`. The
serializer comes from the `Arc4u.Serializer.JSon` package.

## Documentation

- Guide: [Memory cache](https://arc4u-org.github.io/Arc4u/guides/caching/memory.html)
- API reference: [Arc4u.Caching.Memory](https://arc4u-org.github.io/Arc4u/api/Arc4u.Caching.Memory.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
