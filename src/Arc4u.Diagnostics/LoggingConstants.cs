namespace Arc4u.Diagnostics;

/// <summary>Names of the properties that the Arc4u logger adds to every log entry. These names are reserved and cannot be used as custom property keys.</summary>
public static class LoggingConstants
{
    /// <summary>The name of the application.</summary>
    public const string Application = "Application";

    /// <summary>The id of the current process.</summary>
    public const string ProcessId = "Pid";

    /// <summary>The id of the current managed thread.</summary>
    public const string ThreadId = "Tid";

    /// <summary>The name of the calling method.</summary>
    public const string MethodName = "Method";

    /// <summary>The full name of the type that emits the log entry.</summary>
    public const string Class = "SourceContext";

    /// <summary>The id of the current <see cref="System.Diagnostics.Activity"/>.</summary>
    public const string ActivityId = "ActivityId";

    /// <summary>The identity of the current user.</summary>
    public const string Identity = "Identity";

    /// <summary>The stack trace of the log entry.</summary>
    public const string Stacktrace = "Stacktrace";

    /// <summary>The Arc4u message category, see <see cref="MessageCategory"/>.</summary>
    public const string Category = "Category";
}
