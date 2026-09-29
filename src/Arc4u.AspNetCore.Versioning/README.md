# Arc4u.AspNetCore.Versioning

One API versioning convention for every Arc4u REST service, on top of Asp.Versioning: the version is read from the URL segment, the `x-api-version` header or the `api-version` query string, and it is mandatory.

## Install

> This package is not published on NuGet.org yet. Reference the project from the repository until a preview is available.

```bash
dotnet add package Arc4u.AspNetCore.Versioning --prerelease
```

## Usage

```csharp
using Arc4u.AspNetCore.Versioning;

builder.Services.AddServiceApiVersioning();
```

The package targets `net10.0`.

## Documentation

- Guide: [gRPC and API versioning](https://arc4u-org.github.io/Arc4u/guides/grpc-versioning/)
- API reference: [Arc4u.AspNetCore.Versioning](https://arc4u-org.github.io/Arc4u/api/Arc4u.AspNetCore.Versioning.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
