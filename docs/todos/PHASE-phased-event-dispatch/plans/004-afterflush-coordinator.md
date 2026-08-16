# AfterFlush: Consumer-Signaled Drain via IFactoryEventPhaseCoordinator

**Plan #:** 004
**Date:** 2026-08-14
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done
**Last Updated:** 2026-08-16
**Plan-review opt-in:** Yes (new public interface; contract with consumer transaction abstractions)
**Code-review opt-in:** Yes (behavior-changing)

---

## Scope

Expose the public `IFactoryEventPhaseCoordinator` with `DrainAsync(DispatchPhase, ct)` so a
consumer's transaction abstraction can drain the `AfterFlush` queue between its outermost
flush and commit, and implement the fail-open fallback: `AfterFlush` handlers never drained
by the consumer run at the `AfterCommit` point with a logged warning. Includes the
cross-phase ordering guarantee tests (all `Immediate` before any `AfterFlush` before any
`AfterCommit`). This plan does NOT add transaction awareness to the framework — the
coordinator is a drain trigger, nothing more.

---

## Inherited from PHASE-001 (recorded at its Step 5 gate)

- **The fail-open sweep is already implemented and test-pinned.** `DrainAsync(phase, …)`
  drains the requested phase *and every earlier one*, earliest first, so `AfterFlush`
  handlers a consumer never drained are swept up at the `AfterCommit` point under that
  drain point's swallow semantics (`FactoryEventPhaseSchedulerTests.DrainAsync_SweepsAnEarlierPhaseTheConsumerNeverDrained`).
  What is missing is only the **logged warning** the todo's AC-5 requires — and the
  discriminator is already plumbed: `TryDequeueThrough` returns the phase each dispatch was
  queued at (code review C3). Wire the warning; do not re-plumb the sweep.
- `IFactoryEventPhaseCoordinator.DrainAsync(AfterFlush)` should call through to the
  scheduler with `inTransaction: true` so handler exceptions propagate and the consumer's
  transaction can still roll back.

---

## Inherited from PHASE-003 (recorded at its Step 5 gates)

- **Open question this plan owns (code review C2):** a drained handler's
  `OperationCanceledException` at the *AfterCommit entry drain* propagates and fails a
  call that already succeeded — chartered by the todo's AC ("OCE still propagates") but
  in tension with the no-token entry-drain policy. Decide here, as the owner of the
  consumer-facing drain surface, whether post-completion drains should swallow OCE too
  (and if so, restate the AC as a planned amendment).
- The entry drain passes `CancellationToken.None`; the coordinator's `AfterFlush` drain
  is in-transaction and consumer-invoked, so it takes the consumer's token — the two
  drain points deliberately differ.

---

## Inherited from PHASE-002 (recorded at its Step 5 gates)

- **`AfterFlush` is already consumer-reachable, and already fail-opens — silently.** PHASE-002
  made the attribute's phase argument real, so `[FactoryEventHandler<T>(DispatchPhase.AfterFlush)]`
  works today and the scheduler's sweep drains it at the AfterCommit entry point. What is
  missing is only AC-5's logged warning. Acceptance here must cover an **attribute-declared**
  AfterFlush handler, not only one registered through the registry's 3-arg overload — the
  consumer-facing path is otherwise untested end to end.
- NF0504 (Warning) now fires when one class declares the same event type twice, and the
  duplicate's entry is skipped. If this plan widens the registry's `(event type, handler class
  type)` dedupe key for any reason, that diagnostic's premise changes and it must be revisited.

---

## Intent

- A consumer's transaction abstraction gets a first-class, public drain trigger for
  `AfterFlush` work — the last missing piece of the phase feature's consumer surface. The
  consumer pattern is a factory method (or the abstraction wrapping it) that flushes,
  drains via the coordinator, then commits; handlers see flushed state while the
  transaction can still roll back.
- The fail-open path becomes honest. Since PHASE-002, never-drained `AfterFlush` work is
  silently swept at the `AfterCommit` point; after this plan the sweep announces itself
  with a dedicated warning naming the event type. The warning covers every `AfterFlush`
  dispatch that reaches the post-completion sweep — whether the consumer wired no drain at
  all or raised the work after their drain had already run; both are AC-5's letter
  ("never drained by the consumer") and both are actionable from the log line. The one
  silent case is the documented carve-out: work *created during* that sweep by a
  later-phase handler had no drain point left to miss.
