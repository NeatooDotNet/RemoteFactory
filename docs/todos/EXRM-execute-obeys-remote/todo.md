# EXRM — `[Execute]` Obeys `[Remote]`

**ID:** EXRM
**Type:** Enhancement (behavior change)
**Status:** In Progress
**Priority:** High (blocks zTreatment UCG-001, which pins the RemoteFactory version carrying this)
**Created:** 2026-09-08
**Last Updated:** 2026-09-08
**Initial split:** 5 plans
**Plan cap:** 8 — max(ceil(5 × 1.5), 5 + 2). Counts plan numbers *issued*, including Abandoned and Retired. Issuing 009 is a stop-and-ask.
**Arc branch:** `EXRM` — per `docs/todos/CONVENTIONS.md` (`{ID}`, as PHASE used); this line wins over the skill's `{id}-arc` default. Every plan and punchlist branch PRs into it; it PRs into `main` at Step 8.
**Target version:** 1.9.0 — user decision 2026-09-08. The CI standards would call this change breaking (major); the user chose a single minor release with no diagnostic ramp because zTreatment is the only consumer and its 43 `[Execute]` sites already carry `[Remote]`. The release notes carry a prominent behavior-change section and the migration rule. Recorded here so the close-out audit reads it as a decision, not a slip.

---

## Goal

`[Execute]` obeys `[Remote]` like every other factory operation. An `[Execute]` without `[Remote]` is local-only: one unguarded local registration on both client and server, no remote delegate, no endpoint, and `[Service]` parameters resolved from whichever container runs the call, so the body ships to the client exactly as a bare `[Create]` does today. `[Remote, Execute]` keeps the v1.8.1 shape unchanged. Both the static-factory and the class-level `[Execute]` paths follow the rule, and the trimmed-client gate measures the pair rather than inferring it. Driving need: zTreatment's UCG work must run its fluency engine and therapy step planner in WASM and cannot express that as an `[Execute]` today. UCG-001 pins the RemoteFactory version that carries this.

## Acceptance Criteria

- [x] **AC-1** · Must — A bare `[Execute]` on a static factory or a class factory runs on the client with client-container service resolution and makes no remote call.
- [x] **AC-2** · Must — `[Remote, Execute]` on both shapes generates and behaves exactly as v1.8.1, including the non-async wrapper guard.
- [x] **AC-3** · Must — The trimmed-client gate measures a `[Remote, Execute]` body absent and a bare `[Execute]` body present, for both shapes.
- [x] **AC-4** · Should — `[AspAuthorize]` on a bare `[Execute]` still forces remote, and `[AuthorizeFactory]` on a bare `[Execute]` runs locally.
- [x] **AC-5** · Must — Design projects, published docs, and the skill state the new contract with the decorative claim gone, and release notes ship per CI standards.
- [x] **AC-6** · Could — The NF0105 static-factory exemption is re-examined and either kept with a stated reason or narrowed.

## Out of Scope

- Relaxing NF0102's `Task<T>` return rule for local Execute — the API stays uniform.
- Interface factories — they stay always-remote under NF0106 (`FactoryModelBuilder.cs` hardcodes it).
- Entity registration for `[Execute]`-only factories — shipped in v1.6.1 (commit `21a4365`); zTreatment's TreatmentContext hack is a delete-at-upgrade row on its side.
- zTreatment's upgrade, its `[Execute]` audit, and UCG-001 itself.

---

## Plan Index

