---
description: "Report invalid input with ValidationError, validate with FluentValidation into a Result, and understand the 422 response."
---
# Validation errors

Input validation is a failure the caller can fix, so it is not an exception. `Arc4u.Results` defines
`ValidationError` for it, `Arc4u.FluentValidation` turns [FluentValidation](https://docs.fluentvalidation.net/) failures into
`ValidationError`s, and `Arc4u.AspNetCore.Results` answers `422 Unprocessable Entity` with a
`ValidationProblemDetails`. This page is part of the [results guide](index.md); the error types are introduced in
[Fluent result chains](fluent-results.md).

## Create a ValidationError

<xref:Arc4u.Results.Validation.ValidationError> is created with `Create` and completed with `With...` methods.

| Member | Type | Default | Description |
|---|---|---|---|
| `Create(errorMessage)` | `string` | none (required) | The message returned to the client. |
| `WithSeverity` / `Severity` | <xref:Arc4u.Results.Validation.Severity> | `Error` | `Error`, `Warning` or `Info`. |
| `WithCode` / `Code` | `string` | Empty string | A code for your logs. It is not part of the HTTP response. |
| `WithMetadata(key, value)` | `string`, `object` | none | Adds metadata. An existing key is kept: the call is then ignored. |

`ValidationError` converts implicitly to a failed `Result`. The `WithValidationError` extensions add one to an
existing `Result` or `Result<T>`, with an optional code and severity:

```csharp
using Arc4u.Results.Validation;
using FluentResults;

public static class Checks
{
    public static Result<int> Quantity(int quantity)
    {
        var result = Result.Ok(quantity);

        if (quantity <= 0)
        {
            result = result.WithValidationError("The quantity must be positive.", "QTY_POSITIVE");
        }

        if (quantity > 100)
        {
            result = result.WithValidationError("Large order, please confirm.", Severity.Warning);
        }

        return result;
    }
}
```

Adding an error fails the result, whatever its severity: a result that only holds a warning is failed.

## The HTTP response

Endpoints converted with the methods of [Results to HTTP responses](http-mapping.md) answer `422` when the result
holds at least one `ValidationError`. The `errors` member groups the messages by the name of their severity:

```http
HTTP/1.1 422 Unprocessable Entity
Content-Type: application/problem+json; charset=utf-8

{"type":"https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#validation-error","title":"Error from validation.","status":422,"errors":{"Error":["Id 3 is not valid."],"Warning":["Something to check."]}}
```

The keys are `Error`, `Warning` and `Info`, not the names of the properties that failed. The code and the metadata of a
`ValidationError` are not returned. If your clients need the property name, build the `ProblemDetails` yourself with
[`SetFromErrorFactory`](http-mapping.md#change-the-mapping).

## Validate with FluentValidation

`Arc4u.FluentValidation` (assembly `Arc4u.Validation`) adds extension methods to `IValidator<T>` in the
`Arc4u.Validation` namespace. They return a `Result<T>`: successful and holding the validated value, or failed with
one `ValidationError` per FluentValidation failure.

| Method | Returns |
|---|---|
| `ValidateWithResultAsync(value)` | `ValueTask<Result<T>>` |
| `ValidateWithResultAsync(value, cancellationToken)` | `ValueTask<Result<T>>` |
| `ValidateWithResult(value)` | `Result<T>` |

The conversion of each `ValidationFailure`:

| FluentValidation | `ValidationError` |
|---|---|
| `ErrorMessage` | `Message` |
| `ErrorCode` | `Code`. Unless you call `WithErrorCode`, FluentValidation sets it to the name of the validator, for example `GreaterThanValidator`. |
| `Severity.Error` | `Severity.Error` |
| `Severity.Warning` | `Severity.Warning` |
| `Severity.Info` | `Severity.Info` |

`ToValidationError` and `ToResultErrors` do the same conversion on a `ValidationFailure` or a list of them, when you
run the validator yourself.

```csharp
using Arc4u.Validation;
using FluentResults;
using FluentValidation;

public record NewOrder(string Name, int Quantity);

public class NewOrderValidator : AbstractValidator<NewOrder>
{
    public NewOrderValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithErrorCode("NAME_REQUIRED");
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class OrderService(IValidator<NewOrder> validator)
{
    public async Task<Result<NewOrder>> CreateAsync(NewOrder order, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateWithResultAsync(order, cancellationToken);

        return validation.IsFailed ? validation : Result.Ok(order);
    }
}
```

Register validators with FluentValidation's own mechanisms, for example
`builder.Services.AddScoped<IValidator<NewOrder>, NewOrderValidator>()`. The controller returns the result with
`ToActionCreatedResultAsync` or any other method of the [mapping page](http-mapping.md); an invalid `NewOrder`
answers `422`.

> [!NOTE]
> `Arc4u.Results.Validation.Severity` and `FluentValidation.Severity` have the same name. A file that imports both
> namespaces must qualify the one it uses.

## Rules for persisted entities

`Arc4u.FluentValidation` also contains rules for entities that carry a `PersistChange` (the `IPersistEntity` interface
of `Arc4u`, used by the [data guide](../data/index.md)). They check that the state of the entity matches the operation
you are validating.

| Rule | Passes when the property is |
|---|---|
| `IsInsert()` | `PersistChange.Insert` |
| `IsUpdate()` | `PersistChange.Update` |
| `IsDelete()` | `PersistChange.Delete` |
| `IsNone()` | `PersistChange.None` |
| `IsUtcDateTime()` | A `DateTime` whose `Kind` is `Utc` |
| `IsDateOnly()` | A `DateTime` whose time of day is zero |

```csharp
using Arc4u.Data;
using Arc4u.Validation;
using FluentValidation;

public class CustomerInsertValidator : AbstractValidator<Customer>
{
    public CustomerInsertValidator()
    {
        RuleFor(x => x.PersistChange).IsInsert();
        RuleFor(x => x.CreatedOn).IsUtcDateTime();
    }
}

public class Customer : PersistEntity
{
    public DateTime CreatedOn { get; set; }
}
```

`ValidatorPredicates` in the namespace `Arc4u.FluentValidation` offers the same tests as predicates for
`When(...)`: `IsInsert`, `IsUpdate`, `IsDelete` and `IsNone`.

## Troubleshooting

### The `errors` object has `Error` and `Warning` keys, not property names

That is the current format, see [The HTTP response](#the-http-response).

### A warning makes the request fail

Every `ValidationError` fails the result, including warnings and `Info`. If a rule is only advisory, do not report
it through the result.

## See also

- [Results and errors](index.md)
- [Results to HTTP responses](http-mapping.md)
- [FluentValidation documentation](https://docs.fluentvalidation.net/)
- <xref:Arc4u.Results.Validation> and <xref:Arc4u.FluentValidation> in the API reference
