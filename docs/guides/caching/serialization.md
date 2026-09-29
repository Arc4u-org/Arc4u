---
description: "Choose and register the System.Text.Json serializer, plain or compressed, that the Arc4u caches use to store values."
---
# Serialization

The Memory, Redis and SQL Server caches store `byte[]`. <xref:Arc4u.Serializer.IObjectSerialization> converts the
objects you put in a cache to bytes and back. Since Arc4u 9 the only format shipped is JSON with System.Text.Json, plain
or compressed. This page extends the [caching guide](index.md).

## What it solves

You call `cache.Put("key", order)` and `cache.Get<Order>("key")`; the cache serializes for you, even the memory cache.
Arc4u keeps the format behind a small interface, so you can:

- keep the default JSON, or set your own `JsonSerializerOptions`;
- compress large values (GZip, Deflate, Brotli or Zip);
- use a source-generated `JsonSerializerContext`, which does not need reflection (for trimming and Native AOT);
- give each cache its own serializer.

The Dapr cache does not use this interface: the Dapr client serializes the values itself.

## Packages involved

| Package | Use it for |
|---|---|
| `Arc4u.Serializer` | The `IObjectSerialization` interface. No dependency. |
| `Arc4u.Serializer.JSon` | `JsonSerialization` and the compressed variants. Uses System.Text.Json and `Microsoft.IO.RecyclableMemoryStream`. |

## Install

```bash
dotnet add package Arc4u.Serializer.JSon --prerelease
```

## Configuration

The serializers have no configuration section. You register them in the service collection, and a cache selects one with
the `SerializerName` setting of its `Settings`.

### The interface

<xref:Arc4u.Serializer.IObjectSerialization> has three members:

| Member | Description |
|---|---|
| `byte[] Serialize<T>(T value)` | Converts the value to bytes. |
| `T? Deserialize<T>(byte[] data)` | Converts the bytes to a `T`. |
| `object? Deserialize(byte[] data, Type objectType)` | Same, when the type is known at run time only. |

### The implementations

All are in the `Arc4u.Serializer` namespace.

| Class | Format |
|---|---|
| <xref:Arc4u.Serializer.JsonSerialization> | UTF-8 JSON. |
| <xref:Arc4u.Serializer.JsonGZipSerialization> | JSON compressed with GZip (fastest level). |
| <xref:Arc4u.Serializer.JsonDeflateSerialization> | JSON compressed with Deflate (fastest level). |
| <xref:Arc4u.Serializer.JsonBrotliSerialization> | JSON compressed with Brotli (fastest level). |
| <xref:Arc4u.Serializer.JsonZipSerialization> | A Zip archive with one entry, `content`, that holds the JSON. |

Each class has three constructors:

| Constructor | Use it for |
|---|---|
| `()` | The default `JsonSerializerOptions` of System.Text.Json. Reflection based. |
| `(JsonSerializerOptions options)` | Your own options (naming policy, converters). Reflection based. |
| `(JsonSerializerContext context)` | A source-generated context. No reflection: use it when you trim or publish with Native AOT. |

The first two are marked `RequiresUnreferencedCode` and show trimming warnings in a trimmed or AOT project.

### Code

Register one unkeyed serializer, which every cache uses unless it names another one, and optional keyed serializers:

```csharp
// Program.cs
using System.Text.Json;
using Arc4u.Serializer;

var builder = WebApplication.CreateBuilder(args);

// Used by the caches without a SerializerName.
builder.Services.AddSingleton<IObjectSerialization>(
    _ => new JsonSerialization(new JsonSerializerOptions(JsonSerializerDefaults.Web)));

// Used by the caches with "SerializerName": "gzip".
builder.Services.AddKeyedSingleton<IObjectSerialization>("gzip", (_, _) => new JsonGZipSerialization());

var app = builder.Build();
app.Run();
```

Then select the keyed serializer in the configuration of a cache:

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
          "SerializerName": "gzip"
        }
      }
    ]
  }
}
```

A cache resolves its serializer when it is initialized: the keyed one named by `SerializerName`, otherwise the unkeyed one.
A `SerializerName` that is not registered falls back to the unkeyed serializer without an error. When neither exists, the
cache stays uninitialized and its operations throw `CacheNotInitializedException`.

## Common scenarios

### Use a source-generated context

```csharp
using System.Text.Json.Serialization;
using Arc4u.Serializer;

public record Product(string Id, string Name);

[JsonSerializable(typeof(Product))]
public partial class AppJsonContext : JsonSerializerContext
{
}

public static class AotSerializer
{
    public static IObjectSerialization Create() => new JsonSerialization(AppJsonContext.Default);
}
```

The context must list every type you put in the cache, otherwise System.Text.Json throws at run time.

### Serialize outside a cache

The serializers are ordinary objects:

```csharp
using Arc4u.Serializer;

public static class RoundTrip
{
    public static Product? Copy(Product product)
    {
        IObjectSerialization serializer = new JsonSerialization();
        byte[] bytes = serializer.Serialize(product);
        return serializer.Deserialize<Product>(bytes);
    }
}
```

## Extensibility points

| Service | Default implementation | Replace it to |
|---|---|---|
| <xref:Arc4u.Serializer.IObjectSerialization> | none: you register `JsonSerialization` or a variant | Use another format. Implement the three members and register the class, unkeyed or keyed. |

Data written with one serializer cannot be read with another: change the serializer of a shared cache (Redis, SQL Server)
only when the cache is empty or its entries can be lost. The serializers set the `SerializerType` tag on the current
`Activity`, so a trace shows which one ran.

## Troubleshooting

### Values come back as `null` or `Get` throws `DataCacheException` after a change of serializer

The bytes in the store were written by a different serializer or different options. Clear the cache or change the
`InstanceName`, `TableName` or the cache name.

### `NotSupportedException` or missing properties with a source-generated context

The type, or one of its members, is not registered in the `JsonSerializerContext`. Add a `[JsonSerializable]` attribute for it.

### Where are the Newtonsoft.Json, Protobuf and BSON serializers?

They were removed in Arc4u 9. See
[Newtonsoft.Json replaced by System.Text.Json](../../migration/8x-to-9.md#newtonsoftjson-replaced-by-systemtextjson) and
[ADAL and Protobuf removed](../../migration/8x-to-9.md#adal-and-protobuf-removed).

## See also

- [Caching](index.md)
- [Migrate from 8.x to 9](../../migration/8x-to-9.md)
- <xref:Arc4u.Serializer> in the API reference
