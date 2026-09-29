# Arc4u.Configuration

Application-wide settings for Arc4u: `ApplicationConfig` (application name, environment, time zone) and helpers to read key/value settings from `IConfiguration`.

## Install

```bash
dotnet add package Arc4u.Configuration --prerelease
```

## Usage

```csharp
using Arc4u.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Reads the "Application.Configuration" section.
builder.Services.AddApplicationConfig(builder.Configuration);
```

## Documentation

- Guide: [Configuration](https://arc4u-org.github.io/Arc4u/guides/configuration/)
- API reference: [Arc4u.Configuration](https://arc4u-org.github.io/Arc4u/api/Arc4u.Configuration.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
