# Fix Event Record Trimming in Blazor WASM Release Builds

**Date:** 2026-04-13
**Related Todo:** [Fix Event Record Trimming in Blazor WASM Release Builds](../todos/fix-event-record-trimming.md)
**Status:** Complete
**Last Updated:** 2026-04-13

---

## Overview

Factory Events (`[FactoryEventHandler<T>]` / `IFactoryEvents.Raise<T>`) were released in v1.0/v1.1 without any trimming-preservation hint for the event record types. In Blazor WASM Release builds (which publish with `PublishTrimmed=true` + the domain assembly marked `IsTrimmable=true`), the IL trimmer strips the event records' primary constructors and public properties, breaking JSON round-trip through `NeatooJsonTypeInfoResolver` + `RecordBypassConverterFactory`. Raise/dispatch fails at runtime with a serialization error.

This plan extends the generator's existing DTO-preservation pattern (`DtoConstructorRegistry.Register<T>` with `[DynamicallyAccessedMembers(All)]`, introduced in v0.27 for factory return types and v0.27 nested-DTO discovery) to also cover event types reached through `[FactoryEventHandler<T>]`.

---

## Skills

- `skills/RemoteFactory/SKILL.md` — the RemoteFactory feature surface (factory patterns, `[FactoryEventHandler<T>]`, IL trimming, `NeatooJsonTypeInfoResolver`, `DtoConstructorRegistry`). The `trimming.md` and `factory-events.md` references under this skill are the primary domain references for this work.

---

## Business Rules (Testable Assertions)

1. WHEN a class is decorated with `[FactoryEventHandler<TEvent>]`, THEN the generator-emitted `FactoryServiceRegistrar` emits exactly one `DtoConstructorRegistry.PreserveType<TEvent>()` call for that event type, regardless of whether `TEvent` has a parameterless constructor — Source: NEW
2. WHEN `TEvent` has public instance properties (including inherited) whose types meet `IsDtoStructureCandidate` (not primitive, not System.*, not abstract/interface, not `[Factory]`-annotated), THEN the generator recursively discovers and emits a registration for each reachable nested type — `DtoConstructorRegistry.Register<N>(() => new N())` when N has a public parameterless ctor, otherwise `DtoConstructorRegistry.PreserveType<N>()` — Source: NEW (extends e630387 nested-DTO discovery to event roots; recursive walk behavior identical to existing factory-return-type walker)
3. WHEN recursive walking encounters a type already visited within a single `FactoryServiceRegistrar`'s emission pass, THEN no second registration for that type is emitted (cycle/duplicate suppression) — Source: existing (v0.27)
4. WHEN a class declares multiple `[FactoryEventHandler<A>]` and `[FactoryEventHandler<B>]` attributes whose recursive walks share a nested type `N`, THEN `N` is emitted exactly once across that class's generated `FactoryServiceRegistrar` — Source: NEW
5. WHEN any caller invokes `IFactoryEvents.Raise<T>(factoryEvent, ...)` with a concrete type `T` in a trimmed assembly, THEN the substituted `T` is preserved by the trimmer because the generic parameter on `Raise<T>` carries `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]`. The same annotation MUST appear on every implementation of `IFactoryEvents.Raise<T>` (`FactoryEventsDispatcher`, `RemoteFactoryEvents`) — the compiler enforces via `IL2091` — Source: NEW
6. WHEN the generator-emitted `FactoryEventHandlerRegistry.RegisterHandler<TEvent>(...)` call is produced for a `[FactoryEventHandler<TEvent>]` whose method is `static`, THEN the substituted `TEvent` is preserved because `RegisterHandler<TEvent>`'s generic parameter carries `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]` — a third preservation layer that complements Rules 1-2 and is free at call-site (generator emits the concrete type on every call) — Source: NEW
7. WHEN the generator emits preservation calls for event types, THEN they are emitted UNCONDITIONALLY (no `if (NeatooRuntime.IsServerRuntime)` guard) because both the client (deserialize incoming server events, serialize outgoing `ServerOnly` raises) and server (serialize outgoing raises to relay clients, deserialize incoming client raises) paths need the type metadata intact. Emission is consistent for both static-method handlers (server-side) and instance-method handlers (client-side relay) — Source: NEW
8. WHEN `[FactoryEventHandler<TEvent>]` decorates a class whose handler method signature does NOT match the method-match rules (NF0501/NF0502 diagnostic case), THEN preservation for `TEvent` (and its reachable nested types) is STILL emitted — preservation gathering happens at the attribute-scan level, not at the handler-match level — Source: NEW
9. WHEN `DtoConstructorRegistry.PreserveType<T>()` is called, THEN it is idempotent and produces no runtime side effect beyond the preservation attribute on T — no dictionary mutation, no allocation, and `DtoConstructorRegistry.TryCreate(typeof(T), out _)` still returns `false` after the call (because `PreserveType` does not populate `Constructors`) — Source: NEW

