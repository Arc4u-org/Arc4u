using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Arc4u.Dependency.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Arc4u.Dependency.Tool;

/// <summary>
/// A source generator that reads the <c>Application.Dependency</c> section of the <c>Configs/appsettings.json</c> (or <c>wwwroot/appsettings.json</c>) additional file
/// and emits an extension method on <c>IServiceCollection</c> (<c>RegisterTypes</c>, or <c>RegisterWwwTypes</c> for the <c>wwwroot</c> file, generated in <c>GeneratedWwwRootTypes.g.cs</c>) registering the listed types that carry the <c>Export</c> attribute.
/// </summary>
[Generator]
public class GenerateRegisteredTypes : IIncrementalGenerator
{
    const string section = "Application.Dependency";
    const string settingsFileName = "appsettings.json";

    // appsettings.json files are read by Microsoft.Extensions.Configuration, which accepts comments and trailing commas.
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    // Deserializing a JsonElement reads its raw text again, comments included.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Registers the generation of the <c>GeneratedTypes.g.cs</c> file from the <c>appsettings.json</c> additional file.
    /// </summary>
    /// <param name="context">The generator initialization context.</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
#if DEBUG
        if (!Debugger.IsAttached)
        {
            //Debugger.Launch();
        }
#endif
        // if I have more than one file, the latest one will win!
        // To enforce the rule and having for sure one result => Take only the file under the path Configs\appsettings.json!
        var normalizedTargetPath = Path.DirectorySeparatorChar + Path.Combine("Configs", settingsFileName);
        var wwwrootTargetPath = Path.DirectorySeparatorChar + Path.Combine("wwwroot", settingsFileName);

        var appSettingFiles = context.AdditionalTextsProvider
            .Where(file =>
            {
                // Normalize the path to ensure that the comparison is case insensitive on all platforms.
                var normalizedPath = NormalizePath(file.Path);
                return normalizedPath.EndsWith(normalizedTargetPath, StringComparison.InvariantCultureIgnoreCase)
                    ||
                       normalizedPath.EndsWith(wwwrootTargetPath, StringComparison.InvariantCultureIgnoreCase);
            });

        // ProjectDir is a compiler visible property in every project using the .NET SDK.
        var projectDirectory = context.AnalyzerConfigOptionsProvider
            .Select(static (provider, _) => provider.GlobalOptions.TryGetValue("build_property.ProjectDir", out var directory) && !string.IsNullOrWhiteSpace(directory) ? directory : null);

        context.RegisterSourceOutput(appSettingFiles.Combine(context.CompilationProvider).Combine(projectDirectory), (ctx, source) =>
        {
            var ((appSettingFile, compilation), projectDir) = source;
            var text = appSettingFile.GetText(ctx.CancellationToken);
            if (text is null)
            {
                return;
            }

            // Without the ProjectDir property (a compilation not built by the .NET SDK), guess the project folder from the source files.
            var rootPath = projectDir is not null ? NormalizePath(projectDir) : GetAssemblyPath(compilation);
            if (rootPath is null)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(DependencyDiagnostics.ProjectDirectoryUnknown, Location.None, appSettingFile.Path));
                return;
            }
            rootPath = rootPath.TrimEnd(Path.DirectorySeparatorChar);

