using System.ComponentModel;
using FluentResults;
using Microsoft.Extensions.Logging;

namespace Arc4u.Results;

/// <summary>
/// Reads a use case out loud: OnSuccess / OnSuccessNull / OnSuccessNotNull / OnFailed describe
/// what happens to a <see cref="Result"/> without the caller writing a single if statement.
///
/// Two rules keep that readability honest:
///   1. A method named *Async always returns a Task or a ValueTask, so the caller must await it.
///      Nothing in this class ever blocks on a Task (no .Wait(), no .Result).
///   2. A method NOT named *Async never accepts an asynchronous callback. Passing one is a
///      compile error pointing at the *Async twin, because an async lambda bound to an Action
///      would run as 'async void': fire-and-forget, with nothing awaiting it. Its exception
///      would then surface on a thread pool thread instead of at the caller's await.
///
/// The *Async twins take Task-returning callbacks. Adapt a ValueTask-returning call with an async
/// lambda ('async () => await SaveAsync()') or with .AsTask(): a second overload taking Func&lt;ValueTask&gt;
/// would make every async lambda ambiguous.
///
/// None of these methods turns an exception thrown by a callback into a failed Result: the
/// exception propagates to the caller. Use FluentResults' Result.Try for that conversion.
/// </summary>
/// <example>
/// <code>
/// Task&lt;Result&lt;Order&gt;&gt; GetOrderAsync(int id) => ...;
///
/// var globalResult = Result.Ok();
///
/// var result = await GetOrderAsync(42)
///     .OnSuccessNotNull(order => Console.WriteLine($"Order {order.Id} found"))
///     .OnSuccessNull(() => Console.WriteLine("No such order"))
///     .OnFailed(globalResult)
///     .LogIfFailed();
/// </code>
/// </example>
public static class ResultExtension
{
    private const string AsyncCallbackOnSyncMethod =
        "This overload takes a synchronous callback. An asynchronous one would be invoked as 'async void': " +
        "nothing would await it, so its exception would surface on a thread pool thread instead of at the " +
        "caller's await. Use the *Async twin of this method and await the chain. A ValueTask-returning call " +
        "is adapted with an async lambda ('async () => await SaveAsync()') or with .AsTask().";

    #region OnSuccess

