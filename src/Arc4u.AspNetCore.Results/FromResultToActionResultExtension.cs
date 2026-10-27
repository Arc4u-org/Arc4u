using System.Diagnostics.CodeAnalysis;
using Arc4u.Results;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Arc4u.AspNetCore.Results;
/// <summary>
/// Converts a <see cref="Result"/> or <see cref="Result{TValue}"/> (or a task of it) into an MVC <see cref="ActionResult"/> for controller actions.
/// </summary>
/// <remarks>
/// <para>
/// The <c>ToActionOkResult</c> family answers <c>200 OK</c> with the value (or <c>204 No Content</c> for a non-generic <see cref="Result"/>);
/// the <c>ToActionCreatedResult</c> family answers <c>201 Created</c> with a location. A failed result is turned into an
/// <see cref="ObjectResult"/> whose value is the <see cref="ProblemDetails"/> built by <see cref="FromResultToProblemDetailExtension"/>.
/// </para>
/// <para>The overloads on a <see cref="Task"/> or <see cref="ValueTask"/> await it first; the ones on a plain result do not await anything. The overloads with a mapper project the value before it is written to the response.</para>
/// </remarks>
/// <example>
/// <code>
/// [HttpGet("{id}")]
/// public Task&lt;ActionResult&lt;OrderDto&gt;&gt; Get(int id)
///     =&gt; _orders.GetAsync(id).ToActionOkResultAsync();   // _orders.GetAsync returns Task&lt;Result&lt;OrderDto&gt;&gt;
///
/// [HttpPost]
/// public Task&lt;ActionResult&lt;OrderDto&gt;&gt; Create(NewOrder order)
///     =&gt; _orders.CreateAsync(order).ToActionCreatedResultAsync(new Uri("/orders", UriKind.Relative));
/// </code>
/// </example>
public static class FromResultToActionResultExtension
{
    #region ActionResult

