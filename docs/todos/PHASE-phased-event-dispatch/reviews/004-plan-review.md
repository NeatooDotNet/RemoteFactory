# PHASE-004 Plan Review

**Plan:** [../plans/004-afterflush-coordinator.md](../plans/004-afterflush-coordinator.md)
**Reviewer:** plan-reviewer agent
**Date:** 2026-08-15
**Verdict:** CONCERNS (5 veto-tier findings — 2 Pass A, 3 Pass B; direction sound)
**Outcome:** All five vetoes adopted; draft amended before implementation. Disposition table at the end.

---

## Reviewer Report (verbatim)

**Plan style:** Prescriptive (iterative-todo)

### Pass A — Plan vs. Documented Requirements

**Requirement docs consulted:**
- `src/Design/CLAUDE-DESIGN.md` (lines 259-264 factory-events rules; 1006-1024 diagnostics + Runtime Log Events tables)
- `src/Design/Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs`, `.../FactoryEventRelayPattern.cs`, `src/Design/Design.Tests/FactoryTests/FactoryEventHandlerTests.cs` (no phase coverage yet)
- `docs/todos/PHASE-phased-event-dispatch/todo.md` (Goal, AC-1…AC-9, Out of Scope, Discovery Log)
- `docs/todos/PHASE-phased-event-dispatch/reviews/003-code-review.md` (C2 charter), `plans/001-phase-model-and-queueing.md`, `plans/005-design-docs-skill.md`
- `docs/` published docs + `skills/RemoteFactory/` — swept for phase/OCE prose: **none exists yet** (PHASE-005 owns it), so the XML docs and CLAUDE-DESIGN are the only shipped statements.

**Veto-tier findings**

**A-V1 — The OCE restatement contradicts two rows of `CLAUDE-DESIGN.md`, and neither is in the pre-declared amendment set.**
CLAUDE.md designates `src/Design/` as the requirements source of truth. Two rows in the Runtime Log Events table state the behavior this plan inverts:
- `src/Design/CLAUDE-DESIGN.md:1021` (9003): "*Swallowed — … remaining queued handlers still run. `OperationCanceledException` still propagates.*"
- `src/Design/CLAUDE-DESIGN.md:1024` (9006): "*a failed call's clear, **or the leftovers of a drain a handler's `OperationCanceledException` aborted***" — after this plan the entry drain can never be aborted by a handler OCE (it passes `CancellationToken.None`, whose `IsCancellationRequested` is permanently false), so that second cause becomes unreachable. PHASE-003's code review C4 authored that wording specifically for that cause.

The plan's Constraints assert "no signature or behavior breaks outside the pre-declared set," and the set lists only the two XML sites plus the two tests. *Options (advisory):* add both rows to the pre-declared set, **or** hand them to PHASE-005 with the anchors recorded — the arc's own precedent (Discovery Log 2026-08-15, "docs this plan invalidates — handed to PHASE-005"). Doing neither leaves the requirements doc actively contradicting shipped behavior.

**A-V2 — The consumer-drain-happened flag is narrower than todo AC-5, and the plan does not name the gap.**
AC-5: "*AfterFlush handlers **never drained by the consumer** run at the AfterCommit point with a logged warning (fail-open).*" The plan's mechanism (Step 3: "*remember whether a consumer AfterFlush drain happened during the current entry call*") suppresses the warning for the **remainder of the entry call** after any drain. Three cases exist; the plan names two:
1. never drained at all → warns (bullet 4) ✅
2. later-phase handler raises AfterFlush work mid-drain → silent (the documented carve-out, bullet 5) ✅
3. **consumer's own code raises AfterFlush work after its drain, before completion** → silent, unnamed, and literally "never drained by the consumer" under AC-5.

Note the plan's Intent paragraph already describes the narrower behavior ("*a consumer who declared AfterFlush and **forgot to wire a drain*** finds out from their logs"), while the Constraint frames the carve-out as only the handler-raise case. So the plan is internally inconsistent about which promise it is keeping, and neither acceptance bullet can detect case 3. This matters in the dominant real-world shape (an RFEF-style abstraction that always drains once at the outermost flush): the warning degrades to a "no drain wired anywhere" signal.
*Options (advisory, pick one and record it):* (a) restate AC-5 the way AC-3 is being restated, scoping the warning to "no consumer drain in this entry call"; (b) replace the per-entry-call boolean with a per-dispatch mark of "enqueued while a drain was in progress" — the scheduler already knows it is inside `DrainAsync`, that discriminator matches the Constraint's carve-out exactly, covers case 3, and needs no entry-state reset (which also disposes of B-C1).

