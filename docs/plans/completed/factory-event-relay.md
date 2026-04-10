# Factory Event Relay — Server-to-Client Event Forwarding

**Date:** 2026-04-09
**Related Todo:** [Factory Event Relay](../todos/factory-event-relay.md)
**Status:** Complete
**Last Updated:** 2026-04-09

<!-- Valid status values (do not render in plan):
Draft | Under Review (Architect) | Concerns Raised (Architect) | Ready for Implementation |
In Progress | Awaiting Code Review | Code Review Concerns | Awaiting Verification | Sent Back |
Requirements Documented | Documentation Complete | Complete
-->

---

## Overview

Enable factory events raised on the server during factory operations to be relayed back to the client. Events are captured in a request-scoped buffer during server-side execution, serialized alongside the operation response in `RemoteResponseDto`, and replayed on the client after the factory operation completes. Client-side viewmodels implement `IFactoryEventHandler<T>` and register with `IFactoryEventRelay` to receive events.

This is a lightweight, request-piggybacked alternative to SignalR — events travel on the existing HTTP response with zero additional infrastructure.

---

## Skills

- `skills/RemoteFactory/SKILL.md` — RemoteFactory patterns, factory attributes, service injection, static factory pattern, event handler pattern

---

## Business Rules (Testable Assertions)

### Event Capture (Server-Side)

