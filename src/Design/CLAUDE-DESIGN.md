# CLAUDE-DESIGN.md

---
design_version: 1.7
last_updated: 2026-04-14
target_frameworks: [net9.0, net10.0]
---

This file provides Claude Code with the authoritative reference for RemoteFactory's design. When proposing changes or implementing features, consult this document and the Design.Domain code.

## How to Use This Reference

Follow this workflow when working with RemoteFactory patterns:

1. **To understand a pattern**: Read the relevant file in `Design.Domain/`
   - `FactoryPatterns/AllPatterns.cs` - All three patterns side-by-side
   - `Aggregates/Order.cs` - Complete lifecycle example
   - `Entities/OrderLine.cs` - Child entity without [Remote]

2. **To verify syntax**: Check `Design.Tests/FactoryTests/` for working examples
   - Tests demonstrate correct usage that compiles and runs
   - Look at test assertions to understand expected behavior

3. **To propose a change**: Cross-reference against "DID NOT DO THIS" sections
   - These sections document deliberate design decisions
   - If your proposal contradicts one, you need strong justification

4. **To understand generator behavior**: Look for `[GENERATOR BEHAVIOR]` comments
   - These describe what the generator outputs for each pattern
   - Critical for understanding what code you're actually getting

---

## When to Use Each Pattern (Decision Table)

| Pattern | Use When | Example | Key Characteristics |
|---------|----------|---------|---------------------|
| **Class Factory** | Aggregate roots with lifecycle, entities needing Create/Fetch/Save | `Order`, `Customer`, `Invoice` | Instance state, serializable, IFactorySaveMeta support |
| **Interface Factory** | Remote services without entity identity | `IOrderRepository`, `IPaymentService` | Server implementation, client proxy, no operation attributes |
| **Static Factory** | Stateless commands and side effects | `EmailCommands.SendNotification` | No instance state, [Execute] operation |

### Detailed Guidance

**Choose Class Factory when:**
- You need to create, fetch, and persist domain entities
- The object has identity and lifecycle (IsNew, IsDeleted)
- State needs to cross the client/server boundary
- You want factory.Save() to route to Insert/Update/Delete

**Choose Interface Factory when:**
- You have a service that runs only on the server
- The client needs to call service methods remotely
- You don't need entity state management
- You want clean separation between contract and implementation

**Choose Static Factory when:**
- The operation is pure request-response (no instance state)
- Operations are naturally expressed as functions, not methods on objects
- You want CancellationToken support

---

## Quick Reference: The Three Factory Patterns

### Pattern 1: Class Factory (Aggregate Root)
```csharp
public interface IMyEntity { int Id { get; set; } }

[Factory]
internal partial class MyEntity : IMyEntity
{
    public int Id { get; set; }  // Public setter required for serialization

    [Remote, Create]
    internal void Create(string name, [Service] IMyService service) { }

    [Remote, Fetch]
    internal void Fetch(int id, [Service] IMyService service) { }
}
```
**Generates**: `public IMyEntityFactory` with `Create()`, `Fetch()` methods returning `IMyEntity`. `[Remote]` promotes `internal` methods to `public` on the factory interface.

### Pattern 1b: Class Factory (Child Entity)
```csharp
public interface IMyChild { int Id { get; set; } }

[Factory]
internal partial class MyChild : IMyChild
{
    public int Id { get; set; }

    [Create]
    internal void Create(string name) { }  // internal = server-only, trimmable

    [Fetch]
    internal void Fetch(int id, string name) { }
}
```
**Generates**: `internal IMyChildFactory` with `Create()`, `Fetch()` methods. Not visible to client.

### Pattern 2: Interface Factory
```csharp
[Factory]
public interface IMyRepository
{
    Task<List<Item>> GetAllAsync();      // No operation attributes needed
    Task<Item?> GetByIdAsync(int id);    // All methods become remote
}
```
**Generates**: Proxy implementation that serializes to server.

### Pattern 2a: Interface Factory with [AuthorizeFactory<T>]
```csharp
public interface IRepositoryAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)] bool HasAccess();           // every method
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)] bool CanAccessItem(Guid id); // methods with matching Guid param
}

[Factory]
[AuthorizeFactory<IRepositoryAuth>]
public interface IAuthorizedRepository
{
    Task<Item?> GetItem(Guid id);                 // No attributes — Anti-Pattern 2 / Critical Rule 4
    Task<Item>  UpdateItem(Guid id, string name);
}

public class AuthorizedRepository : IAuthorizedRepository { /* plain service class */ }
```
**Generates**: `IAuthorizedRepositoryFactory : IAuthorizedRepository` with `Can{Method}(matching-params)` helpers. `Local{Method}` invokes all applicable auth methods and throws `NotAuthorizedException` on denial before calling the impl.

**Key rules:**
- Scopes `Execute` and `Read` apply uniformly across all interface methods. CRUD scopes (`Create`/`Fetch`/`Insert`/`Update`/`Delete`) silently never fire because interface methods have no CRUD operation.
- Parameter matching by type enables per-method authorization (e.g., `CanAccessItem(Guid id)` runs only on interface methods that have a `Guid` parameter).
- The impl class is a plain service — no `[Factory]`, no operation attributes, no `[Remote]`. Register it on the server only (`services.AddScoped<IAuthorizedRepository, AuthorizedRepository>()`).
- Contrast with `AuthorizedOrder.cs` (class factory, CRUD scopes, `CanCreate`/`CanFetch`/`CanSave`/`CanDelete`).

See `AuthorizedRepository.cs` for the fully-commented pedagogy file.

### Pattern 3: Static Factory
```csharp
[Factory]
public static partial class MyCommands
{
    [Remote, Execute]
    private static Task<bool> _DoSomething(      // Underscore prefix, private
        string input,
        [Service] IMyService service) => service.ProcessAsync(input);
}
```
**Generates**:
- `MyCommands.DoSomething` delegate (Execute)

### Pattern 4: Factory Event Handler (Mediator) + Client Relay

Server-side handlers and client-side relay live in **two separate surfaces**:

- **Server handlers** — `[FactoryEventHandler<T>]` on a class with a `static` method. Source-generated; registered into `FactoryEventHandlerRegistry`; runs in the caller's DI scope.
- **Client relay** — the consumer implements `IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase>)`. RemoteFactory invokes it fire-and-forget, strictly after the factory method returns to the caller. No source generation for the client path.

Instance-method `[FactoryEventHandler<T>]` handlers compile but are silently skipped and produce diagnostic **NF0503** (Warning). Either make the method `static` (server) or implement `IFactoryEventRelay` (client).

**Event type** (shared between client and server):
```csharp
public record OrderPlacedEvent(int OrderId, string Email) : FactoryEventBase;
```

