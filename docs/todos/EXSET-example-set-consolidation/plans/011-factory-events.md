# Factory events page

**Plan #:** 011
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** Done
**Last Updated:** 2026-10-15
**Plan-review opt-in:** No. Design already holds every event shape the page teaches (`FactoryEventHandlerPattern.cs`, `FactoryEventPhasesPattern.cs`, `FactoryEventRelayPattern.cs`), with their tests. This plan adds regions and one `CancellationToken`.
**Code-review opt-in:** Yes. The page is restated against the event runtime, and C-06 changes every raising example.
**Branch:** EXSET-011-factory-events
**PR:** —

---

## Scope

Re-point every C# block on `docs/factory-events.md` to `src/Design`, or turn it into prose or a short WRONG block. `docs/events.md` is a redirect stub with no C#, and stays.

**Split at Step 2 (2026-10-15):** `trimming.md`, the appendix pages and the LazyLoad removal move to EXSET-016. They share no region with this page.

**New Design regions, on code the event tests already pin:**
- `events-event`, on `OrderCheckoutCompleted`;
- `events-raise`, on CheckoutOrder's `[Remote, Insert]`;
- `events-serveronly`, on its `[Remote, Update]`;
- `events-phase-handlers`, on the three `InvoiceFinalizedEvent` handlers;
- `events-phase-drain`, on `InvoiceFinalizer._Finalize`;
- `events-coalesce`, on `StatementProjection`;
- `events-relay`, on `InMemoryAggregatorRelay`;
- `events-relay-register`, on the relay tests' client registration;
- `events-client-raise`, on the client-raise test.

**Reused:** `attributes-factoryeventhandler`, for the server handler.

**Becomes prose:**
- library signatures (`Raise<T>`, `IFactoryEventRelay`, `RemoteResponseDto`);
- stacked handler attributes;
- the complete example, which the regions above already make up.

**WRONG blocks:**
- `[Factory]` on a handler class: a probe compile gives CS0111;
- a `void` handler: NF0501.

**Findings to fix:**
- C-06: every raising example raises from an `[Insert]`/`[Update]` or a `[Remote, Execute]`, not a `[Create]`;
- I-08: where events are raised;
- T-18: the handler rules say `static`;
- T-19: registration names, and Remote-mode `IFactoryEvents`;
- this page's share of C-19.

Does NOT touch:
- the skill's `factory-events.md` prose (Todo 2);
- `trimming.md` (EXSET-016).

---

## Intent

The page shows events raised where the rulings put them, in a write or a command, never a `[Create]`. Every example is Design code its tests pin.

---

## Framework & Architectural Alignment

- **Rulings:** D3 (writes belong in Insert/Update), D14, R-E and R-M.
- **`.claude/rules/design-snippets.md`.**
- **The attributes reference (EXSET-014),** which this page links for `[FactoryEventHandler<T>]` and `[FactoryEvent]`.

---

## Constraints & Invariants

- **Embeds.** Every C# block on the page is a Design embed or a short WRONG block. Text diagrams stay.
- **Regions.** Each region name is defined once.
- **The whole still works:** both solutions green, and mdsnippets swallows nothing.

---

## Steps

1. The new Design regions. CheckoutOrder's Insert takes and passes its `CancellationToken`, as the page teaches.
2. Rewrite `factory-events.md`.
3. Verify and record: mdsnippets, both solutions, the ledger.

---

## Acceptance

- [x] Every C# block on `factory-events.md` is a Design embed or WRONG `[explicit-skip: grep]` · Must
- [x] The page's statements match the event runtime and generator `[explicit-skip: source cited; probe compile for the WRONG blocks]` · Must
- [x] The embedded regions are pinned by Design tests, including the `CancellationToken` CheckoutOrder now passes `[integration]` · Must
- [x] mdsnippets clean; both solutions green `[explicit-skip: meta]` · Must

---

## Current State (Pre-Flight)

- **`factory-events.md`:** 632 lines, no embeds, 19 hand fences.
- **Its raising examples are `[Remote, Create]`** (C-06).
- **Its "Right" handlers are instance methods** (T-18).
- **Its DI table names `AddRemoteFactoryServices` and `FactoryMode`, and calls Remote-mode `IFactoryEvents` "no relay".** The source registers `RemoteFactoryEvents`, which sends to the server (T-19).
- **Probe compile (2026-10-15):**
  - `[Factory]` with `[FactoryEventHandler<T>]` on a static class is CS0111 (both pipelines emit `FactoryServiceRegistrar`);
  - an `async void` handler is NF0501.

---

## Punchlist

---

## Test Evidence

