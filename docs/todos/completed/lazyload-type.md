# Add LazyLoad<T> Type to RemoteFactory

**Status:** Complete
**Priority:** High
**Created:** 2026-03-28
**Last Updated:** 2026-03-28

---

## Problem

`LazyLoad<T>` currently lives in Neatoo, tightly coupled to Neatoo's PropertyManager and meta-state interfaces (`IValidateMetaProperties`, `IEntityMetaProperties`). The core wrapper pattern — deferred async loading with explicit `LoadAsync()`, serialization of Value + IsLoaded — is general-purpose and belongs in RemoteFactory so any RemoteFactory consumer can use it, not just Neatoo.

## Solution

Extract a minimal `LazyLoad<T>` into RemoteFactory's core library:

- **Core wrapper:** `Value`, `IsLoaded`, `IsLoading`, `HasLoadError`, `LoadError`, `LoadAsync()`, `SetValue()`, `INotifyPropertyChanged`
- **`ILazyLoadFactory`** for DI creation (with loader delegate, with pre-loaded value)
- **`ILazyLoadDeserializable`** internal interface for converter to merge deserialized state into constructor-created instances (preserves loader delegates across serialization)
- **Serializer support:** Custom `JsonConverter` that serializes Value + IsLoaded, drops the loader delegate
- **Ordinal serialization:** Generator recognizes `LazyLoad<T>` properties — two-slot encoding `[value, isLoaded]` in ordinal arrays
- **Constructor-initialization pattern:** Loader created in constructor survives serialization via constructor reconstruction + `ILazyLoadDeserializable` merge
- **NO** `IValidateMetaProperties`, **NO** `IEntityMetaProperties` — Neatoo extends for those

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-28
**Verdict:** APPROVED

### Relevant Requirements Found

1. **Serialization unsupported types list** (`src/Design/Design.Tests/FactoryTests/SerializationTests.cs:36`) — The comment "Lazy<T> or other deferred types" is listed under "NO - These will NOT serialize correctly." `LazyLoad<T>` is a new RemoteFactory-native deferred type with purpose-built serialization support, so this entry must be updated. The plan correctly identifies this in step 11.

2. **Public setter requirement** (CLAUDE-DESIGN.md Critical Rule 5, Anti-Pattern 4) — Properties need public setters for serialization. The `LazyLoad<T>` API specifies `Value` as get-only (`T? Value { get; }`) and `IsLoaded` as get-only. This is intentional — `LazyLoad<T>` is NOT a `[Factory]` class and does not participate in standard property-based ordinal serialization. The plan handles this via `ILazyLoadDeserializable.ApplyDeserializedState()` and custom converter/generator logic. No violation.

3. **Anti-Pattern 5: Storing method-injected services/delegates in fields** (CLAUDE-DESIGN.md) — The loader delegate (`Func<Task<T?>>`) is intentionally NOT serialized, consistent with the rule that delegates/service references are lost after serialization. The plan's merge pattern reconstructs the loader from the constructor on deserialization. This aligns with the existing constructor injection principle: constructor-created state survives serialization.

4. **Ordinal serialization contract** (`src/Generator/Renderer/OrdinalRenderer.cs`, `src/Generator/FactoryGenerator.Types.cs`) — The current ordinal system assigns one array slot per property. `LazyLoad<T>` properties need two slots (Value + IsLoaded). This is a fundamental extension to the ordinal encoding contract. The generator's `CollectOrdinalProperties` method (FactoryGenerator.Types.cs:342-408) collects properties with public getters AND setters, sorted alphabetically. `LazyLoad<T>` properties have getters but their Value/IsLoaded are not settable via standard property assignment — the generator must special-case these properties.

5. **Converter ordering in NeatooJsonSerializer** (`src/RemoteFactory/Internal/NeatooJsonSerializer.cs:76-93`) — The converter chain is: (1) NeatooOrdinalConverterFactory, (2) NeatooJsonConverterFactory instances from DI, (3) RecordBypassConverterFactory. `LazyLoad<T>` will NOT be claimed by any existing converter: it does not implement `IOrdinalSerializable`, is not an interface/abstract type, and has a public parameterless constructor (so RecordBypassConverterFactory skips it). A new `LazyLoadJsonConverterFactory` must be inserted into this chain.

6. **RecordBypassConverterFactory interaction** (`src/RemoteFactory/Internal/RecordBypassConverterFactory.cs:36-57`) — `LazyLoad<T>` has a public parameterless constructor, so `RecordBypassConverterFactory.CanConvert()` returns `false`. No conflict. However, the inner type `T` may be a record — the plan correctly notes that the inner value serialization delegates to `JsonSerializer.Serialize/Deserialize(ref reader, typeof(T), options)`, which will correctly route records through `RecordBypassConverterFactory`.

