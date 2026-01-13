# GAP-004: Interface Factory Authorization Enforcement (REGRESSION)

## Problem

The CancellationToken feature changes accidentally removed authorization enforcement from interface factories. This is a **regression** - authorization was working in origin/main (commit ff12771) but is broken locally.

Interface factories with `[AuthorizeFactory<T>]` generate `Can*` methods for checking authorization, but the actual operation methods (`GetData`, `ProcessData`, etc.) no longer enforce authorization before execution.

**Class-based factories** enforce authorization:
```csharp
public virtual IShowcaseAuthObj? Save(IShowcaseAuthObj target, CancellationToken cancellationToken = default)
{
    var authorized = LocalSave(target, cancellationToken);
    if (!authorized.HasAccess)
    {
        throw new NotAuthorizedException(authorized);
    }
    return authorized.Result;
}
```

**Interface factories** do not:
```csharp
public virtual Task<string> GetData(Guid id)
{
    return GetDataProperty(id);  // No auth check!
}
```

## Current Behavior

- `Can*` methods are generated and work correctly
- UI can check `CanGetData(id)` before calling `GetData(id)`
- But `GetData(id)` executes without enforcing the check
- Unauthorized calls succeed when they should throw `NotAuthorizedException`

## Expected Behavior

Interface factory methods should check authorization before execution:
```csharp
public virtual Task<string> GetData(Guid id)
{
    var authorized = CanGetData(id);
    if (!authorized.HasAccess)
    {
        throw new NotAuthorizedException(authorized);
    }
    return GetDataProperty(id);
}
```

## Failing Tests

10 tests in `InterfaceFactoryAuthTests.cs` fail because they expect `NotAuthorizedException`:
- `InterfaceAuth_GetData_AuthorizationFailsBool_ThrowsException`
- `InterfaceAuth_ProcessData_AuthorizationFailsBool_ThrowsException`
- `InterfaceAuth_CheckStatus_AuthorizationFailsBool_ThrowsException`
- `InterfaceAuth_GetData_AuthorizationFailsString_ThrowsExceptionWithMessage`
- `InterfaceAuth_ProcessData_AuthorizationFailsString_ThrowsExceptionWithMessage`
- `InterfaceAuthDeny_GetDeniedData_AuthorizationAlwaysFails`
- `InterfaceAuthDeny_GetDeniedData_FailsWithMessage`
- `InterfaceAuth_DirectInterface_GetData_AuthorizationFails`
- `InterfaceAuth_DifferentIdScenarios_EachAuthorizedIndependently`

## Root Cause

In `FactoryGenerator.Types.cs`, the `InterfaceFactoryMethod.PublicMethod` was changed from:
```csharp
// Origin/main - delegates to base which includes auth enforcement
public override StringBuilder PublicMethod(bool? overrideHasAuth = null) => base.PublicMethod(false);
```

To:
```csharp
// CancellationToken changes - custom implementation that bypasses auth
public override StringBuilder PublicMethod(bool? overrideHasAuth = null)
{
    // InterfaceFactoryMethod always uses hasAuth = false, so no auth handling needed  <-- WRONG
    ...
    methodBuilder.AppendLine($"return Local{this.UniqueName}({this.ParameterIdentifiersText()});");
    ...
}
```

The comment "no auth handling needed" was misleading - `hasAuth = false` controls the return type wrapper, not whether auth is checked.

## Fix

Restore the call to `base.PublicMethod(false)` while preserving the CancellationToken signature requirements for interface methods.

## Tasks

- [x] Override `LocalMethodStart` in `InterfaceFactoryMethod` to include auth enforcement
- [x] Verify CancellationToken handling still works for interface factories
- [x] Run `InterfaceFactoryAuthTests` to verify fix (25 tests pass)
- [x] Run full test suite to check for regressions (519 tests pass)
- [x] Add `AuthorizationEnforcementTests` - side-by-side tests for class and interface factory authorization

## Files

- Generator: `src/Generator/FactoryGenerator.cs`
- Tests: `src/Tests/FactoryGeneratorTests/InterfaceFactory/InterfaceFactoryAuthTests.cs`
- Generated example: `src/Tests/FactoryGeneratorTests/Generated/.../IAuthorizedServiceFactory.g.cs`

## Reference

- Test plan: `docs/todos/FACTORY_TEST_PLAN.md` (GAP-004)
- Class factory with auth: `ShowcaseAuthObjFactory.g.cs` (for reference pattern)