    /// <summary>
    /// Invokes the callback only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <param name="result">The result to inspect.</param>
    /// <param name="action">The callback to run.</param>
    /// <returns>The <paramref name="result"/> received, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static Result OnSuccess(this Result result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result is { IsSuccess: true })
        {
            action();
        }
        return result;
    }

    /// <summary>
    /// Awaits the asynchronous callback only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback to run.</param>
    /// <returns>A task completing with the <paramref name="result"/> received, unchanged, once the callback has run.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result> OnSuccessAsync(this Result result, Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (result is { IsSuccess: true })
        {
            await func().ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>
    /// Awaits the task, then invokes the callback only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="action">The callback to run.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async Task<Result> OnSuccess(this Task<Result> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true })
        {
            action();
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then awaits the asynchronous callback only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback to run.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result> OnSuccessAsync(this Task<Result> result, Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true })
        {
            await func().ConfigureAwait(false);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then invokes the callback only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="action">The callback to run.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> OnSuccess(this ValueTask<Result> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true })
        {
            action();
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then awaits the asynchronous callback only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback to run.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> OnSuccessAsync(this ValueTask<Result> result, Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true })
        {
            await func().ConfigureAwait(false);
        }
        return r;
    }

    /// <summary>
    /// Invokes the callback only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="action">The callback to run.</param>
    /// <returns>The <paramref name="result"/> received, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static Result<TValue> OnSuccess<TValue>(this Result<TValue> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result is { IsSuccess: true })
        {
            action();
        }
        return result;
    }

    /// <summary>
    /// Awaits the asynchronous callback only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback to run.</param>
    /// <returns>A task completing with the <paramref name="result"/> received, unchanged, once the callback has run.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccessAsync<TValue>(this Result<TValue> result, Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (result is { IsSuccess: true })
        {
            await func().ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>
    /// Invokes the callback with the value of the result only when the result is successful.
    /// </summary>
    /// <remarks>
    /// A successful Result may still carry a null value: use <see cref="OnSuccessNotNull{TValue}(Result{TValue}, Action{TValue})"/>
    /// when the callback needs a value it can dereference.
    /// </remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="action">The callback receiving the value of the result.</param>
    /// <returns>The <paramref name="result"/> received, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static Result<TValue> OnSuccess<TValue>(this Result<TValue> result, Action<TValue> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result is { IsSuccess: true })
        {
            action(result.Value);
        }
        return result;
    }

    /// <summary>
    /// Awaits the asynchronous callback, passing it the value of the result only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback receiving the value of the result.</param>
    /// <returns>A task completing with the <paramref name="result"/> received, unchanged, once the callback has run.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccessAsync<TValue>(this Result<TValue> result, Func<TValue, Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (result is { IsSuccess: true })
        {
            await func(result.Value).ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>
    /// Awaits the task, then invokes the callback only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="action">The callback to run.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccess<TValue>(this Task<Result<TValue>> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true })
        {
            action();
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then invokes the callback with the value of the result only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="action">The callback receiving the value of the result.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccess<TValue>(this Task<Result<TValue>> result, Action<TValue> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true })
        {
            action(r.Value);
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then awaits the asynchronous callback only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback to run.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccessAsync<TValue>(this Task<Result<TValue>> result, Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true })
        {
            await func().ConfigureAwait(false);
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then awaits the asynchronous callback, passing it the value of the result only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback receiving the value of the result.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccessAsync<TValue>(this Task<Result<TValue>> result, Func<TValue, Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true })
        {
            await func(r.Value).ConfigureAwait(false);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then invokes the callback only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="action">The callback to run.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnSuccess<TValue>(this ValueTask<Result<TValue>> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true })
        {
            action();
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then invokes the callback with the value of the result only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="action">The callback receiving the value of the result.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnSuccess<TValue>(this ValueTask<Result<TValue>> result, Action<TValue> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true })
        {
            action(r.Value);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then awaits the asynchronous callback only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback to run.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnSuccessAsync<TValue>(this ValueTask<Result<TValue>> result, Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true })
        {
            await func().ConfigureAwait(false);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then awaits the asynchronous callback, passing it the value of the result only when the result is successful.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback receiving the value of the result.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnSuccessAsync<TValue>(this ValueTask<Result<TValue>> result, Func<TValue, Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true })
        {
            await func(r.Value).ConfigureAwait(false);
        }
        return r;
    }

    #endregion

    #region OnSuccessNull

    /// <summary>
    /// Invokes the callback only when the result is successful and its value is <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="action">The callback to run.</param>
    /// <returns>The <paramref name="result"/> received, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static Result<TValue> OnSuccessNull<TValue>(this Result<TValue> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result is { IsSuccess: true, ValueOrDefault: null })
        {
            action();
        }
        return result;
    }

    /// <summary>
    /// Awaits the asynchronous callback only when the result is successful and its value is <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback to run.</param>
    /// <returns>A task completing with the <paramref name="result"/> received, unchanged, once the callback has run.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccessNullAsync<TValue>(this Result<TValue> result, Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (result is { IsSuccess: true, ValueOrDefault: null })
        {
            await func().ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>
    /// Awaits the task, then invokes the callback only when the result is successful and its value is <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="action">The callback to run.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccessNull<TValue>(this Task<Result<TValue>> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true, ValueOrDefault: null })
        {
            action();
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then awaits the asynchronous callback only when the result is successful and its value is <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback to run.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccessNullAsync<TValue>(this Task<Result<TValue>> result, Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true, ValueOrDefault: null })
        {
            await func().ConfigureAwait(false);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then invokes the callback only when the result is successful and its value is <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="action">The callback to run.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnSuccessNull<TValue>(this ValueTask<Result<TValue>> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true, ValueOrDefault: null })
        {
            action();
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then awaits the asynchronous callback only when the result is successful and its value is <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback to run.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnSuccessNullAsync<TValue>(this ValueTask<Result<TValue>> result, Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true, ValueOrDefault: null })
        {
            await func().ConfigureAwait(false);
        }
        return r;
    }

    #endregion

    #region OnSuccessNotNull

    /// <summary>
    /// Invokes the callback only when the result is successful and its value is not <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="action">The callback to run.</param>
    /// <returns>The <paramref name="result"/> received, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static Result<TValue> OnSuccessNotNull<TValue>(this Result<TValue> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result is { IsSuccess: true, ValueOrDefault: not null })
        {
            action();
        }
        return result;
    }

    /// <summary>
    /// Awaits the asynchronous callback only when the result is successful and its value is not <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback to run.</param>
    /// <returns>A task completing with the <paramref name="result"/> received, unchanged, once the callback has run.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccessNotNullAsync<TValue>(this Result<TValue> result, Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (result is { IsSuccess: true, ValueOrDefault: not null })
        {
            await func().ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>
    /// Invokes the callback with the value of the result only when the result is successful and its value is not <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="action">The callback receiving the value of the result.</param>
    /// <returns>The <paramref name="result"/> received, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static Result<TValue> OnSuccessNotNull<TValue>(this Result<TValue> result, Action<TValue> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result is { IsSuccess: true, ValueOrDefault: not null })
        {
            action(result.Value);
        }
        return result;
    }

    /// <summary>
    /// Awaits the asynchronous callback, passing it the value of the result only when the result is successful and its value is not <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback receiving the value of the result.</param>
    /// <returns>A task completing with the <paramref name="result"/> received, unchanged, once the callback has run.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccessNotNullAsync<TValue>(this Result<TValue> result, Func<TValue, Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (result is { IsSuccess: true, ValueOrDefault: not null })
        {
            await func(result.Value).ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>
    /// Awaits the task, then invokes the callback only when the result is successful and its value is not <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="action">The callback to run.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccessNotNull<TValue>(this Task<Result<TValue>> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true, ValueOrDefault: not null })
        {
            action();
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then invokes the callback with the value of the result only when the result is successful and its value is not <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="action">The callback receiving the value of the result.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccessNotNull<TValue>(this Task<Result<TValue>> result, Action<TValue> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true, ValueOrDefault: not null })
        {
            action(r.Value);
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then awaits the asynchronous callback only when the result is successful and its value is not <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback to run.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccessNotNullAsync<TValue>(this Task<Result<TValue>> result, Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true, ValueOrDefault: not null })
        {
            await func().ConfigureAwait(false);
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then awaits the asynchronous callback, passing it the value of the result only when the result is successful and its value is not <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback receiving the value of the result.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnSuccessNotNullAsync<TValue>(this Task<Result<TValue>> result, Func<TValue, Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true, ValueOrDefault: not null })
        {
            await func(r.Value).ConfigureAwait(false);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then invokes the callback only when the result is successful and its value is not <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="action">The callback to run.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnSuccessNotNull<TValue>(this ValueTask<Result<TValue>> result, Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true, ValueOrDefault: not null })
        {
            action();
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then invokes the callback with the value of the result only when the result is successful and its value is not <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="action">The callback receiving the value of the result.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnSuccessNotNull<TValue>(this ValueTask<Result<TValue>> result, Action<TValue> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true, ValueOrDefault: not null })
        {
            action(r.Value);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then awaits the asynchronous callback only when the result is successful and its value is not <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback to run.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnSuccessNotNullAsync<TValue>(this ValueTask<Result<TValue>> result, Func<Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true, ValueOrDefault: not null })
        {
            await func().ConfigureAwait(false);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then awaits the asynchronous callback, passing it the value of the result only when the result is successful and its value is not <see langword="null"/>.
    /// </summary>
    /// <remarks>The callback is skipped otherwise. The original result is always returned unchanged, so calls can be chained. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback receiving the value of the result.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnSuccessNotNullAsync<TValue>(this ValueTask<Result<TValue>> result, Func<TValue, Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsSuccess: true, ValueOrDefault: not null })
        {
            await func(r.Value).ConfigureAwait(false);
        }
        return r;
    }

    #endregion

    #region OnFailed

    /// <summary>
    /// Invokes the callback with the errors of the result when the result is failed.
    /// </summary>
    /// <remarks>The callback is skipped when the result is successful. The original result is always returned unchanged. An exception thrown by the callback propagates to the caller.</remarks>
    /// <param name="result">The result to inspect.</param>
    /// <param name="action">The callback receiving the errors of the failed result.</param>
    /// <returns>The <paramref name="result"/> received, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static Result OnFailed(this Result result, Action<IReadOnlyCollection<IError>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result is { IsFailed: true })
        {
            action(result.Errors);
        }
        return result;
    }

    /// <summary>
    /// Awaits the asynchronous callback with the errors of the result when the result is failed.
    /// </summary>
    /// <remarks>The callback is skipped when the result is successful. The original result is always returned unchanged. An exception thrown by the callback propagates to the caller.</remarks>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback receiving the errors of the failed result.</param>
    /// <returns>A task completing with the <paramref name="result"/> received, unchanged, once the callback has run.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result> OnFailedAsync(this Result result, Func<IReadOnlyCollection<IError>, Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (result is { IsFailed: true })
        {
            await func(result.Errors).ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>
    /// Copies the errors of the result into <paramref name="globalResult"/> when the result is failed.
    /// </summary>
    /// <remarks>Use it to aggregate the errors of several steps into a single result. The original result is returned unchanged.</remarks>
    /// <param name="result">The result to inspect.</param>
    /// <param name="globalResult">The result that collects the errors of the failed result.</param>
    /// <returns>The <paramref name="result"/> received, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globalResult"/> is <see langword="null"/>.</exception>
    public static Result OnFailed(this Result result, Result globalResult)
    {
        ArgumentNullException.ThrowIfNull(globalResult);

        if (result is { IsFailed: true })
        {
            globalResult.WithErrors(result.Errors);
        }
        return result;
    }

    /// <summary>
    /// Collects the errors of a typed result into an untyped global one, the synchronous twin of
    /// <see cref="OnFailed{TValue}(Task{Result{TValue}}, Result)"/>.
    /// </summary>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="globalResult">The result that collects the errors of the failed result.</param>
    /// <returns>The <paramref name="result"/> received, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globalResult"/> is <see langword="null"/>.</exception>
    public static Result<TValue> OnFailed<TValue>(this Result<TValue> result, Result globalResult)
    {
        ArgumentNullException.ThrowIfNull(globalResult);

        if (result is { IsFailed: true })
        {
            globalResult.WithErrors(result.Errors);
        }
        return result;
    }

    /// <summary>
    /// Copies the errors of the result into <paramref name="globalResult"/> when the result is failed.
    /// </summary>
    /// <remarks>Use it to aggregate the errors of several steps into a single result. The original result is returned unchanged.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TGlobal">The type of the value carried by the global result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="globalResult">The result that collects the errors of the failed result.</param>
    /// <returns>The <paramref name="result"/> received, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globalResult"/> is <see langword="null"/>.</exception>
    public static Result<TValue> OnFailed<TValue, TGlobal>(this Result<TValue> result, Result<TGlobal> globalResult)
    {
        ArgumentNullException.ThrowIfNull(globalResult);

        if (result is { IsFailed: true })
        {
            globalResult.WithErrors(result.Errors);
        }
        return result;
    }

    /// <summary>
    /// Collects the errors of an untyped result into a typed global one, the synchronous twin of
    /// <see cref="OnFailed{TGlobal}(Task{Result}, Result{TGlobal})"/>.
    /// </summary>
    /// <typeparam name="TGlobal">The type of the value carried by the global result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="globalResult">The result that collects the errors of the failed result.</param>
    /// <returns>The <paramref name="result"/> received, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globalResult"/> is <see langword="null"/>.</exception>
    public static Result OnFailed<TGlobal>(this Result result, Result<TGlobal> globalResult)
    {
        ArgumentNullException.ThrowIfNull(globalResult);

        if (result is { IsFailed: true })
        {
            globalResult.WithErrors(result.Errors);
        }
        return result;
    }

    /// <summary>
    /// Invokes the callback with the errors of the result when the result is failed.
    /// </summary>
    /// <remarks>The callback is skipped when the result is successful. The original result is always returned unchanged. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="action">The callback receiving the errors of the failed result.</param>
    /// <returns>The <paramref name="result"/> received, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static Result<TValue> OnFailed<TValue>(this Result<TValue> result, Action<IReadOnlyCollection<IError>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        if (result is { IsFailed: true })
        {
            action(result.Errors);
        }
        return result;
    }

    /// <summary>
    /// Awaits the asynchronous callback with the errors of the result when the result is failed.
    /// </summary>
    /// <remarks>The callback is skipped when the result is successful. The original result is always returned unchanged. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback receiving the errors of the failed result.</param>
    /// <returns>A task completing with the <paramref name="result"/> received, unchanged, once the callback has run.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnFailedAsync<TValue>(this Result<TValue> result, Func<IReadOnlyCollection<IError>, Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        if (result is { IsFailed: true })
        {
            await func(result.Errors).ConfigureAwait(false);
        }
        return result;
    }

    /// <summary>
    /// Awaits the task, then copies the errors of the result into <paramref name="globalResult"/> when the result is failed.
    /// </summary>
    /// <remarks>Use it to aggregate the errors of several steps into a single result. The original result is returned unchanged.</remarks>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="globalResult">The result that collects the errors of the failed result.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globalResult"/> is <see langword="null"/>.</exception>
    public static async Task<Result> OnFailed(this Task<Result> result, Result globalResult)
    {
        ArgumentNullException.ThrowIfNull(globalResult);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            globalResult.WithErrors(r.Errors);
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then copies the errors of the result into <paramref name="globalResult"/> when the result is failed.
    /// </summary>
    /// <remarks>Use it to aggregate the errors of several steps into a single result. The original result is returned unchanged.</remarks>
    /// <typeparam name="TGlobal">The type of the value carried by the global result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="globalResult">The result that collects the errors of the failed result.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globalResult"/> is <see langword="null"/>.</exception>
    public static async Task<Result> OnFailed<TGlobal>(this Task<Result> result, Result<TGlobal> globalResult)
    {
        ArgumentNullException.ThrowIfNull(globalResult);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            globalResult.WithErrors(r.Errors);
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then invokes the callback with the errors of the result when the result is failed.
    /// </summary>
    /// <remarks>The callback is skipped when the result is successful. The original result is always returned unchanged. An exception thrown by the callback propagates to the caller.</remarks>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="action">The callback receiving the errors of the failed result.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async Task<Result> OnFailed(this Task<Result> result, Action<IReadOnlyCollection<IError>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            action(r.Errors);
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then awaits the asynchronous callback with the errors of the result when the result is failed.
    /// </summary>
    /// <remarks>The callback is skipped when the result is successful. The original result is always returned unchanged. An exception thrown by the callback propagates to the caller.</remarks>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback receiving the errors of the failed result.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result> OnFailedAsync(this Task<Result> result, Func<IReadOnlyCollection<IError>, Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            await func(r.Errors).ConfigureAwait(false);
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then copies the errors of the result into <paramref name="globalResult"/> when the result is failed.
    /// </summary>
    /// <remarks>Use it to aggregate the errors of several steps into a single result. The original result is returned unchanged.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="globalResult">The result that collects the errors of the failed result.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globalResult"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnFailed<TValue>(this Task<Result<TValue>> result, Result globalResult)
    {
        ArgumentNullException.ThrowIfNull(globalResult);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            globalResult.WithErrors(r.Errors);
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then copies the errors of the result into <paramref name="globalResult"/> when the result is failed.
    /// </summary>
    /// <remarks>Use it to aggregate the errors of several steps into a single result. The original result is returned unchanged.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TGlobal">The type of the value carried by the global result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="globalResult">The result that collects the errors of the failed result.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globalResult"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnFailed<TValue, TGlobal>(this Task<Result<TValue>> result, Result<TGlobal> globalResult)
    {
        ArgumentNullException.ThrowIfNull(globalResult);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            globalResult.WithErrors(r.Errors);
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then invokes the callback with the errors of the result when the result is failed.
    /// </summary>
    /// <remarks>The callback is skipped when the result is successful. The original result is always returned unchanged. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="action">The callback receiving the errors of the failed result.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnFailed<TValue>(this Task<Result<TValue>> result, Action<IReadOnlyCollection<IError>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            action(r.Errors);
        }
        return r;
    }

    /// <summary>
    /// Awaits the task, then awaits the asynchronous callback with the errors of the result when the result is failed.
    /// </summary>
    /// <remarks>The callback is skipped when the result is successful. The original result is always returned unchanged. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback receiving the errors of the failed result.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async Task<Result<TValue>> OnFailedAsync<TValue>(this Task<Result<TValue>> result, Func<IReadOnlyCollection<IError>, Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            await func(r.Errors).ConfigureAwait(false);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then copies the errors of the result into <paramref name="globalResult"/> when the result is failed.
    /// </summary>
    /// <remarks>Use it to aggregate the errors of several steps into a single result. The original result is returned unchanged.</remarks>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="globalResult">The result that collects the errors of the failed result.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globalResult"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> OnFailed(this ValueTask<Result> result, Result globalResult)
    {
        ArgumentNullException.ThrowIfNull(globalResult);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            globalResult.WithErrors(r.Errors);
        }
        return r;
    }

    /// <summary>
    /// Collects the errors of an untyped result into a typed global one, the ValueTask twin of
    /// <see cref="OnFailed{TGlobal}(Task{Result}, Result{TGlobal})"/>.
    /// </summary>
    /// <typeparam name="TGlobal">The type of the value carried by the global result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="globalResult">The result that collects the errors of the failed result.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globalResult"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> OnFailed<TGlobal>(this ValueTask<Result> result, Result<TGlobal> globalResult)
    {
        ArgumentNullException.ThrowIfNull(globalResult);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            globalResult.WithErrors(r.Errors);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then invokes the callback with the errors of the result when the result is failed.
    /// </summary>
    /// <remarks>The callback is skipped when the result is successful. The original result is always returned unchanged. An exception thrown by the callback propagates to the caller.</remarks>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="action">The callback receiving the errors of the failed result.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> OnFailed(this ValueTask<Result> result, Action<IReadOnlyCollection<IError>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            action(r.Errors);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then awaits the asynchronous callback with the errors of the result when the result is failed.
    /// </summary>
    /// <remarks>The callback is skipped when the result is successful. The original result is always returned unchanged. An exception thrown by the callback propagates to the caller.</remarks>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback receiving the errors of the failed result.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result> OnFailedAsync(this ValueTask<Result> result, Func<IReadOnlyCollection<IError>, Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            await func(r.Errors).ConfigureAwait(false);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then copies the errors of the result into <paramref name="globalResult"/> when the result is failed.
    /// </summary>
    /// <remarks>Use it to aggregate the errors of several steps into a single result. The original result is returned unchanged.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="globalResult">The result that collects the errors of the failed result.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globalResult"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnFailed<TValue>(this ValueTask<Result<TValue>> result, Result globalResult)
    {
        ArgumentNullException.ThrowIfNull(globalResult);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            globalResult.WithErrors(r.Errors);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then copies the errors of the result into <paramref name="globalResult"/> when the result is failed.
    /// </summary>
    /// <remarks>Use it to aggregate the errors of several steps into a single result. The original result is returned unchanged.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TGlobal">The type of the value carried by the global result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="globalResult">The result that collects the errors of the failed result.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="globalResult"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnFailed<TValue, TGlobal>(this ValueTask<Result<TValue>> result, Result<TGlobal> globalResult)
    {
        ArgumentNullException.ThrowIfNull(globalResult);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            globalResult.WithErrors(r.Errors);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then invokes the callback with the errors of the result when the result is failed.
    /// </summary>
    /// <remarks>The callback is skipped when the result is successful. The original result is always returned unchanged. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="action">The callback receiving the errors of the failed result.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnFailed<TValue>(this ValueTask<Result<TValue>> result, Action<IReadOnlyCollection<IError>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            action(r.Errors);
        }
        return r;
    }

    /// <summary>
    /// Awaits the value task, then awaits the asynchronous callback with the errors of the result when the result is failed.
    /// </summary>
    /// <remarks>The callback is skipped when the result is successful. The original result is always returned unchanged. An exception thrown by the callback propagates to the caller.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback receiving the errors of the failed result.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="func"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Result<TValue>> OnFailedAsync<TValue>(this ValueTask<Result<TValue>> result, Func<IReadOnlyCollection<IError>, Task> func)
    {
        ArgumentNullException.ThrowIfNull(func);

        var r = await result.ConfigureAwait(false);

        if (r is { IsFailed: true })
        {
            await func(r.Errors).ConfigureAwait(false);
        }
        return r;
    }

    #endregion

    #region LogIfFailed

    /// <summary>
    /// Awaits the task, then passes the result and <paramref name="logLevel"/> to the FluentResults logger when the result is failed.
    /// </summary>
    /// <remarks>It calls the instance method <c>LogIfFailed(LogLevel)</c> of the FluentResults result, which hands the failed result and the level to the <c>IResultLogger</c> configured with <c>Result.Setup</c> (see <c>AddResultLogger</c>); nothing is logged when none is configured. Arc4u's <c>FluentLogger</c> logs each error at <paramref name="logLevel"/>, except a <c>ValidationError</c> (logged at the level of its severity) and an exceptional error (logged as an exception). The original result is returned unchanged.</remarks>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="logLevel">The level handed to the result logger. Defaults to <see cref="LogLevel.Error"/>.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    public static async Task<Result> LogIfFailed(this Task<Result> result, LogLevel logLevel = LogLevel.Error)
    {
        var r = await result.ConfigureAwait(false);

        r.LogIfFailed(logLevel);

        return r;
    }
    /// <summary>
    /// Awaits the task, then passes the result and <paramref name="logLevel"/> to the FluentResults logger when the result is failed.
    /// </summary>
    /// <remarks>It calls the instance method <c>LogIfFailed(LogLevel)</c> of the FluentResults result, which hands the failed result and the level to the <c>IResultLogger</c> configured with <c>Result.Setup</c> (see <c>AddResultLogger</c>); nothing is logged when none is configured. Arc4u's <c>FluentLogger</c> logs each error at <paramref name="logLevel"/>, except a <c>ValidationError</c> (logged at the level of its severity) and an exceptional error (logged as an exception). The original result is returned unchanged.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="logLevel">The level handed to the result logger. Defaults to <see cref="LogLevel.Error"/>.</param>
    /// <returns>A task completing with the awaited result, unchanged.</returns>
    public static async Task<Result<TValue>> LogIfFailed<TValue>(this Task<Result<TValue>> result, LogLevel logLevel = LogLevel.Error)
    {
        var r = await result.ConfigureAwait(false);

        r.LogIfFailed(logLevel);

        return r;
    }

    /// <summary>
    /// Awaits the value task, then passes the result and <paramref name="logLevel"/> to the FluentResults logger when the result is failed.
    /// </summary>
    /// <remarks>It calls the instance method <c>LogIfFailed(LogLevel)</c> of the FluentResults result, which hands the failed result and the level to the <c>IResultLogger</c> configured with <c>Result.Setup</c> (see <c>AddResultLogger</c>); nothing is logged when none is configured. Arc4u's <c>FluentLogger</c> logs each error at <paramref name="logLevel"/>, except a <c>ValidationError</c> (logged at the level of its severity) and an exceptional error (logged as an exception). The original result is returned unchanged.</remarks>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="logLevel">The level handed to the result logger. Defaults to <see cref="LogLevel.Error"/>.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    public static async ValueTask<Result> LogIfFailed(this ValueTask<Result> result, LogLevel logLevel = LogLevel.Error)
    {
        var r = await result.ConfigureAwait(false);

        r.LogIfFailed(logLevel);

        return r;
    }
    /// <summary>
    /// Awaits the value task, then passes the result and <paramref name="logLevel"/> to the FluentResults logger when the result is failed.
    /// </summary>
    /// <remarks>It calls the instance method <c>LogIfFailed(LogLevel)</c> of the FluentResults result, which hands the failed result and the level to the <c>IResultLogger</c> configured with <c>Result.Setup</c> (see <c>AddResultLogger</c>); nothing is logged when none is configured. Arc4u's <c>FluentLogger</c> logs each error at <paramref name="logLevel"/>, except a <c>ValidationError</c> (logged at the level of its severity) and an exceptional error (logged as an exception). The original result is returned unchanged.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="logLevel">The level handed to the result logger. Defaults to <see cref="LogLevel.Error"/>.</param>
    /// <returns>A value task completing with the awaited result, unchanged.</returns>
    public static async ValueTask<Result<TValue>> LogIfFailed<TValue>(this ValueTask<Result<TValue>> result, LogLevel logLevel = LogLevel.Error)
    {
        var r = await result.ConfigureAwait(false);

        r.LogIfFailed(logLevel);

        return r;
    }
    #endregion

    #region async void guards

    // These overloads exist only to be picked by overload resolution when an asynchronous callback
    // is handed to a synchronous method, and to fail the compilation with an actionable message.
    // A plain lambda still binds to the Action overload, so correct code is unaffected.
    // They also catch the subtler 'c => SaveAsync(c)' form, where the returned Task or ValueTask is
    // silently dropped: without a guard such a lambda binds to the Action overload.
    //
    // The ValueTask guards are generic with a constraint that only ValueTask satisfies. A plain
    // Func<ValueTask> overload would make every async lambda ambiguous with the Func<Task> guard, and an
    // unconstrained Func<T> would also swallow legitimate value-returning statements such as '() => set.Add(x)'.
    // A callback returning ValueTask<T> is not caught.

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result OnSuccess(this Result result, Func<Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result> OnSuccess(this Task<Result> result, Func<Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result> OnSuccess(this ValueTask<Result> result, Func<Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccess<TValue>(this Result<TValue> result, Func<Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccess<TValue>(this Result<TValue> result, Func<TValue, Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccess<TValue>(this Task<Result<TValue>> result, Func<Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccess<TValue>(this Task<Result<TValue>> result, Func<TValue, Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccess<TValue>(this ValueTask<Result<TValue>> result, Func<Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccess<TValue>(this ValueTask<Result<TValue>> result, Func<TValue, Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNull</c> and fails the build with an error pointing to <c>OnSuccessNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccessNull<TValue>(this Result<TValue> result, Func<Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNull</c> and fails the build with an error pointing to <c>OnSuccessNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNull<TValue>(this Task<Result<TValue>> result, Func<Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNull</c> and fails the build with an error pointing to <c>OnSuccessNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessNull<TValue>(this ValueTask<Result<TValue>> result, Func<Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNotNull</c> and fails the build with an error pointing to <c>OnSuccessNotNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccessNotNull<TValue>(this Result<TValue> result, Func<Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNotNull</c> and fails the build with an error pointing to <c>OnSuccessNotNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccessNotNull<TValue>(this Result<TValue> result, Func<TValue, Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNotNull</c> and fails the build with an error pointing to <c>OnSuccessNotNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNotNull<TValue>(this Task<Result<TValue>> result, Func<Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNotNull</c> and fails the build with an error pointing to <c>OnSuccessNotNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNotNull<TValue>(this Task<Result<TValue>> result, Func<TValue, Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNotNull</c> and fails the build with an error pointing to <c>OnSuccessNotNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessNotNull<TValue>(this ValueTask<Result<TValue>> result, Func<Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNotNull</c> and fails the build with an error pointing to <c>OnSuccessNotNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessNotNull<TValue>(this ValueTask<Result<TValue>> result, Func<TValue, Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnFailed</c> and fails the build with an error pointing to <c>OnFailedAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result OnFailed(this Result result, Func<IReadOnlyCollection<IError>, Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnFailed</c> and fails the build with an error pointing to <c>OnFailedAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnFailed<TValue>(this Result<TValue> result, Func<IReadOnlyCollection<IError>, Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnFailed</c> and fails the build with an error pointing to <c>OnFailedAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result> OnFailed(this Task<Result> result, Func<IReadOnlyCollection<IError>, Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnFailed</c> and fails the build with an error pointing to <c>OnFailedAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnFailed<TValue>(this Task<Result<TValue>> result, Func<IReadOnlyCollection<IError>, Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnFailed</c> and fails the build with an error pointing to <c>OnFailedAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result> OnFailed(this ValueTask<Result> result, Func<IReadOnlyCollection<IError>, Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnFailed</c> and fails the build with an error pointing to <c>OnFailedAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnFailed<TValue>(this ValueTask<Result<TValue>> result, Func<IReadOnlyCollection<IError>, Task> func) => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result OnSuccess<TValueTask>(this Result result, Func<TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result> OnSuccess<TValueTask>(this Task<Result> result, Func<TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result> OnSuccess<TValueTask>(this ValueTask<Result> result, Func<TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccess<TValue, TValueTask>(this Result<TValue> result, Func<TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccess<TValue, TValueTask>(this Result<TValue> result, Func<TValue, TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccess<TValue, TValueTask>(this Task<Result<TValue>> result, Func<TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccess<TValue, TValueTask>(this Task<Result<TValue>> result, Func<TValue, TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccess<TValue, TValueTask>(this ValueTask<Result<TValue>> result, Func<TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccess</c> and fails the build with an error pointing to <c>OnSuccessAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccess<TValue, TValueTask>(this ValueTask<Result<TValue>> result, Func<TValue, TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNull</c> and fails the build with an error pointing to <c>OnSuccessNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccessNull<TValue, TValueTask>(this Result<TValue> result, Func<TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNull</c> and fails the build with an error pointing to <c>OnSuccessNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNull<TValue, TValueTask>(this Task<Result<TValue>> result, Func<TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNull</c> and fails the build with an error pointing to <c>OnSuccessNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessNull<TValue, TValueTask>(this ValueTask<Result<TValue>> result, Func<TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNotNull</c> and fails the build with an error pointing to <c>OnSuccessNotNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccessNotNull<TValue, TValueTask>(this Result<TValue> result, Func<TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNotNull</c> and fails the build with an error pointing to <c>OnSuccessNotNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccessNotNull<TValue, TValueTask>(this Result<TValue> result, Func<TValue, TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNotNull</c> and fails the build with an error pointing to <c>OnSuccessNotNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNotNull<TValue, TValueTask>(this Task<Result<TValue>> result, Func<TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNotNull</c> and fails the build with an error pointing to <c>OnSuccessNotNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNotNull<TValue, TValueTask>(this Task<Result<TValue>> result, Func<TValue, TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNotNull</c> and fails the build with an error pointing to <c>OnSuccessNotNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessNotNull<TValue, TValueTask>(this ValueTask<Result<TValue>> result, Func<TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnSuccessNotNull</c> and fails the build with an error pointing to <c>OnSuccessNotNullAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessNotNull<TValue, TValueTask>(this ValueTask<Result<TValue>> result, Func<TValue, TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnFailed</c> and fails the build with an error pointing to <c>OnFailedAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result OnFailed<TValueTask>(this Result result, Func<IReadOnlyCollection<IError>, TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnFailed</c> and fails the build with an error pointing to <c>OnFailedAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result> OnFailed<TValueTask>(this Task<Result> result, Func<IReadOnlyCollection<IError>, TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnFailed</c> and fails the build with an error pointing to <c>OnFailedAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result> OnFailed<TValueTask>(this ValueTask<Result> result, Func<IReadOnlyCollection<IError>, TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnFailed</c> and fails the build with an error pointing to <c>OnFailedAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnFailed<TValue, TValueTask>(this Result<TValue> result, Func<IReadOnlyCollection<IError>, TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnFailed</c> and fails the build with an error pointing to <c>OnFailedAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnFailed<TValue, TValueTask>(this Task<Result<TValue>> result, Func<IReadOnlyCollection<IError>, TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback is
    /// passed to the synchronous <c>OnFailed</c> and fails the build with an error pointing to <c>OnFailedAsync</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TValueTask">The <see cref="ValueTask"/> type returned by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(AsyncCallbackOnSyncMethod, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnFailed<TValue, TValueTask>(this ValueTask<Result<TValue>> result, Func<IReadOnlyCollection<IError>, TValueTask> func) where TValueTask : struct, IEquatable<ValueTask> => throw new NotSupportedException(AsyncCallbackOnSyncMethod);

    #endregion
}