- This plan settles the PHASE-003 C2 question it owns. **Intended decision:** at
  post-completion drain points, only *genuine cooperative cancellation* (a live, cancelled
  token) propagates; a handler-internal `OperationCanceledException` is logged and
  swallowed like any other post-completion failure, and the dispatches queued behind it
  still run. Rationale: the entry drain deliberately passes no token, so an OCE reaching it
  can never mean "the caller cancelled" — letting it fail an already-committed call and
  discard the remaining queue is exactly the failure AC-3 exists to prevent. The todo's
  AC-3 wording ("OCE still propagates") is restated accordingly as a planned amendment.
- The todo's cross-phase ordering guarantee (AC-1) becomes verified end to end with the
  consumer drain in play, not just at the scheduler seam.

---

## Framework & Architectural Alignment

- **Failure semantics key off the drain point, not the phase** (PHASE-001 plan review) —
  the coordinator's drain is in-transaction (exceptions propagate, consumer token honored);
  the entry drain is post-completion (log-and-swallow, no token).
- **Persistence-agnostic:** the framework exposes drain points; it never flushes or
  commits. The coordinator is a trigger the consumer's own transaction code calls.
- **Consumer-facing public surface** lives in the root `Neatoo.RemoteFactory` namespace
  (like `IFactoryEvents`); scope-scoped runtime services; registration parity with the
  scheduler — Server and Logical, absent in Remote, where no handlers dispatch.
- **`[Service]` method injection** is how consumer factory code reaches the coordinator —
  the standard server-only service pattern from the Design projects.
- **Logging via the LoggerMessage source-generated `Log` class**, 9xxx phased-dispatch
  event-id range, next free id.

---

## Constraints & Invariants

- The sweep is already implemented and pinned — wire the warning into it; do not re-plumb
  it (inherited PHASE-001).
- The coordinator delegates to the scheduler's in-transaction drain so handler exceptions
  propagate and the consumer's transaction can roll back (inherited PHASE-001).
- The registry's `(event type, handler class type)` dedupe key stays untouched — NF0504's
  premise depends on it (inherited PHASE-002).
- The fail-open warning discriminates **per dispatch, not per entry call**: any
  `AfterFlush` dispatch swept at the post-completion drain warns — including work the
  consumer raised after their own drain — *except* work enqueued **while any drain was
  in flight in the scope** (the documented carve-out: its drain points had already
  passed). The typical case is a later-phase handler raising during the sweep; the rule
  as shipped is the broader "any drain in flight," which also covers the aborted-drain
  corner (a consumer drain that throws, is swallowed, and the call still succeeds) and,
  under the documented per-scope granularity, a concurrent flow's work. Wording widened
  at the Step 5 code review (C4) to match what shipped and what `CLAUDE-DESIGN.md`'s
  9007 row already states — the narrower phrasing invited a "fix" toward a rule the
  scheduler does not implement.
  AC-5 stands as written. A per-entry-call "consumer drained" flag was considered and
  rejected at plan review (A-V2: it silently under-warns the raised-after-the-drain case
  and carries a reset hazard in long-lived scopes, B-C1).
- The coordinator validates by **whitelist**: `AfterFlush` is the only phase a consumer
  may drain; every other value — `AfterCommit`, `Immediate`, and undefined casts like
  `(DispatchPhase)99` — is rejected. A blacklist would let an undefined value sweep the
  `AfterCommit` queue in-transaction through the `p <= through` sweep (plan review B-V1),
  and a public surface shipped in a minor release cannot be tightened later.
- Outside an entry call the coordinator **short-circuits** — it never delegates to the
  scheduler — because under the documented per-scope concurrency limitation "outside my
  entry call" can mean "inside another flow's," where an unconditional delegate would
  drain that flow's in-transaction work on the wrong token (plan review B-V3). Its XML
  acknowledges that limitation rather than overclaiming "empty by construction."
- The coordinator resolves the scope's **existing scheduler instance**; a registration
  that constructs its own would give each scope two schedulers with independent queues
  and a drain that quietly finds nothing (plan review B-C2).
