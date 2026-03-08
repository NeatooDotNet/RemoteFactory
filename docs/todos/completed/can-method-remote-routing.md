# Can* Method Behavior Should Derive from Auth Class, Not Factory Method

**Status:** Complete
**Priority:** High
**Created:** 2026-03-08
**Last Updated:** 2026-03-08


---

## Problem

Currently, generated `Can*` methods inherit their remote/guard behavior from the parent factory method. When a factory method is `[Remote] internal`, its `CanDelete()` gets an `IsServerRuntime` guard that throws "Server-only method called in non-server runtime" on the client. This breaks client-side authorization checks in Blazor WASM — the Person example fails when selecting any role because `CanFetch()`, `CanUpsert()`, and `CanDelete()` all throw.

But `Can*` methods don't call the factory method — they call the **auth class** methods. The factory method's `[Remote]` status is irrelevant to whether the auth check can run. The auth class should determine Can* behavior using the same accessibility paradigm as everything else in RemoteFactory:

- `public` auth methods → Can* runs on client (no guard, no remote call)
- `internal` auth methods → server-only (guarded, trimmed on client)
- `[Remote] internal` auth methods → Can* routes to server via remote delegate

**Discovered references:**
- `src/Examples/Person/Person.Client/Pages/Home.razor:217-220` — calls `CanCreate()`, `CanFetch()`, `CanUpsert()`, `CanDelete()` on role change
- `src/Examples/Person/Person.DomainModel/PersonModel.cs` — `[Remote] internal` on Fetch, Upsert, Delete methods
- `src/Examples/Person/Person.DomainModel/PersonModelAuth.cs` — auth class with `public` methods and no server-only dependencies
- `src/Generator/Renderer/ClassFactoryRenderer.cs` — generates the "Server-only method called" guard for Can* methods
- `src/Generator/Renderer/InterfaceFactoryRenderer.cs:257` — same guard in interface factory renderer
- `src/Generator/Model/Methods/CanMethodModel.cs` — Can* method model
- `src/Generator/Builder/FactoryModelBuilder.cs` — builds Can* methods
- `src/Tests/RemoteFactory.IntegrationTests/Showcase/ShowcaseAuthRemoteTests.cs` — existing remote auth tests
- Related work: [authorize-factory-design-coverage](completed/authorize-factory-design-coverage.md) — just-completed Design project coverage (may need updating)

## Solution

Decouple Can* method behavior from the factory method. Instead, derive Can* remote/trim/guard behavior from the auth class and its methods, following the same accessibility paradigm used everywhere else in RemoteFactory. The auth class is `public` with `public` methods? Can* runs on client. Auth methods are `[Remote] internal`? Can* routes to server. This is consistent and predictable.

---

## Clarifications

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-08
**Verdict:** APPROVED (with documented contradictions that are intentional corrections)

### Relevant Requirements Found

**R1. Current Can* visibility rule** (`src/Design/CLAUDE-DESIGN.md:396-408`, `docs/authorization.md:230-243`): The current documented rule states: "Can* methods inherit guard behavior from their parent factory method. Public parent method => no guard. Internal parent method => guard." This rule was established in the `internal-factory-visibility` todo and is verified by `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Visibility/CanMethodVisibilityTests.cs`. The todo proposes **changing** this rule; see Contradictions section.

**R2. Documented rule in docs/authorization.md:232-243**: Two explicit statements: (1) "Can* methods generated from public factory methods run locally on the client without a server round-trip." (2) "Can* methods generated from internal factory methods retain the IsServerRuntime guard and are not callable on the client. This is correct because internal factory methods represent server-only operations." These statements document the current behavior that this todo proposes to change.

**R3. AuthorizedOrder.cs Design source-of-truth comment** (`src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs:29-37`): The comments explicitly state: "Can* methods inherit the [Remote] promotion and IsServerRuntime guard" and "[GENERATOR BEHAVIOR]: Because the source methods are [Remote] internal, the generated Can* methods are promoted to public on the factory interface. The underlying LocalCan* methods have an IsServerRuntime guard, so authorization checks execute on the server where the auth implementation has access to server-only services."

**R4. AuthorizedOrderAuth.cs "DID NOT DO THIS" comment** (`src/Design/Design.Domain/Aggregates/AuthorizedOrderAuth.cs:48-54`): "DID NOT DO THIS: [Remote] on auth interface methods. Auth interface methods can have [Remote], making the auth check itself execute on the server with server-only service dependencies. This is an advanced pattern deferred to a future todo. See ShowcaseAuthRemoteTests.cs for that pattern." This establishes that auth methods CAN have [Remote], which aligns with the proposed accessibility paradigm for Can* methods.

