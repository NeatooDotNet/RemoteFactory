# Opt-In Coalescing of Queued Phase Dispatches

**Plan #:** 006
**Date:** 2026-08-14
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-18
**Plan-review opt-in:** Yes (public API surface on the attribute; dedup semantics are contract) — **ran 2026-08-18, CONCERNS; all 4 vetoes adopted by draft amendment, see [reviews/006-plan-review.md](../reviews/006-plan-review.md)**
**Code-review opt-in:** Yes (behavior-changing)

---

## Scope

Queued per user decision (2026-08-14), condition met (001–005 landed): add an opt-in
flag to `[FactoryEventHandler<T>]` so identical queued `(handler, event)` pairs collapse
to one pending dispatch, addressing the multiple-recomputes-per-save observation from
the motivating proposal without consumer code. "Identical" means the queued events
compare equal per `Equals` — the synthesized structural equality records give by
default, with the two documented hazards that entails (see Constraints). Same-event
coalescing only — cross-event coalescing stays out of scope per the parent todo. The
flag threads attribute → generator → registration → scheduler; docs, skill,
CLAUDE-DESIGN, and the Design projects each gain their coalescing coverage in the
surfaces PHASE-005 just laid. This plan does NOT change any behavior for handlers that
don't opt in, does not affect any dispatch that is never queued (`Immediate`, and the
9004/9005 no-scheduler / no-entry-call fall-throughs), and does not touch the relay
batch (every `Raise` is still collected and relayed — coalescing is about handler
dispatch, not event delivery).

---

## Intent

- A projection handler registered at `AfterFlush`/`AfterCommit` that today runs once
  per raise of a value-identical event (N staged changes → N identical recomputes) can
  opt in to running once per drain instead — declaratively, on its own attribute,
  without the raiser knowing.
- Opting in is per attribute (per handler-class × event-type registration), so one
  event type can have both coalescing and non-coalescing handlers.
- Nothing changes for anyone who doesn't opt in: duplicate queued dispatches still run
  once each, in order — today's contract, pinned as backcompat.
- **Pending-queue collapse semantics** (plan-review Q4, settled at draft): at any
  moment the scope holds at most one pending dispatch per identity key, so the
  observable counts — 9002 drained, 9006 discarded, `HasPending` — reflect the
  collapsed state. This is what makes the discard-path acceptance falsifiable; the
  in-lock mechanism that achieves it is the keyboard's choice (see Notes).
- The consumer-visible contract (the identity key, its hazards, the fail-open-warning
  preservation, unqueued-path behavior) is documented wherever PHASE-005 documented
  the phase contract.

---

## Framework & Architectural Alignment

- **Same pass-through pattern as the phase argument (PHASE-002):** the generator reads
  the attribute as primitives (cache-safety rule — no `TypedConstant` on transform
  output), renders the registration argument with the established `global::` hygiene,
  and the registry stores it per entry. No new pipeline.
- **Failure/ordering semantics stay owned by the drain point (PHASE-001/003/004):**
  coalescing changes how many dispatches a drain runs, never when a drain runs, what
  exceptions do, or cross-phase ordering.
- **Registry dedupe precedent:** `(event type, handler class)` first-registration-wins
  and NF0504's "the first declaration registers" extend to the new flag — the
  surviving declaration's flag is the flag. The five published survivor-rule strings
  this widens are enumerated in Step 7 (plan review A-V1).
- **Compiles-but-inert shapes diagnose as Warning** (NF0503/NF0504 precedent) — here,
  the flag on an `Immediate`-declared registration. The runtime unqueued paths
  (9004/9005 fall-throughs) are unreachable by any compile-time diagnostic and are
  handled by documentation instead (plan review B-C4).
- **`Internal` namespace policy (PHASE-004):** scheduler changes stay public/unsealed
  per CLAUDE-DESIGN's policy section; the scheduler interface grows an overload
  ("may change in any release" is the namespace's contract, but 53 pinned test call
  sites make a required-parameter change contradict this plan's own backcompat
  constraint — plan review B-C1).
- **Docs land with the behavior** (project rule): the PHASE-005 surfaces grow their
  coalescing rows/paragraphs in this plan, not a follow-up.

---

## Constraints & Invariants

- **Backcompat is absolute:** without the flag, queued duplicate dispatches run once
  each — the full existing suite passes unmodified (existing tests are sacred).
- **The relay batch is untouched:** coalescing never drops a collected event;
  `IFactoryEventRelay` consumers see every raise. (Structurally true — collection
  happens at raise time before any queueing — and stays that way.)
