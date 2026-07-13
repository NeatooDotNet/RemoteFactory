# TRIM — DTO Trimming Preservation Gaps

**ID:** TRIM
**Type:** Generator defect / trimming-support completion
**Status:** In Progress
**Priority:** High (blocks a consumer from retiring a ~50-entry LinkerConfig.xml workaround)
**Created:** 2026-07-06

---

## Goal

Close the remaining IL-trimming preservation gaps in the source generator's DTO discovery so that a consumer's Blazor WASM client needs **no manual `LinkerConfig.xml` entries** for types that flow through RemoteFactory. Driving evidence: zTreatment's 2026-06-28 production cut-over hit repeated `DeserializeNoConstructor` / stripped-property failures on its trimmed client and accumulated a bulk-preserve block of ~50 `<type preserve="all" />` entries (`zTreatment.BlazorStandAlone/LinkerConfig.xml`) as a whack-a-mole mitigation. The consumer-side retirement of that block is tracked in zTreatment as **PCB-003**; this todo is the framework side.

The two confirmed gaps (verified by reading the generator at v1.6.1 = current HEAD):

1. **Positional records in factory method signatures are silently dropped.** `MethodInfo.DiscoverDtoTypes` walks return types AND non-service parameters, but delegates to `DtoTypeWalker.WalkFactoryReturn`, which requires `HasParameterlessCtor` — any record with only a parameterized ctor is skipped with no preservation at all. `DtoConstructorRegistry.PreserveType<T>()` exists for exactly this shape but is emitted nowhere (its only caller, `WalkEventRoot`, is dead code since the v1.4.0 event-relay redesign). Deserialization-side support (`RecordBypassConverterFactory`) is present; preservation-side emission is missing.
2. **DTO-typed properties on `[Factory]` entities are never discovered.** `WalkFactoryReturn` bails on `[Factory]`-annotated roots *without descending into their properties*, so a plain DTO reachable only as an entity property (e.g. a record carried by an `[Execute]`-opened aggregate) gets trimmed on the client.

A third suspected gap turned out to be already fixed: event records derive `FactoryEventBase`, which has carried inherited `[FactoryEvent]` + `[DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]` since v1.4.0 (commit `68e7324`) — no handler required. The consumer's event-record LinkerConfig entries predate v1.4.0 and were carried forward untested; TRIM-003 verifies this with a trimmed repro rather than assuming it.

## Acceptance Criteria

1. A positional-record DTO appearing in a remote factory method signature — as return type, as parameter, or nested as a property of another discovered DTO — deserializes on a publish-trimmed client with no consumer LinkerConfig entry. [TRIM-001]
2. A plain DTO reachable only as a public property of a `[Factory]` entity survives publish-trimming and deserializes on the client with no consumer LinkerConfig entry. [TRIM-002]
3. Verified (not assumed): a `FactoryEventBase`-derived record whose only client-side reference is a subscription-lambda call site deserializes on a publish-trimmed client. [TRIM-003]
4. `docs/trimming.md` ("What Qualifies as a DTO", "DTO Return Type Preservation") updated to match the shipped behavior; release notes per CI/CD standards.
5. Consumer proof: released version consumed by zTreatment (PCB-003) with the LinkerConfig bulk-preserve block deleted and a Release WASM publish verified. (Tracked zTreatment-side; this todo closes on the framework release, not the consumer rollout.)

## Out of Scope