**Server-side raiser** (in any factory method):
```csharp
[Factory]
internal partial class Order
{
    [Remote, Create]
    internal async Task Create(int id, [Service] IFactoryEvents events)
    {
        // ... do work ...
        await events.Raise(new OrderPlacedEvent(id, "x@y.com"));
    }
}
```

**Server-side handler** (static method → runs in the **caller's DI scope**,
sequentially, awaited):
```csharp
[FactoryEventHandler<OrderPlacedEvent>]
public static partial class OrderNotifyHandlers
{
    internal static async Task SendEmail(
        OrderPlacedEvent evt,
        [Service] INotificationService service,
        [Service] AppDbContext db,     // same DbContext the factory is using
        CancellationToken ct)           // same CT the caller passed to Raise
    {
        await service.SendAsync(evt.Email, $"Order {evt.OrderId} placed");
        // db changes here participate in the caller's transaction — throwing
        // from this handler aborts the factory operation
    }
}
```

**Execution model for `[FactoryEventHandler<T>]`** — the three invariants:

1. **Shared scope.** Handlers resolve `[Service]` dependencies from the caller's
   `IServiceProvider`. A `DbContext` in the factory method and a `DbContext` in
   the handler are the same instance and the same transaction.
2. **Sequential.** Handlers run one after another in unspecified order. Callers
   must not rely on a specific ordering.
3. **Awaited.** `Raise<T>()` returns only after every handler has completed. A
   handler exception aborts the remaining handlers and propagates to the caller
   so the transaction can roll back. Across the client/server boundary the HTTP
   call stays open until all server-side handlers finish.

**When to use `[FactoryEventHandler<T>]`**: domain events that must participate
in the caller's transaction — i.e. the handler touches the same `DbContext` as
the aggregate.

**For fire-and-forget work** (email, webhooks, audit sinks to external systems,
queue publishes): compose your own fire-and-forget pattern inside a factory
method — `Task.Run(...)` plus `IServiceScopeFactory.CreateScope()` to obtain a
fresh DI scope, with any ambient context (tenant, correlation) explicitly
snapshotted from the caller's scope before entering the background work. See the
v1.5.0 release notes for the migration pattern.

**Client-side relay** (consumer implements the interface — no generator involvement):
```csharp
public sealed class MyClientRelay : IFactoryEventRelay
{
    private readonly IEventAggregator _aggregator;

    public MyClientRelay(IEventAggregator aggregator) => _aggregator = aggregator;

    public Task Relay(IReadOnlyList<FactoryEventBase> events)
    {
        // One [Remote] call = exactly one Relay invocation (may be empty).
        // Bridge the batch to your aggregator / MediatR / UI bus.
        foreach (var evt in events) _aggregator.Publish(evt);
        return Task.CompletedTask;
    }
}
```

Register the relay in DI **either** before or after `AddNeatooRemoteFactory`:
```csharp
// Remote mode registers NoOpFactoryEventRelay via TryAdd; consumer-first wins, consumer-after overrides
services.AddSingleton<IFactoryEventRelay, MyClientRelay>();
services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(Order).Assembly);
```

**Key points:**
- `[FactoryEventHandler<T>]` classes do NOT need `[Factory]` — it's a separate pipeline
- Multiple `[FactoryEventHandler<T>]` attributes on one class = handles multiple **server** event types (static methods only)
- Instance-method `[FactoryEventHandler<T>]` → **NF0503 Warning** and silently skipped; use `IFactoryEventRelay` for client reception
- `RaiseOptions.ServerOnly` excludes the event from the client relay (server-side handlers still run)
- The relay piggybacks on the existing HTTP response (`RemoteResponseDto.RelayedEvents`) — no SignalR needed
- On the wire, `RemoteResponseDto.RelayedEvents` is `null` when zero events are captured (preserves backward-compatible JSON payloads); the client normalizes `null` to `Array.Empty<FactoryEventBase>()` so `Relay` is still invoked exactly once
- **One [Remote] call = exactly one `Relay` invocation.** The only exception: if batch deserialization throws `UnknownFactoryEventTypeException`, `Relay` is not invoked for that call and EventId 3009 is logged.
- **Post-return ordering is a hard guarantee.** Dispatch uses `Task.Run + Task.Yield + CancellationToken.None` so `Relay` runs strictly after the caller's continuation resumes, on both sync-context and no-sync-context hosts.
- Relay exceptions and deserialization failures are caught inside the dispatch task and logged via `ILogger` (EventId 3008 `FactoryEventRelayFailed`, EventId 3009 `FactoryEventDeserializationFailed`). Neither propagates to the factory caller.
- Event types: any descendant of `FactoryEventBase` is automatically discoverable (via `[FactoryEvent]` + `[DynamicallyAccessedMembers]` inherited from the base) and trim-safe. No per-event annotation required.
- `FactoryEventTypeRegistry` (internal, runtime) lazily scans `AppDomain.CurrentDomain.GetAssemblies()` on first use; rescans on miss to pick up dynamically-loaded assemblies. Logs EventId 3012 (Warning) on `FullName` collisions.
- In Remote mode, if the consumer registers nothing, `NoOpFactoryEventRelay` is registered via `TryAddSingleton`. It logs EventId 3011 (Warning) once per process on the first non-empty batch it drops — a signal the consumer forgot to register a custom relay.
- Logical mode registers neither the collector nor the relay (no cross-boundary communication needed). Server mode does not register `IFactoryEventRelay`.
- NF0501 if no matching server handler method; NF0502 if multiple methods match; NF0503 (Warning) if an instance method is declared inside a `[FactoryEventHandler<T>]` class.

---

## Quick Decisions Table

