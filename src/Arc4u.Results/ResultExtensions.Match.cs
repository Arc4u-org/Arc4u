using FluentResults;

namespace Arc4u.Results;

// Match ends a chain: it turns a result into another type (an HTTP response, a DTO, a message) by calling the
// callback matching the state of the result, and returns what that callback produces.
public static partial class ResultExtension
{
    #region Match

    /// <summary>
    /// Calls the callback matching the state of the result and returns what it produces.
    /// </summary>
    /// <remarks>Ends a chain by turning the result into another type, for instance an HTTP response or a message for the user. Exactly one
    /// callback is called. Unlike <c>OnSuccess</c> and <c>OnFailed</c>, the callbacks produce the value returned by the method, so no variable needs
    /// to be assigned from a callback. An exception thrown by a callback propagates to the caller.</remarks>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">Produces the outcome of a successful result.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>The value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static TOut Match<TOut>(this Result result, Func<TOut> onSuccess, Func<IReadOnlyCollection<IError>, TOut> onFailed)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailed);

        return result.IsSuccess ? onSuccess() : onFailed(result.Errors);
    }

    /// <summary>
    /// Awaits the task, then calls the callback matching the state of the result and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TOut}(Result, Func{TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The task producing the result to match.</param>
    /// <param name="onSuccess">Produces the outcome of a successful result.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async Task<TOut> Match<TOut>(this Task<Result> result, Func<TOut> onSuccess, Func<IReadOnlyCollection<IError>, TOut> onFailed)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailed);

        return (await result.ConfigureAwait(false)).Match(onSuccess, onFailed);
    }

    /// <summary>
    /// Awaits the value task, then calls the callback matching the state of the result and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TOut}(Result, Func{TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The value task producing the result to match.</param>
    /// <param name="onSuccess">Produces the outcome of a successful result.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A value task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async ValueTask<TOut> Match<TOut>(this ValueTask<Result> result, Func<TOut> onSuccess, Func<IReadOnlyCollection<IError>, TOut> onFailed)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailed);

        return (await result.ConfigureAwait(false)).Match(onSuccess, onFailed);
    }

    /// <summary>
    /// Calls the callback matching the state of the result and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TValue, TOut}(Result{TValue}, Func{TValue, TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">Produces the outcome of a successful result from its value, which can be <see langword="null"/>.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>The value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static TOut Match<TValue, TOut>(this Result<TValue> result, Func<TValue, TOut> onSuccess, Func<IReadOnlyCollection<IError>, TOut> onFailed)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailed);

        return result.IsSuccess ? onSuccess(result.Value) : onFailed(result.Errors);
    }

    /// <summary>
    /// Awaits the task, then calls the callback matching the state of the result and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TValue, TOut}(Result{TValue}, Func{TValue, TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The task producing the result to match.</param>
    /// <param name="onSuccess">Produces the outcome of a successful result from its value, which can be <see langword="null"/>.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async Task<TOut> Match<TValue, TOut>(this Task<Result<TValue>> result, Func<TValue, TOut> onSuccess, Func<IReadOnlyCollection<IError>, TOut> onFailed)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailed);

        return (await result.ConfigureAwait(false)).Match(onSuccess, onFailed);
    }

    /// <summary>
    /// Awaits the value task, then calls the callback matching the state of the result and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TValue, TOut}(Result{TValue}, Func{TValue, TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The value task producing the result to match.</param>
    /// <param name="onSuccess">Produces the outcome of a successful result from its value, which can be <see langword="null"/>.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A value task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async ValueTask<TOut> Match<TValue, TOut>(this ValueTask<Result<TValue>> result, Func<TValue, TOut> onSuccess, Func<IReadOnlyCollection<IError>, TOut> onFailed)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailed);

        return (await result.ConfigureAwait(false)).Match(onSuccess, onFailed);
    }

    /// <summary>
    /// Calls the callback matching the state of the result, telling a successful result with a value from one whose value is <see langword="null"/>, and returns what it produces.
    /// </summary>
    /// <remarks>Meant for a result whose value is declared nullable, such as <c>Result&lt;Order?&gt;</c>: <paramref name="onSuccessNotNull"/> then receives
    /// a value that is not <see langword="null"/>. For a value declared not nullable, use the overload with a single success callback.
    /// See <see cref="Match{TOut}(Result, Func{TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccessNotNull">Produces the outcome of a successful result from its value, which is not <see langword="null"/>.</param>
    /// <param name="onSuccessNull">Produces the outcome of a successful result whose value is <see langword="null"/>.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>The value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException">One of <paramref name="onSuccessNotNull"/>, <paramref name="onSuccessNull"/>, <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static TOut Match<TValue, TOut>(this Result<TValue?> result, Func<TValue, TOut> onSuccessNotNull, Func<TOut> onSuccessNull, Func<IReadOnlyCollection<IError>, TOut> onFailed)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccessNotNull);
        ArgumentNullException.ThrowIfNull(onSuccessNull);
        ArgumentNullException.ThrowIfNull(onFailed);

        if (result.IsFailed)
        {
            return onFailed(result.Errors);
        }

        return result.ValueOrDefault is { } value ? onSuccessNotNull(value) : onSuccessNull();
    }

    /// <summary>
    /// Awaits the task, then calls the callback matching the state of the result, telling a successful result with a value from one whose value is <see langword="null"/>, and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TValue, TOut}(Result{TValue}, Func{TValue, TOut}, Func{TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The task producing the result to match.</param>
    /// <param name="onSuccessNotNull">Produces the outcome of a successful result from its value, which is not <see langword="null"/>.</param>
    /// <param name="onSuccessNull">Produces the outcome of a successful result whose value is <see langword="null"/>.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException">One of <paramref name="onSuccessNotNull"/>, <paramref name="onSuccessNull"/>, <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async Task<TOut> Match<TValue, TOut>(this Task<Result<TValue?>> result, Func<TValue, TOut> onSuccessNotNull, Func<TOut> onSuccessNull, Func<IReadOnlyCollection<IError>, TOut> onFailed)
    {
        ArgumentNullException.ThrowIfNull(onSuccessNotNull);
        ArgumentNullException.ThrowIfNull(onSuccessNull);
        ArgumentNullException.ThrowIfNull(onFailed);

        return (await result.ConfigureAwait(false)).Match(onSuccessNotNull, onSuccessNull, onFailed);
    }

    /// <summary>
    /// Awaits the value task, then calls the callback matching the state of the result, telling a successful result with a value from one whose value is <see langword="null"/>, and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TValue, TOut}(Result{TValue}, Func{TValue, TOut}, Func{TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The value task producing the result to match.</param>
    /// <param name="onSuccessNotNull">Produces the outcome of a successful result from its value, which is not <see langword="null"/>.</param>
    /// <param name="onSuccessNull">Produces the outcome of a successful result whose value is <see langword="null"/>.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A value task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException">One of <paramref name="onSuccessNotNull"/>, <paramref name="onSuccessNull"/>, <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async ValueTask<TOut> Match<TValue, TOut>(this ValueTask<Result<TValue?>> result, Func<TValue, TOut> onSuccessNotNull, Func<TOut> onSuccessNull, Func<IReadOnlyCollection<IError>, TOut> onFailed)
    {
        ArgumentNullException.ThrowIfNull(onSuccessNotNull);
        ArgumentNullException.ThrowIfNull(onSuccessNull);
        ArgumentNullException.ThrowIfNull(onFailed);

        return (await result.ConfigureAwait(false)).Match(onSuccessNotNull, onSuccessNull, onFailed);
    }

    /// <summary>
    /// Awaits the callback matching the state of the result and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TOut}(Result, Func{TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">Produces the outcome of a successful result.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async Task<TOut> MatchAsync<TOut>(this Result result, Func<Task<TOut>> onSuccess, Func<IReadOnlyCollection<IError>, Task<TOut>> onFailed)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailed);

        return result.IsSuccess ? await onSuccess().ConfigureAwait(false) : await onFailed(result.Errors).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then awaits the callback matching the state of the result and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TOut}(Result, Func{TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The task producing the result to match.</param>
    /// <param name="onSuccess">Produces the outcome of a successful result.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async Task<TOut> MatchAsync<TOut>(this Task<Result> result, Func<Task<TOut>> onSuccess, Func<IReadOnlyCollection<IError>, Task<TOut>> onFailed)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailed);

        return await (await result.ConfigureAwait(false)).MatchAsync(onSuccess, onFailed).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then awaits the callback matching the state of the result and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TOut}(Result, Func{TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The value task producing the result to match.</param>
    /// <param name="onSuccess">Produces the outcome of a successful result.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A value task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async ValueTask<TOut> MatchAsync<TOut>(this ValueTask<Result> result, Func<Task<TOut>> onSuccess, Func<IReadOnlyCollection<IError>, Task<TOut>> onFailed)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailed);

        return await (await result.ConfigureAwait(false)).MatchAsync(onSuccess, onFailed).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the callback matching the state of the result and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TValue, TOut}(Result{TValue}, Func{TValue, TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">Produces the outcome of a successful result from its value, which can be <see langword="null"/>.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async Task<TOut> MatchAsync<TValue, TOut>(this Result<TValue> result, Func<TValue, Task<TOut>> onSuccess, Func<IReadOnlyCollection<IError>, Task<TOut>> onFailed)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailed);

        return result.IsSuccess ? await onSuccess(result.Value).ConfigureAwait(false) : await onFailed(result.Errors).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then awaits the callback matching the state of the result and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TValue, TOut}(Result{TValue}, Func{TValue, TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The task producing the result to match.</param>
    /// <param name="onSuccess">Produces the outcome of a successful result from its value, which can be <see langword="null"/>.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async Task<TOut> MatchAsync<TValue, TOut>(this Task<Result<TValue>> result, Func<TValue, Task<TOut>> onSuccess, Func<IReadOnlyCollection<IError>, Task<TOut>> onFailed)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailed);

        return await (await result.ConfigureAwait(false)).MatchAsync(onSuccess, onFailed).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then awaits the callback matching the state of the result and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TValue, TOut}(Result{TValue}, Func{TValue, TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The value task producing the result to match.</param>
    /// <param name="onSuccess">Produces the outcome of a successful result from its value, which can be <see langword="null"/>.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A value task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="onSuccess"/> or <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async ValueTask<TOut> MatchAsync<TValue, TOut>(this ValueTask<Result<TValue>> result, Func<TValue, Task<TOut>> onSuccess, Func<IReadOnlyCollection<IError>, Task<TOut>> onFailed)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailed);

        return await (await result.ConfigureAwait(false)).MatchAsync(onSuccess, onFailed).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the callback matching the state of the result, telling a successful result with a value from one whose value is <see langword="null"/>, and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TValue, TOut}(Result{TValue}, Func{TValue, TOut}, Func{TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccessNotNull">Produces the outcome of a successful result from its value, which is not <see langword="null"/>.</param>
    /// <param name="onSuccessNull">Produces the outcome of a successful result whose value is <see langword="null"/>.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException">One of <paramref name="onSuccessNotNull"/>, <paramref name="onSuccessNull"/>, <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async Task<TOut> MatchAsync<TValue, TOut>(this Result<TValue?> result, Func<TValue, Task<TOut>> onSuccessNotNull, Func<Task<TOut>> onSuccessNull, Func<IReadOnlyCollection<IError>, Task<TOut>> onFailed)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccessNotNull);
        ArgumentNullException.ThrowIfNull(onSuccessNull);
        ArgumentNullException.ThrowIfNull(onFailed);

        if (result.IsFailed)
        {
            return await onFailed(result.Errors).ConfigureAwait(false);
        }

        return result.ValueOrDefault is { } value ? await onSuccessNotNull(value).ConfigureAwait(false) : await onSuccessNull().ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then awaits the callback matching the state of the result, telling a successful result with a value from one whose value is <see langword="null"/>, and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TValue, TOut}(Result{TValue}, Func{TValue, TOut}, Func{TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The task producing the result to match.</param>
    /// <param name="onSuccessNotNull">Produces the outcome of a successful result from its value, which is not <see langword="null"/>.</param>
    /// <param name="onSuccessNull">Produces the outcome of a successful result whose value is <see langword="null"/>.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException">One of <paramref name="onSuccessNotNull"/>, <paramref name="onSuccessNull"/>, <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async Task<TOut> MatchAsync<TValue, TOut>(this Task<Result<TValue?>> result, Func<TValue, Task<TOut>> onSuccessNotNull, Func<Task<TOut>> onSuccessNull, Func<IReadOnlyCollection<IError>, Task<TOut>> onFailed)
    {
        ArgumentNullException.ThrowIfNull(onSuccessNotNull);
        ArgumentNullException.ThrowIfNull(onSuccessNull);
        ArgumentNullException.ThrowIfNull(onFailed);

        return await (await result.ConfigureAwait(false)).MatchAsync(onSuccessNotNull, onSuccessNull, onFailed).ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then awaits the callback matching the state of the result, telling a successful result with a value from one whose value is <see langword="null"/>, and returns what it produces.
    /// </summary>
    /// <remarks>See <see cref="Match{TValue, TOut}(Result{TValue}, Func{TValue, TOut}, Func{TOut}, Func{IReadOnlyCollection{IError}, TOut})"/>.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TOut">The type produced by the callbacks.</typeparam>
    /// <param name="result">The value task producing the result to match.</param>
    /// <param name="onSuccessNotNull">Produces the outcome of a successful result from its value, which is not <see langword="null"/>.</param>
    /// <param name="onSuccessNull">Produces the outcome of a successful result whose value is <see langword="null"/>.</param>
    /// <param name="onFailed">Produces the outcome of a failed result from its errors.</param>
    /// <returns>A value task completing with the value produced by the callback that was called.</returns>
    /// <exception cref="ArgumentNullException">One of <paramref name="onSuccessNotNull"/>, <paramref name="onSuccessNull"/>, <paramref name="onFailed"/> is <see langword="null"/>.</exception>
    public static async ValueTask<TOut> MatchAsync<TValue, TOut>(this ValueTask<Result<TValue?>> result, Func<TValue, Task<TOut>> onSuccessNotNull, Func<Task<TOut>> onSuccessNull, Func<IReadOnlyCollection<IError>, Task<TOut>> onFailed)
    {
        ArgumentNullException.ThrowIfNull(onSuccessNotNull);
        ArgumentNullException.ThrowIfNull(onSuccessNull);
        ArgumentNullException.ThrowIfNull(onFailed);

        return await (await result.ConfigureAwait(false)).MatchAsync(onSuccessNotNull, onSuccessNull, onFailed).ConfigureAwait(false);
    }

    #endregion
}
