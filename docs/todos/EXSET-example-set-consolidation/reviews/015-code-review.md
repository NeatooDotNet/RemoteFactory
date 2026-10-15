# EXSET-015 code review — 2026-10-15

**Reviewer:** code-reviewer (per-plan, Step 5; `50eb667` against `9aa6bd0`, plus the working tree's gate punch). **Verdict:** APPROVED. No veto-tier findings.

The mechanics landed cleanly:
- the four new regions are markers only;
- each embedded key is defined once, and no retired key is defined or embedded;
- the twelve sample files are deleted, and the reference app builds;
- every corrected line I traced matches the generator's output.

There are five callouts, all on Must criteria:
- one sentence beside a new embed that now contradicts it and R-C (1);
- two sections whose embed no longer shows half the shape the section teaches (2, 3);
- two record problems: the ledger (4) and the Follow-on line (5).

**Build.** 0 errors in all three builds:
- Main solution: 2 warnings, both the OrderEntry WASM `NativeFileReference` notice (`015-build.log`).
- Design solution: 0 warnings (`015-design-build.log`). It builds `Design.Client.Blazor`, so `Home.razor`'s `@code` region compiles.
- Reference app: 0 warnings (`015-refapp-build.log`), after the twelve deletions.

**Tests.** 0 failed on both TFMs:
- Unit 852. Integration 657 + 7 skipped: `ShowcasePerformanceTests` (3), `RelayTimingTests` (2) and `FactoryReferenceIdentityTests.Ordinal_*` (2), each named in `015-test.log`.
- Design 174 + 1 skipped, `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull` (#104) (`015-design-test.log`). The extra test is the test review's punch, `InterfaceFactory_ListContacts_ReturnsContacts_AndDeniesTheDeniedCustomer`.
- `015-mdsnippets.log` has no warning, error or missing-snippet line (`TreatMissingAsWarning: True`). It extracts from `Home.razor` (`:24`) and ends `Finished`.

**Timestamps.**
- The main build, test, reference-app and mdsnippets logs are 15:47:22–15:47:53, before the commit (15:49:02).
- The Design build and test logs are 15:54:32–15:54:34. The punched test file is 15:54:05, so they include it.
- The punch and the plan's amended Test Evidence are **uncommitted in the working tree** (`git status`: `InterfaceFactoryAuthorizationTests.cs` and `015-skill-embeds.md` modified, `015-test-review.md` untracked).

The deliverable landed as scoped:

- **Q1. Shape per section.**
  - These embeds show the shape their section teaches:
    - the lifecycle hooks (state prepared, nothing validated);
    - `operations-class-execute` (`public static`, `[Remote]`, returns `IOrder`);
    - the interface factory contract and implementation;
    - both auth regions;
    - `TaxRate`;
    - the complex aggregate's interface, local Create and client usage.
  - The duality pair shows a root and a child signature on one class (R-G). Its prose now contradicts one of them (callout 1).
  - Two sections lose part of what they teach:
    - `class-factory.md`'s "Complete Example" and `static-factory.md`'s commands show members without their declaring type (callout 2);
    - `setup.md`'s "Inject and Use" shows no injection (callout 3).
- **Q2. Corrected lines, against the code and the generator.**
  - **`class-factory.md:87`.** `IOrderFactory` declares `Create` (sync), `Fetch` and `Save`, and also `OpenDraft` and an `internal` `FetchDraft` (`OrderFactory.g.cs:17-24`). The line is not a closed list, so it holds.
  - **`static-factory.md:52-54`.** The two delegates are `Task<SubmitOutcome> SubmitOrder(int orderId, CancellationToken cancellationToken = default)` and `Task CancelOrder(int orderId, CancellationToken cancellationToken = default)` (`OrderCommandsFactory.g.cs:16-17`). Both are registered `AddTransient` in every mode (`:21-28`, `:32+`), so "resolved from DI" holds. `_CancelOrder` declares no token, and the delegate still takes one, which is what the line says.
  - **Duality method names.** `Fetch(OrderLineRow row)` is `internal` on the public `IOrderLineFactory`, and `LocalFetch` carries an `IsServerRuntime` guard. `Fetch(Guid id)` is `public` (`OrderLineFactory.g.cs:18-21, :97-99, :233-235`). The names and claims hold. The clause before them does not (callout 1).
  - **Auth-class registration.** `services.TryAddTransient<ICustomerDirectoryAuth, CustomerDirectoryAuth>()` is generated unconditionally (`ICustomerDirectoryFactory.g.cs:452`). `CustomerDirectory` carries no `[Factory]` (`CustomerDirectory.cs:304`).
  - **Value-object lead and note.** The lead (`advanced-patterns.md:276`) is R-F as `TaxRate.cs:5-8` states it. The note (`:295`) matches the ordinal converter, which rebuilds the record with `new TaxRate(...)` (`TaxRate.Ordinal.g.cs:32, :86`). It also matches `serialization.md:51` ("a record needs its primary constructor").
  - **Complex-aggregate note (`:357`).** `OrderLineList` holds no field. Its `Fetch` and `Update` take `IOrderLineFactory` as method parameters (`OrderLine.cs:297-298, :313`), and `readme-client-usage` calls `lineFactory.Create` and then `AddLine`.
- **Q3. Behaviour-neutral Design edits.** The diff to `CustomerDirectory.cs`, `TaxRate.cs` and `Home.razor` is four `#region`/`#endregion` pairs and nothing else.
  - `skill-value-object-factory` wraps the nested `service-injection-server-only`, and mdsnippets strips the inner markers from the render.
  - The three Design `.cs` regions sit between an XML doc comment and its declaration. `attributes-factory-interface` already did, and the Design build shows 0 warnings.
  - The punched test is additive and asserts through the client container.
- **Q4. Ledger.**
  - C-11, C-14, C-15, C-20 and I-05 match the diff.
    - C-15's skill site (`audit-remotefactory.md:54`) also counted "not found" command results. `_SubmitOrder` returns `SubmitOutcome.NotFound`, which R-I and U-05's EXSET-007 outcome endorse.
  - C-02 records the embeds but not that the audit's INTERNAL contradiction persists (callout 1).
  - U-02 under-records 015's sites, and C-19 claims "every skill embed" (callout 4).
- **Q5. Regions.** Every key the skill embeds is defined exactly once outside `obj`/`bin`/`Generated` (grep of `#region <key>` per key). The keys are the 33 under `src/Design` plus the two `skill-lazyload-*` keys.
  - The eight retired keys are neither defined nor embedded outside `docs/todos` and `docs/plans/completed`.
  - No file outside `docs/todos` links a retired `#snippet-skill-*` anchor.
  - Every `snippet source` under `skills/` is `/src/Design/` except `lazyload.md:65, :117` (EXSET-011).
- **Q6. Follow-on.** It is complete for hand-written C#. Grepping `skills/` for the retired type names finds only the sites it names, plus `lazyload.md`. It misses one prose site and describes two sites imprecisely (callout 5).
- **Rules.**
  - The skill gains no link to a repository file; its new prose lines name types only.
  - D14 holds: no `[Remote, Create]` remains outside `lazyload.md`.
  - R-E holds: the hooks validate nothing.
  - R-F and R-G hold: TaxRate takes `[Factory]` for its service, and OrderLine is both a root and a child.
  - R-M holds: the hooks set `IsNew` publicly.
  - R-C is held by the embeds and contradicted by one prose clause (callout 1).
  - No LazyLoad outside `lazyload.md`.

**Sacred tests.** No test file changed in `9aa6bd0..HEAD`. None of the twelve deleted reference-app files contains a `[Fact]` or `[Theory]`, checked with grep at `9aa6bd0`. The working tree adds one Design test and alters none.

| # | Callout | Affects | Triage |
|---|---|---|---|
| 1 | **"For entity duality, use `internal` on all factory methods." The embed directly above it shows `[Create] public void Create(...)`.**<br>• `advanced-patterns.md:270`, against `orderline-child` (`:207`).<br>• R-C: a child's `[Create]` "is `public` and local, so client code can call `itemFactory.Create(...)` then `Add`" (`VisionAlignment.md:44`). The generator gives the public `Create` no guard (`OrderLineFactory.g.cs:52`).<br>• This is the audit's C-02 INTERNAL finding at the same sentence ("says 'use `internal` on all factory methods' directly under a snippet showing `public`", `audit-remotefactory.md:41`). It now sits over a different method.<br>• 015 corrected the two method names later in this sentence and left the clause. C-02's 015 outcome (`audit-ledger.md:12`) records the embeds, not that the prose still contradicts them.<br>• Not veto-tier: the clause predates 015 and is Todo 2's prose. It is listed first because 015's embed now contradicts it on the same screen.<br>• Confidence High.<br>• Reachable by: a reader who follows the sentence and makes `OrderLine.Create` `internal`. It gains an `internal` modifier on `IOrderLineFactory` and an `IsServerRuntime` guard. The client calls this page and `setup.md` show, `lineFactory.Create(...)` then `AddLine`, then fail to compile outside the assembly or throw on a trimmed client. | AC-4 · Must (plan Acceptance 2; C-02) | **Punch** (fixed): the sentence excepts the child's `[Create]`, public and local; C-02's 015 outcome records it |
| 2 | **Two lead examples show members without their declaring type. `class-factory.md`'s "Complete Example" never shows `[Factory]` on a class.**<br>• `class-factory.md:7-85`. `readme-domain-model` is Order's five operations (`Order.cs:232-316`), with no `[Factory]`, `partial`, class declaration or `IFactorySaveMeta`. The retired embed showed `[Factory] public partial class SkillEmployee : IFactorySaveMeta`.<br>• Order's header (`Order.cs:80-81`, `[Factory] internal partial class Order : IOrder, IFactorySaveMeta, ...`) is in no region. The only `[Factory]` class on the page is the hand-written `OrderLine` at `:272-273`.<br>• `static-factory.md:11-50` likewise shows two `private static` methods without `[Factory] public static partial class OrderCommands` (`OrderCommands.cs:47-48`). There the hand-written `:128-129` shows the shape, so the gap is smaller.<br>• Confidence Medium. This is a judgment on what "Complete Example" must show. Scope chose the region knowingly ("Order's operations").<br>• Reachable by: a reader or agent who builds a class factory from the page's lead example and omits `[Factory]`, or the `partial` Key Rule 1 requires. Either way no `IOrderFactory` is generated. One option: a region on Order's header, or `order-interface` and `save-ifactorysavemeta` beside it. | AC-3 · Must (plan Acceptance 2; Q1) | **Amend** (fixed): new regions `order-class-declaration` and `ordercommands-declaration` (markers only), embedded ahead of the lead examples |
| 3 | **`setup.md`'s "Inject and Use" shows no injection.**<br>• `setup.md:105-163`. `skill-blazor-usage` (`Home.razor:175-233`) is the four handlers. The `@inject IOrderFactory OrderFactory` and `@inject IOrderLineFactory LineFactory` lines are at `Home.razor:17-18`, outside it. The retired embed showed `[Inject] private IEmployeeFactory EmployeeFactory`.<br>• The handlers also read `InMemoryOrderDbContext.DraftOrderId`, Design's seed constant (`OrderPersistence.cs:91`), as the id to fetch.<br>• Confidence Medium.<br>• Reachable by: a reader looking up how a Blazor component gets a factory. The section heading promises it, and the page has no code for it. One option: a second region or snippet marker on the `@inject` lines. | AC-3 · Must (plan Acceptance 2; Q1) | **Amend** (fixed): new `skill-blazor-inject` (a `begin-snippet` razor comment around Home.razor's two `@inject` lines), embedded ahead of `skill-blazor-usage` |
| 4 | **Two 015 ledger outcomes do not match the diff. U-02 records one of the four skill sites 015 replaced, and C-19 claims "every skill embed", which `lazyload.md`'s are not.**<br>• **U-02** (`audit-ledger.md:86`) names only the lead class-factory embed. The audit's U-02 skill sites (`audit-remotefactory.md:136`) also list:<br>&nbsp;&nbsp;– `skill-class-execute`, whose `[Remote, Create] CreateAcute` is now `OpenDraft` calling a local `Create`;<br>&nbsp;&nbsp;– `skill-complex-aggregate`, now `operations-create-method`, local;<br>&nbsp;&nbsp;– `setup.md:159`'s awaited `Create`, now `skill-blazor-usage`'s synchronous `OrderFactory.Create`.<br>&nbsp;&nbsp;015 replaced all three.<br>• **C-19** (`audit-ledger.md:29`) says "every skill embed is interface-first or a record". `skill-lazyload-complete` is `public partial class SkillEmployeeWithReviews` (`lazyload.md:18`). C-19's Owners include 011, so this is not a closure error, but the sentence is false as written.<br>• Confidence High.<br>• Reachable by: the close-out AC-4 trace, which reads each outcome as the record of what fixed the finding's sites. | AC-4 · Must (U-02, C-19) | **Amend** (fixed): U-02 names all four replaced sites; C-19 excepts `lazyload.md` (EXSET-011) |
| 5 | **The Follow-on line misses one site and describes two others imprecisely.**<br>• **Missed:** skill `trimming.md:208` names `EmployeeDto`, which existed only in the deleted `InterfaceFactorySamples.cs`. It is prose, so outside the line's stated "hand-written skill C#", but nothing else records it.<br>• **Imprecise:** the line gives class-factory.md's "Caller Usage" as `ISkillConsultationFactory`. That block resolves `IConsultationFactory` (`class-factory.md:225`).<br>• **Imprecise:** `setup.md:178`'s `SkillEmployeeEvents.OnEmployeeCreatedEvent` ("Fire-and-forget events") is the `[Event]` API removed in v1.5.0 (`static-factory.md:62`). The section is listed, but the line describes it only as a retired sample name.<br>• Confidence High.<br>• Reachable by: Todo 2 working from the Follow-on list. It would leave `trimming.md:208` naming a type that no longer exists. | AC-4 · Must (skill prose handed to Todo 2) | **Amend** (fixed): Follow-on names `IConsultationFactory` and the `[Event]` remnant; `trimming.md`'s `EmployeeDto` added to EXSET-011's scope |

## Could not verify

- **The generated `.g.cs` files** are ignored output (15:47, the logged main build's). I read them as that build's and compiled nothing.
- **The test log is a summary.** It does not name passing tests, so I confirmed the punched test passes from the count (174) and the file's timestamp, not by name.

## Theoretical (not triaged)

- **The two hook embeds come from different classes:** the start hook from `CustomerPreferences.cs:110-119`, the complete hook from `Order.cs:183-193`. The page does not say so. Order implements both interfaces (`Order.cs:81`).
- **"Register the impl on the server only" (`interface-factory.md:272`).** It sits beside the new `CustomerDirectory` line, but `CustomerDirectory` is on the `RegisterMatchingName` convention, which Design's test harness runs on the client too (EXSET-010 callout 1). The sentence is Todo 2's prose.
- **`readme-client-usage` declares `IOrder? order`** for `OpenDraft`, which returns a non-null `Task<IOrder>` (`OrderFactory.g.cs:22`).
- **`SKILL.md:121`'s "extracted from the reference application"** is not re-raised: the test review dismissed it as Todo 2's (T-22).

## Read report

- **Read beyond the brief:**
  - `reviews/015-test-review.md`, and the working-tree diff (the punch and the plan's amended Test Evidence)
  - Generated `OrderFactory.g.cs:1-45`, `OrderLineFactory.g.cs` (interface and guards), `OrderCommandsFactory.g.cs:1-40`, `ICustomerDirectoryFactory.g.cs:390-456`, `TaxRateFactory.g.cs:1-40`, and `TaxRate.Ordinal.g.cs` (grep)
  - `OrderLine.cs:100-360`, `Order.cs` (regions, and the header at `:53-125`), `CustomerDirectory.cs:288-345`, `TaxRate.cs`, `Home.razor:1-21, :150-240`, and `InterfaceFactoryAuthorizationTests.cs` (fields)
  - `docs/serialization.md` (records, grep), `lazyload.md:14-40`, and `SKILL.md:112-130`
  - The deleted sample files at `9aa6bd0` (for `[Fact]`/`[Theory]` and `EmployeeDto`)
- **Named but unused:**
  - `.claude/rules/design-snippets.md` beyond its region-uniqueness, migration and fence rules
  - `docs/VisionAlignment.md` beyond D14, R-C, R-E, R-F, R-G, R-J and R-M