| Question | Answer | Reference | Reason |
|----------|--------|-----------|--------|
| Should this method be [Remote]? | Only aggregate root entry points | `Order.cs` vs `OrderLine.cs` | Once on server, stay on server |
| Should a [Remote] method be `internal`? | Yes, always -- `[Remote]` requires `internal` (NF0105 error if `public`) | `Order.cs` | Enables IL trimming; `[Remote]` promotes to `public` on factory interface |
| Should non-[Remote] methods be `internal`? | Yes, if only called from server-side code (child entities, within-aggregate ops) | `OrderLine.cs` | Internal methods get `IsServerRuntime` guard and are trimmable |
| Can I use private setters? | No | `AllPatterns.cs:73` | IL trimming + source generation |
| Should interface methods have attributes? | No | `AllPatterns.cs:203` | Interface IS the boundary |
| Do I need `partial` keyword? | Yes, always | `AllPatterns.cs:49` | Generator adds code to class |
| Should child entities have [Remote]? | No | `OrderLine.cs:27-41` | Would cause N+1 remote calls |
| Can [Execute] return void? | No, must return Task<T> | `AllPatterns.cs:340-347` | Client needs result to confirm |
| Where does business logic go? | In the entity, not the factory | `Order.cs:229-242` | DDD principle |
| Can I store method-injected services? | Only if using constructor injection | `AllPatterns.cs:86-96` | Fields lost after serialization |
| Does constructor injection affect ordinal serialization? | Yes -- a class with no parameterless or all-default-parameter ctor causes the generator to skip `IOrdinalSerializable`; the type then deserializes via the named/DI path (`GetRequiredService`), which resolves the ctor parameters from the DI container on each side of the wire | `CtorInjectionExample.cs`, `SerializationTests.cs` (`CtorInjectedEntity_*` tests) | Ordinal `FromOrdinalArray` uses object-initializer / positional construction and does not resolve DI. The generator's `RequiresServiceInstantiationCheck` looks only at ctor shape (parameter count, default values) -- not at `[Service]` attribute presence |
| Which authorization approach? | `[AuthorizeFactory<T>]` for domain-specific rules; `[AspAuthorize]` for ASP.NET Core policies | Class factory: `AuthorizedOrder.cs`. Interface factory: `AuthorizedRepository.cs`. ASP.NET: `SecureOrder.cs` | AuthorizeFactory gives client-side Can* methods; AspAuthorize leverages existing ASP.NET Core policies |
| Which `[AuthorizeFactory]` scope applies on an interface factory? | `Execute` and `Read` only | `AuthorizedRepository.cs` | Interface methods have no CRUD operation; `Create`/`Fetch`/`Insert`/`Update`/`Delete` scopes silently never fire. Use parameter matching for per-method authorization |
| Does Can* inherit guard from the factory method? | No -- Can* derives guard from the auth class methods | `AuthorizedOrder.cs`, `AuthorizedOrderAuth.cs` | Can* calls auth methods, not the factory method; auth method accessibility determines Can* behavior |
| Can Interface Factory return a record? | Yes, plain records/DTOs without Neatoo types | `AllPatterns.cs` | Records bypass reference handling (`RecordBypassConverterFactory`); do not mix Neatoo types into record properties |
| How do I handle a factory event on the client? | Implement `IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase>)` and register it in DI | `FactoryEventRelayPattern.cs` (`InMemoryAggregatorRelay`) | Consumer-owned bridge to any aggregator; RemoteFactory invokes it fire-and-forget exactly once per [Remote] call (even empty batch), strictly after the caller's continuation resumes |
| How do I handle a factory event on the server? | `[FactoryEventHandler<T>]` class attribute with a `static` method | `FactoryEventHandlerPattern.cs` | Static method = server handler running in the caller's scope (shared DbContext), sequential, awaited |
| Does `[FactoryEventHandler<T>]` need `[Factory]`? | No, it's a separate generator pipeline | `FactoryEventRelayPattern.cs` | Keeps handler classes clean — not factories |
| How do I stop an event from relaying to the client? | `events.Raise(..., RaiseOptions.ServerOnly)` | `FactoryEventRelayPattern.cs` | Server handlers still run; event excluded from `RemoteResponseDto` |
| I want a handler to participate in the factory's DB transaction. | Use `[FactoryEventHandler<T>]` — it runs in the caller's scope | `FactoryEventHandlerPattern.cs` | Shared scope → shared `DbContext` → same transaction; a throwing handler rolls the whole thing back |
| I want a handler to fire-and-forget (email, webhook). | Compose your own `Task.Run` + `IServiceScopeFactory.CreateScope()` inside the factory method; copy any ambient state explicitly before entering the background task. | v1.5.0 release notes | No framework-supplied fire-and-forget surface after v1.5.0 — explicit copy is required |
| Can I handle multiple event types in one class? | Yes, stack multiple `[FactoryEventHandler<T>]` attributes | `PersonEventHandler.cs` (Person example) | Generator finds one matching method per attribute |
| How do I defer loading of related data? | Use `LazyLoad<T>` property with constructor-initialization pattern | `LazyLoadExample.cs` | Value is passive (no auto-load); call LoadAsync() explicitly; two-slot ordinal encoding |
| Can I use BCL `Lazy<T>`? | No -- use `LazyLoad<T>` instead | `SerializationTests.cs` | BCL `Lazy<T>` has no serialization support; `LazyLoad<T>` serializes Value + IsLoaded |
| Do I need to register DTOs for IL trimming? | No -- the generator auto-preserves DTO types from factory signatures, records included | `DtoConstructorRegistry.cs` | Parameterless ctor → `Register<T>(() => new T())` lambda used by `NeatooJsonTypeInfoResolver`; positional records → `PreserveType<T>()` rooting, deserialized via `RecordBypassConverterFactory` |
| What if my nested DTO fails to deserialize under trimming? | Check that it is reachable as a public property of a discovered DTO; if not, return it from a factory method or register manually | `docs/trimming.md` | The generator recursively walks properties of discovered DTOs; only unreachable types need manual registration |
| Can auth methods receive factory method parameters? | Yes -- parameters are matched by type | `ParamAuthOrder.cs`, `ParamAuthOrderAuth.cs` | Auth method `CanFetch(Guid orderId)` receives the Guid from `Fetch(Guid orderId)` for per-entity access control |
| Can auth methods receive the target entity? | Yes -- on write operations (Insert/Update/Delete) | `ParamAuthOrder.cs`, `ParamAuthOrderAuth.cs` | Auth method `CanWrite(IEntity target)` inspects entity state; suppresses CanInsert/CanUpdate/CanDelete generation but CanSave gets two overloads |
| How does CanSave work with target-param auth? | Two overloads: `CanSave()` runs non-target auth only; `CanSave(target)` runs ALL auth | `ParamAuthOrderAuth.cs` | Caller has the entity in hand before Save; CanInsert/CanUpdate/CanDelete remain suppressed |

---

## Anti-Patterns (What NOT to Do)

### Anti-Pattern 1: [Remote] on Child Entities

**WRONG:**
```csharp
[Factory]
internal partial class OrderLine : IOrderLine
{
    [Remote, Create]  // WRONG: Causes N+1 remote calls
    internal void Create(string productName, decimal price, int qty) { }
}
```

**RIGHT:**
```csharp
[Factory]
internal partial class OrderLine : IOrderLine
{
    [Create]  // No [Remote] - called from server-side Order operations
    internal void Create(string productName, decimal price, int qty) { }
}
```

**Why it matters:** Each [Remote] creates a network round-trip. If Order has 10 lines, that's 10 extra HTTP calls instead of 1 atomic operation.

---

### Anti-Pattern 2: Attributes on Interface Factory Methods

**WRONG:**
```csharp
[Factory]
public interface IMyRepository
{
    [Fetch]  // WRONG: emits NF0106 — factory-operation attribute on interface method
    Task<Item> GetByIdAsync(int id);
}
```

