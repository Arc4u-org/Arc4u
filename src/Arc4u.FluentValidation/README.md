# Arc4u.FluentValidation

FluentValidation extensions that return a FluentResults `Result<T>` holding `ValidationError`s, which `Arc4u.AspNetCore.Results` answers as HTTP `422`.

## Install

```bash
dotnet add package Arc4u.FluentValidation --prerelease
```

## Usage

```csharp
using Arc4u.Validation;
using FluentValidation;

var validator = new InlineValidator<string>();
validator.RuleFor(name => name).NotEmpty();

// A failed Result<string> holding one ValidationError per failure.
var result = validator.ValidateWithResult("");
```

## Documentation

- Guide: [Results and errors](https://arc4u-org.github.io/Arc4u/guides/results/)
- API reference: [Arc4u.FluentValidation](https://arc4u-org.github.io/Arc4u/api/Arc4u.FluentValidation.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
