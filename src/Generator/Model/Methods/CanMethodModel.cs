using System.Collections.Generic;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a CanXxx authorization check method that returns Authorized status
/// rather than the entity itself.
/// </summary>
internal sealed record CanMethodModel : FactoryMethodModel
{
    /// <summary>
    /// Whether the source operation method (e.g., Fetch for CanFetch) has [Remote].
    /// Used for interface visibility promotion: when true, the CanXxx method is promoted
    /// to public on the factory interface even if the source method is internal.
    /// </summary>
    public bool IsSourceMethodRemote { get; }

    public CanMethodModel(
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
        bool isInternal = false,
        bool isSourceMethodRemote = false)
        : base(name, uniqueName, returnType, serviceType, implementationType, operation,
               isRemote, isTask, isAsync, isNullable, parameters, authorization, isInternal)
    {
        IsSourceMethodRemote = isSourceMethodRemote;
    }
}
