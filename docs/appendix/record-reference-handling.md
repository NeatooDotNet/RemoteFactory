# Appendix: Record Reference Handling and Value Object Semantics

This appendix explains **why** records bypass reference tracking (`$id`/`$ref`) during serialization and **how** RemoteFactory's four serialization paths handle reference identity for different type categories. For user-facing serialization documentation, see [Serialization](../serialization.md). For serialization internals (type resolution, DI integration), see [Serialization Internals](serialization.md).

## The Problem: Reference Tracking vs. Parameterized Constructors

System.Text.Json's `ReferenceHandler.Preserve` adds `$id`/`$ref` metadata to track shared object identity across a serialization graph. This works for mutable types (classes with default constructors, dictionaries, lists), but **fails for types with parameterized constructors**:

```
System.NotSupportedException:
  Deserialization of reference type 'MyRecord' with parameterized constructor
  is not supported. (ObjectWithParameterizedCtorRefMetadataNotSupported)
```

STJ deserializes parameterized-constructor types by reading the entire JSON payload, then passing all values to the constructor at once. A `$ref` pointer requires the referenced object to already exist -- but it cannot exist yet because construction has not completed. Microsoft has closed this as NOT_PLANNED ([dotnet/runtime#73302](https://github.com/dotnet/runtime/issues/73302)).

This creates a tension: RemoteFactory needs `ReferenceHandler` for shared-instance identity on mutable types, but cannot have it for records.

## The DDD Resolution: Value Objects Have No Identity to Track

The resolution is not a workaround -- it follows directly from DDD value object semantics.

**Records are value objects.** A value object is defined by its values, not by its reference identity. Two `Address("123 Main", "Springfield")` instances with the same state are interchangeable. There is no meaningful distinction between "the same Address object" and "two Address objects with identical state."

**Reference tracking is semantically wrong for value objects.** Tracking identity with `$id`/`$ref` implies the object's reference matters -- that the consumer needs to know "this is the same instance I saw earlier." For entities, that is true. For value objects, it is not. Serializing a record twice (one copy per property) produces the correct result: two independent value objects with identical state.

**Nested reference types within a record are part of its state.** A `Dictionary<string, string>` inside a record's constructor parameter is part of the value object's definition. If the same `Dictionary` instance is also referenced from an entity property elsewhere in the graph, the entity's reference participates in `$id`/`$ref` tracking (it has identity), while the record's copy is logically independent (it is state). `ReferenceEquals` between them returns `false` after round-trip. This is correct.

For example, take a record `ProductSpec(string Name, Dictionary<string, string> Attributes)`, and one dictionary assigned to both an entity's `Metadata` property and a `ProductSpec`'s `Attributes`. After the round trip, `Metadata` keeps its tracked identity across the graph, while `Spec.Attributes` is an independent copy with the same entries. `ReferenceEquals(entity.Metadata, entity.Spec.Attributes)` is `false`, which is correct.

## Serialization Paths

RemoteFactory routes each type through one of four paths, chosen by the converter that claims it. The paths that write `$id`/`$ref` share one `NeatooReferenceResolver` per operation (via `AsyncLocal`), so reference IDs are unique across the entire graph.

### Path 1: Generated Ordinal Converters (Ordinal Format)

In the Ordinal format, the default, `NeatooOrdinalConverterFactory` is registered first. It claims each `[Factory]` class that has a generated ordinal converter (a `partial`, non-nested class whose constructors take no services) and writes the object as a JSON array of its property values.

- **Types:** `[Factory]` classes with a generated ordinal converter, in the Ordinal format only
- **Reference tracking:** No. The array carries no `$id`/`$ref`, so an object two properties share arrives as two copies. That is a known bug, [#133](https://github.com/NeatooDotNet/RemoteFactory/issues/133); see [Serialization](../serialization.md)

### Path 2: `NeatooJsonConverterFactory` Converters (Interfaces and Framework Types)

`NeatooJsonConverterFactory` is RemoteFactory's extension point: every implementation registered in DI is added to the serializer ahead of the remaining paths. RemoteFactory registers one, `NeatooInterfaceJsonConverterFactory`, which claims interface- and abstract-typed values and writes each as `$type` plus `$value` ([Serialization Internals](serialization.md#how-interface-serialization-works)). The value inside `$value` then takes its own concrete type's path.

A framework built on RemoteFactory registers its own. Neatoo, for example, registers converters for its entity and list base types (`NeatooBaseJsonTypeConverter`, `NeatooListBaseJsonTypeConverter`), which read `NeatooReferenceResolver.Current` directly to write `$id`/`$ref` under their own control. Those types are Neatoo's, not RemoteFactory's.

- **Types:** interface- and abstract-typed values; a framework's own types
- **Reference tracking:** as the converter does it

### Path 3: STJ Built-in Converters with ReferenceHandler (Mutable Types)

Types no earlier converter claims, with a default (parameterless) constructor, are handled by STJ's built-in converters. `NeatooPreserveReferenceHandler` is set on `JsonSerializerOptions`, which tells STJ to call `CreateResolver()` and use the returned resolver for `$id`/`$ref` tracking.

`NeatooPreserveReferenceHandler.CreateResolver()` returns `NeatooReferenceResolver.Current`, the same resolver a Path 2 converter can read. STJ's built-in converters and a framework's converters therefore share a single ID space.

- **Types:** `Dictionary<K,V>`, `List<T>`, plain classes with default constructors, `[Factory]` classes with a parameterless constructor in the Named format, other mutable reference types
- **Reference tracking:** Yes -- serializer-level, via `ReferenceHandler` on options delegating to `NeatooReferenceResolver.Current`
- **Handles:** Shared instances (same object in two properties), circular references (A.Next = B, B.Next = A)

### Path 4: Record Bypass Converter (Value Objects)

Types with parameterized constructors (no public parameterless constructor) are claimed by `RecordBypassConverterFactory`. This converter delegates serialization to an inner `JsonSerializerOptions` that is identical to the outer options except `ReferenceHandler` is `null` and `RecordBypassConverterFactory` is removed (to prevent recursion).

The record and its entire subtree serialize without `$id`/`$ref` metadata.

- **Types:** C# records with primary constructors, classes with only parameterized constructors
- **Reference tracking:** No -- records are value objects; identity is defined by values, not reference
- **Prevents:** STJ's `ObjectWithParameterizedCtorRefMetadataNotSupported` exception

### Converter Priority

STJ checks converters in registration order. `NeatooJsonSerializer` registers the converters of these paths in this order:

1. `NeatooOrdinalConverterFactory` (Ordinal format only) -- claims `[Factory]` classes with a generated ordinal converter
2. `NeatooJsonConverterFactory` implementations -- RemoteFactory's interface converter, and any a framework registers
3. `RecordBypassConverterFactory` -- claims types with parameterized constructors
4. STJ built-in converters -- handle everything else (with `ReferenceHandler` active)

A type claimed by an earlier converter is never seen by later ones. A framework's types are handled by its own converters, regardless of whether they have parameterized constructors. `RecordBypassConverterFactory` only sees what the earlier converters left.

### Reference Flow

```
NeatooJsonSerializer.Serialize()
    |
    +-- Creates NeatooReferenceResolver, sets .Current via AsyncLocal
    |
    +-- JsonSerializer.Serialize(target, OuterOptions)
            |
            +-- [Factory] class with an ordinal converter (Ordinal format):
            |       Path 1: written as an array, no $id/$ref (#133)
            |
            +-- Interface- or abstract-typed property:
            |       Path 2: $type + $value; the value takes its own path
            |
            +-- Record property:
            |       Path 4: RecordBypassConverterFactory claims it
            |       Delegates to InnerOptions (ReferenceHandler = null)
            |       No $id/$ref in the record's subtree
            |
            +-- Dictionary/List/plain class property:
            |       Path 3: STJ built-in converter
            |       ReferenceHandler calls CreateResolver() -> .Current
            |       STJ emits $id/$ref using the shared resolver
    |
    +-- Clears .Current in finally block
```

## Detection Rule: Parameterized Constructors

`RecordBypassConverterFactory` uses a simple detection rule: claim any type that has no public parameterless constructor but has at least one public constructor with parameters. This matches STJ's own heuristic for parameterized-constructor deserialization.

This rule is intentionally broad. It claims all C# records with primary constructors, but also non-record classes with only parameterized constructors, a `[Factory]` class whose constructor takes services among them. The simpler rule avoids fragile heuristics that attempt to distinguish `record class Foo(int X)` from `class Foo(int X)` -- STJ cannot distinguish them either, and both have the same parameterized-constructor limitation.

Types with **both** a parameterless and a parameterized constructor are NOT claimed. STJ uses the parameterless constructor for those types, so they work with `ReferenceHandler` and participate in `$id`/`$ref` tracking.

## What This Means in Practice

| Type Category | Reference Tracked? | Example |
|---|---|---|
| `[Factory]` class with an ordinal converter, Ordinal format | No (Path 1, #133) | Design's `Order` |
| `[Factory]` class with a parameterless constructor, Named format | Yes (Path 3) | Design's `Order` in the Named format |
| Interface-typed value | As its concrete type's path | `IOrderLineList Lines` |
| A framework's own entity or list | As its converter does (Path 2) | Neatoo's entity and list base types |
| Dictionary, List, plain class | Yes (Path 3) | `Dictionary<string, string>`, `List<int>`, `class Holder { ... }` |
| C# record | No (Path 4) | `record Address(string Street, string City)` |
| Class with only parameterized ctor | No (Path 4) | `class Foo { public Foo(int x) { } }`, a `[Factory]` class whose constructor takes services |
| Primitives, enums, strings | N/A | Not reference types; no tracking needed |

**What to expect.** In the Named format, `[Factory]` classes with a parameterless constructor get reference tracking. In the Ordinal format, the default, their generated converter does not track references yet (#133). Value objects (records) get value-based serialization in both. Collections and dictionaries shared across properties preserve identity.

## The STJ Limitation in Detail

System.Text.Json's `ReferenceHandler.Preserve` adds `$id` to the first occurrence of every reference type and `$ref` to subsequent occurrences. On deserialization, STJ resolves `$ref` pointers to previously-deserialized objects.

For types with parameterized constructors, STJ must read all properties from the JSON before calling the constructor. But `$id`/`$ref` metadata must be processed during reading -- and the object does not exist until after construction. From [Microsoft's documentation](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/preserve-references#immutable-types-and-records):

> This feature can't be used to preserve value types or immutable types. On deserialization, the instance of an immutable type is created after the entire payload is read. So it would be impossible to deserialize the same instance if a reference to it appears within the JSON payload.

This is not a bug to be fixed -- it is a fundamental constraint of how parameterized-constructor deserialization works. The `$id` token must be processed when the object is first encountered, but the object cannot be constructed until all its properties are read. STJ throws `NotSupportedException` rather than silently producing incorrect results.

`RecordBypassConverterFactory` prevents this exception by removing `ReferenceHandler` from the options used for record serialization. Since records are value objects (no identity to track), this produces the correct result.

## Related Documentation

- [Serialization](../serialization.md) -- User-facing serialization documentation
- [Serialization Internals](serialization.md) -- Type resolution pipeline and DI integration
- [Client-Server Architecture](../client-server-architecture.md) -- How serialization fits in the remote call lifecycle