### Documentation-only requirement (not a testable code rule)

- A consuming project that declares `[FactoryEventHandler<TEvent>]` must have a direct `Neatoo.RemoteFactory` `PackageReference`. This is a Roslyn source-generator constraint documented in `docs/trimming.md#prerequisite-direct-neatooremotefactory-reference-in-every-project-with-factory-types` — already landed. Not enforced by the generator at build time.

### Known Limitation (explicitly out of scope for this plan)

- `Dictionary<TKey, TValue>` (and any two-type-argument generic) property types are not recursively walked by the existing `UnwrapType` in `FactoryGenerator.Types.cs`. An event property `Dictionary<string, SomeRecord>` will NOT preserve `SomeRecord`. This is pre-existing walker behavior; widening it is out of scope for this plan. Will be documented in the release notes as a known gap with a workaround (emit `PreserveType<SomeRecord>` manually, or declare another `[FactoryEventHandler<SomeRecord>]` referencing it).

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Record event with parameterized ctor is preserved | `record OrderPlaced(Guid Id, decimal Total) : FactoryEventBase` with `[FactoryEventHandler<OrderPlaced>]` on a handler class | Rule 1, 7 | Generated `FactoryServiceRegistrar` contains `DtoConstructorRegistry.PreserveType<OrderPlaced>()` outside any `IsServerRuntime` guard |
| 2 | DTO-shaped event with parameterless ctor ALSO uses PreserveType | `class SimpleEvent : FactoryEventBase { public string Name { get; set; } = ""; }` with a handler | Rule 1 | Generated `FactoryServiceRegistrar` contains `DtoConstructorRegistry.PreserveType<SimpleEvent>()` (NOT `Register<>`). Parameterless-ctor events still use `PreserveType` because the event record may be deserialized via `RecordBypassConverterFactory` depending on the `RaiseUntyped` path's runtime type; `PreserveType` covers both cases. |
| 3 | Nested plain-DTO property of an event is registered | `record OrderPlaced(Address ShippingAddress) : FactoryEventBase` where `Address` is a class with a public parameterless ctor | Rule 1, 2 | Generated output contains `PreserveType<OrderPlaced>()` AND `Register<Address>(() => new Address())` |
| 4 | Nested parameterized-record property is preserved | `record OrderPlaced(LineItemDetail First) : FactoryEventBase` where `LineItemDetail` is another record with primary ctor params | Rule 1, 2 | Generated output contains `PreserveType<OrderPlaced>()` AND `PreserveType<LineItemDetail>()` |
| 5 | Collection / array / nullable unwrapping on event properties | `record Batch(IReadOnlyList<LineItem> Items, Coupon? Optional) : FactoryEventBase` | Rule 2 | `LineItem` and `Coupon` are each registered via the appropriate primitive |
| 6 | Cycle suppression | Event A has a property of type Event A (or transitively references itself) | Rule 3 | Exactly one registration for A is emitted per `FactoryServiceRegistrar`; no stack overflow at compile time |
| 7 | Framework/primitive property types are skipped | Event has `string`, `int`, `DateTime`, `Guid`, `decimal` properties | existing `IsDtoStructureCandidate` | Only the event type itself is registered; no attempt to register `System.*` types |
| 8 | `[Factory]`-annotated property types are skipped | Event has a property whose type has `[Factory]` (factories handle their own preservation) | existing `IsDtoStructureCandidate` | That property's type is not registered here (already preserved by its own `FactoryServiceRegistrar`) |
| 9 | `Raise<T>` call site preserves the concrete T | Client code: `factoryEvents.Raise<OrderPlaced>(evt)` in a trimmed assembly with `IsServerRuntime=false` and NO `[FactoryEventHandler<OrderPlaced>]` declared in that project | Rule 5 | `OrderPlaced` survives trimming because `[DynamicallyAccessedMembers(All)]` is on the `T` parameter of `IFactoryEvents.Raise<T>` |
| 10 | End-to-end ClientServerContainers round-trip (Debug) | ClientServerContainers round-trip with an event record that has nested parameterized records | Rules 1, 2, 7 | Handler receives the deserialized event with all property values intact; no reflection/JSON failures. **This proves the new generated code does not break serialization — it does NOT prove trimming works. Trimming is verified manually in Step 6A via `dotnet publish -c Release` on the Person example.** |
| 11 | Idempotent `PreserveType<T>` | `PreserveType<X>()` is called multiple times across multiple `FactoryServiceRegistrar` registrations at app startup | Rule 9 | No allocation, no dictionary growth, no exception; `DtoConstructorRegistry.TryCreate(typeof(X), out _)` still returns `false` |
| 12 | Negation: event with zero reference-type properties | `record MinimalEvent(int Count, string Tag) : FactoryEventBase` (all primitives) with a handler | Rule 1 (negation of Rule 2) | Generated `FactoryServiceRegistrar` contains EXACTLY ONE `PreserveType<MinimalEvent>()` call and NO other `Register<>`/`PreserveType<>` calls sourced from this event |
| 13 | Abstract/interface property type is skipped | `record Event(IAnimal Pet, AbstractBase Base) : FactoryEventBase` where `IAnimal` is an interface and `AbstractBase` is an abstract class | existing `IsDtoStructureCandidate` | Neither `IAnimal` nor `AbstractBase` is registered; only `Event` itself gets a `PreserveType` call |
| 14 | Multi-attribute cross-event dedupe | One handler class has both `[FactoryEventHandler<EventA>]` and `[FactoryEventHandler<EventB>]`, where `EventA` and `EventB` both have a property of type `SharedNestedRecord` | Rule 4 | `SharedNestedRecord` is emitted exactly once in the generated `FactoryServiceRegistrar`; `EventA` and `EventB` each get their own `PreserveType<>` call |
| 15 | Static-method handler AND instance-method handler both preserve | Two handlers for two events — one `static Task Handle(EventStatic)`, one `Task Handle(EventInstance)` — on separate classes | Rule 7 | Both generated `FactoryServiceRegistrar` methods emit `PreserveType<>` calls for their event types OUTSIDE the `if (NeatooRuntime.IsServerRuntime)` block. Static-method class ALSO has the handler registration inside the guard; instance-method class has the relay registration unguarded as today |
| 16 | Known gap: `Dictionary<K,V>` value type not discovered | `record Cache(Dictionary<string, Payload> Items) : FactoryEventBase` where `Payload` is a record | "Known Limitation" section above | `Payload` is NOT registered (current walker only unwraps `IEnumerable<T>` and arrays). This is existing walker behavior; the test exists to document/regression-guard the gap, not to fix it |
| 17 | Missing handler method (NF0501) still preserves event type | Class decorated with `[FactoryEventHandler<TEvent>]` but no method matches the handler signature | Rule 8 | `NF0501` diagnostic is produced AND `PreserveType<TEvent>()` is still emitted in the generated `FactoryServiceRegistrar` |

