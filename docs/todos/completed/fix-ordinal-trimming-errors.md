# Fix IL2026 Trimming Errors in Generated Ordinal Converters

**Status:** Complete
**Priority:** High
**Created:** 2026-03-07
**Last Updated:** 2026-03-07
**Requirements Review:** See below

## Problem

The Design.Domain project fails to build with IL2026 errors. The source generator emits ordinal converters that call `JsonSerializer.Serialize<T>()` and `JsonSerializer.Deserialize<T>()`, which are marked `[RequiresUnreferencedCode]` and are not IL-trimming-safe. With `IsTrimmable=true` and `TreatWarningsAsErrors=True`, these become build errors.

9 generated `*.Ordinal.g.cs` files are affected across all Design.Domain types (aggregates, entities, value objects, services).

## Solution

Fix the source generator (`OrdinalRenderer.cs`) to emit trim-safe serialization code instead of calling `JsonSerializer.Serialize/Deserialize<T>`. For primitive types (`int`, `decimal`, `string`, `bool`, `DateTime`, etc.), use the `Utf8JsonReader`/`Utf8JsonWriter` primitive methods directly (e.g., `reader.GetDecimal()`, `writer.WriteNumberValue()`). For complex/nested types, use the `JsonTypeInfo`-based overloads or the options-based resolver pattern.

## Scope

### Generator code
- `src/Generator/Renderer/OrdinalRenderer.cs` — `RenderReadMethod()` (line 75) and `RenderWriteMethod()` (line 122) emit the problematic `JsonSerializer.Serialize/Deserialize<T>` calls

### Generated output (9 files affected)
- `Design.Domain.ValueObjects.Money.Ordinal.g.cs`
- `Design.Domain.ValueObjects.Percentage.Ordinal.g.cs`
- `Design.Domain.Entities.OrderLine.Ordinal.g.cs`
- `Design.Domain.Entities.OrderLineList.Ordinal.g.cs`
- `Design.Domain.Aggregates.Order.Ordinal.g.cs`
- `Design.Domain.Aggregates.SecureOrder.Ordinal.g.cs`
- `Design.Domain.Services.AuditedOrder.Ordinal.g.cs`
- `Design.Domain.FactoryPatterns.ExampleClassFactory.Ordinal.g.cs`
- `Design.Domain.FactoryPatterns.ClassExecuteDemo.Ordinal.g.cs`

## Plans

- [Fix IL2026 Trimming Errors in Generated Ordinal Converters](../plans/fix-ordinal-trimming-errors.md)

## Tasks

- [x] Business requirements review (Step 2)
- [x] Architect comprehension check (Step 3)
- [x] Architect plan creation (Step 4)
- [x] Developer review (Step 5) — skipped, user directed simpler approach (Option B)
- [x] Implementation (Step 7)
- [x] Verification (Step 8) — Architect: VERIFIED, Requirements: SATISFIED
- [x] Documentation (Step 9) — N/A, internal generator change
- [x] Completion (Step 10)

## Progress Log

- 2026-03-07: Todo created. The generator's `OrdinalRenderer.RenderReadMethod()` and `RenderWriteMethod()` emit `JsonSerializer.Serialize/Deserialize<T>()` calls that trigger IL2026. Design.Domain has `IsTrimmable=true` + `TreatWarningsAsErrors=True`, promoting these to build errors.
- 2026-03-07: Business requirements review complete. APPROVED. 9 relevant requirements found across Design projects, docs, and tests. 4 gaps identified (no policy on generated code API surface, missing enum/interface test coverage, dead generator code). Zero contradictions. Key recommendations: fix both generator paths (OrdinalRenderer + dead code in FactoryGenerator), categorize property types for trim-safe API selection, preserve all existing test contracts.
- 2026-03-07: Architect comprehension check complete. Ready -- no clarifying questions.
- 2026-03-07: Architect plan created at `docs/plans/fix-ordinal-trimming-errors.md`. Two-tier design: primitives use Utf8JsonReader/Writer native methods; complex types use trim-safe JsonSerializer overloads via options.GetTypeInfo(). 26 business rules, 18 test scenarios, dead code removal included. Ready for developer review.

