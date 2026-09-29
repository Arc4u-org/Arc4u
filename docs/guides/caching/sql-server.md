---
description: "Configure a named cache stored in a SQL Server table with the Sql kind."
---
# SQL Server cache

The `Sql` kind stores the values in a SQL Server table. Use it when you already run SQL Server and want a shared cache that
survives restarts without adding Redis to the infrastructure. It is slower than memory or Redis. This page extends the
[caching guide](index.md).

## What it solves

`SqlCache` (<xref:Arc4u.Caching.Sql.SqlCache>) wraps the `Microsoft.Extensions.Caching.SqlServer` distributed cache and adds
the named configuration and the serialization of the values. The kind is `Sql`; the NuGet package is
`Arc4u.Caching.SqlServer` and lives in the `src/Arc4u.Caching.Sql` folder of the repository.

## Packages involved

| Package | Use it for |
|---|---|
| `Arc4u.Caching.SqlServer` | `SqlCache` (kind `Sql`). |
| `Arc4u.Caching` | `ICacheContext` and `AddCacheContext`. |
| `Arc4u.Serializer.JSon` | The serializer (any `IObjectSerialization` works). |

## Install

```bash
dotnet add package Arc4u.Caching.SqlServer --prerelease
dotnet add package Arc4u.Serializer.JSon --prerelease
```

Create the cache table before the first use. Arc4u does not create it. Use the
[`dotnet sql-cache` tool](https://learn.microsoft.com/aspnet/core/performance/caching/distributed#distributed-sql-server-cache)
of ASP.NET Core:

```bash
dotnet tool install --global dotnet-sql-cache
dotnet sql-cache create "<connection-string>" dbo SqlCache
```

## Configuration

### appsettings.json

```json
{
  "Caching": {
    "Default": "Durable",
    "Caches": [
      {
        "Name": "Durable",
        "Kind": "Sql",
        "IsAutoStart": true,
        "Settings": {
          "ConnectionString": "Server=localhost;Database=Cache;Integrated Security=true;TrustServerCertificate=true",
          "SchemaName": "dbo",
          "TableName": "SqlCache"
        }
      }
    ]
  }
}
```

The settings bind to <xref:Arc4u.Configuration.Sql.SqlCacheOption>:

| Key | Type | Default | Description |
|---|---|---|---|
| `Caching:Caches:n:Settings:ConnectionString` | `string` | none (required) | Connection string of the database that holds the table. |
| `Caching:Caches:n:Settings:SchemaName` | `string` | `dbo` | Schema of the table. |
| `Caching:Caches:n:Settings:TableName` | `string` | `SqlCache` | Name of the table. |
| `Caching:Caches:n:Settings:SerializerName` | `string` | none | Key of the `IObjectSerialization` to use. When empty or not registered, the unkeyed one is used. |

> [!CAUTION]
> The connection string can hold credentials. Read it from user secrets, an environment variable
> (`Caching__Caches__0__Settings__ConnectionString`) or a secret store, not from a committed file.

### Code

```csharp
// Program.cs
using Arc4u.Caching;
using Arc4u.Caching.Sql;
using Arc4u.Dependency;
using Arc4u.Serializer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddILogger();
builder.Services.AddCacheContext(builder.Configuration);
builder.Services.AddSingleton<IObjectSerialization, JsonSerialization>();
builder.Services.AddKeyedTransient<ICache, SqlCache>(CacheContext.Sql);

var app = builder.Build();
app.Run();
```

## Common scenarios

### Keep a cache across restarts

Declare a `Sql` cache next to a `Memory` cache and choose by name: durable, shared data (a computed catalog, a token store)
in `Durable`, per-instance data in memory. See [Use several caches](index.md#use-several-caches).

## Extensibility points

`SqlCache` is registered under the key `Sql`. Register your own `ICache` under that key to replace it.

## Troubleshooting

### `ArgumentNullException` for `ConnectionString` at startup

`ConnectionString` is required and is checked when the cache is initialized. A `Sql` cache with no `Settings` section
has none.

### `DataCacheException` on `Get`, or `Put` fails with a SQL error

The table does not exist or has another name or schema. Create it with `dotnet sql-cache create`, then check `SchemaName`
and `TableName`.

## See also

- [Caching](index.md)
- [Serialization](serialization.md)
- <xref:Arc4u.Caching.Sql> in the API reference
