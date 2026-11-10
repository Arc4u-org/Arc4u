# Arc4u.Results

Fluent `OnSuccess` / `OnFailed` / `LogIfFailed` chains for FluentResults, steps that can fail (`Then`, `EnsureNotNull`, `Ensure`, `Compensate`, `Recover`), `Match` to end a chain, plus `ProblemDetailError`, `ValidationError` and a result logger, for the business layer of an Arc4u application.

## Install

```bash
dotnet add package Arc4u.Results
```

## Usage

`OnSuccess` and `OnFailed` run an action that cannot fail and return the result unchanged:

```csharp
using Arc4u.Results;
using FluentResults;

Result result = await SaveAsync()
    .OnSuccess(() => Console.WriteLine("Saved."))
    .LogIfFailed()
    .OnFailed(errors => Console.WriteLine($"{errors.Count} error(s)."));

static Task<Result> SaveAsync() => Task.FromResult(Result.Fail(ProblemDetailError.Create("Not saved.").WithStatusCode(409)));
```

A step that returns a result goes through `Then` or the other steps, which keep the errors of every step and stop at the first failure:

```csharp
using Arc4u.Results;
using FluentResults;

Task<Result> UpdateAsync(Guid id, string name)
    => repository.GetByIdAsync(id)                                                         // Task<Result<Template?>>
                 .EnsureNotNull(() => ProblemDetailError.Create($"No template {id}.").WithStatusCode(404))
                 .Ensure(template => template.Rename(name))                                // Result: the template is kept
                 .ThenAsync(template => repository.UpdateAsync(template))                  // Task<Result>
                 .LogIfFailed();
```

Passing a callback that returns a result to `OnSuccess` or `OnFailed` is a compile error, because its errors would be discarded.

`Match` ends a chain by turning the result into another type, from the callback matching its state:

```csharp
string message = await UpdateAsync(id, name)
    .Match(() => "Template updated.", errors => $"Not updated: {errors.First().Message}");
```

## Documentation

- Guide: [Results and errors](https://arc4u-org.github.io/Arc4u/guides/results/)
- API reference: [Arc4u.Results](https://arc4u-org.github.io/Arc4u/api/Arc4u.Results.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
