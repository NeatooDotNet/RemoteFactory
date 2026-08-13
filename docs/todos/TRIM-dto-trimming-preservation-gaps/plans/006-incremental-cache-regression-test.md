# TRIM-006 — Incremental-generator caching regression test

**Plan #:** 006
**Date:** 2026-08-11
**Related Todo:** [../todo.md](../todo.md)
**Status:** In Progress
**Last Updated:** 2026-08-12
**Plan-review opt-in:** No (test-first plan, narrow blast radius, emitted output unchanged; the diagnosis was verified at the keyboard at draft time rather than inherited — see Current State)
**Code-review opt-in:** Yes (touches generator transform-output types)

---

## Scope

Close the project-wide hole plan-review B1 (TRIM-001) exposed: the generator's incremental cache boundary sits entirely on the transform-output records, and a field whose equality falls back to reference semantics silently breaks caching for every consumer with **no failing test** — `DiagnosticTestHelper.RunGenerator` runs the driver exactly once and never asserts cached steps. This plan adds a driver-level regression test that runs the generator twice across an unrelated edit with step tracking enabled and asserts each pipeline branch's output step is cached, then fixes whatever the guard finds. Pre-flight established that it will find something: the relay-handler branch's transform output is not value-equatable today. Does NOT change what any renderer emits — generated output must be byte-identical before and after — and does NOT extend to the ordinal-serialization or diagnostic paths beyond whatever the caching assertion naturally covers.

---

## Intent

- The generator's incremental caching becomes an **asserted** property instead of an assumed one, so the next transform-output field that breaks it fails a test instead of quietly degrading every consumer's incremental build.
- Any branch whose caching is *already* broken gets surfaced and repaired — consumers stop paying a full re-render on unrelated edits.
- The repo's value-equatable idiom (`EquatableArray<T>`) becomes the enforced convention on transform outputs rather than a discipline maintained by memory.

---

## Framework & Architectural Alignment

- Roslyn's incremental-generator caching contract: the transform output is the cache boundary. All four branches call `RegisterSourceOutput` directly on their transform node and do model-building *and* rendering inside the output stage, so the transform record's equality is the only thing standing between an unrelated edit and a full re-render.
- `EquatableArray<T>` is the established in-repo idiom for value-equatable collections on transform outputs. Anything brought onto the idiom should match it rather than introducing a bespoke comparer.
- Driver-level verification follows the standard Roslyn approach — `GeneratorDriverOptions` with `WithTrackingIncrementalGeneratorSteps`, asserting step run reasons — rather than inferring caching from output text.
- Test placement and fixture-source conventions follow the existing generator suites in `RemoteFactory.UnitTests`.
- The guard must cover all four pipeline branches, not only the class-factory branch that TRIM-001 was working in when the hole was found.

---

## Constraints & Invariants

- Generated output is byte-identical before and after this plan — cache behavior changes, emission does not.
- No renderer changes.
- `DiagnosticTestHelper.RunGenerator`'s existing signature and single-run behavior keep working; the diagnostic suite that depends on it stays green and unmodified. Step tracking is additive, not a rewrite of the shared helper.
- The relay-handler fix (if it lands as pre-flight expects) preserves the transform's current semantics — same entries, same diagnostics, same skip behavior for instance-method handlers.
- Full suite green on net9.0 and net10.0.

---

## Steps

1. Add driver-level test infrastructure that can run the generator twice over an evolving compilation with step tracking enabled, without disturbing the single-run helper the existing diagnostic suite depends on.
2. Assert that an unrelated edit between the two runs leaves each pipeline branch's source-output step reporting cached/unchanged.
3. Extend that assertion across all four branches — class factory, interface factory, relay handler, event preservation — so no branch is guarded by accident of which one the fixture happened to exercise.
4. Make the fixture source populate every collection-bearing field on each transform output. An empty collection can compare equal by accident, so a sparse fixture would let a future non-equatable field slip through the guard unnoticed.
5. Fix what the guard finds — pre-flight expects at least the relay-handler branch to fail — by bringing the offending transform output onto the repo's value-equatable idiom, without changing emitted output.
6. Prove the guard's sensitivity with a keyboard negative control: give a transform output a reference-equality field, confirm the assertion goes red, restore.

---

## Acceptance

