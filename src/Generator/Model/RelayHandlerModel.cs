using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a class decorated with [FactoryEventHandler&lt;T&gt;] carrying server-side
/// static-method handlers. Instance-method handlers are the former client-relay pattern
/// and are silently skipped by the transform; they do not reach the renderer.
/// </summary>
internal sealed record RelayHandlerModel
{
    public RelayHandlerModel(
        string className,
        string classSignatureText,
        string @namespace,
        IReadOnlyList<string> usings,
        string hintName,
        IReadOnlyList<EventHandlerEntry> entries,
        IReadOnlyList<DiagnosticInfo> diagnostics)
    {
        ClassName = className;
        ClassSignatureText = classSignatureText;
        Namespace = @namespace;
        Usings = usings;
        HintName = hintName;
        Entries = entries;
        Diagnostics = diagnostics;
    }

    public string ClassName { get; }
    public string ClassSignatureText { get; }
    public string Namespace { get; }
    public IReadOnlyList<string> Usings { get; }
    public string HintName { get; }
    public IReadOnlyList<EventHandlerEntry> Entries { get; }
    public IReadOnlyList<DiagnosticInfo> Diagnostics { get; }
}

/// <summary>
/// A single [FactoryEventHandler&lt;T&gt;] entry with the matched server-side static
/// handler method details.
/// </summary>
internal sealed record EventHandlerEntry
{
    public EventHandlerEntry(
        string eventTypeName,
        string methodName,
        bool isStatic,
        bool isAsync,
        IReadOnlyList<ParameterModel> parameters,
        IReadOnlyList<ParameterModel> serviceParameters,
        IReadOnlyList<ParameterModel> allParameters)
    {
        EventTypeName = eventTypeName;
        MethodName = methodName;
        IsStatic = isStatic;
        IsAsync = isAsync;
        Parameters = parameters;
        ServiceParameters = serviceParameters;
        AllParameters = allParameters;
    }

    public string EventTypeName { get; }
    public string MethodName { get; }

    /// <summary>
    /// Always <c>true</c> — the generator only emits registrations for static handlers now.
    /// Instance-method handlers are the removed client-relay pattern and are silently skipped.
    /// </summary>
    public bool IsStatic { get; }
    public bool IsAsync { get; }

    /// <summary>Non-service parameters (event type + CancellationToken), in declaration order.</summary>
    public IReadOnlyList<ParameterModel> Parameters { get; }

    /// <summary>Parameters marked with <c>[Service]</c>, in declaration order.</summary>
    public IReadOnlyList<ParameterModel> ServiceParameters { get; }

    /// <summary>
    /// Every parameter in the order it appears in the method signature. Used by the
    /// renderer to emit invocation arguments in declaration order.
    /// </summary>
    public IReadOnlyList<ParameterModel> AllParameters { get; }
}
