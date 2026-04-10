# Fix Missing global:: Namespace Qualifier in Generated Code

**Date:** 2026-03-26
**Related Todo:** [Fix Missing global:: Namespace Qualifier](../../todos/completed/fix-global-namespace-qualifier.md)
**Status:** Complete
**Last Updated:** 2026-03-26

---

## Overview

The RemoteFactory source generator emits fully-qualified type references without the `global::` prefix. This causes compilation errors when a class name in scope matches a namespace segment. For example, a class `PersonModel` in namespace `Person.DomainModel` that references type `Person.Ef.PersonEntity` -- C# resolves the leading `Person` as the class `PersonModel` (since `Person` is a valid namespace prefix AND could be a type prefix), producing errors like `'Person' does not contain a definition for 'Ef'`.

The fix is to emit `global::` on all fully-qualified type references in generated code, which is standard best practice for Roslyn source generators.

---

## Difficulty & Risk Assessment

**Difficulty: Low-Medium**
- The changes are mechanical: add `global::` prefix or switch to `FullyQualifiedFormat` at each identified location
- No new architecture, no new abstractions, no behavioral changes
- The fix locations are well-identified and isolated to the generator's extraction and rendering phases

**Risk: Low**
- `global::` is a safe, additive prefix -- it never changes semantics for correct code, it only resolves ambiguities
- Existing tests will continue to compile and pass (the prefix doesn't break anything)
- The only risk is missing a location, which would leave the bug for that specific code path

---

## Business Requirements Context

**Source:** Todo's Requirements Review section (skipped per workflow -- this is an open-source library, not a business application; the "requirements" are the API contracts and generated code correctness)

### Relevant Existing Requirements

#### Generator Behavior Contract
- `src/Design/CLAUDE-DESIGN.md` Section "Trimming-Safe Factory Registration": Assembly attribute emits `typeof({Namespace}.{ClassName}Factory)`. This typeof expression must resolve unambiguously.
- `src/Design/CLAUDE-DESIGN.md` Section "Properties Need Public Setters": Ordinal serialization generates code referencing property types. These type references must resolve unambiguously.

#### Existing Tests
- All existing integration tests (`src/Tests/RemoteFactory.IntegrationTests/`) and unit tests (`src/Tests/RemoteFactory.UnitTests/`) define expected behavior for generated code. These tests must continue to pass after the fix.
- The Person example project (`src/Examples/Person/`) currently fails to build due to this bug. After the fix, it must build successfully.
- Design project tests (`src/Design/Design.Tests/`) must continue to pass.

### Gaps

No documented requirement currently states that generated code must use `global::` qualified names. This plan establishes that as a new rule.

### Contradictions

None. Adding `global::` is purely additive and cannot conflict with existing requirements.

---

## Business Rules (Testable Assertions)

1. WHEN the generator emits a fully-qualified type reference in `typeof()` expressions (assembly attributes), THEN the type reference MUST be prefixed with `global::`. -- Source: NEW
2. WHEN the generator extracts property types via `ToDisplayString()` for ordinal serialization, THEN the extracted type string MUST use `SymbolDisplayFormat.FullyQualifiedFormat` (which produces `global::` prefixed types). -- Source: NEW
3. WHEN the generator builds `OrdinalSerializationModel.FullTypeName` by string concatenation, THEN the result MUST be prefixed with `global::`. -- Source: NEW
4. WHEN the generator emits DTO return type references in `DtoConstructorRegistry.Register<T>()` calls, THEN type T MUST be prefixed with `global::` (stop stripping `global::` from `FullyQualifiedFormat` output). -- Source: NEW
5. WHEN the generator extracts a constructor return type via `ToDisplayString()`, THEN the type string MUST use `FullyQualifiedFormat`. -- Source: NEW
6. WHEN the generator emits `{unit.Namespace}.{TypeName}` patterns in assembly attributes or other rendered code, THEN the combined string MUST be prefixed with `global::`. -- Source: NEW
7. WHEN a consumer project has a class name that matches a namespace segment (e.g., class `PersonModel` in namespace `Person.DomainModel` referencing `Person.Ef.PersonEntity`), THEN the generated code MUST compile without errors. -- Source: Person example project bug
8. WHEN the generator emits property type references in ordinal serialization code (`typeof()`, casts, generic type arguments), THEN those types MUST already include `global::` from the extraction phase (Rule 2). -- Source: NEW
9. WHEN existing test projects build and run, THEN all tests MUST continue to pass (no regressions from adding `global::` prefix). -- Source: Existing test suite

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Person example builds successfully | Build `src/Examples/Person/` solution | Rule 7 | Build succeeds with zero errors |
| 2 | Assembly attribute uses global:: prefix | Generated code for any [Factory] class | Rule 1, 6 | `typeof(global::Namespace.TypeFactory)` in assembly attribute |
| 3 | Ordinal property types have global:: prefix | Entity with property of type from another namespace | Rule 2, 8 | `typeof(global::Some.Namespace.SomeType)` in ordinal serialization code |
| 4 | DTO return types preserve global:: prefix | Interface factory returning a DTO type | Rule 4 | `DtoConstructorRegistry.Register<global::Some.Namespace.DtoType>(...)` |
| 5 | OrdinalSerializationModel.FullTypeName has global:: | Any [Factory] class with ordinal serialization | Rule 3 | `FullTypeName` starts with `global::` |
| 6 | Constructor return type has global:: prefix | Record with primary constructor | Rule 5 | ReturnType starts with `global::` |
| 7 | All existing integration tests pass | Run full test suite | Rule 9 | All tests pass on net9.0 and net10.0 |
| 8 | All existing unit tests pass | Run full test suite | Rule 9 | All tests pass on net9.0 and net10.0 |
| 9 | Design project tests pass | Run Design.Tests | Rule 9 | All 26+ tests pass |
| 10 | Built-in types are unaffected | Properties of type `int`, `string`, `DateTime` | Rule 2 | `global::` prefix does not break built-in type references (FullyQualifiedFormat handles them correctly, e.g. `int` stays `int`) |

---

## Approach

The fix targets two phases of the generator pipeline:

### Phase A: Extraction Phase (FactoryGenerator.Types.cs, FactoryGenerator.Transform.cs)

Fix the data extraction so that type strings stored in the model already include `global::`:

1. **OrdinalPropertyInfo.Type**: Switch from `ToDisplayString()` to `ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)` at line 384-386
2. **Constructor ReturnType**: Switch from `ToDisplayString()` to `ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)` at line 734
3. **DTO return types**: Stop stripping `global::` at line 898 -- the `.Replace("global::", "")` must be removed

### Phase B: Rendering Phase (Renderer/*.cs, FactoryModelBuilder.cs)

Fix where type names are assembled by string concatenation:

4. **Assembly attributes**: In all three renderers (ClassFactoryRenderer.cs:60, InterfaceFactoryRenderer.cs:48, StaticFactoryRenderer.cs:47), change `typeof({unit.Namespace}.{model.XxxTypeName}...)` to `typeof(global::{unit.Namespace}.{model.XxxTypeName}...)`
5. **OrdinalSerializationModel.FullTypeName**: In FactoryModelBuilder.cs:981, change `$"{typeInfo.Namespace}.{typeInfo.ImplementationTypeName}"` to `$"global::{typeInfo.Namespace}.{typeInfo.ImplementationTypeName}"`

### What stays the same

- Type references inside `namespace { }` blocks that use simple (unqualified) names like `model.ImplementationTypeName` or `model.ServiceTypeName` are safe because they are already within the correct namespace scope and never use namespace-qualified references
- Using statements in generated code are not affected
- BCL types in OrdinalRenderer.cs already use `global::` (e.g., `global::System.Text.Json.JsonSerializer`)

### Important note on `FullyQualifiedFormat` and built-in types

`SymbolDisplayFormat.FullyQualifiedFormat` renders built-in types with their C# keyword form (e.g., `int`, `string`, `bool`), not as `global::System.Int32`. This is safe and correct for use in `typeof()` and cast expressions.

---

## Design

### Categorization of Changes

**Category 1: Roslyn symbol to string (use FullyQualifiedFormat)**

| Location | Current Code | Fix |
|----------|-------------|-----|
| `FactoryGenerator.Types.cs:384-386` | `.ToDisplayString()` | `.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)` |
| `FactoryGenerator.Types.cs:734` | `constructorSymbol.ContainingType.ToDisplayString()` | `constructorSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)` |

**Category 2: Stop stripping global::**

| Location | Current Code | Fix |
|----------|-------------|-----|
| `FactoryGenerator.Types.cs:898` | `.Replace("global::", "")` | Remove the `.Replace(...)` call |

**Category 3: String concatenation (prepend global::)**

| Location | Current Code | Fix |
|----------|-------------|-----|
| `FactoryModelBuilder.cs:981` | `$"{typeInfo.Namespace}.{typeInfo.ImplementationTypeName}"` | `$"global::{typeInfo.Namespace}.{typeInfo.ImplementationTypeName}"` |
| `ClassFactoryRenderer.cs:60` | `typeof({unit.Namespace}.{model.ImplementationTypeName}Factory)` | `typeof(global::{unit.Namespace}.{model.ImplementationTypeName}Factory)` |
| `InterfaceFactoryRenderer.cs:48` | `typeof({unit.Namespace}.{model.ImplementationTypeName}Factory)` | `typeof(global::{unit.Namespace}.{model.ImplementationTypeName}Factory)` |
| `StaticFactoryRenderer.cs:47` | `typeof({unit.Namespace}.{model.TypeName})` | `typeof(global::{unit.Namespace}.{model.TypeName})` |

### Files to Modify

1. `src/Generator/FactoryGenerator.Types.cs` (3 changes: lines 384-386, 734, 898)
2. `src/Generator/Builder/FactoryModelBuilder.cs` (1 change: line 981)
3. `src/Generator/Renderer/ClassFactoryRenderer.cs` (1 change: line 60)
4. `src/Generator/Renderer/InterfaceFactoryRenderer.cs` (1 change: line 48)
5. `src/Generator/Renderer/StaticFactoryRenderer.cs` (1 change: line 47)

### Files NOT to Modify

- `src/Generator/Renderer/OrdinalRenderer.cs` -- This file consumes `model.FullTypeName` and `prop.Type` from the model. Once the extraction phase (Category 1) and model builder (Category 3) are fixed, OrdinalRenderer automatically gets correct `global::`-prefixed types without any changes to OrdinalRenderer itself.
- `src/Generator/Renderer/FactoryRenderer.cs` -- Entry point only; dispatches to specific renderers.
- `src/Generator/Model/*.cs` -- Model types are data carriers; they don't construct type strings.

### Downstream Impact Analysis

When `OrdinalPropertyInfo.Type` includes `global::`, every place that consumes `prop.Type` in OrdinalRenderer.cs (typeof expressions, cast expressions, type arguments) automatically gets the `global::` prefix. This is the correct cascading behavior.

When `OrdinalSerializationModel.FullTypeName` includes `global::`, every place that consumes `model.FullTypeName` in OrdinalRenderer.cs (JsonConverter generic argument, Read method return type, Write method parameter type, IOrdinalConverterProvider generic argument, CreateOrdinalConverter return type, `new` expressions) automatically gets the `global::` prefix.

When DTO return types keep their `global::` prefix, `DtoConstructorRegistry.Register<T>()` calls in all three renderers automatically get correct type arguments.

---

## Implementation Steps

### Step 1: Fix Extraction Phase (FactoryGenerator.Types.cs)

1. At line 384-386, change `OrdinalPropertyInfo.Type` extraction to use `SymbolDisplayFormat.FullyQualifiedFormat`:
   - Change `.ToDisplayString()` to `.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)`
   - Keep the existing `.WithNullableAnnotation(NullableAnnotation.NotAnnotated)` and `TrimEnd('?')` logic

2. At line 734, change constructor return type to use `FullyQualifiedFormat`:
   - Change `constructorSymbol.ContainingType.ToDisplayString()` to `constructorSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)`

3. At line 898, stop stripping `global::` from DTO return types:
   - Remove `.Replace("global::", "")` from the `dtoTypes.Add(...)` call

### Step 2: Fix Model Builder (FactoryModelBuilder.cs)

1. At line 981, prepend `global::` to `OrdinalSerializationModel.FullTypeName`:
   - Change `$"{typeInfo.Namespace}.{typeInfo.ImplementationTypeName}"` to `$"global::{typeInfo.Namespace}.{typeInfo.ImplementationTypeName}"`

### Step 3: Fix Renderers (ClassFactoryRenderer.cs, InterfaceFactoryRenderer.cs, StaticFactoryRenderer.cs)

1. In ClassFactoryRenderer.cs line 60, prepend `global::` to assembly attribute typeof:
   - Change `typeof({unit.Namespace}.{model.ImplementationTypeName}Factory)` to `typeof(global::{unit.Namespace}.{model.ImplementationTypeName}Factory)`

2. In InterfaceFactoryRenderer.cs line 48, same change

3. In StaticFactoryRenderer.cs line 47, prepend `global::` to assembly attribute typeof:
   - Change `typeof({unit.Namespace}.{model.TypeName})` to `typeof(global::{unit.Namespace}.{model.TypeName})`

### Step 4: Build and Test

1. Build the full solution: `dotnet build src/Neatoo.RemoteFactory.sln`
2. Run all tests: `dotnet test src/Neatoo.RemoteFactory.sln`
3. Build the Person example: `dotnet build src/Examples/Person/Person.Server/Person.Server.csproj` (this is the project that currently fails)
4. Build the Design project: `dotnet build src/Design/Design.Tests/Design.Tests.csproj`
5. Run Design tests: `dotnet test src/Design/Design.Tests/Design.Tests.csproj`

---

## Acceptance Criteria

- [ ] All 7 identified locations emit `global::` prefixed type references
- [ ] Person example project (`src/Examples/Person/`) builds without errors
- [ ] All existing unit tests pass (`dotnet test src/Tests/RemoteFactory.UnitTests/`)
- [ ] All existing integration tests pass (`dotnet test src/Tests/RemoteFactory.IntegrationTests/`)
- [ ] Design project tests pass (`dotnet test src/Design/Design.Tests/`)
- [ ] Generated code for any factory includes `global::` in assembly attribute typeof expressions
- [ ] Generated ordinal serialization code uses `global::` for user-defined property types
- [ ] No `global::` stripping occurs for DTO return types

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: All code changes | developer | Yes | All 7 changes are small and interdependent; best done in a single pass | None |
| Phase 2: Build and test verification | developer | No (same agent) | Same context needed for debugging any failures | Phase 1 |

**Parallelizable phases:** None -- this is a single-phase implementation.

**Notes:** The changes are few enough that a single developer agent can handle all of them efficiently. Fresh phasing is not needed for this scope.

---

## Dependencies

- Roslyn's `SymbolDisplayFormat.FullyQualifiedFormat` is available in the netstandard2.0 Roslyn APIs used by the generator
- No new NuGet packages or framework dependencies required

---

## Risks / Considerations

1. **Unit test snapshot changes**: Some unit tests may assert on the exact generated code output. These tests will need their expected output updated to include `global::` prefixes. This is expected and correct -- the tests should reflect the new (correct) behavior.

2. **FullyQualifiedFormat and nullable value types**: `SymbolDisplayFormat.FullyQualifiedFormat` renders `int?` as `int?` (not `global::System.Nullable<int>`). The existing `TrimEnd('?')` logic at line 386-389 strips the `?` to get the base type. This should continue to work correctly because the format preserves the C# keyword forms for built-in types.

3. **FullyQualifiedFormat and generic types**: For types like `List<SomeType>`, `FullyQualifiedFormat` produces `global::System.Collections.Generic.List<global::SomeNamespace.SomeType>`. This is correct and desirable -- both the outer and inner types get qualified.

4. **Documentation deliverables**: No user-facing documentation changes are needed for this bug fix. The fix is internal to the generator and transparent to users.
