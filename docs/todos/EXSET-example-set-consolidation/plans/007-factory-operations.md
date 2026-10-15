# Factory operations page

**Plan #:** 007
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** Done
**Last Updated:** 2026-10-15
**Plan-review opt-in:** Yes (new Design shapes: a `[Create]` constructor, an `[Insert, Update]` upsert and two commands that act through a root; the most-embedded page after README)
**Code-review opt-in:** Yes (the page is rewritten against generator behaviour)
**Branch:** EXSET-007-factory-operations
**PR:** —

---

## Scope

Re-point every C# block on `docs/factory-operations.md` to `src/Design`. That covers the 23 `operations-*` and `collection-factory-*` embeds, and the 10 hand-written blocks: nine fake "generated interfaces" and a placeholder fire-and-forget Insert. `collection-factory-basic` also renders in the skill's `advanced-patterns.md`, and it re-points with them.

**Design gains** (Keith, 2026-10-15). The other forms the page documents become prose with no example: a static `[Create]`, `params`, and `IFactoryOnCancelled`.
- **A `[Create]` constructor.** It sits beside R-J's warning: the constructor also runs on deserialization.
- **An `[Insert, Update]` upsert,** with its generated name, `SaveUpsert`.
- **Two static commands that act on Order through its root.** Each fetches through `IOrderFactory`, calls a domain verb, and saves (statement 6):
  - submit returns an outcome (`Submitted`, `NotFound`, `NotSubmittable`), reading Order's new `CanSubmit` verdict first (R-E, R-I, U-05);
  - cancel throws when the order is missing (R-I).
- **A start hook with real work, on `CustomerPreferences`:** it stamps `LastSavedAt` on Insert and Update. Order's hooks keep their behaviour, and their false comments are corrected.

**Parameter order** (Keith, 2026-10-15). The page states one rule for every operation: values, then `[Service]` parameters, then the `CancellationToken`. Design follows it. The generator does not enforce it: class operations tolerate other orders, while `[Execute]` breaks in generated code. That is issue #135, for Todo 3's analyzers.

**Findings to fix:**
- C-05: static `[Create]` as create-or-fetch;
- C-09: a command writing another aggregate's row;
- C-10: delete wording;
- C-11: hooks as validation gates;
- C-15: `bool` `[Create]` and a business filter in `[Fetch]`;
- C-01: "`[Remote]` executes on server";
- C-02 and U-01: child visibility and creation;
- C-19 and C-20;
- C-28 and C-30: persistence-routing framing; `[Execute]` "decides which persistence path";
- I-02: "server-only" `[Service]`;
- I-05 and I-06: parameter defaults and ordering;
- the page's "No `[Remote]` on collection" (VisionAlignment allows a `[Remote]` list Fetch) and "child factory is constructor-injected" (Design rejects storing it);
- I-12: hand-written generated interfaces;
- T-03: write methods on the interface;
- T-04: `TryFetch`;
- T-17: seven operations;
- T-22;
- U-02 and U-03: when Create needs the server; the cascade on plain classes;
- U-05: a missing target in a command;
- U-10 and U-11: who maps a child's row; bare `List<Child>`.

Delete the reference-app sources: `OperationsSamples.cs`, `ExecuteOperationSamples.cs`, `ClassExecuteSamples.cs`, `LifecycleHookSamples.cs` and `OrderLineCollectionSamples.cs`, including the orphan `operations-params-array-batch`.

Does NOT touch: `save-operation.md` (008), `service-injection.md` (010), the skill's prose (Todo 2).

---

## Intent

- A reader learning the operations sees the Order aggregate's real Create, Fetch, the writes, create-or-fetch, the list cascade and the hooks, each compiled and tested.
- The page describes the generated factory as it is:
  - `Save`, never Insert/Update/Delete on the interface;
  - nullable reads;
  - a `CancellationToken` on every method;
  - no defaults carried;
  - `SaveUpsert` for a method named `Upsert`.
- Commands change aggregates through their root, never by writing another aggregate's row.

---

## Framework & Architectural Alignment

