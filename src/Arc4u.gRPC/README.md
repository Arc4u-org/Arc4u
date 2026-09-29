# Arc4u.gRPC

gRPC interceptors and helpers for Arc4u: forward the caller's bearer token and culture, and read a failed call as a failed `Result` carrying a ProblemDetails.

## Install

```bash
dotnet add package Arc4u.gRPC --prerelease
```

## Usage

```csharp
using Arc4u.gRPC.Results;

try
{
    var reply = await client.GetOrderAsync(request, cancellationToken: cancellationToken);
}
catch (RpcException e)
{
    var failed = e.ToResult<OrderReply>(); // a failed FluentResults Result<OrderReply>
}
```

## Documentation

- Guide: [gRPC and API versioning](https://arc4u-org.github.io/Arc4u/guides/grpc-versioning/)
- API reference: [Arc4u.gRPC](https://arc4u-org.github.io/Arc4u/api/Arc4u.gRPC.html)
- Source and issues: [github.com/Arc4u-org/Arc4u](https://github.com/Arc4u-org/Arc4u)
