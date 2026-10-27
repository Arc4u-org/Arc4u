using System.Diagnostics.CodeAnalysis;
using Arc4u.Results;
using FluentResults;
using Microsoft.AspNetCore.Http;
using HttpResults = Microsoft.AspNetCore.Http.Results;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace Arc4u.AspNetCore.Results;
/// <summary>
/// Converts a <see cref="Result"/> or <see cref="Result{TValue}"/> (or a task of it) into an <see cref="IResult"/> for minimal API endpoints.
/// </summary>
/// <remarks>
/// <para>
/// The <c>ToHttpOkResult</c> family answers <c>200 OK</c> with the value (or <c>204 No Content</c> for a non-generic <see cref="Result"/>);
/// the <c>ToHttpCreatedResult</c> family answers <c>201 Created</c> with a location. A failed result is turned into a problem result built
/// from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> produced by <see cref="FromResultToProblemDetailExtension"/>.
/// </para>
/// <para>The overloads on a <see cref="Task"/> or <see cref="ValueTask"/> await it first; the ones on a plain result do not await anything. The overloads with a mapper project the value before it is written to the response.
/// Because the return type is <see cref="IResult"/>, the possible responses are not visible to OpenAPI; see <see cref="FromResultToTypedResultExtension"/> for the typed variant.</para>
/// </remarks>
/// <example>
/// <code>
/// app.MapGet("/orders/{id}", (int id, IOrderService orders) =&gt; orders.GetAsync(id).ToHttpOkResultAsync());
/// </code>
/// </example>
public static class FromResultToHttpResultExtension
{
    #region ActionResult