**RIGHT:**
```csharp
[Factory]
public interface IMyRepository
{
    Task<Item> GetByIdAsync(int id);  // No attribute - interface IS the boundary
}
```

**Why it matters:** The generator treats all interface methods as remote. Adding operation attributes would cause duplicate registrations — enforced by **NF0106** (factory-operation attribute on interface factory method). Applies to `[Create]`/`[Fetch]`/`[Insert]`/`[Update]`/`[Delete]`/`[Execute]`, with or without `[AuthorizeFactory<T>]`.

---

### Anti-Pattern 3: Public Static Factory Methods

**WRONG:**
```csharp
[Factory]
public static partial class Commands
{
    [Remote, Execute]
    public static Task<bool> SendNotification(...) { }  // WRONG: Conflicts with generated code
}
```

**RIGHT:**
```csharp
[Factory]
public static partial class Commands
{
    [Remote, Execute]
    private static Task<bool> _SendNotification(...) { }  // Private with underscore
}
```

**Why it matters:** The generator creates the public method. A public method in your code conflicts with the generated public method.

---

### Anti-Pattern 4: Private Property Setters

**WRONG:**
```csharp
public int Id { get; private set; }  // WRONG: Won't deserialize
```

**RIGHT:**
```csharp
public int Id { get; set; }  // Public setter for serialization
```

**Why it matters:** Serialization uses property setters. Private setters break deserialization, causing data loss across the wire.

---

### Anti-Pattern 5: Storing Method-Injected Services in Fields

**WRONG:**
```csharp
[Factory]
internal partial class MyEntity
{
    private IMyService _service;  // WRONG: Lost after serialization

    [Remote, Create]
    internal void Create([Service] IMyService service)
    {
        _service = service;  // This field will be null on client after round-trip
    }

    public void DoSomething()
    {
        _service.Execute();  // NullReferenceException on client!
    }
}
```

**RIGHT (Option A - Constructor Injection):**
```csharp
[Factory]
public partial class MyEntity
{
    public MyEntity([Service] ILogger logger)  // Constructor = available everywhere
    {
        _logger = logger;
    }
}
```

**RIGHT (Option B - Call from Server Operation):**
```csharp
[Remote, Update]
internal void Update([Service] IMyService service)
{
    service.Execute();  // Use immediately, don't store
}
```

**Why it matters:** Only serializable state survives the round-trip. Service references are infrastructure, not state.

---

### Anti-Pattern 6: Missing partial Keyword

**WRONG:**
```csharp
[Factory]
public class MyEntity { }  // WRONG: Won't compile
```

**RIGHT:**
```csharp
[Factory]
public partial class MyEntity { }  // partial required
```

**Why it matters:** The generator adds a partial class with `IOrdinalSerializable` implementation. Without `partial`, you get CS0260 compilation error.

---

### Anti-Pattern 7: Entity Duality Mistakes

An entity can be an aggregate root in one context and a child in another. The mistake is applying [Remote] based on the type, not the context.

**WRONG (applying [Remote] to all operations because "it's a Product"):**
```csharp
// Product.cs - used as aggregate root AND as child of Order
[Factory]
internal partial class Product : IProduct
{
    [Remote, Fetch]  // OK as aggregate root entry point
    internal void Fetch(int id, [Service] IProductRepository repo) { }

    [Remote, Fetch]  // WRONG: This child-context method doesn't need [Remote]
    internal void FetchAsChild(int id, string name, decimal price) { }
}
```

**RIGHT (separate operations for different contexts):**
```csharp
[Factory]
internal partial class Product : IProduct
{
    [Remote, Fetch]  // Aggregate root context - client entry point (internal + [Remote] = promoted to public on interface)
    internal void Fetch(int id, [Service] IProductRepository repo) { }

    [Fetch]  // Child context - called from Order.Fetch on server
    internal void FetchAsChild(int id, string name, decimal price) { }
}
```

**Why it matters:** [Remote] is about *how the method is called*, not *what the type is*. Same type can have both remote and non-remote operations.

---

### Anti-Pattern 8: [Remote] on Public Methods

**WRONG:**
```csharp
[Factory]
internal partial class Order : IOrder
{
    [Remote, Create]   // WRONG: Diagnostic NF0105 -- [Remote] requires internal
    public void Create(string name, [Service] IMyService service) { }
}
```

**RIGHT:**
```csharp
[Factory]
internal partial class Order : IOrder
{
    [Remote, Create]   // Correct: [Remote] + internal, promoted to public on factory interface
    internal void Create(string name, [Service] IMyService service) { }
}
```

**Why it matters:** `[Remote]` requires `internal` to enable IL trimming of method bodies on client assemblies. The generator promotes `[Remote] internal` methods to `public` on the factory interface, so clients still call them through the factory. `[Remote] public` emits diagnostic error NF0105.

---

### Anti-Pattern 9: Mixing Neatoo Types with Records in Interface Factory Return Types

**WRONG:**
```csharp
// A record that contains a Neatoo domain type as a property
public record OrderSummary(
    string CustomerName,
    IOrder ActiveOrder);  // WRONG: Neatoo domain type inside a plain record

[Factory]
public interface IOrderService
{
    Task<OrderSummary> GetSummaryAsync(int customerId);
}
```

**RIGHT (Option A - Use Neatoo types for the entire graph):**
```csharp
// If you need Neatoo domain types, return them directly
[Factory]
public interface IOrderService
{
    Task<IOrder> GetOrderAsync(int orderId);
}
```

**RIGHT (Option B - Use plain DTOs/records throughout):**
```csharp
// If you need a record return type, use only plain data -- no Neatoo types
public record OrderSummary(
    string CustomerName,
    string Status,
    decimal Total);  // All plain data, no Neatoo types

[Factory]
public interface IOrderService
{
    Task<OrderSummary> GetSummaryAsync(int customerId);
}
```

**Why it matters:** RemoteFactory uses a two-path serialization strategy for reference handling. Mutable reference types (Dictionary, List, plain classes with default constructors) participate in `$id`/`$ref` reference tracking via `NeatooPreserveReferenceHandler` on `JsonSerializerOptions`. Types with parameterized constructors (records, immutable types) are claimed by `RecordBypassConverterFactory`, which serializes them without any reference metadata -- this is correct DDD behavior because records are value objects whose identity is defined by their values, not by reference. STJ cannot deserialize `$id`/`$ref` metadata on types with parameterized constructors (`ObjectWithParameterizedCtorRefMetadataNotSupported`), so bypassing is also a technical necessity. Mixing Neatoo types into a plain record creates a serialization mismatch: the record bypasses reference handling entirely (including its subtree), but the embedded Neatoo type's converter expects the resolver to be tracking references across the graph. Use either pure Neatoo types (with `[Factory]`) or pure records/DTOs -- not a mix.

