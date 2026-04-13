# Generate CanSave(T target) for Target-Parameterized Authorization

**Date:** 2026-04-06
**Related Todo:** [CanSave Target Parameter](../todos/cansave-target-parameter.md)
**Status:** Complete
**Last Updated:** 2026-04-06

---

## Overview

When an authorization class has Write-scoped methods with a target type parameter (e.g., `bool CanWrite(IParamAuthOrder target)`), the generator currently suppresses CanSave generation entirely. This is overly conservative — the caller has the entity in hand when they're about to call Save, so `CanSave(target)` is perfectly callable.

This plan adds `CanSave(T target, CancellationToken)` to the `IFactorySave<T>` interface and modifies the generator to produce both parameterless and target-parameterized CanSave methods when target-param auth is configured.

---

## Skills

- `skills/RemoteFactory/SKILL.md` — RemoteFactory patterns, authorization patterns, factory generation rules

---

## Business Rules (Testable Assertions)

1. WHEN an auth class has Write-scoped methods with target parameters, THEN `CanSave(T target)` IS generated on the factory interface and class — NEW
2. WHEN an auth class has Write-scoped methods with target parameters, THEN `CanSave()` (parameterless) IS ALSO generated on the factory — NEW
3. WHEN `CanSave(target)` is called, THEN ALL Write-scoped auth methods are invoked — both parameterless and target-parameterized — same pattern as `CanFetch(Guid)` calling both `CanRead()` and `CanFetchOrder(Guid)` — NEW
4. WHEN `CanSave()` (parameterless) is called, THEN only non-target Write-scoped auth methods are invoked — NEW
5. WHEN `CanSave()` (parameterless) is called AND there are no non-target Write-scoped auth methods, THEN it returns `Authorized(true)` — NEW
6. WHEN `IFactorySave<T>.CanSave(T target, CancellationToken)` is called, THEN it delegates to the concrete `CanSave(target)` method — NEW
7. WHEN `IFactorySave<T>.CanSave(CancellationToken)` is called (parameterless), THEN it runs only non-target auth methods — consistent with Rule 4 — NEW
8. WHEN an auth class has ONLY parameterless Write auth methods (no target params), THEN existing behavior is unchanged — the single CanSave() runs all auth methods — Source: existing ParamAuthOrder suppression + AuthorizedOrder behavior

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | CanSave(target) returns true when all auth passes | target state passes target-param auth, non-target auth also passes | 1, 3 | `HasAccess = true` — all Write-scoped auth methods called and pass |
| 2 | CanSave(target) returns false when target-param auth fails | target state fails target-param auth, non-target auth passes | 1, 3 | `HasAccess = false` — target-param auth method fails |
| 3 | CanSave(target) returns false when non-target auth fails | target state passes, non-target auth fails | 3 | `HasAccess = false` — non-target auth method fails before target-param auth runs |
| 4 | CanSave() parameterless returns true when non-target auth passes | non-target auth passes | 2, 4 | `HasAccess = true` — only non-target auth called, target-param auth not invoked |
| 5 | CanSave() parameterless returns false when non-target auth fails | non-target auth fails | 2, 4 | `HasAccess = false` — non-target auth method fails |
| 6 | IFactorySave<T>.CanSave(target) delegates correctly | target state fails target-param auth | 6 | `HasAccess = false` — delegates to concrete CanSave(target) |
| 7 | IFactorySave<T>.CanSave() parameterless runs non-target auth | non-target auth passes | 7 | `HasAccess = true` — runs only non-target auth |
| 8 | AuthorizedOrder CanSave unchanged | Existing parameterless auth | 8 | Existing tests continue to pass, no regression |
| 9 | CanSave(target) works through client-server serialization | target state fails auth, client→server | 1, 3 | `HasAccess = false` — target auth works across boundary |

---

## Approach

### High-Level Strategy

