---
name: RemoteFactory
description: |
  Use when building or reviewing a RemoteFactory domain library, or when asking the questions a mainstream .NET habit asks of it - how do I load-or-create, can I [Create] from loaded data, how does the client call the repository, where does my application service go, who writes a child's row, does Save() update my object, which services can a constructor take, should a child's [Insert] be [Remote], does [Remote] mean "runs on the server", my command needs the server so plain [Execute], should this screen get a DTO from a controller. Also triggers on "RemoteFactory", "Neatoo.RemoteFactory", [Factory], [Create], [Fetch], [Insert], [Update], [Delete], [Execute], [Remote], [Service], IFactorySaveMeta, "Save routing", [AuthorizeFactory], [AspAuthorize], [FactoryEventHandler<T>], FactoryEventBase, IFactoryEvents, IFactoryEventRelay, RaiseOptions, ServerOnly, DispatchPhase, AfterFlush, AfterCommit, IFactoryEventPhaseCoordinator, Coalesce, "domain events", UseNeatoo, AddNeatooAspNetCore, AddNeatooRemoteFactory, "IL trimming", PublishTrimmed, NeatooRuntime, IsServerRuntime. RemoteFactory works with any class: a Neatoo entity, a read model, a context, a static command, a record. Neatoo's editing session (rules, change tracking, the UI contract) is the Neatoo skill's, loaded beside this one.
version: 2.0.0
---

# RemoteFactory

RemoteFactory is a Roslyn source generator for 3-tier .NET applications. When the library that holds the domain objects is loaded on both tiers, the client/server seam needs no DTOs and no controllers: RemoteFactory generates the factories and their serialization at compile time, and one endpoint carries every call. It works with any class that carries `[Factory]` - a Neatoo entity, a plain class, a static command, a record.

Every C# block in this skill and its references is compiled and tested in RemoteFactory's `src/Design` projects, on real client and server containers. The only hand-written code is a short block labelled WRONG.

## The model

1. **One domain assembly, loaded on both tiers.** The browser and the server run the same compiled classes. A domain object can exist on either side, and its code runs wherever the object is. Each tier has its own container. The tiers differ in which services are registered, not in which classes exist. This is RemoteFactory's premise: when the library holding the object definitions is on both tiers, the seam needs no DTOs and no controllers.

2. **The factory operation is the seam.** An object is created, loaded, sent across the wire and written only through a factory operation. That operation is where services are injected, authorization is checked, the transaction lives and events are raised. Code outside an operation holds no server service. Nothing with `[Factory]` is constructed with `new`.

3. **The operation attribute is a claim about persistence.**
   - `[Create]`: this object is not in the store yet. Its first save inserts. It takes what a constructor would take and is local unless creation needs the server.
   - `[Fetch]`: this object reflects what the store holds, and is null when the store holds nothing. It takes a key.
   - `[Insert]`, `[Update]`, `[Delete]`: write this object's own row. They are never called by name and are not on the generated interface; `Save` routes by `IFactorySaveMeta` state.
   - `[Execute]`: anything else. Decide, orchestrate, compute. An operation that might return a new object or an existing one is an `[Execute]` that calls Create or Fetch inside it. `[Execute]` obeys `[Remote]`.

   The claim is what matters, not what the body does. A `[Create]` may read the database for defaults and is still a Create. Building a loaded child with `Create` is wrong even when every value is right, because the child then claims to be new and the next save inserts it again.

4. **`[Remote]` marks where a call crosses, not where code lives.** It goes on the operations the client calls: a root's operations, a command that needs the server, a child list's `[Fetch]` when the client loads it on demand. A `[Service]` parameter is resolved on the tier where its method runs, so a server-only service belongs only on a method that runs on the server: one that is `[Remote]` or `internal`. A child's operations are `internal` and never `[Remote]`, because the call is already on the server when they run; `[Remote]` there is not an N+1 problem, it is a placement and trimming error. A child's `[Create]` is the exception: it is `public` and local, so the client can build a child before adding it.

5. **Every object writes itself, and only itself.** The root's operation opens the transaction and commits. Each child's own `[Insert]`, `[Update]` or `[Delete]` maps and writes its own row, reached through the child factory's `Save`. A parent never maps a child's row. An operation touches only its own aggregate's rows; another aggregate's fact comes through that aggregate's domain service. A command that mutates an aggregate fetches it through its factory, calls a domain method, and saves through the root factory; it does not write the row.

## The questions the habit asks

A mainstream .NET developer reaches for these under load. Each row is the question, RemoteFactory's answer, and the compiled example to copy instead.