- **Pre-declared pin-amendment set for the OCE decision** (sacred-tests rule — declared
  before the first edit, amended only as chartered):
  - `FactoryEventPhaseSchedulerTests.DrainAsync_PostCompletion_StillPropagatesCancellation`
    — intent inverts to pin the swallow (plus the behind-the-OCE dispatch running).
  - `FactoryEntryCallTests.HandlerThrowsOperationCanceled_MidDrain_EntryExitStillClearsAndDepthSurvives`
    — its premise (the one path where a successful entry exit throws) dissolves; repurpose
    to pin that the entry completes, the sibling handler behind the OCE runs, and the scope
    stays reusable.
  - XML docs stating "OCE still propagates" (`DispatchPhase`, the scheduler interface).
  - `src/Design/CLAUDE-DESIGN.md` Runtime Log Events rows for 9003 ("OCE still
    propagates") and 9006 (its "drain a handler's OCE aborted" cause becomes unreachable)
    — the requirements doc must not contradict shipped behavior (plan review A-V1); the
    new warning's row is added in the same edit.
  Every other existing pin stays green unmodified — including PHASE-003's six named
  pins and the integration cancellation tests (the choke point's post-invoke cancellation
  check is cooperative cancellation and keeps propagating).
- Public API addition only; no signature or behavior breaks outside the pre-declared set —
  the arc stays a minor release.
- No reflection; no generator emission changes, so the trimming posture is untouched.

---

## Steps

1. Define the public coordinator contract — interface plus XML docs that state the
   consumer-owned drain-point model: `AfterFlush` is the only drainable phase (whitelist —
   every other value, defined or undefined, is rejected), a drain outside any entry call
   short-circuits without touching the scheduler, and failure semantics come from the
   drain point. The XML acknowledges the per-scope concurrency limitation.
2. Implement it as a thin, scope-scoped delegation to the scope's existing scheduler's
   in-transaction drain honoring the consumer's token, and register it with scheduler
   parity (Server and Logical, absent Remote).
3. Teach the post-completion sweep to warn (new 9xxx id, Warning level, naming the event
   type) for each `AfterFlush` dispatch it picks up that was not created during the sweep
   itself — the per-dispatch discriminator that leaves the documented carve-out
   warning-free while catching both never-drained and raised-after-the-drain work.
4. Enact the OCE decision at the post-completion drain: only a live, cancelled token
   propagates; handler-internal OCE is logged and swallowed and the queue keeps draining.
   A propagating cancellation abandons the rest of the drain; the abandoned dispatches
   stay queued for the entry exit's clear (the existing contract). Amend the two
   pre-declared pins and restate todo AC-3 in `todo.md` itself.
5. Add integration targets exercising the real consumer pattern: factory methods that
   inject the coordinator via `[Service]`, with attribute-declared `AfterFlush` handlers
   registered only through the generated registrar — covering drained, never-drained
   (fail-open + warning), and full three-phase ordering scenarios.
6. Update the phase-contract XML docs (`DispatchPhase`, scheduler, dispatcher,
   `FactoryEventHandlerAttribute`) wherever the coordinator's arrival or the OCE
   restatement changes them — including scoping `DispatchPhase`'s unconditional
   cross-phase ordering sentence, which a consumer raising an `Immediate` event after
   their drain now inverts (plan review A-C3) — plus the `CLAUDE-DESIGN.md` log-event
   rows named in the pre-declared amendment set.
7. Red-proof the discriminators that could pass against a wrong implementation — first on
   the list, the consumer-drain marker ordering, which is the only thing separating the
   coordinator from the fail-open sweep PHASE-002 already ships (plan review B-V2); then
   the three-phase ordering sequence, the warning's fire/no-fire pair, and the
   behind-the-OCE dispatch still running.

---

## Acceptance

- [x] Mid-entry-call, `IFactoryEventPhaseCoordinator.DrainAsync(AfterFlush, ct)` runs the
      queued `AfterFlush` handlers at that point, in-transaction: a handler exception
      propagates to the drain caller, and when the entry call then fails, nothing queued
      survives into the next entry call. `[unit]`
- [x] End to end, an attribute-declared `[FactoryEventHandler<T>(DispatchPhase.AfterFlush)]`
      handler — registered only through the generated registrar, no hand registration —
      runs at the consumer's drain point, for both remote and logical invocation: its
      marker is recorded **before** the factory method's completion marker, the one
      ordering a no-op coordinator (whose markers land after completion, via the
      PHASE-002 fail-open sweep) cannot produce. `[integration]`
- [x] Cross-phase ordering holds as one observed sequence for a factory operation raising
      events in all three phases: `Immediate` marker, then the consumer drain's
      `AfterFlush` marker, then a second `Immediate` marker for an event raised *after*
      that drain (ordering is anchored per drain point, not a global barrier — todo AC-1
      as restated by this plan), then the method-completion marker, then the
      `AfterCommit` marker. `[integration]`
- [x] Fail-open end to end: the same attribute-declared `AfterFlush` handler in a call
      with no consumer drain runs **after** the method-completion marker (the sweep — 
      shipping since PHASE-002), and the new dedicated warning event id appears in the
      captured server logs. The warning is the load-bearing half of this bullet. `[integration]`
- [x] The warning discriminates per dispatch: `AfterFlush` work created mid-sweep by a
      later-phase handler runs warning-free (the documented carve-out), while work the
      consumer raised after their own drain completed still warns. `[unit]`
- [x] The consumer's token reaches the drained handlers, and cooperative cancellation
      propagates to the drain caller. `[unit]`
- [x] OCE policy: at a post-completion drain, a handler-internal OCE (no live cancelled
      token) is logged and swallowed and the dispatches queued behind it still run; a
      genuinely cancelled token still propagates, abandoning the rest of the drain — the
      abandoned dispatches stay queued for the exit clear. `[unit]`
- [x] Coordinator surface: resolvable in Server and Logical scopes, absent in Remote;
      every phase but `AfterFlush` is rejected — `AfterCommit` and an undefined cast such
      as `(DispatchPhase)99` both throw; outside any entry call the coordinator
      short-circuits — work enqueued directly (no entry call active) is left untouched
      rather than drained. `[unit]`
- [x] The restated OCE contract is reflected in the `DispatchPhase` and scheduler XML docs. `[explicit-skip: doc bullet — checked at code review]`
- [x] Full existing suite green with only the pre-declared pin amendments touched. `[explicit-skip: meta-bullet — verified from the Step 5 gate logs]`

---

## Current State (Pre-Flight)

Walked 2026-08-15, after plan review, before the first edit.

**Scheduler** — `src/RemoteFactory/Internal/FactoryEventPhaseScheduler.cs`. The interface is
public in the `Internal` namespace; the impl is `internal sealed`, per-scope, one `_gate`
lock over `Dictionary<DispatchPhase, Queue<QueuedDispatch>>` + `_entryDepth`.
`DrainAsync(phase, inTransaction, ct)` (`:202`) dequeues one-at-a-time via
`TryDequeueThrough` (`:280`), which already returns the queued phase — the warning's
discriminator input is plumbed exactly as PHASE-001's inherited note claimed. The
post-completion branch's OCE carve-out is `catch (OperationCanceledException) { throw; }`
(`:227-230`) ahead of the general swallow that logs 9003 — the OCE policy change is a
`when` filter on that catch, nothing structural. The entry drain is
`EndEntryCallAsync(true)` → `DrainAsync(AfterCommit, false, CancellationToken.None)`
(`:169`) with `ClearAtExit()` in a `finally` (`:176`); `ClearAtExit` (`:249`) is the
depth-0 reset point. `QueuedDispatch` (`:102`) is a private `record struct` — the natural
carrier for the per-dispatch "created mid-drain" mark; a drain-in-progress counter under
`_gate` gives `Enqueue` (`:180`) the stamp.

**Where the coordinator plugs in** — `AddRemoteFactoryServices.cs:72-97`: the non-Remote
`else` branch holds `TryAddScoped<IFactoryEventPhaseScheduler>(sp => new …)` (`:84-85`).
The coordinator registration goes beside it and must resolve
`sp.GetRequiredService<IFactoryEventPhaseScheduler>()` (B-C2). Public interface file goes
in the root namespace beside `IFactoryEvents`; impl beside the scheduler in `Internal/`.

**Log** — `Internal/Log.cs` 9xxx region ends at 9006; 9007 is free for the warning.

**Entry-call seam for unit tests** — `Internal/FactoryEntryCall.cs`: public static
`RunAsync<T>/RunAsync/Run` wrappers; null-tolerant scheduler resolution. Existing
`FactoryEntryCallTests` drive real Server-mode scopes through it — the coordinator's
unit tests follow that shape. The OCE pin to repurpose sits at `:220`; the scheduler pin
at `FactoryEventPhaseSchedulerTests.cs:146`.

**Unit log capture** — `FactoryEventPhaseSchedulerTests.NewDispatcher(out CapturingLoggerProvider)`
(`:16-71`) exists and pins 9003, exactly as B-C3 said. Its `LogEntry` records
`(EventId, Level, Exception, Phase)` but not the event-type value — pinning "the warning
names the event type" needs a small additive extension (capture the `EventType` structured
value or the formatted message). Additive harness change, not a pin edit.

