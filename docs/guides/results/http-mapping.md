---
description: "Convert a Result into a controller ActionResult, a minimal API IResult or a typed result, and know which status code and ProblemDetails each error produces."
---
# Results to HTTP responses

`Arc4u.AspNetCore.Results` converts a `Result` or `Result<T>` into the response of an endpoint,
so that every action does not repeat the same success and failure handling. This page lists the
methods for controllers and minimal APIs, the status code of every case, and the `ProblemDetails`
JSON your clients receive. Every response shown here was captured from a running application.
Read [Fluent result chains](fluent-results.md) first for the error types.

## What you would otherwise write

For each action you would return a success status, a `ProblemDetails` with a `4xx` status for a
failure the caller can act on, and a generic `500` when an exception happened:

```csharp
ActionResult response = new BadRequestResult();
var result = await bl.DeleteAsync(id, cancellationToken);
result
    .OnSuccess(() => response = new NoContentResult())
    .OnFailed(_ => response = new ObjectResult(result.ToProblemDetails()));
return response;
```

The extension methods do exactly this. The same action becomes:

```csharp
return await bl.DeleteAsync(id, cancellationToken).ToActionOkResultAsync();
```

## Choose the method for your endpoint

Three families exist, one per return type. Each has an `Ok` method (200 or 204) and a `Created` method (201),
and each comes in a synchronous form (on `Result`) and an `Async` form (on `Result`, `Task<Result>` and
`ValueTask<Result>`, with the generic equivalents).

| Endpoint style | Return type | Ok | Created | Class |
|---|---|---|---|---|
| Controller action | `ActionResult` or `ActionResult<T>` | `ToActionOkResult`, `ToActionOkResultAsync` | `ToActionCreatedResult`, `ToActionCreatedResultAsync` | <xref:Arc4u.AspNetCore.Results.FromResultToActionResultExtension> |
| Minimal API, untyped | `IResult` | `ToHttpOkResult`, `ToHttpOkResultAsync` | `ToHttpCreatedResult`, `ToHttpCreatedResultAsync` | <xref:Arc4u.AspNetCore.Results.FromResultToHttpResultExtension> |
| Minimal API, typed | `Results<..., ProblemHttpResult, ValidationProblem>` | `ToTypedOkResult`, `ToTypedOkResultAsync` | `ToTypedCreatedResult`, `ToTypedCreatedResultAsync` | <xref:Arc4u.AspNetCore.Results.FromResultToTypedResultExtension> |

# [Controllers](#tab/controllers)

```csharp
using Arc4u.AspNetCore.Results;
using Arc4u.Results;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

public record OrderDto(int Id, string Name);

public interface IOrderService
{
    Task<Result<OrderDto>> GetAsync(int id);
    Task<Result> DeleteAsync(int id);
}

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderService orders) : ControllerBase
{
    [HttpGet("{id:int}")]
    public Task<ActionResult<OrderDto>> Get(int id) => orders.GetAsync(id).ToActionOkResultAsync();

    [HttpDelete("{id:int}")]
    public Task<ActionResult> Delete(int id) => orders.DeleteAsync(id).ToActionOkResultAsync();
}
```

# [Minimal APIs (IResult)](#tab/minimal-apis)

```csharp
// Program.cs (excerpt)
using Arc4u.AspNetCore.Results;

app.MapGet("/orders/{id:int}", (int id, IOrderService orders) => orders.GetAsync(id).ToHttpOkResultAsync());
app.MapDelete("/orders/{id:int}", (int id, IOrderService orders) => orders.DeleteAsync(id).ToHttpOkResultAsync());
```

# [Minimal APIs (typed)](#tab/typed)

The declared return type must be exactly the one of the method, otherwise the lambda does not compile.

```csharp
// Program.cs (excerpt)
using Arc4u.AspNetCore.Results;
using Microsoft.AspNetCore.Http.HttpResults;

app.MapGet("/orders/{id:int}",
    Task<Results<Ok<OrderDto>, ProblemHttpResult, ValidationProblem>> (int id, IOrderService orders)
        => orders.GetAsync(id).ToTypedOkResultAsync());

app.MapDelete("/orders/{id:int}",
    Task<Results<NoContent, ProblemHttpResult, ValidationProblem>> (int id, IOrderService orders)
        => orders.DeleteAsync(id).ToTypedOkResultAsync());
```

