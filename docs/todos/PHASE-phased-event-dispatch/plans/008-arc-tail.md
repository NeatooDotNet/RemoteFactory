# Arc Tail: Emission Qualification, Scheduler Concurrency, and the 9007 Placement Qualifier

**Plan #:** 008
**Date:** 2026-08-27
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-27
**Plan-review opt-in:** No (user direction 2026-08-27: the arc's plan-and-review ceremony is the cost being cut. The harness would have been the strongest candidate in the arc, so the mitigation is explicit — cheap work lands first, the harness is the last thing implemented, and the mandatory Step 5 gate still runs)
**Code-review opt-in:** Yes (generator emission, the scheduler's lock discipline, and one pinned consumer-facing log message)

---

## Scope

Close the arc's remaining tech debt in one pass: give 9007's Warning the drain-*placement*
qualifier it is missing (a consumer who wrapped the factory call from outside did do what the
message tells them to do), `global::`-qualify the type-bearing tokens the relay registration
still emits bare and audit the other emission legs for the same hazard, measure rather than
infer the partial-declaration attribute-split behavior, make the generator test helper reject
an input compilation that does not compile, move a misfiled diagnostic test class, and build
the deterministic scheduler-concurrency harness the arc has been routing work to since
PHASE-006 — using it to settle the per-scope entry-state semantics under real interleaving,
the two named-but-unguarded re-entrant-`Equals` pathologies, the registry test-isolation
discipline, and the two allocation notes on the same class. This plan folds the former Index
rows 009 and 010 into itself; both become tombstones. It does **not** change any phase
contract, does **not** add a phase or a drain point, does **not** redesign entry tracking to
be per-flow (AsyncLocal remains rejected per the 2026-08-14 ruling), and does **not** take on
the `Internal`-usage analyzer (EF1001 equivalent) that PHASE-004's discovery queued as its own
candidate.

---

## Inherited (routed here by the 002–007 gates — provenance per item)

Transplanted verbatim in substance from the former Index rows 008, 009, and 010 when they
folded into this plan, so retiring those rows loses no routing history.

**From row 008 — generator emission hygiene**

- The **event type** token in the emitted relay registration is not `global::`-qualified — the
  identical hazard PHASE-002 spent a Constraint, a veto, and a negative pin on, three tokens to
  the left in the same statement. Audit the other legs for the same. *(002 gate + code review)*
- Attributes split across partial declarations should collide on hint name
  (`ForAttributeWithMetadataName` fires per syntax node; the transform reads
  `symbol.GetAttributes()`) — **inferred, never measured**. *(002 gate)*
- `RunGeneratorTracked` never checks the input compilation for CS errors. *(002 gate)*
- `NF04xx…Tests.cs` contains `class NF05xx…Tests`. *(002 gate)*
- *(The `DiagnosticTestHelper` double-count that shared this row was pulled forward and fixed in
  PHASE-002 itself.)*
- Ratchet noted at 002: five assertions written there hardcode the unqualified event-type form
  and need editing when this lands. See the chartered-test-edit Constraint.

**From row 009 — scheduler concurrency**

- The scheduler has zero concurrency coverage against its own shared-scope contract; predates
  the arc. *(006 gate round 1; candidacy queued to the re-split decision, executed at 007's
  drafting)*
- Both 006 reviewers recommended a dedicated **deterministic** harness, explicitly not a
  `Task.WhenAll` race. *(006 gate + code review)*
- Stakes raised: the coalescing identity scan runs consumer `Equals` under `_gate`,
  contradicting the class's own "handlers are invoked outside the lock" comment, so a
  re-entrant `Equals` mutates the queue mid-scan. *(006 code review C4)*
- Raised again by 007's storage change: `PhaseQueue.Pending` hands out a span over the live
  backing array, so a re-entrant `Equals` that enqueues mutates an array a span is open over,
  not merely a `List`. Two specifics named in the `_gate` comment and neither guarded — an
  appended entry is not scanned because the scan captures span and length up front, and
  `Replace` resolves its index against the **current** head, so a re-entrant dequeue between
  read and merge lands the write on the wrong entry. *(007 gate)*
- Candidate pin: the 003 round-2 N1 timing window — work a concurrent flow enqueues while the
  survivor's outermost drain runs either joins that drain or is discarded by the post-drain
  clear, depending on timing. Recorded, never pinned. *(003 gate round 2)*
- Accepted-not-closed registry isolation risk: `FactoryEventHandlerRegistry.Clear()` stays
  internal and uncalled, and 007's discipline notes live in two files a new test author may not
  open. *(001 routing; 006 dependents note; 007 gate)*
- Two allocation notes, neither a correctness issue: `HasPending` builds a LINQ enumerator per
  call under `_gate` *(007 gate)*, and `TryDequeueThrough` builds a `Where`+`OrderBy` chain
  **per dequeued dispatch**, so the bulk-save scenario the storage comment names still pays a
  per-dispatch allocation even after 007's O(1) dequeue fix *(007 code review C6)*.

**From row 010 — the 9007 qualifier**

- 9007's Warning says to call the coordinator's drain "between your flush and your commit" —
  which the consumer who wrapped the factory call from *outside* did do. The missing words are
  "from inside the factory method body." PHASE-007 put that guidance in 9009, but 9009 is Debug
  and therefore invisible under a default Information minimum, so the consumer who most needs
  it sees only the misleading Warning. Row existed separately because it edits an existing
  **pinned** message. *(007 code review C2)*

---

## Intent

- A consumer whose transaction abstraction wraps the factory call from the outside reads one
  Warning and learns the actual fix — that the drain belongs **inside** the factory method
  body — instead of being told to do the thing they already did. Today that guidance exists
  only at Debug (9009), which a default Information minimum hides from exactly the consumer
  who needs it.
- Generated registration code binds to the types it names even when a consumer namespace
  shadows the first segment of the framework's, closing the last instances of the latent
  defect that shipped for four releases and that PHASE-002 spent a Constraint and a veto on
  three tokens to the left of the ones still bare.
- The scheduler's shared-scope concurrency contract stops being a claim in a comment. The
  class documents behavior under concurrent flows, re-entrant `Equals`, and mid-drain
  enqueues; none of it is exercised, and PHASE-007's storage change widened the surface by
  handing the identity scan a span over the live backing array.
- The generator's own test infrastructure stops being able to pass against a compilation that
  never compiled.
- The arc's routing debt closes: every item the 001–007 gates deferred is either done, pinned,
  or recorded as a conscious accept with its reason.

---

## Framework & Architectural Alignment

- **Emission qualification** follows the rule PHASE-002 established and
  `RelayHandlerRenderer`'s own `DispatchPhaseType` comment states: type-bearing tokens in
  generated files are `global::`-qualified because the file's `using` is not enough. The
  acceptance requires a **negative** pin on the bare form — a `Contains` on the qualified
  string is satisfied by the bare one as a substring, which is the false green PHASE-002
  documented.
- **`Internal` namespace policy** (PHASE-004): the scheduler stays public and unsealed; its
  members stay non-virtual with the interlocking-contract reason intact. Any guard added for
  re-entrancy is an implementation detail behind the existing interface.
- **Existing tests are sacred**, with one chartered exception recorded under Constraints: the
  assertions that hardcode the unqualified emitted form are updated, not weakened — their
  intent ("the registration references the right type") is preserved and sharpened.
- **Red-proof discipline:** every discriminator this plan claims is measured or labeled
  derived. This arc has three instances of a wrong prediction and eleven of a can't-go-red
  assertion; the concurrency harness is the highest-risk place yet to inherit a claim.
- **Deterministic concurrency testing, not racing.** Both PHASE-006 reviewers named
  `Task.WhenAll` as the wrong shape. Interleaving is driven by explicit synchronization the
  test controls, so a failure is reproducible and a pass means something.
- **No reflection** — including in tests, per the standing rule that cost PHASE-007 its
  drift-detector design.

---

## Constraints & Invariants

- No consumer-facing contract change except 9007's message text. No new phase, drain point,
  log id, or public member unless the re-entrancy work proves one necessary — and that would
  be a Plan Amendment with the user consulted, not a quiet addition.
- The scheduler's pinned semantics are invariant: FIFO within a phase, earliest-phase-first
  sweeps, collapsed coalescing counts, the warn-preserving merge, discard-on-failure, and
  "between entry calls the scheduler is empty." The 001–007 pins are the proof and stay green.
- **Chartered test edit:** assertions hardcoding the bare emitted token form are updated to the
  qualified form. This is the ratchet PHASE-002's Discovery Log predicted by name. Every such
  edit preserves the assertion's original intent; any assertion that cannot be updated without
  weakening it stops the plan and goes to the user.
- The suite totals only grow. Unit 743, integration 595 (+5 standing skips), Design 98 — each
  ×2 TFMs — stay green with existing assertions intact.
- Concurrency tests are deterministic and must not introduce a timing-dependent flake. The arc
  already carries one hard-poll flake defused in PHASE-007; adding a new one here would trade
  a documented gap for an undocumented one.
- Retiring Index rows 009 and 010 does not orphan their inbound references. The scheduler
  source comment routing to PHASE-009 is repaired in this plan, and the tombstone rows stay so
  the review files citing them still resolve.

---

## Steps

1. Fold the former Index rows 009 and 010 into this plan as tombstones, and repair the
   in-source routing comment that names PHASE-009 so the pointer does not outlive the row —
   the incidental-doc-invalidation species this arc has now caught three times.
2. Give 9007's Warning the drain-*placement* qualifier, so the message distinguishes "you did
   not drain" from "you drained from the wrong side of the factory boundary," and reconcile it
   with 9009's Debug text and the three log tables that publish both.
3. Qualify the type-bearing tokens the relay registration emits bare, and audit the other
   emission legs for the same hazard — recording which are structurally immune (a token naming
   a type nested in the generated file itself) rather than qualifying them reflexively.
4. Measure the partial-declaration attribute-split behavior instead of inferring it: whatever
   the probe finds — a hint-name collision, a silent drop, or working behavior — becomes a test
   pinning the real answer, and a fix only if the answer is a defect.
5. Make the generator test helper reject an input compilation carrying C# errors, so a
   generator test cannot pass against source that never compiled.
6. Move the misfiled diagnostic test class into a file that names it.
7. Build the deterministic scheduler-concurrency harness: interleaving driven by explicit
   synchronization the test owns, designed so a failure reproduces. This is the plan's
   headline and the reason it opted into plan review.
8. Pin the per-scope entry-state semantics under that harness — including the timing window
   PHASE-003's round 2 recorded but left unpinned, where work a concurrent flow enqueues while
   the survivor's outermost drain runs either joins that drain or is discarded by the
   post-drain clear.
9. Settle the two named re-entrant-`Equals` pathologies the scheduler's `_gate` comment
   describes and neither guards. Guard or document-with-a-pin is a keyboard decision; leaving
   them as prose is not an option this plan permits.
10. Settle the remaining routed items on the same class: the registry test-isolation discipline
    (currently internal, uncalled, and documented in two files a new test author may not open)
    and the two allocation notes, each either fixed or accepted with its reason recorded where
    the next reader will find it.

---

## Acceptance

- [ ] A consumer who drains from outside the factory call and never drains inside it reads a
      Warning that names drain **placement** as the fix, distinguishable from the message that
      ships today — pinned on the emitted text, not on the event id alone. `[unit]`
- [ ] 9007's Warning and 9009's Debug text agree about the same situation, and the three
      published log tables carry the revised wording.
      `[explicit-skip: doc consistency, verified by review — the message itself is pinned above]`
- [ ] A compilation whose consumer namespace shadows the first segment of the framework's
      still produces relay registration that compiles and binds to the framework types — with
      a negative pin rejecting the bare form, since a `Contains` on the qualified string is
      satisfied by the bare token as a substring. `[unit]`
- [ ] The other emission legs are covered by the same standard: each type-bearing token is
      either qualified or recorded as structurally immune with the reason stated in the
      renderer. `[unit]`
- [ ] The partial-declaration attribute-split behavior is pinned by a test asserting what the
      generator **actually** does, replacing the inference recorded in PHASE-002's Discovery
      Log. `[unit]`
- [ ] A generator test whose input compilation carries a C# error fails loudly instead of
      asserting against a broken compilation. `[unit]`
- [ ] Two concurrent flows sharing one DI scope produce the documented entry-state semantics
      under deterministic interleaving the test controls — not a `Task.WhenAll` race — and the
      test fails reproducibly if those semantics change. `[unit]`
- [ ] The mid-drain enqueue window PHASE-003 recorded and left unpinned resolves to one
      observable outcome under the harness, and that outcome is pinned. `[unit]`
- [ ] A re-entrant `Equals` that mutates the queue mid-scan produces a defined, pinned
      outcome — whether that is a guard the scheduler enforces or the current behavior made
      explicit. `[unit]`
- [ ] The registry test-isolation discipline is enforceable from the test infrastructure
      rather than resident only in prose, or is accepted with the reason recorded at the seam
      a new test author reads first. `[unit]`
- [ ] Draining N deferred dispatches no longer allocates a per-dispatch LINQ chain, or the
      cost is accepted with its reason recorded on the class.
      `[explicit-skip: allocation shape — no behavior change to assert; verified at code review]`
- [ ] The misfiled diagnostic test class lives in a file that names it.
      `[explicit-skip: file organization, no behavior]`
- [ ] Both solutions build and all three suites stay green with existing assertions intact;
      totals only grow. `[explicit-skip: meta-bullet, satisfied by the Step 5 gate pre-flight]`

---

## Current State (Pre-Flight)

*(pending — filled at Step 3, before the first edit)*

---

## Test Evidence

*(pending — filled after implementation, before the Step 5 gate)*

---

## Plan Amendments

*(none yet)*

---

## Notes

- **Why one plan instead of three.** The user asked for 008/009/010 in quick succession on a
  single branch and PR. Three plan documents on a shared branch would still cost three Test
  Evidence maps, three mandatory gates, three review files, and three Discovery Log entries —
  nearly all of the ceremony for none of the saving, because the gate is per plan. Merging is
  the only shape that actually reduces it.
- **The counter-argument, and why it is overridden.** PHASE-002 and PHASE-007's drafting both
  re-split *out* of a bundle on reviewer advice that 007 carried too many unrelated items.
  That reasoning held when 007 already had seventeen items and the concurrency work would have
  ridden as a passenger. Here the bundle is the entire arc tail and the concurrency harness is
  the headline. Recorded so the reversal is visible rather than accidental.
- **Sequencing inside the plan: Steps 2 → 6 before 7 → 10.** The cheap deterministic work
  lands and commits first, so if the harness needs real design iteration, the emission and
  message work is already on the branch and the harness can still be split back out without
  stranding anything. That split would be a Re-split Discovery Log entry, not a silent
  reshuffle.
- **This plan is at the upper bound of the skill's size guidance** ("more than a day of
  draft-plus-execution means split it"). Accepted deliberately at the user's direction; the
  escape hatch above is the mitigation.
- **Plan review was declined by direction, not by risk assessment.** Recorded plainly because
  the header's one-line reason cannot carry it: on this arc's own evidence plan review returned
  4–6 veto findings on nearly every plan that ran it, several of them acceptance bullets that
  could not go red. This plan carries thirteen. The Step 5 gate remains the backstop.
- Watch for the arc's two recurring failure modes, both of which have a home here: an
  acceptance bullet that cannot go red (eleven instances), and a confident sentence about what
  the code does with no run behind it (three instances, twice inside a correction of itself).
