# Factory Events

RemoteFactory's factory-event surface is a mediator pattern: factory methods raise strongly-typed events via `IFactoryEvents.Raise<T>`, server-side static handlers decorated with `[FactoryEventHandler<T>]` process them in the caller's DI scope, and captured events flow back to the client through a consumer-implemented `IFactoryEventRelay`. Each handler declares *when* it runs via the attribute's optional [`DispatchPhase`](#dispatch-phases) argument — the default, `Immediate`, is the contract in the table below.

| Aspect | Behavior (`Immediate` phase — the default) |
|--------|----------|
| Trigger | `IFactoryEvents.Raise(...)` inside a factory method |
| DI scope | **Shared with the caller** (same `DbContext`, same transaction, staged/unflushed state visible) |
| Dispatch | **Sequential, awaited** — `Raise` returns only after all `Immediate` handlers complete |
| Exceptions | **Propagate to the caller** — a throwing handler aborts the chain |
| Cancellation | Caller's `CancellationToken` (threaded through `Raise`) |
| Client relay | `IFactoryEventRelay` (consumer implementation; batch delivered after factory result returns) |
| Deferred phases | `AfterFlush` / `AfterCommit` handlers are queued at `Raise` and run at their [drain points](#dispatch-phases) |
| Use for | **Transactional domain events** — handlers that must participate in the caller's DB transaction |

> **Fire-and-forget work (email, webhooks, audit sinks to external systems)** has no framework-supplied surface after v1.5.0. Compose your own `Task.Run` + `IServiceScopeFactory.CreateScope()` inside the factory method and explicitly snapshot any ambient state (tenant, correlation) into the background scope. See the [v1.5.0 release notes](release-notes/v1.5.0.md) for the migration pattern.

---

## The Execution Model

`IFactoryEvents.Raise<T>()` is a mediator that dispatches events to every handler decorated with `[FactoryEventHandler<T>]`. The three invariants of the **default (`Immediate`) phase** — an attribute with no `DispatchPhase` argument — are:

1. **Shared scope.** Handlers resolve `[Service]` dependencies from the caller's `IServiceProvider`. A `DbContext` injected into the factory method and a `DbContext` injected into the handler are the same instance and the same transaction. The flip side: an `Immediate` handler observes the caller's **staged (unflushed) state** — a projection that queries the database here reads the world without the aggregate's pending writes. That is what the [deferred phases](#dispatch-phases) are for.
2. **Sequential.** Handlers run one after another in unspecified order. Callers must not rely on a specific ordering. A `DbContext` is not thread-safe, so handlers cannot run in parallel.
3. **Awaited.** `Raise<T>()` returns only after every `Immediate` handler has completed. A handler exception aborts the remaining handlers and propagates to the caller so the transaction can roll back. Across the client/server boundary, the HTTP call stays open until all server-side handlers finish — including, for deferred phases, the entry call's completion drain.

Handlers do not know about each other, and the raising code does not know which handlers exist. Handlers registered at `AfterFlush` or `AfterCommit` keep invariants 1 and 2 (same scope, sequential) but run at a later [drain point](#dispatch-phases) with different failure semantics.

### Raising an Event

Inject `IFactoryEvents` as a `[Service]` parameter inside a factory method and call `Raise`:

```csharp
public record OrderCheckoutCompleted(int OrderId, decimal Total) : FactoryEventBase;

[Factory]
internal partial class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }

    [Remote, Create]
    internal async Task Create(
        int id,
        decimal total,
        [Service] IFactoryEvents factoryEvents,
        CancellationToken ct)
    {
        Id = id;
        Total = total;
        // Pass the factory method's CancellationToken through to Raise so
        // handlers that declare a CancellationToken parameter observe cancellation.
        await factoryEvents.Raise(new OrderCheckoutCompleted(id, total), RaiseOptions.None, ct);
    }
}
```

Event types must be records inheriting `FactoryEventBase`. Records give structural equality and immutability; `FactoryEventBase` is the marker the generator looks for.

The `Raise<T>` signature is:

```csharp
Task Raise<T>(
    T factoryEvent,
    RaiseOptions options = RaiseOptions.None,
    CancellationToken cancellationToken = default)
    where T : FactoryEventBase;
```

Thread the caller's `CancellationToken` through — handlers that declare a `CancellationToken` parameter receive it.

### Server-Side Handler (Static Method)

A `[FactoryEventHandler<T>]` class with a `static` matching method is treated as a server-side handler. The generator registers it with `FactoryEventHandlerRegistry`. Each handler runs in the caller's DI scope — the same `IServiceProvider` that resolved the factory method. `[Service]` parameters resolve from that scope, and the `CancellationToken` is the token the caller passed to `Raise`.

```csharp
[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class OrderCheckoutJournal
{
    internal static async Task RecordCheckoutInLedger(
        OrderCheckoutCompleted evt,
        [Service] AppDbContext db,    // SAME DbContext the factory method is using
        CancellationToken ct)          // caller's CancellationToken
    {
        db.LedgerEntries.Add(new LedgerEntry(evt.OrderId, evt.Total));
        await db.SaveChangesAsync(ct);
        // These changes participate in the factory's transaction. Throwing from
        // this handler aborts the factory operation and rolls everything back.
    }
}
```

The generator finds the handler method by signature:

- Return type must be `Task` (not `void`, not `Task<T>`)
- The first non-`[Service]`, non-`CancellationToken` parameter must be of type `T`
- Accessibility may be any level (public, internal, private)

Exactly one matching method must exist — see [Diagnostics](#diagnostics) below.

### Multiple Handler Types on One Class

Stack multiple `[FactoryEventHandler<T>]` attributes to handle several event types from the same class. The generator matches one method per attribute:

```csharp
[FactoryEventHandler<OrderCheckoutCompleted>]
[FactoryEventHandler<OrderShipped>]
public static partial class OrderAuditHandler
{
    internal static Task LogCheckout(
        OrderCheckoutCompleted evt,
        [Service] IAuditLogService audit,
        CancellationToken ct) =>
        audit.LogAsync("Checkout", evt.OrderId, "Order", $"Total: {evt.Total:C}", ct);

    internal static Task LogShipment(
        OrderShipped evt,
        [Service] IAuditLogService audit,
        CancellationToken ct) =>
        audit.LogAsync("Shipped", evt.OrderId, "Order", evt.Carrier, ct);
}
```

### Transactional vs Fire-and-Forget

`[FactoryEventHandler<T>]` is declarative, transactional, and awaited. The raiser publishes a typed event via `IFactoryEvents.Raise`; handlers share the caller's DI scope, run sequentially, and propagate exceptions. Use it for domain events that must participate in the caller's DB transaction — handlers that touch the same `DbContext` as the aggregate that raised the event.

If your handler talks to an external system that should never block or fail the aggregate save (email, webhook, queue publish), do not use `[FactoryEventHandler<T>]` — a throw from such a handler will roll back the aggregate. Instead, compose your own fire-and-forget pattern inside the factory method using `Task.Run` + `IServiceScopeFactory.CreateScope()`, and explicitly snapshot any ambient state (tenant, correlation ID) into the background scope before resolving services. See the [v1.5.0 release notes](release-notes/v1.5.0.md) for the recommended pattern and the migration path from the retired `[Event]` attribute API.

---

## Dispatch Phases

The attribute's optional `DispatchPhase` argument declares *when* a handler runs relative to the factory operation that raised the event:

```csharp
[FactoryEventHandler<InvoiceFinalized>]                            // Immediate — the default
[FactoryEventHandler<InvoiceFinalized>(DispatchPhase.AfterFlush)]  // deferred, consumer-drained
[FactoryEventHandler<InvoiceFinalized>(DispatchPhase.AfterCommit)] // deferred, framework-drained
```

| Phase | Runs at | Sees | Exceptions |
|-------|---------|------|------------|
| `Immediate` (default) | `Raise` | Staged (unflushed) state, mid-transaction | Propagate to the raiser — atomic with the save |
| `AfterFlush` | The factory method's `IFactoryEventPhaseCoordinator.DrainAsync(AfterFlush)` call — typically between its flush and its commit | Flushed state (database-generated keys, computed columns), transaction still open | Propagate to the drain call — can still roll the transaction back |
| `AfterCommit` | Framework-owned drain after the entry factory call completes successfully | Committed state, no ambient transaction | Logged ([9003](#runtime-log-events)) and swallowed — remaining queued handlers still run |

RemoteFactory owns **no persistence concepts** — it never flushes and never commits. The phase names describe consumer intent at the drain points the framework exposes; your persistence code decides where "after flush" actually is by calling the drain.

### Draining AfterFlush — `IFactoryEventPhaseCoordinator`

`[Service]`-inject the coordinator (method injection — server-only, like the persistence services it belongs beside) and drain **inside the factory method body**, at your flush/commit seam:

```csharp
[Remote, Create]
internal async Task Finalize(
    Guid id,
    [Service] AppDbContext db,
    [Service] IFactoryEvents events,
    [Service] IFactoryEventPhaseCoordinator coordinator,
    CancellationToken ct)
{
    // ... stage writes ...

    // Immediate handlers run here, atomic with the staged writes.
    await events.Raise(new InvoiceFinalized(id), RaiseOptions.None, ct);

    await db.SaveChangesAsync(ct);          // flush — DB-generated state exists, tx open

    // AfterFlush handlers run here, in-transaction, seeing flushed state.
    // An exception propagates out of this call so the transaction rolls back.
    await coordinator.DrainAsync(DispatchPhase.AfterFlush, ct);

    await db.Database.CurrentTransaction!.CommitAsync(ct);  // commit
}
// AfterCommit handlers run after this method returns and the entry call completes.
```

Only `AfterFlush` is consumer-drainable: `Immediate` handlers are never queued, and the `AfterCommit` drain point belongs to the framework — it runs only after the entry call has succeeded. `DrainAsync` throws `ArgumentOutOfRangeException` for any other phase. The drain must run inside the factory body: the coordinator returns without draining when no entry factory call is active in the scope, so a transaction helper that wraps the factory call *from outside* drains into nothing — compose the helper into the body instead.

### The Phase Contract

- **Failure semantics belong to the drain point, not the phase.** In-transaction drains (`Immediate` dispatch, the coordinator's `AfterFlush` drain) propagate exceptions; the post-completion drain logs and swallows them — including a handler-internal `OperationCanceledException`, because the entry drain passes no token and nothing may abort a succeeded call's post-completion work.
- **Ordering is anchored per drain point, not a global barrier.** For events raised before a given drain point, all `Immediate` handlers complete before any `AfterFlush` handler, which complete before any `AfterCommit` handler. Code that raises again *after* its own drain call interleaves that later `Immediate` work between drain points. Order *within* a phase is unspecified.
- **A failed entry call discards queued work.** If the factory operation throws, `AfterFlush`/`AfterCommit` queues are discarded ([9006](#runtime-log-events)) — deferred handlers never observe a failed operation. Its `Immediate` handlers already ran, inside the transaction the consumer's rollback unwinds.
- **Never-drained `AfterFlush` work is fail-open.** A handler the consumer never drains runs at the `AfterCommit` point instead, under post-completion swallow semantics it did not ask for, and the framework logs Warning [9007](#runtime-log-events) naming the event type. Forgetting the drain costs the intended transaction placement, never the handler.
- **Deferred handlers still relay.** Events raised *by* `AfterFlush`/`AfterCommit` handlers join the same HTTP response's relay batch — the response stays open through the entry call's completion drain.
- **One factory call per DI scope at a time.** Phase queues and entry-call tracking are per-scope, not per-flow: two concurrent factory calls in one scope share both, so a failed flow's discard and a surviving flow's drain can interleave. Scopes are the framework's isolation unit — give concurrent flows their own scopes (an ASP.NET Core request already is one).
- **Synchronous factory bodies and phased events don't mix on context-bound scopes.** A synchronous factory body that raised phased work forces the completion drain to block (the no-silent-loss invariant outranks staying non-blocking), and blocking under a captured `SynchronizationContext` — e.g. Logical mode inside a Blazor Server circuit — deadlocks if a drained handler awaits without `ConfigureAwait(false)`. Prefer async factory bodies wherever phased handlers are in play.
- **An event raised outside any factory call** (no entry call active in the scope) dispatches its phased handlers immediately, with a Debug log ([9005](#runtime-log-events)) — no throw, no silent drop.

### Coalescing Identical Dispatches (Opt-In)

A save that touches N items often raises the same event N times, and a deferred projection then recomputes N times for one visible change. The attribute's `Coalesce` named argument collapses identical **queued** dispatches so the drain runs the handler once:

```csharp
[FactoryEventHandler<StatementChanged>(DispatchPhase.AfterCommit, Coalesce = true)]
public static partial class StatementProjection { /* ... */ }
```

- **Identity is `Equals`.** Two queued dispatches collapse when they carry the same handler registration, `Equals`-equal events, and the same `RaiseOptions`. Records give structural equality by default — with two hazards that follow. An event with a **reference-typed member** (a `List<T>`, an entity) never compares equal across raises, so coalescing is a silent no-op for it: prefer value-only payloads on events whose handlers coalesce. And a **custom `Equals` override** that compares semantically distinct raises equal collapses dispatches you may have expected — the override owns that outcome.
- **Pending work only.** At any moment the scope holds at most one pending dispatch per identity; a dispatch a running drain has already taken is history, and a raise arriving after it starts a fresh pending dispatch. The observable counts — [9002](#runtime-log-events) drained, [9006](#runtime-log-events) discarded — reflect the collapsed queue, and each collapse logs Debug [9008](#runtime-log-events) instead of a second 9001.
- **The fail-open warning survives the collapse.** If any absorbed raise would have produced the [9007](#runtime-log-events) never-drained warning, the surviving dispatch produces it.
- **Inert wherever nothing queues.** `Immediate` handlers are never queued — declaring the flag there emits **NF0505** (Warning). Phased raises that fall through to immediate dispatch (no scheduler in scope, or no entry factory call active) also run once per raise regardless of the flag.
- **The relay is untouched.** Every `Raise` is still collected and relayed to the client; coalescing is about handler dispatch, not event delivery.
- **Same-event only.** Cross-event coalescing ("any of these four events → one recompute") is not provided — guard inside the handler if you need it.
- Handlers that don't opt in are unaffected: N raises remain N dispatches, in order.

### Choosing a Phase

| Your handler... | Phase |
|-----------------|-------|
| Must be atomic with the save — its failure should roll the operation back at `Raise` time | `Immediate` (default) |
| Needs database-generated state (identity keys, computed columns) while the transaction can still abort | `AfterFlush` + a coordinator drain between flush and commit |
| Is a read-only projection / cache refresh that must never fail the save | `AfterCommit` |
| Recomputes identically for every raise of the same value-identical event | A deferred phase + `Coalesce = true` (see [Coalescing](#coalescing-identical-dispatches-opt-in)) |

---

## The Client Relay Pattern

When a factory operation runs on the server and raises events via `IFactoryEvents.Raise`, those events are also captured by a request-scoped `IFactoryEventCollector`. The captured events travel back to the client piggybacked on the existing HTTP response — no SignalR, no push channel, no additional infrastructure.

### Data Flow

```
CLIENT                           SERVER
  |                                 |
  | 1. factory.Create(...)          |
  |-------------------------------->|
  |                                 | 2. Create runs in the request scope
  |                                 | 3. await events.Raise(new OrderCheckoutCompleted(...))
  |                                 |    - every Immediate [FactoryEventHandler<T>] static
  |                                 |      handler runs sequentially in the same scope;
  |                                 |      Raise returns after they complete. AfterFlush/
  |                                 |      AfterCommit handlers are queued for their
  |                                 |      drain points (still before the response)
  |                                 |    - event is also captured in IFactoryEventCollector
  |                                 |
  |        RemoteResponseDto        | 4. HandleRemoteDelegateRequest attaches
  |   { Json, RelayedEvents: [..] } |    collected events to the response
  |<--------------------------------|
  |                                 |
  | 5. Factory result returned to caller; caller's continuation resumes
  | 6. IFactoryEventRelay.Relay(batch) invoked exactly once
  |    (fire-and-forget, strictly after step 5)
```

The server runs every handler before serializing the response — `Immediate` at `Raise`, deferred phases at their [drain points](#dispatch-phases) — so events raised by deferred handlers still join the same relay batch. An in-transaction handler exception (`Immediate` dispatch, the consumer's `AfterFlush` drain) fails the operation and propagates back to the client; post-completion handler exceptions are logged (9003) and never fail the response. On the client, the factory result is returned to the caller **before** `IFactoryEventRelay.Relay` is invoked — this is a hard ordering guarantee backed by a `Task.Run + Task.Yield` dispatch in `MakeRemoteDelegateRequest`.

### Client-Side Relay (Consumer Implements `IFactoryEventRelay`)

RemoteFactory does not generate any client-relay machinery. Consumers implement `IFactoryEventRelay` and register it in DI. RemoteFactory invokes `Relay(batch)` fire-and-forget, exactly once per `[Remote]` call — including the empty-batch case — strictly after the caller's `await` resumes.

```csharp
public sealed class AggregatorRelay : IFactoryEventRelay
{
    private readonly IEventAggregator _aggregator;

    public AggregatorRelay(IEventAggregator aggregator) => _aggregator = aggregator;

    public Task Relay(IReadOnlyList<FactoryEventBase> events)
    {
        // One [Remote] call = exactly one Relay invocation, even when events.Count == 0.
        // The empty-batch case is a useful "a factory call just returned" signal.
        foreach (var evt in events) _aggregator.Publish(evt);
        return Task.CompletedTask;
    }
}
```

> **Instance-method `[FactoryEventHandler<T>]` handlers are no longer supported on the client.** The former client-relay pattern (register-by-attribute + `Register/Unregister` weak refs) has been removed. Declaring an instance method inside a `[FactoryEventHandler<T>]` class now emits **NF0503 (Warning)** and is silently skipped at runtime. Make the method `static` (server handler) **or** implement `IFactoryEventRelay` on your own class (client reception).

### IFactoryEventRelay

```csharp
public interface IFactoryEventRelay
{
    Task Relay(IReadOnlyList<FactoryEventBase> events);
}
```

**Contract guarantees (see also [CLAUDE-DESIGN](../src/Design/CLAUDE-DESIGN.md) Execution Model):**

- Invoked exactly once per `[Remote]` factory call. The empty-batch case (no events raised) still produces one invocation with `events.Count == 0`. The only exception: if batch deserialization throws `UnknownFactoryEventTypeException`, `Relay` is not invoked for that call and log event **3009** is emitted.
- Post-return ordering is a hard guarantee — dispatch uses `Task.Run(async () => { await Task.Yield(); ... }, CancellationToken.None)` in `MakeRemoteDelegateRequest`, so `Relay` runs strictly after the caller's continuation. Holds across sync-context (Blazor UI) and no-sync-context (console, server-render) hosts.
- Relay exceptions are caught and logged (EventId **3008** `FactoryEventRelayFailed`). They never propagate to the factory caller.
- Consumers own threading / SyncContext marshaling inside `Relay`. For Blazor UI work, dispatch back to the UI thread inside the implementation.
- Events arrive fully deserialized as `FactoryEventBase` subclass instances, in the server-side order in which `Raise<T>` was called.

### Default No-Op Registration

In Remote mode, if the consumer registers no `IFactoryEventRelay`, RemoteFactory registers `NoOpFactoryEventRelay` via `TryAddSingleton`. The first non-empty batch the no-op drops emits log event **3011** `NoOpFactoryEventRelayFirstEvent` (Warning, once per process) to surface the common "forgot to register" mistake.

TryAdd semantics:

- Registering your relay **before** `AddNeatooRemoteFactory` — your registration wins, no-op is not added.
- Registering your relay **after** `AddNeatooRemoteFactory` — standard DI override replaces the no-op.

Server and Logical modes do not register `IFactoryEventRelay` at all (no relay surface on the server; no cross-boundary communication in Logical).

### RaiseOptions.ServerOnly

To raise an event that runs server-side handlers but is **not** relayed to the client, pass `RaiseOptions.ServerOnly`:

```csharp
await factoryEvents.Raise(
    new OrderCheckoutCompleted(id, total),
    RaiseOptions.ServerOnly,
    ct);
```

Use this for server-internal concerns (trigger a downstream process, record an audit row) that the UI does not need to know about. `ServerOnly` composes with other flags:

| Flag | Meaning |
|------|---------|
| `None` | Default. Server handlers run (sequentially, in the caller's scope, awaited); event is captured for client relay. |
| `ServerOnly` | Server handlers run; event is NOT relayed to the client. |

### Nested Operations

The `IFactoryEventCollector` is request-scoped. Events raised during nested operations — for example, a child `Insert` running as part of a parent `Save` — are captured by the same collector and relayed to the client along with the parent's events.

### Logical Mode

In Logical (local) mode no collector is registered and no relay occurs, because nothing ever crosses the client/server boundary. Handlers still dispatch via the mediator in the caller's scope, sequentially, awaited — the same execution model as server mode.

---

## Serialization and Transport

Relayed events travel in `RemoteResponseDto.RelayedEvents`:

```csharp
public class RemoteResponseDto
{
    public string? Json { get; private set; }
    public IReadOnlyList<RelayedFactoryEvent>? RelayedEvents { get; private set; }
}

public class RelayedFactoryEvent
{
    public string TypeFullName { get; set; } = null!;
    public string Json { get; set; } = null!;
}
```

On the wire, `RelayedEvents` is `null` (not an empty list) when zero events are captured — this preserves backward-compatible JSON payloads. The client normalizes `null` to `Array.Empty<FactoryEventBase>()` before invoking `Relay`, so the consumer always observes exactly one `Relay` call per `[Remote]` invocation.

Event types are resolved on the client by `TypeFullName` against the runtime `FactoryEventTypeRegistry` (internal) — an `AppDomain.CurrentDomain.GetAssemblies()` scan for non-abstract `FactoryEventBase` descendants, lazy-initialized on first use and thread-safe. On a miss, the registry rescans once before throwing `UnknownFactoryEventTypeException` (to pick up dynamically-loaded assemblies). When two distinct `Type`s share a `FullName`, log event **3012** `FactoryEventTypeRegistryCollision` is emitted (Warning) and the first-scanned type wins. `RelayedFactoryEvent` and `List<RelayedFactoryEvent>` are registered with `NeatooTransportJsonContext` so they survive IL trimming.

### IL Trimming and Event Records

Every accessible descendant of `FactoryEventBase` is automatically preserved from IL trimming: the source generator discovers each concrete descendant declared in a compilation and emits a per-assembly event-preservation registrar that preserves the event's constructors/properties and its nested property graph. (The `[DynamicallyAccessedMembers]` annotation on `FactoryEventBase` does not do this — DAM does not flow to derived types under ILLink, which a publish-trimmed repro proved.) `IFactoryEvents.Raise<T>` retains `[DynamicallyAccessedMembers(All)]` on its generic parameter for producer-side call-site preservation.

This model also drives discovery: `FactoryEventBase` carries `[FactoryEvent]` with `Inherited = true`, which the runtime `FactoryEventTypeRegistry` keys off during its assembly scan. Inheriting `FactoryEventBase` is sufficient — consumers never apply `[FactoryEvent]` directly.

See [IL Trimming](trimming.md#factory-event-type-preservation) for the full mechanism, the end-to-end publish-trimmed verification (`EventSubscribeOnlySmokeTest.cs` — the subscribe-only consumer shape — plus the `EventRelaySmokeTest.cs` round-trip), and the `IL2091` consideration for user code that forwards `Raise<T>` through its own generic wrapper.

---

## Decision Guide

| Question | Answer |
|----------|--------|
| How do I handle a factory event on the **server**? | `[FactoryEventHandler<T>]` class with a `static` matching method |
| How do I handle a factory event on the **client**? | Implement `IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase>)` and register it in DI (see [Client-Side Relay](#client-side-relay-consumer-implements-ifactoryeventrelay)) |
| Does a `[FactoryEventHandler<T>]` class need `[Factory]`? | No — it is a separate generator pipeline |
| Can one class handle multiple event types? | Yes — stack multiple `[FactoryEventHandler<T>]` attributes |
| How do I stop an event from reaching the client? | Pass `RaiseOptions.ServerOnly` to `Raise` |
| Where must events be raised? | Inside a factory method, via `[Service] IFactoryEvents` |
| What type must events inherit from? | `FactoryEventBase` (records only) |
| What must the handler method return? | `Task` |
| What is the required parameter order? | `T evt` first (after any `[Service]`/`CancellationToken`), services and `CancellationToken` anywhere |
| How do I defer a handler until after the flush? | `[FactoryEventHandler<T>(DispatchPhase.AfterFlush)]` + `IFactoryEventPhaseCoordinator.DrainAsync(AfterFlush)` inside the factory body (see [Dispatch Phases](#dispatch-phases)) |
| How do I run a handler only after the operation has committed? | `[FactoryEventHandler<T>(DispatchPhase.AfterCommit)]` — the framework drains it after the entry call succeeds |
| Do deferred handlers run if the factory call throws? | No — queued work is discarded; only `Immediate` handlers had already run, inside the rolled-back transaction |

---

## Anti-Patterns

### Raising a Factory Event Outside a Factory Method

**Wrong:**

```csharp
// Client code calling a factory, then trying to raise an event
var order = await factory.Create(...);
await factoryEvents.Raise(new OrderCheckoutCompleted(order.Id, order.Total));  // Wrong side
```

**Right:**

```csharp
[Remote, Create]
internal async Task Create(
    int id,
    decimal total,
    [Service] IFactoryEvents events,
    CancellationToken ct)
{
    Id = id; Total = total;
    // Server-side, inside factory method — caller's CT threaded through.
    await events.Raise(new OrderCheckoutCompleted(id, total), RaiseOptions.None, ct);
}
```

**Why it matters:** Events are captured by a request-scoped `IFactoryEventCollector` that only exists on the server during a factory operation. Events raised outside that scope have no collector and cannot be relayed.

### Decorating a Handler Class with [Factory]

**Wrong:**

```csharp
[Factory]                                   // WRONG: two pipelines on one class
[FactoryEventHandler<OrderCheckoutCompleted>]
public partial class OrderNotifier
{
    public Task HandleCheckout(OrderCheckoutCompleted evt) => Task.CompletedTask;
}
```

**Right:**

```csharp
[FactoryEventHandler<OrderCheckoutCompleted>]
public partial class OrderNotifier
{
    public Task HandleCheckout(OrderCheckoutCompleted evt) => Task.CompletedTask;
}
```

**Why it matters:** `[FactoryEventHandler<T>]` runs through a completely separate generator pipeline from `[Factory]`. The handler class is not a factory and should not be treated as one. Adding `[Factory]` forces the class through the factory generation pipeline where it would need factory methods, interfaces, and registration.

### Wrong Return Type

**Wrong:**

```csharp
[FactoryEventHandler<OrderCheckoutCompleted>]
public partial class OrderNotifier
{
    public async void HandleCheckout(OrderCheckoutCompleted evt) { }          // NF0501
    public Task<string> HandleCheckoutTyped(OrderCheckoutCompleted evt) { }   // NF0501
}
```

**Right:**

```csharp
public Task HandleCheckout(OrderCheckoutCompleted evt) => Task.CompletedTask;
```

**Why it matters:** The dispatcher awaits `Task`. `void` cannot be awaited, and `Task<T>` would force every handler to return a value with no clear meaning. The generator enforces this with NF0501.

---

## Diagnostics

| ID | Severity | Description |
|----|----------|-------------|
| NF0501 | Error | No matching handler method found for `[FactoryEventHandler<T>]`. The class must declare exactly one method returning `Task` whose first non-`[Service]`/non-`CancellationToken` parameter is of type `T`. |
| NF0502 | Error | Multiple matching handler methods found for `[FactoryEventHandler<T>]`. Remove the extras or split into separate handler classes. |
| NF0503 | Warning | A `[FactoryEventHandler<T>]` class declares an **instance**-method handler. This was the former client-relay pattern and is no longer wired up. The method is silently ignored at runtime. Make the method `static` for server-side dispatch, or implement `IFactoryEventRelay` on the class (and register it in DI) for client-side reception. |
| NF0504 | Warning | One class declares `[FactoryEventHandler<T>]` for the **same** event type more than once. Attribute stacking is for several event *types*; a repeated event type resolves to the same handler method, so only the first declaration registers — its `DispatchPhase` and its `Coalesce` flag. The message names the surviving registration. Remove the duplicate. |
| NF0505 | Warning | `Coalesce = true` on an `Immediate`-declared registration. Immediate dispatches run at `Raise` and are never queued, so there is nothing to coalesce — the flag has no effect (the registration is still emitted faithfully). Remove the flag, or defer the handler to `AfterFlush`/`AfterCommit`. |

### Runtime Log Events

| EventId | Name | Level | Fires When |
|---------|------|-------|-----------|
| 3008 | `FactoryEventRelayFailed` | Error | Consumer's `IFactoryEventRelay.Relay` threw; exception is swallowed, never propagates to the factory caller |
| 3009 | `FactoryEventDeserializationFailed` | Error | Wire-format event deserialization failed (e.g. `UnknownFactoryEventTypeException`); `Relay` is not invoked for that call |
| 3011 | `NoOpFactoryEventRelayFirstEvent` | Warning | `NoOpFactoryEventRelay` received its first non-empty batch — consumer forgot to register a relay; fires once per process |
| 3012 | `FactoryEventTypeRegistryCollision` | Warning | `FactoryEventTypeRegistry` found two distinct `Type`s sharing the same `FullName` during its assembly scan |
| 9001 | `FactoryEventPhaseQueued` | Debug | A handler registered at a non-`Immediate` `DispatchPhase` was deferred instead of dispatched at `Raise` time |
| 9002 | `FactoryEventPhaseDrained` | Debug | A phase drain completed, reporting how many dispatches ran |
| 9003 | `FactoryEventPhaseHandlerFailed` | Error | A deferred handler threw during a **post-completion** drain; the exception (including a handler-internal `OperationCanceledException`) is swallowed and the remaining queued handlers still run. Never fires for in-transaction drains — those propagate. |
| 9004 | `FactoryEventPhaseNoQueueInScope` | Debug | An event with a phased handler was raised in a scope with no phase scheduler registered; dispatched immediately rather than dropped |
| 9005 | `FactoryEventPhaseRaisedOutsideEntryCall` | Debug | An event with a phased handler was raised while no entry factory call was active in the scope; dispatched immediately rather than queued into a drain nobody owns |
| 9006 | `FactoryEventPhaseDiscardedAtExit` | Debug | A failed entry call discarded its deferred dispatches without running them |
| 9007 | `FactoryEventPhaseNeverDrained` | Warning | The post-completion sweep picked up an `AfterFlush` dispatch the consumer never drained — it ran at the `AfterCommit` point instead (fail-open). One warning per dispatch, naming the event type. Call `IFactoryEventPhaseCoordinator.DrainAsync(DispatchPhase.AfterFlush)` between your flush and your commit, or register the handler at a different phase. A coalesced survivor warns if any absorbed raise would have. |
| 9008 | `FactoryEventPhaseCoalesced` | Debug | A raise for a `Coalesce = true` handler collapsed into an identical pending dispatch — logged instead of a second 9001; the 9002/9006 counts reflect the collapsed queue |

`UnknownFactoryEventTypeException` is public and carries `UnresolvedTypeFullName` plus `BatchTypeFullNames` for diagnostics — consumers may inspect it via log context.

---

## DI Registration

Registered automatically by `AddRemoteFactoryServices` based on `FactoryMode`:

| Service | Server mode | Remote (client) mode | Logical mode |
|---------|-------------|----------------------|--------------|
| `IFactoryEvents` | Scoped | Scoped (no relay) | Scoped |
| `IFactoryEventCollector` | Scoped | — | — |
| `IFactoryEventRelay` | — | Singleton (`NoOpFactoryEventRelay` via `TryAdd`) | — |
| `IFactoryEventPhaseCoordinator` | Scoped | — | Scoped |

The phase queue and coordinator exist wherever handlers dispatch (Server and Logical). Remote mode has no handlers to drain — `[Service]`-injecting the coordinator on a non-`[Remote]` method there fails with the standard not-registered exception, the same client/server-boundary contract as any server-only service.

Consumer registrations for `IFactoryEventRelay` work via standard DI semantics:

- Register **before** `AddNeatooRemoteFactory` — `TryAddSingleton<IFactoryEventRelay, NoOpFactoryEventRelay>` keeps your registration.
- Register **after** `AddNeatooRemoteFactory` — your registration overrides the no-op.

Static handler classes (`[FactoryEventHandler<T>]` with a static method) register themselves into `FactoryEventHandlerRegistry` at startup via the generated `FactoryServiceRegistrar`. There is no client-side generator pipeline for relay anymore — the consumer's `IFactoryEventRelay` implementation is the sole dispatch target.

---

## Complete Example

```csharp
// --------------- Shared assembly (visible to client and server) ---------------
public record OrderCheckoutCompleted(int OrderId, decimal Total) : FactoryEventBase;

public interface IOrder { int Id { get; set; } decimal Total { get; set; } }

// --------------- Server + client: the factory raises the event ---------------
[Factory]
internal partial class Order : IOrder
{
    public int Id { get; set; }
    public decimal Total { get; set; }

    public Order() { }

    [Remote, Create]
    internal async Task Create(
        int id,
        decimal total,
        [Service] IFactoryEvents events,
        CancellationToken ct)
    {
        Id = id;
        Total = total;
        // Thread the factory method's CancellationToken through so handlers
        // that declare a CancellationToken parameter observe cancellation.
        await events.Raise(new OrderCheckoutCompleted(id, total), RaiseOptions.None, ct);
    }
}

// --------------- Server: static handler runs in the caller's scope ---------------
// Sharing the factory method's DI scope means audit.LogAsync participates in
// the same transaction as the Create above. A throw here rolls everything back.
[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class OrderCheckoutAudit
{
    internal static Task Log(
        OrderCheckoutCompleted evt,
        [Service] IAuditLogService audit,
        CancellationToken ct) =>
        audit.LogAsync("Checkout", evt.OrderId, "Order", $"Total: {evt.Total:C}", ct);
}

// --------------- Client: consumer-owned relay bridges to UI bus ---------------
// Implement IFactoryEventRelay once, dispatch from there to viewmodels / aggregator.
public sealed class CheckoutUiRelay : IFactoryEventRelay
{
    private readonly ICheckoutBanner _banner;

    public CheckoutUiRelay(ICheckoutBanner banner) => _banner = banner;

    public Task Relay(IReadOnlyList<FactoryEventBase> events)
    {
        foreach (var evt in events)
        {
            if (evt is OrderCheckoutCompleted oc)
            {
                _banner.Show($"Order {oc.OrderId} completed: {oc.Total:C}");
            }
        }
        return Task.CompletedTask;
    }
}

// --------------- Client Program.cs ---------------
// Either order works (TryAdd-before / override-after).
services.AddSingleton<IFactoryEventRelay, CheckoutUiRelay>();
services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(Order).Assembly);
```

---

## Next Steps

- [Attributes Reference](attributes-reference.md) — `[FactoryEventHandler<T>]` quick reference
- [Client-Server Architecture](client-server-architecture.md) — How `[Remote]` and the transport layer fit together
- [Serialization](serialization.md) — How `FactoryEventBase` and `RelayedFactoryEvent` cross the wire
- [v1.5.0 Release Notes](release-notes/v1.5.0.md) — Migration guide for former `[Event]` attribute consumers
