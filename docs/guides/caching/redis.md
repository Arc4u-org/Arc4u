---
description: "Configure named caches stored in Redis, directly or through Redis Sentinel."
---
# Redis cache

The `Redis` and `RedisSentinel` kinds store the values in [Redis](https://redis.io/), so that every instance of a service
sees the same data. Use `Redis` for a single server or a managed service, and `RedisSentinel` when Redis Sentinel
provides the failover. This page extends the [caching guide](index.md).

## What it solves

Both classes (<xref:Arc4u.Caching.Redis.RedisCache> and <xref:Arc4u.Caching.Redis.RedisSentinelCache>) wrap the
`Microsoft.Extensions.Caching.StackExchangeRedis` distributed cache. Arc4u adds the named configuration and the
serialization of the values. The connection to Redis is opened on the first operation, not when the cache is initialized.

## Packages involved

| Package | Use it for |
|---|---|
| `Arc4u.Caching.Redis` | `RedisCache` (kind `Redis`) and `RedisSentinelCache` (kind `RedisSentinel`). |
| `Arc4u.Caching` | `ICacheContext` and `AddCacheContext`. |
| `Arc4u.Serializer.JSon` | The serializer (any `IObjectSerialization` works). |

## Install

```bash
dotnet add package Arc4u.Caching.Redis --prerelease
dotnet add package Arc4u.Serializer.JSon --prerelease
```

You need a reachable Redis server, or Sentinel nodes, at run time.

## Configuration

### Redis

```json
{
  "Caching": {
    "Default": "Shared",
    "Caches": [
      {
        "Name": "Shared",
        "Kind": "Redis",
        "IsAutoStart": true,
        "Settings": {
          "ConnectionString": "localhost:6379",
          "InstanceName": "MyApp-"
        }
      }
    ]
  }
}
```

The settings bind to <xref:Arc4u.Configuration.Redis.RedisCacheOption>:

| Key | Type | Default | Description |
|---|---|---|---|
| `Caching:Caches:n:Settings:ConnectionString` | `string` | none (required) | A [StackExchange.Redis configuration string](https://stackexchange.github.io/StackExchange.Redis/Configuration.html). |
| `Caching:Caches:n:Settings:InstanceName` | `string` | `Default` | Prefix added to every key. Use a distinct value per application when they share a server. |
| `Caching:Caches:n:Settings:SerializerName` | `string` | none | Key of the `IObjectSerialization` to use. When empty or not registered, the unkeyed one is used. |

`AddCacheContext` throws an `ArgumentNullException` for `ConnectionString` at startup when it is missing.

### Redis Sentinel

```json
{
  "Caching": {
    "Default": "HighAvailability",
    "Caches": [
      {
        "Name": "HighAvailability",
        "Kind": "RedisSentinel",
        "IsAutoStart": true,
        "Settings": {
          "InstanceName": "MyApp-",
          "MasterName": "mymaster",
          "SentinelEndpoints": [ "sentinel-0:26379", "sentinel-1:26379", "sentinel-2:26379" ],
          "RedisPassword": "<password>",
          "DefaultDatabase": 1
        }
      }
    ]
  }
}
```

The settings bind to <xref:Arc4u.Configuration.Redis.RedisSentinelCacheOption>:

| Key | Type | Default | Description |
|---|---|---|---|
| `Caching:Caches:n:Settings:InstanceName` | `string` | empty (required) | Prefix added to every key. |
| `Caching:Caches:n:Settings:MasterName` | `string` | `mymaster` | Name of the monitored master, as declared in the Sentinel configuration. |
| `Caching:Caches:n:Settings:SentinelEndpoints` | `string[]` | empty (at least one required) | Sentinel nodes, as `host:port`. |
| `Caching:Caches:n:Settings:RedisPassword` | `string` | none | Password used to connect to Redis. |
| `Caching:Caches:n:Settings:DefaultDatabase` | `int?` | `0` | Redis database number. |
| `Caching:Caches:n:Settings:SerializerName` | `string` | none | Key of the `IObjectSerialization` to use. |

`AddCacheContext` throws an `ArgumentException` when `InstanceName` is empty and an `ArgumentNullException` when
`SentinelEndpoints` is empty. The Sentinel cache does not connect at initialization: it lets StackExchange.Redis discover the master
from `MasterName` and the endpoints, and connects on the first operation.

> [!CAUTION]
> Do not commit the password. Read it from user secrets, an environment variable
> (`Caching__Caches__0__Settings__RedisPassword`) or a secret store.

### Code

```csharp
// Program.cs
using Arc4u.Caching;
using Arc4u.Caching.Redis;
using Arc4u.Dependency;
using Arc4u.Serializer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddILogger();
builder.Services.AddCacheContext(builder.Configuration);
builder.Services.AddSingleton<IObjectSerialization, JsonSerialization>();

// Register the class of the kind you use.
builder.Services.AddKeyedTransient<ICache, RedisCache>(CacheContext.Redis);
builder.Services.AddKeyedTransient<ICache, RedisSentinelCache>(CacheContext.RedisSentinel);

var app = builder.Build();
app.Run();
```

## Common scenarios

### Share a cache between two services

Point both services to the same server and use the same `InstanceName` and the same serializer. Values written by one are
read by the other, so both must be able to deserialize the same type. Use a different `InstanceName` to isolate two applications
that share a Redis server.

### Combine a memory cache and a Redis cache

Declare both and pick by name: keep hot, per-instance data in a `Memory` cache and data shared across instances in
Redis. See [Use several caches](index.md#use-several-caches).

## Extensibility points

The classes are registered under the keys `Redis` and `RedisSentinel`. Register another `ICache` under one of these keys
to replace it. To tune StackExchange.Redis beyond the options above, register your own `ICache` (for example one that derives
from <xref:Arc4u.Caching.BaseDistributeCache`1>).

## Troubleshooting

### `ArgumentNullException` or `ArgumentException` at startup

A required setting is missing: `ConnectionString` for `Redis`; `InstanceName`, `SentinelEndpoints` for `RedisSentinel`. Check the
index of the cache in `Caching:Caches`. A cache with no `Settings` section behaves as if every setting was missing.

### Operations fail or time out

The initialization does not contact Redis, so a wrong host appears on the first operation. `Get` wraps the error in
`DataCacheException`; `TryGetValue` and `Remove` return `false` instead of throwing.

## See also

- [Caching](index.md)
- [Serialization](serialization.md)
- <xref:Arc4u.Caching.Redis> in the API reference
