# EXSET-010 test review — 2026-10-15

## Round 1 — APPROVED (0 veto-tier, 0 callouts)

**Reviewer:** test-reviewer, against `f623b48`, diffed from `d16ceba`. Logs green:
- main build (`010-build.log`): 0 errors, 2 warnings. Both are the known OrderEntry WASM native-reference notice.
- Design build (`010-design-build.log`): 0 errors, 0 warnings. `Design.Domain`, `Design.Server` and `Design.Tests` were built on both TFMs.
- reference app (`010-refapp-build.log`): 0 errors, 0 warnings, all six EmployeeManagement projects on both TFMs.
- tests (`010-test.log`, `010-design-test.log`): unit 852, integration 657 + 7 skips, Design 173 + 1 skip, each on both TFMs. No failures. The Design skip is `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull` (#104). The integration skips are the known three `ShowcasePerformanceTests`, two `RelayTimingTests` and two `FactoryReferenceIdentityTests` (#133). The counts are unchanged, which is what markers, a moved property and a reordered pair of registrations should give.
- mdsnippets (`010-mdsnippets.log`): no error, exception or missing-snippet line; it ends `Finished`.

Every changed `src/` file is older than the builds: the last Design edit is 15:24:47, the reference-app strips are 15:27:24, and the builds start at 15:27:50. The pages are 15:28:28, the mdsnippets run. The commit is 15:30.

Notes per bullet:
- **1. Pages and sources (Must): honest.**
  - `service-injection.md` has 9 snippet pairs. Its only fences outside a pair are one WRONG C# block (`:91-100`) and an untagged exception-text block (`:188-190`), which is not C#.
  - `interface-factory.md` has 3 snippet pairs. Its only fences outside a pair are two WRONG C# blocks (`:134-142`, `:150-154`). That makes the three WRONG blocks the map names.
  - Every `snippet source` link on both pages, and on the skill's `service-injection.md`, is under `src/Design`.
  - Each of the nine new regions is defined once, in `src/Design`. So are the four reused ones (`aspnetcore-correlation-id`, `aspnetcore-service-registration`, `aspnetcore-testing`, `orderline-child`).
  - No retired name is defined or embedded outside `docs/todos`: `service-injection-lifetimes`, `-matching-name`, `-multiple`, `-scoped`, `-client`, `-mixed`, `-serviceprovider`, `clientserver-container-usage`, `skill-child-entity-no-remote`.
  - Both deleted files are gone. The four stripped files lost only `#region`/`#endregion` lines; no other line changed under `src/docs`.
- **2. Statements match the library (Must): honest as cited.**
  - The generated `ExampleCommandsFactory.g.cs` resolves services with `cc.GetRequiredService<...>` from the running container. Line 25 is a non-`[Remote]` method's `ITextScorer`, and lines 50 and 62 are the server path's `INotificationService`.
  - `AddRemoteFactoryServices.cs:256-276` is `RegisterMatchingName`, using `TryAddTransient(i, t)` and `TryAddTransient(t)`.
  - `WebApplicationExtensions.cs:56` resolves the handler from `httpContext.RequestServices`, the request scope.
  - `IProductCatalogFactory.g.cs:117-150` registers the proxy for `IProductCatalog` and `IProductCatalogFactory` under Remote, and only delegates under Server. It registers nothing under Logical, as the page says.
  - `DiagnosticDescriptors.cs:65` is NF0106, an Error.
  - `FactoryModelBuilder.cs:68` is NF0101, on static factories only, and `:309` gates the ordinal converter on `IsPartial`. Together they match the page's `partial` row.
  - The probe compile for `[Factory]` on the implementation (CS0101, CS0111) is recorded in Plan Amendments and in ledger T-24.
- **3. Embedded regions pinned (Must): honest.** Each new region's code is exercised on the real containers by the test the map names:
  - `service-injection-basic`: `OperationsTests.Fetch_StoredCustomer_ReturnsTheSavedValues` saves and then fetches through the client factory, asserting `IsNew` false and the stored values. `Fetch_UnknownCustomer_ReturnsNull` asserts null.
  - `service-injection-constructor`: `SerializationTests.CtorInjectedEntity_ServiceArrivesOnEachSideAfterRoundTrip` registers distinct server and client tokens and asserts `TokenFromContext == "client-token"` after the round trip. That is the region's own property.
  - `service-injection-server-only`: `ValueObjectTests.TaxRate_LoadsFromTheServer_AndAppliesToMoney` asserts one remote call, then the region and the rate.
  - `service-injection-httpcontext`: `AuthorizeFactoryHttpTests` runs Design.Server's real `Program`. The host replaces neither `ICurrentUser` nor the region's registration, and wraps the real `AuthorizedOrderAuth` over it. `Fetch_UserWithoutAFetchRole_GetsNull` asserts Manager null and Support non-null, so the request's user does reach `ICurrentUser`.
  - `service-injection-testing`: the region is the arrange of `StaticFactoryTests.Execute_ReturningTask_RunsOnServer`, which asserts `Assert.Single(recorder.Sent)` with the recipient and message.
  - `clientserver-container-setup`: it is `Scopes()` itself, which every harness test goes through.
  - `interface-factory-contract` and `-implementation`: `InterfaceFactoryTests` covers all three contract methods through the client proxy, the null result and Logical mode (`IsType<ProductCatalog>`).
  - `interface-factory-usage`: the region is the act of `InterfaceFactory_SearchAsync_ReturnsDataFromServer`, which asserts 3 results and `RemoteCallCount == 1`.
  - The reused regions keep their definitions and their tests from 002–009.
- **4. Meta (Must): honest.**
  - The logs and counts are as stated above.
  - The eight shared pages (`aspnetcore-integration`, `attributes-reference`, `factory-modes`, `factory-operations`, `interfaces-reference`, `save-operation`, `serialization`, and the skill's `static-factory.md`) change only in `snippet source` lines.
  - `advanced-patterns.md` changes only in the `clientserver-container-usage` → `aspnetcore-testing` embed.

**Sacred-test check: clean.**
- `StaticFactoryTests.cs` and `InterfaceFactoryTests.cs` gain only a `#region`/`#endregion` pair each. No assertion, setup or call changed.
- `DesignClientServerContainers.Scopes` moves `AddScoped<ServerServiceProvider>()` and `AddScoped<IMakeRemoteDelegateRequest, MakeSerializedServerStandinDelegateRequest>()` up, from after the local registrations to straight after the three `ConfigureContainer` calls. Only server and local registrations sat between the old and new positions. So the client collection's order is unchanged: `ConfigureContainer`, then the two moved lines, then the shared `TestUser`, then `configureClient`. The stand-in still overrides the library's `IMakeRemoteDelegateRequest`, and a test's `configureClient` still wins. Beyond that the change is the region pair and comment text.

**Plan-introduced reachable paths:** none. The only production-side code change is `CtorInjectedEntity.TokenFromContext`, moved next to the constructor with the same body. Everything else is markers and comments (`AllPatterns.cs`, I-02).

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|
| 1 | Tech-debt | NF0106's `description` (`src/Generator/DiagnosticDescriptors.cs:71`) still says "causes duplicate generation". That is the phrase T-24 removed from `interface-factory.md`. | — | **Dismiss**: the description gives the reason the attribute is an error; T-24 was the page naming no diagnostic, and it now names NF0106 |
| 2 | Tech-debt | `#region service-injection-httpcontext-aspnetcore` (`src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCoreSamples.cs:30`) is still defined, and nothing outside `docs/todos` embeds it. It is a `service-injection-*` key, but not a duplicate of Design's `service-injection-httpcontext`. | — | **Dismiss**: nothing embeds it, and EXSET-012 deletes the reference app |

Could not verify:
- The probe compile itself: no log or source was kept. The CS0101/CS0111 claim rests on the Plan Amendment and ledger T-24. It is consistent with the generated names: the interface factory for `IProductCatalog` is `IProductCatalogFactory`/`ProductCatalogFactory`, which a class factory on `ProductCatalog` would also take.

Theoretical (not triaged):
- None.
