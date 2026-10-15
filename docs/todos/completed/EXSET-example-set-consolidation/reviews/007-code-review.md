# EXSET-007 code review — 2026-10-15

**Reviewer:** code-reviewer (per-plan, Step 5; `70af11b` against `93da2e9`). **Verdict:** APPROVED. No veto-tier findings.

**Build.** 0 errors in all three builds:
- Main solution: 2 warnings, both the OrderEntry WASM `NativeFileReference` notice (`007-build.log`).
- Design solution: 0 warnings (`007-design-build.log`).
- Reference app: 0 warnings (`007-refapp-build.log`).

**Tests.** 0 failed on both TFMs: unit 839, integration 646 + 7 skipped, Design 160.
- The 7 skips are the known ones: three `ShowcasePerformanceTests`, two `RelayTimingTests` and the two `FactoryReferenceIdentityTests.Ordinal_*` cases (#133), each named in `007-test.log` on both TFMs.
- Design rose 155 → 160, the five `[Fact]`s in `OperationsTests`, so the run exercised this change. Unit and integration are unchanged, as expected with no library change.
- `007-mdsnippets.log` shows no missing snippet and no error.

The deliverable landed as scoped:

- **Direction and shape.** The page now teaches from Order's real operations, and Design gains only what the page needs:
  - `CustomerPreferences`: a no-argument `[Create]` constructor, a `[Remote, Fetch]` and a `[Remote, Insert, Update]` `Upsert`;
  - `OrderCommands`: two static commands that act through `IOrderFactory`;
  - Order's `CanSubmit`.

  These follow the plan's rulings:
  - D3: Fetch takes a key;
  - D14: the Create is local;
  - R-J: the constructor sets defaults only;
  - R-M: public setters, `IsNew`/`IsDeleted`;
  - statement 2: no `new` on a `[Factory]` type;
  - statement 6: the commands fetch, call the verb and save;
  - R-I: `NotFound` in a result, a throw in a `Task` command;
  - R-E: `CanSubmit` read before `Submit`.

  The store is registered explicitly as a singleton in the three places plan-review callout 1 named (`ServerServices.cs:167`, `DesignClientServerContainers.cs:263` and `:271`, `DesignServerCompositionTests.cs:99`), and `RegisterMatchingName` misses it (`InMemory` prefix). Design still reads as the happy path. Order's hooks keep their behaviour.
- **Generator claims on the page, checked against source.** These hold:
  - **Write methods are not on the interface** (`ClassFactoryRenderer.cs:139-143`). `TrySave` exists only when the save has authorization (`:151-156`).
  - **A `bool` Fetch, or authorization, makes a read nullable** (`FactoryModelBuilder.cs:361-363`). A `false` return gives `default` (`ClassFactoryRenderer.cs:508-545`).
  - **No defaults are carried, and the token is optional.** Every interface, delegate and local signature is `{Type} {Name}` plus `CancellationToken cancellationToken = default` (`:1684-1708`; static delegates `StaticFactoryRenderer.cs:87-105`).
  - **The token goes before a `params` array** on the class factory only (`ClassFactoryRenderer.cs:1692-1700`). The page scopes that sentence correctly.
  - **The factory returns the matching `I{ClassName}`** (`FactoryGenerator.Types.cs:157-168`, direct interfaces).
  - **`[Execute]` invocation is positional**: values, then services, then the token, for both static (`StaticFactoryRenderer.cs:304-321`) and class-level (`ClassFactoryRenderer.cs:855-866`). Class operations pass the declared order (`GetParameterIdentifiersForDomainInvocation`). #135 is open, labelled `techdebt`.
  - **A class-level `[Execute]` gets no hooks** (`:834-880`).
  - **Overloads**: the interface uses `Name`, with unique names internally (`FactoryModelBuilder.AssignUniqueNames`).
  - **With no `[Delete]`, `IsDeleted` throws** `NotImplementedException` (`ClassFactoryRenderer.cs:1140-1143`).
  - **The server's token** is `RequestAborted` linked with `ApplicationStopping` (`WebApplicationExtensions.cs:66-80`). The endpoint is `/api/neatoo`.
  - **The events and fire-and-forget prose** match `factory-events.md:3-25` and `:16`/`:129`.
- **Anchors.** The inbound `#execute-operation`, `#method-parameters` and `#combining-operations` resolve to `## Execute Operation`, `## Method Parameters` and `### Combining Operations`. The page's own `aspnetcore-integration.md#when-a-call-fails`, `#collection-factories` and `attributes-reference.md#execute` resolve.
- **Regions.**
  - All 14 page embeds source from `src/Design`, and each region is defined once.
  - The 11 retired names and `operations-params-array-batch` are defined and embedded nowhere outside `docs/todos`/`docs/plans`.
  - The skill's `collection-factory-basic` re-points to `OrderLine.cs#L283-L351`. That makes it the only other embed of a 007 region.
  - The nested regions leave the rendered `readme-domain-model` unchanged on getting-started and the-problem; only the attribution lines move. `order-interface` gains only `bool CanSubmit { get; }`. serialization.md's `serialization-ordinal-versioning` moves its attribution only, and `CanSubmit` is get-only, so the ordinal array is unchanged.
  - The five reference-app files are deleted. `Samples/Skill/LifecycleHookSamples.cs` remains, and it feeds `class-factory.md:137`. No file names a deleted one.
- **Findings text.** None of the following remains on the page: "persistence stage" or the routing-engine framing, "executes on server", "server-only" for `[Service]`, `TryFetch`, "seven", validation gates, "constructor-injected", or "No `[Remote]` on collection".
- **Ledger.** All 25 rows that list 007 as an owner have a 007 outcome. I-01 is untouched (plan-review callout 12). The outcomes match the diff, except that I-05's and T-03's wording leaves out callouts 1 and 2 below.

**Sacred tests.** No existing test lost an assertion. `DesignServerCompositionTests.cs` gains two `Assert.NotNull` lines, and `DesignClientServerContainers.cs` adds two registrations. The test review's round 1, with its six punches, is not re-raised.

| # | Callout | Affects | Triage |
|---|---|---|---|
| 1 | **The Save nullability reason is wrong: after a Delete, Save returns the deleted object.**<br>• `factory-operations.md:24`: "It is nullable because a Delete returns nothing to keep."<br>• Every generated write's local method returns `cTarget` (`ClassFactoryRenderer.cs:992-1012`). The Delete branch is `return {deleteCall}` (`:1138`). Only a deleted object that is also new returns `default` (`:1137`).<br>• `DeleteBehaviorTests.cs:37-41` asserts `NotNull` on the result of a save that deletes, together with `LastOperationCalled == "Delete"`.<br>• Confidence High. Reachable by: a reader who tests `await factory.Save(order) is null` to learn the order was deleted, or who drops the object expecting null. They get the deleted object back, with `IsDeleted` true.<br>• One option: "nullable because a new object marked deleted is never written and returns null; after a Delete, Save returns the deleted object". | AC-4 · Must (T-03; the Intent's "nullable reads") | **Punch** — Save is nullable for a new-and-deleted object or a `bool` write that returns `false`; a deleted stored object comes back. |
| 2 | **The `SaveUpsert` naming rule strips only the highest-ranked operation's name.**<br>• The page says "Save plus your method's name without its operation prefix ... `Upsert` starts with neither" (`factory-operations.md:213`). `CustomerPreferences.cs:23-27` has the same wording.<br>• `BuildSaveMethodFromGroup` takes the group's highest-ranked method and calls `GetNamePostfix(primaryMethod.Name, primaryMethod.Operation)` (`FactoryModelBuilder.cs:681-683`). That strips only that operation's name (`:743-751`). For `[Insert, Update]` the primary is Update.<br>• So `[Insert, Update] Insert()` gives `SaveInsert`, and `[Insert, Update] InsertOrUpdate()` gives `SaveInsertOrUpdate`. "Neither" and "its operation prefix" both suggest that Insert would be stripped too.<br>• Confidence High on the code reading; not executed. Reachable by: a reader who names the combined method `Insert…` and calls the `Save…` name the page implies; the call does not compile.<br>• One option: "Save plus the method name, with the name of the group's highest-ranked operation (Delete, then Update, then Insert) removed when the method name starts with it". | AC-4 · Must (plan-review callout 7) | **Punch** — the page and `CustomerPreferences` state the rule as the generator applies it, with `SaveInsert` and `SaveInsertOrUpdate` as examples. |
| 3 | **Four hook-timing statements are looser than the generator.**<br>• "When an operation throws, no hook runs" (`:459`, and the remarks at `Order.cs:176`). The start hooks run inside the `try`, before the body (`ClassFactoryRenderer.cs:451-454`, `:919-920`). So when the body throws, the start hook has already run; only the complete hook is skipped. The catch at `:616-621` only logs and rethrows.<br>• "A constructor or static Create or Fetch gets only the complete hook" (`:457`). It gets only the synchronous `IFactoryOnComplete` (`:563-578`), even when the static method returns `Task`. Read beside `:456` ("async hooks run ... around Task-returning operations"), the page suggests a `Task`-returning static Fetch gets `IFactoryOnCompleteAsync`.<br>• Not stated: a `bool` operation that returns `false` skips the complete hook (`:508-545`, `:936-970`). That covers Order's and CustomerPreferences' Fetch on a miss.<br>• `Order.cs:164`, the summary over the block the plan corrected, still says "Called after any factory operation completes successfully". Its own remarks below contradict that.<br>• Confidence High. Reachable by: a reader implementing `IFactoryOnCompleteAsync` on a type with a static async Fetch (it never fires), or pairing work in the start hook with cleanup they expect to be skipped on failure. | AC-4 · Must (C-11; plan bullet 4) | **Punch** — all four: the start hook has run when an operation throws; constructor and static operations get only the sync complete hook, on the result; a `bool` false skips the complete hook; Order's summary and remarks name its Task-returning operations and the bool case. |
| 4 | **`CancelOrder` inherits `Cancel`'s throw, the way `SubmitOrder` inherited `Submit`'s before plan-review callout 11.**<br>• `Order.Cancel` throws `InvalidOperationException` when the order is already cancelled (`Order.cs:438-444`). `_CancelOrder` calls it unconditionally (`OrderCommands.cs:84`).<br>• The page presents the command as the `Task`-only pattern whose one throw is the missing target (`factory-operations.md:263`). The Design header says the same (`OrderCommands.cs:16-21`).<br>• R-E: a check the user can recover from is "a verdict the UI reads before acting ... never a throw in a factory method".<br>• Confidence Medium: whether a repeated cancel is a user-recoverable case is a reading of R-E. Reachable by: a client that sends `cancelOrder(id)` twice (a double click, a second tab). The second call is a 500, and `HttpRequestException` over HTTP.<br>• Options: an `IOrder.CanCancel` verdict that the command reads, as `SubmitOrder` does; or a sentence on the page that the verb's own throw is a caller bug. | AC-4 · Must (C-09, U-05; R-E) | **Punch** — `CancelOrder` reads the status and leaves a cancelled order, so a retried cancel succeeds; pinned in `CancelOrder_CancelsAStoredOrder_AndThrowsForAMissingOne`; the page says so. |
| 5 | **The skill's re-pointed `collection-factory-basic` names types that the skill never defines, under a note it contradicts.**<br>• `advanced-patterns.md:368-440` now renders Design's `OrderLineList` with `OrderLineRow`, `IOrderLineFactory`, `OrderLine` and `IOrderLine`. `OrderLineRow` appears nowhere else under `skills/RemoteFactory/`, and CLAUDE.md requires the skill to be self-contained. This is the same class of finding as 013 callout 5.<br>• Directly above it, `skill-complex-aggregate`'s note says "Collection uses constructor injection (`[Service]` on constructor) so factory is available after round-trip" (`:360`). The new embed is Design's rejection of a stored list factory (`OrderLine.cs:115-118`). Before this change the embed agreed with the note.<br>• The skill's prose is Todo 2's, so the note itself is out of scope here.<br>• Confidence Medium. Reachable by: an agent reading the Child Collection Factory section, which gets two opposite list shapes and an undefined row type. | AC-3 · Must | **Punch** — a line after the embed says what `OrderLineRow` is and that the list takes its factory per operation. The `:360` note belongs to `skill-complex-aggregate`, which EXSET-010 re-points. |

