using Arc4u.AspNetCore.Results;
using FluentResults;
using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Any = Google.Protobuf.WellKnownTypes.Any;
using Mvc = Microsoft.AspNetCore.Mvc;
using Protos = Arc4u.gRPC.Protos;
using RpcStatus = Google.Rpc.Status;
using Severity = Arc4u.Results.Validation.Severity;

namespace Arc4u.AspNetCore.gRpc.Results;

/// <summary>
/// Turns a failed <see cref="Result"/> into the fault a gRPC service throws. Everything is derived from the very
/// ProblemDetails the REST endpoints return (ToProblemDetails()), so both transports report a failure identically.
/// The ProblemDetails itself travels along following the gRPC richer error model: packed as an
/// arc4u.grpc.v1.ProblemDetails in the google.rpc.Status of the grpc-status-details-bin trailer, which every gRPC
/// language has a helper to read. The .NET client-side counterpart is FromRpcExceptionToResultExtension in Arc4u.gRPC.
/// </summary>
public static class FromResultToRpcExceptionExtension
{
    /// <summary>
    /// Largest grpc-status-details-bin trailer sent, base64 encoding included. gRPC clients cap the metadata of a
    /// response - 8 KiB by default in Java and in the C core behind Python - and that budget also has to hold
    /// grpc-status and grpc-message. A document past it loses its least severe validation messages first.
    /// </summary>
    private const int MaxStatusDetailsLength = 6 * 1024;

    /// <summary>
    /// Converts a failed result into the <see cref="RpcException"/> to throw back to the caller.
    /// </summary>
    /// <remarks>
    /// As with ToProblemDetails(), converting a successful result is a programming error: it is reported as an
    /// unexpected failure.
    /// </remarks>
    /// <param name="result">The failed result to convert.</param>
    /// <returns>An <see cref="RpcException"/> carrying the gRPC equivalent of the result's ProblemDetails.</returns>
    public static RpcException ToRpcException(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return ToRpcException(result.ToProblemDetails());
    }

    /// <summary>
    /// Converts a failed result into the <see cref="RpcException"/> to throw back to the caller.
    /// </summary>
    /// <remarks>
    /// As with ToProblemDetails(), converting a successful result is a programming error: it is reported as an
    /// unexpected failure.
    /// </remarks>
    /// <typeparam name="TValue">The type carried by the result.</typeparam>
    /// <param name="result">The failed result to convert.</param>
    /// <returns>An <see cref="RpcException"/> carrying the gRPC equivalent of the result's ProblemDetails.</returns>
    public static RpcException ToRpcException<TValue>(this Result<TValue> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return ToRpcException(result.ToProblemDetails());
    }

    /// <summary>
    /// Converts the ProblemDetails of a failed result into the <see cref="RpcException"/> to throw.
    /// </summary>
    /// <param name="problem">The ProblemDetails describing the failure.</param>
    /// <returns>The fault to throw.</returns>
    private static RpcException ToRpcException(Mvc.ProblemDetails problem)
    {
        var code = ToStatusCode(problem.Status);

        var detail = problem.Detail ?? problem.Title ?? "The request failed.";

        // Null when not even the document stripped of its validation messages fits: the status alone still
        // reaches the caller.
        return ToRpcStatus(code, detail, ToProto(problem))?.ToRpcException()
               ?? new RpcException(new Status(code, detail));
    }

    /// <summary>
    /// Copies the ProblemDetails into its protobuf counterpart.
    /// </summary>
    /// <param name="problem">The ProblemDetails describing the failure.</param>
    /// <returns>The document to send, validation messages most severe first.</returns>
    private static Protos.ProblemDetails ToProto(Mvc.ProblemDetails problem)
    {
        var proto = new Protos.ProblemDetails
        {
            Type = problem.Type ?? string.Empty,
            Title = problem.Title ?? string.Empty,
            Status = problem.Status ?? 0,
            Detail = problem.Detail ?? string.Empty,
            Instance = problem.Instance ?? string.Empty,
        };

        if (problem is Mvc.ValidationProblemDetails validation)
        {
            // The errors of a validation document are keyed by severity, not by field, and sorted by key name.
            // Ordering them most severe first is what makes the truncation drop the least severe ones.
            proto.Errors.Add(validation.Errors
                                       .Select(group => (Severity: ToSeverity(group.Key), Messages: group.Value))
                                       .OrderBy(group => group.Severity)
                                       .SelectMany(group => group.Messages.Select(message => new Protos.ValidationMessage
                                       {
                                           Severity = group.Severity,
                                           Message = message,
                                       })));
        }

        return proto;
    }

