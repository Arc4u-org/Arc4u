using System.Diagnostics.CodeAnalysis;
using Arc4u.Results;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Arc4u.AspNetCore.Results;

/// <summary>
/// Converts a <see cref="Result"/> or <see cref="Result{TValue}"/> (or a task of it) into a typed <c>Results&lt;...&gt;</c> union for minimal API endpoints.
/// </summary>
/// <remarks>
/// <para>
/// The <c>ToTypedOkResult</c> family answers <c>Ok&lt;T&gt;</c> (or <c>NoContent</c> for a non-generic <see cref="Result"/>);
/// the <c>ToTypedCreatedResult</c> family answers <c>Created</c>. A failed result is a <see cref="ProblemHttpResult"/> built from the
/// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> produced by <see cref="FromResultToProblemDetailExtension"/>. The <c>ValidationProblem</c> member of the union is
/// there so that the endpoint metadata advertises the validation response; the conversion itself only produces a <see cref="ProblemHttpResult"/>.
/// </para>
/// <para>The overloads on a <see cref="Task"/> or <see cref="ValueTask"/> await it first; the ones on a plain result do not await anything. The overloads with a mapper project the value before it is written to the response.</para>
/// </remarks>
/// <example>
/// <code>
/// app.MapGet("/orders/{id}", (int id, IOrderService orders) =&gt; orders.GetAsync(id).ToTypedOkResultAsync());
/// </code>
/// </example>
public static class FromResultToTypedResultExtension
{
    #region ActionResult

    #region ValueTask<Result<T>>

