# Requirements Reviewer -- Generator-Emitted DTO Constructor Lambdas

Last updated: 2026-03-30
Current step: Post-implementation requirements verification (Step 7B) complete

## Key Context

- First run of this agent on this plan (no prior memory file existed)
- All verification was done by reading source code directly, not relying solely on developer/architect evidence
- Generated files in `obj/` were not available (transient build artifacts), so generated code behavior was verified through renderer source code and passing tests

## Mistakes to Avoid

- None yet (first run)

## User Corrections

- None yet (first run)

## Requirements Verification

**Verdict:** REQUIREMENTS SATISFIED
**Date:** 2026-03-30

### Compliance Table

| # | Requirement | Source | Status | Notes |
|---|------------|--------|--------|-------|
| 1 | Trimming-Safe Registration Pattern -- generator emits static references the trimmer preserves | `CLAUDE-DESIGN.md` "Trimming-Safe Factory Registration" section | Satisfied | `DtoConstructorRegistry.Register<T>(() => new T())` creates a static reference. `[DynamicallyAccessedMembers(All)]` on `T` parameter provides additional trimmer annotation. Follows the exact same strategy as `NeatooFactoryRegistrarAttribute`. |
| 2 | Auth Type Auto-Registration Precedent -- registration happens in `FactoryServiceRegistrar` | `CLAUDE-DESIGN.md` "Auth Type Auto-Registration for Trimming" section | Satisfied | All three renderers emit `DtoConstructorRegistry.Register` calls inside `RenderFactoryServiceRegistrar`, after auth type registrations. Verified in `InterfaceFactoryRenderer.cs:480-488`, `ClassFactoryRenderer.cs:1527-1535`, `StaticFactoryRenderer.cs:148-156`. |
| 3 | NeatooJsonTypeInfoResolver CreateObject Pattern -- Activator.CreateInstance replaced | `NeatooJsonTypeInfoResolver.cs:29-48` (pre-change) | Satisfied | `Activator.CreateInstance` branch fully replaced with `DtoConstructorRegistry.TryCreate` at line 33-38. No `Activator.CreateInstance`, no `BindingFlags`, no `System.Reflection` import remains. |
| 4 | RecordBypassConverterFactory Exclusion -- records excluded from DTO registration | `RecordBypassConverterFactory.cs:36-57` | Satisfied | `DiscoverDtoReturnTypes` at `FactoryGenerator.Types.cs:927-939` checks for public parameterless constructor. Records with only parameterized constructors are excluded. `RecordBypassConverterFactory.CanConvert()` logic unchanged (still claims types with no parameterless ctor + at least one parameterized ctor). |
| 5 | Interface Factory Returns Non-Neatoo Types -- all three patterns scanned | `AllPatterns.cs:204-230`; `CLAUDE-DESIGN.md` Quick Decisions | Satisfied | All three model types (`ClassFactoryModel`, `InterfaceFactoryModel`, `StaticFactoryModel`) have `DtoReturnTypes` property. `FactoryModelBuilder` passes `typeInfo.DtoReturnTypes.ToList()` through for all three patterns. |
| 6 | No Reflection Policy -- reflection usage reduced | `CLAUDE.md` global instructions | Satisfied | `Activator.CreateInstance` call removed from `NeatooJsonTypeInfoResolver.cs`. `System.Reflection` import removed. No new reflection introduced. Net improvement in reflection posture. |
| 7 | Design Project as Source of Truth -- Design tests still pass | `CLAUDE.md`; Design project tests | Satisfied | 2142 tests passed, 0 failures (architect independently verified). `InterfaceFactory_GetAllAsync_ReturnsDataFromServer` and `InterfaceFactory_GetRecordByIdAsync_ReturnsRecordFromServer` both pass. Tests are unmodified (verified by reading `InterfaceFactoryTests.cs`). |
| 8 | Generator Pipeline Architecture -- DTO discovery in transform phase where ITypeSymbol is available | `FactoryGenerator.Types.cs:676-688` | Satisfied | `DiscoverDtoReturnTypes` is called in `MethodInfo` constructor at line 761, where `methodSymbol.ReturnType` is an `ITypeSymbol`. Discovered types flow as `EquatableArray<string>` through `TypeInfo` -> `FactoryModelBuilder` -> model -> renderer. |
| 9 | EquatableArray for incremental caching | `FactoryGenerator.Types.cs` | Satisfied | Both `MethodInfo.DtoReturnTypes` (line 810) and `TypeInfo.DtoReturnTypes` (line 318) use `EquatableArray<string>`, preserving incremental generator caching. |
| 10 | Fully qualified type names in generated code | Generated code patterns | Satisfied | `DiscoverDtoReturnTypes` uses `SymbolDisplayFormat.FullyQualifiedFormat` (line 941), producing `global::Namespace.TypeName` format. This ensures unambiguous type references. |
| 11 | `using Neatoo.RemoteFactory.Internal` in generated code | `FactoryGenerator.Types.cs:162` | Satisfied | The using statement is always included in generated files, so `DtoConstructorRegistry` resolves without a `global::` prefix in the renderer emission. |
| 12 | No Design Debt violations | `CLAUDE-DESIGN.md` Design Debt table | Satisfied | None of the five design debt items relate to DTO constructor registration. No deliberately deferred feature is being implemented. |
| 13 | No anti-pattern violations | `CLAUDE-DESIGN.md` Anti-Patterns 1-9 | Satisfied | Implementation does not touch any of the documented anti-pattern areas. No changes to `[Remote]` behavior, property setters, method visibility, or serialization reference handling. |

### Unintended Side Effects

None detected.

1. **Generated code for existing factory patterns:** The renderers only emit `DtoConstructorRegistry.Register` calls when `model.DtoReturnTypes.Count > 0`. Class factories returning their own `[Factory]`-annotated types and static factories returning primitives correctly produce empty `DtoReturnTypes` lists, so no unnecessary code is emitted.

2. **Serialization contracts:** The only change to the serialization path is replacing `Activator.CreateInstance` with `DtoConstructorRegistry.TryCreate` in `NeatooJsonTypeInfoResolver`. The DI-based `CreateObject` path (for Neatoo types) is completely unchanged. The `RecordBypassConverterFactory` is completely unchanged.

3. **Design project tests:** All tests unmodified and passing. The `InterfaceFactoryTests` exercise the exact DTO round-trip path that this feature fixes.

4. **Fallback behavior change:** The old code would attempt `Activator.CreateInstance` for any type with a public parameterless constructor. The new code only sets `CreateObject` for types explicitly registered by the generator. For types not in DI and not in the registry, STJ falls through to its default behavior. This is a deliberate and documented design decision (Business Rule #6 from the plan) that produces clearer error messages under trimming.

5. **Published docs accuracy:** No published docs were changed. The todo notes a Step 9 deliverable to update `docs/trimming.md` and `CLAUDE-DESIGN.md`. This is future work, not a side effect.

### Issues Found

None.

### Known Limitation (Not a Violation)

Nested DTOs (DTO properties that are themselves DTOs) are NOT discovered recursively. The progress log in the todo (2026-03-31) documents a production issue in zTreatment where `AdminClinicAssignment` (a property type on `AdminUserListItem`) was not registered. This is an acknowledged gap tracked separately -- it is not a violation of any existing documented requirement, since the DTO discovery scope was a Gap (Gap #1) that the architect defined as covering direct return types and generic type arguments only.