---

### Anti-Pattern 10: Raising Factory Events Outside a Factory Method

**WRONG:**
```csharp
// Client code calling a factory, then trying to raise an event
var order = await factory.Create(...);
await factoryEvents.Raise(new OrderPlacedEvent(order.Id));  // Wrong side!
```

**RIGHT:**
```csharp
[Factory]
internal partial class Order
{
    [Remote, Create]
    internal async Task Create(int id, [Service] IFactoryEvents events)
    {
        // ... do work ...
        await events.Raise(new OrderPlacedEvent(id));  // Raised server-side
    }
}
```

**Why it matters:** Events are captured by a request-scoped `IFactoryEventCollector` that only exists on the server during a factory operation. Events raised outside that scope on the client have no collector and cannot be relayed. Always raise events from inside a factory method via an injected `[Service] IFactoryEvents`.

---

### Anti-Pattern 11: Decorating a [FactoryEventHandler<T>] Class with [Factory]

**WRONG:**
```csharp
[Factory]
[FactoryEventHandler<OrderPlacedEvent>]
public partial class OrderNotifier  // WRONG: Two pipelines on the same class
{
    public Task HandleOrderPlaced(OrderPlacedEvent evt) => Task.CompletedTask;
}
```

**RIGHT:**
```csharp
[FactoryEventHandler<OrderPlacedEvent>]
public partial class OrderNotifier
{
    public Task HandleOrderPlaced(OrderPlacedEvent evt) => Task.CompletedTask;
}
```

**Why it matters:** `[FactoryEventHandler<T>]` runs in a completely separate generator pipeline from `[Factory]`. The handler class does not need (and should not have) `[Factory]` — it's not a factory. Adding `[Factory]` forces the class through the factory generation pipeline where it would need factory methods, interfaces, etc. Keep handler classes clean.

---

## Critical Rules

### 1. [Remote] is ONLY for Aggregate Root Entry Points
```csharp
// CORRECT: Aggregate root has [Remote] + internal (promoted to public on factory interface)
[Factory]
internal partial class Order : IOrder
{
    [Remote, Create]  // Client entry point -- internal required by [Remote]
    internal void Create(...) { }
}

// CORRECT: Child entity does NOT have [Remote]
[Factory]
internal partial class OrderLine : IOrderLine
{
    [Create]  // Server-side only - called from Order operations
    internal void Create(...) { }
}
```

### 2. Factory Method Visibility Controls Guard Emission and Trimming

The developer's `public` vs `internal` on factory methods tells the generator who is allowed to call each method. This determines whether an `IsServerRuntime` guard is emitted and whether the method body survives IL trimming on the client.

| Method Declaration | Guard Emitted? | Client Behavior | Trimmable? |
|---|---|---|---|
| `[Remote] internal` | Yes | Routes to server via delegate fork; promoted to `public` on factory interface | Yes (guarded) |
| `public` (no Remote) | No | Runs locally on client | No (always available) |
| `internal` (no Remote) | Yes | Throws if called when `IsServerRuntime=false` | Yes (guarded) |
| `[Remote] public` | N/A | **Diagnostic NF0105** -- `[Remote]` requires `internal` | N/A |

**Why this matters:** `[Remote]` requires `internal` so the IL trimmer can remove method bodies from client assemblies. The generator promotes `[Remote] internal` methods to `public` on the factory interface, so clients call them through the factory. `public` non-`[Remote]` methods have no guard and work on both sides.

```csharp
// Aggregate root: internal + [Remote] for client entry points (promoted to public on interface)
[Factory]
internal partial class Order : IOrder
{
    [Remote, Create]  // Guard: yes (Remote). Client routes to server. Promoted to public on IOrderFactory.
    internal void Create(string name, [Service] IOrderLineListFactory lines) { }

    [Remote, Fetch]   // Guard: yes (Remote). Client routes to server. Promoted to public on IOrderFactory.
    internal Task<bool> Fetch(int id, [Service] IOrderRepository repo) { }
}

// Child entity: internal methods for server-only operations
[Factory]
internal partial class OrderLine : IOrderLine
{
    [Create]           // Guard: yes (internal). Server-only.
    internal void Create(string name, decimal price, int qty) { }

    [Fetch]            // Guard: yes (internal). Server-only.
    internal void Fetch(int id, string name, decimal price, int qty) { }
}
```

#### Can* Method Guard Derivation (Auth-Method-Driven)

Can* methods (e.g., `CanCreate()`, `CanFetch()`, `CanSave()`) derive their guard behavior from the **auth class methods**, not from the parent factory method. This is because Can* methods call the auth methods, not the factory method. The auth method's accessibility determines whether the Can* check can run on the client.

| Auth Method Declaration | Can* Guard? | Can* Client Behavior | Can* Interface Promotion |
|---|---|---|---|
| `public` (no `[Remote]`) | No | Runs locally on client (sync, returns `Authorized`) | Not independently promoted |
| `internal` (no `[Remote]`) | Yes | Throws if called when `IsServerRuntime=false` | Not promoted |
| `[Remote] internal` | Yes | Routes to server via remote delegate (async, returns `Task<Authorized>`) | Promoted to `public` on factory interface |

**CanSave aggregation:** CanSave aggregates auth methods from Insert, Update, and Delete operations. If ANY constituent auth method is `internal` or `[Remote]`, CanSave gets the guard (most restrictive wins for security).

**`[AspAuthorize]` interaction:** When `[AspAuthorize]` is present on a factory method alongside `[AuthorizeFactory<T>]`, the Can* method always gets the guard because `[AspAuthorize]` requires server-side `HttpContext`.

```csharp
// Public auth methods => Can* runs on client, no guard
public interface IMyAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();  // public => CanCreate() has no guard, runs on client
}

// [Remote] internal auth methods => Can* routes to server
public interface IServerAuth
{
    [Remote]
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    internal bool CanCreate();  // [Remote] internal => CanCreate() has guard, routes to server
}
```

See `AuthorizedOrder.cs` and `AuthorizedOrderAuth.cs` for the public auth pattern. See `ShowcaseAuthRemoteTests.cs` for the `[Remote]` auth method pattern.

#### Factory Interface Visibility Rules

The generated factory interface visibility derives from the methods. `[Remote]` promotes `internal` methods to `public` on the factory interface:

| Method Visibility | Generated Interface | Interface Members |
|---|---|---|
| All methods `public` (or `[Remote] internal`) | `public interface IXxxFactory` | All methods included as `public` |
| All methods `internal` (no `[Remote]`) | `internal interface IXxxFactory` | All methods included |
| Mix of `public`/`[Remote] internal` and plain `internal` | `public interface IXxxFactory` | All methods included; plain `internal` methods get `internal` modifier; `[Remote] internal` methods are promoted to `public` |