---

## Approach

Three independent preservation layers, each covering a different path by which an event type can reach JSON (de)serialization:

1. **Registry-based preservation (Primary)** — handles indirect dispatch via `RaiseUntyped`, server-raised events that reach relay clients, and events reached only through their `[FactoryEventHandler<T>]` registration:
   - Add `DtoConstructorRegistry.PreserveType<[DynamicallyAccessedMembers(All)] T>()` — a no-state preservation primitive.
   - The event TYPE ITSELF always uses `PreserveType<T>` — whether or not it has a parameterless ctor. This simplifies the model: the event is potentially deserialized via STJ's parameterized-ctor pipeline (`RecordBypassConverterFactory`) OR via `NeatooJsonTypeInfoResolver.CreateObject`; `[DynamicallyAccessedMembers(All)]` covers both. `Register<T>(() => new T())` is reserved for NESTED plain DTOs (matches existing factory-return-type behavior for those types).
   - Extend `FactoryGenerator.RelayHandler.cs` to perform the recursive DTO walk from `FactoryGenerator.Types.cs::DiscoverDtoTypesRecursive`, rooted at each event type. The walk must happen at the **attribute-scan loop** (before the handler-method match and its `continue` branch for NF0501/NF0502), so events with no valid handler method STILL get preservation.
   - The walker produces two disjoint buckets for NESTED types: parameterless-ctor DTOs (→ `Register<N>(() => new N())`) and parameterized-record types or other non-parameterless-ctor candidates (→ `PreserveType<N>()`).
   - Extend `RelayHandlerModel` / `EventHandlerEntry` to carry two lists. Use `EquatableArray<string>` (NOT `IReadOnlyList<string>`) — `IReadOnlyList<string>` uses reference equality in record-synthesized `Equals`, which is a latent incrementality bug in the existing `ClassFactoryModel.DtoReturnTypes`; do not propagate.
   - Dedupe BOTH lists at the model level across all `[FactoryEventHandler<T>]` attributes on the class.
   - Extend `RelayHandlerRenderer` to emit the calls at the top of `FactoryServiceRegistrar`, unconditionally (no `IsServerRuntime` guard — preservation needed on both client and server).

