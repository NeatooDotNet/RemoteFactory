# Arc Tail: Emission Qualification, Scheduler Concurrency, and the 9007 Placement Qualifier

**Plan #:** 008
**Date:** 2026-08-27
**Related Todo:** [../todo.md](../todo.md)
**Status:** In Progress (implementation complete; Step 5 gate pending)
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

Walked 2026-08-27 before the first edit. Covers the deterministic legs (Steps 1–6) plus the
scheduler and registry surfaces for Steps 7–10; the partial-declaration probe is an experiment
and is deliberately left to implementation.

**The `global::` strip is deliberate, and the naive fix breaks a pinned diagnostic.**
`FactoryGenerator.RelayHandler.cs:149-151` takes `SymbolDisplayFormat.FullyQualifiedFormat` —
which *includes* `global::`, as `FactoryGenerator.Events.cs:47` confirms by comparing against
`"global::Neatoo.RemoteFactory.FactoryEventBase"` — and then explicitly strips the prefix. The
same strip repeats at `:205-206` for the handler method's event parameter. So the row's premise
("the token is not qualified") is correct, but its implied cause ("someone forgot") is not.
That stripped value has **three** consumers, not one:

1. the `registeredPhaseByEventType` dictionary key for NF0504 duplicate detection (`:153`),
2. the `paramTypeName` comparison that matches handler methods to the attribute (`:208`), and
3. the value carried into the model and emitted at `RelayHandlerRenderer.cs:180` and `:182`.

It also flows into the **NF0504 diagnostic message** at `:165` — a message PHASE-002 pinned
deliberately, including the source-order-wins case. Deleting the strip would change that
message to a `global::`-prefixed type name and take a sacred assertion with it. The fix
therefore has to separate the normalized comparison key from the emitted form, not remove the
normalization.

**Two emission tokens need qualifying; two others are structurally immune.**
`RelayHandlerRenderer.cs:180` (`RegisterHandler<{eventTypeName}>`) and `:182`
(`({eventTypeName})eventObj`) both carry the hazard. By contrast `typeof({className})` (`:180`)
and `{className}.{MethodName}` (`:174`) are emitted *inside the user's own namespace and inside
the user's own partial class body* (`Render` at `:50-52`), where a type shadowing the enclosing
class's own name is CS0542 — immune by construction, and to be recorded as such rather than
qualified reflexively.

> **Correction, made during implementation.** This paragraph first ended "Service-parameter
> types are already qualified: `RelayHandler.cs:293` uses `FullyQualifiedFormat` with **no**
> strip." That is wrong — `:294-295` strips them exactly as `:150-151` does, so
> `sp.GetRequiredService<{p.Type}>()` was a **third** bare emission site, not zero. Left visible
> rather than quietly overwritten: it is the read-one-line-too-few error this arc keeps logging,
> committed here by the pre-flight whose whole purpose was to prevent it.

**Same hazard class, undecided:** the bare framework tokens the generated body leans on the
file's `using` for — `FactoryEventHandlerRegistry`, `NeatooRuntime.IsServerRuntime`. Not named
by the routed item; cheap and consistent to qualify. Keyboard decision, recorded either way.

**9007's guidance sentence has zero test coverage.** Every pin matches on `EventId == 9007`
alone (`FactoryEventPhaseSchedulerTests.cs:305,361`, `FactoryEventPhaseCoalescingTests.cs:376,416`,
`FactoryEventPhaseCoordinatorTests.cs:311`, integration `:120,137`); the single message
assertion (integration `:120`) checks only that the *event type name* appears. So the sentence
this plan rewrites is unpinned today — which is why the acceptance bullet demands a pin on the
emitted text and not on the id.

**The ratchet is bigger than PHASE-002 predicted.** Seven assertion sites hardcode the bare
emitted form, not five: `AssemblyAttributeEmissionTests.cs:821, 840, 854, 857, 865, 1060, 1083`.
All are the chartered edit under Constraints.

**Confirmed as recorded:** `NF04xxFactoryEventHandlerTests.cs:14` declares
`public class NF05xxFactoryEventHandlerTests`. `RunGeneratorTracked` is
`DiagnosticTestHelper.cs:187` and inspects nothing about the input compilation;
`IncrementalCacheTests.cs:234-235` already documents that gap in a remark.

