# TRIM-001 — Positional-record preservation in factory signatures

**Plan #:** 001
**Date:** 2026-07-06
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-07-06
**Plan-review opt-in:** Yes (changes the generator's emission contract for every consumer's registrar; touches documented behavior in docs/trimming.md and CLAUDE-DESIGN.md; incremental-pipeline equality semantics involved)
**Code-review opt-in:** Yes (behavior-changing generator work)

---

## Scope

Make the factory-signature DTO walk preserve positional records (types with only parameterized public ctors) instead of silently dropping them. Today `DtoTypeWalker.WalkFactoryReturn` requires `HasParameterlessCtor`, so a record like zTreatment's `StartVisitResultV2` — returned from a `[Remote, Execute]` command — gets no preservation and the trimmed client throws `DeserializeNoConstructor`. The fix shape already exists in the codebase's own history: bucket-sort discovered types the way the (now-dead) `WalkEventRoot` did for *nested* types — parameterless ctor → `DtoConstructorRegistry.Register<T>(() => new T())`, parameterized/record → `DtoConstructorRegistry.PreserveType<T>()` (deserialization then flows through the existing `RecordBypassConverterFactory`). Roots bucket by ctor shape too — `WalkEventRoot`'s root-always-PreserveType rule is event-specific and must not be ported (plan review B2). Applies uniformly to return types, non-service parameters, and nested properties of discovered DTOs — with property descent into both buckets. Includes retiring the dead `WalkEventRoot` in favor of the refit shared walk, trimmed-harness repro checks (record-as-return, record-as-parameter, record-nested-in-DTO), and the docs/Design corrections for "What Qualifies as a DTO". Does NOT touch `[Factory]` entity property descent (TRIM-002), event preservation (TRIM-003), or the over-retention question (TRIM-005).

---

## Intent

- A consumer returning or accepting a positional record through any factory method gets a trimming-safe client with zero manual preservation work — the exact shape that broke zTreatment's cut-over.
- The Design projects' already-documented promise (`ExampleRecordResult(int Id, string Name)` as an interface-factory return type, `AllPatterns.cs`) becomes true under `PublishTrimmed=true`, not just in untrimmed test runs.
- The generator's two-bucket emission (Register vs PreserveType) becomes the documented, tested contract for DTO preservation going forward — TRIM-002 reuses it for entity property descent.

---

## Framework & Architectural Alignment

- Emission lands in the generated `FactoryServiceRegistrar` alongside the existing `Register<T>` calls — same pattern, second bucket (`PreserveType<T>` already exists at `DtoConstructorRegistry` with `[DynamicallyAccessedMembers(All)]` rooting; it is currently emitted nowhere).
- Deserialization path unchanged: parameterized-ctor types are claimed by `RecordBypassConverterFactory` (detection rule matches the bucket rule for reference types; `record struct` diverges benignly — Roslyn reports the synthesized parameterless ctor → Register bucket, reflection omits it → bypass converter claims it; both sides round-trip, see plan review B3); parameterless DTOs keep flowing through `NeatooJsonTypeInfoResolver.CreateObject`.
- Roslyn incremental-generator discipline: the pipeline cache boundary is the transform-output records (`TypeInfo` / `TypeFactoryMethodInfo` / `MethodInfo`) — the second bucket must be an `EquatableArray<string>` there or incremental caching silently regresses with no failing test (plan review B1). The factory models run inside `RegisterSourceOutput` and may keep `IReadOnlyList<string>`.
- Unit-test pattern: `DiagnosticTestHelper.RunGenerator` + assertions over generated trees (the `NestedDtoDiscoveryTests` shape).
- Trimmed-repro pattern: named bool checks in the `RemoteFactory.TrimmingTests` harness under the TRIM-004 exit-code contract.

---

## Constraints & Invariants

- Existing `Register<T>` emission for parameterless DTOs is unchanged — same call shape, same idempotent `TryAdd` semantics in consumer registrars.
- `IsDtoStructureCandidate` exclusions stay intact: `[Factory]` types (direct or via interface), `System.*`, primitives, abstract/interface types get neither bucket.
- A type never lands in both buckets, and the visited-set dedupe holds across return/parameter/nested discovery within a method and across methods within a type.
- The existing DtoDiscovery unit tests and the full suite stay green on net9.0 + net10.0.
- The TrimmingTests harness stays green in CI (TRIM-004 gate) — new checks added, existing checks untouched.

---

## Steps

