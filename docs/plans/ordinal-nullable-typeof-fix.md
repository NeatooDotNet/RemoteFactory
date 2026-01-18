# Ordinal Generator: typeof() Nullable Reference Type Fix - Implementation Plan

**Status:** Research Complete - Ready for Implementation
**Priority:** High
**Created:** 2026-01-18

---

## Executive Summary

The ordinal serialization generator outputs invalid C# when a property has a nullable reference type in `typeof()` expressions. For example:

```csharp
// Generated code (INVALID)
typeof(System.Collections.Generic.List<string>?)  // CS8639: typeof cannot be used on nullable reference type
```

The root cause is that Roslyn's `ITypeSymbol.ToDisplayString()` includes nullable reference type annotations (`?`), which are invalid in `typeof()` expressions since nullability is a compile-time annotation, not a runtime type distinction.

---

## Root Cause Analysis

### Location of the Bug

**File:** `src/Generator/FactoryGenerator.Types.cs`
**Line:** 371
**Method:** `CollectPropertiesRecursive`

```csharp
properties.Add(new OrdinalPropertyInfo(
    propertySymbol.Name,
    propertySymbol.Type.ToDisplayString(),  // ← BUG: Includes nullable annotation
    isNullable,
    depth));
```

### Current Incomplete Fix Attempt

**File:** `src/Generator/Renderer/OrdinalRenderer.cs`
**Lines:** 165-168

```csharp
// Strip nullable annotation for typeof() (CS8639)
if (typeName.EndsWith("?") && !typeName.Contains("<"))
{
    typeName = typeName.TrimEnd('?');
}
```

**Problem:** This fix only handles simple nullable types like `string?` or `int?`, but **fails for generic types** like `List<string>?` because of the condition `!typeName.Contains("<")`.

### Why the Current Fix Fails

The condition `!typeName.Contains("<")` was likely added to avoid accidentally stripping `?` from within generic type arguments (like `List<int?>`). However, this also prevents stripping the trailing `?` from nullable generic types (like `List<string>?`).

**Examples:**
- `string?` → Works (no `<` character) → Stripped to `string` ✓
- `int?` → Works (nullable value type uses `Nullable<int>` format) → Stripped to `int` ✓
- `List<string>?` → **FAILS** (contains `<`) → Not stripped → `typeof(List<string>?)` → **CS8639** ✗
- `Dictionary<string, int>?` → **FAILS** (contains `<`) → Not stripped → `typeof(Dictionary<string, int>?)` → **CS8639** ✗
- `List<string?>?` → **FAILS** (contains `<`) → Not stripped, inner `?` should remain → Complex case ✗

---

## Proposed Solution

### Option 1: Strip at Source (Recommended)

Modify the type extraction in `FactoryGenerator.Types.cs` to use Roslyn's built-in method to get the type without nullable annotations.

**Location:** `src/Generator/FactoryGenerator.Types.cs:371`

**Change:**
```csharp
// BEFORE
properties.Add(new OrdinalPropertyInfo(
    propertySymbol.Name,
    propertySymbol.Type.ToDisplayString(),
    isNullable,
    depth));

// AFTER
properties.Add(new OrdinalPropertyInfo(
    propertySymbol.Name,
    propertySymbol.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(),
    isNullable,
    depth));
```

**Rationale:**
- Uses Roslyn's built-in `WithNullableAnnotation` method to remove top-level nullable annotation
- Handles all cases: simple types, generics, nested generics
- Preserves inner nullable annotations (e.g., `List<string?>` remains `List<string?>` after stripping outer `?`)
- More robust than string manipulation
- Prevents the bug at the source rather than fixing it downstream

### Option 2: Improved String Stripping (Alternative)

If we want to keep the fix in the renderer, improve the string manipulation logic:

**Location:** `src/Generator/Renderer/OrdinalRenderer.cs:165-168`

**Change:**
```csharp
// BEFORE
if (typeName.EndsWith("?") && !typeName.Contains("<"))
{
    typeName = typeName.TrimEnd('?');
}

// AFTER
if (typeName.EndsWith("?"))
{
    typeName = typeName.TrimEnd('?');
}
```

**Rationale:**
- Simpler logic
- Handles generic types
- Risk: Might strip `?` from complex nested scenarios incorrectly

**Recommendation:** Use **Option 1** (strip at source) as it's cleaner and more correct.

---

## Detailed Implementation Steps

### 1. Fix the Type Extraction (Option 1 - Recommended)

**File:** `src/Generator/FactoryGenerator.Types.cs`
**Line:** 371
**Method:** `CollectPropertiesRecursive`

**Change:**
```csharp
properties.Add(new OrdinalPropertyInfo(
    propertySymbol.Name,
    propertySymbol.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(),
    isNullable,
    depth));
```

**Required using:** Already present (`using Microsoft.CodeAnalysis;`)