**R5. CanMethodModel tracks source method state** (`src/Generator/Model/Methods/CanMethodModel.cs:17`): `IsSourceMethodRemote` tracks whether the source factory method has `[Remote]`. This is used in the renderer (`src/Generator/Renderer/ClassFactoryRenderer.cs:121`) for interface visibility promotion: `var isPromotedByRemote = method.IsRemote || (method is CanMethodModel cm && cm.IsSourceMethodRemote)`. The todo will need to change how this property is derived.

**R6. FactoryModelBuilder.AddCanMethods()** (`src/Generator/Builder/FactoryModelBuilder.cs:790-830`): Line 822-823 passes `isInternal: method.IsInternal` and `isSourceMethodRemote: method.IsRemote` from the parent factory method to `BuildCanMethod()`. This is the specific code that implements the current "inherit from parent" behavior.

**R7. Guard emission in ClassFactoryRenderer.RenderCanLocalMethod()** (`src/Generator/Renderer/ClassFactoryRenderer.cs:1296-1306`): The guard is emitted when `method.IsInternal || method.IsRemote`. Currently Can* gets `IsInternal` from the factory method. The proposal would derive this from the auth class/methods instead.

**R8. [Remote] requires internal** (`src/Design/CLAUDE-DESIGN.md:141`, Anti-Pattern 8): `[Remote]` requires `internal` visibility. `[Remote] public` emits NF0105. This rule applies to factory methods. The todo does NOT change this rule. However, the same accessibility paradigm for auth methods uses the same pattern: `[Remote] internal` on auth methods would route Can* to server.

**R9. ShowcaseAuthRemoteTests pattern** (`src/Tests/RemoteFactory.IntegrationTests/Showcase/ShowcaseAuthRemoteTests.cs:13-34`): Auth interface `IAuthRemote` has `[Remote] [AuthorizeFactory(AuthorizeFactoryOperation.Create)] bool Create()`. The auth implementation `AuthServerOnly` has a `[Service] IServerOnlyService` constructor dependency. Tests verify that `CanCreate()` works through the client-server boundary. This is the existing proof-of-concept for auth-method-driven Can* routing.

**R10. Person example auth class** (`src/Examples/Person/Person.DomainModel/PersonModelAuth.cs`): `IPersonModelAuth` has all `public` methods (no `[Remote]`). The implementation `PersonModelAuth` takes `IUser` via constructor injection. `IUser` is registered on both client and server. Under the proposed change, all Can* methods for PersonModel would run on the client with no guard, fixing the reported bug.

**R11. Design Debt table** (`src/Design/CLAUDE-DESIGN.md:636-644`): No entry prohibits changing Can* derivation from factory method to auth class. The only authorization-related debt item is "OR logic for [AspAuthorize]" which is unrelated.

**R12. CanMethodVisibilityTests** (`src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Visibility/CanMethodVisibilityTests.cs`): Two test classes: `PublicMethodWithAuth` (public Create method) and `InternalMethodWithAuth` (internal Create method). Both use `VisibilityTestAuth` which has `public` auth methods. Under the proposed change: both Can* methods would have NO guard (because the auth methods are public). The test at line 148-188 (`InternalMethod_CanCreate_GeneratedCode_HasGuard`) explicitly asserts that `LocalCanCreate` for an internal factory method HAS the guard. **This test will fail** under the proposed change because the auth method is `public`, so no guard would be emitted. This test must be updated.

**R13. CanMethodVisibilityTargets** (`src/Tests/RemoteFactory.UnitTests/TestTargets/Visibility/CanMethodVisibilityTargets.cs:37-51`): `InternalMethodWithAuth` has `internal void Create()` with `[AuthorizeFactory<VisibilityTestAuth>]` where `VisibilityTestAuth` has `public bool CanAccess()`. The class comment says: "CanCreate should HAVE IsServerRuntime guard (internal method => guard on Can)." Under the proposed change, this comment and the test would need updating because the auth method is `public`.

**R14. AuthorizedOrder Design tests** (`src/Design/Design.Tests/FactoryTests/AuthorizationTests.cs:60-72`): Tests call `factory.CanCreate()`, `factory.CanFetch()`, etc. in `local` mode (server-side, `IsServerRuntime=true`). These tests will continue to pass regardless of whether the guard is present, because the guard only blocks when `IsServerRuntime=false`. However, the test comments at lines 48-52 say "Can* methods return Authorized directly (not Task<Authorized>)." Under the proposed change, if auth methods remain `public` (no `[Remote]`), Can* methods will remain synchronous. No test changes needed here.

