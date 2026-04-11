# Factory Events

RemoteFactory has two distinct event features, named separately because they have different execution models.

| Feature | `[Event]` (attribute on method) | `[FactoryEventHandler<T>]` (attribute on class) |
|---------|--------------------------------|-------------------------------------------------|
| Page | [Events](events.md) | This page |
| Trigger | Direct delegate invocation from anywhere | `IFactoryEvents.Raise(...)` inside a factory method |
| DI scope | Isolated (new scope per handler) | **Shared with the caller** (same `DbContext`, same transaction) |
| Dispatch | Fire-and-forget via `Task.Run`, tracked by `IEventTracker` | **Sequential, awaited** — `Raise` returns only after all handlers complete |
| Exceptions | Swallowed / logged | **Propagate to the caller** — a throwing handler aborts the chain |
| Cancellation | `IHostApplicationLifetime.ApplicationStopping` | Caller's `CancellationToken` (threaded through `Raise`) |
| Client relay | No | Yes (instance methods receive events after factory result returns) |
| Use for | Notifications, emails, webhooks, audit sinks | **Transactional domain events** — handlers that must participate in the caller's DB transaction |

This page covers the `[FactoryEventHandler<T>]` + `IFactoryEvents.Raise` pattern. For fire-and-forget work, see [Events](events.md).

---

## The Execution Model

`IFactoryEvents.Raise<T>()` is a mediator that dispatches events to every handler decorated with `[FactoryEventHandler<T>]`. The three invariants that define this dispatch model are:

1. **Shared scope.** Handlers resolve `[Service]` dependencies from the caller's `IServiceProvider`. A `DbContext` injected into the factory method and a `DbContext` injected into the handler are the same instance and the same transaction.
2. **Sequential.** Handlers run one after another in unspecified order. Callers must not rely on a specific ordering. A `DbContext` is not thread-safe, so handlers cannot run in parallel.
3. **Awaited.** `Raise<T>()` returns only after every handler has completed. A handler exception aborts the remaining handlers and propagates to the caller so the transaction can roll back. Across the client/server boundary, the HTTP call stays open until all server-side handlers finish.

Handlers do not know about each other, and the raising code does not know which handlers exist.

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

### Why Not [Event]?

`[Event]` and `[FactoryEventHandler<T>]` serve different needs and have different execution models.

