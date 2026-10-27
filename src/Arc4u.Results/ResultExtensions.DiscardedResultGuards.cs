using System.ComponentModel;
using FluentResults;

namespace Arc4u.Results;

public static partial class ResultExtension
{
    private const string DiscardedResultCallback =
        "This callback returns a result, but OnSuccess, OnSuccessNull, OnSuccessNotNull and OnFailed only run an action " +
        "and return the original result unchanged: the result of the callback, and its errors, would be discarded. Use Then " +
        "for a step producing the next result, Check for a step that can fail but keeps the value, Compensate for a clean-up " +
        "after a failure that can itself fail, or Recover to replace a failure. To discard the result on purpose, use a " +
        "statement lambda: '() => { Step(); }'.";

    #region discarded result guards

    // These overloads exist only to be picked by overload resolution when a callback returning a result is handed
    // to OnSuccess*, OnFailed or their *Async twins, and to fail the compilation with an actionable message. Without them, a lambda
    // such as 'v => v.Update()' binds to the Action overload and its result, failed or not, is silently dropped.
    // A lambda returning a value is preferred to an Action, so a statement lambda ('v => { v.Update(); }') is the
    // explicit way to discard the result.
    //
    // The synchronous guards take Func<..., IResultBase>: every result converts to it, and the shape differs from the
    // ValueTask guards, which are generic with the same arity. The asynchronous guards are generic on TResult because
    // Task<Result> does not convert to Task<IResultBase>; a task producing anything else is not caught.

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccess</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result OnSuccess(this Result result, Func<IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccess</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result> OnSuccess(this Task<Result> result, Func<IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccess</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result> OnSuccess(this ValueTask<Result> result, Func<IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccess</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccess<TValue>(this Result<TValue> result, Func<IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccess</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccess<TValue>(this Result<TValue> result, Func<TValue, IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccess</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccess<TValue>(this Task<Result<TValue>> result, Func<IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccess</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccess<TValue>(this Task<Result<TValue>> result, Func<TValue, IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccess</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccess<TValue>(this ValueTask<Result<TValue>> result, Func<IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccess</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccess<TValue>(this ValueTask<Result<TValue>> result, Func<TValue, IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccessNull</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccessNull<TValue>(this Result<TValue> result, Func<IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccessNull</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNull<TValue>(this Task<Result<TValue>> result, Func<IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccessNull</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessNull<TValue>(this ValueTask<Result<TValue>> result, Func<IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccessNotNull</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccessNotNull<TValue>(this Result<TValue> result, Func<IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccessNotNull</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnSuccessNotNull<TValue>(this Result<TValue> result, Func<TValue, IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccessNotNull</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNotNull<TValue>(this Task<Result<TValue>> result, Func<IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccessNotNull</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNotNull<TValue>(this Task<Result<TValue>> result, Func<TValue, IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccessNotNull</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessNotNull<TValue>(this ValueTask<Result<TValue>> result, Func<IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnSuccessNotNull</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessNotNull<TValue>(this ValueTask<Result<TValue>> result, Func<TValue, IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnFailed</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result OnFailed(this Result result, Func<IReadOnlyCollection<IError>, IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnFailed</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result> OnFailed(this Task<Result> result, Func<IReadOnlyCollection<IError>, IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnFailed</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result> OnFailed(this ValueTask<Result> result, Func<IReadOnlyCollection<IError>, IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnFailed</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Result<TValue> OnFailed<TValue>(this Result<TValue> result, Func<IReadOnlyCollection<IError>, IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnFailed</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnFailed<TValue>(this Task<Result<TValue>> result, Func<IReadOnlyCollection<IError>, IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when a callback returning a result is passed to
    /// <c>OnFailed</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnFailed<TValue>(this ValueTask<Result<TValue>> result, Func<IReadOnlyCollection<IError>, IResultBase> func) => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result> OnSuccessAsync<TResult>(this Result result, Func<Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result> OnSuccessAsync<TResult>(this Task<Result> result, Func<Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result> OnSuccessAsync<TResult>(this ValueTask<Result> result, Func<Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessAsync<TValue, TResult>(this Result<TValue> result, Func<Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessAsync<TValue, TResult>(this Result<TValue> result, Func<TValue, Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessAsync<TValue, TResult>(this Task<Result<TValue>> result, Func<Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessAsync<TValue, TResult>(this Task<Result<TValue>> result, Func<TValue, Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessAsync<TValue, TResult>(this ValueTask<Result<TValue>> result, Func<Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessAsync<TValue, TResult>(this ValueTask<Result<TValue>> result, Func<TValue, Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessNullAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNullAsync<TValue, TResult>(this Result<TValue> result, Func<Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessNullAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNullAsync<TValue, TResult>(this Task<Result<TValue>> result, Func<Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessNullAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessNullAsync<TValue, TResult>(this ValueTask<Result<TValue>> result, Func<Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessNotNullAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNotNullAsync<TValue, TResult>(this Result<TValue> result, Func<Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessNotNullAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNotNullAsync<TValue, TResult>(this Result<TValue> result, Func<TValue, Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessNotNullAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNotNullAsync<TValue, TResult>(this Task<Result<TValue>> result, Func<Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessNotNullAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnSuccessNotNullAsync<TValue, TResult>(this Task<Result<TValue>> result, Func<TValue, Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessNotNullAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessNotNullAsync<TValue, TResult>(this ValueTask<Result<TValue>> result, Func<Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnSuccessNotNullAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnSuccessNotNullAsync<TValue, TResult>(this ValueTask<Result<TValue>> result, Func<TValue, Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnFailedAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result> OnFailedAsync<TResult>(this Result result, Func<IReadOnlyCollection<IError>, Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnFailedAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result> OnFailedAsync<TResult>(this Task<Result> result, Func<IReadOnlyCollection<IError>, Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnFailedAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result> OnFailedAsync<TResult>(this ValueTask<Result> result, Func<IReadOnlyCollection<IError>, Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnFailedAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnFailedAsync<TValue, TResult>(this Result<TValue> result, Func<IReadOnlyCollection<IError>, Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnFailedAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static Task<Result<TValue>> OnFailedAsync<TValue, TResult>(this Task<Result<TValue>> result, Func<IReadOnlyCollection<IError>, Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    /// <summary>
    /// Compile-time guard, not a usable API: it is selected by overload resolution when an asynchronous callback whose task produces a result is passed to
    /// <c>OnFailedAsync</c>, which would discard that result, and fails the build with an error pointing to <c>Then</c>.
    /// </summary>
    /// <remarks>Marked <c>[Obsolete(error: true)]</c> and hidden from IntelliSense.</remarks>
    /// <typeparam name="TValue">The type of the value carried by the result.</typeparam>
    /// <typeparam name="TResult">The type of the result produced by the callback.</typeparam>
    /// <param name="result">The value task producing the result to inspect.</param>
    /// <param name="func">The callback; never executed.</param>
    /// <returns>Never returns.</returns>
    /// <exception cref="NotSupportedException">Always thrown if the guard were ever executed.</exception>
    [Obsolete(DiscardedResultCallback, error: true)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static ValueTask<Result<TValue>> OnFailedAsync<TValue, TResult>(this ValueTask<Result<TValue>> result, Func<IReadOnlyCollection<IError>, Task<TResult>> func) where TResult : IResultBase => throw new NotSupportedException(DiscardedResultCallback);

    #endregion
}
