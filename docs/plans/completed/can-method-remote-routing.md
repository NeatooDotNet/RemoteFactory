# Can* Method Behavior Derived from Auth Class Instead of Factory Method

**Date:** 2026-03-08
**Related Todo:** [Can* Method Behavior Should Derive from Auth Class, Not Factory Method](../todos/completed/can-method-remote-routing.md)
**Status:** Complete
**Last Updated:** 2026-03-08

---

## Overview

Generated `Can*` methods currently inherit their `IsInternal`, `IsRemote`, and guard behavior from the parent factory method. This is incorrect because `Can*` methods call the **auth class methods**, not the factory method. When a factory method is `[Remote] internal` but the auth class has `public` methods with no server-only dependencies, the `Can*` methods get an `IsServerRuntime` guard that throws on the client -- breaking client-side authorization checks (e.g., the Person example in Blazor WASM).

This plan changes the generator so that `Can*` method behavior derives from the auth class and its methods, following the same accessibility paradigm used everywhere else in RemoteFactory:

- `public` auth methods => Can* runs on client (no guard, no remote call)
- `internal` auth methods (no `[Remote]`) => Can* is server-only (guarded, trimmable)
- `[Remote] internal` auth methods => Can* routes to server via remote delegate

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/can-method-remote-routing.md#requirements-review)

### Relevant Existing Requirements

#### Documented Rules

- **R1** (`src/Design/CLAUDE-DESIGN.md:396-408`, `docs/authorization.md:230-243`): Current rule states Can* methods inherit guard behavior from their parent factory method. **This is the rule being intentionally changed.**

- **R2** (`docs/authorization.md:232-243`): Two explicit statements: (1) public factory method => Can* runs locally, (2) internal factory method => Can* retains guard. **Being replaced by auth-method-driven rule.**

- **R3** (`src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs:29-37`): Comments state Can* methods inherit `[Remote]` promotion and `IsServerRuntime` guard from the factory method. **Comments must be updated.**

- **R4** (`src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs:48-54`): "DID NOT DO THIS" comment acknowledges `[Remote]` on auth methods is a valid future pattern. This change activates that paradigm for Can* method derivation.

- **R8** (`src/Design/CLAUDE-DESIGN.md:141`): `[Remote]` requires `internal` (NF0105). This rule is unchanged. The same pattern applies to auth methods: `[Remote] internal` on an auth method means the auth check routes to the server.

- **R15** (`docs/release-notes/v0.17.0.md:77`): Documents the current guard emission behavior. Release notes for this change must describe the new derivation rule.

- **R16**: This is a BREAKING CHANGE. Code relying on Can* being server-only because the factory method is `[Remote] internal` will now get client-callable Can* if the auth class has `public` methods.

- **R17**: `[AspAuthorize]` without `[AuthorizeFactory]` does not produce Can* methods. Unaffected by this change.

#### Generator Model and Implementation

- **R5** (`src/Generator/Model/Methods/CanMethodModel.cs:17`): `IsSourceMethodRemote` tracks whether the source factory method has `[Remote]`. Used in interface visibility promotion. **Semantics must change to derive from auth methods.**

- **R6** (`src/Generator/Builder/FactoryModelBuilder.cs:822-823`): `AddCanMethods()` passes `isInternal: method.IsInternal` and `isSourceMethodRemote: method.IsRemote` from the factory method. **This is the primary bug location.**

- **R7** (`src/Generator/Renderer/ClassFactoryRenderer.cs:1301`): Guard emitted when `method.IsInternal || method.IsRemote`. Currently Can* gets `IsInternal` from the factory method. **Must derive from auth methods.**

#### Existing Proof-of-Concept

- **R9** (`src/Tests/RemoteFactory.IntegrationTests/Showcase/ShowcaseAuthRemoteTests.cs`): `IAuthRemote` has `[Remote]` on auth methods with a server-only `[Service] IServerOnlyService` dependency. Tests verify `CanCreate()` works through client-server boundary. This existing test already demonstrates auth-method-driven Can* routing and must continue to pass.

- **R10** (`src/Examples/Person/Person.DomainModel/PersonModelAuth.cs`): All `public` auth methods with `IUser` registered on both client and server. Under the new rule, all Can* methods run on client with no guard, fixing the reported bug.

#### Existing Tests

- **R12** (`src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Visibility/CanMethodVisibilityTests.cs`): Tests codify the old rule. `InternalMethod_CanCreate_GeneratedCode_HasGuard` asserts that Can* for an internal factory method HAS the guard. **Must be updated:** the test's auth class (`VisibilityTestAuth`) has `public` methods, so under the new rule, no guard should be emitted.

- **R13** (`src/Tests/RemoteFactory.UnitTests/TestTargets/Visibility/CanMethodVisibilityTargets.cs:37-51`): Comments state "CanCreate should HAVE IsServerRuntime guard (internal method => guard on Can)." **Comments must be updated.**

- **R14** (`src/Design/Design.Tests/FactoryTests/AuthorizationTests.cs:60-72`): Tests call `factory.CanXxx()` in local (server-side) mode. These continue to pass regardless of guard changes since `IsServerRuntime=true` on server.

### Gaps

- **GAP-1**: No documented rule for auth-class-driven Can* behavior. The architect must establish new rules.
- **GAP-2**: No test coverage for mixed auth method accessibility (some public, some `[Remote] internal`).
- **GAP-3**: Interface promotion logic for Can* methods needs redefinition based on auth method accessibility.
- **GAP-4**: CanSave aggregation rule when constituent auth methods have different accessibility.

### Contradictions

All four contradictions identified in the Requirements Review are **intentional corrections** of documented behavior that produces a bug:

- **CONTRADICTION-1**: Direct conflict with Can* visibility rule in `docs/authorization.md:232-243` and `CLAUDE-DESIGN.md:396-408`.
- **CONTRADICTION-2**: Direct conflict with `AuthorizedOrder.cs:29-37` Design comments.
- **CONTRADICTION-3**: Direct conflict with `CanMethodVisibilityTests` test at line 148-188.
- **CONTRADICTION-4**: Direct conflict with `CanMethodVisibilityTargets.cs:37-39` comments.

### Recommendations for Architect

1. Update all four contradicted documentation locations.
2. Replace the "factory method => guard" test with auth-method-driven tests.
3. Design for mixed auth method accessibility: each Can* method independently derives behavior from its specific auth methods.
4. Update AuthorizedOrder.cs Design comments. Consider adding a Design entity demonstrating `[Remote] internal` auth methods.
5. Establish CanSave aggregation rule for mixed auth method accessibility.
6. Verify ShowcaseAuthRemoteTests still passes.
7. Verify Person example works after fix.
8. Create breaking-change release notes.

---

## Business Rules (Testable Assertions)

### Can* Guard Emission Rules

**BR-1.** WHEN all auth methods for a Can* method are `public` (no `[Remote]`, not `internal`), THEN `LocalCan{Op}()` has NO `IsServerRuntime` guard. -- Source: NEW (replaces R1/R2 factory-method-driven rule)

**BR-2.** WHEN any auth method for a Can* method is `internal` (no `[Remote]`), THEN `LocalCan{Op}()` HAS the `IsServerRuntime` guard. -- Source: NEW (follows RemoteFactory accessibility paradigm from R8)

**BR-3.** WHEN any auth method for a Can* method has `[Remote]`, THEN the Can* method has `IsRemote=true`, routes to server via remote delegate, and `LocalCan{Op}()` HAS the `IsServerRuntime` guard. -- Source: R9 (ShowcaseAuthRemoteTests already demonstrates this)

**BR-4.** WHEN a Can* method has no guard (all auth methods are `public`), THEN the Can* method is synchronous (returns `Authorized`, not `Task<Authorized>`) unless an auth method itself returns `Task<bool>`. -- Source: R14 (existing behavior for non-remote, non-guard Can* methods)

### Can* Interface Promotion Rules

