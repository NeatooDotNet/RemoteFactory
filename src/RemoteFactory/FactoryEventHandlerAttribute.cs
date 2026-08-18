namespace Neatoo.RemoteFactory;

/// <summary>
/// Marks a class as a handler for factory events of type <typeparamref name="T"/>.
/// The handler method must be <c>static</c>, return <see cref="Task"/>, and have a first
/// non-[Service]/non-CancellationToken parameter of type <typeparamref name="T"/>.
/// <para>
/// Handlers are registered in <see cref="FactoryEventHandlerRegistry"/> and dispatched
/// on the server in the caller's DI scope, sequentially, awaited. <see cref="Phase"/>
/// controls <i>when</i>: the default <see cref="DispatchPhase.Immediate"/> dispatches at
/// <see cref="IFactoryEvents.Raise{T}"/> time, mid-transaction, observing the caller's
/// staged (unflushed) state; <see cref="DispatchPhase.AfterFlush"/> and
/// <see cref="DispatchPhase.AfterCommit"/> queue the dispatch and drain it later.
/// </para>
/// <para>
/// Instance-method handlers are silently ignored by the generator — they were the
/// former client-side relay pattern, now replaced by <see cref="IFactoryEventRelay"/>.
/// Client-side event consumers implement <see cref="IFactoryEventRelay"/> to bridge
/// relayed events to their own event aggregator.
/// </para>
/// <para>
/// <b>Trimming:</b> every generated handler registration is wrapped in
/// <c>NeatooRuntime.IsServerRuntime</c>, so on a client published with the feature switch
/// set to <c>false</c> the handler bodies and their <c>[Service]</c> dependencies are
/// removed from the output. This works because the generator points the assembly's
/// <see cref="NeatooFactoryRegistrarAttribute"/> at a generated forwarding holder rather
/// than at the handler class itself — the attribute preserves every method on whatever it
/// names, bodies included, so naming the handler class would ship the handler bodies to the
/// browser. Fixed in v1.7.0; before that, it did.
/// </para>
/// <para>
/// One consequence for consumers: because the registrations are server-guarded, there is
/// nothing on a trimmed client to resolve, and handler registration cannot be verified from
/// a client-side test. Coverage for it lives in server-side/untrimmed tests.
/// </para>
/// <para>
/// This attribute lives outside <c>FactoryAttributes.cs</c> deliberately: that file is
/// linked into the netstandard2.0 Generator project, and this type references
/// <see cref="DispatchPhase"/>. Compiling the enum into the generator assembly would make
/// it a duplicate of the runtime's own copy in every project referencing both. The
/// generator matches this attribute by metadata-name string, so it never needs the type.
/// </para>
/// </summary>
/// <typeparam name="T">The event type (must inherit from <see cref="FactoryEventBase"/>).</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
#pragma warning disable CA1813 // Sealed generic attribute must be unsealed for generator discovery
public sealed class FactoryEventHandlerAttribute<T> : Attribute
{
	/// <summary>Registers the handler at <see cref="DispatchPhase.Immediate"/>.</summary>
	public FactoryEventHandlerAttribute() : this(DispatchPhase.Immediate) { }

	/// <summary>Registers the handler at <paramref name="phase"/>.</summary>
	public FactoryEventHandlerAttribute(DispatchPhase phase)
	{
		this.Phase = phase;
	}

	/// <summary>When the handler runs relative to the factory operation that raised the event.</summary>
	public DispatchPhase Phase { get; }

	/// <summary>
	/// Opt-in: collapse identical queued dispatches for this handler so a drain runs it
	/// once where it would otherwise run once per raise.
	/// </summary>
	/// <remarks>
	/// <para>
	/// "Identical" means the queued events compare equal per <see cref="object.Equals(object)"/>
	/// — for records, the synthesized structural equality — with the same
	/// <see cref="RaiseOptions"/>. Two hazards follow from that definition. An event with a
	/// reference-typed member (a <c>List&lt;T&gt;</c>, an entity) never compares equal across
	/// raises, so coalescing is a silent no-op for it — prefer value-only payloads on events
	/// whose handlers coalesce. And a custom <c>Equals</c> override that compares semantically
	/// distinct raises equal collapses dispatches the consumer may have expected; the override
	/// owns that outcome. Identity is evaluated when the dispatch becomes pending.
	/// </para>
	/// <para>
	/// The flag affects only dispatches that are actually queued. It has no effect on
	/// <see cref="DispatchPhase.Immediate"/> handlers (never queued — declaring the flag there
	/// is diagnosed NF0505), and no effect when a phased raise falls through to immediate
	/// dispatch because the scope has no scheduler or no entry factory call is active: those
	/// dispatches run once per raise regardless. The client relay is also unaffected — every
	/// <see cref="IFactoryEvents.Raise{T}"/> is still collected and relayed; coalescing is
	/// about handler dispatch, not event delivery.
	/// </para>
	/// <para>
	/// A collapse never erases the fail-open warning: if any collapsed raise would have
	/// produced the 9007 never-drained warning at the post-completion sweep, the surviving
	/// dispatch produces it.
	/// </para>
	/// </remarks>
	public bool Coalesce { get; set; }
}
#pragma warning restore CA1813
