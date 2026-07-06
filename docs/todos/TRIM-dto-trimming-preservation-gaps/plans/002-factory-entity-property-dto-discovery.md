# TRIM-002 — `[Factory]` entity property-graph DTO discovery

**Plan #:** 002
**Date:** 2026-07-06
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-07-06
**Plan-review opt-in:** Yes (the todo's one design-open walk-boundary decision is resolved in this draft and needs the adversarial check; generator emission contract change; documented-behavior change)
**Code-review opt-in:** Yes (behavior-changing generator work)

---

## Scope

Extend DTO discovery to descend into `[Factory]`-annotated types' public property graphs without treating the entity itself as a DTO. Today `WalkDtoGraph` rejects a `[Factory]` node (correct — entities are preserved via DI registration) but never walks its properties, so a plain DTO reachable *only* as an entity property is never discovered and gets trimmed on the client. Consumer evidence from the zTreatment cut-over: `TreatmentBanner` (a record property on the `[Execute]`-opened `TreatmentContext` aggregate) and `DashboardContactResult` (a `List<T>` property on the `PatientSearchQuery` factory entity) both required manual LinkerConfig entries. The descent reuses TRIM-001's bucketed walk (Register vs PreserveType) and emits into the entity's own registrar. Also absorbs the TRIM-001 code-review callout: tighten `IsDtoStructureCandidate`'s `ns.StartsWith("System")` prefix match to a segment match so consumer namespaces like `Systems.Domain` aren't silently excluded. Includes publish-trimmed harness cases (DTO and record reachable only via entity property) and the docs deltas. Does NOT change entity preservation itself (already handled by DI registration), the signature walk's behavior (TRIM-001, unchanged apart from the shared candidate-check hardening), event preservation (TRIM-003), or over-retention (TRIM-005).

---

## Intent

- A consumer whose aggregate carries plain DTOs or records as properties (the entity-duality pattern — DTOs riding on `[Execute]`-opened aggregates or query entities) gets a trimming-safe client with no manual preservation work — the second of the two zTreatment failure classes.
- The walk-boundary question the original stub left open is resolved as **per-entity self-walk**: every `[Factory]` class type walks its *own* property graph during its own generation and emits preservation in its *own* registrar. No global "reachable from signatures" set exists; when any walk (signature or entity) meets a `[Factory]`-typed node it skips both bucketing and descent, because that type's own registrar owns its graph.

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
- Existing tests untouched; full suite green on net9.0 + net10.0; CI trimming gate green.

---

## Steps

1. Add the entity property walk: during `TypeInfo` construction for `[Factory]` class types (non-interface, non-static), walk the type's public instance property graph (inherited included) with `WalkDtoGraph` semantics, merging results into the existing two buckets the type's registrar already emits.
2. Keep `[Factory]`-node skipping symmetric: both the signature walk and the entity walk skip factory-typed nodes entirely (no bucket, no descent).
3. Tighten the `System` namespace exclusion in `IsDtoStructureCandidate` to a segment match (`ns == "System" || ns.StartsWith("System.")`).
4. Settle wrapper coverage for entity properties at the keyboard — verify whether `LazyLoad<T>` properties unwrap to `T` for discovery (its `Value` crosses the wire) and whether `LazyLoad<T>` itself is currently mis-bucketed as a DTO; name the finding either way (fix here if small, queue if not).
5. Unit tests pinning: DTO-typed and record-typed entity properties (both buckets, incl. collection and nested-through-DTO), child-entity properties skipped by the parent (covered by the child's own registrar), no walk for interface/static factories, `Systems.*` namespace discovered post-hardening, cycle safety.
6. Trimmed-harness case: a plain DTO and a positional record reachable *only* as `TrimTestEntity` properties — never constructed in harness code — deserialize on the publish-trimmed client; keyboard negative control.
7. Docs to shipped behavior: `docs/trimming.md` (nested-discovery and "not returned by any factory method" guidance now includes entity properties), CLAUDE-DESIGN.md (discovery criteria / nested-discovery paragraph / FAQ row on nested-DTO trimming failures), Design.Domain comments if a pattern file documents entity-carried DTOs.

---

## Acceptance

Tier note: `[trimmed-harness]` = named check in `RemoteFactory.TrimmingTests` under `PublishTrimmed=true`, enforced by the TRIM-004 CI gate.

- [ ] An entity's registrar emits `Register<T>`/`PreserveType<T>` for DTO types reachable only through the entity's public property graph — direct property, collection element, record property, and DTO-nested-under-DTO — bucketed by ctor shape. `[unit]`
- [ ] Child-entity-typed properties produce no bucket entry and no parent-side descent; the child entity's own registrar carries the child's DTO properties. `[unit]`
- [ ] Interface-factory and static-factory types get no entity property walk. `[unit]`
- [ ] A DTO in a `Systems.*`-style namespace is discovered (segment-match hardening); `System.*` framework types remain excluded. `[unit]`
- [ ] A plain DTO and a positional record whose only reachability is via `[Factory]` entity properties survive publish-trimming and deserialize on the client. `[trimmed-harness]`
- [ ] Full solution build/test green (net9.0 + net10.0); CI trimming gate green. `[explicit-skip: build/test/CI gates]`
- [ ] Docs updated to the shipped discovery behavior. `[explicit-skip: doc delta, reviewed at code review]`

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

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| — | — | — | — |

---

## Plan Amendments

(None yet.)

---

## Notes

- The rejected walk-boundary alternative, for the record: inline descent through `[Factory]`-typed properties during the signature walk. Rejected because it bloats every factory's registrar with other entities' DTOs, makes one factory's generated output depend on another entity's shape, and needs cross-entity cycle tracking — the per-entity self-walk gets the same coverage with none of that.
- TRIM-001 gate lesson applies to Step 6: harness property types must never be constructed in harness code, or the check is vacuous.