- **Rulings:**
  - D3: Create is `new`, Fetch is a key;
  - D11: create-or-fetch is an `[Execute]`;
  - D12: a static command is a command;
  - D14: a root's Create is local;
  - D15: persistence routing is one use;
  - R-C, R-D, R-E, R-I, R-J, R-M, R-O;
  - statement 6: each object writes its own row; a command goes through the root.
- **`.claude/rules/design-snippets.md`.**
- **Substrate from EXSET-002–006:**
  - `Order` (`readme-domain-model`, `OpenDraft`, the hooks);
  - `OrderLineList` (the R-D cascade);
  - `OrderSummary` (`aspnetcore-cancellation`).

---

## Constraints & Invariants

- **Embeds.** Every C# block on the page is an embed from `src/Design` or a short WRONG block.
- **Region map.** New regions nest inside existing ones where they share lines; nesting strips the inner markers. Migrating names keep their names, except these, which retire into existing regions:
  - `operations-fetch-bool-return` → `operations-fetch-instance`;
  - `operations-cancellation` and the `operations-params-*` value, service and cancellation regions → `aspnetcore-cancellation`, which shows the standard parameter order;
  - `collection-factory-parent` and `operations-remote` → `readme-domain-model`;
  - `operations-create-static`, `operations-create-return-types`, `operations-params-array` and `operations-lifecycle-oncancelled` → prose.
- **README's regions keep their shape.** A new nested region adds no line to `readme-domain-model`'s output.
- **New Design types are interface-first,** with public setters and `IsNew`/`IsDeleted` (R-M), and no `new` on a `[Factory]` type outside its own factory methods.
  - A root's Create is local (D14).
  - The `[Create]` constructor has no side effect (R-J).
- **Commands act through the root.** A command fetches the aggregate through its factory, calls its verb, and saves. A missing order is a result in a command with a result, and an exception in a `Task`-only command (R-I).
- **Hooks are not checks.** The start hook prepares state and is not a check (R-E). The page says when each hook actually runs: the async start hook only for `Task`-returning operations, and no start hook for constructor, static, or class-level `[Execute]` operations.
- **The whole still works:** both solutions green, mdsnippets swallows nothing, and the reference app builds.

---

## Steps

1. **Design shapes:**
   - **The new type:** `CustomerPreferences`, a small Order-domain root with:
     - a `[Create]` constructor;
     - a `[Remote, Fetch]` by customer;
     - a `[Remote, Insert, Update]` `Upsert` over a server-only store.
   - **Two static commands on Order through `IOrderFactory`:** submit, which returns whether the order was found, and cancel, which throws when it is missing.
   - **`CustomerPreferences`'s start hook** stamps `LastSavedAt` on Insert and Update. Correct Order's hook comments: when each one runs, and that `IFactoryOnCancelled` is for cancellation.
   - **Order** gains `CanSubmit`; `Submit` throws only when it is false.
   - **The new store** is registered explicitly as a singleton on the server, in the harness's server and local lists, and in `DesignServerCompositionTests`.
2. **Regions,** nested in Order and OrderLineList where they share lines; the region map above.
3. **Tests through the client containers:**
   - the constructor Create runs locally;
   - `SaveUpsert` inserts, then updates;
   - Fetch returns null when missing;
   - submit returns true and the order is Submitted; a missing order returns false;
   - cancel of a missing order throws;
   - the start hook's normalization persists.
4. **Re-point the page and the skill embed; delete the reference-app sources;** build `EmployeeManagement.sln`.
5. **Rewrite the page's prose against the findings and the generator:**
   - the operation table without "persistence stage";
   - six operations;
   - create-or-fetch as an `[Execute]` (D11);
   - the generated interface as prose;
   - the bool Fetch → null rule;
   - no `bool` Create (R-E);
   - parameter order;
   - lists per R-D;
   - when each hook runs.

   Keep the inbound anchors `#execute-operation`, `#method-parameters` and `#combining-operations`.
6. **Verify and record:** mdsnippets, both solutions, and the ledger outcomes for the findings above.

---

## Acceptance

- [x] `CustomerPreferences`:
  - its `[Create]` constructor runs locally;
  - `SaveUpsert` inserts, then updates, through the client;
  - Fetch is null for an unknown customer.

  `[integration]` · Must
