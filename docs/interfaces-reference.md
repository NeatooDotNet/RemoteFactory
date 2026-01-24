---
title: Interfaces Reference
nav_order: 11
---

# Interfaces Reference

RemoteFactory provides interfaces for lifecycle hooks, authorization, state management, and save routing. Implement these interfaces on your domain models to integrate with factory-generated code.

## Lifecycle Hooks

### IFactoryOnStart

Called before a factory operation executes.

```csharp
public interface IFactoryOnStart
{
    void FactoryStart(FactoryOperation factoryOperation);
}
```

**When to use:** Pre-operation validation, logging, or setup that doesn't require async work.

Implement this interface on your domain model to receive a callback before any factory operation:

<!-- snippet: interfaces-factoryonstart -->
<a id='snippet-interfaces-factoryonstart'></a>
```cs
[Factory]
public partial class FactoryOnStartExample : IFactoryOnStart
{
    public Guid Id { get; private set; }
    public FactoryOperation? StartedOperation { get; private set; }
    public DateTime StartTime { get; private set; }

    [Create]
    public FactoryOnStartExample() { Id = Guid.NewGuid(); }

    // Called BEFORE the factory operation executes
    public void FactoryStart(FactoryOperation factoryOperation)
    {
        StartedOperation = factoryOperation;
        StartTime = DateTime.UtcNow;

        // Pre-operation logic: validation, setup, logging
        if (factoryOperation == FactoryOperation.Delete && Id == Guid.Empty)
        {
            throw new InvalidOperationException("Cannot delete: entity has no Id");
        }
    }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id)
    {
        Id = id;
        return Task.FromResult(true);
    }
}
```
<sup><a href='/src/docs/samples/InterfacesReferenceSamples.cs#L13-L44' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryonstart' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnStartAsync

Async version of `IFactoryOnStart`.

```csharp
public interface IFactoryOnStartAsync
{
    Task FactoryStartAsync(FactoryOperation factoryOperation);
}
```

**When to use:** Pre-operation work that requires async calls (database queries, external services).

Async pre-operation hook with database access:

<!-- snippet: interfaces-factoryonstart-async -->
<a id='snippet-interfaces-factoryonstart-async'></a>
```cs
[Factory]
public partial class FactoryOnStartAsyncExample : IFactoryOnStartAsync
{
    public Guid Id { get; private set; }
    public bool PreConditionsValidated { get; private set; }

    [Create]
    public FactoryOnStartAsyncExample() { Id = Guid.NewGuid(); }

    // Async version for operations requiring async validation
    public async Task FactoryStartAsync(FactoryOperation factoryOperation)
    {
        // Async pre-operation logic
        await Task.Delay(1); // Simulate async validation

        PreConditionsValidated = true;
    }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id)
    {
        Id = id;
        return Task.FromResult(true);
    }
}
```
<sup><a href='/src/docs/samples/InterfacesReferenceSamples.cs#L46-L72' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryonstart-async' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnComplete

Called after a factory operation completes successfully.

```csharp
public interface IFactoryOnComplete
{
    void FactoryComplete(FactoryOperation factoryOperation);
}
```

**When to use:** Post-operation cleanup, audit logging, or state updates that don't require async work.

Implement this interface to track successful operations:

<!-- snippet: interfaces-factoryoncomplete -->
<a id='snippet-interfaces-factoryoncomplete'></a>
```cs
[Factory]
public partial class FactoryOnCompleteExample : IFactoryOnComplete
{
    public Guid Id { get; private set; }
    public FactoryOperation? CompletedOperation { get; private set; }
    public DateTime CompleteTime { get; private set; }

    [Create]
    public FactoryOnCompleteExample() { Id = Guid.NewGuid(); }

    // Called AFTER the factory operation completes successfully
    public void FactoryComplete(FactoryOperation factoryOperation)
    {
        CompletedOperation = factoryOperation;
        CompleteTime = DateTime.UtcNow;

        // Post-operation logic: logging, notifications, cleanup
    }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id)
    {
        Id = id;
        return Task.FromResult(true);
    }
}
```
<sup><a href='/src/docs/samples/InterfacesReferenceSamples.cs#L74-L101' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncomplete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnCompleteAsync

Async version of `IFactoryOnComplete`.

```csharp
public interface IFactoryOnCompleteAsync
{
    Task FactoryCompleteAsync(FactoryOperation factoryOperation);
}
```

**When to use:** Post-operation work that requires async calls (notifications, external logging).

Async post-operation hook for notifications:

<!-- snippet: interfaces-factoryoncomplete-async -->
<a id='snippet-interfaces-factoryoncomplete-async'></a>
```cs
[Factory]
public partial class FactoryOnCompleteAsyncExample : IFactoryOnCompleteAsync
{
    public Guid Id { get; private set; }
    public bool PostProcessingComplete { get; private set; }

    [Create]
    public FactoryOnCompleteAsyncExample() { Id = Guid.NewGuid(); }

    // Async version for post-operation processing
    public async Task FactoryCompleteAsync(FactoryOperation factoryOperation)
    {
        // Async post-operation logic
        await Task.Delay(1); // Simulate async processing

        PostProcessingComplete = true;
    }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id)
    {
        Id = id;
        return Task.FromResult(true);
    }
}
```
<sup><a href='/src/docs/samples/InterfacesReferenceSamples.cs#L103-L129' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncomplete-async' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnCancelled

Called when a factory operation is cancelled via `CancellationToken`.

```csharp
public interface IFactoryOnCancelled
{
    void FactoryCancelled(FactoryOperation factoryOperation);
}
```

**When to use:** Cleanup after operation cancellation, rollback logic, or cancellation logging.

Handle operation cancellation:

<!-- snippet: interfaces-factoryoncancelled -->
<a id='snippet-interfaces-factoryoncancelled'></a>
```cs
[Factory]
public partial class FactoryOnCancelledExample : IFactoryOnCancelled
{
    public Guid Id { get; private set; }
    public FactoryOperation? CancelledOperation { get; private set; }
    public bool CleanupPerformed { get; private set; }

    [Create]
    public FactoryOnCancelledExample() { Id = Guid.NewGuid(); }

    // Called when an OperationCanceledException occurs
    public void FactoryCancelled(FactoryOperation factoryOperation)
    {
        CancelledOperation = factoryOperation;

        // Cleanup logic for cancelled operations
        CleanupPerformed = true;
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(100, ct);
        Id = id;
        return true;
    }
}
```
<sup><a href='/src/docs/samples/InterfacesReferenceSamples.cs#L131-L160' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncancelled' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnCancelledAsync

Async version of `IFactoryOnCancelled`.

```csharp
public interface IFactoryOnCancelledAsync
{
    Task FactoryCancelledAsync(FactoryOperation factoryOperation);
}
```

**When to use:** Async cleanup after cancellation (database rollback, external API calls).

Async cancellation with database rollback:

<!-- snippet: interfaces-factoryoncancelled-async -->
<a id='snippet-interfaces-factoryoncancelled-async'></a>
```cs
[Factory]
public partial class FactoryOnCancelledAsyncExample : IFactoryOnCancelledAsync
{
    public Guid Id { get; private set; }
    public bool AsyncCleanupComplete { get; private set; }

    [Create]
    public FactoryOnCancelledAsyncExample() { Id = Guid.NewGuid(); }

    // Async version for cleanup requiring async operations
    public async Task FactoryCancelledAsync(FactoryOperation factoryOperation)
    {
        // Async cleanup logic
        await Task.Delay(1); // Simulate async cleanup

        AsyncCleanupComplete = true;
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Delay(100, ct);
        Id = id;
        return true;
    }
}
```
<sup><a href='/src/docs/samples/InterfacesReferenceSamples.cs#L162-L190' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncancelled-async' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Lifecycle Hook Execution Order

