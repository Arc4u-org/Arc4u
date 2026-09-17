using Arc4u.AspNetCore.gRpc.Results;
using Arc4u.AspNetCore.Results;
using Arc4u.gRPC.Results;
using Arc4u.Results;
using Arc4u.Results.Validation;
using AwesomeAssertions;
using FluentResults;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using Protos = Arc4u.gRPC.Protos;
using RpcStatus = Google.Rpc.Status;

namespace Arc4u.UnitTest.gRPC;

/// <summary>
/// A failed result crosses gRPC with FromResultToRpcExceptionExtension on the server and comes back with
/// FromRpcExceptionToResultExtension on the client. What matters is that the client renders the very
/// ProblemDetails the server would have returned over REST.
/// </summary>
public class ProblemDetailsOverGrpcTests
{
    private const string StatusDetailsTrailer = "grpc-status-details-bin";

    [Fact]
    [Trait("Category", "CI")]
    public void A_ProblemDetailError_Should_Cross_Unchanged()
    {
        // arrange
        var result = Result.Fail<int>(ProblemDetailError.Create("Contract 42 does not exist.")
                                                        .WithStatusCode(404)
                                                        .WithTitle("Contract not found.")
                                                        .WithType(new Uri("https://errors.arc4u.net/not-found")));

        // act
        var fault = result.ToRpcException();
        var rebuilt = fault.ToResult<int>();

        // assert
        fault.StatusCode.Should().Be(StatusCode.NotFound);
        fault.Status.Detail.Should().Be("Contract 42 does not exist.");
        rebuilt.ToProblemDetails().Should().BeEquivalentTo(result.ToProblemDetails());
    }

