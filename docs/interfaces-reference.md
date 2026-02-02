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
// IFactoryOnStart: Called before factory operation executes
public void FactoryStart(FactoryOperation factoryOperation)
{
    // Pre-operation validation
    if (factoryOperation == FactoryOperation.Delete && EmployeeId == Guid.Empty)
        throw new InvalidOperationException("Cannot delete unsaved employee");
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L35-L43' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryonstart' title='Start of snippet'>anchor</a></sup>
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
// IFactoryOnStartAsync: Async pre-operation hook for database/service calls
public async Task FactoryStartAsync(FactoryOperation factoryOperation)
{
    if (_repository == null) return;

    // Async validation: check department limit before insert
    var existing = await _repository.GetAllAsync();
    if (factoryOperation == FactoryOperation.Insert && existing.Count >= 100)
        throw new InvalidOperationException("Maximum department limit reached");
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L81-L92' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryonstart-async' title='Start of snippet'>anchor</a></sup>
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
// IFactoryOnComplete: Called after factory operation succeeds
public void FactoryComplete(FactoryOperation factoryOperation)
{
    CompletedOperation = factoryOperation;
    CompleteTime = DateTime.UtcNow;
    // Post-operation: audit logging, cache invalidation, etc.
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L134-L142' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncomplete' title='Start of snippet'>anchor</a></sup>
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
// IFactoryOnCompleteAsync: Async post-operation hook
public async Task FactoryCompleteAsync(FactoryOperation factoryOperation)
{
    if (_notificationService != null)
        await _notificationService.SendAsync("admin@company.com",
            $"Operation {factoryOperation} completed for {Name}");
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L179-L187' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncomplete-async' title='Start of snippet'>anchor</a></sup>
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
// IFactoryOnCancelled: Called when operation cancelled via CancellationToken
public void FactoryCancelled(FactoryOperation factoryOperation)
{
    CancelledOperation = factoryOperation;
    CleanupPerformed = true;
    // Cleanup logic for cancelled operation
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L215-L223' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncancelled' title='Start of snippet'>anchor</a></sup>
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
// IFactoryOnCancelledAsync: Async cancellation cleanup
public async Task FactoryCancelledAsync(FactoryOperation factoryOperation)
{
    if (_unitOfWork != null)
        await _unitOfWork.RollbackAsync();  // Rollback partial changes
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L262-L269' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factoryoncancelled-async' title='Start of snippet'>anchor</a></sup>
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
// Lifecycle execution order: Start -> Operation -> Complete (or Cancelled)
[Factory]
public partial class EmployeeWithLifecycleOrder : IFactoryOnStart, IFactoryOnComplete, IFactoryOnCancelled
{
    public List<string> LifecycleEvents { get; } = new();

    public void FactoryStart(FactoryOperation factoryOperation)
        => LifecycleEvents.Add($"Start: {factoryOperation}");
    public void FactoryComplete(FactoryOperation factoryOperation)
        => LifecycleEvents.Add($"Complete: {factoryOperation}");
    public void FactoryCancelled(FactoryOperation factoryOperation)
        => LifecycleEvents.Add($"Cancelled: {factoryOperation}");

    // After Fetch: ["Start: Fetch", "Complete: Fetch"]
    // If cancelled: ["Start: Fetch", "Cancelled: Fetch"]

    public Guid EmployeeId { get; private set; }
    public string Name { get; set; } = "";

    [Create]
    public EmployeeWithLifecycleOrder() => EmployeeId = Guid.NewGuid();

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;
        EmployeeId = entity.Id;
        Name = $"{entity.FirstName} {entity.LastName}";
        return true;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/LifecycleHooksSamples.cs#L272-L305' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-lifecycle-order' title='Start of snippet'>anchor</a></sup>
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
// IFactorySaveMeta: Provides IsNew/IsDeleted for Save routing
[Factory]
public partial class EmployeeSaveDemo : IFactorySaveMeta
{
    public bool IsNew { get; private set; } = true;   // true = Insert, false = Update
    public bool IsDeleted { get; set; }               // true = Delete

    // Routing: IsNew=true -> Insert, IsNew=false -> Update, IsDeleted=true -> Delete

    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    [Create]
    public EmployeeSaveDemo()
    {
        Id = Guid.NewGuid();
        IsNew = true;
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false;  // Fetched = existing
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L132-L201' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factorysavemeta' title='Start of snippet'>anchor</a></sup>
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
// IFactorySave<T>: Generated Save() routes to Insert/Update/Delete based on state
public class SaveLifecycleDemo
{
    public async Task Demo(IEmployeeWithSaveMetaFactory factory)
    {
        var employee = factory.Create();           // IsNew=true
        employee.Name = "John Smith";

        await factory.Save(employee);              // IsNew=true -> Insert
        employee.Name = "Jane Smith";
        await factory.Save(employee);              // IsNew=false -> Update

        employee.IsDeleted = true;
        await factory.Save(employee);              // IsDeleted=true -> Delete
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Application/Samples/Interfaces/FactorySaveSamples.cs#L6-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-factorysave' title='Start of snippet'>anchor</a></sup>
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
// IAspAuthorize: Custom authorization with audit logging
public class AuditingAspAuthorize : IAspAuthorize
{
    private readonly IAspAuthorize _inner;
    private readonly IAuditLogService _auditLog;

    public AuditingAspAuthorize(IAspAuthorize inner, IAuditLogService auditLog)
    {
        _inner = inner;
        _auditLog = auditLog;
    }

    public async Task<string?> Authorize(IEnumerable<AspAuthorizeData> authorizeData, bool forbid = false)
    {
        var policies = string.Join(", ", authorizeData.Select(a => a.Policy ?? a.Roles ?? "Default"));
        await _auditLog.LogAsync("AuthCheck", Guid.Empty, "Auth", $"Policies: {policies}", default);

        var result = await _inner.Authorize(authorizeData, forbid);

        await _auditLog.LogAsync(string.IsNullOrEmpty(result) ? "AuthSuccess" : "AuthFailed",
            Guid.Empty, "Auth", result ?? "OK", default);
        return result;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L279-L304' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-aspauthorize' title='Start of snippet'>anchor</a></sup>
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
// IOrdinalSerializable: Compact array-based JSON serialization
public class MoneyValueObject : IOrdinalSerializable
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyValueObject(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    // Properties in alphabetical order: Amount, Currency
    public object?[] ToOrdinalArray() => [Amount, Currency];
    // JSON: [100.50, "USD"] instead of {"Amount":100.50,"Currency":"USD"}
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L203-L220' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-ordinalserializable' title='Start of snippet'>anchor</a></sup>
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
// IOrdinalConverterProvider<TSelf>: Custom converter for ordinal serialization
public class MoneyWithConverter : IOrdinalSerializable, IOrdinalConverterProvider<MoneyWithConverter>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyWithConverter(decimal amount, string currency)
        => (Amount, Currency) = (amount, currency);

    public object?[] ToOrdinalArray() => [Amount, Currency];

    // Static factory provides the converter
    public static JsonConverter<MoneyWithConverter> CreateOrdinalConverter()
        => new MoneyOrdinalConverter();
}

// Converter implementation (outside snippet for brevity)
file sealed class MoneyOrdinalConverter : JsonConverter<MoneyWithConverter>
{
    public override MoneyWithConverter Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException();
        reader.Read(); var amount = reader.GetDecimal();
        reader.Read(); var currency = reader.GetString() ?? "USD";
        reader.Read();
        return new MoneyWithConverter(amount, currency);
    }

    public override void Write(
        Utf8JsonWriter writer, MoneyWithConverter value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Amount);
        writer.WriteStringValue(value.Currency);
        writer.WriteEndArray();
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L222-L261' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-ordinalconverterprovider' title='Start of snippet'>anchor</a></sup>
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
// IEventTracker: Wait for pending fire-and-forget events during shutdown
[Factory]
public static partial class EventTrackerDemo
{
    [Execute]
    private static async Task<int> _WaitForEvents([Service] IEventTracker eventTracker, CancellationToken ct)
    {
        var pending = eventTracker.PendingCount;
        if (pending > 0)
            await eventTracker.WaitAllAsync(ct);  // Graceful shutdown
        return pending;
    }
}
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Interfaces/InterfacesSamples.cs#L263-L277' title='Snippet source file'>snippet source</a> | <a href='#snippet-interfaces-eventtracker' title='Start of snippet'>anchor</a></sup>
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