1. Replace the walker's parameterless-ctor gate with bucket classification — parameterless → Register bucket, parameterized-public-ctor → PreserveType bucket — applied by ctor shape at every level (roots and nested alike; `WalkEventRoot`'s root-always-PreserveType rule is event-specific and not ported), shared across return types, non-service parameters, and nested property descent (descending into both buckets' types). Retire the dead `WalkEventRoot` in favor of this shared walk and fix the stale file-header comment that still names the removed relay-handler caller.
2. Thread the ctor-shape distinction through the discovery pipeline: method-level discovery → per-type aggregation/dedupe → model builder → all three factory models, keeping the bucket `EquatableArray`-backed on the transform-output records (`TypeInfo`/`MethodInfo`/`TypeFactoryMethodInfo`) where incremental caching keys live.
3. Emit `PreserveType<T>()` alongside `Register<T>()` in all three registrar renderers (class, interface, static).
4. Extend the DtoDiscovery unit tests to pin bucket assignment: records as return type, as parameter, nested in a class DTO, class DTO nested in a record; existing exclusions still emit nothing.
5. Add trimmed-harness repro checks mirroring the consumer failure: a positional record returned from a `[Remote, Execute]` command, taken as a parameter, and nested as a property of a discovered DTO — each serializer round-tripped on the trimmed client (the `EventRelaySmokeTest` shape).
6. Update the documentation to the shipped behavior: `docs/trimming.md` "What Qualifies as a DTO" (records are preserved via `PreserveType`, not merely "handled separately by `RecordBypassConverterFactory`" — that sentence conflates deserialization mechanics with preservation), CLAUDE-DESIGN.md's DTO-registry section and FAQ row, and the `AllPatterns.cs` comments around `ExampleRecordResult`.

---

## Acceptance

Tier note: `[trimmed-harness]` is a project-local tier — a named check in `RemoteFactory.TrimmingTests` executed under `PublishTrimmed=true` by the CI trimming gate (TRIM-004).

- [ ] The generator emits `PreserveType<T>()` for a positional record appearing as a factory-method return type, as a non-service parameter, and as a property of a discovered DTO; `Register<T>()` emission for parameterless DTOs is unchanged. `[unit]`
- [ ] A positional record returned from a `[Remote, Execute]` command round-trips through the Neatoo serializer on a publish-trimmed client (the zTreatment `StartVisitResultV2` shape). `[trimmed-harness]`
- [ ] Record-as-parameter and record-nested-in-a-discovered-DTO shapes round-trip on the publish-trimmed client. `[trimmed-harness]`
- [ ] `[Factory]` types, `System.*`/primitive, and abstract/interface types still produce no preservation emission of either kind. `[unit]`
- [ ] Full solution build/test green on net9.0 + net10.0; CI trimming gate green. `[explicit-skip: build/test/CI gates]`
- [ ] `docs/trimming.md`, CLAUDE-DESIGN.md, and `AllPatterns.cs` comments describe the two-bucket emission as shipped. `[explicit-skip: doc delta, reviewed at code review]`

---

## Current State (Pre-Flight)

Walked 2026-07-06 on `TRIM` (post PR #68 merge, a566538):

- Gate: `DtoTypeWalker.WalkFactoryReturn` rejects at `src/Generator/DtoTypeWalker.cs:145` (`!IsDtoStructureCandidate || !HasParameterlessCtor`); property descent only happens for accepted types.
- Dead code: `WalkEventRoot` (`DtoTypeWalker.cs:173-231`) has no callers; it already implements the bucket shape (root → parameterized bucket always; nested → bucket by ctor). File-header comment (`DtoTypeWalker.cs:3-4`) still claims a `FactoryGenerator.RelayHandler` caller — stale.
- Discovery: `MethodInfo.DiscoverDtoTypes` (`FactoryGenerator.Types.cs:741-772`) walks return type (`unwrapTask: true`) and non-service, non-CancellationToken parameters; collects into a flat `List<string>` + shared visited set.
- Aggregation: per-type dedupe via `HashSet<string>` at `FactoryGenerator.Types.cs:236-245` → `TypeInfo.DtoReturnTypes` (`EquatableArray<string>`, lines 318/731).
- Model flow: `FactoryModelBuilder.cs:92/144/278` pass `typeInfo.DtoReturnTypes.ToList()` → `ClassFactoryModel.cs:48`, `InterfaceFactoryModel.cs:30`, `StaticFactoryModel.cs:32` (`IReadOnlyList<string>`).
- Emission sites: `ClassFactoryRenderer.cs:1541`, `InterfaceFactoryRenderer.cs:480`, `StaticFactoryRenderer.cs:117` — identical `Register<{dtoType}>(() => new {dtoType}())` loops.
- Runtime: `DtoConstructorRegistry.PreserveType<T>` exists (`DtoConstructorRegistry.cs:43`, DAM All); `RecordBypassConverterFactory.CanConvert` claims exactly the PreserveType bucket shape (no public parameterless ctor + ≥1 public parameterized ctor).
- Unit-test home: `RemoteFactory.UnitTests/FactoryGenerator/DtoDiscovery/` — `NestedDtoDiscoveryTests` (regex helper `GetRegisteredDtoTypes` extracts `Register<...>` matches; needs a PreserveType twin), `NestedDtoFailureTest`.
- Harness: TRIM-004 contract — named bool checks aggregating into `failedChecks`, keyed `HttpClient` with `NoOpHttpHandler` registered, CI publishes linux-x64 and runs. New checks slot in before the summary block in `Program.cs`.
- Docs to correct: `docs/trimming.md` "What Qualifies as a DTO" (~line 261) documents the record exclusion as handled; CLAUDE-DESIGN.md FAQ row (~295) and "DTO Constructor Registry for Trimming" (~768); `Design.Domain/FactoryPatterns/AllPatterns.cs` `ExampleRecordResult` comments (~453-463) promise record returns work.
- Edge shapes to settle at the keyboard: `record struct` (structs report an implicit parameterless ctor → Register bucket; verify `new T()` renders), records with both parameterless and parameterized ctors (Register bucket — matches `RecordBypassConverterFactory.CanConvert` declining them), private-ctor-only types.

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

- TRIM-002 will reuse the bucket walk for `[Factory]` entity property descent — keep the classification a single shared code path, not per-call-site logic.
- Plan review (2026-07-06, APPROVED — `../reviews/001-plan-review.md`) carry-alongs: anchor the unit-test PreserveType regex to `PreserveType<(.+?)>\(\)` and confirm TS-010/TS-014 count-assertions stay Register-only (B4); expect new additive `PreserveType<>` lines in existing record-target registrars (`InterfaceFactoryRecordTargets`) once descent enters record graphs (B5); Step 6 doc edits must leave no sentence implying `PreserveType` is emitted nowhere — the event-path removal statement at `docs/trimming.md:300` stays accurate, the factory-signature path is distinct (A1).