**Callout-tier findings**

- **A-C1 — The AC-3 restatement is legitimately chartered, verified.** `reviews/003-code-review.md:29` (C2): "*Recorded; the 'swallow OCE at a post-completion drain?' question handed to PHASE-004 (its stub carries it).*" Not a contradiction — this is the plan exercising a delegated decision, and Step 4 correctly routes the restatement into `todo.md`. Two residues: the restatement must actually land in the todo (not only in the plan), and `plans/001-phase-model-and-queueing.md:123` / `:191` carry an Acceptance bullet and a Test Evidence row naming `DrainAsync_PostCompletion_StillPropagatesCancellation` as evidence for the inverted claim. Arc precedent is to leave completed plans as historical record (PHASE-003 inverted PHASE-001 pins the same way) — but the Step 7 close-out audit will read them.
- **A-C2 — The Design-projects obligation (todo AC-9 / CLAUDE.md Step 7B) is chartered to PHASE-005 but never named here.** 005's Scope already covers "`CLAUDE-DESIGN.md` (pattern narrative and the log-id table)", so the new 9007 row, the coordinator narrative, and the registration bullets near `CLAUDE-DESIGN.md:259-264` trace. But plan 004 names PHASE-007 for log-pin reuse and never names 005 — a reader cannot tell the public-interface demonstration is owned. One line in Notes closes it.
- **A-C3 — AC-1's ordering guarantee changes scope once a consumer owns the AfterFlush drain point.** `src/RemoteFactory/DispatchPhase.cs:16-19` states it unconditionally ("all `Immediate` handlers complete before any `AfterFlush` handler runs"). With a consumer-signaled drain, a factory body that raises an `Immediate` event *after* calling the coordinator inverts it — and that is **not** the existing carve-out at `:21-27` (work created by a drained handler). Step 6 says "verify the prose matches what actually shipped"; this is the sentence to verify.

### Pass B — Plan vs. Codebase

**Files Examined**

*Runtime (core):* `src/RemoteFactory/Internal/FactoryEventPhaseScheduler.cs`, `src/RemoteFactory/Internal/FactoryEntryCall.cs`, `src/RemoteFactory/FactoryEventsDispatcher.cs`, `src/RemoteFactory/DispatchPhase.cs`, `src/RemoteFactory/FactoryEventHandlerAttribute.cs`, `src/RemoteFactory/Internal/Log.cs`, `src/RemoteFactory/AddRemoteFactoryServices.cs`, `src/RemoteFactory/HandleRemoteDelegateRequest.cs`
*Generator (service-resolution shape):* `src/Generator/Renderer/ClassFactoryRenderer.cs`, `InterfaceFactoryRenderer.cs`, `StaticFactoryRenderer.cs`, `RelayHandlerRenderer.cs`
*Tests:* `src/Tests/RemoteFactory.UnitTests/Internal/FactoryEventPhaseSchedulerTests.cs`, `.../FactoryEntryCallTests.cs`, `src/Tests/RemoteFactory.IntegrationTests/TestContainers/ClientServerContainers.cs`, `.../Events/Phases/FactoryEventPhaseEntryTests.cs`, `.../Events/Phases/FactoryEventPhaseAttributeTests.cs`, `.../TestTargets/Events/FactoryEventPhaseAttributeTargets.cs`

**Codebase Reality Check**

The plan's direction matches the code. The scheduler already implements the total sweep (`TryDequeueThrough`, `FactoryEventPhaseScheduler.cs:280-299`) and already returns the *queued* phase, so the warning discriminator is genuinely plumbed as the inherited section claims — no re-plumbing needed. Failure semantics already key off `inTransaction` rather than the phase (`:216-235`), so "coordinator drains in-transaction, entry drains post-completion" is the shape the code is built for, not a new concept. Registration parity is exactly as claimed: `TryAddScoped<IFactoryEventPhaseScheduler>` sits in the non-Remote `else` branch (`AddRemoteFactoryServices.cs:84-85`), i.e. Server + Logical, absent in Remote. `[Service]` parameters resolve through `GetRequiredService<T>()` on every leg (`ClassFactoryRenderer.cs:852,1643`, `InterfaceFactoryRenderer.cs:564`, `StaticFactoryRenderer.cs:224`), so a Remote-mode consumer injecting the coordinator on a non-`[Remote]` method gets the standard "no service for type … has been registered" `InvalidOperationException` — precisely the client/server-boundary failure CLAUDE.md documents. Pressure point 6 checks out.

