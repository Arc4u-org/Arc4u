using Arc4u.OAuth2.Token;
using Arc4u.Results;
using Arc4u.Results.Validation;
using Arc4u.Validation;
using AutoFixture;
using AutoFixture.AutoMoq;
using AwesomeAssertions;
using FluentResults;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Severity = FluentValidation.Severity;

namespace Arc4u.UnitTest.Results;

public class ResultTests
{
    private readonly Fixture _fixture;

    public ResultTests()
    {
        _fixture = new Fixture();
        _fixture.Customize(new AutoMoqCustomization());
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Test_EmptyResult_Should()
    {
        Result<TokenInfo> result = new();
        var flag = false;

        var sut = result.OnSuccess(() => { flag = true; });

        flag.Should().BeTrue();
        sut.Should().BeSameAs(result);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Test_On_Success_Should()
    {
        var result = Result.Ok();
        var flag = false;

        var sut = result.OnSuccess(() => { flag = true; });

        flag.Should().BeTrue();
        sut.Should().BeSameAs(result);
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Test_On_Success_Async_Should()
    {
        var result = Task.FromResult(Result.Ok());
        var flag = false;

        var sut = await result.OnSuccessAsync(async () =>
        {
            await Task.Delay(1).ConfigureAwait(false);
            flag = true;
        });

        flag.Should().BeTrue();
        sut.Should().BeSameAs(result.Result);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Test_On_Success_With_Failed_Should()
    {
        var result = Result.Fail("");
        var flag = false;

        var sut = result.OnSuccess(() => { flag = true; });

        flag.Should().BeFalse();
        sut.Should().BeSameAs(result);
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Test_On_Success_With_Failed_Async_Should()
    {
        var result = Task.FromResult(Result.Fail(""));
        var flag = false;

        var sut = await result.OnSuccessAsync(async () =>
        {
            await Task.Delay(1).ConfigureAwait(false);
            flag = true;
        });

        flag.Should().BeFalse();
        sut.Should().BeSameAs(result.Result);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Test_On_Failed_Should()
    {
        var result = Result.Fail("");
        var flag = false;

        var sut = result.OnFailed(errors => { flag = true; });

        flag.Should().BeTrue();
        sut.Should().BeSameAs(result);
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Test_On_Failed_Async_Should()
    {
        var result = Task.FromResult(Result.Fail(""));
        var flag = false;

        var sut = await result.OnFailedAsync(async errors =>
            {
                await Task.Delay(1000).ConfigureAwait(false);
                flag = true;
            })
            ;

        flag.Should().BeTrue();
        sut.Should().BeSameAs(result.Result);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Test_On_Failed_With_Success_Should()
    {
        var result = Result.Ok();
        var flag = false;

        var sut = result.OnFailed(errors => { flag = true; });

        flag.Should().BeFalse();
        sut.Should().BeSameAs(result);
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Test_On_Failed_With_Success_Async_Should()
    {
        var result = Task.FromResult(Result.Ok());
        var flag = false;

        var sut = await result.OnFailedAsync(async errors =>
            {
                await Task.Delay(1).ConfigureAwait(false);
                flag = true;
            })
            ;

        flag.Should().BeFalse();
        sut.Should().BeSameAs(result.Result);
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Test_Task_Log_If_Failed_Should()
    {
        var result = Task.FromResult(Result.Fail(""));
        var globalResult = Result.Ok();

        var sut = await result.LogIfFailed()
            .OnFailed(globalResult);

        sut.Should().BeSameAs(result.Result);
        globalResult.IsFailed.Should().BeTrue();
        globalResult.Errors.Count.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Test_ValueTask_Log_If_Failed_Should()
    {
        var result = ValueTask.FromResult(Result.Fail<Guid>(""));
        Result<string> globalResult = Result.Ok();

        var sut = await result.LogIfFailed()
            .OnFailed(errors => globalResult.WithErrors(errors));

        sut.Should().BeSameAs(result.Result);
        globalResult.IsFailed.Should().BeTrue();
        globalResult.Errors.Count.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Test_On_Failed_Typed_Result_With_Untyped_Global_Result_Should()
    {
        var result = Result.Fail<Guid>("");
        var globalResult = Result.Ok();

        var sut = result.OnFailed(globalResult);

        sut.Should().BeSameAs(result);
        globalResult.IsFailed.Should().BeTrue();
        globalResult.Errors.Count.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Test_On_Failed_Untyped_Result_With_Typed_Global_Result_Should()
    {
        var result = Result.Fail("");
        Result<string> globalResult = Result.Ok();

        var sut = result.OnFailed(globalResult);

        sut.Should().BeSameAs(result);
        globalResult.IsFailed.Should().BeTrue();
        globalResult.Errors.Count.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Test_ValueTask_On_Failed_With_Typed_Global_Result_Should()
    {
        var result = ValueTask.FromResult(Result.Fail(""));
        Result<string> globalResult = Result.Ok();

        var sut = await result.OnFailed(globalResult);

        sut.IsFailed.Should().BeTrue();
        globalResult.IsFailed.Should().BeTrue();
        globalResult.Errors.Count.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Test_On_Failed_Typed_Result_With_Differently_Typed_Global_Result_Should()
    {
        var result = Result.Fail<Guid>("");
        Result<string> globalResult = Result.Ok("value");

        var sut = result.OnFailed(globalResult);

        sut.Should().BeSameAs(result);
        globalResult.IsFailed.Should().BeTrue();
        globalResult.Errors.Count.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Test_Task_On_Failed_Typed_Result_With_Differently_Typed_Global_Result_Should()
    {
        var result = Task.FromResult(Result.Fail<Guid>(""));
        Result<string> globalResult = Result.Ok("value");

        var sut = await result.OnFailed(globalResult);

        sut.Should().BeSameAs(result.Result);
        globalResult.IsFailed.Should().BeTrue();
        globalResult.Errors.Count.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Test_ValueTask_On_Failed_Typed_Result_With_Differently_Typed_Global_Result_Should()
    {
        var result = ValueTask.FromResult(Result.Fail<Guid>(""));
        Result<string> globalResult = Result.Ok("value");

        var sut = await result.OnFailed(globalResult);

        sut.IsFailed.Should().BeTrue();
        globalResult.IsFailed.Should().BeTrue();
        globalResult.Errors.Count.Should().Be(1);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Test_On_Failed_Typed_Result_With_Differently_Typed_Global_Result_And_Success_Should()
    {
        var result = Result.Ok(Guid.NewGuid());
        Result<string> globalResult = Result.Ok("value");

        var sut = result.OnFailed(globalResult);

        sut.Should().BeSameAs(result);
        globalResult.IsSuccess.Should().BeTrue();
        globalResult.Value.Should().Be("value");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Test_On_Failed_With_Global_Result_And_Success_Should()
    {
        var result = Result.Ok(Guid.NewGuid());
        var globalResult = Result.Ok();

        var sut = result.OnFailed(globalResult);

        sut.Should().BeSameAs(result);
        globalResult.IsSuccess.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Test_Exception_Failed_Should()
    {
        var globalResult = Result.Ok();

        Func<Task> error = () => throw new DbUpdateException();

        await Result.Try(() => error())
            .OnFailed(globalResult);

        globalResult.IsFailed.Should().BeTrue();
        globalResult.Errors.Count.Should().Be(1);
        globalResult.Errors[0].Should().BeOfType<ExceptionalError>();
        globalResult.Errors[0].As<ExceptionalError>().Exception.Should().BeOfType<DbUpdateException>();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void MessageDetail_tests()
    {
        var error = new ValidationFailure { ErrorMessage = "A", ErrorCode = "Code" }.ToValidationError();

        error.Message.Should().Be("A");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Test_Error_Code_Is_Not_Filled_Should()
    {
        var validator = new Validation();

        var sut = validator.ValidateWithResult(string.Empty);

        sut.IsFailed.Should().BeTrue();
        sut.Errors.Count.Should().Be(1);
        var error = sut.Errors[0].As<ValidationError>();
        error.Message.Should().Be("A");
        error.Code.Should().Be("Code");
        error.Severity.Should().Be(Arc4u.Results.Validation.Severity.Error);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Test_Implicit_ProblemDetailError_To_Result_Should()
    {
        var error = ProblemDetailError.Create(_fixture.Create<string>())
            .WithSeverity(_fixture.Create<string>())
            .WithStatusCode(StatusCodes.Status400BadRequest)
            .WithTitle(_fixture.Create<string>())
            .WithType(_fixture.Create<Uri>())
            .WithInstance(_fixture.Create<string>());

        Result result = error;

        result.IsFailed.Should().BeTrue();
        result.Errors.Count.Should().Be(1);
        result.Errors[0].Should().BeOfType<ProblemDetailError>();
        var problem = (ProblemDetailError)result.Errors[0];
        problem.Message.Should().Be(error.Message);
        problem.StatusCode.Should().Be(error.StatusCode);
        problem.Title.Should().Be(error.Title);
        problem.Severity.Should().Be(error.Severity);
        problem.Instance.Should().Be(error.Instance);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Test_Implicit_ValidationError_To_Result_Should()
    {
        var error = ValidationError.Create(_fixture.Create<string>())
            .WithCode(_fixture.Create<string>())
            .WithSeverity(Arc4u.Results.Validation.Severity.Warning)
            .WithMetadata("key", _fixture.Create<string>());

        Result result = error;

        result.IsFailed.Should().BeTrue();
        result.Errors.Count.Should().Be(1);
        result.Errors[0].Should().BeOfType<ValidationError>();
        var validationError = result.Errors[0].As<ValidationError>();
        validationError.Message.Should().Be(error.Message);
        validationError.Code.Should().Be(error.Code);
        validationError.Severity.Should().Be(error.Severity);
        validationError.Metadata.Count.Should().Be(1);
        validationError.Metadata["key"].Should().Be(error.Metadata["key"]);
    }

    private sealed class Validation : AbstractValidator<string>
    {
        public Validation()
        {
            RuleFor(s => s).NotEmpty()
                .WithMessage("A")
                .WithErrorCode("Code")
                .WithSeverity(Severity.Error);
        }
    }
}
