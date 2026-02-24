# Focus Documentation Snippets

**Status:** Complete
**Priority:** Medium
**Created:** 2026-02-02
**Last Updated:** 2026-02-02

---

## Problem

Documentation snippets currently include full class definitions with ceremony (XML doc comments, full method implementations, complete class structure, IFactorySaveMeta boilerplate). This makes side-by-side comparisons verbose and harder to scan.

Additionally, some documents show multiple snippet variants (e.g., `snippet-1` versions) that appear to be duplicates or alternate implementations, adding redundancy.

## Solution

Apply the "focused snippets" pattern from KnockOff: strip each snippet to its essential 2-5 lines showing only the API usage and an explanatory comment. No class ceremony, no full implementations, no duplicate variants.

**Focused snippet characteristics:**
- Strip: XML doc comments, full class definitions, IFactorySaveMeta properties, complete method bodies
- Keep: The attribute pattern + minimal method signature showing the concept
- One concept per snippet
- Remove duplicate/alternate snippets where possible

---

## Plans

---

## Documents to Focus (11 files, ~180 snippets)

Work document-by-document, updating both the markdown file and corresponding C# sample file in `src/docs/reference-app/`.

### High Priority (Core Guides)
- [x] `docs/attributes-reference.md` (25 snippets) - DONE: 25 snippets reduced to 2-8 lines each
- [x] `docs/factory-operations.md` (24 snippets) - DONE: snippets reduced to 3-17 lines each
- [x] `docs/events.md` (21 snippets) - DONE: consolidated duplicates, reduced to 2-8 lines each
- [x] `docs/save-operation.md` (19 snippets) - DONE: reduced to 3-15 lines each, removed 6 duplicates

### Medium Priority (Configuration & Architecture)
- [x] `docs/service-injection.md` (16 snippets) - DONE: reduced to 6-18 lines each
- [x] `docs/authorization.md` (15 snippets) - DONE: reduced to 4-15 lines each, removed duplicates
- [x] `docs/aspnetcore-integration.md` (15 snippets) - DONE: reduced to 6-27 lines each, removed all duplicates
- [x] `docs/serialization.md` (14 snippets) - DONE: reduced to 2-25 lines each, consolidated duplicates
- [x] `docs/factory-modes.md` (13 snippets) - DONE: reduced to 2-35 lines each, removed all duplicates
- [x] `docs/interfaces-reference.md` (13 snippets) - DONE: 64% reduction, removed all duplicates

### Lower Priority (Getting Started)
- [x] `docs/getting-started.md` (5 snippets) - DONE: reduced from 368 to 82 lines, removed all duplicates

---

## Workflow

Process documents **sequentially**, spawning a **fresh docs-code-samples agent per file**.

### Orchestrator Instructions

For each unchecked document in the list below:

1. Spawn a fresh `docs-code-samples` agent with this prompt:
   ```
   Focus the code snippets in [DOCUMENT_PATH] to be minimal and scannable.

   Goal: Strip each snippet to 2-5 essential lines showing API usage + explanatory comment.
   Remove: XML doc comments, full class definitions, IFactorySaveMeta boilerplate, duplicate snippet variants.

   Steps:
   1. Read the markdown file to identify all snippets
   2. Find corresponding C# sample files in src/docs/reference-app/
   3. Update #region markers to isolate only essential lines
   4. Remove duplicate/alternate snippet variants where possible
   5. Run: mdsnippets
   6. Build and test: dotnet build src/docs/reference-app/EmployeeManagement.sln && dotnet test src/docs/reference-app/EmployeeManagement.sln
   7. Report what was changed
   ```

2. Wait for agent completion
3. Mark document as complete in this todo
4. Proceed to next document

### Per-Document Agent Tasks

Each agent handles one document completely before the next begins.

---

## Example Transformation

### Before (verbose)
```cs
/// <summary>
/// Employee with constructor-based Create operation.
/// </summary>
[Factory]
public partial class EmployeeWithConstructorCreate
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    /// <summary>
    /// Parameterless constructor marked as Create operation.
    /// </summary>
    [Create]
    public EmployeeWithConstructorCreate()
    {
        Id = Guid.NewGuid();
    }
}
```

### After (focused)
```cs
// Parameterless constructor marked as Create operation
[Create]
public Employee()
{
    Id = Guid.NewGuid();
}
```

---

## Progress Log

---

## Results / Conclusions

**Completed:** 2026-02-02

All 11 documentation files processed sequentially with fresh docs-code-samples agents per file.

### Summary

| Document | Snippets | Result |
|----------|----------|--------|
| attributes-reference.md | 25 | Reduced to 2-8 lines each |
| factory-operations.md | 24 | Reduced to 3-17 lines each |
| events.md | 21 | Reduced to 2-8 lines, consolidated duplicates |
| save-operation.md | 19 | Reduced to 3-15 lines, removed 6 duplicates |
| service-injection.md | 16 | Reduced to 6-18 lines each |
| authorization.md | 15 | Reduced to 4-15 lines, removed duplicates |
| aspnetcore-integration.md | 15 | Reduced to 6-27 lines, removed all duplicates |
| serialization.md | 14 | Reduced to 2-25 lines, consolidated duplicates |
| factory-modes.md | 13 | Reduced to 2-35 lines, removed all duplicates |
| interfaces-reference.md | 13 | 64% reduction, removed all duplicates |
| getting-started.md | 5 | Reduced from 368 to 82 lines |

### Key Improvements

1. **Eliminated duplicate snippet variants** - Each snippet now has a single source
2. **Removed XML doc comments** - Explanatory text in markdown, not code
3. **Stripped class boilerplate** - Focus on attribute usage and API patterns
4. **All tests pass** - 48-52 tests across net8.0, net9.0, net10.0
