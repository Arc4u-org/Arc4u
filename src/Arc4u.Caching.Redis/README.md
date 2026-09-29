# Arc4u.Caching.Redis

The `Redis` and `RedisSentinel` cache kinds of Arc4u: named caches stored in Redis, shared between instances of a service.

## Install

```bash
dotnet add package Arc4u.Caching.Redis --prerelease
```

## Usage

```csharp
builder.Services.AddCacheContext(builder.Configuration);
builder.Services.AddSingleton<IObjectSerialization, JsonSerialization>();
builder.Services.AddKeyedTransient<ICache, RedisCache>(CacheContext.Redis);
builder.Services.AddKeyedTransient<ICache, RedisSentinelCache>(CacheContext.RedisSentinel);
```

The `Caching` section declares the cache with `"Kind": "Redis"` (settings `ConnectionString`, `InstanceName`) or
`"Kind": "RedisSentinel"` (settings `MasterName`, `SentinelEndpoints`, `InstanceName`).

## Documentation

- Guide: [Caching](https://arc4u-org.github.io/Arc4u/guides/caching/redis.html)
- API reference: [Arc4u.Caching.Redis](https://arc4u-org.github.io/Arc4u/api/Arc4u.Caching.Redis.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