The Constraints' claim about cancellation is also accurate and I verified it rather than inheriting it: the choke point's post-invoke `cancellationToken.ThrowIfCancellationRequested()` is at `HandleRemoteDelegateRequest.cs:166`, *after* `EndEntryCallAsync(success: true)` at `:152` — so `TokenCancelledAfterTheEntryCallSucceeds_DrainStillRuns` stays green under the OCE restatement, and that OCE is genuine cooperative cancellation that keeps propagating. Likewise the pre-declared pin analysis holds: with the swallow in place `EndEntryCallAsync(true)` no longer throws, `ClearAtExit` still runs in the `finally` (`:167-177`), and `HandlerThrowsOperationCanceled_MidDrain_...`'s premise does dissolve exactly as the plan says.

**Veto-tier findings**

**B-V1 — "Framework-owned phases rejected" invites a blacklist; the sweep makes a blacklist unsafe on a public API.**
`DrainAsync` sweeps every phase `p <= through` (`FactoryEventPhaseScheduler.cs:284`). The todo's own Discovery Log (2026-08-15, "undefined enum values are expressible and will not drain") records that `[FactoryEventHandler<T>((DispatchPhase)99)]` compiles and the cast flows through — the same expressibility applies to a coordinator call. So `coordinator.DrainAsync((DispatchPhase)99, ct)` under a `!= AfterCommit` guard would sweep the **AfterCommit** queue with `inTransaction: true`: framework-owned post-completion handlers running inside the consumer's transaction with propagating exceptions, which is the exact separation the phase model exists to create. `Immediate` is a second undefined corner (harmless in effect — that queue is always empty — but undefined in contract; the plan says "AfterFlush only" in Step 1 and then names only `AfterCommit` as rejected). Acceptance bullet 8 as written is satisfied by the blacklist. This is a public API shipping in a minor release, so tightening it later is a breaking change.
*Option (advisory):* accept only `AfterFlush`, throw for everything else, and have the acceptance bullet name an undefined value alongside `AfterCommit`.

