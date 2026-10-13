# Create-or-fetch and value objects

**Plan #:** 004
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-2, AC-4
**Status:** In Progress
**Last Updated:** 2026-10-13
**Plan-review opt-in:** Yes (two new Design shapes later plans embed; documented rules D3, D11, D14, R-F; reshapes 9 pinned tests)
**Code-review opt-in:** Yes (behavior-changing: `Money` loses its factory, three event examples change operation)
**Branch:** EXSET-004-create-or-fetch-value-objects
**PR:** —

---

## Scope

Give Design the two AC-2 shapes it still lacks and remove its `[Create]` misuses (D3, C-06). (a) Create-or-fetch (D11): `Order` gains a class-level `[Remote, Execute]` that opens a customer's Draft order, fetching it when one exists and creating one otherwise. (b) Value objects (R-F): `Money` and `Percentage` become plain records whose constructors throw on an invalid value, and a tax rate looked up by region carries `[Factory]` because its construction reads a server-only table. (c) `[Create]` means `new` (D3): `ClassExecuteDemo`'s Create stops calling a service and its `[Execute]` methods build through the factory instead of `new`; the event examples that raise inside a `[Create]` move onto a save (`CheckoutOrder`) or a static command (`Invoice` finalize, invoice archive), each test keeping its intent. Does NOT touch `docs/` pages, the skill, `src/Design/README.md` (EXSET-012), the reference app's `StartForPatient` (retired with it, EXSET-012) or the `[Remote, Create]` authorized orders (EXSET-009).

---

## Intent

- A reader asking "how do I load-or-create?" (T3) finds a compiled, tested class-level `[Remote, Execute]` in Design's Order domain, instead of a sample in the reference app that is being retired.
- A reader asking "does my value object need a factory?" finds both answers side by side: `Money` without one, `TaxRate` with one, and the reason (a service).
- No Design `[Create]` raises an event or writes (C-06), and none this plan touches takes a service; a Create that reads defaults or a sequence stays legal (§2.3, D14). Plans 007, 010 and 011 can embed these instead of the reference app's samples.

---

## Framework & Architectural Alignment

- D3: `[Create]` is `new`; Fetch takes a key. D11: an operation that may return a new or existing object is an `[Execute]` calling Fetch, then Create. D14: a root Create is local unless it needs the server.
- R-F: a value object is a record; `[Factory]` only when construction needs a service or authorization; constructors throw. R-I: a `bool` Fetch returns null for a missing object.
- C-06 (events raised inside `[Create]`), C-14 (value-object constructors throw).
- D12: a command is a static `[Execute]`; `QuarterClose` and `PaymentIntake` are the existing precedents in the same file.
- `.claude/rules/design-snippets.md`.

---

## Constraints & Invariants

- Each reshaped test keeps its stated intent; only the operation it calls changes. In particular: the relayed event still reaches the consumer, and the ServerOnly event still does not; the ServerOnly test waits for the insert's relayed event before the update, then asserts that no second one arrives. The NoOp-relay test still makes a remote call (a Save). `Finalize_Remote` still proves a value crossed the boundary, through the command's return value. Every phase-order assertion is unchanged.
- `OpenDraft` returns the existing Draft order for a customer who has one and a new, unsaved order otherwise, in one round trip; it never inserts.
- `Money` and `Percentage` keep their operations and round-trip unchanged (the wire shape moves from a generated ordinal array to a named object, and nothing pins the array); `Order.Total` and the line arithmetic are unchanged.
- No `new` on a `[Factory]` type outside its own factory methods; no `[Remote]` on anything but entry points; no method-injected service stored in a field.
- `Design.Server` composition still resolves every service (`DesignServerCompositionTests`); the `IMoneyFactory` assertion goes with the factory, and `TaxRate`'s factory takes its place.
- Both solutions green.

---

## Steps

