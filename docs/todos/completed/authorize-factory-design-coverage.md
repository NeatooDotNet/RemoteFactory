# AuthorizeFactory Design Project Coverage

**Status:** Complete
**Priority:** Medium
**Created:** 2026-03-08
**Last Updated:** 2026-03-08



---

## Problem

`[AuthorizeFactory]` is not covered in the Design projects. `SecureOrder.cs` only comments about the attribute but doesn't actually use it. There are no Design.Tests for `CanXxx` authorization methods. The Design projects are supposed to be the source of truth, but they don't demonstrate `[AuthorizeFactory]` with `[Remote]` methods — which means the `CanXxx` promotion behavior (especially `[Remote] internal` → `CanXxx` promoted to `public` on the factory interface) has no source-of-truth demonstration.

**Discovered references:**
- `src/Design/Design.Domain/Aggregates/SecureOrder.cs` — documents `[AuthorizeFactory]` in comments only, does not use the attribute
- `src/Design/Design.Tests/` — no `CanXxx` tests exist
- `src/Examples/Person/Person.DomainModel/PersonModel.cs` — uses `[AuthorizeFactory<IPersonModelAuth>]` with `[Remote]` methods (working example, not source-of-truth)
- Related work: [remote-requires-internal](remote-requires-internal.md) — changed `[Remote]` to require `internal`, which affects `CanXxx` visibility promotion

## Solution

Add actual `[AuthorizeFactory]` usage to the Design project (likely `SecureOrder`), including demonstrating `[Remote] internal` methods with `CanXxx` promotion to `public` on the factory interface. Add Design.Tests covering authorization behavior. Ensure thorough testing of `[AuthorizeFactory]` in the test projects (unit and integration).

---

## Clarifications

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-08
**Verdict:** APPROVED

### Relevant Requirements Found

**R1. [AuthorizeFactory<T>] attribute contract** (`src/RemoteFactory/FactoryAttributes.cs:113-117`): The attribute is defined with `AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)` and takes a generic type parameter `T`. The type `T` can be an interface or a concrete class. The attribute is well-established in the codebase with extensive test coverage in unit and integration tests.

**R2. [AuthorizeFactory] operation attribute contract** (`src/RemoteFactory/FactoryAttributes.cs:119-127`): Individual auth methods are marked with `[AuthorizeFactory(AuthorizeFactoryOperation.Xxx)]` and must return `bool`, `Task<bool>`, `string?`, or `Task<string?>`. Returning the wrong type emits diagnostic NF0202 (verified by `src/Tests/RemoteFactory.UnitTests/Diagnostics/NF0202Tests.cs`).

**R3. Can* method visibility rules** (`src/Design/CLAUDE-DESIGN.md:396-443`, `docs/authorization.md:230-243`): Can* methods inherit guard behavior from their parent factory method. Public parent method produces a Can* method with NO `IsServerRuntime` guard (runs on client). Internal parent method produces a Can* method WITH `IsServerRuntime` guard (server-only). This is verified by tests in `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Visibility/CanMethodVisibilityTests.cs`.

**R4. [Remote] requires internal** (`src/Design/CLAUDE-DESIGN.md:141`, Anti-Pattern 8): `[Remote]` requires `internal` visibility. `[Remote] public` is a compile-time error (NF0105). `[Remote] internal` methods are promoted to `public` on the factory interface. This means Can* methods for `[Remote] internal` operations inherit the `[Remote]` promotion -- they appear as `public` on the factory interface and their underlying `LocalCanXxx` gets a guard.

**R5. Auth type auto-registration for trimming** (`src/Design/CLAUDE-DESIGN.md:469-473`, `docs/trimming.md:125-129`): The generator emits explicit `services.TryAddTransient<IFooAuth, FooAuth>()` registrations in `FactoryServiceRegistrar` for every `[AuthorizeFactory<T>]` type. The concrete type is resolved at compile time using naming convention (`IPersonModelAuth` -> `PersonModelAuth`).

**R6. Authorization failure behavior** (`docs/authorization.md:245-250`): Create/Fetch return null on auth failure. Save throws `NotAuthorizedException`. Events bypass authorization. These behaviors are tested in `src/Tests/RemoteFactory.IntegrationTests/Showcase/ShowcaseAuthTests.cs`.