7. **NeatooInterfaceJsonConverterFactory interaction** (`src/RemoteFactory/Internal/NeatooInterfaceJsonConverterFactory.cs:19-26`) — This factory claims non-generic interface/abstract types. If `T` in `LazyLoad<T>` is an interface type (e.g., `LazyLoad<IOrderLineList>`), the inner value serialization will correctly use the `NeatooInterfaceJsonConverterFactory` because it delegates via `JsonSerializer.Serialize/Deserialize` with the full options chain. The plan's risk note about "Inner type serialization" and "converter ordering" is relevant and well-identified.

8. **IL trimming constraints** (docs/trimming.md, CLAUDE-DESIGN.md "Trimming-Safe Factory Registration") — The plan's `LazyLoadJsonConverterFactory` will likely use `MakeGenericType` to create generic converters. The plan correctly identifies this risk and says to follow `RecordBypassConverterFactory` patterns. Both `RecordBypassConverterFactory` and `NeatooOrdinalConverterFactory` already use `MakeGenericType` with `Activator.CreateInstance`, establishing the precedent.

9. **Design Debt table** (CLAUDE-DESIGN.md:734-743) — `LazyLoad<T>` is NOT in the Design Debt table. No deferred-feature conflict.

10. **Multi-targeting requirement** (CLAUDE.md) — The plan must work across net9.0 and net10.0. The plan does not mention multi-targeting explicitly but uses only standard .NET APIs. No concern.

11. **`partial` keyword requirement** (CLAUDE-DESIGN.md Critical Rule, Anti-Pattern 6) — `LazyLoad<T>` itself is NOT a `[Factory]` class and does not need `partial`. Classes that USE `LazyLoad<T>` as a property must already be `[Factory] partial` — no change to existing requirements.

12. **`[Factory]` class property collection** (`src/Generator/FactoryGenerator.Types.cs:370-408`) — The property collector filters for properties with public getters AND setters (`propertySymbol.SetMethod != null`). A `LazyLoad<T>` property on a `[Factory]` class (e.g., `public LazyLoad<OrderLineList> Lines { get; set; }`) has a public setter on the `LazyLoad<T>` property itself, so the collector WILL pick it up as an ordinal property of type `LazyLoad<T>`. The two-slot encoding then needs to be applied during rendering.

### Gaps

1. **No existing pattern for multi-slot ordinal encoding** — The current ordinal system is strictly one-property-one-slot. The plan proposes two-slot encoding for `LazyLoad<T>` properties. This is a new pattern with no existing precedent in the codebase. The architect must define clear rules for when multi-slot encoding is used and how PropertyNames/PropertyTypes arrays represent these synthetic slots.

2. **No existing pattern for custom type-aware property rendering in OrdinalRenderer** — `OrdinalRenderer.cs` currently treats all properties uniformly. `LazyLoad<T>` requires type-specific rendering (read Value + IsLoaded from two slots, reconstruct `LazyLoad<T>` from them). The architect must decide how to extend the rendering model — add a flag to `OrdinalPropertyModel` (e.g., `IsLazyLoad`, `InnerType`) or add a separate model type.

3. **No documentation of LazyLoad<T> as a supported property type** — The serialization guide (`docs/serialization.md`) and the serialization round-trip guide in `SerializationTests.cs` do not mention `LazyLoad<T>`. Both need updating to document this as a supported type with its two-format serialization behavior.

4. **No existing pattern for converter-level merge on deserialization** — The `ILazyLoadDeserializable.ApplyDeserializedState()` merge pattern (constructor creates instance with loader, converter merges deserialized Value + IsLoaded) is novel in RemoteFactory. Existing converters create new instances; they don't merge into existing ones. The ordinal-generated `FromOrdinalArray` creates new instances, but the named-format converter needs to merge. The architect should clarify how the merge pattern works for both formats.

5. **No `ILazyLoadFactory` in Design project DI registration** — The plan says to register `ILazyLoadFactory` in `AddRemoteFactoryServices()`, but the Design projects (`Design.Server/Program.cs`, `Design.Client.Blazor/Program.cs`) may need to demonstrate this registration or confirm it's automatic.

### Contradictions

None found. The proposal does not violate any documented pattern, anti-pattern, critical rule, or design debt decision.

The `SerializationTests.cs:36` comment listing "Lazy<T> or other deferred types" as unsupported is not a contradiction — it is a documentation artifact that reflects the current state. `LazyLoad<T>` is a purpose-built RemoteFactory type with custom serialization support, unlike BCL `Lazy<T>` which remains unsupported. The plan explicitly addresses updating this comment.

### Recommendations for Architect

1. **Converter placement** — Define exactly where `LazyLoadJsonConverterFactory` goes in the converter chain. It should go BEFORE `RecordBypassConverterFactory` (last) but the question is whether it should be a `NeatooJsonConverterFactory` subclass (DI-resolved, like `NeatooInterfaceJsonConverterFactory`) or a standalone factory added directly to `Options.Converters` (like `NeatooOrdinalConverterFactory` and `RecordBypassConverterFactory`). Since `LazyLoadJsonConverterFactory` does not need DI services (it creates instances via constructors, not service resolution), a standalone factory added in `NeatooJsonSerializer`'s constructor is simpler.

