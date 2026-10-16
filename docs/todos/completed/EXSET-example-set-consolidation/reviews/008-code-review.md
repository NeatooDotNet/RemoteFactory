# EXSET-008 code review — 2026-10-15

**Reviewer:** code-reviewer (per-plan, Step 5; `d926ae7` against `2d19668`). **Verdict:** APPROVED. No veto-tier findings.

**Build.** 0 errors in all three builds:
- Main solution: 2 warnings, both the OrderEntry WASM `NativeFileReference` notice (`008-build.log`).
- Design solution: 0 warnings (`008-design-build.log`).
- Reference app: 0 warnings (`008-refapp-build.log`), after the 14 deletions.

**Tests.** 0 failed on both TFMs: unit 839, integration 646 + 7 skipped, Design 166 + 1 skipped.
- The 7 integration skips are the known ones: three `ShowcasePerformanceTests`, two `RelayTimingTests` and the two `FactoryReferenceIdentityTests.Ordinal_*` cases (#133), each named in `008-test.log` on both TFMs.
- The Design skip is `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull`, the #104 known bug, on both TFMs (`008-design-test.log`).
- Design's total is 167, up by the four tests in `SaveTests`, so the run exercised this change.
- `008-mdsnippets.log` shows no missing snippet and no error. The logs (12:49–12:50) predate the commit (12:51:45). Every attribution line on both pages matches the current source lines: `Order.cs#L119-L122`, `SaveTests.cs#L31-L41` and `#L53-L74`.

The deliverable landed as scoped:

- **Direction and shape.** Both pages now teach from Order, CustomerPreferences and `SaveTests`. Design gains only the plan's one region and one test file. The library signatures became prose, following the I-12 precedent. The `operations-*` regions are reused unchanged, so the factory-operations, README, getting-started and the-problem pages change only in `<sup>` lines.
- **Save routing, checked against the generator.** These hold:
  - **`IsDeleted` is checked first.** New-and-deleted returns the default, and a deleted stored object goes to Delete (`ClassFactoryRenderer.cs:1133-1144`). New goes to Insert, and anything else to Update (`:1146-1170`). A missing method throws `NotImplementedException` in its branch, so with no `[Delete]` a new-and-deleted object throws too (`:1140-1143`). At `d926ae7` no test pins the missing-Delete throw. The committed `PartialSaveTests` pins only a missing Update or Insert (`:189-197`, `:282-290`, `:351-359`). That is test review finding 1, and the working-tree tests that pin it are covered under Could not verify.
  - **After a Delete, Save returns the object** (`:1138`). The table says so, which settles 007 callout 1.
  - **A `bool` write that returns `false` gives `default`** without authorization, and `new Authorized<T>()` with it (`:936-970`). The Save wrapper turns `!HasAccess` into `NotAuthorizedException` (`:1083-1092`), and `TrySave` returns the refusal (`:1097-1103`).
  - **#104.** The page's two cases (new-and-deleted, and a `bool` false) are exactly the issue's two conditions. #104 is open, labelled `bug`. "Although no check refused" holds: for new-and-deleted no check runs, and for a `bool` false the checks ran and passed (`:899`).
- **`IFactorySave<T>`.** It is implemented explicitly and registered keyed on the class (`:164-166`, `:1569-1572`). `Save` returns `Task<IFactorySaveMeta?>` (`:1245`). Both `CanSave` overloads return `Authorized(true)` when no check applies (`:1279-1321`). The page's member list matches `IFactorySave.cs`.
- **409.** Nothing in `src/RemoteFactory` or `src/RemoteFactory.AspNetCore` names 409 or `Conflict`. The handler rethrows every exception except `AspForbidException` (`HandleRemoteDelegateRequest.cs:240-258`). That makes "a 500, as `HttpRequestException`" right, and it agrees with `aspnetcore-integration.md:152-159`.
- **Hook timing.** It agrees with `factory-operations.md:457-464` bullet for bullet:
  - the start hook has run when the method throws;
  - the cancelled hook runs in place of the complete hook;
  - the async hooks run only around `Task`-returning instance operations;
  - a constructor or static operation gets only `IFactoryOnComplete`;
  - a class-level `[Execute]` gets no hooks;
  - a `bool` false skips the complete hook.

  The generator agrees (`ClassFactoryRenderer.cs:724-787`). The hook signatures match `IFactoryOnStart.cs`, `IFactoryOnComplete.cs` and `IFactoryOnCancelled.cs`.
- **Other interfaces.**
  - **`IAspAuthorize`** matches `IAspAuthorize.cs` and `AspAuthorize.cs:66-111`. `AddNeatooAspNetCore` registers it with `TryAddScoped` (`ServiceCollectionExtensions.cs:30`).
  - **The ordinal members** match `IOrdinalSerializable.cs` and `IOrdinalConverterProvider.cs`.
  - **Ordinal order** is own properties first, then by `StringComparer.Ordinal`. That matches `FactoryGenerator.Types.cs:416-421` and `serialization.md:26`.
  - **The phase coordinator:** EventIds 3008 and 9007 exist (`Log.cs:147`, `:540`). The no-op relay is registered with `TryAddSingleton` in Remote mode only (`AddRemoteFactoryServices.cs:118-130`). The `DrainAsync` prose matches `IFactoryEventPhaseCoordinator.cs`.
- **Rulings.**
  - D15: the intro calls Save "one use of the client/server seam".
  - R-D: the cascade follows `OrderLine.cs:313-340` step for step.
  - R-E: the Validation section.
  - R-K: no LazyLoad on either page, and `IFactoryCore` is gone.
  - R-M: public `get; set;`, with the private-setter and `[JsonInclude]` guidance gone.
  - R-F holds except for callout 3.
- **`.claude/rules/design-snippets.md`.**
  - Neither page has a fence outside a `snippet:`/`endSnippet` pair.
  - All nine region names on the two pages are defined once, all under `src/Design`.
  - Of the 31 regions in the deleted files, 28 are defined and embedded nowhere outside `docs/todos`/`docs/plans`. The three that migrated (`save-ifactorysavemeta`, `save-usage`, `save-testing`) kept their names and are defined only in Design.
- **Links.**
  - Every outbound link on both pages resolves, including `factory-events.md#dispatch-phases` (`:133`), `serialization.md#what-the-ordinal-array-holds`, `aspnetcore-integration.md#when-a-call-fails`, and `factory-operations.md#collection-factories`, `#combining-operations` and `#lifecycle-hooks`.
  - Inbound links resolve too: `attributes-reference.md:249` (`#ifactoryeventrelay`), `release-notes/v1.1.0.md:221` (`#ifactoryevents`) and `trimming.md:451` (`#ifactorysavemeta`).
  - The one exception is `lazyload.md:262`'s `#ilazyloadfactory`, the known EXSET-011 case.
- **Ledger.** The 11 rows' outcomes match the diff:
  - C-12's "validating hooks retired" holds: the old page's throwing hooks were at `interfaces-reference.md:33-67` at `2d19668`.
  - C-21's fields holds (`LifecycleHooksSamples.cs:61`, `:156` at `2d19668`).
  - T-11 records only the 409 half, which leaves the 401/403 half to 009, as owned.
  - T-12's citation of `PartialSaveTests` covers the missing Update and Insert throws only, until the working-tree missing-Delete tests are committed (test review finding 1).
- **Shared pages.** README, getting-started, the-problem and factory-operations change only in `<sup>` lines. `trimming.md` changes one sentence, as Amendments records.

**Sacred tests.** No existing test was modified. `SaveTests.cs` is new. The only tests deleted are the three `[Fact]`s in the reference app's `SaveRoutingTestSample.cs`, which the plan's Scope names among the 14 files of the retired app (AC-6). The test review (`008-test-review.md`, Round 1, written during this review) is not re-litigated here.

| # | Callout | Affects | Triage |
|---|---|---|---|
| 1 | **The events contracts are narrower than the library's in two places.**<br>• `interfaces-reference.md:146`: `Raise<T>` runs handlers "in order, awaited". The source says "run one after another in unspecified order" (`IFactoryEvents.cs:13-14`, `RaiseOptions.cs:9-10`). `factory-events.md:25` adds "Callers must not rely on a specific ordering".<br>• `:152`: "`Relay` is called once per `[Remote]` call". The contract is once per remote round-trip, which also covers a client-initiated `Raise`, and that `Raise` gets its own event back in the batch. `Relay` is not called when the batch fails to deserialize (`IFactoryEventRelay.cs:15-23`; `factory-events.md:286`, EventId 3009).<br>• Confidence High. Reachable by:<br>  – a reader who registers two handlers for one event and relies on their order;<br>  – a client that raises an event, also handles it locally, and gets it a second time from `Relay`. | AC-4 · Must | **Punch** — `Raise<T>` runs handlers in no specified order; `Relay` is once per remote round trip, a client `Raise` included, and a batch that fails to deserialize is logged (3009) and not relayed. |
| 2 | **The concurrency prose leaves a mismatch outcome to inference, and the inference is a 500.**<br>• `save-operation.md:174`: "have Update compare it", followed at once by "An exception thrown by a write reaches a remote caller as an `HttpRequestException` for a 500 response".<br>• Over HTTP, the exception's type and message do not reach the client (`aspnetcore-integration.md:159`). So a conflict looks like any server failure.<br>• R-E: "a check the user can recover from is a verdict the UI reads before acting (a `CanX` or a result), never a throw in a factory method". Whether a stale-version conflict is user-recoverable is a reading of R-E that the page makes silently. The plan's Notes record only that the section has no Design example.<br>• Confidence Medium. Reachable by: two users who edit the same order. The second save gets a 500 that the screen cannot tell apart from a bug.<br>• One option: say what Update does on a mismatch. A `bool` Update that returns `false` is already an outcome on this page (Save returns `null`), but with authorization it is #104. | AC-4 · Must (T-11; R-E) | **Punch** — a stale version is a result: Update returns `false` and Save returns `null`, with #104 named for the authorized case; a thrown write is a 500 whose type stays on the server. |
| 3 | **"A value object ... implements none of them" is false for the service-needing value object that R-F allows and Design has.**<br>• `interfaces-reference.md:128`: "A value object is a plain `record`, needs no `[Factory]` to cross the wire, and implements none of them."<br>• R-F: a value object "takes `[Factory]` when its construction needs a service". Design's `TaxRate` is that case: `[Factory] public partial record TaxRate(string Region, Percentage Rate)` (`TaxRate.cs:33-34`).<br>• A partial record with a primary constructor skips the service-instantiation check (`FactoryGenerator.Types.cs:294-299`) and gets the ordinal converter (`FactoryModelBuilder.cs:307-313`). `OrdinalRenderer.cs:255` then emits all three interfaces on it.<br>• Confidence High on the code reading; not compiled output. Reachable by: a reader who reorders or renames `TaxRate`'s properties because the page says it has no ordinal array. A client and server on different builds then disagree about the array. | AC-4 · Must (R-F) | **Punch** — the page says a `[Factory]` value object such as `TaxRate` gets all three interfaces. |
| 4 | **Design's Order remark still says "When an operation throws, no hook runs".**<br>• `Order.cs:179`, in the remarks directly above the `operations-lifecycle-oncomplete` region (outside it). This plan's `interfaces-reference.md:62` and 007's `factory-operations.md:464` both say that the start hook has already run.<br>• The start hooks run inside the `try`, before the body (`ClassFactoryRenderer.cs:916-920`). 007 callout 3 named this sentence, and the punch fixed the page but not this remark. It is outside 008's diff.<br>• Confidence High. Reachable by: a reader of Design, the stated source of truth, who pairs work in the start hook with cleanup they expect to be skipped on failure. | AC-4 · Must (C-11) | **Punch** — `Order.cs`'s remark now says the start hook has run and the complete hook does not. |
| 5 | **"`Save` returns `Task` of the object's interface, nullable" is stated for every Save. It holds only with a `[Delete]` or a `bool` write.**<br>• `save-operation.md:145`.<br>• `IsNullable` is `methods.Any(m => m.Operation == Delete \|\| m.IsNullable)` (`FactoryModelBuilder.cs:710`). The page's own partial example, `CustomerPreferences.SaveUpsert`, returns non-nullable `Task<ICustomerPreferences>`. `PartialSaveTests.InsertUpdateOnly_Save_IsNotNullable` (`:61-73`) pins the non-nullable shape.<br>• `IsTask` is `isRemote \|\| methods.Any(m => m.IsTask)` (`:708`). A local save with synchronous writes returns the object directly, not a `Task` (`PartialSaveTests.cs:69`).<br>• Confidence High. Reachable by: a reader who writes a local class with synchronous writes and `await`s `Save`; the call does not compile. For CustomerPreferences the cost is only a needless null check.<br>• One option: "nullable when the class has a Delete or a `bool` write; `Task` when a write is `[Remote]` or returns `Task`". | AC-4 · Must (T-12) | **Punch** — the return shape: `Task` when a write is `[Remote]` or returns `Task`, nullable with a `[Delete]` or `bool` write; `SaveUpsert` returns `Task<ICustomerPreferences>`. |

## Could not verify

- Design does not write its generated files to disk. Every generated-shape claim above, including callout 3's ordinal interfaces on `TaxRate`, comes from generator source and the `PartialSaveTests` pins, not from `.g.cs` output.
- The working tree has changes from 12:57, after every 008 log (12:49–12:50) and after the commit (12:51:45). They appear to be the test review's punches:
  - `PartialSaveTests.cs` gains `InsertOnly_Save_ThrowsNotImplemented_WhenDeleted` and `..._WhenNewAndDeleted`. Unit tests should then number 841 on each TFM.
  - In `SaveTests.cs`, the `save-usage` region's Delete line becomes `order = (await orderFactory.Save(order))!;`. That changes an embedded region, so both pages render the old line until mdsnippets runs again.

  None of this has been built or tested. Before commit it needs both solutions built and tested and an mdsnippets run.

## Theoretical (not triaged)

Three were taken anyway (2026-10-15): `trimming.md` drops the ruling code, "maps no exception from a write to a status of its own" (a forbidding `[AspAuthorize]` is a 403), and "a property with a private setter does not cross it". The rest are left.

- `IFactorySave<T>` exists only when a Save takes no value parameter beyond the target (`HasDefaultSave`, `FactoryModelBuilder.cs:641-647`). It is not on every `IFactorySaveMeta` class factory, as `interfaces-reference.md:89` says.
- "Maps no exception to an HTTP status" (`save-operation.md:174`) is broader than T-11. `AspAuthorize` with `forbid` calls `ForbidAsync` (403) and then throws `AspForbidException`, and the handler answers that exception with an empty body (`AspAuthorize.cs:87-104`, `HandleRemoteDelegateRequest.cs:246-251`).
- The #104 skipped test asserts `null`. The issue's suggested direction leaves `null` against `ArgumentException` open, so the test pins one of two fixes that #104 has not chosen between.
- `trimming.md:451` now cites "R-M", a ruling code, in a published page. The section is EXSET-011's punchlist item.
- Remaining reference-app comments name deleted files. EXSET-012 retires them all:
  - `TestingSamples.cs:146`, `:221`, `:259`;
  - `AuthorizationPolicySamples.cs:173`, `:251`;
  - `SaveMetaSamples.cs:7`;
  - `SerializationSamples.cs:8`;
  - `AuthorizationSamples.cs:7`;
  - `EmployeeWithSave.cs:7`.
- The public `ICorrelationContext` is not on the page, which opens "The interfaces RemoteFactory defines". It was absent before this change too.
- "A property with a private setter is not serialized" (`save-operation.md:20`): in Named format, System.Text.Json writes it and does not read it back. It does not cross either way.
- `IOrdinalConverterProvider`'s "without reflection" (`interfaces-reference.md:133`) holds for the generated registration (`ClassFactoryRenderer.cs:1623-1624`). `NeatooOrdinalConverterFactory`'s unregistered path invokes `CreateOrdinalConverter` by reflection (`:85-101`).

## Read report

- **Read beyond the brief:**
  - `ClassFactoryRenderer.cs:130-180`, `:724-800`, `:895-1040`
  - `FactoryModelBuilder.cs:270-345`, `:600-745`
  - `FactoryGenerator.Types.cs:285-300`, `:400-425`, `:520-565`
  - `OrdinalRenderer.cs:255`, by grep
  - `HandleRemoteDelegateRequest.cs:225-262`
  - `WebApplicationExtensions.cs:40-110`
  - `ServiceCollectionExtensions.cs`, `AddRemoteFactoryServices.cs:105-132`, `Log.cs` EventIds 3008 and 9007
  - `PartialSaveTests.cs`, by grep, and `:55-75`, `:184-200`, `:277-293`, `:346-360` at `d926ae7`
  - the working-tree diff and `008-test-review.md`, both of which appeared during the review
  - `OrderLine.cs:290-380`, `TaxRate.cs`, `AuthorizedOrder.cs`, by grep
  - `factory-events.md`, `serialization.md` and `aspnetcore-integration.md:148-175`, by grep
  - `factory-operations.md:1-30`, `:114-215`, `:405-466`
  - the old pages and the deleted files at `2d19668`
  - issue #104 (`gh issue view`)
- **Named but unused:**
  - `ClassFactoryRenderer.cs` beyond the ranges listed
  - VisionAlignment, read only for D15, R-D, R-E, R-F, R-K and R-M
