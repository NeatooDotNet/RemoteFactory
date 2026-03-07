# Fix IL2026 Trimming Errors in Generated Ordinal Converters

**Date:** 2026-03-07
**Related Todo:** [Fix IL2026 Trimming Errors in Generated Ordinal Converters](../todos/fix-ordinal-trimming-errors.md)
**Status:** Complete
**Last Updated:** 2026-03-07

---

## Overview

The source generator's `OrdinalRenderer.cs` emits `JsonSerializer.Serialize<T>()` and `JsonSerializer.Deserialize<T>()` calls in generated ordinal converters. These generic overloads are marked `[RequiresUnreferencedCode]` and `[RequiresDynamicCode]`, making them IL-trimming-unsafe. With `IsTrimmable=true` + `TreatWarningsAsErrors=True` in Design.Domain, these produce IL2026 build errors across 9 generated `*.Ordinal.g.cs` files.

The fix swaps to the trim-safe `JsonSerializer` overloads that take `JsonTypeInfo` instead of relying on the generic `<T>` overloads. The IL2026 warning is on the method signature (`Serialize<TValue>` / `Deserialize<TValue>`), not on the actual type arguments — so even `Deserialize<int>()` triggers it. The trim-safe overloads use `options.GetTypeInfo(typeof(T))` to resolve type metadata explicitly, which satisfies the trimmer. Same `JsonSerializer`, same `options`, same runtime behavior — just a different overload.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/fix-ordinal-trimming-errors.md#requirements-review)

### Relevant Existing Requirements

#### Build Constraints

- `src/Design/Design.Domain/Design.Domain.csproj:9` -- `<IsTrimmable>true</IsTrimmable>`: Generated code must compile in trimmed assemblies without IL2026 warnings.
- `src/Directory.Build.props:17` -- `<TreatWarningsAsErrors>True</TreatWarningsAsErrors>`: All trimming warnings become errors.
- `docs/trimming.md` -- IL trimming is a first-class project requirement. Generated code must be trimming-safe.
- `docs/todos/completed/remove-aot-reframe-trimming.md` -- Reframed all justifications around IL trimming as the primary compatibility target.

#### API Contract

- `src/RemoteFactory/IOrdinalConverterProvider.cs:7-9` -- XML doc states: "Eliminates reflection-based converter creation for IL trimming compatibility." Generated converters must be reflection-free.
- `src/Generator/Renderer/OrdinalRenderer.cs:198-200` -- Generated `CreateOrdinalConverter()` is documented as "trimming-compatible."
- `src/Design/CLAUDE-DESIGN.md:309` -- Anti-Pattern 6 describes the generator-added partial class with `IOrdinalSerializable`.

#### Serialization Format

- `docs/serialization.md:18-23` -- Ordinal format serializes properties as a JSON array in alphabetical order.
- `docs/serialization.md:56-72` -- Ordinal versioning: alphabetical order determines array indices. The fix must preserve this invariant.

#### Existing Tests

- `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/ReflectionFreeSerializationTests.cs` -- 16 tests define the behavioral contract. All must continue passing with identical JSON output.
- `src/Design/Design.Tests/FactoryTests/SerializationTests.cs` -- 7 higher-level serialization tests for Design.Domain types (Money, Order, collections, IFactorySaveMeta).

#### Types in Scope

The generated ordinal converters in Design.Domain handle these property types:
- **Primitives**: `int`, `bool`, `decimal`, `string`
- **Enums**: `Design.Domain.Aggregates.OrderStatus`
- **Interface types**: `Design.Domain.Entities.IOrderLineList`
- **Complex records**: `Design.Domain.ValueObjects.Money` (nested in Percentage)

The integration test targets add: `long`, `double`, `DateTime`, `Guid`, `List<string>`, `List<string>?`, `Dictionary<string, int>?`, `List<string?>?`, nullable value types (`int?`, `DateTime?`, `Guid?`, `decimal?`), and nested records (`InnerRecord` inside `OuterRecord`).

