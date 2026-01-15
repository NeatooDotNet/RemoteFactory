using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Model for generating ordinal-based JSON serialization converters.
/// </summary>
internal sealed record OrdinalSerializationModel
{
    public OrdinalSerializationModel(
        string typeName,
        string fullTypeName,
        string @namespace,
        bool isRecord = false,
        bool hasPrimaryConstructor = false,
        IReadOnlyList<OrdinalPropertyModel>? properties = null,
        IReadOnlyList<string>? constructorParameterNames = null,
        IReadOnlyList<string>? usings = null)
    {
        TypeName = typeName;
        FullTypeName = fullTypeName;
        Namespace = @namespace;
        IsRecord = isRecord;
        HasPrimaryConstructor = hasPrimaryConstructor;
        Properties = properties ?? System.Array.Empty<OrdinalPropertyModel>();
        ConstructorParameterNames = constructorParameterNames ?? System.Array.Empty<string>();
        Usings = usings ?? System.Array.Empty<string>();
    }

    public string TypeName { get; }
    public string FullTypeName { get; }
    public string Namespace { get; }
    public bool IsRecord { get; }
    public bool HasPrimaryConstructor { get; }
    public IReadOnlyList<OrdinalPropertyModel> Properties { get; }
    public IReadOnlyList<string> ConstructorParameterNames { get; }
    public IReadOnlyList<string> Usings { get; }
}

/// <summary>
/// Represents a property for ordinal serialization.
/// </summary>
internal sealed record OrdinalPropertyModel
{
    public OrdinalPropertyModel(
        string name,
        string type,
        bool isNullable = false)
    {
        Name = name;
        Type = type;
        IsNullable = isNullable;
    }

    public string Name { get; }
    public string Type { get; }
    public bool IsNullable { get; }
}