2. **Call-site preservation on `Raise<T>` (Secondary)** — handles direct `Raise<MyEvent>(...)` calls in assemblies that do NOT declare a matching `[FactoryEventHandler<T>]` (producer-only projects):
   - Annotate `IFactoryEvents.Raise<T>` with `[DynamicallyAccessedMembers(All)]` on `T`. The compiler enforces the same annotation on implementations (`FactoryEventsDispatcher.Raise<T>`, `RemoteFactoryEvents.Raise<T>`) via `IL2091`.
   - Propagates preservation backward from each concrete call site. No-op for callers that already have handler-based preservation (Layer 1).

3. **Call-site preservation on `RegisterHandler<TEvent>` (Supplemental)** — belt-and-suspenders, free:
   - Annotate `FactoryEventHandlerRegistry.RegisterHandler<TEvent>` with `[DynamicallyAccessedMembers(All)]` on `TEvent`.
   - The generator emits `FactoryEventHandlerRegistry.RegisterHandler<{eventTypeName}>(...)` with the concrete event type at every static-method handler (RelayHandlerRenderer.cs:94). Every such emission is now a preservation site too, complementing Layer 1's explicit `PreserveType<>` call.

Together these layers cover every path by which an event record reaches the STJ reflection pipeline.

---

## Domain Model Behavioral Design

N/A — this is a generator/library internals change. No domain model behavioral design applies. The `DtoConstructorRegistry` registry is infrastructure, not a domain model; the `IFactoryEvents` interface is the public surface and is unchanged other than the `T` parameter annotation.

---

## Design

### New preservation primitive

File: `src/RemoteFactory/Internal/DtoConstructorRegistry.cs`

```csharp
/// <summary>
/// Declares a type as preserved-from-trimming without registering a constructor factory.
/// Use for record-shaped DTOs without a public parameterless constructor — deserialization
/// flows through RecordBypassConverterFactory rather than DefaultJsonTypeInfoResolver.CreateObject.
/// The [DynamicallyAccessedMembers(All)] attribute on T instructs the trimmer to preserve
/// every constructor, property, and field on T.
/// </summary>
public static void PreserveType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
{
    // The attribute above is the preservation mechanism. The typeof expression ensures the
    // method body carries a concrete reference to T so the trimmer walks into it.
    _ = typeof(T);
}
```

No dictionary entry is created — `PreserveType<T>` is strictly a trimmer hint. If someone later needs to *instantiate* a type registered only through `PreserveType`, they still go through the normal JSON converter pipeline (`RecordBypassConverterFactory` for parameterized records).

### Generator changes

File: `src/Generator/FactoryGenerator.RelayHandler.cs`

`TransformRelayHandler` currently iterates `symbol.GetAttributes()` looking for `[FactoryEventHandler<T>]` attributes. Inside that loop there is a method-matching phase that emits `NF0501` or `NF0502` and `continue;` when no valid method is found — **this is the ordering bug to avoid**. The new preservation walk MUST run at the top of the attribute iteration, BEFORE the method-match, so that preservation is gathered even when the handler class is diagnostic-broken.

New structure (pseudo):

```csharp
foreach (var attr in symbol.GetAttributes())
{
    // ... existing [FactoryEventHandler<T>] recognition ...
    var eventType = attr.AttributeClass.TypeArguments[0];
    var eventTypeName = ...;

    // NEW: gather preservation BEFORE method-match. Event type itself always
    // goes to the PreserveType bucket. Nested walk follows existing behavior.
    DtoTypeWalker.Walk(eventType, eventTypePreserveBucket: classEventRecords,
                      nestedParameterlessBucket: classEventDtos,
                      nestedParameterizedBucket: classEventRecords,
                      visited: classVisited);

    // ... existing method-match phase with NF0501/NF0502 continue branches ...
}
```

The `DtoTypeWalker.Walk` API splits the root (the event type itself — always preserved via `PreserveType`, regardless of whether it has a parameterless ctor) from the recursive nested walk (which bucket-sorts into parameterless vs parameterized). Buckets are shared `HashSet<string>`s at the class level so dedupe (Rule 4) is natural.

Walker reachability: `DiscoverDtoTypesRecursive` / `IsDtoCandidate` today rejects parameterless-ctor-less types outright. The fix is to split `IsDtoCandidate` into two predicates:
- `IsDtoStructureCandidate(namedType)` — all current checks EXCEPT the parameterless-ctor check.
- `HasParameterlessCtor(namedType)` — just that check.

