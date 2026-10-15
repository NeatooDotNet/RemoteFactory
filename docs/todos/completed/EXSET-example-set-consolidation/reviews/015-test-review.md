# EXSET-015 test review — 2026-10-15

## Round 1 — CONCERNS (1 veto-tier, 0 callouts)

**Reviewer:** test-reviewer, against `50eb667`, diffed from `9aa6bd0`. Logs green:
- main build (`015-build.log`): 0 errors, 2 warnings. Both are the known OrderEntry WASM native-reference notice.
- Design build (`015-design-build.log`): 0 errors, 0 warnings. `Design.Domain`, `Design.Client.Blazor`, `Design.Server` and `Design.Tests` were built on both TFMs. So `Home.razor`'s `@code` block compiles.
- reference app (`015-refapp-build.log`): 0 errors, 0 warnings, all six EmployeeManagement projects on both TFMs, with the twelve sample files gone.
- tests (`015-test.log`, `015-design-test.log`): unit 852, integration 657 + 7 skips, Design 173 + 1 skip, each on both TFMs. No failures. The Design skip is `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull` (#104). The integration skips are the known three `ShowcasePerformanceTests`, two `RelayTimingTests` and two `FactoryReferenceIdentityTests`. The counts are unchanged, which is what three region pairs should give.
- mdsnippets (`015-mdsnippets.log`): no error, exception or missing-snippet line; it ends `Finished`. It extracts `skill-blazor-usage` from `src/Design/Design.Client.Blazor/Pages/Home.razor`.

The three Design edits are timestamped 15:45:07–15:45:21. The logs are 15:47:22–15:47:53, the pages 15:47:53 (the mdsnippets run), and the commit 15:49.

Notes per bullet:
- **1. Skill embeds (Must): honest.**
  - Every `snippet source` link under `skills/` is under `src/Design`, apart from `lazyload.md`'s two (`LazyLoadSamples.cs`, EXSET-011). There are 35 links across the reference pages, and 35 snippet openings. The only other `snippet:` text is `SKILL.md:121`'s prose `snippet: name`.
  - Each of the 34 keys the reference pages embed is defined once under `src`. That includes the four new keys: `skill-interface-factory-auth-authclass` and `-factory` in `CustomerDirectory.cs`, `skill-value-object-factory` in `TaxRate.cs`, and `skill-blazor-usage` in `Home.razor`.
  - The deleted files' regions (`skill-class-factory-complete`, `skill-lifecycle-hooks`, `skill-class-execute`, `skill-interface-factory-complete`, `skill-entity-duality`, `skill-complex-aggregate`, `skill-static-execute-commands`, `skill-interface-factory-auth-impl`) are neither defined nor embedded outside `docs/todos`.
  - Ten `Samples/Skill/*.cs` files and `BlazorUsageSamples.cs`/`.razor` are deleted. `ChildEntitySamples.cs` had no markers left. `LazyLoadSamples.cs` is the only file left in `Samples/Skill`, and the Blazor `Samples/Skill` directory is gone.
- **2. Corrected notes (Must): honest as cited.**
  - `OrderCommandsFactory.g.cs:16-17` are the `SubmitOrder` (`Task<SubmitOutcome>`) and `CancelOrder` (`Task`) delegates, each with an optional `CancellationToken`. That matches `static-factory.md:52-54`.
  - `class-factory.md:87` names `IOrderFactory`.
  - `advanced-patterns.md:276`/`:295` match TaxRate, a positional `[Factory]` record with no setters.
  - `:357` matches `readme-client-usage` (`lineFactory.Create`, then `AddLine`).
  - `:270` names both OrderLine Fetch signatures shown in `orderline-child` and `orderline-duality`.
  - `interface-factory.md:270-272` names `CustomerDirectory` and `ICustomerDirectoryAuth`. The `TryAddTransient<ICustomerDirectoryAuth, CustomerDirectoryAuth>()` it quotes is generated at `ICustomerDirectoryFactory.g.cs:452`.
- **3. Embedded regions pinned (Must): not honest as cited for `skill-blazor-usage`.** See finding 1. The other new regions are honest:
  - `skill-value-object-factory`: `ValueObjectTests.TaxRate_LoadsFromTheServer_AndAppliesToMoney` asserts one remote call, `Region`, `Rate` and `Rate.Of`. `TaxRate_IsNull_ForAnUnknownRegion` asserts null on the client.
  - `skill-interface-factory-auth-authclass` and `-factory`: `InterfaceFactoryAuthorizationTests` covers on the client container:
    - `HasAccess` denial on `GetCustomer` and `CloseAccount`;
    - `CanAccessCustomer(Guid)` per-id denial;
    - `CheckReadAccess`'s message in the exception;
    - `Can{Method}` true/false, and `CanRenameCustomer` taking only the Guid;
    - `GetCustomer`, `RenameCustomer` and `CloseAccount` when allowed.
  - The reused regions keep their definitions; the three Design files gained only region pairs, so their 002–010 tests are untouched.
- **4. Meta (Must): honest.**
  - The logs and counts are as stated above.
  - `docs/attributes-reference.md`, `docs/service-injection.md` and the skill's `service-injection.md` change only in `snippet source` line ranges (`CustomerDirectory.cs` and `TaxRate.cs` shifted by the new markers).

**Sacred-test check: clean.** No file under a test project changed in `9aa6bd0..HEAD`. The Design diff is three `#region`/`#endregion` pairs in `Home.razor`, `CustomerDirectory.cs` and `TaxRate.cs`.

**Plan-introduced reachable paths:** none. The plan's code changes are region markers; everything else is pages and deletions.

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|
| 1 | Veto | Test Evidence 3 (and Plan Amendments) says `skill-blazor-usage`'s calls "local Create, Fetch, lineFactory.Create then AddLine, Save" are the ones `OrderUsageTests` makes on the client container. They are not. `OrderUsageTests.Client_OpensADraft_AddsALine_AndSaves` calls `OpenDraft`, `lineFactory.Create` + `AddLine`, and `Save`. It never calls `IOrderFactory.Create` or `Fetch`, so the map's named test does not pin two of the region's four methods. The behaviour is pinned elsewhere on the client container: `AggregateTests.Order_CreateAndEditLines_RunOnClientWithoutServerCall` (client `Create`, zero remote calls), `AggregateTests.Order_EditLinesFromClient_SavesExactlyThoseChanges` (client `Fetch(DraftOrderId)`, `lineFactory.Create`/`AddLine`, `Save`, refetch), and `ClientServerTestingTests.Order_SavesAndFetches_FromTheClientScope` (`Create` → `Save` → `Fetch`). Shape: correct the citation in the map row and the amendment. No test or code change. | AC-3 | **Amend** (fixed): Test Evidence 3 and the amendment cite `AggregateTests.Order_CreateAndEditLines_RunOnClientWithoutServerCall`, `Order_EditLinesFromClient_SavesExactlyThoseChanges` and `ClientServerTestingTests.Order_SavesAndFetches_FromTheClientScope` |
| 2 | Tech-debt | `ICustomerDirectory.ListContacts`, shown in the embedded `skill-interface-factory-auth-factory` region, is called by no Design test. This gap predates the plan. | — | **Punch** (fixed): `InterfaceFactory_ListContacts_ReturnsContacts_AndDeniesTheDeniedCustomer` added; Design 174 + 1 |
| 3 | Tech-debt | `skills/RemoteFactory/SKILL.md:121` still says snippet blocks "are extracted from the reference application". That is Todo 2's prose. | — | **Dismiss**: skill prose, Todo 2 (T-22) |

Could not verify:
- None.

Theoretical (not triaged):
- None.

## Round 2 — 2026-10-15 — APPROVED (0 veto-tier, 0 callouts)

**Reviewer:** test-reviewer, against `60cd0d1`, diffed from `50eb667`. Within `src/`, only four Design files changed: `Home.razor`, `Order.cs` and `OrderCommands.cs` (+2 lines each), and `InterfaceFactoryAuthorizationTests.cs` (+20). Nothing else under `src/` changed, so the round-1 main-solution and reference-app logs still apply.

Logs green, and they postdate the edits. The source edits are 15:54:05 (the test) and 15:59:04 (the three marker files). The logs are 16:00:04–16:00:07 and the commit 16:00:20.
- Design build (`015-design-build.log`): `Build succeeded`, 0 warnings, 0 errors. `Design.Domain`, `Design.Client.Blazor`, `Design.Server` and `Design.Tests` were built on both TFMs.
- Design tests (`015-design-test.log`): `Failed: 0, Passed: 174, Skipped: 1` on net9.0 and on net10.0. The only skip is `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull` (#104). The count rose by one, which is the new test.
- mdsnippets (`015-mdsnippets.log`): no error, exception or missing-snippet line; it ends `Finished`. It extracts `skill-blazor-inject` and `skill-blazor-usage` from `Home.razor`, `order-class-declaration` from `Order.cs` and `ordercommands-declaration` from `OrderCommands.cs`.

Round-1 findings:
- **Finding 1 (veto): closed.** Test Evidence 3 and the Plan Amendments now cite the three tests round 1 named for `skill-blazor-usage`: `AggregateTests.Order_CreateAndEditLines_RunOnClientWithoutServerCall`, `AggregateTests.Order_EditLinesFromClient_SavesExactlyThoseChanges` and `ClientServerTestingTests.Order_SavesAndFetches_FromTheClientScope`. Each was read in round 1 and pins the calls it is cited for. The `OrderUsageTests` citation is gone.
- **Finding 2 (tech-debt punch): honest.** `InterfaceFactory_ListContacts_ReturnsContacts_AndDeniesTheDeniedCustomer` resolves `ICustomerDirectoryFactory` from the client container, so the call goes through the remote proxy. It asserts three things:
  - `Count == 2`, which matches the two `_contacts` seed rows for `...0001`;
  - every contact's `CustomerId` is `AllowedId`. This `Assert.All` is not vacuous, because the count assertion comes first;
  - `NotAuthorizedException` for `DeniedId` (`...0042`). The generated `LocalListContactsCore` runs `CanAccessCustomer` on it.
  The diff only adds lines; no existing test in the file was altered.

Gate punches (markers only):
- `order-class-declaration` wraps the `[Factory]` attribute and the `internal partial class Order ...` line, and nothing else. Order is pinned by the existing `AggregateTests`, `ClientServerTestingTests` and `OrderUsageTests`.
- `ordercommands-declaration` wraps `[Factory]` and `public static partial class OrderCommands`, and nothing else. OrderCommands is pinned by `OperationsTests`: `SubmitOrder_ChangesTheOrderThroughItsRoot_AndReportsWhatItDid`, `CancelOrder_CancelsAStoredOrder_AndThrowsForAMissingOne` and `SubmitOrder_EmptyDraft_IsNotSubmittable_AndStaysADraft`, all on the client container.
- `skill-blazor-inject` is a pair of razor comments (`@* begin-snippet *@` / `@* end-snippet *@`) around the page's two `@inject` lines, `IOrderFactory` and `IOrderLineFactory`. Razor comments emit nothing. The lines are DI declarations, and `Design.Client.Blazor` compiles them on both TFMs.
- Each of the three names is defined once outside `docs/todos`. They are embedded in `class-factory.md`, `static-factory.md` and `setup.md`.

**Sacred-test check: clean.** The only test-file change in `50eb667..HEAD` is the 20-line addition above. Nothing changed under `src/Tests`.

**Plan-introduced reachable paths:** none. The punches add markers only.

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|
| — | — | None | — | |

Could not verify:
- None.

Theoretical (not triaged):
- None.
