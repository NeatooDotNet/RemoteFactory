# PHASE-008 — Test Review

**Plan:** [../plans/008-arc-tail.md](../plans/008-arc-tail.md)
**Gate:** Step 5, mandatory per-plan test review
**Budget:** `deep` — this plan declined plan review by user direction, so the gate was its
only independent eye on a source-generator behavior change plus a rewrite inside the arc's
most safety-critical class.

---

## Round 1 — 2026-08-27

**Verdict: CLOSED at must- and should-cover.** 2 must-cover, 5 should-cover, 1 nice-to-have
(plan-related); 1 should-cover and 1 nice-to-have (pre-existing tech debt). All 8
plan-related findings addressed. Both tech-debt items queued rather than absorbed.

The reviewer verified the pre-flight independently — build log clean on both solutions,
six green test summaries, and the totals reconciled by counting `[Fact]`s in the diff
(743 + 12 = 755, exactly 12 new). It confirmed no test was silently lost.

### Must-cover

| # | Finding | Disposition |
|---|---------|-------------|
| M1 | **`IsCanonicalDeclaration` silently dropped a whole handler class.** The split-partial fix chose its canonical declaration from `symbol.DeclaringSyntaxReferences` — *all* declarations — but `ForAttributeWithMetadataName` only ever yields **attributed** nodes. So a partial class whose attribute sits on a later declaration selected an unattributed canonical, nothing matched it, and the transform returned null for every node: **no registration, no diagnostic**. | **Fixed.** Canonical choice now ranges over attributed declarations only. Confirmed red before the fix (`Assert.Single() Failure: The collection was empty`) and measured after as **RP-9**. |
| M2 | **No test drove two overlapping *entry calls*.** The Acceptance bullet claimed entry-state semantics under deterministic interleaving and the evidence row was ticked ✓, but neither cited test calls `BeginEntryCall` more than once. The repo's only two-flow entry-state pin is fully sequential. | **Fixed.** Two tests added driving a second `BeginEntryCall` while flow A is parked inside its own exit drain. See RP-10/RP-11 — what they pin is not what they were drafted to pin. |

**M1 is the finding that justified the gate.** It was a regression introduced by this
plan's own fix, caught by a reviewer *reading code* with nothing run, in a plan that had
declined plan review. It is strictly worse than the CS8785 it replaced: CS8785 is loud and
immediate; a silently dropped registration surfaces only as a handler that never runs.

### Should-cover

| # | Finding | Disposition |
|---|---------|-------------|
| S1 | Two concurrency tests are near-duplicates of existing single-threaded pins, and **RP-8's scope made that unobservable** — it ran filtered to the concurrency class, so it could not see that two pre-existing pins redden under the same sabotage. | **Closed.** RP-8 re-run at full-suite scope: **16 red**, including both pins the reviewer named from a code read. Evidence rows now carry RP-7's second-witness phrasing. |
| S2 | Red-proof self-reporting named **one** unmeasured test where there were **three**; and RP-5 is a disproof of a prediction, not a red-proof of what shipped. | **Closed.** Full declaration written into the log, superseding the partial one. |
| S3 | The registry pin varied only the event type, so a registry keyed by event type alone would have passed it — the implementation its own remarks call wrong. | **Closed.** Third registration added: same event type, different handler class. |
| S4 | The input-compiles guard covers `RunGeneratorTracked` but not `RunGenerator`, while the Acceptance bullet generalised to "a generator test." | **Recorded, not extended.** Diagnostic tests deliberately feed erroring source; guarding there would fail the tests whose subject is bad input. Reason written onto `AssertInputCompiles`. |
| S5 | `IsCanonicalDeclaration`'s file-path ordering leg and its incremental-cache stability claim are both unpinned; and the canonical choice discards other partials' `using` directives. | **Recorded.** Both unpinned claims stated in the method's XML; the usings-loss consequence documented as benign *because* every consumer-derived token is now `global::`-qualified — and explicitly conditional on that staying true. |

### Nice-to-have

| # | Finding | Disposition |
|---|---------|-------------|
| N1 | One bare type-bearing token survives — the assembly attribute's own *name* — contradicting the "every token" framing. | **Recorded as immune.** Assembly attributes bind at compilation-unit scope, outside any namespace, so nothing consumer-declared can shadow it. The reviewer also noted the consequence that the fixture's `TestNamespace.Neatoo` decoy cannot reach that line; the test's remarks now scope themselves to the registration body. |

### Tech debt — queued, not absorbed

| # | Finding | Routing |
|---|---------|---------|
| T1 | Nothing mechanically prevents the next author from calling `FactoryEventHandlerRegistry.Clear()` and breaking the suite. The XML-doc correction was judged the right call for *this* plan (pinning it would have meant weakening a sacred test), but documentation is the accepted-risk position. | New Index row **011** |
| T2 | The other four renderers carry the same bare assembly-attribute token, and none has a namespace-shadowing compile test. The reviewer called the new one "the single best artifact this plan produced" and worth cloning per renderer. | New Index row **012** |

### Sacred tests

**Nine** assertion sites edited in `AssemblyAttributeEmissionTests.cs`; the reviewer found
**no weakening**. Two were *strengthened* (the registrar-holder signature regex and the
server-runtime-guard regex now pin fully-qualified types). Noted for the record: the
pre-flight enumerated seven sites and nine were edited — the two extra are the
framework-token sites amendment A2 added, legitimately within the chartered edit, but the
plan's enumerated list was not updated to say so. Corrected in the Constraints note.

### What the reviewer confirmed rather than flagged

- **The `Rendezvous` handoff is genuinely deterministic** — `_arrived` is set before the
  parked side awaits `_release`, and `RunContinuationsAsynchronously` on both sides prevents
  the releasing thread running the continuation inline. In all drain tests the handler is
  provably parked inside the `DrainAsync` loop when the driving statements run. The
  PHASE-006 reviewers' warning against a `Task.WhenAll` race was heeded correctly; the
  reviewer found no flake risk.
- `RelayHandler_EveryEmittedTypeToken_IsGlobalQualified` is a well-built negative pin (the
  leading-space trick on `" FactoryEventHandlerRegistry."` is correct).
- RP-3 is the log's strongest entry, and the plan's characterisation of it — a probe whose
  "before" run is the positive control — is right.
- The RP-4 fix discriminates each call site for the right reason.
- All PHASE-007 routed items (C2, C6, the `Pending` span note, the `Clear()` accepted risk,
  the `HasPending` LINQ note) were **addressed rather than restated**.

### Suites at close of round 1

Unit **758×2 TFMs** (743 → 755 at implementation, → 758 after this gate), integration
**595×2 (+5 standing skips)**, Design **98×2**. Both solutions built explicitly.
Logs: `008-build.log` (0 errors), `008-test.log`, `008-redproof.log` (RP-1 … RP-11).

### Worth carrying forward

Two lessons, both about *how evidence was scoped* rather than whether it existed:

1. **A red-proof's scope is part of its claim.** RP-8 filtered to the class under test,
   which answers "does my test go red" while silently declining to answer "was it already
   covered." Only the second question separates new coverage from a duplicate — and RP-7,
   two entries above it, had disclosed exactly that caveat for itself.
2. **A test's stated rationale is a claim like any other.** RP-10 disproved the rationale
   the M2 closures were drafted with; RP-11 then established what they actually hold down.
   The tests were kept and their remarks rewritten to the measured answer, with the
   disproved claim left visible.
