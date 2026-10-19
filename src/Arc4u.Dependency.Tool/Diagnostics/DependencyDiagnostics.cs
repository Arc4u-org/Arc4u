using Microsoft.CodeAnalysis;

namespace Arc4u.Dependency.Tool;

/// <summary>
/// The diagnostics reported by the generators of <c>Arc4u.Dependency.Tool</c>.
/// </summary>
internal static class DependencyDiagnostics
{
    private const string Category = "Arc4u.Dependency";

    public static readonly DiagnosticDescriptor InvalidSettings = new(
        id: "ARC4UDEP001",
        title: "The appsettings.json file cannot be read",
        messageFormat: "'{0}' cannot be read: {1}. The generated '{2}' method registers no type.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor MalformedEntry = new(
        id: "ARC4UDEP002",
        title: "Malformed RegisterTypes entry",
        messageFormat: "The RegisterTypes entry '{0}' is skipped: it must be written 'Namespace.Type, AssemblyName'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor AssemblyNotReferenced = new(
        id: "ARC4UDEP003",
        title: "RegisterTypes entry refers to an assembly that is not referenced",
        messageFormat: "The RegisterTypes entry '{0}' is skipped: {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor TypeNotExported = new(
        id: "ARC4UDEP004",
        title: "RegisterTypes entry refers to a type that cannot be registered",
        messageFormat: "The RegisterTypes entry '{0}' is skipped: no public, non-nested type '{1}' with the [Export] attribute was found in '{2}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConflictingLifetimes = new(
        id: "ARC4UDEP005",
        title: "A type has both the [Shared] and [Scoped] attributes",
        messageFormat: "'{0}' has both [Shared] and [Scoped]: it is registered as scoped. Remove the attribute that does not apply.",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor GenericExportNotSupported = new(
        id: "ARC4UDEP006",
        title: "Generic exported type cannot be registered",
        messageFormat: "'{0}' is not registered: {1}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ProjectDirectoryUnknown = new(
        id: "ARC4UDEP007",
        title: "The project folder cannot be determined",
        messageFormat: "'{0}' is ignored: the project folder cannot be determined, so the generator cannot check that the file is at the root of the project",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