### 2. Remove Redundant Fix in Renderer

**File:** `src/Generator/Renderer/OrdinalRenderer.cs`
**Lines:** 161-170
**Action:** Remove or simplify the nullable stripping code since it will no longer be needed

**Current code:**
```csharp
var propertyTypesArray = string.Join(", ", model.Properties.Select(p =>
{
    var typeName = p.Type;
    // Strip nullable annotation for typeof() (CS8639)
    if (typeName.EndsWith("?") && !typeName.Contains("<"))
    {
        typeName = typeName.TrimEnd('?');
    }
    return $"typeof({typeName})";
}));
```

**Updated code:**
```csharp
var propertyTypesArray = string.Join(", ", model.Properties.Select(p => $"typeof({p.Type})"));
```

**Rationale:** If Option 1 is implemented, the type string will already be free of nullable annotations, making this strip unnecessary.

### 3. Add Comprehensive Tests

**File:** `src/Tests/FactoryGeneratorTests/Factory/RecordTestObjects.cs`
**Location:** After line 215

**Add test records:**
```csharp
// ============================================================================
// Record with nullable collection
// ============================================================================

/// <summary>
/// Record with nullable collection property to verify typeof() generation.
/// Tests that typeof(List<string>) is generated correctly (without trailing ?).
/// </summary>
[Factory]
[Create]
public partial record RecordWithNullableCollection(string Name, List<string>? Items);

// ============================================================================
// Record with complex nullable generics
// ============================================================================

/// <summary>
/// Record with complex nullable generic types to verify edge cases.
/// Tests nested generics with mixed nullability.
/// </summary>
[Factory]
[Create]
public partial record RecordWithComplexNullableGenerics(
    string Name,
    Dictionary<string, int>? Metadata,
    List<string?>? NullableItems,
    List<List<string>?>? NestedNullable);
```

### 4. Add Unit Tests for Generated Code

**File:** Create or update `src/Tests/FactoryGeneratorTests/Factory/OrdinalSerializationTests.cs`

**Add test methods:**
```csharp
[Fact]
public void NullableCollection_GeneratesValidPropertyTypesArray()
{
    // Arrange & Act
    var propertyTypes = RecordWithNullableCollection.PropertyTypes;

    // Assert
    Assert.Equal(2, propertyTypes.Length);
    Assert.Equal(typeof(List<string>), propertyTypes[0]); // NOT List<string>?
    Assert.Equal(typeof(string), propertyTypes[1]);
}

[Fact]
public void NullableGenericTypes_GenerateWithoutOuterNullableAnnotation()
{
    // Arrange & Act
    var propertyTypes = RecordWithComplexNullableGenerics.PropertyTypes;

    // Assert - all types should be without trailing ?
    Assert.Equal(typeof(Dictionary<string, int>), propertyTypes[0]);
    Assert.Equal(typeof(List<string>), propertyTypes[1]); // Inner ? preserved in generic arg
    Assert.Equal(typeof(List<List<string>>), propertyTypes[2]);
}

[Fact]
public void NullableCollection_SerializesAndDeserializesCorrectly()
{
    // Arrange
    var record = new RecordWithNullableCollection("Test", new List<string> { "a", "b" });
    var options = new JsonSerializerOptions();
    NeatooJsonSerializer.ConfigureOptions(options);

    // Act
    var json = JsonSerializer.Serialize(record, options);
    var deserialized = JsonSerializer.Deserialize<RecordWithNullableCollection>(json, options);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal("Test", deserialized.Name);
    Assert.Equal(2, deserialized.Items?.Count);
}

[Fact]
public void NullableCollection_SerializesNullCorrectly()
{
    // Arrange
    var record = new RecordWithNullableCollection("Test", null);
    var options = new JsonSerializerOptions();
    NeatooJsonSerializer.ConfigureOptions(options);

    // Act
    var json = JsonSerializer.Serialize(record, options);
    var deserialized = JsonSerializer.Deserialize<RecordWithNullableCollection>(json, options);

    // Assert
    Assert.NotNull(deserialized);
    Assert.Equal("Test", deserialized.Name);
    Assert.Null(deserialized.Items);
}
```

### 5. Verify Generated Code Compiles

**Action:** After implementing the fix, build the solution and verify that the generated `.Ordinal.g.cs` files compile without CS8639 errors.

**Expected generated code for `RecordWithNullableCollection`:**
```csharp
public static Type[] PropertyTypes { get; } = new[]
{
    typeof(List<string>),  // ✓ No trailing ?
    typeof(string)
};
```

---

## Edge Cases to Consider

### Case 1: Simple Nullable Reference Types
```csharp
string? Name
```
**Expected:** `typeof(string)` ✓

### Case 2: Nullable Value Types
```csharp
int? Age
```
**Expected:** `typeof(int)` (Roslyn represents as `Nullable<int>`) ✓

