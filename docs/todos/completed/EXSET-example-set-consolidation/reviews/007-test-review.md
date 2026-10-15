# EXSET-007 test review — 2026-10-15

## Round 1 — APPROVED (no veto-tier; 3 callouts)

**Reviewer:** test-reviewer, against `70af11b`, diffed from `93da2e9`. Logs green:
- main build (`007-build.log`): 0 errors, 2 warnings. Both are the known OrderEntry WASM native-reference notice.
- Design build (`007-design-build.log`): 0 errors, 0 warnings.
- reference app (`007-refapp-build.log`): 0 errors, 0 warnings.
- tests (`007-test.log`, `007-design-test.log`): unit 839, integration 646 + 7 skips, Design 160, each on both TFMs. No failures.

The counts moved by what was added:
- Design went 155 → 160, the five `[Fact]`s in `OperationsTests`.
- Unit and integration are unchanged, as expected for a plan with no library change.

Every cited test exists and runs at the declared tier. `DesignClientServerContainers.Scopes()` builds fresh server, client and local providers on each call (`DesignClientServerContainers.cs:282-284`), so each test's singleton store and order context start empty or seeded. They are not shared across tests.
- **1. CustomerPreferences.**
  - `CreateConstructor_RunsOnTheClient_WithItsDefaults` asserts the constructor default (`DeliveryWindow == "Any"`), `IsNew`, and `RemoteCallCount == 0` on the client's `ServerServiceProvider`. That count goes up on every request through the serializing stand-in (`:97`, `:123`), so the Create ran locally.
  - `SaveUpsert_InsertsThenUpdatesTheOneRow`: see "SaveUpsert" below.
  - `Fetch_UnknownCustomer_ReturnsNull` asserts null from the client factory.
- **2. Commands.**
  - `SubmitOrder_…` saves a draft with one line through the client. It asserts `Submitted`, and it reads the server's `IOrderDbContext` row to assert `OrderStatus.Submitted`. A second submit of the same order (now not a draft) returns `NotSubmittable`, and `999_999` returns `NotFound`. New ids start at 1000 and the seeds are below that, so `999_999` is genuinely missing.
  - `CancelOrder_…` asserts the stored row is `Cancelled`, and that a missing id throws `KeyNotFoundException`. That is the in-process type, as plan-review callout 6 amended.
- **3. Start hook and IsNew.** The Insert half and `IsNew` are pinned. The Update half is not: see finding 1.
- **5. List and create-or-fetch regions.** The existing tests are present and unchanged:
  - the cascade tests in `AggregateTests` (`Order_EditLinesFromClient_…`, `Order_SavedResult_CanBeEditedAndSavedAgain`, `AddLine` from the client at `:471`);
  - `Order_Fetch_LoadsChildEntities` for the list's Fetch from rows;
  - `OrderLineDualityTests`;
  - `CreateOrFetchTests` (both `OpenDraft` paths).

  The new regions around these lines are marker-only; the code inside them is unchanged.