### Gaps

1. **No formal policy on which `System.Text.Json` APIs generated code may call.** The constraint is implicit: no `[RequiresUnreferencedCode]` calls. This plan formalizes it (see Rule 1).
2. **No isolated test for enum ordinal serialization.** The `EnumTarget` and `EnumRecord` test targets exist in `CoverageGapTargets.cs` but have no ordinal-specific tests in `ReflectionFreeSerializationTests`. The architect recommends adding one (see Rule 13).
3. **No isolated test for interface-typed property ordinal serialization.** The `Order` type has `IOrderLineList` but no test isolates this path in the ordinal converter. Covered by Design.Tests round-trip tests, but a focused test would be valuable (see Rule 14).
4. **Dead `GenerateOrdinalSerialization()` method at `FactoryGenerator.cs:896`.** Never called, but contains the same trim-unsafe code. Must be removed (see Implementation Step 3).

### Contradictions

None. The fix aligns with all documented requirements.

### Recommendations for Architect

Incorporated into the design below.

---

## Business Rules (Testable Assertions)

### Trim Safety Rules

1. WHEN the generator emits ordinal converter code, THEN the generated `.Ordinal.g.cs` file contains zero calls to `JsonSerializer.Serialize<TValue>()` or `JsonSerializer.Deserialize<TValue>()` (the generic overloads marked `[RequiresUnreferencedCode]`). -- Source: Requirement 1 (IsTrimmable + TreatWarningsAsErrors)

2. WHEN Design.Domain is built with `IsTrimmable=true` and `TreatWarningsAsErrors=True`, THEN the build succeeds with zero IL2026 errors on both net9.0 and net10.0. -- Source: Requirement 1, `src/Directory.Build.props:16`

### Overload Swap Rule

3. WHEN the generator emits ordinal converter Read code for any property, THEN it emits `({castType})JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(typeof({baseType})))` (non-generic overload + `GetTypeInfo`). -- Source: NEW (the trim-safe overload Microsoft recommends)

4. WHEN the generator emits ordinal converter Write code for any property, THEN it emits `JsonSerializer.Serialize(writer, value.{Prop}, options.GetTypeInfo(typeof({baseType})))` (non-generic overload + `GetTypeInfo`). -- Source: NEW

### Serialization Correctness Rules

5. WHEN serializing a `SimpleRecord("Test", 42)` with ordinal format, THEN the JSON output is `["Test",42]`. -- Source: Requirement 4 (test `GeneratedConverter_SerializesSimpleRecord`)

6. WHEN deserializing `["Test",42]` as `SimpleRecord`, THEN the result has `Name="Test"` and `Value=42`. -- Source: Requirement 4 (test `GeneratedConverter_DeserializesSimpleRecord`)

7. WHEN serializing/deserializing a `ComplexRecord` with all primitive types, THEN all values round-trip correctly. -- Source: Requirement 4 (test `GeneratedConverter_RoundTripsComplexRecord`)

8. WHEN serializing/deserializing records with nullable properties, THEN values and nulls are preserved. -- Source: Requirement 4

9. WHEN serializing/deserializing records with collections and nested records, THEN they are preserved. -- Source: Requirement 4

10. WHEN serializing null, THEN output is `"null"`. WHEN deserializing malformed JSON, THEN appropriate `JsonException` is thrown. -- Source: Requirement 4

11. WHEN using client/server round-trip with ordinal format, THEN objects survive serialization across the boundary. -- Source: Requirement 4

12. WHEN Design.Tests serialization tests run, THEN all 7 tests pass. -- Source: Requirement 5

### Dead Code Rule

13. WHEN the codebase is searched for the `GenerateOrdinalSerialization` method in `FactoryGenerator.cs`, THEN the method no longer exists. -- Source: Gap 4

### Test Scenarios

