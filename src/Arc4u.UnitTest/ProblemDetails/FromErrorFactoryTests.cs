using Arc4u.AspNetCore.Results;
using Arc4u.Results;
using Arc4u.Results.Validation;
using AwesomeAssertions;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Arc4u.UnitTest.ProblemDetail;

/// <summary>
/// The factory set by <see cref="FromResultToProblemDetailExtension.SetFromErrorFactory"/> is process wide:
/// these tests run alone so that the other tests building a ProblemDetails always see the default factory.
/// </summary>
[CollectionDefinition(nameof(FromErrorFactoryCollection), DisableParallelization = true)]
public class FromErrorFactoryCollection;

/// <summary>
/// A custom factory can handle some errors and fall back to the default Arc4u translation for the others.
/// </summary>
[Trait("Category", "CI")]
[Collection(nameof(FromErrorFactoryCollection))]
public sealed class FromErrorFactoryTests : IDisposable
{
    private sealed class NotFoundError(string message) : Error(message);

    public void Dispose()
    {
        FromResultToProblemDetailExtension.SetFromErrorFactory((errors, defaultFromError) => defaultFromError(errors));
    }

    [Fact]
    public void A_Custom_Factory_Should_Fall_Back_To_The_Default_Translation()
    {
        // arrange
        FromResultToProblemDetailExtension.SetFromErrorFactory((errors, defaultFromError) =>
            errors.OfType<NotFoundError>().Any()
                ? new ProblemDetails().WithTitle("Not found.").WithStatusCode(StatusCodes.Status404NotFound)
                : defaultFromError(errors));

        // act
        var notFound = Result.Fail(new NotFoundError("Missing.")).ToProblemDetails();
        var validation = Result.Fail(ValidationError.Create("Name is required.")).ToProblemDetails();
        var other = Result.Fail("Bad request.").ToProblemDetails();

        // assert
        notFound.Status.Should().Be(StatusCodes.Status404NotFound);
        validation.Should().BeOfType<ValidationProblemDetails>();
        validation.Status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        other.Status.Should().Be(StatusCodes.Status400BadRequest);
        other.Detail.Should().Be("Bad request.");
    }

    [Fact]
    public void A_Custom_Factory_Should_Be_Able_To_Decorate_The_Default_Translation()
    {
        // arrange
        FromResultToProblemDetailExtension.SetFromErrorFactory((errors, defaultFromError) =>
            defaultFromError(errors).WithMetadata("application", "sample"));

        // act
        var sut = Result.Fail("Bad request.").ToProblemDetails();

        // assert
        sut.Status.Should().Be(StatusCodes.Status400BadRequest);
        sut.Extensions.Should().ContainKey("application").WhoseValue.Should().Be("sample");
    }

    [Fact]
    public void FromError_Should_Use_The_Registered_Factory()
    {
        // arrange
        FromResultToProblemDetailExtension.SetFromErrorFactory(_ => new ProblemDetails().WithStatusCode(StatusCodes.Status418ImATeapot));

        // act
        var sut = FromResultToProblemDetailExtension.FromError([new Error("Any.")]);

        // assert
        sut.Status.Should().Be(StatusCodes.Status418ImATeapot);
    }

    [Fact]
    public void Restoring_The_Default_Translation_Should_Drop_The_Custom_Factory()
    {
        // arrange
        FromResultToProblemDetailExtension.SetFromErrorFactory(_ => new ProblemDetails().WithStatusCode(StatusCodes.Status418ImATeapot));

        // act
        FromResultToProblemDetailExtension.SetFromErrorFactory((errors, defaultFromError) => defaultFromError(errors));
        var sut = Result.Fail("Bad request.").ToProblemDetails();

        // assert
        sut.Status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Setting_A_Null_Factory_Should_Throw()
    {
        // act
        var replace = () => FromResultToProblemDetailExtension.SetFromErrorFactory((Func<IEnumerable<IError>, ProblemDetails>)null!);
        var decorate = () => FromResultToProblemDetailExtension.SetFromErrorFactory((Func<IEnumerable<IError>, Func<IEnumerable<IError>, ProblemDetails>, ProblemDetails>)null!);

        // assert
        replace.Should().Throw<ArgumentNullException>();
        decorate.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void The_Default_Translation_Of_No_Error_Should_Throw()
    {
        // arrange
        FromResultToProblemDetailExtension.SetFromErrorFactory((_, defaultFromError) => defaultFromError([]));

        // act
        var act = () => Result.Fail("Bad request.").ToProblemDetails();

        // assert
        act.Should().Throw<ArgumentException>();
    }
}
