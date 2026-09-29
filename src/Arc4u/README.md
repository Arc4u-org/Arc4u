# Arc4u

Base library of the Arc4u framework: intervals and periods, timeout and time zone helpers, exceptions, extension methods and the security and diagnostics building blocks that the other Arc4u packages build on.

## Install

```bash
dotnet add package Arc4u --prerelease
```

## Usage

```csharp
using Arc4u;

var timeout = new TimeoutHelper(TimeSpan.FromSeconds(5));
// ... do some work ...
TimeSpan left = timeout.RemainingTime();
```

`TimeoutHelper` tracks the time left of an initial timeout across several operations.

## Documentation

- Guide: [Concepts](https://arc4u-org.github.io/Arc4u/concepts/)
- API reference: [Arc4u](https://arc4u-org.github.io/Arc4u/api/Arc4u.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
