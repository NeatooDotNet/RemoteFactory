using System.Collections.Generic;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents an [Execute] method on a non-static [Factory] class.
/// Unlike ReadMethodModel, Execute methods don't call DoFactoryMethodCall --
/// they directly invoke the public static method on the implementation type.
/// </summary>
internal sealed record ClassExecuteMethodModel : FactoryMethodModel
{
    public ClassExecuteMethodModel(
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
        IReadOnlyList<ParameterModel>? serviceParameters = null,
        bool hasCancellationToken = false,
        bool isInternal = false)
        : base(name, uniqueName, returnType, serviceType, implementationType, operation,
               isRemote, isTask, isAsync, isNullable, parameters, authorization, isInternal)
    {
        ServiceParameters = serviceParameters ?? System.Array.Empty<ParameterModel>();
        HasCancellationToken = hasCancellationToken;
    }

    /// <summary>
    /// Server-only services injected from DI, excluded from the delegate signature.
    /// </summary>
    public IReadOnlyList<ParameterModel> ServiceParameters { get; }

    /// <summary>
    /// Whether the domain method accepts a CancellationToken parameter.
    /// </summary>
    public bool HasCancellationToken { get; }
}
