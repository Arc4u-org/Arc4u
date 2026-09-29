# Arc4u.Results

Fluent `OnSuccess` / `OnFailed` / `LogIfFailed` chains for FluentResults, plus `ProblemDetailError`, `ValidationError` and a result logger, for the business layer of an Arc4u application.

## Install

```bash
dotnet add package Arc4u.Results --prerelease
```

## Usage

```csharp
using Arc4u.Results;
using FluentResults;

Result result = await SaveAsync()
    .OnSuccess(() => Console.WriteLine("Saved."))
    .LogIfFailed()
    .OnFailed(errors => Console.WriteLine($"{errors.Count} error(s)."));

static Task<Result> SaveAsync() => Task.FromResult(Result.Fail(ProblemDetailError.Create("Not saved.").WithStatusCode(409)));
```

## Documentation

- Guide: [Results and errors](https://arc4u-org.github.io/Arc4u/guides/results/)
- API reference: [Arc4u.Results](https://arc4u-org.github.io/Arc4u/api/Arc4u.Results.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
