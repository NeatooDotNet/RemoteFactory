# TRIM-003 — Verify event-record preservation needs no consumer entries

**Plan #:** 003
**Date:** 2026-07-07
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done
**Last Updated:** 2026-07-07 (closed: verification completed with a RED result — the gap is real; re-split into TRIM-007, which owns the fix, the doc corrections, and the merge of this branch)
**Plan-review opt-in:** No (verification plan, expected no-code-change to the library/generator; deliverables are a trimmed-harness repro and comment-accuracy fixes)
**Code-review opt-in:** No (test-only + doc-comment-only if verification is green; a red result triggers a re-split, not silent scope growth)

---

## Scope

Verification plan, expected no-code-change. `FactoryEventBase` has carried inherited `[FactoryEvent]` + `[DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]` since v1.4.0 (`68e7324`), which should make every derived event record trimming-safe with no `[FactoryEventHandler<T>]` and no consumer LinkerConfig entry. But the consuming evidence is ambiguous: zTreatment's LinkerConfig event entries predate v1.4.0, were carried forward during its 1.5.0 migration, and were never re-tested against the annotation — and recon confirmed the existing `EventRelaySmokeTest` cannot settle the question (it constructs its event via `new TrimTestRelayEvent(...)` and references it via `typeof(...)`, statically rooting exactly the metadata under test). Confirm with a publish-trimmed repro matching the consumer's exact shape: an event record whose ONLY client-side static reference is a generic `Subscribe<TEvent>(...)` lambda call site in a consumer-implemented `IFactoryEventRelay` aggregator — no construction, no `typeof`, no handler attribute — deserialized from the `RelayedFactoryEvent` wire shape and dispatched by runtime type. Also fixes the stale Design comments recon flagged as an internal contradiction (`FactoryEventHandlerPattern.cs` and a `FactoryEventHandlerTests.cs` doc comment still describe the removed per-handler `PreserveType` emission). Outcome either way is recorded: green → zTreatment PCB-003 deletes its event-record entries on this verification; red → the gap becomes a new TRIM plan with the repro as its failing check. Does NOT touch generator emission or library annotations.

---

## Intent

- Convert an assumed guarantee into a verified one: the todo's Acceptance Criterion 3 demands the subscribe-only consumer shape be *proven* on a trimmed client, not inferred from the annotation's existence.
- Give zTreatment PCB-003 a definitive answer on whether its event-record LinkerConfig entries can be deleted.
- Clear the recon-flagged internal contradiction: the Design source of truth must stop describing the removed per-handler `PreserveType` pipeline.

---

## Framework & Architectural Alignment

- Verification lands as a named check in the TRIM-004 harness (bool check, aggregated exit code, CI-gated) following the no-construction rule from the TRIM-001/002 gate lessons — the event type must be rooted only the way a consumer roots it.
- The consumer shape under test is the documented client-relay pattern: consumer-implemented `IFactoryEventRelay` receiving `IReadOnlyList<FactoryEventBase>` deserialized by `FactoryEventDeserializer` from `RelayedFactoryEvent` wire entries, dispatched by runtime type from a generic `Subscribe<TEvent>` registration.
- Design projects remain the requirements source of truth — the comment fixes align them with CLAUDE-DESIGN.md and `docs/trimming.md`, which recon verified are already accurate.

---

## Constraints & Invariants

- No changes to `src/RemoteFactory` or `src/Generator` — a red verification re-splits instead.
- The repro must not statically root the event record's members: no `new`, no `typeof(TrimSubscribeOnlyEvent)` outside the generic-argument position, no handler attribute. `TypeFullName` on the wire entry is a string literal.
- Existing harness checks and the full suite stay green; CI trimming gate green.
- The Design comment fixes change prose only — no test behavior, no sample code semantics.

---

## Steps

1. Add the subscribe-only repro to the harness: a `FactoryEventBase`-derived positional record referenced solely as the generic argument of a consumer-style aggregator's `Subscribe<TEvent>(handler)` call; drive a string-literal `RelayedFactoryEvent` through `FactoryEventDeserializer` and the aggregator's runtime-type dispatch; assert the typed handler fires with values intact.
2. Keyboard negative control: temporarily weaken the preservation under test (the inherited DAM annotation on `FactoryEventBase`) and confirm the check fails on the trimmed client — proving the repro is non-vacuous — then restore.
3. Fix the stale Design comments: `FactoryEventHandlerPattern.cs` (~119–137, describes the removed per-handler `PreserveType` emission and its nested-walk) and the `FactoryEventHandlerTests.cs` doc comment (~55–61, claims the generator emits `PreserveType<ShippingAddress>()`) — rewrite to the shipped `FactoryEventBase`-annotation story, consistent with CLAUDE-DESIGN.md `:791` and `docs/trimming.md`.
4. Touch the verification pointers in docs: `docs/trimming.md` and CLAUDE-DESIGN.md cite `EventRelaySmokeTest` as the end-to-end verification — add the subscribe-only check as the consumer-shape verification.
5. Record the outcome in the Discovery Log either way; green → note that zTreatment PCB-003 may delete its event-record LinkerConfig entries; red → re-split with the failing check as the new plan's starting point.

---

## Acceptance