1. Pre-flight probe: a static `[Remote, Fetch]` on a `[Factory]` record returning null for an unknown key reaches the client as null (the shape compiles: `RecordTestObj`). If it does not, stop and ask; a Create that looks up a key is not the fallback (D3, R-I).
2. Make `Money` and `Percentage` plain records whose constructors throw (`Money`: a currency that is not a three-letter uppercase code; `Percentage`: a value outside 0–100). Rewrite `Money.cs`'s header and remarks from "value objects use `[Factory]` for self-hydration" to R-F, with the old shape as WRONG.
3. Add `TaxRate` (region, `Percentage`): a `[Factory]` record loaded from the client through a server-only tax table service, null for an unknown region, with an operation applying the rate to a `Money`. The table's implementation takes a name `RegisterMatchingName` does not match, and is registered explicitly in `ServerServices` and in the test server and local containers only.
4. Add create-or-fetch to `Order`: a server-only `[Fetch]` for a customer's Draft order (the store gains that lookup) and a class-level `[Remote, Execute]` `OpenDraft(customerName)` that returns it, or the local Create's result when there is none.
5. `ClassExecuteDemo`: Create becomes local and service-free, taking the id and name; each `[Execute]` builds its result through the injected factory's Create. Remarks updated to D3.
6. Events: `CheckoutOrder` becomes an `IFactorySaveMeta` entity with a local Create, clearing `IsNew` after insert as `Order` does; its `[Remote, Insert]` raises the relayed event and its `[Update]` the ServerOnly one. `Invoice.Finalize` and `InvoiceArchiver.Archive` move to static command classes (`[Remote, Execute]`, like `QuarterClose`), so `IInvoiceFactory` and `IInvoiceArchiverFactory` go and their callers resolve the delegates; `QuarterClose`'s "same as a class factory method" remark is revised to match.
7. Tests (below), CLAUDE-DESIGN (value-object rule and checklist line, create-or-fetch in the decision table and file index, the event examples at :38, :395 and :397, and the hand-written `[Remote, Create] Finalize` block at :234-246 that stages, flushes and commits), and ledger outcomes for C-06 and C-14 (C-14 notes that R-F settled the audit's premise) (what 004 fixes; 010, 011, 012 and T2 keep theirs).

---

## Acceptance

- [ ] From the client, `OpenDraft` returns the seeded Draft order with its lines for a customer who has one, and a new unsaved order for one who does not, each in one remote call `[integration]` · Must
- [ ] `Money` and `Percentage` reject an invalid value at construction, and a plain-record `Money` survives the round trip as an order line's property `[unit]` · Must
- [ ] From the client, `TaxRate` loads for a known region, is null for an unknown one, and applies its rate to a `Money`; the table is not registered on the client `[integration]` · Must
- [ ] `ClassExecuteDemo`'s Create runs on the client with no server call, and each `[Execute]` returns an instance built through the factory `[integration]` · Should
- [ ] The relay and phase tests keep their intent on the new operations: saved checkout relays its event, the ServerOnly update does not, finalize and archive run each phase at its drain point `[integration]` · Must
- [ ] No Design `[Create]` raises an event or writes, and none in this plan's files takes a service `[explicit-skip: grep of src/Design for [Create] bodies and CLAUDE-DESIGN's blocks]` · Should
- [ ] Both solutions green `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