| The habit asks | RemoteFactory's answer | Copy this |
|---|---|---|
| How do I load-or-create? | An `[Execute]` that calls Fetch, then Create, inside it: a class-level static `[Remote, Execute]` returning the aggregate. Every open, start and ensure seam is one. Never a `[Create]` that looks for an existing row, never a `[Fetch]` that creates one. | `OpenDraft` and `FetchDraft` [below](#create-or-fetch) |
| Can I `[Create]` from loaded data? | No. `[Create]` is `new`: not in the store, first save inserts. `[Fetch]` is what the store holds. A loaded child is built by its own `[Fetch]`, handed its row by the list. | `OrderLine`'s `Create` and `Fetch(row)` [below](#one-class-a-child-and-a-root) |
| How does the client call the repository? | It doesn't. A repository is a `[Service]` parameter on a server operation. What the client calls is an interface factory: a domain service named for what it does (`IProductCatalog`), never a repository. | `Order`'s Fetch in [class-factory.md](references/class-factory.md); `IProductCatalog` in [interface-factory.md](references/interface-factory.md) |
| Where does my application service go? | A static `[Factory]` command when the call returns and is done. A class-level static `[Execute]` when the result is that aggregate. A context - a plain `[Factory]` instance holding the aggregate - when a screen binds to the result and later verbs act on it; its verbs are plain instance methods calling the aggregate's verbs. There is no instance `[Execute]`. | `OrderCommands` in [static-factory.md](references/static-factory.md); `OpenDraft` and the checkout context [below](#which-factory-shape) |
| Who writes a child's row? | The child. The root's operation makes its own row and hands the row's child collection to the list factory's `Save`; the list makes, finds or removes each child's row and hands each child its own; the child's `[Insert]` or `[Update]` maps itself into it. The root flushes once. | `OrderLine`'s `Insert` and `Update` [below](#one-class-a-child-and-a-root); `Order` and `OrderLineList` in [class-factory.md](references/class-factory.md) |
| `Save()` updates my object? | It returns the saved instance; reassign it. Called from the client, `Save` returns a new instance from the server, and the one you passed still reports its old state. On the server it may write the same object; reassign either way. | The context's `SaveAsync` [below](#which-factory-shape); the page in [setup.md](references/setup.md) |
| Which services can a constructor take? | Only services registered on both tiers: the object is built through DI on whichever side deserializes it. A server-only service is a `[Service]` method parameter, resolved on the tier where the method runs - the server, for a `[Remote]` or `internal` method. | The checkout context's constructor [below](#which-factory-shape); [service-injection.md](references/service-injection.md) |
| Should the child's `[Insert]` be `[Remote]`? | No; `internal`. The call is already on the server. `[Remote]` there costs no extra round trip; it is a placement and trimming error. A type that is both root and child carries `[Remote]` root operations and `internal` child operations side by side. | `OrderLine` [below](#one-class-a-child-and-a-root) |
| Does `[Remote]` mean "runs on the server"? | No. It marks where the client's call crosses. A method without it runs on the tier that calls it; once on the server, a `[Remote]` method called there runs locally, with no second round trip. | The checkout context's `Fetch` [below](#which-factory-shape), which calls the order's `[Remote]` Fetch on the server |
| My command needs the server, so plain `[Execute]`? | `[Execute]` obeys `[Remote]`. Bare, it runs on whichever tier resolves it, with that tier's services: a client-side engine. With `[Remote]`, the client crosses. `internal` without `[Remote]` is server-only. | The three placements [below](#execute-obeys-remote); [static-factory.md](references/static-factory.md) |
| I'll return a DTO from a controller for this screen? | No DTO class and no controller: the one assembly on both tiers is the point. A screen's server truth is a read model - a plain `[Factory]` class with `[Fetch]` only, which the client holds - or, for a call that returns and is done, an interface-factory method returning a record. | `OrderSummary` [below](#read-model) |

**Neatoo's questions.** The Neatoo skill, loaded beside this one, answers these; they are listed here so the two skills read as one list. Where do I validate before Save? Where do I throw when input is bad? Nobody edits this, so plain EF? Where do the screen's derived values and visibility go? How do I set a value in Fetch without marking it modified? How do I get computed values after Fetch - `RunRules(All)` at the end? The parent saves its children's rows? A rule can't call the server? Value object, so `ValidateBase`? Can I call `Save()` on a child? Its model is the half this skill assumes: what an operation sets is baseline; Neatoo is the editing session; logic is placed by who is driving it; the UI binds and adapts gestures and decides nothing. Its "Neatoo, or plain RemoteFactory" decision says when a type needs Neatoo's base classes.

## The decisions

### Which factory shape

| The type | Shape | Example |
|---|---|---|
| An aggregate a client creates, loads, edits and saves | Class factory: `[Factory]` on the class, `[Remote]` on the operations the client calls, `IFactorySaveMeta` for `Save` routing | `Order` in [class-factory.md](references/class-factory.md) |
| A service the client calls and is done with | Interface factory: `[Factory]` on the interface, no attributes on its methods, a plain implementation on the server. A domain service, never a repository | `IProductCatalog` in [interface-factory.md](references/interface-factory.md) |
| A command | Static factory: `[Factory] static partial class` with `[Remote, Execute] private static _Name` | `OrderCommands` in [static-factory.md](references/static-factory.md) |
| A screen that binds to an aggregate and acts on it | Context: a plain `[Factory]` instance holding the aggregate | `OrderCheckout` below |
| A screen that displays server truth | Read model: a plain `[Factory]` class with `[Fetch]` only | `OrderSummary` [below](#read-model) |
| A value | A `record`. It crosses the wire with no factory; it takes `[Factory]` only when building it needs a service or authorization | `TaxRate` in [advanced-patterns.md](references/advanced-patterns.md) |

The context loads its order in one round trip, works on it on the client, and saves through the order's root factory. Its factories arrive through the constructor, so they exist on whichever tier holds it:

<!-- snippet: skill-context-checkout -->
<a id='snippet-skill-context-checkout'></a>
```cs
[Factory]
internal partial class OrderCheckout : IOrderCheckout
{
    private readonly IOrderFactory _orderFactory;
    private readonly IOrderLineFactory _lineFactory;

    public OrderCheckout(IOrderFactory orderFactory, IOrderLineFactory lineFactory)
    {
        _orderFactory = orderFactory;
        _lineFactory = lineFactory;
    }

    public IOrder Order { get; set; } = null!;

    /// <summary>
    /// Loads the context and its order in one call.
    /// </summary>
    /// <remarks>
    /// Runs on the server: the order's own Fetch runs there too, as a local
    /// call, so the screen's whole state arrives in one round trip. A missing
    /// order is a false Fetch, and the factory returns null.
    /// </remarks>
    [Remote, Fetch]
    internal async Task<bool> Fetch(int orderId)
    {
        var order = await _orderFactory.Fetch(orderId);
        if (order == null)
            return false;

        Order = order;
        return true;
    }

    /// <summary>
    /// Adds the product the screen holds as a line on the order. No server call.
    /// </summary>
    public void AddProduct(ProductListing product, int quantity)
    {
        Order.AddLine(_lineFactory.Create(product.Name, product.UnitPrice, quantity));
    }

    /// <summary>
    /// Submits the order. No server call; the change is saved by SaveAsync.
    /// </summary>
    public void Submit()
    {
        Order.Submit();
    }

    /// <summary>
    /// Saves the order through its root factory and holds the saved instance.
    /// </summary>
    /// <remarks>
    /// Save returns a new instance from the server; the context reassigns it.
    /// </remarks>
    public async Task SaveAsync()
    {
        Order = await _orderFactory.Save(Order)
            ?? throw new InvalidOperationException("Saving the order returned nothing.");
    }
}
```
<sup><a href='/src/Design/Design.Domain/Contexts/OrderCheckout.cs#L63-L125' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-context-checkout' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Create or fetch

`OpenDraft` is the seam: the client calls it, it crosses, and on the server it asks for the existing draft and creates one only when there is none. `FetchDraft` is `internal` without `[Remote]`, so only the server can call it:

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

### One class, a child and a root

`OrderLine` is a child of `Order` and, for a line-detail screen, a root. Its child operations take the row its list hands it and are `internal`; its `Create` is `public` so the client builds a line before adding it; its root `Fetch` takes a key and is `[Remote]`. The signature is the difference:

<!-- snippet: skill-orderline-root-and-child -->
<a id='snippet-skill-orderline-root-and-child'></a>
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
<sup><a href='/src/Design/Design.Domain/Entities/OrderLine.cs#L144-L204' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-orderline-root-and-child' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### `[Execute]` obeys `[Remote]`

Three placements on one class, differing in the attribute and the visibility alone:

<!-- snippet: skill-execute-remote -->
<a id='snippet-skill-execute-remote'></a>
```cs
[Remote, Execute]
public static async Task<ClassExecuteDemo> RunCommand(
    string input, [Service] IExampleService service, [Service] IClassExecuteDemoFactory factory)
{
    return factory.Create(service.GenerateId(), $"Executed: {input}");
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/ClassFactoryWithExecute.cs#L119-L126' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-execute-remote' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-execute-local -->
<a id='snippet-skill-execute-local'></a>
```cs
[Execute]
public static Task<ClassExecuteDemo> ScoreLocally(
    string input, [Service] ITextScorer scorer, [Service] IClassExecuteDemoFactory factory)
{
    return Task.FromResult(factory.Create(scorer.Score(input), $"Scored: {input}"));
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/ClassFactoryWithExecute.cs#L154-L161' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-execute-local' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-execute-internal -->
<a id='snippet-skill-execute-internal'></a>
```cs
[Execute]
internal static Task<ClassExecuteDemo> ArchiveOnServer(
    string input, [Service] IExampleService service, [Service] IClassExecuteDemoFactory factory)
{
    return Task.FromResult(factory.Create(service.GenerateId(), $"Archived: {input}"));
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/ClassFactoryWithExecute.cs#L191-L198' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-execute-internal' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A static command follows the same rule; [static-factory.md](references/static-factory.md) has both.

### Read model

The client holds it and a screen binds to it; nothing creates or saves it. Its lines are plain records carried as a property:

<!-- snippet: skill-readmodel -->
<a id='snippet-skill-readmodel'></a>
```cs
[Factory]
internal partial class OrderSummary : IOrderSummary
{
    public int OrderId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<OrderSummaryLine> Lines { get; set; } = [];

    /// <summary>
    /// Loads the summary on the server from the order's row.
    /// </summary>
    /// <remarks>
    /// A read model may read the store directly: it writes nothing, so no
    /// aggregate's rules are bypassed. It reads the same IOrderDbContext the
    /// Order aggregate writes.
    ///
    /// The CancellationToken is not serialized and not a [Service]. Over HTTP it is
    /// the request's: it fires when the client cancels or disconnects (a screen
    /// navigating away while the summary loads), or when the server shuts down.
    /// Passing it to the store is what lets the store stop work nobody will read.
    /// GENERATOR BEHAVIOR: the factory's Fetch takes an optional CancellationToken
    /// either way; declaring one here is what hands it to this method.
    /// </remarks>
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
}
```
<sup><a href='/src/Design/Design.Domain/ReadModels/OrderSummary.cs#L63-L110' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-readmodel' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Visibility decides the guard and the trimming

| Declared | On the factory interface | Where it runs |
|---|---|---|
| `[Remote] internal` | promoted to `public` | crosses to the server |
| `internal`, no `[Remote]` | `internal` | server only: throws on a client whose `IsServerRuntime` switch is `false`, and its body is trimmed there |
| `public`, no `[Remote]` | `public` | the tier that calls it, unguarded |
| `[Remote] public` | - | compile error NF0105 |

A static method has no promotion to do: on a static factory or a class-level static `[Execute]`, `[Remote]` alone decides whether the client crosses. IL trimming removes the guarded bodies from the published client: [trimming.md](references/trimming.md).

## Mechanism quick reference

| Question | Answer |
|---|---|
| Should interface-factory methods have attributes? | No. Every method of an interface factory is a remote entry point; an operation attribute on one is NF0106 |
| Which `[AuthorizeFactory<T>]` scopes apply on an interface factory? | `Execute` and `Read`. Per-method checks match on parameters; a refusal throws `NotAuthorizedException` on the client. [interface-factory.md](references/interface-factory.md) |
| Can `[Execute]` return `void`? | No (NF0102). `Task` when there is no result; `Task<T>` when the caller needs one. A class-level `[Execute]` returns `Task<T>` of the class's own service type |
| Do properties need public setters? | Yes. The serializer writes only what it can set on the other side |
| Do I need `partial`? | Yes, on every `[Factory]` class |
| How do I defer loading a child collection? | A `[Remote, Fetch]` on the child list's factory, called when the data is needed |
| How do I handle a factory event on the server? | A `[FactoryEventHandler<T>]` class with a `static` matching method; it runs in the caller's scope. [factory-events.md](references/factory-events.md) |
| When does a handler run? | Its `DispatchPhase`: `Immediate` (at `Raise`), `AfterFlush` (when the operation drains `IFactoryEventPhaseCoordinator`), `AfterCommit` (after the entry call succeeds; exceptions swallowed). `Coalesce = true` collapses identical queued dispatches |
| How does the client hear about an event? | Register an `IFactoryEventRelay`; it gets each round trip's batch. `RaiseOptions.ServerOnly` keeps an event off it |
| Where must an operation's event be raised? | Inside the operation, through a `[Service] IFactoryEvents` |
| Fire-and-forget external IO from an operation? | Not a factory event: `Task.Run` with a new scope from `IServiceScopeFactory`, snapshotting any ambient context first |
| Can several types share a generic base? | Yes: `[Factory]` on each concrete type, with a CRTP constraint on the base. [polymorphic-hierarchy.md](references/polymorphic-hierarchy.md) |
| How do I keep server code out of the Blazor bundle? | IL trimming with the `IsServerRuntime` switch set to `false`. [trimming.md](references/trimming.md) |
| How do I wire the server and the client? | `AddNeatooAspNetCore` and `UseNeatoo()` on the server; `AddNeatooRemoteFactory` in Remote mode and a keyed `HttpClient` on the client. [setup.md](references/setup.md) |
| Where does business logic go? | In the entity, not the factory. The Neatoo skill places it |

## Known pains

Open defects and missing diagnostics a new library meets, each with its issue or the analyzer it waits for.

- A `[Factory]` class (not a static one) without `partial` still gets a factory but silently serializes in the named shape - analyzer pending.
- A property with a `private` or `protected` setter is silently left out of serialization - analyzer pending.
- A class-level `[Execute]` that does not return `Task<T>` of the containing type fails inside the generated code - analyzer pending.
- A `[Service]` parameter before a value parameter breaks a static `[Execute]` in generated code, with no diagnostic - [#135](https://github.com/NeatooDotNet/RemoteFactory/issues/135). Put value parameters first.
- On an interface factory with `[AuthorizeFactory<T>]`, an auth method whose parameter some interface methods lack fails CS7036 in generated code - [#123](https://github.com/NeatooDotNet/RemoteFactory/issues/123). Keep every authorized method on the parameter the auth method takes.
- On an authorized class factory, `Save` reports two things that are not refusals as `NotAuthorizedException`: a `bool` `[Insert]`, `[Update]` or `[Delete]` that returns `false`, and an object that is both new and deleted - [#104](https://github.com/NeatooDotNet/RemoteFactory/issues/104). Do not use a `bool` write's `false` as a result on an authorized factory.
- A generated factory cannot be declared `internal` when its class has operations the client calls - [#165](https://github.com/NeatooDotNet/RemoteFactory/issues/165).
- `[Factory]` declares `Inherited = true`, but a derived class gets no factory from its base's attribute - [#141](https://github.com/NeatooDotNet/RemoteFactory/issues/141). Put `[Factory]` on each concrete type.
- `RegisterMatchingName` registers concrete types DI may not be able to build, which fails `ValidateOnBuild` - [#131](https://github.com/NeatooDotNet/RemoteFactory/issues/131).
- Generated files rely on the consuming project's implicit usings - [#97](https://github.com/NeatooDotNet/RemoteFactory/issues/97). Keep `ImplicitUsings` enabled.
- `LazyLoad<T>` and `ILazyLoadFactory` are being removed - [#114](https://github.com/NeatooDotNet/RemoteFactory/issues/114). Build nothing on them; defer a child collection with a `[Remote, Fetch]` on its list factory.

## References

Each file is a mechanism reference under the decisions above.

- [class-factory.md](references/class-factory.md) - the aggregate root: operations, `IFactorySaveMeta` and `Save` routing, lifecycle hooks, class-level `[Execute]`, child visibility
- [interface-factory.md](references/interface-factory.md) - the domain service the client calls: the contract, the server implementation, registration, `[AuthorizeFactory<T>]` on an interface
- [static-factory.md](references/static-factory.md) - commands: `[Remote, Execute]`, the local `[Execute]`, the generated delegates
- [service-injection.md](references/service-injection.md) - constructor injection on both tiers, `[Service]` parameters where the method runs, child entities
- [setup.md](references/setup.md) - server and client configuration, the Blazor page
- [factory-events.md](references/factory-events.md) - handlers, dispatch phases, coalescing, the client relay, `RaiseOptions`
- [advanced-patterns.md](references/advanced-patterns.md) - authorization, correlation, entity duality, value objects, child collections, testing on client and server containers
- [polymorphic-hierarchy.md](references/polymorphic-hierarchy.md) - several concrete types over one generic base
- [trimming.md](references/trimming.md) - IL trimming for the Blazor client
- [anti-patterns.md](references/anti-patterns.md) - the mistakes, each labelled WRONG, with the fix

## Editing this skill

Every C# block is a MarkdownSnippets embed of a `#region` in RemoteFactory's `src/Design`, whose behaviour a Design test pins. To change one, change the region and its test, then run `mdsnippets`. The only hand-written C# is a short block labelled WRONG that shows a forbidden pattern; anything else that cannot be an embed is prose.
