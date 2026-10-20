namespace Arc4u.Diagnostics;

/// <summary>
/// Specifies the different categories.
/// </summary>
[Flags]
public enum MessageCategory : short
{
    /// <summary>
    /// A message dedicates for IT people.
    /// </summary>
    Technical = 1,
    /// <summary>
    /// A message dedicates for IT and Business people.
    /// </summary>
    Business = 2,

    /// <summary>
    /// For Monitoring purpose.
    /// </summary>
    Monitoring = 4,
}
