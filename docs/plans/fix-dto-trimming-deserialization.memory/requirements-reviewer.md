# Requirements Reviewer -- Fix DTO Trimming Deserialization

Last updated: 2026-03-25
Current step: Post-implementation verification (Step 8B) complete

## Key Context

Single file changed: `src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs`. An `else if` branch was added to `GetTypeInfo()` that sets `CreateObject = () => Activator.CreateInstance(type)!` for non-DI types that have a public parameterless constructor. This provides a fallback for plain DTO classes whose constructor metadata is stripped by the IL trimmer.

The deviation from the plan (`else if` with constructor guard instead of plain `else`) is sound and necessary: `RecordBypassConverter<T>.Read()` re-enters the resolver via `JsonSerializer.Deserialize<T>(ref reader, _innerOptions)` for record types. Without the constructor guard, the `else` branch would call `Activator.CreateInstance` on a record type with no parameterless constructor, throwing `MissingMethodException`.

## Mistakes to Avoid

None so far.

## User Corrections

None so far.

## Requirements Verification

**Verdict:** REQUIREMENTS SATISFIED
**Date:** 2026-03-25

### Compliance Table

| # | Requirement | Source | Status | Notes |
|---|------------|--------|--------|-------|
| 1 | DI-registered types use `ServiceProvider.GetRequiredService()` for `CreateObject` | `NeatooJsonTypeInfoResolver.cs:31-36` (existing pattern) | Satisfied | The `if (this.ServiceProviderIsService.IsService(type))` branch is unchanged. DI types still go through DI. |
| 2 | Non-DI types with parameterless constructors get `Activator.CreateInstance` fallback | Plan Business Rule 2 (NEW) | Satisfied | `else if` at line 38-48 implements this exactly. Guard checks for public parameterless constructor via `type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null)`. |
| 3 | Records with parameterized constructors handled by `RecordBypassConverterFactory` | `RecordBypassConverterFactory.cs:36-57`, `CLAUDE-DESIGN.md` Quick Decisions Table | Satisfied | `RecordBypassConverterFactory.CanConvert()` claims types with no public parameterless ctor and at least one parameterized ctor. These types never reach the `else if` branch because (a) the converter claims them first, and (b) the constructor guard would exclude them anyway since they lack a public parameterless constructor. |
| 4 | Interface Factory returning records/DTOs is first-class | `CLAUDE-DESIGN.md` Quick Decisions Table: "Can Interface Factory return a record? Yes" | Satisfied | The fix improves DTO support under trimming without breaking record support. |
| 5 | Anti-Pattern 9: No mixing Neatoo types with records | `CLAUDE-DESIGN.md` Anti-Pattern 9 | Not affected | The fix targets pure DTO return types, not mixed graphs. |
| 6 | Serialization contract: DI types use DI, plain types use standard STJ | `docs/serialization.md` line 5 | Satisfied | The fallback to `Activator.CreateInstance` for non-DI types is the same behavior STJ's `DefaultJsonTypeInfoResolver` would provide if constructor metadata survived trimming. |
| 7 | Multi-targeting: net9.0 and net10.0 | `CLAUDE-DESIGN.md` header | Satisfied | Developer reports 2,068 tests passing across all projects and frameworks. |
| 8 | Design Debt table not violated | `CLAUDE-DESIGN.md` Design Debt table | Satisfied | No deferred feature is being implemented. This is a bug fix for existing first-class functionality. |
| 9 | `RecordBypassConverter.Read()` re-entry safe | Deviation analysis | Satisfied | The `else if` guard (vs plan's `else`) correctly prevents `Activator.CreateInstance` from being called on record types during re-entry. Record types have no public parameterless constructor, so the guard returns false and `CreateObject` remains null, allowing STJ's standard parameterized constructor path to handle them. |

### Unintended Side Effects

**None identified.** The change is narrowly scoped:

1. **No effect on generated code** -- No generator changes. The resolver is runtime-only.
2. **No effect on serialization contracts** -- DI types still use DI. Records still use `RecordBypassConverterFactory`. The fallback only activates for types that previously would have failed (trimmed constructor metadata).
3. **No effect on Design project tests** -- Design projects demonstrate correct patterns and continue to work.
4. **No effect on published docs accuracy** -- `docs/serialization.md` states "RemoteFactory replaces [Activator.CreateInstance] with ServiceProvider.GetRequiredService()." This remains true for DI types. The new code adds `Activator.CreateInstance` back only for non-DI types, which is consistent with the documented intent (DI types get DI resolution, everything else falls through to standard STJ).
5. **Trimming analyzer is disabled** (`EnableTrimAnalyzer: false`) for the RemoteFactory project, consistent with existing `Activator.CreateInstance` usage elsewhere in the codebase (`NeatooOrdinalConverterFactory.cs:110`, `RecordBypassConverterFactory.cs:65`).

### Issues Found

None.