**R7. AuthorizeFactory operation flags** (`docs/authorization.md:87-101`): `Read` covers Create + Fetch. `Write` covers Insert + Update + Delete. `Execute` covers Execute operations. Individual flags for fine-grained control. Bitwise OR combines multiple operations.

**R8. Remote auth interface pattern** (`src/Tests/RemoteFactory.IntegrationTests/Showcase/ShowcaseAuthRemoteTests.cs:13-34`): Auth interface methods can have `[Remote]` attribute, making the auth check itself execute on the server. This enables server-only service dependencies in auth logic. The auth implementation is only registered on the server, not the client.

**R9. SecureOrder currently uses public visibility** (`src/Design/Design.Domain/Aggregates/SecureOrder.cs:39`): `SecureOrder` is declared as `public partial class SecureOrder` (not `internal`). This is a different pattern from `Order` which is `internal`. If `[AuthorizeFactory]` is added to `SecureOrder`, the architect must decide whether to also convert it to the `internal class + public interface` pattern, or keep it `public` for demonstration simplicity.

**R10. Design Completeness Checklist** (`src/Design/CLAUDE-DESIGN.md:615-628`): The checklist has `[ ] ASP.NET Core policy-based authorization (SecureOrder.cs)` but has NO item for `[AuthorizeFactory<T>]` custom domain authorization. This is a documentation gap -- `[AuthorizeFactory]` is arguably more important than `[AspAuthorize]` since it provides unified client+server auth.

**R11. Existing integration test patterns** (`src/Tests/RemoteFactory.IntegrationTests/Showcase/ShowcaseAuthTests.cs`): `ShowcaseAuthObj` demonstrates the `[AuthorizeFactory<IShowcaseAuthorize>]` pattern on an `internal class` with an `IShowcaseAuthObj` public interface, `IFactorySaveMeta`, and all CRUD operations. Tests cover CanCreate, CanFetch, CanSave, CanDelete, TrySave, NotAuthorizedException on denied operations. This is the closest existing model for what the Design project should demonstrate.

**R12. Person example pattern** (`src/Examples/Person/Person.DomainModel/PersonModel.cs`, `PersonModelAuth.cs`): Demonstrates `[AuthorizeFactory<IPersonModelAuth>]` with `[Remote] internal` methods on an `internal class`. Auth interface uses `AuthorizeFactoryOperation.Read | Write` for broad scope plus individual operation flags. Implementation uses constructor-injected `IUser` service. This is a real-world working example.

**R13. Design Debt table** (`src/Design/CLAUDE-DESIGN.md:632-643`): No entry in the design debt table prohibits implementing `[AuthorizeFactory]` in the Design project. The only authorization-related debt item is "OR logic for [AspAuthorize]" which is unrelated to this todo.

### Gaps

**GAP-1. No Design source-of-truth for [AuthorizeFactory]**: The Design project has zero usage of `[AuthorizeFactory<T>]`. SecureOrder.cs mentions it in comments (lines 26-29) but does not demonstrate it. This means the single source of truth has no authoritative example of the most important authorization pattern.

**GAP-2. No Design.Tests for Can* methods**: Confirmed by grep -- zero results for CanCreate, CanFetch, CanSave, CanDelete, CanUpdate in `src/Design/Design.Tests/`. The Design tests do not cover any authorization behavior.

**GAP-3. No Design source-of-truth for remote auth interface**: The `[Remote]` attribute on auth interface methods (enabling server-only auth checks) is only demonstrated in integration tests (`ShowcaseAuthRemoteTests.cs`). The architect should decide whether to include this pattern in the Design project or leave it for a future todo.

**GAP-4. CLAUDE-DESIGN.md Design Completeness Checklist missing [AuthorizeFactory] item**: The checklist needs an item for `[AuthorizeFactory<T>]` custom domain authorization, separate from the existing `[AspAuthorize]` item.

**GAP-5. No Design demonstration of CanSave/TrySave with auth**: The `ShowcaseAuthTests` demonstrate that CanSave aggregates all write auth checks and TrySave returns null on failure. Neither pattern exists in the Design project.

### Contradictions

None found. The todo proposes adding coverage for an existing, well-tested feature. No design debt entries prohibit this work. No documented anti-patterns are violated.