When a factory operation executes:

1. `IFactoryOnStart` / `IFactoryOnStartAsync` - Before operation
2. Factory operation method (`[Create]`, `[Fetch]`, `[Update]`, etc.)
3. `IFactoryOnComplete` / `IFactoryOnCompleteAsync` - After successful operation

If cancelled:
- `IFactoryOnCancelled` / `IFactoryOnCancelledAsync` - After `OperationCanceledException`

Combining sync and async hooks:

<!-- snippet: interfaces-lifecycle-order -->
<a id='snippet-interfaces-lifecycle-order'></a>
```cs
[Factory]
public partial class LifecycleOrderExample : IFactoryOnStart, IFactoryOnComplete, IFactoryOnCancelled
{
    public Guid Id { get; private set; }
    public List<string> LifecycleEvents { get; } = new();

    [Create]
    public LifecycleOrderExample() { Id = Guid.NewGuid(); }

    // Execution order:
    // 1. FactoryStart (before operation)
    public void FactoryStart(FactoryOperation factoryOperation)
    {
        LifecycleEvents.Add($"Start: {factoryOperation}");
    }

    // 2. Factory operation executes (Fetch, Insert, etc.)
    [Remote, Fetch]
    public Task<bool> Fetch(Guid id)
    {
        LifecycleEvents.Add("Operation: Fetch");
        Id = id;
        return Task.FromResult(true);
    }

    // 3a. FactoryComplete (on success)
    public void FactoryComplete(FactoryOperation factoryOperation)
    {
        LifecycleEvents.Add($"Complete: {factoryOperation}");
    }

    // 3b. FactoryCancelled (on cancellation - instead of Complete)
    public void FactoryCancelled(FactoryOperation factoryOperation)
    {
        LifecycleEvents.Add($"Cancelled: {factoryOperation}");
    }
}
```
<sup><a href='/src/docs/samples/InterfacesReferenceSamples.cs#L192-L230' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-lifecycle-order' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Save Operation

### IFactorySaveMeta

Provides state properties that the generated factory's `Save` method uses to route to Insert, Update, or Delete.

```csharp
public interface IFactorySaveMeta
{
    bool IsDeleted { get; }
    bool IsNew { get; }
}
```

**Routing logic:**
- `IsNew = true, IsDeleted = false` → Insert
- `IsNew = false, IsDeleted = false` → Update
- `IsNew = false, IsDeleted = true` → Delete
- `IsNew = true, IsDeleted = true` → No operation (new item deleted before save)

Implement this interface on domain models that use the Save pattern:

<!-- snippet: interfaces-factorysavemeta -->
<a id='snippet-interfaces-factorysavemeta'></a>
```cs
[Factory]
public partial class FactorySaveMetaExample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = string.Empty;

    // IFactorySaveMeta implementation
    // These properties control Save routing
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public FactorySaveMetaExample()
    {
        Id = Guid.NewGuid();
        // IsNew = true by default - Save will call Insert
    }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id)
    {
        Id = id;
        IsNew = false; // After fetch, IsNew = false - Save will call Update
        return Task.FromResult(true);
    }

    [Remote, Insert]
    public Task Insert()
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Update]
    public Task Update()
    {
        return Task.CompletedTask;
    }

    [Remote, Delete]
    public Task Delete()
    {
        return Task.CompletedTask;
    }
}

// Save routing based on IFactorySaveMeta:
// IsNew=true,  IsDeleted=false -> Insert
// IsNew=false, IsDeleted=false -> Update
// IsNew=false, IsDeleted=true  -> Delete
// IsNew=true,  IsDeleted=true  -> No operation (new item deleted)
```
<sup><a href='/src/docs/samples/InterfacesReferenceSamples.cs#L232-L284' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factorysavemeta' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See [Save Operation](save-operation.md) for complete usage details.

