# TRIM-004 — Trimming harness pass/fail semantics + CI gate

**Plan #:** 004
**Date:** 2026-07-06
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done
**Last Updated:** 2026-07-06 (closed: PR #68 merged, CI trimming step green on linux-x64)
**Plan-review opt-in:** No (test-infrastructure/CI wiring only; no public API, schema, or documented business-rule surface)
**Code-review opt-in:** No (no library behavior change; harness and workflow only)

---

## Scope

Give the publish-trimmed harness enforceable pass/fail semantics and make CI run it. Today `RemoteFactory.TrimmingTests` sits outside `Neatoo.RemoteFactory.sln`, is never published or executed by `.github/workflows/build.yml`, and its `Program.cs` / smoke tests print `FAILED` lines to the console but always exit 0 — so the trimmed-repro acceptance signals that TRIM-001/002/003 depend on would verify nothing in CI. Convert failure paths to a non-zero process exit (aggregated across all checks so one failure doesn't mask others), add a CI step that publishes the trimmed exe (Release; RID matching the ubuntu runner) and runs it, automate the README's manual binary-inspection check, and settle solution membership at the keyboard. Does NOT add new trimming test cases — those land with TRIM-001/002/003.

---

## Intent

- Every remaining TRIM plan's acceptance signal has the shape "X survives publish-trimming and deserializes on the client." This plan makes that class of signal *enforceable*: a trimmed-repro regression turns CI red instead of printing to a console nobody runs.
- After this lands, TRIM-001/002/003 add their repro cases into an already-gated harness — their acceptance bullets become CI-checkable facts, and the gate keeps protecting consumers (zTreatment PCB-003) after this todo closes.

---

## Framework & Architectural Alignment

- CI/CD standards (user-level CLAUDE.md): single `build.yml` workflow, build-job structure, no new workflow files.
- TrimmingTests project conventions per its README: standalone single-TFM net9.0 console exe (clearing `TargetFrameworks` is what makes ILLink actually run), feature-switch client configuration (`IsServerRuntime=false`, `Trim="true"`).
- The harness stays a plain console exe, not an xUnit project — trimmed-publish executables and test SDKs don't mix; the exe's exit code is the test result.

---

## Constraints & Invariants

- Existing per-check console diagnostics remain (they are the failure forensics when CI goes red).
- All five existing checks keep passing in the trimmed run: service-provider build with `ValidateOnBuild`, class-factory resolution, static-delegate resolution, event-delegate resolution, feature-switch fold, event-relay smoke.
- `dotnet build` / `dotnet test` of `Neatoo.RemoteFactory.sln` stay green on net9.0 + net10.0 — the harness's single-TFM/`PublishTrimmed` setup must not leak into the multi-target solution build.
- The CI publish must actually trim (self-contained, RID-specific) — a non-trimmed run would pass vacuously.
- No changes to `src/RemoteFactory` or `src/Generator` in this plan.

---

## Steps

1. Convert the harness's reporting to aggregated exit semantics: every check contributes to a single failure flag, the process exits non-zero if any check failed, and no early `return` masks later checks.
2. Add a CI step to the build job that publishes the trimmed harness (Release, runner RID, self-contained) and runs it, failing the job on non-zero exit.
3. Automate the README's binary-inspection check in the same CI step: assert the server-only marker strings are absent from the published assembly.
4. Settle solution membership — in-sln (so plain builds surface compile breaks in harness code) vs. standalone (publish step restores it independently) — at the keyboard, based on how the multi-target solution build tolerates the project; record the decision here.
5. Update the TrimmingTests README to describe the gate and the one-command local run.

---

## Acceptance

- [x] A deliberately-injected smoke-check failure makes the published trimmed exe exit non-zero; the all-green run exits 0. `[explicit-skip: harness-gate semantics — verified by one-off failure injection at the keyboard; the harness itself is the test]`
- [x] CI publishes and runs the trimmed harness on every push/PR build, and the job fails when the harness fails. `[explicit-skip: CI wiring — verified by this plan's own workflow run]` *(verified: PR #68 run 28817388916 — trimming step published linux-x64, marker grep passed, harness "All checks passed")*
- [x] Server-only marker absence in the published assembly is asserted by CI, not just documented in the README. `[explicit-skip: binary-inspection gate — workflow grep step]` *(grep logic verified locally against the win-x64 publish; CI asserts the linux-x64 artifact)*
- [x] `dotnet build` and `dotnet test` of `Neatoo.RemoteFactory.sln` remain green (net9.0 + net10.0). `[explicit-skip: build gate]`

---

## Current State (Pre-Flight)

Walked 2026-07-06 on the `TRIM` branch (recon commit ba3a744):

- `src/Tests/RemoteFactory.TrimmingTests/Program.cs` — top-level statements; service-provider failure paths print and `return;` with implicit exit 0 (lines 39, 45); factory/delegate checks print `resolved: True/False` booleans without affecting exit; `DirectFeatureSwitchTest.Run()` and `EventRelaySmokeTest.Run()` are `void` and print `PASSED`/`FAILED` lines only.
- `EventRelaySmokeTest.Run` early-returns on each failure branch (lines 59, 79, 84, 92, 98, 104) — six distinct FAILED messages, none propagated.
- `RemoteFactory.TrimmingTests.csproj` — single-TFM net9.0 (deliberately clears `TargetFrameworks`; README documents ILLink silently not running otherwise), `PublishTrimmed=true`, `TrimMode=full`, no `RuntimeIdentifier` pinned (local `bin/Release/net9.0/win-x64/publish` artifacts came from a manual `-r win-x64` publish), feature switch `Neatoo.RemoteFactory.IsServerRuntime=false Trim=true` at line 30.
- Project is absent from `src/Neatoo.RemoteFactory.sln`; `.github/workflows/build.yml` restores/builds/tests only the solution, on `ubuntu-latest` (CI RID will be `linux-x64`), .NET 9 + 10 SDKs installed.
- Binary markers for the trimmed-away assertion: `"ServerOnlyDirect_MARKER"` (`DirectFeatureSwitchTest.cs:34`) and the `ServerOnly*` type names (`ServerOnlyTypes.cs`); README's manual check greps `"ServerOnly"` in the published dll.
- README documents the manual workflow (publish win-x64, grep, ilspycmd, run exe) and the `TargetFrameworks` gotcha — keep both, add the gate.

---

## Test Evidence

Filled after implementation, before the Step 5 gate. All four Acceptance bullets are `explicit-skip` (gate/infrastructure signals — the harness itself is the test), so the expectation is a `test-reviewer` skip recorded in Skipped Steps rather than an evidence map with cited xUnit methods.

| Acceptance bullet (short) | Tier declared | Test method / evidence | Tier confirmed |
|---|---|---|---|
| Injected failure → non-zero exit; all-green → 0 | `[explicit-skip]` | Keyboard verification 2026-07-06: injected `failedChecks.Add(...)` → `dotnet run` exit 1; removed → exit 0; trimmed publish all-green → exit 0 | ✓ |
| CI publishes and runs the trimmed harness | `[explicit-skip]` | `build.yml` "Trimming verification" step; verified by PR #68 workflow run 28817388916 (linux-x64, all checks passed) | ✓ |
| Marker absence asserted by CI | `[explicit-skip]` | `build.yml` grep step; logic verified locally against win-x64 publish (implementations absent, interface retention → TRIM-005) | ✓ |
| Solution build/test green | `[explicit-skip]` | `reviews/004-build.log` (0 errors), `reviews/004-test.log` (2254 passed, 0 failed, net9.0+net10.0) | ✓ |

---

## Plan Amendments

### 2026-07-06 — Harness didn't compile at HEAD: dead `[Event]` API usage removed

- **Section affected:** Constraints ("all five existing checks keep passing") / Steps 1
- **Original said:** the harness compiles and its five checks pass; this plan only re-plumbs reporting.
- **What changed:** `TrimTestCommands._OnWorkCompleted` used the `[Remote, Event]` method attribute deleted in v1.5.0 (`eec581c`) — the harness has not compiled since, and the on-disk publish artifacts were stale pre-v1.5 builds. Removed the dead method and the obsolete `OnWorkCompletedEvent` delegate check; event coverage at HEAD is `EventRelaySmokeTest` (current relay API). Four checks remain: service-provider build, class-factory resolution, static-delegate resolution, feature-switch fold, event-relay smoke.
- **Why:** the project being outside the solution meant the v1.5.0 breaking change never touched it — exactly the failure mode this plan exists to close.
- **Discovery Log link:** 2026-07-06 — TRIM-004 (harness rot).

### 2026-07-06 — Class-factory check had never actually passed; two harness bugs fixed

- **Section affected:** Constraints / Current State
- **Original said:** existing checks pass; the old `Class factory resolved: False` output was masked only by the exit-0 bug.
- **What changed:** with exit codes enforced, the class-factory check surfaced two real harness defects: (1) factories are registered scoped and the check resolved from the root provider (`ValidateScopes=true` throws) — now resolves inside a scope; (2) Remote mode requires the consumer-registered keyed `HttpClient` (`RemoteFactoryServices.HttpClientKey`), which the harness never registered — now registered with a `NoOpHttpHandler` (avoids constructing `SocketsHttpHandler`, whose `System.Net.Security` dependency is trimmed out of the full-trim publish). `Class factory resolved: True` now holds on the trimmed run for the first time.
- **Why:** the check was authored against an older registration shape and silently rotted behind exit-0.
- **Discovery Log link:** 2026-07-06 — TRIM-004 (harness rot).

### 2026-07-06 — CI marker grep narrowed to implementation types; interface-name retention deferred to TRIM-005

- **Section affected:** Step 3 / Acceptance bullet 3
- **Original said:** CI asserts the README's `grep "ServerOnly"` returns no matches.
- **What changed:** at HEAD the `IServerOnlyRepository` TypeDef name and its `DoServerWork` member survive the trimmed publish — generated `LocalCreate` bodies are rooted client-side by delegate registration, and their early-`throw` feature-switch guard plus `try/catch` region defeats ILLink's unreachable-code elimination, so dead body references are retained. Implementations (`ServerOnlyRepository`, `ServerOnlyDirect`, marker string) are correctly trimmed. The CI grep now asserts implementation-type absence (`ServerOnlyDirect`, `(?<!I)ServerOnlyRepository`); the over-retention itself is queued as TRIM-005 rather than absorbed here.
- **Why:** over-retention is a different defect class (bundle size / docs accuracy) from this plan's gate deliverable; the docs' "should return no matches" claim needs either a generator guard-shape fix or a docs correction — its own plan.
- **Discovery Log link:** 2026-07-06 — TRIM-004 (over-retention, Defer → TRIM-005).

---

## Notes

- The harness cannot self-report "type was trimmed away" for the marker check — string absence is only observable from outside the process, hence the CI grep step (Step 3).
- TRIM-001/002/003 will each add repro cases to this harness; keep the check-aggregation shape easy to extend (one flag, many named checks).
- **Step 4 decision (2026-07-06):** added to `Neatoo.RemoteFactory.sln` under the `Tests` solution folder. Rationale: the `[Event]` rot proved out-of-sln projects go stale invisibly; sln membership makes plain builds catch compile breaks, while trimming enforcement stays in the dedicated CI publish+run step. `dotnet test` skips it (no test SDK); the single-TFM/`PublishTrimmed` settings only activate at publish, so the multi-target solution build is unaffected (verified by the 004 build log).
