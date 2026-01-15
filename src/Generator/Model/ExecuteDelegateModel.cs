using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a delegate for static factory execute operations.
/// </summary>
internal sealed record ExecuteDelegateModel
{
    public ExecuteDelegateModel(
        string name,
        string delegateName,
        string returnType,
        bool isNullable = false,
        IReadOnlyList<ParameterModel>? parameters = null,
        IReadOnlyList<ParameterModel>? serviceParameters = null,
        bool hasCancellationToken = false)
    {
        Name = name;
        DelegateName = delegateName;
        ReturnType = returnType;
        IsNullable = isNullable;
        Parameters = parameters ?? System.Array.Empty<ParameterModel>();
        ServiceParameters = serviceParameters ?? System.Array.Empty<ParameterModel>();
        HasCancellationToken = hasCancellationToken;
    }

    public string Name { get; }
    public string DelegateName { get; }
    public string ReturnType { get; }
    public bool IsNullable { get; }
    public IReadOnlyList<ParameterModel> Parameters { get; }
    public IReadOnlyList<ParameterModel> ServiceParameters { get; }
    /// <summary>
    /// Whether the domain method has a CancellationToken parameter.
    /// </summary>
    public bool HasCancellationToken { get; }
}
