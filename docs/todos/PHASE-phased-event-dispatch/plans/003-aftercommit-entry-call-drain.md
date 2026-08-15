# AfterCommit: Entry-Call Tracking and Framework-Owned Drain

**Plan #:** 003
**Date:** 2026-08-14
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-14
**Plan-review opt-in:** Yes (touches all three factory renderers; entry-shape subtleties found at recon make this the riskiest plan)
**Code-review opt-in:** Yes (behavior-changing across generated code and runtime)

---

## Scope

Make the `AfterCommit` queue drain when the *entry* factory call completes successfully,
uniformly for HTTP-dispatched `[Remote]` calls and direct server-side/local invocation,
with rollback-discard on failure, swallow-and-log exception semantics (dedicated log event
ids), and drained-handler events still joining the same response's relay batch. Also owns
the "Raise outside any factory call" semantics (dispatch immediately, debug log), since
that is the absence-of-entry-tracking case. Known recon risks this plan must resolve at
the keyboard: the three renderers share no pipeline helper; static factories have no
`Local*` methods (DI delegate lambdas instead); public wrappers are mostly non-async;
`LocalSave` nests into `LocalInsert`/`LocalUpdate`/`LocalDelete`; HTTP calls enter
`Local*` directly on the class leg but through the public wrapper on the interface leg.
This plan does NOT own the consumer-facing drain API
(PHASE-004), and does NOT thread the attribute's phase argument through the generator
(PHASE-002) — its tests register phased handlers through the registry's 3-arg overload.

---

## Intent

Today the framework has no notion of "a factory call is in flight" — phased dispatches are
queued (PHASE-001) but nothing ever drains them, and the dispatcher queues whenever a
scheduler exists in the scope regardless of whether any factory call is active. After this
plan, the framework knows when an entry call begins and ends:

- **Entry-call tracking** is a per-scope, depth-aware notion. Nested factory work — the
  `LocalSave` → `LocalInsert` nesting, one factory method invoking another factory, the
  HTTP handler wrapping a `Local*` method — increments depth rather than creating a second
  entry. Only the outermost completion is "the entry call completing."
- **Two entry families, one contract.** Remote entries (every `[Remote]` factory call and
  every client-raised event) all pass through the single runtime choke point that resolves
  and invokes the DI-registered delegate; that choke point marks the entry and drains on
  success, before relay collection, so events raised by `AfterCommit` handlers join the
  same HTTP response's relay batch — and *before* the choke point's post-invoke
  cancellation check, so a token cancelled between success and drain cannot skip the
  drain (plan review B-V2: the same failure mode B-C5 flagged, relocated). Local/direct
  entries (Logical mode, server-side code calling a factory) are marked by generated code
  at the local execution seam in all three factory patterns. Both families drain by
  calling the PHASE-001 scheduler (`DrainAsync(AfterCommit, inTransaction: false, …)` —
  which sweeps `AfterFlush` first, giving PHASE-004's fail-open its drain point for
  free).
- **Failure discards explicitly — a clear, never a drain.** The drain call exists only on
  the success path. At the *outermost* exit of a failed entry call, the queues are
  **cleared** (plan review A-V2: "the scope dies with its queues" is false for long-lived
  scopes — Logical mode, Blazor Server circuits, and the integration harness's single
  reused server scope — where surviving queues would drain into the *next* successful
  call). Clearing is not draining: PHASE-001's C4 constraint forbids running handlers on
  the failure path; it says nothing against discarding them there. The resulting
  invariant: **between entry calls, the scheduler is always empty.** (The
  `AspForbidException` denial shape throws and therefore clears; the `Authorized<T>`
  denial shape returns normally — a successful call whose body never ran — and drains an
  empty queue harmlessly.)
- **The entry stays active for the duration of the entry drain** (plan review B-V3):
  depth pops only after the drain completes. An event raised *by* a drained handler
  therefore still queues through the dispatcher and joins the current drain via the
  scheduler's drain-until-empty contract — preserving the sweep behavior installed by
  PHASE-001's gate-defect fix. "Raise outside a factory call" can never trigger during a
  drain.