    [Fact]
    [Trait("Category", "CI")]
    public void The_Instance_Of_A_ProblemDetailError_Should_Cross()
    {
        // arrange
        var result = Result.Fail(ProblemDetailError.Create("Environment information is not available.")
                                                   .WithStatusCode(503)
                                                   .WithInstance("/environment/info"));

        // act
        var fault = result.ToRpcException();

        // assert: it travels in the status details and comes back on the rebuilt error.
        fault.GetRpcStatus()!.GetDetail<Protos.ProblemDetails>()!.Instance.Should().Be("/environment/info");
        fault.ToProblemDetailError().Instance.Should().Be("/environment/info");
        fault.ToResult().ToProblemDetails().Instance.Should().Be("/environment/info");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void A_Validation_Failure_Should_Cross_With_Its_Messages()
    {
        // arrange
        var result = Result.Fail(new List<IError>
        {
            ValidationError.Create("Description is unusually long.").WithSeverity(Severity.Warning),
            ValidationError.Create("Name is required."),
            ValidationError.Create("Référence déjà utilisée."),
            ValidationError.Create("Amounts are rounded.").WithSeverity(Severity.Info),
        });

        // act
        var fault = result.ToRpcException();
        var rebuilt = fault.ToResult();

        // assert
        fault.StatusCode.Should().Be(StatusCode.InvalidArgument);

        var expected = (ValidationProblemDetails)result.ToProblemDetails();
        var actual = rebuilt.ToProblemDetails().Should().BeOfType<ValidationProblemDetails>().Subject;
        actual.Status.Should().Be(422);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void A_Bare_Failure_Should_Cross_As_A_Bad_Request()
    {
        // arrange
        var result = Result.Fail("Something went wrong.");

        // act
        var fault = result.ToRpcException();
        var error = fault.ToProblemDetailError();

        // assert
        fault.StatusCode.Should().Be(StatusCode.InvalidArgument);
        error.StatusCode.Should().Be(400);
        error.Message.Should().Be("Something went wrong.");
    }

    [Theory]
    [Trait("Category", "CI")]
    [InlineData(StatusCode.Unavailable, 503)]
    [InlineData(StatusCode.DeadlineExceeded, 504)]
    [InlineData(StatusCode.PermissionDenied, 403)]
    [InlineData(StatusCode.Unauthenticated, 401)]
    [InlineData(StatusCode.AlreadyExists, 409)]
    [InlineData(StatusCode.Unknown, 500)]
    [InlineData(StatusCode.DataLoss, 500)]
    public void A_Fault_Without_Status_Details_Should_Map_Its_Status(StatusCode code, int expectedStatus)
    {
        // arrange: a transport failure never reached a service, so it carries no ProblemDetails.
        var fault = new RpcException(new Status(code, "failed to connect to all addresses"));

        // act
        var error = fault.ToResult<int>().Errors.Should().ContainSingle().Which.Should().BeOfType<ProblemDetailError>().Subject;

        // assert
        error.StatusCode.Should().Be(expectedStatus);
        error.Message.Should().Be("failed to connect to all addresses");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Unreadable_Status_Details_Should_Fall_Back_On_The_Status()
    {
        // arrange
        var fault = new RpcException(new Status(StatusCode.NotFound, "Not here."),
                                     new Metadata { { StatusDetailsTrailer, new byte[] { 0xFF, 0xFF, 0xFF } } });

        // act
        var error = fault.ToProblemDetailError();

        // assert
        error.StatusCode.Should().Be(404);
        error.Message.Should().Be("Not here.");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void An_Implausible_Status_On_The_Wire_Should_Be_Ignored()
    {
        // arrange
        var problem = new Protos.ProblemDetails { Status = 200, Title = "Contract not found." };
        var fault = new RpcStatus { Code = (int)StatusCode.NotFound, Message = "Not here.", Details = { Any.Pack(problem) } }
                    .ToRpcException();

        // act
        var error = fault.ToProblemDetailError();

        // assert
        error.StatusCode.Should().Be(404);
        error.Title.Should().Be("Contract not found.");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Too_Many_Validation_Messages_Should_Drop_The_Least_Severe_First()
    {
        // arrange: interleaved on purpose, so the order on the wire has to come from the severity.
        var errors = Enumerable.Range(1, 450)
                               .Select(i => (IError)ValidationError.Create($"Line {i}: the amount must be greater than zero.")
                                                                   .WithSeverity(i > 400 ? Severity.Info : i % 2 == 0 ? Severity.Warning : Severity.Error))
                               .ToList();

        // act
        var fault = Result.Fail(errors).ToRpcException();
        var rebuilt = fault.ToResult().Errors.Cast<ValidationError>().ToList();

        // assert: the trailer fits in its budget, base64 included.
        var details = fault.Trailers.GetValueBytes(StatusDetailsTrailer)!;
        ((details.Length + 2) / 3 * 4).Should().BeLessThanOrEqualTo(6 * 1024);

        var problem = fault.GetRpcStatus()!.GetDetail<Protos.ProblemDetails>()!;
        problem.Errors.Should().NotBeEmpty().And.OnlyContain(message => message.Severity == Protos.Severity.Error);
        (problem.Errors.Count + problem.OmittedErrors).Should().Be(450);

        var notice = rebuilt[^1];
        notice.Severity.Should().Be(Severity.Warning);
        notice.Message.Should().Be($"{problem.OmittedErrors} more validation message(s) were left out.");
        rebuilt.Should().HaveCount(problem.Errors.Count + 1);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void A_Document_Too_Large_For_The_Trailer_Should_Still_Send_Its_Status()
    {
        // arrange
        var detail = new string('x', 10 * 1024);
        var result = Result.Fail(ProblemDetailError.Create(detail).WithStatusCode(409));

        // act
        var fault = result.ToRpcException();

        // assert
        fault.StatusCode.Should().Be(StatusCode.Aborted);
        fault.Status.Detail.Should().Be(detail);
        fault.GetRpcStatus().Should().BeNull();
        fault.ToProblemDetailError().StatusCode.Should().Be(409);
    }
}
