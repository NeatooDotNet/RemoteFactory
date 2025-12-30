---
layout: default
title: "Factory Lifecycle"
description: "IFactoryOnStart and IFactoryOnComplete lifecycle hooks"
parent: Advanced
nav_order: 1
---

# Factory Lifecycle

RemoteFactory provides lifecycle hooks that let you execute code before and after factory operations. These are useful for cross-cutting concerns like logging, auditing, state initialization, and cleanup.

## Lifecycle Interfaces

### IFactoryOnStart

Called before a factory operation executes:

```csharp
public interface IFactoryOnStart
{
    void FactoryStart(FactoryOperation factoryOperation);
}
```

### IFactoryOnStartAsync

Async version for operations requiring async initialization:

```csharp
public interface IFactoryOnStartAsync
{
    Task FactoryStartAsync(FactoryOperation factoryOperation);
}
```

### IFactoryOnComplete

Called after a factory operation completes successfully:

```csharp
public interface IFactoryOnComplete
{
    void FactoryComplete(FactoryOperation factoryOperation);
}
```

### IFactoryOnCompleteAsync

Async version for async cleanup:

```csharp
public interface IFactoryOnCompleteAsync
{
    Task FactoryCompleteAsync(FactoryOperation factoryOperation);
}
```

## Execution Order

When a factory operation runs, lifecycle methods are called in this order:

```
1. IFactoryOnStart.FactoryStart()          (sync)
2. IFactoryOnStartAsync.FactoryStartAsync() (async)
3. >>> Factory operation executes <<<
4. IFactoryOnComplete.FactoryComplete()     (sync)
5. IFactoryOnCompleteAsync.FactoryCompleteAsync() (async)
```

## The FactoryOperation Enum

The lifecycle methods receive a `FactoryOperation` parameter indicating which operation is running:

```csharp
public enum FactoryOperation
{
    None = 0,
    Execute = AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Execute,
    Create = AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Read,
    Fetch = AuthorizeFactoryOperation.Fetch | AuthorizeFactoryOperation.Read,
    Insert = AuthorizeFactoryOperation.Insert | AuthorizeFactoryOperation.Write,
    Update = AuthorizeFactoryOperation.Update | AuthorizeFactoryOperation.Write,
    Delete = AuthorizeFactoryOperation.Delete | AuthorizeFactoryOperation.Write
}
```

Each `FactoryOperation` value is a composite of `AuthorizeFactoryOperation` flags:

| Operation | Includes | Authorization Check |
|-----------|----------|---------------------|
| `Create` | Create + Read | Read operations |
| `Fetch` | Fetch + Read | Read operations |
| `Execute` | Execute + Read | Read operations |
| `Insert` | Insert + Write | Write operations |
| `Update` | Update + Write | Write operations |
| `Delete` | Delete + Write | Write operations |

This composition enables authorization methods marked with `AuthorizeFactoryOperation.Read` to cover Create, Fetch, and Execute operations, while `AuthorizeFactoryOperation.Write` covers Insert, Update, and Delete.

## Basic Usage

### Logging Operations

```csharp
[Factory]
public class OrderModel : IOrderModel, IFactoryOnStart, IFactoryOnComplete
{
    public int Id { get; set; }
    public string Status { get; set; }

    public void FactoryStart(FactoryOperation operation)
    {
        Console.WriteLine($"Starting {operation} for Order {Id}");
    }

    public void FactoryComplete(FactoryOperation operation)
    {
        Console.WriteLine($"Completed {operation} for Order {Id}");
    }

    [Create]
    public OrderModel() { }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IOrderContext ctx)
    {
        // FactoryStart called before this
        var entity = await ctx.Orders.FindAsync(id);
        if (entity == null) return false;
        Id = entity.Id;
        Status = entity.Status;
        return true;
        // FactoryComplete called after this (if successful)
    }
}
```

### State Initialization

```csharp
[Factory]
public class AuditedModel : IAuditedModel, IFactoryOnStart
{
    public DateTime LastAccessed { get; set; }
    public string AccessedBy { get; set; }

    private readonly ICurrentUser _user;

    public AuditedModel(ICurrentUser user)
    {
        _user = user;
    }

    public void FactoryStart(FactoryOperation operation)
    {
        // Set audit fields before any operation
        LastAccessed = DateTime.UtcNow;
        AccessedBy = _user.Id;
    }
}
```