**B-V2 — Acceptance bullet 2 is the bullet most likely to pass against a no-op coordinator, and it is the one bullet Step 7's red-proof list omits.**
Since PHASE-002, an attribute-declared `AfterFlush` handler **already runs end to end today**, swept at the entry drain (the todo's own Discovery Log entry "AfterFlush became consumer-reachable ahead of its drain point"; pinned by `FactoryEventPhaseSchedulerTests.DrainAsync_SweepsAnEarlierPhaseTheConsumerNeverDrained:255`). So "runs at the consumer's drain point, for both remote and logical invocation" is green against a coordinator that does nothing at all — unless the assertion carries the before-`*-method-done` marker discriminator the existing phase tests use (`FactoryEventPhaseAttributeTests.cs:54`, `FactoryEventPhaseEntryTests.cs:45`). Step 7 pre-commits to red-proofing three discriminators — ordering, warning fire/no-fire, behind-the-OCE dispatch — and bullet 2 is not among them. That is the precise failure recorded in the Discovery Log on 2026-08-15: "*the first where red-proofing four discriminators did not by itself prevent a fifth from slipping through, because the unproofed one was not on the list.*" Bullet 4 carries a lighter version of the same defect: "*still runs (swept at the entry drain)*" is already true today; only the warning-id half can go red.

**B-V3 — Acceptance bullet 8's "benign no-op outside any entry call" is unfalsifiable, and it conceals an undecided contract choice.**
Outside an entry call the queues are empty by construction (`ClearAtExit` empties them at depth 0, `FactoryEventPhaseScheduler.cs:259-266`), so *every* implementation passes that bullet — delegating unconditionally to `scheduler.DrainAsync` and short-circuiting on `!IsEntryCallActive` are indistinguishable by it. They are not equivalent in behavior: `Enqueue` is public, and under the documented per-scope limitation (pinned by `FactoryEntryCallTests.ConcurrentFlowsInOneScope_ShareEntryState_FailedFlowsWorkRidesTheSurvivingDrain:366`) "outside *my* entry call" can mean "inside another flow's live entry call," where an unconditional delegate drains that flow's queued work in-transaction on the wrong token. Related: the XML text Step 1 prescribes — "*the queues are empty by construction*" — is an overclaim under that same limitation and would ship as public documentation. Decide delegate-vs-short-circuit, and either give the bullet something that can fail or mark the clause explicitly as a contract statement checked at code review.

**Callout-tier findings**

- **B-C1 — Flag lifetime in long-lived scopes.** The flag must be set only while `IsEntryCallActive` and reset inside `ClearAtExit`'s depth-0 block (`FactoryEventPhaseScheduler.cs:259-266`) — the same place the queues clear, reached by both the success and failure exits. If it is set by a drain while no entry is active, or reset outside the depth-0 block, a long-lived scope (Blazor Server circuit, Logical mode, and the integration harness's single server scope reused across every remote call in a test — see `FactoryEventPhaseEntryTests.FailedCall_ThenSuccessfulCall_InTheSameServerScope...:139`) suppresses the warning permanently after the first drain, and bullet 4 would silently depend on test ordering. Also worth one recorded line: under the concurrent-flows limitation, flow A's drain suppresses flow B's warning — a consequence of the documented granularity, not a redesign trigger.
- **B-C2 — Split-brain registration hazard.** `IFactoryEventPhaseScheduler` is registered with a factory lambda that news up the implementation (`AddRemoteFactoryServices.cs:84-85`). Registering the coordinator as `TryAddScoped<IFactoryEventPhaseCoordinator, FactoryEventPhaseScheduler>()` — or any registration that constructs its own instance — gives each scope **two** schedulers with independent queues and depth counters, and the coordinator's drain quietly finds nothing. The coordinator must resolve the scope's existing scheduler. Loud when it breaks (bullet 2 fails), cheap to avoid.
- **B-C3 — The Notes' claim about needing a new unit-tier log capture is false.** `FactoryEventPhaseSchedulerTests` already has `CapturingLoggerProvider` and `NewDispatcher(out var logs)` (lines 19-71) and already pins event id 9003 twice (`:274`, `:323`). There is nothing to build for the carve-out bullet, and PHASE-007's queued 9002/9004/9006 pins can reuse what exists. ("Verify, don't inherit.")
- **B-C4 — The `!cancellationToken.IsCancellationRequested` discriminator: two edges worth carrying to the keyboard.** (i) At the entry drain the token is `CancellationToken.None` (`FactoryEventPhaseScheduler.cs:169`), so the discriminator is constant-false and *every* handler OCE is swallowed there — that is the intended semantics and it makes the policy deterministic on the framework's only post-completion drain; state it that way in the XML rather than implying runtime discrimination happens. (ii) On the public `DrainAsync(phase, inTransaction: false, liveToken)` surface, an ambient cancellation arriving mid-drain flips the discriminator for a handler whose OCE has nothing to do with it, **and the propagation abandons the rest of the queue** — the half of AC-3 ("remaining queued handlers still run") the restatement exists to protect. `OperationCanceledException.CancellationToken` is the sharper discriminator if the keyboard wants provenance rather than state. Either way, acceptance bullet 7's "a genuinely cancelled token still propagates" should say whether the remaining queue runs or is abandoned.
- **B-C5 — Warning granularity latitude is acceptance-neutral, message content is not.** Once-per-entry-call vs per-dispatch leaves nothing acceptance-relevant unpinnable (bullet 4 is a `Contains`, bullet 5 a `DoesNotContain`; both pass either way). But a once-per-call warning cannot name the event type, and AC-5's purpose is that the consumer can act on the log line. One sentence on what the message must identify (event type per dispatch, or a count) closes it. If "once per call" is implemented with a second latch, that latch inherits B-C1's reset hazard.
- **B-C6 — Bullet 1's second half is a regression pin, not a discriminator.** "When the entry call then fails, nothing queued survives into the next entry call" is already-shipped PHASE-003 behavior (`ClearAtExit`), green regardless of what this plan does. Harmless to keep; just not evidence for the coordinator.
- **B-C7 — One more amendment site to check at Step 6.** `src/RemoteFactory/FactoryEventHandlerAttribute.cs:7-14` describes phase semantics in consumer-facing XML; Step 6 names `DispatchPhase`, scheduler, and dispatcher but not the attribute. Verify rather than assume it needs no edit.

**Code-density / transcription smell**

Plan is design-focused — no line numbers, no signatures, no parameter lists, no file-by-file edit tables, no code fences. The two places that name concrete symbols are load-bearing and correct: the pre-declared pin-amendment set (the sacred-tests rule requires naming them before the first edit) and the inherited-constraint citations that carry provenance. No smell.

### Plan Index / Companion Plans

