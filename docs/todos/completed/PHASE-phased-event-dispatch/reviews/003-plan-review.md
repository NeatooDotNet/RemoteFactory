# PHASE-003 Plan Review — 2026-08-14

**Reviewer:** plan-reviewer agent (two-pass: A = documented requirements, B = codebase)
**Plan reviewed:** [../plans/003-aftercommit-entry-call-drain.md](../plans/003-aftercommit-entry-call-drain.md) (draft as of commit `d25bef7`)
**Verdict: CONCERNS** — 6 veto-tier findings, 10 callouts. All vetoes addressed by
draft amendment before implementation; disposition recorded at the end of this file.

---

## Pass A — Plan vs. Documented Requirements

### Veto-tier

**A-V1 — Acceptance bullet 8's "relayed in the same response" is unreachable, and
satisfying it naively breaks a documented contract.**
The client-raise path has no relay today: `MakeRemoteDelegateRequest.ForDelegateEvent`
(`src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs:167-201`) awaits the round-trip
and discards the response — never reads `RelayedEvents`, never touches
`IFactoryEventRelay` (compare `ForDelegateNullable` at `:114-148`, which does). The
integration stand-in mirrors this (`ClientServerContainers.cs:111-121` vs `:75-106`).
The server *does* collect the client-raised event (`FactoryEventsDispatcher.cs:52-55`
runs for `RaiseUntyped` too), so naively wiring relay into `ForDelegateEvent` would echo
the client's own just-raised event back into its own `IFactoryEventRelay` — a behavior
change against CLAUDE-DESIGN.md's "One `[Remote]` call = exactly one `Relay` invocation"
semantics, decided in a plan that doesn't own the relay path.

**A-V2 — "Rollback-discard is structural" is false for long-lived scopes, violating
todo AC-2.**
"The scope dies with its queues" holds only for per-operation scopes. In Logical mode,
Blazor Server (scope per circuit), and this repo's own integration harness
(`ClientServerContainers.cs:146-158` creates one server scope per `Scopes()` call, reused
for every remote call at `:62-65`), a failed entry call's queued work survives in
`FactoryEventPhaseScheduler._deferred`, and the sweep + drain-until-empty design
guarantees the *next successful entry call in that scope* drains it — handlers AC-2 says
must never run then run later, attached to an unrelated operation. The plan's
single-failing-call Acceptance bullet cannot catch this. Distinction the plan conflated:
PHASE-001 code review C4 forbids *draining* on the failure path — it says nothing about
*clearing*.

### Callout-tier

- **A-C1** — todo AC-6 traces cleanly; "fresh-scope execution out of scope" honored. No
  conflict.
- **A-C2** — Renderer changes regenerate every Design factory; the backward-compat
  Acceptance bullet should name the Design solution's test run explicitly (PHASE-001
  recorded it as a separate 86×2 run).
- **A-C3** — CLAUDE-DESIGN.md log-table location confirmed; new ids continue cleanly
  after 9004.

---

## Pass B — Plan vs. Codebase

Current State verified largely accurate (choke-point line map, DI wiring, dispatcher
decision sites, class/static shapes, `MoneyFactory` sync shape, renderer seam
locations). `[Execute]`-on-class, ctor-injected, and `[Execute]`+ctor shapes all funnel
through the class renderer's `Local*` seam (covered by Step 4). `ILazyLoadFactory`
re-enters through normal factory seams — not an uncovered entry point. The dispatcher
*can* learn "entry active" from its position (scoped, same scope provider).

### Veto-tier

**B-V1 — The interface renderer is NOT "the same seam as the class renderer."**
`InterfaceFactoryRenderer.RenderLocalMethod` (`InterfaceFactoryRenderer.cs:250-309`)
emits the `IsServerRuntime` guard **inline** with **no** guard + `*Core` split; its own
comment records "THIS LEG HAS RECEIVED NEITHER FIX … Treat body elimination on this leg
as UNVERIFIED. Tracked as Deferred Work item 20 on the TRIM todo." Adding an awaited
drain forces `async` onto currently-sync `Local*` methods, lowering the guard into
`MoveNext` — the measured-bad shape `ClassFactoryRenderer.cs:350-368` documents.
Also: the interface leg's registrar lambdas route to the **public wrapper**
(`IExampleRepositoryFactory.g.cs:122,127,132`), not `Local*` — one extra nesting level
vs. the class leg. Step 5 understates this work by an order of magnitude.

**B-V2 — The drain's position vs `HandleRemoteDelegateRequest.cs:137` decides whether
the cancellation Acceptance can hold.**
`ThrowIfCancellationRequested()` at `:137` sits inside the exact window the plan claims
for the drain. If the drain lands after `:137`, a token cancelled between success and
drain throws first and the drain never runs — the identical B-C5 failure mode,
relocated. The `[unit]` tier makes it worse: a scheduler-level assertion that
`CancellationToken.None` was received passes green regardless of drain placement.

**B-V3 — "Queue only while an entry call is active" collides with drain-until-empty,
and the plan doesn't say which wins.**
If depth pops *before* `DrainAsync`, an event raised by a drained handler sees no entry
active → dispatches immediately and inline, silently voiding the sweep/drain-until-empty
behavior installed by PHASE-001's gate-defect fix. If depth pops *after*, the fix holds
but "raise outside a factory call" needs an explicit carve-out. Evidence it won't be
caught: `FactoryEventsDispatcherPhaseTests.DrainedHandlerRaisingAnEvent_GoesThroughTheRealRaisePath`
(`:161-193`) stays green under either answer while testing nothing.

