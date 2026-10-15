# Arc4u.EfCore

Applies the includes of a `Graph<T>` to Entity Framework Core queries.

## Install

```bash
dotnet add package Arc4u.EfCore --prerelease
```

## Usage

```csharp
using Arc4u;
using Arc4u.EfCore;
using Microsoft.EntityFrameworkCore;

var graph = new Graph<Order>().Include(o => o.Customer).Include(o => o.Lines);

var orders = await graph.ApplySetReferences(db.Orders).ToListAsync();
```

## Documentation

- Guide: [Entity Framework Core](https://arc4u-org.github.io/Arc4u/guides/data/efcore.html)
- API reference: [Arc4u.EfCore](https://arc4u-org.github.io/Arc4u/api/Arc4u.EfCore.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
