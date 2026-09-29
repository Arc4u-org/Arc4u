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
/// <para>The methods ending with <c>Async</c> await the task or value task first. The overloads with a mapper project the value before it is written to the response.</para>
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

        ActionResult objectResult = new BadRequestResult();
        res
            .OnSuccessNotNull(value => objectResult = new OkObjectResult(value))
            .OnSuccessNull(() => objectResult = new OkObjectResult(default(TResult)))
            .OnFailed(_ => objectResult = new ObjectResult(res.ToProblemDetails()));

        return objectResult;
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

        ActionResult objectResult = new BadRequestResult();
        res
            .OnSuccessNotNull(value => objectResult = new OkObjectResult(mapper(value)))
            .OnSuccessNull(() => objectResult = new OkObjectResult(default(TResult)))
            .OnFailed(_ => objectResult = new ObjectResult(res.ToProblemDetails()));

        return objectResult;
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

        ActionResult objectResult = new BadRequestResult();

        if (res.IsSuccess)
        {
            if (res.Value is null)
            {
                objectResult = new OkObjectResult(default(T));
            }
            else
            {
                var mapped = await asyncMapper(res.Value).ConfigureAwait(false);
                objectResult = new OkObjectResult(mapped);
            }
        }
        else
        {
            objectResult = new ObjectResult(res.ToProblemDetails());
        }

        return objectResult;
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

        ActionResult objectResult = new BadRequestResult();
        res
            .OnSuccess(() => objectResult = new NoContentResult())
            .OnFailed(_ => objectResult = new ObjectResult(res.ToProblemDetails()));

        return objectResult;
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

        ActionResult objectResult = new BadRequestResult();
        res
            .OnSuccessNotNull(value => objectResult = new CreatedResult(location, mapper(value)))
            .OnSuccessNull(() => objectResult = new ObjectResult(default(T))
            {
                StatusCode = StatusCodes.Status201Created
            })
            .OnFailed(_ => objectResult = new ObjectResult(res.ToProblemDetails()));

        return objectResult;
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

        ActionResult objectResult = new BadRequestResult();

        if (res.IsSuccess)
        {
            if (res.Value is null)
            {
                objectResult = new ObjectResult(default(T))
                {
                    StatusCode = StatusCodes.Status201Created
                };
            }
            else
            {
                var mapped = await asyncMapper(res.Value).ConfigureAwait(false);
                objectResult = new CreatedResult(location, mapped);
            }
        }
        else
        {
            objectResult = new ObjectResult(res.ToProblemDetails());
        }

        return objectResult;
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

        ActionResult objectResult = new BadRequestResult();
        res
            .OnSuccessNotNull(value => objectResult = new CreatedResult(location, value))
            .OnSuccessNull(() => objectResult = new ObjectResult(default(TResult))
            {
                StatusCode = StatusCodes.Status201Created
            })
            .OnFailed(_ => objectResult = new ObjectResult(res.ToProblemDetails()));

        return objectResult;
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

        ActionResult objectResult = new BadRequestResult();
        res
            .OnSuccessNotNull(value => objectResult = new OkObjectResult(value))
            .OnSuccessNull(() => objectResult = new OkObjectResult(default(TResult)))
            .OnFailed(_ => objectResult = new ObjectResult(res.ToProblemDetails()));

        return objectResult;
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

        ActionResult<T> objectResult = new BadRequestResult();
        res
            .OnSuccessNotNull(value => objectResult = new OkObjectResult(mapper(value)))
            .OnSuccessNull(() => objectResult = new OkObjectResult(default(TResult)))
            .OnFailed(_ => objectResult = new ObjectResult(res.ToProblemDetails()));

        return objectResult;
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

        ActionResult<T> objectResult = new BadRequestResult();

        if (res.IsSuccess)
        {
            if (res.Value is null)
            {
                objectResult = new OkObjectResult(default(T));
            }
            else
            {
                var mapped = await asyncMapper(res.Value).ConfigureAwait(false);
                objectResult = new OkObjectResult(mapped);
            }
        }
        else
        {
            objectResult = new ObjectResult(res.ToProblemDetails());
        }

        return objectResult;
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

        ActionResult objectResult = new BadRequestResult();
        res
            .OnSuccess(() => objectResult = new NoContentResult())
            .OnFailed(_ => objectResult = new ObjectResult(res.ToProblemDetails()));

        return objectResult;
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
        ActionResult objectResult = new BadRequestResult();

        result
            .OnSuccess(() => objectResult = new NoContentResult())
            .OnFailed(_ => objectResult = new ObjectResult(result.ToProblemDetails()));

        return Task.FromResult(objectResult);
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

        ActionResult<T> objectResult = new BadRequestResult();

        if (result.IsSuccess)
        {
            if (result.Value is null)
            {
                objectResult = new OkObjectResult(default(T));
            }
            else
            {
                var mapped = await asyncMapper(result.Value).ConfigureAwait(false);
                objectResult = new OkObjectResult(mapped);
            }
        }
        else
        {
            objectResult = new ObjectResult(result.ToProblemDetails());
        }

        return objectResult;
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

        ActionResult<T> objectResult = new BadRequestResult();
        res
            .OnSuccessNotNull(value => objectResult = new CreatedResult(location, mapper(value)))
            .OnSuccessNull(() => objectResult = new ObjectResult(default(T))
            {
                StatusCode = StatusCodes.Status201Created
            })
            .OnFailed(_ => objectResult = new ObjectResult(res.ToProblemDetails()));

        return objectResult;
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

        ActionResult<T> objectResult = new BadRequestResult();

        if (res.IsSuccess)
        {
            if (res.Value is null)
            {
                objectResult = new ObjectResult(default(T))
                {
                    StatusCode = StatusCodes.Status201Created
                };
            }
            else
            {
                var mapped = await asyncMapper(res.Value).ConfigureAwait(false);
                objectResult = new CreatedResult(location, mapped);
            }
        }
        else
        {
            objectResult = new ObjectResult(res.ToProblemDetails());
        }

        return objectResult;
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

        ActionResult<TResult> objectResult = new BadRequestResult();
        res
            .OnSuccessNotNull(value => objectResult = new CreatedResult(location, value))
            .OnSuccessNull(() => objectResult = new ObjectResult(default(TResult))
            {
                StatusCode = StatusCodes.Status201Created
            })
            .OnFailed(_ => objectResult = new ObjectResult(res.ToProblemDetails()));

        return objectResult;
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

        ActionResult objectResult = new BadRequestResult();
        result
            .OnSuccessNotNull(value => objectResult = new CreatedResult(location, mapper(value)))
            .OnSuccessNull(() => objectResult = new ObjectResult(default(T))
            {
                StatusCode = StatusCodes.Status201Created
            })
            .OnFailed(_ => objectResult = new ObjectResult(result.ToProblemDetails()));

        return Task.FromResult(objectResult);
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

        ActionResult objectResult = new BadRequestResult();

        if (result.IsSuccess)
        {
            if (result.Value is null)
            {
                objectResult = new ObjectResult(default(T))
                {
                    StatusCode = StatusCodes.Status201Created
                };
            }
            else
            {
                var mapped = await asyncMapper(result.Value).ConfigureAwait(false);
                objectResult = new CreatedResult(location, mapped);
            }
        }
        else
        {
            objectResult = new ObjectResult(result.ToProblemDetails());
        }

        return objectResult;
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
        ActionResult objectResult = new BadRequestResult();
        result
            .OnSuccessNotNull(value => objectResult = new CreatedResult(location, value))
            .OnSuccessNull(() => objectResult = new ObjectResult(default(TResult))
            {
                StatusCode = StatusCodes.Status201Created
            })
            .OnFailed(_ => objectResult = new ObjectResult(result.ToProblemDetails()));

        return Task.FromResult(objectResult);
    }

    /// <summary>
    /// Awaits the task, then converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">Unused: it only differentiates the overload, so it must be specified explicitly.</typeparam>
    /// <param name="result">The task producing the result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static async Task<ActionResult>
    ToActionCreatedResultAsync<TResult>(this Task<Result> result, Uri? location)
    {
        var res = await result.ConfigureAwait(false);

        ActionResult objectResult = new BadRequestResult();
        res
            .OnSuccess(() => objectResult = new CreatedResult(location, null))
            .OnFailed(_ => objectResult = new ObjectResult(res.ToProblemDetails()));

        return objectResult;
    }

    /// <summary>
    /// Converts the result to an <see cref="ActionResult"/>: <c>201 Created</c> with the given <paramref name="location"/> on success, an <see cref="ObjectResult"/> carrying the <see cref="ProblemDetails"/> on failure.
    /// </summary>
    /// <remarks>The problem details of a failed result are built by <c>ToProblemDetails</c>, see <see cref="FromResultToProblemDetailExtension"/>.</remarks>
    /// <typeparam name="TResult">Unused: it only differentiates the overload, so it must be specified explicitly.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="location">The URI of the created resource, set in the <c>Location</c> header; may be <see langword="null"/>.</param>
    /// <returns>The HTTP response representing the outcome of the result.</returns>
    public static Task<ActionResult>
    ToActionCreatedResultAsync<TResult>(this Result result, Uri? location)
    {
        ActionResult objectResult = new BadRequestResult();
        result
            .OnSuccess(() => objectResult = new CreatedResult(location, null))
            .OnFailed(_ => objectResult = new ObjectResult(result.ToProblemDetails()));

        return Task.FromResult(objectResult);
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
        ActionResult<TResult> objectResult = new BadRequestResult();
        result
            .OnSuccessNotNull(value => objectResult = new OkObjectResult(value))
            .OnSuccessNull(() => objectResult = new OkObjectResult(default(TResult)))
            .OnFailed(_ => objectResult = new ObjectResult(result.ToProblemDetails()));

        return objectResult;
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

        ActionResult<T> objectResult = new BadRequestResult();
        result
            .OnSuccessNotNull(value => objectResult = new OkObjectResult(mapper(value)))
            .OnSuccessNull(() => objectResult = new OkObjectResult(default(TResult)))
            .OnFailed(_ => objectResult = new ObjectResult(result.ToProblemDetails()));

        return objectResult;
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

        ActionResult<T> objectResult = new BadRequestResult();
        result
            .OnSuccessNotNull(value => objectResult = new CreatedResult(location, mapper(value)))
            .OnSuccessNull(() => objectResult = new ObjectResult(default(T))
            {
                StatusCode = StatusCodes.Status201Created
            })
            .OnFailed(_ => objectResult = new ObjectResult(result.ToProblemDetails()));

        return objectResult;
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
        ActionResult<TResult> objectResult = new BadRequestResult();
        result
            .OnSuccessNotNull(value => objectResult = new CreatedResult(location, value))
            .OnSuccessNull(() => objectResult = new ObjectResult(default(TResult))
            {
                StatusCode = StatusCodes.Status201Created
            })
            .OnFailed(_ => objectResult = new ObjectResult(result.ToProblemDetails()));

        return objectResult;
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
        ActionResult objectResult = new BadRequestResult();

        result
            .OnSuccess(() => objectResult = new NoContentResult())
            .OnFailed(_ => objectResult = new ObjectResult(result.ToProblemDetails()));

        return objectResult;
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
        ActionResult objectResult = new BadRequestResult();
        result
            .OnSuccess(() => objectResult = new CreatedResult(location, null))
            .OnFailed(_ => objectResult = new ObjectResult(result.ToProblemDetails()));

        return objectResult;
    }

    #endregion

    #endregion
}