- [x] Order's commands:
  - submit through the client changes a stored draft to Submitted and returns `Submitted`;
  - it returns `NotFound` for a missing id, and `NotSubmittable` for an order that is not a draft;
  - cancel of a missing id throws.

  `[integration]` · Must
- [x] `CustomerPreferences`' start hook stamps `LastSavedAt` on Insert and Update, and `SaveUpsert` leaves `IsNew` false after the first save `[integration]` · Should
- [x] The page's statements of when each hook runs match the generator source `[explicit-skip: source-cited review; the start and complete hooks are also pinned by tests]` · Should
- [x] The page's regions for the list (Create, Fetch from rows, AddLine) and create-or-fetch are pinned by the existing cascade and create-or-fetch tests `[integration]` · Must
- [x] On the page:
  - every C# block is an embed from Design or WRONG;
  - no region is defined twice, and none retired is embedded;
  - none of the findings' text remains;
  - the reference-app sources are gone, and the reference app builds.

  `[explicit-skip: greps; EmployeeManagement.sln build]` · Must
- [x] mdsnippets clean; both solutions green `[explicit-skip: meta]` · Must

---

## Current State (Pre-Flight)

See the recon notes (2026-10-15). The points that matter:

- **The page:** 813 lines, 23 embeds, 10 hand-written blocks, none labelled WRONG. The source files are `OperationsSamples.cs` (16 regions, private `IsNew`/`Id` setters throughout), `ExecuteOperationSamples.cs` (`[SuppressFactory]`), `ClassExecuteSamples.cs`, `LifecycleHookSamples.cs` and `OrderLineCollectionSamples.cs`.
- **Design already has:**
  - Order Create, Fetch and writes, inside `readme-domain-model`;
  - `OpenDraft` (create-or-fetch);
  - `OrderLineList` Create, Fetch(rows), the `[Update]` cascade and `AddLine`;
  - `aspnetcore-cancellation`;
  - Order hooks: start is a no-op, and complete resets `IsNew`.
- **The generator:**
  - write methods are never on the interface (`ClassFactoryRenderer.cs:139`);
  - a bool Fetch returns nullable, and there is no `TryFetch`;
  - the only `Try*` is `TrySave`;
  - defaults are not captured;
  - a combined write is named `Save` plus the method name without its operation prefix;
  - the start hook is skipped for constructor and static Create/Fetch and for class `[Execute]`, and the async start hook runs only for `Task`-returning operations, so `Order.Create`, which is synchronous, never calls it, against `Order.cs:139-140`.
