using FluentResults;

namespace Arc4u.Results.Validation;

/// <summary>
/// Extension methods adding a <see cref="ValidationError"/> to a <see cref="Result"/> or a <see cref="Result{TValue}"/>.
/// </summary>
/// <example>
/// <code>
/// Result&lt;int&gt; result = Result.Ok(0)
///     .WithValidationError("The quantity must be positive.", "QTY_POSITIVE", Severity.Error);
/// </code>
/// </example>
public static class ValidationExtensions
{

    /// <summary>
    /// Adds a validation error with the default severity (<see cref="Severity.Error"/>) to the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to complete.</param>
    /// <param name="errorMessage">The message describing the validation failure.</param>
    /// <returns>The result, now failed.</returns>
    public static Result<TResult> WithValidationError<TResult>(this Result<TResult> result, string errorMessage)
    {
        return result.WithError(ValidationError.Create(errorMessage));
    }

    /// <summary>
    /// Adds a validation error with the given code and the default severity to the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to complete.</param>
    /// <param name="errorMessage">The message describing the validation failure.</param>
    /// <param name="code">The code identifying the validation rule that failed.</param>
    /// <returns>The result, now failed.</returns>
    public static Result<TResult> WithValidationError<TResult>(this Result<TResult> result, string errorMessage, string code)
    {
        return result.WithError(ValidationError.Create(errorMessage).WithCode(code));
    }

    /// <summary>
    /// Adds a validation error with the given severity to the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to complete.</param>
    /// <param name="errorMessage">The message describing the validation failure.</param>
    /// <param name="severity">The severity of the validation error.</param>
    /// <returns>The result, now failed.</returns>
    public static Result<TResult> WithValidationError<TResult>(this Result<TResult> result, string errorMessage, Severity severity)
    {
        return result.WithError(ValidationError.Create(errorMessage).WithSeverity(severity));
    }

    /// <summary>
    /// Adds a validation error with the given code and severity to the result.
    /// </summary>
    /// <typeparam name="TResult">The type of the value carried by the result.</typeparam>
    /// <param name="result">The result to complete.</param>
    /// <param name="errorMessage">The message describing the validation failure.</param>
    /// <param name="code">The code identifying the validation rule that failed.</param>
    /// <param name="severity">The severity of the validation error.</param>
    /// <returns>The result, now failed.</returns>
    public static Result<TResult> WithValidationError<TResult>(this Result<TResult> result, string errorMessage, string code, Severity severity)
    {
        return result.WithError(ValidationError.Create(errorMessage).WithCode(code).WithSeverity(severity));
    }

    /// <summary>
    /// Adds a validation error with the default severity (<see cref="Severity.Error"/>) to the result.
    /// </summary>
    /// <param name="result">The result to complete.</param>
    /// <param name="errorMessage">The message describing the validation failure.</param>
    /// <returns>The result, now failed.</returns>
    public static Result WithValidationError(this Result result, string errorMessage)
    {
        return result.WithError(ValidationError.Create(errorMessage));
    }

    /// <summary>
    /// Adds a validation error with the given code and the default severity to the result.
    /// </summary>
    /// <param name="result">The result to complete.</param>
    /// <param name="errorMessage">The message describing the validation failure.</param>
    /// <param name="code">The code identifying the validation rule that failed.</param>
    /// <returns>The result, now failed.</returns>
    public static Result WithValidationError(this Result result, string errorMessage, string code)
    {
        return result.WithError(ValidationError.Create(errorMessage).WithCode(code));
    }

    /// <summary>
    /// Adds a validation error with the given severity to the result.
    /// </summary>
    /// <param name="result">The result to complete.</param>
    /// <param name="errorMessage">The message describing the validation failure.</param>
    /// <param name="severity">The severity of the validation error.</param>
    /// <returns>The result, now failed.</returns>
    public static Result WithValidationError(this Result result, string errorMessage, Severity severity)
    {
        return result.WithError(ValidationError.Create(errorMessage).WithSeverity(severity));
    }

    /// <summary>
    /// Adds a validation error with the given code and severity to the result.
    /// </summary>
    /// <param name="result">The result to complete.</param>
    /// <param name="errorMessage">The message describing the validation failure.</param>
    /// <param name="code">The code identifying the validation rule that failed.</param>
    /// <param name="severity">The severity of the validation error.</param>
    /// <returns>The result, now failed.</returns>
    public static Result WithValidationError(this Result result, string errorMessage, string code, Severity severity)
    {
        return result.WithError(ValidationError.Create(errorMessage).WithCode(code).WithSeverity(severity));
    }
}
