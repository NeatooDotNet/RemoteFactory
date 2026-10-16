# Attributes Reference

Every RemoteFactory attribute: what it marks, and what the generator does with it. The code comes from RemoteFactory's compiled example set, `src/Design`.

## Quick Lookup

| Attribute | Target | Purpose |
|-----------|--------|---------|
| `[Factory]` | Class, Interface | Generate a factory for this type |
| `[SuppressFactory]` | Class, Record | Turn generation off for a class marked `[Factory]` |
| `[Create]` | Constructor, Method | Build a new object |
| `[Fetch]` | Method | Load a stored object |
| `[Insert]` | Method | Write a new object |
| `[Update]` | Method | Write a stored object's changes |
| `[Delete]` | Method | Remove a stored object |
| `[Execute]` | Method | A command, or create-or-fetch on a class |
| `[FactoryEventHandler<T>]` | Class | A server-side handler for `FactoryEventBase` events |
| `[Remote]` | Method | A client-to-server entry point |
| `[Service]` | Parameter | Resolve from DI on the tier that runs the method |
| `[AuthorizeFactory<T>]` | Class, Interface | Apply an authorization class |
| `[AuthorizeFactory]` | Method (of the authorization interface) | Map a check to operations |
| `[AspAuthorize]` | Method | ASP.NET Core policies and roles |
| `[assembly: FactoryHintNameLength]` | Assembly | Raise the generated file-name limit |

---

## Factory Discovery

### [Factory]

Marks a class or interface for generation: the generator emits `I{TypeName}Factory` and its implementation. `CustomerPreferences`:

<!-- snippet: attributes-factory -->
<a id='snippet-attributes-factory'></a>
```cs
[Factory]
internal partial class CustomerPreferences : ICustomerPreferences, IFactoryOnStartAsync
```
<sup><a href='/src/Design/Design.Domain/Aggregates/CustomerPreferences.cs#L52-L55' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factory' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The class is `internal` and `partial`, behind its public interface, so the factory is typed on `ICustomerPreferences`.

On an interface, `[Factory]` generates a remote proxy. Every method is a call to the server, and needs no operation attribute. The server registers the implementing class, which carries no `[Factory]`. Design's customer directory:

<!-- snippet: attributes-factory-interface -->
<a id='snippet-attributes-factory-interface'></a>
```cs
[Factory]
[AuthorizeFactory<ICustomerDirectoryAuth>]
public interface ICustomerDirectory
```
<sup><a href='/src/Design/Design.Domain/Services/CustomerDirectory.cs#L261-L265' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factory-interface' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See [Interface Factory](interface-factory.md).

**Only the declaring type.** The generator handles types that carry `[Factory]` on their own declaration. A class derived from a `[Factory]` class gets no factory of its own unless it carries `[Factory]` too. `FactoryAttribute`'s `Inherited = true` does not change that, because the generator reads the declaration, not reflection. `[Factory]` on an `abstract` or generic class is ignored, with no diagnostic. What a derived `[Factory]` class takes from its base is under [Attributes and Inheritance](#attributes-and-inheritance).

### [SuppressFactory]

On a class or record that also carries `[Factory]`, turns generation off for it. The type still compiles, with no factory. It has no effect on an interface, which keeps its factory, and none on a type without `[Factory]`, which gets none anyway, a derived class included. Write it as `[SuppressFactory]`: the generator matches that spelling, so `[SuppressFactoryAttribute]` and namespace-qualified forms are not recognised.

## Operation Attributes

### [Create]

Builds a new object. On a method, the factory calls it on a new instance:

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

On a constructor, the factory calls the constructor. That constructor also runs on every Fetch and every deserialization, so keep it to defaults:

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

A root's Create is local; it is `[Remote]` only when creating needs the server. See [Factory Operations — Create](factory-operations.md#create-operation).

**Auth flags:** `Create | Read`.

### [Fetch]

Loads a stored object into the new instance. Returning `bool`, `false` means not found, and the factory returns `null`:

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

**Auth flags:** `Fetch | Read`.

### [Insert], [Update], [Delete]

The writes, which the factory's `Save` calls according to `IsNew` and `IsDeleted` (`IFactorySaveMeta`). They are not on the factory interface; the caller calls `Save`. `Order`'s Insert:

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

One method can carry more than one of them. `CustomerPreferences` has a single `[Insert, Update]`, saved through `SaveUpsert`:

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

See [Save Operation](save-operation.md).

**Auth flags:** `Insert | Write`, `Update | Write`, `Delete | Write`.

### [Execute]