**R15. v0.17.0 release notes** (`docs/release-notes/v0.17.0.md:77`): "Previously, ALL Local* methods had IsServerRuntime guards. Now only internal and [Remote] methods get guards. public non-[Remote] methods (like Can* and Create) no longer have guards and execute locally on the client." This documents the current behavior. Release notes for this change will need to describe the new derivation rule.

**R16. Breaking change impact**: The todo correctly identifies this as a BREAKING CHANGE. Any code that depends on Can* methods being server-only because the factory method is `[Remote] internal` will now have client-callable Can* methods if the auth class has `public` methods. However, the current behavior is ALSO broken (Person example throws on client). The "correct" behavior depends on whether auth checks need server-only services.

**R17. AspAuthorize and Can* methods**: `[AspAuthorize]` on factory methods also contributes to Can* method behavior via `method.Authorization?.AspAuthorize`. Under the current code, if a method has `[AspAuthorize]` (no `[AuthorizeFactory]`), no Can* method is generated (Can* is only generated when `method.HasAuth` is true, which requires auth methods). `[AspAuthorize]` does not produce Can* methods for class factories. This is unaffected by the proposed change.

### Gaps

**GAP-1. No documented rule for auth-class-driven Can* behavior**: The proposed "derive Can* from auth class accessibility" paradigm is new. No existing documentation describes this pattern. The architect must establish new rules for: (a) what constitutes a "public" vs "internal" auth method for Can* purposes, (b) how mixed accessibility in auth methods (some public, some [Remote] internal) maps to Can* behavior per operation, (c) how the accessibility of the auth CLASS (not just methods) factors in.

**GAP-2. No test coverage for mixed auth method accessibility**: The `ShowcaseAuthRemoteTests` demonstrates `[Remote]` on auth methods (all methods remote). The `CanMethodVisibilityTests` demonstrate all-public auth methods. No test covers a mixed scenario: e.g., `CanCreate` with a `public` auth method and `CanFetch` with a `[Remote] internal` auth method on the same entity.

**GAP-3. Interface promotion logic for Can* methods**: Currently `IsSourceMethodRemote` drives whether Can* methods are promoted to `public` on the factory interface. Under the proposed change, this promotion should be driven by the auth method's accessibility instead. If an auth method is `[Remote] internal`, the Can* method should be promoted to public on the interface (because it routes to server). If the auth method is `public`, the Can* method should match the auth method's visibility for interface purposes.

**GAP-4. Interaction with CanSave aggregation**: `CanSave` aggregates auth methods from Insert, Update, and Delete. If these auth methods have different accessibility (e.g., HasAccess is public, CanDelete is [Remote] internal), the CanSave method's own guard/remote behavior needs a clear rule. Does it take the "most restrictive" auth method's behavior?

### Contradictions

**CONTRADICTION-1. Direct conflict with documented Can* visibility rule**: The todo explicitly proposes changing the rule documented at `docs/authorization.md:232-243` and `src/Design/CLAUDE-DESIGN.md:396-408`. The current rule states: "Can* methods inherit guard behavior from their parent factory method." The proposed rule is: "Can* methods derive behavior from the auth class and its methods." **This is an intentional correction, not an accidental conflict.** The current rule produces broken behavior in the Person example (public auth methods but Can* throws because factory method is [Remote] internal). The proposed change makes the system consistent: Can* calls auth methods, so Can* behavior should derive from auth methods.

**CONTRADICTION-2. Direct conflict with AuthorizedOrder.cs Design comments**: `src/Design/Design.Domain/Aggregates/AuthorizedOrder.cs:29-37` explicitly documents: "Can* methods inherit the [Remote] promotion and IsServerRuntime guard" from the factory method. These comments must be updated to describe the new derivation rule after the change.

**CONTRADICTION-3. Direct conflict with CanMethodVisibilityTests**: The test `InternalMethod_CanCreate_GeneratedCode_HasGuard` (`src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Visibility/CanMethodVisibilityTests.cs:148-188`) asserts that a Can* method for an internal factory method HAS the `IsServerRuntime` guard. Under the proposed change, since the test's auth class (`VisibilityTestAuth`) has public methods, the guard would NOT be emitted. This test codifies the old rule and must be updated.

**CONTRADICTION-4. Direct conflict with CanMethodVisibilityTargets comments**: `src/Tests/RemoteFactory.UnitTests/TestTargets/Visibility/CanMethodVisibilityTargets.cs:37-39` says "CanCreate should HAVE IsServerRuntime guard (internal method => guard on Can)." This comment codifies the old derivation rule.

**Assessment of contradictions**: All four contradictions are **intentional corrections** of a documented behavior that produces a bug. The existing rule ("inherit from factory method") was established before the auth-class-driven pattern was fully understood. The Person example proves the current rule is broken: public auth methods with no server dependencies should produce client-callable Can* methods regardless of the factory method's visibility. The proposed change makes Can* consistent with the general RemoteFactory accessibility paradigm.