For the **factory-return path (unchanged)**, the walker composes both predicates — same behavior as today. For the **event-type path (new)**, the walker accepts any `IsDtoStructureCandidate` and bucket-sorts based on `HasParameterlessCtor`.

File: `src/Generator/Model/RelayHandlerModel.cs`

Add two new properties on `RelayHandlerModel` (class-level dedupe, NOT per-entry):

```csharp
/// <summary>FQNs of types reachable from ANY event type that have a public parameterless constructor.
/// Deduplicated across all [FactoryEventHandler&lt;T&gt;] attributes on the class.</summary>
public EquatableArray<string> EventDtoTypes { get; }

/// <summary>FQNs of types reachable from ANY event type that do NOT have a public parameterless constructor
/// (event records, parameterized-ctor records). Deduplicated across all attributes on the class.</summary>
public EquatableArray<string> EventRecordTypes { get; }
```

`EquatableArray<string>` (NOT `IReadOnlyList<string>`) — the existing `ClassFactoryModel.DtoReturnTypes` / `InterfaceFactoryModel.DtoReturnTypes` / `StaticFactoryModel.DtoReturnTypes` / `RelayHandlerModel.Entries` fields use `IReadOnlyList<T>` inside record types, which gives reference equality in the compiler-synthesized `Equals` — a latent incrementality bug (pipeline cache misses that should be hits). Do not propagate. This plan's new fields correct the pattern; if we later do a cleanup pass for the existing fields, it is a separate todo.

File: `src/Generator/Renderer/RelayHandlerRenderer.cs`

In `Render`, after the `FactoryServiceRegistrar` method opener and BEFORE the per-entry `RenderServerSideHandler` / `RenderClientSideRelayHandler` emission, emit preservation calls for the deduplicated class-level lists:

```csharp
// Event-type trimming preservation — unconditional (no IsServerRuntime guard).
// Both client (deserialize incoming server events, serialize outgoing ServerOnly raises)
// and server (serialize outgoing raises to relay clients, deserialize incoming client
// raises) paths need the event type metadata preserved.
foreach (var dtoType in model.EventDtoTypes)
{
    sb.AppendLine($"            DtoConstructorRegistry.Register<{dtoType}>(() => new {dtoType}());");
}
foreach (var recordType in model.EventRecordTypes)
{
    sb.AppendLine($"            DtoConstructorRegistry.PreserveType<{recordType}>();");
}
```

The per-entry handler registrations that follow (lines 92-103 for static, 113-117 for instance) remain unchanged — the `if (NeatooRuntime.IsServerRuntime)` guard on static-method handlers stays (those registrations are server-only), but the preservation calls above are outside that guard.

### Call-site annotation on `IFactoryEvents.Raise<T>`

File: `src/RemoteFactory/IFactoryEvents.cs`

```csharp
Task Raise<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(
    T factoryEvent,
    RaiseOptions options = RaiseOptions.None,
    CancellationToken cancellationToken = default)
    where T : FactoryEventBase;
```

`RaiseUntyped` takes `FactoryEventBase` (not generic) — no annotation needed there.

Implementations that must mirror the annotation (enforced by `IL2091`):
- `FactoryEventsDispatcher.Raise<T>` in `src/RemoteFactory/FactoryEventsDispatcher.cs`
- `RemoteFactoryEvents.Raise<T>` in `src/RemoteFactory/RemoteFactoryEvents.cs`

### Call-site annotation on `FactoryEventHandlerRegistry.RegisterHandler<TEvent>`

File: `src/RemoteFactory/FactoryEventHandlerRegistry.cs`

```csharp
public static void RegisterHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TEvent>(
    Type handlerClassType,
    Func<IServiceProvider, object, RaiseOptions, CancellationToken, Task> handlerFactory)
    where TEvent : FactoryEventBase
```

The generator (RelayHandlerRenderer.cs:94) emits this call with a concrete `TEvent` at every static-method handler site. Annotating `TEvent` gives a third preservation layer at zero cost. No other callers exist; no migration concern.

### Shared walker relocation

`DiscoverDtoTypesRecursive`, `UnwrapType`, and the new split `IsDtoStructureCandidate`/`HasParameterlessCtor` are currently private inside `TypeFactoryMethodInfo` in `FactoryGenerator.Types.cs`. Pulling them into a small internal static helper class `DtoTypeWalker` (new file `src/Generator/DtoTypeWalker.cs`, namespace `Neatoo.RemoteFactory.Generator`) lets the relay-handler transform reuse them without cross-partial-class coupling. Behavior for existing factory-return-type paths is preserved by keeping `IsDtoStructureCandidate && HasParameterlessCtor` in combination — same logic, refactored boundary.