            var normalizedPath = NormalizePath(appSettingFile.Path);
            if (!normalizedPath.StartsWith(rootPath, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            var relativePath = normalizedPath.Substring(rootPath.Length);
            if (relativePath.Equals(normalizedTargetPath, StringComparison.InvariantCultureIgnoreCase))
            {
                ctx.AddSource("GeneratedTypes.g.cs", SourceText.From(GenerateRegisterTypes(new SettingsFile(appSettingFile.Path, text), "RegisterTypes", compilation, ctx.ReportDiagnostic), Encoding.UTF8));
            }
            else if (relativePath.Equals(wwwrootTargetPath, StringComparison.InvariantCultureIgnoreCase))
            {
                ctx.AddSource("GeneratedWwwRootTypes.g.cs", SourceText.From(GenerateRegisterTypes(new SettingsFile(appSettingFile.Path, text), "RegisterWwwTypes", compilation, ctx.ReportDiagnostic), Encoding.UTF8));
            }
        });
    }

    private static string NormalizePath(string path) => path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);

    /// <summary>
    /// Gets the root directory of the source files of the compilation (the common part of the shortest source file directories).
    /// </summary>
    /// <param name="compilation">The compilation.</param>
    /// <returns>The directory, or <see langword="null"/> when it cannot be determined.</returns>
    public string? GetAssemblyPath(Compilation compilation)
    {
        var symbolPaths = compilation.Assembly.Locations
                                     .Where(l => l.SourceTree?.FilePath is not null)
                                     .Select(l => Path.GetDirectoryName(l.SourceTree?.FilePath))
                                     .Where(path => !string.IsNullOrEmpty(path))
                                     .ToList();

        if (symbolPaths.Count == 0)
        {
            return null;
        }

        // Find the minimum length of the strings in the list
        var minLength = symbolPaths.Min(path => path!.Length);

        // Select all the strings that have the shortest length
        var shortestPaths = symbolPaths.Where(path => path!.Length == minLength).ToList();

        // I can have more than one path with the same length: c:\Temp\A\B and c:\Temp\C\D.
        // I need to find the common part: c:\Temp\
        var commonPath = shortestPaths[0]!;
        for (var i = 1; i < shortestPaths.Count; i++)
        {
            commonPath = GetCommonPath(commonPath, shortestPaths[i]!);
        }
        return commonPath;
    }

    private string GetCommonPath(string path1, string path2)
    {
        var parts1 = path1.Split(Path.DirectorySeparatorChar);
        var parts2 = path2.Split(Path.DirectorySeparatorChar);
        var commonParts = parts1.TakeWhile((part, index) => index < parts2.Length && part == parts2[index]);
        return string.Join(Path.DirectorySeparatorChar.ToString(), commonParts);
    }

    private static List<(string Path, AssemblyIdentity Identity)> RetrieveReferencedAssemblies(Compilation compilation)
    {
        var assemblies = new List<(string Path, AssemblyIdentity Identity)>();
        foreach (var reference in compilation.References.OfType<PortableExecutableReference>())
        {
            if (!string.IsNullOrEmpty(reference.FilePath) && compilation.GetAssemblyOrModuleSymbol(reference) is IAssemblySymbol assembly)
            {
                assemblies.Add((reference.FilePath!, assembly.Identity));
            }
        }

        return assemblies;
    }

    private string GenerateRegisterTypes(SettingsFile settings, string memberName, Compilation compilation, Action<Diagnostic> reportDiagnostic)
    {
        var sb = new StringBuilder();

        // Marks the emitted file as generated code so the analyzers of the consuming project skip it:
        // RegisterExtensions is partial across the files this tool emits, which rules out a
        // [GeneratedCode] attribute (it is not AllowMultiple), so the marker goes in the header.
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection;");
        sb.AppendLine("using Microsoft.Extensions.DependencyInjection.Extensions;");
        sb.AppendLine();
        sb.AppendLine("namespace Arc4u.Dependency;");
        sb.AppendLine();
        sb.AppendLine("public static partial class RegisterExtensions");
        sb.AppendLine("{");
        sb.AppendLine($"    public static void {memberName}(this IServiceCollection services)");
        sb.AppendLine("    {");

        // The method is generated even when the file is invalid, so that its callers still compile: the diagnostic explains why it is empty.
        var dependencies = ReadDependencies(settings, memberName, reportDiagnostic);
        if (dependencies is not null)
        {
            AppendRegistrations(sb, settings, dependencies, compilation, reportDiagnostic);
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static Dependencies? ReadDependencies(SettingsFile settings, string memberName, Action<Diagnostic> reportDiagnostic)
    {
        using var document = ParseDocument(settings, memberName, reportDiagnostic);
        if (document is null)
        {
            return null;
        }

        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            reportDiagnostic(Diagnostic.Create(DependencyDiagnostics.InvalidSettings, settings.Locate(null), settings.Path, "the root of the file must be a JSON object", memberName));
            return null;
        }

        if (!document.RootElement.TryGetProperty(section, out var sectionElement))
        {
            return null;
        }

        try
        {
            var dependencies = JsonSerializer.Deserialize<Dependencies>(sectionElement, SerializerOptions);
            if (dependencies?.RegisterTypes is null)
            {
                reportDiagnostic(Diagnostic.Create(DependencyDiagnostics.InvalidSettings, settings.Locate(section), settings.Path, $"the '{section}' section must be an object whose RegisterTypes property is an array of strings", memberName));
                return null;
            }
            return dependencies;
        }
        catch (JsonException ex)
        {
            // The line and position of the exception are relative to the section, not to the file.
            reportDiagnostic(Diagnostic.Create(DependencyDiagnostics.InvalidSettings, settings.Locate(section), settings.Path, $"the '{section}' section is invalid ({ex.Message.TrimEnd('.')})", memberName));
            return null;
        }
    }

    private static JsonDocument? ParseDocument(SettingsFile settings, string memberName, Action<Diagnostic> reportDiagnostic)
    {
        try
        {
            return JsonDocument.Parse(settings.Text.ToString(), JsonOptions);
        }
        catch (JsonException ex)
        {
            reportDiagnostic(Diagnostic.Create(DependencyDiagnostics.InvalidSettings, settings.Locate(ex.LineNumber, ex.BytePositionInLine), settings.Path, ex.Message.TrimEnd('.'), memberName));
            return null;
        }
    }

    private static void AppendRegistrations(StringBuilder sb, SettingsFile settings, Dependencies dependencies, Compilation compilation, Action<Diagnostic> reportDiagnostic)
    {
        var referencedAssemblies = RetrieveReferencedAssemblies(compilation);

        // Types registered by nuget package!
        var typesByAssemblies = new Dictionary<NugetInfo, List<(TypeInfo Type, string Entry)>>();
        foreach (var typeString in dependencies.RegisterTypes)
        {
            if (typeString is null)
            {
                continue;
            }

            if (!TryParseEntry(typeString, out var typeInfo, out var assemblyName))
            {
                reportDiagnostic(Diagnostic.Create(DependencyDiagnostics.MalformedEntry, settings.Locate(typeString), typeString));
                continue;
            }

            var candidates = referencedAssemblies.Where(a => string.Equals(a.Identity.Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (candidates.Count == 0)
            {
                reportDiagnostic(Diagnostic.Create(DependencyDiagnostics.AssemblyNotReferenced, settings.Locate(typeString), typeString, $"the project does not reference the assembly '{assemblyName.Name}'"));
                continue;
            }

            if (assemblyName.Version is not null)
            {
                // Version= is an assembly version (9.0.0.0); a version found in the path of the reference (a NuGet folder) is also accepted.
                var requestedVersion = assemblyName.Version;
                var matching = candidates.Where(a => IsSameVersion(requestedVersion, a.Identity.Version) || a.Path.Contains(requestedVersion.ToString())).ToList();
                if (matching.Count == 0)
                {
                    var found = string.Join(", ", candidates.Select(a => a.Identity.Version.ToString()).Distinct());
                    reportDiagnostic(Diagnostic.Create(DependencyDiagnostics.AssemblyNotReferenced, settings.Locate(typeString), typeString, $"the project references '{assemblyName.Name}' version {found}, not version {requestedVersion}"));
                    continue;
                }
                candidates = matching;
            }

            var nugetInfo = new NugetInfo(candidates[0].Path, assemblyName);
            if (!typesByAssemblies.TryGetValue(nugetInfo, out var types))
            {
                typesByAssemblies.Add(nugetInfo, types = []);
            }
            types.Add((typeInfo, typeString));
        }

        // Parse each registered nuget files and register the types to the service collection.
        foreach (var typesByAssembly in typesByAssemblies)
        {
            var requestedTypes = typesByAssembly.Value.Select(t => t.Type).ToList();
            var registeredTypes = new HashSet<TypeInfo>();

            sb.AppendLine($"        // Types registered by nuget package: {typesByAssembly.Key.Assembly.FullName}");
            foreach (var exportInfo in TypeExportAttributeScanner.GetExportDescriptions(typesByAssembly.Key.NugetFile, requestedTypes))
            {
                if (requestedTypes.Contains(exportInfo.Implementation))
                {
                    // A type exported several times yields one description per Export: its diagnostics are reported once.
                    var firstExport = registeredTypes.Add(exportInfo.Implementation);
                    var entry = typesByAssembly.Value.First(t => t.Type.Equals(exportInfo.Implementation)).Entry;

                    // A generic type definition is named Type`N in metadata, which is not a valid C# type name.
                    if (exportInfo.Implementation.Name.Contains('`'))
                    {
                        if (firstExport)
                        {
                            reportDiagnostic(Diagnostic.Create(DependencyDiagnostics.GenericExportNotSupported, settings.Locate(entry), exportInfo.Implementation.FullName, "a generic type cannot be registered from appsettings.json; register it in code with typeof(...)"));
                        }
                        continue;
                    }

                    // Scoped wins, as in DependencyToolGenerator: the shorter lifetime cannot capture a scoped dependency in a singleton.
                    if (firstExport && exportInfo.IsScoped && exportInfo.IsShared)
                    {
                        reportDiagnostic(Diagnostic.Create(DependencyDiagnostics.ConflictingLifetimes, settings.Locate(entry), exportInfo.Implementation.FullName));
                    }

                    var contractName = exportInfo.ContractName is not null ? $"\"{exportInfo.ContractName}\"" : "";
                    var keyed = exportInfo.ContractName is not null ? "Keyed" : "";
                    if (exportInfo.IsScoped)
                    {
                        sb.AppendLine($"        services.Add{keyed}Scoped<{exportInfo.Service.FullName}, {exportInfo.Implementation.FullName}>({contractName});");
                    }
                    else if (exportInfo.IsShared)
                    {
                        sb.AppendLine($"        services.Add{keyed}Singleton<{exportInfo.Service.FullName}, {exportInfo.Implementation.FullName}>({contractName});");
                    }
                    else
                    {
                        sb.AppendLine($"        services.Add{keyed}Transient<{exportInfo.Service.FullName}, {exportInfo.Implementation.FullName}>({contractName});");
                    }
                }
            }

            foreach (var (type, entry) in typesByAssembly.Value.Where(t => !registeredTypes.Contains(t.Type)))
            {
                reportDiagnostic(Diagnostic.Create(DependencyDiagnostics.TypeNotExported, settings.Locate(entry), entry, type.FullName, typesByAssembly.Key.Assembly.Name));
            }
        }
    }

    private static bool TryParseEntry(string entry, out TypeInfo typeInfo, out AssemblyName assemblyName)
    {
        typeInfo = null!;
        assemblyName = null!;

        var parts = entry.Split([','], 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var typeName = parts[0].Trim();
        var lastDot = typeName.LastIndexOf('.');
        if (lastDot <= 0 || lastDot == typeName.Length - 1)
        {
            return false;
        }

        try
        {
            assemblyName = new AssemblyName(parts[1].Trim());
        }
        catch (Exception ex) when (ex is ArgumentException or FileLoadException)
        {
            return false;
        }

        if (string.IsNullOrEmpty(assemblyName.Name))
        {
            return false;
        }

        typeInfo = new TypeInfo(typeName);
        return true;
    }

    // Compares the parts given in the requested version: Version=9.0 matches the assembly version 9.0.1.0.
    private static bool IsSameVersion(Version requested, Version actual)
    {
        return requested.Major == actual.Major
            && requested.Minor == actual.Minor
            && (requested.Build < 0 || requested.Build == actual.Build)
            && (requested.Revision < 0 || requested.Revision == actual.Revision);
    }

    private sealed class SettingsFile(string path, SourceText text)
    {
        public string Path { get; } = path;

        public SourceText Text { get; } = text;

        // Points at the first occurrence of the JSON string value, or at the start of the file when it is not found (for example when it contains escape sequences).
        public Location Locate(string? value)
        {
            var start = value is null ? -1 : Text.ToString().IndexOf($"\"{value}\"", StringComparison.Ordinal);
            if (start < 0)
            {
                return Location.Create(Path, new TextSpan(0, 0), new LinePositionSpan(LinePosition.Zero, LinePosition.Zero));
            }

            var span = new TextSpan(start + 1, value!.Length);
            return Location.Create(Path, span, Text.Lines.GetLinePositionSpan(span));
        }

        public Location Locate(long? lineNumber, long? bytePositionInLine)
        {
            if (lineNumber is null || lineNumber < 0 || lineNumber >= Text.Lines.Count)
            {
                return Locate(null);
            }

            // The position is in UTF-8 bytes; it is used as a character offset, which is exact for ASCII content.
            var line = Text.Lines[(int)lineNumber];
            var character = (int)Math.Min(Math.Max(bytePositionInLine ?? 0, 0), line.End - line.Start);
            var position = new LinePosition((int)lineNumber, character);
            return Location.Create(Path, new TextSpan(line.Start + character, 0), new LinePositionSpan(position, position));
        }
    }
}
