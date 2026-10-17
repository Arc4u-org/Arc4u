namespace Arc4u.OAuth2.AspNetCore.Filters;

/// <summary>
/// Configures the <see cref="ManageExceptionsFilter"/>.
/// </summary>
public class ManageExceptionsFilterOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether the details of the exception (type, message, stack trace and inner exceptions)
    /// are returned to the caller in the <c>exception</c> extension of the problem details.
    /// When <see langword="null"/> (default), the details are returned only when the host runs in the <c>Development</c> environment.
    /// </summary>
    /// <remarks>
    /// The details disclose the internals of the service: never enable it on a service exposed to untrusted callers.
    /// The exception is always logged with its details, whatever the value.
    /// </remarks>
    public bool? IncludeExceptionDetails { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the arguments of the action are logged, serialized in JSON, with the exception
    /// (in the <c>ActionArguments</c> property). The default is <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// The arguments often carry personal or sensitive data that is then written in the log: enable it knowingly.
    /// Arguments that cannot be meaningfully serialized (streams, uploaded files, cancellation tokens) are logged with their type only,
    /// and the arguments are never returned to the caller.
    /// </remarks>
    public bool LogActionArguments { get; set; }

    /// <summary>
    /// Gets or sets the maximum length of the JSON logged for one argument when <see cref="LogActionArguments"/> is enabled; a longer value is truncated.
    /// The default is 2048.
    /// </summary>
    public int MaxActionArgumentLength { get; set; } = 2048;
}
