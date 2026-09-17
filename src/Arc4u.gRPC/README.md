# Arc4u.gRPC

Core Framework to use gRPC.

## Reading a failed call as a Result

A service built with Arc4u.AspNetCore.gRpc sends the `ProblemDetails` of a failure along with the gRPC status.
Turn the fault back into a failed `Result`:

```csharp
using Arc4u.gRPC.Results;

try
{
    var response = await client.GetAsync(request, cancellationToken: cancellationToken);
    return Result.Ok(response.ToDomain());
}
catch (RpcException e)
{
    return e.ToResult<EnvironmentInfo>();
}
```

Rendered with `ToProblemDetails()`, the result gives back the document the service produced - validation messages
included. A fault carrying no ProblemDetails, such as a transport failure that never reached the service, is
mapped from its gRPC status alone (`Unavailable` → 503, `DeadlineExceeded` → 504...). When the service had to
leave validation messages out, a last warning says how many.

## The arc4u.grpc.v1 contract

The document is the `arc4u.grpc.v1.ProblemDetails` message, packed in the details of the `google.rpc.Status`
carried by the `grpc-status-details-bin` trailer. Its proto file is:

- in this repository: `src/Arc4u.gRPC/Protos/arc4u/grpc/v1/problem_details.proto`;
- in the package: `content/protos/arc4u/grpc/v1/problem_details.proto`.

A client in another language generates its code from that file and reads the detail with its own helper:
`StatusProto.fromThrowable` (Java), `status.FromError(err).Details()` (Go), `rpc_status.from_call` (Python).

A .NET application never compiles it: the message is compiled in this assembly. When one of its own protos needs
the message - a ProblemDetails per item of a stream, for instance - it just imports it, the package puts the file
on the Grpc.Tools import path:

```proto
import "arc4u/grpc/v1/problem_details.proto";

message ImportLineResult {
  oneof result {
    string order_id = 1;
    arc4u.grpc.v1.ProblemDetails problem = 2;
  }
}
```
