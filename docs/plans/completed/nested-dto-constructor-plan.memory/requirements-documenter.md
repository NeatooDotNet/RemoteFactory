# Requirements Documenter -- Nested DTO Constructor Discovery

Last updated: 2026-03-30
Current step: Requirements Documented

## Key Context

The plan resolves a documented design debt item: "Nested DTO discovery for trimming." The generator now recursively walks public instance properties (including inherited via base type chain) of discovered DTO types to find nested DTOs that need `DtoConstructorRegistry.Register<T>()` calls. Same eligibility criteria apply. Cycle detection via `HashSet<string>`. 15 new tests pass, all 2082 tests pass.

Four documentation locations were updated to reflect the resolved limitation.

## Mistakes to Avoid

None encountered on this run.

## User Corrections

None received.

## Documentation Tracking

### Expected Deliverables

1. Update "Known limitation" paragraph in CLAUDE-DESIGN.md DTO Constructor Registry section
2. Remove Design Debt table entry for nested DTO discovery
3. Update Quick Decisions Table entry about nested DTO deserialization failures
4. Update docs/trimming.md paragraph about nested DTOs not being discovered

### Requirements Documentation Updated

| File | What Changed | Why |
|------|-------------|-----|
| `src/Design/CLAUDE-DESIGN.md` line 598 | Changed "Known limitation" paragraph to "Nested DTO discovery" paragraph describing recursive property walking, collection/nullable unwrapping, inheritance traversal, and cycle detection | Resolves limitation documented by BR-NEST-001 through BR-NEST-009 |
| `src/Design/CLAUDE-DESIGN.md` Design Debt table | Removed "Nested DTO discovery for trimming" row | Design debt resolved by this implementation |
| `src/Design/CLAUDE-DESIGN.md` Quick Decisions Table line 157 | Updated answer from "Return it from a factory method or register manually" to "Check that it is reachable as a public property of a discovered DTO; if not, return from factory method or register manually" | Nested DTOs are now auto-discovered; workarounds only needed for truly unreachable types |
| `docs/trimming.md` lines 260-264 | Changed "Nested DTOs are not automatically discovered" to "Nested DTOs are automatically discovered" with description of recursive walking, added concrete example, updated fallback guidance | Resolves limitation; matches CLAUDE-DESIGN.md update |

### Developer Deliverables

No source code changes needed. The developer has already implemented the feature and all tests pass. The documentation updates are complete within the directly-editable files.

### Step 8 Part B Needed?

Release notes will be needed for this feature when a version is released that includes it. However, this is a minor version bump item (new feature, no breaking changes) and should be bundled with other changes in the next release. No immediate release notes, README changes, migration guide, or architecture docs updates are needed.

No general documentation deliverables identified -- Step 8 Part B can be skipped.
