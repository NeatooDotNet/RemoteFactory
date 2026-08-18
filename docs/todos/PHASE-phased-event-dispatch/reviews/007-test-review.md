# PHASE-007 Test Review (Step 5 gate)

**Plan:** [../plans/007-tech-debt.md](../plans/007-tech-debt.md)
**Round 1 date:** 2026-08-18
**Verdict:** 2 must-cover, 5 should-cover, 6 nice-to-have — all must- and should-cover
closed; 4 of 6 nice-to-haves taken. Both must-cover findings were in code this plan
itself introduced.

## What the gate verified rather than inherited

It re-derived the drain's control flow from source instead of accepting the plan's
account of it, which is what produced the sharpest finding (below). It also traced the
new coalescing pin's arithmetic to confirm the pin discriminates, checked that the
`CapturingLoggerProvider` snapshot change created no stale-snapshot call site, and
confirmed by diff that the `FactoryEventHandlerLocalTests` tuple-name correction changed
no call site. It independently confirmed the tautology claim about
`ServerOnlyEvent_ExcludedFromRelayBatch` and established that no other test depended on
the wait's silent return.

## Findings and disposition

| # | Tier | Finding | Disposition |
|---|------|---------|-------------|
| G1 | must-cover | **9009's production wiring was unpinned.** Both new pins construct the coordinator by hand and pass a logger factory; the constructor parameter is optional, so dropping `sp.GetService<ILoggerFactory>()` from the DI registration silences 9009 in every real application with both pins green — the plan's headline feature passing by accident. Same species as PHASE-004's must-cover. | **Closed:** `DrainAsync_ResolvedFromDI_OutsideAnyEntryCall_LogsTheShortCircuit`, using the file's existing `ServerScopeWithLogs()`. **Measured (RP-5):** the predicted 1 red ×2 TFMs, with both hand-built pins green — the diagnosis confirmed, not inherited. |
| G2 | must-cover | **`PhaseQueue.Replace`'s `_head +` offset had no discriminating test.** It is the only write path for the warn-preserving merge — the mechanism behind PHASE-006's #1 veto-adopted constraint — and every merge test ran with the cursor at zero, where the offset is a no-op. | **Closed** by the production-shaped merge pin below (G3), which is the only merge in either suite running with `_head > 0`. **Measured (RP-6):** exactly 1 red ×2 TFMs. |
| G3 | should-cover | **Plan Amendment A4's reachability claim is false, and it was the load-bearing sentence answering PHASE-006 code review C5.** A4 argued both warn-merge orderings are unreachable because "every drain runs until empty." The in-transaction branch of `DrainAsync` has no catch, so a handler exception abandons the queue; cancellation does the same. Either leaves mid-drain-stamped work pending behind a non-zero cursor. C5's complaint therefore stood. | **Closed:** verified against source, then **A4 retracted** in the plan and the in-file REACHABILITY block rewritten. `Coalesce_AbortedConsumerDrain_ThenPreDrainRaiseCollapses_TheSurvivorStillWarns9007` added — the production-shaped variant C5 asked for, every state framework-produced. |
| G4 | should-cover | The Acceptance clause "the warn-merge holds in a production-shaped ordering" had **no Test Evidence row and no `MISSING —` row** — disposed of only in A4's prose, i.e. the claim that turned out to be wrong. | **Closed:** row added, now genuinely covered. Worth keeping as a rule: a clause discharged by argument still needs a row saying so. |
| G5 | should-cover | **`PhaseQueue.Count`'s `- _head` was not discriminated either.** No test discarded from a partly-drained queue, so 9006's count — PHASE-006's falsifiability discriminator — had never been observed with the cursor off zero. | **Closed:** `EndEntryCallAsync_Failed_AfterAnAbortedDrain_DiscardCountExcludesWhatAlreadyRan`. **RP-7 round 1 came back green** — the first version drained a phase fully, and a fully-drained queue resets the cursor, so both implementations agreed. Rebuilt around an aborted drain; round 2 measured the predicted 1 red ×2. |
| G6 | should-cover | **The Design.Server residual was understated:** the service *list* was shared but the *composition root* was still a mirror — the test restated `AddNeatooAspNetCore(typeof(IOrder).Assembly)`, so a drifting assembly argument or a deleted registration call would have left all 98 Design tests green. | **Closed:** the framework call moved inside the seam as `ServerServices.AddDesignServer()`; `Program.cs` and the test both call that one method. Residuals restated in the test's remarks — what remains is that `Program.cs` could stop calling it at all, which is one line. |
| G7 | nice-to-have | A third relay-wait poll survived the consolidation (`FactoryEventPhaseEntryTests:196`), keeping the short deadline and silent fall-through while Acceptance bullet 6 claimed closure. | **Taken** — folded into `RelayTestHarness.WaitForAsync`. |
| G8 | nice-to-have | The harness order pin asserted only `NotNull` on the relay, not that it is registered on the **client** container. | **Taken** — `Assert.Same` against the client scope's resolved relay. |
| G9 | nice-to-have | 4 of 13 `[Service]`-injected types unasserted and unexercised (`ILazyLoadFactory`, `IOrderLineFactory`, `IOrderLineListFactory`, `IMoneyFactory`). | **Taken** — four resolutions added. |
| G10 | nice-to-have | `NF0505`'s closing `Assert.NotEqual("CoalesceHandlers", …)` is vacuous after the preceding exact `Assert.Equal`. | **Taken** — removed; the contrast moved into the comment. |
| G11 | nice-to-have | Test Evidence said "unit 739 → 740 (+11)"; 739 was a mid-implementation count, so the arithmetic did not reconcile against PHASE-006's closing 729. Same stale-number species PHASE-005's round-2 close caught. | **Taken** — baselines corrected to 729 → 743 (+14). |
| G12 | nice-to-have | The Design composition bullet is tagged `[integration]` but landed in the Design tier. | **Not taken** — recorded. Placement is correct; only the label is imprecise, and CLAUDE.md's tier list has no Design tier to name. |

