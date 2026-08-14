# PHASE-001 Code Review — 2026-08-14

**Reviewer:** code-reviewer agent (findings-only, no grade)
**Disposition:** all four veto-tier findings fixed; callouts fixed, traced forward, or
accepted with reason below.

## Verified clean by the reviewer

- **Trimming invariant holds.** `[DynamicallyAccessedMembers(All)]` is present on `TEvent`
  for *both* `RegisterHandler` overloads, and the 2-arg forwards into the annotated 3-arg
  parameter so the annotation flows rather than being dropped at the hop.
- **No reflection introduced.** The only `GetType()` calls are `.Name` for log messages.
- **Backward compatibility is structural:** zero modified test files; the generator's
  emitted 2-arg `RegisterHandler` call and the checked-in generated output still compile.
- **The `TryDequeueThrough` LINQ-over-dictionary pattern is safe:** `OrderBy` buffers its
  source on first `MoveNext`, and the loop returns synchronously before any `await`, so the
  only mutation that can add a key (`Enqueue`, reachable only from inside a handler) happens
  after enumeration has finished.
- **Scoping split is right:** process-static registry keeps its locking; the per-scope
  scheduler is unsynchronized, matching the established `FactoryEventCollector` stance.
- **The build-config move is correct:** the generator matches the handler attribute by
  metadata-name string in both places it looks, so it never needed the type.

## Veto-tier findings — all fixed

- **V1 — `CLAUDE-DESIGN.md` log-id table not updated.** Added rows for 9001/9002/9003 (and
  9004, added in this same pass) with propagation semantics reflecting the drain-point
  keying.
- **V2 — no test-review record.** The gate had in fact run; the record is now written to
  [`001-test-review.md`](./001-test-review.md), including a second round.
- **V3 — Test Evidence cited numbers the log contradicted.** Corrected to the actual
  figures: 653 unit × 2 TFMs against a 614 baseline on `main`, integration 561 passed /
  5 skipped / 566 total, Design 86 × 2 with its own retained log.
- **V4 — `DispatchPhase` XML doc asserted an ordering guarantee the drain-until-empty
  stance breaks.** Added the carve-out sentence naming the re-entrant case, and rewrote the
  `DrainAsync` doc (which the test reviewer independently flagged as stale).

## Callout dispositions

- **C1 (naming collision)** — fixed by renaming to `IFactoryEventPhaseScheduler` /
  `FactoryEventPhaseScheduler`, settled *before* PHASE-003 emits generated call sites.
  (`…Queue` was not available: CA1711 forbids the suffix, which is what produced the
  original `Dispatcher` name.)
- **C2 (silent phase→Immediate fallback)** — fixed: new debug event id 9004 fires when a
  phased handler is raised in a scope with no scheduler, matching the todo's house rule of
  "dispatch immediately with a debug log, no silent drop."
- **C3 (fail-open discriminator already plumbed)** — recorded as a constraint on the
  PHASE-004 draft; the sweep is now also test-pinned, so 004 wires a warning rather than
  re-plumbing.
- **C4 (no discard affordance)** — recorded as a hard constraint on the PHASE-003 draft:
  the drain call must sit on the success path only, never in a `finally`.
- **C5 (near-tautological provider assertion)** — fixed: `Assert.Same` against the
  originating scope provider.
- **C6 (evidence claims without artifacts)** — fixed: `001-redproof.log` and
  `001-test-design.log` retained in `reviews/`.
- **C9 (handler not null-guarded)** — fixed.
- **C11 (LoggerMessage parameter order)** — fixed for 9002/9003 to match the repo's
  template-order convention.
- **C7 (per-item LINQ re-derivation)** — accepted. ≤3 phases makes the cost nil, and the
  safety argument is documented in the method's comment. Pre-seeding the dictionary is a
  reasonable future hardening but changes `HasPending`'s shape.
- **C8 (single-logical-flow assumption undocumented)** — accepted with a note: the
  assumption matches the pre-existing `FactoryEventCollector` stance. Worth a sentence on
  the interface if PHASE-003 finds a concurrent-raise scenario.
- **C10 (`NeatooLoggerCategories.Server` in Logical mode)** — accepted; the existing
  category set has no better fit and inventing one is out of this plan's scope.
