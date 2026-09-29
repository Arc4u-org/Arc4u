# Arc4u.EfCore

Saves a whole entity graph with Entity Framework Core by turning the `PersistChange` of each Arc4u entity into an `EntityState`, and applies `Graph<T>` includes to queries.

## Install

```bash
dotnet add package Arc4u.EfCore --prerelease
```

## Usage

```csharp
using Arc4u.EfCore;
using Microsoft.EntityFrameworkCore;

static async Task SaveAsync(DbContext db, object root)   // root: an entity implementing IPersistEntity
{
    db.ChangeTracker.TrackGraph(root, ChangeGraphTracker.Tracker);
    await db.SaveChangesAsync();
}
```

The model must ignore the `PersistChange` property of the entities (`entity.Ignore(e => e.PersistChange)`).

## Documentation

- Guide: [Entity Framework Core](https://arc4u-org.github.io/Arc4u/guides/data/efcore.html)
- API reference: [Arc4u.EfCore](https://arc4u-org.github.io/Arc4u/api/Arc4u.EfCore.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
