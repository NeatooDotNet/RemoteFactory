# Architect -- Generator DTO Constructor Emission

Last updated: 2026-03-25
Current step: Step 4 complete -- plan created, ready for developer review (Step 5)

## Key Context

### Pipeline Architecture (verified by reading actual source)

The critical insight: `ITypeSymbol` is available in the `MethodInfo` constructor at `FactoryGenerator.Types.cs:666-698` where `methodSymbol.ReturnType` is used. By line 676, it's converted to a string (`this.ReturnType = methodSymbol.ReturnType.ToString()`). DTO analysis must happen BEFORE this point.

The `MethodInfo` constructor already unwraps `Task<T>` at lines 680-688 by checking `returnTypeSymbol.Name == "Task"` and extracting `returnTypeSymbol.TypeArguments.First()`. The DTO discovery can piggyback on this same logic but must preserve the `ITypeSymbol` for deeper analysis.

### Data Flow Path (verified)

```
MethodInfo constructor (ITypeSymbol) --> MethodInfo.DtoReturnTypes (strings)
    |
TypeInfo constructor aggregates --> TypeInfo.DtoReturnTypes (deduplicated strings)
    |
FactoryModelBuilder.Build*() passes through --> Model.DtoReturnTypes
    |
Renderer.RenderFactoryServiceRegistrar emits --> DtoConstructorRegistry.Register<T>(() => new T())
```

### Three Model Types Need DtoReturnTypes

- `ClassFactoryModel` (constructor at `FactoryModelBuilder.cs:265`)
- `InterfaceFactoryModel` (constructor at `FactoryModelBuilder.cs:145`)
- `StaticFactoryModel` (constructor at `FactoryModelBuilder.cs:92`)

### ExampleDto Discovery Path

`IExampleRepository.GetAllAsync()` returns `Task<IReadOnlyList<ExampleDto>>`.

In the `MethodInfo` constructor:
1. `methodSymbol.ReturnType` is `Task<IReadOnlyList<ExampleDto>>`
2. Unwrap Task -> `IReadOnlyList<ExampleDto>` (ITypeSymbol)
3. This is a generic collection implementing `IEnumerable<ExampleDto>`
4. Unwrap collection -> `ExampleDto` (ITypeSymbol)
5. `ExampleDto` has public parameterless constructor, no `[Factory]` attribute, not a primitive
6. Result: `"Design.Domain.FactoryPatterns.ExampleDto"` added to DtoReturnTypes

### EquatableArray Constraint

`EquatableArray<T>` requires `T : IEquatable<T>`. `string` satisfies this. Using `EquatableArray<string>` for `DtoReturnTypes` is correct for the incremental generator pipeline.

### RecordBypassConverterFactory Rule (verified)

From `RecordBypassConverterFactory.cs:36-57`: `!hasParameterlessCtor && hasParameterizedCtor`. This is the exact inverse of our DTO discovery rule. DTOs must have a parameterless constructor; records don't. No overlap.

### Interface Factory Methods in Transform

For interface factories, `defaultFactoryOperations` includes `FactoryOperation.Execute` (line 152 of `FactoryGenerator.Types.cs`), so all interface methods go through `TypeFactoryMethods`. The `MethodInfo` constructor runs for each, giving us access to `ITypeSymbol` for all interface methods.

## Mistakes to Avoid

1. **Do NOT add DTO discovery in FactoryModelBuilder** -- `ReturnType` is already a string there. Must happen in `MethodInfo` constructor where `ITypeSymbol` is available. (This was the developer's prior feedback, confirmed by reading the code.)

2. **Do NOT over-engineer** -- This is the second attempt. The first plan was too complex. Keep it simple: static dictionary, string-keyed discovery, emitted registrations in existing `FactoryServiceRegistrar`.

3. **Do NOT supplement Activator.CreateInstance -- REPLACE it** -- The user explicitly wants the `Activator.CreateInstance` fallback removed, not kept alongside the registry. It provides false safety since it fails under trimming anyway.

4. **Do NOT modify FactoryGenerationUnit** -- The todo's Requirements Review mentioned it, but the data can flow through the existing TypeInfo -> Model path without changing the sealed record.

## User Corrections

None yet (Step 4 -- no user feedback on plan yet).

## Architectural Verification (Pre-Handoff)

### Scope Table

| Claim | Evidence | Status |
|-------|----------|--------|
| `MethodInfo` constructor has `ITypeSymbol` access | `FactoryGenerator.Types.cs:676` -- `methodSymbol.ReturnType` is `ITypeSymbol` before `.ToString()` | Verified |
| `EquatableArray<string>` works for pipeline | `EquatableArray<T>` constraint is `IEquatable<T>`; `string` satisfies | Verified |
| Auth type registration is the precedent | `ClassFactoryRenderer.cs:1498-1524` and `InterfaceFactoryRenderer.cs:452-477` emit `services.TryAddTransient` in `FactoryServiceRegistrar` | Verified |
| `RecordBypassConverterFactory` excludes records | `RecordBypassConverterFactory.cs:56-57`: `!hasParameterlessCtor && hasParameterizedCtor` | Verified |
| `ExampleDto` has public parameterless constructor | `AllPatterns.cs:493-497`: `public class ExampleDto { public int Id { get; set; } ... }` | Verified |
| `ExampleRecordResult` has no parameterless constructor | `AllPatterns.cs:510`: `public record ExampleRecordResult(int Id, string Name)` | Verified |
| `NeatooJsonTypeInfoResolver` Activator.CreateInstance at line 47 | `NeatooJsonTypeInfoResolver.cs:47`: `jsonTypeInfo.CreateObject = () => Activator.CreateInstance(type)!;` | Verified |
| Design tests exist for both DTO and record round-trip | `InterfaceFactoryTests.cs:35` and `:96` | Verified |

### Breaking Changes

- Removing `Activator.CreateInstance` fallback means types NOT discovered by the generator AND NOT in DI will no longer get a `CreateObject` set. This is intentional -- such types were already failing under trimming anyway. Non-trimmed environments will still work because STJ's `DefaultJsonTypeInfoResolver` discovers constructors via reflection when metadata is available.

### Verification Resources Used

- Design project (`src/Design/`) -- confirmed `ExampleDto` and `ExampleRecordResult` definitions
- Design tests -- confirmed test methods exist and validate DTO/record round-trip
- Generator source -- confirmed pipeline data flow and `ITypeSymbol` availability