- [x] An unrelated edit between two driver runs leaves every pipeline branch's source-output step reporting cached/unchanged. `[unit]`
- [x] Generated output is byte-identical before and after this plan's changes. `[unit]`
- [x] The relay-handler branch caches across an unrelated edit (it does not today). `[unit]`
- [x] The guard's sensitivity is proven by a keyboard negative control — a reference-equality field on a transform output turns the assertion red. `[explicit-skip: one-off keyboard verification, per TRIM-001/002/007 precedent]`
- [x] The existing diagnostic suite passes unchanged against `DiagnosticTestHelper.RunGenerator`. `[explicit-skip: regression of existing suite, satisfied by the full-suite run]`
- [x] Full solution build/test green (net9.0 + net10.0). `[explicit-skip: build/test gates]`

---

## Current State (Pre-Flight)

Walked 2026-08-11 on branch `TRIM` (3fe679e). **Diagnosis verified at the keyboard, not inherited** — the explicit lesson from TRIM-005's abandonment.

- **Pipeline shape** (`FactoryGenerator.cs:19-129`): four branches. Branches 1–2 (`:19-52`, `:54-78`) are `ForAttributeWithMetadataName` → `RegisterSourceOutput`, with `FactoryModelBuilder.Build` *and* `FactoryRenderer.Render` both inside the output stage. Branch 3 (`:81-106`) is the relay handler, same shape. Branch 4 (`:113-129`) is `CreateSyntaxProvider` → `.Where(...)` → `.Collect()` → `RegisterSourceOutput`. No intermediate `Select` anywhere, so the transform record's equality **is** the cache boundary in every branch.
- **The hole itself:** `DiagnosticTestHelper.RunGenerator` (`TestContainers/DiagnosticTestHelper.cs:101`) creates the driver via `CSharpGeneratorDriver.Create(generator)` with no `GeneratorDriverOptions` — step tracking off — and runs once at `:103`. Effectively the whole generator suite goes through it.
- **`TypeInfo`** (branches 1–2, `FactoryGenerator.Types.cs:71`): discipline holds today — every collection-typed *property* is `EquatableArray<T>` (`:340`, `:341`, `:342`, `:349`, `:356`, `:369`, `:375`, including TRIM-001's `DtoPreserveTypes`). The `List<>`/`HashSet<>` occurrences at `:75-76`, `:103`, `:161`, `:172`, `:184`, `:237-238`, `:259-260` are all constructor locals, not fields.
- **`FactoryEventInfo`** (branch 4, `FactoryGenerator.Events.cs:19-31`): clean — `string` plus two `EquatableArray<string>`.
- **`RelayHandlerModel` (branch 3, `Model/RelayHandlerModel.cs:10-36`) is NOT value-equatable.** `Usings`, `Entries`, and `Diagnostics` are `IReadOnlyList<T>`. A record's synthesized `Equals` uses `EqualityComparer<T>.Default`, which is reference equality for an interface-typed field, and the transform allocates fresh lists on every run. **Expect the guard to go red on this branch immediately** — a live break, not a hypothetical, and the reason Step 5 exists.
- **The idiom to match:** `EquatableArray<T>` (`src/Generator/EquatableArray.cs:12-44`) — `internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T> where T : IEquatable<T>` with real `Equals`/`GetHashCode`.
- **Sparse-fixture trap** (drives Step 4): a transform output whose new bad field is left empty in the fixture can still compare equal, so the guard would pass vacuously. The fixture has to populate the collection-bearing fields it means to protect.

---

## Test Evidence

Filled 2026-08-12, before the Step 5 gate. Logs in the session scratchpad; per-run detail in Implementation Record below.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| Every branch stays cached across an unrelated edit | `[unit]` | `IncrementalCacheTests.UnrelatedEdit_TransformOutputStaysCached` — `[Theory]`, 4 cases (`FactoryClass`, `FactoryInterface`, `RelayHandler`, `FactoryEvents`); non-vacuity backed by `Fixture_ExercisesEveryPipelineBranch` | `[unit]` |
| Emitted output byte-identical before/after | `[unit]` | `UnrelatedEdit_GeneratedOutputIsIdentical` covers run-to-run determinism. The *before/after-this-plan* half is **not** a test — it is verified by zero `git status` drift in the committed `Generated/` trees of both solutions after a full rebuild that re-emitted them (relay-handler file re-emitted 09:42:33, checked 09:44:01) | `[unit]` + repo-artifact diff |
| Relay-handler branch caches | `[unit]` | Same `[Theory]`, `RelayHandler` case — observed `Modified` → `Unchanged` across the fix | `[unit]` |
| Guard sensitivity (negative control) | `[explicit-skip]` | Performed at the keyboard, not committed — see Implementation Record. All four branches proven to go red on a reference-equality field | `[explicit-skip]` — honored |
| Existing diagnostic suite unchanged | `[explicit-skip]` | Covered by full-suite run; `RunGenerator`'s signature and single-run behavior untouched (extraction of `BuildReferences` only) | `[explicit-skip]` — honored |
| Full build/test green (net9.0 + net10.0) | `[explicit-skip]` | Solution: 601+601 unit, 561+561 integration, 0 failed, 5 pre-existing skips. Design solution: 86+86, 0 failed | `[explicit-skip]` — honored |

---

## Implementation Record

**Landed:** `TrackingNames.cs` (new); `.WithTrackingName(...)` on all four pipeline nodes in `FactoryGenerator.cs`; `RunGeneratorTracked(...)` + `BuildReferences()` extraction in `DiagnosticTestHelper.cs`; `IncrementalCacheTests.cs` (new); `RelayHandlerModel`/`EventHandlerEntry` collections moved to `EquatableArray<T>`.

**Red → green (Step 5).** Guard against unmodified HEAD: `RelayHandler` failed with reason `Modified`, the other three cached — pre-flight's prediction confirmed exactly, and the guard proven non-vacuous before it was made to pass. After moving `RelayHandlerModel.Usings/Entries/Diagnostics` and `EventHandlerEntry.Parameters/ServiceParameters/AllParameters` onto `EquatableArray<T>`: 6/6 green. Constructors normalize incoming sequences rather than taking the array type, so a future call site cannot reintroduce the defect by passing a plain list.

**Negative control (Step 6).** Added a `IReadOnlyList<string>` auto-property with a fresh-allocation initializer to `TypeInfo` (branches 1–2) and `FactoryEventInfo` (branch 4), covering the three branches that were *already* passing — the RelayHandler red above is the control for branch 3. Result: `FactoryClass`, `FactoryInterface`, `FactoryEvents` all went red with reason `Modified`; `RelayHandler` stayed `Unchanged`. Probes and the throwaway dump test removed; `git status` confirms no residue.

**Finding — the guard can read stale generator code locally.** The first negative-control run *passed*, which was wrong. Cause: the generator is loaded once per process via `Assembly.LoadFrom` into a static `Lazy`, so rebuilding only the generator and re-running with `--no-build` let a surviving testhost serve the previously loaded assembly. A dump of all tracked steps proved the probe was live in the DLL while the guard reported green. Rebuilding the test project produced the correct red. Documented in `RunGeneratorTracked`'s remarks; CI is unaffected because every run starts cold. Recorded in the todo's Discovery Log — this will bite the next person doing a red/green cycle on any dynamically-loaded-generator test, not just this one.

**Environment note.** `csharp-ls` repeatedly re-locked `src/Generator/bin/Debug/netstandard2.0/Neatoo.Generator.dll`, failing the solution build with MSB3021/MSB3027 until killed (twice, with the user's authorization). Separately, one solution build failed with a spurious `MSB3552` (`**/*.resx` not found) in IntegrationTests; the project builds clean in isolation and the error did not recur — transient, unrelated to this plan.

---

## Plan Amendments

(None yet.)

---

## Abandonment / Retirement Reason

<!-- Only if Status becomes Abandoned or Retired. -->

---

## Notes

- The relay-handler break is the plan's first expected discovery. If it lands as pre-flight predicts, it is an **Amend** — Step 5 already scopes fixing what the guard finds — not a re-split.
- Source of the hole: TRIM-001's test-review gate (plan review B1, 2026-07-06), recorded as pre-existing tech debt rather than TRIM-001 scope.
- This is the last queued plan in the TRIM Index. When it closes, the todo falls through to the close-out audit and the release step (AC4/AC5).
- Branch topology: implemented on `TRIM-006-incremental-cache-guard` off `TRIM`. That base carries the TRIM-007 bookkeeping commit from closed PR #72 and the TRIM-005 abandonment, both of which ride along in this plan's PR to `main`.
