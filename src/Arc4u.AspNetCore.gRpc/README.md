# Arc4u.AspNetCore.gRpc

Core framework to integrate gRpc in asp net core.

## Reporting a failed Result

A gRPC service reports a failed `Result` by throwing the `RpcException` built from it:

```csharp
using Arc4u.AspNetCore.gRpc.Results;

var result = await useCase.ExecuteAsync(context.CancellationToken);

if (result.IsFailed)
{
    throw result.ToRpcException();
}
```

The exception is derived from the very `ProblemDetails` a REST endpoint returns for the same failure
(`ToProblemDetails()`), so both transports report it identically:

- the gRPC status code is the closest counterpart of the HTTP status (404 → `NotFound`, 422 and 400 →
  `InvalidArgument`, 500 → `Internal`...), and the status detail is the `Detail` of the ProblemDetails;
- the `ProblemDetails` itself is sent following the gRPC richer error model: packed as an
  `arc4u.grpc.v1.ProblemDetails` in the `google.rpc.Status` of the `grpc-status-details-bin` trailer. Every gRPC
  language can read it - see the Arc4u.gRPC package for the contract.

gRPC clients cap the metadata of a response (8 KiB by default in Java and in the C core behind Python), so the
trailer is kept within 6 KiB: when a validation failure has too many messages, the least severe ones are left out
and their number is sent in `omitted_errors`.
