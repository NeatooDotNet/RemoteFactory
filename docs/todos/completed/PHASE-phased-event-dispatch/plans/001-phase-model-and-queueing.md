# DispatchPhase Model, Registry Phase, Dispatcher Queueing

**Plan #:** 001
**Date:** 2026-08-14
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done
**Last Updated:** 2026-08-14
**Plan-review opt-in:** Yes (public API: new enum, attribute constructor, registry signature — hard to change after release)
**Code-review opt-in:** Yes (changes the core dispatch path every event flows through)

---

## Scope

Introduce the `DispatchPhase` enum (`Immediate`/`AfterFlush`/`AfterCommit`), give
`FactoryEventHandlerAttribute<T>` an optional phase argument, extend
`FactoryEventHandlerRegistry` so each registration carries its phase, and change
`FactoryEventsDispatcher` so `Raise` dispatches `Immediate` handlers exactly as today but
*queues* the event for phase-registered handlers in a new per-scope queue (mirroring the
`IFactoryEventCollector` scoped-service pattern), including the internal drain primitive
that later plans trigger and the rollback-discard semantics. This plan does NOT touch the
generator (PHASE-002), does not wire any drain *point* (entry-call tracking is PHASE-003,
the consumer-facing coordinator is PHASE-004), and does not change relay or serialization.

---

## Intent

- Separate *when a handler is registered to run* (a declaration on the attribute) from
  *when the framework runs it* (a drain point), so read-only projection handlers can stop
  inheriting the in-transaction dispatch contract.
- Deliver the queue-and-drain core with the per-phase failure semantics the proposal
  specifies: in-transaction phases propagate handler exceptions (the caller's transaction
  must be able to roll back); `AfterCommit` logs-and-swallows per handler because a throw
  there can no longer roll anything back.
- Rollback-discard falls out structurally: queues are scoped; a scope whose entry call
  never completes simply never drains.

## Framework & Architectural Alignment

- Per-scope queue follows the existing `IFactoryEventCollector` scoped-service pattern —
  but registered for every mode that dispatches handlers (server *and* local/logical),
  not server-only like the collector.
- Logging via the source-generated `LoggerMessage` pattern in `Internal/Log.cs`, new ids
  in the unused 9xxx range with their own section.
- Dispatch stays sequential and awaited within a phase; handler order within a phase stays
  unspecified (the todo's cross-phase ordering guarantee is what's new).
- Registry keeps its static shape and `(eventType, handlerClassType)` dedupe contract.

## Constraints & Invariants

- **Zero behavior change for existing registrations.** No phase argument ⇒ `Immediate` ⇒
  today's contract exactly; the full existing suite passes unmodified.
- The framework stays persistence-free: the queue knows nothing about flushes, commits,
  or transactions — those words appear only in docs describing consumer intent.
- Queue state is scoped DI state — no statics, no cross-scope leakage.
- **Trimming invariant (review A-V1):** `RegisterHandler<TEvent>` carries
  `[DynamicallyAccessedMembers(All)]` on its generic parameter — a documented contract
  (skills/RemoteFactory/references/trimming.md). Any new overload carries it too; its loss
  is silent and no test can go red for it.
- **Registry change is additive only (review B-C1):** the two-argument
  `RegisterHandler<TEvent>(typeof(Class), func)` call the in-repo generator emits today
  must keep compiling and mean `Immediate` — the tree builds green between PHASE-001 and
  PHASE-002.
- **Failure semantics are keyed to the drain point, not the phase (review B-V2):** an
  in-transaction drain propagates handler exceptions (the caller can still roll back); a
  post-completion drain logs-and-swallows regardless of which phase's handlers it is
  running — the proposal's swallow rationale ("a throw cannot roll anything back")
  attaches to the drain point.
- **Re-entrant enqueue is drain-until-empty (review B-V3):** a handler raising an event
  during a drain whose handlers land in the draining (or an already-passed) phase joins
  the current drain, FIFO. Infinite raise loops remain the consumer's bug, exactly as
  with today's synchronous chained raises.
- Relay collection at `Raise` time is untouched: a phase-registered event is still
  collected for the client batch (unless `ServerOnly`) when it is raised.
