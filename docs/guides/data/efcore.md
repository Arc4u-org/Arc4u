---
description: "Apply the include paths of a Graph<T> to Entity Framework Core queries."
---
# Entity Framework Core

`Arc4u.EfCore` applies a <xref:Arc4u.Graph`1> (a list of include paths) to an Entity Framework Core query. It sits in the data access layer next to your `DbContext`; the [overview](index.md) shows where it fits.

Arc4u does not track changes on your entities. Persist your aggregates with the Entity Framework Core API (`Add`, `Update`, `Remove`, `SaveChangesAsync`), driven by the behavior of your domain model.

## What it solves

A caller decides which parts of an aggregate it needs (an order with its lines, or with its customer). `Graph<T>` lets it express that as a list of paths built from expressions, and because `Graph<T>` serializes with `System.Text.Json` it can be sent between processes. At the data layer, one call turns the paths into `Include` and `ThenInclude` calls. [Include graphs](graph.md) explains how to build and use a graph.

## Install

```bash
dotnet add package Arc4u.EfCore --prerelease
```

`Arc4u.EfCore` references `Microsoft.EntityFrameworkCore` and `Arc4u`, which defines `Graph<T>`. Add the provider package of your database yourself. The package has no configuration section and registers nothing in dependency injection: you keep registering your `DbContext` with `AddDbContext` as usual.

## Apply the includes of a Graph to a query

<xref:Arc4u.EfCore.GraphExtension> applies a graph to an EF Core query:

```csharp
using Arc4u;
using Arc4u.EfCore;
using Microsoft.EntityFrameworkCore;

public class OrderLine
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
}

public class Order
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<OrderLine> Lines { get; set; } = [];
}

public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
                    .HasMany(o => o.Lines)
                    .WithOne()
                    .HasForeignKey(l => l.OrderId);
    }
}

public static class GraphSamples
{
    public static Task<List<Order>> LoadAsync(ShopContext db)
    {
        var graph = new Graph<Order>().Include(o => o.Lines);

        return graph.ApplySetReferences(db.Orders).ToListAsync();
    }
}
```

| Method | Applies |
|---|---|
| `ApplySetReferences(query)` | Every path of the graph, with `Include` and `ThenInclude`. Collections are included. |
| `ApplySingleReferences(query)` | Only the paths made of single-valued references. A path is cut at the first collection. `Graph<Order>` with `Lines` produces no `Include`. |
| `ApplyReferences(query, o => o.Lines)` | Only the paths that start with the given first-level property (`Lines`, `Lines.Product`, ...), with `Include` and `ThenInclude`. A deeper expression such as `o => o.Customer.Address` throws `InvalidOperationException`. |

## Pitfalls

- **The graph helpers use reflection** (`GraphExtension` builds `Include` and `ThenInclude` calls at run time), so they are not trim-safe or native AOT-safe.

## Troubleshooting

### `InvalidOperationException: It is not allowed to check more than one level!` from `ApplyReferences`

The path expression goes deeper than one property, for example `o => o.Customer.Address`. Pass the first-level property (`o => o.Customer`): every path of the graph under it is included.

## See also

- [Data access](index.md)
- [Include graphs](graph.md)
- [Configuration store on EF Core](../configuration/index.md) for the separate `Arc4u.Configuration.Store.EfCore` package
- <xref:Arc4u.EfCore> in the API reference
