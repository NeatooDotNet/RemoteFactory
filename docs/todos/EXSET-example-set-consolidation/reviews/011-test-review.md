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