## Requirements Review

### Reviewer
business-requirements-reviewer agent

### Reviewed
2026-03-07

### Verdict
APPROVED

### Relevant Requirements Found

**1. Design.Domain must build with `IsTrimmable=true` and `TreatWarningsAsErrors=True`**
- `src/Design/Design.Domain/Design.Domain.csproj:9` -- `<IsTrimmable>true</IsTrimmable>`
- `src/Directory.Build.props:17` -- `<TreatWarningsAsErrors>True</TreatWarningsAsErrors>`
- These two settings together mean any `[RequiresUnreferencedCode]` call in code compiled by Design.Domain (including generated code) becomes a build error (IL2026). This is the root cause of the bug. The fix must eliminate all `[RequiresUnreferencedCode]` calls from generated ordinal converter code.

**2. Trimming documentation establishes IL trimming as a first-class project requirement**
- `docs/trimming.md` -- Comprehensive documentation of the IL trimming strategy, feature switch guards, and configuration. This confirms that generated code must be trimming-safe. The doc states the generated code uses `if (NeatooRuntime.IsServerRuntime)` guards for trimming; ordinal converters are a different category of generated code that must also be trimming-safe.
- The recently completed todo `docs/todos/completed/remove-aot-reframe-trimming.md` reframed all project justifications around IL trimming, confirming IL trimming is the project's primary compatibility target.

**3. Generated ordinal converters must remain reflection-free**
- `src/RemoteFactory/IOrdinalConverterProvider.cs:7-9` -- XML doc states: "Eliminates reflection-based converter creation for IL trimming compatibility." The `IOrdinalConverterProvider<TSelf>` interface exists specifically to provide pre-compiled converters that bypass the reflection fallback path in `NeatooOrdinalConverterFactory`.
- `src/Generator/Renderer/OrdinalRenderer.cs:198-200` -- Generated `CreateOrdinalConverter()` method XML doc says "Creates a trimming-compatible ordinal converter for this type."
- `src/Design/CLAUDE-DESIGN.md:309` -- Anti-Pattern 6 notes that the generator adds a partial class with `IOrdinalSerializable` implementation. The ordinal converter is part of this generated surface.

**4. Serialization correctness contracts (from tests)**
- `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/ReflectionFreeSerializationTests.cs` -- 16 tests define the behavioral contract for generated ordinal converters:
  - WHEN serializing a `SimpleRecord("Test", 42)` with ordinal format, THEN output is `["Test",42]` (alphabetical property order).
  - WHEN deserializing `["Test",42]`, THEN result has `Name="Test"` and `Value=42`.
  - WHEN serializing/deserializing `ComplexRecord` with `string`, `int`, `long`, `double`, `decimal`, `bool`, `DateTime`, `Guid` properties, THEN all values round-trip correctly.
  - WHEN serializing/deserializing records with nullable properties (both with values and with nulls), THEN values and nulls are preserved.
  - WHEN serializing/deserializing records with `List<string>` collections (including empty), THEN collections are preserved.
  - WHEN serializing/deserializing nested records (`OuterRecord` containing `InnerRecord`), THEN nested objects are preserved.
  - WHEN serializing null, THEN output is `"null"`.
  - WHEN deserializing malformed JSON (object instead of array), THEN `JsonException` is thrown.
  - WHEN deserializing array with too many values, THEN `JsonException` with "Too many values" message is thrown.
  - WHEN using client/server round-trip with ordinal format, THEN objects survive serialization across the boundary.

**5. Design.Tests serialization contracts**
- `src/Design/Design.Tests/FactoryTests/SerializationTests.cs` -- 7 tests define higher-level serialization contracts:
  - WHEN creating an object via client factory (remote), THEN all property values survive the round-trip (Create, Fetch).
  - WHEN value objects (Money) are serialized, THEN `Amount` and `Currency` are preserved.
  - WHEN `IFactorySaveMeta` properties (`IsNew`, `IsDeleted`) are serialized, THEN they survive the round-trip correctly.
  - WHEN collections of child entities are serialized, THEN they are properly reconstructed.

