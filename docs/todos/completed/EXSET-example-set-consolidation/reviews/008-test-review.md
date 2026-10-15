# EXSET-008 test review — 2026-10-15

## Round 1 — APPROVED (no veto-tier; 2 callouts)

**Reviewer:** test-reviewer, against `d926ae7`, diffed from `2d19668`. Logs green:
- main build (`008-build.log`): 0 errors, 2 warnings. Both are the known OrderEntry WASM native-reference notice.
- Design build (`008-design-build.log`): 0 errors, 0 warnings.
- reference app (`008-refapp-build.log`): 0 errors, 0 warnings. `EmployeeManagement.Tests` builds on both TFMs.
- tests (`008-test.log`, `008-design-test.log`): unit 839, integration 646 + 7 skips, Design 166 + 1 skip, each on both TFMs. No failures. The one Design skip is `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull`.
- mdsnippets (`008-mdsnippets.log`): no error, exception or missing-snippet line.

The logs are timestamped 12:49–12:50, and the commit is 12:51.

The counts moved by what was added:
- Design went 163 → 166 passed + 1 skipped. Those are the four `[Fact]`s in `SaveTests`.
- Unit and integration are unchanged, as expected for a plan with no library change.

Each cited test exists and runs at the declared tier. Notes per bullet:
- **1. Routing through the client (Must).** Both tests use the client's `IOrderFactory`.
  - `Save_RoutesOnIsNewAndIsDeleted` does an Insert, then an Update, then a Delete. It asserts `Fetch(order.Id)` is null at the end.
    - The assertion is not vacuous. `Order.Update` and `Order.Delete` throw `KeyNotFoundException` when the row is missing (`Order.cs:296`, `:311`), so the Insert must have stored a row.
    - A Delete routed to Update would leave the row in place, and the `Fetch` would find it.
    - Reassignment matters here. The Update gets the right route only because the Insert's result, with `IsNew` cleared on the server, was reassigned.
  - `Save_InsertThenUpdate_WritesTheOneRow` reads the server's `IOrderDbContext` after each save. It sees the inserted name, then the renamed one on the same `Id`.
- **2. Outcomes (Should).**
  - `Save_NewAndDeleted_ReturnsNull` asserts the null, and that `FindDraftAsync("Never_Saved")` is null. That catches a misroute to Insert. A misroute to Delete would throw `KeyNotFoundException`.
  - The #104 test is written naturally and skipped with the repo's `Known bug #104: …` convention. It asserts null, which is the page's stated intent and matches the unauthorized factory. Issue #104 is open, and its second case is exactly this one.
  - The missing-`[Delete]` throw is not pinned: see finding 1.
- **3–5. Explicit-skip spot checks.**
  - Neither page has a code fence outside a `snippet:`/`endSnippet` pair.
  - `save-operation.md` has 7 embeds and `interfaces-reference.md` has 4. All of them come from `src/Design`.
  - Neither page has a `LazyLoad`, `IFactoryCore`, `JsonInclude` or `409` hit. The one `private set` hit is the R-M statement that a private setter is not serialized (`save-operation.md:20`).
  - The deleted files defined 31 region names. None is still defined or embedded outside `docs/todos`, except the three that moved to Design (`save-ifactorysavemeta`, `save-usage`, `save-testing`). Each of those three now has a single definition, in Design.
  - README, getting-started, the-problem and factory-operations change only in `snippet source` attribution lines. So the new region adds no line to any existing embed.
  - The page's ordinal order ("own properties first, then its base class's", `interfaces-reference.md:136`) is what 013 established.

**Sacred-test check: clean.**
- The only test file changed besides the new `SaveTests.cs` is the deleted `EmployeeManagement.Tests/Samples/SaveOperation/SaveRoutingTestSample.cs`.
- Its three `[Fact]`s tested routing on `EmployeeCrud`, a reference-app sample type, through the local container. The plan retires that type too. They were the `save-testing` snippet source, which is the same retirement 006 and 013 did.
- The routing they covered is now pinned more strongly by `SaveTests`: through the client, against the stored row.
  - The old Delete test asserted only `NotNull(result)`.
  - `PartialSaveTests`, `SaveCodePathTests` and the `Comb_*` integration targets still pin the library's routing. None of them changed.
