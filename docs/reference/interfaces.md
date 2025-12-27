---
layout: default
title: "Interfaces Reference"
description: "Complete reference for all RemoteFactory interfaces"
parent: Reference
nav_order: 2
---

# Interfaces Reference

This document provides a complete reference for all interfaces in the RemoteFactory framework.

## Core Interfaces

### IFactorySaveMeta

Provides state information for Save operations. Domain models that support Insert, Update, and Delete must implement this interface.

```csharp
public interface IFactorySaveMeta
{
    bool IsDeleted { get; }
    bool IsNew { get; }
}
```

**Properties:**

| Property | Type | Description |
|----------|------|-------------|
| `IsNew` | `bool` | `true` if the object hasn't been saved yet |
| `IsDeleted` | `bool` | `true` if the object should be deleted on save |

**Usage:**

```csharp
[Factory]
public class PersonModel : IPersonModel, IFactorySaveMeta
{
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // Other properties...

    [Insert]
    public void Insert([Service] IContext context)
    {
        // Called when IsNew == true && IsDeleted == false
        IsNew = false;
    }

    [Update]
    public void Update([Service] IContext context)
    {
        // Called when IsNew == false && IsDeleted == false
    }

    [Delete]
    public void Delete([Service] IContext context)
    {
        // Called when IsDeleted == true && IsNew == false
    }
}
```

**Save Logic:**

```csharp
// Generated Save method logic
if (target.IsDeleted)
{
    if (target.IsNew)
        return null;  // Nothing to delete
    return LocalDelete(target);
}
else if (target.IsNew)
{
    return LocalInsert(target);
}
else
{
    return LocalUpdate(target);
}
```

---

### IFactorySave<T>

Interface for factory classes that support Save operations. Generated factories implement this when the domain model has Insert, Update, or Delete methods.

```csharp
public interface IFactorySave<T> where T : IFactorySaveMeta
{
    Task<IFactorySaveMeta?> Save(T entity);
}
```

**Purpose:**
- Enables saving through a generic interface
- Used internally for factory orchestration
- Can be used for generic save handling

**Usage:**

```csharp
// Get a generic save interface
var factory = serviceProvider.GetRequiredService<IFactorySave<PersonModel>>();
var result = await factory.Save(personModel);
```

---

## Lifecycle Interfaces

### IFactoryOnStart

Called before a factory operation executes. Implement this for pre-operation logic.

```csharp
public interface IFactoryOnStart
{
    void FactoryStart(FactoryOperation factoryOperation);
}
```

**Usage:**

```csharp
[Factory]
public class OrderModel : IOrderModel, IFactoryOnStart
{
    public void FactoryStart(FactoryOperation operation)
    {
        Console.WriteLine($"Starting operation: {operation}");

        // Initialize state before operation
        if (operation == FactoryOperation.Create)
        {
            OrderNumber = GenerateOrderNumber();
        }
    }

    // Other implementation...
}
```

---

### IFactoryOnStartAsync

Async version of IFactoryOnStart for operations that need async initialization.

```csharp
public interface IFactoryOnStartAsync
{
    Task FactoryStartAsync(FactoryOperation factoryOperation);
}
```

**Usage:**

```csharp
[Factory]
public class OrderModel : IOrderModel, IFactoryOnStartAsync
{
    private readonly IOrderContext _context;

    public async Task FactoryStartAsync(FactoryOperation operation)
    {
        if (operation == FactoryOperation.Update)
        {
            // Load related data before update
            await LoadRelatedDataAsync();
        }
    }
}
```

---

### IFactoryOnComplete

Called after a factory operation completes successfully. Implement this for post-operation logic.

```csharp
public interface IFactoryOnComplete
{
    void FactoryComplete(FactoryOperation factoryOperation);
}
```

**Usage:**

```csharp
[Factory]
public class AuditedModel : IAuditedModel, IFactoryOnComplete
{
    public DateTime? LastAccessedAt { get; set; }

    public void FactoryComplete(FactoryOperation operation)
    {
        // Track last access
        if (operation == FactoryOperation.Fetch)
        {
            LastAccessedAt = DateTime.UtcNow;
        }

        // Log completed operations
        Console.WriteLine($"Completed: {operation}");
    }
}
```

---

### IFactoryOnCompleteAsync

Async version of IFactoryOnComplete for async cleanup or finalization.

```csharp
public interface IFactoryOnCompleteAsync
{
    Task FactoryCompleteAsync(FactoryOperation factoryOperation);
}
```

**Usage:**

```csharp
[Factory]
public class OrderModel : IOrderModel, IFactoryOnCompleteAsync
{
    public async Task FactoryCompleteAsync(FactoryOperation operation)
    {
        if (operation == FactoryOperation.Insert)
        {
            // Send notification after order created
            await NotifyOrderCreatedAsync();
        }

        if (operation == FactoryOperation.Delete)
        {
            // Cleanup related resources
            await CleanupResourcesAsync();
        }
    }
}
```

**Execution Order:**

1. `IFactoryOnStart.FactoryStart()` (sync)
2. `IFactoryOnStartAsync.FactoryStartAsync()` (async)
3. **Factory operation executes**
4. `IFactoryOnComplete.FactoryComplete()` (sync)
5. `IFactoryOnCompleteAsync.FactoryCompleteAsync()` (async)

