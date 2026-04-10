# IFactoryEvents Mediator Pattern Design

**Date:** 2026-04-09
**Related Todo:** [IFactoryEvents Mediator Pattern](../todos/factory-events-mediator.md)
**Status:** Documentation Complete
**Last Updated:** 2026-04-09

**Last Updated:** 2026-04-09

<!-- Valid status values (do not render in plan):
Draft | Under Review (Architect) | Concerns Raised (Architect) | Ready for Implementation |
In Progress | Awaiting Code Review | Code Review Concerns | Awaiting Verification | Sent Back |
Requirements Documented | Documentation Complete | Complete
-->

---

## Overview

Add a source-generated mediator pattern for events in RemoteFactory. Publishers inject `IFactoryEvents` and call `Raise<T>(new SomeEvent(...))` to dispatch to all registered `[FactoryEventHandler]` methods matching the event type. The generator discovers handlers at compile time and produces a dispatch table — no reflection at runtime.

This coexists with the current `[Event]` delegate pattern. No breaking changes.

---

## Skills

- `skills/RemoteFactory/SKILL.md` — RemoteFactory patterns, attributes, generator behavior
- `skills/knockoff/SKILL.md` — If stubbing is needed in tests for IFactoryEvents

---

## Business Rules (Testable Assertions)

### Event Objects

1. WHEN a class/record inherits from `FactoryEventBase`, THEN it is a valid event type for `Raise<T>` — NEW
2. WHEN an event object is raised with `[Remote]` handlers, THEN it must be serializable via standard System.Text.Json (STJ) serialization (ordinal serialization deferred to v2) — NEW

### Handler Discovery

3. WHEN a static method has `[FactoryEventHandler]` attribute in a `[Factory]` static class, THEN the generator registers it as a handler — NEW
4. WHEN a `[FactoryEventHandler]` method's first non-`[Service]` parameter type is `TEvent : FactoryEventBase`, THEN it handles events of type `TEvent` — NEW
5. WHEN multiple `[FactoryEventHandler]` methods across multiple `[Factory]` classes match the same event type, THEN all are invoked on `Raise<TEvent>` — NEW
6. WHEN a `[FactoryEventHandler]` method is in a different assembly than the event type, THEN it is still discovered via the assembly's generated registrar — NEW
7. WHEN a `[FactoryEventHandler]` method lacks `CancellationToken` as final parameter, THEN a compile-time diagnostic is emitted (same as current `[Event]`) — NEW

### Publishing

8. WHEN `IFactoryEvents.Raise<T>(event)` is called, THEN all registered handlers for type `T` execute in parallel — NEW
9. WHEN `IFactoryEvents.Raise<T>(event)` is called with no registered handlers, THEN it completes as a no-op (no exception) — NEW
10. WHEN `Raise<T>(event)` is called with `RaiseOptions.None` (default), THEN remote handlers are fire-and-forget (await server acknowledgment only, not handler completion) — NEW
11. ~~DEFERRED TO v2~~ WHEN `Raise<T>(event)` is called with `RaiseOptions.AwaitRemote`, THEN the caller awaits full handler completion including remote handlers (HTTP connection stays open) — NEW — See [AwaitRemote Todo](../todos/factory-events-await-remote.md)
12. WHEN a `[FactoryEventHandler]` method is not `[Remote]`, THEN it executes locally regardless of `RaiseOptions` — NEW

### Error Handling

13. WHEN a handler throws and `RaiseOptions.ContinueOnFail` is NOT set (default), THEN the exception propagates to the caller and remaining handlers may not complete — NEW
14. WHEN a handler throws and `RaiseOptions.ContinueOnFail` IS set, THEN remaining handlers continue executing and exceptions are aggregated — NEW
15. WHEN multiple handlers fail with `ContinueOnFail`, THEN an `AggregateException` is thrown containing all handler exceptions — NEW

### Scope Isolation

16. WHEN a handler executes, THEN it runs in its own DI scope (same as current `[Event]` behavior) — Source: existing `[Event]` pattern
17. WHEN a handler executes, THEN the correlation ID from the publisher's scope is propagated to the handler's scope — Source: existing `[Event]` pattern

### Dispatch Mechanics

18. WHEN `Raise<TDerived>` is called where `TDerived : TBase` and both are `FactoryEventBase`, THEN only handlers for `TDerived` execute (no polymorphic dispatch) — NEW
19. WHEN the generated dispatcher resolves handlers, THEN it uses a `Dictionary<Type, Func<...>>` populated at construction time — no reflection — NEW

### Handler Validation