---

## Implementation Steps

1. **Library: new preservation primitive**
   - Add `DtoConstructorRegistry.PreserveType<[DynamicallyAccessedMembers(All)] T>()` in `src/RemoteFactory/Internal/DtoConstructorRegistry.cs`.
   - Add a unit test in `src/Tests/RemoteFactory.UnitTests/` covering Rule 9: multiple calls are idempotent; `DtoConstructorRegistry.TryCreate(typeof(X), out _)` returns `false` after `PreserveType<X>()` (no dictionary entry created).

2. **Library: annotate `IFactoryEvents.Raise<T>` and `FactoryEventHandlerRegistry.RegisterHandler<TEvent>`**
   - Add `[DynamicallyAccessedMembers(All)]` on `T` in `IFactoryEvents.cs::Raise<T>`.
   - Mirror on `FactoryEventsDispatcher.Raise<T>` and `RemoteFactoryEvents.Raise<T>` — compiler enforces via `IL2091`.
   - Add `[DynamicallyAccessedMembers(All)]` on `TEvent` in `FactoryEventHandlerRegistry.RegisterHandler<TEvent>`.
   - Build with `-warnaserror` if possible to catch any unexpected trimming-warning cascade. Scan the existing tests — every `Raise<...>` call uses a concrete type (architect verified), so no migration should be needed.

3. **Generator: extract shared DTO walker**
   - Create `src/Generator/DtoTypeWalker.cs` (namespace `Neatoo.RemoteFactory.Generator`).
   - Move `DiscoverDtoTypesRecursive`, `UnwrapType`, and the existing `IsDtoCandidate` into the new file.
   - Split `IsDtoCandidate` into `IsDtoStructureCandidate` (all current checks EXCEPT parameterless-ctor) + `HasParameterlessCtor` (just that one check). Keep a composed helper for the old semantics if convenient.
   - Expose TWO walker entry points:
     - `WalkFactoryReturn(ITypeSymbol root, HashSet<string> visited) → EquatableArray<string>` — existing semantics: requires `IsDtoStructureCandidate && HasParameterlessCtor` at every node. Used by `TypeFactoryMethodInfo.DiscoverDtoTypes` for factory return types (UNCHANGED behavior — regression-guarded by `NestedDtoDiscoveryTests.cs`).
     - `WalkEventRoot(ITypeSymbol eventRoot, HashSet<string> visited) → (EquatableArray<string> parameterlessCtorTypes, EquatableArray<string> parameterizedTypes)` — new: event root itself goes unconditionally to `parameterizedTypes` (always `PreserveType`); nested props bucket-sort by `HasParameterlessCtor`.
   - Update `TypeFactoryMethodInfo.DiscoverDtoTypes` (both overloads) to delegate to `WalkFactoryReturn`.
   - Run existing tests in `DtoDiscovery/NestedDtoDiscoveryTests.cs` — all 13 Facts must still pass unchanged.

4. **Generator: relay-handler event-type discovery**
   - In `FactoryGenerator.RelayHandler.cs::TransformRelayHandler`, create class-level dedupe sets (`HashSet<string> visited`, plus separate collectors for the two buckets).
   - Inside the `foreach (var attr in symbol.GetAttributes())` loop, at the POINT where `[FactoryEventHandler<T>]` is recognized and `eventType` is extracted (line ~59), invoke `DtoTypeWalker.WalkEventRoot(eventType, visited)` and merge the results into the class-level collectors. **Crucially: this happens BEFORE the method-match `continue` branches (NF0501/NF0502), so events with no valid handler method still get preservation (Rule 8, Scenario 17).**
   - After the attribute loop, produce `EquatableArray<string> EventDtoTypes` and `EquatableArray<string> EventRecordTypes` and pass to `RelayHandlerModel`.

5. **Generator: renderer emission**
   - In `RelayHandlerRenderer.Render`, after the `FactoryServiceRegistrar` method opener (line ~46), emit preservation calls for `model.EventDtoTypes` (as `Register<T>(() => new T())`) and `model.EventRecordTypes` (as `PreserveType<T>()`), outside any `IsServerRuntime` guard. Per-entry `RenderServerSideHandler` / `RenderClientSideRelayHandler` emission follows, unchanged.

