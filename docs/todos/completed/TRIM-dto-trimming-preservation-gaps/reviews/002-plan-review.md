# TRIM-002 Plan Review — 2026-07-06

**Reviewer:** plan-reviewer agent (two-pass).
**Verdict: APPROVED** — no veto findings either pass. Five callouts, all folded into the draft before implementation.

## Pass A — vs documented requirements

- The one documented-boundary change (`docs/trimming.md:283` "preserve it yourself" sentence; `CLAUDE-DESIGN.md:296` FAQ row) is exactly the delta the todo's Goal + Acceptance Criterion 2 sanction, and Step 7 targets it.
- "What Qualifies as a DTO" tables not contradicted — entities still never become DTOs; only their DTO-typed properties are discovered. No conflict with entity-duality guidance.
- **A1 (callout):** Step 7 must also rewrite the nested-discovery *prose* ("properties of each discovered DTO" framing), not just the boundary sentence. Anchors: `trimming.md:279,:283`; `CLAUDE-DESIGN.md:296,:789`. → Folded into Step 7.

## Pass B — vs codebase

Reality check passed: insertion seam (`FactoryGenerator.Types.cs:236-257`), model/renderer flow ("no renderer change" holds), over-preservation trade-off confirmed real and consistent (`RegisterFactories` runs every registrar regardless of mode; emissions idempotent).

- **B1 (callout, key):** "That type's own registrar owns its graph" is over-broad — a class implementing a `[Factory]` interface without carrying the attribute is rejected by `IsDtoStructureCandidate` (`DtoTypeWalker.cs:114-120`) but never gets a TypeInfo/registrar (`ForAttributeWithMetadataName` matches direct application only, `FactoryGenerator.cs:19-29`) — its property graph is covered by nobody. Diagnosis, not a live gap: those are stateless server-only service impls, never serialized. → Invariant narrowed; Constraints line added; no machinery.
- **B2 (callout, key):** `LazyLoad<T>` suspicion corrected at the code: `T` **is** walked (descent through the public `Value` getter, `LazyLoad.cs:176-181`); the only artifact is a benign spurious `Register<LazyLoad<T>>` emission (`[JsonConstructor] public LazyLoad()` at `LazyLoad.cs:92-97` puts it in the Register bucket; it deserializes via `LazyLoadJsonConverterFactory`, never the registry). → Step 4 rewritten with the corrected diagnosis; fix-vs-accept at keyboard; queue needs a stub row.
- **B3 (callout):** Ordinal interplay clean — different outputs to different registries; entity walk skips factory-typed (ordinal-serialized) properties. → Constraints note added.
- **B4 (callout):** `System` segment-match hardening sound; no BCL namespace is System-prefixed beyond `System`/`System.*`; no test pins the old over-exclusion.
- **B5 (callout):** Child-vs-parent registrar assertions need per-tree generated text — `runResult.GeneratedTrees` per-tree `FilePath` embeds the factory hint name; the existing concatenating helper cannot distinguish registrars. → Already in Current State; carried to test design.

## Disposition

| # | Finding | Disposition |
|---|---------|-------------|
| B1 | Interface-factory impl classes outside the walk | Invariant narrowed; Constraints exception documented |
| B2 | LazyLoad diagnosis corrected | Step 4 rewritten |
| A1 | Nested-discovery prose anchors | Step 7 anchors added |
| B4 | Hardening safe | No change needed |
| B3/B5 | Ordinal orthogonality / per-tree helper | Constraints note; test-design note |

Calibration: diagnoses adopted; all prescriptions matched the code walk and were adopted as-is.