(Stubs carry Scope only; the rest is fleshed out at each plan's Step 2.)

| # | File | Title (≤ 8 words) | Serves | Status | PR |
|---|------|-------|--------|--------|----|
| 001 | [001-static-delegates-obey-remote](./plans/001-static-delegates-obey-remote.md) | Static-factory delegates obey [Remote]; weld removed | AC-1, AC-2 | Done | [#92](https://github.com/NeatooDotNet/RemoteFactory/pull/92) |
| 002 | [002-local-execute-coverage](./plans/002-local-execute-coverage.md) | Local Execute proven on both shapes, plus auth | AC-1, AC-2, AC-4 | Done | [#93](https://github.com/NeatooDotNet/RemoteFactory/pull/93) |
| 003 | [003-trimming-gate-pair](./plans/003-trimming-gate-pair.md) | Trimming gate measures absent and present pair | AC-3, AC-1 | Done | [#95](https://github.com/NeatooDotNet/RemoteFactory/pull/95) |
| 004 | [004-contract-in-design-docs-skill](./plans/004-contract-in-design-docs-skill.md) | New contract in Design, docs, skill, diagnostics | AC-5, AC-6 | Done | [#96](https://github.com/NeatooDotNet/RemoteFactory/pull/96) |
| 005 | [005-release-1-9-0](./plans/005-release-1-9-0.md) | Release notes and version for v1.9.0 | AC-5 | Done | [#99](https://github.com/NeatooDotNet/RemoteFactory/pull/99) |

---

## Punchlist

- [→ EXRM-005] `FileVersion` reads 1.7.0 while `PackageVersion` reads 1.8.1 · `src/Directory.Build.props:18-19` · pulled down at 005's Step 2 triage (Step 6 sweep)
- [→ EXRM-004] NF0102 description justifies the `Task` rule by remoteness · `src/Generator/DiagnosticDescriptors.cs:35` · pulled down at 004's Step 2 triage
- [→ EXRM-004] Bare `[Execute] internal static` on a class factory yields an internal factory interface · `docs/attributes-reference.md` and the Design comments, pinned `[unit]` · pulled down at 004's Step 2 triage (moved from EXRM-002's bullet 7 at its plan review)
- [→ EXRM-004] Reference app's only bare `[Execute]` takes a server-only `[Service]` · `EmployeeManagement.Application/Samples/Attributes/ExecuteSamples.cs:13-20` · pulled down at 004's Step 2 triage
- [→ EXRM-005] CLAUDE.md release step names `<VersionPrefix>`, which does not exist · `CLAUDE.md:242` · pulled down at 005's Step 2 triage (Step 6 sweep)
- [x] mdsnippets placeholder trap: a bare `<!-- snippet: x -->` is read as the start of an existing embed and swallows content to the next `endSnippet` · `CLAUDE.md` "Skill Code Samples" workflow · [#98](https://github.com/NeatooDotNet/RemoteFactory/pull/98) · user-directed 2026-09-08 — serves no AC; a repo-workflow note surfaced by EXRM-004
- [→ EXRM-001] Region header "Execute is always remote" · `ExecuteBehaviorTests.cs:9,27` · pulled down at 001's Step 2 triage
- [→ EXRM-001] Dead Execute clause in the record-primary-constructor ctor · `FactoryGenerator.Types.cs:629` · pulled down at 001's Step 2 triage

## Dismissed

- EXRM · `TargetClassGenerator.cs` (367 lines, referenced by nothing) hardcodes bare `[Execute]` · dead code; serves no criterion — edit `CombinationGenerator.cs`, not it
- EXRM · Rename the five `Comb_Execute_*_Remote` targets · unnecessary once the combination generator emits `[Remote]` for Remote mode
- EXRM · Stray `remotefactory-execute-on-class-factory.md` at the repo root (March, commit 3768f4f) duplicates a completed plan · unpublished, serves no criterion; user may pull it into the sweep for deletion
- EXRM-001 · Registry unit tests leave probe delegate types in the process-global `LocalOnlyDelegateRegistry` · harmless; no test asserts registry contents (test-review tech-debt 2)
- EXRM-001 · `[AspAuthorize]` on a static-factory Execute is collected but never enforced · pre-existing, undocumented; serves no criterion — captured as [#91](https://github.com/NeatooDotNet/RemoteFactory/issues/91); 001 folds its presence into the remote flag only
- EXRM-002 · Design's static bare-`[Execute]` sample must be `private static _Name`, not the combination targets' `public static` · the Design convention (`AllPatterns.cs:380-388`) already binds the sample; noted for pre-flight (plan-review B6)
- EXRM-002 · Intermittent `MSB3552: Resource file "**/*.resx" cannot be found` on a multi-target build · pre-existing race between the `PreBuild` `RemoveDir` and the parallel inner build's glob enumeration, in six projects; serves no criterion — captured as [#94](https://github.com/NeatooDotNet/RemoteFactory/issues/94)
- EXRM-003 · `FactoryAttributes.cs:102-108` XML doc still calls `[Remote]` decorative on `[Execute]` · already in EXRM-004's Scope ("the attribute XML docs") and its Notes inventory; not a second row (plan-review A1)
- EXRM-004 · `docs/plans/combination-testing-generator.md:168` says "(always remote)" · historical archive of a completed plan, excluded from mdsnippets; archives are not edited
- EXRM-004 · `docs/attributes-reference.md:416` says derived methods inherit remote execution · a `[Remote]`-inheritance claim, not the `[Execute]` contract; the generator reads declared attributes only, so the row is inert (plan-review M1)
- EXRM-004 · Generated factories name `Task`, `CancellationToken`, `IServiceProvider` and `InvalidOperationException` unqualified and rely on the consumer's `ImplicitUsings` (surfaced by the gate's crash-proofing assertion) · pre-existing, every consumer in the repo enables implicit usings, fixture convention already documented in `AssemblyAttributeEmissionTests`; serves no criterion — captured as [#97](https://github.com/NeatooDotNet/RemoteFactory/issues/97)

---

## Discovery Log

(Append-only. ≤ 60 words per entry. Only Amend / Queue / Abandon / Re-split / Reprioritize.)

### 2026-09-08 — EXRM-001 · serves AC-1
- **Finding:** The wire refusal as drafted was an allow-list scoped to the static registrar, which would refuse every class- and interface-factory remote call (plan-review veto V1).
- **Decision:** Amend — refuse-list of bare static delegates; callouts B2–B4 and a tier correction amended in the same entry; B1 punched to EXRM-004's path.
- **Follow-up:** n/a

### 2026-09-08 — EXRM-002 · serves AC-1
- **Finding:** Plan review APPROVED. Callouts: bare-target test names would collide with the existing `_Local_` tests; bullet 7 served no 002 criterion; Step 6 rewrote a trimming sentence 003 has not measured; bullet 4 misnamed null-on-denial.
- **Decision:** Amend — `BareExecute_` prefix; bullet 7 → EXRM-004 with Punchlist row 3; decorative claim only; "returns null". B6 dismissed.
- **Follow-up:** n/a

### 2026-09-08 — EXRM-002 · serves AC-4
- **Finding:** Step 4's targets exposed a generator bug: a class-level `[Execute]` under `[AuthorizeFactory]`/`[AspAuthorize]` declares a non-nullable factory method but returns `Authorized<T>.Result` — CS8603 in generated code, fatal under TreatWarningsAsErrors. Bare and `[Remote]` alike; never compiled in-repo before.
- **Decision:** Amend — the plan's no-generator-change constraint gains this one exception; `BuildClassExecuteMethod` mirrors the Read path; new Should bullet pins both shapes; 005 notes the fix.
- **Follow-up:** n/a

### 2026-09-08 — EXRM-003 · serves AC-3
- **Finding:** Plan review APPROVED. Callouts: a present check on an unconditionally registered port name can never go red; a `Bare` prefix on port names keeps absent markers as substrings; a stale guard sentence in the class-Execute target; `Serves` omitted AC-1.
- **Decision:** Amend — gate asserts body literals only; distinct-stem naming; `Serves: AC-3, AC-1`; B3 punched on the plan. A1 dismissed (already in 004's inventory).
- **Follow-up:** n/a

### 2026-09-08 — EXRM-003 · serves AC-3
- **Finding:** The red-before-green run exposed the new present checks as vacuous: `Program.cs` is never trimmed, so its `Contains("…_MARKER")` assertions rooted the literals the gate greps for, and the known-bad variant passed the gate while failing the harness.
- **Decision:** Amend — marker literals live only in the `[Execute]` bodies; the harness matches the caller's token plus the client-safe port's stamp. Identical variant then went red as required.
- **Follow-up:** n/a

### 2026-09-08 — EXRM-003 · serves AC-3
- **Finding:** Code review CLEAN with two callouts: the B3 correction left the same stale claim on the `[Remote]` sibling's own summary; and "differs only in `[Remote]`" overclaimed — the class pair also varied in async-ness, TRIM-009's failure axis.
- **Decision:** Amend — stale sentence punched; the bare class half made async so both halves await a port call; the forced `[Service]` difference stated and shown neutralised by the known-bad run. Full evidence re-run.
- **Follow-up:** n/a — the bare async *static* gap was closed in EXRM-003 itself at the user's direction (its third amendment); no Punchlist row.

### 2026-09-08 — EXRM-004 · serves AC-5
- **Finding:** Plan review CONCERNS, five Must callouts: an inherited-`[Remote]`-over-`[Execute]` sentence that cannot occur; bare `internal static [Execute]` on a class factory is guarded, contradicting the rule sentence; the skill self-containment bullet already red; a `skill-*` region cannot be shared; auth enforcement is class-shape only.
- **Decision:** Amend — all ten callouts amended; the internal case gets its own row and a Design sample; M1's inheritance row dismissed.
- **Follow-up:** n/a

### 2026-09-08 — EXRM-005 · serves AC-5
- **Finding:** Plan review CONCERNS, four Must callouts: the page cites only CLAUDE.md's CI/CD standard and never v1.0.0's published API Stability Commitment; the "runs where it is called" headline is false for class-level `internal static` and omits that authorization still forces the server; and "no signature changed" is false — the auth'd class `[Execute]` now generates `Task<T?>`.
- **Decision:** Amend — all nine callouts amended; the header line becomes "Yes — behaviour, and one generated signature"; the blockquote carves out v1.0.0; a blob URL replaces a docs-site URL that would 404 (Pages is disabled).
- **Follow-up:** n/a

---

## Skipped Steps

-

---

## Sibling Todos

-

---

## Close-Out Audit

### YYYY-MM-DD — Grade: [A | B | C]

**Veto-tier findings:**
**Callouts:**
**Accepted gaps (B only):**
**User acknowledgment:**
**Full audit:** [`reviews/close-out-audit.md`](./reviews/close-out-audit.md)

---

## Follow-on

-

---

## Docs & Retro

**Documentation:**

**Retro (one paragraph):**

---

## Results / Conclusions

```
Plans: {issued} issued of 8 cap — {done} Done, {abandoned} Abandoned, {retired} Retired.
Punchlist: {closed} closed. Dismissed: {n}. Follow-on: {n}.
Close-Out Audit: Grade {A|B} (acknowledged YYYY-MM-DD).
Arc: EXRM → main, PR #{n}.
```