### IFactorySave&lt;T&gt;

Generated factories implement this interface when the domain model implements `IFactorySaveMeta`.

```csharp
public interface IFactorySave<T> where T : IFactorySaveMeta
{
    Task<IFactorySaveMeta?> Save(T entity, CancellationToken cancellationToken = default);
}
```

**You do not implement this interface.** The generator creates it automatically.

Using the generated Save method:

<!-- snippet: interfaces-factorysave -->
<a id='snippet-interfaces-factorysave'></a>
```cs
public partial class FactorySaveUsageExample
{
    // [Fact]
    public async Task UsingIFactorySave()
    {
        var scopes = SampleTestContainers.Scopes();

        // IFactorySave<T> is implemented by generated factories
        var factory = scopes.local.GetRequiredService<IFactorySaveMetaExampleFactory>();

        // Create new entity
        var entity = factory.Create();
        entity.Name = "Test";

        // Save routes based on IsNew/IsDeleted
        // IsNew=true -> Insert
        var saved = await factory.Save(entity);
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);

        // Modify and save again
        // IsNew=false -> Update
        saved.Name = "Updated";
        var updated = await factory.Save(saved);

        // Mark for deletion and save
        // IsDeleted=true -> Delete
        updated!.IsDeleted = true;
        await factory.Save(updated);
    }
}
```
<sup><a href='/src/docs/samples/InterfacesReferenceSamples.cs#L286-L318' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factorysave' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Authorization

### IAspAuthorize

Performs ASP.NET Core authorization checks on the server. Injected into factory operations decorated with `[AspAuthorize]`.

```csharp
public interface IAspAuthorize
{
    Task<string?> Authorize(
        IEnumerable<AspAuthorizeData> authorizeData,
        bool forbid = false);
}
```

**When to use:** Custom ASP.NET Core authorization implementations that need different policy evaluation logic.

**You rarely implement this interface.** The default implementation (`AspAuthorize`) is registered automatically by `AddNeatooAspNetCore()` and integrates with ASP.NET Core's `IAuthorizationPolicyProvider` and `IPolicyEvaluator`.

**Return value:**
- Empty string if authorized
- Error message string if not authorized
- Throws `AspForbidException` if `forbid = true` and authorization fails

Custom authorization implementation:

<!-- snippet: interfaces-aspauthorize -->
<a id='snippet-interfaces-aspauthorize'></a>
```cs
// Custom IAspAuthorize implementation for simplified authorization scenarios
// (e.g., testing, non-ASP.NET Core environments, or custom authorization logic)
public partial class CustomAspAuthorize : IAspAuthorize
{
    private readonly IUserContext _userContext;

    public CustomAspAuthorize(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public Task<string?> Authorize(
        IEnumerable<AspAuthorizeData> authorizeData,
        bool forbid = false)
    {
        // Check if user is authenticated
        if (!_userContext.IsAuthenticated)
        {
            var message = "User is not authenticated";
            if (forbid)
            {
                throw new AspForbidException(message);
            }
            return Task.FromResult<string?>(message);
        }

        // Check role requirements from authorization data
        foreach (var data in authorizeData)
        {
            if (!string.IsNullOrEmpty(data.Roles))
            {
                var requiredRoles = data.Roles.Split(',', StringSplitOptions.TrimEntries);
                if (!requiredRoles.Any(role => _userContext.IsInRole(role)))
                {
                    var message = $"User lacks required role(s): {data.Roles}";
                    if (forbid)
                    {
                        throw new AspForbidException(message);
                    }
                    return Task.FromResult<string?>(message);
                }
            }
        }

        // Return empty string to indicate success
        return Task.FromResult<string?>(string.Empty);
    }
}
```
<sup><a href='/src/docs/samples/InterfacesReferenceSamples.cs#L320-L367' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-aspauthorize' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See [Authorization](authorization.md) for standard authorization patterns.

