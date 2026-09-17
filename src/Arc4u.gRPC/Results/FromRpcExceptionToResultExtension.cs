using System.Globalization;
using System.Net;
using Arc4u.Results;
using Arc4u.Results.Validation;
using FluentResults;
using Google.Protobuf;
using Grpc.Core;
using Protos = Arc4u.gRPC.Protos;

namespace Arc4u.gRPC.Results;

/// <summary>
/// Turns the fault of a gRPC call back into a failed <see cref="Result"/>. It is the client-side counterpart of
/// FromResultToRpcExceptionExtension in Arc4u.AspNetCore.gRpc: the server packs the ProblemDetails of a failed
/// result in the google.rpc.Status of the grpc-status-details-bin trailer, and this extension turns it back into
/// the errors Arc4u builds that document from. Rendered with ToProblemDetails(), they give back the very document
/// the server produced.
/// </summary>
public static class FromRpcExceptionToResultExtension
{
    /// <summary>
    /// Converts a gRPC fault into a failed <see cref="Result"/>.
    /// </summary>
    /// <param name="exception">The fault raised by the gRPC client.</param>
    /// <returns>A failed <see cref="Result"/> carrying the ProblemDetails of the remote failure.</returns>
    public static Result ToResult(this RpcException exception)
    {
        return Result.Fail(ToErrors(exception));
    }

    /// <summary>
    /// Converts a gRPC fault into a failed <see cref="Result{TValue}"/>.
    /// </summary>
    /// <typeparam name="TValue">The type the result would have carried on success.</typeparam>
    /// <param name="exception">The fault raised by the gRPC client.</param>
    /// <returns>A failed <see cref="Result{TValue}"/> carrying the ProblemDetails of the remote failure.</returns>
    public static Result<TValue> ToResult<TValue>(this RpcException exception)
    {
        return Result.Fail<TValue>(ToErrors(exception));
    }

    /// <summary>
    /// Converts a gRPC fault into the <see cref="ProblemDetailError"/> describing it.
    /// </summary>
    /// <remarks>
    /// The messages of a validation failure do not fit in a <see cref="ProblemDetailError"/>: use
    /// <see cref="ToResult(RpcException)"/> to keep them.
    /// </remarks>
    /// <param name="exception">The fault raised by the gRPC client.</param>
    /// <returns>
    /// An error rebuilt from the ProblemDetails the server sent in the status details, or - when the fault carries
    /// none, as for a transport failure that never reached the service - one derived from the gRPC status alone.
    /// </returns>
    public static ProblemDetailError ToProblemDetailError(this RpcException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return ToProblemDetailError(exception, ReadProblemDetails(exception));
    }

    /// <summary>
    /// Converts a gRPC fault into the errors of the failed result.
    /// </summary>
    /// <param name="exception">The fault raised by the gRPC client.</param>
    /// <returns>The errors describing the failure.</returns>
    private static IEnumerable<IError> ToErrors(RpcException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var problem = ReadProblemDetails(exception);

        // A validation failure is reported with ValidationErrors, and they are what its 422 document is built
        // from. Putting them back as they were lets ToProblemDetails() rebuild that document whole, messages
        // included.
        return problem is { Errors.Count: > 0 } or { OmittedErrors: > 0 }
            ? ToValidationErrors(problem)
            : [ToProblemDetailError(exception, problem)];
    }

    /// <summary>
    /// Converts a gRPC fault into the <see cref="ProblemDetailError"/> describing it, reusing the document
    /// already read from its status details.
    /// </summary>
    /// <param name="exception">The fault raised by the gRPC client.</param>
    /// <param name="problem">The ProblemDetails read from the status details, if any.</param>
    /// <returns>The error describing the failure.</returns>
    private static ProblemDetailError ToProblemDetailError(RpcException exception, Protos.ProblemDetails? problem)
    {
        // Status.Detail is what the server put in the Status - the Detail of its ProblemDetails. Message is
        // the framework's "Status(StatusCode=..., Detail=...)" rendering, only worth using when Detail is empty.
        var statusDetail = string.IsNullOrWhiteSpace(exception.Status.Detail) ? exception.Message : exception.Status.Detail;

        var statusCode = (int)ToHttpStatusCode(exception.StatusCode);

        if (problem is null)
        {
            return ProblemDetailError.Create(statusDetail).WithStatusCode(statusCode);
        }

        // Protobuf has no null: an empty string is an absent member. The status is the one member acted upon,
        // so only a plausible failure status is taken from the wire.
        var error = ProblemDetailError.Create(string.IsNullOrEmpty(problem.Detail) ? statusDetail : problem.Detail)
                                      .WithStatusCode(problem.Status is >= 400 and <= 599 ? problem.Status : statusCode);

        if (!string.IsNullOrWhiteSpace(problem.Title))
        {
            error = error.WithTitle(problem.Title);
        }

        if (!string.IsNullOrWhiteSpace(problem.Instance))
        {
            error = error.WithInstance(problem.Instance);
        }

        // Checked for emptiness first: an empty string is a valid relative URI.
        if (!string.IsNullOrWhiteSpace(problem.Type) && Uri.TryCreate(problem.Type, UriKind.RelativeOrAbsolute, out var type))
        {
            error = error.WithType(type);
        }

        return error;
    }

