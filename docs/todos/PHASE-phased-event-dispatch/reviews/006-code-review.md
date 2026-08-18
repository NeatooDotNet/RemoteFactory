# PHASE-006 Code Review (Step 5, opted in)

**Plan:** [../plans/006-coalescing.md](../plans/006-coalescing.md)
**Date:** 2026-08-18
**Verdict:** Zero veto-tier findings; 7 callouts — C1/C2/C4 closed before Done (C1 with a measured red-proof), C3/C6/C7 text corrections in place, C3-storage + C5 routed to PHASE-007.

## Direction verified

The reviewer traced (not inherited) the load-bearing shape decisions: the per-phase identity scan is sufficient because handler-delegate reference equality means "same surviving registration" and a registration carries exactly one phase; `Equals` on `FactoryEventBase`-typed variables binds the record's strongly-typed overload with the `EqualityContract` check, so structurally-identical instances of *different* event types cannot collapse; the generator side is cache-safe (record equality includes the new primitive; no `TypedConstant` escapes); `global::` hygiene holds (the only new emitted token is a bool literal); every published doc claim traced to shipped code held; the `Internal`-namespace policy is honored.

## Findings and disposition

| # | Tier | Finding | Disposition |
|---|------|---------|-------------|
| C1 | callout (high) | **The warn-preserving merge's true→false branch was dead code to the suite** — RP-1 measured the flip (false→true erasure) but no test ordered the mid-drain raise first; deleting the merge left 728/591/94 green. The arc's "can't go red" shape, landed on the plan's headline B-V1 constraint. | **Closed:** `Coalesce_MidDrainRaiseFirst_ThenPreDrainRaiseCollapses_TheSurvivorStillWarns9007` added and the omission sabotage **measured as RP-3** — exactly the predicted 1 red ×2 TFMs, this pin alone. |
| C2 | callout (med) | **A sixth and seventh survivor-species doc string A-V1's own enumeration missed:** CLAUDE-DESIGN's narrative diagnostics bullet omitted NF0505 and the pass-through bullet omitted `Coalesce` (the table was updated; the narrative pair was not). Second time this arc caught the incidental-invalidation species one review after adopting a veto about it. | **Closed:** both bullets updated. Lesson recorded in the Discovery Log: an enumerated anchor list is itself a claim that can be incomplete. |
| C3 | callout (med) | The Queue→List comment blamed `Queue<T>` for what the readonly-struct element imposes, and the O(n) front-dequeue cost is paid by the non-opted-in default path on an unenforced "queues stay small" assumption. | **Comment corrected** to name the real constraint and the assumption; the storage-shape question **routed to PHASE-007**. |
| C4 | callout (med) | Consumer code (`Equals`, custom overrides invited) now runs under `_gate`, contradicting the class comment "handlers are invoked outside the lock"; a re-entrant `Equals` mutates the queue mid-scan. | **Comment amended** (names the one consumer call under the lock + the cheap/non-re-entrant expectation); raises the stakes on the scheduler-concurrency item already routed. |
| C5 | callout (low) | The round-1 warn-merge pin drives `Enqueue(Immediate, …)` — a state the dispatcher never produces (the PHASE-004 "pin ran where production can't" species; the stamp is phase-agnostic so the pin stands). | **Routed to PHASE-007** (recorded; an AfterFlush trigger would be the production-shaped variant). |
| C6 | callout (low) | The new interface member's remark covered callers, not third-party implementors (a break the Internal policy permits but the XML didn't signal). | **XML sentence added.** |
| C7 | callout (low) | `HasPending` described as a "count". | **Reworded.** |

Internal contradictions: one trivial (Acceptance bullet 4's "sibling in the same test" shipped as two paired tests with the discriminator fully present) — recorded here, no doc change needed.

Round-3 gate logs after the closures: build 0 errors both solutions; unit 729×2 (705+24), integration 591×2 (+5 standing skips), Design 94×2 — totals reconcile.
