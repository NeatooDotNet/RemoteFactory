# TRIM-002 Test Review (Step 5 Gate) — 2026-07-06

**Reviewer:** test-reviewer agent, two passes (initial + closure).
**Logs:** `002-build.log` (0 errors), `002-test.log` (full parallel run: units 581+581 green; 2 pre-existing relay-family flakes in integration), `002-test-integration-seq.log` (integration 561+561 green both TFMs, `xUnit.MaxParallelThreads=1` — the authoritative integration signal), `002-publish.log`, `002-harness-run.log` (trimmed run: all checks passed, exit 0).
**Gate result: CLEARED** — no open must-cover or should-cover findings.

## Initial pass

Evidence map verified honest (all 9 cited methods exist, load-bearing assertions at declared tier; both zTreatment failure shapes directly mirrored; per-tree child-vs-parent assertion non-vacuous; harness types confirmed never constructed repo-wide). Findings:

1. **should-cover (plan):** no test for a DTO carried on the entity's *base class* (entity-root base-chain application).
2. **should-cover (plan):** no test for a `[Factory] record` aggregate's self-walk.
3. **nice-to-have:** `System.*` exclusion half of the hardening unpinned; cross-walk dedupe untested; `FactoryTree` substring-match fragility.
4. **tech-debt (visibility):** the FactoryEventRelay integration family is parallel-load flaky *beyond* the two skipped `RelayTimingTests` members — different members flake per run, all pass isolated/sequential.
5. **process:** the trimmed-harness *run* output wasn't in the provided logs.
6. **sacred tests:** both `RelayTimingTests` skips verified faithful (bodies/assertions intact, reasons recorded) — with the honest note that the relay post-return ordering contract now has no active guard in the default parallel run.

## Response and closure

| Finding | Disposition |
|---|---|
| Base-class property | **CLOSED** — `EntityBaseClassDtoProperty_Discovered` (abstract base traversed) |
| Record entity self-walk | **CLOSED** — `RecordEntitySelfWalk_CarriedDtoDiscovered` (residual: primary-ctor-record permutation accepted as nice-to-have; walk gate is independent of the create-path) |
| `System.*` exclusion / dedupe / helper anchoring | **CLOSED** — `SystemFrameworkTypeProperty_StillExcluded`, `DtoInBothSignatureAndEntityProperty_SingleEmission`, `FactoryTree` anchored to `.{hint}.g.cs` |
| Harness run evidence | **CLOSED** — `002-harness-run.log` captured |
| Relay-family parallel flakiness | **ACCEPTED-WITH-REASON** — user declined queueing (Discovery Log records the family-wide pattern) |
| Relay skips coverage-loss | **ACCEPTED-WITH-REASON** — standing user decision; on record |

## Closing tier picture

- must-cover: none (never open).
- should-cover: both closed.
- nice-to-have: all closed.
- tech-debt: one accepted-with-reason (user decision).

13 unit tests total; negative control (entity walk disabled → carried-DTO ctor stripped → harness exit 1) verified at the keyboard per the TRIM-001 precedent.
