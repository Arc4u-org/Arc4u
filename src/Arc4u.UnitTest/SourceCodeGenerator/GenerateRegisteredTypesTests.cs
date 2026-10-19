#if NET10_0
using System.Collections.Immutable;
using Arc4u.Dependency.Tool;
using Arc4u.UnitTest.Dependency;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Arc4u.UnitTest.SourceCodeGenerator;


public class GenerateRegisteredTypesTests
{
    private const string DddRegistryText = $@"
{{
""Application.Dependency"": {{
    ""RegisterTypes"": [
      ""Arc4u.AppSettings, Arc4u.Configuration"",
      ""Arc4u.Diagnostics.DefaultLoggingProperties, Arc4u"",
      ""Arc4u.UnitTest.Dependency.ImpTuple, Arc4u.UnitTest"",
      ""Arc4u.Blazor.Options.ConfigureAuthStateDeserializationOptions, Arc4u.OAuth2.Blazor""
    ]
  }}
}}";

    // A project folder that only exists in the paths given to the compilation: nothing is read from disk.
    private static readonly string ProjectDir = Path.Combine(Path.GetTempPath(), "Contoso.Api") + Path.DirectorySeparatorChar;

    private static (SyntaxTree? Generated, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(string json, string settingsPath, string? projectDir, params string[] sourcePaths)
    {
        var optionsProvider = new TestAnalyzerConfigOptionsProvider(
            projectDir is null ? new Dictionary<string, string>() : new Dictionary<string, string> { ["build_property.ProjectDir"] = projectDir });

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new GenerateRegisteredTypes().AsSourceGenerator()],
            [new TestAdditionalFile(settingsPath, json)],
            optionsProvider: optionsProvider);

