# Arc4u.Core

Small abstractions shared by all Arc4u packages: the `IKeyValueSettings` and `IAppSettings` contracts and the `ValueObject` base class for domain value objects.

## Install

```bash
dotnet add package Arc4u.Core --prerelease
```

## Usage

```csharp
using Arc4u.Core;

public sealed class Money(decimal amount, string currency) : ValueObject
{
    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return amount;
        yield return currency;
    }
}
```

Two `Money` instances with the same amount and currency are equal.

## Documentation

- Guide: [Concepts](https://arc4u-org.github.io/Arc4u/concepts/)
- API reference: [Arc4u.Core](https://arc4u-org.github.io/Arc4u/api/Arc4u.Core.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
