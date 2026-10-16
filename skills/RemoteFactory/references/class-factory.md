# Class Factory

Use for an aggregate the client creates, loads, edits and saves. `[Factory]` goes on the class, `[Remote]` on the operations the client calls, and `IFactorySaveMeta` gives `Save` its routing. The example is RemoteFactory Design's `Order`, whose lines are `OrderLine` children in an `OrderLineList`.

## The aggregate

<!-- snippet: order-class-declaration -->
<a id='snippet-order-class-declaration'></a>
```cs
[Factory]
internal partial class Order : IOrder, IFactorySaveMeta, IFactoryOnStartAsync, IFactoryOnCompleteAsync
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L80-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-order-class-declaration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: readme-domain-model -->
<a id='snippet-readme-domain-model'></a>
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
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L234-L318' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-domain-model' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`Create` is local: building a new order needs nothing only the server has, so the client calls it with no round trip, and its `[Service]` parameter resolves on the client. It is `[Remote, Create]` only when creation needs the server - tenant defaults, a sequence, an authorization check that needs server services. `Fetch`, `Insert`, `Update` and `Delete` are `[Remote] internal`: the client's call crosses to the server, where the persistence services are.

**Generates** `IOrderFactory` with `Create`, `Fetch`, `OpenDraft` and `Save`. `Insert`, `Update` and `Delete` are not on the interface; `Save` chooses one.

## `IFactorySaveMeta` and Save routing

<!-- snippet: save-ifactorysavemeta -->
<a id='snippet-save-ifactorysavemeta'></a>
```cs
public bool IsNew { get; set; } = true;
public bool IsDeleted { get; set; }
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L121-L124' title='Snippet source file'>snippet source</a> | <a href='#snippet-save-ifactorysavemeta' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

| `IsNew` | `IsDeleted` | `Save` calls |
|---|---|---|
| true | false | `[Insert]` |
| false | false | `[Update]` |
| false | true | `[Delete]` |
| true | true | nothing |

Both properties are public `get; set;`: routing happens on the server, so they must cross the wire. `Save` returns the saved instance - from the client, a new one - so the caller reassigns:

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

