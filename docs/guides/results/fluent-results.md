---
description: "Chain OnSuccess, OnFailed, LogIfFailed and the steps that can fail (Then, EnsureNotNull, Check, Compensate, Recover) on a Result, end a chain with Match, create ProblemDetailError and log failed results with Arc4u.Results."
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
  honest: what you read is what runs. These methods are for actions that cannot fail (log,
  notify, fill a cache). A step that can fail returns a result: chain it with `Then` or another of the
  [steps that can fail](#steps-that-can-fail). To collect the errors of independent operations in
  one result, use `OnFailed(globalResult)`, as in the sample above. The errors are added to
  `globalResult`, which then fails.
- **A callback returning a result is refused.** `OnSuccess(t => t.Update())` would discard the result
  of `Update`, errors included, and the chain would carry on as if it had succeeded. The overload is
  marked as an error that points to `Then`, `Check`, `Compensate` and `Recover`. The same applies to
  the `*Async` methods when the task of the callback produces a result. To discard a result on purpose,
  write a statement lambda: `OnSuccess(t => { t.Update(); })`.
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

## Steps that can fail

These methods chain a step that returns a result. Each one runs its callback only in the case it
handles, stops the chain at the first failure and keeps the reasons of every step.

| Method | Runs the callback when | Returns |
|---|---|---|
| `Then` / `ThenAsync` | The result succeeded | The result of the step, with its value and errors |
| `EnsureNotNull(() => error)` | The result succeeded with a `null` value | A `Result<T>` with a value that is not `null`, or failed with `error` |
| `Check` / `CheckAsync` | The result succeeded | The original value when the step succeeded, otherwise the errors of the step |
| `Compensate` / `CompensateAsync` | The result failed | The failure, with the reasons of the clean-up added: an error of the clean-up is not lost |
| `Recover` / `RecoverAsync` | The result failed | The result of the fallback when it succeeded, otherwise the errors of both |

They exist on `Result<T>`, `Task<Result<T>>` and `ValueTask<Result<T>>`; `Then`, `Compensate` and
`Recover` also exist on `Result`, `Task<Result>` and `ValueTask<Result>`. They return a new result carrying the
reasons of the original one followed by those of the callback, except when the callback is not called:
the original result is then returned. The rules of `OnSuccess` and `OnFailed` apply: a method named `*Async` takes a
callback returning a `Task` and returns a task, and an exception thrown by a callback propagates.

A use case that loads, changes and saves an entity reads in one chain, and returns the first failure,
whichever step produced it:

```csharp
using System.Globalization;
using Arc4u.Results;
using FluentResults;

public class RenameTemplate(ITemplateRepository db)
{
    public Task<Result> RunAsync(Guid id, string name, CancellationToken cancellationToken)
        => db.GetByIdAsync(id, cancellationToken)
             .EnsureNotNull(() => ProblemDetailError.Create(string.Format(CultureInfo.InvariantCulture, "No template with an id {0} exists.", id))
                                                    .WithStatusCode(404))
             .Check(template => template.Rename(name))
             .ThenAsync(template => db.UpdateAsync(template, cancellationToken))
             .LogIfFailed();
}

public class Template
{
    public Result Rename(string name) => name.Length <= 50 ? Result.Ok() : Result.Fail("The name is too long.");
}

public interface ITemplateRepository
{
    Task<Result<Template?>> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Result> UpdateAsync(Template template, CancellationToken cancellationToken);
}
```

`Then` is FluentResults' `Bind` under a name of the Arc4u chain, available on every receiver, so
`FluentResults.Extensions` is not needed: the chain continues with the result of the step, and with
its value for a `Result<T>`. `Check` is for a step whose result has no value of its own: the template
is kept for the next step, where `Then(t => t.Rename(name).ToResult(t))` would be needed otherwise.

`Compensate` and `Recover` are the failure side. `Compensate` runs a clean-up, for instance deleting a
file written before the failure; the result stays failed, with the errors of the clean-up added.
`Recover` handles the failure, for instance by reading a cached value; its result replaces the failure:

```csharp
Task<Result<Rate>> GetRateAsync(string currency)
    => rates.GetAsync(currency)
            .RecoverAsync(_ => cache.GetAsync(currency))
            .Compensate(_ => alerts.Raise($"No rate for {currency}."));
```

## End a chain with Match

`Match` turns a result into another type, for instance an HTTP response, a DTO or a message for the
user. It calls the callback matching the state of the result and returns what that callback produces,
so no variable has to be assigned from an `OnSuccess` and an `OnFailed` callback:

```csharp
using Arc4u.Results;
using FluentResults;

public static class CustomerMessages
{
    public static string Describe(Result<Customer?> result)
        => result.Match(customer => $"Customer {customer.Name}.",
                        () => "No customer.",
                        errors => $"{errors.Count} error(s).");

    public static string Describe(Result result)
        => result.Match(() => "Done.", errors => errors.First().Message);
}
```

| Overload | Callbacks |
|---|---|
| On `Result` | `onSuccess()`, `onFailed(errors)` |
| On `Result<T>` | `onSuccess(value)`, `onFailed(errors)`; the value can be `null` |
| On `Result<T?>` | `onSuccessNotNull(value)`, `onSuccessNull()`, `onFailed(errors)`; the value passed is not `null` |

Exactly one callback is called. `Match` exists on `Task` and `ValueTask` results too, and `MatchAsync` takes
callbacks returning a `Task`. Use the overload with `onSuccessNull` when the value is declared nullable,
such as `Result<Customer?>`; for a value declared not nullable, the overload with a single success callback
avoids a nullable warning.

The type produced is inferred from the callbacks. When they return different types, for instance
`OkObjectResult` and `ObjectResult` for an `ActionResult`, give it explicitly:
`result.Match<Order, ActionResult>(order => new OkObjectResult(order), _ => new ObjectResult(problem))`.
`Arc4u.AspNetCore.Results` builds its HTTP responses this way.

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
`await` chain. It logs only when the result failed, through the logger given to `Result.Setup`, which `AddResultLogger()` does for you
(see [Configuration](index.md#configuration)).

<xref:Arc4u.Results.Logging.FluentLogger> is the Arc4u implementation of that logger. For a failed result it logs each error on its own:

| Error | Logged as |
|---|---|
| `ValidationError` | At the level matching its `Severity` (`Error`, `Warning`, `Info`), with its code in a `Code` property (empty when no code was set) |
| `IExceptionalError` | The exception, with `LogException` |
| Any other error, including `ProblemDetailError` | The level passed to `LogIfFailed` (`Error` when omitted) |

The informational reasons of the result (not the errors) are then logged once, at information level.

The level passed to `LogIfFailed` is also used for the optional `content` line, logged when you call the FluentResults
overload `LogIfFailed(context, content, logLevel)` with a non-empty context and content. A `ValidationError` keeps the
level of its own `Severity`, and an exception is always logged at error level.

`FluentLogger` needs the Arc4u `ILogger<T>`; register it with `AddResultLogger()` as shown in the [overview](index.md#code).

## Extensibility points

Implement `FluentResults.IResultLogger` and pass it to `Result.Setup` to log results in your own way.
Use `FluentLogger` as the model: it receives the `ResultBase` and decides what to log.

## Troubleshooting

### Compile error `CS0619: 'ResultExtension.OnSuccess(Result, Func<Task>)' is obsolete`

You passed an asynchronous lambda to a method that is not named `*Async`. Rename the call to
`OnSuccessAsync` (or `OnSuccessNotNullAsync`, `OnSuccessNullAsync`, `OnFailedAsync`) and await the chain.
The `OnFailedAsync` callback has one parameter, the collection of errors: `OnFailedAsync(async errors => ...)`.

### Compile error `CS0619: ... This callback returns a result`

The callback of `OnSuccess`, `OnSuccessNull`, `OnSuccessNotNull`, `OnFailed` or one of their `*Async` twins
returns a `Result` or a `Result<T>`, which the method would discard. Pick the method matching what the step does:

- It produces the next value or result: `Then` (`ThenAsync` for an asynchronous step).
- It can fail but the value must be kept: `Check` (`CheckAsync` for an asynchronous step).
- It cleans up after a failure and can fail itself: `Compensate`.
- It replaces the failure with another result: `Recover`.
- The result really does not matter, as in `errors => global.WithErrors(errors)`: write a statement
  lambda, `errors => { global.WithErrors(errors); }`, or use `OnFailed(global)`.

### `OnSuccess(() => throw ...)` gives CS0121 or CS0619

A lambda that only throws converts to every delegate type, so it matches the obsolete overloads too, and the
call is reported as ambiguous. Cast it: `OnSuccess((Action)(() => throw new InvalidOperationException()))`.

### The method `OnFailed(action, result)` does not exist

Some older samples passed the callback and the global result in the same call. The two forms are separate
overloads: `OnFailed(errors => ...)` and `OnFailed(globalResult)`. Chain them.

## See also

- [Results to HTTP responses](http-mapping.md)
- [Validation errors](validation.md)
- [FluentResults documentation](https://github.com/altmann/FluentResults)
- <xref:Arc4u.Results.ResultExtension> in the API reference
