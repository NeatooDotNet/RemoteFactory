# Ordinal Serialization Code Review - Action Items

## Overview
Comprehensive code review findings for the compact/ordinal JSON serialization feature.

**Status: COMPLETED** - All items implemented and tested.

---

## High Priority

### 1. Cache JsonSerializerOptions for Fallback Path
**File:** `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs`

**Status:** [x] COMPLETED

Added `ConcurrentDictionary<JsonSerializerOptions, JsonSerializerOptions>` to cache fallback options. Removed the `#pragma warning disable CA1869` workaround.

---

### 2. Standardize Field Naming Conventions
**File:** `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs`

**Status:** [x] COMPLETED

Renamed field from `_options` to `options` for naming consistency with the rest of the codebase.

---

### 3. Support Init-Only Properties in Records
**File:** `src/Generator/FactoryGenerator.Types.cs`

**Status:** [x] COMPLETED

Updated comment to clarify that init accessors are supported for deserialization. The existing `SetMethod != null` check already includes init-only properties.

---

## Medium Priority

### 4. Add Diagnostic Warning for Nested Types
**Files:** `src/Generator/DiagnosticDescriptors.cs`, `src/Generator/FactoryGenerator.cs`

**Status:** [x] COMPLETED

Added `NF0207` diagnostic descriptor `NestedTypeOrdinalSkipped`. Updated `GenerateOrdinalSerialization` to report diagnostic when nested types are skipped.

---

### 5. Improve Error Messages with Property Names
**File:** `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs`

**Status:** [x] COMPLETED

Updated error messages to include property names list for easier debugging of version mismatch issues.

---

### 6. Align Test Defaults with Runtime Defaults
**File:** `src/Tests/FactoryGeneratorTests/ClientServerContainers.cs`

**Status:** [x] COMPLETED

Added XML documentation explaining why test default (Named) differs from production default (Ordinal). Decision: Keep test default as Named for backwards compatibility.

---

### 7. Add Missing Test Scenarios
**File:** `src/Tests/FactoryGeneratorTests/Factory/OrdinalSerializationTests.cs`

**Status:** [x] COMPLETED

Added new tests for:
- Nested record types
- Default values handling

---

### 8. Fix Thread Safety Issues in Test Helper
**File:** `src/Tests/FactoryGeneratorTests/ClientServerContainers.cs`

**Status:** [x] COMPLETED

- Changed lock object to `readonly`
- Changed `Dictionary` to `ConcurrentDictionary`
- Updated `Scopes` method to use `GetOrAdd` for thread-safe initialization

---

## Low Priority (Style/Cleanup)

### 9. Replace Verbose Boolean Comparisons
**Files:** `src/Generator/FactoryGenerator.Types.cs`, `src/RemoteFactory/Internal/NeatooOrdinalConverterFactory.cs`

**Status:** [x] COMPLETED

Replaced `== false` with `!` operator throughout.

---

### 10. Remove Obsolete [Serializable] Attribute
**File:** `src/RemoteFactory/Internal/NeatooJsonSerializer.cs`

**Status:** [x] COMPLETED

Removed obsolete `[Serializable]` attribute from `MissingDelegateException`.

---

### 11. Move Exception Class to Own File
**New File:** `src/RemoteFactory/Internal/MissingDelegateException.cs`

**Status:** [x] COMPLETED

Created separate file with proper documentation. Removed from `NeatooJsonSerializer.cs`.

---

### 12. Consolidate Nullable Type Detection Logic
**File:** `src/Generator/FactoryGenerator.cs`

**Status:** [x] COMPLETED

Simplified nullable type detection logic in `GenerateOrdinalSerialization`. Added `isEffectivelyNullable` variable for clarity.

---

## Documentation Tasks

### 13. Document .NET 7+ Requirement
**Status:** [x] NOT NEEDED

Project already requires .NET 8.0+ (all three supported frameworks support static abstract members).

---

## Summary

| Priority | Count | Status |
|----------|-------|--------|
| High     | 3     | All completed |
| Medium   | 5     | All completed |
| Low      | 4     | All completed |
| Docs     | 1     | Not needed |

**Test Results:**
- Build: Succeeded with 0 warnings, 0 errors
- Tests: 430 passed, 5 skipped (increased from 424 due to new tests)
- All tests pass across .NET 8.0, 9.0, and 10.0
