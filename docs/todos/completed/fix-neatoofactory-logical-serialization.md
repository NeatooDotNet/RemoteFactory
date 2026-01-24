# Fix NeatooFactory.Logical - Remove Incorrect Serialization

## Status: Complete

**Completed:** 2026-01-24

## History

The three-mode enum was introduced in commit `dfe021ab` on May 13, 2025 (version 9.15.0). Before this, there were only two modes: `Local` and `Remote`.

The commit added the `Local` mode (later renamed to `Logical`) with this comment:
```csharp
else if (remoteLocal == NeatooFactory.Local)
{
    // Client Only
    // We still Serialize the objects
    // but we don't need to make a call to the server
    services.AddScoped<IMakeRemoteDelegateRequest, MakeLocalSerializedDelegateRequest>();
}
```

The serialization was included from the very beginning - this was not a regression but an original (incorrect) design decision.

## The Issue

`NeatooFactory.Logical` was performing JSON serialization when it should not.

### Intended Semantics

| Mode | Description | Serialization |
|------|-------------|---------------|
| `NeatooFactory.Server` | Server in 3-tier architecture | Deserializes requests, serializes responses |
| `NeatooFactory.Remote` | Client in 3-tier architecture | Serializes requests, deserializes responses |
| `NeatooFactory.Logical` | Single-tier app, no server | **NO serialization** |

## Solution Implemented

1. **Removed serialization from `NeatooFactory.Logical`** - Changed DI registration so Logical mode no longer registers `IMakeRemoteDelegateRequest`, causing generated factories to use the local constructor (direct execution).

2. **Deleted `MakeLocalSerializedDelegateRequest`** - This class was no longer needed after the fix.

3. **Added verification tests** - Created `ModeBehaviorTests.cs` with 8 tests to verify:
   - `NeatooFactory.Server`: Does NOT register `IMakeRemoteDelegateRequest`
   - `NeatooFactory.Logical`: Does NOT register `IMakeRemoteDelegateRequest` (fixed behavior)
   - `NeatooFactory.Remote`: DOES register `IMakeRemoteDelegateRequest`
   - Server and Logical modes use local constructor (identical behavior)
   - All factory operations execute correctly in both modes

4. **Updated documentation**:
   - Updated NeatooFactory enum XML docs
   - Updated `factory-modes.md` reference documentation
   - Updated `LogicalContainerBuilder` and `LocalContainerBuilder` remarks
   - Updated `LogicalModeTests` remarks

## Files Changed

### Modified
- `src/RemoteFactory/AddRemoteFactoryServices.cs` - Removed Logical mode serialization registration
- `docs/reference/factory-modes.md` - Updated Logical mode documentation
- `src/Tests/RemoteFactory.UnitTests/TestContainers/LogicalContainerBuilder.cs` - Updated XML remarks
- `src/Tests/RemoteFactory.IntegrationTests/TestContainers/LocalContainerBuilder.cs` - Updated XML remarks
- `src/Tests/RemoteFactory.UnitTests/Logical/LogicalModeTests.cs` - Updated XML remarks

### Deleted
- `src/RemoteFactory/Internal/MakeLocalSerializedDelegateRequest.cs` - No longer needed

### Created
- `src/Tests/RemoteFactory.UnitTests/FactoryModes/ModeBehaviorTests.cs` - Verification tests for mode behavior

## Test Results

All 1,948 tests pass across all three target frameworks (net8.0, net9.0, net10.0):
- 440 UnitTests per framework
- 432 IntegrationTests per framework
- Plus other test projects

The new `ModeBehaviorTests` (8 tests) verify the fix works correctly.