22. WHEN a `[FactoryEventHandler]` method returns `Task<T>` (not `Task` or `void`), THEN a compile-time diagnostic is emitted — NEW
23. ~~DEFERRED~~ WHEN `[FactoryEventHandler]` is placed on a method outside a `[Factory]` class, THEN a compile-time diagnostic is emitted — NEW — Generator only discovers methods in `[Factory]` classes; silently ignored otherwise
24. WHEN a `[FactoryEventHandler]` method is not `private static`, THEN a compile-time diagnostic is emitted (must be `private static`) — NEW

### Event Tracking

25. WHEN a handler executes in fire-and-forget mode, THEN its Task is registered with `IEventTracker.Track()` for graceful shutdown — Source: existing `[Event]` pattern

### Cross-Assembly Registration

26. WHEN an assembly contains `[FactoryEventHandler]` methods, THEN the generator extends the existing `FactoryServiceRegistrar` to register handler metadata into a shared static registry — NEW

### Coexistence

20. WHEN `[Event]` and `[FactoryEventHandler]` both exist in the same compilation, THEN both patterns work independently — NEW
21. WHEN a method has `[Event]` (not `[FactoryEventHandler]`), THEN it continues to generate a delegate type with current fire-and-forget semantics — Source: existing `[Event]` pattern

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Single handler, default options | `Raise(new OrderPlaced(1))`, one handler registered | 4, 8, 10, 16 | Handler executes in isolated scope, caller gets Task that completes after server ack |
| 2 | Multiple handlers, same event | `Raise(new OrderPlaced(1))`, three handlers registered | 5, 8 | All three execute in parallel |
| 3 | No handlers registered | `Raise(new UnhandledEvent())` | 9 | Task completes, no exception |
| 4 | Cross-assembly handler | Handler in Assembly A, event in Assembly B, Raise in Assembly C | 6 | Handler discovered and invoked |
| 5 | Handler throws, default options | Handler throws `InvalidOperationException` | 13 | Exception propagates to caller |
| 6 | Handler throws, ContinueOnFail | Two handlers, first throws | 14, 15 | Second handler runs, AggregateException with first handler's error |
| 7 | Remote handler, default (fire-and-forget) | `[Remote, FactoryEventHandler]` method, `Raise(event)` | 10, 16 | Caller awaits HTTP ack only, handler runs on server |
| 8 | ~~DEFERRED~~ Remote handler, AwaitRemote | `[Remote, FactoryEventHandler]` method, `Raise(event, AwaitRemote)` | 11 | Deferred to v2 |
| 9 | Mixed local + remote handlers | One local, one `[Remote]` handler | 8, 10, 12 | Local runs locally, remote fires-and-forget, both in parallel |
| 10 | Derived event type | `Raise<DerivedEvent>(...)`, handlers for `DerivedEvent` and `BaseEvent` | 18 | Only `DerivedEvent` handlers run |
| 11 | Missing CancellationToken | `[FactoryEventHandler]` method without `CancellationToken` | 7 | Compile-time diagnostic |
| 12 | Coexistence | `[Event]` delegate and `[FactoryEventHandler]` method in same compilation | 20, 21 | Both patterns work independently |
| 13 | Event serialization round-trip | Event object raised from client, handled on server | 2 | Event object survives serialization/deserialization |
| 14 | Correlation ID propagation | Publisher has correlation ID, handler in new scope | 17 | Handler scope has same correlation ID |
| 15 | Handler returns Task<T> | `[FactoryEventHandler]` method returns `Task<string>` | 22 | Compile-time diagnostic |
| 16 | ~~DEFERRED~~ Attribute outside [Factory] | `[FactoryEventHandler]` on method in non-`[Factory]` class | 23 | Deferred — generator silently ignores |
| 17 | Handler not private static | `[FactoryEventHandler]` on `public static` method | 24 | Compile-time diagnostic |
| 18 | Fire-and-forget tracking | Handler runs fire-and-forget | 25 | Task registered with `IEventTracker` |
| 19 | Multiple event types in sequence | `Raise(EventA)` then `Raise(EventB)` | 8, 18 | Each dispatches only to its own handlers |

---

## Approach

### API Surface

```csharp
// Base class for event objects — v1 uses STJ serialization, ordinal deferred to v2
public abstract class FactoryEventBase { }

// Options for Raise behavior
[Flags]
public enum RaiseOptions
{
    None = 0,
    AwaitRemote = 1,
    ContinueOnFail = 2
}

// Publisher interface — injected via DI
public interface IFactoryEvents
{
    Task Raise<T>(T @event, RaiseOptions options = RaiseOptions.None) where T : FactoryEventBase;
}
```

### Handler Declaration