## Tech debt surfaced (not this plan's)

- The head-cursor refactor slightly raises PHASE-009's stakes: `Pending` hands out a
  `Span<QueuedDispatch>` over the live backing array, walked under `_gate` while consumer
  `Equals` runs inside the loop. A re-entrant `Equals` now mutates an array a span is open
  over, not just a `List`. Noted on the 009 row.
- `FactoryEventHandlerRegistry.Clear()` remains internal and uncalled; 007 chose
  "document rather than isolate" (permitted by its bullet), and the discipline is now
  written in two places, neither of which a new unit-test file's author necessarily opens.
  Recorded as an accepted risk rather than closed.
- `HasPending` allocates a LINQ enumerator per call under `_gate` — unchanged by this
  plan, noted because the storage comment now claims performance as a motive.

## Round 2 (2026-08-18) — gate closed at must- and should-cover

Every closure verified by re-tracing the mechanism, not by accepting the citation, and
each sabotage log checked by **failing-test name** rather than count (counts alone would
not show that the *predicted* test is the one that reddened). All 2 must-cover and all 5
should-cover confirmed closed; no closure introduced a new can't-go-red.

Three residuals, all fixed in the round-2 commit:

| # | Tier | Finding | Disposition |
|---|------|---------|-------------|
| H1 | should-cover | **The retracted A4 claim still stood verbatim in the red-proof log** — the round-1 "Not measured, and why" bullet on the warn-merge pins still said neither ordering is reachable, ~35 lines below its own retraction. Same species as PHASE-004's RP-3 sentence and PHASE-005's RP-0 rule sentence; the arc's convention is to correct in place, and the retraction had been written as a new section instead. | **Corrected in place**, with the reason recorded. |
| H2 | nice-to-have | `DesignServerCompositionTests`' remarks still named `AddDesignServerServices` after the seam widened to `AddDesignServer` — a stale cross-reference inside the file whose whole subject is drift, on the sentence carrying the load-bearing "rather than from a copy of it" claim. | **Fixed.** |
| H3 | nice-to-have | Acceptance bullet 4 was ticked with the wording A1 had retracted ("resolves every `[Service]` parameter type"). | **Restated with provenance**, following the todo's own AC-1/AC-3 precedent, rather than ticked as written. |

It also sharpened this plan's recorded lesson and answered both questions I put to it:

- **The first root-cause diagnosis was too loose.** "A cursor change needs a test that
  leaves the structure partially consumed" would not have caught RP-2 round 2, which
  *did* leave it partially consumed and still passed — the slot blanking ate it. The
  invariant that covers all three wrong predictions: every `_head`-dependent member is
  observable only inside `0 < _head < _items.Count`, and two independent housekeeping
  actions attack that window (`Clear()` collapses it, blanking neuters its contents), so
  a cursor test must leave the queue partially consumed **and** assert something the
  blanking cannot also satisfy. Substituted into the Discovery Log entry.
- **One fourth instance exists and is deliberately not tested:** the `Clear()` call inside
  `Dequeue` cannot be failed by any test — delete it and behavior is identical, only the
  backing array grows. On the reviewer's own recommendation (a capacity assertion would
  be white-box and brittle) it is **recorded in the comment as unpinnable** rather than
  given a test, so it stops reading as an intentional behavior among pinned ones.

## Round-3 verification

Builds: both solutions Release, 0 errors. Serial (`-m:1`): unit 743×2, integration
595×2 (+5 standing skips), Design 98×2. Full-parallel (default `-m`): identical totals,
all green — the Acceptance bullet about the relay flake, satisfied with the relay tests
included. Expected totals reconcile against PHASE-006's close (729 + 14, 591 + 4,
94 + 4).
