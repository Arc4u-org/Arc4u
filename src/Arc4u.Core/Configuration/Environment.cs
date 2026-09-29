namespace Arc4u.Configuration;

/// <summary>
/// Describes the environment (for example development, test or production) an application is running in.
/// </summary>
public class Environment
{
    /// <summary>
    /// Gets or sets the name of the environment. It is used, for example, to build the file name of the token cache.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the application name written in the log entries.
    /// </summary>
    public string LoggingName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the time zone (as understood by <see cref="TimeZoneInfo.FindSystemTimeZoneById(string)"/>) used by the application. When empty, or not found, the time zone of the machine is used by the <c>TimeZoneContext</c>.
    /// </summary>
    public string TimeZone { get; set; } = string.Empty;
}
