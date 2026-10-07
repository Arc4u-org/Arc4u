using System.Collections.Immutable;
using System.Diagnostics;
using Arc4u.Results;
using Arc4u.Results.Validation;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Arc4u.AspNetCore.Results;

/// <summary>
/// Translates the errors of a failed <see cref="Result"/> into a <see cref="ProblemDetails"/>.
/// </summary>
/// <remarks>
/// The default translation is, in this order: an exceptional error is logged and hidden behind a generic technical error (status 500);
/// <see cref="ValidationError"/>s become a <see cref="ValidationProblemDetails"/> grouped by severity (status 422);
/// a <see cref="ProblemDetailError"/> is copied field by field (status 500 when none is set); any other error gives a 400 with the error message.
/// Replace the translation with <see cref="SetFromErrorFactory(Func{IEnumerable{IError}, ProblemDetails})"/>, or customize it while keeping the default rules for the other errors
/// with <see cref="SetFromErrorFactory(Func{IEnumerable{IError}, Func{IEnumerable{IError}, ProblemDetails}, ProblemDetails})"/>.
/// </remarks>
public static class FromResultToProblemDetailExtension
{
    private static readonly Uri UnexpectedErrorType = new("https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#unexpected-error");
    private static readonly Uri ExpectedErrorType = new("https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#expected-error");
    private static readonly Uri ValidationErrorType = new("https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#validation-error");
    private static readonly Uri AboutBlankType = new("about:blank");
    /// <summary>
    /// Gets the function used to translate a set of errors into a <see cref="ProblemDetails"/>: the default translation, or the one set by <c>SetFromErrorFactory</c>.
    /// </summary>
    /// <remarks>
    /// The function always calls the translation registered when it runs. Do not call it from a custom translation, which would call itself
    /// until the process dies with a stack overflow: use <see cref="SetFromErrorFactory(Func{IEnumerable{IError}, Func{IEnumerable{IError}, ProblemDetails}, ProblemDetails})"/>, which gives the default translation to your function.
    /// </remarks>
    public static Func<IEnumerable<IError>, ProblemDetails> FromError => errors => _fromErrors(errors);

    /// <summary>
    /// Replaces the function translating errors into a <see cref="ProblemDetails"/> for the whole process.
    /// </summary>
    /// <remarks>
    /// The setting is static and not synchronized: call it once at startup, before requests are served.
    /// To keep the default translation for the errors your function does not handle, use <see cref="SetFromErrorFactory(Func{IEnumerable{IError}, Func{IEnumerable{IError}, ProblemDetails}, ProblemDetails})"/>.
    /// </remarks>
    /// <param name="fromErrors">The function that builds the <see cref="ProblemDetails"/> from the errors of a failed result.</param>
    /// <exception cref="ArgumentNullException"><paramref name="fromErrors"/> is <see langword="null"/>.</exception>
    public static void SetFromErrorFactory(Func<IEnumerable<IError>, ProblemDetails> fromErrors)
    {
        ArgumentNullException.ThrowIfNull(fromErrors);

        _fromErrors = fromErrors;
    }

    /// <summary>
    /// Replaces the function translating errors into a <see cref="ProblemDetails"/> for the whole process, giving it the default translation
    /// to call for the errors it does not handle.
    /// </summary>
    /// <remarks>
    /// The setting is static and not synchronized: call it once at startup, before requests are served.
    /// The second argument of <paramref name="fromErrors"/> is always the default Arc4u translation, so calling it never recurses.
    /// To restore the default translation, pass <c>(errors, defaultFromError) =&gt; defaultFromError(errors)</c>.
    /// </remarks>
    /// <param name="fromErrors">
    /// The function that builds the <see cref="ProblemDetails"/> from the errors of a failed result; its second argument is the default translation.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="fromErrors"/> is <see langword="null"/>.</exception>
    public static void SetFromErrorFactory(Func<IEnumerable<IError>, Func<IEnumerable<IError>, ProblemDetails>, ProblemDetails> fromErrors)
    {
        ArgumentNullException.ThrowIfNull(fromErrors);

        _fromErrors = errors => fromErrors(errors, From);
    }
    private static Func<IEnumerable<IError>, ProblemDetails> _fromErrors = From;