- The reference app builds without the deleted sources, so no remaining test referenced their types.

**Plan-introduced reachable paths:** none. The only production change is the marker pair around `Order.IsNew`/`IsDeleted` (`Order.cs:119-122`).

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|
| 1 | Callout | **Bullet 2 (`· Should`): "with no `[Delete]`, marking the object deleted throws" is not pinned by the cited tests, or by any test in the repo.**<br>The Test Evidence map cites `PartialSaveTests` `:196`, `:289` and `:358`. Those are the other missing-method branches:<br>- `:196`: `InsertDeleteOnly`, not new and not deleted, so the missing **Update** throws;<br>- `:289`: `UpdateDeleteOnly` with `IsNew`, so the missing **Insert** throws;<br>- `:358`: `InsertOnly`, not new, so the missing **Update** throws.<br>No test sets `IsDeleted = true` on a type without `[Delete]`. `IsDeleted = true` appears in `PartialSaveTests` only at `:178` and `:271`, and both of those types have a Delete. `SaveNotNullableNoDeleteTests` and `SaveWNoDeleteIsNotNullableTests` hard-code `IsDeleted => false`. The branch being cited is `ClassFactoryRenderer.cs:1132-1143` (`if (target.IsDeleted)` with no `DeleteMethod` emits `throw new NotImplementedException()` at `:1142`). The page states it at `save-operation.md:42`, including "that includes a new-and-deleted object".<br>The tier is also unit, not the declared integration. The amendment accepts the library test as a substitute, but the substitute does not cover this branch.<br>Suggested shape (unit, library; that is the amendment's tier): in `InsertUpdateOnlyTests` or `InsertOnlyTests`, set `IsDeleted = true` with `IsNew` false, then with `IsNew` true, and assert `NotImplementedException` for each. | AC-3, AC-4 | **Punch** — `PartialSaveTests.InsertOnly_Save_ThrowsNotImplemented_WhenDeleted` and `…_WhenNewAndDeleted` (unit) pin the no-Delete branch, stored and new. |
| 2 | Callout | **Bullet 1 (`· Must`): the map says "each result reassigned", but the Delete's result is discarded.**<br>`save-usage` ends with `await orderFactory.Save(order); // IsDeleted: Delete` (`SaveTests.cs:40`), and the page then says "Always reassign it" (`save-operation.md:141`).<br>The routing behaviour is pinned, and so is reassignment where it carries server state: the Update depends on the Insert's reassigned result. After a Delete, the returned object carries nothing the original lacks. So I tiered this as an overstatement in the map, not an unpinned behaviour. If Keith reads "each result" literally, it is veto-tier.<br>Suggested shape (integration, same test): either reassign the Delete's result in the region, or narrow the bullet and the map to "each Insert and Update result". | AC-3 | **Punch** — the `save-usage` region reassigns the Delete result too, as the page says. |
| 3 | Tech-debt | `SaveTests` disposes its three scopes at the end of each test body, not in `using`/`finally`. A failed assertion leaks them. This is the same pattern 007's finding 4 punched for not disposing at all. | — | **Dismiss** — every Design test disposes at the end of its body (007 punched to that pattern); the containers are in-memory, so a failed test leaks nothing that outlives the test host, and `try`/`finally` would add noise to the embedded `save-testing` region. |

Could not verify:
- That `AuthorizedSave_NewAndDeleted_ReturnsNull` fails unskipped with `NotAuthorizedException` on both TFMs, as the map states. I did not run it, and no log of the unskipped run was provided. The mechanism in #104 (`ClassFactoryRenderer.cs:1121-1123`, emitted at `:1137`: `new Authorized<T>()`, with `HasAccess` false, for new-and-deleted) supports the claim.
- That the remaining reference-app tests pass. `008-refapp-build.log` shows the build only. Bullet 3 asks only that the reference app builds.

Theoretical (not triaged):
- The #104 test resolves `IAuthorizedOrderFactory` from the local container, not the client. That matches every test in `AuthorizationTests`, and the defect is in the local Save body, which the server also runs in remote mode. So the client tier would observe the same throw.