**6. Serialization format specification**
- `docs/serialization.md:18-23` -- Ordinal format serializes properties as a JSON array in alphabetical order. Named format uses standard JSON with property names. Both formats must be supported.
- `docs/serialization.md:56-72` -- Ordinal versioning: adding or renaming properties changes alphabetical order and array indices, requiring coordinated rebuild. The fix must preserve this alphabetical-order invariant.

**7. Types that must cross the serialization boundary**
- `src/Design/Design.Tests/FactoryTests/SerializationTests.cs:23-38` -- Documents which types serialize: primitives, nullable primitives, enums, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, records, `[Factory]` classes, concrete collections (`List<T>`, `Dictionary<TKey,TValue>`, arrays), and nested objects.
- The generated ordinal converters in Design.Domain handle: `decimal`, `string` (Money), `decimal` (Percentage), `int`, `bool`, `string`, `Design.Domain.Entities.IOrderLineList` (interface type), `Design.Domain.Aggregates.OrderStatus` (enum) -- a mix of primitives, enums, and complex/interface types.

**8. Two code paths exist for generating ordinal serialization**
- Active path: `src/Generator/FactoryGenerator.cs:50` calls `FactoryRenderer.RenderOrdinalSerialization()`, which calls `OrdinalRenderer.Render()`.
- Dead code: `src/Generator/FactoryGenerator.cs:896` -- `GenerateOrdinalSerialization()` method exists but is never called. It contains the same `JsonSerializer.Serialize/Deserialize<T>` pattern.
- Both paths must be fixed or the dead code must be acknowledged/removed by the architect.

**9. The `NeatooOrdinalConverter<T>` fallback converter also uses `JsonSerializer.Serialize/Deserialize`**
- `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs:205,241,262,276` -- The generic `NeatooOrdinalConverter<T>` (the reflection-based fallback in the runtime library) also uses `JsonSerializer.Serialize/Deserialize`. However, this is in the runtime library, not in generated code. It is the fallback path for types that do NOT have a generated converter. This code is NOT in the current todo's scope, but the architect should be aware that fixing the generator does not make the runtime library itself fully trim-safe.

### Gaps

**1. No documented policy on what the generated code may or may not call from `System.Text.Json`.**
There is no requirement document that specifies which `System.Text.Json` APIs the generator may emit. The current constraint is implicit: generated code must compile in assemblies with `IsTrimmable=true` and `TreatWarningsAsErrors=True`, which rules out any API marked `[RequiresUnreferencedCode]`. The architect should formalize this as a constraint in the plan.

**2. No test coverage for the generated converter handling enum types.**
The `ReflectionFreeSerializationTests` test `ComplexRecord` with primitives but no enum properties. The Design.Domain `Order` type has an `OrderStatus` enum property. The proposed fix must handle enums, but there is no standalone test isolating enum serialization in the ordinal converter tests. The architect should decide whether to add one.

**3. No test coverage for the generated converter handling interface-typed properties.**
The Design.Domain `Order` type has a `Lines` property typed as `IOrderLineList` (an interface). The generated converter currently emits `JsonSerializer.Deserialize<Design.Domain.Entities.IOrderLineList>(...)`. The proposed fix must handle interface-typed properties correctly. This is handled by RemoteFactory's custom `NeatooInterfaceJsonTypeConverter` at runtime, but no ordinal-specific test isolates this case.

**4. The dead `GenerateOrdinalSerialization` method in `FactoryGenerator.cs` (line 896).**
This method contains the same problematic `JsonSerializer.Serialize/Deserialize<T>` calls but is never called. The architect should decide whether to fix it, remove it, or leave it as dead code. If someone re-enables it without fixing it, the same IL2026 errors would recur.

### Contradictions

