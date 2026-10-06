# Walker covers dictionary entries and public fields

**Plan #:** 002
**Date:** 2026-09-10
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-2, AC-6
**Status:** In Progress
**Last Updated:** 2026-10-05
**Plan-review opt-in:** No — one generator seam, additive, no public API or documented-rule change.
**Code-review opt-in:** Yes — behavior-changing generator emission.
**Branch:** dict-001-gate-dictionary-carried-dto — shared with DICT-001 (one seam; Discovery Log 2026-10-05 / DICT-001)
**PR:** —

---

## Scope

Brings the DTO walker up to the two shapes this todo covers. A `KeyValuePair<K,V>` element yields both its type arguments, so every pair-enumerating dictionary shape — `Dictionary`, `IDictionary`, `IReadOnlyDictionary`, sorted, concurrent, immutable, consumer-defined — contributes its key and value types, recursing through a value that is itself a collection or nullable so `Dictionary<string, List<Dto>>` reaches `Dto`. Public instance fields are walked alongside properties, because the serializer runs with `IncludeFields` on. Both shapes flow through the same constructor-shape bucket sort list elements get, through all three callers: factory signatures, `[Factory]` entity properties, and event-record graphs. Generator unit tests pin each shape per caller and per bucket, including dictionary keys, and the DICT-001 harness cases turn green. It does not walk tuple elements, emits no diagnostic, and does not change the runtime, `DtoConstructorRegistry`, or the harness beyond what DICT-001 added.

---

## Intent

- A consumer whose data object reaches the client only as a dictionary entry, or only through a public field, gets a working trimmed client with no `LinkerConfig.xml` entry, the same as a list element already does.
- The walk's rule becomes "every public instance member the serializer can write, through every generic collection shape", rather than a list of shapes found one consumer failure at a time.

---

## Framework & Architectural Alignment

- `DtoTypeWalker` is the single seam shared by all three callers (TRIM-002, TRIM-007): change it once, prove it per caller.
- The constructor-shape bucket sort from TRIM-001: a public parameterless constructor goes to `Register`, parameterized-only goes to `PreserveType`.
- Incremental-generator rules: walker output flows into `EquatableArray<string>`; no non-equatable state is added to any transform.
- Roslyn type identity through `SymbolEqualityComparer.Default`, which ignores nullable annotations.

---

## Constraints & Invariants

- Every existing generator test stays green and unmodified; shapes the walk already covered produce the same registrations.
- `System.*` types are never bucketed; `[Factory]` types are never bucketed.
- The walk terminates on a self-referential enumerable and treats it as single-level unwrapping did.
- No runtime, registry, or harness change beyond DICT-001's.
- The generator stays `netstandard2.0`, gains no dependency, and builds with warnings as errors.

---

## Steps

1. Write unit tests for each shape per caller and per bucket, plus the safety cases, and run them against the unchanged generator: shape tests red, safety tests green.
2. Make collection unwrapping recursive, with a seen-set so a self-referential enumerable stops where single-level unwrapping stopped.
3. Expand a key-value-pair element into its key and value types.
4. Walk public instance fields alongside properties, excluding static, const, and implicitly declared fields.
5. Bring the walker's own comments in line with the shapes it covers.
6. Run the new tests green, then the full build and test of both solutions.
7. Publish the trimmed harness, run the gate and the harness, confirm both DICT-001 checks pass with nothing regressed, and record the run in `reviews/002-evidence/`.

---

## Acceptance

- [ ] A DTO reachable only as a dictionary value is bucketed by constructor shape through each of the three callers, including when the value is itself a collection. `[unit]` · Must
- [ ] The DICT-001 dictionary check passes on the publish-trimmed harness. `[trimmed-harness]` · Must
- [ ] A DTO reachable only as a dictionary key is registered. `[unit]` · Could
- [ ] A DTO reachable only through a public instance field is bucketed by constructor shape on DTOs and on entities, and private, static, and const fields are not walked. `[unit]` · Should
- [ ] The DICT-001 public-field check passes on the publish-trimmed harness. `[trimmed-harness]` · Should
- [ ] A self-referential enumerable terminates with its pre-change registration, and a dictionary of only `System` types registers nothing. `[unit]` · Must
- [ ] Every pre-existing unit, integration, and trimmed-harness check stays green, and both solutions build on net9.0 and net10.0. `[explicit-skip: build/test gates]` · Must