| Bullet | Pinned by | Tier |
|---|---|---|
| 1. Embeds | explicit-skip: `factory-events.md`'s 10 `snippet source` links are all under `src/Design`. Its only C# fences outside a snippet pair are three WRONG blocks. Text diagrams stay as plain fences | explicit-skip |
| 2. Statements match the runtime | explicit-skip: the DI table against `AddRemoteFactoryServices.cs:118-194` (`RemoteFactoryEvents` in Remote, `FactoryEventsDispatcher` and the coordinator in Server and Logical, the collector in Server); `RemoteResponseDto` against `RemoteResponse.cs`; the WRONG blocks against `reviews/011-probe-handler-factory.log` (CS0111; NF0501) | explicit-skip |
| 3. Regions pinned | `events-event`, `events-raise`, `events-relay`, `events-relay-register`: `FactoryEventRelayTests.Relay_EventRaisedInRemoteInsert_DeliveredToConsumer`. `events-serveronly`: `Relay_ServerOnlyFlag_EventNotRelayedToConsumer`. `events-client-raise`: `Relay_ClientRaisedEvent_DeliversOwnEventAndHandlerRaisedEvent`. `events-phase-handlers`, `events-phase-drain`: `FactoryEventPhasesTests.Finalize_Remote_RunsEachPhaseAtItsDrainPoint` and `Finalize_Logical_RunsEachPhaseAtItsDrainPoint`. `events-coalesce`: `StatementClose_CoalescingHandlerRunsOnce_ControlRunsPerRaise`. `attributes-factoryeventhandler`: `FactoryEventHandlerTests`. The token the Insert and Update pass to `Raise`: `FactoryEventRelayTests.Save_PassesTheCallersToken_ToTheServerHandler` (added at the gate), which saves twice from the client with a live token and asserts `CheckoutLedgerHandler` received it, then sees its cancellation. With `CancellationToken.None` in the Insert it fails (`reviews/011-token-unfixed-test.log`); dropping the argument is CA2016, a build error. `CheckoutLedgerHandler`'s registration: `DesignServerCompositionTests` | integration |
| 4. Meta | explicit-skip: `reviews/011-mdsnippets.log`. Logs: `011-build.log`, `011-test.log`, `011-design-build.log`, `011-design-test.log` (unit 852; integration 657 + 7 skips; Design 175 + 1 known-bug skip, one test added at the gate; both TFMs) | explicit-skip |

---

## Gate Record

- **Test review** ([011-test-review.md](../reviews/011-test-review.md)):
  - **Round 1: CONCERNS.** One veto-tier finding: no Design test pinned the token CheckoutOrder's Insert passes to `Raise`. It was punched with `CheckoutLedgerHandler` over a server-only `ICheckoutLedger`, and with `Save_PassesTheCallersToken_ToTheServerHandler`, which fails under a `CancellationToken.None` mutation.
  - **Tech-debt:** the Update's token was punched, and the ServerOnly Update's server handler is now asserted. The `OrderNotifyHandlers` token was dismissed, because `INotificationService` takes none.
  - **Round 2: APPROVED,** with no findings.
- **Code review** ([011-code-review.md](../reviews/011-code-review.md)): APPROVED, with 4 callouts, all amended:
  - `ServerOnly` keeps only the raised event out of the batch;
  - `Relay` runs once per successful round trip;
  - a handler shares scoped and singleton services, not transient ones;
  - C-19's outcome.

  Two theoretical items were taken: the shared-scope cause, and the Create wording.
- **After the punches:**
  - Design builds and tests 175 + 1 known-bug skip on both TFMs. The main solution is unchanged since round 1 (unit 852, integration 657 + 7 known skips).
  - mdsnippets is clean. `aspnetcore-integration.md` and `service-injection.md` gain only the ledger's registration line in the shared `aspnetcore-service-registration` embed.
- **Open, by design:** the skill's `factory-events.md` prose (Todo 2).

---

## Plan Amendments

- **2026-10-15 · Implementation:**
  - **The complete example** is a numbered walk through the page's own regions, closed by the one test that runs the flow end to end. Design has no handler for `OrderCheckoutCompleted`, so step 3 names `OrderNotifyHandlers`.
  - **Stacked handler attributes are prose:** Design has no class that stacks them, and the attributes reference states the rule.
  - **The "less good" client raise** is a short block labelled WRONG for an operation's own event, because a client raise is otherwise supported.
- **2026-10-15 · Gate:**
  - **Design gains a server handler for `OrderCheckoutCompleted`:** `CheckoutLedgerHandler`, over a server-only `ICheckoutLedger`, registered in Design.Server and the harness's server and local containers. CheckoutOrder's Update now passes its token too. The new test pins both.
  - **Page statements corrected against the runtime:** `ServerOnly` keeps only the raised event out of the batch; `Relay` runs once per *successful* round trip; a handler shares scoped and singleton services, not transient ones; shared scope comes from dispatching on the method's provider.
  - **The Complete Example's step 3** is now `CheckoutLedgerHandler`.

---

## Notes

- **Run unattended** under Keith's standing instruction.