**Docs to touch, verified** — `DispatchPhase.cs:16-19` states the cross-phase ordering
unconditionally (A-C3's sentence, confirmed); `:44-52` (AfterFlush) already names
`IFactoryEventPhaseCoordinator.DrainAsync` — the interface does not exist yet, so this
plan makes an already-published doc reference true; `:54-64` (AfterCommit) says "OCE still
propagates" — restate. Scheduler interface XML `:76-79` — restate. The attribute's XML
(`FactoryEventHandlerAttribute.cs:7-14`, B-C7) describes phase timing only — no OCE or
ordering claims; expected outcome: no edit, verified at Step 6. `CLAUDE-DESIGN.md`
Runtime Log Events rows confirmed at `:1021` (9003) and `:1024` (9006); 9007 row appends.

**Integration harness** — `ClientServerContainers.Scopes()` (parameterless and
format overloads, `:136`/`:144`) returns `(server, client, local)`; only the
`configure*` overload (`:163`) returns `(client, server, local)`;
`ScopesWithLogging(out TestLoggerProvider)` (`:207`) returns `(server, client, local)` —
the tuple-order divergence PHASE-007 tracks; read destructurings carefully.
*(Corrected at the Step 5 gate — the original note had the parameterless overload's
order backwards; every destructuring in the shipped tests is correct.)* PHASE-003's `FactoryEventPhaseEntryTargets.cs` +
`PhaseHandlerRegistrations.EnsureRegistered()` is the hand-registered model;
PHASE-002's `FactoryEventPhaseAttributeTargets.cs` is the attribute-declared model with
the one-event-type-per-scenario discipline and the `*-method-done` ordering marker. New
coordinator targets follow the attribute-declared model: factory methods inject
`IFactoryEventPhaseCoordinator` via `[Service]`, drain mid-body, record markers.

**No surprises that shift the plan** — no amendments needed at pre-flight.

---

## Test Evidence

Unit tests in `RemoteFactory.UnitTests.Internal`; integration tests in
`RemoteFactory.IntegrationTests.Events.Phases.FactoryEventPhaseCoordinatorTests`
(distinct assembly/namespace from the unit class of the same name). Logs:
[004-build.log](../reviews/004-build.log), [004-test.log](../reviews/004-test.log);
red-proofing: [004-redproof.log](../reviews/004-redproof.log).

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| Mid-entry-call drain, in-transaction; entry failure discards | `[unit]` | `FactoryEventPhaseCoordinatorTests.DrainAsync_MidEntryCall_DrainsQueuedAfterFlushAtTheCallPoint` + `.DrainAsync_HandlerException_PropagatesAndTheFailedEntryDiscardsTheRest` (RP-1) | ✓ |
| Attribute-declared AfterFlush at the consumer's point, marker before method-done, remote + logical | `[integration]` | `FactoryEventPhaseCoordinatorTests.RemoteCreate_CoordinatorDrain_RunsAfterFlushHandlersAtTheConsumersPoint` + `.LogicalCreate_…` (RP-1) | ✓ |
| Three-phase ordering as one observed sequence | `[integration]` | `.RemoteExecute_ThreePhases_RunInPhaseOrderNotRaiseOrder` + `.LogicalExecute_…` (RP-1; raise order is reverse phase order; sequence now includes a post-drain Immediate raise pinning the A-C3 scoped-ordering sentence — gate nice-to-have) | ✓ |
| Fail-open end to end + 9007 in captured logs | `[integration]` | `.RemoteCreate_NeverDrainedAfterFlush_RunsAtTheSweepWithTheWarning` (RP-3) — warning-free drained path: `.RemoteCreate_ConsumerDrainedAfterFlush_ProducesNoWarning` (RP-1) | ✓ |
| Warning discriminates per dispatch (carve-out silent; raised-after-drain warns) | `[unit]` | **Load-bearing pin (gate must-cover closure):** `FactoryEventPhaseCoordinatorTests.DrainAsync_RaiseAfterTheDrain_InARealEntryCall_SweepsAndWarnsExactlyOnce` (RP-7 — red against the rejected per-entry-call flag, in the state production reaches). Scheduler-seam mechanics: `FactoryEventPhaseSchedulerTests.DrainAsync_AfterFlushWorkCreatedMidSweep_RunsWithoutTheFailOpenWarning` (RP-2) + `.DrainAsync_AfterFlushRaisedAfterTheConsumersOwnDrain_StillWarnsAtTheSweep` (RP-3; measured GREEN under the guarded flag in RP-7 — kept as mechanics, not decision evidence) + `.DrainAsync_PostCompletionSweepOfUndrainedAfterFlush_WarnsPerDispatchNamingTheEventType` (RP-3). Overlap/counter enforcement (gate should-cover): `FactoryEventPhaseCoordinatorTests.DrainAsync_CoordinatorCalledInsideTheEntrySweep_LaterMidSweepWorkStaysWarningFree` (RP-8; carries a witness dispatch so it cannot go vacuous). Both rejected flag variants measured: RP-7 (pure) and RP-9 (flag-plus-stamp — caught only by the entry-call pin). (`DrainAsync_ConsumerDrainedAfterFlush_NeverWarns` retained as the happy-path smoke; near-tautological per the gate — not cited as discrimination evidence.) | ✓ |
| Consumer token reaches handlers; cooperative cancellation propagates | `[unit]` | `FactoryEventPhaseCoordinatorTests.DrainAsync_PassesTheConsumersTokenToDrainedHandlers` (RP-1) + `.DrainAsync_ConsumersTokenCancelledMidDrain_PropagatesAndLeavesTheRestQueued` (gate should-cover closure — at the COORDINATOR's drain point; the original citation of the scheduler's post-completion test was the wrong drain point, corrected per the gate) + `FactoryEventPhaseSchedulerTests.DrainAsync_PostCompletion_CooperativeCancellationStillPropagatesAndAbandonsTheRest` (the post-completion analogue) | ✓ |
| OCE policy: handler-internal swallowed + rest runs; cooperative propagates + abandons | `[unit]` | `FactoryEventPhaseSchedulerTests.DrainAsync_PostCompletion_SwallowsHandlerInternalCancellationAndRunsTheRest` + `FactoryEntryCallTests.HandlerThrowsOperationCanceled_MidEntryDrain_IsSwallowedAndTheEntryStillSucceeds` (both RP-4, the amended pre-declared pins) + the cooperative test above | ✓ |
| Coordinator surface: modes, whitelist incl. undefined casts, short-circuit | `[unit]` | `FactoryEventPhaseRegistrationTests.PhaseCoordinator_RegisteredInModesThatDispatchHandlers` + `.PhaseCoordinator_NotRegisteredInRemoteMode` + `FactoryEventPhaseCoordinatorTests.DrainAsync_EveryPhaseButAfterFlush_IsRejected` (Theory: Immediate, AfterCommit, 99, −1; RP-5) + `.DrainAsync_OutsideAnyEntryCall_LeavesQueuedWorkUntouched` (RP-6) + `.DrainAsync_RejectedPhaseOutsideAnyEntryCall_StillThrows` (gate nice-to-have — validation ordered before the short-circuit) | ✓ |
| Restated OCE contract in XML docs | `[explicit-skip: doc bullet]` | `DispatchPhase.cs` (ordering scoped per A-C3 + AfterCommit OCE prose), scheduler interface `inTransaction` doc, `EndEntryCallAsync` finally comment, `CLAUDE-DESIGN.md` 9003/9006 rows + 9007 row; `FactoryEventHandlerAttribute.cs` verified — no OCE/ordering claims, no edit needed (B-C7 outcome) | ✓ (code review checks) |
| Full suite green, only pre-declared pins touched | `[explicit-skip: meta-bullet]` | 004-build.log (0 errors), 004-test.log: unit 705×2, integration 587+5skip×2, Design 86×2 (counts after gate closure). Pre-existing relay-timing flake under full-parallel load diagnosed and recorded in 004-redproof.log + Discovery Log (PHASE-007 handoff) | ✓ |

---

## Plan Amendments

### 2026-08-16 — Cross-phase ordering is per drain point, not a global barrier

- **Section affected:** Scope (frozen text), Acceptance bullet 3, todo AC-1
- **Original said:** Scope promises "the cross-phase ordering guarantee tests (all
  `Immediate` before any `AfterFlush` before any `AfterCommit`)" — the global-barrier
  reading, inherited from todo AC-1.
