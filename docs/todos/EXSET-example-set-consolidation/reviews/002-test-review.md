# EXSET-002 test review — 2026-10-12

## Round 1 — CONCERNS

**Reviewer:** test-reviewer (tight). Logs green (build 0 errors; Design 111, unit 831, integration 636 + 5 pre-existing skips, both TFMs). Sacred-test check clean: every kept Order test keeps or strengthens its assertions; the two R-C-reversed tests (`Order_AddLine_CreatesChildThroughAggregate_InLocalMode`, `Order_WorksInLocalMode`) were rewritten with Keith's approval of 2026-10-12 and their assertions survive in `Order_Save_RoutesToInsert_WhenNew` and `Order_DomainMethods_WorkOnClient`; the four changed `SerializationTests` keep or strengthen theirs; nothing outside the Order aggregate changed.

| Tier | Finding | Affects | Disposition |
|---|---|---|---|
| must-cover | "Removed lines stay gone" after a second save was unpinned: deleting the list's `RemoveAll(IsDeleted)` left every test green while the next save would throw | AC-2 · Must | pinned: `Order_SavedResultAfterRemoval_CanBeSavedAgain`; proven red without the purge (`InvalidOperationException: Sequence contains no matching element`) |
| must-cover | "`OrderLineList.Create` is public" has no consumer outside the domain assembly; the test project sees internals and the guard switch is per process, so no test can observe it | AC-2 · Must | bullet narrowed to what is observable (Plan Amendment "The list's public Create is read, not tested"); the generated interface shows it public |
| should-cover | `Order.Delete` throwing for a gone row untested | AC-2 · Must | pinned: `Order_Delete_Throws_WhenRowWasDeleted` |
| should-cover | `Order.RemoveLine` guard on a submitted order untested | AC-2 · Must | pinned: `Order_RemoveLine_Refused_WhenSubmitted` |

Tech-debt: none. Theoretical: "never written" is pinned by the store's end state, which cannot tell "never added" from "added then removed in one flush" (moot: a new line is dropped on the client); no positive control for `RemoteCallCount` (wiring confirmed by reading).

After round 1: Design 114 tests, both TFMs; main solution unchanged. Logs refreshed in `002-build.log`, `002-test.log`.

## Round 2

**CLEAN.** Both must-cover items closed: `Order_SavedResultAfterRemoval_CanBeSavedAgain` asserts the store's single remaining line after the second save; bullet 5, its Test Evidence row and the amendment agree. Both should-cover tests assert the behavior. Logs green (Design 114, unit 831, integration 636 + 5 skipped, both TFMs). Tech-debt: the plan's Design count read 111 (fixed to 114). Plan Done.
