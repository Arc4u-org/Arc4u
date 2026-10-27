#if NET10_0
using System.Globalization;
using Arc4u.Results;
using AwesomeAssertions;
using FluentResults;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Arc4u.UnitTest.Results;

/// <summary>
/// The async-void guards of <see cref="ResultExtension"/> only exist at compile time: they are picked by
/// overload resolution and fail the build with CS0619. The only way to test them is to compile code.
/// Each snippet runs inside an async method where r, rt, t, tt, vt and vtt are respectively a Result,
/// a Result{int}, a Task{Result}, a Task{Result{int}}, a ValueTask{Result} and a ValueTask{Result{int}}.
/// Step returns a Result, StepT a Result{int}, StepAsync a Task{Result} and StepTAsync a Task{Result{int}}.
/// </summary>
public class ResultExtensionGuardTests
{
    private const string ObsoleteError = "CS0619";

    [Theory]
    [Trait("Category", "CI")]
    // A ValueTask-returning expression lambda would otherwise bind to the Action overload and be dropped.
    [InlineData("r.OnSuccess(() => SaveVt());", "Func<TValueTask>")]
    [InlineData("rt.OnSuccess(() => SaveVt());", "Func<TValueTask>")]
    [InlineData("rt.OnSuccess(v => SaveVt(v));", "Func<TValue, TValueTask>")]
    [InlineData("rt.OnSuccessNull(() => SaveVt());", "Func<TValueTask>")]
    [InlineData("rt.OnSuccessNotNull(() => SaveVt());", "Func<TValueTask>")]
    [InlineData("rt.OnSuccessNotNull(v => SaveVt(v));", "Func<TValue, TValueTask>")]
    [InlineData("r.OnFailed(e => SaveVtErr(e));", "Func<IReadOnlyCollection<IError>, TValueTask>")]
    [InlineData("rt.OnFailed(e => SaveVtErr(e));", "Func<IReadOnlyCollection<IError>, TValueTask>")]
    [InlineData("tt.OnSuccess(v => SaveVt(v));", "Func<TValue, TValueTask>")]
    [InlineData("tt.OnSuccessNotNull(() => SaveVt());", "Func<TValueTask>")]
    [InlineData("vt.OnSuccess(() => SaveVt());", "Func<TValueTask>")]
    [InlineData("vt.OnFailed(e => SaveVtErr(e));", "Func<IReadOnlyCollection<IError>, TValueTask>")]
    [InlineData("vtt.OnSuccessNull(() => SaveVt());", "Func<TValueTask>")]
    [InlineData("vtt.OnFailed(e => SaveVtErr(e));", "Func<IReadOnlyCollection<IError>, TValueTask>")]
    // The Task-returning forms are still caught by the Task guards.
    [InlineData("r.OnSuccess(() => SaveT());", "Func<Task>")]
    [InlineData("rt.OnSuccess(v => SaveT());", "Func<TValue, Task>")]
    public void ValueTask_Callback_On_Sync_Method_Should_Not_Compile(string statement, string expectedGuard)
    {
        var errors = Compile(statement);

        errors.Should().ContainSingle();
        errors[0].Id.Should().Be(ObsoleteError);
        errors[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain(expectedGuard);
        errors[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("async void");
    }

    [Theory]
    [Trait("Category", "CI")]
    // An async lambda must keep binding to the Func<Task> guard: the ValueTask guard must never make
    // the call ambiguous (CS0121), otherwise the actionable message would be lost.
    [InlineData("r.OnSuccess(async () => await SaveT());", "Func<Task>")]
    [InlineData("r.OnSuccess(async () => await SaveVt());", "Func<Task>")]
    [InlineData("rt.OnSuccess(async v => await SaveVt(v));", "Func<TValue, Task>")]
    [InlineData("rt.OnSuccessNotNull(async v => await SaveVt(v));", "Func<TValue, Task>")]
    [InlineData("r.OnFailed(async e => await SaveVtErr(e));", "Func<IReadOnlyCollection<IError>, Task>")]
    [InlineData("tt.OnSuccess(async () => await SaveT());", "Func<Task>")]
    [InlineData("vt.OnSuccess(async () => await SaveT());", "Func<Task>")]
    public void Async_Lambda_On_Sync_Method_Should_Bind_To_Task_Guard(string statement, string expectedGuard)
    {
        var errors = Compile(statement);

        errors.Should().ContainSingle();
        errors[0].Id.Should().Be(ObsoleteError);
        errors[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain(expectedGuard);
    }

    [Theory]
    [Trait("Category", "CI")]
    // A synchronous callback whose expression happens to return a value is legitimate and must
    // keep binding to the Action overload: the ValueTask guard is constrained to ValueTask only.
    [InlineData("r.OnSuccess(() => set.Add(1));")]
    [InlineData("rt.OnSuccess(v => set.Add(v));")]
    [InlineData("rt.OnSuccessNotNull(v => set.Remove(v));")]
    [InlineData("rt.OnSuccessNull(() => set.Add(0));")]
    [InlineData("r.OnFailed(e => set.Add(e.Count));")]
    [InlineData("tt.OnSuccess(v => set.Add(v));")]
    [InlineData("vt.OnFailed(e => set.Add(e.Count));")]
    // The documented ways to hand a ValueTask-returning call to the *Async twins.
    [InlineData("await r.OnSuccessAsync(async () => await SaveVt());")]
    [InlineData("await r.OnSuccessAsync(() => SaveVt().AsTask());")]
    [InlineData("await rt.OnSuccessAsync(async v => await SaveVt(v));")]
    [InlineData("await rt.OnSuccessNotNullAsync(async v => await SaveVt(v));")]
    [InlineData("await rt.OnSuccessNullAsync(async () => await SaveVt());")]
    [InlineData("await r.OnFailedAsync(async e => await SaveVtErr(e));")]
    [InlineData("await tt.OnSuccessAsync(async () => await SaveVt());")]
    [InlineData("await vt.OnSuccessAsync(async () => await SaveVt());")]
    [InlineData("await vtt.OnFailedAsync(e => SaveVtErr(e).AsTask());")]
    // A typed result propagates its errors into a global result of another type.
    [InlineData("rt.OnFailed(globalS);")]
    [InlineData("await tt.OnFailed(globalS);")]
    [InlineData("await vtt.OnFailed(globalS);")]
    public void Legitimate_Callback_Should_Compile(string statement)
    {
        var errors = Compile(statement);

        errors.Should().BeEmpty();
    }

    [Theory]
    [Trait("Category", "CI")]
    // A callback returning a result would bind to the Action overload of OnSuccess or OnFailed and its result would be dropped.
    [InlineData("r.OnSuccess(() => Step());", "Func<IResultBase>")]
    [InlineData("r.OnSuccess(Step);", "Func<IResultBase>")]
    [InlineData("t.OnSuccess(() => StepT(1));", "Func<IResultBase>")]
    [InlineData("vt.OnSuccess(() => { return Step(); });", "Func<IResultBase>")]
    [InlineData("rt.OnSuccess(v => StepT(v));", "Func<TValue, IResultBase>")]
    [InlineData("rt.OnSuccess(() => Step());", "Func<IResultBase>")]
    [InlineData("tt.OnSuccess(v => Step());", "Func<TValue, IResultBase>")]
    [InlineData("vtt.OnSuccess(v => StepT(v));", "Func<TValue, IResultBase>")]
    [InlineData("rt.OnSuccessNull(() => Step());", "Func<IResultBase>")]
    [InlineData("tt.OnSuccessNull(() => Step());", "Func<IResultBase>")]
    [InlineData("rt.OnSuccessNotNull(v => StepT(v));", "Func<TValue, IResultBase>")]
    [InlineData("vtt.OnSuccessNotNull(() => Step());", "Func<IResultBase>")]
    [InlineData("r.OnFailed(e => Step());", "Func<IReadOnlyCollection<IError>, IResultBase>")]
    [InlineData("tt.OnFailed(e => global.WithErrors(e));", "Func<IReadOnlyCollection<IError>, IResultBase>")]
    [InlineData("vt.OnFailed(e => StepT(e.Count));", "Func<IReadOnlyCollection<IError>, IResultBase>")]
    // The asynchronous twins: the task of the callback produces a result that would be dropped.
    [InlineData("await r.OnSuccessAsync(() => StepAsync());", "Func<Task<TResult>>")]
    [InlineData("await r.OnSuccessAsync(async () => await StepAsync());", "Func<Task<TResult>>")]
    [InlineData("await t.OnSuccessAsync(StepAsync);", "Func<Task<TResult>>")]
    [InlineData("await rt.OnSuccessAsync(v => StepTAsync(v));", "Func<TValue, Task<TResult>>")]
    [InlineData("await tt.OnSuccessAsync(async v => await StepTAsync(v));", "Func<TValue, Task<TResult>>")]
    [InlineData("await vtt.OnSuccessAsync(() => StepAsync());", "Func<Task<TResult>>")]
    [InlineData("await tt.OnSuccessNullAsync(() => StepAsync());", "Func<Task<TResult>>")]
    [InlineData("await tt.OnSuccessNotNullAsync(v => StepTAsync(v));", "Func<TValue, Task<TResult>>")]
    [InlineData("await r.OnFailedAsync(e => StepAsync());", "Func<IReadOnlyCollection<IError>, Task<TResult>>")]
    [InlineData("await vtt.OnFailedAsync(async e => await StepTAsync(e.Count));", "Func<IReadOnlyCollection<IError>, Task<TResult>>")]
    public void Result_Returning_Callback_On_OnSuccess_Or_OnFailed_Should_Not_Compile(string statement, string expectedGuard)
    {
        var errors = Compile(statement);

        errors.Should().ContainSingle();
        errors[0].Id.Should().Be(ObsoleteError);
        errors[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain(expectedGuard);
        errors[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("would be discarded");
        errors[0].GetMessage(CultureInfo.InvariantCulture).Should().Contain("Use Then");
    }

    [Theory]
    [Trait("Category", "CI")]
    // A statement lambda discards the result on purpose and keeps binding to the Action overload.
    [InlineData("r.OnSuccess(() => { Step(); });")]
    [InlineData("rt.OnSuccessNotNull(v => { StepT(v); });")]
    [InlineData("tt.OnFailed(e => { global.WithErrors(e); });")]
    // A lambda that only throws converts to every delegate type: the documented cast selects the Action overload.
    [InlineData("r.OnSuccess((Action)(() => throw new InvalidOperationException()));")]
    [InlineData("await r.OnSuccessAsync(async () => { await StepAsync(); });")]
    [InlineData("await tt.OnFailedAsync(async e => { await StepTAsync(e.Count); });")]
    // A task producing something other than a result still binds to the Func<Task> overload.
    [InlineData("await r.OnSuccessAsync(() => Task.FromResult(1));")]
    [InlineData("await rt.OnSuccessAsync(v => Task.FromResult(v));")]
    // The steps that propagate a result. Then picks the overload matching the result returned by the step.
    [InlineData("Result x = r.Then(() => Step());")]
    [InlineData("Result<int> x = r.Then(() => StepT(1));")]
    [InlineData("Result x = rt.Then(v => Step());")]
    [InlineData("Result<int> x = rt.Then(v => StepT(v)).Then(StepT);")]
    [InlineData("Result<string> x = await tt.Then(v => Result.Ok(v.ToString()));")]
    [InlineData("Result x = await vt.Then(Step);")]
    [InlineData("Result<int> x = await t.ThenAsync(() => StepTAsync(1)).ThenAsync(StepTAsync);")]
    [InlineData("Result x = await vtt.ThenAsync(async v => await StepAsync());")]
    [InlineData("Result<int> x = await rt.ThenAsync(async v => await StepTAsync(v)).Then(v => StepT(v));")]
    [InlineData("Result x = await r.ThenAsync(StepAsync);")]
    [InlineData("Result<int> x = rt.Check(v => Step()).Check(v => StepT(v)).Compensate(e => Step()).Recover(e => StepT(0));")]
    [InlineData("Result<int> x = await tt.Check(v => Step()).CheckAsync(v => StepAsync()).CheckAsync(v => StepTAsync(v));")]
    [InlineData("Result<int> x = await vtt.CheckAsync(async v => await StepTAsync(v)).Compensate(e => Step()).RecoverAsync(e => StepTAsync(0));")]
    [InlineData("Result x = await t.CompensateAsync(e => StepTAsync(e.Count)).Recover(e => Step()).RecoverAsync(e => StepAsync());")]
    [InlineData("Result<int> x = await vtt.CompensateAsync(async e => await StepAsync());")]
    [InlineData("Result<string> x = await Task.FromResult(Result.Ok<string?>(null)).EnsureNotNull(() => new Error(\"none\"));")]
    [InlineData("Result<string> x = await new ValueTask<Result<string?>>(Result.Ok<string?>(\"a\")).EnsureNotNull(() => new Error(\"none\"));")]
    // Match picks the overload from the number of callbacks; the type produced is inferred from them.
    [InlineData("string x = r.Match(() => \"ok\", e => \"failed\");")]
    [InlineData("int x = rt.Match(v => v, e => e.Count);")]
    [InlineData("int x = await tt.Match(v => v, e => 0);")]
    [InlineData("string x = await Task.FromResult(Result.Ok<string?>(null)).Match(v => v, () => \"none\", e => \"failed\");")]
    [InlineData("int x = await vtt.MatchAsync(v => StepTAsync(v).ContinueWith(_ => v), e => Task.FromResult(0));")]
    [InlineData("object x = rt.Match<int, object>(v => v, e => \"failed\");")]
    public void Explicit_Discard_And_Steps_Should_Compile(string statement)
    {
        var errors = Compile(statement);

        errors.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "CI")]
    // EnsureNotNull exists to remove the null: the value it returns must be usable without a nullable warning.
    public void EnsureNotNull_Should_Return_A_Not_Nullable_Value()
    {
        var diagnostics = Compile("var x = await Task.FromResult(Result.Ok<string?>(null)).EnsureNotNull(() => new Error(\"none\")); _ = x.Value.Length;", includeWarnings: true);

        diagnostics.Where(d => d.Id.StartsWith("CS86", StringComparison.Ordinal)).Should().BeEmpty();
    }

    [Theory]
    [Trait("Category", "CI")]
    // The callback for a value that is not null must receive it as not nullable, and a value declared not nullable
    // must not warn with the overload taking a single success callback.
    [InlineData("var x = Result.Ok<string?>(null).Match(v => v.Length, () => 0, e => -1);")]
    [InlineData("var x = Result.Ok(\"a\").Match(v => v.Length, e => -1);")]
    [InlineData("var x = await Task.FromResult(Result.Ok<string?>(\"a\")).Match(v => v.Length, () => 0, e => -1);")]
    public void Match_Should_Not_Raise_A_Nullable_Warning(string statement)
    {
        var diagnostics = Compile(statement, includeWarnings: true);

        diagnostics.Where(d => d.Id.StartsWith("CS86", StringComparison.Ordinal)).Should().BeEmpty();
    }

    private static IReadOnlyList<Diagnostic> Compile(string statement, bool includeWarnings = false)
    {
        var source = $$"""
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            using Arc4u.Results;
            using FluentResults;

            static class Probe
            {
                static ValueTask SaveVt() => default;
                static ValueTask SaveVt(int v) => default;
                static Task SaveT() => Task.CompletedTask;
                static ValueTask SaveVtErr(IReadOnlyCollection<IError> e) => default;
                static readonly HashSet<int> set = new();
                static Result Step() => Result.Ok();
                static Result<int> StepT(int v) => Result.Ok(v);
                static Task<Result> StepAsync() => Task.FromResult(Result.Ok());
                static Task<Result<int>> StepTAsync(int v) => Task.FromResult(Result.Ok(v));

                static async Task Run()
                {
                    var r = Result.Ok();
                    var rt = Result.Ok(1);
                    var t = Task.FromResult(r);
                    var tt = Task.FromResult(rt);
                    var vt = new ValueTask<Result>(r);
                    var vtt = new ValueTask<Result<int>>(rt);
                    Result<string> globalS = Result.Ok("x");
                    var global = Result.Ok();

                    {{statement}}
                }
            }
            """;

        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(ResultExtension).Assembly.Location)
            .Append(typeof(Result).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            nameof(ResultExtensionGuardTests),
            [CSharpSyntaxTree.ParseText(source)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        return compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error || (includeWarnings && d.Severity == DiagnosticSeverity.Warning))
            .ToList();
    }
}
#endif