**B-V4 — The pre-declared amendment set is smaller than the actual breakage.**
PHASE-001 annotated three interim bullets (four tests). Step 3 additionally inverts
`RaiseUntyped_DeferredHandler_DefersJustLikeRaise` (`:129-158`, asserts deferral on a
bare-scope raise) — listed in PHASE-001's evidence as "additional coverage," never
annotated interim. With the B-V3 false-green re-point, that is two out-of-scope tests;
the global "Existing Tests Are Sacred — REPORT and ASK" rule applies.

### Callout-tier

- **B-C1 (observability)** — "Forbidden does not drain" is unfalsifiable as written:
  the `Authorized<T>` denial shape **returns successfully** (and would drain — an empty
  queue, harmlessly, since the body never ran), and the `AspForbidException` shape
  throws before anything can be queued. Falsifiable form: an outer entry enqueues, then
  a forbidden inner call throws. Intent's "forbidden by authorization — simply never
  drains" is only true for the throwing shape.
- **B-C2 (concurrency)** — `FactoryEventPhaseScheduler` uses an unsynchronized
  `Dictionary<DispatchPhase, Queue<>>`; a depth counter inherits the hazard. Blazor
  Server / Logical / the shared-scope harness make concurrent flows in one scope
  realistic. PHASE-001's code review parked this for PHASE-003 — it lands here.
- **B-C3 (client-reachable emission)** — `MoneyFactory.LocalCreate` has **no**
  `IsServerRuntime` guard; it runs client-side. Whatever Step 6 emits there ships into
  trimmed client assemblies and must be null-tolerant at runtime.
- **B-C4 (static leg is the easy one)** — `StaticFactoryRenderer.cs:99` emits
  `Task<…>`-returning delegates unconditionally (no sync shape), and its guard wraps the
  *registration*, not the lambda body — making the lambda `async` moves no guard into a
  state machine. Don't lump it with the interface leg.
- **B-C5 (Logical mode + interface factories)** — interface factories register for
  `Remote` and `Server` only; **`Logical` registers nothing**. The Logical-mode
  Acceptance bullet is only writable against a class or static factory.
- **B-C6 (`LocalSave` depth release)** — `LocalSave` is a `virtual` sync forwarder with
  four return paths (one returning `Task.FromResult(default)` directly); a naive
  `try/finally` decrements depth at *return*, before the inner task completes — firing
  the drain mid-operation. Depth must release on task completion; `LocalSave` needs a
  split it doesn't have.
- **B-C7 (harness fidelity)** — one server scope serves every remote call in a test;
  queues, collector contents, and depth state persist across calls within a test.
- **B-C8 (two registrations of the choke point)** — core registers
  `HandleRemoteDelegateRequest` transient; AspNetCore registers a scoped copy that wins
  in a real server. Fine for tracker resolution, but "one choke point" ≠ "one
  registration."
- **B-C9** — no Skills section; naming the trimming/wrapper-split rules would help.

**Code-density check:** design-focused; zero code fences; file:line confined to Current
State (its sanctioned home). 13 Acceptance bullets is large but the charter is coherent —
no split recommended.

---

## Orchestrator Disposition (2026-08-14)

Calibration applied: diagnoses adopted; remedies picked at the keyboard.

| Finding | Disposition |
|---|---|
| A-V1 | **Amended.** Acceptance bullet 8 split: drain half stays; relay half removed. The client-raise relay gap (pre-existing, affects Immediate handlers equally) recorded in the todo Discovery Log as a deferred question — not owned by this plan. |
| A-V2 | **Amended.** Failure discard is now explicit: outermost exit clears (never drains) on failure. New invariant: between entry calls the scheduler is empty. New Acceptance bullet: a failure followed by a success in the same scope runs only the success's handlers. |
| B-V1 | **Amended.** Current State corrected (inline guard, no split, TRIM item 20, wrapper-routed lambdas); Step 5 re-scoped: interface leg = introduce the split on trimming-unverified ground; static leg separated as the cheap case (B-C4). |
| B-V2 | **Amended.** Plan now pins the drain *before* the post-invoke cancellation check; cancellation Acceptance re-tiered to `[integration]` at the choke point. |
| B-V3 | **Amended.** Decision: the entry remains active for the duration of the entry drain (depth pops after the drain completes), preserving sweep/drain-until-empty and the V4 carve-out; "outside factory call" therefore cannot trigger during a drain. Re-entrancy Acceptance bullet added; `DrainedHandlerRaisingAnEvent…` re-pointed (listed in the amendment set). |
| B-V4 | **Amended.** Pre-declared amendment set widened to name all six tests, each with its preserved intent. Reported to the user in the session summary per the sacred-tests rule. |
| A-C2 | Folded in: backward-compat bullet names the Design solution run. |
| B-C1 | Folded in: Intent wording fixed; forbidden bullet made falsifiable (outer enqueues, inner forbid throws). |
| B-C2, B-C6 | Folded into Constraints (concurrency hazard; depth release on task completion). |
| B-C3 | Folded into Constraints (client-reachable unguarded sync shape must be null-tolerant). |
| B-C4, B-C5 | Folded into Steps/Acceptance (static leg separated; Logical bullet targets a class factory). |
| B-C7, B-C8 | Folded into Current State notes. |
| B-C9 | Skipped — no compact skill reference exists for the wrapper-split rules; the TRIM todo's docs are linked from Current State instead. |