6. **Tests: generator output tests**
   - Add a new test file under `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/` following the pattern of `DtoDiscovery/NestedDtoDiscoveryTests.cs`. Name suggestion: `EventTrimming/EventDtoDiscoveryTests.cs`.
   - Cover Scenarios 1-8, 12-17 with snapshot-style assertions on the generated C# string. Examples:
     - Contains `DtoConstructorRegistry.PreserveType<OrderPlaced>()` outside any `if (NeatooRuntime.IsServerRuntime)` block.
     - Contains `DtoConstructorRegistry.Register<Address>(() => new Address())` for a nested parameterless-ctor DTO.
     - For negation (Scenario 12): assert exactly ONE `PreserveType<` occurrence, zero `Register<` occurrences.
     - For multi-attribute dedupe (Scenario 14): assert exactly ONE `PreserveType<SharedNestedRecord>()` occurrence across the full generated output.
     - For NF0501 + preservation (Scenario 17): assert both the diagnostic and the `PreserveType<TEvent>()` emission.

7. **Tests: integration round-trip (regression guard, not trimming proof)**
   - Confirm existing `TestComplexEvent(Guid, string, TestAddress, List<string>)` in `TestTargets/Events/FactoryEventHandlerTargets.cs` still round-trips through `ClientServerContainers.Scopes()` after the change — if its tests still pass, no new integration test is needed. If we want an additional safety net, add one test covering the multi-nested case in `src/Tests/RemoteFactory.IntegrationTests/Events/FactoryEventHandler/`.

8. **Design project: nested-record event example**
   - In `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs`, add a second event record alongside the existing `OrderPlacedEvent`: `OrderShippedEvent(Guid OrderId, ShippingAddress Address) : FactoryEventBase` where `ShippingAddress` is a record with a primary ctor. Add a `[FactoryEventHandler<OrderShippedEvent>]` class (static or instance) so the generator emits `PreserveType<OrderShippedEvent>()` and `PreserveType<ShippingAddress>()` — this makes the preservation pattern visible as a concrete source-of-truth example in the generated output.
   - Add a brief XML doc comment on `[FactoryEventHandler<T>]` in the pattern file noting automatic IL-trimming preservation.
   - Add a Design test asserting the round-trip works (if not already covered).

9. **Build & test**
   - `dotnet build src/Neatoo.RemoteFactory.sln`
   - `dotnet test src/Neatoo.RemoteFactory.sln` — all 548 unit + 578 integration per TFM must pass on net9.0 and net10.0.

10. **Trimming verification (Step 6A, architect)**: `dotnet publish src/Examples/Person/Person.Server/Person.Server.csproj -c Release` and manually verify the client DLL retains event-record type metadata. Use ILSpy or `grep -aob "OrderPlaced" bin/Release/.../publish/*.dll` — the type name (and its ctor parameter names) must appear in the trimmed output. This is the ground-truth verification; the architect performs it in Step 6A. No automated trimming gate in the test suite.

---

## Acceptance Criteria

- [ ] `DtoConstructorRegistry.PreserveType<T>()` exists and carries `[DynamicallyAccessedMembers(All)]` on its generic parameter
- [ ] `IFactoryEvents.Raise<T>` AND `FactoryEventsDispatcher.Raise<T>` AND `RemoteFactoryEvents.Raise<T>` all carry `[DynamicallyAccessedMembers(All)]` on `T`
- [ ] `FactoryEventHandlerRegistry.RegisterHandler<TEvent>` carries `[DynamicallyAccessedMembers(All)]` on `TEvent`
- [ ] Generator emits a `PreserveType<T>()` call for every `[FactoryEventHandler<T>]` event type (regardless of whether `T` has a parameterless ctor) and appropriate `Register<N>` or `PreserveType<N>` calls for every recursively-reachable nested DTO/record
- [ ] Preservation emission is UNCONDITIONAL (no `IsServerRuntime` guard) — verified by snapshot tests for both static-method and instance-method handlers
- [ ] Preservation gathering runs at the attribute-scan level, BEFORE NF0501/NF0502 diagnostic branches — a class with `[FactoryEventHandler<T>]` but no matching method still emits `PreserveType<T>()` (Scenario 17)
- [ ] New model fields use `EquatableArray<string>`, not `IReadOnlyList<string>`
- [ ] Existing tests pass unchanged (548 unit + 578 integration per TFM, net9.0 and net10.0). In particular `NestedDtoDiscoveryTests.cs` all 13 Facts continue to pass (factory-return-path walker behavior is unchanged)
- [ ] New generator tests cover Scenarios 1-8 and 12-17 in `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/EventTrimming/`
- [ ] Trimmed `dotnet publish` on the Person example retains event-type metadata (manual verification; architect confirms in Step 6A)
- [ ] Release notes entry (next patch after 1.1.x) describes the fix AND includes a "Potential `IL2091` callout" migration note for user code that passes generic event types through their own API surface
- [ ] Skill `skills/RemoteFactory/references/factory-events.md` mentions automatic trimming preservation for event types
- [ ] Design project has a second event record with a nested record property demonstrating the preservation pattern

