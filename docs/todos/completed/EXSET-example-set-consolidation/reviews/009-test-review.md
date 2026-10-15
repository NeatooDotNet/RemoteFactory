# EXSET-009 test review — 2026-10-15

## Round 1 — APPROVED (no veto-tier; 2 callouts)

**Reviewer:** test-reviewer, against `09483b9`, diffed from `64a5d73`. Logs green:
- main build (`009-build.log`): 0 errors, 2 warnings. Both are the known OrderEntry WASM native-reference notice.
- Design build (`009-design-build.log`): 0 errors, 0 warnings. Design.Domain, Design.Client.Blazor, Design.Server and Design.Tests each build on both TFMs.
- reference app (`009-refapp-build.log`): 0 errors, 0 warnings. `EmployeeManagement.Tests` builds on both TFMs.
- tests (`009-test.log`, `009-design-test.log`): unit 852, integration 652 + 7 skips, Design 171 + 1 skip, each on both TFMs. No failures. The one Design skip is `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull` (#104).
- mdsnippets (`009-mdsnippets.log`): no error, exception or missing-snippet line. All eleven `authorization-*` regions the page embeds are extracted from `src/Design`.
- unfixed run (`009-unfixed-aspauthorize-test.log`): 4 `AspAuthorizeHttpTests` per TFM. `Fetch_AnonymousCaller_IsDenied` fails on net9.0 and net10.0 with `Assert.Null() Failure`; the actual value is `SecureOrder { Id = 5, ... }`. The other three pass.

The logs are timestamped 14:19, and the commit is 14:21.

The counts moved by what was added:
- Design went 166 → 171 passed + 1 skipped. Those are the four `[Fact]`s in `AspAuthorizeHttpTests` and `AuthorizationTests.AuthorizedOrder_CanCreate_GuardsTheCreate_OnTheClient`.
- Unit and integration match the expected counts from `main`'s merge.

Each cited test exists and runs at the declared tier. Notes per bullet:
- **1. AuthorizedOrder by roles (Must).** All 12 scenarios are present, with their assertions as before.
  - Each flag the tests flipped maps to the role that rule now reads (`AuthorizedOrderAuth.cs`):
    - `AllowCreate = false` became removing `Sales` (`CanCreate`);
    - `AllowFetch = false` became removing `Sales` and `Support` (`CanFetch` is either one);
    - `AllowDelete = false` became removing `Manager` (`CanDelete`).
  - `ResetFlags()` became the fresh `TestUser`, which is signed in with all three roles. That matches the old all-true defaults.
  - Scenario 12 (`Fetch_ReturnsNull_ThroughClientServer`) removes the roles from the client's `TestUser`. That is the same instance the server resolves (`DesignClientServerContainers.cs:278-285`), as the static flag was shared before.
  - Over HTTP, `ClientErrorHandlingTests.AllowedSave_ReplacesTheScreensOrder` runs an allowed Fetch and Save through `GatedOrderAuth` → `AuthorizedOrderAuth` → the server's `ClaimsUser`, built over the test scheme's principal. So the map's HTTP sentence holds for the allowed side; for the deny side, see finding 1.
- **2. Local Creates, client `CanCreate` (Must).**
  - `Create_WorksThroughClientServer` now asserts `RemoteCallCount == 0`. That counter is real: other tests assert 1 and 2 on it.
  - `CanCreate_GuardsTheCreate_OnTheClient` asserts three things on the client container: the Create happened, `CanCreate` is denied once `Sales` is removed, and no remote call was made.
  - `ParamAuthOrder`'s and `SecureOrder`'s locality is pinned at compile time. A `[Remote]` Create returns `Task<T>`, so the synchronous call sites would not compile against one: `ParamAuthorizationTests` (including the two client-container tests at `:504` and `:527`), and `AspAuthorizeHttpTests.Insert_NeedsAnAdminOrManager`, which passes `Create(...)` straight to `Save`. The map leaves out SecureOrder's call site, but the test does cover it.
- **3. `[AspAuthorize]` over HTTP (Must).**
  - Fetch: a `Support` user gets order 5, and an anonymous caller gets null. The test scheme returns `NoResult` for `anonymous`, so the null comes from the policy. The unfixed log shows this test discriminates: on the arc's old `AspAuthorize`, the anonymous caller got the order.
  - Insert: `Manager` saves and gets an `Id`; `Sales` gets `NotAuthorizedException`. `SecureOrder` has no `[AuthorizeFactory]`, so the refusal can only come from the `Roles` attribute.
  - Update: `Manager` passes both attributes; `Admin` is refused.
- **4–6. Explicit-skip spot checks.**
  - `authorization.md` has no code fence outside a `snippet:`/`endSnippet` pair. All its embeds source from `src/Design`: AuthorizedOrder 1, AuthorizedOrderAuth 2, ParamAuthOrderAuth 2, SecureOrder 3, ServerServices 1, AuthorizationTests 2. The count is 11, not 12: see finding 2.
  - The skill's five `authorization-*` embeds all source from Design.
  - No file outside `docs/todos` mentions `aspnetcore-middleware-order`, `authorization-combined` or `authorization-method-level`.
  - `AuthorizationPolicyConfig.cs` and `BasicSetupSamples.cs` are deleted. `AuthorizationSamples.cs` has no `#region` left.
  - The citations for bullet 4 resolve:
    - `FactoryModelBuilder.cs:437` is `BuildAuthorization(method, aspForbid: true)` for interface methods;
    - `HandleRemoteDelegateRequest.cs:246-251` catches `AspForbidException` and returns an empty body;
    - `AspAuthorize.cs:86-110` forbids only when `forbid` is set.

**Sacred-test check: clean.** Every changed line in the four pre-existing files was compared with `64a5d73`. Assertion counts went from 27 to 31 in AuthorizationTests (+1 in scenario 11, +3 in the new test), and stayed at 36, 8 and 21→22 in the other three files (the composition test gained one). `[Fact]` counts went from 12 to 13, and stayed at 20, 4 and 4.
- **`AuthorizationTests`.**
  - The only setup changes are the approved ones: `ResetFlags()` and flag flips became role removals from `TestUser`, and `await` was dropped from `Create`. `Create_ReturnsNull_WhenDenied` and `Create_WorksThroughClientServer` became `void`, because the Create is synchronous.
  - In both Save-denial tests the denial still comes after the Create, as it did before.
  - No assertion was removed or changed, and no expected value moved.
  - Scenario 11 kept its name and its three assertions and gained a fourth. Its doc comment now describes a local Create: D14 removed the server trip it described, so the test cannot keep exercising one. An allowed AuthorizedOrder round trip is still pinned, over HTTP, by `ClientErrorHandlingTests.AllowedSave_ReplacesTheScreensOrder`, which is unchanged.
- **`ParamAuthorizationTests`.** Ten lines, each `await factory.Create(...)` → `factory.Create(...)`. Nothing else changed. This is the amendment's approved change.
- **`SaveTests`.** The #104 test lost `ResetFlags()`, because the method is gone and the `TestUser` default is equivalent, and lost the `await` on Create. The skip and its assertions are unchanged.
- **`DesignServerCompositionTests`.** One assertion was added (`ICurrentUser` resolves), and nothing was removed.
- **Other test files.** `InterfaceFactoryAuthorizationTests` and `ClientErrorHandlingTests` are not in the diff.

**Plan-introduced reachable paths:** one, the server's `ICurrentUser` deny side (finding 1).

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|
| 1 | Callout | **The server's `ICurrentUser` is pinned only on its allowed side.**<br>`ServerServices.AddDesignAuthorization` registers `new ClaimsUser(HttpContext?.User ?? new ClaimsPrincipal())`, and `ClaimsUser` (`CurrentUser.cs`) denies an anonymous principal through `IsAuthenticated` and the `IsAuthenticated &&` guard in `IsInRole`. Both are new in this plan.<br>Over HTTP, only `ClientErrorHandlingTests` reach AuthorizedOrder. They send the default all-roles user and deny through `GatedOrderAuth`'s switches, never through the request's user.<br>`AsUser` is used only against `SecureOrder`. So no test shows a request user who is anonymous, or who lacks `Sales`/`Support`/`Manager`, being refused by `AuthorizedOrderAuth` on the server.<br>The rules themselves are pinned in-process on `TestUser`; what is not pinned is the `HttpContext.User` → `ClaimsUser` → deny chain.<br>Reachable by: `/api/neatoo` for `IAuthorizedOrderFactory.Fetch`/`Save` on Design.Server. Design.Server registers no scheme, so as shipped every such request is anonymous.<br>Suggested shape (integration, HTTP, `DesignHttpHost`): `AsUser(OrderRoles.Manager)` Fetch of an AuthorizedOrder returns null, and `AsUser(TestUserScheme.Anonymous)` Save throws `NotAuthorizedException`. | AC-3 | **Punch** — `AuthorizeFactoryHttpTests`: a Manager's Fetch is null and a Support user's is not; an anonymous Save throws `NotAuthorizedException`. Both run with the switches on. Both classes are in one xUnit collection: run in parallel with `ClientErrorHandlingTests`, a Fetch landed while its switch was off and returned null (net9.0, every run). |
| 2 | Callout | **The Test Evidence map says the page has 12 embeds; it has 11.**<br>Row 5 says "All 12 embeds come from Design". `authorization.md` has 11 `snippet:` markers: interface, implementation, apply, generated, policy-config, policy-apply, policy-roles, policy-multiple, parameterized, target and testing.<br>`64a5d73` had 12. The amendment turned two of them into prose (`authorization-combined`, `authorization-method-level`) and added one (`authorization-generated`).<br>All 11 are from Design, so the bullet holds. Only the map's number is wrong.<br>Suggested shape: correct the count in row 5. | AC-3 | **Punch** — "All 11 embeds". |
| 3 | Tech-debt | `AuthorizationTests.cs:20-21` still says scenarios 11 and 12 "verify serialization round-trip". Scenario 11 no longer crosses, and its name (`…_WorksThroughClientServer`) now misdescribes a local Create. | — | **Punch** — the header comment describes scenarios 11 and 12 as they are; scenario 11 is renamed `AuthorizedOrder_Create_RunsOnTheClient`. |
| 4 | Tech-debt | `AuthorizationTests.cs:51` says "Even though the source methods are [Remote] internal"; Create no longer is. | — | **Punch** — the comment names Create as local. |
| 5 | Tech-debt | `ClientErrorHandlingTests.cs:13-14` says AuthorizedOrder's rules "come from the host's switchable stand-in". They are now the real rules behind `GatedOrderAuth`'s switches. | — | **Punch** — the comment says the rules are the real ones behind switches. |
| 6 | Tech-debt | The todo's Plan Index lists 009 as serving AC-2, AC-3, AC-4; the plan's Serves says AC-3, AC-4. | — | **Punch** — the Plan Index says AC-3, AC-4. |

Could not verify:
- That the logged tree is `09483b9`. The logs are timestamped 14:19 and the commit 14:21, and nothing in the logs fixes the tree they ran against.
- That the five new Design tests ran by name. `009-design-test.log` lists only the skipped test. Their presence is inferred from the 166 → 171 count, which matches exactly.

Theoretical (not triaged):
- `SecureOrder.Delete`'s `[AspAuthorize(Roles = "Admin")]` is not exercised over HTTP. The method predates this plan, no page region embeds it, and it has the same shape as Insert's `Roles`, which is pinned.
- `Update_NeedsBothPolicies` shows the `Roles = "Manager"` half refusing. The `RequireAuthenticated` half cannot refuse on its own, because a user who holds a role is authenticated.
- Design.Client.Blazor's anonymous `ICurrentUser` has no caller: no Blazor page uses `IAuthorizedOrderFactory`.
