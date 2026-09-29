namespace Arc4u.Diagnostics;

/// <summary>
/// Class used to bind settings from the configuration file.
/// </summary>
public partial class OpenTelemetrySettings
{
    /// <summary>Initializes a new instance of the <see cref="OpenTelemetrySettings"/> class with empty values.</summary>
    public OpenTelemetrySettings()
    {
        Attributes = [];
        Address = string.Empty;
        Sources = [];
    }
    /// <summary>Gets or sets the address of the OpenTelemetry collector.</summary>
    public string Address { get; set; }

    /// <summary>Gets or sets the resource attributes attached to the telemetry.</summary>
    public Dictionary<string, object> Attributes { get; set; }

    /// <summary>Gets or sets the names of the activity sources to collect.</summary>
    public List<string> Sources { get; set; }
}