`[Remote] internal` methods are treated as `public` for interface visibility purposes -- they appear as `public` members on the factory interface because clients need to call them. Plain `internal` methods (without `[Remote]`) appear with the `internal` access modifier. An all-`internal` factory interface (e.g., `IOrderLineFactory` where no methods have `[Remote]`) is not injectable from the client container. The client cannot even see it. This is the desired behavior for child entity factories.

#### Internal Class with Public Interface Pattern

Entity classes are `internal` with a matching `public interface` (naming convention: `Order` -> `IOrder`). The generator detects the `I{ClassName}` interface and uses it in all factory signatures instead of the concrete class:

```csharp
// Public interface -- visible to client
public interface IOrder : IFactorySaveMeta
{
    int Id { get; set; }
    string CustomerName { get; set; }
}

// Internal class -- invisible to client
[Factory]
internal partial class Order : IOrder, IFactorySaveMeta { ... }

// Generated factory uses the interface type:
// public interface IOrderFactory
// {
//     Task<IOrder> Create(string customerName, ...);
//     Task<IOrder?> Save(IOrder target, ...);
// }
```

#### Auth Type Auto-Registration for Trimming

The generator emits explicit `services.TryAddTransient<IFooAuth, FooAuth>()` registrations in `FactoryServiceRegistrar` for every `[AuthorizeFactory<T>]` type. This creates static references that the IL trimmer preserves — without this, auth classes (often `internal`) would be trimmed because they're only discovered at runtime via `RegisterMatchingName` reflection.

The concrete type is resolved at compile time using the naming convention (`IPersonModelAuth` → `PersonModelAuth`). If the auth type argument is already a concrete class (not an interface), the generator registers it directly. If no matching concrete type is found in the compilation, no registration is emitted and the user must register it explicitly.

#### Trimming-Safe Factory Registration

The generator emits `[assembly: NeatooFactoryRegistrar(typeof(X))]` for every factory type (class, static, and interface). The `NeatooFactoryRegistrarAttribute` carries `[DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)]` on its `Type` property, which creates a dataflow contract the IL trimmer follows — ensuring each factory type's `FactoryServiceRegistrar` method (and all other methods) survive trimming.

At startup, `RegisterFactories()` enumerates these assembly attributes via `assembly.GetCustomAttributes<NeatooFactoryRegistrarAttribute>()` instead of scanning all types with `assembly.GetTypes()`. This makes factory discovery trimming-safe: the trimmer sees the static `typeof()` references in the assembly attributes and preserves the referenced types.

| Factory Pattern | Assembly Attribute Target |
|----------------|--------------------------|
| Class Factory | `typeof({Namespace}.{ClassName}Factory)` — the generated factory implementation class |
| Static Factory | `typeof({Namespace}.{StaticClassName})` — the static class itself |
| Interface Factory | `typeof({Namespace}.{ImplName}Factory)` — the generated factory implementation class |

This mechanism is internal to the generator and library. Users do not need to emit or configure these attributes — they are generated automatically for every `[Factory]`-annotated type.

#### DTO Constructor Registry for Trimming

The generator emits preservation calls in `FactoryServiceRegistrar` for DTO types discovered in factory method signatures (return types and non-service parameters), bucket-sorted by constructor shape: `DtoConstructorRegistry.Register<Dto>(() => new Dto())` for types with a public parameterless constructor, `DtoConstructorRegistry.PreserveType<Dto>()` for types with only parameterized public constructors (positional records). Both create static references that survive IL trimming — without them, `System.Text.Json` deserialization fails because `DefaultJsonTypeInfoResolver` uses reflection to discover constructors, and the trimmer strips that metadata from types in assemblies marked `IsTrimmable=true`.

At runtime, `NeatooJsonTypeInfoResolver` uses the registered lambda instead of `Activator.CreateInstance` (which also fails under trimming). If a type is not in DI and not in the DTO registry, `CreateObject` is not set — STJ uses its default behavior, which produces a clear error if the constructor was trimmed.

**DTO discovery criteria** — the generator preserves a discovered signature type when it:

| Criterion | Why |
|-----------|-----|
| Has at least one public constructor | Parameterless → `Register<T>(() => new T())`; parameterized-only (positional records) → `PreserveType<T>()`, deserialized via `RecordBypassConverterFactory` |
| Is NOT a `[Factory]`-annotated type | Already DI-registered; uses `GetRequiredService` path |
| Is NOT a primitive, string, or framework type | STJ handles these natively |
| Is NOT abstract or an interface | Cannot be instantiated |

(`record struct` edge: Roslyn reports the synthesized parameterless ctor, so value-type records land in the `Register` bucket; at runtime `RecordBypassConverterFactory` still claims them because reflection omits the implicit struct ctor. Both mechanisms preserve and round-trip them — the divergence is benign.)

The generator unwraps `Task<T>`, nullable `T?`, and generic collection types (`IReadOnlyList<T>`, `List<T>`, etc.) to discover the inner DTO type. Both `Register<T>` and `PreserveType<T>` carry `[DynamicallyAccessedMembers(All)]` on the type parameter, which instructs the trimmer to preserve the entire type — constructors, properties, and all metadata. `PreserveType<T>` deliberately does not populate the constructor registry — parameterized-ctor types never take the `CreateObject` path.

Duplicate registrations from multiple factories returning the same DTO type are idempotent (`ConcurrentDictionary.TryAdd`).

**Nested DTO discovery:** The generator recursively walks public instance properties (including inherited properties via base type chain) of each discovered DTO — classes and records alike — to find nested DTOs that also need preservation. Collection properties (`List<T>`, `IReadOnlyList<T>`, arrays) and nullable properties (`T?`) are unwrapped to find the inner DTO type. The same eligibility criteria and bucket rule apply to nested DTOs as to direct signature types. Cycle detection prevents infinite recursion from circular references (e.g., `DtoA` -> `DtoB` -> `DtoA`).

**Factory event type preservation.** `FactoryEventBase` itself carries `[FactoryEvent]` and `[DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]`, both with `Inherited = true`. Every descendant is therefore automatically discoverable by the runtime `FactoryEventTypeRegistry` and has its constructors and properties preserved through IL trimming with **no generator emission and no per-event annotation**. Inheriting `FactoryEventBase` is sufficient; consumers never apply `[FactoryEvent]` directly.

This supersedes the prior per-`[FactoryEventHandler<T>]` emission of `DtoConstructorRegistry.PreserveType<T>()` — the relay-handler pipeline no longer walks event types for trimming. Net result: stronger guarantee (covers every descendant, even those with no server handler), less generated code. `IFactoryEvents.Raise<T>` retains `[DynamicallyAccessedMembers(All)]` on its generic parameter for producer-side call-site preservation.

