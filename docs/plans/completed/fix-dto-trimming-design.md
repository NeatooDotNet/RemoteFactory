# Fix DTO Trimming Deserialization -- Design Plan

**Date:** 2026-03-25
**Related Todo:** [Fix NeatooJsonSerializer Trimming Failure for Non-Neatoo DTOs](../todos/fix-dto-trimming-deserialization.md)
**Status:** Complete
**Last Updated:** 2026-03-25

---

## Overview

When a Blazor WASM client is published with IL trimming, `NeatooJsonSerializer.Deserialize<T>()` fails for plain DTO classes returned by Interface Factory and Static Factory methods. The trimmer strips the parameterless constructor metadata that STJ's `DefaultJsonTypeInfoResolver` needs. The fix: add an `else` branch in `NeatooJsonTypeInfoResolver.GetTypeInfo()` that uses `Activator.CreateInstance` as a fallback for non-DI types. No generator changes needed.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/fix-dto-trimming-deserialization.md#requirements-review)

### Relevant Existing Requirements

#### Business Rules

- **`NeatooJsonTypeInfoResolver` CreateObject pattern** (`src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs:28-36`): The resolver already sets `CreateObject` for Neatoo types via DI when `CreateObject is null`. The fix adds the missing `else` branch for non-DI types. -- Relevance: Direct extension of existing pattern.

- **Interface Factory returning DTOs/records is first-class** (`CLAUDE-DESIGN.md`, Quick Decisions Table): "Can Interface Factory return a record? Yes, plain records/DTOs without Neatoo types." -- Relevance: DTO return types must work correctly under trimming.

- **RecordBypassConverterFactory claims parameterized-constructor types** (`src/RemoteFactory/Internal/RecordBypassConverterFactory.cs`): Records with only parameterized constructors are handled by a separate converter. They never reach `NeatooJsonTypeInfoResolver.CreateObject`. -- Relevance: Records are unaffected by this change.

#### Existing Tests

- `src/Design/Design.Tests/FactoryTests/InterfaceFactoryTests.cs`: Tests Interface Factory with `ExampleDto` (class) and `ExampleRecordResult` (record) return types. -- Relevance: These tests validate the serialization path but not under IL trimming conditions.

### Gaps

1. **No integration test for DTO round-trip through two-container pattern.** Existing Design tests validate the pattern works but `ClientServerContainers` tests should confirm the serialization path explicitly.

### Contradictions

None.

### Recommendations for Architect

1. Follow the existing `CreateObject` pattern in `NeatooJsonTypeInfoResolver` -- this is just the missing `else` branch.
2. Verify records are unaffected (handled by `RecordBypassConverterFactory`).
3. Verify on both net9.0 and net10.0.

---

## Business Rules (Testable Assertions)

1. WHEN `NeatooJsonTypeInfoResolver.GetTypeInfo()` is called for a type where `CreateObject is null` AND the type is registered in DI, THEN `CreateObject` is set to resolve from DI. -- Source: Existing `NeatooJsonTypeInfoResolver` pattern (no change)

2. WHEN `NeatooJsonTypeInfoResolver.GetTypeInfo()` is called for a type where `CreateObject is null` AND the type is NOT registered in DI, THEN `CreateObject` is set to `Activator.CreateInstance(type)`. -- Source: NEW

3. WHEN a plain DTO class is returned by an Interface Factory method and serialized/deserialized through `NeatooJsonSerializer`, THEN the round-trip succeeds with all properties preserved. -- Source: Interface Factory DTO requirement + NEW (trimming)

4. WHEN a record with only parameterized constructors is returned by a factory method, THEN it continues to be handled by `RecordBypassConverterFactory` (no regression). -- Source: RecordBypassConverterFactory requirement

5. WHEN a Neatoo type (DI-registered) is deserialized, THEN it continues to use DI-based `CreateObject` (no regression). -- Source: Existing `NeatooJsonTypeInfoResolver` pattern

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Plain DTO round-trip via ClientServer | Interface Factory returns `ExampleDto` with properties set; serialize/deserialize through two-container pattern | 2, 3 | Deserialization succeeds, all property values preserved |
| 2 | Record round-trip (no regression) | Interface Factory returns `ExampleRecordResult` | 4 | Deserialization succeeds (handled by RecordBypassConverterFactory, not affected by this change) |
| 3 | Neatoo type round-trip (no regression) | Factory creates a Neatoo type registered in DI | 1, 5 | Deserialization uses DI, succeeds |
| 4 | DTO in collection round-trip | Interface Factory returns `List<ExampleDto>` | 2, 3 | Each element deserialized correctly |
| 5 | All existing tests pass | Full test suite | 1-5 | Zero regressions |

