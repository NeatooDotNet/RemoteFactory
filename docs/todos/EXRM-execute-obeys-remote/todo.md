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

- [ ] **AC-1** · Must — A bare `[Execute]` on a static factory or a class factory runs on the client with client-container service resolution and makes no remote call.
- [ ] **AC-2** · Must — `[Remote, Execute]` on both shapes generates and behaves exactly as v1.8.1, including the non-async wrapper guard.
- [ ] **AC-3** · Must — The trimmed-client gate measures a `[Remote, Execute]` body absent and a bare `[Execute]` body present, for both shapes.
- [ ] **AC-4** · Should — `[AspAuthorize]` on a bare `[Execute]` still forces remote, and `[AuthorizeFactory]` on a bare `[Execute]` runs locally.
- [ ] **AC-5** · Must — Design projects, published docs, and the skill state the new contract with the decorative claim gone, and release notes ship per CI standards.
- [ ] **AC-6** · Could — The NF0105 static-factory exemption is re-examined and either kept with a stated reason or narrowed.

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
| 001 | [001-static-delegates-obey-remote](./plans/001-static-delegates-obey-remote.md) | Static-factory delegates obey [Remote]; weld removed | AC-1, AC-2 | Draft | — |
| 002 | [002-local-execute-coverage](./plans/002-local-execute-coverage.md) | Local Execute proven on both shapes, plus auth | AC-1, AC-2, AC-4 | Draft | — |
| 003 | [003-trimming-gate-pair](./plans/003-trimming-gate-pair.md) | Trimming gate measures absent and present pair | AC-3 | Draft | — |
| 004 | [004-contract-in-design-docs-skill](./plans/004-contract-in-design-docs-skill.md) | New contract in Design, docs, skill, diagnostics | AC-5, AC-6 | Draft | — |
| 005 | [005-release-1-9-0](./plans/005-release-1-9-0.md) | Release notes and version for v1.9.0 | AC-5 | Draft | — |

---

## Punchlist

- [ ] `FileVersion` reads 1.7.0 while `PackageVersion` reads 1.8.1 · `src/Directory.Build.props:18-19` · done when both read 1.9.0 · AC-5 · Must
- [ ] Region header "Execute is always remote" · `src/Tests/RemoteFactory.IntegrationTests/Combinations/ExecuteBehaviorTests.cs:9,27` · done when the header states the [Remote]-follows rule · AC-2 · Must
- [ ] NF0102 description justifies the `Task` rule by remoteness · `src/Generator/DiagnosticDescriptors.cs:35` · done when the text no longer says "designed for remote execution" · AC-5 · Must
- [ ] Dead Execute clause in the record-primary-constructor ctor · `src/Generator/FactoryGenerator.Types.cs:629` · done when removed with the weld · AC-2 · Must

## Dismissed

- EXRM · `TargetClassGenerator.cs` (367 lines, referenced by nothing) hardcodes bare `[Execute]` · dead code; serves no criterion — edit `CombinationGenerator.cs`, not it
- EXRM · Rename the five `Comb_Execute_*_Remote` targets · unnecessary once the combination generator emits `[Remote]` for Remote mode

---

## Discovery Log

(Append-only. ≤ 60 words per entry. Only Amend / Queue / Abandon / Re-split / Reprioritize.)

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