- zTreatment's upgrade / LinkerConfig retirement / smoke verification — that is zTreatment PCB-003.
- The `IFactorySaveMeta` visibility-narrowing interaction (documented in `docs/trimming.md`; separate concern).
- DTOs that never flow through RemoteFactory (consumer's own HTTP/JSON paths) — those remain the consumer's responsibility, documented as such.
- The v1.6.1 `[Execute]`-only DI-registration fix — already shipped.

## Plan Index

(Stubs carry Scope only; Steps/Acceptance flesh out at each plan's turn, per the iterative-todo workflow.)

| #   | Status | Plan | Source |
|-----|--------|------|--------|
| 004 | Done | [Trimming harness pass/fail semantics + CI gate](./plans/004-trimming-harness-ci-gate.md) | 2026-07-06 recon: TrimmingTests outside .sln/CI, exits 0 on failure — 001–003's trimmed acceptance signals need this gate first |
| 001 | Done | [Positional-record preservation in factory signatures](./plans/001-positional-record-signature-preservation.md) | `DtoTypeWalker.WalkFactoryReturn` `HasParameterlessCtor` gate; zTreatment cut-over `StartVisitResultV2` hotfix |
| 002 | Done | [`[Factory]` entity property-graph DTO discovery](./plans/002-factory-entity-property-dto-discovery.md) | `WalkFactoryReturn` bails on `[Factory]` roots without descending; zTreatment `TreatmentBanner` / `DashboardContactResult` hotfixes |
| 003 | Done | [Verify event-record preservation needs no consumer entries](./plans/003-verify-event-record-preservation.md) | `FactoryEventBase` DAM annotation shipped v1.4.0; consumer entries predate it, never re-tested — **verification came back RED**, re-split → TRIM-007 |
| 007 | Done | [Subscribe-only event preservation fix](./plans/007-subscribe-only-event-preservation-fix.md) | TRIM-003 finding: inherited DAM doesn't flow to derived types under ILLink; fixed via generator-emitted per-assembly event-preservation registrar |
| 005 | Draft | [Server-only reference over-retention in trimmed clients](./plans/005-server-only-reference-over-retention.md) | TRIM-004 discovery: guarded-dead `LocalCreate` bodies retain server-only interface refs, contradicting `docs/trimming.md` |
| 006 | Draft | [Incremental-generator caching regression test](./plans/006-incremental-cache-regression-test.md) | TRIM-001 gate: no test asserts cached pipeline steps — non-EquatableArray transform fields regress silently (plan review B1) |

Execution order: 004 → 001 → 002 → 003 → 007 → 005 → 006 (rows listed in execution order; numbering stays monotonic by creation). Branching: todo/plan docs commit on the `TRIM` branch; each plan's implementation gets its own branch off `TRIM`. (TRIM-003's red verification and TRIM-007's fix merged together via PR #71.)

## Skipped Steps

- TRIM-004 — `test-reviewer` gate skipped (test-infrastructure-only plan: every Acceptance bullet is `explicit-skip`; the harness itself is the test artifact, evidence recorded in the plan's Test Evidence table).
- TRIM-003 — `test-reviewer` gate skipped (verification-only plan whose deliverable is the finding itself; non-vacuity proven by the red-trimmed / green-untrimmed / green-annotated triplet recorded in the plan's Test Evidence; the repro check lands under TRIM-007's full gate).

## Discovery Log

### 2026-07-06 — Todo created from zTreatment PCB-003 reconnaissance
- **Finding:** zTreatment's cut-over LinkerConfig block traces to two real generator gaps and one already-fixed one. (1) `DiscoverDtoTypes` walks parameters and returns, but `WalkFactoryReturn` drops any type without a public parameterless ctor — positional records get no `PreserveType` emission because the only `PreserveType` caller (`WalkEventRoot`) has been dead since the v1.4.0 relay redesign. (2) `[Factory]` roots are rejected without walking their properties, so entity-carried DTOs are undiscoverable. (3) Event records have been annotation-preserved since v1.4.0 (`68e7324`); the consumer's event entries predate that and were carried forward untested.
- **Decision:** three plans — fix the record bucket (001), add entity property descent (002), verify events with a trimmed repro instead of assuming (003). Consumer side tracked as zTreatment PCB-003 (full-vertical scope confirmed by user 2026-07-06).
- **Index changes:** initial split, 001–003.
- **Follow-up:** target release consumed by zTreatment PCB-003.

### 2026-07-06 — TRIM re-split after in-repo recon (TRIM branch)
- **Finding:** Both gaps confirmed in code (`WalkFactoryReturn` ctor gate at `DtoTypeWalker.cs:145`; `[Factory]` roots rejected before any property descent; `PreserveType` emitted nowhere — the parameterized bucket lost its renderer in the v1.4.0 relay-codegen removal, and `WalkEventRoot`'s header comment claiming a `FactoryGenerator.RelayHandler` caller is stale). New findings the outside-perspective split missed: (1) `RemoteFactory.TrimmingTests` is outside `Neatoo.RemoteFactory.sln` and CI, and exits 0 even when its smoke tests print FAILED — TRIM-001/002/003's publish-trimmed acceptance signals have no enforceable gate. (2) `EventRelaySmokeTest` constructs its event via `new TrimTestRelayEvent(...)`, statically rooting the ctor — it cannot pin TRIM-003's subscribe-only consumer shape, so that repro is confirmed necessary. (3) `Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs` (~119–137) and a `FactoryEventHandlerTests.cs` doc comment still describe the removed per-handler `PreserveType` emission — internal contradiction with CLAUDE-DESIGN.md/docs/trimming.md; callout carried to TRIM-003's doc delta.
- **Decision:** Re-split.
- **Index changes:** add TRIM-004 (harness pass/fail semantics + CI gate), executed first; 001–003 unchanged.
- **Follow-up:** TRIM-004.

### 2026-07-06 — TRIM-004 (harness rot)
- **Finding:** The harness hadn't compiled since v1.5.0 — `TrimTestCommands` still used the `[Event]` method attribute deleted in `eec581c`; on-disk publish artifacts were stale pre-v1.5 builds. With exit codes enforced, the class-factory check then surfaced two harness bugs it had been silently failing on: root-provider resolution of a scoped factory, and the missing consumer-side keyed `HttpClient` registration Remote mode requires. All fixed in-harness; `Class factory resolved: True` now holds on a trimmed run for the first time. Long form: TRIM-004 Plan Amendments 1–2.
- **Decision:** Amend.
- **Follow-up:** n/a.

### 2026-07-06 — TRIM-001 (unrelated flaky test observed at gate)
- **Finding:** `RelayTimingTests.Relay_FiresAfterCallerSynchronousWriteOnContinuation` (integration, event relay) failed with `TimeoutException` on net9.0 under full-suite parallel load, passed in isolation and on the next full run. Unrelated to TRIM's generator changes — timing-sensitive test.
- **Decision:** Defer.
- **Follow-up:** flagged to user — out-of-goal tech debt; queue as sibling todo or accept as known flake (not queued in TRIM).
- **Resolution (2026-07-06):** flaked again on PR #69's first CI run (green on re-run). User decision: test marked `[Fact(Skip = ...)]`, no todo. Its sibling `Relay_FiresAfterCallerContinuation_InNoSyncContextHost` flaked with the identical signature during the TRIM-002 gate run (green in isolation) — same decision applied, also skipped.

### 2026-07-06 — TRIM-001 (gate closed)
- **Finding:** Test-review gate returned zero must-cover gaps but caught false trimmed-harness coverage: the constructed-body harness design let the return/nested checks pass with the emission disabled (guarded-dead bodies root ctors — the TRIM-005 behavior). Harness redesigned so no record is ever constructed; negative controls v1+v2 now prove each shape depends on `PreserveType`. Added `record struct` + cross-method dedupe unit tests from the should-cover tier. Long form: TRIM-001 Plan Amendment + `reviews/001-test-review.md`.
- **Decision:** Amend.
- **Index changes:** add TRIM-006 (incremental-cache regression test — pre-existing tech debt, plan review B1), executed last.
- **Follow-up:** TRIM-006.

### 2026-07-13 — TRIM-007 (gates cleared, merged)
- **Finding:** Generator-emission fix landed (PR #71, CI green first run): fourth pipeline branch + per-assembly `NeatooEventPreservationRegistrar`; TRIM-003's red check green in the pure consumer shape incl. nested record. Plan review's veto (accessibility gate — the repo's own private nested test events would have broken every consumer build) folded pre-implementation. Test gate cleared (10 unit tests incl. determinism + FQN-decoy guards). Code review caught 3 veto doc findings — the falsified DAM claim surviving in `FactoryEventRelayPattern.cs`, `docs/factory-events.md`, and the smoke test's own summary — all fixed, plus skill-reference and IL2026-justification callouts. Reviews: `reviews/007-*.md`.
- **Decision:** Amend.
- **Follow-up:** n/a — remaining queue: TRIM-005, TRIM-006, then todo-level release step (AC4 release notes deferred there).

### 2026-07-07 — TRIM-003 (verification RED, re-split → TRIM-007)
- **Finding:** The subscribe-only consumer shape FAILS on a trimmed client: the event type survives (generic instantiation + runtime `[FactoryEvent]` scan) but its ctor is stripped — inherited `[DynamicallyAccessedMembers]` on `FactoryEventBase` does not flow to derived types under ILLink (DAM is `AttributeUsage(Inherited = false)`; the docs' "Inherited = true" story is runtime-reflection semantics). The todo's "third gap already fixed" premise was wrong; zTreatment's event LinkerConfig entries are load-bearing. Triplet evidence: red trimmed / green untrimmed / green trimmed with DAM on `Subscribe<TEvent>` (fix mechanism validated — the `Raise<T>` producer-side pattern). Long form: TRIM-003 Amendment 1.
- **Decision:** Re-split.
- **Index changes:** add TRIM-007 (fix + the now-larger doc corrections, migrated from 003's Steps 3–4), executed next; 003 Done as a red verification; its branch stays unmerged until 007 goes green.
- **Follow-up:** TRIM-007 — fix direction (consumer-annotation docs vs generator `PreserveType`-per-descendant vs both) is a user decision, pending.

### 2026-07-06 — TRIM-002 (gate closed)
- **Finding:** Test gate CLEARED with zero must-cover; two should-covers (base-class property, `[Factory]` record self-walk) and three nice-to-haves closed with tests; harness run log captured. New visibility item: the FactoryEventRelay integration family is parallel-load flaky *beyond* the two skipped members (different members flake per run; all green isolated and with `MaxParallelThreads=1`) — user previously declined queueing, recorded here for the close-out audit. Long form: `reviews/002-test-review.md`.
- **Decision:** Amend.
- **Follow-up:** n/a.

### 2026-07-06 — TRIM-001 (code review clean)
- **Finding:** Opt-in code review returned zero veto findings (B1/B2 compliance, emission placement, semantics, docs all verified — `reviews/001-code-review.md`). One low-confidence pre-existing callout: `IsDtoStructureCandidate`'s `StartsWith("System")` prefix match would exclude a consumer namespace like `Systems.Domain` from preservation.
- **Decision:** Amend.
- **Index changes:** none — the hardening is folded into TRIM-002's stub scope (that plan already reworks the candidate checks at the same seam).
- **Follow-up:** TRIM-002.

### 2026-07-06 — TRIM-004 (server-only over-retention)
- **Finding:** A trimmed client retains the `IServerOnlyRepository` TypeDef and `DoServerWork` member ref: generated `LocalCreate` bodies are rooted by delegate registration and their early-`throw` guard + `try/catch` defeats ILLink unreachable-code elimination. Implementations are correctly trimmed. Contradicts `docs/trimming.md` "should return no matches" / "dead code is removed" claims. TRIM-004's CI grep narrowed to implementation types (Plan Amendment 3).
- **Decision:** Defer.
- **Index changes:** add TRIM-005 (over-retention: generator guard-shape fix or docs correction), executed last.
- **Follow-up:** TRIM-005.
