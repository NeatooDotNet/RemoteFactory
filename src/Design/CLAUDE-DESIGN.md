# CLAUDE-DESIGN.md

---
design_version: 1.0
last_updated: 2026-02-01
target_frameworks: [net8.0, net9.0, net10.0]
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
| **Static Factory** | Stateless commands, events, side effects | `EmailCommands.SendNotification`, `OrderEvents.OnOrderPlaced` | No instance state, [Execute] or [Event] operations |

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
- You need fire-and-forget event handling
- Operations are naturally expressed as functions, not methods on objects
- You want CancellationToken support for graceful shutdown

---

## Quick Reference: The Three Factory Patterns

### Pattern 1: Class Factory
```csharp
[Factory]
public partial class MyEntity
{
    public int Id { get; set; }  // Public setter required for serialization

    [Remote, Create]
    public void Create(string name, [Service] IMyService service) { }

    [Remote, Fetch]
    public void Fetch(int id, [Service] IMyService service) { }
}
```
**Generates**: `IMyEntityFactory` with `Create()`, `Fetch()` methods.

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

### Pattern 3: Static Factory
```csharp
[Factory]
public static partial class MyCommands
{
    [Remote, Execute]
    private static Task<bool> _DoSomething(      // Underscore prefix, private
        string input,
        [Service] IMyService service) => service.ProcessAsync(input);

    [Remote, Event]
    private static async Task _OnSomethingHappened(  // Events run in isolated scope
        int entityId,
        [Service] IMyService service,
        CancellationToken cancellationToken) => await service.NotifyAsync(entityId);
}
```
**Generates**:
- `MyCommands.DoSomething` delegate (Execute)
- `MyCommands.OnSomethingHappenedEvent` delegate (Event - note `Event` suffix)

---

## Quick Decisions Table

| Question | Answer | Reference | Reason |
|----------|--------|-----------|--------|
| Should this method be [Remote]? | Only aggregate root entry points | `Order.cs` vs `OrderLine.cs` | Once on server, stay on server |
| Can I use private setters? | No | `AllPatterns.cs:73` | AOT compilation + source generation |
| Should interface methods have attributes? | No | `AllPatterns.cs:203` | Interface IS the boundary |
| Do I need `partial` keyword? | Yes, always | `AllPatterns.cs:49` | Generator adds code to class |
| Should child entities have [Remote]? | No | `OrderLine.cs:27-41` | Would cause N+1 remote calls |
| Can [Execute] return void? | No, must return Task<T> | `AllPatterns.cs:340-347` | Client needs result to confirm |
| Do [Event] methods need CancellationToken? | Yes, as final parameter | `AllPatterns.cs:417-427` | Graceful shutdown support |
| Where does business logic go? | In the entity, not the factory | `Order.cs:229-242` | DDD principle |
| Can I store method-injected services? | Only if using constructor injection | `AllPatterns.cs:86-96` | Fields lost after serialization |

---

## Anti-Patterns (What NOT to Do)

### Anti-Pattern 1: [Remote] on Child Entities

**WRONG:**
```csharp
[Factory]
public partial class OrderLine
{
    [Remote, Create]  // WRONG: Causes N+1 remote calls
    public void Create(string productName, decimal price, int qty) { }
}
```

**RIGHT:**
```csharp
[Factory]
public partial class OrderLine
{
    [Create]  // No [Remote] - called from server-side Order operations
    public void Create(string productName, decimal price, int qty) { }
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
    [Fetch]  // WRONG: Causes duplicate generation
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

**Why it matters:** The generator treats all interface methods as remote. Adding operation attributes creates duplicate registrations.

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
public partial class MyEntity
{
    private IMyService _service;  // WRONG: Lost after serialization

    [Remote, Create]
    public void Create([Service] IMyService service)
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
public void Update([Service] IMyService service)
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

**WRONG (applying [Remote] because "it's a Product"):**
```csharp
// Product.cs - used as aggregate root AND as child of Order
[Factory]
public partial class Product
{
    [Remote, Fetch]  // WRONG if Product is also fetched as part of Order
    public void Fetch(int id, [Service] IProductRepository repo) { }
}
```

**RIGHT (separate operations for different contexts):**
```csharp
[Factory]
public partial class Product
{
    [Remote, Fetch]  // Aggregate root context - client entry point
    public void Fetch(int id, [Service] IProductRepository repo) { }

    [Fetch]  // Child context - called from Order.Fetch on server
    public void FetchAsChild(int id, string name, decimal price) { }
}
```

**Why it matters:** [Remote] is about *how the method is called*, not *what the type is*. Same type can have both remote and non-remote operations.

---

## Critical Rules

### 1. [Remote] is ONLY for Aggregate Root Entry Points
```csharp
// CORRECT: Aggregate root has [Remote]
[Factory]
public partial class Order
{
    [Remote, Create]  // Client entry point
    public void Create(...) { }
}

// CORRECT: Child entity does NOT have [Remote]
[Factory]
public partial class OrderLine
{
    [Create]  // Server-side only - called from Order operations
    public void Create(...) { }
}
```

### 2. Static Factory Method Signatures
```csharp
// WRONG
[Remote, Execute]
public static Task<bool> SendNotification(...) { }  // Public, no underscore

