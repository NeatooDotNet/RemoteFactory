using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Neatoo.RemoteFactory;

/// <summary>
/// Static registry for <c>[FactoryEventHandler&lt;T&gt;]</c> handler factories.
/// Each assembly's generated <c>FactoryServiceRegistrar</c> adds its handlers here during DI setup.
/// The <see cref="FactoryEventsDispatcher"/> reads from this registry to dispatch events.
/// </summary>
public static class FactoryEventHandlerRegistry
{
    private static readonly ConcurrentDictionary<Type, List<HandlerEntry>> _handlers = new();

    private readonly struct HandlerEntry
    {
        public HandlerEntry(Type handlerClassType, DispatchPhase phase, bool coalesce, Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> invoke)
        {
            HandlerClassType = handlerClassType;
            Phase = phase;
            Coalesce = coalesce;
            Invoke = invoke;
        }

        public Type HandlerClassType { get; }
        public DispatchPhase Phase { get; }
        public bool Coalesce { get; }
        public Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> Invoke { get; }
    }

    /// <summary>
    /// Registers a handler factory for the given event type at <see cref="DispatchPhase.Immediate"/>.
    /// </summary>
    public static void RegisterHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        Type handlerClassType,
        Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handlerFactory)
        where TEvent : FactoryEventBase
        => RegisterHandler<TEvent>(handlerClassType, DispatchPhase.Immediate, coalesce: false, handlerFactory);

    /// <summary>
    /// Registers a handler factory for the given event type at <paramref name="phase"/>.
    /// Called by generated <c>FactoryServiceRegistrar</c> methods during DI setup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The handler factory is invoked with the caller's <see cref="IServiceProvider"/> — handlers
    /// resolve their <c>[Service]</c> dependencies from the caller's scope. The
    /// <see cref="CancellationToken"/> parameter is threaded from
    /// <see cref="IFactoryEvents.Raise{T}"/> to any handler parameter of that type.
    /// </para>
    /// <para>
    /// Registrations are deduplicated by the <c>(event type, handler class type)</c> pair
    /// so multiple DI container builds in a test run do not multiply registrations. One
    /// consequence: a handler class declaring the same event type twice keeps the
    /// registration made first — its phase and its coalesce flag — for the life of the
    /// process.
    /// </para>
    /// </remarks>
    public static void RegisterHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        Type handlerClassType,
        DispatchPhase phase,
        Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handlerFactory)
        where TEvent : FactoryEventBase
        => RegisterHandler<TEvent>(handlerClassType, phase, coalesce: false, handlerFactory);

    /// <summary>
    /// Registers a handler factory for the given event type at <paramref name="phase"/>,
    /// optionally coalescing identical queued dispatches (see
    /// <see cref="FactoryEventHandlerAttribute{T}.Coalesce"/> for the identity contract).
    /// Called by generated <c>FactoryServiceRegistrar</c> methods during DI setup.
    /// </summary>
    /// <remarks>
    /// A new overload rather than an optional parameter on the existing one: an optional
    /// parameter is source-compatible but binary-breaking for assemblies compiled against
    /// the previous package. The dedupe remark on the three-argument overload applies here
    /// too — the first registration for an <c>(event type, handler class)</c> pair wins,
    /// including its phase AND its coalesce flag.
    /// </remarks>
    public static void RegisterHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
        Type handlerClassType,
        DispatchPhase phase,
        bool coalesce,
        Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handlerFactory)
        where TEvent : FactoryEventBase
    {
        var list = _handlers.GetOrAdd(typeof(TEvent), _ => new List<HandlerEntry>());
        lock (list)
        {
            // Avoid duplicate registration from multiple DI container setups in tests.
            if (!list.Any(e => e.HandlerClassType == handlerClassType))
            {
                list.Add(new HandlerEntry(handlerClassType, phase, coalesce, handlerFactory));
            }
        }
    }

    /// <summary>
    /// Gets all registered handler factories for the given event type, paired with the
    /// phase and coalesce flag each was registered with.
    /// </summary>
    internal static IReadOnlyList<(DispatchPhase Phase, bool Coalesce, Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> Invoke)>? GetHandlers(Type eventType)
    {
        if (!_handlers.TryGetValue(eventType, out var handlers))
            return null;
        lock (handlers)
        {
            return handlers.Select(h => (h.Phase, h.Coalesce, h.Invoke)).ToArray();
        }
    }

    /// <summary>
    /// Clears all registrations.
    /// </summary>
    /// <remarks>
    /// <b>Not a test-isolation escape hatch — do not call it from a test.</b> This registry is
    /// process-wide static and xUnit runs test classes in parallel, so clearing it strips
    /// registrations out from under whatever else is mid-run. Measured, not theorised
    /// (PHASE-008): a single test calling this turned
    /// <c>FactoryEntryCallTests.DrainedHandlerInvokingAFactory_NestsWithoutDrainingOrClearingTheDrainInProgress</c>
    /// red, a test that passes on its own.
    /// <para>
    /// The isolation the suite actually relies on needs no teardown: entries are keyed by
    /// <c>(event type, handler class type)</c>, so a test that declares its own event type
    /// cannot collide with another's. Do that instead. The method stays for a single-threaded
    /// host that genuinely needs to reset the process, and is uncalled in this repo.
    /// </para>
    /// </remarks>
    internal static void Clear()
    {
        _handlers.Clear();
    }
}
