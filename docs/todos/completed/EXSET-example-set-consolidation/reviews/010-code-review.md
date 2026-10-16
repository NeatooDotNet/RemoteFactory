# EXSET-010 code review — 2026-10-15

**Reviewer:** code-reviewer (per-plan, Step 5; `f623b48` against `d16ceba`). **Verdict:** APPROVED. No veto-tier findings.

There are six callouts, and every one bears on a Must criterion. More than five is itself the lead finding. The mechanics landed cleanly: the regions, the embeds, the deletions, the attributions and the behaviour-neutral Design edits. The callouts are about what the pages and the ledger *say*:
- four sentences on `service-injection.md` that do not match the code (1–4);
- two ledger problems: an outcome that claims a sentence the page does not have (5), and the 015 split, which the Owners column does not record (6).

**Build.** 0 errors in all three builds:
- Main solution: 2 warnings, both the OrderEntry WASM `NativeFileReference` notice (`010-build.log`).
- Design solution: 0 warnings (`010-design-build.log`).
- Reference app: 0 warnings (`010-refapp-build.log`), after the two deletions and the four marker strips.

**Tests.** 0 failed on both TFMs:
- Unit 852. Integration 657 + 7 skipped: `ShowcasePerformanceTests` (3), `RelayTimingTests` (2) and `FactoryReferenceIdentityTests.Ordinal_*` (2), each named in `010-test.log`.
- Design 173 + 1 skipped, `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull` (#104) (`010-design-test.log`).
- The counts are unchanged from 014. The plan adds markers and no tests, so that is expected.
- `010-mdsnippets.log` has no missing-snippet or error line.
- The logs (15:27:50–15:28:29) predate the commit (15:30:04). Every changed source's mtime is earlier than its build (15:22–15:27:24), and the pages were written at 15:28:28 by the mdsnippets run. I checked every `/src/Design` attribution on the two pages, the skill's `service-injection.md`, `advanced-patterns.md`, `static-factory.md` and the seven shared pages. Each opens on its `#region` line and closes on its `#endregion`.

The deliverable landed as scoped:

- **Direction and shape.**
  - The nine new regions are markers around Design code that tests already pin:
    - `CustomerPreferences.Fetch` (`OperationsTests.Fetch_StoredCustomer_ReturnsTheSavedValues`, `:152`);
    - `CtorInjectedEntity`'s constructor and `TokenFromContext` (`SerializationTests.CtorInjectedEntity_ServiceArrivesOnEachSideAfterRoundTrip`, `:547`);
    - `TaxRate.Fetch` (`ValueObjectTests.TaxRate_LoadsFromTheServer_AndAppliesToMoney`, `:68`);
    - the server's `ICurrentUser` (`DesignHttpHost` is `WebApplicationFactory<Program>` over Design.Server);
    - `StaticFactoryTests.Execute_ReturningTask_RunsOnServer`'s own setup;
    - the `Scopes()` configuration;
    - `IProductCatalog`, `ProductCatalog` and `InterfaceFactoryTests`' call.
  - The reused regions are 006–009's.
  - Library behaviour became prose, as the 007–014 precedent sets.
- **Behaviour-neutral Design edits.**
  - `CtorInjectionExample.cs`: `TokenFromContext` moved up beside the constructor, with its doc comment turned into a line comment. The member is unchanged.
  - `AllPatterns.cs`: two comments only (I-02).
  - `DesignClientServerContainers.Scopes`: `ServerServiceProvider` and `IMakeRemoteDelegateRequest` move from after the server- and local-only registrations to just after `ConfigureContainer`. Both positions follow `AddNeatooRemoteFactory`'s `MakeRemoteDelegateRequest` and precede `configureClient`. Nothing between them registers on `clientCollection` (`:257-278`), so the last registration still wins.
- **Checked against the library and the generator.** These hold:
  - **Where a `[Service]` is resolved.** The generated code resolves services from the factory's `ServiceProvider` at the call, never from a parameter (`CustomerPreferencesFactory.g.cs:112`). On a static delegate it resolves from the `cc` that resolved the delegate (`SupportRequestsFactory.g.cs:34-35`). A `[Service]` never appears on the generated method (`:19`). `IsService` is the same test for constructor and method parameters (`FactoryGenerator.Types.cs:880`), so "on a `[Create]` constructor, `[Service]` marks the DI parameters" holds.
  - **Constructor injection after deserialization.** A type with a required constructor parameter skips ordinal (`FactoryModelBuilder.cs:307`) and is registered transient (`:297-299`). `NeatooJsonTypeInfoResolver` then builds any DI-registered type with `GetRequiredService` (`:24-30`). That matches `:43` and `:64` for `CtorInjectedEntity` (`CtorInjectedEntityFactory.g.cs:66, :125`). The R-J warning (`:106`) is VisionAlignment's sentence verbatim.
  - **The request scope.** The middleware writes the correlation ID into the request's scoped `ICorrelationContext` (`WebApplicationExtensions.cs:33-51`; `AddRemoteFactoryServices.cs:109`). The endpoint resolves `HandleRemoteDelegateRequest` from `RequestServices` (`:56`). Nothing in either library calls `CreateScope` (grep). `Immediate` handlers run in the caller's provider (`FactoryEventsDispatcher.cs:15-16`). So ":155" holds.
  - **`RegisterMatchingName`.** Same assembly, `IFoo` → `Foo` by the full name with the `I` dropped, `TryAddTransient` for the interface and the class (`AddRemoteFactoryServices.cs:256-276`). That matches `:159` and `interface-factory.md:100` (both types are in `Design.Domain.FactoryPatterns`).
  - **Interface factory registrations.** Remote: `AddScoped` of `IProductCatalog` and `IProductCatalogFactory` to the proxy. Server: the factory and one delegate per method, and no implementation. Logical: nothing (`IProductCatalogFactory.g.cs:117-154`). `IProductCatalogFactory : IProductCatalog` (`:14`). Remote's `AddScoped` beats the convention's `TryAdd` in either order, so "the client registers nothing" holds even for a client that runs the convention.
  - **NF0106.** It is an Error (`DiagnosticDescriptors.cs:64-71`), raised for an operation attribute the user wrote on a method declared on an interface (`FactoryGenerator.Transform.cs:139-162`). The WRONG block's comment is the descriptor's title.
  - **`partial`.** A class factory has no diagnostic for it, and ordinal needs `IsPartial` (`FactoryModelBuilder.cs:304-309`). The static factory's NF0101 is at `:67-80`. That matches `interface-factory.md:168`.
  - **Interface authorization order.** Each method's `Local*Core` resolves the auth class, runs every matching auth method, and throws `NotAuthorizedException` on the first refusal, all before it resolves `ICustomerDirectory` and calls it (`ICustomerDirectoryFactory.g.cs:89-113`). `Can*` is on `ICustomerDirectoryFactory` (`:16-19`). The `HttpRequestException` outcome agrees with `authorization.md:275-276`.
- **Rules.**
  - Both pages have no fence outside a snippet pair except three WRONG blocks and one plain-text exception message (`service-injection.md:188-190`, not C#).
  - Each of the nine new keys and the four reused ones is defined once, under `src/Design` (grep of `#region` across the repository).
  - `clientserver-container-usage`, `service-injection-multiple`, `-lifetimes`, `-scoped`, `-client`, `-matching-name`, `-serviceprovider`, `serialization-caveat-broken`/`-fixed` and `skill-child-entity-no-remote` are neither defined nor embedded. `service-injection-httpcontext-aspnetcore` (reference app) is a different key, and nothing embeds it.
  - No stale path to the two deleted files outside `docs/todos` and ignored `Generated/` output.
  - D12 (`interface-factory.md:5`), D14 (no `[Remote, Create]`), R-C (`orderline-child`), R-H (`:5`, `:178`), R-J (`:106`) and R-M: none contradicted. No LazyLoad.
- **Links.** Every outbound link resolves: `factory-operations.md#method-parameters` and `#execute-operation`, `decision-guide.md#which-factory-pattern`, `service-injection.md#registermatchingname`, `authorization.md#authorization-failures`, `serialization.md`, and the Next Steps pages. No other page links into an anchor on either page (grep).
- **Ledger.** C-01, C-15, C-18, C-19, C-20, C-28, I-02, T-06, T-22, T-24, U-06, U-07 and U-08 match the diff. T-02 claims a sentence the page lacks (callout 5). C-11, C-14, I-05, U-01 and U-02 are right about 010's pages, but the Owners column does not record the split (callout 6).

**Sacred tests.** `InterfaceFactoryTests.cs` and `StaticFactoryTests.cs` gain region markers only. `TestingSamples.cs` (reference app) loses markers only. `ClientServerContainerSamples.cs` is deleted with its one `[Fact]`, `Local_Create_WorksWithoutSerialization`. That is a reference-app sample, deleted under the plan's Scope ("delete the reference-app sources whose every region retires") and AC-6, and recorded in Plan Amendments. Design covers Logical mode (`LogicalModeTests`).

| # | Callout | Affects | Triage |
|---|---|---|---|
| 1 | **"Design names its server-only implementations off the convention ..., so a client that runs it cannot pick them up." Only two of them are, and Design's own test client picks up the rest.**<br>• `service-injection.md:161`.<br>• `ServerServices.cs:171-173` names only `InMemoryTaxTable` and `InMemorySupportLog` as deliberately off the convention. `:194-195` says the convention covers `INotificationService` → `NotificationService` and `IProductCatalog` → `ProductCatalog`. `ExampleService`, `PhaseAuditService` and `CustomerDirectory` are on the convention too. `AllPatterns.cs:557` calls `IExampleService` and `INotificationService` server-side.<br>• `DesignClientServerContainers.ConfigureContainer` runs `RegisterMatchingName` on all three containers, the client's included (`:323`). The client collection registers neither service first (`:263-278` are server and local only). So the test client resolves `INotificationService` → `NotificationService` and `IExampleService` → `ExampleService`. Design.Client.Blazor does not run the convention at all (`Program.cs`, grep).<br>• Confidence High.<br>• Reachable by: a reader who runs `RegisterMatchingName` on the client, as Design's harness does, trusting the sentence. A bare `[Execute]` that takes `INotificationService` then runs on the client with the server's implementation instead of failing (the failure `AllPatterns.cs:494-498` describes). | AC-4 · Must (plan Acceptance 2) | **Amend** (fixed): the convention registers in whichever container calls it; server-only implementations are named off it and registered explicitly, naming both Design examples |
| 2 | **"A method without `[Remote]` runs on the tier that calls it, and its services come from that tier's container." The generator also sends a method with `[AspAuthorize]`, or a `[Remote]` auth method, to the server.**<br>• `service-injection.md:39`.<br>• `isRemote = method.IsRemote \|\| AuthMethodInfos.Any(m => m.IsRemote) \|\| AspAuthorizeCalls.Any()` (`FactoryModelBuilder.cs:351, :402, :477, :545`). `authorization.md:201` says so.<br>• This is the 014 callout 4 finding, fixed on `attributes-reference.md`, here restated on the page whose subject is which container a service comes from.<br>• Confidence High, from the source. The 014 review traced it.<br>• Reachable by: a reader who puts `[AspAuthorize]` on a bare method and expects its `[Service]` from the client's container. It runs on the server and resolves from the server's, or fails there if only the client registers the service. | AC-4 · Must | **Amend** (fixed): the `[AspAuthorize]` and `[Remote]` auth-method exception added, linking `authorization.md#placement-on-execute` |
| 3 | **"The generated `ReportProblem` delegate takes `(int orderId, string description)`." It also takes `CancellationToken cancellationToken = default`.**<br>• `service-injection.md:131`. The generated declaration is `delegate Task<string> ReportProblem(int orderId, string description, CancellationToken cancellationToken = default)` (`SupportRequestsFactory.g.cs:15`). The page's own `:110` says the caller sees the token.<br>• Confidence High.<br>• Reachable by: a reader who writes a test double for the delegate as a two-parameter lambda (`(id, text) => ...`). It does not compile against the three-parameter delegate. | AC-4 · Must | **Amend** (fixed): the delegate's signature includes `CancellationToken cancellationToken = default` |
| 4 | **"`DesignClientServerContainers.Scopes` takes a configuration for each container." It takes one for the server and one for the client. The local container has none.**<br>• `service-injection.md:199`. The parameters are `configureServer`, `configureClient` and `format` (`DesignClientServerContainers.cs:240-243, :291-292`).<br>• Confidence High. The consequence is a compile error, not a wrong result.<br>• Reachable by: a reader who wants to replace a service in the local (Logical) container and looks for the third argument. | AC-4 · Must | **Amend** (fixed): "an optional configuration for the server container and one for the client container" |
| 5 | **The T-02 outcome says interface-factory states "that NF0101 is the static-class error". The page does not mention NF0101.**<br>• `audit-ledger.md:57`. `grep NF0101` of `interface-factory.md` and `service-injection.md` finds nothing. The page's `partial` row (`:168`) covers the class factory only. The audit's fix is "Class: silently falls back to named JSON. Static class: NF0101" (`audit-remotefactory.md:100`).<br>• Confidence High.<br>• Reachable by: the close-out audit's AC-4 trace, which reads the ledger as the record of what fixed each finding. | AC-4 · Must (T-02) | **Amend** (fixed): T-02's outcome no longer claims an NF0101 statement |
| 6 | **The 015 split is in the 010 outcomes but not in the Owners column. 015's finding list also omits C-02 and C-20, whose skill embeds it now owns.**<br>• The 010 outcomes for C-11, C-14 and I-05 say the skill's share "is EXSET-015's" (`audit-ledger.md:21, :24, :45`). Their Owners columns do not list 015, nor do those of C-15, C-19 and U-02, which 015's Scope names. The ledger's rule is that a finding "is closed by the last of" its owners (`:5`). C-14's only published site is `skill-value-object-factory` (`audit-remotefactory.md:53`), and its owners end at 012.<br>• 015's Scope moves `skill-entity-duality` and `skill-complex-aggregate` (`015-skill-embeds.md:18`). The audit cites those two embeds for C-02 (`ref/advanced-patterns.md:213-214`, the `public void FetchAsChild` under the "internal" prose) and C-20 (`ref/advanced-patterns.md:284, 338`) (`audit-remotefactory.md:41, :59`). 015 does not list either finding, and no 010 outcome on C-02 or C-20 hands them on.<br>• Confidence Medium-High. The rows' Owners column is the orchestrator's to define. The audit sites are read from the audit.<br>• Reachable by: the close-out AC-4 trace. It could close C-14 at 012, or C-02 and C-20 at T2, while the embeds that carry them are still the reference app's. | AC-4 · Must | **Amend** (fixed): 015 added to the Owners of C-02, C-11, C-14, C-15, C-19, C-20, I-05 and U-02 |

## Could not verify

- **The probe compile.** Its CS0101/CS0111 outcome for `[Factory]` on both `IProductCatalog` and `ProductCatalog` is not in the repository. It agrees with the naming: a class factory for `CustomerPreferences` is `CustomerPreferencesFactory`/`ICustomerPreferencesFactory`, and the interface factory for `IProductCatalog` is `ProductCatalogFactory`/`IProductCatalogFactory` (`IProductCatalogFactory.g.cs:14-18`). I compiled nothing.
- **The generated `.g.cs` files** are ignored output on disk (`.gitignore:405`), timestamped 15:27:52. I read them as the logged build's.
- **The test log is a summary.** It does not name passing tests, so the pinning tests are confirmed by source, not by the log.

## Theoretical (not triaged)

- **Where `Can*` runs.** It runs on the calling tier: the Remote constructor binds `CanGetCustomerProperty = LocalCanGetCustomer` (`ICustomerDirectoryFactory.g.cs:66-69`). It resolves `ICustomerDirectoryAuth` from the client's container, behind an `IsServerRuntime` guard (`:251-252`), so a client with the trimming switch set throws. `interface-factory.md:158` does not say where `Can*` runs.
- **Logical mode registers no `IProductCatalogFactory`** (`IProductCatalogFactory.g.cs:117-154`). `interface-factory.md:126` covers only `IProductCatalog`.
- **The lifetimes differ.** `interface-factory.md:100` gives the "explicit form" as `AddScoped`, where the convention it replaces registers Transient.
- **The trimming guard.** `service-injection.md:186-190`: on a client with the trimming switch set, an `internal` non-`[Remote]` method throws "Server-only method called in non-server runtime." before any DI resolution.
- **All-default constructors.** `service-injection.md:43`, "by DI ... again when it is deserialized", holds for a constructor with a required parameter. A constructor whose parameters all have defaults keeps ordinal, which builds with `new T { ... }` (`CustomerPreferences.Ordinal.g.cs:126`), so its service is the default.
- **Unrecorded retirements.** U-01's and C-02's `service-injection.md` sites (`serialization-caveat-fixed`'s "Works on client!", `audit-remotefactory.md:41, :135`) retired with `SerializationCaveatSamples.cs`. The 010 outcomes do not record that.
- **Stale generated files.** `OrderLineListBroken*`/`OrderLineListFixed*` remain in the reference app's ignored `Generated/` folder.

## Read report

- **Read beyond the brief:**
  - The generated `IProductCatalogFactory.g.cs`, `ICustomerDirectoryFactory.g.cs` (`:1-150`, `:243-280`), `SupportRequestsFactory.g.cs`, `CustomerPreferencesFactory.g.cs` (`:19-126`, `:364-378`), `CtorInjectedEntityFactory.g.cs` and `ExampleCommandsFactory.g.cs` (registrations), and `CustomerPreferences.Ordinal.g.cs:126`
  - `NeatooRuntime.cs`; `NeatooJsonTypeInfoResolver.cs:11-38`; `FactoryEventsDispatcher.cs` (grep); `HandleRemoteDelegateRequest.cs:51-90`
  - `FactoryGenerator.Transform.cs:95-170`; `FactoryModelBuilder.cs:55-80, :280-330`; `FactoryGenerator.Types.cs:880`
  - `DesignClientServerContainers.cs:225-345`; `AllPatterns.cs:475-575`; `FactoryEventPhasesPattern.cs:95-125`; `Design.Client.Blazor/Program.cs:70-100`; `ServerServices.cs:140-215`
  - `authorization.md:201, :266-291`; the old pages at `d16ceba`; the deleted reference-app files at `d16ceba`; the skill's `service-injection.md` and `advanced-patterns.md:465-490`
  - `todo.md` (ACs, Punchlist, Dismissed, Discovery Log); `015-skill-embeds.md`
- **Named but unused:**
  - `docs/VisionAlignment.md` beyond rows D12, D14, R-C, R-H, R-J and R-M
  - `.claude/rules/design-snippets.md` beyond its region and fence rules
