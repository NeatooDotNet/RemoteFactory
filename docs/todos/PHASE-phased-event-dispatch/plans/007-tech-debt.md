# Test Harness Debt, Emission Pins, and the Coordinator's Silent Short-Circuit

**Plan #:** 007
**Date:** 2026-08-18
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-18
**Plan-review opt-in:** No (test infrastructure and pins; the one behavior addition is a Debug log event; blast radius bounded by the sacred-tests rule)
**Code-review opt-in:** Yes (touches sacred harness files broadly; one runtime addition; a possible scheduler storage tweak)

---

## Scope

Pay down the debt every PHASE gate routed here: pin the phase log events that ship
unasserted (9002/9004/9006, plus 9002's collapsed count), pin the accepted-but-undocumented
behaviors (undefined-phase silent no-op, `Enqueue` null-handler guard, NF0505's attribute
location, coalescing's runtime inertness on an unqueued path, a production-shaped
warn-merge variant), give the coordinator's silent short-circuit a Debug event id so a
consumer draining outside an entry call gets a signal instead of a 9007 telling them to do
what they just did, add the first Design.Server test (a composition check that its
container can resolve every `[Service]` parameter type Design.Domain's factory methods
declare) and replace the `Assert.True(true)` trio, consolidate the duplicated test
harnesses (relay wait helper, `ScopesWithLogging`, tuple-order divergence,
`CapturingLoggerProvider` snapshot accessor, `IEventTestService`/registry isolation
discipline — written down, whichever way each lands), defuse the `SingleEventRelay` 2s
poll flake, restore O(1) dequeue for the scheduler's default path (or formally accept the
List cost), and record the `Design.sln` build requirement where verification instructions
live. This plan does NOT touch the generator's emission (PHASE-008), does NOT build the
scheduler concurrency harness (re-split to PHASE-009), and changes no consumer-facing
contract beyond the one new Debug log id.

---

## Inherited (routed here by the 001–006 gates — provenance per item)

- 9002/9004/9006 positive emission pins; harness exists since 004 (`CapturingLoggerProvider`). *(003 gate)*
- 9002's drained count under collapse. *(006 gate)*
- Undefined-phase silent no-op documenting pin. *(002 discovery, accepted; 004 routing)*
- `Enqueue` null-handler guard pin. *(004 gate)*
- NF0505 location pin — it deliberately points at the attribute, diverging from the class-identifier convention, and the better choice is unprotected. *(006 gate)*
- Coalescing runtime-inertness pin on an unqueued path (XML claims it; only emission is pinned). *(006 gate)*
- Production-shaped warn-merge variant — the shipped pin drives `Enqueue(Immediate, …)`, which the dispatcher never produces. *(006 code review C5)*
- Coordinator short-circuit observability — new Debug event id. *(004 code review / discovery)*
- Design.Server composition test — it registers 3 services while Design.Domain `[Service]`-injects `INotificationService` and `IPhaseAuditService`; no Design.Server test exists. *(005 gate)*
- `FactoryEventHandlerTests` `Assert.True(true)` trio — after 005's rescope it is the Design tier's nominal `Immediate` pin and asserts nothing. *(005 gate)*
- Registry test-isolation: `Clear()` internal and uncalled; every test invents unique event types; 006 added five more. *(001 routing; 006 dependents note)*
- `ClientServerContainers` tuple-order divergence + `ScopesWithLogging` duplication + cross-container log attribution; another divergent call site in 006's relay-pin helper. *(003 gate; 006 note)*
- `IEventTestService` shared-singleton Guid-filter discipline. *(004 routing)*
- `CapturingLoggerProvider.Entries` snapshot accessor; 006 added three more pins on the raw list. *(004 routing; 006 note)*
- `SingleEventRelay` hard 2s poll flaking under full-parallel runs; 006 duplicated the harness. *(004 discovery; 006 note)*
- Scheduler storage shape — O(n) front-dequeue paid by the non-opted-in path on an unenforced smallness assumption. *(006 code review C3)*
- Explicit `Design.sln` build in verification docs — the main solution omits it. *(005 RP-0)*

---

## Intent

- Every phase log event id (9001–9008, plus the new short-circuit id) has at least one
  positive emission pin, so a wording or wiring regression is caught by a test rather
  than a consumer.
- Accepted behaviors stop being oral tradition: each "we decided not to change this"
  from the arc's gates gets a documenting pin that fails if the behavior drifts silently.
- A consumer whose transaction abstraction wraps the factory call from outside gets a
  Debug breadcrumb at the moment their drain did nothing, instead of only a 9007 later
  telling them to do what they just did.
- Design.Server stops being the one project with zero tests while its container silently
  cannot serve the domain it hosts.
- The test harnesses this arc leaned on (and duplicated under deadline) become single,
  documented seams; the isolation disciplines the suite depends on are written where the
  next test author will see them.
- The full suite stops having a known flake, and the scheduler's default path stops
  paying for a feature it doesn't use — or the acceptance is recorded formally.

---

## Framework & Architectural Alignment

- **Existing tests are sacred:** harness consolidation preserves every existing
  assertion's intent; mechanical call-site alignment only where names/positions change.
