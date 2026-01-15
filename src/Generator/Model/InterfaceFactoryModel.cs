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
        IReadOnlyList<InterfaceMethodModel>? methods = null)
    {
        ServiceTypeName = serviceTypeName;
        ImplementationTypeName = implementationTypeName;
        Methods = methods ?? System.Array.Empty<InterfaceMethodModel>();
    }

    public string ServiceTypeName { get; }
    public string ImplementationTypeName { get; }
    public IReadOnlyList<InterfaceMethodModel> Methods { get; }
}
