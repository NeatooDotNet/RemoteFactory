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
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate implementing IFactoryOnStart
- FactoryStart method validates business rules before operation executes
- For Delete operation, throw if EmployeeId is empty (cannot delete unsaved employee)
- Track StartedOperation and StartTime properties for audit purposes
- Include [Create] constructor and [Remote, Fetch] method to show full lifecycle context
- Domain layer code - this is an aggregate root with factory-generated lifecycle hooks
-->
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
<!--
SNIPPET REQUIREMENTS:
- Department aggregate implementing IFactoryOnStartAsync
- FactoryStartAsync validates department budget limits via async database query before operation
- Use injected IDepartmentRepository to check existing department count or budget constraints
- Track PreConditionsValidated property to confirm validation occurred
- Include [Create] constructor and [Remote, Fetch] method
- Domain layer code - demonstrates async pre-operation validation with database access
-->
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
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate implementing IFactoryOnComplete
- FactoryComplete method tracks successful operation for audit logging
- Store CompletedOperation and CompleteTime properties
- Show comment indicating where post-operation logic goes (audit logging, cache invalidation)
- Include [Create] constructor and [Remote, Fetch] method
- Domain layer code - demonstrates post-operation hook for audit/logging purposes
-->
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
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate implementing IFactoryOnCompleteAsync
- FactoryCompleteAsync sends notification via injected INotificationService after successful operation
- Use async notification call (e.g., await _notificationService.SendAsync(...))
- Track PostProcessingComplete property to confirm notification was sent
- Include [Create] constructor and [Remote, Fetch] method
- Domain layer code - demonstrates async post-operation hook for notifications/external services
-->
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
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate implementing IFactoryOnCancelled
- FactoryCancelled method handles cleanup when operation is cancelled
- Track CancelledOperation property for logging which operation was cancelled
- Set CleanupPerformed flag to indicate cleanup logic executed
- Include [Create] constructor and [Remote, Fetch] with CancellationToken parameter
- Fetch method should use ct.ThrowIfCancellationRequested() to demonstrate cancellation point
- Domain layer code - demonstrates cancellation handling and cleanup logic
-->
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
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate implementing IFactoryOnCancelledAsync
- FactoryCancelledAsync performs async cleanup via injected IUnitOfWork to rollback partial changes
- Use await _unitOfWork.RollbackAsync() or similar async cleanup operation
- Track AsyncCleanupComplete property to confirm cleanup completed
- Include [Create] constructor and [Remote, Fetch] with CancellationToken parameter
- Fetch method demonstrates cancellable async operation
- Domain layer code - demonstrates async cancellation cleanup with database rollback
-->
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
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate implementing IFactoryOnStart, IFactoryOnComplete, AND IFactoryOnCancelled
- Track LifecycleEvents as List<string> to show execution order
- FactoryStart adds "Start: {operation}" to list (step 1)
- Fetch method adds "Operation: Fetch" to list (step 2)
- FactoryComplete adds "Complete: {operation}" to list (step 3a on success)
- FactoryCancelled adds "Cancelled: {operation}" to list (step 3b on cancellation)
- Include numbered comments showing execution order: 1. Start, 2. Operation, 3a. Complete OR 3b. Cancelled
- Domain layer code - demonstrates full lifecycle hook ordering with all three interfaces combined
-->
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
<!--
SNIPPET REQUIREMENTS:
- Employee aggregate implementing IFactorySaveMeta
- Properties: EmployeeId (Guid), Name (string), IsNew (bool, default true), IsDeleted (bool)
- [Create] constructor sets EmployeeId = Guid.NewGuid(), IsNew = true (comment: Save will call Insert)
- [Remote, Fetch] sets IsNew = false after loading (comment: Save will call Update)
- [Remote, Insert] sets IsNew = false after successful insert
- [Remote, Update] persists changes (no state change needed)
- [Remote, Delete] removes the entity
- Include trailing comment block showing Save routing logic:
  - IsNew=true, IsDeleted=false -> Insert
  - IsNew=false, IsDeleted=false -> Update
  - IsNew=false, IsDeleted=true -> Delete
  - IsNew=true, IsDeleted=true -> No operation
