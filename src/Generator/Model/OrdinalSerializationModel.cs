using System.Collections.Generic;
using System.Linq;

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

    /// <summary>
    /// True when the converter writes $id/$ref for the object (#133). Records are value
    /// objects and bypass reference tracking, as they do in the Named format.
    /// </summary>
    public bool TracksReferences => !IsRecord;

    /// <summary>
    /// True when the converter builds the object before reading its properties, so a back
    /// reference to it from a child resolves. Needs every property assignable after construction.
    /// </summary>
    public bool BuildsFirst => TracksReferences && Properties.All(p => p.IsSettableAfterConstruction);
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
        string? innerType = null,
        bool isSettableAfterConstruction = true)
    {
        Name = name;
        Type = type;
        IsNullable = isNullable;
        IsLazyLoad = isLazyLoad;
        InnerType = innerType;
        IsSettableAfterConstruction = isSettableAfterConstruction;
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

    /// <summary>
    /// True when the property can be assigned after construction (not init-only, not required).
    /// </summary>
    public bool IsSettableAfterConstruction { get; }
}
