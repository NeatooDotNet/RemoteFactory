using System.Collections.Generic;
using Neatoo.RemoteFactory.FactoryGenerator;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a Save factory method that delegates to Insert, Update, or Delete
/// based on the entity's state.
/// </summary>
internal sealed record SaveMethodModel : FactoryMethodModel
{
    public SaveMethodModel(
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
        WriteMethodModel? insertMethod = null,
        WriteMethodModel? updateMethod = null,
        WriteMethodModel? deleteMethod = null,
        bool isDefault = false)
        : base(name, uniqueName, returnType, serviceType, implementationType, operation,
               isRemote, isTask, isAsync, isNullable, parameters, authorization)
    {
        InsertMethod = insertMethod;
        UpdateMethod = updateMethod;
        DeleteMethod = deleteMethod;
        IsDefault = isDefault;
    }

    public WriteMethodModel? InsertMethod { get; }
    public WriteMethodModel? UpdateMethod { get; }
    public WriteMethodModel? DeleteMethod { get; }
    public bool IsDefault { get; }
}
