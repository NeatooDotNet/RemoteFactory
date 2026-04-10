# Factory Events

RemoteFactory has two distinct event features. Both use the same `[FactoryEventHandler<T>]` class-level attribute — the difference is whether the matching method is `static` (server handler) or an instance method (client relay handler).

| Feature | `[Event]` (attribute on method) | `[FactoryEventHandler<T>]` (attribute on class) |
|---------|--------------------------------|-------------------------------------------------|
| Page | [Events](events.md) | This page |
| Trigger | Direct delegate invocation from anywhere | `IFactoryEvents.Raise(...)` inside a factory method |
| Purpose | Fire-and-forget domain events in isolated scopes | Mediator + server-to-client relay during factory operations |
| Dispatch | Generated `{MethodName}Event` delegate | Source-generated type-keyed dispatch table |
| Client relay | No | Yes (instance methods receive events after factory result returns) |

This page covers the mediator + relay pattern. For the isolated-scope fire-and-forget pattern, see [Events](events.md).

---

## The Mediator Pattern

`IFactoryEvents` is a request-scoped mediator that dispatches events to any handler decorated with `[FactoryEventHandler<T>]`. Handlers do not know about each other, and the raising code does not know which handlers exist.

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
        [Service] IFactoryEvents factoryEvents)
    {
        Id = id;
        Total = total;
        await factoryEvents.Raise(new OrderCheckoutCompleted(id, total));
    }
}
```

Event types must be records inheriting `FactoryEventBase`. Records give structural equality and immutability; `FactoryEventBase` is the marker the generator looks for.

### Server-Side Handler (Static Method)

A `[FactoryEventHandler<T>]` class with a `static` matching method is treated as a server-side handler. The generator registers it with `FactoryEventHandlerRegistry`. Each handler invocation runs in a new DI scope with full `[Service]` injection and a `CancellationToken` tied to `IHostApplicationLifetime.ApplicationStopping`.

```csharp
[FactoryEventHandler<OrderCheckoutCompleted>]
public static partial class OrderCheckoutEmailHandler
{
    internal static async Task SendConfirmationEmail(
        OrderCheckoutCompleted evt,
        [Service] IEmailService email,
        CancellationToken ct)
    {
        await email.SendAsync(
            "sales@company.com",
            $"Order {evt.OrderId} checked out",
            $"Total: {evt.Total:C}",
            ct);
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

`[Event]` and `[FactoryEventHandler<T>]` serve different needs.

- `[Event]` is imperative: the caller knows the delegate name and invokes it directly.
- `[FactoryEventHandler<T>]` is declarative: the raiser publishes a typed event and any number of handlers subscribe without the raiser knowing they exist.

Use `[Event]` when you need a single, specific fire-and-forget operation (send this email). Use `[FactoryEventHandler<T>]` when you want decoupled fan-out to multiple subscribers during a factory operation, and especially when you need server-to-client relay.

---

## The Client Relay Pattern

When a factory operation runs on the server and raises events via `IFactoryEvents.Raise`, those events are also captured by a request-scoped `IFactoryEventCollector`. The captured events travel back to the client piggybacked on the existing HTTP response — no SignalR, no push channel, no additional infrastructure.

### Data Flow

```
CLIENT                           SERVER
  |                                 |
  | 1. factory.Create(...)          |
  |-------------------------------->|
  |                                 | 2. Create runs
  |                                 | 3. events.Raise(new OrderCheckoutCompleted(...))
  |                                 |    - dispatches to [FactoryEventHandler<T>] static handlers
  |                                 |    - captures in IFactoryEventCollector
  |                                 |
  |        RemoteResponseDto        | 4. HandleRemoteDelegateRequest attaches
  |   { Json, RelayedEvents: [..] } |    collected events to the response
  |<--------------------------------|
  |                                 |
  | 5. Factory result returned to caller FIRST
  | 6. IFactoryEventRelay dispatches RelayedEvents to
  |    registered instance handlers (fire-and-forget)
```

Key ordering guarantee: the factory operation result is returned to the caller **before** relayed events are dispatched. Event handlers run after `await factory.Create(...)` has completed.

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
    RaiseOptions.ServerOnly);
```

Use this for server-internal concerns (trigger a downstream process, record an audit row) that the UI does not need to know about. `ServerOnly` composes with other flags:

| Flag | Meaning |
|------|---------|
| `None` | Default. Server handlers run; event is captured for client relay. |
| `AwaitRemote` | Wait for server handlers to complete before returning from `Raise`. |
| `ContinueOnFail` | Continue dispatching remaining handlers if one throws. |
| `ServerOnly` | Server handlers run; event is NOT relayed to the client. |

Flags combine with bitwise OR:

```csharp
await factoryEvents.Raise(
    new OrderCheckoutCompleted(id, total),
    RaiseOptions.ServerOnly | RaiseOptions.ContinueOnFail);
```

### Nested Operations

The `IFactoryEventCollector` is request-scoped. Events raised during nested operations — for example, a child `Insert` running as part of a parent `Save` — are captured by the same collector and relayed to the client along with the parent's events.

### Logical Mode

In Logical (local) mode no collector is registered and no relay occurs, because nothing ever crosses the client/server boundary. Handlers still dispatch via the mediator; static handlers run in isolated scopes as usual.

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
internal async Task Create(int id, decimal total, [Service] IFactoryEvents events)
{
    Id = id; Total = total;
    await events.Raise(new OrderCheckoutCompleted(id, total));  // Server-side, inside factory method
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
    internal async Task Create(int id, decimal total, [Service] IFactoryEvents events)
    {
        Id = id;
        Total = total;
        await events.Raise(new OrderCheckoutCompleted(id, total));
    }
}

// --------------- Server: static handler runs in isolated scope ---------------
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