- **Raise outside any factory call stops queueing.** When no entry call is active, phased
  handlers dispatch immediately with a debug-level log — the designed replacement for
  PHASE-001's interim "queue and hope someone drains" behavior. The existing no-scheduler
  fallback (9004) remains for scopes that have no scheduler at all.
- **Cancellation policy (closes plan-review B-C5):** entry-call drains pass no
  cancellation token (`CancellationToken.None`). By the time an `AfterCommit` drain runs,
  the entry call has already succeeded; honoring the request token would let a client
  disconnect — or a token cancelled between success and drain — fail a call that already
  succeeded, which is the exact failure mode B-C5 flagged. The scheduler's API and its
  drain-time-token contract are unchanged; the *entry caller* chooses `None`.

---

## Framework & Architectural Alignment

- **Persistence-agnostic:** "entry call completes" is the framework's only observable
  signal — no flush, no commit, no transaction awareness. (RFEF later builds transaction
  scoping on exactly this tracking; keep the seam clean.)
- **Forwarding-holder / trimming invariants (v1.7.0):** nothing in this plan ships handler
  bodies to trimmed clients; entry tracking in generated code must be inert in Remote-mode
  containers (no scheduler registered there) and sit behind the existing
  `NeatooRuntime.IsServerRuntime` guard structure where applicable.
