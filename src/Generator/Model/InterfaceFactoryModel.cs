using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a factory generated for an interface with the [Factory] attribute.
/// The generated implementation delegates to an injected service.
/// </summary>
internal sealed record InterfaceFactoryModel
{
    public InterfaceFactoryModel(
        string serviceTypeName,
        string implementationTypeName,
        IReadOnlyList<InterfaceMethodModel>? methods = null,
        IReadOnlyList<string>? dtoReturnTypes = null,
        IReadOnlyList<string>? dtoPreserveTypes = null)
    {
        ServiceTypeName = serviceTypeName;
        ImplementationTypeName = implementationTypeName;
        Methods = methods ?? System.Array.Empty<InterfaceMethodModel>();
        DtoReturnTypes = dtoReturnTypes ?? System.Array.Empty<string>();
        DtoPreserveTypes = dtoPreserveTypes ?? System.Array.Empty<string>();
    }

    public string ServiceTypeName { get; }
    public string ImplementationTypeName { get; }
    public IReadOnlyList<InterfaceMethodModel> Methods { get; }

    /// <summary>
    /// Plain DTO types that need constructor registration for IL trimming support.
    /// </summary>
    public IReadOnlyList<string> DtoReturnTypes { get; }

    /// <summary>
    /// Positional-record DTO types (no public parameterless ctor) that need
    /// PreserveType registration for IL trimming support.
    /// </summary>
    public IReadOnlyList<string> DtoPreserveTypes { get; }
}
