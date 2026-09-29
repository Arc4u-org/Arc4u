namespace Arc4u.Diagnostics;

/// <summary>
/// Class used to bind settings from the configuration file. This type is not consumed by any other Arc4u package.
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
    /// <summary>Gets or sets the address setting.</summary>
    public string Address { get; set; }

    /// <summary>Gets or sets the attributes setting.</summary>
    public Dictionary<string, object> Attributes { get; set; }

    /// <summary>Gets or sets the names of the activity sources.</summary>
    public List<string> Sources { get; set; }
}