### Case 3: Nullable Generic Types
```csharp
List<string>? Items
```
**Expected:** `typeof(List<string>)` ✓

### Case 4: Generic with Nullable Type Argument
```csharp
List<string?> Items
```
**Expected:** `typeof(List<string>)` (inner `?` is compile-time annotation on `string`, not part of `List<>` type) ✓

### Case 5: Complex Nested Nullability
```csharp
List<List<string>?>? NestedItems
```
**Expected:** `typeof(List<List<string>>)` (both outer `?` removed by `WithNullableAnnotation`, inner `?` also removed as it's on the type argument) ✓

### Case 6: Dictionary with Nullable Value Type
```csharp
Dictionary<string, int?>? Metadata
```
**Expected:** `typeof(Dictionary<string, int>)` (outer `?` removed, inner `int?` becomes `Nullable<int>` in runtime representation) ✓

**Note:** The `WithNullableAnnotation(NullableAnnotation.NotAnnotated)` method only removes the **top-level** nullable annotation. Nullable annotations on type arguments within generics are preserved in the type's structure but don't affect the `typeof()` expression since they're compile-time only.

---

## Files to Modify

### 1. Source Code Changes

| File | Line | Change | Reason |
|------|------|--------|--------|
| `src/Generator/FactoryGenerator.Types.cs` | 371 | Add `.WithNullableAnnotation(NullableAnnotation.NotAnnotated)` before `.ToDisplayString()` | Fix root cause |
| `src/Generator/Renderer/OrdinalRenderer.cs` | 161-170 | Simplify nullable stripping code (optional cleanup) | Remove redundant fix |

### 2. Test Files

| File | Action | Content |
|------|--------|---------|
| `src/Tests/FactoryGeneratorTests/Factory/RecordTestObjects.cs` | Add | `RecordWithNullableCollection`, `RecordWithComplexNullableGenerics` |
| `src/Tests/FactoryGeneratorTests/Factory/OrdinalSerializationTests.cs` | Add/Update | Unit tests for nullable collection serialization |

### 3. Generated Files (Verify After Build)

| File | Expected Change |
|------|----------------|
| `src/Tests/FactoryGeneratorTests/Generated/.../RecordWithNullableCollection.Ordinal.g.cs` | `PropertyTypes` array contains `typeof(List<string>)` without `?` |

---

## Testing Strategy

### 1. Unit Tests (PropertyTypes Validation)
- Verify `PropertyTypes` array contains correct runtime types
- Verify no trailing `?` in `typeof()` expressions
- Test simple nullable, nullable collections, complex nested generics

### 2. Integration Tests (Serialization Round-Trip)
- Serialize and deserialize records with nullable collections
- Test null values
- Test non-null values
- Verify data integrity

### 3. Compilation Tests
- Build solution in Debug and Release modes
- Verify no CS8639 compiler errors
- Check all generated `.Ordinal.g.cs` files compile

### 4. Regression Tests
- Run existing ordinal serialization tests
- Ensure no breaking changes to existing functionality

---

## Implementation Checklist

- [ ] Modify `FactoryGenerator.Types.cs` line 371 to use `WithNullableAnnotation`
- [ ] Simplify `OrdinalRenderer.cs` lines 161-170 (optional cleanup)
- [ ] Add `RecordWithNullableCollection` test record
- [ ] Add `RecordWithComplexNullableGenerics` test record
- [ ] Add unit tests for `PropertyTypes` validation
- [ ] Add integration tests for serialization round-trip
- [ ] Build solution and verify no CS8639 errors
- [ ] Run all existing tests to ensure no regressions
- [ ] Verify generated code for new test records
- [ ] Update documentation if needed

---

## Potential Risks and Mitigation

### Risk 1: Breaking Change to Generated Code
**Impact:** PropertyTypes array will change for types with nullable reference type properties
**Mitigation:** This is a bug fix, not a feature change. The previous generated code was invalid and didn't compile.
**Severity:** Low

### Risk 2: Roslyn API Compatibility
**Impact:** `WithNullableAnnotation` might not be available in netstandard2.0 (generator target framework)
**Mitigation:** Verify API availability. The `WithNullableAnnotation` method is part of `ITypeSymbol` and should be available in Roslyn for netstandard2.0.
**Severity:** Low - easily verifiable during implementation

### Risk 3: Unexpected Behavior with Complex Generics
**Impact:** Edge cases with deeply nested generics might behave unexpectedly
**Mitigation:** Comprehensive test coverage with edge cases outlined above
**Severity:** Low - covered by test plan

---

## Success Criteria

1. ✅ Solution builds without CS8639 errors
2. ✅ All existing tests pass
3. ✅ New tests for nullable collections pass
4. ✅ Generated `PropertyTypes` arrays contain valid `typeof()` expressions
5. ✅ Serialization round-trip works for nullable collection properties
6. ✅ No regressions in existing ordinal serialization functionality
