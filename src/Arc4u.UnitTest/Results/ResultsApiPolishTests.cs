using Arc4u.AspNetCore.Results;
using Arc4u.Data;
using Arc4u.Results.Validation;
using Arc4u.Validation;
using AwesomeAssertions;
using FluentResults;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using ValidatorPredicates = Arc4u.FluentValidation.ValidatorPredicates;

namespace Arc4u.UnitTest.Results;

public class ResultsApiPolishTests
{
    public sealed class DatedEntity
    {
        public DateTime CreatedOn { get; set; }
    }

    private sealed class UtcDateTimeValidator : AbstractValidator<DatedEntity>
    {
        public UtcDateTimeValidator()
        {
            RuleFor(e => e.CreatedOn).IsUtcDateTime();
        }
    }

    private sealed class DateOnlyValidator : AbstractValidator<DatedEntity>
    {
        public DateOnlyValidator()
        {
            RuleFor(e => e.CreatedOn).IsDateOnly();
        }
    }

    private sealed class Entity : IPersistEntity
    {
        public PersistChange PersistChange { get; set; }
    }

    [Fact]
    [Trait("Category", "CI")]
    public void ValidationError_WithMetadata_Duplicate_Key_Should_Keep_The_First_Value()
    {
        var sut = ValidationError.Create("message").WithMetadata("key", 1);

        var act = () => sut.WithMetadata("key", 2);

        act.Should().NotThrow();
        sut.Metadata["key"].Should().Be(1);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void IsUtcDateTime_Should_Have_A_Default_Message()
    {
        var result = new UtcDateTimeValidator().Validate(new DatedEntity { CreatedOn = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Local) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().StartWith("Property must be a DateTime and in Utc");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void IsDateOnly_Should_Have_A_Default_Message()
    {
        var result = new DateOnlyValidator().Validate(new DatedEntity { CreatedOn = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().StartWith("Property must be a DateTime with no Time");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void IsNone_Should_Detect_An_Unchanged_Entity()
    {
        ValidatorPredicates.IsNone(new Entity { PersistChange = PersistChange.None }).Should().BeTrue();
        ValidatorPredicates.IsNone(new Entity { PersistChange = PersistChange.Insert }).Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void ToProblemDetails_On_Success_Should_Not_Alter_The_Result()
    {
        var result = Result.Ok();

        var sut = result.ToProblemDetails();

        sut.Status.Should().Be(StatusCodes.Status500InternalServerError);
        sut.Title.Should().Be("A technical error occurred!");
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void ToProblemDetails_Of_T_On_Success_Should_Not_Alter_The_Result()
    {
        var result = Result.Ok("value");

        var sut = result.ToProblemDetails();

        sut.Status.Should().Be(StatusCodes.Status500InternalServerError);
        result.IsSuccess.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Created_On_Result_Should_Not_Need_A_Type_Argument()
    {
        var uri = new Uri("https://arc4u.net/orders/1");

        var action = await Result.Ok().ToActionCreatedResultAsync(uri);
        var actionFromTask = await Task.FromResult(Result.Ok()).ToActionCreatedResultAsync(uri);
        var http = await Result.Ok().ToHttpCreatedResultAsync(uri);
        var httpFromTask = await Task.FromResult(Result.Ok()).ToHttpCreatedResultAsync(uri);
        var typed = await Result.Ok().ToTypedCreatedResultAsync(uri);
        var typedFromTask = await Task.FromResult(Result.Ok()).ToTypedCreatedResultAsync(uri);
        var typedFromValueTask = await ValueTask.FromResult(Result.Ok()).ToTypedCreatedResultAsync(uri);

        action.Should().BeOfType<CreatedResult>();
        actionFromTask.Should().BeOfType<CreatedResult>();
        http.Should().BeOfType<Created>();
        httpFromTask.Should().BeOfType<Created>();
        typed.Result.Should().BeOfType<Created>();
        typedFromTask.Result.Should().BeOfType<Created>();
        typedFromValueTask.Result.Should().BeOfType<Created>();
    }
}