    /// <summary>
    /// Reads the ProblemDetails packed in the google.rpc.Status of the fault.
    /// </summary>
    /// <param name="exception">The fault raised by the gRPC client.</param>
    /// <returns>The document, or <c>null</c> when there is none or it cannot be read.</returns>
    private static Protos.ProblemDetails? ReadProblemDetails(RpcException exception)
    {
        try
        {
            return exception.GetRpcStatus()?.GetDetail<Protos.ProblemDetails>();
        }
        catch (InvalidProtocolBufferException)
        {
            // Status details that cannot be read are not a reason to lose the failure: fall back on the gRPC
            // status, which is exactly what the caller would have without them.
            return null;
        }
    }

    /// <summary>
    /// Turns the messages of a validation document back into the <see cref="ValidationError"/>s they were
    /// built from.
    /// </summary>
    /// <param name="problem">The validation document read from the status details.</param>
    /// <returns>The validation errors, followed by a notice when the server had to leave some out.</returns>
    private static List<IError> ToValidationErrors(Protos.ProblemDetails problem)
    {
        var errors = problem.Errors
                            .Select(error => (IError)ValidationError.Create(error.Message).WithSeverity(ToSeverity(error.Severity)))
                            .ToList();

        if (problem.OmittedErrors > 0)
        {
            // Said rather than silently dropped: the caller has to know the list it reads is incomplete.
            errors.Add(ValidationError.Create(string.Create(CultureInfo.InvariantCulture,
                                                  $"{problem.OmittedErrors} more validation message(s) were left out."))
                                      .WithSeverity(Severity.Warning));
        }

        return errors;
    }

    /// <summary>
    /// Maps the severity of a validation message to Arc4u's.
    /// </summary>
    /// <param name="severity">The severity sent by the server.</param>
    /// <returns>The matching Arc4u severity.</returns>
    private static Severity ToSeverity(Protos.Severity severity)
    {
        // Unspecified - or a severity added to the contract after this client was built - counts as an error.
        return severity switch
        {
            Protos.Severity.Warning => Severity.Warning,
            Protos.Severity.Info => Severity.Info,
            _ => Severity.Error,
        };
    }

    /// <summary>
    /// Maps a gRPC status code to its closest HTTP counterpart, for the faults carrying no ProblemDetails.
    /// </summary>
    /// <param name="statusCode">The status code carried by the fault.</param>
    /// <returns>The HTTP status code to report.</returns>
    private static HttpStatusCode ToHttpStatusCode(StatusCode statusCode)
    {
        // The listed codes are the ones the server side produces, mapped back to the status they came from.
        // Anything else - Unknown, Internal, DataLoss, a transport failure that never reached the service -
        // is an InternalServerError: the failure is not the caller's.
        return statusCode switch
        {
            StatusCode.InvalidArgument or StatusCode.OutOfRange => HttpStatusCode.BadRequest,
            StatusCode.Unauthenticated => HttpStatusCode.Unauthorized,
            StatusCode.PermissionDenied => HttpStatusCode.Forbidden,
            StatusCode.NotFound => HttpStatusCode.NotFound,
            StatusCode.Aborted or StatusCode.AlreadyExists => HttpStatusCode.Conflict,
            StatusCode.FailedPrecondition => HttpStatusCode.PreconditionFailed,
            StatusCode.ResourceExhausted => HttpStatusCode.TooManyRequests,
            StatusCode.Unimplemented => HttpStatusCode.NotImplemented,
            StatusCode.Unavailable => HttpStatusCode.ServiceUnavailable,
            StatusCode.DeadlineExceeded => HttpStatusCode.GatewayTimeout,
            _ => HttpStatusCode.InternalServerError,
        };
    }
}
