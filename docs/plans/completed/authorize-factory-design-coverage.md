# AuthorizeFactory Design Project Coverage

**Date:** 2026-03-08
**Related Todo:** [AuthorizeFactory Design Project Coverage](../todos/completed/authorize-factory-design-coverage.md)
**Status:** Complete
**Last Updated:** 2026-03-08

---

## Overview

Add `[AuthorizeFactory<T>]` coverage to the Design project (the source of truth for RemoteFactory patterns). Currently, `SecureOrder.cs` only describes `[AuthorizeFactory]` in comments without using it, and no Design.Tests cover `Can*` authorization methods. This plan converts SecureOrder to use `[AuthorizeFactory<T>]` with `[Remote] internal` methods, creates an authorization interface and implementation, and adds Design.Tests covering all authorization behaviors: `CanCreate`, `CanFetch`, `CanSave`, `CanDelete`, auth failure behaviors, and `TrySave`.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/authorize-factory-design-coverage.md#requirements-review)

### Relevant Existing Requirements

#### Business Rules

- **R1** (`src/RemoteFactory/FactoryAttributes.cs:113-117`): `[AuthorizeFactory<T>]` takes a generic type `T` (interface or concrete class), applied to classes or interfaces. Well-established with extensive test coverage. -- Relevance: The Design project must follow this attribute contract exactly.

- **R2** (`src/RemoteFactory/FactoryAttributes.cs:119-127`): Auth interface methods are marked with `[AuthorizeFactory(AuthorizeFactoryOperation.Xxx)]` and must return `bool`, `Task<bool>`, `string?`, or `Task<string?>`. Wrong return type emits NF0202. -- Relevance: Auth interface methods must use correct return types and operation attributes.

- **R3** (`src/Design/CLAUDE-DESIGN.md:396-443`, `docs/authorization.md:230-243`): Can* methods inherit guard behavior from parent factory method. Public parent = no `IsServerRuntime` guard. Internal parent = `IsServerRuntime` guard. -- Relevance: Core behavior to demonstrate; SecureOrder uses `[Remote] internal` so its Can* methods will have the guard.

- **R4** (`src/Design/CLAUDE-DESIGN.md:141`): `[Remote]` requires `internal` (NF0105). `[Remote] internal` methods are promoted to `public` on the factory interface. -- Relevance: SecureOrder already uses `[Remote] internal`, so Can* methods for its operations get promoted to `public` on the factory interface with guards.

- **R5** (`src/Design/CLAUDE-DESIGN.md:469-473`): Generator emits explicit `services.TryAddTransient<IFooAuth, FooAuth>()` registrations for every `[AuthorizeFactory<T>]` type. Naming convention: `ISecureOrderAuth` -> `SecureOrderAuth`. -- Relevance: Auth type will be auto-registered for trimming; Design code should document this.

- **R6** (`docs/authorization.md:245-250`): Create/Fetch return null on auth failure. Save throws `NotAuthorizedException`. Events bypass authorization. -- Relevance: Tests must verify all three failure modes.

- **R7** (`docs/authorization.md:87-101`): `Read` covers Create + Fetch. `Write` covers Insert + Update + Delete. Individual flags for fine-grained control. Bitwise OR combines operations. -- Relevance: Auth interface should demonstrate both broad-scope (`Read | Write`) and fine-grained operation flags.

- **R8** (`src/Tests/RemoteFactory.IntegrationTests/Showcase/ShowcaseAuthRemoteTests.cs`): Auth interface methods can have `[Remote]`, making auth checks execute on the server with server-only dependencies. -- Relevance: Advanced pattern. Defer to future todo per Recommendation 8.

- **R9** (`src/Design/Design.Domain/Aggregates/SecureOrder.cs:39`): SecureOrder is currently `public partial class`. Different from the `internal class + public interface` pattern used by `Order`. -- Relevance: Key design decision -- convert to `internal` to demonstrate canonical pattern with `[Remote] internal`.

- **R10** (`src/Design/CLAUDE-DESIGN.md:615-628`): Design Completeness Checklist has no item for `[AuthorizeFactory<T>]`. -- Relevance: Must add checklist item.

- **R11** (`src/Tests/RemoteFactory.IntegrationTests/Showcase/ShowcaseAuthTests.cs`): ShowcaseAuthObj is the most complete existing example of `[AuthorizeFactory]` with `internal class + public interface + IFactorySaveMeta`. -- Relevance: Template for the Design project implementation.

- **R12** (`src/Examples/Person/Person.DomainModel/PersonModel.cs`, `PersonModelAuth.cs`): Person example uses `[AuthorizeFactory<IPersonModelAuth>]` with `[Remote] internal` on an `internal class`. Auth implementation uses constructor-injected `IUser` service. -- Relevance: Real-world working pattern to reference.

- **R13** (`src/Design/CLAUDE-DESIGN.md:632-643`): No design debt entry prohibits implementing `[AuthorizeFactory]` in Design. -- Relevance: No blockers.

#### Existing Tests

- `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Visibility/CanMethodVisibilityTests.cs`: Verifies Can* guard behavior at generator level (public = no guard, internal = guard). -- Relevance: Generator-level coverage exists; Design.Tests add integration-level coverage.

- `src/Tests/RemoteFactory.IntegrationTests/Showcase/ShowcaseAuthTests.cs`: Full integration coverage of `[AuthorizeFactory]` -- CanCreate, CanFetch, CanSave, CanDelete, TrySave, NotAuthorizedException. -- Relevance: Proves the feature works; Design project needs its own source-of-truth demonstration.

- `src/Tests/RemoteFactory.UnitTests/Diagnostics/NF0202Tests.cs`: Verifies wrong return type on auth methods emits NF0202. -- Relevance: Diagnostic coverage exists.

### Gaps

- **GAP-1**: No Design source-of-truth for `[AuthorizeFactory<T>]`. SecureOrder mentions it in comments only.
- **GAP-2**: No Design.Tests for Can* methods. Zero test coverage of authorization behavior in Design.
- **GAP-3**: No Design source-of-truth for remote auth interface (`[Remote]` on auth methods). Deferred per Recommendation 8.
- **GAP-4**: CLAUDE-DESIGN.md Design Completeness Checklist missing `[AuthorizeFactory<T>]` item.
- **GAP-5**: No Design demonstration of CanSave/TrySave with auth.

### Contradictions

None. The todo proposes adding coverage for an existing, well-tested feature.

### Recommendations for Architect

1. Follow ShowcaseAuthObj pattern as template.
2. Convert SecureOrder to `internal class + public interface` (matching Order.cs canonical pattern).
3. Demonstrate Can* methods on `[Remote] internal` methods (guard behavior).
4. Include auth failure behaviors in Design.Tests (null returns, NotAuthorizedException, TrySave).
5. Demonstrate `AuthorizeFactoryOperation.Read | Write` for broad scope.
6. Update CLAUDE-DESIGN.md Design Completeness Checklist.
7. Document auth auto-registration for trimming in Design code comments.
8. Defer remote auth interface (`[Remote]` on auth methods) to future todo.

---

## Business Rules (Testable Assertions)

### Attribute Application

1. WHEN `[AuthorizeFactory<ISecureOrderAuth>]` is applied to SecureOrder class, THEN the generated `ISecureOrderFactory` interface includes `CanCreate()`, `CanFetch()`, `CanSave()`, `CanDelete()` methods returning `Authorized` or `Authorized<ISecureOrder>`. -- Source: R1, R2

2. WHEN the auth interface `ISecureOrderAuth` defines a method with `[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]`, THEN that method is consulted for all Read (Create, Fetch) and Write (Insert, Update, Delete) operations. -- Source: R7

### Can* Method Behavior

3. WHEN `ISecureOrderAuth.CanCreate()` returns `true`, THEN `factory.CanCreate()` returns `Authorized` with `HasAccess == true`. -- Source: R3, R11

4. WHEN `ISecureOrderAuth.CanFetch()` returns `false`, THEN `factory.CanFetch()` returns `Authorized` with `HasAccess == false`. -- Source: R3, R11

5. WHEN `ISecureOrderAuth.CanDelete()` returns `false`, THEN `factory.CanSave()` returns `Authorized` with `HasAccess == false`, because CanSave aggregates all write auth checks and the Delete check fails. -- Source: R6, R11 (ShowcaseAuthTests.ShowcaseAuth_CanSave)

6. WHEN all write auth checks pass (CanInsert, CanUpdate, CanDelete all true, or broad Write scope returns true), THEN `factory.CanSave()` returns `Authorized` with `HasAccess == true`. -- Source: R6, R11

### Auth Failure Behavior -- Create and Fetch

7. WHEN authorization denies a Create operation, THEN `factory.Create(...)` returns `null`. -- Source: R6

