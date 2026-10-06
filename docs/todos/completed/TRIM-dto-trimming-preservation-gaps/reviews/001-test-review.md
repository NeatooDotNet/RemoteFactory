# TRIM-001 Test Review (Step 5 Gate) — 2026-07-06

**Reviewer:** test-reviewer agent, two passes (initial + closure).
**Logs:** `001-build.log` (0 errors), `001-test.log` (final: 572+572 unit, 563+563 integration, 0 failed, net9.0+net10.0), `001-test-relay-rerun.log`, `001-publish.log`.
**Gate result: CLEARED** — no open must-cover or should-cover findings.

## Initial pass

Evidence map verified honest (all cited methods exist, assert what they claim, at declared tier; renderer coverage static/class/interface real; no vacuous assertions; no sacred tests touched — Program.cs/TrimTestCommands.cs additive only). Findings:

1. **should-cover (quality):** trimmed-harness negative control didn't isolate the return/nested shapes — `_ProcessRecord`'s constructed body could root the ctors (guarded-dead bodies are retained, per TRIM-005), making those checks potentially vacuous.
2. **should-cover (plan-related):** `record struct` bucket assignment untested (plan-review B3 edge).
3. **should-cover (tech-debt):** no incremental-cache regression test exists project-wide (plan-review B1) — nothing guards the `EquatableArray` requirement on transform-output fields.
4. **nice-to-have:** cross-method preserve-bucket dedupe; abstract/nullable record edges.

## Response and closure

| Finding | Disposition |
|---|---|
| Negative-control isolation | **CLOSED** — `_ProcessRecord` now returns `null` (no record constructed anywhere in the harness; discovery is signature-based). Two-stage control: v1 (constructed body, emission off) failed on the *parameter* shape — proving the return shape had been body-rooted, exactly as the reviewer suspected; v2 (null body, emission off) failed on the *return* shape itself (`NotSupportedException` on `TrimRecordResult`, exit 1). Emission restored → all green, exit 0. |
| `record struct` | **CLOSED** — `RecordStruct_LandsInRegisterBucket` pins Register-bucket assignment + Preserve-bucket absence. |
| Incremental-cache tech debt | **CLOSED via queue** — TRIM-006 stub + Index row (not absorbed into this plan). |
| Cross-method dedupe | **CLOSED** — `SameRecordFromTwoMethods_SinglePreserveTypeEmission`. |
| Abstract/nullable record edges | **ACCEPTED-WITH-REASON** — shared `IsDtoStructureCandidate`/`UnwrapType` gates already exercised for class DTOs (`NestedDtoDiscoveryTests` TS-005/TS-010/TS-011); low-risk. |
| Untrimmed `record struct` round-trip | **ACCEPTED-WITH-REASON** — runtime bypass-converter struct behavior is pre-existing and untouched by this plan. |

## Closing tier picture

- must-cover: none (never open).
- should-cover: none open.
- nice-to-have: dedupe added; two declines accepted with recorded reasons.
- tech-debt: queued as TRIM-006.

**Reviewer's closing note:** the Test Evidence map "now survives an independent read with no overreach"; the previously-unproven return/nested trimmed-harness controls are genuine. The gate's marquee catch — false trimmed-harness coverage that the self-authored evidence map could not see — is recorded in the plan's Amendments and the todo Discovery Log.

Also observed at this gate (unrelated to the plan): `RelayTimingTests.Relay_FiresAfterCallerSynchronousWriteOnContinuation` flaked once under parallel load (net9.0, TimeoutException), green in isolation and on both subsequent full runs — logged in the Discovery Log, flagged to the user, not queued in TRIM.
