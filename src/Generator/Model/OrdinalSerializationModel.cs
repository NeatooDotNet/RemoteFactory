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
        bool isNullable = false,
        bool isLazyLoad = false,
        string? innerType = null)
    {
        Name = name;
        Type = type;
        IsNullable = isNullable;
        IsLazyLoad = isLazyLoad;
        InnerType = innerType;
    }

    public string Name { get; }
    public string Type { get; }
    public bool IsNullable { get; }

    /// <summary>
    /// True if this property is a LazyLoad&lt;T&gt;. When true, the property occupies
    /// two ordinal slots: Value (using <see cref="InnerType"/>) and IsLoaded (bool).
    /// </summary>
    public bool IsLazyLoad { get; }

    /// <summary>
    /// The fully-qualified inner type T when <see cref="IsLazyLoad"/> is true.
    /// For example, for LazyLoad&lt;string&gt; this would be "string".
    /// </summary>
    public string? InnerType { get; }
}