    private static ProblemDetails From(IEnumerable<IError> errors)
    {
        // A custom factory can call the default translation with any value: fail with a clear message instead of in First().
        ArgumentNullException.ThrowIfNull(errors);

        errors = errors as IReadOnlyCollection<IError> ?? errors.ToList();
        if (!errors.Any())
        {
            throw new ArgumentException("At least one error is needed to build a ProblemDetails.", nameof(errors));
        }

        if (errors.OfType<IExceptionalError>().Any())
        {
            var exceptionalError = errors.OfType<IExceptionalError>().First();
            var result = Result.Fail(exceptionalError);
            return result.ToGenericMessage(Activity.Current?.Id, true);
        }

        if (errors.OfType<ValidationError>().Any())
        {
            var orderedErrors = errors.OfType<ValidationError>()
                                      .OrderBy(x => x.Severity)
                                      .GroupBy(x => x.Severity)
                                      .ToImmutableSortedDictionary(g => g.Key.ToString(), g => g.ToImmutableList().Select(vError => vError.Message).ToArray());

            return new ValidationProblemDetails(orderedErrors)
                        .WithTitle("Error from validation.")
                        .WithStatusCode(StatusCodes.Status422UnprocessableEntity)
                        .WithType(ValidationErrorType);
        }

        if (errors.OfType<ProblemDetailError>().Any())
        {
            var problemDetailError = errors.OfType<ProblemDetailError>().First();
            var problem = new ProblemDetails()
                        .WithTitle(problemDetailError.Title ?? "Error.")
                        .WithDetail(problemDetailError.Message)
                        .WithStatusCode(problemDetailError.StatusCode ?? StatusCodes.Status500InternalServerError)
                        .WithSeverity(problemDetailError.Severity ?? Severity.Error.ToString())
                        .WithType(problemDetailError.Type ?? new Uri("about:blank"))
                        .WithInstance(problemDetailError.Instance);

            foreach (var metadata in problemDetailError.Metadata)
            {
                problem.WithMetadata(metadata.Key, metadata.Value);
            }

            return problem;
        }

        var error = errors.First();
        return new ProblemDetails()
                    .WithTitle("Error.")
                    .WithDetail(error.Message)
                    .WithStatusCode(StatusCodes.Status400BadRequest)
                    .WithType(AboutBlankType)
                    .WithSeverity(Severity.Error.ToString());
    }

    /// <summary>
    /// Logs the failed result and returns a generic technical error, using the id of the current <see cref="System.Diagnostics.Activity"/> (when any) as correlation id.
    /// </summary>
    /// <remarks>The <paramref name="unexpectedType"/> argument is not taken into account by this overload: the result is always the unexpected-error (status 500) variant.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to log.</param>
    /// <param name="unexpectedType">Kept for compatibility; ignored by this overload.</param>
    /// <returns>A <see cref="ProblemDetails"/> with a generic message that does not disclose the errors.</returns>
    public static ProblemDetails ToGenericMessage<TResult>(this Result<TResult> result, bool unexpectedType = true)
    {
        return ToGenericMessage(result, Activity.Current?.Id);
    }

    /// <summary>
    /// Logs the failed result and returns a generic technical error mentioning <paramref name="activityId"/> as correlation id.
    /// </summary>
    /// <remarks>The <paramref name="unexpectedType"/> argument is not taken into account by this overload: the result is always the unexpected-error (status 500) variant.</remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to log.</param>
    /// <param name="activityId">The correlation id written in the detail; when <see langword="null"/> a message without id is returned.</param>
    /// <param name="unexpectedType">Kept for compatibility; ignored by this overload.</param>
    /// <returns>A <see cref="ProblemDetails"/> with a generic message that does not disclose the errors.</returns>
    public static ProblemDetails ToGenericMessage<TResult>(this Result<TResult> result, string? activityId, bool unexpectedType = true)
    {
        return result.ToResult().ToGenericMessage(activityId);
    }