**SaveUpsert: what the test observes (the brief's question).**
- **Insert path, and the start hook on Insert: observed.**
  - Nothing but `FactoryStartAsync` writes `LastSavedAt`. The constructor leaves it null, and `Upsert` copies it to the row unchanged (`CustomerPreferences.cs:55-58`, `:91`, `:107-110`).
  - So `LastSavedAt` non-null and `>= before` after the first save shows the hook ran on the server and its value came back across the wire.
  - `IsNew == false` shows `Upsert` ran (it sets `IsNew` after the write, `:92`). That is the flag the generated save routes on.
- **Update path: observed as the store sees it.** The second save changes the one stored row:
  - `row.DeliveryWindow == "Evening"`, read from the server's store, so the second save crossed to the server;
  - `Count == 1`, which is meaningful because the store is fresh per `Scopes()`;
  - `NotifyBySms` carried from the first edit.

  Both branches of `SaveUpsert` call the same body, so which `FactoryOperation` the generator passed is not observable; plan-review callout 5 accepted that, with the `IsNew` assertion as the substitute.
- **Start hook on Update: not observed.**
  - `Assert.True(row.LastSavedAt >= firstStamp)` (`OperationsTests.cs:67`) also holds when the hook does not run on the second save.
  - In that case the object carries `firstStamp` to the server, `Upsert` writes it, and `>=` passes on equality.

**Sacred-test check: clean.**
- `DesignServerCompositionTests.cs` gains two `Assert.NotNull(GetRequiredService<…>)` lines (`ISupportLog`, `ICustomerPreferencesStore`). Nothing else changed.
- `DesignClientServerContainers.cs` adds the store to the server and local lists only.
- No other test file changed. The five deleted reference-app files are sample sources under `EmployeeManagement.Domain` and `.Application`, not tests. The reference app builds without them, so no test referenced their types.
- **Submit's existing behaviour is still pinned.** The success path (a draft with lines becomes `Submitted`) is still asserted, unchanged, by:
  - `AggregateTests.cs:472-475`;
  - `SerializationTests.cs:427-432`, through a save;
  - `ReadModelAndContextTests.cs:140-158`, through `Checkout`.

  `Submit`'s two conditions and their messages are the same as before, now folded into `!CanSubmit` (`Order.cs:425-434`). Before this plan, no test asserted either throw. The plan did not remove a pin there, so that is not a weakening (see tech-debt).

Explicit-skip spot checks (bullets 4, 6, 7):
- **4. Hook timing.**
  - The async start and complete hooks are emitted only `if (isDomainMethodTask)` (`ClassFactoryRenderer.cs:733-739`, `:755-760`).
  - Constructor and static operations take the read lifecycle, which gets only a sync `IFactoryOnComplete` on the result (`:428`, `:563-578`).
  - The cancelled hooks are only in the write-style async `catch` (`:609-613`).

  The page's "When Each Hook Runs" (`factory-operations.md:453-459`) matches those lines.
- **4. The complete hook's pin.** It holds. `Order.Insert` does not touch `IsNew` (`Order.cs:268-279`); only `FactoryCompleteAsync` clears it (`:182-189`). So `Assert.False(saved.IsNew)` after an insert (`SerializationTests.cs:433`, `AggregateTests.cs:162`) pins the complete hook.
- **6. Fences.** No language fence on `factory-operations.md` lies outside a `snippet:`/`endSnippet` pair. All 14 embeds source from `src/Design`: Order 7, CustomerPreferences 3, OrderCommands 2, OrderLine 1, OrderSummary 1.
- **6. Retired names.** None of the nine retired region names (including `operations-params-array-batch`) is defined or embedded in a `.cs` or `.md` file outside `docs/todos`.
- **7. mdsnippets.** `007-mdsnippets.log` has no error, exception or missing-snippet line.

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|
| 1 | Callout | **Bullet 3 (`· Should`): "stamps `LastSavedAt` on … Update" is vacuous.**<br>`SaveUpsert_InsertsThenUpdatesTheOneRow` asserts `row.LastSavedAt >= firstStamp` (`OperationsTests.cs:67`). If the hook skipped Update, the second save would write the carried `firstStamp`, and `>=` passes on equality. The Insert half and "`IsNew` false after the first save" are genuinely pinned.<br>Suggested shape (integration, same test): before the second save, set `preferences.LastSavedAt = null` on the client, then assert the stored row's stamp is non-null and `>= firstStamp`. Asserting it strictly greater than `firstStamp` also works, but depends on clock resolution.<br>Tiered as a callout because `Should` reads as Optional (the conversion checklist's default; same reading as 006's review). If Keith reads `Should` as Required, this is veto-tier. | AC-3, AC-4 | **Punch** — the second save clears `LastSavedAt` on the client first, and the stored row is asserted to carry a stamp `>= firstStamp`. |
| 2 | Callout | **`Order.CanSubmit`'s empty-draft conjunct has no test.** `Order.cs:425`: `Status == Draft && Lines.Count > 0`.<br>Bullet 2's `NotSubmittable` case is an order that is not a draft, so only the `Status` conjunct is exercised.<br>**Reachable by:** `OrderCommands._SubmitOrder` (`OrderCommands.cs:65`), called from the client, on a stored draft with no lines. `Create` then `Save` with no `AddLine` stores one. It is also reachable by any screen reading `IOrder.CanSubmit`, which the plan added for that purpose (R-E).<br>Suggested shape (integration): submit a saved empty draft through the client and assert `NotSubmittable`, with the row still `Draft`. | AC-3, AC-4 | **Punch** — `SubmitOrder_EmptyDraft_IsNotSubmittable_AndStaysADraft`. |
| 3 | Callout | **`CustomerPreferences.Fetch`'s found branch has no test.** `CustomerPreferences.cs:73-85`: the row mapped onto the object, and `IsNew = false`.<br>Only the unknown-customer null is pinned. Fetch is not embedded on the page, but the type's header states the flow "fetches the stored one, edits it, and saves" (`:5-7`).<br>**Reachable by:** `ICustomerPreferencesFactory.Fetch` from the client, after any `SaveUpsert`. This branch is also where the start hook runs for an operation other than Insert or Update, and must not stamp.<br>Suggested shape (integration): `SaveUpsert`, then `Fetch` the same customer through the client. Assert the stored values, `IsNew == false` and the unchanged `LastSavedAt`. | AC-3 | **Punch** — `Fetch_StoredCustomer_ReturnsTheSavedValues`: values, `IsNew == false`, stamp unchanged by the fetch. |
| 4 | Tech-debt | `OperationsTests` discards the local scope from `Scopes()` and never disposes it, in all five tests. This is the same pattern 013's review flagged and punched. | — | **Punch** — every test disposes server, client and local. |
| 5 | Tech-debt | `README.md` is modified and uncommitted in the working tree: `order-interface` gains `bool CanSubmit { get; }`, plus attribution lines. At `70af11b`, README still renders `order-interface` without `CanSubmit`, while the Test Evidence row for bullet 7 says README gains it. | — | **Punch** — committed with the gate. |
| 6 | Tech-debt | `Order.Submit`'s two `InvalidOperationException` paths ("not in draft status", "empty order") have never been asserted, before this plan or after it. | — | **Punch** — `Submit_WhenCanSubmitIsFalse_Throws` asserts both messages. |

Could not verify:
- That the logs were produced from exactly `70af11b`. They are timestamped 12:22, and the commit is 12:23. The Design count of 160 matches the five new tests, so I took them as the committed tree. The uncommitted README change suggests mdsnippets ran after the tree was staged.

Theoretical (not triaged):
- Which `FactoryOperation` `SaveUpsert` passes on each save (Insert, then Update) is not observable, because both branches call one body and the hook treats them alike. Plan-review callout 5 accepted `IsNew == false` as the substitute, so this is not a gap against bullet 1 as amended.