None found. The proposed fix (replacing `JsonSerializer.Serialize/Deserialize<T>` with trim-safe alternatives in generated code) directly addresses a bug that prevents the Design.Domain project from building. It aligns with all documented requirements:
- IL trimming is a first-class project requirement (docs/trimming.md, CLAUDE-DESIGN.md).
- Generated ordinal converters are documented as "trimming-compatible" and "reflection-free."
- The current generated code violates these documented claims by calling `[RequiresUnreferencedCode]`-marked APIs.

### Recommendations for Architect

1. **Both generator paths need attention.** The active path is `OrdinalRenderer.Render()` (called from `FactoryRenderer.RenderOrdinalSerialization`). The dead code in `FactoryGenerator.GenerateOrdinalSerialization()` (line 896) has the same bug. Fix or remove the dead code to prevent future confusion.

2. **Categorize property types for the fix strategy.** The generated converters handle a range of types. The architect should categorize each and choose the appropriate trim-safe API:
   - Primitives (`int`, `bool`, `decimal`, `long`, `double`, `string`): `Utf8JsonReader.GetXxx()` / `Utf8JsonWriter.WriteXxxValue()`
   - `DateTime`, `Guid`, `DateTimeOffset`: `Utf8JsonReader.GetXxx()` / `Utf8JsonWriter.WriteStringValue()`
   - Enums: `JsonSerializer.Serialize/Deserialize` with `JsonSerializerOptions` (the options-based overload that takes `Type` is NOT marked `[RequiresUnreferencedCode]` for some types) -- or use `reader.GetString()` + `Enum.Parse<T>()` for string-formatted enums.
   - Complex/nested types and interface types: Use `JsonSerializer.Serialize(writer, value, value.GetType(), options)` or the `JsonTypeInfo`-based overloads, depending on which are trim-safe.
   - Nullable types: Must wrap any primitive handling with null checks.
   - Collections (`List<T>`, arrays): Likely need to use the `JsonSerializer` overloads that pass through `options` with the registered converters.

3. **The runtime fallback `NeatooOrdinalConverter<T>` in `NeatooOrdinalConverterFactory.cs` is out of scope but has the same issue.** It uses `JsonSerializer.Deserialize(ref reader, _propertyTypes[index], options)` and `JsonSerializer.Serialize(writer, values[i], _propertyTypes[i], options)`. These calls are also potentially trim-unsafe, but they are in the runtime library (not generated code) and are the fallback path for non-generated types. The architect should note this as future work but keep it out of this todo's scope.

4. **Preserve all existing test contracts.** The fix must not alter the JSON output format. The `ReflectionFreeSerializationTests` assert exact JSON strings (e.g., `["Test",42]`). Any change to the serialization output would break these tests and the cross-client/server contract.

5. **Verify with both net9.0 and net10.0.** The `System.Text.Json` APIs may differ in trim annotations between framework versions. The fix must compile cleanly on both target frameworks.

6. **The `JsonSerializer.Serialize<T>()` and `JsonSerializer.Deserialize<T>()` generic overloads are the trim-unsafe ones.** The non-generic `JsonSerializer.Serialize(Utf8JsonWriter, object?, Type, JsonSerializerOptions)` overload is also marked `[RequiresUnreferencedCode]`. The architect must verify which specific overloads are trim-safe before choosing the replacement pattern.

## Results / Conclusions

Fixed by swapping `JsonSerializer.Serialize<T>()` / `Deserialize<T>()` to the trim-safe `GetTypeInfo`-based overloads in `OrdinalRenderer.cs`. The IL2026 warnings were on the generic method signatures, not the actual type arguments — even `Deserialize<int>()` triggered them. The fix uses `JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(typeof(T)))` and `JsonSerializer.Serialize(writer, value, options.GetTypeInfo(typeof(T)))` — same runtime behavior, different overload that satisfies the trimmer. Nullable value types required additional null-token/null-value handling. Dead `GenerateOrdinalSerialization()` method removed from `FactoryGenerator.cs`. All 999 tests pass, zero build warnings.
