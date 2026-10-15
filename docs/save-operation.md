# Save Operation

A class factory whose object implements `IFactorySaveMeta` gets one `Save` method in place of its write methods. The object says what it is (new, stored, or marked deleted) and Save calls the matching `[Insert]`, `[Update]` or `[Delete]`. The caller does not choose. Save is one use of the client/server seam that RemoteFactory generates: the object crosses to the server with its state, and the write runs there.

The code on this page comes from RemoteFactory's compiled example set, `src/Design`, mostly its `Order` aggregate.

## IFactorySaveMeta

`IFactorySaveMeta` has two properties, `IsNew` and `IsDeleted`. `Order` declares them as plain public properties:

<!-- snippet: save-ifactorysavemeta -->
<a id='snippet-save-ifactorysavemeta'></a>
```cs
public bool IsNew { get; set; } = true;
public bool IsDeleted { get; set; }
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L119-L122' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-ifactorysavemeta' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Both are public `get; set;`. Both must cross the wire, so that the server's copy routes the same way the client's would, and a property with a private setter does not cross it. The object's own operations keep them right:
- Create sets `IsNew`;
- Fetch clears it;
- `Order`'s complete hook clears it after an Insert;
- `MarkDeleted` sets `IsDeleted`.

A screen binds to the domain verb, not to the flag.

`IOrder` extends `IFactorySaveMeta`, so the generated `IOrderFactory` has `Save(IOrder target)`.

## Routing

Save checks `IsDeleted` first, then `IsNew`:

| IsNew | IsDeleted | Save calls | Returns |
|-------|-----------|-----------|---------|
| true | false | `[Insert]` | the object |
| false | false | `[Update]` | the object |
| false | true | `[Delete]` | the object |
| true | true | nothing: it was never stored | `null` |

The other outcomes:
- **A missing write method.** A branch whose method the class does not declare throws `NotImplementedException`. With no `[Delete]`, that includes a new-and-deleted object. A type that has no Delete should not offer a way to mark itself deleted: `CustomerPreferences`, below, has none.
- **A `bool` write that returns `false`.** Save returns `null`.
- **Authorization.** On a factory with `[AuthorizeFactory<T>]`, `Save` throws `NotAuthorizedException` when a check denies the write, and `TrySave` returns the refusal instead. Two outcomes are currently reported as refusals although no check refused: a new-and-deleted object, and a `bool` write that returns `false` ([#104](https://github.com/NeatooDotNet/RemoteFactory/issues/104)). See [Authorization](authorization.md).

## Write Operations

Each write is a method on the class. `Order` is the aggregate root, so its Insert and Update write its own row and hand the row's line collection to the line list's factory:

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
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L268-L284' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-insert' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L286-L301' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-update' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L303-L315' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-delete' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Cascading to Children

One Save on the root writes the whole aggregate, but nothing in the generator walks the graph. The root's Insert and Update call `IOrderLineListFactory.Save(Lines, row.Lines)`, and the list does the rest:
- it makes a row for each new line;
- it finds the row of each existing line;
- it removes the row of each deleted line;
- it hands each live line its own row.

Each line maps itself into the row it is handed. The root saves once, at the end. See [Factory Operations — Collection Factories](factory-operations.md#collection-factories).

## Calling Save

The client calls Save and keeps what it returns:

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

Save returns the object the server sent back, not the one the client passed in. Always reassign it. The server may have set an id, cleared `IsNew`, or removed deleted lines, and none of that reaches the original.

## Return Values

`Save` returns the object's interface. It is a `Task` when any write is `[Remote]` or returns `Task`, and nullable when the class has a `[Delete]` or a `bool` write. So `Order`'s is `Task<IOrder?>`, while `CustomerPreferences`' `SaveUpsert` returns `Task<ICustomerPreferences>`, never null. Where it is nullable, it is `null` only in the cases the routing table and outcomes above name. When authorization applies, the factory also has `TrySave`, which returns `Authorized<IOrder>`, and `CanSave`.

## Partial Operations

A class declares only the writes it has. `CustomerPreferences` has one method for both Insert and Update, and no Delete:

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

| Writes declared | Shape |
|-------------------|---------|
| Insert only | records that never change after they are written |
| Insert and Update (or one `[Insert, Update]` method) | stored and edited, never removed |
| Insert, Update and Delete | the full lifecycle |

A combined method's save is named after it: `SaveUpsert` here. See [Factory Operations — Combining Operations](factory-operations.md#combining-operations).

## Optimistic Concurrency

RemoteFactory has no concurrency check of its own, and it maps no exception from a write to a status of its own. Keep a version on the row and on the object, have Fetch load it, and have Update compare it. A stale version is a conflict the user can resolve by reloading, so it is a result, not a throw: declare Update as `bool` and return `false`, and `Save` returns `null`. With `[AuthorizeFactory<T>]`, that `false` currently surfaces as `NotAuthorizedException` ([#104](https://github.com/NeatooDotNet/RemoteFactory/issues/104)). An exception thrown by a write reaches a remote caller as an `HttpRequestException` for a 500 response, and its type and message stay on the server. See [ASP.NET Core Integration — When a Call Fails](aspnetcore-integration.md#when-a-call-fails).

## Validation

Save does not validate. A check the user can recover from is a verdict the screen reads before it calls Save. `Order.CanSubmit` is one. A throw in a write method or a hook is for broken state. Who may write is authorization: see [Authorization](authorization.md).

## Testing Save Routing

Save through the client container, so the object crosses the serializer the way it would over HTTP, then read the server's store:

<!-- snippet: save-testing -->
<a id='snippet-save-testing'></a>
```cs
[Fact]
public async Task Save_InsertThenUpdate_WritesTheOneRow()
{
    var (server, client, local) = DesignClientServerContainers.Scopes();
    var orderFactory = client.GetRequiredService<IOrderFactory>();
    var db = server.GetRequiredService<IOrderDbContext>();

    var order = orderFactory.Create("Routing_Customer");
    order = (await orderFactory.Save(order))!;
    Assert.False(order.IsNew);
    Assert.Equal("Routing_Customer", (await db.FindAsync(order.Id))!.CustomerName);

    order.CustomerName = "Routing_Customer_Renamed";
    order = (await orderFactory.Save(order))!;
    Assert.Equal("Routing_Customer_Renamed", (await db.FindAsync(order.Id))!.CustomerName);

    server.Dispose();
    client.Dispose();
    local.Dispose();
}
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/SaveTests.cs#L53-L74' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Factory Operations](factory-operations.md#lifecycle-hooks): Insert, Update, Delete and the lifecycle hooks
- [Authorization](authorization.md): securing writes
- [Serialization](serialization.md): what crosses the wire
