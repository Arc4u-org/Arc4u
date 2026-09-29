# Arc4u.Diagnostics.Serilog

Serilog integration for the Arc4u logging: a one-line text formatter, an anonymizer sink, helpers to read the Arc4u properties of a Serilog event, and a base class for Serilog based log writers.

## Install

```bash
dotnet add package Arc4u.Diagnostics.Serilog --prerelease
```

## Usage

```csharp
builder.Services.AddILogger();

builder.Host.UseSerilog((context, configuration) => configuration
    .MinimumLevel.Information()
    .WriteTo.Console(new SimpleTextFormatter()));
```

`UseSerilog` comes from the `Serilog.AspNetCore` package; `SimpleTextFormatter` is in `Arc4u.Diagnostics.Formatter`.

## Documentation

- Guide: [Serilog](https://arc4u-org.github.io/Arc4u/guides/diagnostics/serilog.html)
- API reference: [Arc4u.Diagnostics.Serilog](https://arc4u-org.github.io/Arc4u/api/Arc4u.Diagnostics.Serilog.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