**Scheduler / registry surfaces for Steps 7–10.** The `_gate` comment
(`FactoryEventPhaseScheduler.cs:145-157`) names both unguarded re-entrancy specifics and routes
them to PHASE-009 — that comment is the stale in-source pointer Step 1 repairs. The registry
itself is **not** a defect: `RegisterHandler` and `GetHandlers` both `lock (list)` and the read
snapshots to an array (`FactoryEventHandlerRegistry.cs:86-95, 101-109`). One narrow
observation for the harness: `Clear()` (`:114`) drops the `ConcurrentDictionary` entries without
taking the per-list locks, so a `Clear()` racing a `RegisterHandler` between its `GetOrAdd` and
its `lock` can strand a registration in a detached list. Test-only method; note, don't redesign.

---

## Test Evidence

All cited tests are in `RemoteFactory.UnitTests`. Suites at close: **unit 755×2 TFMs**
(743 → 755, +12), **integration 595×2 (+5 standing skips)**, **Design 98×2** — both
solutions built explicitly, per the PHASE-005 RP-0 trap. Logs: `reviews/008-build.log`,
`reviews/008-test.log`.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| 9007's Warning names drain placement | `[unit]` | `Internal.FactoryEventPhaseSchedulerTests.DrainAsync_NeverDrainedWarning_TellsTheConsumerWhereToPutTheDrain` | ✓ |
| 9007 and 9009 agree; three log tables revised | `[explicit-skip]` | Not a test. `Log.cs` 9007 + 9009 read together; tables in `CLAUDE-DESIGN.md`, `docs/factory-events.md`, `skills/RemoteFactory/references/factory-events.md`. The message itself is pinned by the row above | n/a |
| Shadowing consumer namespace still binds correctly | `[unit]` | `FactoryGenerator.AssemblyAttributeEmissionTests.RelayHandler_ConsumerNamespaceShadowsEveryUnqualifiedRoute_OutputStillCompiles` | ✓ |
| Every emitted type token qualified (negative pin) | `[unit]` | `FactoryGenerator.AssemblyAttributeEmissionTests.RelayHandler_EveryEmittedTypeToken_IsGlobalQualified` | ✓ |
| Other legs qualified or recorded immune | `[unit]` | Same test — its `typeof(className)` exception is asserted by omission and the reason is stated in `RelayHandlerRenderer` | ✓ |
| Partial-declaration behavior pinned to what the generator does | `[unit]` | `FactoryGenerator.AssemblyAttributeEmissionTests.RelayHandler_AttributesSplitAcrossPartials_EmitOneFileWithEachRegistrationOnce` | ✓ |
| Generator test with a CS error fails loudly | `[unit]` | `FactoryGenerator.Core.IncrementalCacheTests.RunGeneratorTracked_BaseFixtureDoesNotCompile_ThrowsNamingTheFixture` and `…RunGeneratorTracked_AppendedEditDoesNotCompile_ThrowsNamingTheEdit` | ✓ |
| Two concurrent flows, deterministic interleaving | `[unit]` | `Internal.FactoryEventPhaseSchedulerConcurrencyTests.MidDrainEnqueueFromAnotherFlow_JoinsTheRunningDrain` and `…ConsumerDrainNestedInsideTheEntryDrain_DoesNotClearTheMidDrainMarkEarly` | ✓ |
| The mid-drain enqueue window resolves to one pinned outcome | `[unit]` | `…MidDrainEnqueueFromAnotherFlow_JoinsTheRunningDrain` and `…MidDrainEnqueueIntoAnAlreadyPassedPhase_StillJoinsTheRunningDrain`. **Partial by design:** the discard branch needs a seam between the drain loop and `ClearAtExit` that does not exist from outside the class; recorded in the test's remarks, not silently dropped | ✓ (reachable branch) |
| Re-entrant `Equals` has a defined, pinned outcome | `[unit]` | `…ReentrantEqualsThatAppendsMidScan_MissesTheCollapseButKeepsTheQueueIntact` and `…ReentrantEqualsThatEnqueuesAnIdenticalEntry_StillCollapsesToOnePendingDispatch` | ✓ |
| Registry isolation enforceable, or accepted with the reason at the seam | `[unit]` | `…RegistryEntriesAreKeyedByEventType_SoPerTestEventTypesAreSufficientIsolation`, plus the correction written onto `FactoryEventHandlerRegistry.Clear()`'s XML. **The routed remedy was rejected on measured evidence** — see RP-6 | ✓ |
| Draining N dispatches allocates no per-dispatch LINQ chain | `[explicit-skip]` | Fixed rather than accepted: `HasPending` and `TryDequeueThrough` are hand-rolled scans. No behavior to assert; the existing FIFO/sweep/collapse pins prove semantics are unchanged, and RP-8 confirms the sweep is still discriminating after the rewrite | n/a |
| Misfiled diagnostic test class | `[explicit-skip]` | `NF04xxFactoryEventHandlerTests.cs` → `NF05xxFactoryEventHandlerTests.cs` (git mv, class unchanged) | n/a |
| Both solutions build; all suites green, totals only grow | `[explicit-skip]` | `reviews/008-build.log` (0 errors), `reviews/008-test.log` (six green summaries) | n/a |