---

## Authorization Interfaces

### IAspAuthorize

Server-side ASP.NET Core authorization integration. Implemented by `AspAuthorize` in the AspNetCore package.

```csharp
public interface IAspAuthorize
{
    Task<string?> Authorize(IEnumerable<AspAuthorizeData> authorizeData, bool forbid = false);
}
```

**Parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `authorizeData` | `IEnumerable<AspAuthorizeData>` | Authorization requirements |
| `forbid` | `bool` | If true, throws on failure instead of returning message |

**Return Value:**
- Empty string or null: Authorized
- Non-empty string: Denial message

**Usage (internal to generated code):**

```csharp
// Generated code calls this for [AspAuthorize] methods
var aspAuthorize = ServiceProvider.GetRequiredService<IAspAuthorize>();
var message = await aspAuthorize.Authorize(authorizeDataList, forbid: false);
if (!string.IsNullOrEmpty(message))
{
    return new Authorized<IPersonModel>(new Authorized(message));
}
```

---

## Internal Interfaces

### IFactoryCore<T>

Wrapper interface for factory operation execution. Allows customization of factory behavior without inheritance.

```csharp
public interface IFactoryCore<T>
{
    T DoFactoryMethodCall(FactoryOperation operation, Func<T> factoryMethodCall);
    Task<T> DoFactoryMethodCallAsync(FactoryOperation operation, Func<Task<T>> factoryMethodCall);
    Task<T?> DoFactoryMethodCallAsyncNullable(FactoryOperation operation, Func<Task<T?>> factoryMethodCall);
    T DoFactoryMethodCall(T target, FactoryOperation operation, Action factoryMethodCall);
    T? DoFactoryMethodCallBool(T target, FactoryOperation operation, Func<bool> factoryMethodCall);
    Task<T> DoFactoryMethodCallAsync(T target, FactoryOperation operation, Func<Task> factoryMethodCall);
    Task<T?> DoFactoryMethodCallBoolAsync(T target, FactoryOperation operation, Func<Task<bool>> factoryMethodCall);
}
```

**Purpose:**
- Wraps factory method execution
- Invokes lifecycle interfaces (IFactoryOnStart, IFactoryOnComplete)
- Enables cross-cutting concerns

**Default Implementation:**

```csharp
public class FactoryCore<T> : IFactoryCore<T>
{
    public virtual T DoFactoryMethodCall(FactoryOperation operation, Func<T> factoryMethodCall)
    {
        var target = factoryMethodCall();

        if (target is IFactoryOnComplete onComplete)
        {
            onComplete.FactoryComplete(operation);
        }

        return target;
    }

    // Other methods...
}
```

**Custom Implementation:**

```csharp
public class AuditingFactoryCore<T> : FactoryCore<T>
{
    private readonly IAuditService _audit;

    public AuditingFactoryCore(IAuditService audit)
    {
        _audit = audit;
    }

    public override T DoFactoryMethodCall(FactoryOperation operation, Func<T> factoryMethodCall)
    {
        _audit.LogStart(operation, typeof(T).Name);

        var result = base.DoFactoryMethodCall(operation, factoryMethodCall);

        _audit.LogComplete(operation, typeof(T).Name);

        return result;
    }
}

// Register custom implementation
services.AddSingleton(typeof(IFactoryCore<>), typeof(AuditingFactoryCore<>));
```

---

### IMakeRemoteDelegateRequest

Makes remote delegate calls from clients. Registered in Remote mode.

```csharp
// Internal interface - not typically used directly
internal interface IMakeRemoteDelegateRequest
{
    Task<T?> ForDelegate<T>(Type delegateType, object?[] parameters);
}
```

**Purpose:**
- Serializes parameters to JSON
- POSTs to `/api/neatoo` endpoint
- Deserializes response
- Used by generated Remote* methods

---

### INeatooJsonSerializer

JSON serialization for RemoteFactory. Handles type preservation and reference handling.

```csharp
public interface INeatooJsonSerializer
{
    string Serialize(object? value);
    T? Deserialize<T>(string json);
    object? Deserialize(string json, Type type);
}
```

**Default Implementation:** `NeatooJsonSerializer`

**Features:**
- Reference preservation (handles circular references)
- Interface type handling
- Custom converters for RemoteFactory types

---

## Summary Table

| Interface | Purpose | Implemented By |
|-----------|---------|----------------|
| `IFactorySaveMeta` | Track IsNew/IsDeleted state | Domain models |
| `IFactorySave<T>` | Generic save capability | Generated factories |
| `IFactoryOnStart` | Pre-operation hook (sync) | Domain models |
| `IFactoryOnStartAsync` | Pre-operation hook (async) | Domain models |
| `IFactoryOnComplete` | Post-operation hook (sync) | Domain models |
| `IFactoryOnCompleteAsync` | Post-operation hook (async) | Domain models |
| `IAspAuthorize` | ASP.NET Core authorization | `AspAuthorize` |
| `IFactoryCore<T>` | Factory execution wrapper | `FactoryCore<T>` |

## Next Steps

- **[Factory Modes](factory-modes.md)**: NeatooFactory enum reference
- **[Generated Code](generated-code.md)**: Understanding factory structure
- **[Attributes Reference](attributes.md)**: Complete attribute documentation