```csharp
// Event object
public record OrderPlacedEvent(int OrderId, string CustomerEmail) : FactoryEventBase;

// Handlers — [FactoryEventHandler] on static methods in [Factory] classes
[Factory]
public static partial class OrderNotifications
{
    [FactoryEventHandler]
    private static async Task _SendConfirmation(
        OrderPlacedEvent @event,
        [Service] IEmailService email,
        CancellationToken ct)
    {
        await email.SendAsync(@event.CustomerEmail, "Order confirmed", ct);
    }
}

[Factory]
public static partial class WarehouseHandlers
{
    [Remote, EventHandler]
    private static async Task _ReserveInventory(
        OrderPlacedEvent @event,
        [Service] IWarehouseService warehouse,
        CancellationToken ct)
    {
        await warehouse.ReserveAsync(@event.OrderId, ct);
    }
}
```

### Publishing

```csharp
[Factory]
internal partial class Order
{
    [Remote, Create]
    internal async Task _Create(
        string customer, string email,
        [Service] IFactoryEvents events)
    {
        OrderId = GenerateId();

        // Fire-and-forget (default)
        _ = events.Raise(new OrderPlacedEvent(OrderId, email));

        // Await all handlers including remote
        await events.Raise(new OrderPlacedEvent(OrderId, email), RaiseOptions.AwaitRemote);

        // Continue on failure + await remote
        await events.Raise(new OrderPlacedEvent(OrderId, email),
            RaiseOptions.AwaitRemote | RaiseOptions.ContinueOnFail);
    }
}
```

---

## Domain Model Behavioral Design

N/A — This is a library/generator feature, not a domain model change.

---

## Design

### Generator Pipeline

The generator needs a new **gather phase** to collect `[FactoryEventHandler]` methods across all `[Factory]` classes:

```
Phase 1: For each [Factory] class, extract [FactoryEventHandler] methods
   -> IncrementalValuesProvider<EventHandlerInfo>

Phase 2: Collect() all handlers, group by event type
   -> IncrementalValueProvider<ImmutableArray<EventHandlerGroup>>

Phase 3: Generate FactoryEventsDispatcher with compiled dispatch table
```

### Generated Dispatcher

```csharp
// Generated — one per compilation
internal class FactoryEventsDispatcher : IFactoryEvents
{
    private readonly Dictionary<Type, Func<object, RaiseOptions, IServiceProvider, Task>> _dispatchers;

    public FactoryEventsDispatcher(IServiceProvider sp)
    {
        _dispatchers = new()
        {
            [typeof(OrderPlacedEvent)] = (e, opts, sp) =>
                HandleOrderPlacedEvent((OrderPlacedEvent)e, opts, sp),
        };
    }

    public Task Raise<T>(T @event, RaiseOptions options = RaiseOptions.None) where T : FactoryEventBase
    {
        if (_dispatchers.TryGetValue(typeof(T), out var handler))
            return handler(@event!, options, _sp);
        return Task.CompletedTask; // No handlers — silent no-op
    }

    private Task HandleOrderPlacedEvent(OrderPlacedEvent @event, RaiseOptions options, IServiceProvider sp)
    {
        var tasks = new List<Task>();

        // Local handler — always runs locally
        tasks.Add(InvokeInScope(sp, scope =>
        {
            var email = scope.GetRequiredService<IEmailService>();
            var ct = scope.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
            return OrderNotifications.SendConfirmation(@event, email, ct);
        }));

        // Remote handler — behavior depends on options
        if (options.HasFlag(RaiseOptions.AwaitRemote))
            tasks.Add(InvokeRemoteAwaited(sp, typeof(WarehouseHandlers.ReserveInventoryEvent), @event));
        else
            tasks.Add(InvokeRemoteFireAndForget(sp, typeof(WarehouseHandlers.ReserveInventoryEvent), @event));

        if (options.HasFlag(RaiseOptions.ContinueOnFail))
            return WhenAllContinueOnFail(tasks);
        else
            return Task.WhenAll(tasks); // First failure propagates
    }
}
```

### Cross-Assembly Registration

Extends the existing `FactoryServiceRegistrar` pattern — no parallel registrar mechanism. Each assembly's generated registrar contributes event handler metadata to a shared static registry during DI setup. `AddNeatooRemoteFactory()` collects all handler registrations and builds the `IFactoryEvents` dispatcher from the complete registry.

```csharp
// Generated — extends existing FactoryServiceRegistrar in each assembly
// Handler metadata is added during the same DI setup call that registers factories
```

### Remote Handler HTTP Semantics

Reuses existing `/api/neatoo` endpoint. The distinction between fire-and-forget and AwaitRemote is at the dispatcher level, not the endpoint level.

**Fire-and-forget (default):**
- Client serializes event → HTTP POST to `/api/neatoo` → server acknowledges receipt → client continues
- Server resolves local handler, runs in isolated scope on background task
- Fire-and-forget tasks registered with `IEventTracker.Track()` for graceful shutdown
- Same as current `[Event]` behavior

