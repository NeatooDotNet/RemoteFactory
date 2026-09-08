# EXRM — `[Execute]` Obeys `[Remote]`

**ID:** EXRM
**Type:** Enhancement (behavior change)
**Status:** Complete
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

- [x] `FileVersion` and `PackageVersion` had drifted apart (1.7.0 / 1.8.1) · `src/Directory.Build.props:18-19` · both read 1.9.0 · [#99](https://github.com/NeatooDotNet/RemoteFactory/pull/99) · AC-5 · Must
- [→ EXRM-004] NF0102 description justifies the `Task` rule by remoteness · `src/Generator/DiagnosticDescriptors.cs:35` · pulled down at 004's Step 2 triage
- [→ EXRM-004] Bare `[Execute] internal static` on a class factory yields an internal factory interface · `docs/attributes-reference.md` and the Design comments, pinned `[unit]` · pulled down at 004's Step 2 triage (moved from EXRM-002's bullet 7 at its plan review)
- [→ EXRM-004] Reference app's only bare `[Execute]` takes a server-only `[Service]` · `EmployeeManagement.Application/Samples/Attributes/ExecuteSamples.cs:13-20` · pulled down at 004's Step 2 triage
- [x] CLAUDE.md release process named a `<VersionPrefix>` that does not exist, scanned commits for content, and stated an unfollowable nav_order rule · `CLAUDE.md` release block · all three corrected · [#99](https://github.com/NeatooDotNet/RemoteFactory/pull/99) · AC-5 · Must
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
- EXRM · `CanMethodCodePathTests` shares a static auth flag across five xUnit classes that run in parallel, so `CanLocalMethod_IsPublic` fails intermittently · pre-existing, untouched by this arc (`git log main..EXRM -- .../Can/` is empty), green on an unchanged re-run; serves no criterion — captured as [#100](https://github.com/NeatooDotNet/RemoteFactory/issues/100)

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

### 2026-09-08 — Grade: A

**Veto-tier findings:** None open. One test failure in `final-test.log` — `CanMethodCodePathTests+CanLocalMethodTests.CanLocalMethod_IsPublic` — was examined and root-caused to a static auth flag shared across parallel xUnit classes in a file this arc never touched; green on an unchanged re-run. Filed as [#100](https://github.com/NeatooDotNet/RemoteFactory/issues/100), dismissed here, and routed to Follow-on rather than the grade.
**Callouts:** 3, all bookkeeping-tier. 005's Punchlist rows unchecked on a Done plan (fixed); Discovery Log over budget (accepted, see retro); two new CA1062 warnings on harness code (fixed — `NoWarn` with a reason; rebuild reports 0 warnings, 0 errors).
**Accepted gaps (B only):** n/a — grade A, every criterion traced to code.
**User acknowledgment:** 2026-09-08 — acknowledged, proceed to Step 8.
**Full audit:** [`reviews/close-out-audit.md`](./reviews/close-out-audit.md)

---

## Follow-on

- Flaky `CanLocalMethod_IsPublic`: a static auth flag shared across parallel xUnit classes · [#100](https://github.com/NeatooDotNet/RemoteFactory/issues/100) · close-out audit · Should
- `[AspAuthorize]` on a static-factory `[Execute]` is collected but never enforced · [#91](https://github.com/NeatooDotNet/RemoteFactory/issues/91) · EXRM-001 · Should
- Generated code relies on the consumer's `ImplicitUsings` for `System` / `System.Threading` types · [#97](https://github.com/NeatooDotNet/RemoteFactory/issues/97) · EXRM-004 · Could
- Intermittent `MSB3552` from a `PreBuild` `RemoveDir` race on multi-target builds · [#94](https://github.com/NeatooDotNet/RemoteFactory/issues/94) · EXRM-002 · Could
- Interface-factory body removal under trimming remains unestablished — carried forward from v1.7.0, not this arc's to settle · `docs/trimming.md`, `v1.9.0.md` · Could
- Tag `v1.9.0`, let the workflow publish and cut the GitHub release, then notify the waiting `ztreatmentneatoo-9c` session · after the arc merges · user's

---

## Docs & Retro

**Documentation:** shipped with the behaviour it describes, in EXRM-004 and EXRM-005 rather than as a trailing pass. Published docs — `attributes-reference.md` (the rule's single home, plus the `[Remote]` entry and the NF0105 reason), `trimming.md` (three `[Execute]` guard rows), `factory-operations.md`, `decision-guide.md`, `authorization.md` (the placement rule scoped by shape), `client-server-architecture.md` (three new visibility rows). Skill — `SKILL.md` Quick Decisions and the `static-factory`, `class-factory`, `trimming` and `anti-patterns` references, the "decorative" section replaced. Library — `ExecuteAttribute`'s XML doc rewritten and `RemoteAttribute` given one it never had; NF0102's description rejustified; NF0105's exemption reasoned at the check. Design — `AllPatterns.cs`, `ClassFactoryWithExecute.cs` (three placements, including the new `ArchiveOnServer`), `CLAUDE-DESIGN.md`, the README, the Blazor home page, one test comment. Reference app — a compiled bare sample the docs and skill share. Release — `v1.9.0.md`, the index, twelve `nav_order`s. Repo `CLAUDE.md` gained the mdsnippets placeholder-pair rule and three release-process corrections. No doc debt carried forward; the one internal-contradiction callout a reviewer parked (the gate summary's skimmable "no shape asserted PRESENT" line) is recorded as Theoretical in the audit, not deferred work.

**Retro (one paragraph):** Five plans issued of a cap of 8, all Done, none abandoned or retired — the first arc in a while where the cap was never felt, because the initial split was drawn from the Goal rather than from findings. The mechanic that repeatedly paid was the **opt-in plan review**: it ran four times and returned CONCERNS four times, and in three of those it caught a claim the orchestrator had already put in front of the user as settled — the trimming gate's present-check that could never go red (003), a rule sentence false for `internal static` (004), and a release header saying "no signature changed" when a generated signature had changed (005). Reviews cost four review files and bought four defects that would otherwise have shipped in a public document. The second lesson is narrower and sharper: **evidence has to be falsified, not just produced.** EXRM-003's red-before-green run exposed the orchestrator's own new assertions as vacuous — `Program.cs` is never trimmed, so the harness's marker literals rooted the very strings the gate greps for — and only the deliberate known-bad build revealed it. That discipline then caught its own smaller echo at 005, where a claimed CRLF revert was verified by `od -c` and found incomplete by one byte. Numbers: 12 findings dismissed, 8 punchlist rows worked (5 pulled down into plans, 3 closed inline), 0 queued as new plans — the discovery protocol's bias against new plans held completely, and the todo exited on its Goal with an empty queue rather than despite one. Four defects were filed rather than absorbed (#91, #94, #97, #100), which is the number this arc is proudest of: each is a real problem found, recorded with its mechanism, and left for a decision rather than quietly folded into work that had not asked for it. One accepted deviation: six of eight Discovery Log entries exceed the 60-word budget (worst 98). Accepted on the user's decision rather than trimmed — the entries are dense decision records whose specifics the plan reviews repeatedly turned on, and cutting them to a word count would have cost a successor more than the scannability was worth. Worth watching next time: the budget is right for journal-ish entries and wrong for entries that carry a generator's behaviour in them, so the fix is probably a longer allowance for `Amend` entries rather than shorter entries.

---

## Results / Conclusions

```
Plans: 5 issued of 8 cap — 5 Done, 0 Abandoned, 0 Retired.
Punchlist: 8 closed (5 pulled down into plans, 3 worked inline). Dismissed: 12. Follow-on: 6.
Gates: 13 review files — 4 plan reviews, 5 test reviews, 3 code reviews, 1 close-out audit.
       Every per-plan gate closed in one round; no plan needed a second.
Issues filed rather than fixed: #91, #94, #97, #100.
Close-Out Audit: Grade A (acknowledged 2026-09-08).
Arc: EXRM → main, PR #TBD (filled when opened).
```