## Could not verify

- The working tree has changes that postdate every 007 log (12:22) and the commit (12:23:36). They are the test review's punches:
  - `OperationsTests.cs` (12:31): three new tests, and the Update stamp cleared on the client;
  - the plan's Test Evidence;
  - `README.md` (the `order-interface` embed with `CanSubmit`, which the test review punched to commit with the gate).
- None of these has been built or tested. They need a Design build and test before they are committed; the Design count should then be 163.
- Design does not emit its generated files to disk. Every generated-signature claim above comes from the generator source and the `DeleteBehaviorTests` pins, not from `OrderFactory.g.cs` or `CustomerPreferencesFactory.g.cs`.
- Callout 2's names are reasoned from `FactoryModelBuilder`, not compiled.

## Theoretical (not triaged)

Items 1, 3, 4 and 5 were taken anyway (2026-10-15): item 1 is recorded in the plan's Amendments, and the page says the other three as the generator does. Item 2 is left: `FetchDraft` is internal, so it is not on the factory a caller sees.

- The plan's region map retired `collection-factory-parent` and `operations-remote` into `readme-domain-model`. The page instead embeds the nested regions and gives Remote and the root's list calls as prose. That avoids the double render the plan review's Theoretical item noted. Amendments does not record the change.
- The page's `IOrderFactory` list (`:20-25`) leaves out the `internal` `FetchDraft` member, which `:302` mentions.
- "A constructor with arguments costs the type its ordinal serializer" (`:75`) does not hold when every argument has a default, or when a parameterless constructor exists as well (`FactoryGenerator.Types.cs:546-559`).
- `IFactoryOnCancelled` "cancelled through its `CancellationToken`" (`:451`): the catch is any `OperationCanceledException` (`ClassFactoryRenderer.cs:769`).
- The Execute table's "must return `Task` of the class's interface" (`:224`): it is the class itself when the class has no matching interface.