1. Add `CanSave(T target, CancellationToken)` overload to `IFactorySave<T>` interface
2. Modify generator to produce two CanSave overloads when target-param auth exists
3. Update `RenderCanSaveExplicitInterfaceMethod` to render both interface implementations
4. Update Design project to demonstrate the new behavior with tests

### Key Design Decision: CanSave Follows the Same Pattern as Other Can Methods

CanSave generation follows the same derivation pattern as CanCreate, CanFetch, etc. — the generator examines all Write-scoped auth methods and derives CanSave from them:

- **`CanSave(target)`**: Calls ALL Write-scoped auth methods whose signatures can be satisfied — both parameterless auth methods and target-parameterized ones. This is the same pattern as `CanFetch(Guid)` calling both `CanRead()` and `CanFetchOrder(Guid)`.
- **`CanSave()` (parameterless)**: Calls only the Write-scoped auth methods that don't require a target parameter. If none exist, returns `Authorized(true)`.

### Design Project Update

To test both behaviors (parameterless and target-parameterized CanSave), add a non-target Write-scoped auth method to `ParamAuthOrderAuth` alongside the existing target-parameterized one. This is a test fixture choice — the generator behavior is generic.

---

## Design

### IFactorySave<T> Interface Change

```csharp
public interface IFactorySave<T> where T : IFactorySaveMeta
{
    Task<IFactorySaveMeta?> Save(T entity, CancellationToken cancellationToken = default);
    Task<Authorized> CanSave(CancellationToken cancellationToken = default);           // existing
    Task<Authorized> CanSave(T target, CancellationToken cancellationToken = default); // NEW
}
```

### Generator Changes

#### 1. FactoryModelBuilder.cs — `AddCanMethods` (line 793)

**Current:** Skips CanSave when any auth method has target params (line 805-812).

**New:** When target params exist, generate TWO CanMethodModels:
- **CanSave** (parameterless): Authorization contains only non-target auth methods. If no non-target auth methods exist, generate with empty authorization (returns Authorized(true)).
- **CanSave with target**: Authorization contains ALL auth methods. Parameters include the target parameter.

#### 2. FactoryModelBuilder.cs — `AddCanMethodsForInterface` (line 716)

Same change as above but for the interface factory pattern.

#### 3. ClassFactoryRenderer.cs — `RenderCanSaveExplicitInterfaceMethod` (line 1225)

**Current:** Renders one explicit interface method: `IFactorySave<T>.CanSave(CancellationToken)`.

**New:** Render TWO explicit interface methods:
- `IFactorySave<T>.CanSave(CancellationToken)` — delegates to parameterless CanSave
- `IFactorySave<T>.CanSave(T target, CancellationToken)` — delegates to CanSave(target)

#### 4. ClassFactoryRenderer.cs — `RenderCanMethod` (line 1269)

May need changes to handle rendering two overloaded CanSave methods with different parameter lists.

### Generated Output Example (ParamAuthOrder)

Assuming `ParamAuthOrderAuth` has both a non-target Write auth method and the existing target Write auth method:

```csharp
// Factory-specific interface
public interface IParamAuthOrderFactory
{
    Authorized CanSave(CancellationToken cancellationToken = default);
    Authorized CanSave(IParamAuthOrder target, CancellationToken cancellationToken = default);
}

// Concrete factory class
public partial class ParamAuthOrderFactory
{
    // Parameterless — runs only non-target Write-scoped auth methods
    public virtual Authorized CanSave(CancellationToken cancellationToken = default)
    {
        return LocalCanSave(cancellationToken);
    }

    public Authorized LocalCanSave(CancellationToken cancellationToken = default)
    {
        IParamAuthOrderAuth auth = ServiceProvider.GetRequiredService<IParamAuthOrderAuth>();
        Authorized authorized;
        // Only non-target Write-scoped auth methods called here
        authorized = auth.NonTargetWriteMethod();
        if (!authorized.HasAccess) return authorized;
        return new Authorized(true);
    }

    // With target — runs ALL Write-scoped auth methods
    public virtual Authorized CanSave(IParamAuthOrder target, CancellationToken cancellationToken = default)
    {
        return LocalCanSave(target, cancellationToken);
    }

    public Authorized LocalCanSave(IParamAuthOrder target, CancellationToken cancellationToken = default)
    {
        IParamAuthOrderAuth auth = ServiceProvider.GetRequiredService<IParamAuthOrderAuth>();
        Authorized authorized;
        // ALL Write-scoped auth methods called here
        authorized = auth.NonTargetWriteMethod();
        if (!authorized.HasAccess) return authorized;
        authorized = auth.CanWrite(target);
        if (!authorized.HasAccess) return authorized;
        return new Authorized(true);
    }

    // Explicit interface implementations
    Task<Authorized> IFactorySave<ParamAuthOrder>.CanSave(CancellationToken cancellationToken)
    {
        return Task.FromResult(CanSave(cancellationToken));
    }

    Task<Authorized> IFactorySave<ParamAuthOrder>.CanSave(ParamAuthOrder target, CancellationToken cancellationToken)
    {
        return Task.FromResult(CanSave(target, cancellationToken));
    }
}
```

---

## Domain Model Behavioral Design

N/A — This is a generator/library change, not a domain model change.

---

## Implementation Steps

1. **Add `CanSave(T target)` overload to `IFactorySave<T>`** — Add the new interface method to `src/RemoteFactory/IFactorySave.cs`

2. **Update `ParamAuthOrderAuth` in Design project** — Add a non-target Write-scoped auth method alongside existing `CanWrite(IParamAuthOrder target)` to test the split behavior. Add corresponding static flag for testing.

3. **Modify `AddCanMethods` in FactoryModelBuilder.cs** — When target-param auth methods exist for a Save method, generate two CanMethodModels instead of skipping. Parameterless version gets only non-target auth. Target version gets all auth.

4. **Modify `AddCanMethodsForInterface` in FactoryModelBuilder.cs** — Same logic as step 3 for interface factory pattern.

5. **Update `RenderCanSaveExplicitInterfaceMethod` in ClassFactoryRenderer.cs** — Render both `CanSave(CancellationToken)` and `CanSave(T target, CancellationToken)` explicit interface implementations.

6. **Update `RenderCanMethod` if needed** — Ensure it handles rendering overloaded CanSave methods with different parameter lists.

7. **Build and fix** — Build the solution, resolve any generator issues with the two-overload approach. Verify generated code looks correct.

8. **Add Design project tests** — Add tests to `ParamAuthorizationTests.cs` covering scenarios 1-7 (CanSave with target, CanSave parameterless, IFactorySave delegation, client-server round-trip).

9. **Verify existing tests pass** — Run full test suite. AuthorizedOrder tests must be unchanged.

10. **Update Design project comments** — Update `ParamAuthOrderAuth.cs` comments to reflect the new behavior (CanSave IS generated, not suppressed).

---

## Acceptance Criteria

- [ ] `IFactorySave<T>` has both `CanSave(CancellationToken)` and `CanSave(T target, CancellationToken)` overloads
- [ ] Generator produces two CanSave overloads when target-param auth exists
- [ ] `CanSave(target)` calls ALL matching Write-scoped auth methods
- [ ] `CanSave()` calls only non-target Write-scoped auth methods
- [ ] Existing AuthorizedOrder tests pass unchanged
- [ ] Design project tests demonstrate the new behavior
- [ ] All builds and tests pass

---

## Dependencies

None.

---

## Risks / Considerations

- **Breaking change on IFactorySave<T>**: Adding a new method to a public interface is technically a breaking change for anyone implementing `IFactorySave<T>` manually (unlikely since it's generator-implemented, but worth noting).
- **Serialization of target for remote CanSave**: If CanSave(target) is [Remote], the target entity needs to serialize across the wire. This follows the same pattern as Save(target) which already serializes the entity.
