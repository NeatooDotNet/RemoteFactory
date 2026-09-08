# Trimming Gate Measures Absent and Present Pair

**Plan #:** 003
**Date:** 2026-09-08
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-1
**Status:** Draft
**Last Updated:** 2026-09-08
**Plan-review opt-in:** Yes — the gate is the IP-exposure safety seam and has a recorded history of checks that could not go red; a mis-shaped present control makes it lie in the direction that matters. `plan-reviewer`
**Code-review opt-in:** Yes — user decision 2026-09-08 (orchestrator proposed No: no generator or library change); runs at Step 5 beside the test review
**Branch:** exrm-003-trimming-gate-pair — cut from the arc at Step 2
**PR:** —

---

## Scope

Make the trimmed-client gate measure the `[Execute]` pair instead of inferring half of it: each shape's harness target gains a bare `[Execute]` beside its `[Remote, Execute]`, differing only in the attribute, whose body literal is asserted present in the publish-trimmed client while the `[Remote]` sibling's stays absent; the harness resolves and runs each bare member on the trimmed client with a service from its own container, so a dead feature cannot pass as a clean trim; the gate script gains a present-by-design kind and the new checks are observed failing against a re-guarded variant before they are trusted. The two Design trimming sentences EXRM-002 left alone are rewritten from the measurement. Does not change the generator, the library, `build.yml`, the interface-factory leg, or any published doc, skill, or `CLAUDE-DESIGN.md` text.

---

## Intent

- AC-3 asks for both halves measured. Today the gate asserts only absence for `[Remote, Execute]` on both shapes; that a bare `[Execute]` ships to the client is a reading of the renderers, confirmed untrimmed by EXRM-001/002, and never checked in a trimmed artifact.
- An absence-only gate cannot tell "the guard folded" from "the feature is dead". Pairing each absent marker with a present-by-design sibling makes the gate falsifiable both ways: a guard creeping back onto bare `[Execute]` turns the present half red; a guard lost from `[Remote, Execute]` turns the absent half red.
- Running the bare members on the trimmed client — service resolved from the client container, body executed, marker returned — proves the local path works after trimming, which is the property zTreatment's WASM engines depend on; a surviving name proves less.
- The Design comment that says what makes a body trimmable is rewritten from a measured result rather than a reading, which is the B5 hand-off from EXRM-002's plan review.

---

## Framework & Architectural Alignment