- **`[Event]`** is imperative, detached, and fire-and-forget. The caller knows the delegate name and invokes it directly; each handler runs in its own isolated scope via `Task.Run`, tracked by `IEventTracker` for graceful shutdown. Handler exceptions never affect the caller. Use it for notifications, emails, webhooks, audit sinks to external systems — anything that should survive (or shouldn't block) the caller's work.
- **`[FactoryEventHandler<T>]`** is declarative, transactional, and awaited. The raiser publishes a typed event via `IFactoryEvents.Raise`; handlers share the caller's DI scope, run sequentially, and propagate exceptions. Use it for domain events that must participate in the caller's DB transaction — handlers that touch the same `DbContext` as the aggregate that raised the event.

If your handler needs the factory's transaction, use `[FactoryEventHandler<T>]`. If your handler talks to an external system that should never block or fail the aggregate save, use `[Event]`.

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
  |                                 |    - every server-side [FactoryEventHandler<T>] static
  |                                 |      handler runs sequentially in the same scope;
  |                                 |      Raise returns only after all handlers complete
  |                                 |    - event is also captured in IFactoryEventCollector
  |                                 |
  |        RemoteResponseDto        | 4. HandleRemoteDelegateRequest attaches
  |   { Json, RelayedEvents: [..] } |    collected events to the response
  |<--------------------------------|
  |                                 |
  | 5. Factory result returned to caller
  | 6. IFactoryEventRelay dispatches RelayedEvents to
  |    registered instance handlers (client-side relay)
```

The server awaits every `[FactoryEventHandler<T>]` before serializing the response, so a server handler exception propagates back to the client. On the client, the factory result is returned to the caller **before** relayed events are dispatched to client-side relay handlers.

### Client-Side Handler (Instance Method)

A `[FactoryEventHandler<T>]` class with an **instance** matching method is treated as a client-side relay handler. The class registers itself with `IFactoryEventRelay` and receives events on its existing instance (no DI scope created per dispatch).

```csharp
[FactoryEventHandler<OrderCheckoutCompleted>]
public sealed partial class OrderCheckoutViewModel : IDisposable
{
    private readonly IFactoryEventRelay _relay;

    public OrderCheckoutViewModel(IFactoryEventRelay relay)
    {
        _relay = relay;
        _relay.Register(this);
    }

    public Task HandleCheckout(OrderCheckoutCompleted evt)
    {
        // Update UI state, raise a PropertyChanged, queue a toast, etc.
        return Task.CompletedTask;
    }

    public void Dispose() => _relay.Unregister(this);
}
```

### IFactoryEventRelay

```csharp
public interface IFactoryEventRelay
{
    void Register(object handler);
    void Unregister(object handler);
}
```

- Singleton, lives for the client application lifetime.
- Holds handler instances via `WeakReference` — a handler that is garbage collected without calling `Unregister` is silently removed. No memory leak.
- Thread-safe. Multiple factory operations completing concurrently can dispatch events in parallel.
- Handler exceptions are caught and logged. They never propagate to the factory caller.
- If no handler is registered for a relayed event type, the event is silently dropped.
- If multiple handlers are registered for the same event type, all of them are invoked.

Register from a constructor and unregister from `Dispose` is the standard pattern. In Blazor, this works naturally in components that implement `IDisposable`.

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

When zero events are captured, `RelayedEvents` is `null` (not an empty list). This preserves backward-compatible JSON payloads for responses with no events.

Event types are resolved on the client by `TypeFullName` (string key), not by `Type.GetType()` — the source generator produces a typed deserializer per handled event type. This is trimming-safe: `RelayedFactoryEvent` and `List<RelayedFactoryEvent>` are registered with `NeatooTransportJsonContext` so they survive IL trimming.

---

## Decision Guide

| Question | Answer |
|----------|--------|
| How do I handle a factory event on the **server**? | `[FactoryEventHandler<T>]` class with a `static` matching method |
| How do I handle a factory event on the **client**? | `[FactoryEventHandler<T>]` class with an **instance** matching method, register via `IFactoryEventRelay` |
| Does a `[FactoryEventHandler<T>]` class need `[Factory]`? | No — it is a separate generator pipeline |
| Can one class handle multiple event types? | Yes — stack multiple `[FactoryEventHandler<T>]` attributes |
| How do I stop an event from reaching the client? | Pass `RaiseOptions.ServerOnly` to `Raise` |
| Where must events be raised? | Inside a factory method, via `[Service] IFactoryEvents` |
| What type must events inherit from? | `FactoryEventBase` (records only) |
| What must the handler method return? | `Task` |
| What is the required parameter order? | `T evt` first (after any `[Service]`/`CancellationToken`), services and `CancellationToken` anywhere |

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

---

## DI Registration

Registered automatically by `AddRemoteFactoryServices` based on `FactoryMode`:

| Service | Server mode | Remote (client) mode | Logical mode |
|---------|-------------|----------------------|--------------|
| `IFactoryEvents` | Scoped | Scoped (no relay) | Scoped |
| `IFactoryEventCollector` | Scoped | — | — |
| `IFactoryEventRelay` | — | Singleton | — |

Static handler classes (`[FactoryEventHandler<T>]` with a static method) register themselves into `FactoryEventHandlerRegistry` at startup via the generated `FactoryServiceRegistrar`. Client-relay handler classes register their dispatch delegate into `FactoryEventRelayRegistry` the same way.

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

// --------------- Client: instance handler updates the UI ---------------
[FactoryEventHandler<OrderCheckoutCompleted>]
public sealed partial class CheckoutBannerViewModel : IDisposable
{
    private readonly IFactoryEventRelay _relay;
    public string? LastMessage { get; private set; }

    public CheckoutBannerViewModel(IFactoryEventRelay relay)
    {
        _relay = relay;
        _relay.Register(this);
    }

    public Task ShowCheckoutMessage(OrderCheckoutCompleted evt)
    {
        LastMessage = $"Order {evt.OrderId} completed: {evt.Total:C}";
        return Task.CompletedTask;
    }

    public void Dispose() => _relay.Unregister(this);
}
```

---

## Next Steps

- [Events](events.md) — The `[Event]` fire-and-forget pattern (different feature)
- [Attributes Reference](attributes-reference.md) — `[FactoryEventHandler<T>]` quick reference
- [Client-Server Architecture](client-server-architecture.md) — How `[Remote]` and the transport layer fit together
- [Serialization](serialization.md) — How `FactoryEventBase` and `RelayedFactoryEvent` cross the wire
