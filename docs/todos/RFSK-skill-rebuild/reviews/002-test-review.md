## Test Review — RFSK-002 — 2026-10-16 — Round 1

**Verdict: APPROVED**
**Logs:** build PASSED — `reviews/002-build.log` (0 errors, 2 warnings), `reviews/002-design-build.log` (0 errors, 0 warnings); tests — `reviews/002-test.log`: UnitTests 871 passed / 0 failed on net9.0 and net10.0, IntegrationTests 664 passed / 5 skipped / 0 failed on both; `reviews/002-design-test.log`: Design.Tests 177 passed / 1 skipped / 0 failed on both. No `[FAIL]` line in either test log.

### Veto-tier

None.

The `[integration]` bullet ("Every C# block on the page is an embedded `src/Design` region, each pinned") holds:
- **Every fence is inside an embed.** `SKILL.md` has 8 ```` ```cs ```` fences (lines 69, 141, 156, 180, 246, 259, 272, 291) and no `~~~` fences. Each one sits after an `<a id='snippet-…'>` anchor, between a `snippet:` / `endSnippet` pair (lines 67–133, 139–152, 154–172, 178–238, 244–255, 257–268, 270–281, 289–338). There are no hand-written or WRONG blocks.
- **Every region resolves to a single source.** Each of the 8 names has exactly one `#region` under `src/Design` (`OrderCheckout.cs:63`, `Order.cs:327` and `:364`, `OrderLine.cs:144`, `ClassFactoryWithExecute.cs:119`, `:154` and `:191`, `OrderSummary.cs:63`). The rendered checkout embed no longer contains "(R-I)", so the embed is current with the one-line comment change.
- **Each region is pinned on `DesignClientServerContainers`.** RFSK-001's review read and accepted the cited tests (checkout, summary, `OpenDraft`/`FetchDraft`, the three `ClassExecuteDemo` placements with the unit-tier guard split, line duality). Every cited method exists under those names.
- **The question about `OrderLine`'s child `Insert`/`Update`.** Both are pinned through the order at `[integration]`, but **not by `SaveTests`**. `SaveTests.Save_RoutesOnIsNewAndIsDeleted` adds a line and saves, but its only assertion is that the order is gone after the delete. Nothing in it checks the line. The real pins are:
  - `AggregateTests.Order_EditLinesFromClient_SavesExactlyThoseChanges` (`AggregateTests.cs:310`): the new line Widget C is inserted (Insert), Widget A's quantity 5 is written (Update), and the store holds exactly those rows.
  - `AggregateTests.Order_SavedResult_CanBeEditedAndSavedAgain` (`:362`): insert, then `IsNew == false`, then update to 3 on the second save rather than a second insert.
  - `OrderLineDualityTests.OrderLine_StaysAChild_OfItsOrder`: an update through the cascade, checked against the server row.

  The bullet is pinned, so this is not a veto. The Test Evidence row cites the wrong class and should name those `AggregateTests` methods.

Explicit-skip reasons: all honest. Bullets 1, 2, 4, 5 and 6 are claims about prose and how it agrees with VisionAlignment and the Neatoo skill. No product behaviour can be tested there. Bullet 7 is a grep: `lazy` appears once, at `SKILL.md:384`, in the removal warning. Bullet 8 is the logs, which are green. The only source change is inside a `/// <remarks>` comment (`OrderCheckout.cs:84`), so it adds no executable path.

Sacred tests: none changed. `git diff RFSK...HEAD` touches only the plan, `SKILL.md` and `OrderCheckout.cs`. No file under `src/Design/Design.Tests` or `src/Tests` changed.

### Callouts (≤ 5 Optional)

None. The plan adds no code path. Both Optional bullets are covered by what they declare.

### Tech-debt (untiered, ≤ 5, one line each)

- None.

### Could not verify

- The logs (11:34–11:35) are older than the commit that holds the `OrderCheckout.cs` comment change (11:35:50), so I could not show that the edit was in the build. The change is a doc comment, so neither the build nor the tests could differ.

### Theoretical (not triaged)

- None.

### Read report

- Read beyond the brief: `AggregateTests.cs:300-392` and `OrderLineDualityTests.cs` (to find the real child Insert/Update pins once `SaveTests` turned out not to assert them); `Order.cs:320-373` (to confirm the bounds of the two `Order` regions).
- Named but unused: the todo's Punchlist, Dismissed and Parked sections (all empty); the Discovery Log (its one entry is RFSK-001's, already closed).
