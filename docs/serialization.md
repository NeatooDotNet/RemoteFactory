# Serialization

Every `[Remote]` call carries its arguments to the server and its result back, and an operation on an instance (a Save) carries the object itself. RemoteFactory serializes them with its own System.Text.Json setup, which adds three things to the defaults:

- **A compact format for `[Factory]` types.** The generator writes each `[Factory]` class as a JSON array of property values: the Ordinal format.
- **Interface-typed properties.** A property typed as an interface is written with its concrete type, so the other side knows what to build.
- **Constructor injection on the way in.** A type whose constructor takes services is built through DI when it is read, so it arrives with that tier's services.

Everything else (primitives, enums, collections, records) is plain System.Text.Json. What crosses at all: primitives, enums (as numbers), collections, records (a value object needs no `[Factory]`), plain classes, and `[Factory]` types.

## Serialization Formats

| Format | A `[Factory]` object is written as | Tradeoff |
|--------|-----------------------------------|----------|
| **Ordinal** (default) | `["Customer_42", 7, false, false, {...}, ...]` | Compact; positions, not names |
| **Named** | `{"CustomerName": "Customer_42", "Id": 7, ...}` | Readable in a network trace; larger |

Both tiers must register the same format. The server announces its format in an `X-Neatoo-Format` response header, but the client does not read it: a Named server and an Ordinal client fail every call.

To use Named, pass `new NeatooSerializationOptions { Format = SerializationFormat.Named }` to both registrations: `AddNeatooAspNetCore` on the server and `AddNeatooRemoteFactory` on the client. With no options, both are Ordinal. If you switch by environment, read the .NET 10 caveat in [ASP.NET Core Integration — Serialization Format](aspnetcore-integration.md#serialization-format): a WebAssembly client's environment is fixed when it is built.

### What the Ordinal Array Holds

The positions are the type's `PropertyNames`, which the generator emits:

- **The type's own properties first, then its base class's.** Each group is in ordinal (`StringComparer.Ordinal`) name order, not declaration order.
- **Only properties the other side can set.** A property needs a public getter and a public, `internal` or `init` setter. A get-only property, and one with a `private` setter, is not written at all: the other side keeps whatever its constructor set.

`Order`'s properties:

<!-- snippet: serialization-ordinal-versioning -->
<a id='snippet-serialization-ordinal-versioning'></a>
```cs
public int Id { get; set; }
public string CustomerName { get; set; } = string.Empty;
public OrderStatus Status { get; set; } = OrderStatus.Draft;
public IOrderLineList Lines { get; set; } = null!;
public Money Total => Lines?.CalculateTotal() ?? Money.Zero;
```
<sup><a href='/src/Design/Design.Domain/Aggregates/Order.cs#L94-L100' title='Snippet source file'>snippet source</a> | <a href='#snippet-serialization-ordinal-versioning' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

Its array is `CustomerName, Id, IsDeleted, IsNew, Lines, Status`. `IsNew` and `IsDeleted` are declared further down the class. `Total` has no setter, so it is not written; the other side computes it.

**Versioning.** Adding or renaming a property shifts the positions after it, so a client and a server built from different versions of the domain assembly misread each other. Deploy them together. Named identifies each value by name, so it tolerates the difference better.

**When a type is written in the Named shape anyway.** The generator emits the ordinal form only for a class that meets all of these:

- it is `partial`, not nested, and not a collection;
- it has at least one property it can write;
- it can be built without arguments: no explicit constructor, a parameterless one, or one whose parameters all have defaults (a record needs its primary constructor).

Any other `[Factory]` type is written by name even when the format is Ordinal.

## Constructor Injection Across the Wire

The ordinal path rebuilds an object with `new` from the array, so no DI is involved. A type whose constructor takes services fails the "built without arguments" rule above. It therefore always takes the named path, which resolves the instance from that tier's container when the type is registered there, so it arrives with its constructor-injected services. Design's `CtorInjectionExample` shows the pattern, and `SerializationTests` pins it.

## Interface Properties

System.Text.Json cannot deserialize a property typed as an interface: it doesn't know what to build. RemoteFactory writes such a property with its concrete type's full name:

```json
"Lines": { "$type": "Design.Domain.Entities.OrderLineList", "$value": [ ... ] }
```

In the Ordinal format, the same object sits at the property's position in the array. This applies to a **non-generic** interface (or abstract type) declared in an assembly registered with RemoteFactory, such as `Order.Lines`, which is `IOrderLineList` (shown above). A generic interface such as `IList<T>` or `IEnumerable<T>` is not covered: type the property as a concrete collection, or as your own non-generic interface.

## Reference Identity

Whether two references to the same object arrive as one object depends on the format:

| Graph | Named | Ordinal (default) |
|-------|-------|-------------------|
| A shared reference between `[Factory]` objects (`team.Lead` is `team.Members[0]`) | One object | **Two copies** |
| A back reference between `[Factory]` objects (`member.Team` is `team`) | One object | **Fails:** the serializer throws "a possible object cycle was detected". On a fetch it fails on the server, so over HTTP the call is a 500 |
| Records | Copied (no `$id`/`$ref`) | Copied |

Plain classes with a public parameterless constructor, and collections (`Dictionary`, `List<T>`), keep shared and circular references (`NeatooPreserveReferenceHandler`); the tests cover them serialized on their own. A plain class without a public parameterless constructor is handled like a record: no reference tracking.

The Ordinal rows are a known bug, [#133](https://github.com/NeatooDotNet/RemoteFactory/issues/133): the generated ordinal converter writes no `$id`/`$ref`. Until it is fixed, keep `[Factory]` aggregates free of back references and shared instances, or use the Named format. A child that needs its parent can be handed the parent's id, or receive the parent as a method argument on the server. `FactoryReferenceIdentityTests` in `RemoteFactory.IntegrationTests` pins both formats, with the Ordinal cases marked as known failures.

Records are value objects, so a duplicate is the same value: they bypass reference tracking. A mutable object nested inside a record's constructor parameters is written as an independent copy. See [Appendix: Record Reference Handling](appendix/record-reference-handling.md).

## Debugging

Each request's serialize and deserialize steps are logged at `Trace` under `RemoteFactory.Trace`. That category is not under `Neatoo.RemoteFactory`, so a filter on that prefix does not turn it on. Design.Server's filters keep it at `Warning`:

<!-- snippet: aspnetcore-logging -->
<a id='snippet-aspnetcore-logging'></a>
```cs
services.AddLogging(logging => logging
    .AddFilter("Neatoo.RemoteFactory", LogLevel.Information)
    .AddFilter("RemoteFactory.Trace", LogLevel.Warning)   // Trace: every request's (de)serialization
    .AddFilter("Design", LogLevel.Information));          // the generated class factories
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L132-L137' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-logging' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

To read the payloads themselves, switch both tiers to Named and use the browser's network tools.

## Next Steps

- [Client-Server Architecture](client-server-architecture.md): how serialization fits in the remote call
- [Factory Modes](factory-modes.md): when a call is serialized (a `[Remote]` operation called from a Remote-mode client)
- [Service Injection](service-injection.md): constructor injection that survives serialization