2. **OrdinalPropertyModel extension** — The `OrdinalPropertyModel` currently has `Name`, `Type`, and `IsNullable`. For `LazyLoad<T>` support, consider adding `IsLazyLoad` and `InnerType` fields so `OrdinalRenderer` can emit the two-slot read/write pattern. Alternatively, expand `LazyLoad<T>` properties into two `OrdinalPropertyModel` entries during property collection — but this would require special reconstruction logic.

3. **Named format merge vs. ordinal format reconstruction** — The plan describes two different deserialization approaches: the named-format converter uses `ILazyLoadDeserializable.ApplyDeserializedState()` for merge, while the ordinal-format generated code uses `new LazyLoad<T>(value)` or `new LazyLoad<T>()` for reconstruction. Clarify whether the merge pattern is needed for ordinal format too, or if reconstruction is sufficient (since ordinal deserialization creates new instances anyway).

4. **Design project examples** — Follow the existing Design project pattern: add a new file (e.g., `Design.Domain/Aggregates/` or `Design.Domain/FactoryPatterns/`) demonstrating `LazyLoad<T>` on a `[Factory]` class with constructor-initialization. Add tests to `Design.Tests/FactoryTests/` showing both local and client/server round-trip. Update the Design Completeness Checklist in CLAUDE-DESIGN.md.

5. **Update SerializationTests.cs comment** — Move `LazyLoad<T>` from the "NO" list to the "YES" list. Keep `Lazy<T>` (BCL) in the "NO" list. Distinguish between the two clearly.

6. **Trimming annotations** — Apply `[DynamicallyAccessedMembers]` or `[UnconditionalSuppressMessage]` to `LazyLoadJsonConverterFactory.CreateConverter()` if it uses `MakeGenericType`, following the pattern in `RecordBypassConverterFactory` and `NeatooOrdinalConverterFactory`.

---

## Plans

- [LazyLoad Type Implementation Plan](../../plans/completed/lazyload-type-plan.md)

---

## Tasks

- [x] Requirements review (Step 2) — APPROVED, no contradictions
- [x] Architect review (Step 3) — APPROVED, 26 business rules, 22 test scenarios
- [x] Developer review (Step 4) — APPROVED after clarification (record+LazyLoad out of scope)
- [x] Implementation (Step 6) — 3 phases complete
- [x] Verification (Step 7) — VERIFIED (architect) + REQUIREMENTS SATISFIED (reviewer)
- [x] Documentation (Step 8) — Requirements docs + published docs updated

---

## Progress Log

### 2026-03-28
- Created todo from design discussion with user
- Explored Neatoo's `LazyLoad<T>` implementation in neatoodotnet/Neatoo repo
- Explored RemoteFactory's serialization system (ordinal + named formats, converter factories)
- Key design decisions confirmed:
  - Ordinal format: two-slot encoding `[value, isLoaded]`; Named format: `{"value": ..., "isLoaded": true}`
  - Unloaded state serializes as `[null, false]`, deserializes as `new LazyLoad<T>()` (no loader)
  - `ILazyLoadDeserializable` lives in RemoteFactory (not Neatoo) for converter merge pattern
  - Generator changes included (not deferred) — full ordinal support for `LazyLoad<T>` properties
  - Meta-state interfaces (`IValidateMetaProperties`, `IEntityMetaProperties`) stay in Neatoo only

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors (net9.0 + net10.0)
- Tests: 517 unit + 506 integration + 48 design — all passing, 0 failures

---

## Results / Conclusions

Successfully ported `LazyLoad<T>` from Neatoo into RemoteFactory as a minimal, general-purpose deferred async loading wrapper.

### What was built:
- **Core type:** `LazyLoad<T>` with passive `Value`, explicit `LoadAsync()`, thread-safe loading, INPC forwarding, error tracking
- **Factory:** `ILazyLoadFactory` + `LazyLoadFactory` registered as DI singleton
- **Named serialization:** `LazyLoadJsonConverterFactory` — `{"value": ..., "isLoaded": bool}`
- **Ordinal serialization:** Generator two-slot encoding `[value, isLoaded]` via `IsLazyLoad` flag on `OrdinalPropertyModel`
- **Merge pattern:** `ILazyLoadDeserializable.ApplyDeserializedState()` preserves loader delegates across serialization
- **Design project:** `LazyLoadExample.cs` with constructor-initialization pattern, 6 design tests
- **Documentation:** CLAUDE-DESIGN.md, serialization.md, interfaces-reference.md, SerializationTests.cs all updated

### Key design decisions:
- No Neatoo-specific interfaces (`IValidateMetaProperties`, `IEntityMetaProperties`) — Neatoo extends for those
- Record primary constructor + `LazyLoad<T>` is out of scope (semantically nonsensical)
- Named format uses merge pattern; ordinal uses reconstruction
- `LazyLoadJsonConverterFactory` is standalone (not a `NeatooJsonConverterFactory` subclass)

### Follow-up work (not in scope):
- Neatoo migration to use RemoteFactory's `LazyLoad<T>` as base type
- Release notes for next version
- Skill file updates (requires mdsnippets workflow)