    #region ValueTask<Result<T>>

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="IResult"/> (minimal API): <c>200 OK</c> containing the value of the result on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>200</c> response with no content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async ValueTask<IResult>
    ToHttpOkResultAsync<TResult>(this ValueTask<Result<TResult>> result)
    {
        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, IResult>(
            value => value is null ? HttpResults.Ok() : HttpResults.Ok(value),
            _ => HttpResults.Problem(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="IResult"/> (minimal API): <c>200 OK</c> containing the value produced by <paramref name="mapper"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with no content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static async ValueTask<IResult>
    ToHttpOkResultAsync<TResult, T>(this ValueTask<Result<TResult>> result, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, IResult>(
            value => value is null ? HttpResults.Ok() : HttpResults.Ok(mapper(value)),
            _ => HttpResults.Problem(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="IResult"/> (minimal API): <c>200 OK</c> containing the value produced by <paramref name="asyncMapper"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with no content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<IResult>
    ToHttpOkResultAsync<TResult, T>(this ValueTask<Result<TResult>> result, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        var res = await result.ConfigureAwait(false);

        return await res.MatchAsync<TResult, IResult>(
            async value => value is null ? HttpResults.Ok() : HttpResults.Ok(await asyncMapper(value).ConfigureAwait(false)),
            _ => Task.FromResult<IResult>(HttpResults.Problem(res.ToProblemDetails())))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="IResult"/> (minimal API): <c>204 No Content</c> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async ValueTask<IResult>
    ToHttpOkResultAsync(this ValueTask<Result> result)
    {
        var res = await result.ConfigureAwait(false);

        return res.Match<IResult>(
            () => HttpResults.NoContent(),
            _ => HttpResults.Problem(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="mapper"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static async ValueTask<IResult>
    ToHttpCreatedResultAsync<TResult, T>(this ValueTask<Result<TResult>> result, Uri? location, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, IResult>(
            value => value is null ? HttpResults.Created((Uri?)null, default(T)) : HttpResults.Created(location, mapper(value)),
            _ => HttpResults.Problem(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="asyncMapper"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<IResult>
    ToHttpCreatedResultAsync<TResult, T>(this ValueTask<Result<TResult>> result, Uri? location, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        var res = await result.ConfigureAwait(false);

        return await res.MatchAsync<TResult, IResult>(
            async value => value is null ? HttpResults.Created((Uri?)null, default(T)) : HttpResults.Created(location, await asyncMapper(value).ConfigureAwait(false)),
            _ => Task.FromResult<IResult>(HttpResults.Problem(res.ToProblemDetails())))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the value task, then converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value of the result on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async ValueTask<IResult>
    ToHttpCreatedResultAsync<TResult>(this ValueTask<Result<TResult>> result, Uri? location)
    {
        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, IResult>(
            value => value is null ? HttpResults.Created((Uri?)null, default(TResult)) : HttpResults.Created(location, value),
            _ => HttpResults.Problem(res.ToProblemDetails()));
    }

    #endregion

    #region Task<Result<T>>

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="IResult"/> (minimal API): <c>200 OK</c> containing the value of the result on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>200</c> response with no content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async Task<IResult>
    ToHttpOkResultAsync<TResult>(this Task<Result<TResult>> result)
    {
        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, IResult>(
            value => value is null ? HttpResults.Ok() : HttpResults.Ok(value),
            _ => HttpResults.Problem(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="IResult"/> (minimal API): <c>200 OK</c> containing the value produced by <paramref name="mapper"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with no content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static async Task<IResult>
    ToHttpOkResultAsync<TResult, T>(this Task<Result<TResult>> result, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, IResult>(
            value => value is null ? HttpResults.Ok() : HttpResults.Ok(mapper(value)),
            _ => HttpResults.Problem(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="IResult"/> (minimal API): <c>200 OK</c> containing the value produced by <paramref name="asyncMapper"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with no content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<IResult>
    ToHttpOkResultAsync<TResult, T>(this Task<Result<TResult>> result, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        var res = await result.ConfigureAwait(false);

        return await res.MatchAsync<TResult, IResult>(
            async value => value is null ? HttpResults.Ok() : HttpResults.Ok(await asyncMapper(value).ConfigureAwait(false)),
            _ => Task.FromResult<IResult>(HttpResults.Problem(res.ToProblemDetails())))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="IResult"/> (minimal API): <c>204 No Content</c> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The task producing the result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async Task<IResult>
    ToHttpOkResultAsync(this Task<Result> result)
    {
        var res = await result.ConfigureAwait(false);

        return res.Match<IResult>(
            () => HttpResults.NoContent(),
            _ => HttpResults.Problem(res.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="IResult"/> (minimal API): <c>204 No Content</c> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Task<IResult>
    ToHttpOkResultAsync(this Result result)
    {
        return Task.FromResult(result.Match<IResult>(
            () => HttpResults.NoContent(),
            _ => HttpResults.Problem(result.ToProblemDetails())));
    }

    /// <summary>
    /// Converts the result to an <see cref="IResult"/> (minimal API): <c>200 OK</c> containing the value produced by <paramref name="asyncMapper"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with no content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<IResult>
    ToHttpOkResultAsync<TResult, T>(this Result<TResult> result, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        return await result.MatchAsync<TResult, IResult>(
            async value => value is null ? HttpResults.Ok() : HttpResults.Ok(await asyncMapper(value).ConfigureAwait(false)),
            _ => Task.FromResult<IResult>(HttpResults.Problem(result.ToProblemDetails())))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="mapper"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static async Task<IResult>
    ToHttpCreatedResultAsync<TResult, T>(this Task<Result<TResult>> result, Uri? location, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, IResult>(
            value => value is null ? HttpResults.Created((Uri?)null, default(T)) : HttpResults.Created(location, mapper(value)),
            _ => HttpResults.Problem(res.ToProblemDetails()));
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="asyncMapper"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<IResult>
    ToHttpCreatedResultAsync<TResult, T>(this Task<Result<TResult>> result, Uri? location, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        var res = await result.ConfigureAwait(false);

        return await res.MatchAsync<TResult, IResult>(
            async value => value is null ? HttpResults.Created((Uri?)null, default(T)) : HttpResults.Created(location, await asyncMapper(value).ConfigureAwait(false)),
            _ => Task.FromResult<IResult>(HttpResults.Problem(res.ToProblemDetails())))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value of the result on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async Task<IResult>
    ToHttpCreatedResultAsync<TResult>(this Task<Result<TResult>> result, Uri? location)
    {
        var res = await result.ConfigureAwait(false);

        return res.Match<TResult, IResult>(
            value => value is null ? HttpResults.Created((Uri?)null, default(TResult)) : HttpResults.Created(location, value),
            _ => HttpResults.Problem(res.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="mapper"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static Task<IResult>
    ToHttpCreatedResultAsync<TResult, T>(this Result<TResult> result, Uri? location, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return Task.FromResult(result.Match<TResult, IResult>(
            value => value is null ? TypedResults.Created((Uri?)null, default(T)) : TypedResults.Created(location, mapper(value)),
            _ => TypedResults.Problem(result.ToProblemDetails())));
    }

    /// <summary>
    /// Converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="asyncMapper"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<IResult>
    ToHttpCreatedResultAsync<TResult, T>(this Result<TResult> result, Uri? location, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        return await result.MatchAsync<TResult, IResult>(
            async value => value is null ? TypedResults.Created((Uri?)null, default(T)) : TypedResults.Created(location, await asyncMapper(value).ConfigureAwait(false)),
            _ => Task.FromResult<IResult>(TypedResults.Problem(result.ToProblemDetails())))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value of the result on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Task<IResult>
    ToHttpCreatedResultAsync<TResult>(this Result<TResult> result, Uri? location)
    {
        return Task.FromResult(result.Match<TResult, IResult>(
            value => value is null ? TypedResults.Created((Uri?)null, default(TResult)) : TypedResults.Created(location, value),
            _ => TypedResults.Problem(result.ToProblemDetails())));
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async Task<IResult>
    ToHttpCreatedResultAsync(this Task<Result> result, Uri? location)
    {
        var res = await result.ConfigureAwait(false);

        return res.Match<IResult>(
            () => TypedResults.Created(location),
            _ => TypedResults.Problem(res.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Task<IResult>
    ToHttpCreatedResultAsync(this Result result, Uri? location)
    {
        return Task.FromResult(result.Match<IResult>(
            () => TypedResults.Created(location),
            _ => TypedResults.Problem(result.ToProblemDetails())));
    }

    #endregion

    #region Result<T>

    /// <summary>
    /// Converts the result to an <see cref="IResult"/> (minimal API): <c>200 OK</c> containing the value of the result on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>200</c> response with no content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static IResult
    ToHttpOkResult<TResult>(this Result<TResult> result)
    {
        return result.Match<TResult, IResult>(
            value => value is null ? HttpResults.Ok() : HttpResults.Ok(value),
            _ => HttpResults.Problem(result.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="IResult"/> (minimal API): <c>200 OK</c> containing the value produced by <paramref name="mapper"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with no content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static IResult
    ToHttpOkResult<TResult, T>(this Result<TResult> result, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return result.Match<TResult, IResult>(
            value => value is null ? HttpResults.Ok() : HttpResults.Ok(mapper(value)),
            _ => HttpResults.Problem(result.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="mapper"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static IResult
    ToHttpCreatedResult<TResult, T>(this Result<TResult> result, Uri? location, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        return result.Match<TResult, IResult>(
            value => value is null ? HttpResults.Created((Uri?)null, default(T)) : HttpResults.Created(location, mapper(value)),
            _ => HttpResults.Problem(result.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value of the result on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static IResult
    ToHttpCreatedResult<TResult>(this Result<TResult> result, Uri? location)
    {
        return result.Match<TResult, IResult>(
            value => value is null ? HttpResults.Created((Uri?)null, default(TResult)) : HttpResults.Created(location, value),
            _ => HttpResults.Problem(result.ToProblemDetails()));
    }

    #endregion

    #region Result

    /// <summary>
    /// Converts the result to an <see cref="IResult"/> (minimal API): <c>204 No Content</c> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static IResult
    ToHttpOkResult(this Result result)
    {
        return result.Match<IResult>(
            () => HttpResults.NoContent(),
            _ => HttpResults.Problem(result.ToProblemDetails()));
    }

    /// <summary>
    /// Converts the result to an <see cref="IResult"/> (minimal API): <c>201 Created</c> with the given <paramref name="location"/> on success, a problem <see cref="IResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static IResult
    ToHttpCreatedResult(this Result result, Uri? location)
    {
        return result.Match<IResult>(
            () => TypedResults.Created(location),
            _ => TypedResults.Problem(result.ToProblemDetails()));
    }

    #endregion

    #endregion

}