## Serialization

### IOrdinalSerializable

Interface for types that support ordinal (positional) JSON serialization. Types implementing this interface are serialized as JSON arrays instead of objects, reducing payload size.

```csharp
public interface IOrdinalSerializable
{
    object?[] ToOrdinalArray();
}
```

**When to use:** Types that need compact array-based serialization instead of the default object-based format. The source generator automatically implements this for types with `[Factory]` attribute.

**Ordinal order:** Properties are serialized alphabetically by name. For inherited types, base class properties come first (alphabetically), followed by derived class properties (alphabetically).

Example of implementing IOrdinalSerializable:

<!-- snippet: interfaces-ordinalserializable -->
<a id='snippet-interfaces-ordinalserializable'></a>
```cs
// IOrdinalSerializable marks types for array-based JSON serialization
public partial class OrdinalSerializableExample : IOrdinalSerializable
{
    public string Alpha { get; set; } = string.Empty;  // Index 0
    public int Beta { get; set; }                       // Index 1
    public DateTime Gamma { get; set; }                 // Index 2

    // Convert to array in alphabetical property order
    public object?[] ToOrdinalArray()
    {
        return [Alpha, Beta, Gamma];
    }
}

// JSON output: ["value", 42, "2024-01-15T10:30:00Z"]
// Instead of: {"Alpha":"value","Beta":42,"Gamma":"2024-01-15T10:30:00Z"}
```
<sup><a href='/src/docs/samples/InterfacesReferenceSamples.cs#L356-L373' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-ordinalserializable' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See [Serialization](serialization.md) for details on ordinal format.

### IOrdinalConverterProvider&lt;TSelf&gt;

Provides a custom `JsonConverter` for ordinal serialization. Uses static abstract interface members for AOT compatibility.

```csharp
public interface IOrdinalConverterProvider<TSelf> where TSelf : class
{
    static abstract JsonConverter<TSelf> CreateOrdinalConverter();
}
```

**When to use:** Types implementing `IOrdinalSerializable` that need a custom converter for compact serialization. The source generator automatically implements this for `[Factory]` types.

