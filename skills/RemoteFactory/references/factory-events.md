# Factory Events — `[FactoryEventHandler<T>]` + `IFactoryEvents.Raise`

This is a **separate feature** from the `[Event]` method attribute described in `references/static-factory.md`. Both publish domain events, but they have **different execution models** and exist for different problems:

| Feature | `[Event]` method attribute | `[FactoryEventHandler<T>]` class attribute |
|---------|---------------------------|---------------------------------------------|
| Reference | `references/static-factory.md` | This file |
| Trigger | Direct delegate invocation from anywhere | `IFactoryEvents.Raise(...)` inside a factory method |
| DI scope per handler | **Isolated** — new scope via `IServiceScopeFactory` | **Shared with the caller** — same `DbContext`, same transaction |
| Dispatch | Fire-and-forget via `Task.Run`, tracked by `IEventTracker` for graceful shutdown | **Sequential, awaited** — `Raise` returns only after every handler completes |
| Handler exceptions | Swallowed / logged, never affect the caller | **Propagate to the caller** — abort the chain, roll back the transaction |
| Cancellation token | `IHostApplicationLifetime.ApplicationStopping` | The caller's `CancellationToken`, passed through `Raise` |
| Client relay | No | Yes (instance methods get events after factory result returns) |
| Use for | Notifications, emails, webhooks, audit sinks to external systems, anything you don't want to block the caller | **Transactional domain events** — handlers that must participate in the factory's DB transaction |

**Rule of thumb:** if the handler touches the same `DbContext` as the factory method and its failure should roll the factory operation back, use `[FactoryEventHandler<T>]`. Otherwise use `[Event]`.

---

## The Execution Model — Three Invariants

Every `[FactoryEventHandler<T>]` dispatch obeys three rules:

1. **Shared scope.** Handlers resolve `[Service]` dependencies from the caller's `IServiceProvider`. A `DbContext` injected into the factory method and a `DbContext` injected into the handler are the same instance, so both participate in the same transaction.
2. **Sequential.** Handlers run one after another in unspecified order. Callers must not rely on a specific ordering. A `DbContext` is not thread-safe, so parallel dispatch is not possible.
3. **Awaited.** `IFactoryEvents.Raise<T>()` returns only after every handler has completed. A handler exception aborts the remaining handlers and propagates to the caller. Across the client/server boundary the HTTP call stays open until every server-side handler finishes, and a server exception surfaces on the client.

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

        // Every [FactoryEventHandler<OrderCheckoutCompleted>] handler runs here,
        // in this scope, sharing `db`. A throwing handler aborts this method and
        // rolls back everything saved above.
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

- Return type must be `Task` (not `void`, not `Task<T>`)
- First non-`[Service]`/non-`CancellationToken` parameter must be of type `T`
- Any accessibility is allowed
- Exactly one match required — `NF0501` if none, `NF0502` if multiple

### Multiple Handlers for the Same Event

Any number of classes can handle the same event type. All of them run in the caller's scope, sequentially, in unspecified order, before `Raise` returns:

```csharp
[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class OrderCheckoutInventory
{
    internal static async Task DecrementStock(
        OrderCheckoutCompleted evt,
        [Service] AppDbContext db,
        CancellationToken ct)
    {
        // Another handler writing through the same DbContext.
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

One method per attribute.

---

## The Client Relay

When a factory operation runs on the server and raises events with default options, those events are captured by a request-scoped `IFactoryEventCollector` and travel back to the client piggybacked on the HTTP response (`RemoteResponseDto.RelayedEvents`).

Data flow:

```
CLIENT                           SERVER
  | 1. factory.Create(...)        |
  |------------------------------>| 2. Create runs in the request scope
  |                               | 3. await events.Raise(new OrderCheckoutCompleted(...))
  |                               |    - every server-side [FactoryEventHandler<T>]
  |                               |      static handler runs sequentially in the same
  |                               |      scope, awaited; Raise returns only after all
  |                               |      handlers complete (or one throws)
  |                               |    - event is also captured in IFactoryEventCollector
  |      RemoteResponseDto        | 4. HandleRemoteDelegateRequest attaches
  |  { Json, RelayedEvents: [..] }|    collected events to the response
  |<------------------------------|
  | 5. Factory result returned to caller
  | 6. IFactoryEventRelay dispatches RelayedEvents to
  |    registered client-side instance handlers
```

The server awaits every server-side handler **before** serializing the response, so a server handler exception propagates back as an HTTP error and rethrows on the client. On the client the factory result is returned to the caller first, and then relayed events are dispatched to client-side relay handlers.

### Client-Side Handler (Instance Method)

A `[FactoryEventHandler<T>]` class with an **instance** matching method is a client-side relay handler. The class registers itself with `IFactoryEventRelay` in its constructor and unregisters in `Dispose`:

```csharp
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

### `IFactoryEventRelay`

```csharp
public interface IFactoryEventRelay
{
    void Register(object handler);
    void Unregister(object handler);
}
```

