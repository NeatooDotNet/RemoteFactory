# Architect -- Fix DTO Trimming Deserialization

Last updated: 2026-03-25
Current step: Post-implementation verification complete (Step 8A)

## Key Context

### Problem
`NeatooJsonSerializer.Deserialize<T>()` fails for plain DTO classes under IL trimming. STJ uses reflection to find constructors; the trimmer strips the metadata that `DefaultJsonTypeInfoResolver` uses.

### Chosen Approach (Revised)
The fix is the missing `else` branch in `NeatooJsonTypeInfoResolver.GetTypeInfo()`. When `CreateObject is null` and the type is not a DI service, fall back to `Activator.CreateInstance(type)`. No generator changes needed. One branch addition.

### Key File
- `src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs` -- `else if` branch with constructor guard and `Activator.CreateInstance` fallback

## Mistakes to Avoid
- The original plan was over-engineered: DtoConstructorRegistry, generator changes, DtoTypeDiscovery utility -- all unnecessary
- Do NOT modify any generator code for this fix
- A plain `else` branch causes 9 test failures because `RecordBypassConverter.Read()` re-enters the resolver for record types. Records have no parameterless constructor, so `Activator.CreateInstance` throws `MissingMethodException`. The constructor guard is essential.

## User Corrections
- User doubted `[DynamicallyAccessedMembers]` would work -- research confirmed user was right
- User identified the fix as a simple `else` branch, rejecting the generator-based approach as over-engineered
- Phase 2 (consumer `JsonSerializerContext`) identified as the architecturally correct long-term solution, but out of scope

## Architect Verification (Post-Implementation)

### Verdict: VERIFIED

### Independent Build Results
- **Solution build:** Succeeded, 0 errors, 2 warnings (Blazor WASM native file references -- pre-existing, unrelated)

### Independent Test Results
| Suite | Framework | Passed | Failed | Skipped |
|-------|-----------|--------|--------|---------|
| UnitTests | net9.0 | 490 | 0 | 0 |
| UnitTests | net10.0 | 490 | 0 | 0 |
| IntegrationTests | net9.0 | 502 | 0 | 3 |
| IntegrationTests | net10.0 | 502 | 0 | 3 |
| Design.Tests | net9.0 | 42 | 0 | 0 |
| Design.Tests | net10.0 | 42 | 0 | 0 |
| **Total** | | **2,068** | **0** | **6** |

The 6 skipped tests are `ShowcasePerformanceTests` (3 per framework) -- pre-existing, unrelated to this change.

### Test Scenario Coverage: 5 of 5 verified

| # | Plan Scenario | Mapped Test Method | File | Exists | Passes |
|---|--------------|-------------------|------|--------|--------|
| 1 | Plain DTO round-trip | `InterfaceFactory_GetByIdAsync_ReturnsSpecificItem` | `src/Design/Design.Tests/FactoryTests/InterfaceFactoryTests.cs:64` | Yes | Yes |
| 2 | Record round-trip (no regression) | `InterfaceFactory_SimpleRecord_RoundTrip` | `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceFactoryRecordSerializationTests.cs:39` | Yes | Yes |
| 3 | Neatoo type round-trip (no regression) | Multiple existing tests across integration/unit suites | Various | Yes | Yes (502 integration tests pass) |
| 4 | DTO in collection round-trip | `InterfaceFactory_GetAllAsync_ReturnsDataFromServer` | `src/Design/Design.Tests/FactoryTests/InterfaceFactoryTests.cs:35` | Yes | Yes |
| 5 | All existing tests pass | Full test suite | All | Yes | Yes (2,068 passed, 0 failed) |

### Implementation vs Design Match

**Plan specified:** Plain `else` branch with `Activator.CreateInstance(type)`.

**Developer implemented:** `else if` with constructor guard: `type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) is not null`.

**Deviation assessment:** Acceptable and necessary. The plan's Risk #2 explicitly anticipated this scenario: "Activator.CreateInstance(type) will throw MissingMethodException for types without a public parameterless constructor." The developer discovered that `RecordBypassConverter.Read()` re-enters the resolver via inner options (which lack the bypass converter factory). Record types that have no parameterless constructor would reach the `else` branch and `Activator.CreateInstance` would throw. The constructor guard prevents this by ensuring only types with an accessible public parameterless constructor get the `Activator.CreateInstance` fallback. Types without one (records, abstract classes) are left for STJ's own constructor resolution.

**Code review of modified file (`NeatooJsonTypeInfoResolver.cs`):**
- Line 29: Added `&& type is not null` null guard -- defensive, harmless
- Line 38: `else if` with `BindingFlags.Public | BindingFlags.Instance` constructor check -- correct binding flags for public parameterless constructors
- Lines 40-47: Comment accurately explains the rationale (trimmer strips metadata, Activator uses different path, guard for parameterless constructors only)
- Line 47: `Activator.CreateInstance(type)!` -- correct, null-forgiving operator appropriate since we verified the constructor exists
- No other files modified

**Scope containment:** Single file modified (`NeatooJsonTypeInfoResolver.cs`), no generator changes, no new classes -- matches the plan's minimal scope.

### Design Project Verification
Design.Tests: 42 passed, 0 failed on both net9.0 and net10.0. The DTO round-trip tests (`GetByIdAsync`, `GetAllAsync`) exercise the exact code path this fix addresses.