- **Parameter order:** probed. `Fetch([Service] s, int id)` works; a static `[Execute]` with the same order fails in generated code (#135).
- **Delete:** the page's `:194` "root deletes its own row" matches Design's Delete, where the line rows go with the order's row. C-10's audit wording ("each entity deletes its own row") is about the Save cascade, where `OrderLineList` removes the rows of deleted lines (R-D). The page states both.

---

## Punchlist

---

## Test Evidence

| Bullet | Pinned by | Tier |
|---|---|---|
| 1. CustomerPreferences | `OperationsTests`: `CreateConstructor_RunsOnTheClient_WithItsDefaults` (no remote call), `SaveUpsert_InsertsThenUpdatesTheOneRow`, `Fetch_UnknownCustomer_ReturnsNull`, `Fetch_StoredCustomer_ReturnsTheSavedValues` | integration |
| 2. Commands | `OperationsTests.SubmitOrder_ChangesTheOrderThroughItsRoot_AndReportsWhatItDid` (Submitted, NotSubmittable, NotFound); `SubmitOrder_EmptyDraft_IsNotSubmittable_AndStaysADraft` (the empty-draft conjunct of `CanSubmit`); `Submit_WhenCanSubmitIsFalse_Throws` (both messages); `CancelOrder_CancelsAStoredOrder_AndThrowsForAMissingOne` (a retried cancel succeeds) | integration |
| 3. Start hook and IsNew | `SaveUpsert_InsertsThenUpdatesTheOneRow` asserts `LastSavedAt` stamped by the first save and `IsNew` false after it; the client clears the stamp before the second save, and the stored row carries a fresh one. `Fetch_StoredCustomer_ReturnsTheSavedValues` asserts a Fetch leaves it unchanged | integration |
| 4. Hook timing | explicit-skip: stated from `ClassFactoryRenderer.cs` (425-428, 563-578, 609-613, 732-739, 765-784, 834-880). The start hook is pinned by bullet 3, and the complete hook by Order's existing `IsNew` tests (`AggregateTests`, `SerializationTests`) | explicit-skip |
| 5. List and create-or-fetch regions | existing `AggregateTests` (cascade), `OrderLineDualityTests`, `CreateOrFetchTests` (`OpenDraft`) | integration |
| 6. Page | explicit-skip: every fence on the page is an embed from `src/Design` (Order, OrderLine, OrderSummary, CustomerPreferences, OrderCommands); the finding-text grep is clean; `reviews/007-refapp-build.log` | explicit-skip |
| 7. Meta | explicit-skip: `reviews/007-mdsnippets.log`. README, getting started and the-problem gain only `CanSubmit` (amended) and attribution lines. Logs: `007-build.log`, `007-test.log`, `007-design-build.log`, `007-design-test.log` (unit 839; integration 646 + 7 skips; Design 163, up 8; both TFMs) | explicit-skip |

---

## Gate Record

- **Test review** ([007-test-review.md](../reviews/007-test-review.md)), round 1: APPROVED, with three callouts and three tech-debt items, all punched:
  - the Update stamp is pinned: the client clears it before the second save;
  - three new tests: an empty draft is `NotSubmittable` and stays a draft, a stored customer fetches back unchanged, and `Submit` throws with both messages;
  - every test disposes its scopes, and README's `CanSubmit` line is committed.
- **Code review** ([007-code-review.md](../reviews/007-code-review.md)): APPROVED, with five callouts, all punched:
  - why Save is nullable;
  - the save-method naming rule;
  - four hook-timing statements, on the page and in Order's comments;
  - `CancelOrder` leaves an already-cancelled order, so a retried cancel succeeds (R-E);
  - the skill says what `OrderLineRow` is.

  Four of its five theoretical items were taken as wording fixes, and the region-map change is recorded under Amendments.
- **After the punches:** both solutions rebuilt and passed (unit 839, integration 646 + 7 known skips, Design 163, both TFMs). mdsnippets is clean, and getting started and the-problem change only by the `CanSubmit` line and attribution. One rebuild hit MSB3552: the first TFM's `RemoveDir` of `Generated/` raced the second TFM's resource glob. The rerun was clean, with no change.
- **Open, by design:** the skill's `:360` note on a constructor-injected list factory goes with `skill-complex-aggregate` (EXSET-010); #135 (techdebt).

---

## Plan Amendments

- **2026-10-15 · Plan review** ([007-plan-review.md](../reviews/007-plan-review.md)): APPROVED, with 13 callouts, all amended (see its Triage column). The ones that change the plan:
  - the start-hook example moves to `CustomerPreferences`. Normalizing Order's name would break `OpenDraft`'s draft lookup and pinned tests;
  - submit returns an outcome after reading a new `Order.CanSubmit`, so `order-interface`, which README and getting started embed, gains one line;
  - the `[Create]` constructor takes no arguments;
  - only `Samples/Operations/LifecycleHookSamples.cs` is deleted;
  - the fire-and-forget block becomes prose;
  - the `params` wording.
- **2026-10-15 · Region map:** `collection-factory-parent` and `operations-remote` do not render `readme-domain-model` whole on the page. It embeds the nested Create, Fetch and write regions, and gives `[Remote]` and the root's list calls as prose, so the aggregate is not shown twice.

---

## Notes

- **Keith, 2026-10-15.**
  - Design gains a `[Create]` constructor and an `[Insert, Update]` upsert; a static `[Create]`, `params` and `IFactoryOnCancelled` are prose.
  - Parameter order: values, then `[Service]`, then `CancellationToken` is the standard for every method. Keith doubted that class operations accept other orders; the probe showed they do. He chose to state the rule and file #135.
