# Arc4u.Data

Entity base classes, `PersistChange` tracking and `EntitySet<TEntity>` for the data access layer of an Arc4u service: each entity says whether it must be inserted, updated or deleted.

## Install

```bash
dotnet add package Arc4u.Data --prerelease
```

## Usage

```csharp
using Arc4u.Data;

var order = new Order { PersistChange = PersistChange.Insert };
var line = new OrderLine { PersistChange = PersistChange.Insert };
order.Lines.Add(line);
order.Lines.Remove(line);   // an Insert is removed from the set; a saved entity is marked Delete instead

public class Order : IdEntity { public EntitySet<OrderLine> Lines { get; } = new(); }
public class OrderLine : IdEntity;
```

## Documentation

- Guide: [Data primitives](https://arc4u-org.github.io/Arc4u/guides/data/data-primitives.html)
- API reference: [Arc4u.Data](https://arc4u-org.github.io/Arc4u/api/Arc4u.Data.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
