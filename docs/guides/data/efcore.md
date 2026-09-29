---
description: "Save an entity graph with one call: ChangeGraphTracker turns PersistChange into Entity Framework Core states, and Graph<T> applies includes to queries."
---
# Entity Framework Core

`Arc4u.EfCore` connects the `PersistChange` of [Arc4u entities](data-primitives.md) to Entity Framework Core. It contains a callback for `ChangeTracker.TrackGraph` that sets the state of every entity of a graph, a conversion from `PersistChange` to `EntityState`, and helpers that apply a `Graph<T>` (a list of include paths) to a query. It sits in the [data access layer](../../concepts/glossary.md#data-access-layer) next to your `DbContext`; the [overview](index.md) shows where it fits.

## What it solves

A detached graph (an order with its lines) comes back from a client. Some lines are new, some edited, some removed. Plain EF Core needs code that walks the graph and sets `EntityState.Added`, `Modified` or `Deleted` on each node, or a full reload and compare. With Arc4u each entity already says what to do, and the data layer becomes:

```csharp
db.ChangeTracker.TrackGraph(order, ChangeGraphTracker.Tracker);
await db.SaveChangesAsync(cancellationToken);
```

## Install

```bash
dotnet add package Arc4u.EfCore --prerelease
```

`Arc4u.EfCore` references `Microsoft.EntityFrameworkCore` and `Arc4u.Data`. Add the provider package of your database yourself. The package has no configuration section and registers nothing in dependency injection: you keep registering your `DbContext` with `AddDbContext` as usual.

## Set up the model and the repository

Two things are needed: entities that implement <xref:Arc4u.Data.IPersistEntity> (usually by deriving from <xref:Arc4u.Data.IdEntity> or another class of [Arc4u.Data](data-primitives.md)) and a model that ignores `PersistChange`. `PersistChange` describes what to do with the entity, it is not data to store. Without `Ignore`, EF Core would map it as a column.

```csharp
// ShopContext.cs
using Arc4u.Data;
using Arc4u.EfCore;
using Microsoft.EntityFrameworkCore;

public class OrderLine : IdEntity
{
    public string Label { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
}

public class Order : IdEntity
{
    public string Name { get; set; } = string.Empty;
    public List<OrderLine> Lines { get; set; } = [];
}

public class ShopContext(DbContextOptions<ShopContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Ignore(o => o.PersistChange);
            e.HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId);
        });

        modelBuilder.Entity<OrderLine>(e =>
        {
            e.HasKey(l => l.Id);
            e.Ignore(l => l.PersistChange);
        });
    }
}

public class OrderRepository(ShopContext db)
{
    public async Task SaveAsync(Order order, CancellationToken cancellationToken)
    {
        db.ChangeTracker.TrackGraph(order, ChangeGraphTracker.Tracker);

        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(IEnumerable<Order> orders, CancellationToken cancellationToken)
    {
        foreach (var order in orders)
        {
            db.ChangeTracker.TrackGraph(order, ChangeGraphTracker.Tracker);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
```

```csharp
// Program.cs
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ShopContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Shop")));
builder.Services.AddScoped<OrderRepository>();

var app = builder.Build();
app.Run();
```

`UseSqlite` stands for the provider of your choice.

## How the states are set

`TrackGraph` visits every entity reachable from the root that the context does not track yet and calls your callback for each one. <xref:Arc4u.EfCore.ChangeGraphTracker> provides two callbacks:

| Callback | Use it for |
|---|---|
| `ChangeGraphTracker.Tracker(EntityEntryGraphNode)` | Each entity gets the state of its own `PersistChange`. An entity that does not implement `IPersistEntity` becomes `Unchanged`. Pass it as a method group: `TrackGraph(order, ChangeGraphTracker.Tracker)`. |
| `ChangeGraphTracker.Tracker(IPersistEntity, EntityEntryGraphNode)` | Every entity of the graph gets the state of the root's `PersistChange`. Use it to delete or insert a whole aggregate: `TrackGraph(order, node => ChangeGraphTracker.Tracker(order, node))`. |

The conversion is done by <xref:Arc4u.EfCore.PersisteChangeExtension.Convert*> (the class name is spelled that way), also available on its own:

| `PersistChange` | `EntityState` |
|---|---|
| `Insert` | `Added` |
| `Update` | `Modified` |
| `Delete` | `Deleted` |
| `None` | `Unchanged` |

## Common scenarios

### Apply the edits of a client to a loaded graph

Load the graph without tracking (or receive it from a client), let the caller set `PersistChange` on the nodes it touched, then track and save. With the code below, the lines end up `Modified`, `Deleted` and `Added`, and the root `Unchanged` (checked against SQLite):

```csharp
using Arc4u.Data;
using Arc4u.EfCore;
using Microsoft.EntityFrameworkCore;

public static class EditSamples
{
    public static async Task ApplyEditsAsync(ShopContext db, Guid orderId)
    {
        var order = await db.Orders.AsNoTracking()
                            .Include(o => o.Lines)
                            .SingleAsync(o => o.Id == orderId);

        order.Lines[0].Label = "renamed";
        order.Lines[0].PersistChange = PersistChange.Update;          // Modified
        order.Lines[1].PersistChange = PersistChange.Delete;          // Deleted
        order.Lines.Add(new OrderLine { Label = "new", PersistChange = PersistChange.Insert });   // Added

        db.ChangeTracker.TrackGraph(order, ChangeGraphTracker.Tracker);   // root is None: Unchanged
        await db.SaveChangesAsync();
    }
}
```

An entity loaded from the database has `PersistChange.None`, so the root and every line you did not touch are left alone.

### Delete a whole aggregate

```csharp
using Arc4u.Data;
using Arc4u.EfCore;
using Microsoft.EntityFrameworkCore;

public static class DeleteSamples
{
    public static async Task DeleteAsync(ShopContext db, Order order)
    {
        order.PersistChange = PersistChange.Delete;

        db.ChangeTracker.TrackGraph(order, node => ChangeGraphTracker.Tracker(order, node));
        await db.SaveChangesAsync();
    }
}
```

### Apply the includes of a Graph to a query

<xref:Arc4u.Graph`1> is a list of include paths built from expressions. It is a data contract, so it can be sent between processes. <xref:Arc4u.EfCore.GraphExtension> applies it to an EF Core query:

```csharp
using Arc4u;
using Arc4u.EfCore;
using Microsoft.EntityFrameworkCore;

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

## Pitfalls

- **`PersistChange` is not reset after a save.** After `SaveChangesAsync` the entities still say `Insert`, `Update` or `Delete`. Set them back to `None` (or discard the objects) before reusing them; an entity still on `Insert` also refuses to become `Delete`.
- **Entities already tracked by the context are not visited.** `TrackGraph` skips them, so their state is not changed by `ChangeGraphTracker`. Load with `AsNoTracking`, or use a fresh context, for graphs you track this way.
- **Nothing sets `Update` for you.** Changing a property does not change `PersistChange`. The caller that edits the entity sets it.
- **The graph helpers use reflection** (`GraphExtension` builds `Include` and `ThenInclude` calls at run time), so they are not trim-safe or native AOT-safe.

## Extensibility points

`ChangeGraphTracker`, `PersisteChangeExtension` and `GraphExtension` are static. To change the mapping (for example to treat `Update` as `Unchanged`), pass your own callback to `TrackGraph` and call `PersistChange.Convert()` for the cases you keep.

## Troubleshooting

### A property `PersistChange` appears in the database schema or the migration

The model does not ignore it. Add `entity.Ignore(e => e.PersistChange)` for every entity type that implements `IPersistEntity`.

### The entities are saved as `Unchanged` and nothing is written

`PersistChange` is `None` on the nodes you edited, or the context already tracked them (see [Pitfalls](#pitfalls)). Set `PersistChange` on the edited entities and track detached instances.

### `InvalidOperationException: It is not allowed to check more than one level!` from `ApplyReferences`

> [!WARNING]
> Known issue: `GraphExtension.ApplyReferences(graph, query, path)` throws this exception for every path expression, including a first-level one such as `o => o.Lines`, so it cannot be used. Use `ApplySetReferences` or `ApplySingleReferences`.

## See also

- [Data primitives](data-primitives.md)
- [Data access](index.md)
- [Configuration store on EF Core](../configuration/index.md) for the separate `Arc4u.Configuration.Store.EfCore` package
- <xref:Arc4u.EfCore> in the API reference
