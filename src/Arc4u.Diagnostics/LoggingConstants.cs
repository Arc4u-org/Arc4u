namespace Arc4u.Diagnostics;

/// <summary>Reserved names of the properties used by the Arc4u logger. They cannot be used as custom property keys. Most are added by the logger to each entry; <see cref="ActivityId"/> is only present when there is a current <see cref="System.Diagnostics.Activity"/>, <see cref="Identity"/> only when an <see cref="IAddPropertiesToLog"/> provider supplies it and <see cref="Stacktrace"/> only when requested.</summary>
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

    /// <summary>The trace id (<see cref="System.Diagnostics.Activity.TraceId"/>) of the current <see cref="System.Diagnostics.Activity"/>, shared by all the services taking part in the same distributed trace.</summary>
    public const string ActivityId = "ActivityId";

    /// <summary>The identity of the current user.</summary>
    public const string Identity = "Identity";

    /// <summary>The stack trace of the log entry.</summary>
    public const string Stacktrace = "Stacktrace";

    /// <summary>The Arc4u message category, see <see cref="MessageCategory"/>.</summary>
    public const string Category = "Category";
}
