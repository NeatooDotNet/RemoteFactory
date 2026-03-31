# Requirements Documenter -- Generator-Emitted DTO Constructor Lambdas

Last updated: 2026-03-30
Current step: Requirements Documented

## Key Context

The implementation added a `DtoConstructorRegistry` static registry for DTO constructor lambdas and modified the generator to emit `Register<T>(() => new T())` calls in `FactoryServiceRegistrar` for plain DTO return types. `NeatooJsonTypeInfoResolver` now uses the registry instead of `Activator.CreateInstance`. Records are excluded (handled by `RecordBypassConverterFactory`).

A known limitation was identified in production (zTreatment, 2026-03-31 progress log): nested DTOs (properties of discovered DTOs) are not automatically registered. This is tracked as design debt.

## Mistakes to Avoid

None on this run.

## User Corrections

None on this run.

## Documentation Tracking

### Expected Deliverables

Per plan acceptance criteria: "Update `CLAUDE-DESIGN.md` to document the DTO constructor registry pattern alongside the existing Trimming-Safe Factory Registration section."

### Requirements Documentation Updated

| File | What Changed | Why |
|------|-------------|-----|
| `src/Design/CLAUDE-DESIGN.md` | Added "DTO Constructor Registry for Trimming" section after "Trimming-Safe Factory Registration" | Documents the new pattern: what it does, why, discovery criteria table, unwrapping behavior, `[DynamicallyAccessedMembers(All)]`, idempotent duplicates, and known limitation (nested DTOs). Traced to plan Business Rules 1-6, 10. |
| `src/Design/CLAUDE-DESIGN.md` | Added 2 entries to Quick Decisions Table: "Do I need to register DTOs for IL trimming?" and "What if my nested DTO fails to deserialize under trimming?" | Common questions that arise from this feature. Traced to plan Business Rules 1, 5, 6. |
| `src/Design/CLAUDE-DESIGN.md` | Added "Nested DTO discovery for trimming" row to Design Debt table | Known limitation found in zTreatment production (todo progress log 2026-03-31). |
| `src/Design/CLAUDE-DESIGN.md` | Bumped design_version from 1.2 to 1.3, last_updated to 2026-03-30 | Reflects documentation update. |

### Developer Deliverables

None. The `docs/trimming.md` "DTO Return Type Preservation" section (lines 229-260) already accurately documents the implemented behavior. No source code (.cs) documentation changes are needed -- the `DtoConstructorRegistry.cs` file already has appropriate XML doc comments.

### Step 8 Part B Needed?

The todo's progress log (2026-03-25) mentions a documentation deliverable: "Add a 'DTO Serialization and Trimming' section to `docs/trimming.md`." This was already completed -- `docs/trimming.md` has the "DTO Return Type Preservation" section at lines 229-260.

Release notes will be needed when this feature ships (it's part of a version bump), but that's handled by the release process, not this documentation step.

No general documentation deliverables identified -- Step 8 Part B can be skipped.