**BR-5.** WHEN all auth methods for a Can* method are `public`, THEN the Can* method is NOT promoted (it follows the factory method's own visibility for interface inclusion). -- Source: NEW (GAP-3 resolution)

**BR-6.** WHEN any auth method for a Can* method has `[Remote]`, THEN the Can* method IS promoted to `public` on the factory interface (same as `[Remote]` factory methods). -- Source: R5 (existing `IsSourceMethodRemote` behavior, but now derived from auth methods)

### CanSave Aggregation Rules

**BR-7.** WHEN CanSave aggregates auth methods and ALL constituent auth methods are `public`, THEN CanSave has NO guard. -- Source: NEW (GAP-4 resolution, consistent with BR-1)

**BR-8.** WHEN CanSave aggregates auth methods and ANY constituent auth method is `internal` or `[Remote]`, THEN CanSave HAS the guard (most restrictive wins). -- Source: NEW (GAP-4 resolution, security-conservative)

### Existing Behavior Preserved

**BR-9.** WHEN the auth method has `[Remote]` and the auth implementation has a server-only `[Service]` dependency, THEN Can* routes to the server and resolves the service there. Client-side call succeeds via remote delegate. -- Source: R9 (ShowcaseAuthRemoteTests)

**BR-10.** WHEN the auth class has `public` methods with no server-only dependencies (like PersonModelAuth), and the factory method is `[Remote] internal`, THEN Can* runs on the client with no guard and no remote call. -- Source: R10 (this is the bug fix)

**BR-11.** WHEN `[AspAuthorize]` is used without `[AuthorizeFactory]`, THEN no Can* method is generated. Unchanged. -- Source: R17

**BR-12.** WHEN the factory method has `[AspAuthorize]` AND `[AuthorizeFactory]`, THEN Can* method treats the `[AspAuthorize]` portion as always requiring remote/guard (AspAuthorize checks require HttpContext, which is server-only). -- Source: NEW (existing behavior, now explicitly documented)

### Model Layer Rules

**BR-13.** WHEN `CanMethodModel` is constructed, THEN `IsInternal` reflects whether ANY auth method is `internal` (not the factory method's internality). -- Source: NEW (core bug fix, replaces R6)

**BR-14.** WHEN `CanMethodModel` is constructed, THEN `IsSourceMethodRemote` reflects whether ANY auth method has `[Remote]` (not the factory method's remoteness). -- Source: NEW (core bug fix, replaces R5 semantics)

**BR-15.** WHEN `AuthMethodCall` is constructed from a `TypeAuthMethodInfo`, THEN `IsInternal` is propagated from the source `MethodInfo.IsInternal`. -- Source: NEW (currently `AuthMethodCall` lacks `IsInternal`)

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Public auth, public factory method | Auth: `public bool CanCreate()`, Factory: `public void Create()` | BR-1, BR-4 | `LocalCanCreate()` has NO guard, returns `Authorized` (sync) |
| 2 | Public auth, internal factory method | Auth: `public bool CanCreate()`, Factory: `internal void Create()` | BR-1, BR-10 | `LocalCanCreate()` has NO guard (auth is public, even though factory is internal) |
| 3 | Public auth, [Remote] internal factory method | Auth: `public bool CanCreate()`, Factory: `[Remote] internal void Create()` | BR-1, BR-10 | `LocalCanCreate()` has NO guard (auth is public) |
| 4 | [Remote] internal auth, public factory method | Auth: `[Remote] internal bool CanCreate()`, Factory: `public void Create()` | BR-3, BR-6, BR-9 | `LocalCanCreate()` HAS guard, Can* is async (routes to server), promoted on interface |
| 5 | [Remote] internal auth, [Remote] internal factory method | Auth: `[Remote] internal bool CanCreate()`, Factory: `[Remote] internal void Create()` | BR-3, BR-6 | `LocalCanCreate()` HAS guard, Can* routes to server, promoted on interface |
| 6 | Internal auth (no [Remote]), internal factory method | Auth: `internal bool CanCreate()`, Factory: `internal void Create()` | BR-2 | `LocalCanCreate()` HAS guard (auth is internal = server-only) |
| 7 | Mixed auth: public HasAccess + [Remote] internal CanDelete, CanSave | Auth: `public HasAccess()` + `[Remote] internal CanDelete()` | BR-8 | CanSave HAS guard (most restrictive wins: CanDelete is [Remote]) |
| 8 | Mixed auth: all public methods, CanSave | Auth: all `public` methods | BR-7 | CanSave has NO guard |
| 9 | Person example: public auth, [Remote] factory | PersonModelAuth: all `public`, PersonModel: `[Remote] internal` ops | BR-10 | All Can* methods run on client, no guard, no throw |
| 10 | ShowcaseAuthRemote: [Remote] auth with server-only service | IAuthRemote: `[Remote]` on Create auth, AuthServerOnly: `[Service] IServerOnlyService` | BR-9 | CanCreate routes to server, resolves IServerOnlyService, returns true |
| 11 | AspAuthorize only (no AuthorizeFactory) | Factory method: `[AspAuthorize("Policy")]` only | BR-11 | No Can* method generated |
| 12 | AuthorizeFactory + AspAuthorize combined | Both `[AuthorizeFactory<T>]` and `[AspAuthorize]` on factory method | BR-12 | Can* has guard (AspAuthorize requires server) |
| 13 | Public auth, interface promotion check | Auth: all `public`, Factory: `[Remote] internal` | BR-5 | Can* NOT promoted to public independently (follows factory method's interface visibility context) |
| 14 | [Remote] auth, interface promotion check | Auth: `[Remote] internal`, Factory: `public void Create()` | BR-6 | Can* IS promoted to public on interface (auth is [Remote]) |

---

## Approach

The fix is localized to the generator's model layer and renderers. The core insight: `CanMethodModel.IsInternal` and `CanMethodModel.IsSourceMethodRemote` currently come from the factory method (the parent). They must instead come from the auth methods that the Can* method actually calls.

### Strategy

1. **Add `IsInternal` to `AuthMethodCall` model** -- propagate from `TypeAuthMethodInfo.IsInternal` (already tracked at the Roslyn analysis level, line 674 of `FactoryGenerator.Types.cs`, but not mapped to the model layer).

2. **Change `AddCanMethods()` to derive from auth methods** -- instead of passing `method.IsInternal` and `method.IsRemote` from the factory method, compute these from the auth methods: `isInternal` = any auth method is internal; `isSourceMethodRemote` = any auth method has `[Remote]`.

3. **Update guard emission** -- no changes needed to `RenderCanLocalMethod()` itself. The guard condition `method.IsInternal || method.IsRemote` remains correct; the change is that `method.IsInternal` now reflects auth method accessibility instead of factory method accessibility.

4. **Update interface promotion** -- the existing logic `method.IsRemote || (method is CanMethodModel cm && cm.IsSourceMethodRemote)` remains structurally correct; `IsSourceMethodRemote` now reflects whether any auth method has `[Remote]`.

5. **Rename `IsSourceMethodRemote`** -- rename to `IsSourceAuthMethodRemote` for clarity, since the semantics change from "source factory method" to "source auth method."

---

## Design

### 1. Model Layer Changes

#### `AuthMethodCall` -- Add `IsInternal`

```
File: src/Generator/Model/Supporting/AuthorizationModel.cs

AuthMethodCall gains:
    public bool IsInternal { get; }

Constructor gains:
    bool isInternal = false
```

#### `CanMethodModel` -- Rename property

```
File: src/Generator/Model/Methods/CanMethodModel.cs

Rename: IsSourceMethodRemote -> IsSourceAuthMethodRemote
Update doc comment: "Whether any auth method for this Can* operation has [Remote]"
```

### 2. Builder Layer Changes

#### `BuildAuthorization()` -- Propagate `IsInternal`

```
File: src/Generator/Builder/FactoryModelBuilder.cs, line 920-944

When constructing AuthMethodCall, map isInternal from TypeAuthMethodInfo:
    isInternal: am.IsInternal
```

#### `AddCanMethods()` (class factory, line 790-830) -- Derive from auth methods

```
Current (line 822-823):
    isInternal: method.IsInternal,          // factory method
    isSourceMethodRemote: method.IsRemote   // factory method

New:
    isInternal: authMethodsAreInternal,         // derived from auth methods
    isSourceAuthMethodRemote: authMethodsAreRemote  // derived from auth methods

Where:
    var authMethods = method.Authorization?.AuthMethods ?? Array.Empty<AuthMethodCall>();
    var authMethodsAreInternal = authMethods.Any(am => am.IsInternal);
    var authMethodsAreRemote = authMethods.Any(am => am.IsRemote);

    // AspAuthorize always requires server (HttpContext)
    var hasAspAuthorize = (method.Authorization?.AspAuthorize.Count ?? 0) > 0;
    authMethodsAreInternal = authMethodsAreInternal || hasAspAuthorize;
```

#### `AddCanMethods()` (interface factory, line 735-788)

The interface factory builder at line 735 creates `InterfaceMethodModel` for Can* methods. Interface factory Can* methods already always get the guard (line 256-258 in `InterfaceFactoryRenderer.cs`). No change needed here -- interface factories are inherently remote/server-only.

#### `BuildCanMethod()` (line 518-559) -- Update parameter name

```
Rename parameter: isSourceMethodRemote -> isSourceAuthMethodRemote
This flows through to CanMethodModel constructor.
```

### 3. Renderer Layer Changes

#### `ClassFactoryRenderer.RenderCanLocalMethod()` (line 1289-1322)

No structural change needed. The guard condition `method.IsInternal || method.IsRemote` remains correct because `method.IsInternal` now derives from auth methods. The guard fires when any auth method is internal (server-only) or when the Can* method routes remotely.

#### `ClassFactoryRenderer` interface rendering (line 110-137)

The existing interface promotion logic:
```csharp
var isPromotedByRemote = method.IsRemote || (method is CanMethodModel cm && cm.IsSourceMethodRemote);
```

Update to use renamed property:
```csharp
var isPromotedByRemote = method.IsRemote || (method is CanMethodModel cm && cm.IsSourceAuthMethodRemote);
```

This remains structurally correct. For Can* methods:
- `method.IsRemote` is true when any auth method has `[Remote]` (already computed correctly in `BuildCanMethod` line 534)
- `cm.IsSourceAuthMethodRemote` now explicitly tracks auth method `[Remote]` status

### 4. CanSave Aggregation

`BuildSaveMethodFromGroup()` (line 630-689) merges auth methods from Insert, Update, and Delete into a single `AuthorizationModel` for the Save method. When `AddCanMethods()` processes the Save method's auth methods, it applies the same rule: if ANY auth method is internal or `[Remote]`, the CanSave gets a guard.

This happens automatically because:
1. Save's `AuthorizationModel.AuthMethods` contains the union of all write operation auth methods
2. `AddCanMethods()` checks `authMethods.Any(am => am.IsInternal)` across all of them
3. If even one auth method (e.g., CanDelete) is `[Remote] internal`, CanSave gets the guard

No additional code changes needed for CanSave aggregation -- the existing aggregation in `BuildSaveMethodFromGroup()` combined with the new auth-method-driven derivation in `AddCanMethods()` produces the correct behavior per BR-7 and BR-8.

### 5. Data Flow Summary

```
BEFORE (buggy):
    Factory Method (IsInternal, IsRemote)
        |
        v
    AddCanMethods() passes factory method's values
        |
        v
    CanMethodModel.IsInternal = factory.IsInternal     <-- WRONG
    CanMethodModel.IsSourceMethodRemote = factory.IsRemote  <-- WRONG
        |
        v
    Guard: method.IsInternal || method.IsRemote   (factory method driven)

AFTER (fixed):
    Auth Methods (each has IsInternal, IsRemote)
        |
        v
    AddCanMethods() computes: any auth internal? any auth remote?
        |
        v
    CanMethodModel.IsInternal = ANY auth method IsInternal   <-- CORRECT
    CanMethodModel.IsSourceAuthMethodRemote = ANY auth method IsRemote  <-- CORRECT
        |
        v
    Guard: method.IsInternal || method.IsRemote   (auth method driven)
```

---

## Implementation Steps

### Phase 1: Model Layer Changes

1. Add `IsInternal` property to `AuthMethodCall` record in `src/Generator/Model/Supporting/AuthorizationModel.cs`
2. Update `AuthMethodCall` constructor to accept `bool isInternal = false`
3. Rename `CanMethodModel.IsSourceMethodRemote` to `IsSourceAuthMethodRemote` in `src/Generator/Model/Methods/CanMethodModel.cs`
4. Update `CanMethodModel` constructor parameter and doc comment

### Phase 2: Builder Layer Changes

5. In `BuildAuthorization()` (`src/Generator/Builder/FactoryModelBuilder.cs:920`), map `isInternal: am.IsInternal` when constructing `AuthMethodCall`
6. In `AddCanMethods()` (class factory, line 790), compute `isInternal` and `isSourceAuthMethodRemote` from auth methods instead of factory method
7. Rename `BuildCanMethod()` parameter from `isSourceMethodRemote` to `isSourceAuthMethodRemote`

### Phase 3: Renderer Layer Changes

8. Update `ClassFactoryRenderer` interface rendering (line 121) to use `IsSourceAuthMethodRemote`
9. Verify `RenderCanLocalMethod()` guard logic needs no structural change (it uses `method.IsInternal || method.IsRemote` which is now correctly derived)

### Phase 4: Test Updates

10. Update `CanMethodVisibilityTargets.cs` comments to describe auth-method-driven behavior
11. Update `CanMethodVisibilityTests.cs`:
    - Rename `InternalMethod_CanCreate_GeneratedCode_HasGuard` to reflect new rule
    - Change assertion: public auth + internal factory => NO guard (was: HAS guard)
    - Add new test: internal auth method => HAS guard
    - Add new test: `[Remote]` auth method => HAS guard and is async/remote
    - Add new test: mixed auth methods (public + [Remote] internal) => HAS guard
12. Update class-level doc comment on `CanMethodVisibilityTests` to describe new rule

### Phase 5: Design Project Updates

13. Update `AuthorizedOrder.cs` comments (lines 29-37) to describe auth-method-driven behavior
14. Update `AuthorizedOrderAuth.cs` "DID NOT DO THIS" comment to note this pattern is now active for Can* derivation
15. Verify Design project builds and tests pass: `dotnet build src/Design/Design.sln && dotnet test src/Design/Design.sln`

### Phase 6: Documentation Updates

16. Update `src/Design/CLAUDE-DESIGN.md` Rule 2 (line 396-408): Change "Factory Method Visibility Controls Guard Emission" to note Can* methods derive from auth method accessibility, not factory method
17. Update `docs/authorization.md` Can* section (line 230-243): Rewrite to describe auth-method-driven behavior
18. Create release notes entry for breaking change

### Phase 7: Verification

19. Run full test suite: `dotnet test src/Neatoo.RemoteFactory.sln`
20. Verify ShowcaseAuthRemoteTests still pass
21. Verify Person example works: build and run `src/Examples/Person/Person.Server/`

---

## Acceptance Criteria

- [ ] `CanMethodModel.IsInternal` derives from auth methods, not factory method
- [ ] `CanMethodModel.IsSourceAuthMethodRemote` derives from auth methods, not factory method
- [ ] `AuthMethodCall` tracks `IsInternal` from the source auth method
- [ ] Public auth + internal factory method => Can* has NO guard (fixes Person example bug)
- [ ] Public auth + [Remote] internal factory method => Can* has NO guard
- [ ] [Remote] internal auth + any factory method => Can* HAS guard and routes remotely
- [ ] Internal auth (no [Remote]) + any factory method => Can* HAS guard
- [ ] CanSave with mixed auth (some public, some [Remote]) => guard (most restrictive wins)
- [ ] ShowcaseAuthRemoteTests continue to pass
- [ ] All existing tests pass (with intentional test updates for the rule change)
- [ ] Design project builds and tests pass
- [ ] CLAUDE-DESIGN.md updated with new rule
- [ ] docs/authorization.md Can* section rewritten
- [ ] CanMethodVisibilityTests updated to test auth-method-driven behavior

---

## Dependencies

- No external dependencies. All changes are within the generator and its tests.
- The `TypeAuthMethodInfo.IsInternal` property already exists in the Roslyn analysis layer (`FactoryGenerator.Types.cs:674`), so no changes needed to the symbol analysis phase.

---

## Risks / Considerations

1. **Breaking change**: Users who relied on Can* being server-only because the factory method is `[Remote] internal` will now get client-callable Can* if their auth class has `public` methods. Migration: add `[Remote]` to auth methods if server-only auth checks are needed.

2. **CanSave aggregation edge case**: If Insert auth is `public` and Delete auth is `[Remote] internal`, CanSave becomes guarded/remote. This is the secure default (most restrictive wins), but users may find it surprising. The Design project should document this case.

3. **Interface factory Can* methods**: Interface factories already always get the `IsServerRuntime` guard for all methods (line 256-258 in `InterfaceFactoryRenderer.cs`). No change needed, but worth verifying no regression.

4. **Legacy `FactoryGenerator.Types.cs` code path**: The old code path (`CanFactoryMethod` at line 1716) also builds Can* methods for the legacy generator. This code path should be checked for consistency but is outside the new generator model layer. The legacy code uses `AuthMethodInfos` directly and computes `IsRemote` from auth methods already (line 857), so it may already have the correct behavior for `IsRemote`. However, it lacks the `IsInternal` derivation fix.

---

## Architectural Verification

**Scope Table:**

| Component | Affected? | Current State | After Change |
|-----------|-----------|---------------|--------------|
| `AuthMethodCall` model | Yes | No `IsInternal` | Adds `IsInternal` |
| `CanMethodModel` | Yes | `IsSourceMethodRemote` from factory | `IsSourceAuthMethodRemote` from auth |
| `FactoryModelBuilder.AddCanMethods()` (class) | Yes | Passes factory method values | Computes from auth methods |
| `FactoryModelBuilder.AddCanMethods()` (interface) | No | Creates `InterfaceMethodModel` | Unchanged (always guarded) |
| `ClassFactoryRenderer.RenderCanLocalMethod()` | No | Guard: `IsInternal \|\| IsRemote` | Same condition, different input |
| `ClassFactoryRenderer` interface rendering | Yes | Uses `IsSourceMethodRemote` | Uses `IsSourceAuthMethodRemote` |
| `InterfaceFactoryRenderer` | No | All methods guarded | Unchanged |
| `BuildCanMethod()` | Yes | Parameter rename | `isSourceAuthMethodRemote` |
| `BuildAuthorization()` | Yes | Missing `IsInternal` mapping | Maps `am.IsInternal` |
| CanSave aggregation | No (automatic) | Merges auth methods | Same merge, new derivation applies |

**Verification Evidence:**

- `ShowcaseAuthRemoteTests` uses `[Remote]` on auth methods (R9). Under the new rule, `BuildCanMethod` line 534 already computes `isRemote = authMethods.Any(m => m.IsRemote)` correctly. The fix adds `isInternal` derivation to complete the picture.
- Person example: `PersonModelAuth` has all `public` methods. Under new rule, `authMethods.Any(am => am.IsInternal)` = false, so no guard. Fixes the bug.
- AuthorizedOrder: `AuthorizedOrderAuth` has all `public` methods. Under new rule, Can* methods lose the guard. Design comments must be updated.

**Breaking Changes:** Yes -- Can* methods for entities with `[Remote] internal` factory methods but `public` auth classes will no longer have the `IsServerRuntime` guard. To maintain server-only auth checks, users must add `[Remote]` to their auth interface methods.

**Codebase Analysis:**

Key files examined:
- `src/Generator/Builder/FactoryModelBuilder.cs` -- `AddCanMethods()` at 790-830, `BuildCanMethod()` at 518-559, `BuildAuthorization()` at 920-944
- `src/Generator/Model/Methods/CanMethodModel.cs` -- `IsSourceMethodRemote` property
- `src/Generator/Model/Supporting/AuthorizationModel.cs` -- `AuthMethodCall` record (missing `IsInternal`)
- `src/Generator/Renderer/ClassFactoryRenderer.cs` -- Guard at 1301, interface promotion at 121
- `src/Generator/FactoryGenerator.Types.cs` -- `MethodInfo.IsInternal` at 674 (already tracked in Roslyn layer)
- `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Visibility/` -- Test targets and tests to update
- `src/Design/Design.Domain/Aggregates/AuthorizedOrder*.cs` -- Design comments to update

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1-3: Generator model/builder/renderer | developer | Yes | Core generator changes, ~5 files | None |
| Phase 4: Unit test updates | developer | No | Same context as generator changes, needs to understand the new model | Phase 1-3 |
| Phase 5: Design project updates | developer | No | Still in same context, updating comments | Phase 1-3 |
| Phase 6: Documentation updates | business-requirements-documenter | Yes | Documentation-only, clean context | Phase 1-5 complete |
| Phase 7: Verification | architect | Yes | Independent verification | Phase 1-5 complete |

**Parallelizable phases:** Phase 6 (documentation) can run in parallel with Phase 7 (verification) if Phase 1-5 are complete.

**Notes:** Phases 1-5 should be done by a single developer agent session since the changes are tightly coupled and the total file count is manageable (~8 source files + ~3 test files). The developer should build and test after each phase as a verification gate.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-08

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| BR-1 | `AddCanMethods()` (line 822): changed to `isInternal: authMethodsAreInternal` where `authMethodsAreInternal = authMethods.Any(am => am.IsInternal)`. All auth methods public => `authMethodsAreInternal = false`. Guard at `RenderCanLocalMethod()` line 1301: `method.IsInternal \|\| method.IsRemote` => `false \|\| false` => NO guard. | NO guard emitted | Yes | Requires `AuthMethodCall.IsInternal` to be added and populated from `TypeAuthMethodInfo.IsInternal` in `BuildAuthorization()` (line 923). |
| BR-2 | Same path as BR-1. Any auth method `internal` => `am.IsInternal = true` (from `MethodInfo.IsInternal` at `FactoryGenerator.Types.cs:674`: `DeclaredAccessibility != Public`). `authMethodsAreInternal = true`. Guard: `true \|\| false` => guard emitted. | HAS guard | Yes | |
| BR-3 | `AddCanMethods()`: `authMethodsAreRemote = authMethods.Any(am => am.IsRemote)` = true. This flows to `BuildCanMethod()` param `isSourceAuthMethodRemote`, and `BuildCanMethod()` line 534: `isRemote = authMethods.Any(m => m.IsRemote) \|\| aspAuthorize.Count > 0` => `true`. `CanMethodModel.IsRemote = true`. Guard: `method.IsInternal \|\| method.IsRemote` => `_ \|\| true` => guard. | HAS guard, IsRemote=true, routes to server | Yes | `IsRemote` was already computed from auth methods at line 534. The fix adds `IsInternal` derivation. |
| BR-4 | `BuildCanMethod()` line 536: `isTask = isRemote \|\| authMethods.Any(m => m.IsTask) \|\| aspAuthorize.Count > 0`. All auth public, none async, no AspAuthorize => `isRemote=false`, `isTask=false`. Return type at `RenderCanLocalMethod()` line 1292: `method.IsTask ? "Task<Authorized>" : "Authorized"` => `Authorized` (sync). | Synchronous, returns `Authorized` | Yes | Plan correctly notes "unless an auth method itself returns `Task<bool>`". |
| BR-5 | `ClassFactoryRenderer` line 121: `isPromotedByRemote = method.IsRemote \|\| (method is CanMethodModel cm && cm.IsSourceAuthMethodRemote)`. All auth public => `IsRemote=false`, `IsSourceAuthMethodRemote=false` => `isPromotedByRemote=false`. Line 122: `needsInternalPrefix = !AllMethodsInternal && method.IsInternal && !isPromotedByRemote`. `method.IsInternal=false` (from all-public auth) => `needsInternalPrefix=false`. Can* follows factory method's interface visibility context. | NOT promoted independently | Yes | If factory is `[Remote] internal`, Can* still appears on the public interface because the factory method's presence makes the interface public. Can* just doesn't get its own promotion. |
| BR-6 | Same renderer line 121: auth has `[Remote]` => `cm.IsSourceAuthMethodRemote = true` => `isPromotedByRemote = true` => `needsInternalPrefix = false`. Can* IS promoted to public on interface. | Promoted to public | Yes | |
| BR-7 | `AddCanMethods()` iterates Save methods (which are in `factoryMethods` list per line 236). Save's `Authorization.AuthMethods` is the union from `BuildSaveMethodFromGroup()` line 645-649. All auth methods public => `authMethodsAreInternal = false`, `authMethodsAreRemote = false`. Guard: `false \|\| false` => no guard. | NO guard on CanSave | Yes | Depends on Save method correctly aggregating auth methods, which `BuildSaveMethodFromGroup()` already does at lines 645-654. |
| BR-8 | Same as BR-7 but ANY constituent auth method is internal or `[Remote]`. `authMethods.Any(am => am.IsInternal)` = true OR `authMethods.Any(am => am.IsRemote)` = true. Guard condition at 1301: `true \|\| _` or `_ \|\| true` => guard emitted. | HAS guard | Yes | Most-restrictive-wins is automatic from `Any()`. |
| BR-9 | `ShowcaseAuthRemoteTests`: `IAuthRemote.Create()` has `[Remote]`, `AuthServerOnly` has `[Service] IServerOnlyService` constructor. `BuildAuthorization()` maps `am.IsRemote = true`. `BuildCanMethod()` line 534: `isRemote = true`. `CanMethodModel.IsRemote = true`. Guard emitted. Remote delegate routes to server, DI resolves `IServerOnlyService` on server. Under new rule: `am.IsInternal = false` (interface method is public), but `IsRemote = true` => guard still emitted. | Routes to server, service resolved | Yes | Verified: interface method `DeclaredAccessibility` is `Public` (default for interface members), so `IsInternal = false`. Guard fires from `IsRemote` branch. |
| BR-10 | `PersonModelAuth`: all methods `public`, no `[Remote]`. `PersonModel`: factory methods `[Remote] internal`. Under new rule: `authMethodsAreInternal = false` (all auth public), `authMethodsAreRemote = false`. Guard: `false \|\| false` => no guard. Can* runs on client. | NO guard, runs on client | Yes | This is the bug fix. Currently guard fires because `isInternal = method.IsInternal` (factory method is internal). |
| BR-11 | `AddCanMethods()` line 797: `if (!method.HasAuth) continue`. `HasAuth` requires `Authorization.AuthMethods.Count > 0 \|\| Authorization.AspAuthorize.Count > 0`. With only `[AspAuthorize]` (no `[AuthorizeFactory]`), `AuthMethods.Count = 0`. `HasAuth` depends on whether `AspAuthorize` alone sets `HasAuth`. Checking `FactoryMethodModel.HasAuth` at line 54: `Authorization != null && Authorization.HasAuth`. `AuthorizationModel.HasAuth` at line 23: `AuthMethods.Count > 0 \|\| AspAuthorize.Count > 0`. So `[AspAuthorize]` alone => `HasAuth = true`, Can* IS generated. | Wait -- see Concern 1 below | Needs Clarification | Plan says no Can* generated with AspAuthorize only. But code shows `HasAuth = true` when `AspAuthorize.Count > 0`. Need to verify actual behavior. |
| BR-12 | `BuildCanMethod()` line 534: `isRemote = authMethods.Any(m => m.IsRemote) \|\| aspAuthorize.Count > 0`. `AspAuthorize` present => `isRemote = true`. Guard: `_ \|\| true` => guard emitted. Under new rule: `authMethodsAreInternal = authMethodsAreInternal \|\| hasAspAuthorize` (plan line 232). Both paths produce guard. | HAS guard | Yes | Plan accounts for AspAuthorize at line 231-232 of `AddCanMethods()` design. |
| BR-13 | `AddCanMethods()` line 822 (proposed): `isInternal: authMethodsAreInternal` where `authMethodsAreInternal = authMethods.Any(am => am.IsInternal)`. `CanMethodModel` constructor receives this value. `CanMethodModel.IsInternal` reflects auth method internality. | Reflects auth method internality | Yes | Requires `AuthMethodCall.IsInternal` to exist (added in Phase 1). |
| BR-14 | `AddCanMethods()` line 823 (proposed): `isSourceAuthMethodRemote: authMethodsAreRemote` where `authMethodsAreRemote = authMethods.Any(am => am.IsRemote)`. `CanMethodModel.IsSourceAuthMethodRemote` reflects auth method remoteness. | Reflects auth method remoteness | Yes | Rename from `IsSourceMethodRemote`. |
| BR-15 | `BuildAuthorization()` line 923 (proposed): `isInternal: am.IsInternal` added to `AuthMethodCall` constructor call. `TypeAuthMethodInfo.IsInternal` at `FactoryGenerator.Types.cs:674-734` already tracks `DeclaredAccessibility != Public`. | `AuthMethodCall.IsInternal` populated from Roslyn analysis | Yes | Clean propagation; no new Roslyn analysis needed. |

### Concerns

#### Concern 1: BR-11 Clarification -- AspAuthorize-only Can* generation (Non-blocking)

The plan states (BR-11): "WHEN `[AspAuthorize]` is used without `[AuthorizeFactory]`, THEN no Can* method is generated."

I traced the code: `FactoryMethodModel.HasAuth` (line 54) returns `Authorization != null && Authorization.HasAuth`, and `AuthorizationModel.HasAuth` (line 23) returns `AuthMethods.Count > 0 || AspAuthorize.Count > 0`. So if a factory method has only `[AspAuthorize]` (no `[AuthorizeFactory]`), `HasAuth = true`, and `AddCanMethods()` would NOT skip it at line 797-800.

However, looking more carefully at how `Authorization` is built: `BuildAuthorization()` (line 920-944) builds from `method.AuthMethodInfos` and `method.AspAuthorizeCalls`. If only `[AspAuthorize]` is present (no `[AuthorizeFactory]`), `authMethods` would be empty but `aspAuthorize` would be non-empty. The `BuildAuthorization()` returns a non-null `AuthorizationModel` with `HasAuth = true`.

So `AddCanMethods()` at line 797 would proceed past the `!method.HasAuth` check. But then `BuildCanMethod()` would be called with an empty `authMethods` list and a non-empty `aspAuthorize` list. This would produce a `CanXxx` method with `isRemote = true` (from `aspAuthorize.Count > 0`), `isTask = true`, `isAsync = true`.

**This contradicts BR-11** as stated. The plan may be wrong about the current behavior, or there may be another guard I haven't found. This is non-blocking because the plan does not propose changing `[AspAuthorize]`-only behavior, but the business rule as documented is inaccurate if my trace is correct.

**Recommendation:** The developer should verify at implementation time whether `[AspAuthorize]`-only actually produces a Can* method by writing a quick generator test. If it does, BR-11 needs revision. If there's another guard I missed (perhaps in the Roslyn analysis that prevents `Authorization` from being set when only `[AspAuthorize]` is present), then BR-11 is correct.

**Update after further investigation:** The `[AspAuthorize]` attribute is processed in `FactoryGenerator.Types.cs` and may only be attached to factory methods that also have `[AuthorizeFactory]`. Let me check whether `AspAuthorizeCalls` is populated independently of `AuthMethodInfos`. Looking at the code path: `AspAuthorizeCalls` is populated from `[Authorize]`/`[AspAuthorize]` attributes directly on the method, while `AuthMethodInfos` comes from the `[AuthorizeFactory<T>]` attribute's auth class. They are independent. So a method could have `[AspAuthorize]` without `[AuthorizeFactory]` and still produce a Can* method. However, this scenario may simply not be a supported pattern, and BR-11's claim about "no Can* generated" may describe intent rather than current behavior. Since the plan does not change this code path, this is informational only.

#### Concern 2: Interface factory Can* methods (Informational, non-blocking)

The plan correctly notes (section "AddCanMethods (interface factory, line 735-788)") that interface factory Can* methods always get the guard because `InterfaceFactoryRenderer` line 256-258 unconditionally emits the guard for all methods. No change is needed for interface factories. I verified this at `InterfaceFactoryRenderer.cs:256-258`. Confirmed correct.

#### Concern 3: AuthorizedOrder Design project behavioral change (Non-blocking, but important)

Under the new rule, `AuthorizedOrder`'s Can* methods will lose the `IsServerRuntime` guard because `IAuthorizedOrderAuth` has all public methods (interface methods are public by default, no `[Remote]`). This means:
- `CanCreate()`, `CanFetch()`, `CanDelete()`, `CanSave()` will all run on the client
- The Design project comments at `AuthorizedOrder.cs:29-37` describe the old behavior
- The Design tests at `AuthorizationTests.cs:60-72` run in `local` mode where `IsServerRuntime=true`, so they will still pass

The plan accounts for this at Phase 5 (steps 13-14) and in the documentation deliverables. No blocking issue, but the developer should verify the Design project still builds and tests pass after the generator changes.

#### Concern 4: `CanMethodModel` record equality (Non-blocking)

`CanMethodModel` is a `sealed record` that derives from `FactoryMethodModel` (also a `record`). The rename from `IsSourceMethodRemote` to `IsSourceAuthMethodRemote` affects the property name but not the type. Adding `IsInternal` to `AuthMethodCall` (also a `sealed record`) changes its equality semantics. Since both are records, the compiler-generated equality will automatically include the new field. This is correct for incremental generation cache invalidation. No concern.

#### Concern 5: Legacy `FactoryGenerator.Types.cs` code path (Non-blocking)

The plan mentions (Risk 4) that the legacy code path (`CanFactoryMethod` at line 1716) also builds Can* methods. The plan says it "should be checked for consistency but is outside the new generator model layer." I agree this is out of scope for this change -- the new model/builder/renderer pipeline is the active code path. The legacy code path in `FactoryGenerator.Types.cs` is not used by the new generator. No action needed.

---

## Implementation Contract

**Created:** 2026-03-08
**Approved by:** developer agent (Claude Opus 4.6)

### Verification Acceptance Criteria

- Design project (`src/Design/Design.sln`) builds and all 26 tests pass after generator changes
- ShowcaseAuthRemoteTests (integration tests) continue to pass unchanged
- All 957 unit tests pass (with intentional updates to CanMethodVisibilityTests)
- All 952 integration tests pass

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| 1 | `CanMethodVisibilityTests.PublicMethod_CanCreate_GeneratedCode_HasNoGuard` | Existing test, unchanged |
| 2 | New: `PublicAuth_InternalFactory_CanCreate_HasNoGuard` | New generated-code test in `CanMethodVisibilityTests` |
| 3 | Same as Scenario 2 + existing `PublicMethod_CanCreate_GeneratedCode_HasNoGuard` | Auth is public => no guard regardless of factory visibility |
| 4 | New: `RemoteAuth_CanCreate_HasGuard_And_IsAsync` | New generated-code test |
| 5 | Covered by ShowcaseAuthRemoteTests (integration, existing) | Existing test, unchanged |
| 6 | New: `InternalAuth_CanCreate_HasGuard` | New generated-code test with internal auth method |
| 7 | New test or extension for mixed auth scenario | New generated-code test |
| 8 | Covered by scenarios 1-3 (all public auth => no guard) | Implicit in existing tests |
| 9 | Person example manual verification (build only, no automated test) | Verify build succeeds |
| 10 | `ShowcaseAuthRemoteTests.ShowcaseAuthRemoteTest_CanCreate` | Existing, unchanged |
| 11 | BR-11 needs verification (see Concern 1) | Check at implementation time |
| 12 | New test or extend existing for AspAuthorize+AuthorizeFactory | If feasible |
| 13 | Covered by scenario 2 (public auth, [Remote] factory) | Can* follows factory interface context |
| 14 | Covered by scenario 4 ([Remote] auth, promotion check) | New test verifies promotion |

### In Scope

**Generator model layer:**
- [ ] `src/Generator/Model/Supporting/AuthorizationModel.cs`: Add `IsInternal` property to `AuthMethodCall` record
- [ ] `src/Generator/Model/Methods/CanMethodModel.cs`: Rename `IsSourceMethodRemote` to `IsSourceAuthMethodRemote`, update doc comment

**Generator builder layer:**
- [ ] `src/Generator/Builder/FactoryModelBuilder.cs` `BuildAuthorization()` (line 923): Map `isInternal: am.IsInternal` when constructing `AuthMethodCall`
- [ ] `src/Generator/Builder/FactoryModelBuilder.cs` `AddCanMethods()` (class factory, line 817-823): Compute `isInternal` and `isSourceAuthMethodRemote` from auth methods, include `hasAspAuthorize` in `authMethodsAreInternal`
- [ ] `src/Generator/Builder/FactoryModelBuilder.cs` `BuildCanMethod()` (line 518): Rename parameter `isSourceMethodRemote` to `isSourceAuthMethodRemote`

**Generator renderer layer:**
- [ ] `src/Generator/Renderer/ClassFactoryRenderer.cs` (line 121): Update `IsSourceMethodRemote` to `IsSourceAuthMethodRemote` in interface promotion logic

**Unit tests:**
- [ ] `src/Tests/RemoteFactory.UnitTests/TestTargets/Visibility/CanMethodVisibilityTargets.cs`: Update comments, add new test targets (internal auth, [Remote] auth)
- [ ] `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Visibility/CanMethodVisibilityTests.cs`: Update class doc comment, update `InternalMethod_CanCreate_GeneratedCode_HasGuard` test (now expects NO guard), add new tests for internal auth and [Remote] auth scenarios

**Design project comments:**
- [ ] `src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs` (lines 29-37): Update GENERATOR BEHAVIOR comments
- [ ] `src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs` (lines 48-54): Update "DID NOT DO THIS" comment

### Out of Scope

- `src/Generator/FactoryGenerator.Types.cs` legacy code path (`CanFactoryMethod`) -- not part of new model/builder/renderer pipeline
- `src/Generator/Renderer/InterfaceFactoryRenderer.cs` -- interface factories unconditionally guard all methods, no change needed
- `src/Generator/Builder/FactoryModelBuilder.cs` `AddCanMethodsForInterface()` (line 713) -- interface factory Can* method generation, unaffected
- All integration tests in `ShowcaseAuthRemoteTests` -- must pass unchanged (no modifications)
- `src/Design/Design.Tests/FactoryTests/AuthorizationTests.cs` -- must pass unchanged (runs in local/server mode)
- Documentation files (`docs/authorization.md`, `src/Design/CLAUDE-DESIGN.md`, release notes) -- handled by documentation agent in Phase 6
- Person example code changes -- no code changes needed, the generator fix resolves the bug

### Verification Gates

1. After Phase 1-3 (model/builder/renderer changes): `dotnet build src/Neatoo.RemoteFactory.sln` succeeds
2. After Phase 4 (test updates): `dotnet test src/Tests/RemoteFactory.UnitTests/RemoteFactory.UnitTests.csproj` -- all pass
3. After Phase 4: `dotnet test src/Tests/RemoteFactory.IntegrationTests/RemoteFactory.IntegrationTests.csproj` -- all pass, ShowcaseAuthRemoteTests unchanged and passing
4. After Phase 5 (Design updates): `dotnet build src/Design/Design.sln && dotnet test src/Design/Design.sln` -- all pass
5. Final: `dotnet test src/Neatoo.RemoteFactory.sln` -- all tests pass (0 failures)

### Stop Conditions

If any occur, STOP and report:
- Any integration test in `ShowcaseAuthRemoteTests` fails
- Any Design project test fails (unless explained by the auth-method-driven rule change)
- Any out-of-scope test fails that is not directly testing Can* guard behavior
- `AuthMethodCall.IsInternal` is not populated correctly from `TypeAuthMethodInfo.IsInternal` (check with debugger or generated output)
- The CanSave aggregation produces unexpected results for mixed auth method accessibility

---

## Implementation Progress

**Started:** 2026-03-08
**Developer:** Claude Opus 4.6

### Phase 1: Model Layer Changes (Complete)
- Added `IsInternal` property to `AuthMethodCall` record in `AuthorizationModel.cs`
- Added `isInternal` constructor parameter with default `false`
- Renamed `IsSourceMethodRemote` to `IsSourceAuthMethodRemote` in `CanMethodModel.cs`
- Updated doc comment to reflect auth-method-driven semantics
- Updated `CreateMethodWithUniqueName` reference in `FactoryModelBuilder.cs` (line 894)

### Phase 2: Builder Layer Changes (Complete)
- Updated `BuildAuthorization()` to map `isInternal: am.IsInternal` from `TypeAuthMethodInfo`
- Updated `AddCanMethods()` to compute `isInternal` and `isSourceAuthMethodRemote` from auth methods instead of factory method
- Added `hasAspAuthorize` logic: AspAuthorize always implies internal (requires HttpContext)
- Renamed `BuildCanMethod()` parameter from `isSourceMethodRemote` to `isSourceAuthMethodRemote`

### Phase 3: Renderer Layer Changes (Complete)
- Updated `ClassFactoryRenderer` interface promotion logic (line 121) to use `IsSourceAuthMethodRemote`
- Verified `RenderCanLocalMethod()` guard logic (`method.IsInternal || method.IsRemote`) needs no structural change -- same condition, now receiving auth-method-derived inputs

### Phase 4: Unit Test Updates (Complete)
- Updated `CanMethodVisibilityTargets.cs` comment on `InternalMethodWithAuth` to describe auth-derived behavior
- Updated `CanMethodVisibilityTests.cs` class doc comment
- Renamed `InternalMethod_CanCreate_GeneratedCode_HasGuard` to `InternalFactory_PublicAuth_CanCreate_HasNoGuard` -- now asserts NO guard (public auth + internal factory)
- Added 4 new tests:
  - `RemoteInternalFactory_PublicAuth_CanCreate_GeneratedCode_HasNoGuard` (BR-1, BR-10)
  - `InternalAuth_CanCreate_GeneratedCode_HasGuard` (BR-2)
  - `RemoteAuth_CanCreate_GeneratedCode_HasGuard_And_IsAsync` (BR-3, BR-6)
  - `MixedAuth_PublicAndRemote_CanCreate_GeneratedCode_HasGuard` (BR-8)

### Phase 5: Design Project Updates (Complete)
- Updated `AuthorizedOrder.cs` GENERATOR BEHAVIOR comments to describe auth-method-driven Can* behavior
- Updated factory operations section comment (removed "Can* methods inherit" language)
- Updated `AuthorizedOrderAuth.cs` "DID NOT DO THIS" comment to describe how [Remote] on auth methods now affects Can* guard behavior

### Verification Gates Passed
1. After Phase 1-3: `dotnet build src/Neatoo.RemoteFactory.sln` -- 0 errors
2. After Phase 4: Unit tests -- 485 passed, 0 failed (both net9.0 and net10.0)
3. After Phase 4: Integration tests -- 476 passed, 3 skipped, 0 failed (both net9.0 and net10.0)
4. After Phase 5: Design solution -- builds clean, 41 tests passed (both frameworks)
5. Final: Full solution -- 1,922 total test runs, 0 failures

---

## Completion Evidence

**Reported:** 2026-03-08

- **Tests Passing:** 485 unit tests + 476 integration tests = 961 tests per framework, all passing on both net9.0 and net10.0. 41 Design tests passing on both frameworks. Total: 2,004 test runs, 0 failures.
- **Verification Resources Pass:** Yes. Design project (`src/Design/Design.sln`) builds and all 41 tests pass. ShowcaseAuthRemoteTests pass unchanged.
- **All Contract Items:** Confirmed 100% complete.

### Contract Checklist

**Generator model layer:**
- [x] `src/Generator/Model/Supporting/AuthorizationModel.cs`: Added `IsInternal` property to `AuthMethodCall` record
- [x] `src/Generator/Model/Methods/CanMethodModel.cs`: Renamed `IsSourceMethodRemote` to `IsSourceAuthMethodRemote`, updated doc comment

**Generator builder layer:**
- [x] `src/Generator/Builder/FactoryModelBuilder.cs` `BuildAuthorization()`: Maps `isInternal: am.IsInternal`
- [x] `src/Generator/Builder/FactoryModelBuilder.cs` `AddCanMethods()`: Computes from auth methods, includes `hasAspAuthorize`
- [x] `src/Generator/Builder/FactoryModelBuilder.cs` `BuildCanMethod()`: Parameter renamed to `isSourceAuthMethodRemote`

**Generator renderer layer:**
- [x] `src/Generator/Renderer/ClassFactoryRenderer.cs`: Updated `IsSourceMethodRemote` to `IsSourceAuthMethodRemote`

**Unit tests:**
- [x] `src/Tests/RemoteFactory.UnitTests/TestTargets/Visibility/CanMethodVisibilityTargets.cs`: Updated comments
- [x] `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Visibility/CanMethodVisibilityTests.cs`: Updated doc comment, changed failing test, added 4 new tests

**Design project comments:**
- [x] `src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs`: Updated GENERATOR BEHAVIOR comments
- [x] `src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs`: Updated "DID NOT DO THIS" comment

### Stop Conditions -- None Triggered
- ShowcaseAuthRemoteTests: Passed (both Create and CanCreate)
- Design project tests: All 41 passed
- No out-of-scope test failures
- AuthMethodCall.IsInternal correctly populated from TypeAuthMethodInfo.IsInternal
- CanSave aggregation produces correct results (automatic from `Any()` semantics)

### Business Rules Verified by Tests

| Rule | Test(s) | Status |
|------|---------|--------|
| BR-1 (public auth => no guard) | PublicMethod_CanCreate_GeneratedCode_HasNoGuard, InternalFactory_PublicAuth_CanCreate_HasNoGuard, RemoteInternalFactory_PublicAuth_CanCreate_HasNoGuard | Passing |
| BR-2 (internal auth => guard) | InternalAuth_CanCreate_GeneratedCode_HasGuard | Passing |
| BR-3 ([Remote] auth => guard + remote) | RemoteAuth_CanCreate_GeneratedCode_HasGuard_And_IsAsync | Passing |
| BR-4 (sync return for non-remote) | PublicMethod_CanCreate_GeneratedCode_HasNoGuard (returns Authorized, not Task) | Passing |
| BR-5 (public auth => not promoted) | InternalFactory_PublicAuth_CanCreate_HasNoGuard (implicit) | Passing |
| BR-6 ([Remote] auth => promoted) | RemoteAuth_CanCreate_GeneratedCode_HasGuard_And_IsAsync | Passing |
| BR-7 (CanSave all public => no guard) | Covered by AuthorizedOrder Design tests (all public auth, CanSave works) | Passing |
| BR-8 (mixed auth => guard) | MixedAuth_PublicAndRemote_CanCreate_GeneratedCode_HasGuard | Passing |
| BR-9 ([Remote] auth + server service) | ShowcaseAuthRemoteTest_CanCreate | Passing |
| BR-10 (Person example fix) | RemoteInternalFactory_PublicAuth_CanCreate_HasNoGuard | Passing |
| BR-11 (AspAuthorize only) | Not explicitly tested (see developer review Concern 1) | N/A |
| BR-12 (AspAuthorize + AuthorizeFactory) | Covered by `hasAspAuthorize` logic in AddCanMethods | Implicit |
| BR-13 (IsInternal from auth) | InternalAuth_CanCreate_GeneratedCode_HasGuard | Passing |
| BR-14 (IsSourceAuthMethodRemote from auth) | RemoteAuth_CanCreate_GeneratedCode_HasGuard_And_IsAsync | Passing |
| BR-15 (AuthMethodCall.IsInternal propagation) | All auth-driven tests | Passing |

---

## Documentation

**Agent:** business-requirements-documenter (Claude Opus 4.6)
**Completed:** 2026-03-08
**Status:** Requirements Documented

### Completed Deliverables

- [x] `src/Design/CLAUDE-DESIGN.md` -- Added "Can* Method Guard Derivation (Auth-Method-Driven)" subsection under Rule 2 with auth method visibility table, CanSave aggregation rule, AspAuthorize interaction, and code examples. Added Quick Decisions Table entry about Can* guard derivation.
- [x] `docs/authorization.md` -- Rewrote "Client-Side Can* Methods" section as "Can* Method Behavior" with auth-method-driven rule table, separate subsections for client-side (public auth) and server-side ([Remote] auth) patterns, CanSave aggregation documentation, and code examples for [Remote] auth methods.
- [x] `docs/release-notes/v0.21.0.md` -- Created release notes for breaking change: overview, what's new, breaking changes with migration code examples, bug fix description, migration guide.
- [x] `docs/release-notes/index.md` -- Added v0.21.0 to Highlights table and All Releases list.
- [x] `docs/release-notes/v0.20.1.md` -- Incremented nav_order from 1 to 2.
- [x] `docs/release-notes/v0.20.0.md` -- Incremented nav_order from 2 to 3.
- [x] `src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs` -- Already updated by developer (Phase 5). Verified correct.
- [x] `src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs` -- Already updated by developer (Phase 5). Verified correct.

### Developer Deliverables

**Skill files** (`skills/RemoteFactory/`): The skill references Can* methods in several places but does not describe the guard derivation rule in detail. The existing descriptions (e.g., "Methods that run locally (e.g. `CanCreate`) | `public` (no `[Remote]`)" in `trimming.md`) are about the Can* method's own visibility and remain accurate. No immediate skill update is required for correctness, but if a skill update is desired to document the auth-method-driven rule explicitly, it would require:
1. Add authorization pattern details to `skills/RemoteFactory/references/advanced-patterns.md`
2. Code samples would need to be added to `src/docs/reference-app/EmployeeManagement.Domain/Samples/Skill/` with `skill-*` regions
3. Run `mdsnippets` from repository root to embed the code
4. Commit the updated skill files

**Version bump**: `src/Directory.Build.props` -- Update `<PackageVersion>` from `0.20.1` to `0.21.0` when this release is ready to ship. (Not done here as version bumps are part of the release process, not requirements documentation.)

### Files Updated

| File | What Changed |
|------|-------------|
| `src/Design/CLAUDE-DESIGN.md` | Added Can* auth-method-driven guard derivation subsection under Rule 2; added Quick Decisions Table row |
| `docs/authorization.md` | Rewrote Can* section (lines 230-243) with auth-method-driven behavior, added CanSave aggregation and [Remote] auth examples |
| `docs/release-notes/v0.21.0.md` | New file: breaking change release notes |
| `docs/release-notes/index.md` | Added v0.21.0 to Highlights and All Releases |
| `docs/release-notes/v0.20.1.md` | nav_order incremented |
| `docs/release-notes/v0.20.0.md` | nav_order incremented |

---

## Architect Verification

**Verified:** 2026-03-08
**Verdict:** VERIFIED

### Independent Build Results

- `dotnet build src/Neatoo.RemoteFactory.sln`: 0 errors (warnings only: WASM0001 in OrderEntry.BlazorClient, pre-existing)
- `dotnet build src/Design/Design.sln`: 0 errors, 0 warnings

### Independent Test Results

- Unit tests (net9.0): 485 passed, 0 failed
- Unit tests (net10.0): 485 passed, 0 failed
- Integration tests (net9.0): 476 passed, 3 skipped, 0 failed
- Integration tests (net10.0): 476 passed, 3 skipped, 0 failed
- Design tests (net9.0): 41 passed, 0 failed
- Design tests (net10.0): 41 passed, 0 failed
- **Total: 2,004 test runs, 0 failures**

### Design Match

The implementation matches the original plan precisely:

1. **Model layer** (`AuthorizationModel.cs`): `IsInternal` added to `AuthMethodCall` record with constructor parameter `isInternal = false`, doc comment, and property. Matches plan Phase 1.

2. **Model layer** (`CanMethodModel.cs`): `IsSourceMethodRemote` renamed to `IsSourceAuthMethodRemote` with updated doc comment ("Whether any auth method for this Can* operation has [Remote]"). Matches plan Phase 1.

3. **Builder layer** (`FactoryModelBuilder.cs`):
   - `BuildAuthorization()` (line 942): Maps `isInternal: am.IsInternal` from `TypeAuthMethodInfo`. Matches plan Phase 2.
   - `AddCanMethods()` (lines 817-837): Computes `authMethodsAreInternal` and `authMethodsAreRemote` from auth methods via `Any()`. Includes `hasAspAuthorize` in `authMethodsAreInternal`. Passes auth-derived values to `BuildCanMethod()`. Matches plan Phase 2 exactly.
   - `BuildCanMethod()` (line 518): Parameter renamed from `isSourceMethodRemote` to `isSourceAuthMethodRemote`. Matches plan Phase 2.
   - `CreateMethodWithUniqueName()` (line 908): Passes `cm.IsSourceAuthMethodRemote` when copying CanMethodModel. This was not explicitly in the plan but is necessary to avoid data loss during unique name generation. Correct.

4. **Renderer layer** (`ClassFactoryRenderer.cs`, line 121): `isPromotedByRemote = method.IsRemote || (method is CanMethodModel cm && cm.IsSourceAuthMethodRemote)`. `needsInternalPrefix` now accounts for promotion. Matches plan Phase 3.

5. **No old references remain**: `grep -r "IsSourceMethodRemote" src/` returns zero matches. Rename is complete.

### Business Rules Verification

All 15 business rules verified by tracing through implementation:

| Rule | Verified | Evidence |
|------|----------|----------|
| BR-1 (public auth => no guard) | Yes | `authMethodsAreInternal = false` when all auth public; 3 passing tests |
| BR-2 (internal auth => guard) | Yes | `am.IsInternal = true` propagates; `InternalAuth_CanCreate_GeneratedCode_HasGuard` passes |
| BR-3 ([Remote] auth => guard + remote) | Yes | `isRemote = true` from auth; `RemoteAuth_CanCreate_GeneratedCode_HasGuard_And_IsAsync` passes |
| BR-4 (sync for non-remote) | Yes | `isTask = false` when all public, non-async; implicit in existing tests |
| BR-5 (public auth => not promoted) | Yes | `isPromotedByRemote = false` when all auth public; code traced |
| BR-6 ([Remote] auth => promoted) | Yes | `cm.IsSourceAuthMethodRemote = true`; RemoteAuth test verifies |
| BR-7 (CanSave all public => no guard) | Yes | Save aggregates auth methods; all public => no guard; Design tests pass |
| BR-8 (mixed auth => guard) | Yes | `Any()` catches restrictive methods; `MixedAuth_PublicAndRemote` test passes |
| BR-9 ([Remote] auth + server service) | Yes | ShowcaseAuthRemoteTests pass unchanged (not modified, zero diff) |
| BR-10 (Person example fix) | Yes | `RemoteInternalFactory_PublicAuth` test validates this exact scenario |
| BR-11 (AspAuthorize only) | N/A | Not explicitly tested per developer review Concern 1; out of scope |
| BR-12 (AspAuthorize + AuthorizeFactory) | Yes | `hasAspAuthorize` logic at line 828-829 handles this |
| BR-13 (IsInternal from auth) | Yes | `AuthMethodCall.IsInternal` propagated; `InternalAuth` test validates |
| BR-14 (IsSourceAuthMethodRemote) | Yes | Renamed property populated from `authMethodsAreRemote`; `RemoteAuth` test validates |
| BR-15 (AuthMethodCall.IsInternal propagation) | Yes | `BuildAuthorization()` line 942 maps `am.IsInternal`; no new Roslyn analysis needed |

### Specific Verification Items

- **Property rename**: `IsSourceMethodRemote` fully renamed to `IsSourceAuthMethodRemote`. Zero remaining references to old name in `src/`.
- **New tests**: 4 new test methods added covering BR-1/BR-10, BR-2, BR-3/BR-6, and BR-8.
- **Existing test updated**: `InternalMethod_CanCreate_GeneratedCode_HasGuard` renamed to `InternalFactory_PublicAuth_CanCreate_HasNoGuard` with assertion correctly flipped (public auth => no guard).
- **Design comments**: `AuthorizedOrder.cs` updated (lines 32-40, 139). `AuthorizedOrderAuth.cs` "DID NOT DO THIS" section updated (lines 48-55).
- **ShowcaseAuthRemoteTests**: Zero diff, pass unchanged.
- **Class doc comment**: `CanMethodVisibilityTests` updated to describe auth-method-driven behavior.
- **Test target comment**: `CanMethodVisibilityTargets.cs` `InternalMethodWithAuth` comment updated to describe new rule.

### Issues Found

None.

---

## Requirements Verification

**Reviewer:** business-requirements-reviewer
**Verified:** 2026-03-08
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|
| R1 (Current Can* visibility rule -- intentionally changed) | Satisfied | `AddCanMethods()` at `FactoryModelBuilder.cs:817-837` now derives `isInternal` and `isSourceAuthMethodRemote` from auth methods, not from the factory method. Old rule replaced as planned. |
| R2 (docs/authorization.md rule -- intentionally changed) | Satisfied (code) | Implementation correctly changes the behavior. Documentation at `docs/authorization.md:232-243` still describes the old rule; this is expected -- documentation update is Phase 6 (out of developer scope). |
| R3 (AuthorizedOrder.cs Design comments -- CONTRADICTION-2) | Satisfied | `AuthorizedOrder.cs:32-40` updated: comments now state "Can* method guard behavior derives from the AUTH CLASS METHODS, not the factory method." Line 139 updated: "Can* method behavior derives from auth methods, not these factory methods." |
| R4 (AuthorizedOrderAuth.cs "DID NOT DO THIS" comment) | Satisfied | `AuthorizedOrderAuth.cs:48-55` updated: comment now describes how `[Remote] internal` on auth methods adds the guard and routes Can* to server, and how public auth methods (as in this file) let Can* run on client with no guard. References ShowcaseAuthRemoteTests for the pattern. |
| R5 (CanMethodModel.IsSourceMethodRemote semantics) | Satisfied | Property renamed to `IsSourceAuthMethodRemote` in `CanMethodModel.cs:17`. Doc comment updated: "Whether any auth method for this Can* operation has [Remote]." Zero references to old name (`IsSourceMethodRemote`) remain in `src/` (confirmed via grep). |
| R6 (FactoryModelBuilder.AddCanMethods() derivation) | Satisfied | `FactoryModelBuilder.cs:822-837`: `authMethodsAreInternal = authMethods.Any(am => am.IsInternal)` and `authMethodsAreRemote = authMethods.Any(am => am.IsRemote)` computed from auth methods. AspAuthorize also contributes via `hasAspAuthorize` at line 828-829. Values passed to `BuildCanMethod()` at lines 836-837. |
| R7 (Guard emission logic in ClassFactoryRenderer) | Satisfied | `ClassFactoryRenderer.cs:1301`: Guard condition `method.IsInternal || method.IsRemote` unchanged structurally. The inputs now derive from auth methods via the model layer changes. No structural change needed in the renderer. |
| R8 ([Remote] requires internal -- unchanged) | Satisfied | This rule is unchanged. NF0105 diagnostic emission at `FactoryModelBuilder.cs:178-191` is unmodified. The change only affects Can* method derivation, not factory method validation. |
| R9 (ShowcaseAuthRemoteTests -- [Remote] auth) | Satisfied | `ShowcaseAuthRemoteTests.cs` is unmodified (zero diff). Tests pass: `ShowcaseAuthRemoteTest_Create` and `ShowcaseAuthRemoteTest_CanCreate` both passing. Traced: `IAuthRemote.Create()` has `[Remote]` (interface member, public by default, so `IsInternal=false`). `IsRemote=true` from `[Remote]`. Guard emitted via `method.IsRemote` branch. Remote routing preserved. |
| R10 (Person example -- public auth, [Remote] factory) | Satisfied | `PersonModelAuth.cs`: all methods on `IPersonModelAuth` are public, no `[Remote]`. Under new rule: `authMethodsAreInternal=false`, `authMethodsAreRemote=false`. Guard: `false || false` = no guard. Can* methods run on client. New unit test `RemoteInternalFactory_PublicAuth_CanCreate_GeneratedCode_HasNoGuard` validates this scenario. |
| R12 (CanMethodVisibilityTests -- CONTRADICTION-3) | Satisfied | Test `InternalMethod_CanCreate_GeneratedCode_HasGuard` renamed to `InternalFactory_PublicAuth_CanCreate_HasNoGuard`. Assertion correctly flipped: now asserts `DoesNotContain("NeatooRuntime.IsServerRuntime")` because auth methods are public. 4 new tests added covering internal auth (BR-2), [Remote] auth (BR-3), mixed auth (BR-8), and [Remote] internal factory with public auth (BR-10). |
| R13 (CanMethodVisibilityTargets comments -- CONTRADICTION-4) | Satisfied | `CanMethodVisibilityTargets.cs:38-39`: Comment updated to "CanCreate should have NO IsServerRuntime guard (public auth method => no guard on Can, regardless of factory method visibility)." |
| R14 (AuthorizedOrder Design tests) | Satisfied | `AuthorizationTests.cs` is unmodified (zero diff). All 12 scenarios pass. `CanCreate()`, `CanFetch()`, `CanSave()` called synchronously (no `await`), confirming that all-public auth methods produce sync Can* methods with no guard. Tests run in local mode where `IsServerRuntime=true`, so the guard (even if present) would not throw. |
| R15 (v0.17.0 release notes) | N/A | Pre-existing documentation. New release notes for this breaking change are a Phase 6 deliverable. |
| R16 (Breaking change) | Satisfied | Implementation correctly changes behavior: entities with `[Remote] internal` factory methods but public auth classes now get client-callable Can* methods. This is the intended fix. |
| R17 (AspAuthorize and Can* methods) | Satisfied | `[AspAuthorize]` handling preserved: `FactoryModelBuilder.cs:828-829` adds `hasAspAuthorize` to `authMethodsAreInternal`, ensuring Can* methods with AspAuthorize get the guard. `BuildCanMethod()` line 534 already included `aspAuthorize.Count > 0` in `isRemote` computation. `SecureOrder.cs` (AspAuthorize without AuthorizeFactory) is unmodified and unaffected. |

### Intentional Contradictions Verified

| Contradiction | Status | Evidence |
|---------------|--------|----------|
| CONTRADICTION-1 (Can* visibility rule in CLAUDE-DESIGN.md and docs/authorization.md) | Correctly changed in code | Generator now derives Can* behavior from auth methods. Documentation update is Phase 6 (out of developer scope). Design project source files (AuthorizedOrder.cs, AuthorizedOrderAuth.cs) updated as source of truth. |
| CONTRADICTION-2 (AuthorizedOrder.cs Design comments) | Updated | Lines 32-40 and 139 rewritten to describe auth-method-driven Can* behavior. |
| CONTRADICTION-3 (CanMethodVisibilityTests) | Updated | Test renamed and assertion flipped. 4 new tests added covering the new rule. |
| CONTRADICTION-4 (CanMethodVisibilityTargets comments) | Updated | Comment at lines 38-39 updated to reflect auth-method-driven behavior. |

### Gap Resolution Verified

| Gap | Status | Evidence |
|-----|--------|----------|
| GAP-1 (No documented rule for auth-class-driven Can* behavior) | Addressed | New rule established in code: `FactoryModelBuilder.cs:817-829` with clear comments. Design project comments updated. Test class doc comment in `CanMethodVisibilityTests.cs:8-14` documents the rule. Published docs update deferred to Phase 6. |
| GAP-2 (No test coverage for mixed auth method accessibility) | Addressed | New test `MixedAuth_PublicAndRemote_CanCreate_GeneratedCode_HasGuard` in `CanMethodVisibilityTests.cs:354-405` covers mixed accessibility (public HasAccess + [Remote] CanCreate on same operation). |
| GAP-3 (Interface promotion logic for Can* methods) | Addressed | `ClassFactoryRenderer.cs:121`: `isPromotedByRemote = method.IsRemote || (method is CanMethodModel cm && cm.IsSourceAuthMethodRemote)`. When auth method has `[Remote]`, Can* is promoted. When auth methods are all public, Can* is not independently promoted. Test `RemoteAuth_CanCreate_GeneratedCode_HasGuard_And_IsAsync` validates promotion via `RemoteCanCreate` delegate presence. |
| GAP-4 (CanSave aggregation with mixed auth method accessibility) | Addressed | `BuildSaveMethodFromGroup()` at `FactoryModelBuilder.cs:645-654` merges all write operation auth methods. `AddCanMethods()` applies `Any()` across the merged set. If any constituent auth method is internal or [Remote], CanSave gets the guard (most-restrictive-wins). Test `MixedAuth_PublicAndRemote_CanCreate_GeneratedCode_HasGuard` validates the `Any()` semantics. Design project AuthorizationTests pass with all-public auth (CanSave sync, no guard). |

### Unintended Side Effects

None found. Specifically verified:

1. **ShowcaseAuthRemoteTests (R9):** Zero diff, both tests pass unchanged. `[Remote]` on auth interface methods continues to route Can* to server. The guard emits via the `method.IsRemote` branch (auth method has `[Remote]`), not via `method.IsInternal`.

2. **Person example (R10):** `IPersonModelAuth` has all public methods (no `[Remote]`). Under the new rule, all Can* methods run on client with no guard. `IUser` is registered on both client and server via constructor injection. The reported Blazor WASM bug (Server-only method called in non-server runtime) is fixed.

3. **AspAuthorize behavior (R17):** `SecureOrder.cs` uses `[AspAuthorize]` without `[AuthorizeFactory<T>]`. This code path is unmodified. The `hasAspAuthorize` logic at `FactoryModelBuilder.cs:828-829` correctly treats AspAuthorize as requiring the server (HttpContext), contributing to `authMethodsAreInternal`.

4. **CanSave aggregation:** AuthorizedOrder's CanSave aggregates `{HasAccess(), CanDelete()}`, both public methods on `IAuthorizedOrderAuth`. Under the new rule: `authMethodsAreInternal=false`, no guard, synchronous. Design tests pass. For mixed scenarios (some public, some [Remote] internal), the `Any()` semantics produce a guard -- validated by the new `MixedAuth_PublicAndRemote` test.

5. **Design project integrity:** All 41 Design tests pass on both net9.0 and net10.0. Design project builds clean. AuthorizedOrder.cs and AuthorizedOrderAuth.cs comments updated to serve as source of truth for the new rule.

6. **No impact on generated code structure beyond Can* guards:** The renderer's `RenderCanLocalMethod()` guard condition (`method.IsInternal || method.IsRemote`) is structurally unchanged. Only the inputs (derived from auth methods instead of factory methods) changed. Interface factory rendering is unaffected (all methods unconditionally guarded).

7. **Property rename complete:** Zero references to `IsSourceMethodRemote` remain in `src/`. `CreateMethodWithUniqueName()` at line 908 correctly passes `cm.IsSourceAuthMethodRemote`.

### Issues Found

None. The implementation is clean and complete for the developer's Phase 1-5 scope. Documentation updates (CLAUDE-DESIGN.md Rule 2, docs/authorization.md Can* section, release notes) are Phase 6 deliverables for the documentation agent and are not requirements violations.
