---
description: "Chain OnSuccess, OnFailed and LogIfFailed on a Result, create ProblemDetailError and log failed results with Arc4u.Results."
---
# Fluent result chains

`Arc4u.Results` adds extension methods to FluentResults' `Result` and `Result<T>` so that a use
case reads as "on success do this, on failure do that" instead of a series of `if (result.IsSuccess)`
blocks. This page is the reference for those methods, for `ProblemDetailError` and for the way
failed results are logged. It builds on the [result pattern](../../concepts/glossary.md#result-pattern);
the [guide overview](index.md) shows how the pieces fit together.

## Why a fluent chain

With plain FluentResults, every call site tests the state, and a `Result<T>` that succeeded can
still carry a `null` value that you must test too:

```csharp
var result = await db.SaveAsync(order);
if (result.IsSuccess)
{
    // next step
}
else
{
    // log, then report a problem to the caller
}
```

With the chain, the same code states what happens in each case:

```csharp
using Arc4u.Results;
using FluentResults;

public class SaveOrder(IOrderRepository db)
{
    public async Task<Result> RunAsync(Order order)
    {
        var global = Result.Ok();

        await db.SaveAsync(order)
                .OnSuccess(() => Console.WriteLine("Saved."))
                .OnSuccessAsync(() => db.NotifyAsync(order))
                .LogIfFailed()
                .OnFailed(global)
                .ConfigureAwait(false);

        return global;
    }
}

public record Order(int Id);

public interface IOrderRepository
{
    Task<Result> SaveAsync(Order order);
    Task NotifyAsync(Order order);
}
```

## The methods

Every method exists on `Result`, `Result<T>`, `Task<Result>`, `Task<Result<T>>`, `ValueTask<Result>`
and `ValueTask<Result<T>>`, so the chain works before and after an `await`. They are in the
static class <xref:Arc4u.Results.ResultExtension>, except `LogIfFailed` on a plain `Result` or `Result<T>`, which is FluentResults' own.
`OnSuccessNull` and `OnSuccessNotNull` exist only for `Result<T>`.

| Method | Runs the callback when | Callback receives |
|---|---|---|
| `OnSuccess` / `OnSuccessAsync` | The result succeeded (its value can be `null`) | Nothing, or the value for `Result<T>` |
| `OnSuccessNotNull` / `OnSuccessNotNullAsync` | The result succeeded and the value is not `null` | Nothing, or the value |
| `OnSuccessNull` / `OnSuccessNullAsync` | The result succeeded and the value is `null` | Nothing |
| `OnFailed` / `OnFailedAsync` | The result failed | The `IReadOnlyCollection<IError>` of the result |
| `OnFailed(globalResult)` | The result failed | Nothing: copies the errors into `globalResult` |
| `LogIfFailed` | The result failed | Nothing: logs the errors (see [Logging failures](#logging-failures)) |

Rules that apply to all of them:

- **The original result is returned.** A callback cannot change the result, which keeps the chain
  honest: what you read is what runs. To report the outcome of a whole chain, collect errors in
  a separate result with `OnFailed(globalResult)`, as in the sample above. The errors are added
  to `globalResult`, which then fails.
- **Methods named `*Async` return a task.** Await the chain. Nothing in the library blocks on a task.
- **A method without `Async` refuses an asynchronous callback.** `OnSuccess(async () => ...)` would run
  as `async void`, so the overload is marked as an error and the compiler tells you to use
  `OnSuccessAsync`. Adapt a `ValueTask` call with an async lambda: `async () => await SaveAsync()`.
- **A callback exception propagates.** It is not turned into a failed result. To convert an
  exception into a result, use `Result.Try` from FluentResults.
- A `null` callback throws `ArgumentNullException`.

### Chain on a result with a value

`OnSuccessNull` and `OnSuccessNotNull` cover the two cases of a `Result<T>` that succeeded, so
you can avoid a null test:

```csharp
using Arc4u.Results;
using FluentResults;

public class Lookup(ICustomerRepository db, ILogger<Lookup> logger)
{
    public Task<Result<Customer>> FindAsync(int id)
        => db.FindAsync(id)
             .OnSuccessNotNull(customer => logger.LogInformation("Found {Name}", customer.Name))
             .OnSuccessNull(() => logger.LogInformation("Customer {Id} does not exist", id))
             .LogIfFailed();
}

public record Customer(int Id, string Name);

public interface ICustomerRepository
{
    Task<Result<Customer>> FindAsync(int id);
}
```

## Errors

FluentResults provides `Error` and `ExceptionalError`. To report an exception, add an
`ExceptionalError`: `result.WithError(new ExceptionalError(exception))`. Arc4u adds two error
types that carry the data needed to build a `ProblemDetails`.

### ProblemDetailError

<xref:Arc4u.Results.ProblemDetailError> describes a problem the caller can understand and act on. Its `Message` is the
`detail` of the `ProblemDetails`. Create it with `Create` and complete it with the `With...` methods.

| Member | Type | Default | Becomes in the `ProblemDetails` |
|---|---|---|---|
| `Create(detail)` | `string` | none (required) | `detail` |
| `WithTitle` / `Title` | `string?` | `null`, mapped to `"Error."` | `title` |
| `WithStatusCode` / `StatusCode` | `int?` | `null`, mapped to `500` | `status` |
| `WithType` / `Type` | `Uri?` | `null`, mapped to `about:blank` | `type` |
| `WithInstance` / `Instance` | `string?` | `null` | `instance` |
| `WithSeverity` / `Severity` | `string?` | `null`, mapped to `"Error"` | Extension member `Severity` |
| `WithMetadata(key, value)` | `string`, `object` | none | An extension member named `key` |

> [!NOTE]
> A `ProblemDetailError` without `WithStatusCode` answers `500`, not `400`. Set the status code of every
> error that is not a server fault.

An existing metadata key is kept: `WithMetadata` with a key that is already present is ignored.
`ProblemDetailError` converts implicitly to a failed `Result`, so a method can return it directly:

```csharp
using Arc4u.Results;
using FluentResults;

public static class Stock
{
    public static Result Reserve(int available, int wanted)
    {
        if (wanted > available)
        {
            return ProblemDetailError.Create($"Only {available} items are left.")
                                     .WithTitle("Not enough stock")
                                     .WithStatusCode(409)
                                     .WithMetadata("available", available);
        }

        return Result.Ok();
    }
}
```

For a `Result<T>`, wrap it: `Result.Fail<Customer>(ProblemDetailError.Create("..."))`.

### ValidationError

<xref:Arc4u.Results.Validation.ValidationError> reports invalid input. It has its own page: [Validation errors](validation.md).

### Which error becomes which response

The conversion to HTTP is done by `Arc4u.AspNetCore.Results`. In short: an exception gives `500` with a
generic message, a `ValidationError` gives `422`, a `ProblemDetailError` gives its status code, any other
error gives `400`. The rules and the exact JSON are in [Results to HTTP responses](http-mapping.md).

## Logging failures

`LogIfFailed` comes from FluentResults for a plain `Result`. Arc4u adds the overloads for `Task<Result>`,
`Task<Result<T>>`, `ValueTask<Result>` and `ValueTask<Result<T>>` so that you can call it in the middle of an
`await` chain. It logs only when the result failed, through the logger you registered with `Result.Setup`
(see [Configuration](index.md#configuration)).

<xref:Arc4u.Results.Logging.FluentLogger> is the Arc4u implementation of that logger. For a failed result it logs each error on its own:

| Error | Logged as |
|---|---|
| `ValidationError` | At the level matching its `Severity` (`Error`, `Warning`, `Info`), with its code in a `Code` property (empty when no code was set) |
| `IExceptionalError` | The exception, with `LogException` |
| Any other error, including `ProblemDetailError` | Error level |

The informational reasons of the result (not the errors) are then logged once, at information level.

> [!WARNING]
> Known issue: the `LogLevel` argument of `LogIfFailed` does not change how errors are logged.
> `LogIfFailed(LogLevel.Warning)` on a `ProblemDetailError` still writes at error level. `FluentLogger` uses the
> level only for an optional `content` line, which is logged when you call the FluentResults overload
> `LogIfFailed(context, content, logLevel)` with a non-empty context and content.
>
> Known issue: that overload makes the singleton `FluentLogger` add a `Context` property to its logger, and the
> property is never cleared. It then appears on every later result log in the process. Avoid the overload until it is fixed.

`FluentLogger` needs the Arc4u `ILogger<T>`; register it as shown in the [overview](index.md#code).

## Extensibility points

Implement `FluentResults.IResultLogger` and pass it to `Result.Setup` to log results in your own way.
Use `FluentLogger` as the model: it receives the `ResultBase` and decides what to log.

## Troubleshooting

### Compile error `CS0619: 'ResultExtension.OnSuccess(Result, Func<Task>)' is obsolete`

You passed an asynchronous lambda to a method that is not named `*Async`. Rename the call to
`OnSuccessAsync` (or `OnSuccessNotNullAsync`, `OnSuccessNullAsync`, `OnFailedAsync`) and await the chain.
The `OnFailedAsync` callback has one parameter, the collection of errors: `OnFailedAsync(async errors => ...)`.

### `OnSuccess(() => throw ...)` gives CS0619

A lambda that only throws also binds to the obsolete `Func<Task>` overload. Cast it: `OnSuccess((Action)(() => throw new InvalidOperationException()))`.

### The method `OnFailed(action, result)` does not exist

Some older samples passed the callback and the global result in the same call. The two forms are separate
overloads: `OnFailed(errors => ...)` and `OnFailed(globalResult)`. Chain them.

## See also

- [Results to HTTP responses](http-mapping.md)
- [Validation errors](validation.md)
- [FluentResults documentation](https://github.com/altmann/FluentResults)
- <xref:Arc4u.Results.ResultExtension> in the API reference