---

Typed results describe the possible responses to OpenAPI. The library always returns failures as
a `ProblemHttpResult`; `ValidationProblem` is part of the type but is never the runtime type.

## How successes map

| Result | Method | Status | Body |
|---|---|---|---|
| `Result` succeeded | `To...OkResult` | `204 No Content` | None |
| `Result<T>` succeeded with a value | `To...OkResult` | `200 OK` | The value, or the value returned by the mapper |
| `Result<T>` succeeded with `null` | `To...OkResult` | Depends on the endpoint style, see [null values](#null-values) | |
| `Result` succeeded | `To...CreatedResult(location)` | `201 Created`, `Location` header when a location is passed | None |
| `Result<T>` succeeded with a value | `To...CreatedResult(location)` | `201 Created`, `Location` header | The value, or the value returned by the mapper |
| `Result<T>` succeeded with `null` | `To...CreatedResult(location)` | `201 Created`, no `Location` header | Empty |

For a `Result` (without a value), the `To...CreatedResultAsync` methods (not the synchronous ones) have a type parameter
that the compiler cannot infer and that is not used. Write `result.ToActionCreatedResultAsync<Order>(location)` with any type.

### Map the value

Every method with a value has an overload taking a mapper, so that the business type does not leave the API.
Use any mapper: a lambda, AutoMapper, Mapster. There is an asynchronous form too (`Func<TResult, Task<T>>`).
The mapper is not called when the value is `null`.

```csharp
public record OrderSummary(string Name);

// in the controller of the previous sample
[HttpGet("{id:int}/summary")]
public Task<ActionResult<OrderSummary>> Summary(int id)
    => orders.GetAsync(id).ToActionOkResultAsync(order => new OrderSummary(order.Name));
```

### Null values

A `Result<T>` can succeed with a `null` value, for example a query that finds nothing. What the client
sees depends on the endpoint style:

| Endpoint style | Response |
|---|---|
| Controller | `204 No Content` with an empty body. `OkObjectResult` with a `null` value is written as `204` by MVC's `HttpNoContentOutputFormatter`. |
| Minimal API (`IResult` or typed) | `200 OK` with an empty body |

Some generated clients (NSwag, for example) treat a `204` on an operation declared to return a value as an
error. To answer `200` with the JSON `null` from controllers, remove the formatter:

```csharp
using Microsoft.AspNetCore.Mvc.Formatters;

builder.Services.AddControllers(options => options.OutputFormatters.RemoveType<HttpNoContentOutputFormatter>());
```

If you prefer an explicit answer, handle the case before converting: `OnSuccessNull` sets what you want, or
return a `ProblemDetailError` with status `404` when a missing entity is an error.

### Return 201 Created with a Location

The `Location` depends on the value the business layer returns (the new identifier). Compute it in
`OnSuccessNotNull`, then convert in a second step. If you wrote the whole chain in one expression, the URI would be
evaluated before the result exists.

```csharp
using Arc4u.AspNetCore.Results;
using Arc4u.Results;
using Microsoft.AspNetCore.Mvc;

// in a controller with an action named Get
[HttpPost]
public async Task<ActionResult<OrderDto>> Create(OrderDto dto)
{
    Uri? location = null;

    var result = await orders.CreateAsync(dto)
                             .OnSuccessNotNull(created => location = new Uri(Url.ActionLink("Get", "Orders", new { id = created.Id })!))
                             .ConfigureAwait(false);

    return await result.ToActionCreatedResultAsync(location).ConfigureAwait(false);
}
```

`OnSuccessNotNull` is used because `OnSuccess` also runs when the value is `null`. `IOrderService.CreateAsync` returns
`Task<Result<OrderDto>>`. A minimal API uses the same two steps:

```csharp
// Program.cs (excerpt)
app.MapPost("/orders", async (OrderDto dto, IOrderService orders) =>
{
    Uri? location = null;

    var result = await orders.CreateAsync(dto);
    result.OnSuccessNotNull(created => location = new Uri($"/orders/{created.Id}", UriKind.Relative));

    return await result.ToHttpCreatedResultAsync(location);
});
```

## How errors map to a response

A failed result is converted by `ToProblemDetails`, which looks at the errors in this order and stops at the first match:

| # | The result contains | Status | `type` | Body |
|---|---|---|---|---|
| 1 | An `IExceptionalError` (an exception) | `500` | `.../StatusCodes#unexpected-error` | Generic message with the activity id. Only the first exception is used. The exception is logged. |
| 2 | At least one `ValidationError` | `422` | `.../StatusCodes#validation-error` | `ValidationProblemDetails`, messages grouped by severity. Other errors are ignored. |
| 3 | At least one `ProblemDetailError` | Its `StatusCode`, or `500` | Its `Type`, or `about:blank` | Its title, detail, instance, severity and metadata. Only the first is used. |
| 4 | Any other error | `400` | `about:blank` | Title `Error.`, detail is the message of the first error. |

Every response is served as `application/problem+json` (controllers add `; charset=utf-8`; the JSON blocks below show
the controller headers). Requests with a successful result never go through
this mapping.

The `type` URIs of rows 1 and 2 are constants in the library that point to the page
`https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes` of the Arc4u wiki. Treat them as identifiers. The `expected-error` URI is used only by the `ToGenericMessage` overload that honors
`unexpectedType: false`. Keep the wiki page (or at least its headings) available while responses carry these URIs.

### Unexpected error: an exception

`500`, no exception detail in the response. The `detail` carries the W3C activity id (`Activity.Current.Id`)
that the support team searches in the logs:

```http
HTTP/1.1 500 Internal Server Error
Content-Type: application/problem+json; charset=utf-8

{"type":"https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#unexpected-error","title":"A technical error occured!","status":500,"detail":"Contact the application owner. A message has been logged with id: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-00."}
```

The exception is written to the log only if you registered the result logger (see [Configuration](index.md#configuration)).
The typo "occured" is part of the current message.

### Business problem: ProblemDetailError

The title, status, type, instance, severity and metadata you set are copied. Metadata entries become
top-level members. The severity is always present:

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json; charset=utf-8

{"type":"https://example.com/problems/not-found","title":"Not found","status":404,"detail":"Order 2 does not exist.","instance":"/orders/2","Severity":"Error","orderId":2}
```

With only `Create("no status set")`, the response is a `500` with `"type":"about:blank"` and `"title":"Error."`.

### Validation error

Status `422` with a `ValidationProblemDetails`. The `errors` member groups the messages by severity name; the keys are sorted
alphabetically (`Error`, `Info`, `Warning`). The error codes are not part of the response:

```http
HTTP/1.1 422 Unprocessable Entity
Content-Type: application/problem+json; charset=utf-8

{"type":"https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#validation-error","title":"Error from validation.","status":422,"errors":{"Error":["'Name' must not be empty.","'Quantity' must be greater than '0'."]}}
```

A result that contains only warnings is still failed, so it also answers `422`. See [Validation errors](validation.md).

### Other error

An error that is neither an exception, a `ValidationError` nor a `ProblemDetailError`, for example
`Result.Fail("plain FluentResults error")`, gives `400`:

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json; charset=utf-8

{"type":"about:blank","title":"Error.","status":400,"detail":"plain FluentResults error","Severity":"Error"}
```

### Unauthorized and forbidden

The result mapping never produces `401` or `403` by itself. Authentication and authorization are done by the
ASP.NET Core middleware before your endpoint runs (see the [server authentication guide](../authentication-server/index.md)).
To report a denied action from the business layer, return a `ProblemDetailError` with `WithStatusCode(403)`.

## The generic 500 message

`ToGenericMessage` builds the `500` (or `400`) generic body. `ToProblemDetails` calls it for exceptions. It also
calls `LogIfFailed` on the result.

| Overload | Honors `unexpectedType` |
|---|---|
| `ToGenericMessage(Result, bool)` | No |
| `ToGenericMessage(Result, string? activityId, bool)` | Yes |
| `ToGenericMessage<T>(Result<T>, bool)` | No |
| `ToGenericMessage<T>(Result<T>, string? activityId, bool)` | No |

> [!WARNING]
> Known issue: only `ToGenericMessage(Result, string?, bool)` uses `unexpectedType`. With the other three
> overloads you always get the "unexpected" `500` message, whatever value you pass. With `unexpectedType: false`
> the honored overload returns `400` and the `expected-error` type.

Calling `ToProblemDetails` on a result that succeeded does not make sense. The library adds an `ExceptionalError`
to that result, which is then failed, and returns the generic `500`, so that the mistake is visible.

## Change the mapping

`FromResultToProblemDetailExtension.SetFromErrorFactory` replaces the function that converts errors to a
`ProblemDetails`, for all endpoints and for the whole process. Call it once at startup. Your function receives the
errors of the failed result and returns the `ProblemDetails`; set its `Status`, because the response status code is
read from `ProblemDetails.Status`.

The default rules are not reusable from inside your function. This sample keeps the exception protection by calling
`ToGenericMessage`, and maps a `ProblemDetailError` itself:

```csharp
using System.Diagnostics;
using Arc4u.AspNetCore.Results;
using Arc4u.Results;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

FromResultToProblemDetailExtension.SetFromErrorFactory(errors =>
{
    if (errors.OfType<IExceptionalError>().Any())
    {
        return Result.Fail(errors).ToGenericMessage(Activity.Current?.Id, true);
    }

    var error = errors.First();
    return new ProblemDetails()
        .WithTitle(error is ProblemDetailError { Title: not null } p ? p.Title : "Request failed")
        .WithDetail(error.Message)
        .WithStatusCode(error is ProblemDetailError { StatusCode: not null } s ? s.StatusCode.Value : StatusCodes.Status400BadRequest);
});
```

> [!CAUTION]
> Do not call `FromError` from inside your factory to reuse the default mapping. `FromError` calls the factory that is
> currently registered, which is yours, so the call recurses until the process dies with a stack overflow. Capturing
> `FromError` in a variable before calling `SetFromErrorFactory` does not help: the value is a delegate that reads the
> current factory when it runs.

### Build a ProblemDetails by hand

`ProblemDetailsExtensions` adds fluent methods to `Microsoft.AspNetCore.Mvc.ProblemDetails`. They are what the
default factory uses.

| Method | Sets |
|---|---|
| `WithTitle(string)`, `WithDetail(string)` | `Title`, `Detail` |
| `WithStatusCode(int)` | `Status` |
| `WithType(Uri)` | `Type` (as a string) |
| `WithInstance(string?)` | `Instance`, ignored when empty |
| `WithCode(string)`, `WithSeverity(string)` | Extension members `Code` and `Severity`, ignored when empty |
| `WithMetadata(string key, object value)` | An extension member, replaced when it exists |

```csharp
using Arc4u.AspNetCore.Results;
using Arc4u.Results.Validation;
using Microsoft.AspNetCore.Mvc;

var problem = new ProblemDetails()
    .WithTitle("Some title")
    .WithDetail("Some detail")
    .WithStatusCode(StatusCodes.Status400BadRequest)
    .WithType(new Uri("about:blank"))
    .WithSeverity(Severity.Error.ToString());
```

## Troubleshooting

### I get a 500 where I expected a 4xx

The error is a `ProblemDetailError` without `WithStatusCode`. Its default status is `500`.

### My other errors are not in the response

The mapping stops at the first matching row. If a result contains a `ValidationError` and a `ProblemDetailError`,
only the validation errors are returned; with an exception, only a generic `500` is returned. Return one kind of
error per result.

### The response says "A message has been logged" but I cannot find it

The result logger is not configured. See [Configuration](index.md#configuration).

### A controller returns 204 when the value is null

That is ASP.NET Core's default, see [null values](#null-values).

## See also

- [Results and errors](index.md)
- [Fluent result chains](fluent-results.md)
- [Validation errors](validation.md)
- <xref:Arc4u.AspNetCore.Results> in the API reference
