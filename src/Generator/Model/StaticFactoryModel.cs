using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a factory generated for a static class with [Execute] or [Event] methods.
/// </summary>
internal sealed record StaticFactoryModel
{
    public StaticFactoryModel(
        string typeName,
        string signatureText,
        bool isPartial = false,
        IReadOnlyList<ExecuteDelegateModel>? delegates = null,
        IReadOnlyList<EventMethodModel>? events = null,
        IReadOnlyList<EventMethodModel>? factoryEventHandlers = null)
    {
        TypeName = typeName;
        SignatureText = signatureText;
        IsPartial = isPartial;
        Delegates = delegates ?? System.Array.Empty<ExecuteDelegateModel>();
        Events = events ?? System.Array.Empty<EventMethodModel>();
        FactoryEventHandlers = factoryEventHandlers ?? System.Array.Empty<EventMethodModel>();
    }

    public string TypeName { get; }
    public string SignatureText { get; }
    public bool IsPartial { get; }
    public IReadOnlyList<ExecuteDelegateModel> Delegates { get; }
    public IReadOnlyList<EventMethodModel> Events { get; }

    /// <summary>
    /// [FactoryEventHandler] methods — registered into FactoryEventHandlerRegistry
    /// instead of generating individual delegates.
    /// </summary>
    public IReadOnlyList<EventMethodModel> FactoryEventHandlers { get; }
}
