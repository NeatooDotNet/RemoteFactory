# TRIM-009 — Plan Review (opt-in, pre-implementation)

**Gate:** Step 2, opted in (`Plan-review opt-in: Yes`). **Pass:** one (2026-08-14). **Verdict: CONCERNS** — 8 veto-tier, 8 callout-tier.
**Reviewed at:** plan HEAD `da90dca`, before any implementation.

Every checkable finding was independently re-derived at the keyboard before being accepted. **All of them held.**

---

## Why this review earned its keep

The plan was drafted *after* running a measurement specifically designed to avoid this arc's recurring failure — building on a diagnosis that was never falsifiable. **The review found the same failure inside the plan written to prevent it**, twice, plus two inventories declared exhaustive that were not.

That is the fourth occurrence of `[[trim-arc-verify-dont-inherit]]` in this arc, and the first one caught before implementation rather than after.

## Veto-tier findings

| # | Pass | Finding | Verified how | Disposition |
|---|---|---|---|---|
| **B1** | B | **V3's "half held" was a check that could not go red.** The plan read `<LocalFetchAsync>d__` being absent in V3 as "the fold works once the guard is outside the state machine". The V3 knob emits `LocalFetchAsync` as a **non-async wrapper**, so the compiler never creates a state machine by that name — its absence is a compile-time consequence of the rename, trimmed or not | Re-read the V3 emission: `public Task<TrimTestEntity> LocalFetchAsync(...)`, no `async` | **Fixed.** Row struck from the table with the reason recorded; the Approach no longer claims the wrapper half was "measured in isolation" |
| **B2** | B | **An unmeasured value stated inside a measurement table, and an unavailable causal attribution.** `<LocalFetchAsyncCore>d__` was cited from the table, but the probe searched `<LocalFetchAsync>d__`, which cannot match it (`>` falls after `Async`, not `AsyncCore`). Separately: V3 had **both** candidate roots live — DAM *and* the wrapper's own call — so it cannot attribute the core's survival to DAM, and the two imply different remedies | The value *was* really measured, by a separate grep in the same run that never reached the archived file. Re-measured against the still-on-disk V3 artifact: PRESENT | **Fixed.** Evidence addendum records the re-measurement *and* why the row could not go red; the plan now says V3 cannot attribute survival and hands that to Step 1 |
| **B3** | B | **Root inventory said "two roots", called itself empirical, and missed a third** — the local ctor's `{UniqueName}Property = Local{UniqueName};` method-group assignment, reachable via `AddScoped<{X}Factory>()` and its `DynamicallyAccessedMembers(PublicConstructors)` | Read the emitted ctor: `FetchAsyncProperty = LocalFetchAsync;` | **Fixed.** Three roots. Note that it targets the *wrapper*, so the prediction is unaffected — recorded so the finding is not over-corrected |
| **B4** | B | **Emission-site inventory wrong (three, actually five) and its rationale factually inverted.** The plan said the Save/Can\* leg is reached by the write and `Can*` sites. Measured: `Can*` methods are **synchronous**; `LocalSave` is `public virtual async` and is its own site. Class-level `[Execute]` is a fifth | `grep` for guarded sites → 311/745/804/1034/1310; emitted Save factory shows `public Authorized LocalCanCreate` vs `public virtual async ... LocalSave` | **Fixed.** Sites named by method, not line number. Recorded that leaving `LocalSave` unwrapped would likely still pass Step 1 **while shipping a guarded async body** — invisible to the gate |
| **A3** | A | **Class-level `[Execute]` is unconditionally `async`, guarded, resolves `[Service]`s in the generated body — and has no harness target.** It is a Design source-of-truth pattern. AC6 demands "proven in the trimmed harness, not inferred", which is unsatisfiable for this shape today | Read `RenderClassExecuteLocalMethod`: `public async` emitted with no condition | **Fixed.** New Step 2a takes it in scope with a harness target; Acceptance names it |
| **A1** | A | **The plan declared `FactoryAttributes.cs` untouched while falsifying the contract documented there.** Its remarks say the `Type` "must be a GENERATED registrar type. Never a consumer's own class" — but the class leg *already* named a generated type and still leaked, which the plan itself argues. Shipping that unchanged means shipping a false contract in the remarks written to stop this defect recurring | Read `FactoryAttributes.cs:191-212` against the plan's own line 50 | **Fixed.** New Step 3a; XML-doc only, no API surface change |
| **A2** | A | **Step 7's doc scope covered only body-trimming anchors and missed every anchor the *holder* half falsifies** — six of them, all written by TRIM-008. Most direct: `CLAUDE-DESIGN.md:760` and `docs/trimming.md:249` state that for class factories protection comes from the guard, "**not the choice of attribute target**" — precisely what TRIM-009 reverses | `grep` returned both sentences verbatim | **Fixed.** Step 7 split into 7a (body anchors) and 7b (holder anchors) |
| **A3/8** | A | **Step 8 said "update AC6" without saying what the update is** | — | **Fixed.** AC6 closes as written if Step 2a lands; otherwise it is narrowed *in writing* with the shape named and a Deferred Work row — never closed over an unmeasured shape |

