using FluentResults;

namespace Arc4u.Results;

/// <summary>
/// Create an Error that will be translated into a ProblemDetails.
/// Detail will be assigned to the existing Message property of <see cref="Error"/>.
/// </summary>
public class ProblemDetailError : Error
{
    private ProblemDetailError()
    {
    }

    private ProblemDetailError(string detail)
    {
        Message = detail;
    }

    /// <summary>
    /// Creates a <see cref="ProblemDetailError"/> whose <see cref="Error.Message"/> is the detail of the problem.
    /// </summary>
    /// <param name="detail">The human readable explanation, used as the <c>detail</c> member of the ProblemDetails.</param>
    /// <returns>The new error; complete it with the <c>With...</c> methods.</returns>
    public static ProblemDetailError Create(string detail)
    {
        return new ProblemDetailError(detail);
    }

    /// <summary>
    /// Gets the short summary of the problem type (the <c>title</c> member of the ProblemDetails), or <see langword="null"/> when not set.
    /// </summary>
    public string? Title { get; private set; }

    /// <summary>
    /// Gets the URI reference identifying this occurrence of the problem (the <c>instance</c> member), or <see langword="null"/> when not set.
    /// </summary>
    public string? Instance { get; private set; }

    /// <summary>
    /// Gets the HTTP status code (the <c>status</c> member of the ProblemDetails), or <see langword="null"/> when not set.
    /// </summary>
    public int? StatusCode { get; private set; }

    /// <summary>
    /// Gets the URI identifying the problem type (the <c>type</c> member), or <see langword="null"/> when not set.
    /// </summary>
    public Uri? Type { get; private set; }

    /// <summary>
    /// Gets the severity exposed as an extension of the ProblemDetails, or <see langword="null"/> when not set.
    /// </summary>
    public string? Severity { get; private set; }

    /// <summary>
    /// Sets the URI identifying the problem type.
    /// </summary>
    /// <param name="type">The problem type URI.</param>
    /// <returns>This error, to chain calls.</returns>
    public ProblemDetailError WithType(Uri type)
    {
        Type = type;
        return this;
    }

    /// <summary>
    /// Sets the URI reference identifying this occurrence of the problem.
    /// </summary>
    /// <param name="instance">The instance URI reference.</param>
    /// <returns>This error, to chain calls.</returns>
    public ProblemDetailError WithInstance(string instance)
    {
        Instance = instance;
        return this;
    }

    /// <summary>
    /// Sets the HTTP status code of the problem.
    /// </summary>
    /// <param name="statusCode">The HTTP status code.</param>
    /// <returns>This error, to chain calls.</returns>
    public ProblemDetailError WithStatusCode(int statusCode)
    {
        StatusCode = statusCode;
        return this;
    }

    /// <summary>
    /// Sets the short summary of the problem type.
    /// </summary>
    /// <param name="title">The title.</param>
    /// <returns>This error, to chain calls.</returns>
    public ProblemDetailError WithTitle(string title)
    {
        Title = title;
        return this;
    }

    /// <summary>
    /// Sets the severity exposed as an extension of the ProblemDetails.
    /// </summary>
    /// <param name="severity">The severity, for example the name of a <see cref="Validation.Severity"/> value.</param>
    /// <returns>This error, to chain calls.</returns>
    public ProblemDetailError WithSeverity(string severity)
    {
        Severity = severity;
        return this;
    }

    /// <summary>
    /// Adds a metadata entry, exposed as an extension of the ProblemDetails, and returns this error.
    /// </summary>
    /// <remarks>Unlike <see cref="Error.WithMetadata(string, object)"/> this returns a <see cref="ProblemDetailError"/>. An existing key is kept: the call is then ignored.</remarks>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>This error, to chain calls.</returns>
    public new ProblemDetailError WithMetadata(string key, object value)
    {
        Metadata.TryAdd(key, value);
        return this;
    }

    /// <summary>
    /// Converts the error to a failed <see cref="Result"/> containing it.
    /// </summary>
    /// <param name="error">The error to wrap.</param>
    public static implicit operator Result(ProblemDetailError error)
    {
        return Result.Fail(error);
    }
}
