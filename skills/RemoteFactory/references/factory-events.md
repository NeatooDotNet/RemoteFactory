# Factory Events

RemoteFactory has one event-shaped abstraction. An operation raises a typed event with `IFactoryEvents.Raise`; server-side `static` handlers on a `[FactoryEventHandler<T>]` class run in the caller's DI scope; and the events the call raised travel back to the client, where a consumer-implemented `IFactoryEventRelay` receives them. Each handler declares *when* it runs with the attribute's optional `DispatchPhase`; the default, `Immediate`, is the contract below.

| | `Immediate` (the default) |
|---|---|
| Trigger | `IFactoryEvents.Raise(...)` inside an operation, through a `[Service] IFactoryEvents` |
| Server scope | The caller's: the same `DbContext`, the same transaction, its staged (unflushed) state visible |
| Server dispatch | Sequential and awaited: `Raise` returns after every `Immediate` handler |
| Server exceptions | Propagate to the raiser and abort the chain; the transaction can roll back |
| Client | `IFactoryEventRelay.Relay(...)`, once per remote round trip, after the call's result has reached the caller; exceptions caught and logged |
| Cancellation | The token passed to `Raise` |
| Deferred phases | `AfterFlush` and `AfterCommit` handlers are queued at `Raise` and run at their drain points |
| Use for | Transactional domain events on the server; event-aggregator delivery to the client UI |

**Fire-and-forget external IO** - email, a webhook, a queue publish - is not a factory event: the handler would hold the operation open until it finished, and its failure would roll the operation back. Run it with `Task.Run` inside the operation, with a new scope from `IServiceScopeFactory`, copying any ambient context (correlation ID, tenant) into the new scope first. RemoteFactory does not own that pattern.

## The server-side model

At the `Immediate` phase every dispatch obeys three rules:

1. **Shared scope.** Handlers resolve `[Service]` parameters from the caller's `IServiceProvider`. A scoped `DbContext` in the operation and in the handler is one instance and one transaction; a transient service is a new instance. The handler sees the operation's staged state - a projection that queries the database here reads the world without the pending writes, which is what the deferred phases are for.
2. **Sequential.** Handlers run one after another, in unspecified order. A `DbContext` is not thread-safe, so they cannot run in parallel.
3. **Awaited.** `Raise` returns after every `Immediate` handler. Across the wire, the HTTP call stays open until every server-side handler has finished - for deferred phases, through the entry call's completion drain - and a server exception reaches the client.

`AfterFlush` and `AfterCommit` handlers keep rules 1 and 2 but run later, with different failure semantics.

## Raising an event

An event is a record inheriting `FactoryEventBase`:

<!-- snippet: events-event -->
<a id='snippet-events-event'></a>
```cs
public record OrderCheckoutCompleted(int OrderId, decimal Total) : FactoryEventBase;
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs#L71-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-event' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Raise it inside the operation it belongs to. An event says something happened, so a write (`[Insert]`, `[Update]`, `[Delete]`) or a command (`[Execute]`) raises it; a `[Create]` builds a new object and nothing has happened yet:

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

`Raise<T>(T factoryEvent, RaiseOptions options = RaiseOptions.None, CancellationToken cancellationToken = default)`, where `T : FactoryEventBase`. Pass the operation's token through; handlers that declare a `CancellationToken` receive it.

## Server-side handlers

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

The generator registers the class in `FactoryEventHandlerRegistry` and finds its handler method by signature:

- `static` (an instance method is NF0503 and is never dispatched);
- returns `Task`, not `void` or `Task<T>`;
- its first parameter that is neither `[Service]` nor `CancellationToken` is `T`;
- any accessibility;
- exactly one match: none is NF0501, several is NF0502.

Several classes may handle one event, and all of them run. One class may handle several event types by stacking attributes, one matching method per type and each attribute with its own phase and `Coalesce`; the same type twice on one class is NF0504, and only the first registers. A handler class is not a factory: `[Factory]` on it puts it through the wrong generator.

## Dispatch phases

Design handles one event at all three phases:

<!-- snippet: events-phase-handlers -->
<a id='snippet-events-phase-handlers'></a>
```cs
/// <summary>
/// Immediate (no attribute argument — the default). Runs at <c>Raise</c>, inside
/// the caller's transaction, seeing staged state. A throw here aborts
/// <c>Finalize</c> itself — this handler is atomic with the save.
/// </summary>
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
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventPhasesPattern.cs#L155-L208' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-phase-handlers' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

| Phase | Runs at | Sees | Exceptions |
|---|---|---|---|
| `Immediate` | `Raise` | Staged state, mid-transaction | Propagate to the raiser |
| `AfterFlush` | The operation's `IFactoryEventPhaseCoordinator.DrainAsync(AfterFlush)` call, between its flush and its commit | Flushed state (generated keys, computed columns), transaction open | Propagate to the drain call; can still roll back |
| `AfterCommit` | The framework's drain after the entry call succeeds | Committed state, no transaction | Logged (9003) and swallowed; the remaining handlers still run |

