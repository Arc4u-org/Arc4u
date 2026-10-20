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
/// Each snippet runs inside an async method where r, rt, tt, vt and vtt are respectively a Result,
/// a Result{int}, a Task{Result{int}}, a ValueTask{Result} and a ValueTask{Result{int}}.
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

    private static IReadOnlyList<Diagnostic> Compile(string statement)
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

                static async Task Run()
                {
                    var r = Result.Ok();
                    var rt = Result.Ok(1);
                    var tt = Task.FromResult(rt);
                    var vt = new ValueTask<Result>(r);
                    var vtt = new ValueTask<Result<int>>(rt);
                    Result<string> globalS = Result.Ok("x");

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
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();
    }
}
#endif
