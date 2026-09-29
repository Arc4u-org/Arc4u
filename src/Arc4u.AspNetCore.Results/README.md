# Arc4u.AspNetCore.Results

Turns a FluentResults `Result` or `Result<T>` into a controller `ActionResult`, a minimal API `IResult` or a typed `Results<...>`, and every failure into a `ProblemDetails` with the right status code.

## Install

```bash
dotnet add package Arc4u.AspNetCore.Results --prerelease
```

## Usage

```csharp
using Arc4u.AspNetCore.Results;
using FluentResults;

var app = WebApplication.CreateBuilder(args).Build();

// 200 with the value, or a ProblemDetails (400, 422, 500 or the status of your ProblemDetailError).
app.MapGet("/orders/{id:int}", (int id, IOrders orders) => orders.GetAsync(id).ToHttpOkResultAsync());

public interface IOrders { Task<Result<string>> GetAsync(int id); }
```

## Documentation

- Guide: [Results and errors](https://arc4u-org.github.io/Arc4u/guides/results/)
- API reference: [Arc4u.AspNetCore.Results](https://arc4u-org.github.io/Arc4u/api/Arc4u.AspNetCore.Results.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