RemoteFactory never flushes and never commits. The phase names describe intent at the drain points the framework exposes; your persistence code decides where "after flush" is by calling the drain.

### Draining `AfterFlush`

Inject the coordinator as a `[Service]` - it is server-side, beside the persistence services - and drain inside the operation, at the flush-and-commit seam:

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

Only `AfterFlush` is drained by the consumer; `DrainAsync` throws `ArgumentOutOfRangeException` for another phase. The drain must run inside the operation: with no entry call active in the scope it drains nothing (9009), so a transaction helper that wraps the factory call from outside reaches none of the queued work. Compose the helper into the operation instead.

### The phase contract

- **Failure belongs to the drain point.** In-transaction drains (`Immediate` dispatch, the `AfterFlush` drain) propagate; the post-completion drain logs and swallows, including a handler's own `OperationCanceledException`.
- **Ordering is per drain point.** For events raised before a drain point, every `Immediate` handler finishes before any `AfterFlush` handler, which finish before any `AfterCommit` handler. Order within a phase is unspecified.
- **A failed entry call discards queued work** (9006): deferred handlers never see a failed operation. Its `Immediate` handlers already ran, inside the transaction the rollback undoes.
- **Undrained `AfterFlush` work fails open.** It runs at the `AfterCommit` point instead, under swallow semantics, with Warning 9007 naming the event type.
- **Deferred handlers still relay.** Events they raise join the same response's relay batch.
- **One factory call per DI scope at a time.** Phase queues are per scope; give concurrent flows their own scopes, as an ASP.NET Core request already is.
- **Prefer async operation bodies with phased handlers.** A synchronous body that raised phased work makes the completion drain block, which deadlocks under a captured `SynchronizationContext` (Logical mode inside a Blazor Server circuit) if a handler awaits without `ConfigureAwait(false)`.
- **An event raised outside any factory call** dispatches its phased handlers immediately, with Debug 9005.

### Coalescing identical dispatches

A save that touches N items often raises the same event N times, and a deferred projection recomputes N times for one change. `Coalesce = true` collapses identical queued dispatches into one per drain:

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

- **Identity is `Equals`:** the same registration, an `Equals`-equal event, the same `RaiseOptions`. A record with a reference-typed member (a `List<T>`, an entity) never compares equal, so coalescing silently does nothing for it; prefer value-only payloads.
- **Pending work only:** a dispatch a running drain already took is history; a later raise starts fresh. Each collapse logs Debug 9008.
- **Inert where nothing queues:** on an `Immediate` handler it is NF0505.
- **The relay is untouched:** every raise is still relayed.

### Choosing a phase

| The handler | Phase |
|---|---|
| Must be atomic with the save | `Immediate` |
| Needs database-generated state while the transaction can still abort | `AfterFlush`, drained between flush and commit |
| Is a read-only projection or cache refresh that must never fail the save | `AfterCommit` |
| Recomputes identically for each value-identical raise | A deferred phase with `Coalesce = true` |

## The client relay

