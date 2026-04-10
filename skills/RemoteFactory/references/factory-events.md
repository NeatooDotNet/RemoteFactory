# Factory Events — `[FactoryEventHandler<T>]` Mediator + Client Relay

This is a **separate feature** from the `[Event]` method attribute described in `references/static-factory.md`. Both are about domain events, but they solve different problems:

| Feature | `[Event]` method attribute | `[FactoryEventHandler<T>]` class attribute |
|---------|---------------------------|---------------------------------------------|
| Reference | `references/static-factory.md` | This file |
| Trigger | Direct delegate invocation from anywhere | `IFactoryEvents.Raise(...)` inside a factory method |
| Purpose | Fire-and-forget domain events in isolated scopes | Mediator fan-out + server-to-client relay |
| Client relay | No | Yes (instance methods) |

Use `[FactoryEventHandler<T>]` when you want a factory method to publish an event and have any number of handlers subscribe without the factory knowing about them, **and/or** when the client UI needs to react to events raised during server-side factory operations without adding SignalR.

---

## The Mediator (Server-Side Only)

Factory methods publish events through an injected `IFactoryEvents` service. Events are records that inherit `FactoryEventBase`:

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
        [Service] IFactoryEvents factoryEvents)
    {
        Id = id;
        Total = total;
        await factoryEvents.Raise(new OrderCheckoutCompleted(id, total));
    }
}
```

A handler class with a `[FactoryEventHandler<T>]` attribute and a **static** matching method is registered as a server-side handler. The source generator finds the method by signature (first non-`[Service]`/non-`CancellationToken` parameter must be `T`; return type must be `Task`).

```csharp
[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class OrderCheckoutAudit
{
    internal static Task Log(
        OrderCheckoutCompleted evt,
        [Service] IAuditLogService audit,
        CancellationToken ct) =>
        audit.LogAsync("Checkout", evt.OrderId, "Order", $"Total: {evt.Total:C}", ct);
}
```

Server handlers run in an isolated DI scope per invocation, with full `[Service]` injection and a `CancellationToken` tied to `IHostApplicationLifetime.ApplicationStopping`.

### Multiple Handler Types on One Class

Stack attributes to handle several event types in a single handler class:

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

The generator matches exactly one method per attribute.

---

## The Client Relay

When a factory operation runs on the server and raises events via `IFactoryEvents.Raise` with default options, those events are captured by a request-scoped `IFactoryEventCollector` and travel back to the client piggybacked on the existing HTTP response (`RemoteResponseDto.RelayedEvents`).

The data flow:

```
CLIENT                           SERVER
  | 1. factory.Create(...)        |
  |------------------------------>| 2. Create runs
  |                               | 3. events.Raise(new OrderCheckoutCompleted(...))
  |                               |    - dispatch to static [FactoryEventHandler<T>] handlers
  |                               |    - capture in IFactoryEventCollector
  |      RemoteResponseDto        | 4. HandleRemoteDelegateRequest attaches
  |  { Json, RelayedEvents: [..] }|    collected events to the response
  |<------------------------------|
  | 5. Factory result returned to caller FIRST
  | 6. IFactoryEventRelay dispatches events to
  |    registered instance handlers (fire-and-forget)
```

The ordering guarantee is strict: the caller's `await factory.Create(...)` completes **before** any relayed event is dispatched.

### Client-Side Handler (Instance Method)

A `[FactoryEventHandler<T>]` class with an **instance** matching method is registered as a client-side relay handler. The class registers itself with `IFactoryEventRelay` in its constructor and unregisters in `Dispose`:

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
- Handler exceptions are caught and logged. They never propagate to the factory caller.
- No registered handler for a relayed event type → silently dropped.
- Multiple registered handlers for the same event type → all invoked.

Register from a constructor and unregister from `Dispose`. In Blazor, components implementing `IDisposable` work naturally.

---

## `RaiseOptions.ServerOnly`

To raise an event that runs server-side handlers but is **not** relayed to the client, pass `RaiseOptions.ServerOnly`:

```csharp
await factoryEvents.Raise(
    new OrderCheckoutCompleted(id, total),
    RaiseOptions.ServerOnly);
```

Use this for server-internal concerns (trigger a downstream process, record an audit row) that the UI does not need to know about. `ServerOnly` composes with other flags:

| Flag | Meaning |
|------|---------|
| `None` | Default. Server handlers run; event is captured for client relay. |
| `AwaitRemote` | Wait for server handlers to complete before `Raise` returns. |
| `ContinueOnFail` | Continue dispatching remaining handlers if one throws. |
| `ServerOnly` | Server handlers run; event is NOT relayed to the client. |

Combine with bitwise OR:

```csharp
await factoryEvents.Raise(
    new OrderCheckoutCompleted(id, total),
    RaiseOptions.ServerOnly | RaiseOptions.ContinueOnFail);
```

### Nested Operations

The `IFactoryEventCollector` is request-scoped. Events raised during nested operations — for example, a child `Insert` running as part of a parent `Save` — are captured by the same collector and relayed to the client along with the parent's events.

### Logical Mode

In Logical (local) mode no collector and no relay are registered — nothing crosses the client/server boundary. Handlers still dispatch via the mediator; static handlers run in isolated scopes as usual.

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
internal async Task Create(int id, decimal total, [Service] IFactoryEvents events)
{
    Id = id; Total = total;
    await events.Raise(new OrderCheckoutCompleted(id, total));
}
```

**Why:** The `IFactoryEventCollector` that captures events for client relay is request-scoped and only exists on the server during a factory operation. Events raised outside that scope cannot be relayed.

### Decorating a Handler Class with `[Factory]`

```csharp
// WRONG - two pipelines on the same class
[Factory]
[FactoryEventHandler<OrderCheckoutCompleted>]
public partial class OrderNotifier
{
    public Task HandleCheckout(OrderCheckoutCompleted evt) => Task.CompletedTask;
}

// RIGHT - handler classes are NOT factories
[FactoryEventHandler<OrderCheckoutCompleted>]
public partial class OrderNotifier
{
    public Task HandleCheckout(OrderCheckoutCompleted evt) => Task.CompletedTask;
}
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
| Single, specific fire-and-forget operation (send a welcome email) | `[Event]` method attribute — see `references/static-factory.md` |
| Multiple decoupled subscribers reacting to a domain event | `[FactoryEventHandler<T>]` with static method |
| Client UI needs to update in response to a server-side event | `[FactoryEventHandler<T>]` with instance method + `IFactoryEventRelay` |
| Server-internal event the UI doesn't care about | `[FactoryEventHandler<T>]` static + `RaiseOptions.ServerOnly` |
| Both server logging and client UI update for the same event | One event type, one static handler, one instance handler |