- [x] **VERIFIED RED** — the subscribe-only shape does NOT deserialize on the publish-trimmed client: the type survives (registry resolves it) but the ctor is stripped (`NotSupportedException`, the `DeserializeNoConstructor` symptom). The check exists and is the failing regression pin TRIM-007 must turn green. `[trimmed-harness]`
- [x] Non-vacuity proven three ways: red trimmed (pure consumer shape), green untrimmed (repro logic correct), green trimmed with `[DynamicallyAccessedMembers]` on the subscribe generic parameter (fix mechanism validated). `[explicit-skip: keyboard verification triplet]`
- [x] Design comment fixes migrated to TRIM-007 (Amendment 1) — the doc delta grew from "fix stale prose" to "correct overpromising claims," which must ship with the fix per repo rules. `[explicit-skip: migrated]`
- [x] Build/suite green; the harness check is intentionally red on this unmerged branch — CI green is TRIM-007's exit condition. `[explicit-skip: deferred to TRIM-007's gate]`

---

## Current State (Pre-Flight)

Walked 2026-07-06/07 on `TRIM` (recon + TRIM-001/002 cycles):

- Annotations under test: `FactoryEventBase.cs:15-17` — `[FactoryEvent]` + `[DynamicallyAccessedMembers(PublicConstructors | PublicProperties)]` on the abstract record; `FactoryEventAttribute` is `Inherited = true` (`FactoryEventAttribute.cs:17`).
- Runtime path: `FactoryEventTypeRegistry` resolves wire `TypeFullName` via an assembly scan for `GetCustomAttribute<FactoryEventAttribute>(inherit: true)`; misses throw `UnknownFactoryEventTypeException`; `FactoryEventDeserializer.Deserialize(RelayedFactoryEvent[], serializer)` produces typed `FactoryEventBase` instances (shape used by `EventRelaySmokeTest.cs:63-75`).
- Why the existing smoke can't settle this: `EventRelaySmokeTest.cs:55` constructs `new TrimTestRelayEvent(42, ...)` and `:67` uses `typeof(TrimTestRelayEvent).FullName` — both statically root the record.
- The two trap-shapes to avoid (gate lessons): construction roots ctors (TRIM-001); retained guarded-dead bodies root ctors (TRIM-001 negative-control v1).
- Stale comments to fix: `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs:119-137` (per-handler `PreserveType<OrderShippedEvent>` / `PreserveType<ShippingAddress>` emission story + Dictionary known-gap note tied to the removed pipeline); `src/Design/Design.Tests/FactoryTests/FactoryEventHandlerTests.cs:55-61` (doc comment claiming the generator emits `PreserveType<ShippingAddress>()`).
- Correct doc anchors (already accurate, cite the smoke test): `docs/trimming.md:306` ("Any record inheriting FactoryEventBase is automatically trimming-safe... verification lives in EventRelaySmokeTest"), CLAUDE-DESIGN.md `:794`.
- Harness contract: TRIM-004 named bool checks; `Program.cs` check block; negative controls per TRIM-001/002 precedent.
- ILLink mechanics being verified: a derived record kept only via a generic instantiation (`Subscribe<TEvent>`) must receive the base type's inherited DAM member preservation — this is the annotation's designed behavior; the repro proves it empirically on net9.0 `TrimMode=full`.

---

## Test Evidence

Filled after implementation, before the Step 5 gate.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| Subscribe-only shape on trimmed client | `[trimmed-harness]` | `EventSubscribeOnlySmokeTest.Run` — **RED** (trimmed, pure consumer shape): `NotSupportedException` on `TrimSubscribeOnlyEvent` ctor, harness exit 1 | ✓ (as a finding) |
| Non-vacuity | `[explicit-skip]` | Untrimmed `dotnet run -c Release`: PASSED, exit 0. Trimmed with DAM on `Subscribe<TEvent>`: PASSED, exit 0. Contrast isolates the cause to ILLink + the missing call-site annotation. | ✓ |

---

## Plan Amendments

### 2026-07-07 — Verification RED; Steps 3–4 migrated to TRIM-007

- **Section affected:** Steps 3–4 (doc/comment fixes), Acceptance
- **Original said:** expected no-code-change green verification; this plan would fix the stale Design comments and touch the doc verification pointers.
- **What changed:** the verification came back RED — the inherited DAM on `FactoryEventBase` does not preserve derived members under ILLink (`DynamicallyAccessedMembersAttribute` is `AttributeUsage(Inherited = false)`; the runtime `inherit: true` attribute scan works, which is why the *type* resolves while its *ctor* is stripped). The doc problem is therefore bigger than stale prose: `docs/trimming.md` / CLAUDE-DESIGN.md / `FactoryEventBase.cs` overpromise automatic trimming safety. All doc/comment work migrated to TRIM-007 so it ships with whichever fix is chosen (repo rule: doc deltas ship with the behavior they describe). The fix mechanism was validated at the keyboard: DAM on the subscribe method's generic parameter → green.
- **Why:** the plan's own Scope pre-agreed this path: "red → the gap becomes a new TRIM plan with the repro as its failing test."
- **Discovery Log link:** 2026-07-07 — TRIM-003 (verification RED, re-split → TRIM-007).

---

## Notes

- Nested-record-in-event properties are *documented* as a manual preservation case (`docs/trimming.md` "Nested Reference Types in Event Records") — deliberately NOT re-scoped here; the repro pins the consumer's actual failing shape (flat event records). If the user later wants framework-walked event property graphs, that's a new plan.
- The negative control (Step 2) temporarily edits library source (`FactoryEventBase` annotation) — revert discipline per the TRIM-001 renderer precedent.