A request-response command. Every `[Execute]` returns `Task` or `Task<T>` (NF0102), with or without `[Remote]`. It comes in two shapes:
- **On a static `[Factory]` class:** a `private static` method whose name has an underscore prefix. The generator emits a delegate type named without it.
- **On a class factory:** a `public static` method returning `Task` of the class's interface, or of the class itself when there is no matching interface. It becomes a method on the factory interface. `Order.OpenDraft`, create-or-fetch, is one ([Factory Operations — Class Factory Execute](factory-operations.md#class-factory-execute)).

A static command that crosses to the server:

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

Like every operation, `[Execute]` obeys `[Remote]`. Without it the command runs on whichever tier resolves it, with that tier's services, unless its authorization sends it to the server (below):

<!-- snippet: attributes-execute-local -->
<a id='snippet-attributes-execute-local'></a>
```cs
[Execute]
private static Task<int> _ScoreText(string text, [Service] ITextScorer scorer)
{
    return Task.FromResult(scorer.Score(text));
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs#L507-L513' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-execute-local' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Where it runs:**
- **A bare `[Execute]`** gets one unguarded local path in every factory mode, and no remote delegate or endpoint, unless it carries `[AspAuthorize]` or a `[Remote]` authorization method applies to it; either makes it remote, as [Authorization](authorization.md#placement-on-execute) explains. Its `[Service]` parameters come from the container that resolved it, so a bare command that takes a server-only service compiles, then fails on the client at call time with a DI resolution error.
- **`[Remote, Execute]`** gives the client a remote delegate and the server a guarded local path.
- **On a class factory, visibility follows the ordinary rule:**
  - `public static` runs where the factory resolves;
  - `internal static` without `[Remote]` is server-only and guarded, and carried on the factory interface as `internal`;
  - `[Remote] internal static` is promoted to `public` on the interface.

**Trimming:** `[Remote]` decides whether a guard is emitted at all. With `[Remote]`, or `internal` on a class factory, the local path is guarded by `NeatooRuntime.IsServerRuntime`. A client published with the feature switch set to `false` then drops the method body, its `[Service]` dependencies, and their transitive references. Without a guard the body ships to the client and runs there; that is the point. See [IL Trimming](trimming.md).

**NF0105 and static methods.** `[Remote] public` is an error on instance methods (NF0105), because on a class factory visibility is the local/server-only axis. Static methods are exempt. A static-factory `[Execute]` is `private static` behind a generated public wrapper, and a class-level `[Execute]` is `public static` by convention. On those shapes `[Remote]` alone drives the guard.

**Auth flags:** `Execute | Read`.

### [FactoryEventHandler\<T\>]

Marks a static class as a **server-side** handler for events of type `T` (`T : FactoryEventBase`). The generator finds one matching `static` method by its signature and registers it:

<!-- snippet: attributes-factoryeventhandler -->
<a id='snippet-attributes-factoryeventhandler'></a>
```cs
[FactoryEventHandler<OrderPlacedEvent>]
public static partial class OrderNotifyHandlers
{
    internal static async Task SendOrderConfirmation(
        OrderPlacedEvent orderEvent,
        [Service] INotificationService notificationService,
        CancellationToken cancellationToken)
    {
        await notificationService.SendAsync(
            orderEvent.CustomerEmail,
            $"Order {orderEvent.OrderId} confirmed!");
    }
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs#L92-L106' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factoryeventhandler' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

An optional `DispatchPhase` argument says when the handler runs:
- **`Immediate`**, the default: at `Raise`.
- **`AfterFlush`:** queued, and drained by the factory body's `IFactoryEventPhaseCoordinator.DrainAsync` call.
- **`AfterCommit`:** queued, and drained by the framework after the entry call succeeds.

The optional `Coalesce = true` collapses identical queued dispatches (same handler, `Equals`-equal event, same `RaiseOptions`) to one per drain. It is inert where nothing is queued; on `Immediate` it emits NF0505. See [Factory Events](factory-events.md) and [Dispatch Phases](factory-events.md#dispatch-phases).

**Method matching rules:**
- the method is `static`, of any accessibility, and returns `Task`;
- its first parameter that is neither `[Service]` nor `CancellationToken` is of type `T`;
- exactly one method matches (NF0501 if none, NF0502 if several).

**One attribute per event type.** A class stacks `[FactoryEventHandler<T>]` to handle several event types, one matching method each. Repeating the same event type on one class emits NF0504 (warning), and only the first declaration registers.

**Inherited:** No. **Multiple:** Yes.

Handlers run in the caller's DI scope, one at a time and awaited, in no specified order, at the drain point their phase names:
- **An `Immediate` handler** shares the caller's `DbContext` and transaction, and sees staged state. Its exception aborts the chain and reaches the caller.
- **The in-transaction drains** propagate exceptions.
- **The post-completion drain** logs exceptions and swallows them.
- **A failed factory call** discards queued work.

Registrations are wrapped in `NeatooRuntime.IsServerRuntime`, so a trimmed client drops the handler bodies and their services.

> **Instance-method handlers are not supported.** A non-`static` matching method emits **NF0503** (warning) and is skipped at runtime. To receive events on the client, implement [`IFactoryEventRelay`](interfaces-reference.md#ifactoryeventrelay) and register it in DI ([Factory Events — Client-Side Relay](factory-events.md#client-side-relay-consumer-implements-ifactoryeventrelay)).

### [FactoryEvent]

`FactoryEventBase` carries `[FactoryEvent]` with `Inherited = true`, so every event type is discoverable at runtime with no attribute of its own. An event is a record deriving from `FactoryEventBase`:

<!-- snippet: attributes-factoryevent -->
<a id='snippet-attributes-factoryevent'></a>
```cs
public record OrderPlacedEvent(int OrderId, string CustomerEmail) : FactoryEventBase;
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs#L52-L54' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-factoryevent' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Do not apply `[FactoryEvent]` yourself. On a type that does not derive from `FactoryEventBase` it has no effect.

## Execution Control

### [Remote]

Marks a client-to-server entry point. Without `[Remote]`, a method runs locally, `[Execute]` included, with one exception: an operation with an `[AspAuthorize]` check, or a matching `[Remote]` authorization method, goes to the server as if it carried `[Remote]`, because that is where its check runs. Once a call is on the server, the methods it calls need no `[Remote]`. On a class factory, `[Remote]` requires `internal` (NF0105), and the generator promotes the member to `public` on the factory interface. Static methods are exempt, as [[Execute]](#execute) explains.

`Order`'s Create, above, has no `[Remote]` and runs on the caller's tier. Its Fetch and writes carry `[Remote]` and run on the server. See [Client-Server Architecture](client-server-architecture.md).

### [Service]

Marks a parameter that the factory resolves from DI, on the tier that runs the method. It is never serialized. Value parameters come first, then the `[Service]` parameters, then the `CancellationToken`:

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

See [Service Injection](service-injection.md) for method and constructor injection.

## Authorization

### [AuthorizeFactory\<T\>]

Applies an authorization interface to a class or interface factory. `T`'s methods carry `[AuthorizeFactory]`:

<!-- snippet: authorization-apply -->
<a id='snippet-authorization-apply'></a>
```cs
[Factory]
[AuthorizeFactory<IAuthorizedOrderAuth>]
internal partial class AuthorizedOrder : IAuthorizedOrder, IFactorySaveMeta, IFactoryOnCompleteAsync
```
<sup><a href='/src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs#L99-L103' title='Snippet source file'>snippet source</a> | <a href='#snippet-authorization-apply' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### [AuthorizeFactory]

On a method of the authorization interface, maps it to the operations it checks. Flags combine with `|`:

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

See [Authorization](authorization.md) for the flags and what each refusal does.

### [AspAuthorize]

Applies ASP.NET Core authorization to a factory method. Its properties are `Policy`, `Roles` (comma-separated; any one passes) and `AuthenticationSchemes` (comma-separated). Several `[AspAuthorize]` attributes on one method must all pass:

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

**Inherited:** No. **Multiple:** Yes.

## Assembly-Level Attributes

### [assembly: FactoryHintNameLength]

The generator caps the hint names of the files it generates at 50 characters, to stay inside the Windows 260-character path limit. A type whose fully qualified name exceeds the cap gets a truncated name and **NF0104**, an error, because truncated names can collide. `[assembly: FactoryHintNameLength(100)]` raises the cap for the assembly that declares it; put it in any file of that assembly. Shortening the namespace or type name is the alternative.

## Attributes and Inheritance

A class gets a factory from its own `[Factory]` only, but a derived `[Factory]` class takes part of its factory from its base classes:
- **`[Factory]`:** a derived class needs its own. Without it the class has no factory, whatever its base carries.
- **`[SuppressFactory]`:** affects only a class or record declaration that also carries `[Factory]`.
- **Operation methods:** the derived class's factory includes its base classes' operation methods (`[Create]`, `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]`, `[Execute]`), with their `[Remote]`, `[Service]` and `[AspAuthorize]`. Constructors are the exception: only the derived class's own `[Create]` constructors count.
- **`[AuthorizeFactory<T>]`:** a derived class without its own takes the nearest base class's. A class's own replaces its base's.
- **`[AuthorizeFactory]`:** declared on the authorization interface's methods, not on the class.
- **`[FactoryEventHandler<T>]`:** declared on each handler class.

## Next Steps

- [Interfaces Reference](interfaces-reference.md): every RemoteFactory interface
- [Factory Operations](factory-operations.md): the operations in detail
- [Factory Events](factory-events.md): handlers and the client relay
- [Authorization](authorization.md): authorization in detail
