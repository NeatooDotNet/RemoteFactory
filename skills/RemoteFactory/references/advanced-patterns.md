# Advanced RemoteFactory Patterns

## Custom Authorization with [AuthorizeFactory\<T\>]

> **Scope note:** this section covers `[AuthorizeFactory<T>]` on **class factories** — CRUD-scoped auth (`Create`/`Fetch`/`Insert`/`Update`/`Delete`) with generated `CanCreate`/`CanFetch`/`CanSave`/`CanDelete`. For `[AuthorizeFactory<T>]` on **interface factories** — which uses a different scope model (`Execute`/`Read` only) and per-method parameter matching — see [interface-factory.md](interface-factory.md#authorization-with-authorizefactoryt).

Define domain-specific authorization logic in a dedicated class:

<!-- snippet: authorization-interface -->
<a id='snippet-authorization-interface'></a>
```cs
public interface IAuthorizedOrderAuth
{
    /// <summary>
    /// Broad-scope check covering all Read (Create, Fetch) and Write (Insert, Update, Delete) operations.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool HasAccess();

    /// <summary>
    /// Who may create an order: checked by the local Create (and CanCreate) on
    /// the client, and again on the server when the new order is saved (Insert).
    /// </summary>
    /// <remarks>
    /// A local Create runs only on the client, so the client check is for the
    /// screen; the Insert scope is what the server enforces.
    /// </remarks>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Insert)]
    bool CanCreate();

    /// <summary>
    /// Fine-grained check for Fetch operations only.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    /// <summary>
    /// Fine-grained check for Delete operations only.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs#L68-L100' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: authorization-implementation -->
<a id='snippet-authorization-implementation'></a>
```cs
public class AuthorizedOrderAuth(ICurrentUser user) : IAuthorizedOrderAuth
{
    public bool HasAccess() => user.IsAuthenticated;

    public bool CanCreate() => user.IsInRole(OrderRoles.Sales);

    public bool CanFetch() => user.IsInRole(OrderRoles.Sales) || user.IsInRole(OrderRoles.Support);

    public bool CanDelete() => user.IsInRole(OrderRoles.Manager);
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs#L122-L133' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-implementation' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`ICurrentUser` is the example's signed-in user (`IsAuthenticated`, `IsInRole`), injected by constructor and registered on both tiers, because the rules run wherever the operation runs. `OrderRoles` holds the role names.

<!-- snippet: authorization-apply -->
<a id='snippet-authorization-apply'></a>
```cs
[Factory]
[AuthorizeFactory<IAuthorizedOrderAuth>]
internal partial class AuthorizedOrder : IAuthorizedOrder, IFactorySaveMeta, IFactoryOnCompleteAsync
```
<sup><a href='/src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs#L99-L103' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generated authorization methods:**
- `CanCreate()`, `CanFetch()`, `CanInsert()`, `CanUpdate()`, `CanDelete()` on factory interface
- Returns `Authorized<T>` result with `HasAccess` property

**Use `[AuthorizeFactory<T>]` when:**
- Authorization is domain-specific with complex rules
- You want testable auth without HTTP infrastructure
- Different operations have different authorization requirements

### Parameterized Authorization Methods

An auth method receives the operation's parameters, matched **by type**, not by name. `ParamAuthOrder`'s `Fetch(Guid orderId)` hands its id to:

<!-- snippet: authorization-parameterized -->
<a id='snippet-authorization-parameterized'></a>
```cs
[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
bool CanFetchOrder(Guid orderId);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/ParamAuthOrderAuth.cs#L89-L92' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-parameterized' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The generator produces `CanFetch(Guid)` on the factory interface. Give a parameterized auth method a specific scope (`Fetch`): a `Read`-scoped one with a `Guid` parameter would try to match `Create` too, which may take no `Guid`.

### Target Parameter Authorization

An auth method can receive the **target object** on write operations, for a check that depends on its state:

<!-- snippet: authorization-target -->
<a id='snippet-authorization-target'></a>
```cs
[AuthorizeFactory(AuthorizeFactoryOperation.Write)]
bool CanWrite(IParamAuthOrder target);
```
<sup><a href='/src/Design/Design.Domain/Aggregates/ParamAuthOrderAuth.cs#L131-L134' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-target' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**CanXxx generation with target parameters:** When Write auth has a target parameter, `CanInsert`/`CanUpdate`/`CanDelete` are **not generated** — the entity isn't available before the write operation. `CanSave` is the exception: two overloads are generated because the caller has the entity in hand when deciding whether to save. `CanSave()` (parameterless) runs only non-target Write auth methods (role checks). `CanSave(target)` runs ALL Write auth methods — both non-target and target-parameterized. `CanCreate`/`CanFetch` are still generated when Read auth is parameterless.

---

## Authorization with [AspAuthorize]

Apply ASP.NET Core policy-based authorization to factory operations:

<!-- snippet: authorization-policy-apply -->
<a id='snippet-authorization-policy-apply'></a>
```cs
[Remote, Fetch]
[AspAuthorize("RequireAuthenticated")]
internal void Fetch(int id)
{
    Id = id;
    CustomerName = $"Customer_{id}";
    Total = 100.00m;
    IsNew = false;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/SecureOrder.cs#L102-L112' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Server configuration:**

<!-- snippet: authorization-policy-config -->
<a id='snippet-authorization-policy-config'></a>
```cs
services.AddAuthentication();   // add your scheme here: cookies, JWT bearer, ...
services.AddAuthorization(options =>
    options.AddPolicy("RequireAuthenticated", policy => policy.RequireAuthenticatedUser()));
services.AddHttpContextAccessor();
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L102-L107' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The server also calls `app.UseAuthentication()` and `app.UseAuthorization()` before `app.UseNeatoo()`, so the policy sees the signed-in user.

Roles, and several attributes on one operation, which all must pass:

<!-- snippet: authorization-policy-roles -->
<a id='snippet-authorization-policy-roles'></a>
```cs
[Remote, Insert]
[AspAuthorize(Roles = "Admin,Manager")]
internal Task Insert()
{
    Id = Random.Shared.Next(1000, 9999);
    return Task.CompletedTask;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/SecureOrder.cs#L127-L135' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-roles' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: authorization-policy-multiple -->
<a id='snippet-authorization-policy-multiple'></a>
```cs
[Remote, Update]
[AspAuthorize("RequireAuthenticated")]
[AspAuthorize(Roles = "Manager")]
internal Task Update()
{
    return Task.CompletedTask;
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/SecureOrder.cs#L158-L166' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-policy-multiple' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`[AspAuthorize]` needs `IAspAuthorize`, which `AddNeatooAspNetCore` registers; an operation that carries it always runs its check on the server.

---

## Correlation Context

Track operations across the client/server boundary for distributed tracing:

<!-- snippet: aspnetcore-correlation-id -->
<a id='snippet-aspnetcore-correlation-id'></a>
```cs
[Remote, Execute]
private static async Task<string> _ReportProblem(
    int orderId,
    string description,
    [Service] ICorrelationContext correlation,
    [Service] ISupportLog supportLog)
{
    // The same ID tags the server's log lines for this call
    var reference = correlation.CorrelationId ?? "unknown";
    await supportLog.RecordAsync(new SupportEntry(reference, orderId, description));
    return reference;
}
```
<sup><a href='/src/Design/Design.Domain/Services/SupportRequests.cs#L39-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-correlation-id' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`ICorrelationContext` is automatically registered by `AddNeatooRemoteFactory` and `AddNeatooAspNetCore`. No manual DI registration is required.

---

## Entity Duality

An entity can be an aggregate root in one context and a child in another:

<!-- snippet: orderline-child -->
<a id='snippet-orderline-child'></a>
```cs
/// <summary>
/// Creates a new OrderLine. Public and local: the client calls it.
/// </summary>
[Create]
public void Create(string productName, decimal unitPrice, int quantity)
{
    Id = Guid.NewGuid();
    ProductName = productName;
    UnitPrice = new Money(unitPrice);
    Quantity = quantity;
    IsNew = true;
}

/// <summary>
/// Loads a line from the row its order's load hands it, as a child.
/// </summary>
[Fetch]
internal void Fetch(OrderLineRow row)
{
    LoadFrom(row);
}

/// <summary>
/// Maps a new line into the row the list made for it.
/// </summary>
[Insert]
internal void Insert(OrderLineRow row)
{
    row.Id = Id;
    MapTo(row);
    IsNew = false;
}

/// <summary>
/// Maps an existing line into the row the list found for it.
/// </summary>
[Update]
internal void Update(OrderLineRow row)
{
    MapTo(row);
}
```
<sup><a href='/src/Design/Design.Domain/Entities/OrderLine.cs#L145-L187' title='Snippet source file'>snippet source</a> | <a href='#snippet-orderline-child' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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

**Key insight:** [Remote] is about *how the method is called*, not *what the type is*.

For entity duality, use `internal` on every factory method except the child's `[Create]`, which is `public` and local so client code can build a child and add it to its parent. The aggregate root entry point (`Fetch(Guid id, ...)` above) uses `[Remote] internal` -- `[Remote]` promotes it to `public` on the factory interface so clients can call it, while `internal` enables IL trimming of the method body. The child context method (`Fetch(OrderLineRow row)`) is `internal` without `[Remote]` -- it gets an `IsServerRuntime` guard, becomes trimmable, and appears on the public factory interface with the `internal` modifier (accessible to same-assembly callers only).

---

## Value Objects

A value object that needs nothing to be built is a plain record with no factory. `[Factory]` is for one whose construction needs a service, as `TaxRate` needs the server's tax table:

<!-- snippet: skill-value-object-factory -->
<a id='snippet-skill-value-object-factory'></a>
```cs
[Factory]
public partial record TaxRate(string Region, Percentage Rate)
{
    [Remote, Fetch]
    internal static async Task<TaxRate?> Fetch(string region, [Service] ITaxTable taxTable)
    {
        var rate = await taxTable.FindRateAsync(region);
        return rate is null ? null : new TaxRate(region, rate);
    }
}
```
<sup><a href='/src/Design/Design.Domain/ValueObjects/TaxRate.cs#L33-L46' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-value-object-factory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Note:** A positional record crosses the wire through its primary constructor, so it needs no setters.

---

## Complex Aggregate with Child Collections

<!-- snippet: order-interface -->
<a id='snippet-order-interface'></a>
```cs
public interface IOrder : IFactorySaveMeta
{
    int Id { get; set; }
    string CustomerName { get; set; }
    OrderStatus Status { get; set; }
    IOrderLineList Lines { get; set; }
    Money Total { get; }
    void AddLine(IOrderLine line);
    void RemoveLine(IOrderLine line);
    bool CanSubmit { get; }
    void Submit();
    void Cancel();
    void MarkDeleted();
}
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L37-L52' title='Snippet source file'>snippet source</a> | <a href='#snippet-order-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

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

**Note:** The client builds each line with `IOrderLineFactory.Create` and adds it through the root's `AddLine`. The list holds no factory of its own, so nothing has to survive the round trip.

---

## Child Collection Factory

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

`OrderLineRow` is the example's stored row for one line, held in the order row's line collection. The list holds no factory: it takes `IOrderLineFactory` as a method parameter in each operation, so nothing has to survive the round trip.

---

## Testing with ClientServerContainers

Validate serialization round-trips using the two DI container pattern:

<!-- snippet: aspnetcore-testing -->
<a id='snippet-aspnetcore-testing'></a>
```cs
var (server, client, local) = DesignClientServerContainers.Scopes();
var orderFactory = client.GetRequiredService<IOrderFactory>();

IOrder? order = orderFactory.Create("Test_Customer");   // local: no server call
order = await orderFactory.Save(order);                 // serialized to the server and back
var fetched = await orderFactory.Fetch(order!.Id);      // serialized again
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/ClientServerTestingTests.cs#L27-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-testing' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Container types:**
- `client` - Simulates remote client (serializes through HTTP simulation)
- `server` - Direct server execution (no serialization)
- `local` - Single-tier mode (factory runs locally)

---

## Packages and generated code

`Neatoo.RemoteFactory` is the core library with the source generator embedded; `Neatoo.RemoteFactory.AspNetCore` is the server's ASP.NET Core integration. Both target .NET 9.0 and .NET 10.0. Where the generated files land: [setup.md](setup.md#generated-code).

---

## Assembly-Level Attributes

### [assembly: FactoryHintNameLength]

The generator caps generated file hint names at 50 characters, for the Windows 260-character path limit. A longer fully qualified type name is truncated and raises NF0104, an error, because truncated names can collide. `[assembly: FactoryHintNameLength(100)]`, in any file of the assembly that declares the `[Factory]` types, raises the cap; shortening the namespace or type name is the alternative.

---

## Attributes and Inheritance

A derived class needs its own `[Factory]` to get a factory, whatever its base carries; `[Factory]` on an `abstract` or generic class is ignored; and `[SuppressFactory]` affects only a class or record that also carries `[Factory]`. A derived `[Factory]` class's factory takes from its base classes:
- their operation methods (`[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`, `[Execute]`), with their `[Remote]`, `[Service]` and `[AspAuthorize]`, but not their constructors;
- the nearest base class's `[AuthorizeFactory<T>]`, when the derived class declares none.

The worked example is in [polymorphic-hierarchy.md](polymorphic-hierarchy.md).

---

## Troubleshooting

### "Type not registered" at runtime

The factory was not registered. Check that the assembly is passed to `AddNeatooRemoteFactory` (client) and `AddNeatooAspNetCore` (server), that the class carries `[Factory]` itself (a base class's does not count), and that the method carries an operation attribute. For a `[Service]` parameter, check that the service is registered on the tier where the method runs: a `public` local method resolves it on the client.

### A property is empty after the round trip

On a plain `[Factory]` class the serializer writes only what it can set on the other side. A `private` or `protected` setter is dropped with no diagnostic; make it public ([anti-patterns.md](anti-patterns.md#12-a-private-setter)). A Neatoo partial property serializes through its property manager, so this is not its cause.

### Serialization falls back to the named shape

A class factory without `partial` gets no ordinal serializer and no diagnostic; add `partial` ([anti-patterns.md](anti-patterns.md#13-a-missing-partial)). A static `[Factory]` class without it is NF0101.

### A method-injected service is null

A `[Service]` parameter's service is used inside its method and never crosses the wire; a field holding it is empty on the client. Inject a service the object needs on both tiers through its constructor ([service-injection.md](service-injection.md)).

### A child operation shows up on the client's factory interface

It carries `[Remote]`. A child's persistence operations are `internal` and take no `[Remote]`: the call is already on the server when they run ([anti-patterns.md](anti-patterns.md#5-remote-on-a-childs-operation)).
