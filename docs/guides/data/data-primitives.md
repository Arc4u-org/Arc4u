---
description: "Entity base classes, PersistChange tracking, EntitySet and validation helpers of Arc4u.Data."
---
# Data primitives

`Arc4u.Data` holds the building blocks of the [data access layer](../../concepts/glossary.md#data-access-layer) that do not depend on a database: an interface and base classes that let an entity carry the change to persist, a list type that maintains that change when items are removed, and validation helpers. [Entity Framework Core](efcore.md) turns these values into database operations. The guide overview is in [Data access](index.md).

## What it solves

A client that edits an object graph (an order with its lines) sends the whole graph back. The server must know which nodes were added, changed or removed. Instead of comparing the graph with the database, Arc4u makes each entity say what should happen to it: its `PersistChange`.

## Install

```bash
dotnet add package Arc4u.Data --prerelease
```

`IPersistEntity`, `PersistChange` and `Graph<T>` are defined in the `Arc4u` package, which `Arc4u.Data` references. This package has no configuration and registers nothing in dependency injection.

## PersistChange

<xref:Arc4u.Data.PersistChange> has four values, and <xref:Arc4u.Data.IPersistEntity> is the one-property interface (`PersistChange PersistChange { get; set; }`) the other Arc4u packages rely on.

| Value | Meaning when the graph is saved |
|---|---|
| `None` (0) | The entity is not changed. |
| `Delete` (1) | The entity is deleted. |
| `Insert` (2) | The entity is inserted. |
| `Update` (3) | The entity is updated. |

Nothing in Arc4u sets `Update` for you, for example when a property changes: the code that edits the entity (a client, a facade, your mapping code) sets `PersistChange`. Nothing resets it after a save either, see [Entity Framework Core](efcore.md#pitfalls).

`PersistChange` is a plain property on any class that implements `IPersistEntity`, so an existing domain model can adopt it without a base class. The base classes below add change notification and rules on top.

## Entity base classes

<xref:Arc4u.Data.PersistEntity> is the abstract base class. It implements `IPersistEntity` and `INotifyPropertyChanged`, raises `PropertyChanged` when `PersistChange` changes, and refuses the two impossible transitions: from `Insert` to `Delete` and from `Delete` to `Insert` throw an `ArgumentException` (an entity that was never saved has nothing to delete). Every other transition is accepted.

The derived classes add an identifier and audit properties:

| Class | Adds | Notes |
|---|---|---|
| <xref:Arc4u.Data.IdEntity> | `Guid Id` | The constructor assigns `Guid.NewGuid()`. Equality and hash code use `Id`. |
| `IdEntity<TId>` | `TId Id` | Same, for any identifier type. The `Id` is `default` until you set it. |
| <xref:Arc4u.Data.CreateAuditEntity> | `CreatedBy` (`string`), `CreatedOn` (`DateTimeOffset`) | The constructor sets `CreatedOn` to `DateTimeOffset.UtcNow`. |
| <xref:Arc4u.Data.UpdateAuditEntity> | Adds `UpdatedBy` (`string`), `UpdatedOn` (`DateTimeOffset?`) | Same constructor behavior. |
| <xref:Arc4u.Data.DeleteAuditEntity> | Adds `DeletedBy` (`string`), `DeletedOn` (`DateTimeOffset?`) | Same constructor behavior. |
| <xref:Arc4u.Data.AuditEntity> | `AuditedBy` (`string`), `AuditedOn` (`DateTimeOffset`) | For one generic "who and when" pair. Does not set a date. |

Each non-generic class derives from a generic one (`CreateAuditEntity<TId, TCreatedBy, TCreatedOn>` and so on) if you need another identifier or user type. The audit properties are plain properties: Arc4u does not fill `CreatedBy` or the update and delete pairs. Set them where you know the current user, typically in the data layer before saving.

```csharp
using Arc4u.Data;

public class OrderLine : IdEntity
{
    public string Label { get; set; } = string.Empty;
}

public class Order : UpdateAuditEntity
{
    public string Name { get; set; } = string.Empty;

    public EntitySet<OrderLine> Lines { get; } = new();
}
```

## EntitySet

<xref:Arc4u.Data.EntitySet`1> is a list of `IPersistEntity` items. It implements `IList<TEntity>`, `IList`, `INotifyCollectionChanged` and `INotifyPropertyChanged`, and offers most of the `List<T>` methods (`Find`, `FindAll`, `Sort`, `AddRange`, `RemoveAll` and so on). It differs from a list in one respect: removing an item does not always remove it.

| Item's `PersistChange` when removed | What happens |
|---|---|
| `Insert` | The item is removed from the set. It never reached the database. |
| `None` or `Update` | The item stays in the set and its `PersistChange` becomes `Delete`. |
| `Delete` | Nothing. |

The deleted items stay in the set, and `Count` includes them, so that the whole graph can be sent to the data layer and saved in one call. To work with a subset, use the `PersistChangeActions` flags:

```csharp
using Arc4u.Data;

public static class EntitySetSamples
{
    public static void Run(Order order, OrderLine existing)
    {
        var added = new OrderLine { Label = "new", PersistChange = PersistChange.Insert };
        order.Lines.Add(added);

        order.Lines.Remove(added);      // Insert: removed from the set
        order.Lines.Remove(existing);   // None or Update: kept, marked Delete

        OrderLine[] toDelete = order.Lines.ToArray(PersistChangeActions.Delete);
        int changes = order.Lines.Count(PersistChangeActions.Insert | PersistChangeActions.Update | PersistChangeActions.Delete);
    }
}
```

`PersistChangeActions` is a `[Flags]` enum with `None` (1), `Delete` (2), `Insert` (4), `Update` (8) and the combinations `AllExceptDelete`, `AllExceptNone` and `All`. `None` is 1, not 0: a value of `0` selects nothing. `ToArray(PersistChangeActions)` and the `Count(PersistChangeActions)` extension method (<xref:Arc4u.Data.EntitySetExtension>) filter with these flags. `PropagatePersistChange(PersistChange)` sets the same value on every item.

## EntityItem

<xref:Arc4u.Data.EntityItem`1> wraps an entity (`Entity`) together with its own `PersistChange`. It carries the change of the relation between a parent and the wrapped entity, independently of the entity's own state. Setting its `PersistChange` to `Update` throws, since a relation is inserted or deleted, never updated. `EntitySet<TEntity>` converts implicitly to and from `EntitySet<EntityItem<TEntity>>`.

## Validation

`PersistEntity.TryValidate()` runs the `System.ComponentModel.DataAnnotations` attributes of the entity (`[Required]`, `[StringLength]`, ...) and returns a FluentResults `Result` with one error per message. See [Results](../results/index.md) for how Arc4u uses `Result`.

```csharp
using Arc4u.Data;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

public class Customer : IdEntity
{
    [Required]
    public string? Name { get; set; }
}

public static class ValidationSamples
{
    public static Result Validate(IReadOnlyList<Customer> customers, ILogger<Customer> logger)
    {
        Result single = customers[0].TryValidate();

        return customers.ValidateAll(logger);   // one merged Result for all customers
    }
}
```

`Validate<T>(logger)` and `ValidateAll(logger)` write one error to the logger you pass for each validation message (`Validation of {EntityType} failed: {Message}`). `ValidateAll` also returns the merged `Result`.

## Extensibility points

Implement `IPersistEntity` on your own classes, or derive from `PersistEntity`, `IdEntity<TId>` or one of the audit classes and override the virtual `PersistChange` property to change the transition rules. There are no services to replace.

## Troubleshooting

### `ArgumentException: Invalid transition between Insert and Delete change`

You set `PersistChange` to `Delete` on an entity that is `Insert`, or the reverse. Remove a not yet saved entity from its `EntitySet` instead of marking it: `Remove` drops it. `EntityItem<TEntity>` also refuses `Update`.

### An item removed from an `EntitySet` is still in it

It was `None` or `Update`, so it is marked `Delete` and kept on purpose. Enumerate `ToArray(PersistChangeActions.AllExceptDelete)` if you want only the surviving items.

## See also

- [Entity Framework Core](efcore.md) for saving these entities
- [Data access](index.md)
- <xref:Arc4u.Data> in the API reference
