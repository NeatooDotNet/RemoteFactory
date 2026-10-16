# Factory Operations

An operation attribute tells the generator what a method is for: creating an object, loading one, writing one, or a command. The generator builds the factory that calls it, injects the `[Service]` parameters the method declares, and, for a `[Remote]` operation called from a client, carries the call to the server and the result back.

If you're new to RemoteFactory, start with [Getting Started](getting-started.md). This page is the reference for the six operations. The code comes from RemoteFactory's compiled example set, `src/Design`, mostly its `Order` aggregate.

## Overview

| Operation | What it is | What the factory does |
|-----------|-----------|-----------------------|
| `[Create]` | The equivalent of `new` | Builds the object and calls your Create; returns it |
| `[Fetch]` | Load existing data by key | Builds the object and calls your Fetch; returns it, or `null` when Fetch returns `false` |
| `[Insert]` | Write a new object | `Save()` runs it when `IsNew` is true |
| `[Update]` | Write a changed object | `Save()` runs it when `IsNew` is false |
| `[Delete]` | Remove an object | `Save()` runs it when `IsDeleted` is true |
| `[Execute]` | A command, or a factory method with logic around Create and Fetch | Calls your method and returns its result |

## The Generated Factory

For `Order`, the generator emits `IOrderFactory`:
- **`Create(string customerName)`** for the `[Create]`.
- **`Fetch(int id)` returning `Task<IOrder?>`.** A `bool` Fetch makes the result nullable.
- **`OpenDraft(string customerName)`** for the class-level `[Execute]`.
- **`Save(IOrder target)` returning `Task<IOrder?>`.** It is nullable because saving an object that is both new and deleted returns `null`, as does a `bool` write that returns `false`. Saving a deleted stored object returns the object.
- **`TrySave`, `CanSave` and the other `Can*` methods,** when authorization applies.

Insert, Update and Delete are never on the interface: callers call `Save`.

Each method's parameters are the operation's value parameters plus an optional `CancellationToken cancellationToken = default`. `[Service]` parameters are not on the interface; the factory resolves them. Default values on your method's parameters are not carried over. When the class implements a matching `I{ClassName}` interface, the factory returns that interface.

## Create Operation

Create is `new` for a factory object: it takes what a constructor would take and returns a new object. A root's Create is local. It runs on the tier that calls it, with no server call, and needs `[Remote]` only when creating needs something only the server has.

### A Create Method

A method marked `[Create]` runs after the object is built. It can take `[Service]` parameters, here the factory for the order's line list:

