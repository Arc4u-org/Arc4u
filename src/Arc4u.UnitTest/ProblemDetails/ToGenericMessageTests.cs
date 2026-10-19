using Arc4u.AspNetCore.Results;
using AwesomeAssertions;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Arc4u.UnitTest.ProblemDetail;

/// <summary>
/// Every <see cref="FromResultToProblemDetailExtension.ToGenericMessage(Result, bool)"/> overload must honour the unexpectedType argument (issue #250).
/// </summary>
[Trait("Category", "CI")]
public sealed class ToGenericMessageTests
{
    public static TheoryData<string> Overloads => ["Result", "Result+activityId", "Result<T>", "Result<T>+activityId"];

    private static ProblemDetails Build(string overload, bool unexpectedType) => overload switch
    {
        "Result" => Result.Fail("Boom.").ToGenericMessage(unexpectedType),
        "Result+activityId" => Result.Fail("Boom.").ToGenericMessage("id", unexpectedType),
        "Result<T>" => Result.Fail<int>("Boom.").ToGenericMessage(unexpectedType),
        "Result<T>+activityId" => Result.Fail<int>("Boom.").ToGenericMessage("id", unexpectedType),
        _ => throw new ArgumentOutOfRangeException(nameof(overload)),
    };

    [Theory]
    [MemberData(nameof(Overloads))]
    public void Unexpected_Type_Should_Return_500(string overload)
    {
        var problem = Build(overload, unexpectedType: true);

        problem.Status.Should().Be(StatusCodes.Status500InternalServerError);
        problem.Type.Should().EndWith("#unexpected-error");
    }

    [Theory]
    [MemberData(nameof(Overloads))]
    public void Expected_Type_Should_Return_400(string overload)
    {
        var problem = Build(overload, unexpectedType: false);

        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Type.Should().EndWith("#expected-error");
    }
}