- **What changed:** Ordering is anchored per drain point. For work raised before a given
  drain point the old sequence holds; code that raises *after* its own `AfterFlush` drain
  interleaves that later `Immediate` work between drain points. The shipped test asserts
  the five-marker sequence including that interleave; `DispatchPhase`'s XML, todo AC-1,
  and Acceptance bullet 3 all say so. Scope's wording is left frozen per the workflow —
  this entry is the correction of record.
- **Why:** Creating an in-body consumer drain point made the interleave reachable for the
  first time. Plan review A-C3 caught it for the XML; the Step 5 code review (V1) caught
  that the requirements doc had not followed.
- **Discovery Log link:** 2026-08-16 — PHASE-004 (code review)

### 2026-08-16 — Fail-open carve-out is "any drain in flight," not "during that sweep"

- **Section affected:** Constraints & Invariants
- **Original said:** the carve-out covers "work created during that sweep by a
  later-phase handler."
- **What changed:** the shipped stamp exempts work enqueued while **any** drain is in
  flight in the scope — which additionally covers an aborted consumer drain whose call
  still succeeds, and (under per-scope granularity) a concurrent flow's work.
- **Why:** code review C4 — `CLAUDE-DESIGN.md` already documented the shipped rule, so
  only the plan's phrasing was narrow, and a narrow Constraint invites a "fix" toward a
  rule the scheduler does not implement.
