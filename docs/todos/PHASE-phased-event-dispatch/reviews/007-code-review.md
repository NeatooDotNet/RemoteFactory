# PHASE-007 Code Review (Step 5, opted in)

**Plan:** [../plans/007-tech-debt.md](../plans/007-tech-debt.md)
**Date:** 2026-08-18
**Verdict:** 1 veto-tier finding, 9 callouts. V1 and C1/C3/C4/C5/C7/C8/C9 closed before
Done; C6 routed to PHASE-009; C2 became new Index row 010.

*(The first review attempt died on an infrastructure error before producing any output
and was relaunched from scratch — noted so the gap in agent runs is not mistaken for a
skipped step.)*

## Direction verified

The reviewer traced rather than inherited: it re-derived FIFO, the collapsed 9002/9006
counts, the warn-merge write path, and discard-on-failure against the new storage and
confirmed all four are semantically identical to the `RemoveAt(0)` version; it verified
A4's *replacement* claim by walking the aborted-drain test state by state and confirming
every state is framework-producible; and it independently confirmed
`RegisterMatchingName`'s transient registration against the source and against two
published docs before accepting the new Design guidance.

**It also closed my own open question.** I had flagged the `ReadOnlySpan` over the live
backing array as a probable regression and was holding a fix. The reviewer's trace says
it is safe: a re-entrant `Equals` that grows the list leaves the span pointing at a live,
GC-tracked array — a stale read, never memory unsafety — `Clear()` zeroes rather than
shrinks, and a blanked slot short-circuits on `ReferenceEquals` with a non-null handler.
Cross-thread interleaving cannot reach it at all, since every `PhaseQueue` member runs
under `_gate`. **The fix was therefore not made**, and the two genuine residual deltas
were documented instead (C7). Recording this because the reflex was to change code on my
own reasoning, and the trace said don't.

## Findings and disposition

| # | Tier | Finding | Disposition |
|---|------|---------|-------------|
| V1 | **veto** | **9009's causal claim contradicts the code path it names.** The shipped message and the CLAUDE-DESIGN row said a drain wrapping the factory call from outside "runs before the work it means to flush has been queued." False for the after-the-call case: the dispatcher queues only while an entry call is active, and `EndEntryCallAsync(true)` always drains at the outermost exit — so the work *was* queued and had *already been swept*, with 9007 emitted moments **earlier**, not later. The arc's reasoning-dressed-as-evidence species, in the one consumer-facing contract this plan adds. | **Closed:** verified against `FactoryEventsDispatcher.cs:75` and `EndEntryCallAsync` before acting. Message rewritten in `Log.cs`, the CLAUDE-DESIGN row rewritten, and the plan's Intent bullet corrected in place with provenance. The actionable half ("call it from inside the factory method body") was correct and stands. `docs/factory-events.md` and the skill row never carried the claim. |
| C1 | callout (med) | **A can't-go-red assertion inside the plan's headline pins:** 9009's `{Phase}` is structurally constant, because the coordinator's whitelist throws for every phase but `AfterFlush` before the short-circuit is reachable. | **Closed:** both phase assertions removed, with the reason written at the pin. The parameter stays — it is real structured-log context and stops being constant the day a second phase becomes consumer-drainable. |
| C2 | callout (med) | **The corrective signal ships at Debug while the misleading Warning is unchanged.** 9007 still says "call `DrainAsync` between your flush and your commit" — which the outside-wrapper consumer did. The missing qualifier lives only in 9009, invisible under a default Information minimum. | **Routed:** new Index row **010**. Its own plan because it edits an existing pinned message. |
| C3 | callout (high) | **The Design restructure orphaned its own teaching prose.** CLAUDE-DESIGN's Key Files still pointed only at `Program.cs` for a composition that had moved, and `Program.cs`'s "the server only needs…" list still named two items no longer in the file. The incidental-doc-invalidation species PHASE-004 and PHASE-006 C2 both hit. | **Closed:** Key Files gained `ServerServices.cs` and `DesignServerCompositionTests.cs`; the `Program.cs` list retargeted. |
| C4 | callout (high) | Red-proof log header said "Four measured sabotages"; it records seven. Stale since gate round 1. | **Corrected in place**, with the staleness itself noted. |
| C5 | callout (high) | Plan Index row still `Draft`; plan header still "gate pending". | **Both flipped to Done.** |
| C6 | callout (med) | The storage comment cites a bulk-save motive, but `TryDequeueThrough` still builds a `Where`+`OrderBy` chain **per dequeued dispatch** — not quadratic, but the named scenario still pays a per-dispatch allocation. | **Routed to PHASE-009**, beside the `HasPending` LINQ note already there. |
| C7 | callout (med) | `Replace`'s index arithmetic carries an unstated re-entrancy assumption, and the scan now captures its length up front so a re-entrantly appended entry is not scanned — a small behavior delta from the `List` version. | **Comment amended** to name both specifics, that `lock` re-entrancy is what makes them reachable, and that cross-thread interleaving cannot. |
| C8 | callout (med) | `AddDesignServerServices` was `public` with one caller — a reader could pick the more descriptive-sounding of two public methods and get the registrations without `AddNeatooAspNetCore`, the exact half-composition the gate's G6 closed. | **Made private**, with the reason at the declaration. One entry point. |
| C9 | callout (high) | Plan Amendment A1 still named the pre-widening seam. Same stale-cross-reference species as the gate's H2, in the text that fed it. | **Corrected in place.** |

## Verified clean

Sacred tests (the only removed assertions in the range are the three `Assert.True(true)`,
replaced with strictly stronger observations, and one containment check replaced with
`Assert.Single` + level + event type + phase + a negative); no reflection introduced;
the `Design.sln` build instruction satisfied (CLAUDE.md is the only file carrying
verification commands); the tuple-order correction meaning-preserving across all six call
sites; the new transient guidance consistent with `docs/service-injection.md` and
`docs/aspnetcore-integration.md`.

## Final verification

Builds: both solutions Release, 0 errors. Serial (`-m:1`) and full-parallel: unit 743×2,
integration 595×2 (+5 standing skips), Design 98×2 — all green. The 4 build warnings are
pre-existing and untouched (2× CA1062 in TrimmingTests, 2× WASM manifest warnings).
