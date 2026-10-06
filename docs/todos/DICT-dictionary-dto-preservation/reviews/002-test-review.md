# DICT-002 test review

**Gate:** Step 5, test-reviewer. Two rounds maximum.

## Round 1 — 2026-10-06 — CONCERNS

- **Veto-tier:** none.
  - All logs are green.
  - The branch only adds tests: two new files, plus additions to `Program.cs` and `verify-trimmed.sh`.
  - No sacred test was weakened.
- **Caller isolation:** confirmed. Each of the four caller tests exercises only its own caller's path.
- **Regex:** captures a nested generic argument whole.
- **Red-first honesty:** confirmed. `unit-red.txt` failures are assertion misses only, with no compile errors or exceptions.

### Triage

| # | Finding | Affects | Priority | Size | Disposition |
|---|---|---|---|---|---|
| 1 | `SystemOnlyDictionary_RegistersNothing` had no positive anchor, so it could pass on an empty tree or a generator crash | AC-1 | Must | punch | **Fixed.** Renamed `SystemOnlyDictionaries_RegisterNothingOfTheirOwn` and anchored by a sibling DTO that must be the only registration. `Run` now fails on a generator exception, and the tree helpers fail when no tree matches, so every negative assertion in the class is anchored |
| 2 | No test for a nullable dictionary value, a recursion path the Scope names | AC-1 | Must | punch | **Fixed.** `NullableDictionaryValue_ValueDtoRegistered`, with `#nullable enable` and `Dictionary<string, ValueDto?>` |
| 3 | The tree helpers return `""` when a hint misses | — | — | punch | **Fixed** in this file, under finding 1 |
| 4 | The shared `DiagnosticTestHelper.RunGenerator` does not fail on generator exceptions | — | — | — | **Dismissed.** It is a shared helper outside DICT's criteria, and DICT's tests now guard themselves |

Theoretical, not triaged: the const-field exclusion is redundant behind the static check.

### Evidence for the fixes

`002-evidence/gate-round1-fixes.txt` records the round-1 runs. The full round-2 suites are green:

| Suite | Result per TFM |
|---|---|
| Unit | 816 passed, 0 failed |
| Integration | 628 passed, 5 pre-existing skips |
| Design | 103 passed |

The trimmed harness and the gate also pass.

### Reviewer read report

- **Beyond the brief:**
  - The signature caller in `FactoryGenerator.Types.cs`.
  - A grep of `DiagnosticTestHelper.cs`.
  - The renderer emission lines.
  - The generator diff.
- **Named but unused:**
  - The evidence README.
  - `unit-green-and-negative-control.txt`.
  - The publish log.
