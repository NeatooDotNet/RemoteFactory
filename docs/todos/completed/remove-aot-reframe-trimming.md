# Remove AOT, Reframe Around IL Trimming

**Status:** Complete
**Priority:** High
**Created:** 2026-03-07
**Last Updated:** 2026-03-07

**Requirements Review:** See below
**Plan:** [Remove AOT References, Reframe Around IL Trimming](../../plans/completed/remove-aot-reframe-trimming.md)

## Problem

AOT (Ahead-of-Time / Native AOT) support was added speculatively — users aren't asking for it. Users DO require IL Trimming. Having AOT references throughout the codebase is a distraction from making IL trimming rock solid. The project should remove AOT as a concept entirely and reframe everything around IL trimming.

## Solution

Remove all AOT mentions from code comments, documentation, and design docs. Reframe the justification for reflection-free patterns (ordinal converters, static converter registration) as being for IL trimming compatibility. The underlying code stays — it serves IL trimming — only the framing changes.

## Scope

### Source code comments (~8 locations)
- `src/RemoteFactory/IOrdinalConverterProvider.cs:7` — "AOT compatibility" comment
- `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/ReflectionFreeSerializationTests.cs:18` — "PROVES AOT PATH IS USED"
- `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs:18,78` — "AOT path" comments
- `src/Generator/Renderer/OrdinalRenderer.cs:198` — XML doc "AOT-compatible"
- `src/Generator/Renderer/ClassFactoryRenderer.cs:1547` — "AOT-compatible" comment
- `src/Generator/FactoryGenerator.cs:1144` — XML doc "AOT-compatible"
- `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:71` — "breaks AOT compilation"

### Documentation
- `src/Design/CLAUDE-DESIGN.md:142,633` — AOT in design decision tables
- `docs/interfaces-reference.md:482` — AOT compatibility mention
- `docs/release-notes/v0.5.0.md:81-86,144` — AOT documentation section
- `docs/release-notes/index.md:35` — AOT in highlights table

### Not in scope
- The underlying reflection-free code (ordinal converters, static registration) — this stays, it serves IL trimming
- Any functional code changes — this is comment/doc cleanup only

## Plans

- [Remove AOT References, Reframe Around IL Trimming](../../plans/completed/remove-aot-reframe-trimming.md)

## Tasks

- [x] Business requirements review (Step 2)
- [x] Architect comprehension check (Step 3)
- [x] Architect plan creation (Step 4)
- [x] Developer review (Step 5)
- [x] Implementation (Step 7)
- [x] Verification (Step 8)
- [x] Documentation (Step 9) — N/A, all doc changes were part of implementation
- [x] Completion (Step 10)

## Progress Log

- 2026-03-07: Todo created. Discovery identified ~8 source code comment locations and ~4 documentation files with AOT references. The AOT compatibility doc page (`docs/advanced/aot-compatibility.md`) referenced in v0.5.0 release notes no longer exists.
- 2026-03-07: Architect plan created at `docs/plans/remove-aot-reframe-trimming.md`. All 5 decision points resolved. 12 business rule assertions defined. Single-phase implementation design.
- 2026-03-07: Developer review raised 2 concerns (missing v0.5.0 line 144, grep exclusion scope). Architect resolved both. Developer re-reviewed and approved.
- 2026-03-07: Implementation complete — 17 replacements across 13 files. Build: 0 errors. Tests: 1,940 passed, 0 failed. Grep: zero AOT in scope.
- 2026-03-07: Architect verification: VERIFIED. Requirements verification: REQUIREMENTS SATISFIED. Todo complete.

## Requirements Review

### Reviewer
business-requirements-reviewer agent

### Reviewed
2026-03-07

### Verdict
APPROVED

### Relevant Requirements Found

**1. Private setter restriction and its AOT justification**
- `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:68-73` — "DID NOT DO THIS: Support private setters via reflection. Reasons: 1. Reflection is slow and breaks AOT compilation; 2. Source generation requires compile-time accessible setters; 3. Explicit public setters make serialization behavior obvious."
- `src/Design/CLAUDE-DESIGN.md:142` — Quick Decisions Table: "Can I use private setters? No. AllPatterns.cs:73. AOT compilation + source generation."
- `src/Design/CLAUDE-DESIGN.md:633` — Design Debt Table: "Private setter support. Not supported. Breaks AOT, adds reflection. Reconsider when: If .NET adds AOT-compatible private member access."

The proposed reframing from "AOT" to "IL trimming" is technically accurate here. The primary reason private setters are not supported is reason #2 (source generation uses partial classes, so private setters are inaccessible at compile time). The secondary reason (#1) cites reflection as problematic -- and reflection is equally problematic for IL trimming (the trimmer cannot trace reflection-based access and may remove types). Reframing from "breaks AOT compilation" to "incompatible with IL trimming" preserves the technical accuracy.

**2. Ordinal converter registration pattern (reflection-free serialization)**
- `src/RemoteFactory/IOrdinalConverterProvider.cs:7` — "Eliminates reflection-based converter creation for AOT compatibility."
- `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs:18` — "Static cache for registered converters (AOT path)."
- `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs:78` — "AOT path: Try registered converters first (fastest path)."