- **No collapse may erase a fail-open warning obligation (plan review B-V1):** a
  surviving dispatch warns (9007) at the post-completion sweep if *any* collapsed
  constituent would have warned — the `EnqueuedMidDrain` merge is warn-preserving,
  pinned by a test that goes red under a latest-bit-wins merge. A consumer who wired
  no drain at all must still get their 9007.
- **The identity contract is `Equals`, stated with both hazards (plan review B-V3):**
  reference-typed event members defeat synthesized equality → the feature is a
  documented no-op there (raises stay distinct, N dispatches, no signal beyond the
  docs saying so); a custom `Equals` override can over-collapse semantically distinct
  raises → documented consumer responsibility. Identity is evaluated when work
  becomes pending (pending-queue semantics), so record mutability cannot reopen the
  question at drain time. Docs recommend value-only payloads for coalescing handlers.
- **What ordering actually requires (plan review B-C7):** the cross-phase
  earliest-first sweep is preserved (duplicates share a phase queue by definition;
  within-phase order is documented unspecified) — the real invariants a collapse must
  respect are the sweep and the warn-bit, not a within-phase position.
- **Flag is inert wherever no queue exists:** `Immediate` dispatch, and phased raises
  hitting the 9004 (no scheduler) / 9005 (no entry call) immediate fall-throughs, run
  N times regardless of the flag — stated in the attribute XML and docs.
- **Test-isolation discipline (plan review B-C8):** coalescing and non-coalescing
  integration cases use distinct handler classes *and* distinct event types (the
  registry is process-static, first-wins, `Clear()` uncalled); the N identical raises
  share one per-test Guid, while distinctness controls vary it.
- **Generator cache safety:** the flag crosses the transform boundary as a primitive.
- **Trimming posture unchanged:** registrations stay inside the `IsServerRuntime`
  guard and the forwarding-holder shape; nothing new ships handler bodies to clients.
- Release impact: `feat:` — minor bump. Public-API growth is by **new overloads**
  (`RegisterHandler`) and a new attribute member — never optional parameters on
  existing public methods, which are binary-breaking (plan review B-C2).

---

## Steps

1. **Attribute surface:** add the opt-in member to `[FactoryEventHandler<T>]` as a
   named property (the ctor-overload form has a silent trap: the transform treats any
   non-`int` first constructor argument as `Immediate` — plan review B-C3), with XML
   stating the contract: the `Equals` identity key and its two hazards, per-drain
   collapse, warn-preservation, inert on any unqueued dispatch, no effect on relay.
   Keyboard check: verify the property form binds as an attribute named argument.
2. **Registry:** carry the flag per handler entry through registration (new public
   overload; existing overloads untouched) and `GetHandlers`, preserving the
   first-wins dedupe contract — the surviving declaration's flag wins with it.
3. **Generator pass-through:** read the flag from the attribute's named arguments as a
   primitive, emit it in the registration call; NF0505 (Warning) for the flag on an
   `Immediate`-declared registration, per the inert-shape precedent; reword NF0504's
   message so the survivor rule covers the whole registration (phase and flag), not
   the phase alone.
4. **Scheduler:** pending-queue collapse behind a new `Enqueue` overload — at most one
   pending dispatch per (handler delegate, event-`Equals`, options) key, with the
   warn-preserving `EnqueuedMidDrain` merge, a Debug log event (9008) announcing each
   collapse, and 9001/9002/9006 counts reflecting the collapsed queue (a collapsed
   raise logs 9008 rather than a second 9001).
5. **Dispatcher:** thread the flag from `GetHandlers` to the new `Enqueue` overload.
6. **Tests at every seam:** registry flag round-trip and survivor's-flag; scheduler
   collapse semantics (identical pairs collapse to one pending dispatch; distinct
   events, handlers, options, and phases don't; warn-preserving merge red-proofed
   against latest-bit-wins; 9008 emission; 9002/9006 counts); generator emission
   (named-arg read, rendered form, NF0505, NF0504 message + survivor flag);
   end-to-end attribute-declared coalescing plus the backcompat and
   discard-count pins through the client/server containers.
