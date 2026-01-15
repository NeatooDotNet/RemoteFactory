using System.Collections.Generic;

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
        IReadOnlyList<EventMethodModel>? events = null,
        OrdinalSerializationModel? ordinalSerialization = null,
        bool hasDefaultSave = false,
        bool requiresEntityRegistration = false,
        bool registerOrdinalConverter = false)
    {
        TypeName = typeName;
        ServiceTypeName = serviceTypeName;
        ImplementationTypeName = implementationTypeName;
        IsPartial = isPartial;
        Methods = methods ?? System.Array.Empty<FactoryMethodModel>();
        Events = events ?? System.Array.Empty<EventMethodModel>();
        OrdinalSerialization = ordinalSerialization;
        HasDefaultSave = hasDefaultSave;
        RequiresEntityRegistration = requiresEntityRegistration;
        RegisterOrdinalConverter = registerOrdinalConverter;
    }

    public string TypeName { get; }
    public string ServiceTypeName { get; }
    public string ImplementationTypeName { get; }
    public bool IsPartial { get; }
    public IReadOnlyList<FactoryMethodModel> Methods { get; }
    public IReadOnlyList<EventMethodModel> Events { get; }
    public OrdinalSerializationModel? OrdinalSerialization { get; }
    public bool HasDefaultSave { get; }
    public bool RequiresEntityRegistration { get; }
    public bool RegisterOrdinalConverter { get; }
}