8. WHEN authorization denies a Fetch operation, THEN `factory.Fetch(...)` returns `null`. -- Source: R6

9. WHEN authorization allows a Create operation, THEN `factory.Create(...)` returns a non-null `ISecureOrder` instance. -- Source: R6

### Auth Failure Behavior -- Save

10. WHEN authorization denies a Save operation (e.g., Delete is denied but IsDeleted=true), THEN `factory.Save(target)` throws `NotAuthorizedException`. -- Source: R6, R11

11. WHEN authorization denies a Save operation, THEN `factory.TrySave(target)` returns `Authorized<ISecureOrder>` with `HasAccess == false` and `Result == null`. -- Source: R11 (ShowcaseAuthTests.ShowcaseAuth_TrySave_Null_CannotDelete)

12. WHEN authorization allows a Save operation (Insert path, all write checks pass), THEN `factory.TrySave(target)` returns `Authorized<ISecureOrder>` with `HasAccess == true` and `Result != null`. -- Source: R11 (ShowcaseAuthTests.ShowcaseAuth_TrySave_Success)

### Visibility and Guard Behavior

13. WHEN SecureOrder's factory methods are `[Remote] internal`, THEN Can* methods on the factory interface are promoted to `public` (because `[Remote]` promotes `internal` to `public` on the interface) and the underlying `LocalCan*` methods have the `IsServerRuntime` guard. -- Source: R3, R4

### Auth Type Registration

14. WHEN `[AuthorizeFactory<ISecureOrderAuth>]` is applied, THEN the generator emits `services.TryAddTransient<ISecureOrderAuth, SecureOrderAuth>()` in `FactoryServiceRegistrar`, creating a static reference that survives IL trimming. -- Source: R5

### Class Visibility

15. WHEN SecureOrder is converted to `internal partial class SecureOrder : ISecureOrder`, THEN the generated factory interface uses `ISecureOrder` in signatures (e.g., `Task<ISecureOrder?> Create(...)`, `Task<ISecureOrder?> Save(ISecureOrder target)`). -- Source: R9, R4 (internal class + public interface pattern from Order.cs)

### Design Completeness

16. WHEN `[AuthorizeFactory<T>]` is demonstrated in the Design project, THEN the CLAUDE-DESIGN.md Design Completeness Checklist includes a checked item for `[AuthorizeFactory<T>]` custom domain authorization. -- Source: R10, NEW

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | CanCreate returns true when auth allows | Auth: CanCreate() -> true | Rule 3 | `factory.CanCreate().HasAccess == true` |
| 2 | CanFetch returns false when auth denies | Auth: CanFetch() -> false | Rule 4 | `factory.CanFetch().HasAccess == false` |
| 3 | CanSave returns false when any write check fails | Auth: CanDelete() -> false | Rule 5 | `factory.CanSave().HasAccess == false` |
| 4 | CanSave returns true when all write checks pass | Auth: all write ops -> true | Rule 6 | `factory.CanSave().HasAccess == true` -- requires separate auth impl or configurable auth |
| 5 | Create returns null when auth denies | Auth: CanCreate() -> false | Rule 7 | `factory.Create(...) == null` |
| 6 | Fetch returns null when auth denies | Auth: CanFetch() -> false | Rule 8 | `factory.Fetch(...) == null` |
| 7 | Create returns non-null when auth allows | Auth: CanCreate() -> true | Rule 9 | `factory.Create(...) != null` |
| 8 | Save throws NotAuthorizedException on denied delete | Auth: CanDelete() -> false, target.IsDeleted=true | Rule 10 | `Assert.Throws<NotAuthorizedException>(() => factory.Save(target))` |
| 9 | TrySave returns HasAccess=false on denied delete | Auth: CanDelete() -> false, target.IsDeleted=true | Rule 11 | `result.HasAccess == false`, `result.Result == null` |
| 10 | TrySave returns HasAccess=true on allowed insert | Auth: all write checks pass, target.IsNew=true | Rule 12 | `result.HasAccess == true`, `result.Result != null`, `result.Result.IsNew == false` |
| 11 | Auth-allowed Create works through client-server serialization | Client container, auth allows Create | Rules 3, 9, 13 | Non-null ISecureOrder returned after round-trip |
| 12 | Auth-denied Fetch returns null through client-server | Client container, auth denies Fetch | Rules 4, 8, 13 | null returned |

---

## Approach

### Strategy

Convert `SecureOrder.cs` from a `[AspAuthorize]`-only demonstration to also demonstrate `[AuthorizeFactory<T>]`. The two authorization mechanisms serve different purposes (ASP.NET Core policy-based vs domain-specific), so they should be in separate entities. Since SecureOrder currently demonstrates `[AspAuthorize]`, keep that as-is and create a **new entity** `AuthorizedOrder` that demonstrates `[AuthorizeFactory<T>]`.

**Rationale for a new entity rather than modifying SecureOrder:**

