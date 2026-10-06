# TRIM — Close-Out Audit

**Date:** 2026-08-12
**Mode:** Step 7 close-out (whole arc, findings-only)
**Branch audited:** `TRIM` at `3aed354` (fast-forwarded to `main`, all plan PRs merged)
**Verdict:** **CONCERNS** — 4 veto-tier findings, all since addressed (see Disposition below)

---

## Headline

AC1–AC3 are genuinely verified in a publish-trimmed artifact at HEAD, with CI evidence (GitHub Actions run **31658596913** at `3aed354`, linux-x64 self-contained, `TrimMode=full`, `IsServerRuntime: False` — all four preservation smokes PASSED). This is **not** a repeat of TRIM-003, where a preservation claim was assumed and came back red.

The veto findings concentrated in TRIM-006 — the one plan that received no independent per-plan gate, its gates having been folded into this audit.

---

## Veto-Tier Findings and Disposition

### V1 — TRIM-006's guard never exercised `EventHandlerEntry` — FIXED

The fixture declared two matching static handlers (`Notify`, `Audit`) for the **same** event type on one `[FactoryEventHandler<T>]` class. That is the NF0502 ambiguous-match shape: `FactoryGenerator.RelayHandler.cs:160-174` reports the diagnostic and `continue`s **without adding an entry**.

Consequences, all confirmed independently before accepting the finding:

- `RelayHandlerModel.Entries` was empty, so `EventHandlerEntry.Parameters` / `ServiceParameters` / `AllParameters` — the exact three fields TRIM-006 converted to `EquatableArray<T>` — were never constructed and never guarded.
- The observed `RelayHandler` red→green transition was driven entirely by `RelayHandlerModel.Usings` and `.Diagnostics`. The nested element type was never in play.
- `FactoryGenerator.cs:101` returns early on `Entries.Count == 0`, so `RelayHandlerRenderer.Render` never ran either.
- The fixture's own doc comment asserted the opposite — a false statement in a test, worse than no comment.
- Plan Step 4 ("populate every collection-bearing field on each transform output") was unmet with no Plan Amendment recording the divergence.

**Fix:** fixture now carries two *distinct* event types with one matching handler each, so `Entries` holds two values and each entry's three parameter collections are populated. Comment corrected to state the NF0502 trap explicitly. Two fixture-health tests added: `Fixture_ProducesNoDiagnostics` and `Fixture_EmitsRelayHandlerOutput`.

**Verified, not assumed:** with the corrected fixture, reverting `EventHandlerEntry.Parameters` to `IReadOnlyList<ParameterModel>` turns the `RelayHandler` case **red** — coverage the guard provably did not have before. Restored after.

**The health assertion immediately earned its keep:** it caught a *second* latent fixture defect on its first run — `[Fetch]` on an interface-factory member (NF0106; the interface *is* the boundary, so operation attributes are invalid there). That had been silently degrading the `FactoryInterface` branch too.

### V2 — the "byte-identical output" evidence was vacuous — FIXED

The plan cited "zero `git status` drift in the committed `Generated/` trees," explicitly framed as "verified empirically rather than by inference."

`.gitignore:405` is `**/Generated/` and `git ls-files | grep "Generated/"` returns **zero** tracked files. Those trees are untracked and ignored, so `git status` could not report drift regardless of what the generator emitted. The check proved nothing. (`reviews/005-plan-review.md:83` had already recorded that generated files are not git-tracked.)

**Fix:** replaced with a real measurement. Emitted `RemoteFactory.IntegrationTests`' full generated tree with the pre-fix generator (`710498c^:src/Generator/Model/RelayHandlerModel.cs`) into a clean directory, then with HEAD's generator, and diffed recursively: **256 files, byte-identical, including all 16 `.FactoryEventHandler.g.cs` files**. The conclusion was correct; the original evidence for it was not.

### V3 — TRIM-007's plan file contradicted the Plan Index — FIXED

`plans/007-...md:6` read `Status: In Progress` while `todo.md` showed `007 | Done` and commit `602a6d4` was titled "mark TRIM-007 Done." Header reconciled, with a note that the reconciliation is dated 2026-08-12 while the plan itself completed 2026-07-13.

### V4 — three verified `005-plan-review.md` findings had no disposition — FIXED

B8 and B10 were routed nowhere. Both re-verified rather than inherited:

- **B8** — `grep -rn "AppContext.SetSwitch" src/` returns nothing, and `"Server-only method called in non-server runtime."` appears only in generator renderers, never in a test assertion. **Nothing pins the guard's runtime throw.**
- **B10** — `InternalVisibilityTests.cs:235-236,274-275,280-281,322-323` slices generated text with naive `IndexOf` arithmetic delimited by the next member name; an emission reorder mis-slices and the `DoesNotContain` assertions pass vacuously — the same false-green class TRIM-001's test gate caught as its marquee finding.