A remote call's response carries the events that call raised. The client hands them to its `IFactoryEventRelay` once per round trip, on a separate continuation, strictly after the call's result has reached the caller - the caller has already assigned it when the relay runs. An empty batch still produces one `Relay` call, which can serve as a "the call returned" signal.

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
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs#L331-L354' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-relay' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: events-relay-register -->
<a id='snippet-events-relay-register'></a>
```cs
// Consumer replaces the NoOp default by registering their own relay.
services.AddSingleton<IFactoryEventRelay>(aggregator);
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/FactoryEventRelayTests.cs#L23-L26' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-relay-register' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`AddNeatooRemoteFactory` registers a no-op default with `TryAddSingleton`, so the consumer's registration wins in either order. The consumer owns marshaling to the UI thread, the bridge to its aggregator, and fan-out by event type. A `Relay` exception is logged (3008) and never reaches the caller; a batch that cannot be deserialized is logged (3009) and not relayed.

**The no-op default drops every batch.** It logs Warning 3011 once, on its first non-empty batch. Seeing it means no relay is registered.

### `RaiseOptions.ServerOnly`

<!-- snippet: events-serveronly -->
<a id='snippet-events-serveronly'></a>
```cs
[Remote, Update]
internal async Task Update([Service] IFactoryEvents factoryEvents, CancellationToken cancellationToken)
{
    // ServerOnly: server handlers run, client does NOT receive the event
    await factoryEvents.Raise(new OrderCheckoutCompleted(Id, Total), RaiseOptions.ServerOnly, cancellationToken);
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs#L182-L189' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-serveronly' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Handlers run as usual; the event is left out of the relay batch. Use it for server-internal concerns the UI does not need.

### A client-initiated raise

<!-- snippet: events-client-raise -->
<a id='snippet-events-client-raise'></a>
```cs
var events = client.ServiceProvider.GetRequiredService<IFactoryEvents>();
await events.Raise(new ClientRaisedNotice(88));
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/FactoryEventRelayTests.cs#L131-L134' title='Snippet source file'>snippet source</a> | <a href='#snippet-events-client-raise' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

In Remote mode `IFactoryEvents` sends the event to the server as its own round trip; nothing dispatches on the client. Every handler runs there before the response is written, and the batch that comes back contains the caller's own event, plus anything its handlers raised - the only way the client's relay sees it. `ServerOnly` empties the batch. It is not fire-and-forget: `Raise` returns after every server handler, and a handler exception reaches the client.

When the event belongs to an operation, raise it inside the operation: a client raise is a separate request scope, shares no transaction with the call before it, and cannot roll it back.

### Nested operations and Logical mode

The collector is request-scoped, so events raised by nested operations - a child's `Insert` inside its root's `Save` - relay with the root's. In Logical mode there is no collector and no relay; server-side handlers still dispatch in the caller's scope.

## Trimming

The generator emits a per-assembly registrar that preserves every concrete, accessible `FactoryEventBase` descendant and its property graph under `PublishTrimmed=true`, with no annotation per event. [trimming.md](trimming.md).

## Anti-patterns

```csharp
// WRONG - two pipelines on one class
[Factory]
[FactoryEventHandler<OrderPlacedEvent>]
public partial class OrderNotifyHandlers { ... }
```

```csharp
// WRONG - an instance handler: NF0503, never dispatched
[FactoryEventHandler<OrderCheckoutCompleted>]
public sealed partial class CheckoutBanner
{
    public Task Handle(OrderCheckoutCompleted evt) => Task.CompletedTask;
}
```

```csharp
// WRONG - NF0501: the dispatcher awaits Task
public static async void Handle(OrderCheckoutCompleted evt) { }
```

```csharp
// WRONG - not a record, not a FactoryEventBase
public class OrderCheckoutCompleted { public int OrderId { get; set; } }
```

- A handler class is `static partial`, with `[FactoryEventHandler<T>]` and no `[Factory]`.
- The client hears events through `IFactoryEventRelay`, not through a handler class.
- A handler returns `Task`.
- An event is a record that inherits `FactoryEventBase`; the runtime registry needs a concrete type to deserialize it.
- Never rely on one handler's side effect in another: the order is unspecified. Put the logic in one handler, or raise a second event from the first.

## Diagnostics

| ID | Severity | Meaning |
|---|---|---|
| NF0501 | Error | No matching `static` handler method for `[FactoryEventHandler<T>]` |
| NF0502 | Error | More than one matching `static` handler method |
| NF0503 | Warning | An instance handler method; ignored at runtime |
| NF0504 | Warning | The same event type declared twice on one class; only the first registers |
| NF0505 | Warning | `Coalesce = true` on an `Immediate` handler; inert |

## Runtime log events

| Event | Level | Meaning |
|---|---|---|
| 3008 | Error | `IFactoryEventRelay.Relay` threw; isolated from the caller |
| 3009 | Error | The relay batch could not be deserialized; not relayed |
| 3011 | Warning | The no-op relay got its first non-empty batch; once per process |
| 3012 | Warning | Two assemblies declared an event with the same full name; the second was dropped |
| 9001 | Debug | A phased handler was deferred at `Raise` |
| 9002 | Debug | A phase drain completed, with its count |
| 9003 | Error | A deferred handler threw in the post-completion drain; swallowed |
| 9004 | Debug | A phased event was raised in a scope with no scheduler; dispatched immediately |
| 9005 | Debug | A phased event was raised with no entry call active; dispatched immediately |
| 9006 | Debug | A failed entry call discarded its deferred dispatches |
| 9007 | Warning | An undrained `AfterFlush` dispatch ran at the `AfterCommit` point |
| 9008 | Debug | A `Coalesce` raise collapsed into a pending dispatch |
| 9009 | Debug | `DrainAsync` ran with no entry call active; nothing drained |

## DI registration

| Service | Server | Remote (client) | Logical |
|---|---|---|---|
| `IFactoryEvents` | Scoped | Scoped, sends to the server | Scoped |
| `IFactoryEventCollector` | Scoped | - | - |
| `IFactoryEventRelay` | - | Singleton, the no-op default until the consumer registers one | - |
| `IFactoryEventPhaseCoordinator` | Scoped | - | Scoped |

The coordinator exists where handlers dispatch. In Remote mode a non-`[Remote]` method that takes it as a `[Service]` fails with the not-registered exception, as any server-only service does.

## When to use what

| Situation | Use |
|---|---|
| The handler writes in the operation's transaction, and its failure should roll the operation back | `[FactoryEventHandler<T>]`, `Immediate` |
| It needs generated keys but must still be able to roll back | `AfterFlush`, drained between flush and commit |
| A projection that must never fail the save | `AfterCommit` |
| One save raises the same event N times | A deferred phase with `Coalesce = true` |
| The client UI reacts to a server event | An `IFactoryEventRelay` that fans out to the UI |
| A server-internal event the UI does not need | `RaiseOptions.ServerOnly` |
| Email, a webhook, a queue, long-running work | `Task.Run` inside the operation with a new scope; not a factory event |
| A transactional write and a UI update for one event | One event type: a server handler for the write, the client relay for the UI |