- Interim state until PHASE-003/004 land: phase-registered handlers only run via the
  internal drain primitive. Acceptable mid-todo (no release ships from this branch until
  the todo closes).
- `RaiseUntyped` behaves identically to `Raise<T>`.

## Steps

1. Add the public `DispatchPhase` enum and the optional phase argument on
   `[FactoryEventHandler<T>]`, with XML docs stating each phase's contract — including
   the naming-honesty note that `AfterCommit` means "after the entry factory call
   completes" (true "after commit" only because consumers commit inside factory bodies).
   Update the four runtime XML-doc blocks whose "always shared-scope, sequential,
   awaited" wording becomes phase-conditional (`RaiseOptions`, `IFactoryEvents`,
   `FactoryEventsDispatcher`, the handler attribute) — doc delta ships with the behavior.
2. Extend `FactoryEventHandlerRegistry` entries to carry a phase, preserving the dedupe
   contract and the registration path the generator emits.
3. Add the per-scope phase-queue service, registered in every mode that dispatches
   handlers, following the collector's scoped pattern.
4. Change `FactoryEventsDispatcher` so `Raise`/`RaiseUntyped` dispatch `Immediate`
   handlers as today and enqueue `(handler, event)` for phase-registered handlers.
5. Implement the drain primitive (public type in the `Neatoo.RemoteFactory.Internal`
   namespace so PHASE-003's generated-code call sites can reach it): FIFO within a phase,
   drain-until-empty across re-entrant enqueues; the caller declares the drain point —
   in-transaction drains propagate handler exceptions, post-completion drains
   log-and-swallow per handler with a dedicated event id, still propagating
   `OperationCanceledException`.
6. Add the new 9xxx log section (queued / drained / swallowed-exception ids as needed)
   and the matching row(s) in the `CLAUDE-DESIGN.md` log-id table (doc delta ships with
   the behavior, per project rules).
7. Unit tests pinning the acceptance bullets below.

## Acceptance

- [ ] A handler registered without a phase argument dispatches at `Raise` time with
      today's exact contract (shared scope, sequential, exception aborts remaining
      handlers and propagates). `[explicit-skip: pinned by the existing suite passing
      unmodified — the backward-compat signal]`
- [ ] `Raise` with an `AfterCommit`-registered handler does not invoke the handler at
      raise time; the dispatch is queued in the current scope. *(Interim semantics —
      PHASE-003 adds the no-entry-call immediate-dispatch path and will restate this
      bullet as entry-call-scoped; the later test edit is a planned amendment, not
      test-gutting.)* `[unit]`
- [ ] Mixed registration on one event (one `Immediate`, one `AfterCommit` handler):
      the `Immediate` handler runs at raise time, the other is queued. *(Same interim
      annotation as above.)* `[unit]`
- [ ] A post-completion drain runs queued dispatches FIFO; a handler exception is logged
      with the dedicated event id and swallowed; later queued handlers still run;
      `OperationCanceledException` propagates. `[unit]`
- [ ] An in-transaction drain propagates a handler exception to the drain caller —
      including for `AfterFlush` handlers; the same handlers drained at a
      post-completion point get swallow semantics (drain-point-keyed, not phase-keyed).
      `[unit]`
- [ ] An event raised by a handler *during* a drain, whose handlers land in the draining
      or an already-passed phase, is processed in the same drain (drain-until-empty —
      nothing is silently dropped). `[unit]`
- [ ] A drain also sweeps up any earlier phase the consumer never drained, earliest phase
      first, under the drain point's failure semantics — the fail-open behavior PHASE-004
      builds its warning on. Later phases than the one requested are left alone. `[unit]`
- [ ] A queued dispatch reaches its handler with the event instance, `RaiseOptions`, and
      originating scope provider it was queued with. `[unit]`
- [ ] Two scopes' queues are independent; a scope disposed without draining runs nothing
      (rollback-discard). *(Same interim annotation as bullet 2.)* `[unit]`
- [ ] A phase-registered event raised without `ServerOnly` is still collected for the
      relay batch at raise time (needs a Server-mode container — the collector is
      Server-only). `[unit]`