End-to-end verification via `src/Tests/RemoteFactory.TrimmingTests/EventRelaySmokeTest.cs` (publish-trimmed smoke test — confirms event relay round-trips across a trimmed binary).

**Nested property walking.** The per-handler nested-walk behavior for event record properties is gone with the rest of the per-handler trimming pipeline. Trim preservation comes exclusively from `[DynamicallyAccessedMembers]` on `FactoryEventBase`, which applies recursively to every descendant's public constructors and properties. For reference-type properties not reachable from an event record (or from a factory return type), use the existing `DtoConstructorRegistry.PreserveType<V>()` / `Register<V>()` mechanism in DI setup.

#### CS0051 Constraint

When a generated factory interface becomes `internal` (all methods are internal), it cannot be used as a `[Service]` parameter type in a `public` method on another class. C# enforces that parameter types must be at least as accessible as the method. This means `internal` is not applicable to entities whose factory interfaces are referenced in more-accessible methods' `[Service]` parameters. Use `internal` for leaf entities and standalone factories where the factory interface is not passed as a service parameter to public methods.

### 3. Static Factory Method Signatures
```csharp
// WRONG
[Remote, Execute]
public static Task<bool> SendNotification(...) { }  // Public, no underscore

// CORRECT
[Remote, Execute]
private static Task<bool> _SendNotification(...) { }  // Private, underscore prefix
```

### 4. Interface Factory Methods Need NO Attributes
```csharp
// WRONG — emits NF0106
[Factory]
public interface IMyRepository
{
    [Fetch]  // NF0106: factory-operation attribute on interface factory method
    Task<Item> GetByIdAsync(int id);
}

// CORRECT
[Factory]
public interface IMyRepository
{
    Task<Item> GetByIdAsync(int id);  // No attributes - all methods are remote
}
```
Enforced at compile time by **NF0106**. The rule applies to `[Create]`/`[Fetch]`/`[Insert]`/`[Update]`/`[Delete]`/`[Execute]` on any `[Factory]` interface method.

### 5. Properties Need Public Setters
```csharp
// WRONG - won't deserialize
public int Id { get; private set; }

// CORRECT - serialization works
public int Id { get; set; }
```

---

## Service Injection

### Constructor Injection = Client + Server
```csharp
public MyEntity([Service] ILogger logger)  // Available everywhere
```
Services injected via constructor are resolved from DI on both client and server. Use this when you need the service after the object crosses the wire.

### Method Injection = Server Only (Common Case)
```csharp
[Remote, Create]
internal void Create(string name, [Service] IRepository repo)  // Server only
```
Method-injected services stored in fields are NOT serialized - they'll be null after crossing the client/server boundary. If you need a service reference after serialization, use constructor injection.

---

## IFactorySaveMeta for Save Routing

```csharp
public partial class Order : IFactorySaveMeta
{
    public bool IsNew { get; set; }      // Public setter required
    public bool IsDeleted { get; set; }  // Public setter required

    [Remote, Insert]
    internal Task Insert(...) { }  // Called when IsNew=true, IsDeleted=false

    [Remote, Update]
    internal Task Update(...) { }  // Called when IsNew=false, IsDeleted=false

    [Remote, Delete]
    internal Task Delete(...) { }  // Called when IsDeleted=true
}
```

---

## Lifecycle Hooks

```csharp
public partial class Order : IFactoryOnStartAsync, IFactoryOnCompleteAsync
{
    public Task FactoryStartAsync(FactoryOperation op)
    {
        // Before operation - validation, logging, setup
        return Task.CompletedTask;
    }

    public Task FactoryCompleteAsync(FactoryOperation op)
    {
        // After operation - cleanup, reset flags
        if (op == FactoryOperation.Insert)
            IsNew = false;
        return Task.CompletedTask;
    }
}
```

---

## Server Setup (ASP.NET Core)

```csharp
// Program.cs
builder.Services.AddNeatooAspNetCore(typeof(Order).Assembly);
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

var app = builder.Build();
app.UseNeatoo();  // Adds /api/neatoo endpoint
```

---

## Client Setup (Hosted Blazor WASM)

```csharp
// Program.cs
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(Order).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
```

---

## Design Completeness Checklist

When reviewing or extending the Design source of truth, verify these patterns are demonstrated:

- [ ] At least one Class Factory with lifecycle hooks (`Order.cs`)
- [ ] At least one Interface Factory (`IExampleRepository` in `AllPatterns.cs`)
- [ ] At least one Static Factory with [Execute] (`ExampleCommands`)
- [ ] Child entities without [Remote] (`OrderLine.cs`)
- [ ] IFactorySaveMeta implementation with Insert/Update/Delete routing (`Order.cs`)
- [ ] Value objects that serialize correctly (`Money` in `ValueObjects/`)
- [ ] ASP.NET Core policy-based authorization (`SecureOrder.cs`)
- [x] Custom domain authorization with [AuthorizeFactory<T>] (`AuthorizedOrder.cs`, `AuthorizedOrderAuth.cs`)
- [x] Parameterized authorization: type-matched params and target entity params (`ParamAuthOrder.cs`, `ParamAuthOrderAuth.cs`)
- [x] Interface Factory returning a record type (`AllPatterns.cs`: `ExampleRecordResult` record, `IExampleRepository.GetRecordByIdAsync`)
- [x] Factory event handler (mediator) — server-side static handler (`FactoryEventHandlerPattern.cs`)
- [x] Factory event relay — consumer-implemented `IFactoryEventRelay.Relay(IReadOnlyList<FactoryEventBase>)` bridge with in-memory aggregator demo (`FactoryEventRelayPattern.cs`: `InMemoryAggregatorRelay`)
- [x] Event record with a nested record property — exercises automatic IL-trimming preservation for both the event type and the nested record (`FactoryEventHandlerPattern.cs`: `OrderShippedEvent` with `ShippingAddress`, `OrderShippedHandlers`)
- [x] LazyLoad<T> property with constructor-initialization pattern (`LazyLoadExample.cs`)

---

## Design Debt and Future Considerations

These are known limitations or open questions. They are documented here to prevent repeated re-proposals of the same trade-offs.

| Topic | Current State | Why Deferred | Reconsider When |
|-------|--------------|--------------|-----------------|
| Private setter support | Not supported | Adds reflection, incompatible with IL trimming | If .NET adds source-generator-accessible private member access |
| OR logic for [AspAuthorize] | Only AND logic | Matches ASP.NET Core behavior, safer default | User demand + clear use case |
| Automatic [Remote] detection | Must be explicit | Security risk of accidental exposure | Never - explicit is a core principle |
| Collection factory injection | Requires local mode for AddLine | Serialize factories would add complexity | If common complaint from users |
| IEnumerable<T> serialization | Only concrete collections | Type preservation complexity | User demand for interface collections |

