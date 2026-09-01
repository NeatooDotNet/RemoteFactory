# PHASE-002 — Plan Review

**Date:** 2026-08-15
**Reviewer:** `plan-reviewer` (Pass A: documented requirements; Pass B: codebase)
**Verdict:** CONCERNS — 3 veto-tier, 12 callout-tier
**Disposition:** all 3 vetoes addressed in the draft before implementation; callouts addressed
or explicitly declined below.

---

## Veto-tier

### A-V1 — Error severity contradicts this project's own precedent for the identical shape

The reviewer found the documented decision for NF0503 in
`docs/plans/completed/factory-events-relay-redesign.md:76` — Warning was chosen *explicitly*
because it "makes the migration footgun loud at compile time without breaking the build",
for the same problem shape: a declaration that compiles and is silently inert.

And the plan's own taxonomy, correctly stated in Framework Alignment, puts duplicates on the
Warning side. Verified against the transform: NF0501/NF0502 both `continue` **without adding
an entry**, and `FactoryGenerator.cs:100` returns early at `Entries.Count == 0` — the class
emits no file at all, so the declaration is 100% dead and Error is right. A duplicate
same-event attribute is different in kind: the handler is found, the entry is added, the
registration emits and works. Only the second attribute is redundant.

**Disposition: ADOPTED — severity is Warning.** The draft's Error was a mis-application of a
taxonomy the draft itself had written down correctly. Warning also dissolves A-C3 (no
breaking change, no major-version question) at no cost to the signal.

Consequence, from B-C3: Warning alone would *reintroduce* the silent loss — two attributes at
different phases would emit two registrations and the registry's first-wins dedupe would pick
one silently. So Warning is paired with **skipping the duplicate entry at emission**, in
source order, with the surviving phase named in the diagnostic message. The generator's output
then matches what the diagnostic says.

### B-V1 — The incremental-cache Acceptance bullet cannot go red for what the plan claimed

`DiagnosticTestHelper.RunGeneratorTracked` re-parses into the same path and calls
`ReplaceSyntaxTree`, so the transform genuinely re-runs — the guard is not `Cached`-vacuous.
But what it asserts is *equality of two transform outputs across runs*, and a scalar phase
field is equal across runs whether the generator reads the attribute correctly, reads it
wrong, or hardcodes `Immediate`. It is a determinism check, not a correctness check.

Worse for the stated purpose: `ReplaceSyntaxTree` reuses the compilation's reference manager,
so even the genuinely bad representation — storing the raw `TypedConstant` — would likely
compare equal and stay green while rooting a `Compilation` in the generator cache.

This is the `MEMORY.md` "verify, don't inherit" failure mode verbatim, in a plan written by
someone who has that memory loaded. Second time this arc.

**Disposition: ADOPTED, options (a) + (c).** The coverage claim is dropped rather than
propped up:

- The Acceptance bullet is reworded to claim only what the test pins — the branch stays
  cached, with the fixture now populating phase data so a *future collection-shaped* phase
  field would be covered. It no longer pretends to pin the phase read.
- The primitive-representation rule moves into Constraints as a code-review item, restated
  per B-C4 to forbid the thing that actually hurts.
- Correctness of the phase read is pinned where it can genuinely go red: the emission bullets
  and the end-to-end bullet.

Option (b) — two independently constructed compilations to get distinct reference managers —
was considered and declined. The generator's model types are `internal`, so a test cannot
compare models directly; the reachable cross-driver comparison is generated *source text*,
which is another determinism check and would not catch a `TypedConstant` field either. Buying
a second non-discriminating test to replace a non-discriminating claim is the failure mode,
not the fix.

### B-V2 — Missing `global::` invariant on the newly emitted type token

