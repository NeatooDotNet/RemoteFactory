# Opt-In Coalescing of Queued Phase Dispatches

**Plan #:** 006
**Date:** 2026-08-14
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-17
**Plan-review opt-in:** Yes (public API surface on the attribute; dedup semantics are contract)
**Code-review opt-in:** Yes (behavior-changing)

---

## Scope

Queued per user decision (2026-08-14), condition met (001–005 landed): add an opt-in
flag to `[FactoryEventHandler<T>]` so identical queued `(handler, event)` pairs (events
are records — value equality) collapse to one dispatch when a phase queue drains,
addressing the multiple-recomputes-per-save observation from the motivating proposal
without consumer code. Same-event coalescing only — cross-event coalescing stays out of
scope per the parent todo. The flag threads attribute → generator → registration →
scheduler; docs, skill, CLAUDE-DESIGN, and the Design projects each gain their
coalescing coverage in the surfaces PHASE-005 just laid. This plan does NOT change any
behavior for handlers that don't opt in, does not coalesce `Immediate` dispatches
(they are never queued), and does not touch the relay batch (every `Raise` is still
collected and relayed — coalescing is about handler dispatch, not event delivery).

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
- The consumer-visible contract (what "identical" means, which dispatch survives, how
  mid-drain raises interact) is documented wherever PHASE-005 documented the phase
  contract.

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
  and NF0504's "the first declaration registers, including its phase" extend to the new
  flag — the surviving declaration's flag is the flag.
- **Compiles-but-inert shapes diagnose as Warning** (NF0503/NF0504 precedent) — the
  candidate here is the flag on an `Immediate` registration, which has no queue to
  coalesce.
