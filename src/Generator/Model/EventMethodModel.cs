using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents an event method that runs in an isolated scope.
/// </summary>
internal sealed record EventMethodModel
{
    public EventMethodModel(
        string name,
        string delegateName,
        bool isAsync = false,
        IReadOnlyList<ParameterModel>? parameters = null,
        IReadOnlyList<ParameterModel>? serviceParameters = null,
        string? containingTypeName = null,
        bool isStaticClass = false)
    {
        Name = name;
        DelegateName = delegateName;
        IsAsync = isAsync;
        Parameters = parameters ?? System.Array.Empty<ParameterModel>();
        ServiceParameters = serviceParameters ?? System.Array.Empty<ParameterModel>();
        ContainingTypeName = containingTypeName;
        IsStaticClass = isStaticClass;
    }

    public string Name { get; }
    public string DelegateName { get; }
    public bool IsAsync { get; }
    public IReadOnlyList<ParameterModel> Parameters { get; }
    public IReadOnlyList<ParameterModel> ServiceParameters { get; }
    public string? ContainingTypeName { get; }
    public bool IsStaticClass { get; }
}
