# Arc4u.AspNetCore.gRpc

ASP.NET Core support for hosting gRPC services with Arc4u: report a failed `Result` as an `RpcException` carrying a ProblemDetails, return 401 instead of a redirect, and log call durations.

## Install

```bash
dotnet add package Arc4u.AspNetCore.gRpc --prerelease
```

## Usage

```csharp
using Arc4u.AspNetCore.gRpc.Results;

if (result.IsFailed)
{
    throw result.ToRpcException();
}
```

`result` is a failed FluentResults `Result`; the exception carries the same ProblemDetails a REST endpoint would return.

## Documentation

- Guide: [gRPC and API versioning](https://arc4u-org.github.io/Arc4u/guides/grpc-versioning/)
- API reference: [Arc4u.AspNetCore.gRpc](https://arc4u-org.github.io/Arc4u/api/Arc4u.AspNetCore.gRpc.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
