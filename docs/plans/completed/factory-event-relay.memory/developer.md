# Developer — Factory Event Relay

Last updated: 2026-04-09
Current step: Step 5 Developer Code Review

## Key Context

- Plan was implemented with a significant design deviation: instead of an `IFactoryEventHandler<T>` interface that VMs implement, the implementation uses a class-level `[FactoryEventHandler<T>]` attribute (AllowMultiple). Static methods become server-side handlers (replacing the old `[FactoryEventHandler]` method attribute); instance methods become client-side relay handlers. Registry is keyed by handler class type, not event type.
- Business rules 11, 16, 19–21 in the plan reference `IFactoryEventHandler<T>`. The actual implementation satisfies the same INTENT via the class attribute. The assertion text is stale but the behavior is equivalent.
- Server flow: `FactoryEventCollector` (scoped, server-only) -> `FactoryEventsDispatcher` collects on Raise unless `ServerOnly` -> `HandleRemoteDelegateRequest` attaches `List<RelayedFactoryEvent>` to `RemoteResponseDto`.
- Client flow: `MakeRemoteDelegateRequest` deserializes response, returns result to caller, then fire-and-forget awaits `FactoryEventRelayDispatcher.DispatchRelayedEvents` which uses generated `RegisterHandlerType` entries to deserialize + dispatch.
- Test standins (both `ClientServerContainers` and `DesignClientServerContainers`) dispatch synchronously for test determinism.

## Developer Review

**Status:** Concerns Raised
**Date:** 2026-04-09

### Summary

The implementation covers all 21 business rules at the behavioral level, test results are clean (506 + 538 + 47 per TFM), and the design deviation from interface to class attribute is an improvement (unifies the old method-level `[FactoryEventHandler]` with the new relay pattern). However, there are several small concerns: a generator code-gen quirk (client-side registration not guarded by `!IsServerRuntime`), one unit test that asserts weak-reference cleanup only indirectly, and stale plan assertion text that still mentions `IFactoryEventHandler<T>`.

### Assertion Trace Verification

