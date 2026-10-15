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

An event is a record inheriting `FactoryEventBase`. Records give structural equality and immutability; `FactoryEventBase` is the marker the generator looks for. The code on this page comes from RemoteFactory's compiled example set, `src/Design`:

<!-- snippet: events-event -->
<a id='snippet-events-event'></a>
```cs
public record OrderCheckoutCompleted(int OrderId, decimal Total) : FactoryEventBase;
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs#L71-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-event' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Raise it inside the operation it belongs to: inject `IFactoryEvents` as a `[Service]` parameter and call `Raise`. An event announces something that happened, so it is raised by a write (`[Insert]`, `[Update]`, `[Delete]`) or a command (`[Execute]`), never by a `[Create]`, which only builds a new object. Design's checkout raises it when it is stored:

<!-- snippet: events-raise -->
<a id='snippet-events-raise'></a>
```cs
[Remote, Insert]
internal async Task Insert([Service] IFactoryEvents factoryEvents, CancellationToken cancellationToken)
{
    // A real Insert writes the row here, then raises.

    // Raise the event. With default RaiseOptions.None, the event is:
    //   1. Dispatched to every server-side [FactoryEventHandler<T>] static-method
    //      handler in THIS method's DI scope, sequentially, awaited. A handler
    //      that touches a DbContext sees the same DbContext this factory uses;
    //      a handler exception aborts the chain and propagates to this method
    //      (letting the caller's transaction roll back).
    //   2. Captured for relay back to the client in RemoteResponseDto. On the
    //      client, IFactoryEventRelay.Relay(...) is invoked fire-and-forget
    //      strictly AFTER this factory method's return value has been handed
    //      back to the caller and their continuation has resumed.
    // The save's token is threaded through, so handlers that take a
    // CancellationToken observe the caller's cancellation.
    await factoryEvents.Raise(new OrderCheckoutCompleted(Id, Total), RaiseOptions.None, cancellationToken);

    // Stored: the next Save routes to Update.
    IsNew = false;
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs#L147-L170' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-raise' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`Raise<T>(T factoryEvent, RaiseOptions options = RaiseOptions.None, CancellationToken cancellationToken = default)` is constrained to `T : FactoryEventBase`. Thread the operation's `CancellationToken` through, as the Insert does: handlers that declare a `CancellationToken` parameter receive it.

### Server-Side Handler (Static Method)

A `[FactoryEventHandler<T>]` class with a `static` matching method is treated as a server-side handler. The generator registers it with `FactoryEventHandlerRegistry`. Each handler runs in the caller's DI scope — the same `IServiceProvider` that resolved the factory method. `[Service]` parameters resolve from that scope, and the `CancellationToken` is the token the caller passed to `Raise`.

<!-- snippet: attributes-factoryeventhandler -->
<a id='snippet-attributes-factoryeventhandler'></a>
```cs
[FactoryEventHandler<OrderPlacedEvent>]
public static partial class OrderNotifyHandlers
{
    internal static async Task SendOrderConfirmation(
        OrderPlacedEvent orderEvent,
        [Service] INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        await notificationService.SendAsync(
            orderEvent.CustomerEmail,
            $"Order {orderEvent.OrderId} confirmed!");
    }
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs#L92-L106' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factoryeventhandler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A `[Service]` the handler shares with the factory method is the same instance: a `DbContext` the handler writes to takes part in the operation's transaction, and a throw from the handler aborts the operation.

The generator finds the handler method by signature:

- The method is `static`. An instance method emits NF0503 and is ignored (see [Diagnostics](#diagnostics)).
- Return type must be `Task` (not `void`, not `Task<T>`)
- The first non-`[Service]`, non-`CancellationToken` parameter must be of type `T`
- Accessibility may be any level (public, internal, private)

Exactly one matching method must exist — see [Diagnostics](#diagnostics) below.

### Multiple Handler Types on One Class

Stack several `[FactoryEventHandler<T>]` attributes on one class to handle several event types, with one matching `static` method per event type. Repeating the same event type on one class emits NF0504, and only the first declaration registers. Several classes may also handle the same event: Design's `OrderNotifyHandlers` and `OrderAuditHdlrs` both handle `OrderPlacedEvent`, and both run.

### Transactional vs Fire-and-Forget

`[FactoryEventHandler<T>]` is declarative, transactional, and awaited. The raiser publishes a typed event via `IFactoryEvents.Raise`; handlers share the caller's DI scope, run sequentially, and propagate exceptions. Use it for domain events that must participate in the caller's DB transaction — handlers that touch the same `DbContext` as the aggregate that raised the event.

If your handler talks to an external system that should never block or fail the aggregate save (email, webhook, queue publish), do not use `[FactoryEventHandler<T>]` — a throw from such a handler will roll back the aggregate. Instead, compose your own fire-and-forget pattern inside the factory method using `Task.Run` + `IServiceScopeFactory.CreateScope()`, and explicitly snapshot any ambient state (tenant, correlation ID) into the background scope before resolving services. See the [v1.5.0 release notes](release-notes/v1.5.0.md) for the recommended pattern and the migration path from the retired `[Event]` attribute API.

---

## Dispatch Phases

The attribute's optional `DispatchPhase` argument declares *when* a handler runs relative to the factory operation that raised the event. Design handles one event at all three phases:

<!-- snippet: events-phase-handlers -->
<a id='snippet-events-phase-handlers'></a>
```cs
[FactoryEventHandler<InvoiceFinalizedEvent>]
public static partial class InvoiceBalanceGuard
{
    internal static Task Enforce(
        InvoiceFinalizedEvent finalized,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(finalized.InvoiceId, "invoice-immediate");
        return Task.CompletedTask;
    }
}

/// <summary>
/// AfterFlush: queued at <c>Raise</c>, run at the factory method's
/// <c>DrainAsync</c> call. In a real consumer this is the projection that needs
/// database-generated state (identity keys, computed totals) while the
/// transaction is still open — it can still roll the operation back by throwing.
/// </summary>
[FactoryEventHandler<InvoiceFinalizedEvent>(DispatchPhase.AfterFlush)]
public static partial class InvoiceLedgerProjection
{
    internal static Task Project(
        InvoiceFinalizedEvent finalized,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(finalized.InvoiceId, "invoice-flush");
        return Task.CompletedTask;
    }
}

/// <summary>
/// AfterCommit: queued at <c>Raise</c>, run by the framework after the entry
/// call completes. No ambient transaction — a throw is logged (9003) and
/// swallowed. The right phase for read-only refresh work that must never fail
/// the save it follows.
/// </summary>
[FactoryEventHandler<InvoiceFinalizedEvent>(DispatchPhase.AfterCommit)]
public static partial class InvoiceSearchIndexRefresh
{
    internal static Task Refresh(
        InvoiceFinalizedEvent finalized,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(finalized.InvoiceId, "invoice-commit");
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventPhasesPattern.cs#L160-L208' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-phase-handlers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

| Phase | Runs at | Sees | Exceptions |
|-------|---------|------|------------|
| `Immediate` (default) | `Raise` | Staged (unflushed) state, mid-transaction | Propagate to the raiser — atomic with the save |
| `AfterFlush` | The factory method's `IFactoryEventPhaseCoordinator.DrainAsync(AfterFlush)` call — typically between its flush and its commit | Flushed state (database-generated keys, computed columns), transaction still open | Propagate to the drain call — can still roll the transaction back |
| `AfterCommit` | Framework-owned drain after the entry factory call completes successfully | Committed state, no ambient transaction | Logged ([9003](#runtime-log-events)) and swallowed — remaining queued handlers still run |

RemoteFactory owns **no persistence concepts** — it never flushes and never commits. The phase names describe consumer intent at the drain points the framework exposes; your persistence code decides where "after flush" actually is by calling the drain.

### Draining AfterFlush — `IFactoryEventPhaseCoordinator`

`[Service]`-inject the coordinator and drain **inside the factory method body**, at your flush/commit seam. The coordinator is registered on the server and in Logical mode, beside the persistence services it works with, so the method is `[Remote]`. Finalizing an invoice stages writes, flushes and commits, so it is a command, not a Create:

<!-- snippet: events-phase-drain -->
<a id='snippet-events-phase-drain'></a>
```cs
[Factory]
public static partial class InvoiceFinalizer
{
    [Execute]
    [Remote]
    internal static async Task<FinalizedInvoice> _Finalize(
        Guid id,
        decimal total,
        [Service] IFactoryEvents events,
        [Service] IFactoryEventPhaseCoordinator coordinator,
        [Service] IPhaseAuditService audit,
        CancellationToken ct)
    {
        // Immediate handlers run HERE, atomic with the staged writes.
        await events.Raise(new InvoiceFinalizedEvent(id, total), RaiseOptions.None, ct);

        // A real consumer flushes here (e.g. DbContext.SaveChangesAsync) —
        // database-generated state now exists, transaction still open.

        // AfterFlush handlers run HERE, in-transaction, seeing flushed state.
        // An exception propagates out of this call so the transaction rolls back.
        await coordinator.DrainAsync(DispatchPhase.AfterFlush, ct);

        audit.Record(id, "invoice-method-done");

        // A real consumer commits here. AfterCommit handlers run after this
        // method returns and the entry call completes.
        return new FinalizedInvoice(id, total);
    }
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventPhasesPattern.cs#L234-L265' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-phase-drain' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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

<!-- snippet: events-coalesce -->
<a id='snippet-events-coalesce'></a>
```cs
[FactoryEventHandler<StatementChangedEvent>(DispatchPhase.AfterCommit, Coalesce = true)]
public static partial class StatementProjection
{
    internal static Task Recompute(
        StatementChangedEvent changed,
        [Service] IPhaseAuditService audit)
    {
        audit.Record(changed.AccountId, "statement-recompute");
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventPhasesPattern.cs#L528-L540' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-coalesce' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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
  | 1. factory.Save(checkout)       |
  |-------------------------------->|
  |                                 | 2. Insert runs in the request scope
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

Design's relay collects what it receives; a production relay forwards each event to the application's aggregator or message bus:

<!-- snippet: events-relay -->
<a id='snippet-events-relay'></a>
```cs
public sealed class InMemoryAggregatorRelay : IFactoryEventRelay
{
    private readonly ConcurrentQueue<FactoryEventBase> _received = new();

    public IReadOnlyCollection<FactoryEventBase> Received => _received.ToArray();

    public IEnumerable<T> ReceivedOfType<T>() where T : FactoryEventBase =>
        _received.OfType<T>();

    public Task Relay(IReadOnlyList<FactoryEventBase> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        // Per the contract: one [Remote] call = one Relay call, even when the batch is empty.
        // Consumers can use the empty-batch invocation as a "a factory call just returned"
        // signal (e.g. for end-of-batch UI refresh).
        foreach (var evt in events)
        {
            _received.Enqueue(evt);
        }
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs#L284-L307' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-relay' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The client registers it in its container:

<!-- snippet: events-relay-register -->
<a id='snippet-events-relay-register'></a>
```cs
// Consumer replaces the NoOp default by registering their own relay.
services.AddSingleton<IFactoryEventRelay>(aggregator);
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/FactoryEventRelayTests.cs#L23-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-relay-register' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

> **Instance-method `[FactoryEventHandler<T>]` handlers are no longer supported on the client.** The former client-relay pattern (register-by-attribute + `Register/Unregister` weak refs) has been removed. Declaring an instance method inside a `[FactoryEventHandler<T>]` class now emits **NF0503 (Warning)** and is silently skipped at runtime. Make the method `static` (server handler) **or** implement `IFactoryEventRelay` on your own class (client reception).

### IFactoryEventRelay

The interface has one method, `Task Relay(IReadOnlyList<FactoryEventBase> events)`.

**Contract guarantees (see also [CLAUDE-DESIGN](../src/Design/CLAUDE-DESIGN.md) Execution Model):**

- Invoked exactly once per **remote round-trip** — a `[Remote]` factory call, or a [client-initiated `Raise`](#client-initiated-raise). The empty-batch case (no events raised) still produces one invocation with `events.Count == 0`. The only exception: if batch deserialization throws `UnknownFactoryEventTypeException`, `Relay` is not invoked for that call and log event **3009** is emitted.
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

To raise an event that runs server-side handlers but is **not** relayed to the client, pass `RaiseOptions.ServerOnly`. The checkout's Update re-announces a change the client made and already knows about:

<!-- snippet: events-serveronly -->
<a id='snippet-events-serveronly'></a>
```cs
[Remote, Update]
internal async Task Update([Service] IFactoryEvents factoryEvents)
{
    // ServerOnly: server handlers run, client does NOT receive the event
    await factoryEvents.Raise(new OrderCheckoutCompleted(Id, Total), RaiseOptions.ServerOnly);
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs#L182-L189' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-serveronly' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Use this for server-internal concerns (trigger a downstream process, record an audit row) that the UI does not need to know about. `ServerOnly` composes with other flags:

| Flag | Meaning |
|------|---------|
| `None` | Default. Server handlers run (sequentially, in the caller's scope, awaited); event is captured for client relay. |
| `ServerOnly` | Server handlers run; event is NOT relayed to the client. |

The flags mean the same thing for a [client-initiated `Raise`](#client-initiated-raise): `ServerOnly` suppresses relay of the caller's own event exactly as it does for one raised server-side.

### Client-Initiated Raise

Client code can raise an event itself. In Remote mode `IFactoryEvents` resolves to `RemoteFactoryEvents`, which sends the event to the server as its own round-trip:

<!-- snippet: events-client-raise -->
<a id='snippet-events-client-raise'></a>
```cs
var events = client.ServiceProvider.GetRequiredService<IFactoryEvents>();
await events.Raise(new ClientRaisedNotice(88));
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/FactoryEventRelayTests.cs#L131-L134' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-client-raise' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Server-side it lands in the ordinary dispatch path, so the request-scoped collector captures it and every `[FactoryEventHandler<T>]` runs before the response is written. The response carries the batch back and `IFactoryEventRelay.Relay` receives it — the same machinery as a factory call, because `HandleRemoteDelegateRequest` does not branch on which delegate ran.

**The batch contains the caller's own event**, plus anything its handlers raised:

```
CLIENT                              SERVER
  |                                    |
  | 1. await events.Raise(             |
  |      ClientRaisedNotice)           |
  |    (no local dispatch happens)     |
  |----------------------------------->|
  |                                    | 2. handlers for ClientRaisedNotice run
  |                                    | 3. a handler raises NoticeAcknowledged
  |                                    |    - both captured in the collector
  |        RemoteResponseDto           |
  |  { RelayedEvents:                  |
  |      [ClientRaisedNotice,          |
  |       NoticeAcknowledged] }        |
  |<-----------------------------------|
  | 4. Relay([ClientRaisedNotice, NoticeAcknowledged])
```

That echo is deliberate, not a transport artifact. `RemoteFactoryEvents.Raise` dispatches **nothing locally**, so the client's relay has not otherwise seen the event — relaying it back is the only way client-side handlers observe it at all. Pass `RaiseOptions.ServerOnly` to suppress the whole batch; handlers still run server-side and the client receives one invocation with `events.Count == 0`.

**A client raise is not fire-and-forget.** `Raise` returns only after every server handler completes, and a handler exception propagates back to the client, which means nothing relays for that call. RemoteFactory has had no fire-and-forget event surface since v1.5.0 removed `[Event]` — compose `Task.Run` + `IServiceScopeFactory.CreateScope()` yourself if that is what you need.

Prefer raising inside a factory method when the event belongs to that operation — see [the anti-pattern above](#raising-an-operations-event-from-the-client-instead-of-inside-the-method) for why the two are not interchangeable.

### Nested Operations

The `IFactoryEventCollector` is request-scoped. Events raised during nested operations — for example, a child `Insert` running as part of a parent `Save` — are captured by the same collector and relayed to the client along with the parent's events.

### Logical Mode

In Logical (local) mode no collector is registered and no relay occurs, because nothing ever crosses the client/server boundary. Handlers still dispatch via the mediator in the caller's scope, sequentially, awaited — the same execution model as server mode.

---

## Serialization and Transport

Relayed events travel in `RemoteResponseDto.RelayedEvents`, beside the response's `Json`. It is a list of `RelayedFactoryEvent`, each carrying the event's `TypeFullName` and its own `Json`.

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
| Where should events be raised? | Inside the write or command they belong to, via `[Service] IFactoryEvents`. Client code can also raise one, as its own round trip (see [Client-Initiated Raise](#client-initiated-raise)) |
| Can a `[Create]` raise an event? | It should not: a Create builds a new object, and nothing has happened yet. Raise from the `[Insert]` that stores it |
| What type must events inherit from? | `FactoryEventBase` (records only) |
| What must the handler method be? | `static`, returning `Task` |
| What is the required parameter order? | `T evt` first (after any `[Service]`/`CancellationToken`), services and `CancellationToken` anywhere |
| How do I defer a handler until after the flush? | `[FactoryEventHandler<T>(DispatchPhase.AfterFlush)]` + `IFactoryEventPhaseCoordinator.DrainAsync(AfterFlush)` inside the factory body (see [Dispatch Phases](#dispatch-phases)) |
| How do I run a handler only after the operation has committed? | `[FactoryEventHandler<T>(DispatchPhase.AfterCommit)]` — the framework drains it after the entry call succeeds |
| Do deferred handlers run if the factory call throws? | No — queued work is discarded; only `Immediate` handlers had already run, inside the rolled-back transaction |

---

## Anti-Patterns

### Raising an Operation's Event From the Client Instead of Inside the Method

**Less good:**

```csharp
// WRONG for an operation's own event: a second round trip, outside the save's transaction
checkout = await factory.Save(checkout);
await factoryEvents.Raise(new OrderCheckoutCompleted(checkout.Id, checkout.Total));
```

**Better:** raise it inside the write, as the checkout's Insert does in [Raising an Event](#raising-an-event).

**Why it matters:** the two are not equivalent, though both work. An event raised inside the factory method is captured by the same request-scoped `IFactoryEventCollector` as the operation, so its handlers share the method's DI scope — and therefore its `DbContext` and transaction — and an `Immediate` handler that throws rolls the operation back. It costs no extra round-trip, and it rides the operation's own response.

A client-side `Raise` is [a supported path](#client-initiated-raise) but a *separate* round-trip with its own request scope. It shares no transaction with the factory call that preceded it, and it cannot roll that call back. When the event belongs to the operation, raise it inside the operation.

### Decorating a Handler Class with [Factory]

**Wrong:**

```csharp
// WRONG - CS0111: both pipelines emit a FactoryServiceRegistrar for the class
[Factory]
[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class CheckoutNotifier
{
    internal static Task Handle(OrderCheckoutCompleted evt) => Task.CompletedTask;
}
```

**Right:** the handler class carries `[FactoryEventHandler<T>]` only, as `OrderNotifyHandlers` does in [Server-Side Handler](#server-side-handler-static-method).

**Why it matters:** `[FactoryEventHandler<T>]` runs through a separate generator pipeline from `[Factory]`. On a static class, `[Factory]` also generates a static factory, and each pipeline emits the class's `FactoryServiceRegistrar`, so the build fails with CS0111.

### Wrong Return Type

**Wrong:**

```csharp
// WRONG - NF0501: no matching handler method, because it does not return Task
[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class CheckoutNotifier
{
    internal static async void Handle(OrderCheckoutCompleted evt) { await Task.Yield(); }
}
```

**Right:** a `static` method returning `Task`, as in [Server-Side Handler](#server-side-handler-static-method).

**Why it matters:** The dispatcher awaits `Task`. `void` cannot be awaited, and `Task<T>` would force every handler to return a value with no clear meaning. A method of either shape does not match, so the class has no handler: NF0501.

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
| 9007 | `FactoryEventPhaseNeverDrained` | Warning | The post-completion sweep picked up an `AfterFlush` dispatch the consumer never drained — it ran at the `AfterCommit` point instead (fail-open). One warning per dispatch, naming the event type. Call `IFactoryEventPhaseCoordinator.DrainAsync(DispatchPhase.AfterFlush)` **from inside the factory method body**, between your flush and your commit — a drain called from outside the factory call never reaches this work, because the queue only exists while the entry call is active (see [9009](#runtime-log-events)). Otherwise register the handler at a different phase. A coalesced survivor warns if any absorbed raise would have. |
| 9008 | `FactoryEventPhaseCoalesced` | Debug | A raise for a `Coalesce = true` handler collapsed into an identical pending dispatch — logged instead of a second 9001; the 9002/9006 counts reflect the collapsed queue |
| 9009 | `FactoryEventPhaseDrainWithoutEntryCall` | Debug | `IFactoryEventPhaseCoordinator.DrainAsync` was called with no entry factory call active in the scope, so nothing was drained. Usually means the drain wraps the factory call from outside instead of running inside the factory method body — see [Draining AfterFlush](#draining-afterflush--ifactoryeventphasecoordinator). Debug rather than Warning because draining a scope with no factory work in flight is also a correct steady state. |

`UnknownFactoryEventTypeException` is public and carries `UnresolvedTypeFullName` plus `BatchTypeFullNames` for diagnostics — consumers may inspect it via log context.

---

## DI Registration

Registered automatically by `AddNeatooRemoteFactory`, by `NeatooFactory` mode:

| Service | Server mode | Remote (client) mode | Logical mode |
|---------|-------------|----------------------|--------------|
| `IFactoryEvents` | Scoped (`FactoryEventsDispatcher`) | Scoped (`RemoteFactoryEvents`, sends each raise to the server) | Scoped (`FactoryEventsDispatcher`) |
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

The pieces above make up one flow, in `src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs` and its tests:
1. **The event**, `OrderCheckoutCompleted`, is a record in the shared domain assembly ([Raising an Event](#raising-an-event)).
2. **The checkout's `[Remote, Insert]`** raises it on the server, inside the save ([Raising an Event](#raising-an-event)).
3. **Any server handler** for it runs in the save's scope, sequentially and awaited, like `OrderNotifyHandlers` for `OrderPlacedEvent` ([Server-Side Handler](#server-side-handler-static-method)).
4. **The response** carries it back to the client, where the registered `IFactoryEventRelay` receives it after `Save` returns ([Client-Side Relay](#client-side-relay-consumer-implements-ifactoryeventrelay)).

`FactoryEventRelayTests.Relay_EventRaisedInRemoteInsert_DeliveredToConsumer` runs the whole flow on the client/server containers.

---

## Next Steps

- [Attributes Reference](attributes-reference.md) — `[FactoryEventHandler<T>]` quick reference
- [Client-Server Architecture](client-server-architecture.md) — How `[Remote]` and the transport layer fit together
- [Serialization](serialization.md) — How `FactoryEventBase` and `RelayedFactoryEvent` cross the wire
- [v1.5.0 Release Notes](release-notes/v1.5.0.md) — Migration guide for former `[Event]` attribute consumers
