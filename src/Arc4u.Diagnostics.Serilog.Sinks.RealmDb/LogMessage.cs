namespace Arc4u.Diagnostics.Serilog.Sinks;

/// <summary>A log message read from an <see cref="ILogStore"/>, with the Arc4u properties of the log entry.</summary>
public class LogMessage
{
    /// <summary>Gets or sets the time at which the message was logged.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>Gets or sets the name of the Arc4u <see cref="Arc4u.Diagnostics.MessageCategory"/> (Technical, Business or Monitoring).</summary>
    public string MessageCategory { get; set; } = string.Empty;

    /// <summary>Gets or sets the rendered message text.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the <see cref="Microsoft.Extensions.Logging.LogLevel"/> of the message.</summary>
    public string MessageType { get; set; } = string.Empty;

    /// <summary>Gets or sets the full name of the type that emitted the message.</summary>
    public string ClassType { get; set; } = string.Empty;

    /// <summary>Gets or sets the name of the method that emitted the message.</summary>
    public string MethodName { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the thread that emitted the message.</summary>
    public int ThreadId { get; set; }

    /// <summary>Gets or sets the id of the process that emitted the message.</summary>
    public int ProcessId { get; set; }

    /// <summary>Gets or sets the name of the application.</summary>
    public string Application { get; set; } = string.Empty;

    /// <summary>Gets or sets the identity of the user.</summary>
    public string Identity { get; set; } = string.Empty;

    /// <summary>Gets or sets the id of the activity in which the message was logged.</summary>
    public string ActivityId { get; set; } = string.Empty;

    /// <summary>Gets or sets the stack trace, when it was requested.</summary>
    public string Stacktrace { get; set; } = string.Empty;

    /// <summary>Gets or sets the additional (non Arc4u standard) properties, serialized as a JSON object.</summary>
    public string Properties { get; set; } = string.Empty;

}