**No `MISSING` rows.** One row is explicitly partial (the discard branch) with the
unreachability reason recorded rather than a coverage claim; one row records a routed
remedy rejected on measurement rather than delivered as asked.

---

## Plan Amendments

### 2026-08-27 — A1: the partial-split probe found a defect, so Step 4's conditional fix fired

- **Section affected:** Step 4, and the Acceptance bullet on the partial-declaration behavior.
- **Original said:** measure the behavior and pin whatever it is, "and a fix only if the answer
  is a defect."
- **What changed:** the answer was a defect, so a fix landed. A handler class whose
  `[FactoryEventHandler<T>]` attributes are split across two partial declarations produced two
  identical models sharing one hint name, and the second `AddSource` threw `ArgumentException`
  → **CS8785**. The transform now emits one model per symbol, from a canonically-chosen
  declaration (file path, then span start, so the choice is stable for the incremental cache).
- **Why:** CS8785 is not scoped to the offending class. The generator "will not contribute to
  the output," so **every factory in the assembly disappears** and the consumer gets a cascade
  of missing-type errors pointing nowhere near the split partial. PHASE-002 inferred the
  collision and recorded it as unmeasured; the measurement made it a severity question rather
  than a curiosity. The fix also stops such a class reporting its diagnostics once per partial.
- **Discovery Log link:** 2026-08-27 — PHASE-008 (the inferred collision was real).

### 2026-08-27 — A2: the emission fix covers three token classes, not one

- **Section affected:** Step 3.
- **Original said:** qualify the event type in the relay registration and audit the other legs.
- **What changed:** three token classes were qualified — the event type (generic argument and
  the cast), the `[Service]` parameter types, and the framework tokens the generated body took
  from the file's `using` (`NeatooRuntime`, `FactoryEventHandlerRegistry`, `IServiceCollection`,
  `NeatooFactory`). `typeof({className})` and the handler invocation stay bare and are
  documented as structurally immune.
- **Why:** the audit found the service-parameter types stripped on the same lines as the event
  type (see the correction in Current State), and the framework tokens carry the identical
  defect. Fixing only the routed token would have left the same bug behind a comment claiming
  it was fixed. The normalization itself had to stay: the stripped string is also the NF0504
  dedupe key and the type name printed in five diagnostics, several pinned — so the prefix is
  re-applied at the emission site, which is already how the assembly attribute does it.

### 2026-08-27 — A3: the registry-isolation remedy was rejected on measurement

- **Section affected:** Step 10, and its Acceptance bullet.
- **Original said:** make the registry test-isolation discipline "enforceable from the test
  infrastructure rather than resident only in prose," the routed reading of which was to pin
  the `Clear()` escape hatch the discipline notes point at.
- **What changed:** no `Clear()` pin. The test that called it turned an existing
  `FactoryEntryCallTests` case red — that test passes alone and fails only beside the new one.
  What shipped instead: a pin on the property the discipline actually rests on (entries keyed
  by `(event type, handler class)`, so a test that invents its own event type needs no
  teardown), and the correction written onto `Clear()`'s own XML doc.
- **Why:** the registry is process-wide static and xUnit runs test classes in parallel, so
  `Clear()` strips registrations out from under whatever is mid-run. The routed item pointed at
  an escape hatch that breaks the suite. Delivering it as written would have meant weakening a
  sacred test to accommodate a new one — the inversion the standing rule forbids.
- **Discovery Log link:** 2026-08-27 — PHASE-008 (three routed remedies, two of them wrong).

### 2026-08-27 — A4: the allocation items were fixed rather than accepted

- **Section affected:** Step 10's second half, and its `[explicit-skip]` Acceptance bullet.
- **Original said:** each allocation note "either fixed or accepted with its reason recorded."
- **What changed:** both fixed. `HasPending` and `TryDequeueThrough` are hand-rolled scans over
  the dictionary's struct enumerator, allocating nothing.
- **Why:** `TryDequeueThrough` ran its `Where` + `OrderBy` **per dequeued dispatch**, so the
  bulk-save scenario the storage comment names paid it thousands of times — the cost PHASE-007's
  O(1) dequeue fix did not touch. The replacement selects the minimum non-empty phase at or
  before `through` directly, which is the same answer the sort-then-first-non-empty form
  produced. Semantics are held down by the existing FIFO, sweep, and collapse pins, and RP-8
  re-confirms the sweep still discriminates after the rewrite.

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