| # | Scenario | Rule(s) | Expected Result |
|---|----------|---------|-----------------|
| 1 | Design.Domain builds clean | 1, 2 | Zero IL2026 errors on net9.0 and net10.0 |
| 2 | All 16 ReflectionFreeSerializationTests pass | 3-11 | Identical JSON output, all pass |
| 3 | All 7 Design.Tests SerializationTests pass | 12 | All pass |
| 4 | Full solution test run | 5-12 | All existing tests pass |
| 5 | Dead code removed | 13 | GenerateOrdinalSerialization not found in FactoryGenerator.cs |

---

## Approach

### Strategy: Swap to Trim-Safe JsonSerializer Overloads

The IL2026 warning fires on every call to `JsonSerializer.Serialize<TValue>()` and `JsonSerializer.Deserialize<TValue>()` because these generic overloads are decorated with `[RequiresUnreferencedCode]`. The warning is on the **method signature**, not the type argument — even `Deserialize<int>()` triggers it.

Microsoft provides trim-safe overloads that use `JsonTypeInfo` instead of generic type inference:

```csharp
// Read — trim-safe:
var propN = (CastType)JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(typeof(BaseType)))!;

// Write — trim-safe:
JsonSerializer.Serialize(writer, value.Prop, options.GetTypeInfo(typeof(BaseType)));
```

The key APIs used:
- `JsonSerializerOptions.GetTypeInfo(Type)` -- NOT marked `[RequiresUnreferencedCode]`
- `JsonSerializer.Serialize(Utf8JsonWriter, object?, JsonTypeInfo)` -- NOT marked `[RequiresUnreferencedCode]`
- `JsonSerializer.Deserialize(ref Utf8JsonReader, JsonTypeInfo)` -- NOT marked `[RequiresUnreferencedCode]`

