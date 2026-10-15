# EXSET-014 code review — 2026-10-15

**Reviewer:** code-reviewer (per-plan, Step 5; `a564b9d` against `973a90f`). **Verdict:** APPROVED. No veto-tier findings.

There are six callouts. Every one bears on a Must criterion, and more than five is itself the lead finding. The mechanics landed cleanly: the Design regions, the embeds, the deletions, the attributions and #141. The callouts are about what the page and the three skill lines *say*, in four places:
- the inheritance section, which the plan set out to make true (1);
- `[SuppressFactory]` on an interface (2);
- `FactoryHintNameLength`, which the page describes as the opposite of what it does (3);
- "without `[Remote]` it runs locally", which `authorization.md` contradicts (4).

**Build.** 0 errors in all three builds:
- Main solution: 2 warnings, both the OrderEntry WASM `NativeFileReference` notice (`014-build.log`).
- Design solution: 0 warnings (`014-design-build.log`).
- Reference app: 0 warnings (`014-refapp-build.log`), after the two deletions and the marker removal.

**Tests.** 0 failed on both TFMs:
- Unit 852. Integration 657 + 7 skipped. The 7 are the known `ShowcasePerformanceTests` (3), `RelayTimingTests` (2) and `FactoryReferenceIdentityTests.Ordinal_*` (2, #133), each named in `014-test.log`.
- Design 173 + 1 skipped. The skip is `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull` (#104) on both TFMs (`014-design-test.log`).
- The counts are unchanged from the 009 merge. This plan adds region markers and no tests, so that is expected.
- `014-mdsnippets.log` has no missing-snippet or error line.
- The logs (14:55–14:56) predate the commit (14:57:18), so I checked every attribution on the page and in `static-factory.md` against the committed sources. All 16 open on their `#region` line and close on its `#endregion`.

The deliverable landed as scoped:

- **Direction and shape.**
  - The five new regions are markers around existing Design code: `CustomerPreferences`' declaration, `ICustomerDirectory`'s, `ExampleCommands._ScoreText`, `OrderNotifyHandlers` and `OrderPlacedEvent`. There is no new shape, and each is pinned by the test the plan names:
    - `OperationsTests` uses `CustomerPreferences`;
    - `InterfaceFactoryAuthorizationTests` uses `ICustomerDirectory`;
    - `LocalExecuteTests.Execute_StaticFactory_WithoutRemote_RunsOnClient` runs `ScoreText` on the client with no wire;
    - `FactoryEventHandlerTests` raises `OrderPlacedEvent` (`:77`).
  - The reused regions are 007–009's.
  - Library code (`FactoryEventBase`) and the assembly attribute became prose, as the 007–009 precedent sets.
- **Checked against the generator.** These hold:
  - **`[Factory]` on the declaration only.** `ForAttributeWithMetadataName` matches the declaration that carries the attribute (`FactoryGenerator.cs:19-23`). A derived class without its own `[Factory]` gets nothing, whatever `FactoryAttribute`'s `Inherited = true` says (`FactoryAttributes.cs:10`). #141 records this with the probe table and two options.
  - **`[SuppressFactory]` on a class.** The class pipeline's predicate drops a declaration whose own attribute list names `SuppressFactory` (`FactoryGenerator.cs:23`). For classes, records and static classes, the page's "only beside a `[Factory]` on the same declaration" is exact. For interfaces it is not (callout 2).
  - **Auth flags.** `Create | Read`, `Fetch | Read`, the writes `| Write`, and `Execute | Read` (`FactoryOperation.cs:11-16`).
  - **`[Execute]`.**
    - NF0102 fires on `!method.IsTask` on both shapes (`FactoryModelBuilder.cs:86-107, :226-245`).
    - NF0105 skips static methods (`:195-219`), and the static factory never checks it.
    - A class-level Execute returns `Task` of the service type (`:491-512`). The generated interfaces show the three visibility outcomes: `public` (`RunCommand`, `ScoreLocally`), `internal` (`ArchiveOnServer`), and `[Remote] public static` promoted (`OpenDraft`) (`ClassExecuteDemoFactory.g.cs:16-22`, `OrderFactory.g.cs:17-23`).
    - The delegate name drops the underscore (`:530-537`).
    - Insert/Update/Delete are absent from `IOrderFactory`, as the page says.
  - **`[FactoryEventHandler<T>]`.**
    - A matching method is static, returns exactly `Task`, of any accessibility, and its first parameter that is neither `[Service]` nor `CancellationToken` is `T` (`FactoryGenerator.RelayHandler.cs:311-350`).
    - NF0502 is several matches. NF0503 is an instance match (Warning). NF0504 is a repeated event type, where the first declaration registers (Warning). NF0505 is `Coalesce` on `Immediate` (Warning) (`DiagnosticDescriptors.cs`).
    - `Inherited = false, AllowMultiple = true` (`FactoryEventHandlerAttribute.cs`).
    - The phase and drain semantics match `DispatchPhase.cs` and `factory-events.md:25, :185-205`.
  - **`[FactoryEvent]`.** It is declared on `FactoryEventBase` with `Inherited = true` and read by `FactoryEventTypeRegistry` with `inherit: true` (`FactoryEventTypeRegistry.cs:135`).
  - **`[AspAuthorize]`.** `Policy` (constructor), `Roles` and `AuthenticationSchemes`, with `Inherited = false, AllowMultiple = true` (`AspAuthorizeAttribute.cs`).
  - **`[Service]`.** "Resolved on the tier that runs the method" (I-02). The parameter order matches `factory-operations.md` (I-06).
- **Shared pages.** `factory-operations.md`, `interfaces-reference.md` and `save-operation.md` change only in `<sup>` lines (`CustomerPreferences.cs` gained two marker lines). Their statements agree with the page, apart from callout 4 against `authorization.md:201`.
- **Rules.**
  - The page has 15 snippet pairs and 30 fence lines, so every fence is inside a pair, with no hand-written C#.
  - Each of the 15 regions is defined once, under `src/Design`.
  - No `attributes-*` region is defined or embedded outside Design except `attributes-execute-local` in `static-factory.md`, which is Design's.
  - `attributes-suppressfactory`, `attributes-inheritance` and `attributes-factoryhintnamelength` appear nowhere outside `docs/todos`, and `AssemblyAttributes.cs` has no markers (I-14).
  - D3, D11, D12, D14, R-E, R-K and R-M: the page has no bool Create, no throwing check, no repository interface factory, no LazyLoad, and no `IsNew` private setter.
- **The skill.**
  - `static-factory.md`'s embed is now Design's, and the added line about `ITextScorer` makes it self-contained. The client registers `TextScorer` (`Design.Client.Blazor/Program.cs:84`).
  - `class-factory.md`'s `[SuppressFactory]` section and `advanced-patterns.md`'s two sections are self-contained prose. They carry the same errors as the page (callouts 1–3).
- **Links.**
  - Every outbound link resolves: `interface-factory.md`, `factory-operations.md#create-operation`, `#class-factory-execute`, `save-operation.md`, `trimming.md`, `factory-events.md#dispatch-phases`, `#client-side-relay-consumer-implements-ifactoryeventrelay`, `interfaces-reference.md#ifactoryeventrelay`, the in-page `#execute`, `client-server-architecture.md`, `service-injection.md`, `authorization.md`, and the four Next Steps pages.
  - The one inbound anchor, `factory-operations.md:230` → `#execute`, resolves. `CLAUDE.md:82`'s "copy a live pair from `docs/attributes-reference.md`" still has pairs to copy.
- **Ledger.**
  - I-14, I-01 (the old page had four `private set` samples), I-02, I-03, C-19, C-27 and T-22 match the diff.
  - C-16 records a different finding (callout 5).

**Sacred tests.** No test file is in the diff.

| # | Callout | Affects | Triage |
|---|---|---|---|
| 1 | **The inheritance section says the generator reads attributes only from each declaration. Two of its lines are false, and it leaves out the one inheritance the generator does perform.**<br>• `attributes-reference.md:433-438`; the skill's copy is `advanced-patterns.md:545`.<br>• `[AuthorizeFactory<T>]` is found by `ClassOrBaseClassHasAttribute`, which walks `BaseType` (`FactoryGenerator.Transform.cs:19-31, :310`). A derived `[Factory]` class therefore runs its base's authorization class. That is so although the attribute is `Inherited = false` (`FactoryAttributes.cs:145`). The page says these attributes are "declared on each class or method they apply to" (`:437`).<br>• A derived `[Factory]` class's factory is built from `GetMethodsRecursive(symbol)` (`FactoryGenerator.Types.cs:179`). That walks the base types for every non-constructor method (`Transform.cs:58-67`), and `TypeFactoryMethods` has no filter on the declaring type (`:74-300`). So a base class's `[Fetch]`, `[Insert]`, `[Update]`, `[Delete]` and `[Execute]` methods, `[Remote]` included, become operations on the derived factory. Only a `[Create]` constructor stays behind. `:436` ("each method and parameter declares its own") is literally true, but under this heading it reads as "a derived class redeclares its operations", which the old page said outright and the generator does not require.<br>• `[Factory]` on an `abstract` or generic class is dropped by the same predicate (`FactoryGenerator.cs:22`). The base of an inheritance chain is often abstract, and the page doesn't say so.<br>• Confidence High, from the source; not compiled. The plan's probe tested only class-level `[Factory]`/`[SuppressFactory]`.<br>• Reachable by: a reader who gives a derived `[Factory]` class its own `[AuthorizeFactory<T>]`, or none, expecting the base's not to apply. Or one who redeclares the base's `[Fetch]` and gets two. | AC-4 · Must (plan Acceptance 2, "match the generator") | **Amend** (fixed): inheritance section rewritten (base operation methods, nearest base `[AuthorizeFactory<T>]`, abstract and generic ignored); skill likewise; #141 comment |
| 2 | **`[SuppressFactory]` does nothing on an interface, but the page lists Interface as its target and says it turns generation off "for a type marked `[Factory]`".**<br>• `attributes-reference.md:10` (Quick Lookup: "Class, Interface", "Turn generation off for a type marked `[Factory]`"), `:62` ("On a type that also carries `[Factory]`"), `:435`. The skill's `class-factory.md:407` says the same ("a type that also carries `[Factory]`").<br>• The interface pipeline's predicate checks only the declaration kind and generic parameters (`FactoryGenerator.cs:54-56`). The `SuppressFactory` check exists only in the class pipeline (`:23`), and nothing else in the generator reads it (grep).<br>• `SuppressFactoryAttribute` allows `AttributeTargets.Interface` (`FactoryAttributes.cs:18`). #141 covers `Inherited = true`, not this.<br>• Confidence High, from the source; not compiled.<br>• Reachable by: a reader who puts `[Factory][SuppressFactory]` on an interface to switch its proxy off. The factory is still generated and registered. | AC-4 · Must (plan Acceptance 2) | **Amend** (fixed): Class, Record target; no effect on an interface; skill likewise |
| 3 | **The page says `FactoryHintNameLength` *caps* hint names for builds near the Windows path limit. The generator always caps them, at 50, and truncation is a build error. The attribute is how you *raise* the cap.**<br>• `attributes-reference.md:23` ("Limit generated file names"), `:429`. The skill's `advanced-patterns.md:539`, rewritten by this plan, says the same.<br>• `SafeHintName` defaults to 50 when the attribute is absent (`FactoryGenerator.cs:217-228`). A longer `{Namespace}.{Type}` is truncated, and NF0104 is reported as an **Error** whose message says "Consider using `[assembly: FactoryHintNameLength(N)]` to increase the limit" (`FactoryGenerator.Types.cs:301-318`; `DiagnosticDescriptors.cs:76-83`). The page's own example, 100, raises the limit. So did the reference app's (`AssemblyAttributes.cs`: "Increase hint name length").<br>• Neither page names NF0104 or the default, and no other doc does (grep of `docs/` and the skill).<br>• Confidence High.<br>• Reachable by: any project with a qualified type name over 50 characters, such as `MyCompany.Product.Domain.Aggregates.CustomerPreferences`. It fails the build with NF0104, and the reference page tells the reader the attribute is for shortening names. | AC-4 · Must | **Amend** (fixed): page and `advanced-patterns.md` state the default cap of 50, NF0104 as an error, and that the attribute raises the cap |
| 4 | **"Without `[Remote]`, a method runs locally, `[Execute]` included", and "a bare `[Execute]` gets ... no remote delegate or endpoint". `authorization.md` says the opposite for an `[AspAuthorize]` or a `[Remote]` auth method, and the generator agrees with `authorization.md`.**<br>• `attributes-reference.md:213`, `:228`, `:309`.<br>• `authorization.md:201`: "`[AspAuthorize]` on a bare `[Execute]` forces the call to the server on both shapes ... a `[Remote]` auth method forces the call to the server."<br>• `isRemote = method.IsRemote \|\| AuthMethodInfos.Any(m => m.IsRemote) \|\| AspAuthorizeCalls.Any()` for reads, writes, class Execute and the static delegate (`FactoryModelBuilder.cs:351-353, :402-404, :477-479, :546-547`).<br>• The page's `[AspAuthorize]` section (`:405-423`) does not say it moves the call. This is the brief's "agree with `authorization.md`".<br>• Confidence High.<br>• Reachable by: a reader who adds `[AspAuthorize]` to a bare `[Execute]` or a local Create and expects it to stay on the client. It becomes a server call, and on the client it needs the HTTP pipeline. | AC-4 · Must | **Amend** (fixed): `[Execute]` and `[Remote]` sections state the `[AspAuthorize]` and `[Remote]` auth-method exception, linking `authorization.md#placement-on-execute` |
| 5 | **The ledger's C-16 row records a fix for a different finding, and overstates the retirement.**<br>• C-16 in the audit (`audit-remotefactory.md:55`) is `authorization.md`'s `authorization-policy-multiple` sample: a `[SuppressFactory]` class with `public static Task _ProcessPayroll`, which the prose called "the shape to copy". It was resolved when 009 re-pointed that region to `SecureOrder.Update` (`authorization.md` has no "shape to copy" or `_ProcessPayroll`; grep).<br>• The 014 outcome reads "attributes-reference states [SuppressFactory] as it behaves ...; the [SuppressFactory] samples retired; the skill's class-factory section corrected". That is this plan's work on a different sample. The reference app still holds eight `[SuppressFactory]` classes: `AttributesSamples.cs:36`, `AuthorizationSamples.cs:351, :397, :487`, and `ServiceInjectionSamples.cs:79, :122, :220, :287`. They are unpublished and retire with 012.<br>• Confidence High.<br>• Reachable by: the close-out audit's AC-4 trace, which reads the ledger as the record of what fixed each finding. | AC-4 · Must (C-16) | **Amend** (fixed): C-16 row rewritten as fixed by EXSET-009, recorded by 014, the reference app's classes left to EXSET-012 |
| 6 | **The plan put `attributes-factoryeventhandler` on "a phased handler". It landed on an `Immediate` one, and the page now shows the `DispatchPhase` and `Coalesce` arguments nowhere.**<br>• The plan's Scope (`014-attributes-reference.md:32`). The region is `OrderNotifyHandlers`, with no phase argument (`FactoryEventHandlerPattern.cs:92-106`). Neither the Amendments nor the Discovery Log records the change.<br>• `:265-270` describe the phase argument and `Coalesce = true` in prose. The old page's hand-written block of the four attribute forms is gone (rightly), with no embed in its place. Design has phased handlers (`FactoryEventPhasesPattern.cs`; plan Current State).<br>• Confidence Medium. The consequence is a lookup, not a wrong statement.<br>• Reachable by: a reader of the reference page looking for how to write `[FactoryEventHandler<T>(DispatchPhase.AfterCommit, Coalesce = true)]`. They must follow the link to `factory-events.md`. | AC-3 · Must | **Amend** (recorded): Plan Amendment 2026-10-15 · Implementation |

## Could not verify

- Callout 1's base-class behaviour (`[AuthorizeFactory<T>]` from a base, and base operation methods on a derived factory) comes from the generator source. No test in either solution covers it (`HasBaseClassFactoryAttributeTests` checks only that both factories exist), and I compiled nothing.
- The plan's probe compile is not in the repository. Its five outcomes agree with `FactoryGenerator.cs:19-23` and with #141's table.
- The generated `.g.cs` files cited above are untracked output on disk, read as the logged build's.

## Theoretical (not triaged)

- **The trimming paragraph contradicts itself.** `:235` says "`[Remote]` decides whether a guard is emitted at all", then that `internal` on a class factory also guards. It is pre-existing text.
- **"Marks a static class"** (`:243`). The handler pipeline accepts any class (`FactoryGenerator.cs:83`). NF0405 ("must be static") is defined but never emitted.
- **"NF0501 if none"** (`:275`). NF0501 is reported only when the class declares some static ordinary method (`FactoryGenerator.RelayHandler.cs:377-380`). Otherwise the class is silently unregistered.
- **`[FactoryEvent]` "has no effect"** on a non-`FactoryEventBase` type (`:303`). The registry still maps that type by full name (`FactoryEventTypeRegistry.cs:124-157`).
- **`[SuppressFactory]` is matched by syntax text `SuppressFactory`** (`FactoryGenerator.cs:23`). `[SuppressFactoryAttribute]` or a qualified name does not suppress.
- **`[Remote]` is missing from the inheritance list.** Its `AttributeUsage` is `Inherited = true` (`FactoryAttributes.cs:47`). `IsRemote` reads each method symbol's own attributes (`FactoryGenerator.Types.cs:706-711`), and how an override without `[Remote]` interacts with its enumerated base method was not traced.
- **`FactoryHintNameLength` does not reach the event-handler hint names.** That leg reads a static `MaxHintNameLength` nothing assigns (`FactoryGenerator.RelayHandler.cs:546`; `FactoryGenerator.cs:14`).
- **`[AuthorizeFactory<T>]` "to a class or interface factory"** (`:349`) leaves out the static factory, which 009 made enforced.
- **Quick Lookup targets.** `[Create]` omits Class (a record's primary constructor, NF0205), and `[Fetch]` omits Constructor (`FactoryAttributes.cs:74, :85`).
- **Stale reference-app comments.** `ExecuteSamples.cs:6, :11`, `AuthorizationSamples.cs:6` and `AuthorizationOperationSamples.cs:6` (Application) still point at the deleted `MinimalAttributesSamples.cs`. They retire with 012.

## Read report

- **Read beyond the brief:**
  - `FactoryGenerator.cs:1-130, :196-250`; `FactoryGenerator.Transform.cs:1-340, :480-560`; `FactoryGenerator.Types.cs:160-200, :295-320`
  - `FactoryGenerator.RelayHandler.cs:200-420, :535-555`; `FactoryModelBuilder.cs:60-130, :185-260, :470-560`
  - `DiagnosticDescriptors.cs` (NF0104, NF05xx); `DispatchPhase.cs`; `FactoryEventAttribute.cs`; `FactoryEventBase.cs`; `FactoryEventTypeRegistry.cs:100-157`; `AspAuthorizeAttribute.cs`; `AuthorizeFactoryOperation.cs`
  - The generated `OrderFactory.g.cs` and `ClassExecuteDemoFactory.g.cs` interfaces; `LocalExecuteTests.cs:55-100`; `HasBaseClassFactoryAttributeTests.cs`
  - The old page at `973a90f`; the audit rows C-16, I-03 and I-14; issue #141 (read-only `gh`)
  - `factory-operations.md:215-306`; `authorization.md:201` by grep; `factory-events.md` by grep
- **Named but unused:**
  - `interfaces-reference.md` and `save-operation.md`, beyond their `<sup>`-only diffs and the `#ifactoryeventrelay` anchor
  - `.claude/rules/design-snippets.md`, beyond its region and fence rules