---

## Dependencies

- `src/Generator/FactoryGenerator.Types.cs` — source of the DTO walker to refactor out (factory-return semantics unchanged)
- `src/Generator/DtoTypeWalker.cs` — NEW file; houses the shared walker with two entry points (`WalkFactoryReturn`, `WalkEventRoot`)
- `src/Generator/FactoryGenerator.RelayHandler.cs` — extended to call `WalkEventRoot` at the attribute-scan loop (before NF0501/NF0502 branches)
- `src/Generator/Renderer/RelayHandlerRenderer.cs` — emits preservation calls outside `IsServerRuntime` guard
- `src/Generator/Model/RelayHandlerModel.cs` — carries `EventDtoTypes` and `EventRecordTypes` as `EquatableArray<string>`
- `src/RemoteFactory/Internal/DtoConstructorRegistry.cs` — new `PreserveType<T>` method
- `src/RemoteFactory/IFactoryEvents.cs`, `FactoryEventsDispatcher.cs`, `RemoteFactoryEvents.cs` — `[DynamicallyAccessedMembers(All)]` on `T`
- `src/RemoteFactory/FactoryEventHandlerRegistry.cs` — `[DynamicallyAccessedMembers(All)]` on `TEvent`
- `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/EventTrimming/` — NEW test directory (Scenarios 1-8, 12-17)
- `src/Tests/RemoteFactory.UnitTests/DtoDiscovery/NestedDtoDiscoveryTests.cs` — regression guard for the walker refactor
- `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs` — new `OrderShippedEvent` with nested `ShippingAddress` record
- `docs/release-notes/v{next}.md` — NEW release notes with `IL2091` migration callout

---

## Risks / Considerations

- **Refactoring `DiscoverDtoTypesRecursive`**. Pulling it into a shared walker must be behavior-preserving for the factory-return path. The existing nested-DTO tests (`NestedDtoDiscoveryTests.cs`, 13 Facts, 571 lines) are the regression guard. If any fail, the refactor is wrong — not the test.
- **Parameterized-record factory returns stay unchanged**. Today the DTO walker rejects records without parameterless ctors for factory-return paths. This plan KEEPS that behavior (the walker exposes two entry points — `WalkFactoryReturn` and `WalkEventRoot` — so event-path widening does not leak into factory-return behavior). Widening factory-return is a separate future todo.
- **`FactoryEventBase` is abstract**. `IsDtoStructureCandidate` already excludes abstract types, so the walker never tries to preserve `FactoryEventBase` itself. Concrete events (non-abstract) pass. Verified against existing integration test targets.
- **`[DynamicallyAccessedMembers]` viral warnings (`IL2091`)**. Architect scan of `src/` and `src/Tests` confirmed every `Raise<...>` call uses a concrete type. Real-world user impact in Blazor WASM is expected to be near-zero. Mitigation: release-note callout; if a user does hit `IL2091` in their own generic passthrough, the fix is either to annotate their own `T` with `[DynamicallyAccessedMembers(All)]` or pass a concrete type to `Raise<>`.
- **`PreserveType<T>` naming** — settled. Shortest, sits next to `Register` alphabetically in IntelliSense, matches BCL linker vocabulary. No rename before shipping.
- **Known `Dictionary<K,V>` gap**. The existing `UnwrapType` only unwraps `IEnumerable<T>` and arrays. An event property typed as `Dictionary<string, SomeRecord>` will NOT recursively preserve `SomeRecord`. This is pre-existing walker behavior, not a regression introduced by this plan. Documented as a known limitation in the Business Rules section and covered by regression-guard Scenario 16. Workarounds for users: (a) expose the nested type via another `[FactoryEventHandler<SomeRecord>]` or factory-return path; (b) wait for a future widening of `UnwrapType`.
- **Direct-reference prerequisite** (documentation-only rule). Already documented via pre-todo edits to `docs/trimming.md`, `docs/getting-started.md`, and `skills/RemoteFactory/references/trimming.md`. If the architect finds the note unclear during Step 6A, tighten it then.
- **Incrementality win from `EquatableArray<string>`**. Separate from the bug fix, the generator pipeline becomes incrementally cacheable for the new fields (the existing `IReadOnlyList<string>` fields on other models remain a latent bug but are not touched here — separate future todo).
