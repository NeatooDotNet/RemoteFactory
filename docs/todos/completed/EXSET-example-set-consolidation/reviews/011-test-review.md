# EXSET-011 test review — 2026-10-15

## Round 1 — CONCERNS (1 veto-tier, 0 callouts)

**Reviewer:** test-reviewer, against `a5f81ab`, diffed from `a3dcce2`. Logs green:
- main build (`011-build.log`): `Build succeeded`, 0 errors, 2 warnings. Both are the known OrderEntry WASM native-reference notice.
- Design build (`011-design-build.log`): `Build succeeded`, 0 errors, 0 warnings. `Design.Domain`, `Design.Client.Blazor`, `Design.Server` and `Design.Tests` were built on both TFMs.
- tests (`011-test.log`, `011-design-test.log`): unit 852, integration 657 + 7 skips, Design 174 + 1 skip, each on both TFMs. No failures. The Design skip is `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull` (#104). The integration skips are the known three `ShowcasePerformanceTests`, two `RelayTimingTests` and two `FactoryReferenceIdentityTests`. The counts are unchanged, which matches region markers and one added parameter.
- mdsnippets (`011-mdsnippets.log`): no error, exception or missing-snippet line; it ends `Finished`.
- probe (`011-probe-handler-factory.log`): `Build FAILED` with exactly two errors, NF0501 on `ZzProbeVoid` and CS0111 (`FactoryServiceRegistrar` defined twice) on `ZzProbeHandlers`. `ZzProbe.cs` is gone from `Design.Domain`.

The Design edits are timestamped 16:05:58–16:06:56. The logs are 16:09:26–16:09:43, and the commit is 16:10:38. The generated `CheckoutOrderFactory.g.cs` (16:09:28) calls `cTarget.Insert(factoryEvents, cancellationToken)` from `LocalInsertCore`, so the logs ran against the new signature.

Notes per bullet:
- **1. Embeds (Must, explicit-skip): honest.**
  - The page has ten `snippet source` links, all under `src/Design`: `events-event`, `events-raise`, `attributes-factoryeventhandler`, `events-phase-handlers`, `events-phase-drain`, `events-coalesce`, `events-relay`, `events-relay-register`, `events-serveronly` and `events-client-raise`.
  - The only C# fences outside a snippet pair are three blocks, each labelled WRONG: `:506`, `:522` and `:540`. The plain fences at `:289` and `:428` are text diagrams.
  - Each of the ten names is defined once under `src`. `attributes-factoryeventhandler` is embedded on this page and on `attributes-reference.md`.
- **2. Statements match the runtime (Must, explicit-skip): honest for the WRONG blocks.**
  - The `[Factory]` + `[FactoryEventHandler<T>]` block (`:522`) cites CS0111, which matches the probe's `FactoryServiceRegistrar` error.
  - The `async void` block (`:540`) cites NF0501 "no matching handler method", which matches the probe's NF0501 text.
  - The DI-table and `RemoteResponseDto` citations are code-review ground and were not re-read.
- **3. Regions pinned (Must, integration): not honest for the `CancellationToken`.** See finding 1. The other regions are honest:
  - `events-event`, `events-raise`, `events-relay` and `events-relay-register`: `FactoryEventRelayTests.Relay_EventRaisedInRemoteInsert_DeliveredToConsumer` builds the client with the registered `InMemoryAggregatorRelay`, then calls `Create` and `Save` through the client factory. It asserts that a single relayed `OrderCheckoutCompleted` has `OrderId == 42` and `Total == 99.95m`.
  - `events-serveronly`: `Relay_ServerOnlyFlag_EventNotRelayedToConsumer` saves twice. After the Update it asserts that the one received event is still the Insert's (`Total == 5.00m`). Its fixed 50 ms wait is already Dismissed (EXSET-004).
  - `events-client-raise`: `Relay_ClientRaisedEvent_DeliversOwnEventAndHandlerRaisedEvent` runs the region's two lines and asserts that both `ClientRaisedNotice` and `NoticeAcknowledged` were relayed with `OrderId == 88`.
  - `events-phase-handlers` and `events-phase-drain`: `FactoryEventPhasesTests.Finalize_Remote_RunsEachPhaseAtItsDrainPoint` (client) and `Finalize_Logical_RunsEachPhaseAtItsDrainPoint` (local). Each asserts the exact sequence `invoice-immediate`, `invoice-flush`, `invoice-method-done`, `invoice-commit`. Those are the three handlers' markers and the drain body's marker.
  - `events-coalesce`: `StatementClose_CoalescingHandlerRunsOnce_ControlRunsPerRaise` asserts one `statement-recompute` and three `tally-recompute`.
  - `attributes-factoryeventhandler`: `FactoryEventHandlerTests.Raise_DispatchesToAllHandlers` asserts `OrderNotifyHandlers`' "Order 42 confirmed!" message to `test@example.com`.
- **4. Meta (Must, explicit-skip): honest.** The logs and counts are as stated above.

**Sacred-test check: clean.** The only test-file change in `a3dcce2..HEAD` is two `#region`/`#endregion` pairs in `FactoryEventRelayTests.cs`: one around the client relay registration and one around the client raise. No line inside either test changed. Nothing changed under `src/Tests`.

**Plan-introduced reachable paths:** the `CancellationToken` that `CheckoutOrder.Insert` now passes to `Raise` is the only new path. It is reachable by every client `Save` of a new `CheckoutOrder`, and it is covered by finding 1.

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|
| 1 | Veto | Bullet 3 requires Design tests to pin the regions, "including the `CancellationToken` CheckoutOrder now passes". No Design test does. The map cites `RemoteFactory.IntegrationTests` `FactoryEventHandlerSharedScopeTests.Raise_CancellationToken_FlowsToHandler`. That test calls `Raise` directly on the server container with its own token, so it pins the Raise → handler hop. It never touches `CheckoutOrder`. Every test that saves a `CheckoutOrder` calls `Save` without a token, and Design has no `OrderCheckoutCompleted` handler that reads one. Dropping `cancellationToken` from the `Raise` call in `FactoryEventRelayPattern.cs:165` would keep every test green. The page teaches this pass at `:73` ("Thread the operation's `CancellationToken` through, as the Insert does"). The harness can host the test: `DesignClientServerContainers.ForDelegateNullable` forwards the client token to `HandleRemoteDelegateRequest` (`:108`). Suggested shape, at the integration tier: a Design test saves a new `CheckoutOrder` from the client container with a live token. It asserts that an `OrderCheckoutCompleted` handler declaring a `CancellationToken` received the caller's token, observed through `CanBeCanceled` or a cancellation. Design does not yet have that handler. | AC-3 | **Punch** (fixed): Design gains `ICheckoutLedger`/`InMemoryCheckoutLedger` (server-only) and `CheckoutLedgerHandler` for `OrderCheckoutCompleted`; `FactoryEventRelayTests.Save_PassesTheCallersToken_ToTheServerHandler` pins the token on Insert and Update. With `CancellationToken.None` in the Insert's Raise it fails (`011-token-unfixed-test.log`); dropping the token entirely is CA2016, a build error |
| 2 | Tech-debt | `events-serveronly`'s `Update` takes no `CancellationToken` and raises without one. The page shows it next to `:73`'s rule to thread the token through. | — | **Punch** (fixed): Update takes and passes its `CancellationToken`; the same test pins it |
| 3 | Tech-debt | `events-serveronly`'s comment says "server handlers run". Design has no `OrderCheckoutCompleted` handler, so only the not-relayed half of `ServerOnly` is observed. This predates the plan. | — | **Punch** (fixed): `CheckoutLedgerHandler` runs for the ServerOnly Update; the new test asserts its ledger entry |
| 4 | Tech-debt | `OrderNotifyHandlers.SendOrderConfirmation` (`attributes-factoryeventhandler`) declares a `cancellationToken` and does not pass it to `SendAsync`. This predates the plan. | — | **Dismiss**: `INotificationService.SendAsync` takes no token, so the handler has nothing to pass it to |

Could not verify:
- The probe log does not include `ZzProbe.cs`, which has been deleted, so the probe's source shapes are known only from the class names and diagnostics. These match the page's two WRONG blocks.

Theoretical (not triaged):
- None.

## Round 2 — 2026-10-15 — APPROVED (0 veto-tier, 0 callouts)

**Reviewer:** test-reviewer, against `8b92fac`, diffed from `a5f81ab`. Logs green:
- Design build (`011-design-build.log`, 16:23:03): `Build succeeded`, 0 warnings, 0 errors. All four Design projects were built on both TFMs.
- Design tests (`011-design-test.log`, 16:23:06): 175 passed and 1 skipped on net9.0 and on net10.0, with no failures. The skip is still `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull` (#104). The count rose from 174 to 175, so the new test ran.
- mdsnippets (`011-mdsnippets.log`): no error, exception or missing-snippet line. It ends `Finished`.
- Main solution: `git diff a5f81ab..HEAD --stat` touches only `src/Design` and `docs`, and neither is in `src/Neatoo.RemoteFactory.sln`. The round-1 `011-build.log` and `011-test.log` therefore still stand.
- Ordering: the last source edit (`ServerServices.cs`) is at 16:23:00. The generated `CheckoutOrderFactory.g.cs` (16:23:01) calls `cTarget.Insert(factoryEvents, cancellationToken)` and `cTarget.Update(factoryEvents, cancellationToken)`, and the commit is at 16:23:55. The logs ran against the committed code.

**Finding 1 (veto): closed.**
- `FactoryEventRelayTests.Save_PassesTheCallersToken_ToTheServerHandler` checks what it claims to:
  - It replaces `ICheckoutLedger` on the server with a token-recording ledger. `configureServer` runs after the default registrations (`DesignClientServerContainers.cs:293` against `:272`), so the replacement takes effect.
  - It saves a new `CheckoutOrder` and then updates it, both through the client factory with a live `cts.Token`.
  - It asserts that the ledger holds exactly the Insert entry and the Update entry, `(11, 20.00m)` then `(11, 21.00m)`. This shows that `CheckoutLedgerHandler` ran for both raises, the ServerOnly one included.
  - After `cts.Cancel()`, it asserts that both recorded tokens report cancellation. A `CancellationToken.None` or `default` cannot pass that check.
  - The assertion that the tokens were not cancelled before `Cancel()` does not separate fixed from broken code on its own. The assertion after `Cancel()` does.
- The mutation evidence holds. `011-token-unfixed-test.log` fails this one test at `:200`, the post-cancel `Assert.All`, and only item `[0]` fails: `(11, 20.00, CancellationToken)`, which is the Insert. That fits `CancellationToken.None` in the Insert's `Raise` and nothing else changed.
  - The Update half is pinned by the same assertion on item `[1]`. That mutation was not run.
  - Removing the token argument entirely is a build error, which matches `AnalysisMode=all` plus `TreatWarningsAsErrors` in `src/Directory.Build.props`. Even without that, `Raise`'s default is `default`, so the test would fail.
- The handler is registered on the server and local containers (`DesignClientServerContainers.cs:272`, `:281`) and in `ServerServices.cs:205`. `DesignServerCompositionTests.ServerComposition_ResolvesEveryServerOnlyDependency` resolves `ICheckoutLedger` from `AddDesignServer()`, the real composition, so that one added assertion pins the Design.Server registration.
- Tech-debt 2 and 3 are closed by the same test. `Update` now takes and passes its token, and the ServerOnly Update's ledger entry shows the "server handlers run" half.

**Code-review punches: no test or behaviour change.**
- The `docs/factory-events.md`, `aspnetcore-integration.md` and `service-injection.md` diffs are prose, mdsnippets line-range and embed refreshes, and the new `ICheckoutLedger` registration line in the `aspnetcore-service-registration` embed. They add no C# fence outside a snippet pair.
- `FactoryEventPhasesPattern.cs` moves `#region events-phase-handlers` above `InvoiceBalanceGuard`'s doc comment, so the embed now starts at `L155`. No code line moved.
- The rewritten `ServerOnly` paragraph in `FactoryEventRelayPattern.cs:274-277` is a comment outside every region and is not embedded.

**Sacred-test check: clean.** The test-file diff only adds lines: the new test with its private ledger, and one `Assert.NotNull` in the composition test. No existing assertion, case or expected value changed.
- `CheckoutLedgerHandler` now runs for every `CheckoutOrder` save, but it raises nothing. The relay tests' event counts and payloads therefore mean what they meant before, and all of them pass.

**Plan-introduced reachable paths:** `CheckoutLedgerHandler.Record` and `InMemoryCheckoutLedger.RecordAsync` are reached by every server-side `CheckoutOrder` save. The new test covers the handler. `InMemoryCheckoutLedger` itself runs in the existing relay tests, which only check that it does not throw. It is a test stand-in, not taught behaviour.

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|
| — | — | None | — | |

Could not verify:
- None.

Theoretical (not triaged):
- None.