| # | Business Rule | Implementation Path | Verified? |
|---|---|---|---|
| 1 | Raise(default) captured for relay | `FactoryEventsDispatcher.DispatchToHandlers` (FactoryEventsDispatcher.cs:35-38): `if (_collector != null && !options.HasFlag(ServerOnly)) _collector.Collect(...)` | Yes |
| 2 | Raise(ServerOnly) NOT captured | Same check above — `!HasFlag(ServerOnly)` short-circuits collection | Yes |
| 3 | Multiple events captured in order | `FactoryEventCollector.Collect` appends to List; `GetCollectedEvents` returns the list in order (FactoryEventCollector.cs:17-18). Tested in FactoryEventCollectorTests.Collect_MultipleEvents_PreservesOrder | Yes |
| 4 | Nested operation events captured | Collector is Scoped (AddRemoteFactoryServices.cs:122). `FactoryEventsDispatcher` resolves it from constructor SP. Any nested dispatcher in the same request scope gets the SAME collector instance. | Yes (by construction) — **no explicit test** |
| 5 | Operation exception = no events relayed | HandleRemoteDelegateRequest.cs:181-186: exception thrown before `RemoteResponseDto` is constructed. The response never reaches the client. | Yes |
| 6 | ServerOnly \| ContinueOnFail still runs handlers but not relay | `ServerOnly` only affects the collector block at FactoryEventsDispatcher.cs:35; server-side handler dispatch (lines 40-53) proceeds unaffected. Tested by FactoryEventRelayTests.ServerOnlyCombinedWithContinueOnFail_NotRelayed. | Yes |
| 7 | Logical mode no capture | Collector only registered in Server mode (AddRemoteFactoryServices.cs:119-122). In Logical mode, `sp.GetService<IFactoryEventCollector>()` returns null, so the `_collector != null` guard at FactoryEventsDispatcher.cs:35 short-circuits. | Yes (by DI construction) — **no explicit test** |
| 8 | Captured events in RemoteResponseDto | HandleRemoteDelegateRequest.cs:141-161: collector read after method execution, serialized per-event, attached to RemoteResponseDto constructor. | Yes |
| 9 | Zero events = RelayedEvents null (not empty list) | HandleRemoteDelegateRequest.cs:142 initializes to null; only populated when `collected.Count > 0` (line 147). Passed as null into ctor. | Yes — **implicitly tested** by NoEvents_NoRelayedEvents |
| 10 | Event serialized as TypeFullName + Json | HandleRemoteDelegateRequest.cs:152-156: `TypeFullName = evt.GetType().FullName!`, `Json = serializer.Serialize(evt, evt.GetType())`. | Yes |
| 11 | Relayed events dispatched to registered handlers matching event type | FactoryEventRelayDispatcher.cs:48-91 iterates events, looks up deserializer, snapshots handlers whose `eventTypeName` matches, invokes dispatch. Tested by SingleEventRelay_ClientHandlerReceivesEvent and MultipleEventsRelay_AllEventsReceivedInOrder. | Yes (via class attribute, not interface) |
| 12 | Result returned FIRST, events dispatched AFTER (fire-and-forget) | MakeRemoteDelegateRequest.cs:105-113: `DeserializeRemoteResponse<T>` produces `deserialized`, then `_ = _relay.DispatchRelayedEvents(...)` (discarded Task), then `return deserialized`. | Yes — but see Concern 4 |
| 13 | Client handler exception does not propagate | FactoryEventRelayDispatcher.cs:82-89: try/catch around `dispatch(...)`. Tested by HandlerException_DoesNotPropagateToFactoryCaller. | Yes |
| 14 | No handlers = silent drop | Deserializer lookup at FactoryEventRelayDispatcher.cs:50-52: `continue` if null. Tested by NoRegisteredHandlers_EventSilentlyDropped and DispatchRelayedEvents_NoDispatcherRegistered_SilentDrop. | Yes |
| 15 | Multiple handlers = all invoked | Lines 71-74: filter entries by `eventTypeName`, dispatch to each. Tested by MultipleHandlersSameEvent_AllHandlersInvoked and DispatchRelayedEvents_MultipleHandlers_AllInvoked. | Yes |
| 16 | Register/Unregister lifecycle | FactoryEventRelayDispatcher.cs:16-37. Register looks up entries from `FactoryEventRelayRegistry.GetHandlerEntries(handler.GetType())` and adds weak references. Tested by UnregisterStopsDelivery. | Yes (via class attribute) |
| 17 | Unregister stops delivery | Lines 31-37: `RemoveAll` with `ReferenceEquals(target, handler)` predicate. Tested by UnregisterStopsDelivery. | Yes |
| 18 | GC'd handler silently removed | WeakReferences used at line 13 and 26. Opportunistic dead-reference cleanup in DispatchRelayedEvents (lines 67-68). Integration test WeakReferenceCleanup_GarbageCollectedHandlerRemoved forces GC and calls factory, but only asserts result is not null — does NOT directly verify that the dead reference was pruned. | Partially — **weak test** |
| 19 | [FactoryEventHandler<T>] class generates relay entry | FactoryGenerator.cs:84-109 registers `ForAttributeWithMetadataName("Neatoo.RemoteFactory.FactoryEventHandlerAttribute\`1")`. TransformRelayHandler (FactoryGenerator.RelayHandler.cs:19-213) extracts event type, finds matching method, builds RelayHandlerModel. RelayHandlerRenderer emits RegisterHandlerType call. | Yes (via class attribute, plan text about interface is stale) |
| 20 | Dispatch uses typed delegate (no reflection) | Generated code at RelayHandlerRenderer.cs:128-133: `(h, evt) => ((ClassName)h).Method((EventType)evt)` — cast, no reflection. Deserializer is `serializer.Deserialize<EventType>(json)` — typed generic, no runtime Type.GetType. Registry keyed by string typeFullName (FactoryEventRelayRegistry.cs:13). | Yes |
| 21 | Generator writes FactoryServiceRegistrar | RelayHandlerRenderer.cs:45: emits `internal static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)` plus assembly-level `[NeatooFactoryRegistrar(typeof(Class))]` at line 31. | Yes |

### Concerns

**Concern 1: Client-side relay registration runs in Server mode too (generator correctness)**

RelayHandlerRenderer.cs:124-133 emits `FactoryEventRelayRegistry.RegisterHandlerType(...)` UNCONDITIONALLY — no `if (!NeatooRuntime.IsServerRuntime)` guard. The server-side branch at line 87 does have the guard. Plan Step 15 says: "Guard with `if (!NeatooRuntime.IsServerRuntime)` (relay is client-only)."

