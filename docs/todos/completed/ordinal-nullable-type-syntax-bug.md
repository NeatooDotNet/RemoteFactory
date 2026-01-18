# Bug: Ordinal Serialization Nullable Type Syntax Error

**Status:** Completed
**Priority:** High
**Created:** 2026-01-18
**Completed:** 2026-01-18
**Version:** 10.11.1 -> 10.11.2

---

## Problem

The `FromOrdinalArray()` method generation in version 10.11.1 produces invalid C# syntax for nullable types:

1. **Nullable value types**: Generates `(Type ?? )` instead of `(Type?)`
2. **Nullable reference types**: Generates `(Type? )` with extra space instead of `(Type?)`

This causes compilation errors:
- CS1525: Invalid expression term
- CS1003: Syntax error, ',' expected
- CS0747: Invalid initializer member declarator
- CS0119: Type is not valid in the given context

---

## Root Cause

The bug was in `FactoryGenerator.Types.cs` at line 371. For nullable value types like `int?`, Roslyn's `ToDisplayString()` returns `"int?"` even after calling `WithNullableAnnotation(NullableAnnotation.NotAnnotated)`. This is because `int?` is actually `Nullable<int>` - a different underlying type where the nullable annotation is not about reference type nullability.

Then in `OrdinalRenderer.cs`, the renderer unconditionally adds `?` when `IsNullable` is true:
```csharp
var castType = prop.IsNullable ? $"{prop.Type}?" : prop.Type;
```

When `prop.Type` is already `"int?"`, this produces `"int??"` which is invalid C# syntax.

---

## Fix Applied

### 1. Source Fix (FactoryGenerator.Types.cs)

Strip trailing whitespace and `?` from type strings to ensure consistent base types:

```csharp
var typeString = propertySymbol.Type
    .WithNullableAnnotation(NullableAnnotation.NotAnnotated)
    .ToDisplayString()
    .TrimEnd()
    .TrimEnd('?')
    .TrimEnd();
```

### 2. Renderer Fix (OrdinalRenderer.cs)

Made the renderer defensive by:
- Adding `TrimEnd()` before checking `EndsWith("?")`
- Only adding `?` if the type doesn't already end with `?`

Updated in three locations:
- `RenderReadMethod`: For `Deserialize<T>` type parameters
- `RenderFromOrdinalArrayConstruction`: For object initializer casts
- `BuildConstructorArgsForFromArray`: For constructor argument casts

---

## Tests Added

Added `RecordWithNullableValueTypes` test record with:
- `int?`
- `DateTime?`
- `Guid?`
- `decimal?`

Added 4 new unit tests:
- `RecordWithNullableValueTypes_PropertyTypes_NoNullableAnnotation`
- `RecordWithNullableValueTypes_ToOrdinalArray_SerializesCorrectly`
- `RecordWithNullableValueTypes_FromOrdinalArray_DeserializesCorrectly`
- `RecordWithNullableValueTypes_FromOrdinalArray_HandlesNullValues`

---

## Files Changed

1. `src/Generator/FactoryGenerator.Types.cs` - Strip trailing `?` and whitespace from type strings
2. `src/Generator/Renderer/OrdinalRenderer.cs` - Defensive handling in three render methods
3. `src/Tests/FactoryGeneratorTests/Factory/RecordTestObjects.cs` - Added `RecordWithNullableValueTypes`
4. `src/Tests/FactoryGeneratorTests/Factory/OrdinalSerializationTests.cs` - Added 4 new tests

---

## Verification

- Build passes with no compilation errors
- All 34 tests pass (30 existing + 4 new)
- Generated code produces valid C# syntax for nullable value types and reference types