## Read report

- **Read beyond the brief:**
  - `ClassFactoryRenderer.cs:100-300`, `:405-625`, `:720-1230`, `:1655-1735`
  - `StaticFactoryRenderer.cs:60-330`
  - `FactoryModelBuilder.cs:225-305`, `:340-430`, `:608-751`, plus `AssignUniqueNames`
  - `FactoryGenerator.Types.cs:140-170`, `:524-565`, and the `IsNullable` sites by grep
  - `WebApplicationExtensions.cs:54-80` and `MakeRemoteDelegateRequestHttpCall.cs`, by grep
  - `DeleteBehaviorTests.cs:28-41`
  - `DesignClientServerContainers.cs:44-128`, and its `Scopes()` by grep
  - `factory-events.md`, by grep
  - the headings of `attributes-reference.md` and `aspnetcore-integration.md`
  - `advanced-patterns.md:330-470`, and `class-factory.md` by grep
  - issue #135 (`gh issue view`)
  - `007-test-review.md` and the working-tree diff, both of which appeared during the review
- **Named but unused:**
  - `.claude/rules/design-snippets.md` beyond the brief's points
  - VisionAlignment, read only for D3, D11-D15, R-C through R-M and statements 2-6
  - `FactoryGenerator.Types` beyond the lines listed