7. **Docs + Design:** coalescing paragraph/rows in `docs/factory-events.md` and
   `attributes-reference.md`, the skill reference + SKILL.md row, CLAUDE-DESIGN
   (pattern narrative + Quick Decisions + log-event rows: new 9008, plus the
   9001/9002/9006 rows' behavior under collapse), a Design-project demonstration with
   tests — **plus the five survivor-rule strings (plan review A-V1):**
   `docs/factory-events.md` NF0504 row, `docs/attributes-reference.md` (both
   occurrences), the NF0504 message format itself, and the registry XML's
   "keeps the phase registered first" remark.
8. **Gate:** Test Evidence with expected totals recorded per suite (the RP-0
   countermeasure is the count check, not just building both solutions), single
   build+test run to logs, test-reviewer; code review (opted in).

---

## Acceptance

- [ ] A handler opted in at `AfterFlush` or `AfterCommit` runs **once** at its drain
      point when the same value-identical event was raised N times during the entry
      call — observed end to end with attribute-declared handlers through the
      client/server containers. `[integration]`
- [ ] Without the flag, the same N raises produce N dispatches at the drain point —
      the backcompat contract, pinned positively with a distinct handler class and
      event type from the coalescing case. `[integration]`
- [ ] Value-distinct events, distinct handler registrations for the same event, the
      same event at distinct phases, and distinct `RaiseOptions` do not collapse into
      each other. `[unit]`
- [ ] On entry-call failure, a coalescing handler's N identical raises are discarded
      as **one** pending dispatch — 9006 reports the collapsed count (a non-coalescing
      sibling in the same test reports N) — and the handler never runs. `[unit]`
- [ ] A never-drained coalescing `AfterFlush` handler gets exactly one 9007 for the
      surviving dispatch — including when pre-drain and mid-drain raises collapsed:
      the warn-preserving merge is pinned by a test that goes red under a
      latest-bit-wins merge. `[unit]`
- [ ] Each collapse is observable at Debug with the new event id (9008), and a
      collapsed raise does not double-log 9001. `[unit]`
- [ ] The generator threads the flag from named argument to registration; NF0505
      (Warning) fires for the flag on an `Immediate`-declared registration and does
      not fire otherwise; the NF0504 survivor's flag is the one that registers and
      the reworded message covers it. `[unit]`
- [ ] Docs, skill, CLAUDE-DESIGN, and Design projects document/demonstrate the
      coalescing contract — including the identity hazards, unqueued-path inertness,
      and the five widened survivor-rule strings.
      `[explicit-skip: prose + Design demonstration, gated like PHASE-005's]`
- [ ] Full existing suite passes unmodified (verified at the gate as a diff property,
      not claimed as a test); build green both solutions with expected totals
      matching. `[explicit-skip: meta-bullet, satisfied by the gate run]`

---

## Current State (Pre-Flight)

Walked 2026-08-18, after plan review, before any edit. Tree = PHASE-005 tip
(`875519b`, stacked branch).

**Runtime seams:**
- `FactoryEventHandlerAttribute.cs:45-61` — `sealed`, two ctors (default →
  `Immediate`), `Phase` get-only. The new member goes in as `public bool Coalesce
  { get; set; }` — attribute named arguments require a read-write instance property
  (`init` does not bind in attribute named-argument position); keyboard verifies.
- `FactoryEventHandlerRegistry.cs` — private `HandlerEntry` struct (:15-27) holds
  `(HandlerClassType, Phase, Invoke)`; two public `RegisterHandler` overloads
  (:32-36 → :56-71) with first-wins dedupe under a per-list lock (:66); internal
  `GetHandlers` returns `(Phase, Invoke)` tuples (:77-85). Flag: new field + third
  public overload + tuple element.
- `FactoryEventsDispatcher.cs:71-93` — `foreach (var (phase, handler) in handlers)`;
  the enqueue site is :77; the 9004/9005 immediate fall-throughs for phased handlers
  are :81-90 (the unqueued paths the flag must be documented inert on).
- `Internal/FactoryEventPhaseScheduler.cs` — interface `Enqueue` :58 (new overload
  beside it); `QueuedDispatch` :134-138 carries `(Event, Options, Handler,
  EnqueuedMidDrain)` — everything the identity key and warn-merge need;
  `Enqueue` :215-235 stamps `_activeDrains > 0` (:228) and logs 9001 (:231-234);
  `DrainAsync`'s 9007 gate is :270-273; `TryDequeueThrough` :349-368 (earliest-first
  sweep, untouched); `ClearAtExit` :318-342 counts discards into 9006 — the
  pending-queue collapse makes that count the discard discriminator.
- `Internal/Log.cs` — ids top out at 9007; 9008 free for the collapse Debug event.