// CORRECT
[Remote, Execute]
private static Task<bool> _SendNotification(...) { }  // Private, underscore prefix
```

### 3. Interface Factory Methods Need NO Attributes
```csharp
// WRONG
[Factory]
public interface IMyRepository
{
    [Fetch]  // Don't do this
    Task<Item> GetByIdAsync(int id);
}

// CORRECT
[Factory]
public interface IMyRepository
{
    Task<Item> GetByIdAsync(int id);  // No attributes - all methods are remote
}
```

### 4. Properties Need Public Setters
```csharp
// WRONG - won't deserialize
public int Id { get; private set; }

// CORRECT - serialization works
public int Id { get; set; }
```

### 5. Event Delegate Types Have `Event` Suffix
```csharp
// In static class:
[Remote, Event]
private static Task _OnOrderPlaced(int orderId, ..., CancellationToken ct) { }

// Generated delegate type:
MyEvents.OnOrderPlacedEvent  // Note: Event suffix added
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
public void Create(string name, [Service] IRepository repo)  // Server only
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
    public Task Insert(...) { }  // Called when IsNew=true, IsDeleted=false

    [Remote, Update]
    public Task Update(...) { }  // Called when IsNew=false, IsDeleted=false

    [Remote, Delete]
    public Task Delete(...) { }  // Called when IsDeleted=true
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

## Client Setup (Blazor WASM)

```csharp
// Program.cs
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(Order).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri("http://localhost:5000/") });
```

---

## Design Completeness Checklist

When reviewing or extending the Design source of truth, verify these patterns are demonstrated:

- [ ] At least one Class Factory with lifecycle hooks (`Order.cs`)
- [ ] At least one Interface Factory (`IExampleRepository` in `AllPatterns.cs`)
- [ ] At least one Static Factory with [Execute] and [Event] (`ExampleCommands`, `ExampleEvents`)
- [ ] Child entities without [Remote] (`OrderLine.cs`)
- [ ] IFactorySaveMeta implementation with Insert/Update/Delete routing (`Order.cs`)
- [ ] Value objects that serialize correctly (`Money` in `ValueObjects/`)
- [ ] Event handlers with CancellationToken (`ExampleEvents._OnOrderPlaced`)
- [x] CorrelationContext usage (`CorrelationExample.cs`)
- [ ] ASP.NET Core policy-based authorization (`SecureOrder.cs`)

---

## Design Debt and Future Considerations

These are known limitations or open questions. They are documented here to prevent repeated re-proposals of the same trade-offs.

| Topic | Current State | Why Deferred | Reconsider When |
|-------|--------------|--------------|-----------------|
| Private setter support | Not supported | Breaks AOT, adds reflection | If .NET adds AOT-compatible private member access |
| OR logic for [AspAuthorize] | Only AND logic | Matches ASP.NET Core behavior, safer default | User demand + clear use case |
| Automatic [Remote] detection | Must be explicit | Security risk of accidental exposure | Never - explicit is a core principle |
| Collection factory injection | Requires local mode for AddLine | Serialize factories would add complexity | If common complaint from users |
| IEnumerable<T> serialization | Only concrete collections | Type preservation complexity | User demand for interface collections |

---

## Common Mistakes to Avoid (Summary)

1. **Adding [Remote] to child entities** - Children are server-side only
2. **Public static factory methods** - Must be `private static` with underscore
3. **Private property setters** - Won't serialize/deserialize
4. **[Fetch] on interface methods** - Interface factories don't use operation attributes
5. **Forgetting Event suffix** - `OnOrderPlacedEvent` not `OnOrderPlaced`
6. **Method-injected services stored in fields** - Lost after serialization; use constructor injection
7. **Missing partial keyword** - Generator needs to extend your class
8. **Missing CancellationToken on events** - Required for graceful shutdown

---

## Design Files to Consult

| File | Contains |
|------|----------|
| `Design.Domain/FactoryPatterns/AllPatterns.cs` | All three patterns side-by-side with extensive comments |
| `Design.Domain/Aggregates/Order.cs` | Complete aggregate with lifecycle hooks and IFactorySaveMeta |
| `Design.Domain/Aggregates/SecureOrder.cs` | [AspAuthorize] policy-based authorization patterns |
| `Design.Domain/Entities/OrderLine.cs` | Child entity (no [Remote]) - demonstrates entity duality |
| `Design.Domain/ValueObjects/Money.cs` | Record-based value object serialization |
| `Design.Domain/Services/CorrelationExample.cs` | CorrelationContext usage for distributed tracing |
| `Design.Tests/FactoryTests/*.cs` | Working examples of each pattern |
| `Design.Tests/FactoryTests/SerializationTests.cs` | Round-trip serialization validation |
| `Design.Tests/TestInfrastructure/DesignClientServerContainers.cs` | Two DI container test pattern |
| `Design.Server/Program.cs` | Server configuration |
| `Design.Client.Blazor/Program.cs` | Client configuration |
| `Design.Client.Blazor/AssemblyAttributes.cs` | Assembly-level [FactoryMode] configuration |
