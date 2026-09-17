using Arc4u.AspNetCore.Results;
using Arc4u.Results;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using FluentResults;
using Xunit;

namespace Arc4u.UnitTest.ProblemDetail;

/// <summary>
/// Instance is the RFC 7807 member identifying the occurrence of a problem. It is optional: an error that
/// carries one has it rendered, an error without one leaves the member out of the document.
/// </summary>
[Trait("Category", "CI")]
public class ProblemDetailsInstanceTests
{
    private readonly Fixture _fixture;

    public ProblemDetailsInstanceTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    public void The_Instance_Of_A_ProblemDetailError_Should_Be_Rendered()
    {
        // arrange
        var instance = $"/environment/{_fixture.Create<string>()}";
        var result = Result.Fail(ProblemDetailError.Create(_fixture.Create<string>())
                                                   .WithStatusCode(404)
                                                   .WithInstance(instance));

        // act
        var sut = result.ToProblemDetails();

        // assert
        sut.Instance.Should().Be(instance);
    }

    [Fact]
    public void The_Instance_Of_A_ProblemDetailError_Should_Be_Rendered_For_A_Result_Of_T()
    {
        // arrange
        var instance = $"/environment/{_fixture.Create<string>()}";
        var result = Result.Fail<int>(ProblemDetailError.Create(_fixture.Create<string>()).WithInstance(instance));

        // act
        var sut = result.ToProblemDetails();

        // assert
        sut.Instance.Should().Be(instance);
    }

    [Fact]
    public void An_Error_Without_An_Instance_Should_Leave_The_Member_Out()
    {
        // arrange
        var result = Result.Fail(ProblemDetailError.Create(_fixture.Create<string>()).WithStatusCode(409));

        // act
        var sut = result.ToProblemDetails();

        // assert
        sut.Instance.Should().BeNull();
    }
}
