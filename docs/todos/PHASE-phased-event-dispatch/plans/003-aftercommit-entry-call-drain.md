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
`LocalSave` nests into `LocalInsert`/`LocalUpdate`/`LocalDelete`; HTTP calls enter `Local*`
directly, bypassing public wrappers. This plan does NOT own the consumer-facing drain API
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
  same HTTP response's relay batch. Local/direct entries (Logical mode, server-side code
  calling a factory) are marked by generated code at the local execution seam in all three
  factory patterns. Both families drain by calling the PHASE-001 scheduler
  (`DrainAsync(AfterCommit, inTransaction: false, …)` — which sweeps `AfterFlush` first,
  giving PHASE-004's fail-open its drain point for free).
- **Failure discards structurally.** The drain call exists only on the success path. An
  entry call that throws — or is forbidden by authorization — simply never drains; the
  scope dies with its queues. No discard code, nothing to get wrong on the failure path.
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
  at the outermost successful completion only.
- **The authorization-forbidden path is a failure path.** The remote choke point returns
  a success-shaped empty response for `AspForbidException`; it must not drain.
- **No silent loss from sync entries.** Some factory methods generate synchronous,
  non-`Task` signatures (value-object `Create`). A `Raise` inside one can enqueue phased
  work before any await. Whatever shape the keyboard picks (block-drain when pending,
  immediate-dispatch semantics for sync entries, or a generator diagnostic), queued work
  must not evaporate — and the chosen shape gets a Plan Amendment recording it.
- **Planned restatement of PHASE-001 interim pins — not test-gutting.** PHASE-001's gate
  annotated specific tests as pinning interim behavior this plan is chartered to invert
  (dispatcher queues whenever a scheduler exists; nothing drains at entry). Those tests
  are amended here with intent preserved and each amendment listed in this plan's Test
  Evidence; every other existing test passes unmodified.
- **Remote-mode containers are untouched:** no scheduler, no tracker, no behavior change
  client-side; generated entry-tracking code must resolve services null-tolerantly.

---

## Steps

1. Add entry-call tracking to the runtime: per-scope, depth-aware begin/end with
   drain-on-outermost-success. Whether it lives on the scheduler or as a small sibling
   service in `Internal` is a keyboard decision; it must be reachable from both runtime
   and generated code.
2. Wire the remote choke point: mark entry around delegate invocation in the portal
   request handler; on success, drain before relay collection so drained-handler events
   join the same response. Forbidden and thrown paths never drain.
3. Change the dispatcher's queue-or-dispatch decision: queue phased handlers only while
   an entry call is active; otherwise dispatch immediately and log a new debug event id
   ("raise outside factory call"). Keep the existing no-scheduler fallback (9004).
4. Emit entry begin/drain in the class-factory renderer at the local execution seam
   (`Local*` methods), following the guard + `*Core` split precedent; verify the
   `LocalSave` → `LocalInsert`/`Update`/`Delete` nesting drains exactly once.
5. Do the same for the interface-factory renderer (its `Local*` seam) and the
   static-factory renderer (its server-side DI lambda seam, including `[Execute]`
   delegates).
6. Resolve the sync (non-`Task`) factory-method shape at the keyboard under the
   no-silent-loss invariant; record the chosen shape as a Plan Amendment.
7. Amend the PHASE-001 interim-behavior tests flagged for restatement (dispatcher
   queue-when-scheduler-present pins), preserving each test's original intent; list every
   amended test in Test Evidence.
8. Add `RaiseUntyped` remote-entry coverage: a client-raised event with phased handlers
   gets entry semantics (queued during dispatch, drained after the raise delegate
   completes, relayed in the same response).
9. End-to-end integration coverage via `ClientServerContainers` with handlers registered
   through the registry's 3-arg overload (PHASE-002 not yet landed): success drain, relay
   batch inclusion, rollback-discard, handler-failure swallow not failing the response.
10. Add the new log event ids to `Internal/Log.cs` and the `CLAUDE-DESIGN.md` Runtime Log
    Events table.

---

## Acceptance

- [ ] An `AfterCommit` handler runs after the entry factory call completes for an
      HTTP-dispatched `[Remote]` call, and events it raises reach the client in the same
      response's relay batch. `[integration]`
- [ ] An `AfterCommit` handler runs after the entry factory call completes for a direct
      Logical/server-side invocation through the public factory wrapper. `[integration]`
- [ ] A `Save` on an entity (the `LocalSave` → `LocalInsert` nesting) drains exactly once,
      after the outermost save completes — never at the nested completion. `[integration]`
- [ ] Queued `AfterFlush` dispatches run before queued `AfterCommit` dispatches at the
      entry drain (sweep order observable at the entry level, not just the scheduler
      level). `[integration]`
- [ ] If the entry factory call throws, queued phased handlers never run — for both the
      remote choke point and the direct/local path. `[integration]`
- [ ] An authorization-forbidden remote call does not drain. `[integration]`
- [ ] An `AfterCommit` handler exception at the entry drain is logged (9003) and swallowed
      and does not fail the entry call's response; remaining queued handlers still run.
      `[integration]`
- [ ] A client-raised event (`RaiseUntyped` remote path) with phased handlers gets entry
      semantics: drained after the raise completes, relayed in the same response.
      `[integration]`
- [ ] A server-side `Raise` outside any factory call with phase-registered handlers
      dispatches immediately with a debug-level log (no queue growth, no silent drop).
      `[unit]`
- [ ] The entry drain passes no cancellation token: cancelling the request token after the
      entry call succeeds does not abort the drain. `[unit]`
- [ ] A synchronous (non-`Task`) factory method that enqueued phased work does not lose it
      silently. `[unit]`
- [ ] Nested factory calls (a factory method invoking another factory) do not drain at the
      inner completion. `[unit]`
- [ ] Backward compatibility: the full existing suite passes with only the pre-declared
      PHASE-001 interim-pin amendments (each listed in Test Evidence with intent
      preserved). `[integration]`

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

**Generated interface factory** (walked `…IExampleRepositoryFactory.g.cs`): shaped like
the class factory — `Local*` methods (58, 76, 94) with delegate registrations — same seam
as the class renderer.

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
false` → swallow + 9003, OCE rethrows. No entry-call/depth state exists anywhere yet.

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