    /// <summary>
    /// Logs the failed result and returns a generic technical error, using the id of the current <see cref="System.Diagnostics.Activity"/> (when any) as correlation id.
    /// </summary>
    /// <remarks>The <paramref name="unexpectedType"/> argument is not taken into account by this overload: the result is always the unexpected-error (status 500) variant.</remarks>
    /// <param name="result">The result to log.</param>
    /// <param name="unexpectedType">Kept for compatibility; ignored by this overload.</param>
    /// <returns>A <see cref="ProblemDetails"/> with a generic message that does not disclose the errors.</returns>
    public static ProblemDetails ToGenericMessage(this Result result, bool unexpectedType = true)
    {
        return result.ToGenericMessage(Activity.Current?.Id);
    }

    /// <summary>
    /// Logs the failed result and returns a generic technical error mentioning <paramref name="activityId"/> as correlation id.
    /// </summary>
    /// <param name="result">The result to log.</param>
    /// <param name="activityId">The correlation id written in the detail; when <see langword="null"/> a message without id is returned.</param>
    /// <param name="unexpectedType">When <see langword="true"/> (default) the problem is typed as an unexpected error with status 500; otherwise as an expected error with status 400.</param>
    /// <returns>A <see cref="ProblemDetails"/> with a generic message that does not disclose the errors.</returns>
    public static ProblemDetails ToGenericMessage(this Result result, string? activityId, bool unexpectedType = true)
    {
        result.LogIfFailed();

        var type = unexpectedType ? UnexpectedErrorType : ExpectedErrorType;

        if (activityId is not null)
        {
            return new ProblemDetails()
                .WithTitle("A technical error occurred!")
                .WithDetail($"Contact the application owner. A message has been logged with id: {activityId}.")
                .WithType(type)
                .WithStatusCode(unexpectedType ? StatusCodes.Status500InternalServerError : StatusCodes.Status400BadRequest);
        }

        return new ProblemDetails()
                .WithTitle("A technical error occurred!")
                .WithDetail("Contact the application owner. A message has been logged.")
                .WithType(type)
                .WithStatusCode(unexpectedType ? StatusCodes.Status500InternalServerError : StatusCodes.Status400BadRequest);

    }

    /// <summary>
    /// Translates the errors of a failed result into a <see cref="ProblemDetails"/> using <see cref="FromError"/>.
    /// </summary>
    /// <remarks>
    /// Calling it on a successful result is a programming error: the problem details are then built from an exceptional error wrapping an <see cref="UnreachableException"/>,
    /// which is translated to the generic technical error. The result itself is not modified.
    /// </remarks>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The failed result.</param>
    /// <returns>The <see cref="ProblemDetails"/> describing the errors of the result.</returns>
    public static ProblemDetails ToProblemDetails<TResult>(this Result<TResult> result)
    {
        // Could not be a valid scenario to call this method! An exceptional error is reported so the developer is aware of it, without altering the result.
        if (result.IsSuccess)
        {
            return FromResultToProblemDetailExtension.FromError([new ExceptionalError(new UnreachableException("Creating a ProblemDetails on a success Result does not make any sense!"))]);
        }

        return FromResultToProblemDetailExtension.FromError(result.Errors);
    }

    /// <summary>
    /// Translates the errors of a failed result into a <see cref="ProblemDetails"/> using <see cref="FromError"/>.
    /// </summary>
    /// <remarks>
    /// Calling it on a successful result is a programming error: the problem details are then built from an exceptional error wrapping an <see cref="UnreachableException"/>,
    /// which is translated to the generic technical error. The result itself is not modified.
    /// </remarks>
    /// <param name="result">The failed result.</param>
    /// <returns>The <see cref="ProblemDetails"/> describing the errors of the result.</returns>
    public static ProblemDetails ToProblemDetails(this Result result)
    {
        // Could not be a valid scenario to call this method! An exceptional error is reported so the developer is aware of it, without altering the result.
        if (result.IsSuccess)
        {
            return FromResultToProblemDetailExtension.FromError([new ExceptionalError(new UnreachableException("Creating a ProblemDetails on a success Result does not make any sense!"))]);
        }

        return FromResultToProblemDetailExtension.FromError(result.Errors);
    }
}