**Generator seams:**
- `FactoryGenerator.RelayHandler.cs` — `ReadDispatchPhase` :33-58 reads
  `ConstructorArguments` only; `NamedArguments` is unread today, so the flag read is
  additive with no interaction with the non-`int`-first-arg fallback trap the review
  named. Entry construction :286-297 (primitives into `EventHandlerEntry`); the
  NF0504 survivor map `registeredPhaseByEventType` :96, :131-146, :299-300 stores the
  surviving *phase string* — carries the surviving flag too if the reworded message
  names it (keyboard decision on message args).
- `Renderer/RelayHandlerRenderer.cs:152-188` — emits the three-arg
  `RegisterHandler<{event}>(typeof({class}), {phase}, lambda)` at :178; the flag
  becomes a fourth argument on the new overload (bool literal — no `global::`
  concern).
- `Model/RelayHandlerModel.cs:58-110` — `EventHandlerEntry` primitives ctor
  (`phaseName`, `phaseValue`) gains a bool.
- `DiagnosticDescriptors.cs:268-279` — NF0504 descriptor; message format :271 is the
  string A-V1 rewords. NF0505 unused; descriptor map `FactoryGenerator.cs:153-157`
  gains the case.

**Pinned call-site reality (why overloads):** `Enqueue` has 53 test call sites across
the four named unit suites plus one integration target; `RegisterHandler` ~45. All
stay byte-identical under the overload shape.

**No surprises vs. the amended draft** — the review's Pass B anchors matched the code
as read; no pre-flight amendments needed.

---

## Test Evidence

*(filled after implementation, before the Step 5 gate)*

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| | | | |

---

## Plan Amendments

### 2026-08-18 — Plan review adopted (pre-implementation draft amendment)

- **Section affected:** Scope, Intent, Framework Alignment, Constraints, Steps 1–8,
  Acceptance, Notes
- **Original said:** identity = "events are records — value equality"; Acceptance
  bullet 4 bundled discard + 9007 + a diff property; Step 7 named only the new
  coalescing prose; five design questions listed as open.
- **What changed:** all 4 vetoes adopted — warn-preserving `EnqueuedMidDrain` merge as
  a Constraint with a red-proof-required pin (B-V1); bullet 4 split into separately
  falsifiable bullets with the 9006-count discriminator, which forced settling Q4 to
  pending-queue collapse semantics (B-V2 + the reviewer's unnamed coupling); identity
  restated as `Equals` with both hazards documented (B-V3); Step 7 widened to the five
  survivor-rule strings (A-V1). Callouts folded in: `RegisterHandler`/`Enqueue` grow
  overloads not optional parameters (B-C1/C2), named attribute property (B-C3),
  unqueued-path inertness broadened beyond `Immediate` (B-C4), 9001/9002/9006
  behavior decided and documented (B-C5), Q2 closed as Step 6 already implied —
  options in the key (B-C6), the vacuous ordering Constraint replaced (B-C7),
  test-isolation Constraint added (B-C8), enqueue-scan cost named in Notes (B-C9),
  gate records expected totals (B-C10). A-C1 handled in the parent todo (AC bullet +
  Out of Scope clause + Discovery Log trace for the 2026-08-14 queueing decision).
- **Why:** plan review 2026-08-18 (CONCERNS).
- **Discovery Log link:** 2026-08-18 — PHASE-006 plan review entry.

---

## Notes

- **Branch stack (recorded per CONVENTIONS.md):** `PHASE-006-coalescing` is stacked on
  `PHASE-005-design-docs-skill` (PR #83, open at branch time) rather than on `PHASE`,
  because 006 edits the todo bookkeeping and the doc surfaces 005 created. Merge in
  order: #83 first, then this plan's PR.
- **Questions settled at draft (were "open" in the pre-review draft):** Immediate+flag
  → NF0505 Warning + documented runtime inertness on all unqueued paths; identity key
  includes `RaiseOptions` (distinct options don't collapse; note: options are
  invisible to attribute-declared handlers — the emitted lambda never forwards them —
  so including them can only under-coalesce there, never miscall a handler); survivor
  observability → the warn-bit makes it observable, settled by the warn-preserving
  merge; collapse point → pending-queue semantics; interface shape → overloads.
- **Left to the keyboard, deliberately:** the in-lock mechanism for pending-queue
  collapse. `Queue<T>` has no removal primitive and `_gate` serializes concurrent
  flows, so a naive per-enqueue scan is O(n²) under the lock (plan review B-C9) —
  side index keyed on the identity vs. mark-and-replace are both acceptable so long
  as the pending-queue counts and the warn-merge hold.
- The motivating observation is the zTreatment proposal's "any of these four events →
  one recompute" — the deferred cross-event half stays out; this plan only removes
  the same-event N× duplication.
