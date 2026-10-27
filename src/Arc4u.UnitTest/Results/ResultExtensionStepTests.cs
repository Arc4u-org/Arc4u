using Arc4u.Results;
using AwesomeAssertions;
using FluentResults;
using Xunit;

namespace Arc4u.UnitTest.Results;

public class ResultExtensionStepTests
{
    private sealed record Template(string Name);

    #region Then

    [Fact]
    [Trait("Category", "CI")]
    public void Then_Should_Continue_With_The_Result_Of_The_Step()
    {
        var template = new Template("welcome");
        Template? received = null;

        var sut = Result.Ok(template).WithSuccess("loaded")
                        .Then(t => { received = t; return Result.Ok(t.Name).WithSuccess("named"); });

        received.Should().BeSameAs(template);
        sut.Value.Should().Be("welcome");
        sut.Successes.Select(s => s.Message).Should().Equal("loaded", "named");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Then_With_A_Failed_Step_Should_Fail_With_Its_Errors()
    {
        var sut = Result.Ok().WithSuccess("validated").Then(() => Result.Fail<int>("not saved"));

        sut.IsFailed.Should().BeTrue();
        sut.Errors.Should().ContainSingle(e => e.Message == "not saved");
        sut.Successes.Should().ContainSingle(s => s.Message == "validated");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Then_On_A_Failed_Result_Should_Carry_The_Errors_Without_Running_The_Step()
    {
        var called = false;

        var sut = Result.Fail<Template>("db down").Then(_ => { called = true; return Result.Ok(); });

        sut.Errors.Should().ContainSingle(e => e.Message == "db down");
        called.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Then_Should_Behave_Like_Bind()
    {
        var result = Result.Ok(1).WithSuccess("one");

        var then = result.Then(v => Result.Ok(v + 1).WithSuccess("two"));
        var bind = result.Bind(v => Result.Ok(v + 1).WithSuccess("two"));

        then.Value.Should().Be(bind.Value);
        then.Reasons.Select(r => r.Message).Should().Equal(bind.Reasons.Select(r => r.Message));
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Then_On_A_Task_Should_Stop_The_Chain_On_The_First_Failure()
    {
        var steps = new List<string>();

        var sut = await Task.FromResult(Result.Ok(1))
                            .Then(_ => { steps.Add("first"); return Result.Fail<int>("invalid"); })
                            .Then(v => { steps.Add("second"); return Result.Ok(v); });

        steps.Should().Equal("first");
        sut.Errors.Should().ContainSingle(e => e.Message == "invalid");
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task ThenAsync_Should_Await_The_Step()
    {
        var sut = await Result.Ok(new Template("welcome")).ThenAsync(async t =>
        {
            await Task.Yield();
            return Result.Ok(t.Name.Length);
        });

        sut.Value.Should().Be(7);
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task ThenAsync_On_A_ValueTask_Should_Fail_With_The_Errors_Of_The_Step()
    {
        var sut = await ValueTask.FromResult(Result.Ok()).ThenAsync(() => Task.FromResult(Result.Fail("not saved")));

        sut.Errors.Should().ContainSingle(e => e.Message == "not saved");
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task ThenAsync_On_A_Failed_Task_Should_Not_Run_The_Step()
    {
        var called = false;

        var sut = await Task.FromResult(Result.Fail<int>("db down"))
                            .ThenAsync(_ => { called = true; return Task.FromResult(Result.Ok()); });

        sut.Errors.Should().ContainSingle(e => e.Message == "db down");
        called.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Then_With_A_Null_Step_Should_Throw()
    {
        var act = () => Result.Ok().Then((Func<Result>)null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region EnsureNotNull

    [Fact]
    [Trait("Category", "CI")]
    public void EnsureNotNull_With_A_Value_Should_Keep_It()
    {
        var template = new Template("welcome");
        var result = Result.Ok<Template?>(template).WithSuccess("found");
        var called = false;

        var sut = result.EnsureNotNull(() => { called = true; return new Error("none"); });

        sut.IsSuccess.Should().BeTrue();
        sut.Value.Should().BeSameAs(template);
        sut.Successes.Should().ContainSingle(s => s.Message == "found");
        called.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void EnsureNotNull_With_A_Null_Value_Should_Fail_With_The_Error()
    {
        var result = Result.Ok<Template?>(null).WithSuccess("searched");

        var sut = result.EnsureNotNull(() => ProblemDetailError.Create("No template.").WithStatusCode(404));

        sut.IsFailed.Should().BeTrue();
        sut.Errors.Should().ContainSingle().Which.Should().BeOfType<ProblemDetailError>().Which.StatusCode.Should().Be(404);
        sut.Successes.Should().ContainSingle(s => s.Message == "searched");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void EnsureNotNull_With_A_Failed_Result_Should_Keep_The_Errors_Without_Building_One()
    {
        var result = Result.Fail<Template?>("db down");
        var called = false;

        var sut = result.EnsureNotNull(() => { called = true; return new Error("none"); });

        sut.IsFailed.Should().BeTrue();
        sut.Errors.Should().ContainSingle(e => e.Message == "db down");
        called.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task EnsureNotNull_On_A_Task_Should_Fail_With_A_Null_Value()
    {
        var sut = await Task.FromResult(Result.Ok<Template?>(null)).EnsureNotNull(() => new Error("none"));

        sut.Errors.Should().ContainSingle(e => e.Message == "none");
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task EnsureNotNull_On_A_ValueTask_Should_Keep_The_Value()
    {
        var template = new Template("welcome");

        var sut = await ValueTask.FromResult(Result.Ok<Template?>(template)).EnsureNotNull(() => new Error("none"));

        sut.Value.Should().BeSameAs(template);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void EnsureNotNull_With_A_Null_Error_Factory_Should_Throw()
    {
        var act = () => Result.Ok<Template?>(null).EnsureNotNull(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Check

    [Fact]
    [Trait("Category", "CI")]
    public void Check_With_A_Successful_Step_Should_Keep_The_Value_And_The_Reasons_Of_Both()
    {
        var template = new Template("welcome");
        var result = Result.Ok(template).WithSuccess("loaded");
        Template? received = null;

        var sut = result.Check(t => { received = t; return Result.Ok().WithSuccess("updated"); });

        received.Should().BeSameAs(template);
        sut.IsSuccess.Should().BeTrue();
        sut.Value.Should().BeSameAs(template);
        sut.Successes.Select(s => s.Message).Should().Equal("loaded", "updated");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Check_With_A_Failed_Step_Should_Fail_With_Its_Errors()
    {
        var result = Result.Ok(new Template("welcome")).WithSuccess("loaded");

        var sut = result.Check(_ => Result.Fail("name too long"));

        sut.IsFailed.Should().BeTrue();
        sut.Errors.Should().ContainSingle(e => e.Message == "name too long");
        sut.Successes.Should().ContainSingle(s => s.Message == "loaded");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Check_Should_Ignore_The_Value_Of_A_Typed_Step()
    {
        var template = new Template("welcome");

        var sut = Result.Ok(template).Check(_ => Result.Ok(42));

        sut.Value.Should().BeSameAs(template);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Check_On_A_Failed_Result_Should_Return_It_Without_Running_The_Step()
    {
        var result = Result.Fail<Template>("db down");
        var called = false;

        var sut = result.Check(_ => { called = true; return Result.Ok(); });

        sut.Should().BeSameAs(result);
        called.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Check_On_A_Task_Should_Stop_The_Chain_On_The_First_Failure()
    {
        var steps = new List<string>();

        var sut = await Task.FromResult(Result.Ok(new Template("welcome")))
                            .Check(_ => { steps.Add("first"); return Result.Fail("invalid"); })
                            .Check(_ => { steps.Add("second"); return Result.Ok(); });

        steps.Should().Equal("first");
        sut.Errors.Should().ContainSingle(e => e.Message == "invalid");
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task CheckAsync_Should_Await_The_Step_And_Keep_The_Value()
    {
        var template = new Template("welcome");

        var sut = await Result.Ok(template).CheckAsync(async _ =>
        {
            await Task.Yield();
            return Result.Ok().WithSuccess("saved");
        });

        sut.Value.Should().BeSameAs(template);
        sut.Successes.Should().ContainSingle(s => s.Message == "saved");
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task CheckAsync_On_A_ValueTask_Should_Fail_With_The_Errors_Of_The_Step()
    {
        var sut = await ValueTask.FromResult(Result.Ok(new Template("welcome")))
                                 .CheckAsync(_ => Task.FromResult(Result.Fail<int>("not saved")));

        sut.Errors.Should().ContainSingle(e => e.Message == "not saved");
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task CheckAsync_On_A_Failed_Task_Should_Not_Run_The_Step()
    {
        var called = false;

        var sut = await Task.FromResult(Result.Fail<Template>("db down"))
                            .CheckAsync(_ => { called = true; return Task.FromResult(Result.Ok()); });

        sut.Errors.Should().ContainSingle(e => e.Message == "db down");
        called.Should().BeFalse();
    }

    #endregion

    #region Compensate

    [Fact]
    [Trait("Category", "CI")]
    public void Compensate_On_A_Successful_Result_Should_Return_It_Without_Running_The_Clean_Up()
    {
        var result = Result.Ok();
        var called = false;

        var sut = result.Compensate(_ => { called = true; return Result.Ok(); });

        sut.Should().BeSameAs(result);
        called.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Compensate_With_A_Failed_Clean_Up_Should_Keep_Both_Errors()
    {
        var result = Result.Fail("not sent");
        IReadOnlyCollection<IError>? received = null;

        var sut = result.Compensate(errors => { received = errors; return Result.Fail("rollback failed"); });

        received.Should().ContainSingle(e => e.Message == "not sent");
        sut.Errors.Select(e => e.Message).Should().Equal("not sent", "rollback failed");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Compensate_With_A_Successful_Clean_Up_Should_Stay_Failed()
    {
        var sut = Result.Fail<int>("not sent").Compensate(_ => Result.Ok().WithSuccess("rolled back"));

        sut.IsFailed.Should().BeTrue();
        sut.Errors.Should().ContainSingle(e => e.Message == "not sent");
        sut.Successes.Should().ContainSingle(s => s.Message == "rolled back");
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Compensate_On_A_Task_Should_Keep_Both_Errors()
    {
        var sut = await Task.FromResult(Result.Fail<int>("not sent")).Compensate(_ => Result.Fail("rollback failed"));

        sut.Errors.Should().HaveCount(2);
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task CompensateAsync_On_A_ValueTask_Should_Await_The_Clean_Up()
    {
        var sut = await ValueTask.FromResult(Result.Fail("not sent")).CompensateAsync(async _ =>
        {
            await Task.Yield();
            return Result.Fail<int>("rollback failed");
        });

        sut.Errors.Select(e => e.Message).Should().Equal("not sent", "rollback failed");
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task CompensateAsync_On_A_Successful_Task_Should_Not_Run_The_Clean_Up()
    {
        var called = false;

        var sut = await Task.FromResult(Result.Ok(1)).CompensateAsync(_ => { called = true; return Task.FromResult(Result.Ok()); });

        sut.Value.Should().Be(1);
        called.Should().BeFalse();
    }

    #endregion

    #region Recover

    [Fact]
    [Trait("Category", "CI")]
    public void Recover_On_A_Successful_Result_Should_Return_It_Without_Running_The_Fallback()
    {
        var result = Result.Ok(1);
        var called = false;

        var sut = result.Recover(_ => { called = true; return Result.Ok(2); });

        sut.Should().BeSameAs(result);
        called.Should().BeFalse();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Recover_With_A_Successful_Fallback_Should_Return_It()
    {
        var fallback = Result.Ok(new Template("default"));
        IReadOnlyCollection<IError>? received = null;

        var sut = Result.Fail<Template>("not found").Recover(errors => { received = errors; return fallback; });

        received.Should().ContainSingle(e => e.Message == "not found");
        sut.Should().BeSameAs(fallback);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Recover_With_A_Failed_Fallback_Should_Keep_Both_Errors()
    {
        var sut = Result.Fail("primary down").Recover(_ => Result.Fail("secondary down"));

        sut.Errors.Select(e => e.Message).Should().Equal("primary down", "secondary down");
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Recover_On_A_ValueTask_Should_Return_The_Fallback()
    {
        var sut = await ValueTask.FromResult(Result.Fail<int>("not found")).Recover(_ => Result.Ok(0));

        sut.Value.Should().Be(0);
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task RecoverAsync_On_A_Task_Should_Await_The_Fallback()
    {
        var sut = await Task.FromResult(Result.Fail("primary down")).RecoverAsync(async _ =>
        {
            await Task.Yield();
            return Result.Ok();
        });

        sut.IsSuccess.Should().BeTrue();
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task RecoverAsync_With_A_Failed_Fallback_Should_Keep_Both_Errors()
    {
        var sut = await Result.Fail<int>("primary down").RecoverAsync(_ => Task.FromResult(Result.Fail<int>("secondary down")));

        sut.Errors.Select(e => e.Message).Should().Equal("primary down", "secondary down");
    }

    #endregion

    #region Match

    [Fact]
    [Trait("Category", "CI")]
    public void Match_On_A_Successful_Result_Should_Return_What_OnSuccess_Produces()
    {
        var sut = Result.Ok().Match(() => "ok", errors => $"{errors.Count} error(s)");

        sut.Should().Be("ok");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Match_On_A_Failed_Result_Should_Pass_The_Errors_To_OnFailed()
    {
        var sut = Result.Fail("a").WithError("b").Match(() => "ok", errors => string.Join(",", errors.Select(e => e.Message)));

        sut.Should().Be("a,b");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Match_Should_Call_Exactly_One_Callback()
    {
        var calls = new List<string>();

        Result.Ok(1).Match(v => { calls.Add("success"); return v; }, _ => { calls.Add("failed"); return 0; });
        Result.Fail<int>("x").Match(v => { calls.Add("success"); return v; }, _ => { calls.Add("failed"); return 0; });

        calls.Should().Equal("success", "failed");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Match_With_A_Single_Success_Callback_Should_Pass_A_Null_Value()
    {
        var sut = Result.Ok<Template?>(null).Match(t => t is null ? "null" : t.Name, _ => "failed");

        sut.Should().Be("null");
    }

    [Theory]
    [Trait("Category", "CI")]
    [InlineData("welcome", "welcome")]
    [InlineData(null, "not found")]
    public void Match_With_A_Null_Callback_Should_Tell_The_Value_From_Null(string? name, string expected)
    {
        var result = Result.Ok<Template?>(name is null ? null : new Template(name));

        var sut = result.Match(t => t.Name, () => "not found", _ => "failed");

        sut.Should().Be(expected);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Match_With_A_Null_Callback_On_A_Failed_Result_Should_Call_OnFailed()
    {
        var sut = Result.Fail<Template?>("db down").Match(t => t.Name, () => "not found", errors => errors.First().Message);

        sut.Should().Be("db down");
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Match_On_A_Task_Should_Await_The_Result()
    {
        var sut = await Task.FromResult(Result.Ok(41)).Match(v => v + 1, _ => 0);

        sut.Should().Be(42);
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task Match_On_A_ValueTask_Should_Tell_The_Value_From_Null()
    {
        var sut = await ValueTask.FromResult(Result.Ok<Template?>(null)).Match(t => t.Name, () => "not found", _ => "failed");

        sut.Should().Be("not found");
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task MatchAsync_Should_Await_The_Callback()
    {
        var sut = await Result.Ok(new Template("welcome")).MatchAsync(async t =>
        {
            await Task.Yield();
            return t.Name;
        }, _ => Task.FromResult("failed"));

        sut.Should().Be("welcome");
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task MatchAsync_On_A_Failed_Task_Should_Await_OnFailed()
    {
        var sut = await Task.FromResult(Result.Fail("db down")).MatchAsync(() => Task.FromResult("ok"), errors => Task.FromResult(errors.First().Message));

        sut.Should().Be("db down");
    }

    [Fact]
    [Trait("Category", "CI")]
    public async Task MatchAsync_On_A_ValueTask_Should_Call_OnSuccessNull()
    {
        var sut = await ValueTask.FromResult(Result.Ok<Template?>(null))
                                 .MatchAsync(t => Task.FromResult(t.Name), () => Task.FromResult("not found"), _ => Task.FromResult("failed"));

        sut.Should().Be("not found");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void Match_With_A_Null_Callback_Should_Throw()
    {
        var act = () => Result.Ok().Match(null!, _ => 0);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    [Fact]
    [Trait("Category", "CI")]
    public async Task A_Use_Case_Chain_Should_Report_The_Failure_Of_Any_Step()
    {
        var saved = false;

        Task<Result<Template?>> GetByIdAsync() => Task.FromResult(Result.Ok<Template?>(new Template("welcome")));
        Task<Result> UpdateAsync(Template _) { saved = true; return Task.FromResult(Result.Ok()); }

        var sut = await GetByIdAsync().EnsureNotNull(() => ProblemDetailError.Create("No template.").WithStatusCode(404))
                                      .Check(t => t.Name.Length > 3 ? Result.Fail("name too long") : Result.Ok())
                                      .ThenAsync(UpdateAsync);

        sut.Errors.Should().ContainSingle(e => e.Message == "name too long");
        saved.Should().BeFalse();
    }
}