    /// <summary>
    /// Builds the google.rpc.Status to send, keeping as many validation messages as the trailer can hold.
    /// </summary>
    /// <param name="code">The gRPC status code of the fault.</param>
    /// <param name="detail">The message of the fault.</param>
    /// <param name="problem">The complete document.</param>
    /// <returns>The status to send, or <c>null</c> when even the document without any message does not fit.</returns>
    private static RpcStatus? ToRpcStatus(StatusCode code, string detail, Protos.ProblemDetails problem)
    {
        var errors = problem.Errors.ToList();

        RpcStatus Build(int kept)
        {
            problem.Errors.Clear();
            problem.Errors.Add(errors.Take(kept));
            problem.OmittedErrors = errors.Count - kept;

            return new RpcStatus { Code = (int)code, Message = detail, Details = { Any.Pack(problem) } };
        }

        static bool Fits(RpcStatus status) => (status.CalculateSize() + 2) / 3 * 4 <= MaxStatusDetailsLength;

        var status = Build(errors.Count);

        if (Fits(status))
        {
            return status;
        }

        if (!Fits(Build(0)))
        {
            return null;
        }

        // The size only grows with the messages kept, so a binary search finds the most that fit without
        // serializing the document once per message.
        var fitting = 0;
        var overflowing = errors.Count;

        while (overflowing - fitting > 1)
        {
            var kept = fitting + ((overflowing - fitting) / 2);

            if (Fits(Build(kept)))
            {
                fitting = kept;
            }
            else
            {
                overflowing = kept;
            }
        }

        return Build(fitting);
    }

    /// <summary>
    /// Maps the key of a group of validation errors - the name of their <see cref="Severity"/> - to the severity
    /// of the contract.
    /// </summary>
    /// <param name="key">The key of the group in the validation document.</param>
    /// <returns>The severity of the messages in the group.</returns>
    private static Protos.Severity ToSeverity(string key)
    {
        // An unknown key is not worth downplaying: it counts as an error.
        return Enum.TryParse<Severity>(key, out var severity)
            ? severity switch
            {
                Severity.Warning => Protos.Severity.Warning,
                Severity.Info => Protos.Severity.Info,
                _ => Protos.Severity.Error,
            }
            : Protos.Severity.Error;
    }

    /// <summary>
    /// Maps an HTTP status code to its closest gRPC counterpart.
    /// </summary>
    /// <param name="httpStatusCode">The status code carried by the ProblemDetails, if any.</param>
    /// <returns>The gRPC status code to report.</returns>
    private static StatusCode ToStatusCode(int? httpStatusCode)
    {
        // Anything not listed - 500 included - is an Internal failure. A bare Result.Fail("...") carries no
        // ProblemDetailError and is rendered as a 400: it reports InvalidArgument, exactly as the REST endpoints
        // report 400 for the same failure. A validation failure is a 422: gRPC has no code of its own for it,
        // and it is the caller's mistake, so it reports InvalidArgument too - the ProblemDetails in the status
        // details carries the 422.
        return httpStatusCode switch
        {
            StatusCodes.Status400BadRequest => StatusCode.InvalidArgument,
            StatusCodes.Status401Unauthorized => StatusCode.Unauthenticated,
            StatusCodes.Status403Forbidden => StatusCode.PermissionDenied,
            StatusCodes.Status404NotFound => StatusCode.NotFound,
            StatusCodes.Status409Conflict => StatusCode.Aborted,
            StatusCodes.Status412PreconditionFailed => StatusCode.FailedPrecondition,
            StatusCodes.Status422UnprocessableEntity => StatusCode.InvalidArgument,
            StatusCodes.Status429TooManyRequests => StatusCode.ResourceExhausted,
            StatusCodes.Status501NotImplemented => StatusCode.Unimplemented,
            StatusCodes.Status503ServiceUnavailable => StatusCode.Unavailable,
            StatusCodes.Status504GatewayTimeout => StatusCode.DeadlineExceeded,
            _ => StatusCode.Internal,
        };
    }
}