- [ ] Registry dedupe by `(eventType, handlerClassType)` holds for phase registrations
      across repeated container builds; interim semantics documented as
      first-registration-wins (PHASE-002 decides diagnose-vs-last-wins). `[unit]`
- [ ] Build/test green. `[explicit-skip: meta-bullet, satisfied by Step 5 gate pre-flight]`

---

## Current State (Pre-Flight)

Walked 2026-08-14 against v1.7.0 (`main` @ 94a8a12):

- `FactoryEventsDispatcher.DispatchToHandlers` (`src/RemoteFactory/FactoryEventsDispatcher.cs:39-58`):
  collects for relay at raise time unless `ServerOnly`, then
  `FactoryEventHandlerRegistry.GetHandlers(eventType)` → sequential `await`, exceptions
  propagate. This is the queue-instead-of-dispatch seam.
- `FactoryEventHandlerRegistry` (`src/RemoteFactory/FactoryEventHandlerRegistry.cs`):
  `RegisterHandler<TEvent>(Type handlerClassType, Func<IServiceProvider, object,
  RaiseOptions, CancellationToken, Task>)`; internal `HandlerEntry` struct already exists
  to carry per-registration metadata — phase slots in naturally. `GetHandlers` currently
  strips entries down to bare invoke funcs; phase-aware dispatch needs entries surfaced.
  Dedupe is `(eventType, handlerClassType)`; `Clear()` is internal, unused by tests.
- Attribute (`src/RemoteFactory/FactoryAttributes.cs:148-151`): empty sealed generic class,
  `AllowMultiple = true`. **Observation:** a class could declare
  `[FactoryEventHandler<X>(Immediate)]` and `[FactoryEventHandler<X>(AfterCommit)]`
  simultaneously; today's registry dedupe would silently drop the second registration.
  Queued to PHASE-002 as a likely generator diagnostic (see Discovery Log 2026-08-14).
- DI (`src/RemoteFactory/AddRemoteFactoryServices.cs:72-90,128-138`): Remote mode
  registers client-side `RemoteFactoryEvents` (never dispatches handlers — no queue needed
  there); Server/Logical register `FactoryEventsDispatcher` plus, server-only, the
  `IFactoryEventCollector` (`:131`). The phase queue belongs in the Server/Logical branch
  (`:76-90`), not copying the collector's server-only restriction.
- `RaiseFactoryEventRemote` (`:84-89`) is how client-raised events reach `RaiseUntyped`
  server-side — phase-registered handlers of client-raised events will queue in the
  request scope; which drain point covers that entry is PHASE-003's question.
- `Internal/Log.cs`: 3xxx event section ends at 3012; 9xxx entirely free — new
  `Phased Dispatch (9xxx)` section. Log-id table to update lives in
  `src/Design/CLAUDE-DESIGN.md` (~line 1013).

---

## Test Evidence

