using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Arc4u.Dependency.Configuration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Arc4u.Dependency.Tool;

[Generator]

public class GenerateRegisteredTypes : IIncrementalGenerator
{
    const string section = "Application.Dependency";
    const string settingsFileName = "appsettings.json";
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
                var normalizedPath = file.Path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                return normalizedPath.EndsWith(normalizedTargetPath, StringComparison.InvariantCultureIgnoreCase)
                    ||
                       normalizedPath.EndsWith(wwwrootTargetPath, StringComparison.InvariantCultureIgnoreCase);
            });


        context.RegisterSourceOutput(appSettingFiles.Combine(context.CompilationProvider), (ctx, source) =>
        {
            var (appSettingFile, compilator) = source;
            var json = appSettingFile.GetText(ctx.CancellationToken)?.ToString();
            var path = appSettingFile?.Path;

            // Retrieve the different directories and must extract the shared part.
            var assemblyPath = GetAssemblyPath(compilator);
            var nugetAssemblies = RetrieveNugetPackages(compilator);

            // Check metadata through options if needed (although we don't set it here, options is available)
            if (json != null && path is not null && assemblyPath is not null)
            {
                var normalizedPath = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                if (normalizedPath.Substring(assemblyPath.Length).Equals(normalizedTargetPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    ctx.AddSource("GeneratedTypes.g.cs", SourceText.From(GenerateRegisterTypes(json, "RegisterTypes", nugetAssemblies), Encoding.UTF8));
                }
                if (normalizedPath.Substring(assemblyPath.Length).Equals(wwwrootTargetPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    ctx.AddSource("GeneratedWwwRootTypes.g.cs", SourceText.From(GenerateRegisterTypes(json, "RegisterWwwTypes", nugetAssemblies), Encoding.UTF8));
                }
            }
        });
    }

    public string? GetAssemblyPath(Compilation compilation)
    {
        var symbolPaths = compilation.Assembly.Locations
                                     .Where(l => l.SourceTree?.FilePath is not null)
                                     .Select(l => Path.GetDirectoryName(l.SourceTree?.FilePath))
                                     .ToList();

        // Find the minimum length of the strings in the list
        var minLength = symbolPaths.Min(path => path!.Length);

        // Select all the strings that have the shortest length
        var shortestPaths = symbolPaths.Where(path => path!.Length == minLength).ToList();

        // I can have more than one path with the same length: c:\Temp\A\B and c:\Temp\C\D.
        // I need to find the common part: c:\Temp\
        // I will take the first one and remove the last part of the path.
        if (shortestPaths.Count == 1)
        {
            return shortestPaths[0];
        }
        if (shortestPaths.Count > 1)
        {
            var commonPath = shortestPaths[0];
            for (var i = 1; i < shortestPaths.Count; i++)
            {
                commonPath = GetCommonPath(commonPath, shortestPaths[i]);
            }
            return commonPath;
        }

        return null;
    }

    private string GetCommonPath(string path1, string path2)
    {
        var parts1 = path1.Split(Path.DirectorySeparatorChar);
        var parts2 = path2.Split(Path.DirectorySeparatorChar);
        var commonParts = parts1.TakeWhile((part, index) => index < parts2.Length && part == parts2[index]);
        return string.Join(Path.DirectorySeparatorChar.ToString(), commonParts);
    }

    private static List<string> RetrieveNugetPackages(Compilation compilation)
    {
        // Get all metadata references (assemblies) in the compilation
        var metadataReferences = compilation.ExternalReferences;
        // Retrieve the paths to the assemblies
        var assemblyPaths = metadataReferences
                                .OfType<PortableExecutableReference>()
                                .Select(reference => reference.FilePath)
                                .Where(path => !string.IsNullOrEmpty(path))
                                .ToList();

        return assemblyPaths!;
    }

    private string GenerateRegisterTypes(string text, string memberName, List<string> nugetPaths)
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

        // Read the content of the file and generate the code.
        using (var document = JsonDocument.Parse(text))
        {
            if (document.RootElement.TryGetProperty(section, out var sectionElement))
            {
                var dependencies = JsonSerializer.Deserialize<Dependencies>(sectionElement);
                if (null != dependencies)
                {
                    // Types registered by nuget package!
                    var typesByAssemblies = new Dictionary<NugetInfo, List<TypeInfo>>();
                    foreach (var typeString in dependencies.RegisterTypes)
                    {
                        var parts = typeString.Split([','], 2);
                        if (parts.Length == 2)
                        {
                            var typeInfo = new TypeInfo(parts[0].Trim());
                            var assemblyString = parts[1].Trim();

                            // Parse the assembly information
                            var assemblyName = new AssemblyName(assemblyString);

                            var nugetFiles = nugetPaths.Where(p => p.EndsWith($"{assemblyName.Name}.dll"));
                            if (assemblyName.Version is not null)
                            {
                                nugetFiles = nugetFiles.Where(p => p.Contains(assemblyName.Version.ToString()));
                            }

                            if (nugetFiles.Any())
                            {
                                var nugetInfo = new NugetInfo(nugetFiles.First(), assemblyName);

                                if (!typesByAssemblies.ContainsKey(nugetInfo))
                                {
                                    typesByAssemblies.Add(nugetInfo, [typeInfo]);
                                }
                                else
                                {
                                    typesByAssemblies[nugetInfo].Add(typeInfo);
                                }
                            }
                        }
                    }

                    // Parse each registered nuget files and register the types to the service collection.
                    foreach (var typesByAssembly in typesByAssemblies)
                    {
                        sb.AppendLine($"        // Types registered by nuget package: {typesByAssembly.Key.Assembly.FullName}");
                        foreach (var exportInfo in TypeExportAttributeScanner.GetExportDescriptions(typesByAssembly.Key.NugetFile, typesByAssembly.Value))
                        {
                            if (typesByAssembly.Value.Contains(exportInfo.Implementation))
                            {
                                var contractName = exportInfo.ContractName is not null ? $"\"{exportInfo.ContractName}\"" : "";
                                var keyed = exportInfo.ContractName is not null ? "Keyed" : "";
                                if (exportInfo.IsShared)
                                {
                                    sb.AppendLine($"        services.Add{keyed}Singleton<{exportInfo.Service.FullName}, {exportInfo.Implementation.FullName}>({contractName});");
                                }
                                else if (exportInfo.IsScoped)
                                {
                                    sb.AppendLine($"        services.Add{keyed}Scoped<{exportInfo.Service.FullName}, {exportInfo.Implementation.FullName}>({contractName});");
                                }
                                else
                                {
                                    sb.AppendLine($"        services.Add{keyed}Transient<{exportInfo.Service.FullName}, {exportInfo.Implementation.FullName}>({contractName});");
                                }
                            }

                        }

                    }
                }
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();

    }
}
