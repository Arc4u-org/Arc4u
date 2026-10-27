using FluentResults;

namespace Arc4u.Results;

// Steps that can fail. Unlike OnSuccess* and OnFailed, which return the original result unchanged, these methods
// return a new result carrying the reasons of the original one and those of the callback, so nothing it reports is lost:
//   Then           the next step runs on success and the chain continues with its result (FluentResults' Bind).
//   EnsureNotNull  a successful result with a null value becomes a failure.
//   Check          a step that can fail runs on the value, which is kept when the step succeeds.
//   Compensate     a clean-up runs after a failure; its own errors are added to the failure.
//   Recover        a fallback replaces a failure; when the fallback fails too, both errors are kept.
public static partial class ResultExtension
{
    #region Then

    /// <summary>
    /// Runs the next step only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>It is FluentResults' <c>Bind</c> under a name of the Arc4u chain, for every receiver. A failed result skips the step and its errors
    /// are carried on. Otherwise the new result carries the reasons of the original result followed by those of the step, and the value of the step.
    /// An exception thrown by the step propagates to the caller.</remarks>
    /// <param name="result">The result to continue from.</param>
    /// <param name="next">The step.</param>
    /// <returns>A new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static Result Then(this Result result, Func<Result> next)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Chain(result, result.IsFailed ? null : next());
    }

    /// <summary>
    /// Runs the next step only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TNewValue">The type of the value produced by the step.</typeparam>
    /// <param name="result">The result to continue from.</param>
    /// <param name="next">The step.</param>
    /// <returns>A new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static Result<TNewValue> Then<TNewValue>(this Result result, Func<Result<TNewValue>> next)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Chain<TNewValue>(result, result.IsFailed ? null : next());
    }

    /// <summary>
    /// Runs the next step with its value only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to continue from.</param>
    /// <param name="next">The step receiving the value.</param>
    /// <returns>A new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static Result Then<TValue>(this Result<TValue> result, Func<TValue, Result> next)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Chain(result, result.IsFailed ? null : next(result.Value));
    }

    /// <summary>
    /// Runs the next step with its value only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TNewValue">The type of the value produced by the step.</typeparam>
    /// <param name="result">The result to continue from.</param>
    /// <param name="next">The step receiving the value.</param>
    /// <returns>A new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static Result<TNewValue> Then<TValue, TNewValue>(this Result<TValue> result, Func<TValue, Result<TNewValue>> next)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Chain<TNewValue>(result, result.IsFailed ? null : next(result.Value));
    }

    /// <summary>
    /// Awaits the task, then runs the next step only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <param name="result">The task producing the result to continue from.</param>
    /// <param name="next">The step.</param>
    /// <returns>A task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async Task<Result> Then(this Task<Result> result, Func<Result> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return (await result.ConfigureAwait(false)).Then(next);
    }

    /// <summary>
    /// Awaits the task, then runs the next step only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TNewValue">The type of the value produced by the step.</typeparam>
    /// <param name="result">The task producing the result to continue from.</param>
    /// <param name="next">The step.</param>
    /// <returns>A task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TNewValue>> Then<TNewValue>(this Task<Result> result, Func<Result<TNewValue>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return (await result.ConfigureAwait(false)).Then(next);
    }

    /// <summary>
    /// Awaits the task, then runs the next step with its value only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to continue from.</param>
    /// <param name="next">The step receiving the value.</param>
    /// <returns>A task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async Task<Result> Then<TValue>(this Task<Result<TValue>> result, Func<TValue, Result> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return (await result.ConfigureAwait(false)).Then(next);
    }

    /// <summary>
    /// Awaits the task, then runs the next step with its value only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TNewValue">The type of the value produced by the step.</typeparam>
    /// <param name="result">The task producing the result to continue from.</param>
    /// <param name="next">The step receiving the value.</param>
    /// <returns>A task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TNewValue>> Then<TValue, TNewValue>(this Task<Result<TValue>> result, Func<TValue, Result<TNewValue>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return (await result.ConfigureAwait(false)).Then(next);
    }

    /// <summary>
    /// Awaits the value task, then runs the next step only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <param name="result">The value task producing the result to continue from.</param>
    /// <param name="next">The step.</param>
    /// <returns>A value task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> Then(this ValueTask<Result> result, Func<Result> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return (await result.ConfigureAwait(false)).Then(next);
    }

    /// <summary>
    /// Awaits the value task, then runs the next step only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TNewValue">The type of the value produced by the step.</typeparam>
    /// <param name="result">The value task producing the result to continue from.</param>
    /// <param name="next">The step.</param>
    /// <returns>A value task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TNewValue>> Then<TNewValue>(this ValueTask<Result> result, Func<Result<TNewValue>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return (await result.ConfigureAwait(false)).Then(next);
    }

    /// <summary>
    /// Awaits the value task, then runs the next step with its value only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to continue from.</param>
    /// <param name="next">The step receiving the value.</param>
    /// <returns>A value task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> Then<TValue>(this ValueTask<Result<TValue>> result, Func<TValue, Result> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return (await result.ConfigureAwait(false)).Then(next);
    }

    /// <summary>
    /// Awaits the value task, then runs the next step with its value only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TNewValue">The type of the value produced by the step.</typeparam>
    /// <param name="result">The value task producing the result to continue from.</param>
    /// <param name="next">The step receiving the value.</param>
    /// <returns>A value task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TNewValue>> Then<TValue, TNewValue>(this ValueTask<Result<TValue>> result, Func<TValue, Result<TNewValue>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return (await result.ConfigureAwait(false)).Then(next);
    }

    /// <summary>
    /// Awaits the next step only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <param name="result">The result to continue from.</param>
    /// <param name="next">The step.</param>
    /// <returns>A task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async Task<Result> ThenAsync(this Result result, Func<Task<Result>> next)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Chain(result, result.IsFailed ? null : await next().ConfigureAwait(false));
    }

    /// <summary>
    /// Awaits the next step only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TNewValue">The type of the value produced by the step.</typeparam>
    /// <param name="result">The result to continue from.</param>
    /// <param name="next">The step.</param>
    /// <returns>A task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TNewValue>> ThenAsync<TNewValue>(this Result result, Func<Task<Result<TNewValue>>> next)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Chain<TNewValue>(result, result.IsFailed ? null : await next().ConfigureAwait(false));
    }

    /// <summary>
    /// Awaits the next step with its value only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to continue from.</param>
    /// <param name="next">The step receiving the value.</param>
    /// <returns>A task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async Task<Result> ThenAsync<TValue>(this Result<TValue> result, Func<TValue, Task<Result>> next)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Chain(result, result.IsFailed ? null : await next(result.Value).ConfigureAwait(false));
    }

    /// <summary>
    /// Awaits the next step with its value only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TNewValue">The type of the value produced by the step.</typeparam>
    /// <param name="result">The result to continue from.</param>
    /// <param name="next">The step receiving the value.</param>
    /// <returns>A task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TNewValue>> ThenAsync<TValue, TNewValue>(this Result<TValue> result, Func<TValue, Task<Result<TNewValue>>> next)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(next);

        return Chain<TNewValue>(result, result.IsFailed ? null : await next(result.Value).ConfigureAwait(false));
    }

    /// <summary>
    /// Awaits the task, then awaits the next step only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <param name="result">The task producing the result to continue from.</param>
    /// <param name="next">The step.</param>
    /// <returns>A task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async Task<Result> ThenAsync(this Task<Result> result, Func<Task<Result>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return await (await result.ConfigureAwait(false)).ThenAsync(next).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then awaits the next step only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TNewValue">The type of the value produced by the step.</typeparam>
    /// <param name="result">The task producing the result to continue from.</param>
    /// <param name="next">The step.</param>
    /// <returns>A task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TNewValue>> ThenAsync<TNewValue>(this Task<Result> result, Func<Task<Result<TNewValue>>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return await (await result.ConfigureAwait(false)).ThenAsync(next).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then awaits the next step with its value only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to continue from.</param>
    /// <param name="next">The step receiving the value.</param>
    /// <returns>A task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async Task<Result> ThenAsync<TValue>(this Task<Result<TValue>> result, Func<TValue, Task<Result>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return await (await result.ConfigureAwait(false)).ThenAsync(next).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then awaits the next step with its value only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TNewValue">The type of the value produced by the step.</typeparam>
    /// <param name="result">The task producing the result to continue from.</param>
    /// <param name="next">The step receiving the value.</param>
    /// <returns>A task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TNewValue>> ThenAsync<TValue, TNewValue>(this Task<Result<TValue>> result, Func<TValue, Task<Result<TNewValue>>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return await (await result.ConfigureAwait(false)).ThenAsync(next).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then awaits the next step only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <param name="result">The value task producing the result to continue from.</param>
    /// <param name="next">The step.</param>
    /// <returns>A value task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> ThenAsync(this ValueTask<Result> result, Func<Task<Result>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return await (await result.ConfigureAwait(false)).ThenAsync(next).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then awaits the next step only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TNewValue">The type of the value produced by the step.</typeparam>
    /// <param name="result">The value task producing the result to continue from.</param>
    /// <param name="next">The step.</param>
    /// <returns>A value task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TNewValue>> ThenAsync<TNewValue>(this ValueTask<Result> result, Func<Task<Result<TNewValue>>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return await (await result.ConfigureAwait(false)).ThenAsync(next).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then awaits the next step with its value only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to continue from.</param>
    /// <param name="next">The step receiving the value.</param>
    /// <returns>A value task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> ThenAsync<TValue>(this ValueTask<Result<TValue>> result, Func<TValue, Task<Result>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return await (await result.ConfigureAwait(false)).ThenAsync(next).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then awaits the next step with its value only when the result is successful, and continues with the result of that step.
    /// </summary>
    /// <remarks>See <see cref="Then(Result, Func{Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TNewValue">The type of the value produced by the step.</typeparam>
    /// <param name="result">The value task producing the result to continue from.</param>
    /// <param name="next">The step receiving the value.</param>
    /// <returns>A value task completing with a new result with the reasons of both and the value of the step, or with the errors of the original result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="next"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TNewValue>> ThenAsync<TValue, TNewValue>(this ValueTask<Result<TValue>> result, Func<TValue, Task<Result<TNewValue>>> next)
    {
        ArgumentNullException.ThrowIfNull(next);

        return await (await result.ConfigureAwait(false)).ThenAsync(next).ConfigureAwait(false);
    }

    #endregion

    #region EnsureNotNull

    /// <summary>
    /// Turns a successful result whose value is <see langword="null"/> into a failed result carrying the error built by <paramref name="error"/>.
    /// </summary>
    /// <remarks>A failed result keeps its errors and <paramref name="error"/> is not called. A successful result with a value keeps it, now typed as not
    /// <see langword="null"/>. The reasons of the original result are always kept. A nullable value type is returned as is, still typed as nullable.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="error">Builds the error when the value is <see langword="null"/>; it is only called in that case.</param>
    /// <returns>A new result with the value of <paramref name="result"/>, or failed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result<TValue> EnsureNotNull<TValue>(this Result<TValue?> result, Func<IError> error)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(error);

        var ensured = new Result<TValue>().WithReasons(result.Reasons);

        if (result.IsFailed)
        {
            return ensured;
        }

        return result.ValueOrDefault is { } value
            ? ensured.WithValue(value)
            : ensured.WithError(error());
    }

    /// <summary>
    /// Awaits the task, then turns a successful result whose value is <see langword="null"/> into a failed result carrying the error built by <paramref name="error"/>.
    /// </summary>
    /// <remarks>See <see cref="EnsureNotNull{TValue}(Result{TValue}, Func{IError})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="error">Builds the error when the value is <see langword="null"/>; it is only called in that case.</param>
    /// <returns>A task completing with a new result with the value of the awaited result, or failed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> EnsureNotNull<TValue>(this Task<Result<TValue?>> result, Func<IError> error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return (await result.ConfigureAwait(false)).EnsureNotNull(error);
    }

    /// <summary>
    /// Awaits the value task, then turns a successful result whose value is <see langword="null"/> into a failed result carrying the error built by <paramref name="error"/>.
    /// </summary>
    /// <remarks>See <see cref="EnsureNotNull{TValue}(Result{TValue}, Func{IError})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="error">Builds the error when the value is <see langword="null"/>; it is only called in that case.</param>
    /// <returns>A value task completing with a new result with the value of the awaited result, or failed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="error"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> EnsureNotNull<TValue>(this ValueTask<Result<TValue?>> result, Func<IError> error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return (await result.ConfigureAwait(false)).EnsureNotNull(error);
    }

    #endregion

    #region Check

    /// <summary>
    /// Runs a step that can fail on the value of a successful result, and keeps the value when the step succeeds.
    /// </summary>
    /// <remarks>A failed result is returned unchanged and <paramref name="check"/> is not called. Otherwise the new result carries the reasons of
    /// <paramref name="result"/> followed by those of the step: it fails with the errors of the step, or succeeds with the original value. The value
    /// of a result returned by the step is ignored; use <c>Then</c> to continue with it. An exception thrown by the step propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="check">The step receiving the value.</param>
    /// <returns>A new result with the value of <paramref name="result"/> and the reasons of both, or <paramref name="result"/> when it failed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="check"/> is <see langword="null"/>.</exception>
    public static Result<TValue> Check<TValue>(this Result<TValue> result, Func<TValue, IResultBase> check)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(check);

        return result.IsFailed ? result : KeepValue(result, check(result.Value));
    }

    /// <summary>
    /// Awaits the task, then runs a step that can fail on the value of a successful result, and keeps the value when the step succeeds.
    /// </summary>
    /// <remarks>See <see cref="Check{TValue}(Result{TValue}, Func{TValue, IResultBase})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="check">The step receiving the value.</param>
    /// <returns>A task completing with a new result with the value and the reasons of both, or with the awaited result when it failed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="check"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> Check<TValue>(this Task<Result<TValue>> result, Func<TValue, IResultBase> check)
    {
        ArgumentNullException.ThrowIfNull(check);

        return (await result.ConfigureAwait(false)).Check(check);
    }

    /// <summary>
    /// Awaits the value task, then runs a step that can fail on the value of a successful result, and keeps the value when the step succeeds.
    /// </summary>
    /// <remarks>See <see cref="Check{TValue}(Result{TValue}, Func{TValue, IResultBase})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="check">The step receiving the value.</param>
    /// <returns>A value task completing with a new result with the value and the reasons of both, or with the awaited result when it failed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="check"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> Check<TValue>(this ValueTask<Result<TValue>> result, Func<TValue, IResultBase> check)
    {
        ArgumentNullException.ThrowIfNull(check);

        return (await result.ConfigureAwait(false)).Check(check);
    }

    /// <summary>
    /// Awaits an asynchronous step that can fail on the value of a successful result, and keeps the value when the step succeeds.
    /// </summary>
    /// <remarks>See <see cref="Check{TValue}(Result{TValue}, Func{TValue, IResultBase})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the step.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="check">The step receiving the value.</param>
    /// <returns>A task completing with a new result with the value and the reasons of both, or with <paramref name="result"/> when it failed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="check"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> CheckAsync<TValue, TResult>(this Result<TValue> result, Func<TValue, Task<TResult>> check) where TResult : IResultBase
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(check);

        return result.IsFailed ? result : KeepValue(result, await check(result.Value).ConfigureAwait(false));
    }

    /// <summary>
    /// Awaits the task, then awaits an asynchronous step that can fail on the value of a successful result, and keeps the value when the step succeeds.
    /// </summary>
    /// <remarks>See <see cref="Check{TValue}(Result{TValue}, Func{TValue, IResultBase})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the step.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="check">The step receiving the value.</param>
    /// <returns>A task completing with a new result with the value and the reasons of both, or with the awaited result when it failed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="check"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> CheckAsync<TValue, TResult>(this Task<Result<TValue>> result, Func<TValue, Task<TResult>> check) where TResult : IResultBase
    {
        ArgumentNullException.ThrowIfNull(check);

        return await (await result.ConfigureAwait(false)).CheckAsync(check).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then awaits an asynchronous step that can fail on the value of a successful result, and keeps the value when the step succeeds.
    /// </summary>
    /// <remarks>See <see cref="Check{TValue}(Result{TValue}, Func{TValue, IResultBase})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the step.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="check">The step receiving the value.</param>
    /// <returns>A value task completing with a new result with the value and the reasons of both, or with the awaited result when it failed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="check"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> CheckAsync<TValue, TResult>(this ValueTask<Result<TValue>> result, Func<TValue, Task<TResult>> check) where TResult : IResultBase
    {
        ArgumentNullException.ThrowIfNull(check);

        return await (await result.ConfigureAwait(false)).CheckAsync(check).ConfigureAwait(false);
    }

    #endregion

    #region Compensate

    /// <summary>
    /// Runs a clean-up that can fail when the result is failed, and adds its reasons to the failure.
    /// </summary>
    /// <remarks>A successful result is returned unchanged and <paramref name="compensation"/> is not called. Otherwise the new result carries the
    /// reasons of <paramref name="result"/> followed by those of the clean-up, so it stays failed whatever the clean-up returns, and an error of the
    /// clean-up is not lost. Use <c>Recover</c> to replace the failure instead. An exception thrown by the clean-up propagates to the caller.</remarks>
    /// <param name="result">The result to inspect.</param>
    /// <param name="compensation">The clean-up receiving the errors of the failed result.</param>
    /// <returns>A new failed result with the reasons of both, or <paramref name="result"/> when it succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compensation"/> is <see langword="null"/>.</exception>
    public static Result Compensate(this Result result, Func<IReadOnlyCollection<IError>, IResultBase> compensation)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(compensation);

        return result.IsSuccess ? result : new Result().WithReasons(result.Reasons).WithReasons(compensation(result.Errors).Reasons);
    }

    /// <summary>
    /// Runs a clean-up that can fail when the result is failed, and adds its reasons to the failure.
    /// </summary>
    /// <remarks>See <see cref="Compensate(Result, Func{IReadOnlyCollection{IError}, IResultBase})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="compensation">The clean-up receiving the errors of the failed result.</param>
    /// <returns>A new failed result with the reasons of both, or <paramref name="result"/> when it succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compensation"/> is <see langword="null"/>.</exception>
    public static Result<TValue> Compensate<TValue>(this Result<TValue> result, Func<IReadOnlyCollection<IError>, IResultBase> compensation)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(compensation);

        return result.IsSuccess ? result : new Result<TValue>().WithReasons(result.Reasons).WithReasons(compensation(result.Errors).Reasons);
    }

    /// <summary>
    /// Awaits the task, then runs a clean-up that can fail when the result is failed, and adds its reasons to the failure.
    /// </summary>
    /// <remarks>See <see cref="Compensate(Result, Func{IReadOnlyCollection{IError}, IResultBase})"/>.</remarks>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="compensation">The clean-up receiving the errors of the failed result.</param>
    /// <returns>A task completing with a new failed result with the reasons of both, or with the awaited result when it succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compensation"/> is <see langword="null"/>.</exception>
    public static async Task<Result> Compensate(this Task<Result> result, Func<IReadOnlyCollection<IError>, IResultBase> compensation)
    {
        ArgumentNullException.ThrowIfNull(compensation);

        return (await result.ConfigureAwait(false)).Compensate(compensation);
    }

    /// <summary>
    /// Awaits the task, then runs a clean-up that can fail when the result is failed, and adds its reasons to the failure.
    /// </summary>
    /// <remarks>See <see cref="Compensate(Result, Func{IReadOnlyCollection{IError}, IResultBase})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="compensation">The clean-up receiving the errors of the failed result.</param>
    /// <returns>A task completing with a new failed result with the reasons of both, or with the awaited result when it succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compensation"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> Compensate<TValue>(this Task<Result<TValue>> result, Func<IReadOnlyCollection<IError>, IResultBase> compensation)
    {
        ArgumentNullException.ThrowIfNull(compensation);

        return (await result.ConfigureAwait(false)).Compensate(compensation);
    }

    /// <summary>
    /// Awaits the value task, then runs a clean-up that can fail when the result is failed, and adds its reasons to the failure.
    /// </summary>
    /// <remarks>See <see cref="Compensate(Result, Func{IReadOnlyCollection{IError}, IResultBase})"/>.</remarks>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="compensation">The clean-up receiving the errors of the failed result.</param>
    /// <returns>A value task completing with a new failed result with the reasons of both, or with the awaited result when it succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compensation"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> Compensate(this ValueTask<Result> result, Func<IReadOnlyCollection<IError>, IResultBase> compensation)
    {
        ArgumentNullException.ThrowIfNull(compensation);

        return (await result.ConfigureAwait(false)).Compensate(compensation);
    }

    /// <summary>
    /// Awaits the value task, then runs a clean-up that can fail when the result is failed, and adds its reasons to the failure.
    /// </summary>
    /// <remarks>See <see cref="Compensate(Result, Func{IReadOnlyCollection{IError}, IResultBase})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="compensation">The clean-up receiving the errors of the failed result.</param>
    /// <returns>A value task completing with a new failed result with the reasons of both, or with the awaited result when it succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compensation"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> Compensate<TValue>(this ValueTask<Result<TValue>> result, Func<IReadOnlyCollection<IError>, IResultBase> compensation)
    {
        ArgumentNullException.ThrowIfNull(compensation);

        return (await result.ConfigureAwait(false)).Compensate(compensation);
    }

    /// <summary>
    /// Awaits an asynchronous clean-up that can fail when the result is failed, and adds its reasons to the failure.
    /// </summary>
    /// <remarks>See <see cref="Compensate(Result, Func{IReadOnlyCollection{IError}, IResultBase})"/>.</remarks>
    /// <typeparam name="TResult">The type of the result produced by the clean-up.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="compensation">The clean-up receiving the errors of the failed result.</param>
    /// <returns>A task completing with a new failed result with the reasons of both, or with <paramref name="result"/> when it succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compensation"/> is <see langword="null"/>.</exception>
    public static async Task<Result> CompensateAsync<TResult>(this Result result, Func<IReadOnlyCollection<IError>, Task<TResult>> compensation) where TResult : IResultBase
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(compensation);

        return result.IsSuccess ? result : new Result().WithReasons(result.Reasons).WithReasons((await compensation(result.Errors).ConfigureAwait(false)).Reasons);
    }

    /// <summary>
    /// Awaits an asynchronous clean-up that can fail when the result is failed, and adds its reasons to the failure.
    /// </summary>
    /// <remarks>See <see cref="Compensate(Result, Func{IReadOnlyCollection{IError}, IResultBase})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the clean-up.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="compensation">The clean-up receiving the errors of the failed result.</param>
    /// <returns>A task completing with a new failed result with the reasons of both, or with <paramref name="result"/> when it succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compensation"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> CompensateAsync<TValue, TResult>(this Result<TValue> result, Func<IReadOnlyCollection<IError>, Task<TResult>> compensation) where TResult : IResultBase
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(compensation);

        return result.IsSuccess ? result : new Result<TValue>().WithReasons(result.Reasons).WithReasons((await compensation(result.Errors).ConfigureAwait(false)).Reasons);
    }

    /// <summary>
    /// Awaits the task, then awaits an asynchronous clean-up that can fail when the result is failed, and adds its reasons to the failure.
    /// </summary>
    /// <remarks>See <see cref="Compensate(Result, Func{IReadOnlyCollection{IError}, IResultBase})"/>.</remarks>
    /// <typeparam name="TResult">The type of the result produced by the clean-up.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="compensation">The clean-up receiving the errors of the failed result.</param>
    /// <returns>A task completing with a new failed result with the reasons of both, or with the awaited result when it succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compensation"/> is <see langword="null"/>.</exception>
    public static async Task<Result> CompensateAsync<TResult>(this Task<Result> result, Func<IReadOnlyCollection<IError>, Task<TResult>> compensation) where TResult : IResultBase
    {
        ArgumentNullException.ThrowIfNull(compensation);

        return await (await result.ConfigureAwait(false)).CompensateAsync(compensation).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then awaits an asynchronous clean-up that can fail when the result is failed, and adds its reasons to the failure.
    /// </summary>
    /// <remarks>See <see cref="Compensate(Result, Func{IReadOnlyCollection{IError}, IResultBase})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the clean-up.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="compensation">The clean-up receiving the errors of the failed result.</param>
    /// <returns>A task completing with a new failed result with the reasons of both, or with the awaited result when it succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compensation"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> CompensateAsync<TValue, TResult>(this Task<Result<TValue>> result, Func<IReadOnlyCollection<IError>, Task<TResult>> compensation) where TResult : IResultBase
    {
        ArgumentNullException.ThrowIfNull(compensation);

        return await (await result.ConfigureAwait(false)).CompensateAsync(compensation).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then awaits an asynchronous clean-up that can fail when the result is failed, and adds its reasons to the failure.
    /// </summary>
    /// <remarks>See <see cref="Compensate(Result, Func{IReadOnlyCollection{IError}, IResultBase})"/>.</remarks>
    /// <typeparam name="TResult">The type of the result produced by the clean-up.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="compensation">The clean-up receiving the errors of the failed result.</param>
    /// <returns>A value task completing with a new failed result with the reasons of both, or with the awaited result when it succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compensation"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> CompensateAsync<TResult>(this ValueTask<Result> result, Func<IReadOnlyCollection<IError>, Task<TResult>> compensation) where TResult : IResultBase
    {
        ArgumentNullException.ThrowIfNull(compensation);

        return await (await result.ConfigureAwait(false)).CompensateAsync(compensation).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then awaits an asynchronous clean-up that can fail when the result is failed, and adds its reasons to the failure.
    /// </summary>
    /// <remarks>See <see cref="Compensate(Result, Func{IReadOnlyCollection{IError}, IResultBase})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the clean-up.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="compensation">The clean-up receiving the errors of the failed result.</param>
    /// <returns>A value task completing with a new failed result with the reasons of both, or with the awaited result when it succeeded.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="compensation"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> CompensateAsync<TValue, TResult>(this ValueTask<Result<TValue>> result, Func<IReadOnlyCollection<IError>, Task<TResult>> compensation) where TResult : IResultBase
    {
        ArgumentNullException.ThrowIfNull(compensation);

        return await (await result.ConfigureAwait(false)).CompensateAsync(compensation).ConfigureAwait(false);
    }

    #endregion

    #region Recover

    /// <summary>
    /// Replaces a failed result with the result of a fallback.
    /// </summary>
    /// <remarks>A successful result is returned unchanged and <paramref name="recovery"/> is not called. When the fallback succeeds, its result is
    /// returned and the original errors are dropped: the failure has been handled. When the fallback fails too, the new result carries the reasons of
    /// <paramref name="result"/> followed by those of the fallback, so neither failure is lost. An exception thrown by the fallback propagates to the caller.</remarks>
    /// <param name="result">The result to inspect.</param>
    /// <param name="recovery">The fallback receiving the errors of the failed result.</param>
    /// <returns><paramref name="result"/> when it succeeded, the result of the fallback when it succeeded, or a new failed result with the reasons of both.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recovery"/> is <see langword="null"/>.</exception>
    public static Result Recover(this Result result, Func<IReadOnlyCollection<IError>, Result> recovery)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(recovery);

        return result.IsSuccess ? result : Recovered(result, recovery(result.Errors));
    }

    /// <summary>
    /// Replaces a failed result with the result of a fallback.
    /// </summary>
    /// <remarks>See <see cref="Recover(Result, Func{IReadOnlyCollection{IError}, Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="recovery">The fallback receiving the errors of the failed result.</param>
    /// <returns><paramref name="result"/> when it succeeded, the result of the fallback when it succeeded, or a new failed result with the reasons of both.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recovery"/> is <see langword="null"/>.</exception>
    public static Result<TValue> Recover<TValue>(this Result<TValue> result, Func<IReadOnlyCollection<IError>, Result<TValue>> recovery)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(recovery);

        return result.IsSuccess ? result : Recovered(result, recovery(result.Errors));
    }

    /// <summary>
    /// Awaits the task, then replaces a failed result with the result of a fallback.
    /// </summary>
    /// <remarks>See <see cref="Recover(Result, Func{IReadOnlyCollection{IError}, Result})"/>.</remarks>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="recovery">The fallback receiving the errors of the failed result.</param>
    /// <returns>A task completing with the awaited result when it succeeded, the result of the fallback when it succeeded, or a new failed result with the reasons of both.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recovery"/> is <see langword="null"/>.</exception>
    public static async Task<Result> Recover(this Task<Result> result, Func<IReadOnlyCollection<IError>, Result> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        return (await result.ConfigureAwait(false)).Recover(recovery);
    }

    /// <summary>
    /// Awaits the task, then replaces a failed result with the result of a fallback.
    /// </summary>
    /// <remarks>See <see cref="Recover(Result, Func{IReadOnlyCollection{IError}, Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="recovery">The fallback receiving the errors of the failed result.</param>
    /// <returns>A task completing with the awaited result when it succeeded, the result of the fallback when it succeeded, or a new failed result with the reasons of both.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recovery"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> Recover<TValue>(this Task<Result<TValue>> result, Func<IReadOnlyCollection<IError>, Result<TValue>> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        return (await result.ConfigureAwait(false)).Recover(recovery);
    }

    /// <summary>
    /// Awaits the value task, then replaces a failed result with the result of a fallback.
    /// </summary>
    /// <remarks>See <see cref="Recover(Result, Func{IReadOnlyCollection{IError}, Result})"/>.</remarks>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="recovery">The fallback receiving the errors of the failed result.</param>
    /// <returns>A value task completing with the awaited result when it succeeded, the result of the fallback when it succeeded, or a new failed result with the reasons of both.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recovery"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> Recover(this ValueTask<Result> result, Func<IReadOnlyCollection<IError>, Result> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        return (await result.ConfigureAwait(false)).Recover(recovery);
    }

    /// <summary>
    /// Awaits the value task, then replaces a failed result with the result of a fallback.
    /// </summary>
    /// <remarks>See <see cref="Recover(Result, Func{IReadOnlyCollection{IError}, Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="recovery">The fallback receiving the errors of the failed result.</param>
    /// <returns>A value task completing with the awaited result when it succeeded, the result of the fallback when it succeeded, or a new failed result with the reasons of both.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recovery"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> Recover<TValue>(this ValueTask<Result<TValue>> result, Func<IReadOnlyCollection<IError>, Result<TValue>> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        return (await result.ConfigureAwait(false)).Recover(recovery);
    }

    /// <summary>
    /// Awaits an asynchronous fallback replacing a failed result.
    /// </summary>
    /// <remarks>See <see cref="Recover(Result, Func{IReadOnlyCollection{IError}, Result})"/>.</remarks>
    /// <param name="result">The result to inspect.</param>
    /// <param name="recovery">The fallback receiving the errors of the failed result.</param>
    /// <returns>A task completing with <paramref name="result"/> when it succeeded, the result of the fallback when it succeeded, or a new failed result with the reasons of both.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recovery"/> is <see langword="null"/>.</exception>
    public static async Task<Result> RecoverAsync(this Result result, Func<IReadOnlyCollection<IError>, Task<Result>> recovery)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(recovery);

        return result.IsSuccess ? result : Recovered(result, await recovery(result.Errors).ConfigureAwait(false));
    }

    /// <summary>
    /// Awaits an asynchronous fallback replacing a failed result.
    /// </summary>
    /// <remarks>See <see cref="Recover(Result, Func{IReadOnlyCollection{IError}, Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="recovery">The fallback receiving the errors of the failed result.</param>
    /// <returns>A task completing with <paramref name="result"/> when it succeeded, the result of the fallback when it succeeded, or a new failed result with the reasons of both.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recovery"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> RecoverAsync<TValue>(this Result<TValue> result, Func<IReadOnlyCollection<IError>, Task<Result<TValue>>> recovery)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(recovery);

        return result.IsSuccess ? result : Recovered(result, await recovery(result.Errors).ConfigureAwait(false));
    }

    /// <summary>
    /// Awaits the task, then awaits an asynchronous fallback replacing a failed result.
    /// </summary>
    /// <remarks>See <see cref="Recover(Result, Func{IReadOnlyCollection{IError}, Result})"/>.</remarks>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="recovery">The fallback receiving the errors of the failed result.</param>
    /// <returns>A task completing with the awaited result when it succeeded, the result of the fallback when it succeeded, or a new failed result with the reasons of both.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recovery"/> is <see langword="null"/>.</exception>
    public static async Task<Result> RecoverAsync(this Task<Result> result, Func<IReadOnlyCollection<IError>, Task<Result>> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        return await (await result.ConfigureAwait(false)).RecoverAsync(recovery).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then awaits an asynchronous fallback replacing a failed result.
    /// </summary>
    /// <remarks>See <see cref="Recover(Result, Func{IReadOnlyCollection{IError}, Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="recovery">The fallback receiving the errors of the failed result.</param>
    /// <returns>A task completing with the awaited result when it succeeded, the result of the fallback when it succeeded, or a new failed result with the reasons of both.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recovery"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> RecoverAsync<TValue>(this Task<Result<TValue>> result, Func<IReadOnlyCollection<IError>, Task<Result<TValue>>> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        return await (await result.ConfigureAwait(false)).RecoverAsync(recovery).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then awaits an asynchronous fallback replacing a failed result.
    /// </summary>
    /// <remarks>See <see cref="Recover(Result, Func{IReadOnlyCollection{IError}, Result})"/>.</remarks>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="recovery">The fallback receiving the errors of the failed result.</param>
    /// <returns>A value task completing with the awaited result when it succeeded, the result of the fallback when it succeeded, or a new failed result with the reasons of both.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recovery"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> RecoverAsync(this ValueTask<Result> result, Func<IReadOnlyCollection<IError>, Task<Result>> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        return await (await result.ConfigureAwait(false)).RecoverAsync(recovery).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then awaits an asynchronous fallback replacing a failed result.
    /// </summary>
    /// <remarks>See <see cref="Recover(Result, Func{IReadOnlyCollection{IError}, Result})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="recovery">The fallback receiving the errors of the failed result.</param>
    /// <returns>A value task completing with the awaited result when it succeeded, the result of the fallback when it succeeded, or a new failed result with the reasons of both.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recovery"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> RecoverAsync<TValue>(this ValueTask<Result<TValue>> result, Func<IReadOnlyCollection<IError>, Task<Result<TValue>>> recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);

        return await (await result.ConfigureAwait(false)).RecoverAsync(recovery).ConfigureAwait(false);
    }

    #endregion

    // The step is null when the result failed and the step was skipped: only the original reasons are carried on.
    private static Result Chain(IResultBase result, Result? next)
        => new Result().WithReasons(result.Reasons).WithReasons(next?.Reasons ?? []);

    private static Result<TNewValue> Chain<TNewValue>(IResultBase result, Result<TNewValue>? next)
    {
        var chained = new Result<TNewValue>().WithReasons(result.Reasons).WithReasons(next?.Reasons ?? []);

        return chained.IsSuccess ? chained.WithValue(next!.ValueOrDefault) : chained;
    }

    // A successful step keeps the value; a failed one leaves a result without value. The reasons of both are kept in order.
    private static Result<TValue> KeepValue<TValue>(Result<TValue> result, IResultBase step)
    {
        var checkedResult = new Result<TValue>().WithReasons(result.Reasons).WithReasons(step.Reasons);

        return checkedResult.IsSuccess ? checkedResult.WithValue(result.Value) : checkedResult;
    }

    private static Result Recovered(Result result, Result recovered)
        => recovered.IsSuccess ? recovered : new Result().WithReasons(result.Reasons).WithReasons(recovered.Reasons);

    private static Result<TValue> Recovered<TValue>(Result<TValue> result, Result<TValue> recovered)
        => recovered.IsSuccess ? recovered : new Result<TValue>().WithReasons(result.Reasons).WithReasons(recovered.Reasons);
}
