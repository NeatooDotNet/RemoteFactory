# PHASE-006 Plan Review (Step 2)

**Plan:** [../plans/006-coalescing.md](../plans/006-coalescing.md)
**Date:** 2026-08-18
**Verdict:** CONCERNS — 4 veto-tier, 10 callout-tier, all adopted by draft amendment before implementation.

---

## Veto-tier findings and disposition

| # | Finding | Disposition |
|---|---------|-------------|
| B-V1 | **Collapsing across `EnqueuedMidDrain` can silently delete a promised 9007.** The bit is load-bearing for the fail-open warning; a (handler, event)-only key collapses a pre-drain dispatch (bit false → must warn) with a mid-drain one (bit true → carve-out), and a latest-wins merge erases the warning with no trace — violating todo AC-5. Also settles pre-review Q3: the survivor *is* observable, through this bit. | Adopted as a Constraint: the merge is **warn-preserving** (a survivor warns if any constituent would have), pinned by an acceptance bullet requiring a test red under latest-bit-wins. |
| B-V2 | **Acceptance bullet 4 bundled three claims; the discard leg couldn't go red** (`ClearAtExit` discards regardless — only the 9006 *count* discriminates, and that count depended on undecided Q4), and "no existing pin modified" is a diff property no test turns red. Ninth instance of the arc's "can't go red" shape, caught at draft. | Bullet split into separately falsifiable bullets; the discard bullet now pins the 9006 collapsed-count against a non-coalescing sibling's N — which forced settling Q4 (below); the diff property moved to the gate meta-bullet. |
| B-V3 | **"Events are records → value equality" is a false universal.** Synthesized equality uses per-member default comparers: reference-typed payloads never compare equal (silent no-coalesce for exactly the motivating shape); custom `Equals` overrides can over-collapse; mutable records make enqueue-time vs drain-time identity different questions. | Identity restated as the `Equals` contract with both hazards documented as part of the attribute XML and docs; identity evaluated when work becomes pending; docs recommend value-only payloads for coalescing handlers. |
| A-V1 | **The survivor rule is published in five strings the doc step didn't name:** `docs/factory-events.md` NF0504 row, `docs/attributes-reference.md` ×2, the NF0504 message format itself, and the registry XML remark — all become incomplete the day the flag lands. The exact incidental-invalidation species PHASE-004's code review recorded. | Step 7 widened to enumerate all five; NF0504's message reworded to cover the whole surviving registration (phase and flag). |

## Callout-tier findings and disposition

- **B-C1** (Q5 forced): `Enqueue` has 53 pinned test call sites — required parameter contradicts the plan's own backcompat constraint → **overload**.
- **B-C2**: optional parameters on public `RegisterHandler` are binary-breaking → **new overload**, existing kept (renderer precedent: always emit the widest); Constraint phrasing corrected.
- **B-C3**: attribute takes a **named property** — the transform treats any non-`int` first ctor argument as `Immediate` silently; keyboard verifies the property form binds as a named argument.
- **B-C4**: `Immediate` isn't the only unqueued path — the 9004/9005 fall-throughs dispatch phased handlers immediately and no compile-time diagnostic can reach them → NF0505 covers the declared-`Immediate` shape; runtime inertness documented for all unqueued paths.
- **B-C5**: 9001/9002 behavior under collapse decided (collapsed raise logs 9008, not a second 9001; counts reflect the collapsed queue) and documented alongside the new row.
- **B-C6**: Q2 was already decided by Step 6 → closed: options in the identity key; recorded that options are invisible to attribute-declared handlers (under-coalescing is the only cost there).
- **B-C7**: the "must not reorder" Constraint was unfalsifiable → replaced with the real invariants (cross-phase sweep + warn-bit).
- **B-C8**: test-isolation Constraint added (distinct handler classes and event types per case; the N raises share one Guid) — process-static first-wins registry plus per-raise-varying Guids would silently defeat both controls.
- **B-C9**: enqueue-scan O(n²)-under-lock cost named in Notes; mechanism left to the keyboard deliberately.
- **B-C10**: the RP-0 countermeasure is the expected-totals check, not "build both solutions" — Step 8 now records totals per suite.
- **A-C1**: parent todo gained a coalescing AC bullet, the Out of Scope bullet now distinguishes cross-event (out) from same-event (this plan), and the Discovery Log records the 2026-08-14 queueing decision trace.

## Reviewer verifications worth keeping

- No documented rule forbids the feature (Pass A checked all six phase-contract invariants); "order within a phase is unspecified" and raise-time relay collection make the survivor choice and the relay constraint structurally sound.
- Handler-delegate reference equality is a stable registration key (one delegate instance per surviving registration, process lifetime).
- 9008 is free (Log.cs tops out at 9007).
- The draft carried no transcription smell — intent-level steps, no line numbers or signatures.
