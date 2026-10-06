# TRIM-009 — Test Review

**Gate:** mandatory, Step 5. **Pass:** one (2026-08-14).
**Evidence set:** [`009-evidence/`](./009-evidence/).

Every checkable finding was independently re-derived at the keyboard before being accepted. **All of them held**, and re-deriving one of them turned up a defect the review had not seen.

---

## What the reviewer confirmed as sound

- **The 8 flipped gate assertions lost nothing.** Verified by set-diff against `2e50546:verify-trimmed.sh`: every pre-TRIM-009 marker is still asserted, with five new Exec markers and three new named controls on top. **TRIM-008's marker-drop regression did not recur** — that was the specific failure this check existed to catch.
- **The class-`[Execute]` leg is genuinely measurable**, not another structurally-blind leg like the interface factory: its wrapper is rooted by an unguarded delegate registration and a ctor method-group assignment, and the marker-bearing body is reached by a **direct static call**, not an interface hop.
- **The negative-lookahead regex is sound**, not backtracking-vacuous — the inner `\s*` absorbs exactly what the outer gives back, so no backtrack position satisfies the lookahead while the guard is present.
- **Sacred tests:** the one inversion preserves intent and *strengthens* it (a new `DoesNotContain` on the old target). `TrimTestEntity.cs` changes are comment-only. No other pre-existing test file modified.

## must-cover findings

| # | Finding | Disposition |
|---|---|---|
| M1 | **The durable gate could not detect a per-site wrapper regression.** Every marker was a *body* signal, and body signals cannot distinguish "this method was wrapped" from "an ancestor's fold removed the only reference to it". `LocalSaveCore` routes to the Insert/Update/Delete **wrappers**, so unwrapping `RenderSaveLocalMethod` would still clear every marker while shipping a guarded async body. **The plan documented this blind spot and then shipped one** | **Fixed.** New per-site block asserting `<LocalFetchAsync>d__`, `<LocalInsert>d__`, `<LocalUpdate>d__`, `<LocalDelete>d__`, `<LocalSave>d__`, `<LocalRunExecCommand>d__` absent. A wrapped site has no `<LocalX>d__` at all; unwrap it and the name returns *and* its body survives. Four have archived pre-fix PRESENT baselines, so they are real `[D]` discriminators |
| M2 | **Class-`[Execute]` — the shape rescued so AC6 could close "proven, not inferred" — had no untrimmed self-check.** Its five markers, including a UTF-16 literal (the marker class that read false-absent during TRIM-008's probe bug), had never been shown capable of reading PRESENT. The single measurement was one post-fix absence | **Fixed, and the fix found more than the finding claimed.** See below |

### M2's re-derivation turned up a second defect

Re-running the self-check reported **all five Exec markers absent from the untrimmed build** — which would have meant the gate's Exec absence checks were outright vacuous. They were not: `dotnet publish -r win-x64` writes to `bin/Release/net9.0/win-x64/`, so `bin/Release/net9.0/` still held a build predating `ClassExecuteLegTarget.cs` (81,920 bytes vs 89,600 after rebuild). **A stale artifact, indistinguishable at a glance from a vacuous gate.**

Rebuilt and re-probed: all five PRESENT untrimmed, `ClassExecBody_MARKER` among them. The gate's Exec checks are non-vacuous. Both the result and the stale-artifact trap are recorded in `probe-selfcheck-final-all-legs.txt`, because next time the first reading will look the same.

## should-cover findings

| # | Finding | Disposition |
|---|---|---|
| S1 | The async guarded `Can*` site — one of the five changed — has neither an emission assertion nor a trimmed measurement (every harness `Can*` is synchronous) | **Attempted, removed, reason recorded.** An `[AuthorizeFactory<T>]` returning `Task<bool>` produces a Can that is async but **not server-only**, so no guard and no split — the test would have passed for the wrong reason. The guarded async `Can*` shape needs `[AspAuthorize]` policy auth, whose references `DiagnosticTestHelper.BuildReferences()` lacks. Site is exercised by `Design.Domain.Aggregates.SecureOrder` and `RemoteFactory.AspNetCore.TestLibrary` (both emit `LocalCan*Core`, both pass). Recorded at the test file rather than left silent |
| S2 | **Three of four assertions in the async-split test were never observed red** — xUnit aborts at first failure, so the forwarding, core-signature, and no-guard assertions never executed in the broken state. Same for the `DoesNotContain` regression assertion | **Recorded, not fixed.** Stated as a limit in the plan's Test Evidence rather than left as an implied per-assertion red proof |
| S3 | No compile assertion on class-factory generated output, while the static and relay legs both have one | **Fixed.** `Assert.Empty(GetDiagnostics().Where(Error))` added to the holder test — which immediately failed on missing `using System;` / `System.Threading` in the fixture, so the assertion earned its place on the first run |
| S4 | The behaviour change has zero coverage, and deferred item 4's stated trigger ("if the guard's message or shape is ever edited") has now fired | **Recorded.** No sacred test broke, because nothing anywhere asserts the guard's message. The uncovered surface is the exception *contract*; the trimmed gate covers guard *deletion* |
| S5 | Test Evidence overstatements — Exec markers, "every marker", a mis-cited harness log, and a size baseline | **Fixed.** See below |

### Test Evidence corrections

- **"every marker PRESENT untrimmed"** cited an artifact covering 12 markers, none from the Exec leg. Now cites `probe-selfcheck-final-all-legs.txt`, which covers all legs.
- **`harness-v4-liveness.log`** was cited for `Class [Execute] factory resolved: True`; that line exists only in `harness-final.log`. Citation corrected.
- **"52,224 vs 66,560 bytes"** — 66,560 is **V2's knob variant**, not HEAD. No artifact records HEAD's pre-fix trimmed size, so the comparison is **withdrawn** rather than restated from memory.
- **"exactly three tests went red"** came from a run filtered to one test class (16 tests). Now stated as such, not as a blast-radius claim.
- Two Acceptance bullets had no rows; the full-suite row was added, and the "async `Can*`" and doc-anchor gaps are stated explicitly.

## Pre-existing tech debt, unchanged by this plan

- **Deferred item 5** — the `IndexOf`-sliced assertions in `CanMethodVisibilityTests` got *wider*, because the wrapper interposes a private core inside the slice. Still queued and unowned; this is a fresh reason to schedule it.
- **The `IsServerRuntime == false` path is untestable in-process** — no fixture sets the AppContext switch, so the guard's negative branch has never executed in any test. Root cause of item 4 and of S4.
- **`verify-trimmed.sh` names `SaveLegBackend` in a comment listing markers "below" that is asserted nowhere.** Confirmed pre-existing (absent from `2e50546` too).