- **`Internal` namespace policy (PHASE-004):** scheduler changes stay public/unsealed
  per CLAUDE-DESIGN's policy section; the scheduler interface may change shape there
  ("may change in any release" is the namespace's contract).
- **Docs land with the behavior** (project rule): the PHASE-005 surfaces (docs
  factory-events + attributes-reference, skill, CLAUDE-DESIGN, Design projects) grow
  their coalescing rows/paragraphs in this plan, not a follow-up.

---

## Constraints & Invariants

- **Backcompat is absolute:** without the flag, queued duplicate dispatches run once
  each — the full existing suite passes unmodified (existing tests are sacred).
- **The relay batch is untouched:** coalescing never drops a collected event;
  `IFactoryEventRelay` consumers see every raise.
- **`Immediate` dispatch is never coalesced** — it is never queued; the flag on an
  `Immediate` registration must be loud (diagnostic or documented no-op — plan review
  weighs in), never silently meaningful.
- **Discard-on-failure, fail-open 9007, the mid-drain carve-out, and per-scope
  granularity all survive unchanged** — the PHASE-001..005 pins for these stay green
  unmodified.
- **Coalescing must not reorder:** the surviving dispatch occupies a position that
  respects the existing earliest-first cross-phase sweep.
- **Generator cache safety:** the flag crosses the transform boundary as a primitive.
- **Trimming posture unchanged:** registrations stay inside the `IsServerRuntime`
  guard and the forwarding-holder shape; nothing new ships handler bodies to clients.
- Release impact: `feat:` — minor bump; no breaking public API (new overloads/optional
  members only on non-`Internal` surfaces).

---

## Steps

1. **Attribute surface:** add the opt-in member to `[FactoryEventHandler<T>]` (named
   flag alongside the phase), with XML that states the coalescing contract — what
   "identical" means, per-drain collapse, no effect at `Immediate`, no effect on relay.
2. **Registry:** carry the flag per handler entry through registration and
   `GetHandlers`, preserving the first-wins dedupe contract (the surviving
   declaration's flag wins with it).
3. **Generator pass-through:** read the flag as a primitive in the relay-handler
   transform, emit it in the registration call; settle the `Immediate`+flag shape
   (diagnostic vs. documented inert) per plan review.
4. **Scheduler:** collapse identical pending pairs so a drain runs one dispatch where
   today it runs N — scoped to pending work (already-dispatched work is history, not a
   dedupe target), with a Debug log event announcing each collapse, and the 9007
   warning firing once for the surviving dispatch rather than once per collapsed raise.
5. **Dispatcher:** thread the flag from `GetHandlers` to `Enqueue`.
6. **Tests at every seam:** registry flag round-trip; scheduler collapse semantics
   (identical pairs collapse; distinct events, distinct handlers, and distinct options
   don't; ordering position; 9007-once; mid-drain raise behavior); generator emission
   (flag read, rendered form, NF0504 survivor's flag, `Immediate`+flag shape);
   end-to-end attribute-declared coalescing through the client/server containers, plus
   the backcompat pin (no flag → N dispatches).
7. **Docs + Design:** coalescing paragraph/rows in `docs/factory-events.md` and
   `attributes-reference.md`, the skill reference + SKILL.md row, CLAUDE-DESIGN
   (pattern narrative + Quick Decisions + log-event row), and a Design-project
   demonstration with tests.
8. **Gate:** Test Evidence, single build+test run to logs (both solutions — RP-0
   rule), test-reviewer; code review (opted in).

---

## Acceptance

- [ ] A handler opted in at `AfterFlush` or `AfterCommit` runs **once** at its drain
      point when the same value-identical event was raised N times during the entry
      call — observed end to end with attribute-declared handlers through the
      client/server containers. `[integration]`
- [ ] Without the flag, the same N raises produce N dispatches at the drain point —
      the backcompat contract, pinned positively. `[integration]`
- [ ] Value-distinct events, distinct handler registrations for the same event, and
      the same event at distinct phases do not collapse into each other. `[unit]`
- [ ] Coalescing composes with the shipped semantics: discarded on entry-call failure,
      fail-open with a single 9007 for a never-drained surviving dispatch, and the
      mid-drain carve-out — no existing pin modified. `[unit]`
- [ ] The generator threads the flag from attribute to registration; the NF0504
      survivor's flag is the one that registers; the `Immediate`+flag shape resolves
      loudly per the plan-review decision. `[unit]`
- [ ] Each collapse is observable in logs at Debug with a dedicated event id.
      `[unit]`
- [ ] Docs, skill, CLAUDE-DESIGN, and Design projects document/demonstrate the
      coalescing contract on the surfaces PHASE-005 established.
      `[explicit-skip: prose + Design demonstration, gated like PHASE-005's]`
- [ ] Full existing suite passes unmodified; build green both solutions.
      `[explicit-skip: meta-bullet, satisfied by the gate run]`

---

## Current State (Pre-Flight)

*(filled at Step 3, after plan review and before the first edit)*

---

## Test Evidence

*(filled after implementation, before the Step 5 gate)*

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| | | | |

---

## Plan Amendments

*(none yet)*

---

## Notes

- **Branch stack (recorded per CONVENTIONS.md):** `PHASE-006-coalescing` is stacked on
  `PHASE-005-design-docs-skill` (PR #83, open at branch time) rather than on `PHASE`,
  because 006 edits the todo bookkeeping and the doc surfaces 005 created. Merge in
  order: #83 first, then this plan's PR.
- **Open questions flagged for plan review** (named, not designed — the keyboard and
  the reviewer settle them):
  1. The `Immediate`+flag shape: Warning diagnostic (NF0505?) per the NF0503/NF0504
     inert-shape precedent, vs. documented runtime no-op.
  2. The identity key: does `RaiseOptions` participate (ServerOnly vs. None duplicates
     — identical or distinct)?
  3. Which dispatch survives a collapse — the earliest position (keeps sweep ordering
     trivially) or the latest — and whether the answer is observable at all given
     value-identical events.
  4. Collapse point: on enqueue (pending-queue check) vs. at drain (dequeue-time
     dedupe) — semantics differ only for raises that interleave with a drain in
     flight; the mid-drain carve-out constraint above bounds the answer.
  5. Whether the scheduler's `Enqueue` grows a parameter (an `Internal` interface —
     allowed to change) or an overload.
- The motivating observation is the zTreatment proposal's "any of these four events →
  one recompute" — the deferred cross-event half stays out; this plan only removes
  the same-event N× duplication.
