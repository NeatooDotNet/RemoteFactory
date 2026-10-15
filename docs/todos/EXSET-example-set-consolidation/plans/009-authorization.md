# Authorization

**Plan #:** 009
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** In Progress
**Last Updated:** 2026-10-15
**Plan-review opt-in:** Yes. Design gains a cross-tier user service and server authentication, and getting started's server embed changes.
**Code-review opt-in:** Yes. The page is rewritten against the generator's authorization paths.
**Branch:** EXSET-009-authorization
**PR:** —

---

## Scope

Re-point every C# block on `docs/authorization.md` (12 embeds, 9 hand-written fences) and the five `authorization-*` embeds in the skill's `advanced-patterns.md` to `src/Design`. Move `aspnetcore-middleware-order` (`aspnetcore-integration.md`) from the reference app to Design.Server. `attributes-reference.md` is split out to EXSET-014 (Keith, 2026-10-15).

**Design changes** (Keith, 2026-10-15):
- **`ICurrentUser`,** a Design.Domain service holding the signed-in user (`IsAuthenticated`, `IsInRole`), with a claims-backed implementation.
  - The server builds it from `HttpContext.User`.
  - The Blazor client registers an anonymous one.
  - The test harness shares one settable test user across a `Scopes()` call's three containers.
- **`AuthorizedOrderAuth`** takes `ICurrentUser` by constructor, in place of its static flags: a signed-in user, with one role each for Create, Fetch and Delete. `AuthorizationTests` sets the test user's roles where it flipped flags, and keeps every assertion. Per-container state also ends the static flags' cross-test coupling.
- **`AuthorizedOrder`'s Create** is local (D14), and so are `SecureOrder`'s and `ParamAuthOrder`'s. That closes the punchlist row. The client harness, Design.Server, the HTTP test client and the Blazor client register what a local Create and a client-side `Can*` need.
- **`SecureOrder`:**
  - interface-first, as an `internal` class with `ISecureOrder` (C-19);
  - its `[AspAuthorize]` operations tested for the first time, over HTTP through `DesignHttpHost`, with a test authentication scheme whose user the test sets.
- **Design.Server:**
  - `AddAuthentication()`, plus `AddAuthorization` with `SecureOrder`'s policies;
  - `UseAuthentication()` and `UseAuthorization()` before `UseNeatoo()`.

  Getting started's server embed gains those two lines, and the nested `aspnetcore-middleware-order` region shows them. The real scheme (cookies, JWT) is the application's choice, stated in prose.

**Becomes prose:**
- the parameterized and target-parameter interfaces, which embed `IParamAuthOrderAuth`'s declarations instead;
- the `RegisterMatchingName` client line;
- the `[Remote]` auth interface;
- the `NotAuthorizedException` catch.

**Removed:**
- "Events bypass authorization" (T-09: there is no `[Event]` operation);
- `Terminate` as an `[Update]` (C-24);
- the 401/403 claim (T-11). It is replaced by the true outcomes: a class factory's denial is a `null` or a `NotAuthorizedException`, and an interface factory's `[AspAuthorize]` denial answers 403.

**Findings to fix:** T-11, C-24, I-09 (the `Can*` method set and flag table), T-09 (the events line), U-02 (the local Creates), and the C-19, C-27, I-01, I-02 and T-22 shares on this page.

Delete `Samples/Authorization/AuthorizationSamples.cs`, `Server.WebApi/Samples/Authorization/AuthorizationPolicyConfig.cs`, and the middleware-order region in `BasicSetupSamples.cs`, along with any of their orphan regions.

Does NOT touch:
- `attributes-reference.md` (EXSET-014);
- `CustomerDirectoryAuth`'s and `ParamAuthOrderAuth`'s flag-based implementations. Only their declarations are embedded.
- the skill's prose (Todo 2).

---

## Intent

- **A real example.** A reader sees `[AuthorizeFactory<T>]` on a real aggregate, with an auth class that reads the signed-in user, runs its `Can*` on the client and enforces the same rules on the server. They also see `[AspAuthorize]` policies, configured on a server that authenticates.
- **True outcomes.** The page states what a denial does on each path, and the 401/403 claim goes.

---

## Framework & Architectural Alignment

- **Rulings:**
  - D14: a root's Create is local;
  - R-E: authorization is `[AuthorizeFactory<T>]`;
  - R-M;
  - C-19: interface-first;
  - I-12's prose precedent.
- **`.claude/rules/design-snippets.md`.**
- **006's HTTP tier** (`DesignHttpHost`, the failure table in `aspnetcore-integration.md`), which this page links rather than restates.

---

## Constraints & Invariants

- **Embeds.** Every C# block on the page is an embed from `src/Design` or a short WRONG block.
- **Sacred tests.** `AuthorizationTests`' assertions are unchanged. Only the means of denying changes, from a static flag to the test user's roles. `InterfaceFactoryAuthorizationTests` and `ParamAuthorizationTests` are untouched.
- **Getting started's server embed** gains only the two middleware lines, under one comment.
- **The whole still works:** both solutions green, mdsnippets swallows nothing, the reference app builds.

---

## Steps