- `Money.cs`: `[Factory] partial record Money(decimal Amount, string Currency = "USD")` with a static `[Create]`, `Zero`, `Add`/`Subtract` (throw on a currency mismatch), `Multiply`; `[Factory] partial record Percentage(decimal Value)` with `Zero`, `Full`, a static `[Create]`, `Of(Money)`. Header :1-28 teaches self-hydration through `IMoneyFactory`. Constructors validate nothing.
- Consumers: `OrderLine.cs:123, 142, 194` (`new Money`), `:90, 330` and `Order.cs:87` (`Money.Zero`); `DesignServerCompositionTests.cs:108` (`IMoneyFactory`); `SerializationTests.cs:27, 264-270` (comments); `CLAUDE-DESIGN.md:1095, 1217`. No `IPercentageFactory` consumer.
- `ClassFactoryWithExecute.cs`: `[Remote, Create] internal Task Create(string name, [Service] IExampleService)` :54; `RunCommand` `[Remote, Execute]` :108, `ScoreLocally` `[Execute]` with `ITextScorer` :144, `ArchiveOnServer` internal `[Execute]` :181, each `new ClassExecuteDemo()`. Tests: `ClassFactoryExecuteTests` (4), `LocalExecuteTests:97-100`. `IExampleService` is not in the Blazor client, but the test client has it (`RegisterMatchingName` runs on all three test containers, `DesignClientServerContainers.cs:302-303`); `ITextScorer` is on the client.
- `Order.cs`: `[Create] public void Create(string customerName, [Service] IOrderLineListFactory)` :198; `[Remote, Fetch] internal Task<bool> Fetch(int id, …)` :221. `IOrderDbContext` (`OrderPersistence.cs:50`) has `FindAsync(id)`, `Add`, `Remove`, `SaveChangesAsync`; no lookup by customer. Seed: Draft order 300 for `Customer_300`; submitted orders 100, 123, 200, 400, 456, 789.
- `FactoryEventRelayPattern.cs`: `CheckoutOrder` `[Remote, Create]` `Create` :118 (relayed) and `CreateWithServerOnlyEvent` :145, both with `IFactoryEvents`. Tests: `FactoryEventRelayTests` :47, :75, :136 (the ServerOnly test asserts no `OrderCheckoutCompleted` reached the relay; the NoOp test needs a remote call to reach the relay). The relay is fire-and-forget into one aggregator.
- `FactoryEventPhasesPattern.cs`: `Invoice` `[Create][Remote] Finalize` :233 (raises, drains AfterFlush through the coordinator); `InvoiceArchiver` `[Create][Remote] Archive` :380 (never drains). `QuarterClose` :321, `PaymentIntake` :455 and `StatementClose` :555 are already static `[Execute]`. Tests: `FactoryEventPhasesTests` :29 (asserts the returned invoice's Id and Total crossed the boundary), :52, :105; `DesignServerCompositionTests:144`. CLAUDE-DESIGN names them at :38 (`Invoice` as a class-factory example), :234-246 (a hand-written `[Remote, Create] Finalize` that stages, flushes and commits), :395 and :397.
- Precedent for create-or-fetch: the reference app's `StartForPatient` (fetch, return it if found, else create).
- No touched file has a `#region`.

---

## Punchlist

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| `OpenDraft` from the client: the stored Draft with its lines, or a new unsaved order; one call each | Must | `[integration]` | `Design.Tests.FactoryTests.CreateOrFetchTests.OpenDraft_FetchesTheCustomersDraft_WhenOneExists` (Id 300, not new, 2 lines, total 45, one call); `OpenDraft_CreatesANewUnsavedOrder_WhenTheCustomerHasNoDraft` (`Customer_100`, whose only order is Submitted, and an unknown customer: new, empty, one call, store holds no draft afterwards) | ✓ |
| `Money`/`Percentage` reject an invalid value; plain-record `Money` round-trips as a line property | Must | `[unit]` | `ValueObjectTests.Money_RejectsACurrencyThatIsNotAThreeLetterCode` (5 cases), `Money_KeepsItsCurrency_ThroughArithmetic`, `Percentage_RejectsAValueOutsideZeroToOneHundred` (2), `Percentage_AcceptsTheBounds` (2); round trip: `SerializationTests.ValueObject_SurvivesSerializationRoundTrip` (order fetched through the client; ordinal format) | ✓ (the round trip is pinned at `[integration]`, above the declared tier) |
| `TaxRate` from the client: known region, null for unknown, applies to `Money`; table not on the client | Must | `[integration]` | `ValueObjectTests.TaxRate_LoadsFromTheServer_AndAppliesToMoney` (one call; 6.5% of 200.00 is 13.00), `TaxRate_IsNull_ForAnUnknownRegion`, `TaxTable_IsRegisteredOnTheServer_AndNotOnTheClient` | ✓ |
| `ClassExecuteDemo`'s Create runs on the client with no call; each `[Execute]` builds through the factory | Should | `[integration]` | `LocalExecuteTests.Create_ClassFactory_Local_RunsOnClient` (no-wire stand-in), `Execute_ClassFactory_WithoutRemote_RunsOnClient` (`ScoreLocally` builds through Create on the client, no wire); `ClassFactoryExecuteTests.Execute_OnClassFactory_WorksThroughClientServer`, `Execute_OnClassFactory_WorksInLocalMode`, `Execute_InternalStaticWithoutRemote_RunsOnServer`, `Create_StillWorks_AlongsideExecute`. The tests pin each result's values; that they come through Create is in the code, not observable from a test | ✓ |
| Relay and phase tests keep their intent on the new operations | Must | `[integration]` | `FactoryEventRelayTests.Relay_EventRaisedInRemoteInsert_DeliveredToConsumer` (renamed from `…InRemoteCreate…`), `Relay_ServerOnlyFlag_EventNotRelayedToConsumer` (insert's event awaited, then the update adds none), `Relay_NoConsumerRegistration_NoOpDefaultUsed` (now saves, so a remote call reaches the relay); `FactoryEventPhasesTests.Finalize_Remote_RunsEachPhaseAtItsDrainPoint` (the command's result crosses), `Finalize_Logical_…`, `Archive_NeverDrained_AfterFlushHandlerRunsAtTheSweep`; `DesignServerCompositionTests.ServerComposition_RunsAPhasedFactoryOperationEndToEnd`. The library fix: `RemoteFactory.IntegrationTests…FactoryEventRelayTests.SequentialCalls_SameServerScope_EachBatchCarriesOnlyItsOwnEvents`, `FailedCall_SameServerScope_ItsEventsDoNotRideTheNextCall` (both red before the fix), `RemoteFactory.UnitTests…FactoryEventCollectorTests.Clear_DropsEverythingCollected_AndCollectingResumes` | ✓ |
| No Design `[Create]` raises or writes; none in this plan's files takes a service | Should | explicit-skip | grep of `src/Design` (.cs, .md, .razor) for `[Create]` bodies with `Raise(`, `SaveChanges`, `db.Add`, `Remove(`: no code hit (the remaining matches are prose naming `[Create]`). Remaining `[Remote, Create]`: `AuthorizedOrder`, `ParamAuthOrder`, `SecureOrder` (todo Punchlist, EXSET-009), `ExampleClassFactory` (a sequence, D14), `LazyLoadExample` (#114), CLAUDE-DESIGN WRONG blocks, `README.md` (EXSET-012) | ✓ |
| Both solutions green | Must | explicit-skip | `reviews/004-build.log` (0 errors; 2 warnings, the OrderEntry WASM notice), `reviews/004-test.log` (835 unit, 644 integration + 5 pre-existing skips; both TFMs), `reviews/004-design-build.log` (0 errors, 0 warnings), `reviews/004-design-test.log` (137; both TFMs) | ✓ |

---

## Gate Record

---

## Plan Amendments

### 2026-10-13 — Plan-review callouts folded in

- **Section affected:** Intent, Constraints, Steps 1, 3, 6, 7, Acceptance (Should bullet), Current State
- **What changed:** callouts 1–6 of `reviews/004-plan-review.md`:
  - The "no service in a Create" wording narrows to this plan's files, matching §2.3 and D14.
  - The tax table is registered explicitly, outside the naming convention.
  - The ServerOnly and NoOp relay tests keep their intent across insert and update.
  - The invoice commands become static classes.
  - The wire-shape claim is dropped.
  - A failed Step 1 probe is a stop-and-ask.
- **Why:** each was a detail the draft got wrong against doctrine or the code. None changes intent.
- **Discovery Log:** none

### 2026-10-13 — Relayed events leaked between calls in a shared server scope (library fix)

- **Section affected:** Scope (adds a library change), Acceptance bullet 5's evidence
- **Original said:** Design-only changes.
- **What changed:** `HandleRemoteDelegateRequest` clears the scoped `IFactoryEventCollector` as each remote call begins. The ServerOnly relay test was the first Design test to make two remote calls through one server scope, and it received the Insert's event twice: the collector was never cleared, so each response re-sent every earlier call's events, including a failed call's. ASP.NET's per-request scope hides this in production; the test harness shares one server scope. Phased work was already protected against a shared scope; the relay batch now matches. Integration and unit tests pin it (red before the fix).
- **Why:** Keith, 2026-10-13: option 1 (fix in the library); the harness's shared scope is techdebt #128.
- **Discovery Log:** 2026-10-13 entry in `todo.md`

---

## Notes

- 2026-10-13, Keith: create-or-fetch is `Order.OpenDraft(customerName)`, a class-level `[Remote, Execute]` calling a server-only `[Fetch]` for the customer's Draft, then the local Create. `ClassExecuteDemo` builds through the factory: Create local and service-free, `Create(int id, string name)`; each Execute calls the injected factory's Create; `Create_StillWorks_AlongsideExecute` takes the new signature. The service value object is `TaxRate(Region, Percentage Rate)`, a `[Factory]` record loaded `[Remote]` from a server-only `ITaxTable`, null for an unknown region, `Rate.Of(money)`; a probe settles Fetch vs Create. Events: `CheckoutOrder` becomes an `IFactorySaveMeta` entity (local Create; Insert raises the relayed event, Update the ServerOnly one); `Invoice.Finalize` and `InvoiceArchiver.Archive` become static `[Remote, Execute]` commands.
- Defaults, not asked: `Money` throws unless the currency is a three-letter uppercase code; `Percentage` throws outside 0–100.
