using System.Collections.Generic;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents an Insert, Update, or Delete factory method.
/// Write methods operate on an existing target instance.
/// </summary>
internal sealed record WriteMethodModel : FactoryMethodModel
{
    public WriteMethodModel(
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
        bool isDomainMethodTask = false,
        bool isBool = false)
        : base(name, uniqueName, returnType, serviceType, implementationType, operation,
               isRemote, isTask, isAsync, isNullable, parameters, authorization)
    {
        IsDomainMethodTask = isDomainMethodTask;
        IsBool = isBool;
    }

    /// <summary>
    /// Whether the underlying domain method (Insert/Update/Delete) returns Task.
    /// </summary>
    public bool IsDomainMethodTask { get; }

    /// <summary>
    /// Whether the underlying domain method returns bool (success/failure indicator).
    /// </summary>
    public bool IsBool { get; }
}