    /// <summary>
    /// Awaits the value task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>200 OK</c> containing the value of the result on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async ValueTask<Results<Ok<TResult>, ProblemHttpResult, ValidationProblem>>
   ToTypedOkResultAsync<TResult>(this ValueTask<Result<TResult>> result)
    {

        var res = await result.ConfigureAwait(false);

        Results<Ok<TResult>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        res
            .OnSuccessNotNull(value => objectResult = TypedResults.Ok(value))
            .OnSuccessNull(() => objectResult = TypedResults.Ok(default(TResult)))
            .OnFailed(errors => objectResult = TypedResults.Problem(res.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Awaits the value task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>200 OK</c> containing the value produced by <paramref name="mapper"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Results<Ok<T>, ProblemHttpResult, ValidationProblem>>
    ToTypedOkResultAsync<TResult, T>(this ValueTask<Result<TResult>> result, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var res = await result.ConfigureAwait(false);

        Results<Ok<T>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        res
            .OnSuccessNotNull(value => objectResult = TypedResults.Ok(mapper(value)))
            .OnSuccessNull(() => objectResult = TypedResults.Ok(default(T)))
            .OnFailed(errors => objectResult = TypedResults.Problem(res.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Awaits the value task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>200 OK</c> containing the value produced by <paramref name="asyncMapper"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<Results<Ok<T>, ProblemHttpResult, ValidationProblem>>
    ToTypedOkResultAsync<TResult, T>(this ValueTask<Result<TResult>> result, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        var res = await result.ConfigureAwait(false);

        Results<Ok<T>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();

        if (res.IsSuccess)
        {
            if (res.Value is null)
            {
                objectResult = TypedResults.Ok(default(T));
            }
            else
            {
                var mapped = await asyncMapper(res.Value).ConfigureAwait(false);
                objectResult = TypedResults.Ok(mapped);
            }
        }
        else
        {
            objectResult = TypedResults.Problem(res.ToProblemDetails());
        }

        return objectResult;
    }

    /// <summary>
    /// Awaits the value task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>204 No Content</c> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async ValueTask<Results<NoContent, ProblemHttpResult, ValidationProblem>>
    ToTypedOkResultAsync(this ValueTask<Result> result)
    {
        var res = await result.ConfigureAwait(false);

        Results<NoContent, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        res
            .OnSuccess(() => objectResult = TypedResults.NoContent())
            .OnFailed(errors => objectResult = TypedResults.Problem(res.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Awaits the value task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="mapper"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static async ValueTask<Results<Created<T>, ProblemHttpResult, ValidationProblem>>
    ToTypedCreatedResultAsync<TResult, T>(this ValueTask<Result<TResult>> result, Uri? location, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var res = await result.ConfigureAwait(false);

        Results<Created<T>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        res
            .OnSuccessNotNull(value => objectResult = TypedResults.Created(location, mapper(value)))
            .OnSuccessNull(() => objectResult = TypedResults.Created((Uri?)null, default(T)))
            .OnFailed(errors => objectResult = TypedResults.Problem(res.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Awaits the value task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="asyncMapper"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<Results<Created<T>, ProblemHttpResult, ValidationProblem>>
    ToTypedCreatedResultAsync<TResult, T>(this ValueTask<Result<TResult>> result, Uri? location, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        var res = await result.ConfigureAwait(false);

        Results<Created<T>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();

        if (res.IsSuccess)
        {
            if (res.Value is null)
            {
                objectResult = TypedResults.Created((Uri?)null, default(T));
            }
            else
            {
                var mapped = await asyncMapper(res.Value).ConfigureAwait(false);
                objectResult = TypedResults.Created(location, mapped);
            }
        }
        else
        {
            objectResult = TypedResults.Problem(res.ToProblemDetails());
        }

        return objectResult;
    }

    /// <summary>
    /// Awaits the value task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value of the result on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async ValueTask<Results<Created<TResult>, ProblemHttpResult, ValidationProblem>>
    ToTypedCreatedResultAsync<TResult>(this ValueTask<Result<TResult>> result, Uri? location)
    {
        var res = await result.ConfigureAwait(false);

        Results<Created<TResult>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        res
            .OnSuccessNotNull(value => objectResult = TypedResults.Created(location, value))
            .OnSuccessNull(() => objectResult = TypedResults.Created((Uri?)null, default(TResult)))
            .OnFailed(errors => objectResult = TypedResults.Problem(res.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Awaits the value task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">Unused, but it must be specified explicitly because it cannot be inferred.</typeparam>
    /// <param name="result">The value task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async ValueTask<Results<Created, ProblemHttpResult, ValidationProblem>>
    ToTypedCreatedResultAsync<TResult>(this ValueTask<Result> result, Uri? location)
    {
        var res = await result.ConfigureAwait(false);

        Results<Created, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        res
            .OnSuccess(() => objectResult = TypedResults.Created(location))
            .OnFailed(errors => objectResult = TypedResults.Problem(res.ToProblemDetails()));

        return objectResult;
    }

    #endregion

    #region Task<Result> Task<Result<T>>

    /// <summary>
    /// Awaits the task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>200 OK</c> containing the value of the result on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async Task<Results<Ok<TResult>, ProblemHttpResult, ValidationProblem>>
    ToTypedOkResultAsync<TResult>(this Task<Result<TResult>> result)
    {

        var res = await result.ConfigureAwait(false);

        Results<Ok<TResult>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        res
            .OnSuccessNotNull(value => objectResult = TypedResults.Ok(value))
            .OnSuccessNull(() => objectResult = TypedResults.Ok(default(TResult)))
            .OnFailed(errors => objectResult = TypedResults.Problem(res.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Awaits the task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>200 OK</c> containing the value produced by <paramref name="mapper"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static async Task<Results<Ok<T>, ProblemHttpResult, ValidationProblem>>
    ToTypedOkResultAsync<TResult, T>(this Task<Result<TResult>> result, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var res = await result.ConfigureAwait(false);

        Results<Ok<T>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        res
            .OnSuccessNotNull(value => objectResult = TypedResults.Ok(mapper(value)))
            .OnSuccessNull(() => objectResult = TypedResults.Ok(default(T)))
            .OnFailed(errors => objectResult = TypedResults.Problem(res.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Awaits the task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>200 OK</c> containing the value produced by <paramref name="asyncMapper"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<Results<Ok<T>, ProblemHttpResult, ValidationProblem>>
    ToTypedOkResultAsync<TResult, T>(this Task<Result<TResult>> result, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        var res = await result.ConfigureAwait(false);

        Results<Ok<T>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();

        if (res.IsSuccess)
        {
            if (res.Value is null)
            {
                objectResult = TypedResults.Ok(default(T));
            }
            else
            {
                var mapped = await asyncMapper(res.Value).ConfigureAwait(false);
                objectResult = TypedResults.Ok(mapped);
            }
        }
        else
        {
            objectResult = TypedResults.Problem(res.ToProblemDetails());
        }

        return objectResult;
    }

    /// <summary>
    /// Awaits the task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>204 No Content</c> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The task producing the result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async Task<Results<NoContent, ProblemHttpResult, ValidationProblem>>
    ToTypedOkResultAsync(this Task<Result> result)
    {
        var res = await result.ConfigureAwait(false);

        Results<NoContent, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        res
            .OnSuccess(() => objectResult = TypedResults.NoContent())
            .OnFailed(errors => objectResult = TypedResults.Problem(res.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>204 No Content</c> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Task<Results<NoContent, ProblemHttpResult, ValidationProblem>>
    ToTypedOkResultAsync(this Result result)
    {
        Results<NoContent, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();

        result
              .OnSuccess(() => objectResult = TypedResults.NoContent())
              .OnFailed(errors => objectResult = TypedResults.Problem(result.ToProblemDetails()));

        return Task.FromResult(objectResult);
    }

    /// <summary>
    /// Converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>200 OK</c> containing the value produced by <paramref name="asyncMapper"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<Results<Ok<T>, ProblemHttpResult, ValidationProblem>>
    ToTypedOkResultAsync<TResult, T>(this Result<TResult> result, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        Results<Ok<T>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();

        if (result.IsSuccess)
        {
            if (result.Value is null)
            {
                objectResult = TypedResults.Ok(default(T));
            }
            else
            {
                var mapped = await asyncMapper(result.Value).ConfigureAwait(false);
                objectResult = TypedResults.Ok(mapped);
            }
        }
        else
        {
            objectResult = TypedResults.Problem(result.ToProblemDetails());
        }

        return objectResult;
    }

    /// <summary>
    /// Awaits the task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="mapper"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static async Task<Results<Created<T>, ProblemHttpResult, ValidationProblem>>
    ToTypedCreatedResultAsync<TResult, T>(this Task<Result<TResult>> result, Uri? location, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        var res = await result.ConfigureAwait(false);

        Results<Created<T>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        res
            .OnSuccessNotNull(value => objectResult = TypedResults.Created(location, mapper(value)))
            .OnSuccessNull(() => objectResult = TypedResults.Created((Uri?)null, default(T)))
            .OnFailed(errors => objectResult = TypedResults.Problem(res.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Awaits the task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="asyncMapper"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<Results<Created<T>, ProblemHttpResult, ValidationProblem>>
    ToTypedCreatedResultAsync<TResult, T>(this Task<Result<TResult>> result, Uri? location, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        var res = await result.ConfigureAwait(false);

        Results<Created<T>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();

        if (res.IsSuccess)
        {
            if (res.Value is null)
            {
                objectResult = TypedResults.Created((Uri?)null, default(T));
            }
            else
            {
                var mapped = await asyncMapper(res.Value).ConfigureAwait(false);
                objectResult = TypedResults.Created(location, mapped);
            }
        }
        else
        {
            objectResult = TypedResults.Problem(res.ToProblemDetails());
        }

        return objectResult;
    }

    /// <summary>
    /// Awaits the task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value of the result on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async Task<Results<Created<TResult>, ProblemHttpResult, ValidationProblem>>
    ToTypedCreatedResultAsync<TResult>(this Task<Result<TResult>> result, Uri? location)
    {
        var res = await result.ConfigureAwait(false);

        Results<Created<TResult>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        res
            .OnSuccessNotNull(value => objectResult = TypedResults.Created(location, value))
            .OnSuccessNull(() => objectResult = TypedResults.Created((Uri?)null, default(TResult)))
            .OnFailed(errors => objectResult = TypedResults.Problem(res.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="mapper"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static Task<Results<Created<T>, ProblemHttpResult, ValidationProblem>>
    ToTypedCreatedResultAsync<TResult, T>(this Result<TResult> result, Uri? location, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        Results<Created<T>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        result
            .OnSuccessNotNull(value => objectResult = TypedResults.Created(location, mapper(value)))
            .OnSuccessNull(() => objectResult = TypedResults.Created((Uri?)null, default(T)))
            .OnFailed(errors => objectResult = TypedResults.Problem(result.ToProblemDetails()));

        return Task.FromResult(objectResult);
    }

    /// <summary>
    /// Converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="asyncMapper"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="asyncMapper">Asynchronously maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="asyncMapper"/> is <see langword="null"/>.</exception>
    public static async Task<Results<Created<T>, ProblemHttpResult, ValidationProblem>>
    ToTypedCreatedResultAsync<TResult, T>(this Result<TResult> result, Uri? location, [DisallowNull] Func<TResult, Task<T>> asyncMapper)
    {
        ArgumentNullException.ThrowIfNull(asyncMapper);

        Results<Created<T>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();

        if (result.IsSuccess)
        {
            if (result.Value is null)
            {
                objectResult = TypedResults.Created((Uri?)null, default(T));
            }
            else
            {
                var mapped = await asyncMapper(result.Value).ConfigureAwait(false);
                objectResult = TypedResults.Created(location, mapped);
            }
        }
        else
        {
            objectResult = TypedResults.Problem(result.ToProblemDetails());
        }

        return objectResult;
    }

    /// <summary>
    /// Converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value of the result on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Task<Results<Created<TResult>, ProblemHttpResult, ValidationProblem>>
    ToTypedCreatedResultAsync<TResult>(this Result<TResult> result, Uri? location)
    {
        Results<Created<TResult>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        result
            .OnSuccessNotNull(value => objectResult = TypedResults.Created(location, value))
            .OnSuccessNull(() => objectResult = TypedResults.Created((Uri?)null, default(TResult)))
            .OnFailed(errors => objectResult = TypedResults.Problem(result.ToProblemDetails()));

        return Task.FromResult(objectResult);
    }

    /// <summary>
    /// Awaits the task, then converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">Unused, but it must be specified explicitly because it cannot be inferred.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async Task<Results<Created, ProblemHttpResult, ValidationProblem>>
    ToTypedCreatedResultAsync<TResult>(this Task<Result> result, Uri? location)
    {
        var res = await result.ConfigureAwait(false);

        Results<Created, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        res
            .OnSuccess(() => objectResult = TypedResults.Created(location))
            .OnFailed(errors => objectResult = TypedResults.Problem(res.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">Unused, but it must be specified explicitly because it cannot be inferred.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Task<Results<Created, ProblemHttpResult, ValidationProblem>>
    ToTypedCreatedResultAsync<TResult>(this Result result, Uri? location)
    {
        Results<Created, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        result
            .OnSuccess(() => objectResult = TypedResults.Created(location))
            .OnFailed(errors => objectResult = TypedResults.Problem(result.ToProblemDetails()));

        return Task.FromResult(objectResult);
    }

    #endregion

    #region Result<T>

    /// <summary>
    /// Converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>200 OK</c> containing the value of the result on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Results<Ok<TResult>, ProblemHttpResult, ValidationProblem>
    ToTypedOkResult<TResult>(this Result<TResult> result)
    {

        Results<Ok<TResult>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        result
            .OnSuccessNotNull(value => objectResult = TypedResults.Ok(value))
            .OnSuccessNull(() => objectResult = TypedResults.Ok(default(TResult)))
            .OnFailed(errors => objectResult = TypedResults.Problem(result.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>200 OK</c> containing the value produced by <paramref name="mapper"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>200</c> response with a null body. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static Results<Ok<T>, ProblemHttpResult, ValidationProblem>
    ToTypedOkResult<TResult, T>(this Result<TResult> result, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        Results<Ok<T>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        result
            .OnSuccessNotNull(value => objectResult = TypedResults.Ok(mapper(value)))
            .OnSuccessNull(() => objectResult = TypedResults.Ok(default(T)))
            .OnFailed(errors => objectResult = TypedResults.Problem(result.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value produced by <paramref name="mapper"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The mapper is not invoked when the successful result carries a <see langword="null"/> value. A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <typeparam name="T">The type of the value returned to the caller once mapped.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <param name="mapper">Maps the value of the result to the value returned to the caller.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="mapper"/> is <see langword="null"/>.</exception>
    public static Results<Created<T>, ProblemHttpResult, ValidationProblem>
    ToTypedCreatedResult<TResult, T>(this Result<TResult> result, Uri? location, [DisallowNull] Func<TResult, T> mapper)
    {
        ArgumentNullException.ThrowIfNull(mapper);

        Results<Created<T>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        result
            .OnSuccessNotNull(value => objectResult = TypedResults.Created(location, mapper(value)))
            .OnSuccessNull(() => objectResult = TypedResults.Created((Uri?)null, default(T)))
            .OnFailed(errors => objectResult = TypedResults.Problem(result.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> and the value of the result on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>A <see langword="null"/> value yields a <c>201</c> response without location and without content. The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Results<Created<TResult>, ProblemHttpResult, ValidationProblem>
    ToTypedCreatedResult<TResult>(this Result<TResult> result, Uri? location)
    {
        Results<Created<TResult>, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        result
            .OnSuccessNotNull(value => objectResult = TypedResults.Created(location, value))
            .OnSuccessNull(() => objectResult = TypedResults.Created((Uri?)null, default(TResult)))
            .OnFailed(errors => objectResult = TypedResults.Problem(result.ToProblemDetails()));

        return objectResult;
    }
    #endregion

    #region Result
    /// <summary>
    /// Converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>204 No Content</c> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The result to convert.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Results<NoContent, ProblemHttpResult, ValidationProblem>
    ToTypedOkResult(this Result result)
    {
        Results<NoContent, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        result
            .OnSuccess(() => objectResult = TypedResults.NoContent())
            .OnFailed(errors => objectResult = TypedResults.Problem(result.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Converts the result to a typed <c>Results&lt;...&gt;</c> union (minimal API): <c>201 Created</c> with the given <paramref name="location"/> on success, a <see cref="ProblemHttpResult"/> built from the <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Results<Created, ProblemHttpResult, ValidationProblem>
    ToTypedCreatedResult(this Result result, Uri? location)
    {
        Results<Created, ProblemHttpResult, ValidationProblem> objectResult = TypedResults.Problem();
        result
            .OnSuccess(() => objectResult = TypedResults.Created(location))
            .OnFailed(errors => objectResult = TypedResults.Problem(result.ToProblemDetails()));

        return objectResult;
    }

    #endregion

    #endregion
}
