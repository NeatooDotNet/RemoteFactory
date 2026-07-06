# TRIM-002 — `[Factory]` entity property-graph DTO discovery

**Plan #:** 002
**Date:** 2026-07-06
**Related Todo:** [../todo.md](../todo.md)
**Status:** In Progress
**Last Updated:** 2026-07-06
**Plan-review opt-in:** Yes (the todo's one design-open walk-boundary decision is resolved in this draft and needs the adversarial check; generator emission contract change; documented-behavior change)
**Code-review opt-in:** Yes (behavior-changing generator work)

---

## Scope

Extend DTO discovery to descend into `[Factory]`-annotated types' public property graphs without treating the entity itself as a DTO. Today `WalkDtoGraph` rejects a `[Factory]` node (correct — entities are preserved via DI registration) but never walks its properties, so a plain DTO reachable *only* as an entity property is never discovered and gets trimmed on the client. Consumer evidence from the zTreatment cut-over: `TreatmentBanner` (a record property on the `[Execute]`-opened `TreatmentContext` aggregate) and `DashboardContactResult` (a `List<T>` property on the `PatientSearchQuery` factory entity) both required manual LinkerConfig entries. The descent reuses TRIM-001's bucketed walk (Register vs PreserveType) and emits into the entity's own registrar. Also absorbs the TRIM-001 code-review callout: tighten `IsDtoStructureCandidate`'s `ns.StartsWith("System")` prefix match to a segment match so consumer namespaces like `Systems.Domain` aren't silently excluded. Includes publish-trimmed harness cases (DTO and record reachable only via entity property) and the docs deltas. Does NOT change entity preservation itself (already handled by DI registration), the signature walk's behavior (TRIM-001, unchanged apart from the shared candidate-check hardening), event preservation (TRIM-003), or over-retention (TRIM-005).

---

## Intent

- A consumer whose aggregate carries plain DTOs or records as properties (the entity-duality pattern — DTOs riding on `[Execute]`-opened aggregates or query entities) gets a trimming-safe client with no manual preservation work — the second of the two zTreatment failure classes.
- The walk-boundary question the original stub left open is resolved as **per-entity self-walk**: every class type carrying `[Factory]` *directly* (and thus getting its own registrar) walks its *own* property graph during its own generation and emits preservation in its *own* registrar. No global "reachable from signatures" set exists; when any walk (signature or entity) meets a factory-typed node it skips both bucketing and descent, because that type's own registrar owns its graph (plan review narrowed this claim — see Constraints for the interface-factory-impl exception).

---

## Framework & Architectural Alignment

- Per-type generation discipline: the entity property walk runs in `TypeInfo` construction alongside the existing ordinal-property collection (same `!IsInterface && !IsStatic` condition — the entity shapes), keeping discovery per-type and incremental-cache friendly (buckets stay `EquatableArray` on transform outputs, per TRIM-001/plan-review B1).
- Reuses TRIM-001's `WalkDtoGraph` bucketed descent unchanged; the entity root itself contributes no bucket entry (entities preserve via DI registration — `NeatooFactoryRegistrar` + `AddScoped`/`AddTransient`).
- Emission lands in the existing registrar Register/PreserveType block — no new emission surface.
- Trimmed verification per the TRIM-004 harness contract (named bool check, no construction of the types under test, negative control at the keyboard).

---

## Constraints & Invariants

- Entities never land in either preservation bucket — DI registration remains their preservation mechanism.
- The signature walk (TRIM-001) is behaviorally unchanged except the shared `System` segment-match hardening.
- Child-entity-typed properties (including collections of entities) are neither bucketed nor descended by the parent — the child's own registrar covers its graph; cross-entity cycle safety follows from this.
- Accepted trade-off: every `[Factory]` class's DTO property types are preserved on the client even if that entity never crosses the wire — consistent with entities' own unconditional registrar registration; duplicate emissions across registrars stay idempotent (`TryAdd` / no-op `PreserveType`).
- Known boundary, by design (plan review B1): a class implementing a `[Factory]` interface *without* carrying the attribute itself gets no registrar and no entity walk — its property graph is covered by nobody. Acceptable: those are stateless server-only service implementations, never serialized across the wire.
- Ordinal orthogonality (plan review B3): the entity walk records reachable DTO *types* for trimming; `CollectOrdinalProperties` records the entity's own serialization slots — different outputs, different registries, and the entity walk skips factory-typed (ordinal-serialized) properties entirely. No double-handling.
- Existing tests untouched; full suite green on net9.0 + net10.0; CI trimming gate green.

---

## Steps

1. Add the entity property walk: during `TypeInfo` construction for `[Factory]` class types (non-interface, non-static), walk the type's public instance property graph (inherited included) with `WalkDtoGraph` semantics, merging results into the existing two buckets the type's registrar already emits.
2. Keep `[Factory]`-node skipping symmetric: both the signature walk and the entity walk skip factory-typed nodes entirely (no bucket, no descent).
3. Tighten the `System` namespace exclusion in `IsDtoStructureCandidate` to a segment match (`ns == "System" || ns.StartsWith("System.")`).
4. Settle `LazyLoad<T>` handling at the keyboard, starting from the plan review's corrected diagnosis: `T` *is* already walked (descent goes through the public `Value` getter), and the only artifact is a benign spurious `Register<LazyLoad<T>>` emission (`LazyLoad<T>` passes candidacy and has a `[JsonConstructor]` parameterless ctor, but deserializes via `LazyLoadJsonConverterFactory`, never the registry). Decide fix-vs-accept; if queued instead, add a Plan Index stub first.
5. Unit tests pinning: DTO-typed and record-typed entity properties (both buckets, incl. collection and nested-through-DTO), child-entity properties skipped by the parent (covered by the child's own registrar), no walk for interface/static factories, `Systems.*` namespace discovered post-hardening, cycle safety.
6. Trimmed-harness case: a plain DTO and a positional record reachable *only* as `TrimTestEntity` properties — never constructed in harness code — deserialize on the publish-trimmed client; keyboard negative control.
7. Docs to shipped behavior: `docs/trimming.md` — both the nested-discovery prose ("properties of each discovered DTO" framing is now incomplete; entry points include entity graphs) and the trailing manual-preservation boundary sentence; CLAUDE-DESIGN.md — nested-discovery paragraph and the FAQ row on nested-DTO trimming failures; Design.Domain comments if a pattern file documents entity-carried DTOs. (Anchors per plan review: `trimming.md:279,:283`; `CLAUDE-DESIGN.md:296,:789`.)

---

## Acceptance

Tier note: `[trimmed-harness]` = named check in `RemoteFactory.TrimmingTests` under `PublishTrimmed=true`, enforced by the TRIM-004 CI gate.

- [x] An entity's registrar emits `Register<T>`/`PreserveType<T>` for DTO types reachable only through the entity's public property graph — direct property, collection element, record property, and DTO-nested-under-DTO — bucketed by ctor shape. `[unit]`
- [x] Child-entity-typed properties produce no bucket entry and no parent-side descent; the child entity's own registrar carries the child's DTO properties. `[unit]`
- [x] Interface-factory and static-factory types get no entity property walk. `[unit]`
- [x] A DTO in a `Systems.*`-style namespace is discovered (segment-match hardening); `System.*` framework types remain excluded. `[unit]`
- [x] A plain DTO and a positional record whose only reachability is via `[Factory]` entity properties survive publish-trimming and deserialize on the client. `[trimmed-harness]`
- [x] Full solution build/test green (net9.0 + net10.0); CI trimming gate green. `[explicit-skip: build/test/CI gates]` *(build 0 errors; units 581+581 green; integration 561+561 green with `xUnit.MaxParallelThreads=1` — the pre-existing FactoryEventRelay test family is parallel-load flaky, different members flake per run, all pass isolated/sequential; CI verifies on the PR)*
- [x] Docs updated to the shipped discovery behavior. `[explicit-skip: doc delta, reviewed at code review]`

---

## Current State (Pre-Flight)

Walked 2026-07-06 on `TRIM` (post TRIM-001 merge, e0588f7):

- `[Factory]` rejection: `DtoTypeWalker.IsDtoStructureCandidate` (`DtoTypeWalker.cs:105-118`) rejects factory-annotated types (directly or via interface) → `WalkDtoGraph` returns before descent (`:165`). Correct for bucketing; the missing piece is the entity-rooted property walk.
- Insertion seam: `TypeInfo` ctor has the entity `symbol` in hand; per-method DTO aggregation at `FactoryGenerator.Types.cs:237-252`; the ordinal-property walk precedent sits immediately after (`:254-257`, `CollectOrdinalProperties(symbol)` under `!this.IsInterface && !this.IsStatic`).
- `WalkProperties` (`DtoTypeWalker.cs`) already walks public instance getters including the base chain — the entity walk is `WalkProperties(entity, nested => WalkDtoGraph(nested, ...))` in shape.
- Renderers already emit both buckets from the models (TRIM-001); no renderer change expected — the entity-walk results merge into the same `DtoReturnTypes`/`DtoPreserveTypes`.
- `ns.StartsWith("System")` prefix match at `DtoTypeWalker.cs:97` (TRIM-001 code-review callout).
- `LazyLoad<T>`: `UnwrapType` unwraps Task/nullable/IEnumerable only — `LazyLoad<T>` would be treated as a candidate type itself (namespace `Neatoo.RemoteFactory`, likely parameterless ctor → Register bucket, `T` never walked). Unverified — Step 4 keyboard item.
- Harness: `TrimTestEntity` is the class-factory target (`[Remote, Create]` with `[Service]` param); TRIM-004 check contract in `Program.cs`; records for property use must follow the no-construction rule (TRIM-001 gate lesson).
- Unit home: `RemoteFactory.UnitTests/FactoryGenerator/DtoDiscovery/` — `RecordDtoDiscoveryTests` has the Register/Preserve regex helpers to reuse; note the emission-per-registrar assertion needs to distinguish *which* factory's registrar contains the emission (child-vs-parent test) — regex over per-tree generated text rather than the concatenated dump.

---

## Test Evidence

Filled after implementation, before the Step 5 gate.

Filled 2026-07-06, before the Step 5 gate. Unit tests in `RemoteFactory.UnitTests/FactoryGenerator/DtoDiscovery/EntityPropertyDtoDiscoveryTests`.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| Entity registrar emits both buckets for property-graph DTOs | `[unit]` | `EntityWithDtoProperty_RegisterEmittedInEntityRegistrar`, `EntityWithRecordProperty_PreserveTypeEmitted` (TreatmentBanner shape), `EntityWithDtoCollectionProperty_ElementDiscovered` (DashboardContactResult shape), `DtoNestedUnderEntityProperty_BothLevelsDiscovered`, `LazyLoadDtoProperty_InnerDtoDiscoveredThroughValue`, `DtoCycleUnderEntity_TerminatesAndRegistersOnce` | ✓ |
| Child-entity properties: no parent-side entry or descent | `[unit]` | `ChildEntityProperty_CoveredByChildRegistrarNotParent` (per-tree assertion: parent tree lacks child DTO, child tree carries it) | ✓ |
| No walk for interface/static factories | `[unit]` | `InterfaceFactory_NoEntityPropertyWalk` (impl-class property DTO emitted nowhere — the documented B1 boundary); static factories have no instance properties (structurally untestable) | ✓ |
| `Systems.*` discovered post-hardening | `[unit]` | `SystemsPrefixedConsumerNamespace_NotExcluded`; framework exclusion pinned by the whole existing suite staying green | ✓ |
| Entity-carried DTO + record survive publish-trimming | `[trimmed-harness]` | `EntityPropertyDtoSmokeTest.Run` (`TrimEntityCarriedInfo` + `TrimEntityCarriedBanner`, never constructed in harness code) — trimmed run exit 0; **negative control**: entity walk disabled in the generator → carried-DTO check throws `NotSupportedException`, harness exits 1 on "entity property DTO preservation" | ✓ |
| Build/test/CI gates | `[explicit-skip]` | `reviews/002-build.log` (0 errors); `reviews/002-test.log` (units 581+581 green; two pre-existing relay-family parallel flakes in integration); `reviews/002-test-integration-seq.log` (integration 561+561 green, `MaxParallelThreads=1`); `reviews/002-publish.log`; CI on PR | ✓ |
| Docs updated | `[explicit-skip]` | `docs/trimming.md` (entity-graph entry point + boundary sentence), CLAUDE-DESIGN.md (entity property-graph discovery paragraph + FAQ row) | ✓ |

---

## Plan Amendments

### 2026-07-06 — Step 4 resolved: LazyLoad spurious emission ACCEPTED

- **Section affected:** Step 4
- **Original said:** decide fix-vs-accept for the spurious `Register<LazyLoad<T>>` emission at the keyboard.
- **What changed:** accepted, no code change. Keyboard verification confirmed the plan review's diagnosis and went one step further: `LazyLoadConverter<T>` constructs `new LazyLoad<T>()` in *compiled generic code* (`LazyLoadJsonConverterFactory.cs:104-107`), so the emission is redundant for preservation — but it is idempotent, harmless, and conservatively doubles as rooting. Removing it buys nothing and would need its own trimmed verification. `T`-through-`Value` descent is pinned by `LazyLoadDtoProperty_InnerDtoDiscoveredThroughValue`.
- **Why:** zero-risk beats cosmetic registrar cleanliness.
- **Discovery Log link:** covered by the TRIM-002 gate entry (no separate discovery).

---

## Notes

- The rejected walk-boundary alternative, for the record: inline descent through `[Factory]`-typed properties during the signature walk. Rejected because it bloats every factory's registrar with other entities' DTOs, makes one factory's generated output depend on another entity's shape, and needs cross-entity cycle tracking — the per-entity self-walk gets the same coverage with none of that.
- TRIM-001 gate lesson applies to Step 6: harness property types must never be constructed in harness code, or the check is vacuous.