### Recommendations for Architect

1. **Update all contradicted documentation**: The four contradicted locations (CLAUDE-DESIGN.md rule, authorization.md, AuthorizedOrder.cs comments, CanMethodVisibilityTargets comments) must all be updated to describe the new derivation rule.

2. **Update CanMethodVisibilityTests to test auth-driven behavior**: Replace the "internal factory method => guard on Can*" test with tests that verify: (a) public auth method => no guard on Can*, regardless of factory method visibility; (b) [Remote] internal auth method => guard on Can* and remote routing; (c) internal auth method (no [Remote]) => guard on Can*.

3. **Design for mixed auth method accessibility**: An auth interface may have some `public` methods and some `[Remote] internal` methods. Each Can* method should independently derive its guard/remote behavior from the specific auth methods it calls, not from a single class-level determination.

4. **AuthorizedOrder.cs Design entity needs updating**: The current AuthorizedOrder has all `[Remote] internal` factory methods with a `public` auth class (`AuthorizedOrderAuth`). Under the new rule, its Can* methods would lose the `IsServerRuntime` guard (they would be client-callable). The Design comments must be updated. Consider adding a second Design entity that demonstrates `[Remote] internal` auth methods for the "server-only auth" pattern.

5. **Consider the CanSave aggregation case**: When CanSave aggregates auth methods with different accessibility, establish a clear rule. Suggestion: CanSave should be remote/guarded if ANY of its constituent auth methods are remote/guarded (most restrictive wins for security).

6. **Verify ShowcaseAuthRemoteTests still works**: These tests use `[Remote]` on auth methods with a server-only service dependency. Under the proposed change, this pattern should produce Can* methods that route to the server (because the auth methods are `[Remote]`). Verify this continues to work correctly.

7. **Person example should work after the fix**: The Person example has `public` auth methods in `IPersonModelAuth`. After the change, `CanCreate()`, `CanFetch()`, `CanUpsert()`, and `CanDelete()` should all run on the client without guards. Verify this fixes the reported bug.

8. **Release notes**: This is a breaking change. Create a release notes entry explaining the old behavior ("Can* inherited from factory method"), the new behavior ("Can* derives from auth class"), and the migration impact (users who relied on Can* being server-only must add `[Remote]` to their auth methods if they want server-only authorization checks).

---

## Plans

- [Can* Method Behavior Derived from Auth Class Instead of Factory Method](../plans/completed/can-method-remote-routing.md)

---

## Tasks

- [x] Architect comprehension check (Step 2)
- [x] Business requirements review (Step 3)
- [x] Architect plan creation & design (Step 4)
- [x] Developer review (Step 5)
- [x] Implementation (Step 7)
- [x] Verification (Step 8)
- [x] Documentation (Step 9)

---

## Progress Log

### 2026-03-08
- Bug discovered running Person example as trimmed Blazor WASM published build
- Selecting "Delete" role throws "Server-only method called in non-server runtime"
- Initial approach was to route Can* to server since factory method is [Remote]
- User revised: Can* behavior should derive from the auth class, not the factory method — same accessibility paradigm as everything else
- Architect plan created: `docs/plans/can-method-remote-routing.md` with 15 business rules, 14 test scenarios, 7 implementation phases, and agent phasing. Plan status: Draft (Architect), ready for developer review.

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors (Neatoo.RemoteFactory.sln and Design.sln)
- Tests: 2,004 total — 970 unit, 952 integration, 82 Design — all pass, zero failures

---

## Results / Conclusions

Can* method guard/remote behavior now derives from the auth class methods instead of the parent factory method. This is a breaking change (v0.21.0).

**Generator changes:**
- `AuthMethodCall` gained `IsInternal` property
- `CanMethodModel.IsSourceMethodRemote` renamed to `IsSourceAuthMethodRemote`
- `AddCanMethods()` computes `isInternal` and `isRemote` from auth methods via `Any()`
- Guard emission logic structurally unchanged — only inputs changed

**New rules:**
- Public auth methods → Can* runs on client (no guard)
- Internal auth methods → Can* is server-only (guarded)
- `[Remote] internal` auth methods → Can* routes to server
- CanSave: most-restrictive auth method wins (if ANY auth method is internal/remote, CanSave is guarded)

**Tests:** 4 new unit tests, 1 updated. All existing tests pass including ShowcaseAuthRemoteTests.

**Docs updated:** CLAUDE-DESIGN.md, authorization.md, release notes v0.21.0, Design project comments.

**Fixes:** Person example `CanFetch()`/`CanUpsert()`/`CanDelete()` no longer throw on client (auth methods are public).