All cited tests live in `src/Tests/RemoteFactory.UnitTests/Internal/`; namespace prefix
`RemoteFactory.UnitTests.Internal` omitted below for width.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| Phase-less handler keeps today's contract | `[explicit-skip]` | No existing test modified (`git status src/Tests` shows only new files). Suite: 653 unit × net9.0/net10.0 (0 failures) against a 614 baseline on `main` — +39 new cases, all added by this plan; integration 561 passed / 5 skipped / 566 total × 2 (`reviews/001-test.log`); Design 86 × 2, 0 failures (`reviews/001-test-design.log`) | ✓ |
| AfterCommit handler not invoked at raise time | `[unit]` | `FactoryEventsDispatcherPhaseTests.Raise_DeferredHandler_DoesNotDispatchAtRaiseTime`; primitive-level: `FactoryEventPhaseSchedulerTests.Enqueue_DoesNotInvokeHandler` | ✓ |
| Mixed phases: Immediate runs, other queues | `[unit]` | `FactoryEventsDispatcherPhaseTests.Raise_MixedPhases_ImmediateRunsAndDeferredWaits` (also pins cross-phase ordering) | ✓ |
| Post-completion drain: FIFO, logged+swallowed, rest still run, OCE propagates | `[unit]` | `FactoryEventPhaseSchedulerTests.DrainAsync_RunsDeferredDispatchesInFifoOrder`, `.DrainAsync_PostCompletion_SwallowsHandlerExceptionAndRunsTheRest`, `.DrainAsync_PostCompletion_StillPropagatesCancellation`, and for the log half `.DrainAsync_PostCompletionSwallow_LogsTheDedicatedEventIdWithTheException` (asserts event id 9003 + attached exception) | ✓ |
| Drain sweeps earlier phases the consumer never drained | `[unit]` | `FactoryEventPhaseSchedulerTests.DrainAsync_SweepsAnEarlierPhaseTheConsumerNeverDrained` (also asserts 9003 attributes the failure to the *queued* phase), `.DrainAsync_MidDrainEarlierPhaseWork_PreemptsRemainingLaterPhaseWork` (pins the ordering choice), `.DrainAsync_DoesNotRunLaterPhasesThanRequested` — **all three verified red** against the pre-fix drain (`reviews/001-redproof.log`) | ✓ |
| Queued dispatch carries its event, options, and provider intact | `[unit]` | `FactoryEventPhaseSchedulerTests.DrainAsync_HandlerReceivesTheEventAndOptionsItWasQueuedWith` (asserts `Assert.Same` on the originating scope provider, not merely non-null) | ✓ |
| In-transaction drain propagates; same handlers swallow at post-completion point | `[unit]` | `FactoryEventPhaseSchedulerTests.DrainAsync_InTransaction_PropagatesHandlerExceptionAndAbortsRemaining`, `.DrainAsync_SamePhaseHandlersDrainPoint_KeysSemanticsNotThePhase` | ✓ |
| Re-entrant enqueue during drain runs in same drain (same phase AND already-passed phase) | `[unit]` | `FactoryEventPhaseSchedulerTests.DrainAsync_ReentrantEnqueueDuringDrain_RunsInTheSameDrain` (same phase), `.DrainAsync_ReentrantEnqueueIntoAnAlreadyPassedPhase_StillRunsInThisDrain` (already-passed — **verified red against the pre-fix single-queue drain**), `.DrainAsync_DoesNotRunLaterPhasesThanRequested` (bounds it); real raise-path re-entrancy: `FactoryEventsDispatcherPhaseTests.DrainedHandlerRaisingAnEvent_GoesThroughTheRealRaisePath` | ✓ |
| Scope independence; never-drained runs nothing | `[unit]` | `FactoryEventPhaseRegistrationTests.PhaseDispatcher_IsScoped_NotSharedAcrossScopes`, `FactoryEventPhaseSchedulerTests.ScopeDisposedWithoutDraining_RunsNothing` (real scope, disposed) | ✓ |
| Phase-registered event still collected for relay | `[unit]` | `FactoryEventsDispatcherPhaseTests.Raise_DeferredHandler_StillCollectsForRelayAtRaiseTime` (Server-mode container, per review B-C3); `ServerOnly` counterpart: `.Raise_DeferredHandlerWithServerOnly_IsNotCollectedForRelay` | ✓ |
| Registry dedupe holds; first-registration-wins documented | `[unit]` | `FactoryEventPhaseRegistrationTests.RegisterHandler_RepeatedContainerBuilds_DedupesByEventAndHandlerClass`, `.RegisterHandler_SameHandlerClassTwoPhases_KeepsTheFirstRegistration` | ✓ |
| Build/test green | `[explicit-skip]` | `reviews/001-build.log` (0 errors, net9.0+net10.0), `reviews/001-test.log` (0 failures) | ✓ |

Additional coverage not tied to a single bullet: `RegisterHandler_WithoutPhase_DefaultsToImmediate`,
`RegisterHandler_WithPhase_RoundTripsThePhase`, `PhaseDispatcher_RegisteredInModesThatDispatchHandlers`
(Theory: Server + Logical), `PhaseDispatcher_NotRegisteredInRemoteMode`,
`DrainAsync_OnlyDrainsTheRequestedPhase`, `DrainAsync_NothingDeferred_IsANoOp`,
`DrainAsync_DeferredDispatchesRunOnce_NotOnASecondDrain`,
`Attribute_NoArgument_DefaultsToImmediate`, `Attribute_ExplicitPhase_RoundTrips` (Theory: all
three phases), `RaiseUntyped_DeferredHandler_DefersJustLikeRaise`,
`Raise_PhasedHandlerWithNoQueueInScope_DispatchesImmediatelyRatherThanVanishing`,
`DrainAsync_HandlerReceivesTheDrainTimeCancellationToken` (pins the drain-time token choice
PHASE-003 will reason from), `Enqueue_NullEvent_Throws`.

