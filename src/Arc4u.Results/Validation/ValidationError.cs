using FluentResults;

namespace Arc4u.Results.Validation;

/// <summary>
/// An <see cref="Error"/> describing a business validation failure, with an optional code and a <see cref="Severity"/>.
/// </summary>
/// <remarks>
/// Create it with <see cref="Create(string)"/>. When a failed result containing validation errors is translated to an HTTP response
/// by <c>Arc4u.AspNetCore.Results</c>, they are grouped by severity in a validation problem details (status 422).
/// </remarks>
/// <example>
/// <code>
/// Result result = Result.Fail(ValidationError.Create("The name is required.")
///                                             .WithCode("NAME_REQUIRED")
///                                             .WithSeverity(Severity.Warning));
/// </code>
/// </example>
public class ValidationError : Error
{
    private ValidationError(string message)
    {
        Message = message;
        Code = string.Empty;
        Severity = Severity.Error;
    }

    /// <summary>
    /// Creates a validation error with the <see cref="Severity.Error"/> severity and an empty code.
    /// </summary>
    /// <param name="errorMessage">The message describing the validation failure.</param>
    /// <returns>The new validation error.</returns>
    public static ValidationError Create(string errorMessage)
    {
        return new ValidationError(errorMessage);
    }

    /// <summary>
    /// Sets the severity of the validation error.
    /// </summary>
    /// <param name="severity">The severity.</param>
    /// <returns>This error, to chain calls.</returns>
    public ValidationError WithSeverity(Severity severity)
    {
        Severity = severity;
        return this;
    }

    /// <summary>
    /// Sets the code identifying the validation rule that failed.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns>This error, to chain calls.</returns>
    public ValidationError WithCode(string code)
    {
        Code = code;
        return this;
    }

    /// <summary>
    /// Adds a metadata entry to the error and returns this error.
    /// </summary>
    /// <remarks>Unlike <see cref="Error.WithMetadata(string, object)"/> this returns a <see cref="ValidationError"/>. An existing key is kept: the call is then ignored.</remarks>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This error, to chain calls.</returns>
    public new ValidationError WithMetadata(string key, object value)
    {
        Metadata.TryAdd(key, value);
        return this;
    }

    /// <summary>
    /// Gets the code identifying the validation rule that failed; an empty string when not set.
    /// </summary>
    public string Code { get; private set; }

    /// <summary>
    /// Gets the severity of the validation error; <see cref="Validation.Severity.Error"/> by default.
    /// </summary>
    public Severity Severity { get; private set; }

    /// <summary>
    /// Converts the validation error to a failed <see cref="Result"/> containing it.
    /// </summary>
    /// <param name="error">The error to wrap.</param>
    public static implicit operator Result(ValidationError error)
    {
        return Result.Fail(error);
    }

}

