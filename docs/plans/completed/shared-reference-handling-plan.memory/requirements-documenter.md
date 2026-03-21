# Requirements Documenter Memory - Shared Reference Handling

**Date:** 2026-03-21
**Plan:** shared-reference-handling-plan.md
**Todo:** shared-reference-handling-non-custom-types.md

## Work Completed

### Step 2: Categorize Changes

Reviewed all 13 business rules in the plan. Categorization:

| Rule | Source | Category | Action |
|------|--------|----------|--------|
| 1-6 | Phase 1 exploration (existing behavior documentation) | Existing/baseline | No docs update needed -- these document pre-fix behavior |
| 7 | NEW -- shared Dictionary identity for mutable types | New rule | Updated docs/serialization.md, CLAUDE-DESIGN.md |
| 8 | NEW -- RecordBypassConverterFactory for parameterized-constructor types | New rule | Updated docs/serialization.md, CLAUDE-DESIGN.md, verified appendix |
| 9 | NEW -- circular reference support for mutable types | New rule | Updated docs/serialization.md |
| 10 | NEW -- cross-type shared references (Neatoo + non-Neatoo) | New rule | Updated docs/serialization.md |
| 11 | NEW -- record with nested mutable type (DDD semantics) | New rule | Updated docs/serialization.md, verified appendix |
| 12 | NEW -- ordinal format coexistence | New rule | No specific doc update needed (coexistence is implicit) |
| 13 | Sacred tests -- zero regressions | Existing constraint | No doc update needed |

### Step 3: Files Directly Updated

1. **`src/Design/CLAUDE-DESIGN.md`**
   - Anti-Pattern 9 "Why it matters": Replaced stale "no ReferenceHandler set / converter-level concern" with two-path strategy (NeatooPreserveReferenceHandler + RecordBypassConverterFactory)
   - Quick Decisions Table record row: Changed "serialized without `$id`/`$ref`" to "bypass reference handling (`RecordBypassConverterFactory`)"
   - Common Mistakes #10: Changed "cause serialization mismatches" to "bypass reference handling entirely"

2. **`docs/serialization.md`**
   - Replaced entire "Scope: Converter-Level, Not Serializer-Level" subsection with "Reference Handling by Type Category"
   - New content: three-row type category table (Neatoo types / mutable reference types / parameterized constructors), shared resolver explanation, nested-mutable-in-record DDD rationale, cross-link to appendix

3. **`docs/appendix/serialization.md`**
   - Updated section 3 ("Shared Object Identity Is Lost"): added paragraph clarifying scope of reference tracking (Neatoo types + mutable reference types yes, records no) with cross-link to record-reference-handling appendix

4. **`docs/appendix/record-reference-handling.md`**
   - Already existed and is comprehensive. Verified it matches implementation. No changes needed.

5. **`docs/plans/shared-reference-handling-plan.md`**
   - Set status to "Requirements Documented"
   - Updated Documentation deliverables checklist (marked CLAUDE-DESIGN.md item as done)
   - Added "Requirements Documentation (Step 9)" section with files updated and developer deliverables

### Step 4: Developer Deliverables (NOT directly edited)

1. **`src/Design/Design.Tests/FactoryTests/SerializationTests.cs:38`** -- Stale comment. Line 38 says "Circular references without proper handling" under the "NO" list. Should be updated because circular references in mutable types now work via NeatooPreserveReferenceHandler. Suggested change: move to "PARTIAL" section with note that circular references work for mutable types but not for types with parameterized constructors (records).

2. **(Optional) Design.Tests shared-reference test** -- Consider adding a test demonstrating shared mutable object identity preservation on a [Factory] class. Would close Gap 10 from the requirements review. Not blocking since IntegrationTests/SharedReferenceTests.cs covers the scenarios.

### Stale Docs Identified by Reviewer -- Resolution Status

| Location | Issue | Resolution |
|----------|-------|------------|
| `docs/serialization.md:120-124` | "Scope" section says "no ReferenceHandler set" | FIXED -- replaced with "Reference Handling by Type Category" |
| `src/Design/CLAUDE-DESIGN.md:419` | Anti-Pattern 9 references converter-level mechanism | FIXED -- updated to two-path strategy |
| `src/Design/Design.Tests/FactoryTests/SerializationTests.cs:38` | "Circular references without proper handling" partially stale | DEVELOPER DELIVERABLE -- listed for developer agent |

### Design Debt and Completeness

- No Design Debt items resolved (this feature was not in the Design Debt table)
- No Design Completeness Checklist items affected
- No anti-patterns added or removed -- Anti-Pattern 9 updated in-place (same rule, updated explanation)
