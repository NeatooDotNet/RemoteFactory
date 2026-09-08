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
        bool isRemote,
        bool isNullable = false,
        IReadOnlyList<ParameterModel>? parameters = null,
        IReadOnlyList<ParameterModel>? serviceParameters = null,
        bool hasCancellationToken = false)
    {
        Name = name;
        DelegateName = delegateName;
        ReturnType = returnType;
        IsRemote = isRemote;
        IsNullable = isNullable;
        Parameters = parameters ?? System.Array.Empty<ParameterModel>();
        ServiceParameters = serviceParameters ?? System.Array.Empty<ParameterModel>();
        HasCancellationToken = hasCancellationToken;
    }

    public string Name { get; }
    public string DelegateName { get; }
    public string ReturnType { get; }
    /// <summary>
    /// Whether the delegate is a client-to-server entry point. Derived from [Remote] (or a remote
    /// authorization method / [AspAuthorize]) exactly as for class-factory methods — [Execute] is
    /// no longer forced remote. A local-only delegate gets one unguarded registration in every
    /// factory mode and no remote registration.
    /// </summary>
    public bool IsRemote { get; }
    public bool IsNullable { get; }
    public IReadOnlyList<ParameterModel> Parameters { get; }
    public IReadOnlyList<ParameterModel> ServiceParameters { get; }
    /// <summary>
    /// Whether the domain method has a CancellationToken parameter.
    /// </summary>
    public bool HasCancellationToken { get; }
}