<!-- snippet: operations-create-method -->
<a id='snippet-operations-create-method'></a>
```cs
/// <summary>
/// Creates a new Order. Local: runs on the tier that calls it.
/// </summary>
[Create]
public void Create(
    string customerName,
    [Service] IOrderLineListFactory lineListFactory)
{
    CustomerName = customerName;
    Lines = lineListFactory.Create();
    Status = OrderStatus.Draft;
    IsNew = true;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L235-L249' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-method' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### A Create Constructor

A constructor can be the Create:

<!-- snippet: operations-create-constructor -->
<a id='snippet-operations-create-constructor'></a>
```cs
[Create]
public CustomerPreferences()
{
    DeliveryWindow = "Any";
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/CustomerPreferences.cs#L57-L63' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-create-constructor' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Be aware that this constructor is also the one every other path uses to build the object. A Fetch, and a deserialization on either tier, both run it before they fill the properties. Keep it to defaults the next step may overwrite, and put anything with a side effect (a lookup, a counter, an event) in a Create method. The constructor takes no arguments: a type whose only constructor requires arguments loses its ordinal serializer, and DI could not build the object to fetch it.

Two other forms exist:
- **A static `[Create]` method** that builds and returns the object, for a type whose constructor is private.
- **Several `[Create]` overloads,** each generating a `Create` on the factory.

A Create that returns `bool` makes the factory return `null` when it returns `false`. Design does not use it: a check the user can recover from is a verdict the screen reads before it acts, not a Create that declines.

## Fetch Operation

Fetch loads existing data by key. The factory builds the object (running its constructor) and calls your Fetch to fill it:

<!-- snippet: operations-fetch-instance -->
<a id='snippet-operations-fetch-instance'></a>
```cs
/// <summary>
/// Loads an existing Order and its lines; false when there is none.
/// </summary>
[Remote, Fetch]
internal async Task<bool> Fetch(
    int id,
    [Service] IOrderDbContext db,
    [Service] IOrderLineListFactory lineListFactory)
{
    var row = await db.FindAsync(id);
    if (row == null)
        return false;

    LoadFrom(row, lineListFactory);
    return true;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L251-L268' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-fetch-instance' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Not found is `false`.** Fetch runs on the instance, so it cannot return `null` itself. Returning `false` tells the factory to discard the instance and return `null`, and the generated signature is nullable (`Task<IOrder?>`). This is the one operation that handles "not there" cleanly. Use it for a missing row, not for a business filter.

**Constructor and method services.** A `[Service]` on the constructor is something the object needs on whichever tier it is on, and it is injected again after deserialization. A `[Service]` on the method is resolved from the container of the tier the method runs on. For a `[Remote]` Fetch that is the server, so a store or database context goes there.

## Insert, Update, Delete Operations

Callers don't invoke these: they call `factory.Save(order)`, and the factory runs the one the object's state calls for:
- `IsNew` true runs Insert;
- `IsNew` false runs Update;
- `IsDeleted` true runs Delete.

Keep what `Save` returns: `order = await factory.Save(order)`. See [Save Operation](save-operation.md).

Each write handles only its own object's row. An aggregate's children are written by the cascade: the root hands its row's child collection to the list factory's `Save`, and the list makes, finds or removes each child's row and hands each child its own (see [Collection Factories](#collection-factories)). The root flushes once.

### Insert

<!-- snippet: operations-insert -->
<a id='snippet-operations-insert'></a>
```cs
/// <summary>
/// Inserts a new Order and its lines.
/// </summary>
[Remote, Insert]
internal async Task Insert(
    [Service] IOrderDbContext db,
    [Service] IOrderLineListFactory lineListFactory)
{
    var row = new OrderRow();
    MapTo(row);
    db.Add(row);
    lineListFactory.Save(Lines, row.Lines);
    await db.SaveChangesAsync();
    Id = row.Id;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L270-L286' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Update

<!-- snippet: operations-update -->
<a id='snippet-operations-update'></a>
```cs
/// <summary>
/// Updates an existing Order and its lines.
/// </summary>
[Remote, Update]
internal async Task Update(
    [Service] IOrderDbContext db,
    [Service] IOrderLineListFactory lineListFactory)
{
    var row = await db.FindAsync(Id)
        ?? throw new KeyNotFoundException($"Order {Id} not found");
    MapTo(row);
    lineListFactory.Save(Lines, row.Lines);
    await db.SaveChangesAsync();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L288-L303' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A row missing under a loaded object is a failure, so Update throws.

### Delete

<!-- snippet: operations-delete -->
<a id='snippet-operations-delete'></a>
```cs
/// <summary>
/// Deletes an Order. Its line rows go with its row.
/// </summary>
[Remote, Delete]
internal async Task Delete([Service] IOrderDbContext db)
{
    var row = await db.FindAsync(Id)
        ?? throw new KeyNotFoundException($"Order {Id} not found");
    db.Remove(row);
    await db.SaveChangesAsync();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L305-L317' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Deleting the order removes its row, and its line rows go with it, as a database's cascade delete would. A line removed while the order is still kept is a different case: the list's Save removes that line's row during the next Update.

### Combining Operations

When Insert and Update do the same write, one method can carry both attributes:

<!-- snippet: operations-insert-update -->
<a id='snippet-operations-insert-update'></a>
```cs
[Remote, Insert, Update]
internal async Task Upsert([Service] ICustomerPreferencesStore store)
{
    await store.WriteAsync(new CustomerPreferencesRow(CustomerName, DeliveryWindow, NotifyBySms, LastSavedAt));
    IsNew = false;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/CustomerPreferences.cs#L93-L100' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-insert-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The factory's save method is named `Save` plus your method's name. If the name starts with the group's highest-ranked operation (Delete, then Update, then Insert), that word is dropped. Here the group ranks Update highest and `Upsert` does not start with it, so the method is **`SaveUpsert`**. A combined method named `Update` would give `Save`, but one named `Insert` or `InsertOrUpdate` keeps its whole name: `SaveInsert`, `SaveInsertOrUpdate`. With no `[Delete]`, marking the object deleted makes the save throw.

## Execute Operation

`[Execute]` is for two things:
- **A command** on a static `[Factory]` class. It usually returns nothing or a simple result.
- **A factory method on a class factory** with logic around the standard operations, such as create-or-fetch.

| Context | Method | Generates |
|---------|--------|-----------|
| **Static factory** class | `private static`, underscore prefix (`_SubmitOrder`) | A delegate type (`OrderCommands.SubmitOrder`), registered in DI |
| **Class factory** | `public static` (or `internal static` for a server-only one) | A method on the factory interface; must return `Task` of the class's interface, or of the class itself when it has no matching interface |

`[Execute]` obeys `[Remote]` like every other operation:
- **With it,** the client's call crosses to the server.
- **Without it,** the method runs on whichever tier resolves it, with that tier's `[Service]` parameters. So a bare `[Execute]` that takes a server-only service fails on the client at call time.

See the [attributes reference](attributes-reference.md#execute) for the bare shape.

### Static Factory Execute

A command changes an aggregate through the aggregate's own factory: it fetches the aggregate, calls its verb, and saves. It never writes the aggregate's row itself, which would skip the aggregate's rules and its own Insert/Update. On the server, the factory runs locally, with no second round trip:

<!-- snippet: operations-execute -->
<a id='snippet-operations-execute'></a>
```cs
[Remote, Execute]
private static async Task<SubmitOutcome> _SubmitOrder(
    int orderId,
    [Service] IOrderFactory orderFactory,
    CancellationToken cancellationToken)
{
    var order = await orderFactory.Fetch(orderId, cancellationToken);
    if (order == null)
        return SubmitOutcome.NotFound;
    if (!order.CanSubmit)
        return SubmitOutcome.NotSubmittable;

    order.Submit();
    await orderFactory.Save(order, cancellationToken);
    return SubmitOutcome.Submitted;
}
```
<sup><a href='/src/Design/Design.Domain/Services/OrderCommands.cs#L60-L77' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The client resolves the delegate and calls it: `var outcome = await submitOrder(orderId)`. A missing order is reported in the result. The command reads the order's `CanSubmit` verdict before calling `Submit`, which throws only when the caller ignored that verdict.

### Command Pattern

A command that returns only `Task` has nowhere to report a missing target, so a missing order is an exception:

<!-- snippet: operations-execute-command -->
<a id='snippet-operations-execute-command'></a>
```cs
[Remote, Execute]
private static async Task _CancelOrder(int orderId, [Service] IOrderFactory orderFactory)
{
    var order = await orderFactory.Fetch(orderId)
        ?? throw new KeyNotFoundException($"Order {orderId} not found");
    if (order.Status == OrderStatus.Cancelled)
        return;   // already cancelled: a retried call is not an error

    order.Cancel();
    await orderFactory.Save(order);
}
```
<sup><a href='/src/Design/Design.Domain/Services/OrderCommands.cs#L82-L94' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Over HTTP, that exception reaches the caller as `HttpRequestException`: the server answers 500, and the exception's type does not cross. See [ASP.NET Core Integration — When a Call Fails](aspnetcore-integration.md#when-a-call-fails).

An order that is already cancelled is not missing: the command reads its status and leaves it, so a retried cancel succeeds. `Order.Cancel` throws only for a caller that skipped that check.

### Class Factory Execute

Create-or-fetch belongs with the aggregate. It is an `[Execute]` that calls the aggregate's own Fetch and then, when nothing is found, its Create:

<!-- snippet: operations-class-execute -->
<a id='snippet-operations-class-execute'></a>
```cs
[Remote, Execute]
public static async Task<IOrder> OpenDraft(
    string customerName,
    [Service] IOrderFactory factory)
{
    return await factory.FetchDraft(customerName)
        ?? factory.Create(customerName);
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L364-L373' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-class-execute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`FetchDraft` is a plain `internal` `[Fetch]` with no `[Remote]`, so only the server calls it. The client calls `orderFactory.OpenDraft(customerName)` and gets the stored draft or a new, unsaved one, in one round trip. Don't put that choice in a Create that looks for an existing object, or in a Fetch that makes one: either hides which one the caller got.

## Events

There is no `[Event]` method attribute. Domain events are raised with `IFactoryEvents.Raise<T>` inside a factory method. They are dispatched to `[FactoryEventHandler<T>]` handlers, which run in the caller's DI scope, one after another, and are awaited, and they can be relayed to the client. See [Factory Events](factory-events.md).

Work that must not join the caller's transaction (email, webhooks, an external audit sink) has no framework surface. [Factory Events](factory-events.md) describes the manual pattern: a `Task.Run` with its own `IServiceScopeFactory` scope, with any ambient state such as the correlation ID copied in first.

## Collection Factories

A `[Factory]` list class is how an aggregate's children are created, loaded and saved. The list's `[Update]` is the cascade, and it is how a child removed on the client reaches the server: the list keeps the removed child, flagged deleted, until the next save removes its row. A plain `List<Child>` has none of that, so use one only for children that are never written.

`OrderLineList`:

<!-- snippet: collection-factory-basic -->
<a id='snippet-collection-factory-basic'></a>
```cs
/// <summary>
/// Creates an empty list. Public and local, so a root created on the client
/// can create its list there.
/// </summary>
[Create]
public void Create()
{
}

/// <summary>
/// Loads the lines from the order row's line rows, handing each line its row.
/// </summary>
[Fetch]
internal void Fetch(IEnumerable<OrderLineRow> rows, [Service] IOrderLineFactory lineFactory)
{
    foreach (var row in rows)
    {
        Add((OrderLine)lineFactory.Fetch(row));
    }
}

/// <summary>
/// The cascade: makes, finds or removes each line's row and hands each live
/// line its row through the line factory's Save.
/// </summary>
/// <remarks>
/// Called by Order.Insert and Order.Update through IOrderLineListFactory.Save,
/// with the order row's line collection. Neither the list nor the root maps a
/// line's row; the line does.
/// </remarks>
[Update]
internal void Update(ICollection<OrderLineRow> rows, [Service] IOrderLineFactory lineFactory)
{
    foreach (var line in this)
    {
        if (line.IsDeleted)
        {
            rows.Remove(rows.Single(r => r.Id == line.Id));
            continue;
        }

        OrderLineRow row;
        if (line.IsNew)
        {
            row = new OrderLineRow();
            rows.Add(row);
        }
        else
        {
            row = rows.Single(r => r.Id == line.Id);
        }

        lineFactory.Save(line, row);
    }

    // The removed lines' rows are gone; the list the save returns holds live lines only.
    RemoveAll(l => l.IsDeleted);
}

// -------------------------------------------------------------------------
// Domain Methods
// -------------------------------------------------------------------------

public void AddLine(IOrderLine line)
{
    Add((OrderLine)line);
}
```
<sup><a href='/src/Design/Design.Domain/Entities/OrderLine.cs#L286-L354' title='Snippet source file'>snippet source</a> | <a href='#snippet-collection-factory-basic' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- **The list's Create is public and local,** so a root created on the client creates its list there. Its other operations are `internal`: only the root's operations call them, on the server.
- **Load is symmetric with the save.** The list's Fetch receives the rows and hands each line its own; neither the list nor the root maps a line's row.
- **The list does not store the line factory.** A client builds a line with `lineFactory.Create(...)` and adds it with `AddLine`. A factory stored on the list would be a method-injected service, which does not survive serialization.
- **No `[Remote]` on the list's own operations;** the root's `[Remote]` operation is the entry point. A list loaded on demand is the exception: a `[Remote]` Fetch on the list factory, called when needed.

The root creates, loads and saves its list through the list factory: `Lines = lineListFactory.Create()` in Create, `lineListFactory.Fetch(row.Lines)` when it loads, and `lineListFactory.Save(Lines, row.Lines)` in Insert and Update, as shown above.

## Remote Attribute

`[Remote]` marks an entry point from the client to the server. When a Remote-mode client calls a `[Remote]` operation:
- the factory serializes the call and posts it to `/api/neatoo`;
- the server runs the method with its own services;
- the result comes back.

Once on the server, `[Remote]` does nothing different: methods called from server-side code need none. Order's Create, above, has no `[Remote]` and runs on the client; its Fetch and writes have it and run on the server. In Server and Logical mode every operation runs locally. See [Client-Server Architecture](client-server-architecture.md) and [Factory Modes](factory-modes.md).

## Lifecycle Hooks

An object can implement hook interfaces that the factory calls around its operations. They are for preparing state and recording what happened; they are not checks. A check the user can recover from is a verdict the screen reads before acting, and a throw is for broken state.

### IFactoryOnStart / IFactoryOnStartAsync

Called before the operation. `CustomerPreferences` stamps its save time before a write:

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

### IFactoryOnComplete / IFactoryOnCompleteAsync

Called after the operation succeeds. `Order` resets `IsNew` after an Insert:

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
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L185-L195' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-lifecycle-oncomplete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### IFactoryOnCancelled / IFactoryOnCancelledAsync

Called when an async instance operation throws `OperationCanceledException`, as cancelling its `CancellationToken` does, before the cancellation propagates. Use it for cleanup or logging.

### When Each Hook Runs

- **The synchronous hooks** (`IFactoryOnStart`, `IFactoryOnComplete`) run around instance operations: start before the method, complete after it.
- **The async hooks** (`IFactoryOnStartAsync`, `IFactoryOnCompleteAsync`) run only around `Task`-returning operations. So `Order`'s async start hook runs for its Fetch and writes, but not for its synchronous Create.
- **A constructor or static Create or Fetch** gets only the synchronous `IFactoryOnComplete`, called on the result, even when the operation returns `Task`.
- **A class-level `[Execute]`** gets no hooks.
- **A `bool` operation that returns `false`** skips the complete hook; the factory returns `null`.
- **When an operation throws,** the start hook has already run and the complete hook does not. When an async instance operation is cancelled, the cancelled hook runs instead of the complete hook.

## CancellationToken Support

Every generated factory method takes an optional `CancellationToken`. Declare one on your operation to receive it. Over HTTP it is the request's: it fires when the client cancels or disconnects, or when the server begins shutting down. It is never serialized and never a `[Service]`.

<!-- snippet: aspnetcore-cancellation -->
<a id='snippet-aspnetcore-cancellation'></a>
```cs
[Remote, Fetch]
internal async Task<bool> Fetch(
    int orderId,
    [Service] IOrderDbContext db,
    CancellationToken cancellationToken)
{
    var row = await db.FindAsync(orderId, cancellationToken);
    if (row == null)
        return false;

    OrderId = row.Id;
    CustomerName = row.CustomerName;
    Status = row.Status;
    Lines = row.Lines
        .Select(l => new OrderSummaryLine(l.ProductName, l.Quantity, l.UnitPrice * l.Quantity))
        .ToList();
    Total = Lines.Sum(l => l.LineTotal);
    return true;
}
```
<sup><a href='/src/Design/Design.Domain/ReadModels/OrderSummary.cs#L88-L108' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-cancellation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Method Parameters

Declare an operation's parameters in this order: **value parameters, then `[Service]` parameters, then the `CancellationToken`.**

- **Value parameters** are the caller's arguments. They are serialized for a `[Remote]` call and appear on the generated factory method.
- **`[Service]` parameters** are resolved from DI on the tier that runs the method. They are not serialized and not on the factory method. Their order among themselves is free.
- **The `CancellationToken`** goes last.

`OrderSummary.Fetch` above and `SubmitOrder` follow it. A `params` array is a value parameter, and C# requires it to be last of all. On the generated class factory method, the token comes just before the `params` array.

The generator does not check the order. A class operation still compiles with another order. An `[Execute]` does not: its generated call passes values, then services, then the token, so an out-of-order `[Execute]` fails inside the generated code ([#135](https://github.com/NeatooDotNet/RemoteFactory/issues/135)).

Default values on value parameters are not carried to the generated factory method, so every argument is passed.

## Next Steps

- [Service Injection](service-injection.md): inject dependencies into factory methods
- [Authorization](authorization.md): secure factory operations
- [Save Operation](save-operation.md): `IFactorySave` routing
- [Factory Events](factory-events.md): `[FactoryEventHandler<T>]` and the client relay