`RelayHandlerRenderer.cs:38-40` documents that the unqualified form was a latent bug that
shipped for four releases, and line 608 of the emission tests now pins its absence. This plan
emits a *new* type-bearing token into the same file, bound only by a hardcoded
`using Neatoo.RemoteFactory;`. The draft's constraint ("named enum member, not an opaque
numeric cast") is satisfied by the unqualified form, so nothing steers; and the draft's
acceptance bullet would be satisfied by a `Contains("DispatchPhase.AfterCommit")` that cannot
tell the two apart. Third "can't go red" in one plan.

**Disposition: ADOPTED.** The pre-flight had already spotted the qualification (Current State,
last bullet) but left it as an observation with nothing enforcing it — which is exactly how it
would have been lost. Now a Constraint, with the Acceptance bullet worded to require pinning
the qualified form plus a negative pin on the bare form, mirroring line 608.

---

## Callout-tier

| # | Finding | Disposition |
|---|---|---|
| A-C1 | Cite the documented "several event **types**" contract as the basis for Step 4 | Adopted — `docs/attributes-reference.md:222`, skill `factory-events.md:133` cited in Step 4 |
| A-C2 | PHASE-005 doesn't name the prose this plan invalidates, nor the diagnostics tables | Adopted — anchors handed to PHASE-005's stub; Discovery Log entry |
| A-C3 | If Error, classify against the version-impact table (breaking → major) | Moot — Warning chosen |
| B-C1 | "Always a mistake" holds *given the current dedupe key*, not absolutely | Adopted — stated in Notes |
| B-C2 | Step 4 never names last-wins, which the Discovery Log queued by name | Adopted — rejection recorded in Notes |
| B-C3 | Warning also requires deciding whether the duplicate entry still emits | Adopted — folded into A-V1's disposition; entry is skipped |
| B-C4 | Representation constraint forbids the wrong thing | Adopted — restated as primitive `string`/`int`, never `TypedConstant`, never `ISymbol` |
| B-C5 | 2-arg vs 3-arg overload for the defaulted case is undecided | Adopted — 3-arg universally; consequence noted (2-arg keeps zero generated call sites) |
| B-C6 | End-to-end fixture constraints (new event type, `partial`, no overlap) | Adopted — recorded in Current State |
| B-C7 | Trimming safety here is an argument, not a measurement | Adopted — stated in Constraints |
| B-C8 | New descriptor needs a `GetDescriptor` switch case or the generator throws | Adopted — Notes |
| B-C9 | Fixture extension is safe while diagnostic-free | Noted, no action |
| B-C10 | `RegisterHandler_SameHandlerClassTwoPhases_KeepsTheFirstRegistration` comment needs updating | Adopted — comment-only; the test's assertion stands untouched (it exercises the registry API, which the generator diagnostic cannot reach) |
| Index | Undefined-enum-value decision lives only in plan Notes | Adopted — promoted to the Discovery Log |

---

## Reviewer answers to the questions posed

- **Q2 (is the duplicate reasoning true?)** Verified against the transform's matching logic —
  the scan is a pure function of the event type and does not know which attribute instance is
  being processed, so two attributes naming the same `T` compute an identical match set. No
  legitimate shape found: aliased generic arguments collapse to the same fully-qualified
  string and *are* genuine duplicates; closed generics over different arguments are correctly
  not duplicates; `Inherited = false` rules out base-class interaction; `#if` leaves one.
- **Q3 (is the end-to-end bullet reachable?)** Yes. `NeatooRuntime.IsServerRuntime` defaults
  true and the integration process never sets the switch, so the generated guard is open in
  all three containers; registrar discovery is reflective over the assembly attribute, which
  every `ClientServerContainers` path triggers. Registration happens with no test-side call —
  which is what the bullet's falsifiability condition needs.
- **Q4 (trimming)** No missed implication. An enum value lowers to `ldc.i4.<n>` — no new type
  reference, no new DAM surface; the DAM annotation is on both overloads. But the leg has no
  positive control by construction, so this is an argument, not a measurement (→ B-C7).
