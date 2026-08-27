# Factory Events — `[FactoryEventHandler<T>]` (server) + `IFactoryEventRelay` (client)

RemoteFactory ships **one event-shaped abstraction**: transactional domain events raised with `IFactoryEvents.Raise(...)` inside a factory method, dispatched to server-side `[FactoryEventHandler<T>]` static handlers in the caller's DI scope and relayed to the client via `IFactoryEventRelay`. Each handler declares *when* it runs via the attribute's optional `DispatchPhase` argument — the default, `Immediate`, is the contract in the table below; see [Dispatch Phases](#dispatch-phases) for `AfterFlush` and `AfterCommit`.

| Property | Behavior (`Immediate` phase — the default) |
|----------|----------|
| Trigger | `IFactoryEvents.Raise(...)` inside a factory method (injected via `[Service] IFactoryEvents`) |
| Server DI scope per handler | **Shared with the caller** — same `DbContext`, same transaction, staged (unflushed) state visible |
| Server dispatch | **Sequential, awaited** — `Raise` returns only after every `Immediate` handler completes |
| Client dispatch | Fire-and-forget via `IFactoryEventRelay.Relay(...)`, invoked once per `[Remote]` factory call |
| Handler exceptions | Server: **propagate to the caller** — abort the chain, roll back the transaction. Client: caught + logged, never propagate. |
| Cancellation token | The caller's `CancellationToken`, passed through `Raise` |
| Deferred phases | `AfterFlush` / `AfterCommit` handlers are queued at `Raise` and run at their [drain points](#dispatch-phases) |
| Use for | **Transactional domain events** on the server; **event-aggregator delivery** to client UI / view models |

**Rule of thumb:** if the handler touches the same `DbContext` as the factory method and its failure should roll the factory operation back, use a `[FactoryEventHandler<T>]` static method on the server — `Immediate` when it must be atomic with the save, `AfterFlush` when it needs database-generated state first. To react on the client, implement `IFactoryEventRelay` and bridge events to your own aggregator.

**Fire-and-forget external IO** — email, webhook, queue publish — is **not** a factory-event use case. Those handlers would block the factory method until they complete, and a failure would roll back the factory operation. For fire-and-forget work, run `Task.Run` directly inside the factory method with a fresh scope from `IServiceScopeFactory.CreateScope()` and copy any ambient context (correlation ID, tenant) explicitly. RemoteFactory does not own this pattern — the consumer does.

---

## The Server-Side Execution Model — Three Invariants

Every `[FactoryEventHandler<T>]` static-method dispatch at the **default (`Immediate`) phase** — an attribute with no `DispatchPhase` argument — obeys three rules:

1. **Shared scope.** Handlers resolve `[Service]` dependencies from the caller's `IServiceProvider`. A `DbContext` injected into the factory method and a `DbContext` injected into the handler are the same instance, so both participate in the same transaction. The flip side: an `Immediate` handler observes the caller's **staged (unflushed) state** — a projection that queries the database here reads the world without the aggregate's pending writes. That is what the [deferred phases](#dispatch-phases) are for.
2. **Sequential.** Handlers run one after another in unspecified order. Callers must not rely on a specific ordering. A `DbContext` is not thread-safe, so parallel dispatch is not possible.
3. **Awaited.** `IFactoryEvents.Raise<T>()` returns only after every server-side `Immediate` handler has completed. A handler exception aborts the remaining handlers and propagates to the caller. Across the client/server boundary the HTTP call stays open until every server-side handler finishes — including, for deferred phases, the entry call's completion drain — and a server exception surfaces on the client.

Handlers registered at `AfterFlush` or `AfterCommit` keep rules 1 and 2 (same scope, sequential) but run at a later drain point with different failure semantics — see [Dispatch Phases](#dispatch-phases).

---

## Raising a Factory Event

Inject `IFactoryEvents` as a `[Service]` parameter and call `Raise` inside a factory method. Events are records inheriting `FactoryEventBase`:

```csharp
public record OrderCheckoutCompleted(int OrderId, decimal Total) : FactoryEventBase;

[Factory]
internal partial class Order
{
    public int Id { get; set; }
    public decimal Total { get; set; }

    public Order() { }

    [Remote, Create]
    internal async Task Create(
        int id,
        decimal total,
        [Service] AppDbContext db,
        [Service] IFactoryEvents factoryEvents,
        CancellationToken ct)
    {
        Id = id;
        Total = total;
        db.Orders.Add(new OrderEntity(id, total));
        await db.SaveChangesAsync(ct);

        // Every server-side [FactoryEventHandler<OrderCheckoutCompleted>] static
        // handler runs here, in this scope, sharing `db`. A throwing handler aborts
        // this method and rolls back everything saved above.
        //
        // The event is also captured for client-side relay (unless RaiseOptions.ServerOnly).
        await factoryEvents.Raise(new OrderCheckoutCompleted(id, total), RaiseOptions.None, ct);
    }
}
```

`Raise<T>` signature:

```csharp
Task Raise<T>(
    T factoryEvent,
    RaiseOptions options = RaiseOptions.None,
    CancellationToken cancellationToken = default)
    where T : FactoryEventBase;
```

Pass the factory method's `CancellationToken` through so cancellation reaches handler code.

---

## Server-Side Handler (Static Method)

A `[FactoryEventHandler<T>]` class with a `static` matching method is a server-side handler. The generator registers it in `FactoryEventHandlerRegistry`. At dispatch time it receives the caller's `IServiceProvider` and resolves every `[Service]` parameter from it.

```csharp
[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class OrderCheckoutJournal
{
    internal static async Task RecordInLedger(
        OrderCheckoutCompleted evt,
        [Service] AppDbContext db,     // SAME DbContext the factory method uses
        CancellationToken ct)           // caller's CancellationToken
    {
        db.LedgerEntries.Add(new LedgerEntry(evt.OrderId, evt.Total));
        await db.SaveChangesAsync(ct);
        // These changes are part of the factory's transaction.
    }
}
```

The generator matches the handler method by signature:

- The method **must be `static`**
- Return type must be `Task` (not `void`, not `Task<T>`)
- First non-`[Service]`/non-`CancellationToken` parameter must be of type `T`
- Any accessibility is allowed
- Exactly one match required — `NF0501` if none, `NF0502` if multiple

**Instance-method handlers are no longer supported.** A `[FactoryEventHandler<T>]` class declaring an instance-method handler emits diagnostic **NF0503** (Warning) at compile time and the method is silently ignored at runtime. Convert to `static` for server-side dispatch, or implement `IFactoryEventRelay` (below) for client-side reception.

### Multiple Handlers for the Same Event

Any number of classes can handle the same event type, each declaring its own phase. All of them run in the caller's scope, sequentially, in unspecified order within their phase — `Immediate` handlers before `Raise` returns, deferred handlers at their [drain point](#dispatch-phases):

```csharp
[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class OrderCheckoutInventory
{
    internal static async Task DecrementStock(
        OrderCheckoutCompleted evt,
        [Service] AppDbContext db,
        CancellationToken ct)
    {
        var item = await db.Inventory.FindAsync([evt.OrderId], ct);
        item!.Stock--;
        await db.SaveChangesAsync(ct);
    }
}
```

### Multiple Event Types in One Handler Class

Stack attributes to handle several event types in one class:

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

One method per attribute, and each attribute carries its own `DispatchPhase` and `Coalesce` flag. Stacking is for several event **types** — repeating the *same* event type on one class emits `NF0504` (Warning), and only the first declaration's registration (phase and flag) stands.

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
| `AfterCommit` | Framework-owned drain after the entry factory call completes successfully | Committed state, no ambient transaction | Logged (9003) and swallowed — remaining queued handlers still run |

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

- **Failure semantics belong to the drain point, not the phase.** In-transaction drains (`Immediate` dispatch, the coordinator's `AfterFlush` drain) propagate exceptions; the post-completion drain logs (9003) and swallows them — including a handler-internal `OperationCanceledException`, because the entry drain passes no token and nothing may abort a succeeded call's post-completion work.
- **Ordering is anchored per drain point, not a global barrier.** For events raised before a given drain point, all `Immediate` handlers complete before any `AfterFlush` handler, which complete before any `AfterCommit` handler. Code that raises again *after* its own drain call interleaves that later `Immediate` work between drain points. Order *within* a phase is unspecified.
- **A failed entry call discards queued work.** If the factory operation throws, `AfterFlush`/`AfterCommit` queues are discarded (9006) — deferred handlers never observe a failed operation. Its `Immediate` handlers already ran, inside the transaction the consumer's rollback unwinds.
- **Never-drained `AfterFlush` work is fail-open.** A handler the consumer never drains runs at the `AfterCommit` point instead, under post-completion swallow semantics it did not ask for, and the framework logs Warning 9007 naming the event type. Forgetting the drain costs the intended transaction placement, never the handler.
- **Deferred handlers still relay.** Events raised *by* `AfterFlush`/`AfterCommit` handlers join the same HTTP response's relay batch — the response stays open through the entry call's completion drain.
- **One factory call per DI scope at a time.** Phase queues and entry-call tracking are per-scope, not per-flow: two concurrent factory calls in one scope share both, so a failed flow's discard and a surviving flow's drain can interleave. Scopes are the framework's isolation unit — give concurrent flows their own scopes (an ASP.NET Core request already is one).
- **Synchronous factory bodies and phased events don't mix on context-bound scopes.** A synchronous factory body that raised phased work forces the completion drain to block, and blocking under a captured `SynchronizationContext` — e.g. Logical mode inside a Blazor Server circuit — deadlocks if a drained handler awaits without `ConfigureAwait(false)`. Prefer async factory bodies wherever phased handlers are in play.
- **An event raised outside any factory call** (no entry call active in the scope) dispatches its phased handlers immediately, with a Debug log (9005) — no throw, no silent drop.

### Coalescing Identical Dispatches (Opt-In)

A save that touches N items often raises the same event N times, and a deferred projection then recomputes N times for one visible change. The attribute's `Coalesce` named argument collapses identical **queued** dispatches so the drain runs the handler once:

```csharp
[FactoryEventHandler<StatementChanged>(DispatchPhase.AfterCommit, Coalesce = true)]
public static partial class StatementProjection { /* ... */ }
```

- **Identity is `Equals`** — same handler registration, `Equals`-equal event, same `RaiseOptions`. Records give structural equality by default, with two hazards: an event with a reference-typed member (a `List<T>`, an entity) never compares equal across raises, so coalescing is a silent no-op for it — prefer value-only payloads on events whose handlers coalesce; and a custom `Equals` override that equates semantically distinct raises collapses dispatches you expected — the override owns that.
- **Pending work only.** At most one pending dispatch per identity at any moment; a dispatch a running drain already took is history, and a raise after it starts fresh. The drained/discarded counts (9002/9006) reflect the collapsed queue; each collapse logs Debug 9008 instead of a second 9001.
- **The fail-open warning survives:** if any absorbed raise would have produced the 9007 never-drained warning, the surviving dispatch produces it.
- **Inert wherever nothing queues:** `Immediate` handlers (NF0505 warns — nothing to coalesce), and phased raises that fall through to immediate dispatch (no scheduler in scope / no entry call active) run once per raise regardless.
- **The relay is untouched** — every `Raise` is still collected and relayed; coalescing is about handler dispatch, not event delivery.
- **Same-event only.** Cross-event coalescing is not provided — guard inside the handler if you need it. Handlers that don't opt in keep one run per raise.

### Choosing a Phase

| Your handler... | Phase |
|-----------------|-------|
| Must be atomic with the save — its failure should roll the operation back at `Raise` time | `Immediate` (default) |
| Needs database-generated state (identity keys, computed columns) while the transaction can still abort | `AfterFlush` + a coordinator drain between flush and commit |
| Is a read-only projection / cache refresh that must never fail the save | `AfterCommit` |
| Recomputes identically for every raise of the same value-identical event | A deferred phase + `Coalesce = true` (see [Coalescing](#coalescing-identical-dispatches-opt-in)) |

---

## The Client Relay

When a factory operation runs on the server and raises events with default options, those events are captured by a request-scoped `IFactoryEventCollector` and travel back to the client piggybacked on the HTTP response (`RemoteResponseDto.RelayedEvents`).

Data flow:

```
CLIENT                           SERVER
  | 1. _entity = await factory.Create(...)
  |------------------------------>| 2. Create runs in the request scope
  |                               | 3. await events.Raise(new OrderCheckoutCompleted(...))
  |                               |    - every Immediate [FactoryEventHandler<T>]
  |                               |      static handler runs sequentially in the same
  |                               |      scope, awaited; Raise returns only after they
  |                               |      complete (or one throws). AfterFlush/AfterCommit
  |                               |      handlers are queued for their drain points
  |                               |      (still before the response is serialized)
  |                               |    - event is captured in IFactoryEventCollector
  |      RemoteResponseDto        | 4. Server attaches collected events to response
  |  { Json, RelayedEvents: [..] }|
  |<------------------------------|
  | 5. Factory result returned to caller; `_entity` is now assigned
  | 6. (post-return, fire-and-forget) IFactoryEventRelay.Relay(events) is invoked
  |    on a separate continuation — `_entity` assignment is already visible
```

### Post-Return Ordering — Hard Guarantee

`IFactoryEventRelay.Relay` is invoked **strictly after** the factory method's return value reaches the caller and the caller's continuation has begun executing. The pattern below is safe:

```csharp
_entity = await factory.Create(...);
// At this line `_entity` is already assigned.
// Relay fires sometime AFTER this line — handlers reading `_entity` see the new value.
```

Each `[Remote]` factory call produces **exactly one** `Relay` invocation, even when the batch is empty. Consumers can use the empty-batch invocation as a "factory call just returned" signal.

If deserialization of the relayed batch fails (e.g., the wire `TypeFullName` is not a `FactoryEventBase` descendant loaded on the client), the failure is logged (event 3009 `FactoryEventDeserializationFailed`) and `Relay` is not invoked for that call. The factory caller's `await` is unaffected.

### `IFactoryEventRelay`

```csharp
public interface IFactoryEventRelay
{
    Task Relay(IReadOnlyList<FactoryEventBase> events);
}
```

Consumer-implemented. RemoteFactory invokes it fire-and-forget after each `[Remote]` call. The consumer owns:

- Threading / SyncContext marshaling (e.g., posting UI updates back to the renderer thread on Blazor)
- Bridging the batch to their event aggregator (MediatR, plain aggregator, UI message bus, etc.)
- Per-event-type fan-out (`switch` on `evt`, an `IDictionary<Type, Action<FactoryEventBase>>`, etc.)

Exceptions thrown by `Relay` are caught and logged (event 3008 `FactoryEventRelayFailed`); they never propagate to the factory caller.

### A Bridge Implementation

```csharp
public sealed class AggregatorRelay : IFactoryEventRelay
{
    private readonly IMyEventAggregator _aggregator;

    public AggregatorRelay(IMyEventAggregator aggregator)
    {
        _aggregator = aggregator;
    }

    public async Task Relay(IReadOnlyList<FactoryEventBase> events)
    {
        foreach (var evt in events)
        {
            // Dispatch to per-type subscribers in your aggregator.
            await _aggregator.PublishAsync(evt);
        }
    }
}
```

Register as a singleton in DI. RemoteFactory's no-op default is replaced via standard last-writer-wins semantics:

```csharp
services.AddSingleton<IFactoryEventRelay, AggregatorRelay>();
services.AddNeatooRemoteFactory(NeatooFactory.Remote, ...);
```

Or before:

```csharp
services.AddSingleton<IFactoryEventRelay, AggregatorRelay>();
services.AddNeatooRemoteFactory(NeatooFactory.Remote, ...);
```

Either way works (`AddNeatooRemoteFactory` uses `TryAddSingleton` for its no-op default, so consumer-first registration wins; consumer-after registration overrides).

### NoOp Default — Silent Drop Footgun

If no consumer registers an `IFactoryEventRelay`, the no-op default drops every batch. To make this loud rather than silent, the no-op logs `Warning 3011 NoOpFactoryEventRelayFirstEvent` once per process on its first non-empty batch:

```
warn: Neatoo.RemoteFactory.NoOpFactoryEventRelay[3011]
      NoOpFactoryEventRelay received its first non-empty batch (1 event(s)).
      Events are being dropped. Register your own IFactoryEventRelay implementation
      to receive them (this warning fires once).
```

If you see this warning, you forgot to register your relay.

---

## Migration from the Pre-v1.4 Client Pattern

The old client-side pattern used instance-method `[FactoryEventHandler<T>]` handlers that called `_relay.Register(this)` / `_relay.Unregister(this)`:

```csharp
// OLD — no longer dispatched. Emits NF0503 Warning.
[FactoryEventHandler<OrderCheckoutCompleted>]
public sealed partial class CheckoutBannerViewModel : IDisposable
{
    private readonly IFactoryEventRelay _relay;
    public CheckoutBannerViewModel(IFactoryEventRelay relay)
    {
        _relay = relay;
        _relay.Register(this);
    }

    public Task ShowCheckoutMessage(OrderCheckoutCompleted evt) { /* ... */ return Task.CompletedTask; }

    public void Dispose() => _relay.Unregister(this);
}
```

Replace with an `IFactoryEventRelay` implementation that fans out to your own per-type subscribers:

```csharp
// NEW — implement IFactoryEventRelay, bridge to your aggregator
public sealed class UiNotificationRelay : IFactoryEventRelay
{
    private readonly ISnackbar _snackbar;

    public UiNotificationRelay(ISnackbar snackbar)
    {
        _snackbar = snackbar;
    }

    public Task Relay(IReadOnlyList<FactoryEventBase> events)
    {
        foreach (var evt in events)
        {
            switch (evt)
            {
                case OrderCheckoutCompleted o:
                    _snackbar.Add($"Order {o.OrderId} completed: {o.Total:C}");
                    break;
                // ... other cases
            }
        }
        return Task.CompletedTask;
    }
}
```

`IFactoryEventRelay.Register` and `Unregister` no longer exist — remove all calls. The old `FactoryEventRelayDispatcher` and `FactoryEventRelayRegistry` types are deleted.

---

## IL Trimming

Event records are automatically preserved from IL trimming: the source generator discovers every concrete, accessible `FactoryEventBase` descendant declared in a compilation and emits a per-assembly event-preservation registrar, so descendants (and their nested property graphs) survive `PublishTrimmed=true` without per-event annotation. (The `[DynamicallyAccessedMembers]` annotation on `FactoryEventBase` itself does not preserve descendants — DAM does not flow to derived types under ILLink.)

`FactoryEventBase` also carries `[FactoryEvent]` with `Inherited = true` so the runtime `FactoryEventTypeRegistry` discovers descendants via attribute scan during the first relay deserialization.

See `references/trimming.md` for the broader trimming setup and the property-walker rules for nested DTO types reachable through event properties.

---

## `RaiseOptions`

| Flag | Meaning |
|------|---------|
| `None` | Default. Server-side handlers run (sequentially, in the caller's scope, awaited); event is captured for client relay. |
| `ServerOnly` | Server-side handlers run as normal; event is NOT relayed to the client (the relay batch will not contain it). |

```csharp
await factoryEvents.Raise(
    new OrderCheckoutCompleted(id, total),
    RaiseOptions.ServerOnly,
    ct);
```

Use `ServerOnly` for server-internal concerns (trigger a downstream process, record an audit row) that the UI does not need to know about. The client still receives a `Relay` invocation for every `[Remote]` call, but the `ServerOnly` event is not in the batch.

### Nested Operations

The `IFactoryEventCollector` is request-scoped. Events raised during nested operations — for example, a child `Insert` running as part of a parent `Save` — are captured by the same collector and relayed to the client along with the parent's events.

### Logical Mode

In Logical (local) mode no collector and no relay are registered — nothing crosses a client/server boundary. Server-side `[FactoryEventHandler<T>]` static handlers still dispatch via the mediator in the caller's scope.

---

## Method Matching Rules

The source generator finds the server-side handler method by signature:

- **`static`** required (instance methods → NF0503 Warning, not dispatched)
- **Return type:** `Task` (not `void`, not `Task<T>`)
- **First non-`[Service]`/non-`CancellationToken` parameter:** type `T`
- **Accessibility:** any (public, internal, private)
- **Count:** exactly one match per `[FactoryEventHandler<T>]` attribute — see diagnostics below

---

## Anti-Patterns

### Raising a Factory Event Outside a Factory Method

```csharp
// WRONG - no collector exists outside a factory operation
var order = await factory.Create(...);
await factoryEvents.Raise(new OrderCheckoutCompleted(order.Id, order.Total));

// RIGHT - raise inside the factory method itself
[Remote, Create]
internal async Task Create(int id, decimal total, [Service] IFactoryEvents events, CancellationToken ct)
{
    Id = id; Total = total;
    await events.Raise(new OrderCheckoutCompleted(id, total), RaiseOptions.None, ct);
}
```

**Why:** The `IFactoryEventCollector` that captures events for client relay is request-scoped and only exists on the server during a factory operation. Events raised outside that scope cannot be relayed.

### Using a Factory Event for Fire-and-Forget Work

```csharp
// WRONG - this blocks the factory method until the email service responds,
// and an email failure will roll back the whole Create operation
[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class OrderCheckoutEmail
{
    internal static async Task SendConfirmation(
        OrderCheckoutCompleted evt,
        [Service] ISmtpClient smtp,
        CancellationToken ct) =>
        await smtp.SendAsync("ops@example.com", "Order placed", $"{evt.OrderId}", ct);
}

// RIGHT - run fire-and-forget work inside the factory method with a fresh scope.
// Snapshot ambient context before Task.Run; re-assign it in the child scope.
[Factory]
internal partial class Order
{
    [Remote, Create]
    internal async Task Create(
        int id,
        decimal total,
        [Service] AppDbContext db,
        [Service] IServiceScopeFactory scopeFactory,
        [Service] ICorrelationContext correlation,
        [Service] IHostApplicationLifetime lifetime,
        CancellationToken ct)
    {
        db.Orders.Add(new OrderEntity(id, total));
        await db.SaveChangesAsync(ct);

        var correlationId = correlation.CorrelationId;
        var stopping = lifetime.ApplicationStopping;

        _ = Task.Run(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<ICorrelationContext>().CorrelationId = correlationId;

            var smtp = scope.ServiceProvider.GetRequiredService<ISmtpClient>();
            await smtp.SendAsync("ops@example.com", "Order placed", $"{id}", stopping);
        }, stopping);
    }
}
```

**Why:** `[FactoryEventHandler<T>]` is for work that must participate in the caller's transaction. Email, webhooks, and other external-IO work should run as fire-and-forget directly inside the factory method, with a fresh DI scope, so they don't block the factory method or fail it. Track the task yourself if you need graceful shutdown — see the v1.5.0 release notes for the pattern.

### Decorating a Handler Class with `[Factory]`

```csharp
// WRONG - two pipelines on the same class
[Factory]
[FactoryEventHandler<OrderCheckoutCompleted>]
public partial class OrderNotifier { ... }

// RIGHT - handler classes are NOT factories
[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class OrderNotifier { ... }
```

**Why:** `[FactoryEventHandler<T>]` runs through a separate generator pipeline from `[Factory]`. Adding `[Factory]` forces the handler class through factory generation where it would need factory methods, interfaces, and registration. Keep handler classes clean.

### Instance-Method Handler on the Client

```csharp
// WRONG - instance method on client emits NF0503 Warning and is silently dropped
[FactoryEventHandler<OrderCheckoutCompleted>]
public sealed partial class CheckoutBannerViewModel
{
    public Task Handle(OrderCheckoutCompleted evt) { /* ... */ return Task.CompletedTask; }
}

// RIGHT - implement IFactoryEventRelay on the client
public sealed class CheckoutRelay : IFactoryEventRelay
{
    public Task Relay(IReadOnlyList<FactoryEventBase> events)
    {
        foreach (var evt in events)
        {
            if (evt is OrderCheckoutCompleted o) { /* ... */ }
        }
        return Task.CompletedTask;
    }
}
```

### Wrong Return Type

```csharp
// WRONG
public static async void Handle(OrderCheckoutCompleted evt) { }            // NF0501 (and async void is bad anyway)
public static Task<string> Handle(OrderCheckoutCompleted evt) { ... }      // NF0501

// RIGHT
public static Task Handle(OrderCheckoutCompleted evt) => Task.CompletedTask;
```

**Why:** The dispatcher awaits `Task`. `void` cannot be awaited, and `Task<T>` would force every handler to return a value with no clear meaning.

### Using a Class or Interface as the Event Type

```csharp
// WRONG - not a record, not inheriting FactoryEventBase
public class OrderCheckoutCompleted { public int OrderId { get; set; } }

// RIGHT
public record OrderCheckoutCompleted(int OrderId, decimal Total) : FactoryEventBase;
```

**Why:** Events need structural equality (records) and the `FactoryEventBase` marker so the generator and runtime registry can identify them. Interfaces cannot be used as the generic argument of `[FactoryEventHandler<T>]` because the runtime registry needs a concrete type for deserialization.

### Depending on Server-Side Handler Ordering

```csharp
// WRONG - assumes HandlerA runs before HandlerB
[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class HandlerA
{
    internal static Task Write(OrderCheckoutCompleted evt, [Service] AppDbContext db, CancellationToken ct)
    {
        db.Orders.First(o => o.Id == evt.OrderId).Stage = "A-done";
        return Task.CompletedTask;
    }
}

[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class HandlerB
{
    internal static Task Read(OrderCheckoutCompleted evt, [Service] AppDbContext db, CancellationToken ct)
    {
        var stage = db.Orders.First(o => o.Id == evt.OrderId).Stage;
        // Assumes A has already written "A-done" — NOT GUARANTEED
        return Task.CompletedTask;
    }
}
```

**Why:** Handlers run sequentially but the order is **unspecified**. If one handler needs another's side effect, put the logic in one handler or model the dependency explicitly (e.g., a second event raised by the first handler).

---

## Diagnostics

| ID | Severity | Description |
|----|----------|-------------|
| NF0501 | Error | No matching `static` handler method found for `[FactoryEventHandler<T>]`. The class declares at least one static method but none returns `Task` with `T` as the first non-`[Service]`/non-`CancellationToken` parameter. Fixed by either correcting the signature or removing the static candidate. |
| NF0502 | Error | Multiple matching `static` handler methods found for `[FactoryEventHandler<T>]`. Remove the extras or split into separate handler classes. |
| NF0503 | Warning | Instance-method handler in a `[FactoryEventHandler<T>]` class is ignored at runtime. Make the method `static` (server) or implement `IFactoryEventRelay` on the class (client). |
| NF0504 | Warning | One class declares `[FactoryEventHandler<T>]` for the **same** event type more than once. Stacking is for several event *types*; only the first declaration registers — its `DispatchPhase` and its `Coalesce` flag. The message names the surviving registration. Remove the duplicate. |
| NF0505 | Warning | `Coalesce = true` on an `Immediate`-declared registration. Immediate dispatches are never queued — nothing to coalesce; the flag is inert (the registration is still emitted). Remove the flag or defer the handler to `AfterFlush`/`AfterCommit`. |

---

## Runtime Log Events

| Event ID | Level | Source | Meaning |
|----------|-------|--------|---------|
| 3008 | Error | `MakeRemoteDelegateRequest` | `IFactoryEventRelay.Relay` threw — exception isolated from factory caller. |
| 3009 | Error | `MakeRemoteDelegateRequest` | `FactoryEventDeserializer` threw (typically `UnknownFactoryEventTypeException`); relay batch aborted. |
| 3011 | Warning | `NoOpFactoryEventRelay` | First non-empty batch arrived at the no-op default (consumer forgot to register a relay). One-shot per process. |
| 3012 | Warning | `FactoryEventTypeRegistry` | Two assemblies declared a `FactoryEventBase` descendant with the same `FullName`; the second was dropped. |
| 9001 | Debug | Phase scheduler | A handler registered at a non-`Immediate` `DispatchPhase` was deferred instead of dispatched at `Raise` time. |
| 9002 | Debug | Phase scheduler | A phase drain completed, reporting how many dispatches ran. |
| 9003 | Error | Phase scheduler | A deferred handler threw during a **post-completion** drain; swallowed (including handler-internal `OperationCanceledException`), remaining queued handlers still run. In-transaction drains propagate instead — this never fires for them. |
| 9004 | Debug | Phase dispatcher | An event with a phased handler was raised in a scope with no phase scheduler; dispatched immediately rather than dropped. |
| 9005 | Debug | Phase dispatcher | An event with a phased handler was raised while no entry factory call was active in the scope; dispatched immediately rather than queued into a drain nobody owns. |
| 9006 | Debug | Phase scheduler | A failed entry call discarded its deferred dispatches without running them. |
| 9007 | Warning | Phase scheduler | An `AfterFlush` dispatch the consumer never drained ran at the `AfterCommit` point instead (fail-open). One warning per dispatch, naming the event type — call `IFactoryEventPhaseCoordinator.DrainAsync(DispatchPhase.AfterFlush)` **from inside the factory method body**, between your flush and your commit. A drain called from outside the factory call never reaches this work, because the queue only exists while the entry call is active (9009 fires there). Otherwise register the handler at a different phase. A coalesced survivor warns if any absorbed raise would have. |
| 9008 | Debug | Phase scheduler | A raise for a `Coalesce = true` handler collapsed into an identical pending dispatch — logged instead of a second 9001; the 9002/9006 counts reflect the collapsed queue. |
| 9009 | Debug | Phase coordinator | `IFactoryEventPhaseCoordinator.DrainAsync` was called with no entry factory call active, so nothing was drained. Usually means the drain wraps the factory call from outside rather than running inside the factory method body. Debug, not Warning — draining a scope with no factory work in flight is also correct. |

---

## DI Registration

Registered automatically by `AddRemoteFactoryServices` based on `NeatooFactory` mode:

| Service | Server | Remote (client) | Logical |
|---------|--------|-----------------|---------|
| `IFactoryEvents` | Scoped | Scoped (sends to server) | Scoped |
| `IFactoryEventCollector` | Scoped | — | — |
| `IFactoryEventRelay` | — | Singleton (`NoOpFactoryEventRelay` default; consumer overrides via standard DI) | — |
| `IFactoryEventPhaseCoordinator` | Scoped | — | Scoped |

The phase queue and coordinator exist wherever handlers dispatch (Server and Logical). Remote mode has no handlers to drain — `[Service]`-injecting the coordinator on a non-`[Remote]` method there fails with the standard not-registered exception, the same client/server-boundary contract as any server-only service.

Server-side static handler classes register themselves into `FactoryEventHandlerRegistry` at startup via the generated `FactoryServiceRegistrar`. Client-side relay is the consumer's `IFactoryEventRelay` implementation.

---

## When to Use What

| Situation | Use |
|-----------|-----|
| Handler needs to participate in the factory's DB transaction | `[FactoryEventHandler<T>]` static method |
| Handler failure should roll back the factory operation | `[FactoryEventHandler<T>]` static method (`Immediate`, or `AfterFlush` drained before the commit) |
| Handler needs database-generated state (identity keys) but must still be able to roll the save back | `[FactoryEventHandler<T>(DispatchPhase.AfterFlush)]` + `IFactoryEventPhaseCoordinator.DrainAsync(AfterFlush)` between the factory body's flush and commit |
| Read-only projection / cache refresh that must never fail the save | `[FactoryEventHandler<T>(DispatchPhase.AfterCommit)]` — framework-drained after the entry call succeeds, exceptions logged and swallowed |
| One save raises the same event N times and the projection recomputes N times | Add `Coalesce = true` to the deferred handler's attribute — identical queued dispatches collapse to one per drain |
| Client UI needs to update in response to a server-side event | `IFactoryEventRelay` implementation that fans events to your UI / aggregator |
| Server-internal transactional event the UI doesn't care about | `[FactoryEventHandler<T>]` static + `RaiseOptions.ServerOnly` |
| Fire-and-forget external IO (email, webhook, queue publish) | `Task.Run` inside the factory method with a fresh scope from `IServiceScopeFactory.CreateScope()` — RemoteFactory does not own this abstraction |
| Long-running work that must not block the factory response | Same as above — manual `Task.Run` + child scope |
| Both server-side transactional write AND client UI update for the same event | One event type; a `[FactoryEventHandler<T>]` static handler for the write, a single `IFactoryEventRelay` on the client that switches on the event type |