## Callout-tier — all fixed or recorded

**B5** — "H2 falsified in both directions" overstated V2: it grafted **2 of 5** constructs, since the three *awaiting* probes cannot be emitted into a sync body. The falsification rests on V1; V2 is partial corroboration. Corrected in the plan and in the evidence.

**B6** — `experiment-knobs.diff` captured only V3's constant values. Added `knob-values-per-variant.txt`.

**B7** — Step 1's stop condition needed a **liveness** check: `method?.Invoke` fails silently, so a holder that does not forward makes *every* marker vanish and V4 read as flawless. Step 1 now requires a named positive control for the holder plus the harness resolving the factory and exiting 0.

**B8** — Exception timing is narrower than feared in one direction and wider in another. Only the guard moves (auth, casts, and DI failures stay in the core, still faulted `Task`s) — but because the ctor binds the delegate property to the wrapper and the public entry point is non-async, the synchronous throw escapes through `I{X}Factory`, not just `Local*`. Risk low (message asserted nowhere), but recorded.

**B9** — Step 4 is an **inversion** of the passing `AssemblyAttributeEmissionTests.cs:42` assertion, not new coverage. Reworded, with original intent stated as preserved.

**B10** — Step 5's flip is larger than eight assertions: four prose blocks in `verify-trimmed.sh` become false too, plus a second stale paragraph in `TrimTestEntity.cs`.

**B11** — The incremental-cache check was self-defeating (`git diff -- src/Generator/` can never be empty when `ClassFactoryRenderer.cs` is the file being changed). Rescoped to `Model/`, `Builder/`, transform.

**B12** — Step 8 gained deferred items 2 and 8, plus the new row 20.

**A4** — The **interface-factory leg** shares both mechanisms and receives neither fix. Deliberately **not** taken into scope — it would balloon a plan whose arc the user has already flagged as over-running, and deferred item 19 makes the leg structurally unmeasurable. Recorded as **deferred row 20**, with the release-blocking condition that the skill's "Interface factory | Yes" claim be qualified before shipping. Deferring the work is fine; shipping a false claim is not.

**A5** — Design-project requirements verification (Step 7B per `CLAUDE.md`) is deferred to the release step via existing item 11; now stated in the plan rather than left to surface at close-out.

## What the reviewer confirmed as sound

- **H1 is correct and V1 is a sound subtractive test.** The knob suppresses all five constructs, `LocalFetchAsync` stays genuinely `async`, and the marker still reads PRESENT.
- **V2 was not vacuous** — `TrimTestEntity.Create` is an instance method, so `isWriteStyleLifecycle` is true and the forced catch arm really fired.
- **Holder indirection transfers to the class leg**, and is *easier* than TRIM-008's: the CS0122 problem that forced forwarding over hosting does not exist here, since the class registrar is already `public static` on a generated type. Prebuilt-consumer compatibility transfers unchanged.
- **The central prediction survives independent check** — the newly-found ctor root targets the wrapper, so it does not threaten "the registrations need no guarding".
- **Scope discipline:** the eight steps are the right size and none is padding. The findings grow the *claims* work — docs, contract, AC6 disposition — not the fix.

## Verdict

**CONCERNS, all veto-tier findings closed in the plan before implementation.** The plan is stronger for having had two of its own claims struck: it no longer asserts that the wrapper half was measured working, and it no longer attributes the core's survival to a mechanism the data cannot isolate. Both questions now belong to Step 1, which is where they were always answerable.