        var syntaxTrees = sourcePaths.Select(path => CSharpSyntaxTree.ParseText(SourceText.From("public class Dummy {}"), path: path));

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Arc4u.Blazor.Options.ConfigureAuthStateDeserializationOptions).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ImpTuple).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Arc4u.AppSettings).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Arc4u.Diagnostics.DefaultLoggingProperties).Assembly.Location)
        };

        // I have to create a Compiler with Assemblies to test the GenerateRegisteredTypes
        var compilation = CSharpCompilation.Create(nameof(GenerateRegisteredTypes), syntaxTrees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        var generated = newCompilation.SyntaxTrees.SingleOrDefault(t => Path.GetFileName(t.FilePath).Equals("GeneratedTypes.g.cs"));
        return (generated, diagnostics);
    }

    private static string Settings(params string[] entries) =>
        $$"""
        {
          "Application.Dependency": {
            "RegisterTypes": [ {{string.Join(", ", entries.Select(e => $"\"{e}\""))}} ]
          }
        }
        """;

    [Fact]
    [Trait("Category", "CI")]
    public void GenerateClassesBasedOnDDDRegistry()
    {
        // Without ProjectDir, the project folder is guessed from the source files.
        var (generatedFile, diagnostics) = RunGenerator(DddRegistryText,
                                                        Path.Combine(Directory.GetCurrentDirectory(), "Configs/appsettings.json"),
                                                        projectDir: null,
                                                        Path.Combine(Directory.GetCurrentDirectory(), "Dummy.cs"));

        generatedFile.Should().NotBeNull();
        diagnostics.Should().BeEmpty();

        // check we have the 2 registrations!
        generatedFile!.ToString().Should().Contain("public static void RegisterTypes(this IServiceCollection services)");
        generatedFile.ToString().Should().Contain("services.AddSingleton<Arc4u.IAppSettings, Arc4u.AppSettings>();");
        generatedFile.ToString().Should().Contain("services.AddKeyedScoped<Arc4u.Diagnostics.IAddPropertiesToLog, Arc4u.Diagnostics.DefaultLoggingProperties>(\"Scoped\");");
        generatedFile.ToString().Should().Contain("services.AddTransient<Arc4u.UnitTest.Dependency.ITuple<System.Int32, System.String>, Arc4u.UnitTest.Dependency.ImpTuple>();");
        generatedFile.ToString().Should().Contain("services.AddSingleton<Microsoft.Extensions.Options.IConfigureOptions<Microsoft.AspNetCore.Components.WebAssembly.Authentication.AuthenticationStateDeserializationOptions>, Arc4u.Blazor.Options.ConfigureAuthStateDeserializationOptions>();");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void ProjectDirIsUsedWhenNoSourceFileIsAtTheProjectRoot()
    {
        var (generatedFile, diagnostics) = RunGenerator(Settings("Arc4u.AppSettings, Arc4u.Configuration"),
                                                        Path.Combine(ProjectDir, "Configs", "appsettings.json"),
                                                        ProjectDir,
                                                        Path.Combine(ProjectDir, "Controllers", "A.cs"),
                                                        Path.Combine(ProjectDir, "Services", "B.cs"));

        diagnostics.Should().BeEmpty();
        generatedFile.Should().NotBeNull();
        generatedFile!.ToString().Should().Contain("services.AddSingleton<Arc4u.IAppSettings, Arc4u.AppSettings>();");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void SettingsFileNotAtTheProjectRootIsIgnored()
    {
        var (generatedFile, diagnostics) = RunGenerator(Settings("Arc4u.AppSettings, Arc4u.Configuration"),
                                                        Path.Combine(ProjectDir, "Sub", "Configs", "appsettings.json"),
                                                        ProjectDir,
                                                        Path.Combine(ProjectDir, "Program.cs"));

        diagnostics.Should().BeEmpty();
        generatedFile.Should().BeNull();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void ProjectFolderThatCannotBeDeterminedIsReported()
    {
        var (generatedFile, diagnostics) = RunGenerator(Settings("Arc4u.AppSettings, Arc4u.Configuration"),
                                                        Path.Combine(ProjectDir, "Configs", "appsettings.json"),
                                                        projectDir: null);

        generatedFile.Should().BeNull();
        diagnostics.Should().ContainSingle().Which.Id.Should().Be("ARC4UDEP007");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void CommentsAndTrailingCommasAreAccepted()
    {
        const string json = """
        {
          // Registered by the generator.
          "Application.Dependency": {
            "RegisterTypes": [
              "Arc4u.AppSettings, Arc4u.Configuration", /* the settings */
            ],
          },
        }
        """;

        var (generatedFile, diagnostics) = RunGenerator(json, Path.Combine(ProjectDir, "Configs", "appsettings.json"), ProjectDir, Path.Combine(ProjectDir, "Program.cs"));

        diagnostics.Should().BeEmpty();
        generatedFile!.ToString().Should().Contain("services.AddSingleton<Arc4u.IAppSettings, Arc4u.AppSettings>();");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void InvalidJsonIsReportedAtItsPositionAndAnEmptyMethodIsGenerated()
    {
        const string json = """
        {
          "Application.Dependency": {
            "RegisterTypes": [ "Arc4u.AppSettings, Arc4u.Configuration" "Arc4u.Diagnostics.DefaultLoggingProperties, Arc4u" ]
          }
        }
        """;
        var settingsPath = Path.Combine(ProjectDir, "Configs", "appsettings.json");

        var (generatedFile, diagnostics) = RunGenerator(json, settingsPath, ProjectDir, Path.Combine(ProjectDir, "Program.cs"));

        var diagnostic = diagnostics.Should().ContainSingle().Which;
        diagnostic.Id.Should().Be("ARC4UDEP001");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.Location.GetLineSpan().Path.Should().Be(settingsPath);
        diagnostic.Location.GetLineSpan().StartLinePosition.Line.Should().Be(2);

        generatedFile!.ToString().Should().Contain("public static void RegisterTypes(this IServiceCollection services)");
        generatedFile.ToString().Should().NotContain("services.Add");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void RegisterTypesThatIsNotAnArrayIsReported()
    {
        const string json = """{ "Application.Dependency": { "RegisterTypes": "Arc4u.AppSettings, Arc4u.Configuration" } }""";

        var (generatedFile, diagnostics) = RunGenerator(json, Path.Combine(ProjectDir, "Configs", "appsettings.json"), ProjectDir, Path.Combine(ProjectDir, "Program.cs"));

        diagnostics.Should().ContainSingle().Which.Id.Should().Be("ARC4UDEP001");
        generatedFile.Should().NotBeNull();
    }

    [Fact]
    [Trait("Category", "CI")]
    public void EveryExportOfATypeIsRegistered()
    {
        var (generatedFile, diagnostics) = RunGenerator(Settings("Arc4u.UnitTest.Dependency.MultiExport, Arc4u.UnitTest"),
                                                        Path.Combine(ProjectDir, "Configs", "appsettings.json"),
                                                        ProjectDir,
                                                        Path.Combine(ProjectDir, "Program.cs"));

        diagnostics.Should().BeEmpty();
        generatedFile.Should().NotBeNull();
        var generated = generatedFile!.ToString();
        generated.Should().Contain("services.AddSingleton<Arc4u.UnitTest.Dependency.ISingletonObject, Arc4u.UnitTest.Dependency.MultiExport>();");
        generated.Should().Contain("services.AddKeyedSingleton<Arc4u.UnitTest.Dependency.ITuple<System.Int32, System.String>, Arc4u.UnitTest.Dependency.MultiExport>(\"Multi\");");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void FourPartAssemblyVersionMatchesTheReferencedAssembly()
    {
        var version = typeof(Arc4u.AppSettings).Assembly.GetName().Version!;

        var (generatedFile, diagnostics) = RunGenerator(Settings($"Arc4u.AppSettings, Arc4u.Configuration, Version={version}"),
                                                        Path.Combine(ProjectDir, "Configs", "appsettings.json"),
                                                        ProjectDir,
                                                        Path.Combine(ProjectDir, "Program.cs"));

        diagnostics.Should().BeEmpty();
        generatedFile!.ToString().Should().Contain("services.AddSingleton<Arc4u.IAppSettings, Arc4u.AppSettings>();");
    }

    [Theory]
    [Trait("Category", "CI")]
    [InlineData("Arc4u.AppSettings", "ARC4UDEP002")]
    [InlineData("AppSettings, Arc4u.Configuration", "ARC4UDEP002")]
    [InlineData("Arc4u.AppSettings, Arc4u.Configuration, Version=abc", "ARC4UDEP002")]
    [InlineData("Arc4u.AppSettings, Contoso.Missing", "ARC4UDEP003")]
    [InlineData("Arc4u.AppSettings, Arc4u.Configuration, Version=0.0.0.1", "ARC4UDEP003")]
    [InlineData("Arc4u.DoesNotExist, Arc4u.Configuration", "ARC4UDEP004")]
    [InlineData("Arc4u.UnitTest.SourceCodeGenerator.GenerateRegisteredTypesTests, Arc4u.UnitTest", "ARC4UDEP004")]
    public void UnmatchedEntryIsReportedAndSkipped(string entry, string expectedId)
    {
        var settingsPath = Path.Combine(ProjectDir, "Configs", "appsettings.json");

        var (generatedFile, diagnostics) = RunGenerator(Settings(entry, "Arc4u.Diagnostics.DefaultLoggingProperties, Arc4u"), settingsPath, ProjectDir, Path.Combine(ProjectDir, "Program.cs"));

        var diagnostic = diagnostics.Should().ContainSingle().Which;
        diagnostic.Id.Should().Be(expectedId);
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().Should().Contain(entry);
        diagnostic.Location.GetLineSpan().Path.Should().Be(settingsPath);
        diagnostic.Location.GetLineSpan().StartLinePosition.Line.Should().Be(2);

        // The other entries are still registered.
        generatedFile!.ToString().Should().Contain("Arc4u.Diagnostics.DefaultLoggingProperties>(\"Scoped\");");
    }
}
#endif