- Plan 004 appears in the parent Plan Index as `Draft` with a matching number and title. ✅
- Deferral phrases swept:
  - "PHASE-007's queued 9002/9004/9006 pins can reuse it" → Index row 007 exists (Draft). ✅
  - "**Client-raise relay gap** … Recommendation: out of this plan … Decision recorded when the user rules on it" → traces to the Discovery Log entry of 2026-08-14, whose Follow-up is "revisit at PHASE-004 … **or at todo close**." No Index row, by design (it is a decision to be made, not queued work). **Callout:** the plan should name the close-out audit as the fallback venue so a non-ruling doesn't lose it, and note that a "yes, it warrants work" ruling needs a `Draft` Index row (the arc's "no orphan plan files" rule).
  - Design projects / published docs / new 9007 log-id row → chartered to Index row 005, whose Scope explicitly covers `CLAUDE-DESIGN.md` — but 004 never names 005 (see A-C2).

---

## Orchestrator Disposition (2026-08-15)

Calibration applied: diagnoses trusted; prescriptions treated as one option each.

| Finding | Tier | Disposition |
|---|---|---|
| A-V1 | veto | **Adopted — amendment set widened.** The `CLAUDE-DESIGN.md` 9003 and 9006 rows join the pre-declared set; the new warning's row is added in the same edit. Doc deltas ship with the behavior change (project rule); handing a requirements-doc contradiction to 005 would leave the source of truth false in the interim. |
| A-V2 | veto | **Adopted — reviewer's option (b).** Per-dispatch discriminator: any `AfterFlush` dispatch swept at the post-completion drain warns unless it was created during that drain (the carve-out). AC-5 stands as written; case 3 (consumer raises after their own drain) warns — which is both AC-5's letter and actionable. Intent/Constraints reworded; the fire/no-fire acceptance pair now covers case 3. Kills B-C1's per-entry-flag reset hazard as a side effect. |
| B-V1 | veto | **Adopted.** Whitelist: `AfterFlush` only; `AfterCommit`, `Immediate`, and undefined casts all rejected. Acceptance bullet names `AfterCommit` and `(DispatchPhase)99`. |
| B-V2 | veto | **Adopted.** Bullet 2 now requires the AfterFlush marker *before* the method-completion marker (the one thing a no-op coordinator cannot produce); bullet 4 reworded so the warning id is the load-bearing half; the consumer-drain marker ordering heads Step 7's red-proof list. |
| B-V3 | veto | **Adopted — short-circuit decided.** Outside an entry call the coordinator never delegates (per-scope concurrency makes unconditional delegation drain another flow's in-transaction work on the wrong token). Bullet 8 pins it falsifiably via direct `Enqueue` + work-left-untouched. Prescribed XML softened to acknowledge the per-scope limitation. |
| A-C1 | callout | Charter verified by the reviewer. Step 4 now states the AC-3 restatement lands in `todo.md` itself. Plan-001's historical rows left untouched per arc precedent; this disposition is the close-out audit's pointer. |
| A-C2 | callout | PHASE-005 named in Notes as owner of the Design demonstration, pattern narrative, and published docs/skill; this plan takes only the log-event table rows its behavior change falsifies. |
| A-C3 | callout | Added to Step 6: scope `DispatchPhase`'s unconditional ordering sentence. |
| B-C1 | callout | Largely dissolved by A-V2's adoption (no per-entry-call flag). Residual keyboard gotcha recorded in Notes: the per-dispatch mark must not outlive its drain in long-lived scopes. |
| B-C2 | callout | Promoted to a Constraint: the coordinator resolves the scope's existing scheduler instance. |
| B-C3 | callout | Notes corrected — `CapturingLoggerProvider` already exists; nothing to build. |
| B-C4 | callout | **Decided:** a propagating cooperative cancellation abandons the rest of the drain; abandoned dispatches stay queued for the exit clear. Bullet 7 says so. Provenance discrimination (`OperationCanceledException.CancellationToken`) recorded in Notes as the keyboard's option. |
| B-C5 | callout | Falls out of A-V2's adoption: the warning is per-dispatch and names the event type. Recorded in Step 3. |
| B-C6 | callout | Accepted as a regression pin; clause kept, no change. |
| B-C7 | callout | `FactoryEventHandlerAttribute` added to Step 6's verification list. |
| Index callout | callout | Close-out audit named in Notes as the fallback venue for the client-raise relay ruling; a "warrants work" ruling requires a Draft index row first. |