The static converter registration pattern (IOrdinalConverterProvider, RegisterConverter, pre-compiled converters) genuinely serves IL trimming. When converters are registered statically at startup, the trimmer can see the static references and preserve the necessary types. Reflection-based converter creation (the fallback path in NeatooOrdinalConverterFactory lines 87-100) is the path that is unsafe for trimming. Reframing these comments from "AOT" to "IL trimming" is accurate.

**3. Generated code XML docs**
- `src/Generator/Renderer/OrdinalRenderer.cs:198` — Generated XML doc says "Creates an AOT-compatible ordinal converter for this type."
- `src/Generator/FactoryGenerator.cs:1144` — Same XML doc text.
- `src/Generator/Renderer/ClassFactoryRenderer.cs:1547` — Comment "Register AOT-compatible ordinal converter."

These are XML doc comments in generated code. The converters are compatible with both AOT and IL trimming. Reframing to "trimming-compatible" or "IL-trimming-compatible" is accurate.

**4. Test comment**
- `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/ReflectionFreeSerializationTests.cs:18` — Section header "PROVES AOT PATH IS USED."

The test (RegisteredConverterIsUsedInsteadOfReflection) proves that pre-registered converters are used instead of reflection-based creation. This test is valid for both AOT and IL trimming. Renaming the section to reference IL trimming is accurate.

**5. Documentation references**
- `docs/interfaces-reference.md:482` — "Uses static abstract interface members for AOT compatibility."
- `docs/release-notes/v0.5.0.md:81-86` — "AOT Compatibility Documentation" section with link to `docs/advanced/aot-compatibility.md` (page no longer exists).
- `docs/release-notes/index.md:35` — Highlights table entry "Constructor injection compatibility, AOT documentation."

**6. Existing IL trimming documentation (context)**
- `docs/trimming.md` — Comprehensive IL trimming page already exists with full documentation of feature switch guards, configuration, and verification. This page makes zero reference to AOT, confirming that the project has already shifted its primary framing to IL trimming.

**7. Agent file reference**
- `.claude/agents/business-requirements-documenter.md:18` — Contains an example scenario mentioning AOT: "A todo resolved a design debt item -- private setter support was added because .NET added AOT-compatible private member access." This is an illustrative example in an agent description, not a business requirement. However, the architect should note it as an additional location where AOT terminology appears.

### Gaps

**1. The completed todo file also references AOT.**
- `docs/todos/completed/neatoo-constructor-injection-fix.md:291` — "[x] Add AOT compatibility documentation."
- `docs/todos/completed/restructure-test-projects.md:193` — "AOT converter tests."
- `docs/plans/completed/test-restructuring-plan.md:501` — "AOT converter tests."
- `docs/todos/completed/logging-implementation-plan.md:85,92,118` — Multiple AOT references in logging plan.

These are historical records in completed todos/plans. The architect must decide whether completed artifacts should be updated or left as historical documents reflecting the terminology at the time of completion. There is no existing policy on this.

**2. The Design Debt table "Reconsider When" column references AOT.**
- `src/Design/CLAUDE-DESIGN.md:633` — "If .NET adds AOT-compatible private member access."

When reframing, the architect must decide on an appropriate new trigger condition. The current trigger ("If .NET adds AOT-compatible private member access") should be reframed to something like "If .NET adds trimming-safe private member access" or reconsidered entirely, since the primary blocker is compile-time accessibility in partial classes (reason #2), not AOT/trimming.

**3. No policy exists for handling broken doc links in historical release notes.**
- `docs/release-notes/v0.5.0.md:83` — Links to `../advanced/aot-compatibility.md` which no longer exists. The architect should decide how to handle this: remove the link, redirect it, or replace it with a reference to `docs/trimming.md`.

### Contradictions

None found. The proposed changes are comment-only and documentation-only. No functional code is being modified. The reframing from "AOT" to "IL trimming" is technically accurate in every location identified.

### Recommendations for Architect

1. **The reframing is technically sound.** In every location where "AOT" appears, the reflection-free pattern serves IL trimming equally well (or better). The existing `docs/trimming.md` page already frames the project around IL trimming with no AOT references, confirming the project's direction.

2. **Preserve the primary technical reason for private setter restriction.** The reason private setters are not supported is primarily that source-generated partial classes cannot access private members (reason #2 in AllPatterns.cs). When reframing reason #1 from "breaks AOT" to "breaks IL trimming," do not lose the nuance that this is a secondary concern. The primary blocker is compile-time accessibility.

3. **Decide on completed todos/plans.** The scope lists only active files. Completed todos and plans in `docs/todos/completed/` and `docs/plans/completed/` also contain AOT references. Consider whether these historical documents should be updated or left as-is.

4. **Fix the broken link in v0.5.0 release notes.** The link to `../advanced/aot-compatibility.md` points to a page that no longer exists. This is a pre-existing issue but is naturally addressed by this todo's scope.

5. **Note the agent file.** `.claude/agents/business-requirements-documenter.md` contains an illustrative example mentioning AOT. This is not in the todo's scope but the architect should be aware of it.

6. **The v0.5.0 release notes section "AOT Compatibility Documentation" will need careful handling.** This is a historical record of what was released. The architect should decide whether to remove the section, reframe it, or add a note that the AOT page was later removed in favor of `docs/trimming.md`.

## Results / Conclusions

All AOT references removed from source code comments, documentation, and design docs. Reframed around IL trimming. 17 text replacements across 13 files. No functional code changes. All tests pass. Historical completed todos/plans left unchanged per DP-1.