- **Discovery Log link:** 2026-08-16 — PHASE-004 (code review)

### 2026-08-15 — Five plan-review vetoes adopted before the first edit

- **Section affected:** Intent, Constraints & Invariants, Steps, Acceptance
- **Original said:** per-entry-call warning flag; "framework-owned phases rejected"
  (blacklist-shaped); an attribute-declared bullet green against a no-op coordinator;
  "benign no-op outside any entry call"; the OCE restatement's amendment set covering
  only XML and two tests.
- **What changed:** per-dispatch warning discriminator; whitelist validation naming
  undefined casts; bullet 2 requires the before-method-done marker ordering and heads the
  red-proof list; short-circuit decided and pinned falsifiably; `CLAUDE-DESIGN.md`'s
  9003/9006 rows added to the pre-declared amendment set.
- **Why:** [reviews/004-plan-review.md](../reviews/004-plan-review.md) — CONCERNS, 5
  veto-tier findings, full disposition table there.
- **Discovery Log link:** 2026-08-15 — PHASE-004 (plan review)

---

## Notes

- **Client-raise relay gap (Discovery Log 2026-08-14, review A-V1)** — flagged for revisit
  "at PHASE-004 or at todo close." Recommendation: out of this plan. It is pre-existing,
  not phase-related, and needs its own design decision (echo-to-self semantics vs. the
  "one `[Remote]` call = one `Relay` invocation" contract). Decision recorded when the
  user rules on it; **fallback venue is the Step 7 close-out audit**, and a "warrants
  work" ruling needs a `Draft` index row first (no orphan plans).
