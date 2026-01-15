using System.Collections.Generic;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Base class for all factory method models.
/// </summary>
internal abstract record FactoryMethodModel
{
    protected FactoryMethodModel(
        string name,
        string uniqueName,
        string returnType,
        string serviceType,
        string implementationType,
        FactoryOperation operation,
        bool isRemote = false,
        bool isTask = false,
        bool isAsync = false,
        bool isNullable = false,
        IReadOnlyList<ParameterModel>? parameters = null,
        AuthorizationModel? authorization = null)
    {
        Name = name;
        UniqueName = uniqueName;
        ReturnType = returnType;
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Operation = operation;
        IsRemote = isRemote;
        IsTask = isTask;
        IsAsync = isAsync;
        IsNullable = isNullable;
        Parameters = parameters ?? System.Array.Empty<ParameterModel>();
        Authorization = authorization;
    }

    public string Name { get; }
    public string UniqueName { get; }
    public string ReturnType { get; }
    public string ServiceType { get; }
    public string ImplementationType { get; }
    public FactoryOperation Operation { get; }
    public bool IsRemote { get; }
    public bool IsTask { get; }
    public bool IsAsync { get; }
    public bool IsNullable { get; }
    public IReadOnlyList<ParameterModel> Parameters { get; }
    public AuthorizationModel? Authorization { get; }
    public bool HasAuth => Authorization != null && Authorization.HasAuth;
}