    #region ValueTask<Result<T>>

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="ActionResult"/>: <c>200 OK</c> containing the value of the result on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async ValueTask<ActionResult<TResult>>
    ToActionOkResultAsync<TResult>(this ValueTask<Result<TResult>> result)
    {
        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, ActionResult>(
            value => value is null ? new OkObjectResult(default(TResult)) : new OkObjectResult(value),
            _ => new ObjectResult(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="ActionResult"/>: <c>200 OK</c> containing the value produced by <paramref name="mapper"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static async ValueTask<ActionResult<T>>
    ToActionOkResultAsync<TResult, T>(this ValueTask<Result<TResult>> result, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, ActionResult>(
            value => value is null ? new OkObjectResult(default(TResult)) : new OkObjectResult(mapper(value)),
            _ => new ObjectResult(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="ActionResult"/>: <c>200 OK</c> containing the value produced by <paramref name="asyncMapper"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<ActionResult<T>>
    ToActionOkResultAsync<TResult, T>(this ValueTask<Result<TResult>> result, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        var res = await result.ConfigureAwait(false);

        return await res.MatchAsync<TResult, ActionResult>(
            async value => value is null ? new OkObjectResult(default(T)) : new OkObjectResult(await asyncMapper(value).ConfigureAwait(false)),
            _ => Task.FromResult<ActionResult>(new ObjectResult(res.ToProblemDetails())))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="ActionResult"/>: <c>204 No Content</c> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async ValueTask<ActionResult>
    ToActionOkResultAsync(this ValueTask<Result> result)
    {
        var res = await result.ConfigureAwait(false);

        return res.Match<ActionResult>(
            () => new NoContentResult(),
            _ => new ObjectResult(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="mapper"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> object result with a null body and no location. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static async ValueTask<ActionResult<T>>
    ToActionCreatedResultAsync<TResult, T>(this ValueTask<Result<TResult>> result, Uri? location, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, ActionResult>(
            value => value is null ? new ObjectResult(default(T)) { StatusCode = StatusCodes.Status201Created } : new CreatedResult(location, mapper(value)),
            _ => new ObjectResult(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="asyncMapper"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> object result with a null body and no location. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<ActionResult<T>>
    ToActionCreatedResultAsync<TResult, T>(this ValueTask<Result<TResult>> result, Uri? location, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        var res = await result.ConfigureAwait(false);

        return await res.MatchAsync<TResult, ActionResult>(
            async value => value is null ? new ObjectResult(default(T)) { StatusCode = StatusCodes.Status201Created } : new CreatedResult(location, await asyncMapper(value).ConfigureAwait(false)),
            _ => Task.FromResult<ActionResult>(new ObjectResult(res.ToProblemDetails())))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> and the value of the result on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>201</c> object result with a null body and no location. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async ValueTask<ActionResult<TResult>>
    ToActionCreatedResultAsync<TResult>(this ValueTask<Result<TResult>> result, Uri? location)
    {
        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, ActionResult>(
            value => value is null ? new ObjectResult(default(TResult)) { StatusCode = StatusCodes.Status201Created } : new CreatedResult(location, value),
            _ => new ObjectResult(res.ToProblemDetails()));
    }

    #endregion

    #region Task<Result<T>>

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="ActionResult"/>: <c>200 OK</c> containing the value of the result on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async Task<ActionResult<TResult>>
    ToActionOkResultAsync<TResult>(this Task<Result<TResult>> result)
    {

        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, ActionResult>(
            value => value is null ? new OkObjectResult(default(TResult)) : new OkObjectResult(value),
            _ => new ObjectResult(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="ActionResult"/>: <c>200 OK</c> containing the value produced by <paramref name="mapper"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static async Task<ActionResult<T>>
    ToActionOkResultAsync<TResult, T>(this Task<Result<TResult>> result, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, ActionResult<T>>(
            value => value is null ? new OkObjectResult(default(TResult)) : new OkObjectResult(mapper(value)),
            _ => new ObjectResult(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="ActionResult"/>: <c>200 OK</c> containing the value produced by <paramref name="asyncMapper"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<ActionResult<T>>
    ToActionOkResultAsync<TResult, T>(this Task<Result<TResult>> result, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        var res = await result.ConfigureAwait(false);

        return await res.MatchAsync<TResult, ActionResult<T>>(
            async value => value is null ? new OkObjectResult(default(T)) : new OkObjectResult(await asyncMapper(value).ConfigureAwait(false)),
            _ => Task.FromResult<ActionResult<T>>(new ObjectResult(res.ToProblemDetails())))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="ActionResult"/>: <c>204 No Content</c> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The task producing the result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async Task<ActionResult>
    ToActionOkResultAsync(this Task<Result> result)
    {
        var res = await result.ConfigureAwait(false);

        return res.Match<ActionResult>(
            () => new NoContentResult(),
            _ => new ObjectResult(res.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="ActionResult"/>: <c>204 No Content</c> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Task<ActionResult>
    ToActionOkResultAsync(this Result result)
    {
        return Task.FromResult(result.Match<ActionResult>(
            () => new NoContentResult(),
            _ => new ObjectResult(result.ToProblemDetails())));
    }

    /// <summary>
    /// Converts the result to an <see cref="ActionResult"/>: <c>200 OK</c> containing the value produced by <paramref name="asyncMapper"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<ActionResult<T>>
    ToActionOkResultAsync<TResult, T>(this Result<TResult> result, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        return await result.MatchAsync<TResult, ActionResult<T>>(
            async value => value is null ? new OkObjectResult(default(T)) : new OkObjectResult(await asyncMapper(value).ConfigureAwait(false)),
            _ => Task.FromResult<ActionResult<T>>(new ObjectResult(result.ToProblemDetails())))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="mapper"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> object result with a null body and no location. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static async Task<ActionResult<T>>
    ToActionCreatedResultAsync<TResult, T>(this Task<Result<TResult>> result, Uri? location, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, ActionResult<T>>(
            value => value is null ? new ObjectResult(default(T)) { StatusCode = StatusCodes.Status201Created } : new CreatedResult(location, mapper(value)),
            _ => new ObjectResult(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="asyncMapper"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> object result with a null body and no location. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<ActionResult<T>>
    ToActionCreatedResultAsync<TResult, T>(this Task<Result<TResult>> result, Uri? location, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        var res = await result.ConfigureAwait(false);

        return await res.MatchAsync<TResult, ActionResult<T>>(
            async value => value is null ? new ObjectResult(default(T)) { StatusCode = StatusCodes.Status201Created } : new CreatedResult(location, await asyncMapper(value).ConfigureAwait(false)),
            _ => Task.FromResult<ActionResult<T>>(new ObjectResult(res.ToProblemDetails())))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> and the value of the result on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>201</c> object result with a null body and no location. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async Task<ActionResult<TResult>>
    ToActionCreatedResultAsync<TResult>(this Task<Result<TResult>> result, Uri? location)
    {
        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, ActionResult<TResult>>(
            value => value is null ? new ObjectResult(default(TResult)) { StatusCode = StatusCodes.Status201Created } : new CreatedResult(location, value),
            _ => new ObjectResult(res.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="mapper"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> object result with a null body and no location. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static Task<ActionResult>
    ToActionCreatedResultAsync<TResult, T>(this Result<TResult> result, Uri? location, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return Task.FromResult(result.Match<TResult, ActionResult>(
            value => value is null ? new ObjectResult(default(T)) { StatusCode = StatusCodes.Status201Created } : new CreatedResult(location, mapper(value)),
            _ => new ObjectResult(result.ToProblemDetails())));
    }

    /// <summary>
    /// Converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="asyncMapper"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> object result with a null body and no location. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<ActionResult>
    ToActionCreatedResultAsync<TResult, T>(this Result<TResult> result, Uri? location, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        return await result.MatchAsync<TResult, ActionResult>(
            async value => value is null ? new ObjectResult(default(T)) { StatusCode = StatusCodes.Status201Created } : new CreatedResult(location, await asyncMapper(value).ConfigureAwait(false)),
            _ => Task.FromResult<ActionResult>(new ObjectResult(result.ToProblemDetails())))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> and the value of the result on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>201</c> object result with a null body and no location. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Task<ActionResult>
    ToActionCreatedResultAsync<TResult>(this Result<TResult> result, Uri? location)
    {
        return Task.FromResult(result.Match<TResult, ActionResult>(
            value => value is null ? new ObjectResult(default(TResult)) { StatusCode = StatusCodes.Status201Created } : new CreatedResult(location, value),
            _ => new ObjectResult(result.ToProblemDetails())));
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async Task<ActionResult>
    ToActionCreatedResultAsync(this Task<Result> result, Uri? location)
    {
        var res = await result.ConfigureAwait(false);

        return res.Match<ActionResult>(
            () => new CreatedResult(location, null),
            _ => new ObjectResult(res.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Task<ActionResult>
    ToActionCreatedResultAsync(this Result result, Uri? location)
    {
        return Task.FromResult(result.Match<ActionResult>(
            () => new CreatedResult(location, null),
            _ => new ObjectResult(result.ToProblemDetails())));
    }

    #endregion

    #region Result<T>

    /// <summary>
    /// Converts the result to an <see cref="ActionResult"/>: <c>200 OK</c> containing the value of the result on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static ActionResult<TResult>
    ToActionOkResult<TResult>(this Result<TResult> result)
    {
        return result.Match<TResult, ActionResult<TResult>>(
            value => value is null ? new OkObjectResult(default(TResult)) : new OkObjectResult(value),
            _ => new ObjectResult(result.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="ActionResult"/>: <c>200 OK</c> containing the value produced by <paramref name="mapper"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static ActionResult<T>
    ToActionOkResult<TResult, T>(this Result<TResult> result, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return result.Match<TResult, ActionResult<T>>(
            value => value is null ? new OkObjectResult(default(TResult)) : new OkObjectResult(mapper(value)),
            _ => new ObjectResult(result.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="mapper"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> object result with a null body and no location. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static ActionResult<T>
    ToActionCreatedResult<TResult, T>(this Result<TResult> result, Uri? location, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return result.Match<TResult, ActionResult<T>>(
            value => value is null ? new ObjectResult(default(T)) { StatusCode = StatusCodes.Status201Created } : new CreatedResult(location, mapper(value)),
            _ => new ObjectResult(result.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> and the value of the result on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>201</c> object result with a null body and no location. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static ActionResult<TResult>
    ToActionCreatedResult<TResult>(this Result<TResult> result, Uri? location)
    {
        return result.Match<TResult, ActionResult<TResult>>(
            value => value is null ? new ObjectResult(default(TResult)) { StatusCode = StatusCodes.Status201Created } : new CreatedResult(location, value),
            _ => new ObjectResult(result.ToProblemDetails()));
    }

    #endregion

    #region Result

    /// <summary>
    /// Converts the result to an <see cref="ActionResult"/>: <c>204 No Content</c> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static ActionResult
    ToActionOkResult(this Result result)
    {
        return result.Match<ActionResult>(
            () => new NoContentResult(),
            _ => new ObjectResult(result.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static ActionResult
    ToActionCreatedResult(this Result result, Uri? location)
    {
        return result.Match<ActionResult>(
            () => new CreatedResult(location, null),
            _ => new ObjectResult(result.ToProblemDetails()));
    }

    #endregion

    #endregion
}
