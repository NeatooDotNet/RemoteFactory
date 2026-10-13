# Order aggregate: R-C, R-D cascade, D14, R-E

**Plan #:** 002
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-2, AC-4
**Status:** Done
**Last Updated:** 2026-10-12
**Plan-review opt-in:** Yes (changes the Design aggregate every later plan embeds, and its pinned tests; documented rules R-C, R-D, R-E, R-I, D14)
**Code-review opt-in:** Yes (behavior-changing: Design's persistence path and client-side child creation)
**Branch:** EXSET-002-order-aggregate-cascade
**PR:** #121

---

## Scope

Bring Design's `Order` / `OrderLineList` / `OrderLine` to the rulings and make the aggregate work from the client: the child's and the list's `[Create]` public and local (R-C); the R-D cascade, in which `Order` makes or finds its own row, hands the row's line collection to the list factory's `Save`, the list makes, finds or removes each line's row, and each line maps itself into the row it is handed, with one flush by the root; load symmetric, rows handed down; `Order`'s `[Create]` local (D14); `[Fetch]` returning `false` for a missing order and `[Update]`/`[Delete]` throwing for a missing row (R-I); hook documentation without validation (R-E); save results reassigned in tests (T13). The repository becomes a row store with one in-memory implementation. Updates the CLAUDE-DESIGN sections that describe these. Does NOT touch `docs/` pages, the skill, or other Design types (`AuthorizedOrder`, `ParamAuthOrder`, `SecureOrder` keep their `[Remote, Create]` for plan 009).

---

## Intent

- A Blazor client fetches an order, sees its lines, adds a line, changes a quantity, removes a line, and saves; the server persists exactly that. Today the client can do none of it: lines arrived empty until v1.10.3, and `AddLine` fails after a remote fetch because the list's stored factory does not survive serialization.
- Design shows the one write path RemoteFactory endorses for plain classes (R-D, the Person example's cascade), so plans 005–010 can embed it instead of the reference app's shapes that write nothing.
- Every claim Design makes about `Order` is pinned by a test on the real client/server containers, not on the Local container.

---

## Framework & Architectural Alignment

- VisionAlignment §2 statements 2, 3, 4, 6 and §3's aggregate root, child entity and child collection rows.
- R-C (child `[Create]` public and local; other child operations `internal`), Neatoo D9 (client calls `itemFactory.Create(...)` then adds).
- R-D / Neatoo D10: the Person example's cascade (`Neatoo/src/Examples/Person/Person.DomainModel/PersonPhoneList.cs` `Update`): the root's `Save` hands its row's child collection to the list factory's `Save`; the list loops live and removed children; the root flushes once.
- D14 (a root `[Create]` is local unless creation needs the server), R-I, R-E, R-M (public `IsNew`/`IsDeleted`), D19 (interface-first).
- v1.10.3 (#119): a collection's own properties do not cross the wire, so a removed line's state lives on the line.
- `.claude/rules/design-snippets.md`.

---

## Constraints & Invariants

- `Order`, `OrderLine`, `OrderLineList` stay `internal partial` behind their public interfaces; `OrderLineList` stays `List<OrderLine>`-derived (the ordinal list Design teaches).
- No `[Remote]` on any child or list operation. Only `Order`'s Fetch, Insert, Update and Delete cross.
- No method-injected service is stored in a field (C-21).
- All other Design tests stay green and unchanged in intent. Order tests that change are listed in Current State; each keeps its original assertion intent unless a ruling reverses it, and those reversals are named.
- Both solutions build and test green.

---

## Steps

1. Replace the repository with a row store the server injects: an order row owning a collection of line rows, find / add / remove, and one flush that assigns new order ids. One in-memory implementation, seeded with the orders the tests and the Blazor page fetch (submitted ones, and a Draft one for the editing scenarios), registered on the server and the Local container.
2. `OrderLine`: `IFactorySaveMeta`; a client-generated identity on Create; `[Create]` public and local; `[Fetch]`, `[Insert]`, `[Update]` internal, each taking its row; a line is not new once inserted; a removed line keeps `IsDeleted`.
3. `OrderLineList`: `[Create]` public and local; `[Fetch]` takes the line rows; an internal write operation runs the R-D loop over live and removed lines; removing a line marks it deleted and keeps it until save, hidden from the public interface and the total; a new line removed before save is dropped, and removed lines leave the list once their rows are gone. The list itself is never new or deleted, and its public interface carries `IFactorySaveMeta` so the generated `Save` can route. No stored factory.
4. `Order`: `[Create]` public, local and synchronous; `[Fetch]` finds its row and returns `false` when absent; `[Insert]`/`[Update]` map the root row, hand its line collection to the list factory's `Save`, and flush once; `[Update]`/`[Delete]` throw when the row is gone. Domain verbs add a line built by the caller's line factory and remove a line.
5. Hook documentation: logging, setup and state reset only; no validation, no "you CAN throw".
6. Rewrite the Order tests on the client container: create on the client, add lines, save, refetch; change and remove lines and save; a new-then-removed line is never written; missing order fetch is null; update of a deleted row throws; save results reassigned.
7. Update the Blazor page and the composition test to the new shapes.
8. Update the CLAUDE-DESIGN passages for these rules (Pattern 1/1b, Anti-Pattern 1, Critical Rule 1, Lifecycle Hooks, Service Injection's `[Remote, Create]` row, Design Debt's "collection factory injection requires local mode", the Files table's "entity duality" claim for `OrderLine`, Critical Rule 2's `OrderLine.Create` example `:761-783`, the all-internal child factory statement `:828`, the Quick Decisions N+1 row `:374`) and the comment headers and remarks of the three files (including `OrderLine.cs:21-33` and `:160`).
9. Record outcomes in `audit-ledger.md` for the rows this plan closes or advances.

---

## Acceptance

- [ ] From the client: create an order locally, add two lines through the line factory, save; refetching returns the order with both lines and their values `[integration]` · Must
- [ ] From the client: fetch an existing order, change one line's quantity, remove another, add a third, save; refetching shows exactly that, and the removed line's row is gone from the store; a line added and removed before saving is never written `[integration]` · Must
- [ ] Fetching an order that does not exist returns null; saving an update for an order whose row was deleted throws to the caller `[integration]` · Must
- [ ] The order returned by a save can be edited and saved again: inserted lines update, removed lines stay gone `[integration]` · Must
- [ ] `Order.Create` and `OrderLine.Create` are public, shown by `Design.Client.Blazor` compiling calls to them; creating an order with its list makes no server call `[integration]` · Must
- [ ] Deleting an order whose row is gone throws to the caller `[integration]` · Should
- [ ] A line cannot be removed from a submitted order `[integration]` · Should
- [ ] A removed line does not appear in `Lines` or `Total` before save `[integration]` · Should
- [ ] CLAUDE-DESIGN and the three file headers state R-C, R-D, D14 and R-E as above, with no "AddLine only works in local mode" and no validation in hooks `[explicit-skip: documentation; checked by grep and review]` · Should
- [ ] Both solutions green `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

- `Order.cs`: `[Remote, Create] internal Create(customerName, [Service] IOrderLineListFactory)` :195; `[Remote, Fetch] internal Fetch(int, [Service] IOrderLineListFactory)` :209 builds `Customer_{id}`, Submitted, two tuples (Widget A 10.00×2, Widget B 25.00×1) for any id; `[Remote, Insert/Update/Delete] internal Task X([Service] IOrderRepository)` :233/:249/:263 never call the repository (Insert assigns a random id). `FactoryStartAsync` doc :129-139 lists "Validating preconditions" and "you CAN throw". `FactoryCompleteAsync` sets `IsNew = false` after Insert. `AddLine(name, price, qty)` :286 delegates to the list. `IOrderRepository` (`InsertAsync/UpdateAsync/DeleteAsync(IOrder)`) :342 and `InMemoryOrderRepository` :352.
- `OrderLine.cs`: `IOrderLine` exposes `int Id`. `[Create] internal Create(name, price, qty)` :140, `[Fetch] internal Fetch(id, name, price, qty)` :166; no writes, no `IFactorySaveMeta`. `OrderLineList` stores `IOrderLineFactory` in `_lineFactory` from Create/Fetch :218, :266, :277; `AddLine` uses it :306; `RemoveLine(OrderLine)` :320 just removes. D18 "N+1" wording :160.
- Registrations: `Design.Server/ServerServices.cs:101` `AddScoped<IOrderRepository, InMemoryOrderRepository>()`; Design test containers register `IOrderRepository` on server and local (`DesignClientServerContainers.cs:246-255`). Remote calls in the test containers share one server scope per `Scopes()` call (reviews/002-plan-review.md), so the tests cannot tell a scoped store from a singleton; `Design.Server` gives each request its own scope, so the store is a singleton there.
- Consumers: `Home.razor:15, 160` (`OrderFactory.Fetch(123)`, `Lines.Count`); `DesignServerCompositionTests.cs:91` (resolves `IOrderRepository`), `:104-105` (line and list factories), `:165` (create and save).
- Order tests and what happens to each:
  - `AggregateTests.Order_Create_InitializesWithEmptyLines` (client): kept; now local.
  - `Order_Fetch_LoadsChildEntities_InLocalMode` (local, 123): moves to the client; intent kept.
  - `Order_Save_RoutesToInsert_WhenNew` (local): moves to the client, reassigns the result; intent kept.
  - `Order_Save_RoutesToUpdate_WhenNotNew` (client, 456), `Order_Save_RoutesToDelete_WhenDeleted` (client, 789): result reassigned; Delete now asserts the row is gone. Intent kept.
  - `Order_AddLine_CreatesChildThroughAggregate_InLocalMode` and `Order_WorksInLocalMode`: **intent reversed by R-C**; their doc says AddLine only works in local mode. Rewritten to run on the client with the line factory.
  - `SerializationTests.Order_*` (local, 100) and `Collection_WorksInLocalMode` (local, 200): kept; `Collection_WorksInLocalMode`'s "DESIGN DECISION: Collections require local mode" remark is reversed by R-C and rewritten. `Collection_FetchedThroughClient_ArrivesWithItems` (v1.10.3) kept.
- Neatoo's Person cascade: `PersonPhoneList.Update(ICollection<PersonPhoneEntity>, [Service] IPersonPhoneFactory)` loops `this.Union(DeletedList)`: existing child finds its row by id, new child gets a new row added to the collection, deleted child's row is removed, live child is saved through the child factory with its row. The generated `IPersonPhoneFactory` is public with `Create` public and `Fetch`/`Save` as `internal` interface members; `Save(target, row)` takes the row.

---

## Punchlist

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Create locally, add two lines, save, refetch | Must | `[integration]` | `Design.Tests.FactoryTests.AggregateTests.Order_Save_RoutesToInsert_WhenNew` | ✓ |
| Change, remove, add lines; store holds exactly that; added-then-removed never written | Must | `[integration]` | `AggregateTests.Order_EditLinesFromClient_SavesExactlyThoseChanges` | ✓ |
| Missing fetch is null; update of a deleted row throws | Must | `[integration]` | `AggregateTests.Order_Fetch_ReturnsNull_WhenOrderDoesNotExist`, `AggregateTests.Order_Update_Throws_WhenRowWasDeleted` | ✓ |
| Saved result edited and saved again | Must | `[integration]` | `AggregateTests.Order_SavedResult_CanBeEditedAndSavedAgain` | ✓ |
| `Order.Create`/`OrderLine.Create` public; order-with-list creation makes no server call | Must | `[integration]` | `AggregateTests.Order_CreateAndEditLines_RunOnClientWithoutServerCall` (remote call count 0); `Design.Client.Blazor` builds with `Home.razor` calling `OrderFactory.Create` and `LineFactory.Create` (`reviews/002-build.log`). `OrderLineList.Create` is public in the generated `IOrderLineListFactory` (read, not tested: the test project sees internals and the guard switch is per process) | ✓ |
| Saved result after a removal can be saved again | Must | `[integration]` | `AggregateTests.Order_SavedResultAfterRemoval_CanBeSavedAgain` (red when the list's purge is removed: `InvalidOperationException` on the second save) | ✓ |
| Delete of a gone row throws | Should | `[integration]` | `AggregateTests.Order_Delete_Throws_WhenRowWasDeleted` | ✓ |
| RemoveLine refused on a submitted order | Should | `[integration]` | `AggregateTests.Order_RemoveLine_Refused_WhenSubmitted` | ✓ |
| Removed line hidden from Lines and Total before save | Should | `[integration]` | `AggregateTests.Order_RemovedLine_HiddenFromLinesAndTotal_BeforeSave` | ✓ |
| CLAUDE-DESIGN and file headers state R-C, R-D, D14, R-E | Should | explicit-skip | grep of `CLAUDE-DESIGN.md`, `Order.cs`, `OrderLine.cs`, `AggregateTests.cs`, `SerializationTests.cs` for "N+1" as a reason, "local mode only", "AddLine only works", "validation, logging": none left; "N+1" survives only as "not an N+1 problem" | ✓ |
| Both solutions green | Must | explicit-skip | `reviews/002-build.log` (0 errors; 2 warnings, the WASM SQLite notice), `reviews/002-test.log` (831 unit, 636 integration + 5 pre-existing skips, 114 Design; both TFMs) | ✓ |

---

## Gate Record

- Round 1 (2026-10-12): test-reviewer CONCERNS: 2 must-cover (second save after a removal unpinned: pinned, proven red without the fix; list `Create` publicness unprovable from tests: bullet narrowed, amendment below), 2 should-cover (Delete of a gone row, RemoveLine guard: both pinned). code-reviewer CLEAN with 3 callouts: store remark reworded, Blazor message fixed, read-only list flags dismissed (todo Dismissed). `reviews/002-test-review.md`, `reviews/002-code-review.md`.
- Round 2 (2026-10-12): CLEAN. Both must-cover items closed, both should-cover pinned, logs green. Done.

---

## Plan Amendments

### 2026-10-12 — Tier of the removed-line bullet

- **Section affected:** Acceptance bullet 6
- **Original said:** `[unit]`
- **What changed:** `[integration]`
- **Why:** `OrderLineList` is reached through its factory (no `new` on a `[Factory]` type); the test builds it through the client container with no server call.
- **Discovery Log:** none (detail, not intent)

### 2026-10-12 — The list's public Create is read, not tested

- **Section affected:** Acceptance bullet 5
- **Original said:** `Create` on `Order`, `OrderLine` and `OrderLineList` is public; `Design.Client.Blazor` compiles calls to them.
- **What changed:** The bullet names `Order.Create` and `OrderLine.Create`, which the Blazor page calls, plus the no-server-call proof; `OrderLineList.Create` is recorded as public in the generated interface.
- **Why:** No natural client code builds a bare list, the test project has `InternalsVisibleTo`, and the server-only guard switch is fixed per process, so a test cannot observe the member's visibility. A page call added only to prove it would be copied as a pattern.
- **Discovery Log:** none (evidence, not intent)

### 2026-10-12 — Seeded ids and the tests that moved

- **Section affected:** Step 1, Step 6
- **Original said:** Seed the orders the tests fetch; rewrite the Order tests on the client.
- **What changed:** Order 400 joined the seed (`SaveMetaProperties_SurviveSerializationRoundTrip` fetches it). That test, `ModifiedObject_SerializesToServer` and `ValueObject_SurvivesSerializationRoundTrip` moved to the client container; the save-meta test now proves IsNew and IsDeleted cross by saving (Insert, then Delete and a null refetch). `DesignClientServerContainers` gained a remote call counter.
- **Why:** Create is now local, so "IsNew survived the round trip from Create" no longer had a round trip to observe; moving to the client makes each test serialize what its name says it serializes.
- **Discovery Log:** none (detail, not intent)

---

## Notes

- Framework guidance: `docs/VisionAlignment.md` §0 and §2 are the doctrine for this todo; the repository skill is what Todo 2 rebuilds and was not used as a source.
- 2026-10-12: Keith accepted the six design choices (row store `IOrderDbContext`, `Guid` line ids, removed lines flagged in the list, client adds through the line factory, the R-C-reversed tests rewritten, authorized orders left for 009) and all five plan-review callouts as proposed; the callouts are folded into Steps 1–4 and 8 and Acceptance above (`reviews/002-plan-review.md`).