**Fix:** both given explicit dispositions in the Deferred Work Carrying Forward table in `todo.md`.

---

## Acceptance Criteria Trace

| Criterion | Evidence | Holds? |
|---|---|---|
| **AC1** — positional record as return / parameter / nested property deserializes publish-trimmed | Bucket walk `DtoTypeWalker.WalkDtoGraph:156-197`; trimmed proof `RecordDtoSmokeTest.cs:Run:25-79` (JSON-literal deserialization, no record constructed anywhere). CI 31658596913: "Record DTO smoke PASSED" | **Yes** |
| **AC2** — DTO reachable only as a `[Factory]` entity property survives trimming | `DtoTypeWalker.WalkEntityProperties:207-214` via `FactoryGenerator.Types.cs:257-272`; trimmed proof `EntityPropertyDtoSmokeTest.cs:Run:21-67`. CI: "Entity property DTO smoke PASSED" | **Yes** |
| **AC3** — subscribe-only `FactoryEventBase` record deserializes publish-trimmed, verified not assumed | Branch 4 `FactoryGenerator.cs:113-130`; `EventPreservationRenderer.cs:27-103` (registrar targets a **generated** type at `:71`); trimmed proof `EventSubscribeOnlySmokeTest.cs:Run:74-146` (string-literal `TypeFullName`, no `typeof`, no construction). CI: "Subscribe-only event smoke PASSED" | **Yes** |
| **AC4** — `docs/trimming.md` updated + release notes | Docs half landed (`docs/trimming.md:254`, `:266-285`, `:287-314`). Release half **open**: version still `1.6.1`, no `v1.7.0.md` | **Open — deliberately held** |
| **AC5** — consumer proof via zTreatment PCB-003 | Not started; blocked on the held release | **Open** |

---

## Build & Test Evidence

- Build: `Build succeeded. 3 Warning(s), 0 Error(s)` — 2× WASM workload warnings in an unrelated example project, 1× pre-existing `CA1062`.
- Tests: **2324 passed, 0 failed, 10 skipped** — UnitTests 601+601, IntegrationTests 561+561 (5 skips × 2 TFMs).
- Design solution: 86 + 86 passed, 0 failed.
- Skipped inventory: `RelayTimingTests.cs:56,:105` (user decision, this arc) + 3 pre-existing "Optional Performance Demo" tests.
- Trimmed-artifact gate at HEAD: CI run 31658596913, all four smokes passed, "Server-only implementation types absent from trimmed assembly." One pre-existing `IL2057` from `ServiceAssemblies.FindType`, untouched by this arc.

---

## Container Integrity

7 plan files, 7 Index rows, no orphans, numbering monotonic with no duplicates. TRIM-005's Abandoned status carries a substantive Abandonment Reason naming the real seam and the successor lesson. Skipped Steps entries exist for all three non-run gates (004, 003, 006). Out of Scope holds — `git diff v1.6.1..HEAD -- src/` contains zero `IFactorySaveMeta` hits and no zTreatment-side work.

**Sacred tests:** only comment-only edits to `FactoryEventHandlerTests.cs` and `FactoryEventBaseAttributeTests.cs`, plus two `[Fact(Skip=...)]` additions carrying user-decision reasons. No assertions removed, no expected values bent, no reflection introduced.

---

## Verified Non-Findings

Recorded because they were explicitly challenged:

- **No other transform-output type carries a reference-equality collection at HEAD.** The full reachable graph of all three transform outputs (`TypeInfo`, `RelayHandlerModel`, `FactoryEventInfo` → `TypeFactoryMethodInfo`, `TypeAuthMethodInfo`, `MethodInfo`, `MethodParameterInfo`, `OrdinalPropertyInfo`, `AspAuthorizeInfo`, `DiagnosticInfo`, `EventHandlerEntry`, `ParameterModel`) uses `EquatableArray<T>` throughout.
- **`ExecuteDelegateModel.ServiceParameters` is outside the cache boundary**, as suspected but unverified when flagged. It is constructed only at `Builder/FactoryModelBuilder.cs:482`, and `FactoryModelBuilder.Build` is called only from the two `RegisterSourceOutput` lambdas — output-stage, not transform-output. No defect.
- **`skills/.../class-factory.md:318,333-334` and `advanced-patterns.md:227` describe *class* factories**, which `ClassFactoryRenderer.cs:54` targets at the generated type and which the 2026-08-11 trimmed probe showed trim correctly. These were **over-listed** as falsified in the 2026-08-11 Discovery Log entry; they are arguably true as written.