**Deliberately not covered here** (drain *points* are PHASE-003/004's deliverable, so no
integration-tier coverage of real factory calls belongs to this plan): entry-call tracking,
the consumer coordinator, and the same-response relay-batch guarantee.

---

## Plan Amendments

### 2026-08-14 — Handler attribute moved out of the generator-linked source file

- **Section affected:** Step 1
- **Original said:** add the optional phase argument to `[FactoryEventHandler<T>]` in place.
- **What changed:** the attribute moved to its own `FactoryEventHandlerAttribute.cs`.
- **Why:** `FactoryAttributes.cs` is `<Compile Include>`-linked into the netstandard2.0
  Generator project, so referencing `DispatchPhase` from it compiled the enum into
  `Neatoo.Generator.dll`, duplicating a public runtime type (CS0436/CS0433 in every project
  referencing both). The generator matches the attribute by metadata-name string and never
  needed the type.
- **Discovery Log link:** 2026-08-14 — PHASE-001 (shared-source build constraint)

### 2026-08-14 — Drain sweeps earlier phases, not just the requested one

- **Section affected:** Step 5, Constraints (re-entrant enqueue)
- **Original said:** drain-until-empty within the phase being drained.
- **What changed:** `DrainAsync` drains the requested phase *and every earlier phase*,
  earliest first, until none remain.
- **Why:** the test-review gate found the original shape silently dropped work enqueued
  into an already-passed phase — and, separately, would have lost `AfterFlush` handlers
  entirely for any consumer who never called the coordinator. That fail-open behavior is
  PHASE-004's AC-5, now structurally satisfied here.
- **Discovery Log link:** 2026-08-14 — PHASE-001 (gate found a real defect)

### 2026-08-14 — Scheduler naming

- **Section affected:** Step 5
- **Original said:** the drain primitive as `IFactoryEventPhaseDispatcher`.
- **What changed:** renamed to `IFactoryEventPhaseScheduler` / `FactoryEventPhaseScheduler`.
- **Why:** code review C1 — two "…Dispatcher" types in one assembly doing different jobs,
  settled before PHASE-003 emits generated call sites against the name. `…Queue` was
  unavailable: CA1711 forbids the suffix.
- **Discovery Log link:** see [reviews/001-code-review.md](../reviews/001-code-review.md)

---

## Notes

- Recon (2026-08-14, background agent) established: registry at
  `src/RemoteFactory/FactoryEventHandlerRegistry.cs` (dedupe by
  `(eventType, handlerClassType)`, `Clear()` never used by tests); dispatcher seam at
  `FactoryEventsDispatcher.DispatchToHandlers`; scoped-registration precedent at
  `AddRemoteFactoryServices.cs` (collector is server-mode-only — the phase queue must
  not copy that restriction); 9xxx log range free.
- Open design points deliberately left to the keyboard: whether the queue stores the
  handler delegate or re-resolves from the registry at drain time; and whether queued
  dispatches carry the raise-time `CancellationToken` or receive the drain-time token
  (review B-C5 — OCE propagating from a post-success drain can fail a call that already
  succeeded; the policy decision lands at PHASE-003's call site, but the primitive's
  token plumbing is chosen here).
- Plan review 2026-08-14: CONCERNS, 4 veto findings addressed by draft edits before
  implementation — see [../reviews/001-plan-review.md](../reviews/001-plan-review.md).
- Planning-guidelines evaluation (CLAUDE.md): no serialization round-trip applies
  (`DispatchPhase` is registration-time state, never crosses the wire); no new Roslyn
  diagnostics in this plan (deferred to PHASE-002); integration coverage arrives with the
  drain points in PHASE-003/004.
- Known pre-existing footgun, no action here (review B-C6): a consumer registering their
  own `IFactoryEvents` before `AddNeatooRemoteFactory` keeps it (`TryAddScoped`), and
  would leave the phase queue registered but never fed.