---

## Current State (Pre-Flight)

Walked 2026-10-05 at commit `9d6fae2`.

- **The seam** — `src/Generator/DtoTypeWalker.cs`. `UnwrapType` strips `Task<T>` (top level only), then nullable, then unwraps ONE level of collection: the first generic `IEnumerable<T>` in `AllInterfaces`, or `IEnumerable<T>` itself, or an array element. The collection check is gated on `IsGenericType`. `WalkDtoGraph` checks candidacy, buckets by constructor shape, then walks members. `WalkProperties` walks the base chain visiting public, instance, non-indexer properties with a getter — `IPropertySymbol` only.
- **Why each gap happens** — `Dictionary<K,V>` unwraps to `KeyValuePair<K,V>`, which `IsDtoStructureCandidate` rejects as `System.*`. `List<List<Dto>>` unwraps to `List<Dto>`, rejected the same way. Public fields never reach `WalkProperties`.
- **Callers** — signatures at `FactoryGenerator.Types.cs:819` and `:836` (`UnwrapType` then `WalkDtoGraph`); entities at `FactoryGenerator.Types.cs:271` (`WalkEntityProperties`); events at `FactoryGenerator.Events.cs:69` (`WalkDtoGraph` on the event record). All three reach member types through `WalkProperties` and `UnwrapType`.
- **Fields on the wire** — plain DTOs and event records go through the reflection serializer with `IncludeFields = true`, so public fields are serialized. `[Factory]` entities serialize through generated ordinal code that collects properties only (`CollectOrdinalProperties`) in the default `Ordinal` format, but through the reflection serializer, fields included, in `Named` format (`NeatooOrdinalConverterFactory.CanConvert`). The generator cannot see the consumer's format, so fields are walked on entities too, exactly as properties are.
- **Unit-test harness** — `DiagnosticTestHelper.RunGenerator` compiles against CoreLib, `System.Runtime`, `System.ComponentModel`, RemoteFactory, DI, and Logging. `Dictionary`, `IDictionary`, `IReadOnlyDictionary`, `KeyValuePair`, and `List` resolve; `SortedDictionary` and `ConcurrentDictionary` may not, so tests use the CoreLib shapes. The unit-test project references the generator by project reference; the stale-testhost hazard is documented at `DiagnosticTestHelper.cs:181`.
- **No test pins either gap** — nothing under `DtoDiscovery/` mentions dictionaries, `KeyValuePair`, or fields.

---

## Punchlist

- (none)

---

## Test Evidence

(Filled before the Step 5 gate.)

---

## Gate Record

(Filled at Step 5.)

---

## Plan Amendments

### 2026-09-11 — Widened from dictionaries to the shape set

- **Section affected:** Scope, Serves, title
- **Original said:** Dictionary keys and values, with nested wrappers recursing.
- **What changed:** Adds public fields and tuple elements.
- **Why:** User decision 2026-09-11: finish the walker once instead of returning to it per consumer failure.
- **Discovery Log:** 2026-09-11 / DICT

### 2026-10-05 — Narrowed to dictionary entries and public fields

- **Section affected:** Scope, Serves, title
- **Original said:** Dictionaries, recursive wrappers, public fields, and tuple elements.
- **What changed:** Tuple elements dropped. Recursion stays, scoped to reaching a dictionary value that is itself a collection or nullable.
- **Why:** User narrowed the todo 2026-10-05.
- **Discovery Log:** 2026-10-05 / DICT

---

## Notes

- The seam is `DtoTypeWalker.UnwrapType` plus the member walk, shared by all three callers, so one change covers signatures, entity properties, and event graphs; the unit tests exist to prove that claim per caller rather than assume it.
- Fields are in scope because `NeatooJsonSerializer` sets `IncludeFields = true`; if that option is ever turned off, the field walk becomes dead weight and should go with it.
- Side effect, per a read of `UnwrapType`: a nested list such as `List<List<Dto>>` is missed today for the same reason, because the inner list is rejected as a System type. Recursive unwrapping fixes it too. Pin it with one unit test; it is not a criterion.
- Unchanged boundary: the collection check stays gated on `IsGenericType`, so a non-generic subclass such as `class Dtos : List<Dto>` is still bucketed as a DTO itself rather than unwrapped. Relaxing the gate would stop registering the subclass's own constructor, which the serializer needs. DICT-003 lists it as not covered.
