# TRIM-001 Plan Review — 2026-07-06

**Reviewer:** plan-reviewer agent (two-pass: A = documented requirements, B = codebase)
**Verdict: APPROVED** — no veto-tier findings in either pass. Five callout-tier findings, all folded into the draft before implementation (see "Disposition").

---

## Pass A — vs. documented requirements

Docs consulted: `src/Design/CLAUDE-DESIGN.md` (FAQ ~295; DTO-registry section ~768-788), `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs` (~452-463), `docs/trimming.md` (243-306).

- **No veto findings.** The only documented-behavior change (record exclusion wording at `docs/trimming.md:267`, `CLAUDE-DESIGN.md:780`) is exactly the delta the parent todo's Goal and Acceptance Criterion 1 sanction, and Step 6 targets those sites.
- `CLAUDE-DESIGN.md:772` ("not in DI and not in the registry → `CreateObject` not set") stays accurate: `PreserveType<T>()` deliberately does not populate `TryCreate`; records are claimed by `RecordBypassConverterFactory` first.
- Callout A1: after this plan, `PreserveType<T>` is emitted again (by the factory-signature path) — Step 6 doc edits must leave no sentence implying it is emitted nowhere. `docs/trimming.md:300` (event pipeline removal) itself stays accurate — distinct path.
- Callout A2: the stale `FactoryEventHandlerPattern.cs` comments describing the *removed event-path* emission are already routed to TRIM-003 — keep the two doc deltas from colliding.

## Pass B — vs. codebase

Reality check passed: the gate (`DtoTypeWalker.cs:145`), dead `WalkEventRoot` (173-231, stale header 3-4), unemitted `PreserveType` (`DtoConstructorRegistry.cs:43`), and the three-renderer seam enumeration were all confirmed complete (no other `DtoReturnTypes` consumer exists).

- **B1 (key):** The incremental-cache boundary is the transform output `TypeInfo` (`FactoryGenerator.cs:19-31/54-64`) — `FactoryModelBuilder.Build` runs inside `RegisterSourceOutput`, so the three factory *models* are not cache keys. Thread the second bucket as `EquatableArray<string>` on `TypeInfo` (`Types.cs:71`) and `MethodInfo`/`TypeFactoryMethodInfo` (646/521); a plain `List`/`IReadOnlyList` field there would silently break incremental caching with **no failing test**.
- **B2 (key):** Do not copy `WalkEventRoot`'s root semantics — it forces the root into the PreserveType bucket regardless of ctor shape (`DtoTypeWalker.cs:196-197`, correct for event roots only). The factory-return root must bucket **by ctor shape**; a literal port would degrade parameterless class DTO returns to the reflection path under trimming. Only the *nested* bucket-sort (220-227) is the reusable shape.
- **B3:** `record struct` is the one shape where bucket rule and runtime detection diverge: Roslyn reports the synthesized parameterless ctor (→ Register bucket) but reflection `GetConstructors()` omits it (→ `RecordBypassConverterFactory` claims it). Benign (Register also carries DAM-All; bypass round-trips structs), but the "detection rule matches the bucket rule" parity claim isn't exact.
- **B4:** `NestedDtoDiscoveryTests.GetRegisteredDtoTypes` regex (`:24-26`) counts `Register<>` only; TS-010 (`:416`) and TS-014 (`:526`) count-assertions stay Register-only and remain green (fixtures are parameterless class DTOs). Anchor the PreserveType twin to `PreserveType<(.+?)>\(\)`.
- **B5:** Removing the ctor gate means property descent now enters record graphs — existing untrimmed targets (`InterfaceFactoryRecordTargets.cs`) will gain new `PreserveType<>` lines in their registrars. Additive, idempotent, intended; no test asserts their absence.

Infrastructure sweep: no snapshot/golden tests on generated text; `CombinationTestGenerator` unaffected; `init`-only properties are walked (`GetMethod != null`, `DtoTypeWalker.cs:248`); record `EqualityContract` filtered by the `Public` check (245); FQN rendering path identical to today's `Register<>` (nullable modifiers already stripped).

## Recommendations → Disposition

| # | Finding | Disposition |
|---|---------|-------------|
| B1 | Equatability lives on `TypeInfo`/`MethodInfo`, not models | Plan Framework Alignment + Step 2 corrected |
| B2 | Root buckets by ctor shape; only nested walk reusable | Plan Scope + Step 1 corrected |
| B3 | `record struct` bucket/detection divergence | Parity claim softened in Framework Alignment; keyboard note kept |
| B4 | Regex anchoring; TS-010/TS-014 stay Register-only | Added to plan Notes |
| A1 | No doc sentence may imply PreserveType unemitted | Added to plan Notes (Step 6 checklist) |

Calibration note per workflow: diagnoses adopted; prescriptions treated as advisory (all five were adopted as-is — they matched the code walk).