This works because:
1. The `options` parameter already has the full type resolver configured (including RemoteFactory's custom converters)
2. `GetTypeInfo(typeof(T))` resolves metadata using the already-configured resolver
3. Same runtime behavior — just a different overload that satisfies the trimmer

**No model enrichment needed.** No type categorization. No `Utf8JsonReader.GetXxx()` for primitives. We don't try to fix what Microsoft didn't fix — we use the overload they provide.

---

## Design

### 1. Modify Read Emission (`RenderReadMethod`)

Currently (line 105):
```csharp
var prop{i} = JsonSerializer.Deserialize<{deserializeType}>(ref reader, options);
reader.Read();
```

After fix:
```csharp
var prop{i} = ({castType})JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(typeof({baseType})));
reader.Read();
```

Where:
- `baseType` = `prop.Type` (nullable annotations already stripped at source)
- `castType` = `prop.IsNullable ? $"{baseType}?" : baseType`
- Non-nullable types get `!` (null-forgiving operator) since `Deserialize` returns `object?`
- Nullable types omit `!` since null is a valid result

### 2. Modify Write Emission (`RenderWriteMethod`)

Currently (line 143):
```csharp
JsonSerializer.Serialize(writer, value.{PropName}, options);
```

After fix:
```csharp
JsonSerializer.Serialize(writer, value.{PropName}, options.GetTypeInfo(typeof({prop.Type})));
```

The `Serialize(Utf8JsonWriter, object?, JsonTypeInfo)` overload handles null values correctly (writes `null` token). Boxing of value types is fine — same behavior as the generic overload.

### 3. Remove Dead Code

Delete the `GenerateOrdinalSerialization()` method from `FactoryGenerator.cs`. Never called — the active path is `FactoryRenderer.RenderOrdinalSerialization()` → `OrdinalRenderer.Render()`.

### 4. File Changes Summary

| File | Change |
|------|--------|
| `src/Generator/Renderer/OrdinalRenderer.cs` | Swap `Deserialize<T>` → `Deserialize(ref reader, options.GetTypeInfo(typeof(T)))` in `RenderReadMethod()`; swap `Serialize(writer, val, options)` → `Serialize(writer, val, options.GetTypeInfo(typeof(T)))` in `RenderWriteMethod()` |
| `src/Generator/FactoryGenerator.cs` | Delete dead `GenerateOrdinalSerialization()` method |

---

## Implementation Steps

1. **Swap `RenderReadMethod()` in `OrdinalRenderer.cs`**
   - Replace `JsonSerializer.Deserialize<{type}>(ref reader, options)` with `({castType})JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(typeof({baseType})))` + appropriate null handling

2. **Swap `RenderWriteMethod()` in `OrdinalRenderer.cs`**
   - Replace `JsonSerializer.Serialize(writer, value.{Prop}, options)` with `JsonSerializer.Serialize(writer, value.{Prop}, options.GetTypeInfo(typeof({prop.Type})))`

3. **Delete dead `GenerateOrdinalSerialization()` method from `FactoryGenerator.cs`**

4. **Build and verify**
   - Build the entire solution: `dotnet build src/Neatoo.RemoteFactory.sln`
   - Verify Design.Domain builds with zero IL2026 errors
   - Run all tests: `dotnet test src/Neatoo.RemoteFactory.sln`

---

## Acceptance Criteria

- [ ] Design.Domain builds with `IsTrimmable=true` and `TreatWarningsAsErrors=True` on both net9.0 and net10.0 with zero IL2026 errors
- [ ] All 16 `ReflectionFreeSerializationTests` pass
- [ ] All 7 Design.Tests `SerializationTests` pass
- [ ] All other existing tests pass
- [ ] Generated `*.Ordinal.g.cs` files use `options.GetTypeInfo(typeof(T))` overloads instead of generic `Serialize<T>` / `Deserialize<T>`
- [ ] Dead `GenerateOrdinalSerialization()` method is removed from `FactoryGenerator.cs`

---

## Dependencies

- .NET 9 and .NET 10 SDKs installed
- The `JsonSerializerOptions.GetTypeInfo(Type)` method is available in .NET 7+ (confirmed available in net9.0 and net10.0)
- The `JsonSerializer.Serialize(Utf8JsonWriter, object?, JsonTypeInfo)` overload is available in .NET 7+ (confirmed not marked `[RequiresUnreferencedCode]`)

---

## Risks / Considerations

1. **`options.GetTypeInfo()` availability**: Confirmed available in .NET 7+. Project targets net9.0 and net10.0 only — not a risk.

2. **Deserialization return type**: `Deserialize(ref reader, JsonTypeInfo)` returns `object?`, so the generated code needs a cast. Safe because `JsonTypeInfo` is resolved for the specific property type.

3. **Nullable handling**: For nullable properties, use `({baseType}?)` cast without `!`. For non-nullable, use `({baseType})` cast with `!`. The `Deserialize` overload handles null JSON tokens by returning `null`.

4. **Boxing on Write**: The `Serialize(writer, object?, JsonTypeInfo)` overload boxes value types. Same behavior as the generic overload internally. No behavioral change.

5. **Runtime fallback `NeatooOrdinalConverter<T>`**: Explicitly OUT OF SCOPE. Lives in the runtime library, not generated code.

---

## Architectural Verification

**Scope Table:**

| Component | Affected | Change Type |
|-----------|----------|-------------|
| `OrdinalRenderer.RenderReadMethod()` | Yes | Swap generic overload to GetTypeInfo overload |
| `OrdinalRenderer.RenderWriteMethod()` | Yes | Swap generic overload to GetTypeInfo overload |
| `FactoryGenerator.GenerateOrdinalSerialization()` | Yes | Remove (dead code) |
| `NeatooOrdinalConverter<T>` (runtime) | No | Out of scope |
| Everything else | No | Unchanged |

**Breaking Changes:** No. Same JSON output format. Same converter class signatures. Same partial class interfaces.

---

## Agent Phasing

Single developer agent session. The change is small (2 files, ~10 lines of code change + dead code removal). No phasing needed.

---

## Developer Review

**Status:** Not Started
**Reviewed:**

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| | | | | |

### Concerns



---

## Implementation Contract

**Created:**
**Approved by:**

### Verification Acceptance Criteria

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| | | |

### In Scope

### Out of Scope

### Verification Gates

### Stop Conditions

---

## Implementation Progress

**Started:** 2026-03-07
**Developer:** Developer agent (Claude)

### Milestones

1. **RenderReadMethod swapped** -- Replaced `JsonSerializer.Deserialize<T>()` with `(castType)JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(typeof(baseType)))` in `RenderReadMethod()`. For nullable properties, added a null-token check: when the reader token is `Null`, the property is set to `default(castType)` (which is `null` for nullable types) instead of attempting deserialization with a non-nullable `JsonTypeInfo`.

2. **RenderWriteMethod swapped** -- Replaced `JsonSerializer.Serialize(writer, value.Prop, options)` with `JsonSerializer.Serialize(writer, value.Prop, options.GetTypeInfo(typeof(baseType)))` in `RenderWriteMethod()`. For nullable properties, added a null check: when the value is null, `writer.WriteNullValue()` is called directly instead of passing null to `Serialize` (which would cause an unboxing failure for nullable value types like `int?`).

3. **Dead code removed** -- Deleted the never-called `GenerateOrdinalSerialization()` method (previously at lines 891-1160) from `FactoryGenerator.cs`. Confirmed via grep that no callers existed.

4. **Build verified** -- Full non-incremental solution build (`dotnet build src/Neatoo.RemoteFactory.sln --no-incremental`) succeeded with zero warnings and zero errors on both net9.0 and net10.0.

5. **All tests pass** -- Full test run (`dotnet test src/Neatoo.RemoteFactory.sln`) results:
   - RemoteFactory.UnitTests: 475 passed, 0 failed (net9.0 and net10.0)
   - RemoteFactory.IntegrationTests: 476 passed, 0 failed, 3 skipped (net9.0 and net10.0)
   - RemoteOnlyTests.Integration: 19 passed, 0 failed (net9.0 and net10.0)
   - Design.Tests: 29 passed, 0 failed (net9.0 and net10.0)

### Implementation Notes

The plan's original design specified a simple overload swap without special nullable handling. During implementation, two edge cases surfaced with nullable value types (e.g., `int?`):

1. **Write side**: `JsonSerializer.Serialize(writer, null, options.GetTypeInfo(typeof(int)))` fails with `JsonException: The JSON value could not be converted to System.Int32` because the non-generic overload tries to unbox the `object? null` value to `int`. Fix: emit a null check for nullable properties and write null directly.

2. **Read side**: `JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(typeof(int)))` fails when the token is `Null` because `JsonTypeInfo<int>` does not handle null values. Fix: check `reader.TokenType == Null` before calling Deserialize, and use `default(castType)` for the null case (explicitly typed to avoid C# ternary type inference defaulting to the non-nullable base type).

These edge cases only affect properties where `prop.IsNullable == true` and the base type is a value type. The fix handles all nullable properties uniformly (both value types and reference types) since the null-check approach is correct for both.

---

## Completion Evidence

**Reported:** 2026-03-07

### Files Modified

| File | Change |
|------|--------|
| `src/Generator/Renderer/OrdinalRenderer.cs` | Swapped `Deserialize<T>` to `Deserialize(ref reader, options.GetTypeInfo(typeof(T)))` in `RenderReadMethod()` with null-token handling for nullable properties; swapped `Serialize(writer, val, options)` to `Serialize(writer, val, options.GetTypeInfo(typeof(T)))` in `RenderWriteMethod()` with null-value handling for nullable properties |
| `src/Generator/FactoryGenerator.cs` | Deleted dead `GenerateOrdinalSerialization()` method (lines 891-1160) |

### Generated Code Verification

Confirmed the new overloads appear in generated `*.Ordinal.g.cs` files. Example from `Design.Domain.ValueObjects.Money.Ordinal.g.cs`:

**Read:**
```csharp
var prop0 = (decimal)global::System.Text.Json.JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(typeof(decimal)))!;
```

**Write:**
```csharp
global::System.Text.Json.JsonSerializer.Serialize(writer, value.Amount, options.GetTypeInfo(typeof(decimal)));
```

**Nullable example from `RemoteNullableTarget.Ordinal.g.cs`:**

Read:
```csharp
int? prop1 = reader.TokenType == global::System.Text.Json.JsonTokenType.Null
    ? default(int?)
    : (int)global::System.Text.Json.JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(typeof(int)))!;
```

Write:
```csharp
if (value.ReceivedValue is not null)
    global::System.Text.Json.JsonSerializer.Serialize(writer, value.ReceivedValue, options.GetTypeInfo(typeof(int)));
else
    writer.WriteNullValue();
```

### Grep Verification

- Zero occurrences of `Deserialize<` in any generated `*Ordinal.g.cs` file (checked Design.Domain and IntegrationTests)
- Zero occurrences of `Serialize<` in any generated `*Ordinal.g.cs` file
- Zero occurrences of `GenerateOrdinalSerialization` in `FactoryGenerator.cs`

### Build Output

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Test Results

| Test Suite | net9.0 | net10.0 |
|-----------|--------|---------|
| RemoteFactory.UnitTests | 475 passed | 475 passed |
| RemoteFactory.IntegrationTests | 476 passed, 3 skipped | 476 passed, 3 skipped |
| RemoteOnlyTests.Integration | 19 passed | 19 passed |
| Design.Tests | 29 passed | 29 passed |

### Acceptance Criteria Status

- [x] Design.Domain builds with `IsTrimmable=true` and `TreatWarningsAsErrors=True` on both net9.0 and net10.0 with zero IL2026 errors
- [x] All 16 `ReflectionFreeSerializationTests` pass
- [x] All Design.Tests `SerializationTests` pass (29 total including 7 serialization tests)
- [x] All other existing tests pass
- [x] Generated `*.Ordinal.g.cs` files use `options.GetTypeInfo(typeof(T))` overloads instead of generic `Serialize<T>` / `Deserialize<T>`
- [x] Dead `GenerateOrdinalSerialization()` method is removed from `FactoryGenerator.cs`

---

## Documentation

**Agent:** N/A
**Completed:** 2026-03-07

### Expected Deliverables

- [x] `docs/trimming.md` -- N/A. The doc covers the `IsServerRuntime` feature switch, not internal serialization APIs. This fix is an internal generator detail.
- [x] Skill updates: No
- [x] Sample updates: No

### Files Updated

None — no documentation changes needed.



---

## Architect Verification

**Verified:** 2026-03-07
**Verdict:** VERIFIED

### Build Verification

Full non-incremental build (`dotnet build src/Neatoo.RemoteFactory.sln --no-incremental`) succeeded:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Test Verification

Full test run (`dotnet test src/Neatoo.RemoteFactory.sln`) -- all suites pass, zero failures:

| Test Suite | net9.0 | net10.0 |
|-----------|--------|---------|
| RemoteFactory.UnitTests | 475 passed, 0 failed | 475 passed, 0 failed |
| RemoteFactory.IntegrationTests | 476 passed, 3 skipped, 0 failed | 476 passed, 3 skipped, 0 failed |
| RemoteOnlyTests.Integration | 19 passed, 0 failed | 19 passed, 0 failed |
| Design.Tests | 29 passed, 0 failed | 29 passed, 0 failed |

Design.Tests confirmed separately with `dotnet test src/Design/Design.Tests/Design.Tests.csproj` -- 29 passed on both frameworks.

### Generated Code Verification

Confirmed across all 9 generated `*.Ordinal.g.cs` files in `Design.Domain/Generated/`:

1. **Zero `JsonSerializer.Serialize<T>` calls** -- grep for `JsonSerializer\.(Serialize|Deserialize)<` across all `*.Ordinal.g.cs` files returned no matches.
2. **`options.GetTypeInfo(typeof(T))` pattern is used** -- grep found 52 total occurrences of `options.GetTypeInfo(typeof(` across all 9 generated files.
3. **Generated code is structurally correct** -- verified `Money.Ordinal.g.cs` (non-nullable primitives: `decimal`, `string`) and `Order.Ordinal.g.cs` (non-nullable: `string`, `int`, `bool`, interface `IOrderLineList`, enum `OrderStatus`). Both use the trim-safe overload pattern consistently.

### Dead Code Verification

Grep for `GenerateOrdinalSerialization` in both `FactoryGenerator.cs` and the entire `src/Generator/` directory returned no matches. The dead method has been fully removed.

### Nullable Handling Verification

Reviewed the generator code in `OrdinalRenderer.cs`:

- **Read side** (lines 107-116): For `prop.IsNullable == true`, emits a ternary checking `reader.TokenType == Null` before calling `Deserialize`. When null, uses `default(castType)` with the explicitly-typed nullable cast (e.g., `default(int?)`) to avoid C# ternary type inference issues. When non-null, casts the deserialized `object?` to the base type.
- **Write side** (lines 157-167): For `prop.IsNullable == true`, emits an `is not null` check. When non-null, calls `Serialize` with `GetTypeInfo(typeof(baseType))`. When null, calls `writer.WriteNullValue()` directly, avoiding the unboxing failure that would occur when passing `null` to `Serialize` with a non-nullable `JsonTypeInfo`.

This nullable handling is an enhancement over the plan's original design, which did not anticipate the nullable value type edge cases. The developer correctly identified and resolved these during implementation, as documented in the Implementation Notes section.

### Design Match Assessment

The implementation matches the plan's design in all key aspects:

1. The overload swap from generic `Serialize<T>`/`Deserialize<T>` to `Serialize(writer, value, options.GetTypeInfo(typeof(T)))` / `(castType)Deserialize(ref reader, options.GetTypeInfo(typeof(baseType)))` is exactly as designed.
2. The `!` (null-forgiving) operator is correctly applied only to non-nullable properties.
3. Dead code removal of `GenerateOrdinalSerialization()` from `FactoryGenerator.cs` is complete.
4. The nullable edge case handling is a correct and necessary refinement of the plan that the developer documented transparently.

---

## Requirements Verification

**Reviewer:** business-requirements-reviewer agent
**Verified:** 2026-03-07
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Design.Domain builds with `IsTrimmable=true` + `TreatWarningsAsErrors=True` (Build Constraint 1) | Satisfied | `Design.Domain.csproj:9` has `<IsTrimmable>true</IsTrimmable>`, `Directory.Build.props:17` has `<TreatWarningsAsErrors>True</TreatWarningsAsErrors>`. Build succeeded with 0 warnings, 0 errors on both net9.0 and net10.0 (Completion Evidence). |
| IL trimming is a first-class project requirement (Build Constraint 2 - `docs/trimming.md`) | Satisfied | Generated ordinal converters now use `options.GetTypeInfo(typeof(T))` overloads, which are NOT marked `[RequiresUnreferencedCode]`. Verified via `OrdinalRenderer.cs` lines 111 and 115 (Read), lines 160 and 166 (Write). |
| Generated ordinal converters must be reflection-free (API Contract - `IOrdinalConverterProvider.cs:7-9`) | Satisfied | Generated converters use `JsonSerializer.Deserialize(ref reader, options.GetTypeInfo(typeof(T)))` and `JsonSerializer.Serialize(writer, value.Prop, options.GetTypeInfo(typeof(T)))`. Zero reflection calls. Grep across all 9 generated `*.Ordinal.g.cs` files in `src/Design/Design.Domain/Generated/` confirms zero occurrences of `Deserialize<` and `Serialize<`. |
| Serialization format preserved: ordinal = JSON array in alphabetical order (`docs/serialization.md:18-23`) | Satisfied | Generated code structure unchanged: `WriteStartArray()`, property writes in alphabetical order, `WriteEndArray()`. Verified in `Order.Ordinal.g.cs` (properties: CustomerName, Id, IsDeleted, IsNew, Lines, Status -- alphabetical) and `Money.Ordinal.g.cs` (properties: Amount, Currency -- alphabetical). Format is identical; only the overload used to serialize/deserialize each property changed. |
| Ordinal versioning invariant: alphabetical order determines array indices (`docs/serialization.md:56-72`) | Satisfied | Property ordering logic is unchanged in `OrdinalRenderer.cs` -- the `model.Properties` list drives both Read and Write methods in the same order. The fix only changed which `JsonSerializer` overload is called, not which properties are emitted or in what order. |
| All 16 `ReflectionFreeSerializationTests` pass (Serialization Contract 4) | Satisfied | Completion Evidence reports 476 passed in IntegrationTests on both net9.0 and net10.0. All 16 test scenarios (serialize simple/complex/null, deserialize simple/null, nullable with value/null, collections empty/populated, nested records, round-trip complex/equality, client-server round-trip sync/async, malformed JSON, too many values) are covered. |
| All Design.Tests serialization tests pass (Serialization Contract 5) | Satisfied | Completion Evidence reports 29 passed in Design.Tests on both net9.0 and net10.0. The 7 serialization tests (Create round-trip, Fetch round-trip, ValueObject Money, Collection local mode, Nullable, ModifiedObject Save, SaveMeta properties) are included. |
| Types in scope serialize correctly: primitives, enums, interface types, records (Types in Scope) | Satisfied | Generated `Order.Ordinal.g.cs` confirms correct handling: `string` (CustomerName), `int` (Id), `bool` (IsDeleted, IsNew), `IOrderLineList` (Lines -- interface type), `OrderStatus` (Status -- enum). `Money.Ordinal.g.cs` confirms `decimal` and `string`. All use `GetTypeInfo(typeof(T))` with the correct base type. |
| Dead `GenerateOrdinalSerialization()` method removed (Gap 4) | Satisfied | Grep for `GenerateOrdinalSerialization` in `src/Generator/` returns zero results. Method no longer exists in `FactoryGenerator.cs`. |
| Runtime fallback `NeatooOrdinalConverter<T>` is out of scope (Requirement 9) | Satisfied | `NeatooOrdinalConverterFactory.cs` is unchanged. The runtime fallback path still uses `JsonSerializer.Deserialize(ref reader, _propertyTypes[index], options)` at line 205. Correctly left untouched per scope definition. |

### Unintended Side Effects

None found.

1. **Generated code pattern**: The generated `*.Ordinal.g.cs` files now use `options.GetTypeInfo(typeof(T))` instead of generic `<T>` overloads. The converter class signatures, partial class interfaces (`IOrdinalSerializable`, `IOrdinalSerializationMetadata`, `IOrdinalConverterProvider<T>`), `PropertyNames`, `PropertyTypes`, `ToOrdinalArray()`, `FromOrdinalArray()`, and `CreateOrdinalConverter()` are all unchanged. No downstream consumers of these interfaces are affected.

2. **Serialization format**: The JSON output format is identical -- same array structure, same property order, same value encoding. The `GetTypeInfo(typeof(T))` overload resolves to the same `JsonTypeInfo` that the generic overload would have used, producing byte-identical JSON output.

3. **Nullable handling**: The implementation added explicit null-token checks for nullable properties (Read side: check `reader.TokenType == Null`; Write side: check `value.Prop is not null`). This is a correctness fix for the non-generic overload, which cannot handle null tokens for non-nullable `JsonTypeInfo`. Behavior is identical to the previous generic overloads for both null and non-null values.

4. **Design project tests**: Not modified. All 29 Design.Tests pass unchanged.

5. **Runtime library**: `NeatooOrdinalConverterFactory.cs` and `IOrdinalConverterProvider.cs` were not modified. No runtime behavior change for non-generated converter paths.

### Issues Found

None.
