# TRIM-007 Test Review (Step 5 Gate) — 2026-07-13

**Reviewer:** test-reviewer agent, two passes (initial + closure).
**Logs:** `007-build.log` (0 errors — the build itself proves the accessibility gate: `RemoteFactory.UnitTests` declares private nested event records and runs the generator as an analyzer), `007-test.log` (final: 595+595 unit, 561+561 integration expected; see note), `007-publish.log`, `007-harness-run.log` (trimmed run, all checks passed, exit 0).
**Gate result: CLEARED** — no open must-cover or should-cover findings.

## Initial pass

Evidence map verified honest with one overstatement (row 4's "no surviving overpromise" — see below). Negative-control credibility confirmed repo-wide: `TrimSubscribeOnlyEvent`/`TrimEventDetail` constructed nowhere, referenced only via the `Subscribe<TEvent>` generic argument and a string-literal `TypeFullName`; the nested record materializes only through deserialization. Runtime interaction implicitly well-covered: the integration assembly declares ~14 event records alongside `[Factory]` targets, so registrar discovery/invocation and TryAdd double-registration idempotency are exercised by all 561 green integration tests. Findings:

1. **should-cover (plan):** deterministic/sorted registrar output had no regression guard (plan-review B4).
2. **should-cover (tech-debt doc):** one surviving inherited-DAM overpromise outside the plan's anchor list — `FactoryEventBaseAttributeTests.cs` class summary.
3. **nice-to-have:** FQN decoy-base negative test; accessibility variants beyond `private`; real parameterless-ctor event round-trip; concrete-inherits-concrete chain; `outputCompilation` error assertions (suite-wide pre-existing pattern).

## Response and closure

| Finding | Disposition |
|---|---|
| Determinism guard | **CLOSED** — `RegistrarOutput_OrdinallySorted_RegardlessOfDeclarationOrder` (Zebra declared before Alpha; ordinal order asserted — a Collect()-order refactor turns it red) |
| Stale DAM overpromise | **CLOSED** — summary corrected; Evidence row 4 now records the gate catch instead of overstating |
| FQN decoy | **CLOSED** — `SameNamedBaseInOtherNamespace_NotMatched` (genuine decoy: real base still referenced in the compilation, proving FQN-specificity) |
| Accessibility variants / parameterless round-trip / concrete-chain / outputCompilation | **ACCEPTED-WITH-REASON** — low-risk per the reviewer's own tiering; the last is suite-wide tech debt ("its own plan if ever") |

## Closing tier picture

- must-cover: none (never open).
- should-cover: both closed.
- nice-to-have: decoy closed; rest accepted with recorded reasons.
- tech-debt: doc item closed; `outputCompilation` pattern visibility-only, backstopped by the solution build.

Final count: 10 `EventPreservationDiscoveryTests`. Evidence-freshness caveat resolved by re-running the full suite after the gate additions (this file's log line reflects the fresh run).
