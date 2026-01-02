# Ordinal Serialization Documentation Review - Action Items

## Overview
Documentation review findings for the compact/ordinal JSON serialization feature to ensure docs match implementation.

**Status:** COMPLETED

---

## High Priority

### 1. Update Implementation Plan - Mark Phase 6 as Deferred
**File:** `docs/todos/compact-serialization-plan.md`

**Issue:** Phase 6 documents ObjectJson property shortening (`J` and `T` instead of `Json` and `AssemblyType`) but this was NOT implemented. The actual implementation still uses full property names.

**Action:**
- Add "Implementation Status" section at top of document
- Mark Phase 6 as "DEFERRED - Not implemented in initial release"
- Update document status to "COMPLETED - Implementation Notes"

- [x] Update compact-serialization-plan.md with implementation status

---

### 2. Update Implementation Plan - Correct Phase 4
**File:** `docs/todos/compact-serialization-plan.md`

**Issue:** Phase 4 states that interface serialization writes `$t`/`$v` property names, but actual implementation:
- **Writes:** `$type`/`$value` (full names)
- **Reads:** Both `$type`/`$value` AND `$t`/`$v` (backward compatible)

**Action:**
- Update Phase 4 to reflect actual implementation
- Note that short names are read-only for compatibility

- [x] Correct Phase 4 documentation

---

### 3. Create Release Notes
**File:** `docs/release-notes/v10.2.0.md` (or next version)

**Issue:** This significant feature has no release notes.

**Required sections:**
- Overview (40-50% payload reduction)
- What's New:
  - Ordinal serialization format
  - `SerializationFormat` enum
  - `NeatooSerializationOptions` class
  - `IOrdinalSerializable` / `IOrdinalSerializationMetadata` interfaces
  - NF0207 diagnostic for nested types
- Breaking Changes (default format changed to Ordinal)
- Migration Guide (how to revert to Named format)

- [x] Create release notes file
- [x] Update `docs/release-notes/index.md` highlights table
- [x] Add to "All Releases" list

---

## Medium Priority

### 4. Update User Documentation - Serialization Formats
**File:** `docs/advanced/json-serialization.md`

**Issue:** The 505-line JSON serialization documentation doesn't mention:
- `SerializationFormat` enum
- `NeatooSerializationOptions` class
- `X-Neatoo-Format` HTTP header
- `IOrdinalSerializable` interface
- Compact array format examples
- Configuration options

**Action:** Add new section "Serialization Formats" with:

```markdown
## Serialization Formats

RemoteFactory supports two JSON serialization formats:

### Ordinal Format (Default)

Compact array-based format that eliminates property names:
```json
["John", 42, true]
```

### Named Format

Verbose object-based format with property names:
```json
{"Name":"John","Age":42,"Active":true}
```

### Configuring the Format

```csharp
services.AddNeatooRemoteFactory(NeatooFactory.Server,
    new NeatooSerializationOptions { Format = SerializationFormat.Named });
```

### Format Negotiation

The server communicates its format via the `X-Neatoo-Format` HTTP header.
```

- [x] Add "Serialization Formats" section to json-serialization.md
- [x] Add configuration examples
- [x] Add format negotiation documentation

---

### 5. Document New Diagnostic NF0207
**File:** `docs/reference/diagnostics.md` (if exists) or appropriate location

**Issue:** New diagnostic NF0207 (NestedTypeOrdinalSkipped) is not documented for users.

**Content:**
```markdown
### NF0207: Nested type skipped for ordinal serialization

**Severity:** Info

**Message:** "Nested type '{0}' was skipped for ordinal serialization generation.
Move the type to namespace level to enable compact serialization."

**Cause:** Types nested inside other classes cannot have ordinal serialization
generated due to code generation limitations.

**Resolution:** Move the type to namespace level (not nested inside another class).
```

- [x] Add NF0207 to diagnostics documentation

---

## Low Priority

### 6. Standardize Terminology
**Files:** `docs/todos/compact-serialization-plan.md`, comments throughout

**Issue:** Mixed use of "compact" and "ordinal" terminology.

**Standard terms:**
- Use "Ordinal" (capitalized) in documentation prose
- Use "ordinal" (lowercase) in HTTP header values
- Use "Ordinal format" and "Named format" consistently
- Avoid "compact" except in casual descriptions

- [x] Update plan document title/references to use "Ordinal"
- [x] Review and standardize terminology in code comments

---

### 7. Document Interfaces in API Reference
**File:** `docs/reference/interfaces.md` (if exists)

**Issue:** New public interfaces not documented:
- `IOrdinalSerializable` - Instance method for serialization
- `IOrdinalSerializationMetadata` - Static abstract members for metadata

- [x] Add interface documentation if reference docs exist

---

### 8. Move Plan to Internals/Archive
**Current:** `docs/todos/compact-serialization-plan.md`
**Proposed:** `docs/internals/ordinal-serialization-design.md`

**Issue:** Implementation plans should be moved to internals once complete.

- [x] Rename and move to appropriate location after updates

---

## Discrepancies Summary

| Plan Reference | Documented | Actual Implementation |
|----------------|------------|----------------------|
| Phase 6.1 - ObjectJson | `J` and `T` property names | Still uses `Json` and `AssemblyType` |
| Phase 4.1 - Interface serialization | Write uses `$t`/`$v` | Write uses `$type`/`$value`; Read accepts both |
| Options class naming | `NeatooServerOptions` | `NeatooSerializationOptions` |
| Feature flag | `UseVerboseSerialization` | `SerializationFormat.Named` enum |

---

## Accurate Documentation (No Changes Needed)

These items in the plan match the implementation:
- `SerializationFormat.Ordinal` and `SerializationFormat.Named` enum values
- `IOrdinalSerializable.ToOrdinalArray()` method
- Alphabetical property ordering, base class first
- Ordinal is default format
- `X-Neatoo-Format` header name
- Header values "ordinal" and "named"
- NF0207 diagnostic for nested types
- Static abstract `FromOrdinalArray` in metadata interface

---

## Summary

| Priority | Count | Effort Estimate |
|----------|-------|-----------------|
| High     | 3     | ~1.5 hours      |
| Medium   | 2     | ~1 hour         |
| Low      | 3     | ~30 min         |

**Total Estimated Effort:** ~3 hours
