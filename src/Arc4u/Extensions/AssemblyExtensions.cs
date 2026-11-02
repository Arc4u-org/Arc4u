using System.Reflection;

namespace Arc4u.Extensions;

/// <summary>
/// Version of the running service.
/// </summary>
public class VersionInfo
{
    /// <summary>
    /// Full informational version, including the build metadata.
    /// </summary>
    public string? Informational { get; init; }

    /// <summary>
    /// Semantic version, without the build metadata.
    /// </summary>
    public string? Semantic { get; init; }
}

/// <summary>
/// Extension methods on <see cref="Assembly"/>.
/// </summary>
public static class AssemblyExtensions
{
    /// <summary>
    /// Retrieves the informational and semantic version of the <paramref name="assembly"/>.
    /// The informational version comes from the <see cref="AssemblyInformationalVersionAttribute"/>
    /// (falling back to the assembly version), the semantic version is the informational version
    /// without its build metadata (the part after '+').
    /// </summary>
    /// <param name="assembly">The assembly to read the version from.</param>
    /// <returns><see cref="VersionInfo"/> of the assembly.</returns>
    public static VersionInfo GetVersionInfo(this Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                            ?? assembly.GetName().Version?.ToString()
                            ?? "0.0.0";

        var metadataIndex = informational.IndexOf('+', StringComparison.Ordinal);
        var semantic = metadataIndex < 0 ? informational : informational[..metadataIndex];

        return new VersionInfo
        {
            Informational = informational,
            Semantic = semantic
        };
    }
}