- The trimming harness (`RemoteFactory.TrimmingTests` + `verify-trimmed.sh`) as the verification surface for anything observable only in a publish-trimmed artifact — the Design comments already name it as such; `[trimmed-harness]` is the repo's tier tag for it (TRIM-008/009).
- The gate's own discipline: positive controls before any absence result, both string encodings searched, leg-named failures, markers added rather than widened, and red-before-green — every new assertion observed failing against a known-bad artifact before it is trusted.
- The controlled-pair method from TRIM-009 (`TrimTestEntity`'s sync/async halves): same class, same factory, same registrar, one variable.
- Per-leg ports with substring-safe naming (`LegServerOnlyPorts.cs`), and the harness's contract that every check appends to `failedChecks` and exits non-zero.
- `NeatooRuntime.IsServerRuntime` feature-switch folding; EXRM-001's unguarded local registration for bare static delegates in every mode; the class renderer's `isServerOnly` derived from `internal` or `[Remote]`.
- Design comments as the requirements source of truth (repo CLAUDE.md), DDD vocabulary unexplained.

---

## Constraints & Invariants

- No source change under `src/Generator` or `src/RemoteFactory`. A harness target that needs one has found a bug: stop and report, never shape the target around it.
- Every existing positive control, absence marker, and per-site wrapper discriminator in the gate keeps its meaning and stays green. New markers are added; none is widened or renamed.
- New names obey the harness's substring rule with a distinct stem: no new marker is a substring of another marker, nor of any name expected to survive or to be absent — and no new name is an existing marker with a prefix or suffix, because the gate's `present()` is a substring grep and a `BareExecLegInvoke` would redden the `ExecLegInvoke` absence check for the wrong reason (plan-review B1). The arc's `BareExecute_` test-name convention does not carry into the harness.
- Bare targets take only services registered outside the harness's `IsServerRuntime` guard. Nothing server-only is rooted from the client side of the harness.
- The class-shape bare target is `public static`. `internal` without `[Remote]` is the server-only shape — EXRM-004's visibility pin, not this pair.
- The re-guarded falsification variant is a probe, never a commit: reverted before the plan's implementation commit, both artifacts archived.
- `build.yml` is unchanged: the workflow already runs the script and then the harness, so both new check kinds ride the existing step.
- Interface factories stay out (NF0106; structurally unmeasurable here per TRIM deferred items 19/20).
- Both solutions build and test green. Test counts are expected to stay flat — the harness is not under `dotnet test`; its evidence is the publish, the gate, and the harness exit code.
- Published docs, the skill, `CLAUDE-DESIGN.md`, and the reference app are EXRM-004's; this plan edits two Design *comment* sentences only.

---

## Steps

1. Give the static harness target a bare `[Execute]` beside its `[Remote, Execute]` ones, taking a service registered outside the server-only guard, with its own body literal and substring-safe names.
2. Give the class-level `[Execute]` target the same: a bare `public static` sibling on the same class, so each shape's pair differs only in `[Remote]`.
3. Extend the harness program to resolve and invoke each bare member on the trimmed client and check that the returned value carries the body marker — an executed-body check; the harness's no-op HTTP handler already turns any accidental wire call into a failure.
4. Extend the gate script with a present counterpart to its absence check and a present-by-design kind in its legend; assert the two bare body literals present — only the literals, since a client-side port registered unconditionally is rooted from DI whether or not the body survives and its name cannot go red — and rewrite the closing summary that currently says no shape is asserted present.
5. Publish trimmed locally, run the gate and the harness, and archive both outputs under `reviews/003-evidence/`.
6. Falsify before trusting: republish with the bare targets temporarily carrying `[Remote]`, observe the new present checks red and every absence check still green, archive that run, revert.
7. Rewrite the trimming sentence in the Design static sample's note and confirm the class sample's, so each states the measured pair and names the harness as where it is observed.
8. Build and test both solutions to log files; confirm nothing else moved.

---

## Acceptance

- [x] On a publish-trimmed client, the bare static `[Execute]` body literal is present, and its delegate resolves from the harness container and runs there, returning a result stamped by a client-registered service `[trimmed-harness]` · Must
- [x] On a publish-trimmed client, the bare class-level `[Execute]` body literal is present, and its factory method resolves and runs there, returning a result stamped by a client-registered service `[trimmed-harness]` · Must
- [x] The `[Remote, Execute]` siblings on both shapes stay absent: every existing absence marker, positive control, and wrapper discriminator is unchanged and green `[trimmed-harness]` · Must
- [x] The two body-literal present checks were observed red against a variant in which the bare targets carry `[Remote]`, with every absence check green in the same run; both artifacts' gate and harness output are archived `[explicit-skip: one-off falsification run, evidence in reviews/003-evidence]` · Must
- [x] The gate's legend and closing summary name the present-by-design kind, so a present assertion cannot be read as a tolerated leak `[explicit-skip: gate prose]` · Should
- [x] The Design trimming sentences at both `[Execute]` samples state the measured pair and name the harness `[explicit-skip: comment prose, measured by the bullets above]` · Should
- [x] Both solutions build and test green; the local trimmed publish, the gate, and the harness all exit 0 `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

Walked 2026-09-08 on `exrm-003-trimming-gate-pair` @ `f93f353`. No surprise shifts the plan; no amendment.

- **The bare class-Execute shape is measured, not inferred.** `ClassExecLocalFactory.g.cs` (EXRM-002's target, which carries both) is the authority: the bare `RunLocal` gets **no** delegate type, **no** `…Property` fork in either constructor, **no** `AddScoped<…Delegate>` registration, and **no** `IsServerRuntime` guard — the public method calls `LocalRunLocal` directly (`:46-49`), which is a non-async wrapper into `LocalRunLocalCore` (`:51-60`). Only `[Remote] RunRemote` gets the delegate, the fork (`:36`, `:43`), and the guard (`:105`). So on the class shape the pair differs by the guard and the fork alone, and the bare sibling's state machine is `<Local{X}Core>d__` — no collision with the per-site discriminators, which name `<Local{X}>d__` (plan-review B3 confirmed).
- **The harness's class target already has the `[Remote]` half.** `TrimExecTargetFactory.g.cs`: `LocalRunExecCommand` is the non-async wrapper carrying the guard (`:109-114`), `LocalRunExecCommandCore` the async body resolving `IExecLegPort` (`:116-120`). The registrar is the single-method holder `NeatooClassFactoryRegistrar_TrimExecTarget` (`:143-149`), a gate positive control.
- **Baseline is green before any edit.** `dotnet publish -c Release -r win-x64 --self-contained true` exit 0; `verify-trimmed.sh` against `bin/Release/net9.0/win-x64/publish/` exit 0, all 11 positive controls ok, every absence check ok. Archived as `reviews/003-evidence/baseline-publish.log` and `baseline-gate.txt`. The RID subfolder is the artifact — the TRIM arc's recorded stale-publish trap.
- **Gate anatomy.** `present()` is `grep -aqF` over the raw bytes and a NUL-stripped view (`:51-53`) — a substring match, which is what makes callout B1's prefix hazard real. `check_absent` `:144-151`. Legend `[D]/[R]/[N]` at `:119-125`. Closing summary `:326-337`, whose "No shape is asserted PRESENT as a known leak" (`:330`) is the line this plan must rewrite.
- **Harness DI.** Every server-only port is registered inside `if (NeatooRuntime.IsServerRuntime)` (`Program.cs:28-46`); a bare target therefore needs a service registered outside it. `ValidateOnBuild = true, ValidateScopes = true` (`:55`), so an unguarded registration must be resolvable on the client — a dependency-free client-safe port satisfies that. Checks append to `failedChecks` and never throw out of `Program` (`:50`, `:247-251`).
- **Naming surface.** Existing absent markers that a careless new name could contain: `_DoWork`, `_ProcessRecord`, `_DoAsyncWork`, `StaticAsyncBody_MARKER`, `ClassExecBody_MARKER`, `ClassSyncBody_MARKER`, `ClassAsyncBody_MARKER`, `ExecLegInvoke`, `ExecLegBackend`, `AsyncLegInvoke`, `AsyncLegBackend`, `<LocalRunExecCommand>d__`. New names take distinct stems (`…Tally…`, `Bare…Body_MARKER`, `RunBareCommand`) and contain none of them.
- **Design sentences.** `AllPatterns.cs:370-374` — the `[Remote]` paragraph is already correct after EXRM-002; only its last sentence ("The guard is what makes the body trimmable, not the attribute") is the unmeasured claim. `ClassFactoryWithExecute.cs:134-135` already says the body ships and names the harness — it needs the measured result, not a correction.
- **Baselines** (EXRM-002 close, per TFM): UnitTests 777; IntegrationTests 631; Design.Tests 102. Test counts are expected to stay flat — the harness is not under `dotnet test`.

---

## Punchlist

Step 2 triage (2026-09-08): the five open todo-level rows are all AC-5 docs/version rows in EXRM-004's and EXRM-005's paths; none lies in this plan's path, none pulled down.

- [x] `ClassExecuteLegTarget.cs:15-19` says the class-Execute guard sits "inside the async body"; since TRIM-009 it sits on the non-async `Local{X}` wrapper · the file header, edited anyway at Step 2 · done when the sentence names the wrapper · AC-3 · Must (plan-review B3)

---

## Test Evidence

`[trimmed-harness]` is this repo's tier for behavior observable only in a publish-trimmed artifact (TRIM-008/009 precedent). Its evidence is the publish, `verify-trimmed.sh`, and the harness exit code — never `dotnet test`, which does not run this project. All runs archived in [`reviews/003-evidence/`](../reviews/003-evidence/README.md).

| Acceptance bullet (short) | Priority | Tier declared | Evidence | Tier confirmed |
|---|---|---|---|---|
| 1 — bare static body present, and it runs on the trimmed client | Must | `[trimmed-harness]` | `003-gate.txt`: `check_present "BareStaticBody_MARKER"` ok. `003-harness.txt`: "Bare static [Execute] ran on the trimmed client, through the client-safe port" — `Program.cs` resolves `TrimTestCommands.ComputeTally` and invokes it, matching the caller's token and the port's `\|tallied:` stamp. Falsified in `knownbad-gate.txt` / `knownbad-harness.txt` | ✓ |
| 2 — bare class body present, and it runs on the trimmed client | Must | `[trimmed-harness]` | `003-gate.txt`: `check_present "BareClassBody_MARKER"` ok. `003-harness.txt`: same for `ITrimExecTargetFactory.RunBareCommand`. Falsified in the same known-bad run | ✓ |
| 3 — `[Remote, Execute]` siblings stay absent; nothing incumbent moved | Must | `[trimmed-harness]` | `003-gate.txt` exit 0 with **62** ok — the baseline's 60 (11 positive controls + every absence marker + the 6 per-site discriminators) plus the 2 new present checks. `baseline-gate.txt` is the 60-check comparison point; no incumbent check was edited, widened, or renamed | ✓ |
| 4 — the present checks were observed red against the re-guarded variant | Must | `[explicit-skip: one-off falsification run]` | `knownbad-gate.txt`: exit 1, exactly the two `is MISSING` errors, the other 60 checks ok. `knownbad-harness.txt`: exit 1, both invocations failing with `NotSupportedException` from `NoOpHttpHandler` — the `[Remote]` variant routing to the wire. Method and the first attempt's defect recorded in `003-evidence/README.md` | ✓ |
| 5 — the gate's legend and summary name the present-by-design kind | Should | `[explicit-skip: gate prose]` | `verify-trimmed.sh`: `[P] present-by-design` in the legend; the pair block's header; the summary's new "Present:" paragraph, visible in `003-gate.txt` | ✓ |
| 6 — the Design trimming sentences state the measured pair | Should | `[explicit-skip: comment prose]` | `AllPatterns.cs` (the `[Remote]` note's closing paragraph, replacing "The guard is what makes the body trimmable, not the attribute") and `ClassFactoryWithExecute.cs` (`ScoreLocally`'s TRIMMING note). Both cite the harness; the claim they state is bullets 1–3 | ✓ |
| 7 — both solutions build and test green; publish, gate, harness exit 0 | Must | `[explicit-skip: meta-bullet]` | `reviews/003-build-main.log`, `003-build-design.log` (both "Build succeeded"); `003-test-main.log`: UnitTests 777, IntegrationTests 631 (626 passed, 5 skipped); `003-test-design.log`: Design.Tests 102 — all per TFM (net9.0 + net10.0), 0 failed. Counts flat as predicted. `003-publish.log` exit 0, `003-gate.txt` exit 0, `003-harness.txt` exit 0 | ✓ |

Bullet 3's "unchanged" is a diff claim as well as a run claim: the script's incumbent blocks are untouched, and the two additions (`check_present`, the pair block) sit beside them rather than inside them.

---

## Gate Record

_(Step 5)_

---

## Plan Amendments

_(append-only)_

### 2026-09-08 — Plan-review callouts amended before implementation

- **Section affected:** header `Serves`; Constraints (naming); Step 4; Acceptance bullet 4; Punchlist
- **Original said:** `Serves: AC-3`; the naming constraint stated the substring rule only; Step 4 asserted the body literals "and their client-side service names" present; bullet 4 said "the new present checks were observed red".
- **What changed:** `Serves: AC-3, AC-1` (A2 — bullets 1–2 measure AC-1's property in a trimmed artifact); the constraint forbids prefixing or suffixing an existing marker (B1); Step 4 and bullet 4 name the body literals only (B2 — an unconditionally registered port name is rooted from DI and cannot go red); B3 punched on the plan. A1 dismissed as already inventoried by EXRM-004.
- **Why:** `reviews/003-plan-review.md`; user decisions 2026-09-08.
- **Discovery Log:** 2026-09-08 / EXRM-003

### 2026-09-08 — The falsification run caught the new checks being vacuous

- **Section affected:** Step 3's "carries the body marker"; Acceptance bullets 1 and 2
- **Original said:** the harness checks that the invoked bare member's returned value carries the body marker; the bullets said "returning the marker through a client-registered service".
- **What changed:** the harness matches the caller's own input token plus a stamp the client-safe port adds (`|tallied:`), and mentions no marker literal at all; the bullets say "returning a result stamped by a client-registered service". The first known-bad run **passed the gate** while failing the harness — not a property of the variant but a defect in the check: `Program.cs` is the entry point and is never trimmed, so its `Contains("BareStaticBody_MARKER")` assertions rooted the very literals the gate greps for, and both present checks could never have gone red. With the literals confined to the `[Execute]` bodies, the identical variant produced the required red.
- **Why:** Step 6 exists to catch exactly this before the checks are trusted, and it did. Both runs and the correction are recorded in `reviews/003-evidence/README.md` rather than only the final result, because "the check was fixed" and "the check was always sound" are different claims.
- **Discovery Log:** 2026-09-08 / EXRM-003 (second entry)

---

## Notes

Recon (2026-09-08): `verify-trimmed.sh` defines only `check_absent`; the one body-literal PRESENT control is the "Trimming verification app completed" string, searched by the shared `present()` helper in both encodings. TRIM-008 once asserted markers present as a deliberate tripwire, so the shape has precedent. Every harness port is registered inside `if (NeatooRuntime.IsServerRuntime)` in `Program.cs`, so a bare leg needs a service registered outside that guard or none at all. `ValidateOnBuild = true` in the harness: confirm an unguarded delegate registration does not trip it on a client publish.

Pointer from the EXRM-002 plan review (B5, 2026-09-08): the last sentence of the note at `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:368-371` ("The guard is what makes the body trimmable, not the attribute") is the finding this plan measures. EXRM-002 rewrites only the decorative claim beside it; 003 rewrites the trimming sentence once the pair is measured.

Step 2 recon additions: the harness runs `NeatooFactory.Remote` with a `NoOpHttpHandler` that throws on any send, so invoking a bare member is safe and a mis-routed one fails loudly. The static target's three `[Execute]` methods are all `[Remote]`; the class target `TrimExecTarget` has one `[Remote, Execute]` plus a sync `[Remote, Create]`. The gate's closing summary says "No shape is asserted PRESENT as a known leak" — true today, and the wording must change once a shape is asserted present by design. The TRIM arc's local recipe is `dotnet publish -c Release -r win-x64 --self-contained true` (README), with the recorded trap that the RID subfolder is the artifact — a non-RID publish dir is a stale build. `[trimmed-harness]` is not among the iterative-todo template's five tags; it is this repo's precedent from TRIM-008/009 for bullets only a publish-trimmed artifact can pin, and the test-review brief says so. Hand-off to EXRM-004, found while reading: `docs/trimming.md:35` says static-factory `[Execute]` delegate registrations are guarded — true only of `[Remote]` ones now.

Pre-flight pointers from the plan review (2026-09-08): the bare class sibling's `Local{X}` wrapper is non-async unconditionally (`ClassFactoryRenderer.cs:390-405`), so its state machine is `<Local{X}Core>d__` and no new per-site discriminator is needed; NF0105 fires only for non-static-factory methods (`FactoryModelBuilder.cs:195`), so the Step 6 `[Remote] public static` variant compiles on both shapes; the harness's invocation of a bare member is null-guarded and reports through `failedChecks`, never by throwing out of `Program.cs`; `AllMethodsInternal` on `TrimExecTarget` is already false, so a public bare sibling changes no interface visibility.
