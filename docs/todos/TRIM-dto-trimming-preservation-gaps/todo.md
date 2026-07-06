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
| 004 | Draft | [Trimming harness pass/fail semantics + CI gate](./plans/004-trimming-harness-ci-gate.md) | 2026-07-06 recon: TrimmingTests outside .sln/CI, exits 0 on failure — 001–003's trimmed acceptance signals need this gate first |
| 001 | Draft | [Positional-record preservation in factory signatures](./plans/001-positional-record-signature-preservation.md) | `DtoTypeWalker.WalkFactoryReturn` `HasParameterlessCtor` gate; zTreatment cut-over `StartVisitResultV2` hotfix |
| 002 | Draft | [`[Factory]` entity property-graph DTO discovery](./plans/002-factory-entity-property-dto-discovery.md) | `WalkFactoryReturn` bails on `[Factory]` roots without descending; zTreatment `TreatmentBanner` / `DashboardContactResult` hotfixes |
| 003 | Draft | [Verify event-record preservation needs no consumer entries](./plans/003-verify-event-record-preservation.md) | `FactoryEventBase` DAM annotation shipped v1.4.0; consumer entries predate it, never re-tested |

Execution order: 004 → 001 → 002 → 003 (rows listed in execution order; numbering stays monotonic by creation). Branching: todo/plan docs commit on the `TRIM` branch; each plan's implementation gets its own branch off `TRIM`.

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
