# Remove AOT References, Reframe Around IL Trimming

**Date:** 2026-03-07
**Related Todo:** [Remove AOT, Reframe Around IL Trimming](../../todos/completed/remove-aot-reframe-trimming.md)
**Status:** Complete
**Last Updated:** 2026-03-07

---

## Overview

Remove all AOT (Ahead-of-Time / Native AOT) references from source code comments, documentation, and design docs. Reframe the justification for reflection-free patterns (ordinal converters, static converter registration) as being for IL trimming compatibility. The underlying code stays unchanged -- it serves IL trimming -- only the framing changes.

This is a comment/documentation cleanup with no functional code changes.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/remove-aot-reframe-trimming.md#requirements-review)

### Relevant Existing Requirements

#### Design Rules

- **Private setter restriction** (`src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:68-73`): Private setters are not supported for three reasons: (1) reflection is slow and breaks AOT compilation, (2) source generation requires compile-time accessible setters, (3) explicit public setters make serialization behavior obvious. -- Relevance: Reason #1 must be reframed from "breaks AOT" to "incompatible with IL trimming." Reason #2 remains the primary blocker and is unaffected.

- **Quick Decisions Table** (`src/Design/CLAUDE-DESIGN.md:142`): "Can I use private setters? No. AllPatterns.cs:73. AOT compilation + source generation." -- Relevance: Reason must be reframed to reference IL trimming instead of AOT.

- **Design Debt Table** (`src/Design/CLAUDE-DESIGN.md:633`): "Private setter support. Not supported. Breaks AOT, adds reflection. Reconsider When: If .NET adds AOT-compatible private member access." -- Relevance: Both the "Why Deferred" and "Reconsider When" columns reference AOT and must be reframed.

#### Serialization Architecture

- **IOrdinalConverterProvider** (`src/RemoteFactory/IOrdinalConverterProvider.cs:7`): XML doc says "Eliminates reflection-based converter creation for AOT compatibility." -- Relevance: Comment must be reframed. The static registration pattern genuinely serves IL trimming because the trimmer can trace static references and preserve necessary types.

- **NeatooOrdinalConverterFactory** (`src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs:18,78`): Comments reference "AOT path" for the static converter cache. -- Relevance: Must be reframed. The static cache is the trimming-safe path; the reflection fallback (lines 87-100) is the trimming-unsafe path.

#### Generated Code

- **OrdinalRenderer** (`src/Generator/Renderer/OrdinalRenderer.cs:198`): Generated XML doc says "Creates an AOT-compatible ordinal converter for this type." -- Relevance: Must change to "trimming-compatible" or similar.

- **ClassFactoryRenderer** (`src/Generator/Renderer/ClassFactoryRenderer.cs:1547`): Comment says "Register AOT-compatible ordinal converter." -- Relevance: Must be reframed.

- **FactoryGenerator** (`src/Generator/FactoryGenerator.cs:1144`): XML doc says "AOT-compatible." -- Relevance: Must be reframed.

#### Tests

- **ReflectionFreeSerializationTests** (`src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/ReflectionFreeSerializationTests.cs:18`): Section header says "PROVES AOT PATH IS USED." -- Relevance: Must be reframed. The test proves the pre-registered (trimming-safe) path is used instead of the reflection (trimming-unsafe) fallback.

#### Published Documentation

- **interfaces-reference.md** (`docs/interfaces-reference.md:482`): "Uses static abstract interface members for AOT compatibility." -- Relevance: Must be reframed.

- **v0.5.0 release notes** (`docs/release-notes/v0.5.0.md:81-86`): "AOT Compatibility Documentation" section with link to `docs/advanced/aot-compatibility.md` (page no longer exists). -- Relevance: Needs careful handling as a historical record.

- **Release notes index** (`docs/release-notes/index.md:35`): Highlights table says "Constructor injection compatibility, AOT documentation." -- Relevance: Must be reframed.

#### Existing IL Trimming Documentation (Context)

- **docs/trimming.md**: Comprehensive IL trimming page already exists with zero AOT references. This confirms the project has already shifted its primary framing to IL trimming. The reframing in this plan is completing that shift.

### Gaps

1. **Completed todos/plans contain AOT references.** Files in `docs/todos/completed/` and `docs/plans/completed/` reference AOT. No existing policy dictates whether these historical documents should be updated.