**Impact:** In Server mode, the `FactoryServiceRegistrar` still pushes entries into the static `FactoryEventRelayRegistry`. The registry is static singleton global state, so in a process that hosts both server and client (tests), entries persist — but the guard against duplicate registration at FactoryEventRelayRegistry.cs:29 protects against double-insertion. Functionally benign today because `IFactoryEventRelay` is only registered in Remote mode, so server code paths never touch the registry. But it is a plan deviation that produces slightly wasteful registration and makes the generated code symmetry less clean.

**Concern 2: Rule 18 (weak reference cleanup) is not directly verified**

`WeakReferenceCleanup_GarbageCollectedHandlerRemoved` only asserts `result != null` after GC. It does not assert the internal list was pruned or that a dead entry is not re-invoked (the handler has no way to observe it because it is GC'd). A stronger test would expose `_handlers.Count` via an internal method, or use a sentinel handler that tracks finalization. The current test confirms "no exception after GC" but not "pruning happened." Weak coverage for Rule 18.

**Concern 3: No explicit test for Rule 4 (nested operation events captured) or Rule 7 (Logical mode no capture)**

Both rules are satisfied by DI construction, but there is no integration test that exercises a nested factory call raising an event from within another factory operation, and no test that raises an event in Logical mode and verifies the factory result is still returned without relay infrastructure. These would be small additions but would harden the assertion trace.

**Concern 4: `_relay` field is cast from interface to concrete type**

MakeRemoteDelegateRequest.cs:65: `_relay = relay as FactoryEventRelayDispatcher`. This throws away interface substitutability — a user who registers a different `IFactoryEventRelay` implementation would silently get no dispatch. The reason is that `DispatchRelayedEvents` is `internal` on the concrete class, not on the interface. This is a minor design smell — either expose the method on the interface or use a sealed internal contract. Low priority but worth noting.

**Concern 5: Plan assertion text is stale vs implementation**

Rules 11, 16, 19–21 in the plan reference `IFactoryEventHandler<T>` which no longer exists. The implementation satisfies the INTENT (class attribute with handler method), but a reader comparing plan to code will see mismatched terminology. This is a documentation issue, not a bug. The orchestrator should update the plan's rule text to reference `[FactoryEventHandler<T>]` before closing the todo, so future readers don't chase a phantom interface.

**Concern 6: RelayedEvents list null semantics (Rule 9) is tested only implicitly**

`NoEvents_NoRelayedEvents` asserts `handler.ReceivedEvents` is empty, which is a consequence of null OR empty list. It does not directly verify that `RemoteResponseDto.RelayedEvents` is null (not `new List<RelayedFactoryEvent>()`). The production code clearly initializes to null and only populates when count > 0, so the invariant holds — but a unit test on `HandleRemoteDelegateRequest` would make Rule 9 explicit.

### Non-Concerns Considered

- **Transient vs Scoped HandleRemoteDelegateRequest (core lib) vs AspNetCore:** AspNetCore overrides with AddScoped and the request middleware resolves from `httpContext.RequestServices`. In both cases, the closure captures a scoped `sp` so `IFactoryEventCollector` resolves correctly. No bug here.
- **FactoryEventRelayRegistry static global state across tests:** `Clear()` is exposed for tests; duplicate-registration guard protects production.
- **Fire-and-forget scope disposal race:** `MakeRemoteDelegateRequest` is scoped; the serializer captured in `DispatchRelayedEvents` is also scoped. If the scope is disposed while dispatch is still in flight, deserialization could throw — but exceptions are swallowed. Acceptable given fire-and-forget semantics and test standin uses `await` for determinism.
- **Unregister predicate:** The `||` in `_handlers.RemoveAll(entry => !entry.handler.TryGetTarget(out var target) || ReferenceEquals(target, handler))` is correct — removes dead OR target, keeps alive non-target. Verified by walking truth table.
- **Chained events (server handler raises another event):** Plan note at Design line 166-169 says these ARE captured by the request-scoped collector. Verified by Rule 4 trace — same collector instance across the whole request scope.
- **Old non-generic [FactoryEventHandler] attribute:** Verified removed; only generic version remains (FactoryAttributes.cs:129). Plan said method-level attribute was removed; confirmed.

### Verdict

**Concerns Raised** — implementation is behaviorally correct and all business rules are satisfied, but the six concerns above should be reviewed by the orchestrator before proceeding to Step 6 (architect verification). None are blocking; Concerns 1, 2, 3 are test/generator gaps that could be added as follow-up work, and Concerns 5, 6 are documentation polish.