1. **`ICurrentUser` and its registrations:** the server, the client harness, the HTTP host's client, the composition test and the Blazor client.
2. **`AuthorizedOrderAuth` on `ICurrentUser`;** local Creates; `AuthorizationTests` moved to the test user.
3. **`SecureOrder`** interface-first; Design.Server authentication and policies; the middleware lines.
4. **HTTP tests:** `[AspAuthorize]` allows a user with the policy or role, and a denial gives `null` on Fetch and `NotAuthorizedException` on Save.
5. **Regions and the page:**
   - re-point the page, the skill's five embeds and the middleware-order embed;
   - rewrite the flag table, the `Can*` table and the failures section;
   - delete the reference-app sources.
6. **Verify and record:** mdsnippets, both solutions, and the ledger.

---

## Acceptance

- [ ] `AuthorizedOrder`'s `Can*`, Create, Fetch and Save allow or deny by the signed-in user's roles, and a denied client-server Fetch returns null. These are the existing `AuthorizationTests`, now on the test user. `[integration]` · Must
- [ ] Create is local for the three auth aggregates, and `CanCreate` runs on the client with no server call `[integration]` · Must
- [ ] Over HTTP, `SecureOrder`'s `[AspAuthorize]` operations allow a user who meets the policy or role. Denied, the Fetch returns null and the Save throws `NotAuthorizedException`. `[integration]` · Must
- [ ] The page's denial outcomes match the generator: class factory null or `NotAuthorizedException`; interface-factory `[AspAuthorize]` 403. `[explicit-skip: source-cited review; the class-factory outcomes are also pinned by bullets 1 and 3]` · Should
- [ ] On the page and the skill's embeds: every C# block is from Design or WRONG, and no retired region is embedded; the reference-app sources are gone and the app builds `[explicit-skip: greps; EmployeeManagement.sln build]` · Must
- [ ] mdsnippets clean; both solutions green `[explicit-skip: meta]` · Must

---

## Current State (Pre-Flight)

- **Design's authorization types are test scaffolding:**
  - `AuthorizedOrderAuth`, `ParamAuthOrderAuth` and `CustomerDirectoryAuth` decide from static flags;
  - the three aggregates fabricate their persistence;
  - `SecureOrder` is a public concrete class with no tests;
  - all three aggregates are `[Remote, Create]`.
- **No server authentication.** Design.Server registers no authentication or authorization, so `SecureOrder`'s `[AspAuthorize]` would throw "No AspCoreNet Authorization registered" over HTTP. `AspAuthorize` authenticates through `IPolicyEvaluator`; with no scheme named on the attribute, that reads `HttpContext.User`, which `UseAuthentication` sets. (Corrected by the plan review.)
- **`forbid`:** `true` only for interface-factory methods (`FactoryModelBuilder.cs:437`). A forbidding denial is answered 403 by `context.Forbid()`, with an empty body (`HandleRemoteDelegateRequest.cs:246-251`). Class-factory reads, writes, `Can*` and Save pass `false`: the denial becomes a `null`, or `NotAuthorizedException` for Save.
- **`DesignHttpHost`** replaces `IAuthorizedOrderAuth` with a `SwitchableOrderAuth`, and keeps it.
- **The reference app's middleware-order sample** shows CORS, authentication, authorization, then `UseNeatoo`.

---

## Punchlist

---

## Test Evidence

---

## Gate Record

---

## Plan Amendments

- **2026-10-15 · Plan review** ([009-plan-review.md](../reviews/009-plan-review.md)): APPROVED, with 15 callouts, all amended (see its Triage column). The ones that change the plan:
  - **Library fix (Keith).** An anonymous caller passes a class factory's `[AspAuthorize]`. When authentication returns no result, `AspAuthorize` returns a null message, and a challenged denial returns `""`; both convert to `Authorized(true)`. `AspAuthorize` now runs the policy in every case, and a denial always carries a message. This adds unit tests, a Design HTTP test where anonymous is denied, an issue for the record, and release notes.
  - **Create call sites (Keith).** Local Creates are synchronous, so `await factory.Create(...)` loses its `await` in AuthorizationTests, ParamAuthorizationTests and SaveTests' #104 test. The assertions are unchanged. The Constraint that ParamAuthorizationTests is "untouched" no longer holds for these call sites.
  - **The three Creates** become `public` `[Create]`.
  - **`ICurrentUser`'s implementation** has a non-matching name and is registered explicitly. `AddHttpContextAccessor()` is added.
  - **`AuthorizationSamples.cs` stays** for EXSET-014 and EXSET-012; only its region markers go.
  - **Three embeds:** `authorization-generated` is a region in AuthorizationTests; `authorization-combined` and `authorization-method-level` become prose.
  - **`aspnetcore-middleware-order` retires.** `aspnetcore-integration.md` points at the server Program it already embeds, so there is no nested region showing the lines twice.
  - **`SwitchableOrderAuth`** delegates to the real auth class over the request's `ICurrentUser` unless switched, so the allowed HTTP paths exercise the `HttpContext` user.
  - **The 403** is stated with its conditions.
  - **Ledger:** C-27 and I-02 added; U-02 gains 009.

---

## Notes

- **Keith, 2026-10-15:**
  - rework `AuthorizedOrder` onto an injected `ICurrentUser`;
  - bare `AddAuthentication` on Design.Server, with getting started gaining the two middleware lines;
  - split `attributes-reference.md` into EXSET-014.