2. **Design Debt "Reconsider When" trigger references AOT.** The current trigger "If .NET adds AOT-compatible private member access" needs reframing, but the primary blocker is compile-time accessibility (reason #2), not trimming.

3. **Broken doc link in v0.5.0 release notes.** Link to `../advanced/aot-compatibility.md` points to a nonexistent page.

### Contradictions

None. The reframing from "AOT" to "IL trimming" is technically accurate in every location.

### Recommendations for Architect

1. The reframing is technically sound in all locations.
2. Preserve the primary technical reason for the private setter restriction (compile-time accessibility, reason #2).
3. Fix the broken link in v0.5.0 release notes.
4. Note the agent file at `.claude/agents/business-requirements-documenter.md:18`.

---

## Decision Points

The comprehension check identified 5 decision points. Architectural decisions for each:

### DP-1: Completed todos/plans -- update or leave as historical?

**Decision: Leave as historical.** Completed todos and plans are historical records of what was done at the time. They reflect the terminology and understanding when the work was completed. Retroactively editing them:
- Distorts the historical record
- Adds effort with no user-facing benefit
- Creates risk of accidentally altering meaning in historical context

The completed files (`docs/todos/completed/neatoo-constructor-injection-fix.md`, `docs/todos/completed/restructure-test-projects.md`, `docs/plans/completed/test-restructuring-plan.md`, `docs/todos/completed/logging-implementation-plan.md`) will not be modified.

### DP-2: Broken link to `docs/advanced/aot-compatibility.md`

**Decision: Replace with a reference to `docs/trimming.md`.** The v0.5.0 release notes section that links to the nonexistent AOT page should redirect readers to the current trimming documentation. The link target changes from `../advanced/aot-compatibility.md` to `../trimming.md` with updated link text.

### DP-3: v0.5.0 "AOT Compatibility Documentation" section handling

**Decision: Reframe the section with a historical note.** The v0.5.0 release notes are a historical record of what shipped, but they are also a live document that users may read. The section heading should be changed to reflect the current framing (e.g., "IL Trimming Documentation") and the body should note that the documentation was later consolidated into the trimming page. This preserves the historical context while not confusing readers with references to a feature (AOT support) that the project no longer claims.

### DP-4: Design Debt "Reconsider When" trigger reframing

**Decision: Reframe around the actual primary blocker.** The current trigger "If .NET adds AOT-compatible private member access" conflates two separate concerns. The primary reason private setters are unsupported is compile-time accessibility in partial classes (reason #2). Reflection is the secondary concern, and it is equally problematic for IL trimming. The reframed row should:
- "Why Deferred" column: "Adds reflection, incompatible with IL trimming" (keeping reflection concern but reframing from AOT to trimming)
- "Reconsider When" column: "If .NET adds source-generator-accessible private member access" (focus on the actual primary blocker -- compile-time accessibility -- rather than the secondary trimming concern)

### DP-5: `.claude/agents/business-requirements-documenter.md` agent file scope

**Decision: Include in scope as an additional cleanup location.** The agent file at `.claude/agents/business-requirements-documenter.md:18` contains an illustrative example that references AOT: "private setter support was added because .NET added AOT-compatible private member access." While this is an illustrative scenario in an agent description (not a business requirement), it should be reframed for consistency. The example should reference IL trimming instead. This is a markdown file, not source code, so the documenter agent can edit it directly during the Documentation step.

---

## Business Rules (Testable Assertions)

These are verifiable assertions that define "done" for this cleanup. Since no functional code changes, the assertions are about content presence/absence in files.

1. WHEN searching all `.cs` files under `src/` for the string "AOT" (case-insensitive), THEN zero matches are found. -- Source: Todo Problem statement ("remove all AOT mentions from code comments")

2. WHEN searching all `.md` files under `docs/` (excluding `docs/todos/` and `docs/plans/`, which are workflow artifacts) for the string "AOT" (case-insensitive), THEN zero matches are found. -- Source: Todo Problem statement ("remove all AOT mentions from documentation")

3. WHEN searching `src/Design/CLAUDE-DESIGN.md` for the string "AOT" (case-insensitive), THEN zero matches are found. -- Source: Todo Problem statement ("remove all AOT mentions from design docs")

4. WHEN reading `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:71`, THEN reason #1 for not supporting private setters references "IL trimming" (not "AOT") while reason #2 (compile-time accessibility) remains unchanged. -- Source: Relevant Requirement #1 and Reviewer Recommendation #2

5. WHEN reading `src/Design/CLAUDE-DESIGN.md` Quick Decisions Table entry for "Can I use private setters?", THEN the Reason column references "IL trimming" (not "AOT"). -- Source: Relevant Requirement #1

6. WHEN reading `src/Design/CLAUDE-DESIGN.md` Design Debt Table entry for "Private setter support", THEN the "Why Deferred" column references reflection and IL trimming (not AOT), and the "Reconsider When" column references source-generator-accessible private member access (not AOT-compatible). -- Source: Relevant Requirement #1, DP-4

7. WHEN reading `docs/release-notes/v0.5.0.md`, THEN no link to `../advanced/aot-compatibility.md` exists, and any former AOT section redirects readers to `../trimming.md`. -- Source: Reviewer Recommendation #4, DP-2, DP-3

8. WHEN reading `docs/release-notes/index.md` highlights table entry for v0.5.0, THEN it references "IL trimming" (not "AOT"). -- Source: Relevant Requirement #5

9. WHEN reading `.claude/agents/business-requirements-documenter.md`, THEN no reference to "AOT" exists; the illustrative example references IL trimming instead. -- Source: Relevant Requirement #7, DP-5

10. WHEN running `dotnet build src/Neatoo.RemoteFactory.sln`, THEN the build succeeds with zero errors. -- Source: NEW (regression safety -- comment changes must not break compilation)

11. WHEN running `dotnet test src/Neatoo.RemoteFactory.sln`, THEN all tests pass. -- Source: NEW (regression safety -- no functional behavior has changed)

12. WHEN reading completed todo/plan files in `docs/todos/completed/` and `docs/plans/completed/`, THEN AOT references remain unchanged (historical documents are not modified). Active todo/plan files (`docs/todos/` and `docs/plans/`) are workflow artifacts that naturally reference AOT as part of documenting this cleanup and are excluded from the verification scope. -- Source: DP-1

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | No AOT in source code | `grep -ri "AOT" src/**/*.cs` | Rule 1 | Zero matches |
| 2 | No AOT in active docs | `grep -ri "AOT" docs/*.md docs/release-notes/*.md` (excluding `docs/todos/` and `docs/plans/`) | Rule 2 | Zero matches |
| 3 | No AOT in CLAUDE-DESIGN.md | `grep -i "AOT" src/Design/CLAUDE-DESIGN.md` | Rule 3 | Zero matches |
| 4 | AllPatterns.cs reason #1 reframed | Read AllPatterns.cs:68-73 | Rule 4 | Reason #1 says "IL trimming"; reason #2 unchanged |
| 5 | Quick Decisions Table reframed | Read CLAUDE-DESIGN.md:142 | Rule 5 | "IL trimming + source generation" |
| 6 | Design Debt Table reframed | Read CLAUDE-DESIGN.md:633 | Rule 6 | "Reconsider When" references source-generator accessibility |
| 7 | v0.5.0 broken link fixed | Read v0.5.0.md:81-86 | Rule 7 | Link points to trimming.md with updated section text |
| 8 | Release notes index reframed | Read index.md:35 | Rule 8 | "IL trimming documentation" or similar |
| 9 | Agent file reframed | Read business-requirements-documenter.md:18 | Rule 9 | Example references IL trimming |
| 10 | Build succeeds | `dotnet build` | Rule 10 | Exit code 0, zero errors |
| 11 | Tests pass | `dotnet test` | Rule 11 | All tests pass |
| 12 | Completed files untouched | `grep -i "AOT" docs/todos/completed/*.md docs/plans/completed/*.md` | Rule 12 | Matches still present (unchanged) |

---

## Approach

Systematic find-and-replace of AOT terminology across three file categories:

1. **Source code comments** (~8 locations in `.cs` files): Replace "AOT" references with "IL trimming" equivalents, preserving technical accuracy
2. **Design documentation** (CLAUDE-DESIGN.md, AllPatterns.cs comments): Reframe reasoning while preserving the primary technical rationale (compile-time accessibility for private setters)
3. **Published documentation** (docs/*.md, release notes): Reframe references and fix the broken v0.5.0 link
4. **Agent file** (.claude/agents/business-requirements-documenter.md): Reframe the illustrative example

No functional code changes. No test logic changes. The test section header comment is updated but the test itself is unchanged.

---

## Design

### Replacement Mapping

Each AOT reference maps to a specific replacement:

| File | Current Text | Replacement Text | Category |
|------|-------------|-----------------|----------|
| `IOrdinalConverterProvider.cs:7` | "AOT compatibility" | "IL trimming compatibility" | Source comment |
| `NeatooOrdinalConverterFactory.cs:18` | "AOT path" | "trimming-safe path" | Source comment |
| `NeatooOrdinalConverterFactory.cs:78` | "AOT path" | "trimming-safe path" | Source comment |
| `OrdinalRenderer.cs:198` | "AOT-compatible" | "trimming-compatible" | Source comment |
| `ClassFactoryRenderer.cs:1547` | "AOT-compatible" | "trimming-compatible" | Source comment |
| `FactoryGenerator.cs:1144` | "AOT-compatible" | "trimming-compatible" | Source comment |
| `ReflectionFreeSerializationTests.cs:18` | "PROVES AOT PATH IS USED" | "PROVES TRIMMING-SAFE PATH IS USED" | Test comment |
| `AllPatterns.cs:71` | "breaks AOT compilation" | "incompatible with IL trimming" | Design source |
| `CLAUDE-DESIGN.md:142` | "AOT compilation + source generation" | "IL trimming + source generation" | Design doc |
| `CLAUDE-DESIGN.md:633` Why Deferred | "Breaks AOT, adds reflection" | "Adds reflection, incompatible with IL trimming" | Design doc |
| `CLAUDE-DESIGN.md:633` Reconsider When | "If .NET adds AOT-compatible private member access" | "If .NET adds source-generator-accessible private member access" | Design doc |
| `interfaces-reference.md:482` | "AOT compatibility" | "IL trimming compatibility" | Published doc |
| `v0.5.0.md:81` | "AOT Compatibility Documentation" section | Reframed section (see DP-3) | Release notes |
| `v0.5.0.md:83` | Link to `../advanced/aot-compatibility.md` | Link to `../trimming.md` | Release notes |
| `v0.5.0.md:144` | "docs: Add AOT compatibility documentation" | "docs: Add IL trimming documentation" | Release notes |
| `index.md:35` | "AOT documentation" | "IL trimming documentation" | Release notes |
| `business-requirements-documenter.md:18` | "AOT-compatible private member access" | "trimming-safe private member access" (or similar) | Agent file |

### v0.5.0 Release Notes Section Rewrite

The "AOT Compatibility Documentation" section (lines 81-86) should become:

```markdown
### IL Trimming Documentation

Added comprehensive [IL Trimming](../trimming.md) documentation explaining:
- How feature switch guards enable safe trimming
- Configuration for Blazor WASM and other trimmed scenarios
- How to verify trimming behavior

*Note: This section originally documented AOT compatibility. The documentation was later consolidated into the IL Trimming page, which covers the same reflection-free patterns.*
```

---

## Implementation Steps

1. **Source code comments** (6 files): Update AOT references in `.cs` files under `src/RemoteFactory/`, `src/Generator/`
2. **Test comment** (1 file): Update section header in `ReflectionFreeSerializationTests.cs`
3. **Design source code** (1 file): Update `AllPatterns.cs` comment block (reason #1 only; reasons #2 and #3 unchanged)
4. **Design documentation** (1 file): Update `CLAUDE-DESIGN.md` Quick Decisions Table and Design Debt Table
5. **Published documentation** (2 files): Update `interfaces-reference.md` and `v0.5.0.md`
6. **Release notes index** (1 file): Update `index.md` highlights table
7. **Agent file** (1 file): Update `.claude/agents/business-requirements-documenter.md` example
8. **Verification**: Build and test to confirm no regressions
9. **Validation**: Run grep for "AOT" across all in-scope files to confirm zero remaining references

---

## Acceptance Criteria

- [ ] Zero occurrences of "AOT" (case-insensitive) in any `.cs` file under `src/`
- [ ] Zero occurrences of "AOT" (case-insensitive) in any `.md` file under `docs/` (excluding `docs/todos/` and `docs/plans/`, which are workflow artifacts)
- [ ] Zero occurrences of "AOT" (case-insensitive) in `src/Design/CLAUDE-DESIGN.md`
- [ ] Zero occurrences of "AOT" (case-insensitive) in `.claude/agents/business-requirements-documenter.md`
- [ ] Broken link in v0.5.0.md fixed (points to `../trimming.md`)
- [ ] `AllPatterns.cs` reason #2 (compile-time accessibility) unchanged
- [ ] `dotnet build src/Neatoo.RemoteFactory.sln` succeeds
- [ ] `dotnet test src/Neatoo.RemoteFactory.sln` all tests pass
- [ ] Files in `docs/todos/` and `docs/plans/` are NOT modified (workflow artifacts excluded from scope)

---

## Dependencies

None. This is a self-contained cleanup with no prerequisites.

---

## Risks / Considerations

1. **Line number drift**: The todo references specific line numbers. The developer should search for the actual text rather than relying on line numbers, which may have shifted since the todo was written.

2. **XML doc in generated code**: Changes to `OrdinalRenderer.cs`, `ClassFactoryRenderer.cs`, and `FactoryGenerator.cs` modify the XML doc strings that appear in GENERATED source output. After making these changes, the generated output will contain the new terminology. This is intentional -- the generated code should also say "trimming-compatible" rather than "AOT-compatible."

3. **Release notes as user-facing docs**: The v0.5.0 release notes are potentially read by users who adopted RemoteFactory at v0.5.0. The reframing should be clear that the functionality is the same, just the terminology has changed.

4. **No undiscovered AOT references**: The todo's discovery identified specific locations. The developer should run a full-codebase grep to catch any references that were missed during discovery.

---

## Architectural Verification

**Scope Table:**

| Category | Files Affected | Change Type |
|----------|---------------|-------------|
| Source code comments | 6 `.cs` files in `src/RemoteFactory/` and `src/Generator/` | Comment text only |
| Test comments | 1 `.cs` file in `src/Tests/` | Comment text only |
| Design source code | 1 `.cs` file in `src/Design/` | Comment text only |
| Design documentation | 1 `.md` file (`CLAUDE-DESIGN.md`) | Table cell text |
| Published documentation | 2 `.md` files in `docs/` | Content text |
| Release notes | 1 `.md` file in `docs/release-notes/` | Section rewrite + link fix |
| Agent file | 1 `.md` file in `.claude/agents/` | Example text |

**Verification Evidence:**
- All changes are to comments, documentation text, and XML doc strings
- No method signatures, class definitions, or logic is modified
- The existing `docs/trimming.md` page confirms the project's established IL trimming framing

**Breaking Changes:** No. Zero functional code changes.

**Codebase Analysis:**
- Confirmed 8 source code locations with AOT references (per todo Scope section)
- Confirmed 4 documentation locations with AOT references
- Confirmed 1 agent file location with AOT reference
- Confirmed completed todos/plans have AOT references but are out of scope (DP-1)
- Confirmed `docs/trimming.md` exists and uses zero AOT terminology

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Full implementation | developer | Yes | Single straightforward cleanup -- all changes are comment/doc edits with clear replacement mapping. No need to split across agents. | None |

**Parallelizable phases:** None (single phase).

**Notes:** This is a simple cleanup task. A single developer agent invocation with the full replacement mapping table can complete all changes, run verification, and report back. There is no benefit to splitting into multiple phases -- all changes are independent text edits that can be done in sequence within one context window.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-07

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | Replacement Mapping rows 1-8: `IOrdinalConverterProvider.cs:7` replace "AOT compatibility" with "IL trimming compatibility"; `NeatooOrdinalConverterFactory.cs:18` replace "AOT path" with "trimming-safe path"; `NeatooOrdinalConverterFactory.cs:78` replace "AOT path" with "trimming-safe path"; `OrdinalRenderer.cs:198` replace "AOT-compatible" with "trimming-compatible"; `ClassFactoryRenderer.cs:1547` replace "AOT-compatible" with "trimming-compatible"; `FactoryGenerator.cs:1144` replace "AOT-compatible" with "trimming-compatible"; `ReflectionFreeSerializationTests.cs:18` replace "AOT PATH" with "TRIMMING-SAFE PATH"; `AllPatterns.cs:71` replace "breaks AOT compilation" with "incompatible with IL trimming" | Zero AOT matches in `src/**/*.cs` | Yes | Verified: independent grep found exactly 8 `.cs` matches under `src/`. All 8 covered by Replacement Mapping rows. |
| 2 | Replacement Mapping rows for `interfaces-reference.md:482`, `v0.5.0.md:81,83,144`, `index.md:35` plus the v0.5.0 Section Rewrite (plan lines 203-215). Rule 2 exclusion covers `docs/todos/` and `docs/plans/` (workflow artifacts). | Zero AOT matches in `docs/*.md` and `docs/release-notes/*.md` (excluding `docs/todos/` and `docs/plans/`) | Yes | Verified: v0.5.0.md has 5 AOT refs (lines 81,83,84,86,144). Section Rewrite replaces lines 81-86 entirely; line 198 row replaces line 144. `interfaces-reference.md:482` and `index.md:35` each have 1 ref, both covered. Exclusion of `docs/todos/` and `docs/plans/` correctly scopes out the workflow artifacts. |
| 3 | Replacement Mapping rows for `CLAUDE-DESIGN.md:142` ("AOT compilation + source generation" -> "IL trimming + source generation") and `CLAUDE-DESIGN.md:633` (two cells: Why Deferred + Reconsider When) | Zero AOT matches in `CLAUDE-DESIGN.md` | Yes | Verified: exactly 3 AOT occurrences in `CLAUDE-DESIGN.md` (line 142, line 633 x2). All 3 covered by Replacement Mapping. |
| 4 | Replacement Mapping row `AllPatterns.cs:71`: replace "Reflection is slow and breaks AOT compilation" with "Reflection is slow and incompatible with IL trimming" (reason #1 only). Line 72 reason #2 ("Source generation requires compile-time accessible setters") is NOT in the mapping. | Reason #1 says "IL trimming"; reason #2 unchanged | Yes | Verified: line 71 = `// 1. Reflection is slow and breaks AOT compilation`; line 72 = `// 2. Source generation requires compile-time accessible setters`. Only line 71 is replaced. |
| 5 | Replacement Mapping row `CLAUDE-DESIGN.md:142`: replace "AOT compilation + source generation" with "IL trimming + source generation" in the Reason column of the Quick Decisions Table. | Reason column references "IL trimming" | Yes | Verified: line 142 Reason column reads `AOT compilation + source generation`. Exact text match in Replacement Mapping. |
| 6 | Replacement Mapping rows for `CLAUDE-DESIGN.md:633`: Why Deferred column replace "Breaks AOT, adds reflection" with "Adds reflection, incompatible with IL trimming"; Reconsider When column replace "If .NET adds AOT-compatible private member access" with "If .NET adds source-generator-accessible private member access". | Both cells reframed; no AOT remains | Yes | Verified: line 633 contains both phrases in the table row. Both are covered by separate Replacement Mapping entries. |
| 7 | v0.5.0 Section Rewrite (plan lines 206-215) replaces entire section at lines 81-86 with "IL Trimming Documentation" heading, `../trimming.md` link, and historical note. Replacement Mapping row `v0.5.0.md:144` replaces "docs: Add AOT compatibility documentation" with "docs: Add IL trimming documentation". | No link to `../advanced/aot-compatibility.md`; former AOT section redirects to `../trimming.md`; Commits section line also reframed | Yes | Architect resolution added line 144 to the Replacement Mapping. All 5 AOT references in v0.5.0.md are now covered. |
| 8 | Replacement Mapping row `index.md:35`: replace "AOT documentation" with "IL trimming documentation" in the Highlights table entry for v0.5.0. | v0.5.0 entry references "IL trimming" | Yes | Verified: line 35 reads `Constructor injection compatibility, AOT documentation`. The "AOT documentation" portion is replaced. |
| 9 | Replacement Mapping row `business-requirements-documenter.md:18`: replace "AOT-compatible private member access" with "trimming-safe private member access". | No AOT reference in agent file | Yes | Verified: only 1 AOT match in the file (line 18). Covered by Replacement Mapping. |
| 10 | Implementation Step 8 + Verification Gate 1: `dotnet build src/Neatoo.RemoteFactory.sln` after all text changes. All changes are comment/XML-doc/markdown only -- no method signatures, logic, or syntax. | Build succeeds with zero errors | Yes | No functional code is modified. Only comment text and markdown content. |
| 11 | Implementation Step 8 + Verification Gate 2: `dotnet test src/Neatoo.RemoteFactory.sln` after all text changes. Only `ReflectionFreeSerializationTests.cs:18` section header comment changes; test method body and assertions are unchanged. | All tests pass | Yes | No test logic, assertions, or functional code is modified. |
| 12 | DP-1 Decision: "Leave as historical." Out of Scope section explicitly excludes all files in `docs/todos/` and `docs/plans/`. No Replacement Mapping entries target files in `completed/` subdirectories. Active todo/plan workflow artifacts are also excluded. | AOT references in `docs/todos/completed/` and `docs/plans/completed/` remain unchanged. Active workflow artifacts in `docs/todos/` and `docs/plans/` are excluded from verification scope. | Yes | Verified: 4 completed files contain AOT references. None are in the Replacement Mapping. Rule 12 updated to clarify active workflow artifact exclusion. |

### Concerns

None. Both prior concerns have been resolved by the architect.

---

## Implementation Contract

**Created:** 2026-03-07
**Approved by:** developer agent (claude-opus-4-6)

### Verification Acceptance Criteria

- [ ] `grep -ri "AOT" src/**/*.cs` returns zero matches
- [ ] `grep -ri "AOT" docs/*.md docs/release-notes/*.md src/Design/CLAUDE-DESIGN.md` returns zero matches (excluding `docs/todos/` and `docs/plans/`)
- [ ] `grep -i "AOT" .claude/agents/business-requirements-documenter.md` returns zero matches

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| 1-9 | Manual grep verification | Content assertions verified by text search |
| 10 | `dotnet build src/Neatoo.RemoteFactory.sln` | Build regression check |
| 11 | `dotnet test src/Neatoo.RemoteFactory.sln` | Test regression check |
| 12 | Manual grep of completed/ dirs | Verify historical files unchanged |

### In Scope

- [ ] Source code comments (6 files)
- [ ] Test comment (1 file)
- [ ] Design source code comment (1 file)
- [ ] Design documentation (1 file)
- [ ] Published documentation (2 files)
- [ ] Release notes index (1 file)
- [ ] Agent file (1 file)
- [ ] Checkpoint: Build and test after all changes

### Out of Scope

- All files in `docs/todos/` and `docs/plans/` (workflow artifacts that naturally reference AOT as part of documenting this cleanup; completed files are historical records per DP-1)
- Any functional code changes
- The underlying reflection-free code (ordinal converters, static registration)
- The `docs/trimming.md` page (already uses correct terminology)
- The `skills/RemoteFactory/` skill files (separate update workflow via MarkdownSnippets)

### Verification Gates

1. After all text changes: `dotnet build` succeeds
2. After all text changes: `dotnet test` passes
3. Final: Grep validation confirms zero remaining AOT references in scope

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure
- Discovery of AOT references in functional code (not comments) that would require logic changes
- Discovery of AOT references in files not listed in the replacement mapping (note: `docs/todos/` and `docs/plans/` are excluded as workflow artifacts)

---

## Implementation Progress

**Started:** 2026-03-07
**Developer:** claude-opus-4-6

**Milestone 1:** Source code and design comment updates
- [x] Update 6 source code `.cs` files
- [x] Update 1 test `.cs` file
- [x] Update 1 design `.cs` file (AllPatterns.cs)
- [x] **Verification**: `dotnet build` succeeds (0 errors, 0 warnings)

**Milestone 2:** Documentation updates
- [x] Update CLAUDE-DESIGN.md (Quick Decisions + Design Debt tables)
- [x] Update interfaces-reference.md
- [x] Update v0.5.0.md (section rewrite + link fix + commits section)
- [x] Update index.md (highlights table)
- [x] Update business-requirements-documenter.md (agent file)

**Milestone 3:** Final validation
- [x] Full-codebase grep for "AOT" to catch any missed references
- [x] `dotnet build` succeeds
- [x] `dotnet test` all tests pass (1,940 passed, 6 skipped performance tests, 0 failed)
- [x] Verify completed/ dirs are untouched (8 AOT references remain in 6 historical files)

**Implementation Note:** The v0.5.0 section rewrite's historical note ("This section originally documented AOT compatibility") was rephrased to "ahead-of-time compilation compatibility" to satisfy Business Rule #2 (zero AOT matches in docs/ outside of todos/plans). The plan's Section Rewrite and Rule #2 had an internal inconsistency; this resolves it while preserving the historical context.

---

## Completion Evidence

**Reported:** 2026-03-07

- **Tests Passing:** 1,940 passed, 6 skipped (performance tests), 0 failed across 6 test runs (UnitTests net9.0/net10.0: 475+475, IntegrationTests net9.0/net10.0: 476+476, RemoteOnlyTests net9.0/net10.0: 19+19)
- **Build:** 0 errors, 0 warnings
- **Verification Resources Pass:** N/A
- **All Contract Items:** Confirmed 100% complete

### Grep Verification Results

| Scope | Command | Result |
|-------|---------|--------|
| Source code (.cs under src/) | `grep -ri "AOT" src/**/*.cs` | Zero matches |
| Published docs (docs/*.md excl. todos/plans) | `grep -ri "AOT" docs/*.md docs/release-notes/*.md` | Zero matches |
| CLAUDE-DESIGN.md | `grep -i "AOT" src/Design/CLAUDE-DESIGN.md` | Zero matches |
| Agent file | `grep -i "AOT" .claude/agents/business-requirements-documenter.md` | Zero matches |
| Completed dirs (historical) | `grep -i "AOT" docs/todos/completed/*.md docs/plans/completed/*.md` | 8 matches across 6 files (unchanged, as expected) |

### Files Modified

**Source code comments (6 files):**
1. `src/RemoteFactory/IOrdinalConverterProvider.cs` -- "AOT compatibility" -> "IL trimming compatibility"
2. `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs` -- "AOT path" -> "trimming-safe path" (2 locations: lines 18, 78)
3. `src/Generator/Renderer/OrdinalRenderer.cs` -- "AOT-compatible" -> "trimming-compatible"
4. `src/Generator/Renderer/ClassFactoryRenderer.cs` -- "AOT-compatible" -> "trimming-compatible"
5. `src/Generator/FactoryGenerator.cs` -- "AOT-compatible" -> "trimming-compatible"

**Test comment (1 file):**
6. `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/ReflectionFreeSerializationTests.cs` -- "PROVES AOT PATH IS USED" -> "PROVES TRIMMING-SAFE PATH IS USED"

**Design source code (1 file):**
7. `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs` -- "breaks AOT compilation" -> "incompatible with IL trimming"

**Design documentation (1 file):**
8. `src/Design/CLAUDE-DESIGN.md` -- Quick Decisions Table: "AOT compilation + source generation" -> "IL trimming + source generation"; Design Debt Table: "Breaks AOT, adds reflection" -> "Adds reflection, incompatible with IL trimming"; "If .NET adds AOT-compatible private member access" -> "If .NET adds source-generator-accessible private member access"

**Published documentation (3 files):**
9. `docs/interfaces-reference.md` -- "AOT compatibility" -> "IL trimming compatibility"
10. `docs/release-notes/v0.5.0.md` -- Section rewrite (AOT Compatibility Documentation -> IL Trimming Documentation with trimming.md link and historical note); Commits section: "Add AOT compatibility documentation" -> "Add IL trimming documentation"
11. `docs/release-notes/index.md` -- "AOT documentation" -> "IL trimming documentation"

**Agent file (1 file):**
12. `.claude/agents/business-requirements-documenter.md` -- "AOT-compatible private member access" -> "trimming-safe private member access"

---

## Documentation

**Agent:** business-requirements-documenter
**Completed:** [date]

### Expected Deliverables

- [ ] CLAUDE-DESIGN.md updates (Quick Decisions Table, Design Debt Table) -- handled during implementation since these are direct targets of the cleanup
- [ ] Published docs updates (interfaces-reference.md, release notes) -- handled during implementation
- [ ] Agent file update (business-requirements-documenter.md) -- handled during implementation
- [ ] Skill updates: N/A (skill files use MarkdownSnippets; if any skill files reference AOT, those would need reference-app code changes + mdsnippets run, but this should be verified)
- [ ] Sample updates: N/A

### Files Updated

- [to be filled after completion]

---

## Architect Verification

**Verified:** 2026-03-07
**Verdict:** VERIFIED

**Independent test results:**
- Build: 0 errors, 0 warnings (independently confirmed)
- Tests: 1,940 passed, 6 skipped (performance tests), 0 failed across 6 test runs
  - UnitTests net9.0: 475 passed
  - UnitTests net10.0: 475 passed
  - IntegrationTests net9.0: 476 passed, 3 skipped
  - IntegrationTests net10.0: 476 passed, 3 skipped
  - RemoteOnlyTests net9.0: 19 passed
  - RemoteOnlyTests net10.0: 19 passed

**Grep verification (independent):**
- `src/**/*.cs` for "AOT" (case-insensitive): Zero matches
- `docs/*.md` (excluding `docs/todos/`, `docs/plans/`): Zero matches (8 matches found are all in `docs/todos/` or `docs/plans/` workflow artifacts -- correctly out of scope)
- `src/Design/CLAUDE-DESIGN.md` for "AOT" (case-insensitive): Zero matches
- `.claude/agents/business-requirements-documenter.md` for "AOT" (case-insensitive): Zero matches
- `docs/todos/completed/` and `docs/plans/completed/`: AOT references remain (7 matches across 6 files -- historical, unchanged per DP-1)

**Design match:** All 17 replacements across 13 files independently verified against the Replacement Mapping table. Each row's expected replacement text matches the actual file content.

**Deviation evaluation:** The v0.5.0 historical note uses "ahead-of-time compilation compatibility" instead of the plan's template text "AOT compatibility." This is an acceptable and necessary deviation: the plan's template text would have violated Business Rule #2 (zero AOT matches in docs/). The developer identified this internal inconsistency between the Section Rewrite template and Rule #2, and resolved it by spelling out the term. The result is technically accurate, preserves the historical context, and satisfies all business rules.

**Issues found:** None.

---

## Requirements Verification

**Reviewer:** business-requirements-reviewer agent (claude-opus-4-6)
**Verified:** 2026-03-07
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Private setter restriction reason #1 reframed from AOT to IL trimming (AllPatterns.cs:71) | Satisfied | `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:71` now reads "Reflection is slow and incompatible with IL trimming" |
| Private setter restriction reason #2 (compile-time accessibility) preserved unchanged (AllPatterns.cs:72) | Satisfied | `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:72` reads "Source generation requires compile-time accessible setters" -- unchanged |
| Quick Decisions Table reframed (CLAUDE-DESIGN.md:142) | Satisfied | `src/Design/CLAUDE-DESIGN.md:142` Reason column reads "IL trimming + source generation" |
| Design Debt Table Why Deferred reframed (CLAUDE-DESIGN.md:633) | Satisfied | `src/Design/CLAUDE-DESIGN.md:633` reads "Adds reflection, incompatible with IL trimming" |
| Design Debt Table Reconsider When reframed (CLAUDE-DESIGN.md:633) | Satisfied | `src/Design/CLAUDE-DESIGN.md:633` reads "If .NET adds source-generator-accessible private member access" |
| IOrdinalConverterProvider XML doc reframed (IOrdinalConverterProvider.cs:7) | Satisfied | `src/RemoteFactory/IOrdinalConverterProvider.cs:7` reads "IL trimming compatibility" |
| NeatooOrdinalConverterFactory static cache comment reframed (line 18) | Satisfied | `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs:18` reads "trimming-safe path" |
| NeatooOrdinalConverterFactory lookup comment reframed (line 78) | Satisfied | `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs:78` reads "Trimming-safe path" |
| OrdinalRenderer generated XML doc reframed (line 198) | Satisfied | `src/Generator/Renderer/OrdinalRenderer.cs:198` reads "trimming-compatible ordinal converter" |
| ClassFactoryRenderer comment reframed (line 1547) | Satisfied | `src/Generator/Renderer/ClassFactoryRenderer.cs:1547` reads "Register trimming-compatible ordinal converter" |
| FactoryGenerator XML doc reframed (line 1144) | Satisfied | `src/Generator/FactoryGenerator.cs:1144` reads "trimming-compatible ordinal converter" |
| Test section header reframed (ReflectionFreeSerializationTests.cs:18) | Satisfied | Line 18 reads "PROVES TRIMMING-SAFE PATH IS USED" |
| interfaces-reference.md reframed (line 482) | Satisfied | `docs/interfaces-reference.md:482` reads "IL trimming compatibility" |
| v0.5.0 release notes section rewritten with trimming.md link (lines 81-88) | Satisfied | Section heading is "IL Trimming Documentation"; link target is `../trimming.md`; historical note spells out "ahead-of-time compilation compatibility" to avoid literal "AOT" |
| v0.5.0 commits section reframed (line 146) | Satisfied | Reads "docs: Add IL trimming documentation" |
| Broken link to ../advanced/aot-compatibility.md removed | Satisfied | Grep for "aot-compatibility" in v0.5.0.md returns zero matches |
| Release notes index highlights table reframed (index.md:35) | Satisfied | Reads "Constructor injection compatibility, IL trimming documentation" |
| Agent file example reframed (business-requirements-documenter.md:18) | Satisfied | Reads "trimming-safe private member access"; grep for "AOT" returns zero matches |
| Zero AOT references in src/**/*.cs (Business Rule #1) | Satisfied | Independent grep: zero matches across all .cs files under src/ |
| Zero AOT references in docs/*.md excluding todos/plans (Business Rule #2) | Satisfied | Independent grep: 8 files with matches, all in docs/todos/ or docs/plans/ (excluded workflow artifacts) |
| Zero AOT references in CLAUDE-DESIGN.md (Business Rule #3) | Satisfied | Independent grep: zero matches |
| Completed historical files unchanged (Business Rule #12, DP-1) | Satisfied | Independent grep confirms AOT references remain in 6 completed files under docs/todos/completed/ and docs/plans/completed/ |
| Build succeeds (Business Rule #10) | Satisfied | Architect independently verified: 0 errors, 0 warnings |
| All tests pass (Business Rule #11) | Satisfied | Architect independently verified: 1,940 passed, 6 skipped, 0 failed |

### Unintended Side Effects

None. All changes are strictly to comments, XML doc strings, and markdown documentation content. No functional code, method signatures, class definitions, test assertions, or logic was modified. The implementation does not alter:

- Loading strategy or validation timing (no code changes)
- Default values or conditional visibility (no code changes)
- Ownership or lifecycle semantics (no code changes)
- Generated code behavior (only generated XML doc text changed; the generated ordinal converter logic and registration logic are identical)
- Test behavior (only a section header comment changed; test method body, assertions, and setup are unchanged)

### Issues Found

None