**AwaitRemote:**
- Client serializes event + `AwaitRemote` flag → HTTP POST to `/api/neatoo` → server runs handler synchronously in request scope → returns result/error → client Task completes
- HTTP connection stays open for the duration of handler execution
- If handler throws, error serialized back to client

### New Files

| File | Purpose |
|------|---------|
| `src/RemoteFactory/FactoryEventBase.cs` | Abstract base class for event objects (STJ serialization in v1) |
| `src/RemoteFactory/IFactoryEvents.cs` | Publisher interface |
| `src/RemoteFactory/RaiseOptions.cs` | Flags enum |
| `src/Generator/EventHandler/` | New generator pipeline for `[FactoryEventHandler]` discovery and dispatch generation |
| `src/RemoteFactory/FactoryAttributes.cs` | Add `FactoryEventHandlerAttribute` |

### Modified Files

| File | Change |
|------|--------|
| `src/RemoteFactory.AspNetCore/` | Add endpoint for awaited event handling |
| `src/Generator/` | Add Phase 1-3 pipeline for `[FactoryEventHandler]` |
| `src/RemoteFactory/DI registration` | Register `IFactoryEvents` → generated dispatcher |

---

## Implementation Steps

1. **Define public API types** — `FactoryEventBase`, `IFactoryEvents`, `RaiseOptions`, `FactoryEventHandlerAttribute` in core library
2. **Generator Phase 1** — Extract `[FactoryEventHandler]` methods from `[Factory]` classes (reuse existing attribute detection patterns)
3. **Generator Phase 2** — Collect and group handlers by event type using incremental generator `Collect()`
4. **Generator Phase 3** — Generate `FactoryEventsDispatcher` class with dispatch dictionary
5. **Local handler invocation** — Generate scope-isolated invocation (reuse existing `[Event]` scope isolation pattern)
6. **Remote handler invocation** — Generate client-side serialization and server-side dispatch for `[Remote, FactoryEventHandler]` methods
7. **AwaitRemote support** — Modify HTTP endpoint to support synchronous handler execution when `AwaitRemote` flag is set
8. **ContinueOnFail support** — Implement `WhenAllContinueOnFail` that collects exceptions into `AggregateException`
9. **Cross-assembly registration** — Generate per-assembly registrar; wire into `AddNeatooRemoteFactory()`
10. **Design project examples** — Add event handler examples to `src/Design/Design.Domain/` and tests to `src/Design/Design.Tests/`
11. **Unit tests** — Generator output tests, dispatch tests, serialization round-trip tests
12. **Integration tests** — Client/server container tests for remote event handling with both options
13. **Diagnostic** — Add compile-time diagnostic for `[FactoryEventHandler]` without `CancellationToken`

---

## Acceptance Criteria

- [ ] `IFactoryEvents.Raise<T>()` dispatches to all registered handlers for type `T`
- [ ] Handlers execute in parallel, in isolated DI scopes
- [ ] Default behavior is fire-and-forget for remote handlers
- [ ] `RaiseOptions.AwaitRemote` awaits full remote handler completion
- [ ] `RaiseOptions.ContinueOnFail` continues on handler failure, aggregates exceptions
- [ ] Options combine as bitwise flags (`AwaitRemote | ContinueOnFail`)
- [ ] Cross-assembly handler discovery works via generated registrars
- [ ] No reflection at runtime — all dispatch is source-generated
- [ ] Existing `[Event]` delegate pattern continues to work unchanged
- [ ] Compile-time diagnostic for missing `CancellationToken` on `[FactoryEventHandler]`
- [ ] Design project has working examples and tests
- [ ] Serialization round-trip tests pass for event objects crossing client/server boundary

---

## Dependencies

- Existing incremental generator infrastructure (Phase 1-3 pattern)
- Existing scope isolation pattern from `[Event]` code generation
- Existing `IMakeRemoteDelegateRequest` for remote event serialization
- `NeatooJsonSerializer` for event object serialization

---

## Risks / Considerations

- **Generator complexity** — The gather phase (Collect across all `[Factory]` classes) is a new pattern for this generator. Need to ensure incremental generator caching works correctly.
- **Cross-assembly ordering** — Registrar invocation order affects nothing (parallel execution, no ordering guarantees), but need to verify DI registration doesn't conflict.
- **AwaitRemote HTTP semantics** — Keeping HTTP connections open for long-running handlers may cause timeouts. May need configurable timeout or documentation guidance.
- **Event serialization** — Event objects must be serializable. Records with primitive properties are straightforward, but complex event payloads could hit serialization edge cases.
- **Interaction with `[Event]`** — Need to verify no naming collisions between `[FactoryEventHandler]` generated artifacts and existing `[Event]` generated delegates.
