# Arc4u.MongoDB

A `DbContext`-like class per MongoDB database: register it from a connection string, map entity types to collections and get typed collections from a singleton `IMongoClientFactory<TContext>`.

## Install

```bash
dotnet add package Arc4u.MongoDB --prerelease
```

## Usage

```csharp
using Arc4u.MongoDB;
using Arc4u.MongoDB.Configuration;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMongoDatabase<ShopContext>(builder.Configuration, "Shop");   // ConnectionStrings:Shop, with a database name
public class Product { public Guid Id { get; set; } }
public class ShopContext : DbContext
{
    protected override void OnConfiguring(DbContextBuilder context) => context.MapCollection("products").With<Product>();
}
```

Inject `IMongoClientFactory<ShopContext>` and call `GetCollection<Product>()`.

## Documentation

- Guide: [MongoDB](https://arc4u-org.github.io/Arc4u/guides/data/mongodb.html)
- API reference: [Arc4u.MongoDB](https://arc4u-org.github.io/Arc4u/api/Arc4u.MongoDB.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