- **Warning pins:** both tiers already have capture harnesses — integration via
  `ClientServerContainers.ScopesWithLogging` (PHASE-003's 9001/9004/9005 assertions) and
  unit via `FactoryEventPhaseSchedulerTests`' existing `CapturingLoggerProvider` /
  `NewDispatcher(out logs)`, which already pins 9003. Nothing to build (plan review B-C3
  corrected this note's earlier claim); PHASE-007's queued 9002/9004/9006 pins reuse the
  same harnesses.
- **Keyboard gotchas carried from plan review:** whatever mark implements the
  per-dispatch warning discriminator must not outlive its drain in long-lived scopes
  (B-C1, transposed); under the per-scope concurrency limitation, one flow's drain
  affects a concurrent flow's warning — a consequence of documented granularity, not a
  redesign trigger (B-C1); if provenance-based OCE discrimination reads better than token
  state at the keyboard, `OperationCanceledException.CancellationToken` is the sharper
  tool (B-C4).
- **PHASE-005 owns** the Design-project demonstration of the coordinator, the
  `CLAUDE-DESIGN.md` pattern narrative, and the published-docs/skill updates (its Scope
  already covers the log-id table's narrative). This plan touches only the log-event
  table rows its behavior change falsifies (plan review A-V1/A-C2).
- The consumer-drain seam in integration tests is a factory method body calling the
  coordinator — consumer transaction code runs inside factory methods, so no harness
  surgery is needed to model "between flush and commit."
