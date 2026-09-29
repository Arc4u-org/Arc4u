---
description: "Register a MongoDB database with Arc4u.MongoDB, map entity types to collections in a DbContext-like class and get typed collections from a singleton factory."
---
# MongoDB

`Arc4u.MongoDB` gives MongoDB the same shape as Entity Framework Core in the [data access layer](../../concepts/glossary.md#data-access-layer): one context class per database, in which you map entity types to collections, and one registration call. The queries themselves stay plain `MongoDB.Driver` code. The [overview](index.md) explains how it relates to the other data packages.

## What it solves

The MongoDB driver leaves you to create a `MongoClient`, keep it as a singleton, read the database name from somewhere and repeat collection names as strings across the code. `Arc4u.MongoDB`:

- reads the connection string from configuration and builds the `MongoClientSettings` from it;
- creates the client and database once, lazily, and shares them through a singleton `IMongoClientFactory<TContext>`;
- keeps the collection name of each entity type in one place, so that `GetCollection<Product>()` needs no string.

It does not wrap the driver: you get `IMongoClient` and `IMongoCollection<T>` back and use them as documented by MongoDB. It also does not use `Arc4u.Data` entities: any class the driver can serialize works.

## Install

```bash
dotnet add package Arc4u.MongoDB --prerelease
```

The package references `MongoDB.Driver`. It needs a reachable MongoDB server only when the first call reaches the database; registering, resolving the factory and getting a collection object do not fail without a server (the first call creates the `MongoClient`, which connects in the background).

## Configuration

`AddMongoDatabase<TContext>(configuration, connectionStringKey)` reads one entry of the standard `ConnectionStrings` section. The `connectionStringKey` parameter is the name of that entry.

```json
{
  "ConnectionStrings": {
    "Shop": "mongodb://mongo-0.example.com:27017,mongo-1.example.com:27017/shop?replicaSet=rs0&retryWrites=true"
  }
}
```

| Key | Type | Default | Description |
|---|---|---|---|
| `ConnectionStrings:<connectionStringKey>` | string | none (required) | A [MongoDB connection string](https://www.mongodb.com/docs/manual/reference/connection-string/). It must contain the database name after the hosts (`/shop` above): that name becomes `DbContext.DatabaseName`. |

As with any .NET configuration, an environment variable `ConnectionStrings__Shop` overrides the entry. If you do not want a connection string, use the [delegate overload](#configure-the-client-settings-in-code).

## Step-by-step setup

### Define the context

Derive from <xref:Arc4u.MongoDB.DbContext> and map entity types to collections in `OnConfiguring`. The class needs a public parameterless constructor.

```csharp
// ShopContext.cs
using Arc4u.MongoDB;
using Arc4u.MongoDB.Configuration;
using MongoDB.Bson.Serialization.Attributes;

public class Product
{
    [BsonId]
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public class ShopContext : DbContext
{
    protected override void OnConfiguring(DbContextBuilder context)
    {
        context.MapCollection("products").With<Product>();
    }
}
```

`MapCollection(name)` starts the mapping of a collection, and `With<TEntity>()` adds an entity type to it. Entities need no attribute or interface for Arc4u; `[BsonId]` is the MongoDB driver's convention for the identifier.

### Register it

```csharp
// Program.cs
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMongoDatabase<ShopContext>(builder.Configuration, "Shop");

var app = builder.Build();
app.Run();
```

<xref:Microsoft.Extensions.DependencyInjection.MongoDbConnection.AddMongoDatabase*> creates the context once, calls `OnConfiguring`, registers the context (`TContext`) and `IMongoClientFactory<TContext>` as singletons, and registers the `MongoClientSettings` as named options. The options name is the database name in lower case.

### Query

```csharp
// ProductStore.cs
using Arc4u.MongoDB;
using MongoDB.Driver;

public class ProductStore(IMongoClientFactory<ShopContext> factory)
{
    public Task InsertAsync(Product product, CancellationToken cancellationToken) =>
        factory.GetCollection<Product>().InsertOneAsync(product, cancellationToken: cancellationToken);

    public Task<List<Product>> GetAllAsync(CancellationToken cancellationToken) =>
        factory.GetCollection<Product>().Find(FilterDefinition<Product>.Empty).ToListAsync(cancellationToken);
}
```

Register `ProductStore` in dependency injection like any other class. `IMongoClientFactory<TContext>` has three methods:

| Method | Returns |
|---|---|
| `CreateClient()` | The `IMongoClient`. It is created on the first call and the same instance is returned afterwards. |
| `GetCollection<TEntity>()` | The collection the entity type is mapped to in the context. |
| `GetCollection<TEntity>(string collectionName)` | The collection with that name in the context's database, mapped or not. |

## Common scenarios

### Configure the client settings in code

Use the second overload when the settings do not come from a connection string, for example for a value you compute or a setting the connection string cannot carry:

```csharp
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

public static class MongoSamples
{
    public static void Register(IServiceCollection services)
    {
        services.AddMongoDatabase<ShopContext>("shop", settings =>
        {
            settings.Server = new MongoServerAddress("localhost", 27017);
            settings.DirectConnection = true;
        });
    }
}
```

The first argument is the database name; `DbContext.DatabaseName` keeps its casing, while the options are registered under its lower-case form.

> [!NOTE]
> The connection-string overload copies a fixed list of properties from the parsed `MongoClientSettings` (servers, credentials, TLS, timeouts, pool sizes, read and write concerns, `ReplicaSetName`, `RetryReads`, `RetryWrites` and a few more). Options of the connection string that map to other properties, such as `directConnection`, `compressors`, `loadBalanced` or `maxConnecting`, are parsed by the driver but not applied to the client. Set them through the delegate overload.

### Store several entity types in one collection

Call `With` once per type. Each type must be mapped once for a given collection:

```csharp
using Arc4u.MongoDB;
using Arc4u.MongoDB.Configuration;

public class Customer { public Guid Id { get; set; } }
public class VipCustomer : Customer { }

public class CrmContext : DbContext
{
    protected override void OnConfiguring(DbContextBuilder context)
    {
        context.MapCollection("customers")
               .With<Customer>()
               .With<VipCustomer>();
    }
}
```

One entity type per collection is the common case and the simplest to reason about.

### Map one entity type to several collections

Map the type in several `MapCollection` calls, then choose the collection by name:

```csharp
using Arc4u.MongoDB;
using Arc4u.MongoDB.Configuration;

public class Event { public Guid Id { get; set; } }

public class EventsContext : DbContext
{
    protected override void OnConfiguring(DbContextBuilder context)
    {
        context.MapCollection("events-2025").With<Event>();
        context.MapCollection("events-2026").With<Event>();
    }
}
```

`factory.GetCollection<Event>()` now throws `TypeMappedToMoreThanOneCollectionException<Event>`; use `factory.GetCollection<Event>("events-2026")`.

### Use several databases

Write one context per database and register each one. Each gets its own factory (`IMongoClientFactory<ShopContext>`, `IMongoClientFactory<CrmContext>`) and, through the database name in its connection string, its own client settings.

## Extensibility points

| Service | Default implementation | Replace it to |
|---|---|---|
| `IMongoClientFactory<TContext>` | <xref:Arc4u.MongoDB.DefaultMongoClientFactory`1> | Create the client differently (for example from a shared client). `AddMongoDatabase` registers the default with `TryAdd`, so a registration made before the call wins. |
| `MongoClientSettings` (named options) | Registered by `AddMongoDatabase` under the lower-case database name | Post-configure them with `services.Configure<MongoClientSettings>("shop", ...)`. |

## Troubleshooting

### `TypeNotMappedToCollectionException`

`GetCollection<T>()` was called for a type that no `MapCollection(...).With<T>()` declares. The exception carries no specific message. Add the mapping, or call `GetCollection<T>("name")`.

### `TypeMappedToMoreThanOneCollectionException`: "2 registration exist for MyApp.Event."

The type is mapped to several collections in the context, so the parameterless `GetCollection<T>()` cannot choose. Use `GetCollection<T>("name")`.

### `ArgumentException: An item with the same key has already been added. Key: MyApp.Product` when the application starts

The same entity type is mapped twice to the same collection in `OnConfiguring`. It is thrown by `With<TEntity>()` during `AddMongoDatabase`. Remove the duplicate mapping.

### `NullReferenceException` in `AddMongoDatabase`

The connection string has no database name (`mongodb://host:27017` instead of `mongodb://host:27017/shop`). Add the name. A missing `ConnectionStrings` entry throws an `ArgumentNullException` for `connectionString`.

### The client connects to `localhost:27017` instead of my server

> [!WARNING]
> Known issue: `DefaultMongoClientFactory<TContext>.CreateClient` looks up the named `MongoClientSettings` of the database with `IOptionsMonitor.Get`. The options system returns default settings for a name that has no registration, so the check for missing settings never fires and the client silently targets `localhost:27017`. `AddMongoDatabase` registers the settings under the name the factory uses, so this only happens when you register the context or the settings yourself under a different name.

Check the name you registered the settings with: it must be the lower-case database name of the context.

## See also

- [Data access](index.md)
- [MongoDB .NET driver documentation](https://www.mongodb.com/docs/drivers/csharp/current/)
- <xref:Arc4u.MongoDB> in the API reference
