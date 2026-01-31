# CLAUDE-DESIGN.md

This file provides Claude Code with the authoritative reference for RemoteFactory's design. When proposing changes or implementing features, consult this document and the Design.Domain code.

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
        [Service] IMyService service) => await service.NotifyAsync(entityId);
}
```
**Generates**:
- `MyCommands.DoSomething` delegate (Execute)
- `MyCommands.OnSomethingHappenedEvent` delegate (Event - note `Event` suffix)

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
private static Task _OnOrderPlaced(int orderId, ...) { }

// Generated delegate type:
MyEvents.OnOrderPlacedEvent  // Note: Event suffix added
```

## Service Injection

### Constructor Injection = Client + Server
```csharp
public MyEntity([Service] ILogger logger)  // Available everywhere
```

### Method Injection = Server Only (Common Case)
```csharp
[Remote, Create]
public void Create(string name, [Service] IRepository repo)  // Server only
```

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

## Server Setup (ASP.NET Core)

```csharp
// Program.cs
builder.Services.AddNeatooAspNetCore(typeof(Order).Assembly);
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

var app = builder.Build();
app.UseNeatoo();  // Adds /remotefactory endpoint
```

## Client Setup (Blazor WASM)

```csharp
// Program.cs
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(Order).Assembly);
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri("http://localhost:5000/") });
```

## Testing Pattern

Use `DesignClientServerContainers.Scopes()` for client/server simulation:

```csharp
[Fact]
public async Task Test_RemoteOperation()
{
    var (server, client, local) = DesignClientServerContainers.Scopes();

    // client - simulates Blazor WASM (remote calls)
    // server - simulates ASP.NET Core (where [Remote] executes)
    // local - single-tier mode (no serialization)

    var factory = client.GetRequiredService<IOrderFactory>();
    var order = await factory.Create("Customer");  // Goes through serialization

    server.Dispose();
    client.Dispose();
}
```

## Common Mistakes to Avoid

1. **Adding [Remote] to child entities** - Children are server-side only
2. **Public static factory methods** - Must be `private static` with underscore
3. **Private property setters** - Won't serialize/deserialize
4. **[Fetch] on interface methods** - Interface factories don't use operation attributes
5. **Forgetting Event suffix** - `OnOrderPlacedEvent` not `OnOrderPlaced`
6. **Calling AddLine after remote fetch** - Factory references lost in serialization

## Design Files to Consult

| File | Contains |
|------|----------|
| `Design.Domain/FactoryPatterns/AllPatterns.cs` | All three patterns side-by-side |
| `Design.Domain/Aggregates/Order.cs` | Complete aggregate with lifecycle |
| `Design.Domain/Entities/OrderLine.cs` | Child entity (no [Remote]) |
| `Design.Tests/FactoryTests/*.cs` | Working examples of each pattern |
| `Design.Server/Program.cs` | Server configuration |
| `Design.Client.Blazor/Program.cs` | Client configuration |