- **Generator/runtime boundary:** generated code reaches the scheduler via the public
  `Neatoo.RemoteFactory.Internal` surface, matched by name. The Generator project must
  not link any new runtime source (the PHASE-001 CS0436 lesson; warning recorded in
  `FactoryEventHandlerAttribute`'s XML doc).
- **Wrapper-splitting precedent:** where a non-async generated method needs post-completion
  work, the established shape is the guard + `*Core` split (`RenderLocalMethodOpening`,
  TRIM-009); prefer extending that shape over inventing a new one.
- **Log events:** new ids continue the 9xxx phased-dispatch block in `Internal/Log.cs`
  with matching rows in `CLAUDE-DESIGN.md`'s Runtime Log Events table.

---

## Constraints & Invariants

Inherited from PHASE-001 (recorded at its Step 5 gate):

- **The drain call sits on the success path only** — never in a `finally`, never in a
  scope-disposal hook or middleware that runs on failure. Rollback-discard is emergent
  ("a scope that fails simply never drains"), so a drain on the failure path breaks the
  todo's AC-2 silently and no primitive-level test can catch it (code review C4).
- The scheduler API to call is `IFactoryEventPhaseScheduler.DrainAsync(phase,
  inTransaction, ct)` in `Neatoo.RemoteFactory.Internal` — public so generated code can
  reach it. Pass `inTransaction: false` at the entry-call drain point.
- The cancellation-token *policy* question (plan review B-C5) is resolved in this plan:
  entry drains pass `CancellationToken.None` — see Intent. The scheduler-level pin
  (`DrainAsync_HandlerReceivesTheDrainTimeCancellationToken`) stays valid: the drain-time
  token at an entry drain *is* `None`.
- `IFactoryEvents.RaiseUntyped` has no general test coverage repo-wide; it is the
  server-side landing point for client-raised events, so this plan's remote-entry work is
  the natural place to add it (tech debt raised at PHASE-001's gate).

New in this plan:

- **Depth correctness is the invariant everything hangs on:** a drain that fires at a
  nested completion (e.g., inside `LocalInsert` while `LocalSave` is still the entry)
  runs handlers mid-operation — in-transaction once RFEF exists. One entry, one drain,
  at the outermost successful completion only. Two known traps (plan review B-C6, B-C2):
  depth must release on *task completion*, not method return — `LocalSave` is a sync
  forwarder with four return paths that returns inner tasks directly, so a naive
  `try/finally` fires the drain mid-operation and `LocalSave` needs a split it doesn't
  have; and the scheduler's state is unsynchronized while long-lived scopes (Blazor
  Server circuits, Logical mode, the shared-scope harness) make concurrent flows in one
  scope realistic — the keyboard decides the synchronization posture and records it.
- **The authorization-forbidden path is a failure path.** The remote choke point returns
  a success-shaped empty response for `AspForbidException`; it must not drain, and it
  clears like any other failure. (The `Authorized<T>` denial shape is a *successful*
  call — see Intent.)
- **No silent loss from sync entries.** Some factory methods generate synchronous,
  non-`Task` signatures (value-object `Create`). A `Raise` inside one can enqueue phased
  work before any await. Whatever shape the keyboard picks (block-drain when pending,
  immediate-dispatch semantics for sync entries, or a generator diagnostic), queued work
  must not evaporate — and the chosen shape gets a Plan Amendment recording it. Note the
  sync shape is also **client-reachable**: `LocalCreate` on a value-object factory has no
  `IsServerRuntime` guard and ships into trimmed client assemblies, so whatever is
  emitted there must resolve services null-tolerantly and no-op cleanly (plan review
  B-C3).
- **Planned restatement of PHASE-001 pins — pre-declared, not test-gutting.** The
  following six tests (all in `FactoryEventsDispatcherPhaseTests` /
  `FactoryEventPhaseRegistrationTests` / `FactoryEventPhaseSchedulerTests`) pin interim
  behavior this plan is chartered to invert — the dispatcher queueing on a bare scope
  with no entry call active, and drained-handler re-raise semantics with no entry
  tracking. Each is amended with its original intent preserved and restated under entry
  semantics, and each amendment is listed in this plan's Test Evidence (plan review
  B-V4 widened this set beyond PHASE-001's three annotated bullets):
  - `Raise_DeferredHandler_DoesNotDispatchAtRaiseTime` → defers *during an entry call*.
  - `Raise_MixedPhases_ImmediateRunsAndDeferredWaits` → same restatement.
  - `RaiseUntyped_DeferredHandler_DefersJustLikeRaise` → RaiseUntyped parity, restated
    under an active entry (was never annotated interim — flagged by review as the gap).
  - `PhaseDispatcher_IsScoped_NotSharedAcrossScopes` → scope isolation, restated with
    entries active in each scope.
  - `ScopeDisposedWithoutDraining_RunsNothing` → intent survives; the equivalent designed
    behavior is now failure-clear plus never-queued-outside-entry.
  - `DrainedHandlerRaisingAnEvent_GoesThroughTheRealRaisePath` → re-pointed to run under
    entry-active-during-drain semantics so it can go red (review showed the current form
    stays green under either B-V3 answer).
  Every other existing test passes unmodified, including the Design solution's suite.
- **Remote-mode containers are untouched:** no scheduler, no tracker, no behavior change
  client-side; generated entry-tracking code must resolve services null-tolerantly.
- **The client-raise relay gap is not this plan's to fix.** `ForDelegateEvent` discards
  the response today, so nothing raised during a client-initiated `Raise` is ever relayed
  back — a pre-existing gap affecting Immediate handlers equally, with an echo-to-self
  design question attached (plan review A-V1). Recorded in the todo Discovery Log;
  this plan's remote-raise Acceptance claims the drain only.

---

## Steps

1. Add entry-call tracking to the runtime: per-scope, depth-aware begin/end with
   drain-on-outermost-success and clear-on-outermost-failure; the entry stays active
   until the drain completes. Whether it lives on the scheduler or as a small sibling
   service in `Internal` is a keyboard decision; it must be reachable from both runtime
   and generated code.
2. Wire the remote choke point: mark entry around delegate invocation in the portal
   request handler; on success, drain *before* the post-invoke cancellation check and
   before relay collection so drained-handler events join the same response. Forbidden
   and thrown paths never drain (they clear via the entry-exit failure path).
3. Change the dispatcher's queue-or-dispatch decision: queue phased handlers only while
   an entry call is active; otherwise dispatch immediately and log a new debug event id
   ("raise outside factory call"). Keep the existing no-scheduler fallback (9004).
4. Emit entry begin/drain in the class-factory renderer at the local execution seam
   (`Local*` methods), following the guard + `*Core` split precedent; verify the
   `LocalSave` → `LocalInsert`/`Update`/`Delete` nesting drains exactly once, with depth
   released on task completion, not method return.
5. Interface-factory renderer: **introduce** the guard + `*Core` split on its `Local*`
   seam — this leg has the guard inline today, no split, and its trimming behavior is
   explicitly unverified (TRIM Deferred Work item 20), so this is a trimming-invariant
   change, not a mechanical repeat of Step 4. Its delegate lambdas also route through
   the public wrapper, adding one nesting level the depth gating must absorb.
6. Static-factory renderer: mark entry in the server-side DI lambda seam (including
   `[Execute]` delegates) — the cheap leg: every delegate is `Task`-returning and the
   `IsServerRuntime` guard wraps the registration, not the lambda body, so an async
   lambda moves no guard into a state machine.
7. Resolve the sync (non-`Task`) factory-method shape at the keyboard under the
   no-silent-loss invariant; record the chosen shape as a Plan Amendment.
8. Amend the six pre-declared PHASE-001 pin tests (named in Constraints), preserving
   each test's original intent restated under entry semantics; list every amendment in
   Test Evidence.
9. Add `RaiseUntyped` remote-entry coverage: a client-raised event with phased handlers
   gets entry semantics (queued during dispatch, drained after the raise delegate
   completes). The relay half of that path is the deferred A-V1 gap — out of scope.
10. End-to-end integration coverage via `ClientServerContainers` with handlers registered
    through the registry's 3-arg overload (PHASE-002 not yet landed): success drain,
    relay batch inclusion, failure-clear (including a subsequent success in the same
    scope), handler-failure swallow not failing the response.
11. Add the new log event ids to `Internal/Log.cs` and the `CLAUDE-DESIGN.md` Runtime Log
    Events table.

---

## Acceptance

- [ ] An `AfterCommit` handler runs after the entry factory call completes for an
      HTTP-dispatched `[Remote]` call, and events it raises reach the client in the same
      response's relay batch. `[integration]`
- [ ] An `AfterCommit` handler runs after the entry factory call completes for a direct
      Logical/server-side invocation through the public factory wrapper (a *class*
      factory — interface factories register nothing in Logical mode). `[integration]`
- [ ] A `Save` on an entity (the `LocalSave` → `LocalInsert` nesting) drains exactly once,
      after the outermost save completes — never at the nested completion. `[integration]`
- [ ] Queued `AfterFlush` dispatches run before queued `AfterCommit` dispatches at the
      entry drain (sweep order observable at the entry level, not just the scheduler
      level). `[integration]`
- [ ] If the entry factory call throws, queued phased handlers never run — for both the
      remote choke point and the direct/local path. `[integration]`
- [ ] After a failed entry call, a *subsequent successful* call in the same scope runs
      only its own queued handlers — the failure's queued work was cleared, not left to
      ride the next drain (the long-lived-scope case: the harness's single reused server
      scope is the natural fixture). `[integration]`
- [ ] An authorization-forbidden remote call does not drain: an entry that enqueues
      phased work and then hits a forbidden call fails without running it (the falsifiable
      form — a bare forbidden call has an empty queue and proves nothing).
      `[integration]`
- [ ] An `AfterCommit` handler exception at the entry drain is logged (9003) and swallowed
      and does not fail the entry call's response; remaining queued handlers still run.
      `[integration]`
- [ ] A client-raised event (`RaiseUntyped` remote path) with phased handlers gets entry
      semantics: drained after the raise delegate completes. (Relay of that path is the
      deferred A-V1 gap — not claimed here.) `[integration]`
- [ ] An event raised *by* an `AfterCommit` handler during the entry drain, itself having
      phased handlers, joins the current drain — it is not dispatched inline as
      "outside a factory call" (entry-active-during-drain, B-V3). `[unit]`
- [ ] A server-side `Raise` outside any factory call with phase-registered handlers
      dispatches immediately with a debug-level log (no queue growth, no silent drop).
      `[unit]`
- [ ] The entry drain passes no cancellation token, and it runs *before* the choke
      point's post-invoke cancellation check: cancelling the request token after the
      delegate succeeds neither aborts nor skips the drain (exercised at the choke point
      — a scheduler-level assertion cannot fail on the placement risk). `[integration]`
- [ ] A synchronous (non-`Task`) factory method that enqueued phased work does not lose it
      silently. `[unit]`
- [ ] Nested factory calls (a factory method invoking another factory) do not drain at the
      inner completion. `[unit]`
- [ ] Backward compatibility: the full existing suite — unit, integration, AND the
      Design solution's suite (renderer changes regenerate every Design factory) —
      passes with only the six pre-declared pin amendments named in Constraints (each
      listed in Test Evidence with intent preserved). `[integration]`

---

## Current State (Pre-Flight)

*Walked 2026-08-14, on `PHASE-003-aftercommit-entry-call-drain` @ `18035aa` (stacked on
the PHASE-001 branch — its scheduler/registry/dispatcher work is not yet on `main`).*

**The remote choke point** — `src/RemoteFactory/HandleRemoteDelegateRequest.cs`,
`LocalServer.HandlePortalRequest` returns the `HandleRemoteDelegateRequest` delegate
(line 63). Success path: `method.DynamicInvoke(invokeParams)` (103), `await task` (115),
post-invoke cancellation check (137), relay collection from `IFactoryEventCollector`
(159–177), response serialization (181). Failure exits: `OperationCanceledException`
rethrow (190), `AspForbidException` → **returns a success-shaped
`RemoteResponseDto(string.Empty)`** (196–202) — a failure path despite the return, must
not drain — and general rethrow (203–208). The drain point goes between delegate
completion and relay collection. Both the AspNetCore endpoint
(`WebApplicationExtensions.cs:56`) and the integration-test containers
(`ClientServerContainers.cs:65,120`) invoke this same delegate — one choke point covers
every remote entry.

**Remote event raises land here too** — `RemoteFactoryEvents` (client) sends
`RaiseFactoryEventRemote`; the server registers that delegate as a scoped lambda
forwarding to `IFactoryEvents.RaiseUntyped` (`AddRemoteFactoryServices.cs:91–96`), so a
client `Raise` is just another delegate invocation through the choke point.

**DI wiring** — scheduler registered scoped for Server AND Logical
(`AddRemoteFactoryServices.cs:84–85`, `TryAddScoped`); `IFactoryEventCollector` is
Server-only (138); `HandleRemoteDelegateRequest` registered transient at 140–144 (the
AspNetCore package registers its own scoped copy, `ServiceCollectionExtensions.cs:33`).
Remote mode registers neither scheduler nor collector — client containers have no phased
machinery at all.

**The dispatcher's queue decision** — `FactoryEventsDispatcher.DispatchToHandlers`
(`FactoryEventsDispatcher.cs:49–84`): non-Immediate + scheduler present → `Enqueue` and
continue (69–75); scheduler absent → 9004 debug log then immediate dispatch (77–82).
There is no "is an entry call active" input to this decision today — that's the Step 3
change site.

**Generated class factory** (walked `Design.Domain…OrderFactory.g.cs`): public wrapper
`Create` forwards to `CreateProperty` (54–57), a delegate property the constructors point
at `LocalCreate` (server/logical ctor, 37–43) or `RemoteCreate` (remote ctor, 45–52).
`LocalCreate` is sync-returning-`Task` — ends in `Task.FromResult` (92). `LocalInsert` is
the guard + `LocalInsertCore` async split (150–157 → 157–218) — the TRIM-009 precedent
shape. `LocalSave` is a sync forwarder that returns `LocalInsert`/`LocalUpdate`/
`LocalDelete`'s task directly (370–391). The registrar's DI delegate lambdas route
`CreateDelegate`/`FetchDelegate`/`SaveDelegate` to the `Local*` methods (419–433) — that
is what `DynamicInvoke` executes on the HTTP path, bypassing the public wrappers.
**Depth note:** on the HTTP path the choke point holds depth 1 while `Local*` runs at
depth 2 — generated-code drains must be depth-gated or the HTTP path double-drains.

**Generated static factory** (walked `…ExampleCommandsFactory.g.cs`): no `Local*`
methods; the server-side registration is a DI lambda that resolves `[Service]` parameters
and invokes the hidden `_Method` (30–37); the Remote registration is a lambda forwarding
to `IMakeRemoteDelegateRequest` (20–23). The server lambda body is the entry seam. The
lambda is non-async, returning the user method's task directly.

**Generated interface factory** (walked `…IExampleRepositoryFactory.g.cs`) — **corrected
per plan review B-V1; the original walk got this wrong.** It has `Local*` methods (58,
76, 94) but is NOT the class renderer's shape: `InterfaceFactoryRenderer.RenderLocalMethod`
(`InterfaceFactoryRenderer.cs:250–309`) emits the `IsServerRuntime` guard **inline**
(279–281) with no guard + `*Core` split and a conditional `async` keyword (252); its own
comment (272–278) marks body elimination on this leg UNVERIFIED — TRIM Deferred Work
item 20. Making its sync `Local*` forwarders `async` to await a drain would lower the
guard into `MoveNext`, the measured-bad shape `ClassFactoryRenderer.cs:350–368`
documents — so this leg gets the split *introduced*, not repeated. Its registrar lambdas
also route through the **public wrapper** (`IExampleRepositoryFactory.g.cs:122, 127,
132` call `factory.GetAllAsync()` → delegate property → `Local*`), one extra nesting
level vs. the class leg's direct `Local*` routing. And it registers for `Remote` (106)
and `Server` (115) only — **Logical mode registers nothing** for interface factories.

**Sync factory methods are real** — `…MoneyFactory.g.cs` generates
`public virtual Money Create(...)` (37) and a sync `LocalCreate` (42): non-`Task`
signatures with no place to await a drain. Source of the no-silent-loss constraint.

**Renderer seams** — `ClassFactoryRenderer.RenderLocalMethodOpening`
(`ClassFactoryRenderer.cs:370`, called from 5 sites: 422, 846, 899, 1117, 1384) is the
wrapper-splitting helper; `InterfaceFactoryRenderer.RenderLocalMethod`
(`InterfaceFactoryRenderer.cs:250`); `StaticFactoryRenderer` emits the registrar lambdas.
No shared pipeline helper across the three — each gets its own emission change.

**Scheduler surface** (PHASE-001) — `IFactoryEventPhaseScheduler` in
`Internal/FactoryEventPhaseScheduler.cs`: `bool HasPending`, `Enqueue(phase, evt,
options, invoke)`, `DrainAsync(phase, inTransaction, ct = default)`; drain sweeps the
requested phase and all earlier phases, earliest first, drain-until-empty; `inTransaction:
false` → swallow + 9003, OCE rethrows. No entry-call/depth state exists anywhere yet, no
`Clear` on the interface, and the internal `Dictionary<DispatchPhase, Queue<>>` is
unsynchronized.

**Two registrations of the choke point** — the core package registers
`HandleRemoteDelegateRequest` transient (`AddRemoteFactoryServices.cs:140–144`); the
AspNetCore package registers a scoped copy that wins in a real server
(`ServiceCollectionExtensions.cs:33`). Both capture the resolving provider, so tracker
resolution sees the request scope either way — but "one choke point" means one code
path, not one registration.

**Harness scope lifetime** — `ClientServerContainers` creates **one** server scope per
`Scopes()` call and reuses it for every remote call in a test (146–158, 62–65). Queues,
collector contents, and depth state persist across calls within a test — the fixture
that makes the failure-then-success Acceptance bullet natural, and a fidelity caveat for
every "drains exactly once" assertion.

---

## Test Evidence

*(filled before the Step 5 gate)*

---

## Plan Amendments

*(none yet)*

---

## Notes

- **Branching:** this plan's branch is stacked on `PHASE-001-phase-model-and-queueing`
  rather than the `PHASE` todo branch — PHASE-001's implementation (scheduler, registry
  phase, dispatcher queueing) hasn't merged to `main` yet and this plan builds directly
  on it. When PHASE-001 merges, this branch rebases/merges forward per the conventions'
  "pull main back" step.
- **Ordering:** worked ahead of PHASE-002 (see the todo's Discovery Log entry of
  2026-08-14). Tests here register phased handlers via the registry's 3-arg overload;
  PHASE-002 later makes the attribute's phase argument flow end-to-end and owns the
  duplicate-registration diagnostic decision.
