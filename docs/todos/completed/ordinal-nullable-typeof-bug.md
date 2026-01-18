# Ordinal Generator: typeof() on Nullable Reference Types

**Status:** Complete
**Priority:** High
**Created:** 2026-01-17

---

## Problem

The ordinal serialization generator outputs invalid C# when a property has a nullable reference type:

```csharp
// Generated code (invalid)
public static Type[] PropertyTypes { get; } = new[]
{
    typeof(System.Collections.Generic.List<string>?),  // CS8639: typeof cannot be used on nullable reference type
    typeof(object),
    typeof(string)
};
```

The `?` suffix on nullable reference types is invalid in `typeof()` expressions.

## Solution

Strip the nullable annotation (`?`) from reference types when generating the `PropertyTypes` array. The nullability is a compile-time annotation, not a runtime type difference.

```csharp
// Correct output
typeof(System.Collections.Generic.List<string>)  // No trailing ?
```

---

## Tasks

- [x] Find where `PropertyTypes` array is generated in ordinal generator
- [x] Strip `?` from nullable reference types in type name
- [x] Add test case for nullable collection property

---

## Progress Log

### 2026-01-17
- Discovered while adding `[Factory]` to Neatoo test classes
- Affects: `TestValidateObject`, `MaxLengthRuleTestTarget`, `MinLengthRuleTestTarget`, `RequiredRuleTestTarget`
- All have `List<string>?` properties

### 2026-01-18
- Neatoo `lazyloadtype` branch has staged changes blocked by this bug
- Changed test classes from `[SuppressFactory]` to `[Factory]` to match real-life usage
- Build fails with CS8639 errors in generated `.Ordinal.g.cs` files
- **Neatoo is waiting on RemoteFactory fix before changes can be committed**

---

## Results / Conclusions

**Fixed in commit on 2026-01-18**

### Root Cause
The `FactoryGenerator.Types.cs` method `CollectPropertiesRecursive` was using `propertySymbol.Type.ToDisplayString()` which includes the nullable reference type annotation (`?`). This annotation is invalid in `typeof()` expressions because nullability is a compile-time concept only.

### Solution Implemented
1. **Fixed at source**: Modified `FactoryGenerator.Types.cs` line 371 to use `WithNullableAnnotation(NullableAnnotation.NotAnnotated)` before calling `ToDisplayString()`. This strips the nullable annotation at the data extraction phase.

2. **Cleaned up renderer**: Removed the now-redundant string manipulation in `OrdinalRenderer.cs` that attempted to strip trailing `?` from type names.

3. **Fixed secondary issue**: The `FromOrdinalArray` method also needed updating to handle nullable casts correctly. When casting from `object?` to a non-nullable type like `List<string>`, the compiler raises CS8600. Fixed by adding `?` to the cast type for nullable properties.

### Files Modified
- `src/Generator/FactoryGenerator.Types.cs` - Strip nullable annotation at source
- `src/Generator/Renderer/OrdinalRenderer.cs` - Simplify PropertyTypes and fix FromOrdinalArray casts
- `src/Tests/FactoryGeneratorTests/Factory/RecordTestObjects.cs` - Add test records
- `src/Tests/FactoryGeneratorTests/Factory/OrdinalSerializationTests.cs` - Add regression tests

### Tests Added
- `RecordWithNullableCollection` - Simple nullable collection (`List<string>?`)
- `RecordWithComplexNullableGenerics` - Complex nullable generics (`Dictionary<string, int>?`, `List<string?>?`)
- `PropertyTypesGenerationTests` - Direct validation of generated `PropertyTypes` arrays
- Serialization round-trip tests for both ordinal and named formats

**Neatoo `lazyloadtype` branch can now proceed with its changes.**
