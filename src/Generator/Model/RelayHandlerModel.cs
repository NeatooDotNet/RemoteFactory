using System.Collections.Generic;

namespace Neatoo.RemoteFactory.Generator.Model;

/// <summary>
/// Represents a class decorated with [FactoryEventHandler&lt;T&gt;] carrying server-side
/// static-method handlers. Instance-method handlers are the former client-relay pattern
/// and are silently skipped by the transform; they do not reach the renderer.
/// </summary>
/// <remarks>
/// This is a transform output, so its equality IS the incremental pipeline's cache
/// boundary — the branch calls RegisterSourceOutput directly on the transform node.
/// Every collection is therefore <see cref="EquatableArray{T}"/>: the record's
/// synthesized Equals falls back to <c>EqualityComparer&lt;T&gt;.Default</c>, which for
/// an IReadOnlyList&lt;T&gt; means reference equality, and two structurally identical
/// lists built on successive runs would compare unequal. Constructors normalize
/// incoming sequences rather than accepting the array type, so a future call site
/// cannot reintroduce a reference-equality field by passing a plain list.
/// </remarks>
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
        Usings = new EquatableArray<string>([.. usings]);
        HintName = hintName;
        Entries = new EquatableArray<EventHandlerEntry>([.. entries]);
        Diagnostics = new EquatableArray<DiagnosticInfo>([.. diagnostics]);
    }

    public string ClassName { get; }
    public string ClassSignatureText { get; }
    public string Namespace { get; }
    public EquatableArray<string> Usings { get; }
    public string HintName { get; }
    public EquatableArray<EventHandlerEntry> Entries { get; }
    public EquatableArray<DiagnosticInfo> Diagnostics { get; }
}

/// <summary>
/// A single [FactoryEventHandler&lt;T&gt;] entry with the matched server-side static
/// handler method details.
/// </summary>
/// <remarks>
/// Reached through <see cref="RelayHandlerModel.Entries"/>, so this participates in the
/// same cache boundary and follows the same collection rule — see the remarks on
/// <see cref="RelayHandlerModel"/>.
/// </remarks>
internal sealed record EventHandlerEntry
{
    public EventHandlerEntry(
        string eventTypeName,
        string methodName,
        bool isStatic,
        bool isAsync,
        IReadOnlyList<ParameterModel> parameters,
        IReadOnlyList<ParameterModel> serviceParameters,
        IReadOnlyList<ParameterModel> allParameters,
        string phaseName,
        int phaseValue,
        bool coalesce)
    {
        EventTypeName = eventTypeName;
        MethodName = methodName;
        IsStatic = isStatic;
        IsAsync = isAsync;
        Parameters = new EquatableArray<ParameterModel>([.. parameters]);
        ServiceParameters = new EquatableArray<ParameterModel>([.. serviceParameters]);
        AllParameters = new EquatableArray<ParameterModel>([.. allParameters]);
        PhaseName = phaseName;
        PhaseValue = phaseValue;
        Coalesce = coalesce;
    }

    public string EventTypeName { get; }
    public string MethodName { get; }

    /// <summary>
    /// Name of the <c>DispatchPhase</c> member the attribute declared — <c>"Immediate"</c> when
    /// the consumer wrote no argument. Empty when the declared value matches no member of the
    /// runtime enum, i.e. an explicit cast to an undefined value; the renderer then falls back
    /// to <see cref="PhaseValue"/>.
    /// </summary>
    /// <remarks>
    /// The phase is carried as primitives — never a <c>TypedConstant</c>, never an
    /// <c>ISymbol</c>. Both of those drag a <c>Compilation</c> into the incremental cache
    /// through this record's equality, and the caching test cannot see it: it compares
    /// transform outputs across two runs that share a reference manager, so symbol-bearing
    /// fields compare equal and the leak stays green. Enforced by review, not by a test.
    /// <para>
    /// <c>DispatchPhase</c> itself is deliberately unavailable here — see the type-duplication
    /// warning on <c>FactoryEventHandlerAttribute&lt;T&gt;</c>. The member name is looked up on
    /// the enum symbol rather than hardcoded, so a phase added to the runtime enum needs no
    /// generator change.
    /// </para>
    /// </remarks>
    public string PhaseName { get; }

    /// <summary>
    /// Numeric value of the declared phase. Only load-bearing when <see cref="PhaseName"/> is
    /// empty; otherwise the name is what gets rendered.
    /// </summary>
    public int PhaseValue { get; }

    /// <summary>
    /// The attribute's <c>Coalesce</c> named argument (default <c>false</c>). A primitive for
    /// the same cache-safety reason as the phase — see <see cref="PhaseName"/>.
    /// </summary>
    public bool Coalesce { get; }

    /// <summary>
    /// Always <c>true</c> — the generator only emits registrations for static handlers now.
    /// Instance-method handlers are the removed client-relay pattern and are silently skipped.
    /// </summary>
    public bool IsStatic { get; }
    public bool IsAsync { get; }

    /// <summary>Non-service parameters (event type + CancellationToken), in declaration order.</summary>
    public EquatableArray<ParameterModel> Parameters { get; }

    /// <summary>Parameters marked with <c>[Service]</c>, in declaration order.</summary>
    public EquatableArray<ParameterModel> ServiceParameters { get; }

    /// <summary>
    /// Every parameter in the order it appears in the method signature. Used by the
    /// renderer to emit invocation arguments in declaration order.
    /// </summary>
    public EquatableArray<ParameterModel> AllParameters { get; }
}