**This is a static abstract interface (C# 11+).** The implementing type provides a static factory method for its converter.

Custom ordinal converter for a value object:

<!-- snippet: interfaces-ordinalconverterprovider -->
<a id='snippet-interfaces-ordinalconverterprovider'></a>
```cs
// IOrdinalConverterProvider<TSelf> enables types to provide their own ordinal converter
// This is used by the source generator to create AOT-compatible serialization

// Type implements the interface to provide its own converter
public partial class CustomOrdinalType : IOrdinalConverterProvider<CustomOrdinalType>
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";

    // Static abstract method implementation
    public static JsonConverter<CustomOrdinalType> CreateOrdinalConverter()
    {
        return new CustomOrdinalConverter();
    }
}

public partial class CustomOrdinalConverter : JsonConverter<CustomOrdinalType>
{
    public override CustomOrdinalType? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException();

        reader.Read();
        var amount = reader.GetDecimal();
        reader.Read();
        var currency = reader.GetString() ?? "USD";
        reader.Read();

        return new CustomOrdinalType { Amount = amount, Currency = currency };
    }

    public override void Write(
        Utf8JsonWriter writer,
        CustomOrdinalType value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Amount);
        writer.WriteStringValue(value.Currency);
        writer.WriteEndArray();
    }
}
```
<sup><a href='/src/docs/samples/InterfacesReferenceSamples.cs#L375-L422' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-ordinalconverterprovider' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IOrdinalSerializationMetadata

Provides metadata about ordinal serialization for a specific type. Used by the serializer to reconstruct objects from ordinal arrays.

```csharp
public interface IOrdinalSerializationMetadata
{
    static abstract string[] PropertyNames { get; }
    static abstract Type[] PropertyTypes { get; }
    static abstract object FromOrdinalArray(object?[] values);
}
```

**You do not implement this interface.** The source generator automatically implements it for types with `[Factory]` attribute to enable ordinal deserialization.

**PropertyNames and PropertyTypes:** Arrays in ordinal order (alphabetical by property name, base class properties first).

**FromOrdinalArray:** Creates an instance from an array of property values in ordinal order.

## Event Tracking

### IEventTracker

Tracks pending asynchronous event tasks for fire-and-forget operations. Enables graceful shutdown by waiting for all pending events to complete.

```csharp
public interface IEventTracker
{
    void Track(Task eventTask);
    Task WaitAllAsync(CancellationToken ct = default);
    int PendingCount { get; }
}
```

**When to use:** Application shutdown logic that needs to wait for all pending fire-and-forget events to complete before terminating.

**You rarely interact with this interface directly.** RemoteFactory uses it internally for `[Event]` delegate tracking. The default implementation is registered by `AddNeatooRemoteFactory()`.

Using IEventTracker for graceful shutdown:

<!-- snippet: interfaces-eventtracker -->
<a id='snippet-interfaces-eventtracker'></a>
```cs
public partial class EventTrackerExample
{
    // [Fact]
    public async Task UsingIEventTracker()
    {
        var scopes = SampleTestContainers.Scopes();

        // IEventTracker monitors pending fire-and-forget events
        var eventTracker = scopes.local.GetRequiredService<IEventTracker>();

        // Check number of pending events
        var pendingCount = eventTracker.PendingCount;

        // Wait for all pending events to complete
        await eventTracker.WaitAllAsync();

        // After WaitAllAsync, all tracked events have completed
        Assert.Equal(0, eventTracker.PendingCount);
    }
}
```
<sup><a href='/src/docs/samples/InterfacesReferenceSamples.cs#L424-L445' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-eventtracker' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Factory Core

### IFactoryCore&lt;T&gt;

Low-level factory execution abstraction. Generated factories use this internally for lifecycle hook invocation and operation tracking.

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

**You rarely implement this interface.** The default implementation (`FactoryCore<T>`) handles lifecycle hooks (IFactoryOnStart, IFactoryOnComplete, IFactoryOnCancelled) and logging. You can register a custom implementation for a specific type to add custom factory behavior without inheritance.

## Summary

| Interface | Purpose | Who Implements |
|-----------|---------|----------------|
| `IFactoryOnStart` | Pre-operation sync hook | Domain models |
| `IFactoryOnStartAsync` | Pre-operation async hook | Domain models |
| `IFactoryOnComplete` | Post-operation sync hook | Domain models |
| `IFactoryOnCompleteAsync` | Post-operation async hook | Domain models |
| `IFactoryOnCancelled` | Cancellation sync hook | Domain models |
| `IFactoryOnCancelledAsync` | Cancellation async hook | Domain models |
| `IFactorySaveMeta` | Save routing state | Domain models |
| `IFactorySave<T>` | Save method signature | Generated factories |
| `IAspAuthorize` | ASP.NET Core authorization | Custom auth implementations |
| `IOrdinalSerializable` | Ordinal serialization marker | Domain models, value objects |
| `IOrdinalConverterProvider<TSelf>` | Ordinal converter provider | Source generator (automatic) |
| `IOrdinalSerializationMetadata` | Ordinal deserialization metadata | Source generator (automatic) |
| `IEventTracker` | Fire-and-forget event tracking | Framework (rarely customized) |
| `IFactoryCore<T>` | Factory execution pipeline | Framework (rarely customized) |

## Next Steps

- [Attributes Reference](attributes-reference.md) - All available attributes
- [Factory Operations](factory-operations.md) - CRUD operation details
- [Service Injection](service-injection.md) - DI integration
- [Authorization](authorization.md) - Authorization patterns
