# EXSET-002 code review — 2026-10-12

**Reviewer:** code-reviewer (tight, per-plan). **Verdict:** CLEAN. No veto-tier findings. Build 0 errors; the 2 main-solution warnings are the OrderEntry WASM notice. Tests 0 failed.

The deliverable landed as scoped: all three `Create`s public with no `[Remote]`; only Order's Fetch/Insert/Update/Delete cross; the R-D cascade in `OrderLineList.Update` matches Neatoo's `PersonPhoneList.Update` (rows found/added/removed, each live line handed its row, removed lines purged after the loop, new-then-removed lines dropped before the cascade); `Order.Insert` reads its id after the single flush; R-I, R-E and D12 hold; all five plan-review callouts landed.

| # | Callout | Affects | Disposition |
|---|---|---|---|
| 1 | The in-memory store's remark overstated it ("as a tracking ORM's would be"); rows are live, adds/removes wait for the flush, no transaction or isolation between requests | AC-2 · Must | punched: remark reworded as a demonstration stand-in with no transaction, rollback or isolation |
| 2 | `OrderLineList.IsNew`/`IsDeleted` are read-only, against R-M's `get; set;` | AC-4 · Must | dismissed (todo Dismissed): R-M is for objects whose state crosses the wire; a collection's properties never cross (#119). Sentence added to the remark |
| 3 | `Home.razor` still said "Fetching order #123" while fetching the Draft order | AC-2 · Must | punched: message uses `DraftOrderId` |

Theoretical (not triaged): a missing line row throws `InvalidOperationException` from `Single` while Order throws `KeyNotFoundException`; `RemoveLine` accepts a line from another order; no positive control for `RemoteCallCount`; `CLAUDE-DESIGN.md:379` cites stale `Order.cs` line numbers (pre-existing); `src/Design/README.md:44-54` shows `[Remote, Create] public` and `IOrderRepository` (pre-existing; EXSET-012 sweeps it).
