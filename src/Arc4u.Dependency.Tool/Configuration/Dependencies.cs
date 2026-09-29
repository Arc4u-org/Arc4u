namespace Arc4u.Dependency.Configuration;

/// <summary>
/// The <c>Application.Dependency</c> section of the <c>appsettings.json</c> read by the <see cref="Arc4u.Dependency.Tool.GenerateRegisteredTypes"/> generator.
/// </summary>
public class Dependencies
{
    /// <summary>
    /// Gets or sets the types to register, each written as <c>Full.Type.Name, AssemblyName</c>. The assembly must be referenced by the compiling project; the type must carry the <c>Export</c> attribute.
    /// </summary>
    public ICollection<string> RegisterTypes { get; set; } = [];
}