1. SecureOrder serves as the `[AspAuthorize]` source of truth. Mixing both auth mechanisms in one entity muddies the demonstration.
2. `[AuthorizeFactory<T>]` requires a different class structure (auth interface, auth implementation class) that is better shown in isolation.
3. The CLAUDE-DESIGN.md checklist already has separate items for `[AspAuthorize]` and should have a separate item for `[AuthorizeFactory<T>]`.
4. SecureOrder is currently `public` (which is appropriate for `[AspAuthorize]` demonstration since policies don't generate Can* methods that need the internal+public interface pattern). AuthorizedOrder should use the canonical `internal class + public interface` pattern to fully demonstrate Can* promotion.

### Key Design Decisions

**D1. New entity `AuthorizedOrder` instead of modifying SecureOrder.** SecureOrder continues to demonstrate `[AspAuthorize]`. AuthorizedOrder demonstrates `[AuthorizeFactory<T>]` with all CRUD operations.

**D2. `internal class + public interface` pattern.** AuthorizedOrder follows the canonical Order.cs pattern: `internal partial class AuthorizedOrder : IAuthorizedOrder, IFactorySaveMeta`. This enables the full `[Remote] internal` -> promoted to `public` on factory interface -> Can* methods have `IsServerRuntime` guard demonstration.

**D3. Auth interface with both broad-scope and fine-grained operations.** `IAuthorizedOrderAuth` demonstrates:
- `[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]` for a broad access check
- `[AuthorizeFactory(AuthorizeFactoryOperation.Create)]` for Create-specific check
- `[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]` for Fetch-specific check
- `[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]` for Delete-specific check

**D4. Configurable auth implementation for testing.** The auth implementation (`AuthorizedOrderAuth`) uses static boolean flags (like `VisibilityTestAuth.ShouldAllow` in unit tests) so that Design.Tests can toggle authorization on/off per-operation to test both allowed and denied scenarios.

**D5. Defer remote auth interface.** Per Recommendation 8, `[Remote]` on auth interface methods is an advanced pattern that should be its own future todo.

**D6. Design.Tests only (no unit/integration test changes).** Existing unit and integration tests already cover `[AuthorizeFactory]` extensively. This todo adds Design source-of-truth demonstration only.

---

## Domain Model Behavioral Design

Not applicable. This plan adds a source-of-truth demonstration entity and tests, not a user-facing domain model with computed properties, visibility flags, or validation rules. The entity exists to demonstrate the `[AuthorizeFactory<T>]` pattern.

---

## Design

### File Structure

```
src/Design/Design.Domain/
  Aggregates/
    AuthorizedOrder.cs          -- NEW: AuthorizedOrder entity + IAuthorizedOrder interface
    AuthorizedOrderAuth.cs      -- NEW: IAuthorizedOrderAuth interface + AuthorizedOrderAuth implementation
    SecureOrder.cs              -- MODIFY: Update comments to point to AuthorizedOrder for [AuthorizeFactory] demo

src/Design/Design.Tests/
  FactoryTests/
    AuthorizationTests.cs       -- NEW: Tests for [AuthorizeFactory<T>] behavior

src/Design/
  CLAUDE-DESIGN.md              -- MODIFY: Add [AuthorizeFactory<T>] to checklist and Design Files table
```

### AuthorizedOrder Entity (`AuthorizedOrder.cs`)

```
public interface IAuthorizedOrder : IFactorySaveMeta
{
    int Id { get; set; }
    string CustomerName { get; set; }
    decimal Total { get; set; }
}

[Factory]
[AuthorizeFactory<IAuthorizedOrderAuth>]
internal partial class AuthorizedOrder : IAuthorizedOrder, IFactorySaveMeta
{
    int Id { get; set; }
    string CustomerName { get; set; } = string.Empty;
    decimal Total { get; set; }
    bool IsNew { get; set; } = true;
    bool IsDeleted { get; set; }

    [Remote, Create] internal void Create(string customerName) { ... }
    [Remote, Fetch] internal void Fetch(int id) { ... }
    [Remote, Insert] internal Task Insert() { ... }
    [Remote, Update] internal Task Update() { ... }
    [Remote, Delete] internal Task Delete() { ... }
}
```

Design comment blocks follow the same pattern as Order.cs and SecureOrder.cs -- DESIGN SOURCE OF TRUTH header, DESIGN DECISION notes, GENERATOR BEHAVIOR comments, DID NOT DO THIS sections.

### Auth Interface and Implementation (`AuthorizedOrderAuth.cs`)

```
public interface IAuthorizedOrderAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool HasAccess();

    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}

internal class AuthorizedOrderAuth : IAuthorizedOrderAuth
{
    // Static flags for test configurability
    public static bool AllowAccess { get; set; } = true;
    public static bool AllowCreate { get; set; } = true;
    public static bool AllowFetch { get; set; } = true;
    public static bool AllowDelete { get; set; } = true;

    public bool HasAccess() => AllowAccess;
    public bool CanCreate() => AllowCreate;
    public bool CanFetch() => AllowFetch;
    public bool CanDelete() => AllowDelete;
}
```

Design decisions to document in comments:
- Why `bool` return type (simplest; `Task<bool>`, `string?`, `Task<string?>` are also valid per R2)
- Why broad-scope `Read | Write` plus fine-grained individual ops (demonstrates both patterns per R7)
- Why no `CanInsert()` / `CanUpdate()` methods -- `Write` scope covers them via the broad `HasAccess()` method; fine-grained Delete is shown separately because Delete has different auth requirements in most real systems
- Auto-registration for trimming: generator emits `services.TryAddTransient<IAuthorizedOrderAuth, AuthorizedOrderAuth>()` per R5
- Why static flags: enables test configurability without needing mock DI -- follows the VisibilityTestAuth pattern from unit tests

### Authorization Tests (`AuthorizationTests.cs`)

The test class follows the Design.Tests pattern (DESIGN SOURCE OF TRUTH header, DESIGN DECISION comments). Tests use `DesignClientServerContainers.Scopes()` with custom configuration callbacks to register the auth implementation.

**Container setup:**
- Both client and server containers need `IAuthorizedOrderAuth` registered (since Can* methods run on both sides for [Remote] internal methods)
- The auth implementation registers via `RegisterMatchingName` in `DesignClientServerContainers`, but the static flags need to be set per-test
- Tests reset static flags in setup/teardown to avoid test pollution

**Test methods (mapping to scenarios):**

| Test Method | Scenario # | Assertion |
|---|---|---|
| `AuthorizedOrder_CanCreate_ReturnsTrue_WhenAllowed` | 1 | `factory.CanCreate().HasAccess == true` |
| `AuthorizedOrder_CanFetch_ReturnsFalse_WhenDenied` | 2 | `factory.CanFetch().HasAccess == false` |
| `AuthorizedOrder_CanSave_ReturnsFalse_WhenDeleteDenied` | 3 | `factory.CanSave().HasAccess == false` |
| `AuthorizedOrder_CanSave_ReturnsTrue_WhenAllWriteAllowed` | 4 | `factory.CanSave().HasAccess == true` |
| `AuthorizedOrder_Create_ReturnsNull_WhenDenied` | 5 | `result == null` |
| `AuthorizedOrder_Fetch_ReturnsNull_WhenDenied` | 6 | `result == null` |
| `AuthorizedOrder_Create_ReturnsInstance_WhenAllowed` | 7 | `result != null` |
| `AuthorizedOrder_Save_ThrowsNotAuthorizedException_WhenDeleteDenied` | 8 | `Assert.ThrowsAsync<NotAuthorizedException>` |
| `AuthorizedOrder_TrySave_ReturnsFalse_WhenDeleteDenied` | 9 | `result.HasAccess == false`, `result.Result == null` |
| `AuthorizedOrder_TrySave_ReturnsTrue_WhenInsertAllowed` | 10 | `result.HasAccess == true`, `result.Result != null` |
| `AuthorizedOrder_Create_WorksThroughClientServer` | 11 | Non-null after serialization round-trip |
| `AuthorizedOrder_Fetch_ReturnsNull_ThroughClientServer` | 12 | null after serialization round-trip |

### CLAUDE-DESIGN.md Changes

1. **Design Completeness Checklist**: Add `- [x] Custom domain authorization with [AuthorizeFactory<T>] (AuthorizedOrder.cs)` as a new item.
2. **Design Files table**: Add `Design.Domain/Aggregates/AuthorizedOrder.cs` and `AuthorizedOrderAuth.cs` entries.
3. **SecureOrder.cs table entry**: Update description to clarify it covers `[AspAuthorize]` only.

### SecureOrder.cs Comment Update

Update the header comment to add a cross-reference to `AuthorizedOrder.cs` for the `[AuthorizeFactory<T>]` pattern:

```
// For domain-specific authorization with [AuthorizeFactory<T>], see AuthorizedOrder.cs.
```

### DesignClientServerContainers.cs

No changes needed. The auth type `IAuthorizedOrderAuth` -> `AuthorizedOrderAuth` will be auto-discovered by `RegisterMatchingName` since both are in the same assembly. The `RegisterFactoryTypes` method scans for `[Factory]` attributes and will pick up `AuthorizedOrder`. The static flags on `AuthorizedOrderAuth` enable test-level configuration without DI changes.

---

## Implementation Steps

### Phase 1: Domain Entity and Auth Interface

1. Create `src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs`:
   - `IAuthorizedOrderAuth` interface with `[AuthorizeFactory]` operation attributes
   - `AuthorizedOrderAuth` implementation with static boolean flags
   - Full Design Source of Truth comment blocks

2. Create `src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs`:
   - `IAuthorizedOrder` public interface extending `IFactorySaveMeta`
   - `AuthorizedOrder` internal class with `[Factory]`, `[AuthorizeFactory<IAuthorizedOrderAuth>]`
   - All CRUD operations: Create, Fetch, Insert, Update, Delete -- all `[Remote] internal`
   - Full Design Source of Truth comment blocks

3. Update `src/Design/Design.Domain/Aggregates/SecureOrder.cs`:
   - Add cross-reference comment to AuthorizedOrder.cs for `[AuthorizeFactory<T>]` pattern

4. Verify: `dotnet build src/Design/Design.sln` succeeds with no warnings

### Phase 2: Design Tests

5. Create `src/Design/Design.Tests/FactoryTests/AuthorizationTests.cs`:
   - All 12 test scenarios from the Test Scenarios table
   - Follow Design.Tests comment patterns
   - Use `DesignClientServerContainers.Scopes()` for client-server and local tests
   - Reset static auth flags in each test to avoid pollution

6. Verify: `dotnet test src/Design/Design.sln` passes all tests (existing 29 + new tests)

### Phase 3: Documentation Updates

7. Update `src/Design/CLAUDE-DESIGN.md`:
   - Add `[AuthorizeFactory<T>]` item to Design Completeness Checklist (checked)
   - Add AuthorizedOrder.cs and AuthorizedOrderAuth.cs to Design Files table
   - Update SecureOrder.cs description in Design Files table

8. Verify: `dotnet build src/Design/Design.sln` and `dotnet test src/Design/Design.sln` still pass

---

## Acceptance Criteria

- [ ] `AuthorizedOrder.cs` exists with `[Factory]`, `[AuthorizeFactory<IAuthorizedOrderAuth>]`, `internal partial class`, and `IAuthorizedOrder` public interface
- [ ] `AuthorizedOrderAuth.cs` exists with `IAuthorizedOrderAuth` interface (broad-scope + fine-grained operation attributes) and `AuthorizedOrderAuth` implementation with static flags
- [ ] `AuthorizationTests.cs` exists with all 12 test scenarios passing
- [ ] SecureOrder.cs has cross-reference comment to AuthorizedOrder.cs
- [ ] CLAUDE-DESIGN.md checklist has `[AuthorizeFactory<T>]` item (checked)
- [ ] CLAUDE-DESIGN.md Design Files table includes AuthorizedOrder.cs and AuthorizedOrderAuth.cs
- [ ] `dotnet build src/Design/Design.sln` succeeds with 0 warnings
- [ ] `dotnet test src/Design/Design.sln` passes all tests (29 existing + 12 new = 41+ tests)
- [ ] All existing 29 tests continue to pass

---

## Dependencies

- Current `removeRemoteOnly` branch with `[Remote] requires internal` merged
- Design solution builds and all 29 tests pass (verified: 2026-03-08)

---

## Risks / Considerations

1. **Auth type auto-discovery**: `RegisterMatchingName` in `DesignClientServerContainers` must find `AuthorizedOrderAuth` from `IAuthorizedOrderAuth`. This follows the established naming convention and should work, but needs verification during implementation.

2. **Static flag test isolation**: Tests using `AuthorizedOrderAuth.AllowXxx` static flags could pollute each other if not properly reset. Each test must reset all flags before execution. Consider using `IDisposable` pattern on the test class or setting defaults in a constructor.

3. **CanSave aggregation behavior**: CanSave aggregates all write auth checks. The exact aggregation logic depends on which `AuthorizeFactoryOperation` flags are defined on the auth interface. If `HasAccess()` covers `Read | Write`, and `CanDelete()` covers `Delete`, then CanSave checks both `HasAccess()` (for the Write scope) and `CanDelete()` (for the Delete operation). The interplay between broad-scope and fine-grained checks should be confirmed during implementation.

4. **No breaking changes**: This plan adds new files only. SecureOrder.cs gets a comment addition only. No existing files are structurally modified.

---

## Architectural Verification

**Scope Table:**

| Pattern/Feature | Current State | After Implementation |
|---|---|---|
| `[AuthorizeFactory<T>]` in Design | Comment-only in SecureOrder.cs | Full demonstration in AuthorizedOrder.cs |
| Can* method tests in Design | None | 12 tests covering all scenarios |
| `[AspAuthorize]` in Design | Demonstrated in SecureOrder.cs | Unchanged |
| Internal class + public interface with auth | Not demonstrated | AuthorizedOrder follows canonical pattern |
| Auth failure behaviors in Design | Not demonstrated | Tests cover null returns, NotAuthorizedException, TrySave |

**Verification Evidence:**

- Design solution builds: Verified (`dotnet build src/Design/Design.sln` -- 0 warnings, 0 errors)
- Design tests pass: Verified (`dotnet test src/Design/Design.sln` -- 29 passed, 0 failed on both net9.0 and net10.0)
- ShowcaseAuthTests pattern: Verified as template (12 tests covering comprehensive auth scenarios)
- CanMethodVisibilityTests: Verified guard behavior (public = no guard, internal = guard)

**Breaking Changes:** No. All changes are additive (new files, new tests, comment updates).

**Codebase Analysis:**

Files examined:
- `src/Design/Design.Domain/Aggregates/SecureOrder.cs` -- Current `[AspAuthorize]` demonstration
- `src/Design/Design.Domain/Aggregates/Order.cs` -- Canonical `internal class + public interface` pattern
- `src/Tests/RemoteFactory.IntegrationTests/Showcase/ShowcaseAuthTests.cs` -- Template for auth tests
- `src/Tests/RemoteFactory.IntegrationTests/Showcase/ShowcaseAuthRemoteTests.cs` -- Remote auth pattern (deferred)
- `src/Examples/Person/Person.DomainModel/PersonModel.cs` -- Real-world auth example
- `src/Examples/Person/Person.DomainModel/PersonModelAuth.cs` -- Real-world auth interface/impl
- `src/Tests/RemoteFactory.UnitTests/TestTargets/Visibility/CanMethodVisibilityTargets.cs` -- Static flag pattern for auth
- `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Visibility/CanMethodVisibilityTests.cs` -- Guard verification tests
- `src/RemoteFactory/FactoryAttributes.cs` -- Attribute definitions
- `src/RemoteFactory/AuthorizeFactoryOperation.cs` -- Operation enum
- `src/RemoteFactory/NotAuthorizedException.cs` -- Exception class
- `src/RemoteFactory/Authorized.cs` -- Authorized result class
- `src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs` -- Test infrastructure
- `src/Design/Design.Tests/FactoryTests/AggregateTests.cs` -- Existing aggregate test patterns
- `src/Design/CLAUDE-DESIGN.md` -- Design reference document

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Entity + Auth files | developer | Yes | Clean context focused on entity creation | None |
| Phase 2: Tests | developer | No | Same agent continues with context of created files | Phase 1 |
| Phase 3: CLAUDE-DESIGN.md updates | developer | No | Same agent, minor documentation changes | Phase 2 |

**Parallelizable phases:** None -- each phase builds on the previous.

**Notes:** All three phases are small enough for a single agent session. The "No" for Phases 2 and 3 means resume the Phase 1 agent since it has context about the files just created.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-08

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | Generator: `FactoryModelBuilder.AddCanMethods()` iterates factory methods with `HasAuth==true`, calls `BuildCanMethod(method.Name, ...)` which creates `CanMethodModel(name: "Can{method.Name}")`. For AuthorizedOrder with `[AuthorizeFactory<IAuthorizedOrderAuth>]`, generates CanCreate, CanFetch, CanSave (aggregated from write methods), CanDelete. Return type: `Authorized` or `Authorized<IAuthorizedOrder>` depending on method. | Generated factory interface includes CanCreate(), CanFetch(), CanSave(), CanDelete() | Yes | CanSave is generated via `BuildSaveMethodFromGroup()` which merges auth from all write methods (line 644-660 of FactoryModelBuilder.cs), then `AddCanMethods` creates CanSave from the merged auth |
| 2 | Generator: `TypeFactoryMethodInfo` constructor (line 501 of FactoryGenerator.Types.cs): `((int?)authMethod.AuthorizeFactoryOperation & (int)this.FactoryOperation) != 0`. `FactoryOperation.Create = AuthorizeFactoryOperation.Create \| Read = 1\|64 = 65`. Auth method `HasAccess()` has `Read \| Write = 64\|128 = 192`. Check: `192 & 65 = 64 != 0` -> matches Create. Check: `192 & 66 = 64 != 0` -> matches Fetch (Fetch=2\|64=66). Check: `192 & 132 = 128 != 0` -> matches Insert (4\|128=132). Similarly matches Update (8\|128=136) and Delete (16\|128=144). | HasAccess() consulted for all Read and Write operations | Yes | The FactoryOperation enum embeds broad-scope flags: Create includes Read, Insert includes Write, etc. Bitwise AND correctly matches |
| 3 | Generated `LocalCanCreate()` calls `RenderAuthorizationChecks()` (ClassFactoryRenderer.cs line 1308). This resolves `IAuthorizedOrderAuth` from DI, calls `HasAccess()` and `CanCreate()`. Both return `Authorized` via implicit conversion from `bool`. If both return true, method returns `new Authorized(true)`. Test sets `AllowAccess=true, AllowCreate=true`. | `factory.CanCreate().HasAccess == true` | Yes | Auth checks are sequential; both `HasAccess()` (Read\|Write scope) and `CanCreate()` (Create scope) match the Create operation and both must pass |
| 4 | Same path as Rule 3 but for Fetch. `LocalCanFetch()` calls `HasAccess()` (Read\|Write matches Fetch) and `CanFetch()` (Fetch scope matches Fetch). Test sets `AllowFetch=false`. `CanFetch()` returns false -> `authorized.HasAccess == false` -> early return with `new Authorized(false)` at ClassFactoryRenderer.cs line 1413-1414. | `factory.CanFetch().HasAccess == false` | Yes | First failing auth check short-circuits with HasAccess=false |
| 5 | `CanSave` auth is aggregated from Insert, Update, Delete methods (`BuildSaveMethodFromGroup` line 644-660). Insert gets: HasAccess() (Write scope). Update gets: HasAccess() (Write scope). Delete gets: HasAccess() (Write scope) + CanDelete() (Delete scope). All distinct auth methods for CanSave = {HasAccess(), CanDelete()}. When `AllowDelete=false`, `CanDelete()` returns false -> `authorized.HasAccess == false` -> CanSave returns `Authorized(false)`. | `factory.CanSave().HasAccess == false` | Yes | The distinct aggregation (line 648 `.Distinct()`) avoids duplicate HasAccess() calls. CanDelete() failing causes CanSave to fail |
| 6 | Same aggregated auth as Rule 5. When AllowAccess=true and AllowDelete=true, both HasAccess() and CanDelete() return true. All auth checks pass -> returns `new Authorized(true)`. | `factory.CanSave().HasAccess == true` | Yes | |
| 7 | Generated `LocalCreate()` method (ClassFactoryRenderer.cs `RenderReadLocalMethod`) calls `RenderAuthorizationChecks()`. When auth check fails, method returns null (for Read operations, null return is the auth-failure behavior). `AllowCreate=false` -> `CanCreate()` returns false -> returns `default(IAuthorizedOrder)` which is null. | `factory.Create(...) == null` | Yes | Read methods return nullable type when auth is present. Null return on auth failure is generator behavior |
| 8 | Same pattern as Rule 7 but for Fetch. `AllowFetch=false` -> `CanFetch()` returns false -> returns null. | `factory.Fetch(...) == null` | Yes | |
| 9 | `AllowCreate=true` and `AllowAccess=true` -> both `HasAccess()` and `CanCreate()` return true -> auth passes -> Create method body executes -> returns non-null `IAuthorizedOrder`. | `factory.Create(...) != null` | Yes | |
| 10 | Generated `LocalSave()` method checks `IsDeleted` flag. When `IsDeleted=true`, routes to Delete path. Delete has auth checks: HasAccess() + CanDelete(). `AllowDelete=false` -> CanDelete() returns false. For Save/Write operations, auth failure throws `NotAuthorizedException` (ClassFactoryRenderer.cs `RenderSaveLocalMethod` auth failure path). | `factory.Save(target)` throws `NotAuthorizedException` | Yes | Write operations throw on auth failure (unlike Read which returns null). This is consistent with R6 |
| 11 | `TrySave` wraps Save in a try/catch for `NotAuthorizedException`. When auth denies, catches exception -> returns `Authorized<IAuthorizedOrder>` with `HasAccess=false, Result=null`. | `result.HasAccess == false`, `result.Result == null` | Yes | TrySave is a generated wrapper that converts NotAuthorizedException to Authorized<T> with HasAccess=false |
| 12 | `TrySave` with all write checks passing. `AllowAccess=true, AllowDelete=true`, target.IsNew=true -> routes to Insert. Auth passes -> Insert executes -> `IsNew = false` after FactoryComplete. TrySave returns `Authorized<IAuthorizedOrder>` with `HasAccess=true, Result = saved entity`. | `result.HasAccess == true`, `result.Result != null`, `result.Result.IsNew == false` | Yes | |
| 13 | Generator: `RenderCanLocalMethod()` at ClassFactoryRenderer.cs line 1299-1306: `if (method.IsInternal \|\| method.IsRemote) { sb.AppendLine("if (!NeatooRuntime.IsServerRuntime)"); }`. For AuthorizedOrder's `[Remote] internal` Create, the CanMethodModel is built with `isInternal=true` (from `AddCanMethods` line 822: `isInternal: method.IsInternal`) and `isSourceMethodRemote=true` (line 823). Interface promotion: ClassFactoryRenderer.cs line 121: `isPromotedByRemote = method.IsRemote \|\| (method is CanMethodModel cm && cm.IsSourceMethodRemote)` -> true, so no `internal` prefix on interface. | Can* promoted to public on interface; LocalCan* has IsServerRuntime guard | Yes | Guard ensures Can* checks only run on server; interface promotion exposes them to clients |
| 14 | Generator: `RenderFactoryServiceRegistrar()` at ClassFactoryRenderer.cs line 1475-1492. Collects distinct auth types from methods, emits `services.TryAddTransient<IAuthorizedOrderAuth, AuthorizedOrderAuth>()`. The `ConcreteClassName` is resolved at compile time in `FactoryGenerator.Transform.cs` `TypeAuthMethods()` by finding a class implementing the auth interface via naming convention. | Generator emits explicit DI registration in FactoryServiceRegistrar | Yes | `TryAddTransient` is idempotent with `RegisterMatchingName` |
| 15 | Generator: `RenderInterfaceMethodSignature()` at ClassFactoryRenderer.cs line 143+. When the class is `internal` with a public interface `IAuthorizedOrder`, the generator uses `IAuthorizedOrder` as the service type in factory interface signatures. This is controlled by `TypeInfo.ServiceTypeName` which is set to the public interface name when found. | Factory interface uses `IAuthorizedOrder` in signatures | Yes | Same pattern as Order.cs -> IOrder |
| 16 | Manual edit to CLAUDE-DESIGN.md: add `- [x] Custom domain authorization with [AuthorizeFactory<T>] (AuthorizedOrder.cs)` to Design Completeness Checklist. | Checklist has checked item | Yes | Manual documentation edit, no generator involvement |

### Concerns

#### Concern 1: Test Async/Sync Return Types (Clarifying, Not Blocking)

The ShowcaseAuthObj tests use **synchronous** factory methods (`public` methods, no `[Remote]`), so `_factory.CanCreate()` returns `Authorized` directly (or `bool` via implicit conversion). The plan's AuthorizedOrder uses `[Remote] internal`, so `factory.CanCreate()` will return `Task<Authorized>` (the CanMethod inherits async from the remote source method). The test code must use `await factory.CanCreate()` and the test methods must be `async Task`, not `void`.

The plan's Test Scenarios table (line 137) says `factory.CanCreate().HasAccess == true` without `await`. The plan's test method signatures at line 293 (`AuthorizedOrder_CanCreate_ReturnsTrue_WhenAllowed`) don't specify async.

**Recommendation:** The developer should be aware that all CanXxx tests must be async. The plan's pseudocode is illustrative, not literal, so this is not a blocking concern -- the developer will discover the correct signatures from compilation.

#### Concern 2: CanSave Aggregation -- CanCreate/CanFetch Methods Not in CanSave

The plan's auth interface defines:
- `HasAccess()` with `Read | Write` (covers all operations)
- `CanCreate()` with `Create` (fine-grained)
- `CanFetch()` with `Fetch` (fine-grained)
- `CanDelete()` with `Delete` (fine-grained)

For CanSave, the aggregated auth from Insert+Update+Delete's distinct auth methods will be `{HasAccess(), CanDelete()}`. Notably, `CanCreate()` (Create scope = 1) does NOT match any write operation (Insert=4|128, Update=8|128, Delete=16|128), and `CanFetch()` (Fetch scope = 2) also does not match. This is correct behavior -- CanSave should only check write-relevant auth methods. The plan states this correctly in Rule 5. No issue here, just confirming.

#### Concern 3: Static Flag Thread Safety (Low Risk)

The plan uses static boolean flags on `AuthorizedOrderAuth` for test configurability. Since xUnit runs test classes sequentially by default (within a single test class), and each test resets flags before execution, this should be safe. However, if tests in different classes share the same `AuthorizedOrderAuth`, cross-class parallel execution could cause issues. Since Design.Tests likely runs sequentially and only `AuthorizationTests.cs` touches these flags, this is low risk.

**Recommendation:** The developer should reset all static flags at the start of each test method (not just in the constructor) to guard against test ordering issues.

#### Concern 4: `CanSave` Tests Require CanSave on Factory Interface

For the tests to call `factory.CanSave()`, the generated factory interface must include a `CanSave()` method. CanSave is generated when `IFactorySaveMeta` is implemented and the entity has auth. Since AuthorizedOrder implements `IFactorySaveMeta` and has `[AuthorizeFactory<IAuthorizedOrderAuth>]`, CanSave will be generated on the factory interface. Confirmed -- no issue.

#### Concern 5: `IsNew` Default Value

The plan shows `bool IsNew { get; set; } = true;` for AuthorizedOrder. This means a freshly Created entity has `IsNew=true`, and after Save(Insert), `IsNew` should become `false`. However, the plan's Create method doesn't explicitly set `IsNew = true` (it's the default). The Insert method should set `IsNew = false` after execution, OR the entity should implement `IFactoryOnCompleteAsync` to set `IsNew = false` after Insert (like Order.cs does at line 170-174). The plan's pseudocode at line 230-234 doesn't show this. The developer needs to either: (a) implement `IFactoryOnCompleteAsync` to reset `IsNew` after Insert, or (b) set `IsNew = false` directly in the Insert method body.

**Recommendation:** Follow the Order.cs pattern -- implement `IFactoryOnCompleteAsync` or set `IsNew = false` in the Insert method body. Test Scenario 10 expects `result.Result.IsNew == false` after TrySave with Insert, so this MUST be handled. This is not a design flaw -- the developer will see the pattern from Order.cs and implement it correctly.

---

## Implementation Contract

**Created:** 2026-03-08
**Approved by:** developer (Claude Opus 4.6)

### Verification Acceptance Criteria

- [ ] `dotnet build src/Design/Design.sln` -- 0 errors, 0 warnings
- [ ] `dotnet test src/Design/Design.sln` -- 41+ tests pass (29 existing + 12 new), 0 failures

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| 1 | `AuthorizationTests.AuthorizedOrder_CanCreate_ReturnsTrue_WhenAllowed()` | Must be async Task, use await |
| 2 | `AuthorizationTests.AuthorizedOrder_CanFetch_ReturnsFalse_WhenDenied()` | Must be async Task, use await |
| 3 | `AuthorizationTests.AuthorizedOrder_CanSave_ReturnsFalse_WhenDeleteDenied()` | Must be async Task, use await |
| 4 | `AuthorizationTests.AuthorizedOrder_CanSave_ReturnsTrue_WhenAllWriteAllowed()` | Must be async Task, use await |
| 5 | `AuthorizationTests.AuthorizedOrder_Create_ReturnsNull_WhenDenied()` | async Task |
| 6 | `AuthorizationTests.AuthorizedOrder_Fetch_ReturnsNull_WhenDenied()` | async Task |
| 7 | `AuthorizationTests.AuthorizedOrder_Create_ReturnsInstance_WhenAllowed()` | async Task |
| 8 | `AuthorizationTests.AuthorizedOrder_Save_ThrowsNotAuthorizedException_WhenDeleteDenied()` | async Task, use Assert.ThrowsAsync |
| 9 | `AuthorizationTests.AuthorizedOrder_TrySave_ReturnsFalse_WhenDeleteDenied()` | async Task |
| 10 | `AuthorizationTests.AuthorizedOrder_TrySave_ReturnsTrue_WhenInsertAllowed()` | async Task; AuthorizedOrder must set IsNew=false after Insert (see Concern 5) |
| 11 | `AuthorizationTests.AuthorizedOrder_Create_WorksThroughClientServer()` | async Task |
| 12 | `AuthorizationTests.AuthorizedOrder_Fetch_ReturnsNull_ThroughClientServer()` | async Task |

### Developer Notes from Review

1. **All Can* tests must be async** -- AuthorizedOrder uses `[Remote] internal`, so generated Can* methods return `Task<Authorized>`. All test methods must be `async Task` and use `await`.
2. **IsNew must be reset after Insert** -- Test Scenario 10 expects `result.Result.IsNew == false`. AuthorizedOrder must either implement `IFactoryOnCompleteAsync` (like Order.cs) or set `IsNew = false` in the Insert method body.
3. **Reset static flags at start of each test** -- Not just in the constructor. Each test method should explicitly set the flags it depends on to avoid test ordering issues.
4. **CanSave aggregation** -- CanSave's auth methods are `{HasAccess(), CanDelete()}` (the union of distinct auth methods from Insert, Update, Delete). `CanCreate()` and `CanFetch()` are NOT part of CanSave (their scopes don't match write operations). This is correct.

### In Scope

- [ ] `src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs` -- NEW: auth interface and implementation
- [ ] `src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs` -- NEW: entity with `[AuthorizeFactory<T>]`
- [ ] `src/Design/Design.Domain/Aggregates/SecureOrder.cs` -- MODIFY: add cross-reference comment
- [ ] `src/Design/Design.Tests/FactoryTests/AuthorizationTests.cs` -- NEW: 12 authorization tests
- [ ] `src/Design/CLAUDE-DESIGN.md` -- MODIFY: checklist item, Design Files table
- [ ] Checkpoint: Build + test after entity creation (Phase 1)
- [ ] Checkpoint: Build + test after all tests added (Phase 2)
- [ ] Final: Build + test after CLAUDE-DESIGN.md updates (Phase 3)

### Out of Scope

- Remote auth interface (`[Remote]` on auth methods) -- deferred to future todo
- Unit test or integration test changes -- existing coverage is sufficient
- SecureOrder.cs structural changes -- it continues as `[AspAuthorize]` demonstration
- Skill file updates -- left for documentation step (Step 9)
- `docs/authorization.md` updates -- left for documentation step (Step 9)

### Verification Gates

1. After Phase 1: `dotnet build src/Design/Design.sln` succeeds
2. After Phase 2: `dotnet test src/Design/Design.sln` passes all tests (29 existing + 12 new)
3. Final: Full build and test pass, all acceptance criteria met

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure (any of the 29 existing tests start failing)
- Architectural contradiction discovered (e.g., CanSave aggregation doesn't work as expected)
- Auth type not auto-discovered by RegisterMatchingName

---

## Implementation Progress

**Started:** 2026-03-08
**Developer:** Claude Opus 4.6 (developer agent)

**Phase 1: Entity + Auth Files**
- [x] Create AuthorizedOrderAuth.cs -- created with IAuthorizedOrderAuth interface (4 auth methods with Read|Write broad scope + Create/Fetch/Delete fine-grained) and AuthorizedOrderAuth public implementation with static boolean flags and ResetFlags() helper
- [x] Create AuthorizedOrder.cs -- created with [Factory], [AuthorizeFactory<IAuthorizedOrderAuth>], internal partial class, IAuthorizedOrder public interface (with settable IsNew/IsDeleted via `new` keyword), IFactoryOnCompleteAsync for IsNew reset after Insert, all 5 CRUD operations as [Remote] internal
- [x] Update SecureOrder.cs comments -- added cross-reference: "For domain-specific authorization with [AuthorizeFactory<T>], see AuthorizedOrder.cs."
- [x] **Verification**: `dotnet build src/Design/Design.sln` succeeded with 0 errors, 0 warnings

**Phase 2: Tests**
- [x] Create AuthorizationTests.cs with all 12 test scenarios -- all scenarios implemented and passing
- [x] **Verification**: `dotnet test src/Design/Design.sln` -- 41 passed (29 existing + 12 new), 0 failed on both net9.0 and net10.0

**Phase 3: CLAUDE-DESIGN.md Updates**
- [x] Add [AuthorizeFactory<T>] checklist item -- added as checked item: "Custom domain authorization with [AuthorizeFactory<T>] (AuthorizedOrder.cs, AuthorizedOrderAuth.cs)"
- [x] Update Design Files table -- added AuthorizedOrder.cs and AuthorizedOrderAuth.cs entries
- [x] **Verification**: `dotnet test src/Design/Design.sln` -- 41 passed, 0 failed on both net9.0 and net10.0

**Implementation Notes:**

1. **Can* methods are synchronous** -- the developer review (Concern 1) predicted Can* would return `Task<Authorized>` due to [Remote] internal source methods. The generated code actually returns `Authorized` (synchronous). Can* tests are synchronous void methods, not async Task.

2. **AuthorizedOrderAuth made public** -- the plan specified `internal class AuthorizedOrderAuth` following the PersonModelAuth pattern. However, since Design.Tests is a separate project from Design.Domain and there is no InternalsVisibleTo, the static flags were inaccessible. Changed to `public class` with a comment explaining this is for test configurability (production code should use internal with constructor-injected services).

3. **IAuthorizedOrder needs settable IsNew/IsDeleted** -- IFactorySaveMeta only has getters. Added `new bool IsNew { get; set; }` and `new bool IsDeleted { get; set; }` to IAuthorizedOrder, following the ShowcaseAuthObj pattern (IShowcaseAuthObj does the same).

4. **CanSave aggregation confirmed** -- generated LocalCanSave checks HasAccess() three times (once per write method: Insert, Update, Delete) plus CanDelete() once. The distinct deduplication noted in the assertion trace didn't fully deduplicate HasAccess(), but behavior is correct: if HasAccess() returns true, all three checks pass; if false, the first one short-circuits.

---

## Completion Evidence

**Reported:** 2026-03-08

- **Tests Passing:**
  - Design.Tests: 41 passed, 0 failed on net9.0 and net10.0 (29 existing + 12 new)
  - RemoteFactory.UnitTests: 481 passed, 0 failed on net9.0 and net10.0
  - RemoteFactory.IntegrationTests: 476 passed, 3 skipped (pre-existing), 0 failed on net9.0 and net10.0
  - Full solution build: 0 errors, 3 pre-existing WASM0001 warnings (OrderEntry.BlazorClient, SQLite-related)

- **Verification Resources Pass:** Yes -- Design.sln is the verification resource and all 41 tests pass

- **All Contract Items:** Confirmed 100% complete:
  - [x] `src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs` -- NEW
  - [x] `src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs` -- NEW
  - [x] `src/Design/Design.Domain/Aggregates/SecureOrder.cs` -- MODIFIED (cross-reference comment)
  - [x] `src/Design/Design.Tests/FactoryTests/AuthorizationTests.cs` -- NEW (12 tests)
  - [x] `src/Design/CLAUDE-DESIGN.md` -- MODIFIED (checklist item + Design Files table)
  - [x] Checkpoint Phase 1: Build succeeded
  - [x] Checkpoint Phase 2: 41 tests passed
  - [x] Final: Full solution build and test passed

- **Acceptance Criteria Met:**
  - [x] AuthorizedOrder.cs exists with [Factory], [AuthorizeFactory<IAuthorizedOrderAuth>], internal partial class, IAuthorizedOrder public interface
  - [x] AuthorizedOrderAuth.cs exists with IAuthorizedOrderAuth (broad-scope + fine-grained) and AuthorizedOrderAuth with static flags
  - [x] AuthorizationTests.cs exists with all 12 test scenarios passing
  - [x] SecureOrder.cs has cross-reference comment to AuthorizedOrder.cs
  - [x] CLAUDE-DESIGN.md checklist has [AuthorizeFactory<T>] item (checked)
  - [x] CLAUDE-DESIGN.md Design Files table includes AuthorizedOrder.cs and AuthorizedOrderAuth.cs
  - [x] dotnet build src/Design/Design.sln succeeds (0 warnings from Design projects)
  - [x] dotnet test src/Design/Design.sln passes all tests (29 existing + 12 new = 41)
  - [x] All existing 29 tests continue to pass

---

## Documentation

**Agent:** Business Requirements Documenter
**Completed:** 2026-03-08

### Expected Deliverables

- [x] `src/Design/CLAUDE-DESIGN.md` -- Updated checklist and Design Files table (completed in Phase 3), plus Quick Decisions Table entry for authorization approach added by documenter
- [x] Skill updates: Evaluated `skills/RemoteFactory/references/advanced-patterns.md` -- **no updates needed**. The skill already has thorough `[AuthorizeFactory<T>]` coverage using MarkdownSnippets from the reference app (`authorization-interface`, `authorization-implementation`, `authorization-apply` snippets). The Design project adds source-of-truth demonstration, which is separate from the skill's purpose.
- [x] Sample updates: N/A (Design project IS the source of truth)
- [x] `docs/authorization.md`: Evaluated -- **no updates needed**. The published docs already document all the behaviors that the Design project now demonstrates (operation flags, auth failure behaviors, Can* methods, combined flags). No cross-reference from docs to Design source code is needed; the docs stand on their own as user-facing documentation.

### Developer Deliverables

No source code changes needed. All implementation is complete and the documentation layer (CLAUDE-DESIGN.md, published docs, skill files) is current.

### Files Updated

1. **`src/Design/CLAUDE-DESIGN.md`** -- Added authorization approach entry to Quick Decisions Table: "Which authorization approach? [AuthorizeFactory<T>] for domain-specific rules; [AspAuthorize] for ASP.NET Core policies." References `AuthorizedOrder.cs` and `SecureOrder.cs`. (Phase 3 already added the checklist item and Design Files table entries.)

### Status: Requirements Documented

---

## Architect Verification

**Verified:** 2026-03-08
**Verdict:** VERIFIED

**Independent test results:**
- Design.sln build: 0 errors, 0 warnings
- Neatoo.RemoteFactory.sln build: 0 errors, 3 pre-existing WASM0001 warnings (OrderEntry.BlazorClient, SQLite-related)
- Design.Tests: 41 passed, 0 failed (net9.0: 41 passed; net10.0: 41 passed) -- 29 existing + 12 new
- RemoteFactory.UnitTests: 481 passed, 0 failed (net9.0: 481; net10.0: 481)
- RemoteFactory.IntegrationTests: 476 passed, 3 skipped (pre-existing ShowcasePerformanceTests), 0 failed (net9.0: 476+3; net10.0: 476+3)

**Design match:** Implementation matches the original plan. All 16 business rules are covered:

| Rule # | Description | Verified |
|--------|-------------|----------|
| 1 | `[AuthorizeFactory<IAuthorizedOrderAuth>]` produces Can* methods on factory interface | Yes -- AuthorizedOrder.cs line 94, tests resolve IAuthorizedOrderFactory and call CanCreate/CanFetch/CanSave/CanDelete |
| 2 | `Read \| Write` scope on HasAccess() covers all operations | Yes -- IAuthorizedOrderAuth line 70, tests confirm HasAccess is consulted for Create, Fetch, and Save |
| 3 | CanCreate returns true when allowed | Yes -- Test `AuthorizedOrder_CanCreate_ReturnsTrue_WhenAllowed` passes |
| 4 | CanFetch returns false when denied | Yes -- Test `AuthorizedOrder_CanFetch_ReturnsFalse_WhenDenied` passes |
| 5 | CanSave returns false when Delete denied | Yes -- Test `AuthorizedOrder_CanSave_ReturnsFalse_WhenDeleteDenied` passes |
| 6 | CanSave returns true when all write checks pass | Yes -- Test `AuthorizedOrder_CanSave_ReturnsTrue_WhenAllWriteAllowed` passes |
| 7 | Create returns null when denied | Yes -- Test `AuthorizedOrder_Create_ReturnsNull_WhenDenied` passes |
| 8 | Fetch returns null when denied | Yes -- Test `AuthorizedOrder_Fetch_ReturnsNull_WhenDenied` passes |
| 9 | Create returns instance when allowed | Yes -- Test `AuthorizedOrder_Create_ReturnsInstance_WhenAllowed` passes |
| 10 | Save throws NotAuthorizedException when denied | Yes -- Test `AuthorizedOrder_Save_ThrowsNotAuthorizedException_WhenDeleteDenied` passes |
| 11 | TrySave returns HasAccess=false when denied | Yes -- Test `AuthorizedOrder_TrySave_ReturnsFalse_WhenDeleteDenied` passes |
| 12 | TrySave returns HasAccess=true when allowed | Yes -- Test `AuthorizedOrder_TrySave_ReturnsTrue_WhenInsertAllowed` passes, including IsNew=false after Insert |
| 13 | Can* promoted to public, IsServerRuntime guard | Yes -- Structural verification: AuthorizedOrder uses `[Remote] internal`, Can* methods work in tests |
| 14 | Auth type auto-registration | Yes -- Implicit: tests resolve factory and auth checks work without manual registration |
| 15 | Factory interface uses IAuthorizedOrder | Yes -- Tests use `IAuthorizedOrderFactory`, operations return `IAuthorizedOrder` |
| 16 | CLAUDE-DESIGN.md checklist updated | Yes -- Line 628: `- [x] Custom domain authorization with [AuthorizeFactory<T>]` |

All 12 test scenarios map to passing tests:

| Scenario | Test Method | Status |
|----------|-------------|--------|
| 1 | AuthorizedOrder_CanCreate_ReturnsTrue_WhenAllowed | Passed |
| 2 | AuthorizedOrder_CanFetch_ReturnsFalse_WhenDenied | Passed |
| 3 | AuthorizedOrder_CanSave_ReturnsFalse_WhenDeleteDenied | Passed |
| 4 | AuthorizedOrder_CanSave_ReturnsTrue_WhenAllWriteAllowed | Passed |
| 5 | AuthorizedOrder_Create_ReturnsNull_WhenDenied | Passed |
| 6 | AuthorizedOrder_Fetch_ReturnsNull_WhenDenied | Passed |
| 7 | AuthorizedOrder_Create_ReturnsInstance_WhenAllowed | Passed |
| 8 | AuthorizedOrder_Save_ThrowsNotAuthorizedException_WhenDeleteDenied | Passed |
| 9 | AuthorizedOrder_TrySave_ReturnsFalse_WhenDeleteDenied | Passed |
| 10 | AuthorizedOrder_TrySave_ReturnsTrue_WhenInsertAllowed | Passed |
| 11 | AuthorizedOrder_Create_WorksThroughClientServer | Passed |
| 12 | AuthorizedOrder_Fetch_ReturnsNull_ThroughClientServer | Passed |

**File verification:**
- `src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs` -- NEW: `[Factory]`, `[AuthorizeFactory<IAuthorizedOrderAuth>]`, `internal partial class`, `IAuthorizedOrder` public interface, `IFactorySaveMeta`, `IFactoryOnCompleteAsync`, all 5 CRUD operations as `[Remote] internal`, comprehensive Design Source of Truth comments
- `src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs` -- NEW: `IAuthorizedOrderAuth` with `Read|Write` broad scope + `Create`/`Fetch`/`Delete` fine-grained ops, `AuthorizedOrderAuth` implementation with static flags and `ResetFlags()`, comprehensive comments including DID NOT DO THIS for remote auth
- `src/Design/Design.Tests/FactoryTests/AuthorizationTests.cs` -- NEW: 12 tests, all with `ResetFlags()` at start, Design Source of Truth comments
- `src/Design/Design.Domain/Aggregates/SecureOrder.cs` -- MODIFIED: Cross-reference comment at line 9
- `src/Design/CLAUDE-DESIGN.md` -- MODIFIED: Checklist item (line 628), Design Files table entries (lines 666-667)

**Implementation deviations from plan (noted in Implementation Notes, all acceptable):**
1. Can* methods are synchronous (not async as developer review predicted) -- the generator makes Can* synchronous regardless of source method async status. Tests correctly use synchronous assertions.
2. `AuthorizedOrderAuth` is `public` instead of `internal` -- necessary because Design.Tests is a separate assembly without InternalsVisibleTo. Documented with a comment explaining production code should use internal.
3. `IAuthorizedOrder` has `new bool IsNew { get; set; }` and `new bool IsDeleted { get; set; }` -- follows ShowcaseAuthObj pattern to make setters accessible through the interface for test setup.

**Issues found:** None

---

## Requirements Verification

**Reviewer:** business-requirements-reviewer
**Verified:** 2026-03-08
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|
| R1: AuthorizeFactory<T> attribute contract | Satisfied | `AuthorizedOrder.cs:94` applies `[AuthorizeFactory<IAuthorizedOrderAuth>]` to `internal partial class AuthorizedOrder`. The generic type `T` is an interface (`IAuthorizedOrderAuth`), matching the `AttributeTargets.Class` usage defined in `src/RemoteFactory/FactoryAttributes.cs:113-114`. Tests resolve `IAuthorizedOrderFactory` and call CanCreate/CanFetch/CanSave/CanDelete, confirming the generator produced the expected factory interface. |
| R2: Auth method return types | Satisfied | `AuthorizedOrderAuth.cs:71,77,83,89` -- all four auth methods (`HasAccess()`, `CanCreate()`, `CanFetch()`, `CanDelete()`) return `bool`, which is one of the four valid return types (`bool`, `Task<bool>`, `string?`, `Task<string?>`). Each method has a `[AuthorizeFactory(AuthorizeFactoryOperation.Xxx)]` attribute with correct operation flags. No NF0202 diagnostic would be emitted. |
| R3: Can* guard behavior | Satisfied | `AuthorizedOrder.cs` uses `[Remote] internal` on all five CRUD methods. Per the documented rule (`CLAUDE-DESIGN.md:396-443`), Can* methods for `[Remote] internal` parent methods get the `IsServerRuntime` guard. The comment block at `AuthorizedOrder.cs:32-37` explicitly documents this promotion behavior. Tests in `AuthorizationTests.cs` verify the Can* methods work correctly in local mode (scenarios 1-4) and through client-server round-trip (scenarios 11-12). Implementation Note 1 correctly documents that Can* methods are synchronous (return `Authorized`, not `Task<Authorized>`). |
| R4: [Remote] requires internal | Satisfied | All five factory methods in `AuthorizedOrder.cs:139-176` use `[Remote]` with `internal` visibility: `[Remote, Create] internal void Create(...)`, `[Remote, Fetch] internal void Fetch(...)`, `[Remote, Insert] internal Task Insert()`, `[Remote, Update] internal Task Update()`, `[Remote, Delete] internal Task Delete()`. No `[Remote] public` combinations exist. This follows Anti-Pattern 8 (`CLAUDE-DESIGN.md:349-371`). |
| R5: Auth type auto-registration | Satisfied | The generator auto-registers auth types via naming convention `IAuthorizedOrderAuth` -> `AuthorizedOrderAuth`. Tests confirm this works: `AuthorizationTests.cs` resolves `IAuthorizedOrderFactory` from both local and client containers without explicit manual registration of the auth type. `DesignClientServerContainers.cs:231` calls `RegisterMatchingName` which handles the interface-to-class mapping. Comment at `AuthorizedOrderAuth.cs:35-39` documents the auto-registration behavior for trimming. |
| R6: Auth failure behavior | Satisfied | All three failure modes documented in `docs/authorization.md:247-248` are tested: (1) Create returns null when denied -- `AuthorizationTests.cs:158-173` (scenario 5); (2) Fetch returns null when denied -- `AuthorizationTests.cs:180-195` (scenario 6); (3) Save throws `NotAuthorizedException` when denied -- `AuthorizationTests.cs:240-261` (scenario 8). Additionally, TrySave catches `NotAuthorizedException` and returns `Authorized<T>` with `HasAccess=false` -- `AuthorizationTests.cs:269-293` (scenario 9). Comment block at `AuthorizedOrder.cs:51-55` documents these behaviors. |
| R7: Operation flags | Satisfied | `AuthorizedOrderAuth.cs:70-89` demonstrates both broad-scope and fine-grained operation flags: `HasAccess()` uses `AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write` (covers all CRUD operations), while `CanCreate()`, `CanFetch()`, and `CanDelete()` use individual operation flags (`Create`, `Fetch`, `Delete`). The bitwise matching is correct: `Read|Write = 64|128 = 192`, which matches Create (includes Read=64), Fetch (includes Read=64), Insert/Update/Delete (include Write=128). Comment block at `AuthorizedOrderAuth.cs:14-28` explains the design decision for both patterns and why `CanInsert()`/`CanUpdate()` are intentionally omitted. |
| R9: Class visibility pattern | Satisfied | `AuthorizedOrder.cs:95` declares `internal partial class AuthorizedOrder : IAuthorizedOrder, IFactorySaveMeta, IFactoryOnCompleteAsync`. The public interface `IAuthorizedOrder` at line 67 extends `IFactorySaveMeta` and exposes settable properties including `new bool IsNew { get; set; }` and `new bool IsDeleted { get; set; }` (following the `ShowcaseAuthObj` pattern from integration tests). This matches the canonical `internal class + public interface` pattern from `Order.cs`. The comment at `AuthorizedOrder.cs:80-84` documents this decision. |
| R10: Checklist update | Satisfied | `CLAUDE-DESIGN.md:628` has `- [x] Custom domain authorization with [AuthorizeFactory<T>] (AuthorizedOrder.cs, AuthorizedOrderAuth.cs)` as a checked item in the Design Completeness Checklist. Additionally, `CLAUDE-DESIGN.md:666-667` adds both files to the Design Files table with accurate descriptions. |

### Gap Resolution

| Gap | Status | Evidence |
|-----|--------|----------|
| GAP-1: No Design source-of-truth for [AuthorizeFactory<T>] | Resolved | `AuthorizedOrder.cs` and `AuthorizedOrderAuth.cs` are now the Design source of truth with comprehensive `DESIGN SOURCE OF TRUTH` headers, `DESIGN DECISION`, `GENERATOR BEHAVIOR`, and `DID NOT DO THIS` comment blocks. |
| GAP-2: No Design.Tests for Can* methods | Resolved | `AuthorizationTests.cs` contains 12 test scenarios covering CanCreate, CanFetch, CanSave, CanDelete, and their interactions with authorization allow/deny states. |
| GAP-3: No Design source-of-truth for remote auth interface | Resolved (Deferred) | Correctly deferred to a future todo per Recommendation 8. `AuthorizedOrderAuth.cs:48-53` includes a `DID NOT DO THIS` comment block explaining the deferral and pointing to `ShowcaseAuthRemoteTests.cs` for that pattern. |
| GAP-4: CLAUDE-DESIGN.md Design Completeness Checklist missing [AuthorizeFactory<T>] item | Resolved | `CLAUDE-DESIGN.md:628` has the checked item. |
| GAP-5: No Design demonstration of CanSave/TrySave with auth | Resolved | `AuthorizationTests.cs` scenarios 3-4 test CanSave (both allowed and denied). Scenarios 8-10 test Save (throws `NotAuthorizedException`) and TrySave (returns `Authorized<T>` with appropriate `HasAccess` and `Result` values). |

### Unintended Side Effects

**Verified: No unintended side effects found.**

1. **Existing Design tests unaffected:** All 29 pre-existing tests continue to pass (confirmed by architect verification: 41 total = 29 existing + 12 new).
2. **No generated code pattern changes:** The implementation adds new entity files only; no modifications to generator, core library, or existing generated code patterns.
3. **No serialization contract changes:** `AuthorizedOrder` follows the same `IFactorySaveMeta` + `IFactoryOnCompleteAsync` pattern as `Order.cs`. No new serialization contracts introduced.
4. **SecureOrder.cs unchanged structurally:** Only a single cross-reference comment was added at line 9 (`For domain-specific authorization with [AuthorizeFactory<T>], see AuthorizedOrder.cs.`). The `[AspAuthorize]` demonstration remains intact.
5. **Published docs accuracy:** The implementation is consistent with `docs/authorization.md` documentation -- failure behaviors (null for Read, exception for Write), Can* client-side behavior, and operation flag semantics all match.
6. **No design debt conflicts:** The Design Debt table (`CLAUDE-DESIGN.md:632-643`) has no entry that this implementation conflicts with. The only authorization-related debt item (OR logic for `[AspAuthorize]`) is unrelated.
7. **Full solution tests unaffected:** Architect verification confirmed RemoteFactory.UnitTests (481 passed), RemoteFactory.IntegrationTests (476 passed, 3 pre-existing skips), and Design.Tests (41 passed) all pass on both net9.0 and net10.0.

### Acceptable Deviations from Plan

Three deviations were noted in Implementation Notes (plan lines 610-616), all reviewed and found acceptable:

1. **Can* methods are synchronous** (not async as predicted in Developer Review Concern 1): The generator makes Can* methods synchronous regardless of source method async status. This is correct generator behavior documented in the test comments at `AuthorizationTests.cs:48-52`.
2. **AuthorizedOrderAuth is `public`** (plan specified `internal`): Necessary because `Design.Tests` is a separate project without `InternalsVisibleTo`. The class has a comment at lines 96-101 explaining this is for test configurability and production code should use `internal`.
3. **IAuthorizedOrder has `new` keyword for IsNew/IsDeleted setters**: Follows the `ShowcaseAuthObj` pattern (`IShowcaseAuthObj` uses the same `new` keyword approach). Required because `IFactorySaveMeta` only defines getters, and tests need to set these properties through the interface.

### Issues Found

None.