On a factory with `[AuthorizeFactory<T>]`, a new-and-deleted object and a `bool` write that returns `false` both surface as `NotAuthorizedException` today ([#104](https://github.com/NeatooDotNet/RemoteFactory/issues/104)).

## The list cascade

The root's `Insert` and `Update` above make the order's row and hand its line rows to the list factory's `Save`. The list loops: it makes a row for each new line, finds the row of each existing one, removes the row of each removed one, and hands each line its row. Each line maps itself into the row it is handed (`OrderLine` in [SKILL.md](../SKILL.md#one-class-a-child-and-a-root)). The root flushes once.

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

## Lifecycle hooks

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

- `IFactoryOnStart` / `IFactoryOnStartAsync` - before the operation
- `IFactoryOnComplete` / `IFactoryOnCompleteAsync` - after it succeeds
- `IFactoryOnCancelled` / `IFactoryOnCancelledAsync` - when it is cancelled

## Class-level `[Execute]`

A `static` `[Execute]` on the class is an aggregate factory method with logic around the standard operations: it returns the aggregate. The common one is create-or-fetch, every open, start and ensure seam:

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

It calls a server-only `[Fetch]`, `internal` without `[Remote]`, which the client cannot reach:

<!-- snippet: skill-order-fetchdraft -->
<a id='snippet-skill-order-fetchdraft'></a>
```cs
[Fetch]
internal async Task<bool> FetchDraft(
    string customerName,
    [Service] IOrderDbContext db,
    [Service] IOrderLineListFactory lineListFactory)
{
    var row = await db.FindDraftAsync(customerName);
    if (row == null)
        return false;

    LoadFrom(row, lineListFactory);
    return true;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L327-L341' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-order-fetchdraft' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The caller asks the factory, as for any operation:

<!-- snippet: readme-client-usage -->
<a id='snippet-readme-client-usage'></a>
```cs
// The customer's draft: the stored one, or a new one. One server call.
IOrder? order = await orderFactory.OpenDraft("Customer_42");

// Build a line on the client and add it. No server call.
order.AddLine(lineFactory.Create("Widget C", 12.50m, 2));

// Save sends the whole order; keep what it returns.
order = await orderFactory.Save(order);
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/OrderUsageTests.cs#L31-L40' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-client-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

- It is `public static` or `internal static`, named as the factory method will be (no underscore). `[Remote]` decides whether the client crosses: `[Remote] public static` crosses, `public static` runs on whichever tier resolves the factory, `internal static` without `[Remote]` is server-only. The three side by side: [SKILL.md](../SKILL.md#execute-obeys-remote).
- It returns `Task<T>` of the class's service type - `Task<IOrder>` here, or the concrete type when there is no matching interface. A command that returns something else is a static factory command ([static-factory.md](static-factory.md)).
- It becomes a method on `IOrderFactory`, not a delegate type. There is no instance `[Execute]` (NF0103).

## Visibility

The generator reads each method's declared accessibility, not the class's:

| Method | On the factory interface | Guard and trimming |
|---|---|---|
| `[Remote] internal` | promoted to `public` | guarded; body trimmed from a client published with `IsServerRuntime` off |
| `internal`, no `[Remote]` | `internal` | guarded; trimmed |
| `public`, no `[Remote]` | `public` | none; runs on the tier that calls it |
| `[Remote] public` | - | NF0105 (a static method is exempt: `[Remote]` alone decides) |

The factory interface is `internal` only when every member is. A `public` method on an `internal` class is still treated as `public`: unguarded, untrimmed, on the public interface.

| Accessibility | Controls |
|---|---|
| Class `internal` | Hides the concrete type; pair it with a `public` interface (`IOrder`) so the factory returns the interface |
| Method `internal`, no `[Remote]` | Server-only: guard, `internal` modifier on the interface, trimmable |
| Method `[Remote] internal` | The client's entry point: guard, promoted to `public`, trimmable |
| Method `public` | On the interface, no guard, cannot take `[Remote]` |

A child's operations are `internal` and never `[Remote]`; its `Create` is the exception, `public` and local, so the client builds a child before adding it. A type that is both a child and a root carries both shapes, and the signature tells them apart: `OrderLine`'s child `Fetch` takes its row, its root `Fetch` takes a key and is `[Remote]`:

<!-- snippet: orderline-duality -->
<a id='snippet-orderline-duality'></a>
```cs
/// <summary>
/// Loads one line by its id, as a root; false when no order holds it.
/// </summary>
[Remote, Fetch]
internal async Task<bool> Fetch(Guid id, [Service] IOrderDbContext db)
{
    var row = await db.FindLineAsync(id);
    if (row == null)
        return false;

    LoadFrom(row);
    return true;
}
```
<sup><a href='/src/Design/Design.Domain/Entities/OrderLine.cs#L189-L203' title='Snippet source file'>snippet source</a> | <a href='#snippet-orderline-duality' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**CS0051** occurs when an `internal` type appears in a `public` method of a `public` class. On an `internal` class, C# caps its `public` methods at `internal`, so an `internal` factory interface can be a `[Service]` parameter there. If you hit CS0051, make the consuming class `internal`.

## Rules

1. `partial` on every `[Factory]` class; the generator adds serialization to it.
2. Public setters on every property that must cross the wire.
3. `[Remote] internal` on the operations the client calls; `[Remote] public` is NF0105.
4. Child operations `internal`, never `[Remote]`; a child's `Create` `public` and local.
5. A class-level `[Execute]` is `static` and returns `Task<T>` of the class's service type.
6. Business logic lives in the entity, not the factory; the Neatoo skill places it.

## `[SuppressFactory]`

The generator handles only types that carry `[Factory]` on their own declaration: a class derived from a `[Factory]` class gets no factory unless it carries `[Factory]` too ([#141](https://github.com/NeatooDotNet/RemoteFactory/issues/141)). `[SuppressFactory]` turns generation off for a class or record that also carries `[Factory]`; on an interface, or on a type without `[Factory]`, it has no effect. `[Factory]` on an `abstract` or generic class is ignored.
