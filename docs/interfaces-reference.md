---
title: Interfaces Reference
nav_order: 11
---

# Interfaces Reference

The interfaces RemoteFactory defines: what each one's members are, who implements it, and which page covers it in full. The code comes from RemoteFactory's compiled example set, `src/Design`.

## Lifecycle Hooks

An object implements a hook interface to have the factory call it around its operations. The hooks prepare state and record what happened. They are not checks: a check the user can recover from is a verdict the screen reads before it acts, and a throw is for broken state.

Every hook method takes the `FactoryOperation` being run (`Create`, `Fetch`, `Insert`, `Update`, `Delete`, `Execute`).

| Interface | Member | Called |
|-----------|--------|--------|
| `IFactoryOnStart` | `void FactoryStart(FactoryOperation)` | before an instance operation |
| `IFactoryOnStartAsync` | `Task FactoryStartAsync(FactoryOperation)` | before a `Task`-returning instance operation |
| `IFactoryOnComplete` | `void FactoryComplete(FactoryOperation)` | after an operation succeeds |
| `IFactoryOnCompleteAsync` | `Task FactoryCompleteAsync(FactoryOperation)` | after a `Task`-returning instance operation succeeds |
| `IFactoryOnCancelled` | `void FactoryCancelled(FactoryOperation)` | when an async instance operation throws `OperationCanceledException` |
| `IFactoryOnCancelledAsync` | `Task FactoryCancelledAsync(FactoryOperation)` | as `IFactoryOnCancelled` |

`CustomerPreferences` stamps its save time in an async start hook:

<!-- snippet: operations-lifecycle-onstart -->
<a id='snippet-operations-lifecycle-onstart'></a>
```cs
public Task FactoryStartAsync(FactoryOperation factoryOperation)
{
    if (factoryOperation is FactoryOperation.Insert or FactoryOperation.Update)
    {
        LastSavedAt = DateTimeOffset.UtcNow;
    }
    return Task.CompletedTask;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/CustomerPreferences.cs#L110-L119' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-onstart' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`Order` clears `IsNew` in an async complete hook after an Insert:

<!-- snippet: operations-lifecycle-oncomplete -->
<a id='snippet-operations-lifecycle-oncomplete'></a>
```cs
public Task FactoryCompleteAsync(FactoryOperation factoryOperation)
{
    // Example: Reset IsNew after successful Insert
    if (factoryOperation == FactoryOperation.Insert)
    {
        IsNew = false;
    }
    return Task.CompletedTask;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L183-L193' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-oncomplete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### When Each Hook Runs

- **Before and after:** the start hook runs before the method and the complete hook after it. When the method throws, the start hook has already run and the complete hook does not. When an async instance operation is cancelled, the cancelled hook runs instead of the complete hook.
- **Async hooks:** they run only around `Task`-returning instance operations.
- **Constructor and static operations:** a constructor or static Create or Fetch gets only `IFactoryOnComplete`, called on the result.
- **A class-level `[Execute]`** gets no hooks.
- **A `bool` operation that returns `false`** skips the complete hook.

See [Factory Operations — Lifecycle Hooks](factory-operations.md#lifecycle-hooks).

## Save

### IFactorySaveMeta

Two read-only members, `bool IsNew` and `bool IsDeleted`, which the generated `Save` reads to choose Insert, Update or Delete. `IOrder` extends it, and `Order` implements both as public `get; set;` so that they cross the wire:

<!-- snippet: save-ifactorysavemeta -->
<a id='snippet-save-ifactorysavemeta'></a>
```cs
public bool IsNew { get; set; } = true;
public bool IsDeleted { get; set; }
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L119-L122' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-ifactorysavemeta' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See [Save Operation](save-operation.md) for the routing table and every outcome.

### IFactorySave&lt;T&gt;

The generator implements this on each class factory whose class implements `IFactorySaveMeta`. It registers the factory under it, keyed on the class itself (`IFactorySave<Order>`), and implements it explicitly. Its members are:
- `Save(T entity, CancellationToken)`, which returns `Task<IFactorySaveMeta?>`;
- `CanSave(CancellationToken)`;
- `CanSave(T target, CancellationToken)`.

Both `CanSave` overloads return `Task<Authorized>`, and `Authorized(true)` when no authorization applies. The parameterless one runs the write checks that take no target; the other runs them all.

You do not implement it, and application code does not call it. It is for framework code that saves an object knowing only its type. Application code calls the generated factory's own `Save`, typed on the interface:

<!-- snippet: save-usage -->
<a id='snippet-save-usage'></a>
```cs
var order = orderFactory.Create("Save_Customer");
order.AddLine(lineFactory.Create("Widget", 5.00m, 2));
order = (await orderFactory.Save(order))!;      // IsNew: Insert

order.CustomerName = "Save_Customer_Renamed";
order = (await orderFactory.Save(order))!;      // stored: Update

order.MarkDeleted();
order = (await orderFactory.Save(order))!;      // IsDeleted: Delete
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L31-L41' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Authorization

### IAspAuthorize

`Task<string?> Authorize(IEnumerable<AspAuthorizeData> authorizeData, bool forbid = false)`. The generated factory calls it for an operation that carries `[AspAuthorize]`.

`AddNeatooAspNetCore` registers the default, `AspAuthorize`, which evaluates the data against ASP.NET Core's policy provider and evaluator:
- it returns an empty string when authorized, and the failure message when not;
- with `forbid` true, it throws `AspForbidException` instead.

A replacement or a decorator is registered in DI like any service. See [Authorization](authorization.md).

## Serialization

The generator implements these interfaces on `[Factory]` types. A value object is a plain `record` and needs no `[Factory]` to cross the wire, so it implements none of them. One that takes `[Factory]` because its construction needs a service, such as Design's `TaxRate`, gets all three like any other `[Factory]` type. See [Serialization](serialization.md).

| Interface | Members | Purpose |
|-----------|---------|---------|
| `IOrdinalSerializable` | `object?[] ToOrdinalArray()` | writes the object as a JSON array of its property values |
| `IOrdinalConverterProvider<TSelf>` | `static abstract JsonConverter<TSelf> CreateOrdinalConverter()` | supplies the converter without reflection, so it survives IL trimming |
| `IOrdinalSerializationMetadata` | `static abstract string[] PropertyNames`, `static abstract Type[] PropertyTypes`, `static abstract object FromOrdinalArray(object?[])` | rebuilds the object from the array |

**Ordinal order:** the type's own properties first, then its base class's. Each group is in ordinal (`StringComparer.Ordinal`) name order, not declaration order. See [Serialization — What the Ordinal Array Holds](serialization.md#what-the-ordinal-array-holds).

## Factory Events

See [Factory Events](factory-events.md) for each contract in full.

### IFactoryEvents

`Raise<T>(T factoryEvent, RaiseOptions options = RaiseOptions.None, CancellationToken cancellationToken = default)`, plus `RaiseUntyped`, which the server uses for a remote raise. A factory method takes it as `[Service] IFactoryEvents`.

`Raise<T>` runs every matching `[FactoryEventHandler<T>]` in the caller's scope, one at a time and awaited, in no specified order. An `Immediate` handler runs at `Raise`, and its exception reaches the caller. `AfterFlush` and `AfterCommit` handlers are queued for their [drain point](factory-events.md#dispatch-phases). Unless `RaiseOptions.ServerOnly` is set, the event is also relayed to the client.

### IFactoryEventRelay

`Task Relay(IReadOnlyList<FactoryEventBase> events)`. The client application implements it and registers it in DI.

`Relay` is called once per remote round trip (a `[Remote]` factory call, or a client's own `Raise`, whose event comes back in the batch), after the caller's continuation resumes, even when the batch is empty. A batch that fails to deserialize (`UnknownFactoryEventTypeException`) is logged (EventId 3009) and not relayed. Its exceptions are logged (EventId 3008) and never reach the caller. The consumer marshals to the UI thread itself. Remote mode registers a no-op relay with `TryAddSingleton`, so the application's registration wins whether it comes before or after `AddNeatooRemoteFactory`.

### IFactoryEventPhaseCoordinator

`Task DrainAsync(DispatchPhase phase, CancellationToken cancellationToken = default)`. A server factory method takes it as `[Service]` and calls it between its flush and its commit.

`DrainAsync` accepts only `DispatchPhase.AfterFlush`; any other phase throws `ArgumentOutOfRangeException`.
- It runs the scope's queued `AfterFlush` handlers, and a handler's exception reaches the drain call.
- It does nothing when no entry factory call is active in the scope.
- `AfterFlush` work never drained runs at the `AfterCommit` point, with Warning 9007.

## Summary

| Interface | Purpose | Who implements it |
|-----------|---------|----------------|
| `IFactoryOnStart` / `IFactoryOnStartAsync` | before an operation | the domain class |
| `IFactoryOnComplete` / `IFactoryOnCompleteAsync` | after an operation succeeds | the domain class |
| `IFactoryOnCancelled` / `IFactoryOnCancelledAsync` | after an operation is cancelled | the domain class |
| `IFactorySaveMeta` | Save routing state | the domain class |
| `IFactorySave<T>` | Save for framework code | the generator |
| `IAspAuthorize` | ASP.NET Core authorization | RemoteFactory.AspNetCore (replaceable) |
| `IOrdinalSerializable` | ordinal writing | the generator |
| `IOrdinalConverterProvider<TSelf>` | ordinal converter | the generator |
| `IOrdinalSerializationMetadata` | ordinal reading | the generator |
| `IFactoryEvents` | raising factory events | RemoteFactory (inject it) |
| `IFactoryEventRelay` | receiving relayed events on the client | the application |
| `IFactoryEventPhaseCoordinator` | draining `AfterFlush` handlers | RemoteFactory (inject it) |

## Next Steps

- [Attributes Reference](attributes-reference.md): every attribute
- [Factory Operations](factory-operations.md): the operations and their hooks
- [Save Operation](save-operation.md): routing and outcomes
- [Service Injection](service-injection.md): DI integration
- [Factory Events](factory-events.md): handlers and the client relay
- [Authorization](authorization.md): authorization patterns
