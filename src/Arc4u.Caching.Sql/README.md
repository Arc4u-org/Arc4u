# Arc4u.Caching.SqlServer

The `Sql` cache kind of Arc4u: a named cache stored in a SQL Server table.

## Install

```bash
dotnet add package Arc4u.Caching.SqlServer --prerelease
```

## Usage

```csharp
builder.Services.AddCacheContext(builder.Configuration);
builder.Services.AddSingleton<IObjectSerialization, JsonSerialization>();
builder.Services.AddKeyedTransient<ICache, SqlCache>(CacheContext.Sql);
```

The `Caching` section declares the cache with `"Kind": "Sql"` and the settings `ConnectionString`, `SchemaName` and
`TableName`. Create the table first with `dotnet sql-cache create`.

## Documentation

- Guide: [Caching](https://arc4u-org.github.io/Arc4u/guides/caching/sql-server.html)
- API reference: [Arc4u.Caching.Sql](https://arc4u-org.github.io/Arc4u/api/Arc4u.Caching.Sql.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
