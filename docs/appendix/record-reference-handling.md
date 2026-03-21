# Appendix: Record Reference Handling and Value Object Semantics

This appendix explains **why** records bypass reference tracking (`$id`/`$ref`) during serialization and **how** RemoteFactory's three-path serialization architecture handles reference identity for different type categories. For user-facing serialization documentation, see [Serialization](../serialization.md). For serialization internals (type resolution, DI integration), see [Serialization Internals](serialization.md).

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

```csharp
// The Dictionary is part of the value object's state definition
record ProductSpec(string Name, Dictionary<string, string> Attributes);

// The same Dictionary instance assigned to both an entity property and a record:
var attrs = new Dictionary<string, string> { ["color"] = "red" };
entity.Metadata = attrs;           // Entity property -- tracked with $id/$ref
entity.Spec = new ProductSpec("Widget", attrs);  // Value object -- independent copy

// After round-trip:
// entity.Metadata participates in reference tracking (shared identity preserved)
// entity.Spec.Attributes is an independent copy (value object semantics)
// ReferenceEquals(entity.Metadata, entity.Spec.Attributes) is false -- correct
```

## Three-Path Serialization Architecture

RemoteFactory routes types through three serialization paths based on which converter claims the type. All three paths share the same `NeatooReferenceResolver` instance (per-operation, via `AsyncLocal`), so reference IDs are unique across the entire graph.

### Path 1: Neatoo Custom Converters (Entities and Lists)

Types with Neatoo custom converters (`NeatooBaseJsonTypeConverter`, `NeatooListBaseJsonTypeConverter`) are serialized by Neatoo's converters. These converters access `NeatooReferenceResolver.Current` directly to emit `$id`/`$ref` metadata under their own control.

- **Types:** Neatoo entities (`IEditBase`, `IValidateBase`, etc.) and Neatoo lists (`IEditListBase`, etc.)
- **Reference tracking:** Yes -- converter-level, using `NeatooReferenceResolver.Current` directly
- **Unchanged from v0.22.0**

### Path 2: STJ Built-in Converters with ReferenceHandler (Mutable Types)

Types without a custom converter and with a default (parameterless) constructor are handled by STJ's built-in converters. `NeatooPreserveReferenceHandler` is set on `JsonSerializerOptions`, which tells STJ to call `CreateResolver()` and use the returned resolver for `$id`/`$ref` tracking.

`NeatooPreserveReferenceHandler.CreateResolver()` returns `NeatooReferenceResolver.Current` -- the same resolver instance that Neatoo's custom converters use. This means STJ's built-in converters and Neatoo's custom converters share a single ID space.

- **Types:** `Dictionary<K,V>`, `List<T>`, plain classes with default constructors, other mutable reference types
- **Reference tracking:** Yes -- serializer-level, via `ReferenceHandler` on options delegating to `NeatooReferenceResolver.Current`
- **Handles:** Shared instances (same object in two properties), circular references (A.Next = B, B.Next = A)

### Path 3: Record Bypass Converter (Value Objects)

Types with parameterized constructors (no public parameterless constructor) are claimed by `RecordBypassConverterFactory`. This converter delegates serialization to an inner `JsonSerializerOptions` that is identical to the outer options except `ReferenceHandler` is `null` and `RecordBypassConverterFactory` is removed (to prevent recursion).

The record and its entire subtree serialize without `$id`/`$ref` metadata.

- **Types:** C# records with primary constructors, classes with only parameterized constructors
- **Reference tracking:** No -- records are value objects; identity is defined by values, not reference
- **Prevents:** STJ's `ObjectWithParameterizedCtorRefMetadataNotSupported` exception

### Converter Priority

STJ checks converters in registration order. The order in `NeatooJsonSerializer` is:

1. `NeatooOrdinalConverterFactory` (if Ordinal format) -- claims `IOrdinalSerializable` types
2. Neatoo custom converter factories -- claim Neatoo entities and lists
3. `RecordBypassConverterFactory` -- claims types with parameterized constructors
4. STJ built-in converters -- handle everything else (with `ReferenceHandler` active)

A type claimed by an earlier converter is never seen by later ones. Neatoo types are always handled by Neatoo converters (Path 1), regardless of whether they have parameterized constructors. `RecordBypassConverterFactory` only sees non-Neatoo types.

### Reference Flow

```
NeatooJsonSerializer.Serialize()
    |
    +-- Creates NeatooReferenceResolver, sets .Current via AsyncLocal
    |
    +-- JsonSerializer.Serialize(target, OuterOptions)
            |
            +-- Neatoo entity/list property:
            |       Path 1: Neatoo converter reads .Current directly
            |       Emits $id/$ref under converter control
            |
            +-- Record property:
            |       Path 3: RecordBypassConverterFactory claims it
            |       Delegates to InnerOptions (ReferenceHandler = null)
            |       No $id/$ref in the record's subtree
            |
            +-- Dictionary/List/plain class property:
            |       Path 2: STJ built-in converter
            |       ReferenceHandler calls CreateResolver() -> .Current
            |       STJ emits $id/$ref using the shared resolver
            |
            +-- All three paths share the same resolver and ID counter
    |
    +-- Clears .Current in finally block
```

## Detection Rule: Parameterized Constructors

`RecordBypassConverterFactory` uses a simple detection rule: claim any type that has no public parameterless constructor but has at least one public constructor with parameters. This matches STJ's own heuristic for parameterized-constructor deserialization.

This rule is intentionally broad. It claims all C# records with primary constructors, but also non-record classes with only parameterized constructors. In the RemoteFactory ecosystem, such non-record classes are rare. The simpler rule avoids fragile heuristics that attempt to distinguish `record class Foo(int X)` from `class Foo(int X)` -- STJ cannot distinguish them either, and both have the same parameterized-constructor limitation.

Types with **both** a parameterless and a parameterized constructor are NOT claimed. STJ uses the parameterless constructor for those types, so they work with `ReferenceHandler` and participate in `$id`/`$ref` tracking.

## What This Means in Practice

| Type Category | Reference Tracked? | Example |
|---|---|---|
| Neatoo entity | Yes (Path 1) | `Employee`, `IEditBase` implementations |
| Neatoo list | Yes (Path 1) | `EmployeeList`, `IEditListBase` implementations |
| Dictionary, List, plain class | Yes (Path 2) | `Dictionary<string, string>`, `List<int>`, `class Holder { ... }` |
| C# record | No (Path 3) | `record Address(string Street, string City)` |
| Class with only parameterized ctor | No (Path 3) | `class Foo { public Foo(int x) { } }` |
| Primitives, enums, strings | N/A | Not reference types; no tracking needed |

**For most domain models, this works transparently.** Entities get reference tracking. Value objects (records) get correct value-based serialization. Collections and dictionaries shared across properties preserve identity.

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
