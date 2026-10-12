# Order aggregate: R-C, R-D cascade, D14, R-E

**Plan #:** 002
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-2, AC-4
**Status:** Draft
**Last Updated:** 2026-10-11
**Plan-review opt-in:** set at Step 2
**Code-review opt-in:** set at Step 2
**Branch:** cut from EXSET at Step 2
**PR:** —

---

## Scope

Bring Design's `Order` / `OrderLineList` / `OrderLine` to the rulings: the child's and the list's `[Create]` public and local (R-C); the R-D cascade, in which Order's Insert/Update/Delete write through `IOrderRepository` and hand the row's child collection to the list factory's `Save`, the list tracks removed lines and hands each live child its row, each child maps its own row in its own `[Insert]`/`[Update]`/`[Delete]`, and load is symmetric with rows instead of tuples; Order's `[Create]` local (D14); hook documentation without validation (R-E); Save results reassigned (T13). Tests on the real client/server containers, with `AddLine` working on the client. Updates the CLAUDE-DESIGN sections that describe these. Does not touch `docs/` pages or other Design types.
