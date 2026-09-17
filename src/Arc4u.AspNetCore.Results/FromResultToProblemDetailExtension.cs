using System.Collections.Immutable;
using System.Diagnostics;
using Arc4u.Results;
using Arc4u.Results.Validation;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Arc4u.AspNetCore.Results;

public static class FromResultToProblemDetailExtension
{
    private static readonly Uri UnexpectedErrorType = new("https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#unexpected-error");
    private static readonly Uri ExpectedErrorType = new("https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#expected-error");
    private static readonly Uri ValidationErrorType = new("https://github.com/Arc4u-org/Arc4u/wiki/StatusCodes#validation-error");
    private static readonly Uri AboutBlankType = new("about:blank");
    public static Func<IEnumerable<IError>, ProblemDetails> FromError => errors => _fromErrors(errors);

    public static void SetFromErrorFactory(Func<IEnumerable<IError>, ProblemDetails> fromErrors)
    {
        _fromErrors = fromErrors;
    }
    private static Func<IEnumerable<IError>, ProblemDetails> _fromErrors = From;

    private static ProblemDetails From(IEnumerable<IError> errors)
    {
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

    public static ProblemDetails ToGenericMessage<TResult>(this Result<TResult> result, bool unexpectedType = true)
    {
        return ToGenericMessage(result, Activity.Current?.Id);
    }

    public static ProblemDetails ToGenericMessage<TResult>(this Result<TResult> result, string? activityId, bool unexpectedType = true)
    {
        return result.ToResult().ToGenericMessage(activityId);
    }

    public static ProblemDetails ToGenericMessage(this Result result, bool unexpectedType = true)
    {
        return result.ToGenericMessage(Activity.Current?.Id);
    }

    public static ProblemDetails ToGenericMessage(this Result result, string? activityId, bool unexpectedType = true)
    {
        result.LogIfFailed();

        var type = unexpectedType ? UnexpectedErrorType : ExpectedErrorType;

        if (activityId is not null)
        {
            return new ProblemDetails()
                .WithTitle("A technical error occured!")
                .WithDetail($"Contact the application owner. A message has been logged with id: {activityId}.")
                .WithType(type)
                .WithStatusCode(unexpectedType ? StatusCodes.Status500InternalServerError : StatusCodes.Status400BadRequest);
        }

        return new ProblemDetails()
                .WithTitle("A technical error occured!")
                .WithDetail("Contact the application owner. A message has been logged.")
                .WithType(type)
                .WithStatusCode(unexpectedType ? StatusCodes.Status500InternalServerError : StatusCodes.Status400BadRequest);

    }

    /// <summary>
    /// If Failure and no exceptions, return the Errors: Message, Code, Severity.
    /// If Failure and exceptions, Log and return the generic messages.
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="result"></param>
    /// <returns></returns>
    public static ProblemDetails ToProblemDetails<TResult>(this Result<TResult> result)
    {
        // Could not be a valid scenario to call this method! An Exceptional error will be created so the developer is aware of
        if (result.IsSuccess)
        {
            result.WithError(new ExceptionalError(new UnreachableException("Creating a ProblemDetails on a success Result does not make any sense!")));
        }

        return FromResultToProblemDetailExtension.FromError(result.Errors);
    }

    /// <summary>
    /// If Success, return the reasons!
    /// If Failure and no exceptions, return the Errors: Message, Code, Severity.
    /// If Failure and exceptions, Log and return the generic messages.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static ProblemDetails ToProblemDetails(this Result result)
    {
        // Could not be a valid scenario to call this method! An Exceptional error will be created so the developer is aware of
        if (result.IsSuccess)
        {
            result.WithError(new ExceptionalError(new UnreachableException("Creating a ProblemDetails on a success Result does not make any sense!")));
        }

        return FromResultToProblemDetailExtension.FromError(result.Errors);
    }
}
