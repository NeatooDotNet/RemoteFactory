# TRIM-008 — Test Review

**Gate:** mandatory, Step 5. **Passes:** three (2026-08-13).
**Evidence set:** [`008-evidence/`](./008-evidence/) — see the manifest at the bottom.

Findings are recorded with their verification status. Every checkable finding was independently re-derived at the keyboard before being accepted; **all of them held**, across all three passes.

---

## Pass 1 — 4 must-cover, 7 should-cover

| # | Finding | Disposition |
|---|---|---|
| P1 | **Holder method name not pinned.** `Assert.Contains("internal static void FactoryServiceRegistrar(...)")` is satisfied by the *user's* partial class, which emits a byte-identical line — so the assertion passed regardless of the holder's method name. The test's own remarks and the plan's Constraint both claimed it was pinned. This is the one failure mode that is silent (`method?.Invoke`) | **Fixed.** Both tests now `Assert.Matches` an anchor binding the signature to the holder's class declaration. Proven by renaming *only* the holder's emitted method and observing RED, then restoring |
| P2 | **Interface leg measured sync-only.** `IsAsync` is driven by auth methods; the target had no auth, so only the sync branch was exercised | **Fixed, then superseded.** An async variant was added — and the result contradicted the then-current diagnosis. See pass 2 |
| P3 | **`ServerOnlyHelper` could never go red.** Zero references anywhere, so ILLink dropped it unconditionally, while it sat in the gate under a header claiming every marker was measured present-before/absent-after | **Fixed.** Wired into `ServerOnlyRepository.DoServerWork`, so the transitive-removal property its comment claimed is now genuinely tested |
| P4 | **The present-before/absent-after claim was wrong for 8 of 16 markers**, including `RelayLegBackend`, which the probe recorded absent *both* times yet the Test Evidence folded into the fix evidence | **Fixed.** `[D]`/`[R]` labels introduced (later `[N]` added) |
| S5–S11 | Compile test passed on zero generated trees; no private-handler relay test; sync-only async inference; shared markers broke per-leg attribution; both-attributes claim untested; stale counts; red gate runs uncaptured | **All fixed** |

## Pass 2 — 1 must-cover, 6 should-cover

| # | Finding | Disposition |
|---|---|---|
| N1 | **The async interface-factory target could not go red for the property it was cited as measuring.** Its markers sit behind the `GetRequiredService<ITrimIfaceQuery>()` interface hop; verified zero occurrences of any probed marker in the generated factory | **Resolved as structural.** The proposed one-parameter fix (`[Service]` on the interface method) **does not compile** — CS0535, recorded as deferred item 19. So the leg is *structurally* unable to measure body elimination. Now stated at the target, in the gate, and in the inventory rather than left to be inferred from a clean-looking result |
| N2–N7 | Fixture-drift guard on the `Replace`-built private-handler source; async-iface resolution check; `[D]`/`[R]` correction not propagated to three rows; shared async port; evidence-citation nits | **All fixed** |

## Pass 3 — 1 hold-the-gate, 5 should/nice

| # | Finding | Disposition |
|---|---|---|
| Q1 | **"Only `async` differs" is not accurate.** Five constructs are async-only in the generated body: the extra `catch (OperationCanceledException)` **and four interface type-tests** (`IFactoryOnStartAsync`, `IFactoryOnCompleteAsync`, `IFactoryOnCancelled`, `IFactoryOnCancelledAsync`). Type-tests are a *different* ILLink retention mechanism from a state machine, so two hypotheses survive and the data cannot separate them — **including the disproven TRIM-004 story returning in async-only form** | **Fixed.** Conclusion restated as "async-shaped emission"; H1/H2 and their differing remedies handed to TRIM-009 as its first step. Also recorded: DAM roots `LocalFetchAsync` via `typeof(TrimTestEntityFactory)`, so **de-rooting is not an available fix** |
| Q2 | Five Test Evidence drifts, including a direct self-contradiction with the script on `[D]`/`[R]` | **Fixed**, and artifacts are now cited by role against a manifest rather than by filename in eight places |
| Q3 | **The gate ran its assert-PRESENT blocks after positive controls failed**, emitting eight "reopen the diagnosis" instructions on an artifact where the target never existed | **Fixed.** The gate now exits immediately when a control fails |
| T1 | **The controlled pair's service asymmetry is load-bearing and undocumented** — giving the async half `IServerOnlyRepository` would surface that name and turn the static-factory `[D]` markers red for a misleading reason | **Fixed.** Documented as do-not-tidy at the target |
| T2 | Deferred item 19 has no pinning test, unlike items 14/15 | **Recorded, not fixed.** Deliberate: the same treatment items 14/15 got until one was pinned; queued with them |
| T4, T5 | Harness summary line for the async delegate; third cause in the assert-PRESENT message | **T5 fixed; T4 recorded** |

---

## What the gate is judged to have achieved

**The controlled experiment is the strongest evidence this arc has produced** — the first measurement in it that isolates a variable rather than narrating one. Its conclusion holds; only its scope was overstated, and that is corrected.

Both reviewers independently reached the same verdict on the third pass: **the deliverable is done; the residual risk sits in bookkeeping, not in code or tests.** The `[D]`/`[R]`/`[N]` taxonomy and the "untrimmed-present is necessary but not sufficient" reasoning are durable improvements that outlive this plan.

## Standing tech debt, unchanged by this plan

- Deferred item 5 — 16 `IndexOf`-sliced emission assertions that can pass vacuously. Still queued and unowned. Pass 1 noted the pattern reproducing in *new* code (P1), which strengthens the case for giving it its own plan.
- Deferred item 3 — `DiagnosticTestHelper` stale-generator fail-fast. This plan edited that file without closing it. Local-iteration exposure only; CI is cold-build.

## Evidence manifest

Artifacts in [`008-evidence/`](./008-evidence/), all regenerated at the end of pass 3 against HEAD:

| Artifact | Role |
|---|---|
| `build-main.log`, `build-design.log` | Both solutions, 0 errors |
| `test-main-full.log` | 611+611 unit, 561+561 integration, 0 failed, nothing filtered |
| `test-design.log` | 86+86 |
| `publish-trimmed.log` | Trimmed publish |
| `trim-harness.log` | Harness exit 0, six resolution checks |
| `trim-gate.log` | Absence gate, passing run |
| `probe-selfcheck3.txt` | **Untrimmed** control — 53/53 markers PRESENT, proving the probe can see every marker before any absence result is trusted |
| `probe-v5c.txt` | The controlled sync-vs-async experiment |
| `probe-prefix.txt` | Pre-fix baseline |
| `gate-red-nofold.txt` | **The per-leg naming demonstration.** Current code published with the feature switch left ON, so nothing folds: all 6 positive controls pass and the gate names every leg across 30 errors |
| `gate-red-prefix-relayleg.txt`, `gate-red-baseline-prefix.txt` | Archived pre-fix artifacts. These now stop at the positive controls by design — they predate the current targets, so their absence results would be meaningless |
| `gate-red-missing.txt` | Missing-path branch fails loudly |
