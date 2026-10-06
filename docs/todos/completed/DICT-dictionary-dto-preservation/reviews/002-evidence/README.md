# DICT-002 evidence

Recorded 2026-10-05, local machine, .NET SDK 10.0.204.

## Red, then green, at two tiers

| Tier | Before the walker change | After the walker change |
|---|---|---|
| Generator unit tests, `DictionaryAndFieldDtoDiscoveryTests` | 15 shape cases red per TFM, all assertion misses; 3 safety cases green — `unit-red.txt` | 19 of 19 green per TFM, including the new depth-cap case — `unit-green-and-negative-control.txt` |
| Publish-trimmed harness, DICT-001's two checks | Both red with a no-constructor `NotSupportedException` — `../001-evidence/` | Both green; harness exit 0; every check passed — `trimmed-run.txt` |
| Trimming gate, `verify-trimmed.sh` | Passed | Passed; new positive control present — `trimmed-gate.txt` |

The before columns ran on commits `effedce` (harness) and `6d5b262` (unit tests), neither of which
touches `src/Generator`. The after columns ran on the same tests with only `DtoTypeWalker.cs`
changed.

## What the generator emits now

`generated-registrar-excerpt.txt` is the carrier's registrar after the change. It registers
`TrimDictValue` (the dictionary value), `TrimFieldHost`, and `TrimFieldCarried` (the public field's
type). Before the change it registered `TrimFieldHost` only — see
`../001-evidence/generated-registrar-excerpt.txt`.

## Depth-cap negative control

Recursion is new in this plan, so the depth-cap test has no pre-change red. Lifting the cap to
`int.MaxValue` and running that test alone crashed the test host with exit code `-1073741571`
(`0xC00000FD`, `STATUS_STACK_OVERFLOW`). The cap was restored to 8 and the full gate build rebuilt
the generator from that source before any further run.

## Full suites, same tree

| Suite | Result |
|---|---|
| `src/Neatoo.RemoteFactory.sln` build | Succeeded; 0 errors; 2 warnings, both a WASM workload notice about a SQLite native file in `OrderEntry.BlazorClient`, unrelated |
| `src/Design/Design.sln` build | Succeeded; 0 warnings, 0 errors |
| `RemoteFactory.UnitTests` | 812 passed, 0 failed, on net9.0 and on net10.0 |
| `RemoteFactory.IntegrationTests` | 628 passed, 5 skipped, 0 failed, on net9.0 and on net10.0 |
| `Design.Tests` | 103 passed, 0 failed, on net9.0 and on net10.0 |

The 5 skips are `RelayTimingTests` and `ShowcasePerformanceTests` cases that carry `Skip` on `main`;
none is new. Full logs are `../001-002-*.log`, gitignored per the TRIM log-evidence ruling.
