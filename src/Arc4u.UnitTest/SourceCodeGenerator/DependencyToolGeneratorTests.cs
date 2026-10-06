#if NET10_0
using System.Collections.Immutable;
using Arc4u.Dependency.Attribute;
using Arc4u.Dependency.Tool;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Arc4u.UnitTest.SourceCodeGenerator;

public class DependencyToolGeneratorTests
{
    private static (string Generated, ImmutableArray<Diagnostic> GeneratorDiagnostics, ImmutableArray<Diagnostic> CompilationErrors) RunGenerator(string source)
    {
        var references = AppDomain.CurrentDomain.GetAssemblies()
                                  .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                                  .Select(a => MetadataReference.CreateFromFile(a.Location))
                                  .Append(MetadataReference.CreateFromFile(typeof(ExportAttribute).Assembly.Location))
                                  .Append(MetadataReference.CreateFromFile(typeof(ServiceCollectionServiceExtensions).Assembly.Location))
                                  .Append(MetadataReference.CreateFromFile(typeof(ServiceCollection).Assembly.Location));

        var compilation = CSharpCompilation.Create("Contoso.Business",
                                                   [CSharpSyntaxTree.ParseText(source)],
                                                   references,
                                                   new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new DependencyToolGenerator());
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var newCompilation, out var diagnostics);

        var generated = newCompilation.SyntaxTrees.Single(t => Path.GetFileName(t.FilePath) == "Dependencies.g.cs").ToString();
        var errors = newCompilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToImmutableArray();
        return (generated, diagnostics, errors);
    }

    [Fact]
    [Trait("Category", "CI")]
    public void SharedAndScopedIsRegisteredAsScopedWithAWarning()
    {
        var (generated, diagnostics, errors) = RunGenerator("""
            using Arc4u.Dependency.Attribute;
            namespace Contoso.Business;
            [Export, Shared, Scoped]
            public class Both { }
            """);

        errors.Should().BeEmpty();
        generated.Should().Contain("services.AddScoped<global::Contoso.Business.Both>();");
        var diagnostic = diagnostics.Should().ContainSingle().Which;
        diagnostic.Id.Should().Be("ARC4UDEP005");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().Should().Contain("Contoso.Business.Both");
    }

    [Fact]
    [Trait("Category", "CI")]
    public void OpenGenericsAreRegisteredWithTypeOf()
    {
        var (generated, diagnostics, errors) = RunGenerator("""
            using Arc4u.Dependency.Attribute;
            namespace Contoso.Business;
            public interface IRepository<T> { }
            public interface IMap<TKey, TValue> { }
            [Export(typeof(IRepository<>)), Shared]
            public class Repository<T> : IRepository<T> { }
            [Export("Memory", typeof(IMap<,>)), Scoped]
            public class Map<TKey, TValue> : IMap<TKey, TValue> { }
            [Export]
            public class Box<T> { }
            """);

        diagnostics.Should().BeEmpty();
        errors.Should().BeEmpty();
        generated.Should().Contain("services.AddSingleton(typeof(global::Contoso.Business.IRepository<>), typeof(global::Contoso.Business.Repository<>));");
        generated.Should().Contain("services.AddKeyedScoped(typeof(global::Contoso.Business.IMap<,>), \"Memory\", typeof(global::Contoso.Business.Map<,>));");
        generated.Should().Contain("services.AddTransient(typeof(global::Contoso.Business.Box<>));");
    }

    [Theory]
    [Trait("Category", "CI")]
    [InlineData("[Export(typeof(IRepository<string>))] public class Repository<T> : IRepository<T> { }")]
    [InlineData("[Export(typeof(IRepository<>))] public class Repository : IRepository<string> { }")]
    [InlineData("[Export(typeof(IRepository<>))] public class Repository<T> { }")]
    [InlineData("[Export(typeof(IMap<,>))] public class Map<TKey, TValue> : IMap<TValue, TKey> { }")]
    [InlineData("public class Outer<T> { [Export] public class Inner { } }")]
    public void GenericExportThatCannotBeRegisteredIsReported(string declaration)
    {
        var (generated, diagnostics, errors) = RunGenerator($$"""
            using Arc4u.Dependency.Attribute;
            namespace Contoso.Business;
            public interface IRepository<T> { }
            public interface IMap<TKey, TValue> { }
            {{declaration}}
            [Export]
            public class Other { }
            """);

        var diagnostic = diagnostics.Should().ContainSingle().Which;
        diagnostic.Id.Should().Be("ARC4UDEP006");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.Location.SourceTree.Should().NotBeNull();

        // The type is skipped, so the rest of the generated code still compiles.
        errors.Should().BeEmpty();
        generated.Should().Contain("services.AddTransient<global::Contoso.Business.Other>();");
    }
}
#endif
