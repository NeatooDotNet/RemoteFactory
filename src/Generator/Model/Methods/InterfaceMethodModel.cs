using System.Collections.Generic;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a factory method defined on an interface that delegates
/// to an injected service implementation.
/// </summary>
internal sealed record InterfaceMethodModel : FactoryMethodModel
{
    public InterfaceMethodModel(
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
        : base(name, uniqueName, returnType, serviceType, implementationType, operation,
               isRemote, isTask, isAsync, isNullable, parameters, authorization)
    {
    }
}