---

## Approach

### Strategy

Add a single `else` branch to `NeatooJsonTypeInfoResolver.GetTypeInfo()`. When `CreateObject is null` and the type is not a DI service, fall back to `Activator.CreateInstance(type)`.

This works because:
- `Activator.CreateInstance` uses a different code path than STJ's default constructor discovery, avoiding the trimmed metadata
- DTO types referenced as property types on preserved (DI-registered) Neatoo types will have their property metadata survive trimming
- No generator changes are needed -- the resolver handles all types dynamically at runtime

---

## Design

### Single File Change: `NeatooJsonTypeInfoResolver.cs`

Location: `src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs`

The existing code at lines 28-37:

```csharp
if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object && jsonTypeInfo.CreateObject is null)
{
    if (this.ServiceProviderIsService.IsService(type))
    {
        jsonTypeInfo.CreateObject = () =>
        {
            return this.ServiceProvider.GetRequiredService(type);
        };
    }
}
```

Becomes:

```csharp
if (jsonTypeInfo.Kind == JsonTypeInfoKind.Object && jsonTypeInfo.CreateObject is null)
{
    if (this.ServiceProviderIsService.IsService(type))
    {
        jsonTypeInfo.CreateObject = () =>
        {
            return this.ServiceProvider.GetRequiredService(type);
        };
    }
    else
    {
        // Plain DTOs: the IL trimmer strips constructor metadata that STJ's
        // DefaultJsonTypeInfoResolver needs. Activator.CreateInstance uses a
        // different code path. The DTO type's metadata survives because it's
        // referenced as a property type on preserved (DI-registered) types.
        jsonTypeInfo.CreateObject = () => Activator.CreateInstance(type)!;
    }
}
```

No new classes. No generator changes. No `DtoConstructorRegistry`.

---

## Implementation Steps

### Phase 1: Fix and Tests

1. Add the `else` branch to `NeatooJsonTypeInfoResolver.GetTypeInfo()` in `src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs`
2. Add/verify integration tests using `ClientServerContainers` that confirm plain DTO round-trip works (scenarios 1, 2, 3, 4 from the test scenarios table)
3. Run full test suite to confirm zero regressions

---

## Acceptance Criteria

- [ ] Plain DTO classes returned by Interface Factory methods deserialize correctly in two-container tests
- [ ] Records with parameterized constructors continue to work (no regression)
- [ ] Neatoo types continue to use DI-based `CreateObject` (no regression)
- [ ] All existing tests pass on both net9.0 and net10.0

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Fix + Tests | developer | Yes | Single small change + test verification | None |

**Parallelizable phases:** None -- single phase.

**Notes:** This is a one-line production code change. The bulk of the work is verifying existing tests pass and adding any missing round-trip integration tests.

---

## Dependencies

- No new dependencies. Uses existing `Activator.CreateInstance` from the BCL.

---

## Risks / Considerations

1. **Property metadata survival under trimming**: `Activator.CreateInstance` fixes the constructor half of trimming. If the trimmer also strips property metadata for a DTO type, deserialization would produce empty objects with no exceptions (silent data loss). In practice, DTO types referenced as property types on preserved (DI-registered) Neatoo types will have their metadata survive. This is sufficient for the known use cases.

2. **Types without parameterless constructors**: `Activator.CreateInstance(type)` will throw `MissingMethodException` for types without a public parameterless constructor. This is the same failure mode as pre-trimming STJ behavior, so it is not a regression. Records with only parameterized constructors are handled by `RecordBypassConverterFactory` before reaching this code.

3. **Phase 2 future work (out of scope)**: The architecturally correct long-term solution is to let consumers register their own `JsonSerializerContext` (STJ source generation). This is Microsoft's recommended pattern for library authors to support trimming fully. It would handle both constructor and property metadata. This should be tracked as a separate future enhancement, not part of this fix.

4. **Performance**: `Activator.CreateInstance` is slower than a direct `new T()` call. For the deserialization use case this is negligible -- the JSON parsing dominates. If perf becomes a concern, the Phase 2 `JsonSerializerContext` approach would address it.