- **Log-event conventions (this arc's):** new id continues the 9xxx block; `LoggerMessage`
  partial in `Internal/Log.cs`; documented in the three log tables (CLAUDE-DESIGN,
  docs/factory-events.md, skill) in the same change.
- **Red-proof discipline:** documenting pins state what makes them discriminating; any
  claim of "would go red" is measured or labeled derived.
- **`Internal` namespace policy:** any scheduler storage change keeps the public/unsealed
  shape and the pinned semantics (FIFO, counts, warn-merge).
- **Skill self-containment** for any skill-table row added.

---

## Constraints & Invariants

- No consumer-facing contract change except the one new Debug log event (additive).
- The 729/591/94 suites stay green with existing assertions intact; totals only grow.
- The coordinator's short-circuit semantics do not change — only its observability.
- The scheduler's pinned semantics (FIFO order, collapsed counts, warn-merge, discard)
  are invariant under any storage change; the 006 pins are the proof.
- Design.Server gains a test project reference only if the composition test needs it —
  no production restructuring of the sample server.
- Harness changes may move/rename test-infrastructure code but never weaken an assertion;
  where a divergence is documented rather than fixed, the doc lives in the code.

---

## Steps

1. **Emission pins:** positive unit pins for 9002 (incl. the collapsed drained count),
   9004, and 9006 message/level/shape, using the capture harness — with the snapshot
   accessor added first so these pins don't grow the raw-list dependency.
2. **Documenting pins:** undefined-phase silent no-op; `Enqueue` null-handler guard;
   NF0505's attribute location; runtime inertness of the coalesce flag on an unqueued
   path; the production-shaped warn-merge variant (AfterFlush trigger, not
   `Enqueue(Immediate, …)`).
3. **Coordinator short-circuit observability:** new Debug event in `Log.cs` emitted when
   `DrainAsync(AfterFlush)` returns without draining because no entry call is active;
   pinned (fires there, silent when a drain proceeds); the three log tables gain its row.
4. **Design.Server composition test:** resolve every `[Service]` parameter type declared
   by Design.Domain factory methods from Design.Server's registered collection; fix the
   two known missing registrations it will catch (`INotificationService`,
   `IPhaseAuditService`) in Design.Server's Program.
5. **`Assert.True(true)` trio:** real assertions preserving each demonstration's intent
   (dispatch-to-all-handlers observed through a recording seam).
6. **Harness consolidation:** one relay-wait helper; `ScopesWithLogging` and the
   coalescing relay helper folded to single seams; tuple-order divergence either aligned
   or documented at the declaration; `IEventTestService` and registry-isolation
   disciplines written at their seams (isolate-vs-document decided at the keyboard).
7. **Flake defusal:** the `SingleEventRelay` poll made deterministic or its timeout made
   proportionate to parallel load; recorded either way.
8. **Scheduler default-path dequeue:** restore O(1) head removal (cursor or equivalent)
   with the 006 pins proving semantics unchanged, or record formal acceptance in the
   storage comment and the todo.
9. **Docs:** `Design.sln` build added to the verification commands in CLAUDE.md (and
   anywhere else build instructions live); the new log-event row in all three tables.
10. **Gate:** Test Evidence with expected totals, single build+test run to logs (both
    solutions), test-reviewer; code review (opted in).

---

## Acceptance

- [ ] 9002, 9004, and 9006 each have a positive unit pin (message, level, and the
      count where the message carries one), and 9002's count reflects a collapsed
      queue. `[unit]`
- [ ] The accepted behaviors are pinned as documented: undefined-phase registration
      never drains (silent no-op), `Enqueue` null-handler throws, NF0505 locates at
      the attribute, the coalesce flag is runtime-inert on an unqueued dispatch path,
      and the warn-merge holds in a production-shaped ordering. `[unit]`
- [ ] The coordinator short-circuit emits the new Debug event exactly when it returns
      without draining, and not when a drain proceeds; the id appears in all three
      log tables. `[unit]` (tables `[explicit-skip: prose]`)
- [ ] A Design.Server composition test resolves every `[Service]` parameter type of
      Design.Domain's factory methods; it fails before the Program registrations are
      fixed and passes after (measured, not asserted). `[integration]`
- [ ] The three `FactoryEventHandlerTests` demonstrations assert observable dispatch
      behavior; their original intent statements are preserved. `[integration]`
- [ ] One relay-wait helper and one logging-scopes helper exist; the divergences that
      stay are documented at their declarations. `[explicit-skip: harness refactor —
      verified by the suite staying green and by review]`
- [ ] A full-parallel (default `-m`) solution test run passes with the relay tests
      included. `[explicit-skip: run evidence in the gate log, noted alongside the
      -m:1 run]`
- [ ] The scheduler's default-path dequeue decision is executed or formally recorded;
      either way the 006 coalescing/ordering/count pins are untouched and green.
      `[unit]` (acceptance-of-record variant `[explicit-skip: recorded decision]`)
- [ ] Full suites green both solutions, expected totals reconciled. `[explicit-skip:
      meta-bullet, satisfied by the gate run]`

---

## Current State (Pre-Flight)

*(filled at Step 3 of the workflow, before the first edit)*

---

## Test Evidence

*(filled after implementation, before the gate)*

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| | | | |

---

## Plan Amendments

*(none yet)*

---

## Notes

- **Branch stack:** `PHASE-007-tech-debt` is stacked on `PHASE-006-coalescing` (PR #84,
  open at branch time) — several items pin 006's code. Merge order: #84, then this plan.
- **Re-split executed at drafting:** the scheduler concurrency harness (zero coverage
  against its own shared-scope contract; both 006 reviewers recommended a dedicated
  deterministic harness, not a `Task.WhenAll` race) is **PHASE-009**, stubbed in this
  drafting commit. It is deliberately not in this plan's Scope.
- The new Debug id will be 9009 if free at pre-flight (Log.cs topped at 9008 when 006
  closed).
- PHASE-008 (generator emission hygiene) still owns everything about emitted-token
  qualification and generator-test harness defects — nothing here overlaps.
