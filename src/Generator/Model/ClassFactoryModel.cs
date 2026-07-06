using System.Collections.Generic;
using System.Linq;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a factory generated for a class with the [Factory] attribute.
/// </summary>
internal sealed record ClassFactoryModel
{
    public ClassFactoryModel(
        string typeName,
        string serviceTypeName,
        string implementationTypeName,
        bool isPartial = false,
        IReadOnlyList<FactoryMethodModel>? methods = null,
        OrdinalSerializationModel? ordinalSerialization = null,
        bool hasDefaultSave = false,
        bool requiresEntityRegistration = false,
        bool registerOrdinalConverter = false,
        IReadOnlyList<string>? dtoReturnTypes = null,
        IReadOnlyList<string>? dtoPreserveTypes = null)
    {
        TypeName = typeName;
        ServiceTypeName = serviceTypeName;
        ImplementationTypeName = implementationTypeName;
        IsPartial = isPartial;
        Methods = methods ?? System.Array.Empty<FactoryMethodModel>();
        OrdinalSerialization = ordinalSerialization;
        HasDefaultSave = hasDefaultSave;
        RequiresEntityRegistration = requiresEntityRegistration;
        RegisterOrdinalConverter = registerOrdinalConverter;
        DtoReturnTypes = dtoReturnTypes ?? System.Array.Empty<string>();
        DtoPreserveTypes = dtoPreserveTypes ?? System.Array.Empty<string>();
    }

    public string TypeName { get; }
    public string ServiceTypeName { get; }
    public string ImplementationTypeName { get; }
    public bool IsPartial { get; }
    public IReadOnlyList<FactoryMethodModel> Methods { get; }
    public OrdinalSerializationModel? OrdinalSerialization { get; }
    public bool HasDefaultSave { get; }
    public bool RequiresEntityRegistration { get; }
    public bool RegisterOrdinalConverter { get; }

    /// <summary>
    /// Plain DTO types that need constructor registration for IL trimming support.
    /// </summary>
    public IReadOnlyList<string> DtoReturnTypes { get; }

    /// <summary>
    /// Positional-record DTO types (no public parameterless ctor) that need
    /// PreserveType registration for IL trimming support.
    /// </summary>
    public IReadOnlyList<string> DtoPreserveTypes { get; }

    /// <summary>
    /// True if ALL factory methods are internal (excluding [Remote] methods, which are promoted to public).
    /// When true, the generated factory interface is internal.
    /// </summary>
    public bool AllMethodsInternal => Methods.Count > 0 && Methods.All(m => m.IsInternal && !m.IsRemote);
    /// <summary>
    /// True if ANY factory method is public or [Remote] (promoted to public on the interface).
    /// </summary>
    public bool HasPublicMethods => Methods.Any(m => !m.IsInternal || m.IsRemote);
}
