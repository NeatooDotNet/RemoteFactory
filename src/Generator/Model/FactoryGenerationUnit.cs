using System.Collections.Generic;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a complete unit of code generation for a single factory.
/// Contains either a ClassFactoryModel, StaticFactoryModel, or InterfaceFactoryModel.
/// </summary>
internal sealed record FactoryGenerationUnit
{
    public FactoryGenerationUnit(
        string @namespace,
        IReadOnlyList<string>? usings,
        FactoryMode mode,
        string hintName,
        IReadOnlyList<DiagnosticInfo>? diagnostics = null,
        ClassFactoryModel? classFactory = null,
        StaticFactoryModel? staticFactory = null,
        InterfaceFactoryModel? interfaceFactory = null)
    {
        Namespace = @namespace;
        Usings = usings ?? System.Array.Empty<string>();
        Mode = mode;
        HintName = hintName;
        Diagnostics = diagnostics ?? System.Array.Empty<DiagnosticInfo>();
        ClassFactory = classFactory;
        StaticFactory = staticFactory;
        InterfaceFactory = interfaceFactory;
    }

    public string Namespace { get; }
    public IReadOnlyList<string> Usings { get; }
    public FactoryMode Mode { get; }
    public string HintName { get; }
    public IReadOnlyList<DiagnosticInfo> Diagnostics { get; }

    /// <summary>
    /// Factory model for classes with [Factory] attribute.
    /// Exactly one of ClassFactory, StaticFactory, or InterfaceFactory should be set.
    /// </summary>
    public ClassFactoryModel? ClassFactory { get; }

    /// <summary>
    /// Factory model for static classes with [Execute] or [Event] methods.
    /// </summary>
    public StaticFactoryModel? StaticFactory { get; }

    /// <summary>
    /// Factory model for interfaces with [Factory] attribute.
    /// </summary>
    public InterfaceFactoryModel? InterfaceFactory { get; }
}