- Singleton, lives for the client application lifetime.
- Holds handler instances via `WeakReference`. A handler garbage-collected without calling `Unregister` is silently removed — no memory leak.
- Thread-safe.
- Client-side handler exceptions are caught and logged; they never propagate to the factory caller.
- No registered handler for a relayed event type → silently dropped.
- Multiple registered handlers for the same event type → all invoked.

Register from a constructor and unregister from `Dispose`. In Blazor, components implementing `IDisposable` work naturally.

---

## `RaiseOptions`

| Flag | Meaning |
|------|---------|
| `None` | Default. Server-side handlers run (sequentially, in the caller's scope, awaited); event is captured for client relay. |
| `ServerOnly` | Server-side handlers run as normal; event is NOT relayed to the client. |

```csharp
await factoryEvents.Raise(
    new OrderCheckoutCompleted(id, total),
    RaiseOptions.ServerOnly,
    ct);
```

Use `ServerOnly` for server-internal concerns (trigger a downstream process, record an audit row) that the UI does not need to know about.

### Nested Operations

The `IFactoryEventCollector` is request-scoped. Events raised during nested operations — for example, a child `Insert` running as part of a parent `Save` — are captured by the same collector and relayed to the client along with the parent's events.

### Logical Mode

In Logical (local) mode no collector and no relay are registered — nothing crosses a client/server boundary. Handlers still dispatch via the mediator in the caller's scope.

---

## Method Matching Rules

The source generator finds the handler method by signature:

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

// RIGHT - use an [Event] delegate method for email; it runs fire-and-forget in
// an isolated scope and its failure cannot roll back the transaction
[Factory]
internal static partial class EmailEvents
{
    [Event]
    internal static async Task SendOrderConfirmation(int orderId, decimal total, [Service] ISmtpClient smtp, CancellationToken ct) =>
        await smtp.SendAsync("ops@example.com", "Order placed", $"{orderId}", ct);
}
```

**Why:** `[FactoryEventHandler<T>]` is for work that must participate in the caller's transaction. Email, webhooks, and other external-IO work should use `[Event]` so they don't block the factory method or fail it.

### Decorating a Handler Class with `[Factory]`

```csharp
// WRONG - two pipelines on the same class
[Factory]
[FactoryEventHandler<OrderCheckoutCompleted>]
public partial class OrderNotifier { ... }

// RIGHT - handler classes are NOT factories
[FactoryEventHandler<OrderCheckoutCompleted>]
public partial class OrderNotifier { ... }
```

**Why:** `[FactoryEventHandler<T>]` runs through a separate generator pipeline from `[Factory]`. Adding `[Factory]` forces the handler class through factory generation where it would need factory methods, interfaces, and registration. Keep handler classes clean.

### Wrong Return Type

```csharp
// WRONG
public async void Handle(OrderCheckoutCompleted evt) { }            // NF0501
public Task<string> Handle(OrderCheckoutCompleted evt) { ... }      // NF0501

// RIGHT
public Task Handle(OrderCheckoutCompleted evt) => Task.CompletedTask;
```

**Why:** The dispatcher awaits `Task`. `void` cannot be awaited, and `Task<T>` would force every handler to return a value with no clear meaning.

### Using a Class or Interface as the Event Type

```csharp
// WRONG - not a record, not inheriting FactoryEventBase
public class OrderCheckoutCompleted { public int OrderId { get; set; } }

// RIGHT
public record OrderCheckoutCompleted(int OrderId, decimal Total) : FactoryEventBase;
```

**Why:** Events need structural equality (records) and the `FactoryEventBase` marker so the generator can identify them. Interfaces cannot be used as the generic argument of `[FactoryEventHandler<T>]` because the generator needs a concrete type for deserialization.

### Depending on Handler Ordering

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

Static handler classes register themselves into `FactoryEventHandlerRegistry` at startup via the generated `FactoryServiceRegistrar`. Client-relay handler classes register their dispatch delegate into `FactoryEventRelayRegistry` the same way.

---

## When to Use What

| Situation | Use |
|-----------|-----|
| Handler needs to participate in the factory's DB transaction | `[FactoryEventHandler<T>]` with static method |
| Handler failure should roll back the factory operation | `[FactoryEventHandler<T>]` with static method |
| Client UI needs to update in response to a server-side event | `[FactoryEventHandler<T>]` with instance method + `IFactoryEventRelay` |
| Server-internal transactional event the UI doesn't care about | `[FactoryEventHandler<T>]` static + `RaiseOptions.ServerOnly` |
| Fire-and-forget external IO (email, webhook, queue publish) | `[Event]` method attribute — see `references/static-factory.md` |
| Long-running work that must not block the factory response | `[Event]` method attribute |
| Both server-side transactional write AND client UI update for the same event | One event type; a `[FactoryEventHandler<T>]` static handler for the write and an instance handler for the UI |
