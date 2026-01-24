# Factory Operations

RemoteFactory supports seven operation types, each mapping to common data access patterns: Create, Fetch, Insert, Update, Delete, Execute, and Event.

## Overview

| Operation | Purpose | Typical Use | Return Handling |
|-----------|---------|-------------|-----------------|
| `[Create]` | Create new instances | Constructors, factory methods | Instance or null |
| `[Fetch]` | Load existing data | Data retrieval | bool/void for success, or instance |
| `[Insert]` | Persist new entities | First save | void or bool |
| `[Update]` | Persist changes | Subsequent saves | void or bool |
| `[Delete]` | Remove entities | Deletion | void or bool |
| `[Execute]` | Business operations | Commands, queries | Any type |
| `[Event]` | Fire-and-forget | Domain events | void or Task (always returns Task) |

## Create Operation

Creates new instances via constructors or static factory methods.

### Constructor-based Creation

<!-- snippet: operations-create-constructor -->
<a id='snippet-operations-create-constructor'></a>
```cs
[Factory]
public partial class ProductWithConstructorCreate
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Created { get; private set; }

    [Create]
    public ProductWithConstructorCreate()
    {
        Id = Guid.NewGuid();
        Created = DateTime.UtcNow;
        Price = 0.00m;
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L11-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-constructor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generated factory interface:
```csharp
public interface IProductWithConstructorCreateFactory
{
    ProductWithConstructorCreate Create(CancellationToken cancellationToken = default);
}
```

### Static Factory Method

<!-- snippet: operations-create-static -->
<a id='snippet-operations-create-static'></a>
```cs
[Factory]
public partial class ProductWithStaticCreate
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal Price { get; set; }

    private ProductWithStaticCreate() { }

    [Create]
    public static ProductWithStaticCreate Create(string sku, string name, decimal initialPrice)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required", nameof(sku));

        return new ProductWithStaticCreate
        {
            Id = Guid.NewGuid(),
            Sku = sku.ToUpperInvariant(),
            Name = name,
            Price = initialPrice
        };
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L30-L56' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-static' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generated factory interface:
```csharp
public interface IProductWithStaticCreateFactory
{
    ProductWithStaticCreate Create(string sku, string name, decimal initialPrice, CancellationToken cancellationToken = default);
}
```

### Return Value Handling

Create methods support multiple return types:

<!-- snippet: operations-create-return-types -->
<a id='snippet-operations-create-return-types'></a>
```cs
[Factory]
public partial class CreateReturnTypesExample
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;
    public bool Initialized { get; private set; }

    // Constructor Create - sets properties on created instance
    [Create]
    public CreateReturnTypesExample()
    {
        Id = Guid.NewGuid();
    }

    // Instance method Create - can modify this and return void
    [Create]
    public void Initialize(string name)
    {
        Name = name;
        Initialized = true;
    }

    // Static method Create - returns new instance
    [Create]
    public static CreateReturnTypesExample CreateWithDefaults()
    {
        return new CreateReturnTypesExample
        {
            Id = Guid.NewGuid(),
            Name = "Default",
            Initialized = true
        };
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L58-L93' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-return-types' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Factory return types:
- **void**: Returns the instance
- **bool**: Returns instance if true, null if false
- **Task**: Returns instance after await
- **Task\<bool\>**: Returns instance if true, null if false
- **Task\<T\>**: Returns the T instance
- **T**: Returns the instance

## Fetch Operation

Loads data into an existing instance.

### Instance Method Fetch

<!-- snippet: operations-fetch-instance -->
<a id='snippet-operations-fetch-instance'></a>
```cs
[Factory]
public partial class OrderFetchExample
{
    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public decimal Total { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public bool IsNew { get; private set; } = true;

    [Create]
    public OrderFetchExample() { }

    [Remote]
    [Fetch]
    public async Task Fetch(Guid orderId, [Service] IOrderRepository repository)
    {
        var entity = await repository.GetByIdAsync(orderId)
            ?? throw new InvalidOperationException($"Order {orderId} not found");

        Id = entity.Id;
        OrderNumber = entity.OrderNumber;
        Total = entity.Total;
        Status = entity.Status;
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L95-L122' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-fetch-instance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generated factory interface:
```csharp
public interface IOrderFetchExampleFactory
{
    OrderFetchExample Create(CancellationToken cancellationToken = default);
    Task<OrderFetchExample> Fetch(Guid orderId, CancellationToken cancellationToken = default);
}
```

The factory creates a new instance, calls Fetch, and returns the instance. If Fetch throws, the exception propagates.

### Parameters and Return Types

Fetch supports:
- **void**: Returns non-nullable instance, throws on error
- **bool**: True = success, false = not found (factory returns null, generated signature is nullable)
- **Task**: Returns non-nullable instance, throws on error
- **Task\<bool\>**: True = success, false = not found (factory returns null, generated signature is nullable)

<!-- snippet: operations-fetch-bool-return -->
<a id='snippet-operations-fetch-bool-return'></a>
```cs
[Factory]
public partial class OrderFetchBoolExample
{
    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public decimal Total { get; private set; }

    [Create]
    public OrderFetchBoolExample() { }

    [Remote]
    [Fetch]
    public async Task<bool> TryFetch(Guid orderId, [Service] IOrderRepository repository)
    {
        var entity = await repository.GetByIdAsync(orderId);
        if (entity == null)
            return false;

        Id = entity.Id;
        OrderNumber = entity.OrderNumber;
        Total = entity.Total;
        return true;
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L124-L149' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-fetch-bool-return' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generated factory interface for bool return:
```csharp
public interface IOrderFetchBoolExampleFactory
{
    OrderFetchBoolExample Create(CancellationToken cancellationToken = default);
    Task<OrderFetchBoolExample?> TryFetch(Guid orderId, CancellationToken cancellationToken = default);
}
```

Note the nullable return type when Fetch returns bool.

## Insert, Update, Delete Operations

Write operations for persisting changes.

### Insert

<!-- snippet: operations-insert -->
<a id='snippet-operations-insert'></a>
```cs
[Factory]
public partial class OrderInsertExample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public OrderInsertExample()
    {
        Id = Guid.NewGuid();
        OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8]}";
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IOrderRepository repository)
    {
        var entity = new OrderEntity
        {
            Id = Id,
            OrderNumber = OrderNumber,
            Total = Total,
            Status = "Pending",
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };

        await repository.AddAsync(entity);
        await repository.SaveChangesAsync();
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L151-L187' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Update

<!-- snippet: operations-update -->
<a id='snippet-operations-update'></a>
```cs
[Factory]
public partial class OrderUpdateExample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public OrderUpdateExample() { Id = Guid.NewGuid(); }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(Guid orderId, [Service] IOrderRepository repository)
    {
        var entity = await repository.GetByIdAsync(orderId);
        if (entity == null) return false;

        Id = entity.Id;
        OrderNumber = entity.OrderNumber;
        Total = entity.Total;
        Status = entity.Status;
        IsNew = false;
        return true;
    }

    [Remote]
    [Update]
    public async Task Update([Service] IOrderRepository repository)
    {
        var entity = await repository.GetByIdAsync(Id)
            ?? throw new InvalidOperationException($"Order {Id} not found");

        entity.Total = Total;
        entity.Status = Status;
        entity.Modified = DateTime.UtcNow;

        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L189-L233' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Delete

<!-- snippet: operations-delete -->
<a id='snippet-operations-delete'></a>
```cs
[Factory]
public partial class OrderDeleteExample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public OrderDeleteExample() { Id = Guid.NewGuid(); }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(Guid orderId, [Service] IOrderRepository repository)
    {
        var entity = await repository.GetByIdAsync(orderId);
        if (entity == null) return false;
        Id = entity.Id;
        IsNew = false;
        return true;
    }

    [Remote]
    [Delete]
    public async Task Delete([Service] IOrderRepository repository)
    {
        await repository.DeleteAsync(Id);
        await repository.SaveChangesAsync();
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L235-L265' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Multiple attributes on one method:

<!-- snippet: operations-insert-update -->
<a id='snippet-operations-insert-update'></a>
```cs
[Factory]
public partial class OrderUpsertExample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string OrderNumber { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public OrderUpsertExample()
    {
        Id = Guid.NewGuid();
        OrderNumber = $"ORD-{Guid.NewGuid().ToString()[..8]}";
    }

    [Remote]
    [Insert, Update]
    public async Task Upsert([Service] IOrderRepository repository)
    {
        var entity = await repository.GetByIdAsync(Id);

        if (entity == null)
        {
            entity = new OrderEntity
            {
                Id = Id,
                OrderNumber = OrderNumber,
                Total = Total,
                Status = "Pending",
                Created = DateTime.UtcNow,
                Modified = DateTime.UtcNow
            };
            await repository.AddAsync(entity);
        }
        else
        {
            entity.Total = Total;
            entity.Modified = DateTime.UtcNow;
            await repository.UpdateAsync(entity);
        }

        await repository.SaveChangesAsync();
        IsNew = false;
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L267-L314' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-insert-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generated factory interfaces include methods that operate on the instance:
```csharp
public interface IOrderInsertExampleFactory
{
    OrderInsertExample Create(CancellationToken cancellationToken = default);
    Task Insert(OrderInsertExample instance, CancellationToken cancellationToken = default);
}

public interface IOrderUpdateExampleFactory
{
    OrderUpdateExample Create(CancellationToken cancellationToken = default);
    Task<OrderUpdateExample?> Fetch(Guid orderId, CancellationToken cancellationToken = default);
    Task Update(OrderUpdateExample instance, CancellationToken cancellationToken = default);
}

public interface IOrderDeleteExampleFactory
{
    OrderDeleteExample Create(CancellationToken cancellationToken = default);
    Task<OrderDeleteExample?> Fetch(Guid orderId, CancellationToken cancellationToken = default);
    Task Delete(OrderDeleteExample instance, CancellationToken cancellationToken = default);
}
```

Return value handling:
- **void**: Operation succeeded
- **bool**: True = success, false = not authorized or not found
- **Task**: Operation succeeded after await
- **Task\<bool\>**: True = success, false = not authorized or not found

## Execute Operation

Business operations that don't fit Create/Fetch/Write patterns.

<!-- snippet: operations-execute -->
<a id='snippet-operations-execute'></a>
```cs
// [Execute] must be in static partial class
public record OrderApprovalResult(Guid OrderId, bool IsApproved, string? ApprovalNotes);

[SuppressFactory] // Nested in wrapper class - pattern demonstration only
public static partial class OrderApprovalCommand
{
    [Remote]
    [Execute]
    private static async Task<OrderApprovalResult> _ApproveOrder(
        Guid orderId,
        string notes,
        [Service] IOrderRepository repository)
    {
        var entity = await repository.GetByIdAsync(orderId)
            ?? throw new InvalidOperationException($"Order {orderId} not found");

        entity.Status = "Approved";
        entity.Modified = DateTime.UtcNow;

        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();

        return new OrderApprovalResult(orderId, true, notes);
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L316-L342' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generated delegate (not a factory interface):
```csharp
// Method: _ApproveOrder → Delegate: ApproveOrder (underscore prefix removed)
public static partial class OrderApprovalCommand
{
    public delegate Task<OrderApprovalResult> ApproveOrder(
        Guid orderId,
        string notes,
        CancellationToken cancellationToken = default);
}
```

Execute operations generate delegates registered in DI. The delegate name is derived from the method name with underscore prefix removed. They can return any type and accept any parameters.

### Command Pattern

<!-- snippet: operations-execute-command -->
<a id='snippet-operations-execute-command'></a>
```cs
// Command pattern using static partial class
public record OrderShippingResult(Guid OrderId, string TrackingNumber, DateTime ShippedAt, bool Success);

[SuppressFactory] // Nested in wrapper class - pattern demonstration only
public static partial class OrderShippingCommand
{
    [Remote]
    [Execute]
    private static async Task<OrderShippingResult> _ShipOrder(
        Guid orderId,
        string carrier,
        string trackingNumber,
        [Service] IOrderRepository repository)
    {
        var entity = await repository.GetByIdAsync(orderId)
            ?? throw new InvalidOperationException($"Order {orderId} not found");

        if (entity.Status != "Approved")
        {
            throw new InvalidOperationException(
                $"Order must be approved before shipping. Current status: {entity.Status}");
        }

        entity.Status = "Shipped";
        entity.Modified = DateTime.UtcNow;

        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();

        return new OrderShippingResult(orderId, trackingNumber, DateTime.UtcNow, true);
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L344-L377' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Event Operation

Fire-and-forget operations with scope isolation.

Events run asynchronously without blocking the caller. They execute in a separate DI scope for transactional independence.

<!-- snippet: operations-event -->
<a id='snippet-operations-event'></a>
```cs
[SuppressFactory] // Nested in wrapper class - pattern demonstration only
public partial class OrderEventExample
{
    public Guid OrderId { get; set; }

    [Create]
    public OrderEventExample() { }

    [Event]
    public async Task SendOrderConfirmationEmail(
        Guid orderId,
        string customerEmail,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            customerEmail,
            "Order Confirmation",
            $"Your order {orderId} has been received.",
            ct);
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L379-L402' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-event' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generated delegate (not a factory interface):
```csharp
// Method: SendOrderConfirmationEmail → Delegate: SendOrderConfirmationEmailEvent (Event suffix added)
public partial class OrderEventExample
{
    // CancellationToken is NOT included in delegate signature - framework provides it
    public delegate Task SendOrderConfirmationEmailEvent(
        Guid orderId,
        string customerEmail);
}
```

The delegate name is the method name with "Event" suffix appended. CancellationToken is required in the method signature but excluded from the generated delegate (the framework provides ApplicationStopping token). Both void and Task methods generate Task-returning delegates.

Key characteristics:
- **Scope isolation**: New DI scope per event
- **Fire-and-forget**: Caller doesn't wait for completion
- **Graceful shutdown**: EventTracker waits for pending events
- **CancellationToken required**: Must be last parameter, receives ApplicationStopping

### EventTracker

Track event completion for testing or shutdown:

<!-- snippet: operations-event-tracker -->
<a id='snippet-operations-event-tracker'></a>
```cs
// [Fact]
public async Task EventTracker_WaitForPendingEvents()
{
    var scopes = SampleTestContainers.Scopes();
    var eventTracker = scopes.local.GetRequiredService<IEventTracker>();

    // Fire event using namespace-level event type (from EventsSamples.cs)
    var sendEmail = scopes.local.GetRequiredService<OrderWithEvents.SendOrderConfirmationEvent>();
    _ = sendEmail(Guid.NewGuid(), "test@example.com");

    // Wait for all pending events to complete
    await eventTracker.WaitAllAsync();

    Assert.Equal(0, eventTracker.PendingCount);
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L404-L420' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-event-tracker' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Return value handling:
- **void**: Converted to Task automatically
- **Task**: Tracked by EventTracker

## Remote Attribute

Marks methods that execute on the server. Without `[Remote]`, methods execute locally.

<!-- snippet: operations-remote -->
<a id='snippet-operations-remote'></a>
```cs
[Factory]
public partial class RemoteOperationExample
{
    public string Result { get; private set; } = string.Empty;

    [Create]
    public RemoteOperationExample() { }

    // This method executes on the server when called from a remote client
    // The client serializes parameters, sends via HTTP, server executes and returns result
    [Remote]
    [Fetch]
    public Task FetchFromServer(string query, [Service] IPersonRepository repository)
    {
        // This code runs on the server
        Result = $"Executed on server with query: {query}";
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L422-L442' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-remote' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

When a factory is registered with `NeatooFactory.Remote`:
1. Factory method (e.g., `Fetch()`) routes to `RemoteFetch()`
2. Parameters serialized
3. HTTP POST to `/api/neatoo`
4. Server executes method with injected services
5. Response serialized and returned

When a factory is registered with `NeatooFactory.Logical` or `NeatooFactory.Server`:
- Factory method routes to `LocalFetch()`
- Direct method execution
- No serialization, no HTTP call

Use `[Remote]` when:
- Method requires server-only services (database, file system)
- Method performs operations not allowed on client
- Method accesses sensitive data

## Lifecycle Hooks

Interfaces for operation lifecycle:

### IFactoryOnStart / IFactoryOnStartAsync

Called before the operation executes. Use `IFactoryOnStartAsync` for async validation or preparation:

<!-- snippet: operations-lifecycle-onstart -->
<a id='snippet-operations-lifecycle-onstart'></a>
```cs
[Factory]
public partial class LifecycleOnStartExample : IFactoryOnStart
{
    public Guid Id { get; private set; }
    public bool OnStartCalled { get; private set; }
    public FactoryOperation? LastOperation { get; private set; }

    [Create]
    public LifecycleOnStartExample() { Id = Guid.NewGuid(); }

    public void FactoryStart(FactoryOperation factoryOperation)
    {
        OnStartCalled = true;
        LastOperation = factoryOperation;

        // Validate or prepare before operation executes
        if (factoryOperation == FactoryOperation.Delete && Id == Guid.Empty)
            throw new InvalidOperationException("Cannot delete: Id is not set");
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L444-L465' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-onstart' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnComplete / IFactoryOnCompleteAsync

Called after successful operation. Use `IFactoryOnCompleteAsync` for async post-processing:

<!-- snippet: operations-lifecycle-oncomplete -->
<a id='snippet-operations-lifecycle-oncomplete'></a>
```cs
[Factory]
public partial class LifecycleOnCompleteExample : IFactoryOnComplete
{
    public Guid Id { get; private set; }
    public bool OnCompleteCalled { get; private set; }
    public FactoryOperation? CompletedOperation { get; private set; }

    [Create]
    public LifecycleOnCompleteExample() { Id = Guid.NewGuid(); }

    public void FactoryComplete(FactoryOperation factoryOperation)
    {
        OnCompleteCalled = true;
        CompletedOperation = factoryOperation;

        // Post-operation logic: logging, notifications, etc.
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L467-L486' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-oncomplete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnCancelled / IFactoryOnCancelledAsync

Called when operation is cancelled via OperationCanceledException. Use `IFactoryOnCancelledAsync` for async cleanup:

<!-- snippet: operations-lifecycle-oncancelled -->
<a id='snippet-operations-lifecycle-oncancelled'></a>
```cs
[Factory]
public partial class LifecycleOnCancelledExample : IFactoryOnCancelled
{
    public Guid Id { get; private set; }
    public bool OnCancelledCalled { get; private set; }
    public FactoryOperation? CancelledOperation { get; private set; }

    [Create]
    public LifecycleOnCancelledExample() { Id = Guid.NewGuid(); }

    public void FactoryCancelled(FactoryOperation factoryOperation)
    {
        OnCancelledCalled = true;
        CancelledOperation = factoryOperation;

        // Cleanup logic when operation was cancelled
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L488-L507' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-oncancelled' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Lifecycle execution order:
1. `FactoryStart()` or `FactoryStartAsync()`
2. Operation method executes
3. `FactoryComplete()` or `FactoryCompleteAsync()` (if successful)
4. `FactoryCancelled()` or `FactoryCancelledAsync()` (if cancelled)

## CancellationToken Support

All factory methods accept an optional CancellationToken:

<!-- snippet: operations-cancellation -->
<a id='snippet-operations-cancellation'></a>
```cs
[Factory]
public partial class CancellationTokenExample
{
    public Guid Id { get; private set; }
    public bool Completed { get; private set; }

    [Create]
    public CancellationTokenExample() { Id = Guid.NewGuid(); }

    [Remote]
    [Fetch]
    public async Task Fetch(
        Guid id,
        [Service] IPersonRepository repository,
        CancellationToken cancellationToken)
    {
        // Check cancellation before starting
        cancellationToken.ThrowIfCancellationRequested();

        // Pass token to async operations
        var entity = await repository.GetByIdAsync(id, cancellationToken);

        // Check cancellation during processing
        if (cancellationToken.IsCancellationRequested)
            return;

        Id = id;
        Completed = true;
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L509-L540' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Generated factory methods automatically include CancellationToken:
```csharp
public interface ICancellationTokenExampleFactory
{
    CancellationTokenExample Create(CancellationToken cancellationToken = default);
    Task<CancellationTokenExample?> Fetch(Guid id, CancellationToken cancellationToken = default);
}
```

CancellationToken is:
- Automatically passed to async operation methods
- Linked to HttpContext.RequestAborted on server
- Linked to ApplicationStopping for graceful shutdown
- Triggers IFactoryOnCancelled when fired

## Method Parameters

Factory methods support:

**Value parameters**: Serialized and sent to server
<!-- snippet: operations-params-value -->
<a id='snippet-operations-params-value'></a>
```cs
[Factory]
public partial class ValueParametersExample
{
    public int Count { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime Timestamp { get; private set; }
    public decimal Amount { get; private set; }

    [Create]
    public ValueParametersExample() { }

    [Remote]
    [Fetch]
    public Task Fetch(int count, string name, DateTime timestamp, decimal amount)
    {
        Count = count;
        Name = name;
        Timestamp = timestamp;
        Amount = amount;
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L542-L565' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-value' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Service parameters**: Injected from DI, not serialized
<!-- snippet: operations-params-service -->
<a id='snippet-operations-params-service'></a>
```cs
[Factory]
public partial class ServiceParametersExample
{
    public bool ServicesInjected { get; private set; }

    [Create]
    public ServiceParametersExample() { }

    [Remote]
    [Fetch]
    public Task Fetch(
        Guid id,
        [Service] IPersonRepository personRepository,
        [Service] IOrderRepository orderRepository,
        [Service] IUserContext userContext)
    {
        // Services are resolved from DI container on server
        ServicesInjected = personRepository != null
            && orderRepository != null
            && userContext != null;
        return Task.CompletedTask;
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L567-L591' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-service' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Params arrays**: Variable-length arguments
<!-- snippet: operations-params-array -->
<a id='snippet-operations-params-array'></a>
```cs
// Array parameters with [Execute] - use static partial class
public record BatchProcessResult(Guid[] ProcessedIds, List<string> ProcessedNames);

[SuppressFactory] // Nested in wrapper class - pattern demonstration only
public static partial class ArrayParametersExample
{
    [Remote]
    [Execute]
    private static Task<BatchProcessResult> _ProcessBatch(Guid[] ids, List<string> names)
    {
        return Task.FromResult(new BatchProcessResult(ids, names));
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L593-L607' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-array' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**CancellationToken**: Optional, always last parameter
<!-- snippet: operations-params-cancellation -->
<a id='snippet-operations-params-cancellation'></a>
```cs
[Factory]
public partial class OptionalCancellationExample
{
    public bool Completed { get; private set; }

    [Create]
    public OptionalCancellationExample() { }

    [Remote]
    [Fetch]
    public async Task Fetch(
        Guid id,
        [Service] IPersonRepository repository,
        CancellationToken cancellationToken = default)
    {
        // CancellationToken is optional - receives default if not provided by caller
        await repository.GetByIdAsync(id, cancellationToken);
        Completed = true;
    }
}
```
<sup><a href='/src/docs/samples/FactoryOperationsSamples.cs#L609-L630' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-params-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Parameter order rules:
1. Value parameters (required first, optional last)
2. Service parameters (any order among services)
3. CancellationToken (always last)

## Next Steps

- [Service Injection](service-injection.md) - Inject dependencies into factory methods
- [Authorization](authorization.md) - Secure factory operations
- [Save Operation](save-operation.md) - IFactorySave routing
- [Events](events.md) - Deep dive into event handling