1. WHEN a factory event is raised on the server via `IFactoryEvents.Raise()` with default options (`RaiseOptions.None`), THEN the event IS captured for client relay. — NEW
2. WHEN a factory event is raised with `RaiseOptions.ServerOnly`, THEN the event is NOT captured for client relay. — NEW
3. WHEN multiple events are raised during a single factory operation, THEN ALL events are captured in the order they were raised. — NEW
4. WHEN events are raised during nested operations (e.g., child Insert during parent Save), THEN those events are also captured (the collector is request-scoped). — NEW
5. WHEN the factory operation fails (throws an exception), THEN no events are relayed to the client (the response doesn't reach the client). — NEW
6. WHEN `RaiseOptions.ServerOnly` is combined with other flags (e.g., `ServerOnly | ContinueOnFail`), THEN the event is still dispatched to server-side handlers with those flags but NOT captured for relay. — NEW
7. WHEN a factory operation is executed in Logical mode (not Remote), THEN no event capture occurs (no relay needed — everything is local). — NEW

### Transport

8. WHEN a factory operation completes successfully on the server with captured events, THEN the `RemoteResponseDto` includes the serialized events alongside the operation result. — NEW
9. WHEN a factory operation completes with zero captured events, THEN `RemoteResponseDto.RelayedEvents` is null (not an empty list). — NEW
10. WHEN events are serialized for relay, THEN each event includes its full type name and JSON payload for deserialization on the client. — NEW

### Client-Side Relay

11. WHEN the client receives a `RemoteResponseDto` with relayed events, THEN the events are dispatched to all registered `[FactoryEventHandler<T>]` handler instances matching each event type. — NEW
12. WHEN events are relayed to the client, THEN the factory operation result is returned to the caller FIRST, and events are dispatched AFTER (fire-and-forget). — NEW
13. WHEN a client-side handler throws during event dispatch, THEN the exception does NOT propagate to the factory operation caller. — NEW
14. WHEN no handlers are registered for a relayed event type, THEN the event is silently dropped (no error). — NEW
15. WHEN multiple handlers are registered for the same event type, THEN all handlers are invoked. — NEW

### Handler Registration

16. WHEN an instance of a `[FactoryEventHandler<T>]` class calls `IFactoryEventRelay.Register(this)`, THEN it receives relayed events of type T until it calls `Unregister(this)`. — NEW
17. WHEN an object calls `IFactoryEventRelay.Unregister(this)`, THEN it stops receiving relayed events. — NEW
18. WHEN a registered handler is garbage collected without calling Unregister, THEN it is silently removed from the registry (no memory leak). — NEW

### Source-Generated Dispatch

19. WHEN a class is decorated with `[FactoryEventHandler<T>]` and has an instance method matching the signature `Task MethodName(T event [, services...] [, CancellationToken ct])`, THEN the source generator registers the handler class type with `FactoryEventRelayRegistry` for client-side relay dispatch. — NEW
20. WHEN the client receives a relayed event, THEN the dispatch uses the source-generated typed delegate (no reflection). — NEW
21. WHEN the source generator discovers a `[FactoryEventHandler<T>]` class attribute, THEN it generates a `FactoryServiceRegistrar` method on the handler class that registers the dispatch + deserializer with `FactoryEventRelayRegistry` (for instance methods) or `FactoryEventHandlerRegistry` (for static methods). — NEW
22. WHEN a class is decorated with `[FactoryEventHandler<T>]` but has no matching method, THEN the generator emits compile-time diagnostic NF0501. — NEW
23. WHEN a class is decorated with `[FactoryEventHandler<T>]` and has multiple methods matching the signature, THEN the generator emits compile-time diagnostic NF0502. — NEW
24. WHEN a `[FactoryEventHandler<T>]` class has a `static` matching method, THEN the generator registers it as a SERVER-SIDE handler in `FactoryEventHandlerRegistry` (running in isolated scope with `[Service]` injection and CancellationToken). — NEW
25. WHEN a `[FactoryEventHandler<T>]` class has an INSTANCE matching method, THEN the generator registers the handler class type as a CLIENT-SIDE relay handler in `FactoryEventRelayRegistry`. — NEW

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Single event relay | Server raises `TestOrderEvent` during Update; client has handler registered | 1, 8, 11, 12 | Client handler receives event after operation result is returned |
| 2 | ServerOnly excludes relay | Server raises event with `RaiseOptions.ServerOnly` | 2, 6 | Server handler fires; client handler does NOT receive event |
| 3 | Multiple events relay in order | Server raises EventA then EventB during Save | 3, 8, 11 | Client receives both events in order |
| 4 | Nested operation events captured | Parent Save triggers child Insert which raises event | 4 | Client receives the child's event |
| 5 | No events = null in response | Server operation raises no events | 9 | `RemoteResponseDto.RelayedEvents` is null |
| 6 | Client handler exception doesn't propagate | Client handler throws; factory caller awaits result | 13 | Factory result returned successfully; handler exception swallowed |
| 7 | No registered handlers = silent drop | Event relayed but no handler registered for that type | 14 | No error; event is dropped |
| 8 | Multiple handlers per event | Two VMs registered for same event type | 15 | Both handlers invoked |
| 9 | Unregister stops delivery | VM registers, then unregisters, then event relayed | 16, 17 | Handler not invoked after unregister |
| 10 | Weak reference cleanup | Handler registered, then garbage collected without unregister | 18 | No memory leak; handler silently removed |
| 11 | ServerOnly combined with ContinueOnFail | Raise with `ServerOnly \| ContinueOnFail` | 6 | Server handlers execute with ContinueOnFail; no relay |
| 12 | Logical mode no capture | Event raised in Logical mode factory operation | 7 | No collector involved; events dispatched normally |
| 13 | Serialization round-trip | Event with complex properties (nested records, lists) relayed | 10, 11 | Client handler receives event with correct data |
| 14 | Source-generated dispatch table | Class implementing `IFactoryEventHandler<T>` compiled | 19, 20, 21 | Generated code registers dispatch delegate without reflection |

---

## Approach

Three-layer implementation following the existing data flow:

1. **Server-side capture**: Inject a request-scoped `IFactoryEventCollector` into `FactoryEventsDispatcher`. When events are raised (unless `ServerOnly`), they're added to the collector alongside normal handler dispatch.

2. **Transport**: Extend `RemoteResponseDto` with an optional `RelayedEvents` list. In `HandleRemoteDelegateRequest`, after the operation completes, attach collected events to the response. Update `NeatooTransportJsonContext` for trimming safety.

3. **Client-side replay**: After `MakeRemoteDelegateRequest` receives the response and deserializes the result, pass relayed events to `IFactoryEventRelay` for dispatch. The relay uses source-generated dispatch delegates to invoke matching `IFactoryEventHandler<T>` handlers on registered instances.

---

## Design

### New Types

```
IFactoryEventHandler<T>       — Interface for client-side event handlers (VMs implement this)
IFactoryEventRelay             — Client-side service: Register/Unregister handlers, dispatch events
FactoryEventRelayDispatcher    — Implementation of IFactoryEventRelay
FactoryEventRelayRegistry      — Static registry of source-generated dispatch delegates (event type → typed delegate)
IFactoryEventCollector         — Server-side scoped service: captures events during a factory operation
FactoryEventCollector          — Implementation of IFactoryEventCollector
RelayedFactoryEvent            — Transport DTO: type name + serialized JSON for a single event
```

### Modified Types

```
RaiseOptions                   — Add ServerOnly = 4
RemoteResponseDto              — Add optional RelayedEvents property
NeatooTransportJsonContext     — Add RelayedFactoryEvent serialization
FactoryEventsDispatcher        — Inject IFactoryEventCollector, capture events on Raise
HandleRemoteDelegateRequest    — Attach collected events to response
MakeRemoteDelegateRequest      — Extract relayed events from response, dispatch to relay
AddRemoteFactoryServices       — Register IFactoryEventRelay (Remote mode), IFactoryEventCollector (Server mode)
```

### Generator Changes

```
StaticFactoryRenderer          — Generate FactoryEventRelayRegistry.RegisterDispatcher<T>() calls for IFactoryEventHandler<T> implementations
ClassFactoryRenderer           — Same (delegate to StaticFactoryRenderer)
FactoryModelBuilder            — Detect IFactoryEventHandler<T> implementations on [Factory] classes
```

### Data Flow

```
SERVER SIDE (during factory operation):
1. Factory method calls IFactoryEvents.Raise(new TreatmentComplete(id))
2. FactoryEventsDispatcher checks RaiseOptions — if NOT ServerOnly:
   a. Dispatch to server-side [FactoryEventHandler] methods (existing behavior)
   b. Add event to IFactoryEventCollector (NEW)
3. If ServerOnly: dispatch to server handlers only, skip collector

SERVER SIDE (after factory operation completes):
4. HandleRemoteDelegateRequest reads IFactoryEventCollector
5. Serializes collected events as List<RelayedFactoryEvent>
6. Creates RemoteResponseDto(json, relayedEvents)

TRANSPORT:
7. RemoteResponseDto serialized with relayed events
8. HTTP response flows to client

CLIENT SIDE:
9. MakeRemoteDelegateRequest deserializes RemoteResponseDto
10. Returns the operation result to the caller (e.g., saved entity)
11. If RelayedEvents is not null, fire-and-forget dispatch to IFactoryEventRelay
12. IFactoryEventRelay.DispatchRelayedEvents():
    a. For each RelayedFactoryEvent, look up FactoryEventRelayRegistry by TypeFullName (string key)
    b. Use registry's deserializer delegate to deserialize event JSON (no Type.GetType() needed)
    c. For each registered handler instance (alive via WeakReference):
       - Call dispatch delegate: (handler, event) => handler.HandleFactoryEvent(event)
       - Delegate internally casts to IFactoryEventHandler<T> (source-generated, typed)
    d. Swallow any handler exceptions (log them)

NOTE: Chained events — if a server-side [FactoryEventHandler] raises additional events
during the factory operation, they ARE captured by the request-scoped collector. However,
events raised via RemoteFactoryEvents.Raise (client-initiated) do NOT relay back because
ForDelegateEvent doesn't extract a response.
```

### [FactoryEventHandler<T>] Class Attribute (Final Implementation)

**NOTE:** The plan originally called for an `IFactoryEventHandler<T>` interface, but during implementation the design was refined to a class-level attribute. This unifies the existing mediator pattern (server-side `[FactoryEventHandler]` method attribute) with the new relay pattern into one generator pipeline. The class-level attribute is cheaper for the generator to discover (via `ForAttributeWithMetadataName`) than scanning for interface implementations.

```csharp
[System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class FactoryEventHandlerAttribute<T> : Attribute { }
```

Usage:
```csharp
[FactoryEventHandler<OrderPlacedEvent>]
public partial class OrderViewModel
{
    // Instance method → client-side relay handler
    public Task Handle(OrderPlacedEvent evt) => Task.CompletedTask;
}

[FactoryEventHandler<OrderPlacedEvent>]
public static partial class OrderNotifyHandler
{
    // Static method → server-side handler (replaces old [FactoryEventHandler] method attribute)
    internal static Task Handle(OrderPlacedEvent evt, [Service] ISvc s, CancellationToken ct) => Task.CompletedTask;
}
```

The generator finds the matching method by signature: first non-`[Service]`/non-`CancellationToken` parameter must be `T`, return type must be `Task`. NF0501 if no match, NF0502 if ambiguous.

### IFactoryEventRelay

```csharp
public interface IFactoryEventRelay
{
    void Register(object handler);
    void Unregister(object handler);
}
```

Internal method (not on public interface):
```csharp
internal Task DispatchRelayedEvents(IReadOnlyList<RelayedFactoryEvent> events, INeatooJsonSerializer serializer);
```

### FactoryEventRelayRegistry (Source-Generated Dispatch Table)

**Key decision (from architect review):** Registry is keyed by `string` (type full name), NOT `Type`. This avoids trimming-unsafe `Type.GetType()` calls. The generated code knows the type name at compile time.

```csharp
public static class FactoryEventRelayRegistry
{
    // Maps event type full name (string) → (dispatch delegate, deserializer)
    // Dispatch delegate: (object handler, object eventObj) => Task
    // Deserializer: (string json, INeatooJsonSerializer serializer) => object
    
    public static void RegisterDispatcher<TEvent>(
        string typeFullName,
        Func<object, object, Task> dispatcher,
        Func<string, INeatooJsonSerializer, object> deserializer)
        where TEvent : FactoryEventBase;
    
    internal static (Func<object, object, Task> dispatch, Func<string, INeatooJsonSerializer, object> deserialize)? 
        GetDispatcher(string typeFullName);
}
```

Generated code per handler class (client-side relay):
```csharp
FactoryEventRelayRegistry.RegisterHandlerType(
    typeof(OrderViewModel),
    typeof(OrderPlacedEvent).FullName!,
    (h, evt) => ((OrderViewModel)h).Handle((OrderPlacedEvent)evt),
    (json, serializer) => serializer.Deserialize<OrderPlacedEvent>(json)!);
```

The registry is keyed by **handler class type** (not event type), so multiple handler classes for the same event type each get their own dispatch delegate. At runtime, `IFactoryEventRelay.Register(handler)` looks up entries by `handler.GetType()`.

### FactoryEventRelayDispatcher

```csharp
internal sealed class FactoryEventRelayDispatcher : IFactoryEventRelay
{
    private readonly List<WeakReference<object>> _handlers = new();
    private readonly Lock _lock = new();
    
    public void Register(object handler) { ... }
    public void Unregister(object handler) { ... }
    
    internal async Task DispatchRelayedEvents(
        IReadOnlyList<RelayedFactoryEvent> events, 
        INeatooJsonSerializer serializer)
    {
        // For each event:
        // 1. Look up dispatcher from FactoryEventRelayRegistry
        // 2. Deserialize event
        // 3. For each alive handler, check if it implements the handler interface
        // 4. Call dispatcher (no reflection — typed cast in generated delegate)
        // 5. Swallow exceptions
    }
}
```

### RelayedFactoryEvent (Transport DTO)

```csharp
public class RelayedFactoryEvent
{
    public string TypeFullName { get; set; } = null!;
    public string Json { get; set; } = null!;
}
```

### IFactoryEventCollector (Server-Side)

```csharp
internal interface IFactoryEventCollector
{
    void Collect(FactoryEventBase factoryEvent);
    IReadOnlyList<FactoryEventBase> GetCollectedEvents();
}

internal sealed class FactoryEventCollector : IFactoryEventCollector
{
    private readonly List<FactoryEventBase> _events = new();
    
    public void Collect(FactoryEventBase factoryEvent) => _events.Add(factoryEvent);
    public IReadOnlyList<FactoryEventBase> GetCollectedEvents() => _events;
}
```

### RemoteResponseDto Changes

```csharp
public class RemoteResponseDto
{
    [JsonConstructor]
    public RemoteResponseDto(string? json, IReadOnlyList<RelayedFactoryEvent>? relayedEvents = null)
    {
        Json = json;
        RelayedEvents = relayedEvents;
    }
    
    public string? Json { get; private set; }
    public IReadOnlyList<RelayedFactoryEvent>? RelayedEvents { get; private set; }
}
```

### DI Registration

**Server mode:**
- `IFactoryEventCollector` → `FactoryEventCollector` (scoped)

**Remote (client) mode:**
- `IFactoryEventRelay` → `FactoryEventRelayDispatcher` (singleton — lives for app lifetime, holds handler registrations)

**Logical mode:**
- Neither collector nor relay registered (no cross-boundary communication)

---

## Domain Model Behavioral Design

N/A — this is library infrastructure, not a domain model with UI-bound properties.

---

## Implementation Steps

### Phase 1: Server-Side Event Capture

1. **Add `ServerOnly` to `RaiseOptions`** — Add `ServerOnly = 4` flag value with XML doc explaining it excludes events from client relay.

2. **Create `IFactoryEventCollector` and `FactoryEventCollector`** — Request-scoped service that buffers events. Internal interfaces only (not user-facing).

3. **Modify `FactoryEventsDispatcher`** — Inject `IFactoryEventCollector` (optional, via `GetService`). In `Raise<T>()` and `RaiseUntyped()`, after dispatching to handlers, if collector is available and `ServerOnly` is NOT set, call `collector.Collect(factoryEvent)`.

4. **Register `IFactoryEventCollector` in DI** — In `AddRemoteFactoryServices`, register as scoped in Server mode only.

### Phase 2: Transport

5. **Create `RelayedFactoryEvent` DTO** — Simple class with `TypeFullName` and `Json` properties.

6. **Extend `RemoteResponseDto`** — Add optional `IReadOnlyList<RelayedFactoryEvent>? RelayedEvents` property. Update constructor. Maintain backward compatibility (null default).

7. **CRITICAL: Update `NeatooTransportJsonContext`** — Add `[JsonSerializable(typeof(RelayedFactoryEvent))]` and `[JsonSerializable(typeof(List<RelayedFactoryEvent>))]` for trimming safety. Without this, the production HTTP path (`MakeRemoteDelegateRequestHttpCall.cs:40`) will silently drop relayed events during deserialization.

8. **Modify `HandleRemoteDelegateRequest`** — After method execution completes, resolve `IFactoryEventCollector` from the service provider. If events were collected, serialize each with `INeatooJsonSerializer` and attach to `RemoteResponseDto`.

### Phase 3: Client-Side Relay

9. **Create `IFactoryEventHandler<T>` interface** — Public interface in `Neatoo.RemoteFactory` namespace.

10. **Create `FactoryEventRelayRegistry`** — Static registry mapping event type → typed dispatch delegate. Same pattern as `FactoryEventHandlerRegistry`.

11. **Create `IFactoryEventRelay` and `FactoryEventRelayDispatcher`** — Singleton service. `Register(object)` / `Unregister(object)` for handler instances. Internal `DispatchRelayedEvents()` method.

12. **Modify `MakeRemoteDelegateRequest`** — After deserializing the response, if `RelayedEvents` is not null, fire-and-forget dispatch to `IFactoryEventRelay`. The operation result is returned to the caller immediately.

13. **Register `IFactoryEventRelay` in DI** — In `AddRemoteFactoryServices`, register as singleton in Remote mode.

### Phase 4: Source Generator

14. **Extend `FactoryModelBuilder`** — Detect `IFactoryEventHandler<T>` interfaces on `[Factory]`-decorated classes. Extract the event type T.

15. **Generate relay dispatch registration** — In a new `RenderFactoryEventRelayRegistration`, generate `FactoryEventRelayRegistry.RegisterDispatcher<T>(...)` calls. Guard with `if (!NeatooRuntime.IsServerRuntime)` (relay is client-only). **Note:** The dispatch delegate is generated once per event type, not per handler class. It only needs to exist if any `[Factory]` class references the event type (e.g., raises it or handles it). Non-`[Factory]` classes implementing `IFactoryEventHandler<T>` rely on the dispatch entry being generated from the event type's usage elsewhere.

### Phase 5: Testing

16. **Unit tests for `FactoryEventCollector`** — Capture, ordering, clear behavior.

17. **Unit tests for `FactoryEventRelayDispatcher`** — Register/Unregister, dispatch to handlers, weak reference cleanup, exception swallowing.

18. **Modify BOTH test standins for relay** — Update `MakeSerializedServerStandinDelegateRequest` in BOTH `src/Tests/RemoteFactory.IntegrationTests/TestContainers/ClientServerContainers.cs` AND `src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs`. Each standin must: (a) extract `RelayedEvents` from deserialized `RemoteResponseDto`, (b) resolve `IFactoryEventRelay` from client service provider, (c) **await** `DispatchRelayedEvents` synchronously (not fire-and-forget) for test determinism.

19. **Integration tests** — Full round-trip: server raises event during factory operation, client handler receives it after operation completes.

20. **Test `ServerOnly` flag** — Verify event dispatched to server handlers but NOT included in response.

21. **Test multiple events and ordering** — Verify all events captured in order.

22. **Design project tests** — Add event relay examples to Design.Domain and tests to Design.Tests. Update `DesignClientServerContainers` standin.

### Phase 6: Design Project Updates

22. **Add relay example to Design.Domain** — Demonstrate `IFactoryEventHandler<T>` on a class, registration, and a factory method raising events.

23. **Update CLAUDE-DESIGN.md** — Document relay pattern, `ServerOnly` flag, handler interface.

---

## Acceptance Criteria

- [ ] `RaiseOptions.ServerOnly` flag prevents events from being relayed to client
- [ ] Events raised during server-side factory operations appear in `RemoteResponseDto.RelayedEvents`
- [ ] Client-side `IFactoryEventHandler<T>` handlers receive relayed events after operation completes
- [ ] Factory operation result is returned to caller before events are dispatched
- [ ] Client handler exceptions don't propagate to factory caller
- [ ] Source generator produces dispatch delegates for `IFactoryEventHandler<T>` implementations
- [ ] Weak references prevent memory leaks from unregistered handlers
- [ ] No events relayed when none are captured (null, not empty list)
- [ ] All existing tests continue to pass (no regression)
- [ ] Integration tests verify full client→server→client round-trip with event relay
- [ ] Design project demonstrates the pattern

---

## Dependencies

- IFactoryEvents mediator pattern (commit `1750f52`) — already implemented on `factoryEvent` branch
- Existing serialization pipeline (`NeatooJsonSerializer`, `RemoteResponseDto`, `NeatooTransportJsonContext`)
- Existing source generator infrastructure (`FactoryModelBuilder`, renderers, `FactoryEventHandlerRegistry`)

---

## Risks / Considerations

1. **RemoteResponseDto backward compatibility** — Adding `RelayedEvents` to the DTO must not break existing clients that don't expect it. Using a nullable property with default null in the constructor handles this.

2. **Serialization format** — Event objects use `NeatooJsonSerializer` (STJ with custom converters), not ordinal format. Events are records inheriting `FactoryEventBase`, which is already serializable. Need to verify complex event types round-trip correctly.

3. **Thread safety** — `FactoryEventRelayDispatcher` is singleton; multiple factory operations completing concurrently may dispatch events simultaneously. The handler list needs thread-safe access (lock or concurrent collection).

4. **WeakReference overhead** — Periodic cleanup of dead weak references is needed. Can be done lazily during dispatch or register/unregister.

5. **Generator detection of `IFactoryEventHandler<T>`** — The generator currently detects `[FactoryEventHandler]` attributes. Detecting interface implementations is a different Roslyn pattern (checking implemented interfaces). Need to ensure this works in the incremental generator pipeline.

6. **IL trimming** — `RelayedFactoryEvent` and its properties must survive trimming. The `NeatooTransportJsonContext` source-generated JSON context handles this.

7. **Event type resolution on client** — `RelayedFactoryEvent.TypeFullName` must resolve to the correct type on the client. Since event types are in shared assemblies (Domain project), this should work. But assembly-qualified names vs full names need care.

---

## Follow-up Items (Non-blocking concerns from Step 5 developer review)

1. **RelayHandlerRenderer.RenderClientSideRelayHandler lacks `if (!NeatooRuntime.IsServerRuntime)` guard** — intentionally removed because it broke test containers (both client and server DI containers share one process, so the guard would prevent registration on both). Trade-off: server assemblies register unused relay dispatch entries in memory (cheap). If a future refactor can restore the guard without breaking tests (e.g., scoped registry), this should be revisited.

2. **Rule 18 (weak reference cleanup) test is weak** — `WeakReferenceCleanup_GarbageCollectedHandlerRemoved` only asserts the factory operation succeeds after GC, not that the dead entry was pruned from `_handlers`. A stronger test would expose handler count via internal/testonly API.

3. **No explicit tests for Rule 4 (nested ops) or Rule 7 (Logical mode no capture)** — both are satisfied by DI construction but unvalidated by integration tests. Add explicit scenarios in a follow-up.

4. **`MakeRemoteDelegateRequest` casts `IFactoryEventRelay as FactoryEventRelayDispatcher`** — minor design smell bypassing interface substitutability. `DispatchRelayedEvents` is internal on the concrete class. Could expose via an internal interface `IFactoryEventRelayInternal`.

5. **Rule 9 (null, not empty list) is only implicitly tested** — `NoEvents_NoRelayedEvents` passes but doesn't directly assert `RemoteResponseDto.RelayedEvents == null`. Add a direct assertion.
