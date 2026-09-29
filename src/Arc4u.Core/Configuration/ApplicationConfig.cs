namespace Arc4u.Configuration;

/// <summary>
/// Application-level settings shared by the Arc4u components (identification of the application and of its runtime environment).
/// Bound from configuration, typically through <c>AddApplicationConfig</c>, and consumed with <c>IOptions&lt;ApplicationConfig&gt;</c> or <c>IOptionsMonitor&lt;ApplicationConfig&gt;</c>.
/// </summary>
public class ApplicationConfig
{
    /// <summary>
    /// Name used to identify the application when used externally other than logging!
    /// Cache or authorization, etc...
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the environment (name, logging name and time zone) the application runs in.
    /// </summary>
    public Environment Environment { get; set; } = new Environment();
}