---

## Diagnostics and Log Events (Factory Events Relay)

### Compile-Time Diagnostics

| ID | Severity | Fires When | Fix |
|----|----------|-----------|-----|
| NF0501 | Error | `[FactoryEventHandler<T>]` class has no matching method | Declare exactly one method returning `Task` whose first non-`[Service]`/non-`CancellationToken` parameter is of type `T` |
| NF0502 | Error | `[FactoryEventHandler<T>]` class has multiple matching methods | Remove the extras or split into separate handler classes |
| NF0503 | Warning | `[FactoryEventHandler<T>]` class has an **instance**-method handler (former client-relay pattern) | Make the method `static` (server-side handler) **or** implement `IFactoryEventRelay` on the class and register it in DI (client-side reception). Instance methods are silently skipped at runtime. |

### Runtime Log Events

| EventId | Name | Level | Fires When | Propagation |
|---------|------|-------|-----------|-------------|
| 3008 | `FactoryEventRelayFailed` | Error | Consumer's `IFactoryEventRelay.Relay` throws | Swallowed — never propagates to the factory caller |
| 3009 | `FactoryEventDeserializationFailed` | Error | Wire-format event deserialization fails (e.g. `UnknownFactoryEventTypeException`) | Swallowed; `Relay` is NOT invoked for that call (the one legitimate case of zero `Relay` invocations for a [Remote] call) |
| 3011 | `NoOpFactoryEventRelayFirstEvent` | Warning | `NoOpFactoryEventRelay` receives its first non-empty batch (consumer forgot to register a relay) | Informational; fires once per process |
| 3012 | `FactoryEventTypeRegistryCollision` | Warning | `FactoryEventTypeRegistry` assembly scan finds two distinct `Type`s sharing the same `FullName` | Documents kept/dropped assembly; wire messages resolve to the kept type |

### Public Exception

`UnknownFactoryEventTypeException` — thrown by the internal `FactoryEventDeserializer` when a wire-format `TypeFullName` does not resolve via `FactoryEventTypeRegistry` (after a rescan to pick up dynamically-loaded assemblies). Caught at the dispatch isolation boundary and logged as 3009. Carries `UnresolvedTypeFullName` (string) and `BatchTypeFullNames` (`IReadOnlyList<string>`) for diagnostics. Four constructors per CA1032.

---

## Common Mistakes to Avoid (Summary)

1. **Adding [Remote] to child entities** - Children are server-side only
2. **Public static factory methods** - Must be `private static` with underscore
3. **Private property setters** - Won't serialize/deserialize
4. **[Fetch] on interface methods** - Interface factories don't use operation attributes
5. **Method-injected services stored in fields** - Lost after serialization; use constructor injection
6. **Missing partial keyword** - Generator needs to extend your class
7. **[Remote] on public methods** - `[Remote]` requires `internal` for IL trimming. `[Remote] public` emits NF0105. Change to `internal`.
8. **Mixing Neatoo types with records in Interface Factory return types** - Records bypass reference handling entirely (`RecordBypassConverterFactory`), so embedded Neatoo types lose reference tracking. Use pure records/DTOs or pure Neatoo types, not both.
9. **Raising factory events outside a factory method** - The request-scoped `IFactoryEventCollector` only exists server-side during a factory operation. Raise events via `[Service] IFactoryEvents` from inside a factory method.
10. **Stacking `[Factory]` on a `[FactoryEventHandler<T>]` class** - They run in separate generator pipelines. Handler classes are subscribers, not factories. Do not add `[Factory]`.

---

## Design Files to Consult

| File | Contains |
|------|----------|
| `Design.Domain/FactoryPatterns/AllPatterns.cs` | All three patterns side-by-side with extensive comments |
| `Design.Domain/Aggregates/Order.cs` | Complete aggregate with lifecycle hooks and IFactorySaveMeta |
| `Design.Domain/Aggregates/AuthorizedOrder.cs` | [AuthorizeFactory<T>] on a CLASS FACTORY — CRUD scopes, CanCreate/CanFetch/CanSave/CanDelete |
| `Design.Domain/Aggregates/AuthorizedOrderAuth.cs` | Auth interface and implementation for AuthorizedOrder |
| `Design.Domain/Aggregates/AuthorizedRepository.cs` | [AuthorizeFactory<T>] on an INTERFACE FACTORY — Execute/Read scopes, parameter matching, Can{Method} per interface method |
| `Design.Domain/Aggregates/ParamAuthOrder.cs` | Parameterized [AuthorizeFactory<T>] with type-matched and target entity params |
| `Design.Domain/Aggregates/ParamAuthOrderAuth.cs` | Auth interface and implementation with parameterized methods |
| `Design.Domain/Aggregates/SecureOrder.cs` | [AspAuthorize] policy-based authorization patterns |
| `Design.Domain/Entities/OrderLine.cs` | Child entity (no [Remote]) - demonstrates entity duality |
| `Design.Domain/ValueObjects/Money.cs` | Record-based value object serialization |
| `Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs` | `[FactoryEventHandler<T>]` class attribute with `static` method — server-side handler running in the caller's DI scope (shared DbContext/transaction), sequential, awaited |
| `Design.Domain/FactoryPatterns/FactoryEventRelayPattern.cs` | Consumer-implemented `IFactoryEventRelay.Relay` — bridges relayed events to an aggregator (demonstrates the `InMemoryAggregatorRelay` reference impl) |
| `Design.Domain/FactoryPatterns/LazyLoadExample.cs` | LazyLoad<T> property with constructor-initialization and deferred loading |
| `Design.Tests/FactoryTests/*.cs` | Working examples of each pattern |
| `Design.Tests/FactoryTests/FactoryEventRelayTests.cs` | `Relay(IReadOnlyList<FactoryEventBase>)` invocation, batch contents, `RaiseOptions.ServerOnly` exclusion, empty-batch still invokes Relay once |
| `Design.Tests/FactoryTests/ParamAuthorizationTests.cs` | Parameterized auth: type-matched params, target params, CanXxx suppression |
| `Design.Tests/FactoryTests/InterfaceFactoryAuthorizationTests.cs` | Interface-factory auth: Execute/Read scopes, parameter matching, Can{Method}, NotAuthorizedException across client/server |
| `Design.Tests/FactoryTests/LazyLoadTests.cs` | LazyLoad<T> round-trip and deferred loading tests |
| `Design.Tests/FactoryTests/SerializationTests.cs` | Round-trip serialization validation |
| `Design.Tests/TestInfrastructure/DesignClientServerContainers.cs` | Two DI container test pattern |
| `Design.Server/Program.cs` | Server configuration |
| `Design.Client.Blazor/Program.cs` | Client configuration |
