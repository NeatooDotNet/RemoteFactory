# Docs Writer — Generator-Emitted DTO Constructor Lambdas

Last updated: 2026-03-30
Current step: Documentation complete

## Documentation Tracking

### Assessment

The "DTO Return Type Preservation" section already existed in `docs/trimming.md` (lines 229-260) covering the core deliverable: why trimming breaks DTO deserialization and how the generator solves it. The section was well-written and matched the project's documentation style.

The missing piece was the known limitation about nested DTOs (identified in the 2026-03-31 Progress Log entry in the todo). The task instructions explicitly required mentioning this: "only discovers direct return types and generic type arguments from factory methods, not nested DTO properties."

### Files Updated
| File | What Changed |
|------|-------------|
| `docs/trimming.md` | Added nested DTO limitation to "What You Need to Know" subsection within "DTO Return Type Preservation" section. Explains that the generator only discovers direct return types and generic type arguments, not DTO properties. Provides two workarounds: (1) return the nested DTO from a factory method, or (2) use `[DynamicDependency]` or `TrimmerRootDescriptor`. |

### Files Created
| File | Purpose |
|------|---------|
| (none) | No new documentation files needed — existing section was already comprehensive |

### Deliverables Skipped (N/A)
- No new "DTO Serialization and Trimming" section was created because the existing "DTO Return Type Preservation" section (lines 229-260) already covers the exact same content described in the deliverable. Renaming or restructuring was unnecessary — the existing section name is more precise and consistent with the adjacent "Factory Type Preservation" and "Authorization Types and Trimming" sections.