- Domain layer code - demonstrates IFactorySaveMeta for Save operation routing
-->
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
<!--
SNIPPET REQUIREMENTS:
- Application layer service or test class demonstrating IFactorySave<Employee> usage
- Get IEmployeeFactory from DI container (use scopes pattern from test containers)
- Create new employee: var employee = factory.Create(); employee.Name = "John Smith";
- First Save (Insert): var saved = await factory.Save(employee); - comment: IsNew=true -> Insert
- Assert saved is not null and IsNew is false after save
- Second Save (Update): saved.Name = "Jane Smith"; await factory.Save(saved); - comment: IsNew=false -> Update
- Third Save (Delete): saved.IsDeleted = true; await factory.Save(saved); - comment: IsDeleted=true -> Delete
- Application layer code - demonstrates consuming IFactorySave<T> from generated factory
-->
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
<!--
SNIPPET REQUIREMENTS:
- Custom IAspAuthorize implementation for simplified authorization
- Inject IUserContext (custom interface for user identity/roles)
- Authorize method implementation:
  - Check if user is authenticated; if not, return error message or throw AspForbidException if forbid=true
  - Iterate through AspAuthorizeData to check Roles requirements
  - For each role requirement, verify user has at least one required role
  - Return empty string on success, error message on failure
  - Throw AspForbidException if forbid=true and authorization fails
- Include leading comment: "Custom IAspAuthorize for testing or non-ASP.NET Core environments"
- Infrastructure layer code - custom authorization implementation for non-standard scenarios
-->
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
<!--
SNIPPET REQUIREMENTS:
- Value object implementing IOrdinalSerializable for compact JSON serialization
- Use EmployeeSnapshot or similar DTO with 3+ properties
- Properties: DepartmentCode (string), EmployeeCount (int), LastUpdated (DateTime)
- Comment each property with its ordinal index (Index 0, Index 1, Index 2)
- ToOrdinalArray returns properties in alphabetical order: [DepartmentCode, EmployeeCount, LastUpdated]
- Include trailing comment showing JSON comparison:
  - Array format: ["HR", 42, "2024-01-15T10:30:00Z"]
  - Object format: {"DepartmentCode":"HR","EmployeeCount":42,"LastUpdated":"2024-01-15T10:30:00Z"}
- Domain layer code - demonstrates compact array-based serialization for value objects/DTOs
-->
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
<!--
SNIPPET REQUIREMENTS:
- Money value object implementing IOrdinalConverterProvider<Money>
- Properties: Amount (decimal), Currency (string, default "USD")
- Static CreateOrdinalConverter method returns new MoneyOrdinalConverter()
- Include leading comment: "IOrdinalConverterProvider<TSelf> enables types to provide their own ordinal converter"
- MoneyOrdinalConverter class implementing JsonConverter<Money>:
  - Read method: parse StartArray, read Amount as decimal, read Currency as string, return new Money
  - Write method: WriteStartArray, WriteNumberValue(Amount), WriteStringValue(Currency), WriteEndArray
- Domain layer code - demonstrates custom ordinal converter for value objects with special serialization needs
-->
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
<!--
SNIPPET REQUIREMENTS:
- Application layer service or test demonstrating IEventTracker usage for graceful shutdown
- Get IEventTracker from DI container
- Check PendingCount property to see how many fire-and-forget events are in progress
- Call await eventTracker.WaitAllAsync() to wait for all pending events
- Assert PendingCount equals 0 after WaitAllAsync completes
- Include comments explaining: "IEventTracker monitors pending fire-and-forget events"
- Application layer code - demonstrates graceful shutdown by waiting for pending events
-->
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
