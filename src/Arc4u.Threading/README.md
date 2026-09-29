# Arc4u.Threading

Asynchronous synchronization primitives: an awaitable lock and semaphore, plus an ambient `Scope<T>` and culture helpers.

## Install

```bash
dotnet add package Arc4u.Threading --prerelease
```

## Usage

```csharp
using Arc4u.Threading;

var gate = new AsyncLock();

using (await gate.LockAsync())
{
    // One caller at a time runs here, and you can await inside.
    await Task.Delay(10);
}
```

## Documentation

- Guide: [Concepts](https://arc4u-org.github.io/Arc4u/concepts/)
- API reference: [Arc4u.Threading](https://arc4u-org.github.io/Arc4u/api/Arc4u.Threading.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