### Async Initialization

```csharp
[Factory]
public class ProductModel : IProductModel, IFactoryOnStartAsync
{
    public decimal Price { get; set; }
    public decimal DiscountedPrice { get; set; }

    private IPricingService _pricingService;

    public async Task FactoryStartAsync(FactoryOperation operation)
    {
        if (operation == FactoryOperation.Fetch)
        {
            // Load dynamic pricing before fetch completes
            DiscountedPrice = await _pricingService.GetCurrentPrice(Id);
        }
    }
}
```

### Post-Operation Notifications

```csharp
[Factory]
public class OrderModel : IOrderModel, IFactoryOnCompleteAsync
{
    public int Id { get; set; }
    public string CustomerEmail { get; set; }

    public async Task FactoryCompleteAsync(FactoryOperation operation)
    {
        if (operation == FactoryOperation.Insert)
        {
            // Send order confirmation after successful insert
            await SendOrderConfirmationEmail();
        }

        if (operation == FactoryOperation.Delete)
        {
            // Notify customer of cancellation
            await SendCancellationEmail();
        }
    }

    private async Task SendOrderConfirmationEmail()
    {
        // Email logic
    }

    private async Task SendCancellationEmail()
    {
        // Email logic
    }
}
```

## Operation-Specific Handling

Handle different operations differently:

```csharp
[Factory]
public class DocumentModel : IDocumentModel, IFactoryOnStart, IFactoryOnComplete
{
    public int Id { get; set; }
    public string Content { get; set; }
    public bool IsLocked { get; set; }

    public void FactoryStart(FactoryOperation operation)
    {
        switch (operation)
        {
            case FactoryOperation.Create:
                // Initialize new document
                Content = string.Empty;
                IsLocked = false;
                break;

            case FactoryOperation.Update:
                // Check if document can be modified
                if (IsLocked)
                {
                    throw new InvalidOperationException("Document is locked");
                }
                break;

            case FactoryOperation.Delete:
                // Log deletion attempt
                LogDeletionAttempt();
                break;
        }
    }

    public void FactoryComplete(FactoryOperation operation)
    {
        switch (operation)
        {
            case FactoryOperation.Insert:
                // Initialize version history
                CreateInitialVersion();
                break;

            case FactoryOperation.Update:
                // Create new version
                CreateNewVersion();
                break;

            case FactoryOperation.Fetch:
                // Track access
                RecordAccess();
                break;
        }
    }
}
```

## Combining Multiple Interfaces

You can implement all four interfaces for complete control:

```csharp
[Factory]
public class FullLifecycleModel :
    IFullLifecycleModel,
    IFactoryOnStart,
    IFactoryOnStartAsync,
    IFactoryOnComplete,
    IFactoryOnCompleteAsync
{
    private Stopwatch _timer;

    public void FactoryStart(FactoryOperation operation)
    {
        _timer = Stopwatch.StartNew();
        Console.WriteLine($"[SYNC START] {operation}");
    }

    public async Task FactoryStartAsync(FactoryOperation operation)
    {
        Console.WriteLine($"[ASYNC START] {operation}");
        await PrepareAsync();
    }

    public void FactoryComplete(FactoryOperation operation)
    {
        _timer.Stop();
        Console.WriteLine($"[SYNC COMPLETE] {operation} in {_timer.ElapsedMilliseconds}ms");
    }

    public async Task FactoryCompleteAsync(FactoryOperation operation)
    {
        Console.WriteLine($"[ASYNC COMPLETE] {operation}");
        await CleanupAsync();
    }
}
```

## Custom FactoryCore for Cross-Cutting Concerns

For concerns that apply to all domain models, you can create a custom `IFactoryCore<T>`:

```csharp
public class LoggingFactoryCore<T> : FactoryCore<T>
{
    private readonly ILogger<T> _logger;

    public LoggingFactoryCore(ILogger<T> logger)
    {
        _logger = logger;
    }

    public override T DoFactoryMethodCall(FactoryOperation operation, Func<T> factoryMethodCall)
    {
        _logger.LogDebug("Starting {Operation} for {Type}", operation, typeof(T).Name);

        try
        {
            var result = base.DoFactoryMethodCall(operation, factoryMethodCall);
            _logger.LogDebug("Completed {Operation} for {Type}", operation, typeof(T).Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed {Operation} for {Type}", operation, typeof(T).Name);
            throw;
        }
    }

    public override async Task<T> DoFactoryMethodCallAsync(FactoryOperation operation, Func<Task<T>> factoryMethodCall)
    {
        _logger.LogDebug("Starting async {Operation} for {Type}", operation, typeof(T).Name);

        try
        {
            var result = await base.DoFactoryMethodCallAsync(operation, factoryMethodCall);
            _logger.LogDebug("Completed async {Operation} for {Type}", operation, typeof(T).Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed async {Operation} for {Type}", operation, typeof(T).Name);
            throw;
        }
    }
}

// Register in DI
services.AddSingleton(typeof(IFactoryCore<>), typeof(LoggingFactoryCore<>));
```

## Use Cases

### Audit Trail

```csharp
[Factory]
public class AuditedEntity : IFactoryOnComplete
{
    public int Id { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime ModifiedAt { get; set; }
    public List<AuditEntry> AuditLog { get; set; }

    public void FactoryComplete(FactoryOperation operation)
    {
        AuditLog.Add(new AuditEntry
        {
            Operation = operation.ToString(),
            Timestamp = DateTime.UtcNow,
            User = ModifiedBy,
            EntityId = Id
        });
    }
}
```

### Cache Invalidation

```csharp
[Factory]
public class CachedModel : IFactoryOnCompleteAsync
{
    private readonly ICacheService _cache;

    public int Id { get; set; }
    public string Data { get; set; }

    public async Task FactoryCompleteAsync(FactoryOperation operation)
    {
        if (operation is FactoryOperation.Update or FactoryOperation.Delete)
        {
            await _cache.InvalidateAsync($"model:{Id}");
            await _cache.InvalidateAsync("model:list");
        }
    }
}
```

### Validation Before Save

```csharp
[Factory]
public class ValidatedModel : IFactoryOnStart
{
    public string Email { get; set; }
    public int Age { get; set; }

    public void FactoryStart(FactoryOperation operation)
    {
        if (operation is FactoryOperation.Insert or FactoryOperation.Update)
        {
            if (string.IsNullOrEmpty(Email))
                throw new ValidationException("Email is required");

            if (Age < 0 || Age > 150)
                throw new ValidationException("Invalid age");
        }
    }
}
```

## Error Handling

If a lifecycle method throws an exception:

- **OnStart exceptions**: The operation does not execute
- **OnComplete exceptions**: The operation has already executed, but the exception propagates

```csharp
public void FactoryStart(FactoryOperation operation)
{
    if (operation == FactoryOperation.Update && IsLocked)
    {
        // Operation won't run - exception thrown before
        throw new InvalidOperationException("Cannot update locked record");
    }
}

public void FactoryComplete(FactoryOperation operation)
{
    // Be careful here - operation already completed
    // Any exception means the save succeeded but post-processing failed
}
```

## Best Practices

### Keep Lifecycle Methods Fast

```csharp
// Good: Quick synchronous work
public void FactoryStart(FactoryOperation operation)
{
    LastAccessed = DateTime.UtcNow;
}

// Bad: Slow work in sync method
public void FactoryStart(FactoryOperation operation)
{
    Thread.Sleep(1000);  // Don't do this
}
```

### Use Async for I/O

```csharp
// Good: Async for I/O operations
public async Task FactoryCompleteAsync(FactoryOperation operation)
{
    await _emailService.SendAsync(notification);
}

// Bad: Blocking in sync method
public void FactoryComplete(FactoryOperation operation)
{
    _emailService.SendAsync(notification).Wait();  // Don't do this
}
```

### Handle Failures Gracefully

```csharp
public async Task FactoryCompleteAsync(FactoryOperation operation)
{
    try
    {
        await _notificationService.NotifyAsync(Id);
    }
    catch (Exception ex)
    {
        // Log but don't fail the operation
        _logger.LogWarning(ex, "Failed to send notification");
    }
}
```

## Next Steps

- **[Interface Factories](interface-factories.md)**: Using [Factory] on interfaces
- **[Extending FactoryCore](extending-factory-core.md)**: Custom factory behavior
- **[Interfaces Reference](../reference/interfaces.md)**: Complete interface documentation