### Recommendations for Architect

1. **Follow the ShowcaseAuthObj pattern** (`src/Tests/RemoteFactory.IntegrationTests/Showcase/ShowcaseAuthTests.cs`): This is the most complete existing example of `[AuthorizeFactory]` with `internal class + public interface + IFactorySaveMeta`. Use it as the template for the Design project implementation.

2. **Decide on SecureOrder's class visibility**: SecureOrder is currently `public partial class`. If adding `[AuthorizeFactory]`, consider whether to convert to the `internal class + public interface` pattern (matching Order.cs) to demonstrate both `[Remote] internal` visibility promotion AND Can* method behavior, or create a separate entity. The `internal class` pattern is the canonical one for aggregate roots.

3. **Demonstrate Can* methods on both public and [Remote] internal methods**: The core behavior difference is that Can* methods for public methods have no guard (client-callable) while Can* methods for `[Remote] internal` methods have the `IsServerRuntime` guard. Tests in `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Visibility/CanMethodVisibilityTests.cs` verify this at the generator level -- Design.Tests should verify it at the integration level.

4. **Include auth failure behaviors in Design.Tests**: Test that Create/Fetch return null on auth failure, Save throws `NotAuthorizedException`, and TrySave returns `HasAccess=false`. These are documented in `docs/authorization.md:245-250`.

5. **Demonstrate `AuthorizeFactoryOperation.Read | Write` for broad scope**: The Person example uses this pattern effectively. Show that `Read` covers both Create and Fetch, while `Write` covers Insert, Update, and Delete.

6. **Update CLAUDE-DESIGN.md Design Completeness Checklist**: Add a new checklist item for `[AuthorizeFactory<T>]` custom domain authorization. The current checklist only mentions `[AspAuthorize]`.

7. **Document the auth auto-registration for trimming in Design code**: Add a comment in the Design project noting that the generator emits explicit DI registrations for auth types (per `CLAUDE-DESIGN.md:469-473`).

8. **Scope decision for remote auth interface**: The `[Remote]` on auth interface methods (from `ShowcaseAuthRemoteTests.cs`) is an advanced pattern. Recommend deferring this to a future todo unless the architect sees natural overlap.

---

## Plans

- [AuthorizeFactory Design Project Coverage Plan](../plans/completed/authorize-factory-design-coverage.md)

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
- Created todo from gap discovered during remote-requires-internal implementation
- SecureOrder.cs only comments about [AuthorizeFactory], doesn't use it
- No Design.Tests for CanXxx methods exist
- Architect comprehension check completed (Step 2)
- Business requirements review completed -- APPROVED with 13 requirements, 5 gaps, 8 recommendations (Step 3)
- Architect plan created at docs/plans/authorize-factory-design-coverage.md (Step 4)
  - Decision: New entity AuthorizedOrder rather than modifying SecureOrder
  - 16 business rules as testable assertions, 12 test scenarios
  - 3 implementation phases, single agent session

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors, 0 warnings (Design.sln and Neatoo.RemoteFactory.sln)
- Tests: 41 Design (29 existing + 12 new), 481 unit, 476 integration — all pass

---

## Results / Conclusions

Added `[AuthorizeFactory<T>]` source-of-truth coverage to the Design project:

- **AuthorizedOrder.cs** — New entity with `[AuthorizeFactory<IAuthorizedOrderAuth>]`, `internal class + public interface` pattern, all 5 CRUD operations as `[Remote] internal`, `IFactorySaveMeta`, `IFactoryOnCompleteAsync`
- **AuthorizedOrderAuth.cs** — Auth interface with broad-scope (`Read | Write`) and fine-grained (`Create`, `Fetch`, `Delete`) operation flags; static boolean flags for test configurability
- **AuthorizationTests.cs** — 12 tests covering Can* methods, auth failure behaviors (null returns, NotAuthorizedException, TrySave), and client-server round-trip
- **CLAUDE-DESIGN.md** — Added `[AuthorizeFactory<T>]` checklist item, Design Files entries, Quick Decisions Table row
- **SecureOrder.cs** — Cross-reference comment to AuthorizedOrder.cs

All 5 gaps (GAP-1 through GAP-5) from the requirements review are resolved. Remote auth interface (`[Remote]` on auth methods) deferred to a future todo.
