---
description: "Configure a named cache stored in the memory of the process with the Memory kind."
---
# Memory cache

The `Memory` kind stores the values in the memory of the current process. Use it for data that is cheap to rebuild and
that does not need to be shared between instances of a service, and as the default cache on a developer machine.
It is one of the providers of the [caching guide](index.md); read that page first for the `Caching` section and the
[named cache](../../concepts/glossary.md#named-cache) concept.

## What it solves

`MemoryCache` (<xref:Arc4u.Caching.Memory.MemoryCache>) wraps the `MemoryDistributedCache` of
`Microsoft.Extensions.Caching.Memory`. Like the other providers, it serializes the values, so a cached object is a copy:
changing the object after `Put` does not change what `Get` returns, and the size limit counts serialized bytes.

## Packages involved

| Package | Use it for |
|---|---|
| `Arc4u.Caching.Memory` | The `MemoryCache` class. |
| `Arc4u.Caching` | `ICacheContext`, `AddCacheContext`, and `AddMemoryCache` with `MemoryCacheOption` (namespace `Arc4u.Configuration.Memory`). |
| `Arc4u.Serializer.JSon` | The serializer (any `IObjectSerialization` works). |

## Install

```bash
dotnet add package Arc4u.Caching.Memory --prerelease
dotnet add package Arc4u.Serializer.JSon --prerelease
```

`Arc4u.Caching.Memory` brings `Arc4u.Caching`.

## Configuration

### appsettings.json

```json
{
  "Caching": {
    "Default": "Volatile",
    "Caches": [
      {
        "Name": "Volatile",
        "Kind": "Memory",
        "IsAutoStart": true,
        "Settings": {
          "SizeLimitInMB": 50,
          "CompactionPercentage": 0.2
        }
      }
    ]
  }
}
```

The settings bind to <xref:Arc4u.Configuration.Memory.MemoryCacheOption>:

| Key | Type | Default | Description |
|---|---|---|---|
| `Caching:Caches:n:Settings:SizeLimitInMB` | `long` | `100` | Maximum size of the cache, in megabytes, of serialized data. |
| `Caching:Caches:n:Settings:CompactionPercentage` | `double` | `0.2` | Fraction of the entries removed when the limit is reached. |
| `Caching:Caches:n:Settings:SerializerName` | `string` | none | Key of the `IObjectSerialization` to use. When empty or not registered, the unkeyed `IObjectSerialization` is used. |

> [!NOTE]
> `MemoryCacheOption.SizeLimitInMB` keeps the value in megabytes, as configured. The limit given to the underlying
> cache is `MemoryCacheOption.SizeLimitInBytes` (`SizeLimitInMB` times 1,048,576). A `Memory` cache declared without
> a `Settings` section uses the default of 100 MB.

### Code

```csharp
// Program.cs
using Arc4u.Caching;
using Arc4u.Caching.Memory;
using Arc4u.Dependency;
using Arc4u.Serializer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddILogger();
builder.Services.AddCacheContext(builder.Configuration);
builder.Services.AddSingleton<IObjectSerialization, JsonSerialization>();
builder.Services.AddKeyedTransient<ICache, MemoryCache>(CacheContext.Memory);

var app = builder.Build();

var cache = app.Services.GetRequiredService<ICacheContext>().Default;
cache.Put("greeting", "hello");
Console.WriteLine(cache.Get<string>("greeting"));
```

## Common scenarios

### Declare several memory caches

Each name has its own store and limit. Give a small, short-lived cache its own name instead of sharing one limit:

```json
{
  "Caching": {
    "Default": "Volatile",
    "Caches": [
      { "Name": "Volatile", "Kind": "Memory", "IsAutoStart": true, "Settings": { "SizeLimitInMB": 100 } },
      { "Name": "Lookups", "Kind": "Memory", "IsAutoStart": true, "Settings": { "SizeLimitInMB": 10, "SerializerName": "gzip" } }
    ]
  }
}
```

`"SerializerName": "gzip"` needs a keyed serializer registered with that key; see [Serialization](serialization.md).

### Expire and evict

`Put` with a `TimeSpan` expires the value (absolute, or sliding with `isSlided: true`). When the size limit is reached,
the cache removes `CompactionPercentage` of its entries, and the entry being written when the limit is hit can be dropped too
(`Get` then returns `null`). Values are never shared with another process: after a restart, or
in a second instance of the service, the cache is empty.

## Extensibility points

`MemoryCache` is registered under the key `Memory`. Register your own `ICache` under that key to replace it, or under
another key for a new kind (see [Caching](index.md#extensibility-points)).

## Troubleshooting

### `Get` returns `null` right after `Put`

Check, in this order: the value fits in `SizeLimitInMB`,
and the timeout passed to `Put` is not already over.

### `CacheNotInitializedException` mentioning `IObjectSerialization`

No serializer could be resolved. Register `builder.Services.AddSingleton<IObjectSerialization, JsonSerialization>()`.

## See also

- [Caching](index.md)
- [Serialization](serialization.md)
- <xref:Arc4u.Caching.Memory> in the API reference
