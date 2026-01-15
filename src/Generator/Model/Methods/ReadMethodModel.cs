using System.Collections.Generic;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a Create or Fetch factory method.
/// </summary>
internal sealed record ReadMethodModel : FactoryMethodModel
{
    public ReadMethodModel(
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
        AuthorizationModel? authorization = null,
        bool isConstructor = false,
        bool isStaticFactory = false,
        bool isBool = false)
        : base(name, uniqueName, returnType, serviceType, implementationType, operation,
               isRemote, isTask, isAsync, isNullable, parameters, authorization)
    {
        IsConstructor = isConstructor;
        IsStaticFactory = isStaticFactory;
        IsBool = isBool;
    }

    public bool IsConstructor { get; }
    public bool IsStaticFactory { get; }
    public bool IsBool { get; }
}
