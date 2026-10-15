# Factory operations page

**Plan #:** 007
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** In Progress
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
  - one reports a missing order through its result (U-05);
  - one throws when the order is missing (R-I).
- **An Order start hook that does real work,** where today it is a no-op. The hooks' false comments are corrected.

**Parameter order** (Keith, 2026-10-15). The page states one rule for every operation: values, then `[Service]` parameters, then the `CancellationToken`. Design follows it. The generator does not enforce it: class operations tolerate other orders, while `[Execute]` breaks in generated code. That is issue #135, for Todo 3's analyzers.

**Findings to fix:**
- C-05: static `[Create]` as create-or-fetch;
- C-09: a command writing another aggregate's row;
- C-10: delete wording;
- C-11: hooks as validation gates;
- C-15: `bool` `[Create]` and a business filter in `[Fetch]`;
- C-01: "`[Remote]` executes on server";
- C-02 and U-01: child visibility and creation;
- C-19, C-20 and I-01;
- C-28 and C-30: persistence-routing framing; `[Execute]` "decides which persistence path";
- I-02: "server-only" `[Service]`;
- I-05 and I-06: parameter defaults and ordering;
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
   - **Order's start hook** normalizes the customer name before a write. Correct the hooks' comments: when each one runs, and that `IFactoryOnCancelled` is for cancellation.
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

- [ ] `CustomerPreferences`:
  - its `[Create]` constructor runs locally;
  - `SaveUpsert` inserts, then updates, through the client;
  - Fetch is null for an unknown customer.

  `[integration]` · Must
- [ ] Order's commands:
  - submit through the client changes a stored order to Submitted and returns true, and returns false for a missing id;
  - cancel of a missing id throws.

  `[integration]` · Must
- [ ] Order's start hook normalizes the customer name before Insert and Update `[integration]` · Should
- [ ] The page's regions for the list (Create, Fetch from rows, AddLine) and create-or-fetch are pinned by the existing cascade and create-or-fetch tests `[integration]` · Must
- [ ] On the page:
  - every C# block is an embed from Design or WRONG;
  - no region is defined twice, and none retired is embedded;
  - none of the findings' text remains;
  - the reference-app sources are gone, and the reference app builds.

  `[explicit-skip: greps; EmployeeManagement.sln build]` · Must
- [ ] mdsnippets clean; both solutions green `[explicit-skip: meta]` · Must

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

---

## Gate Record

---

## Plan Amendments

---

## Notes

- **Keith, 2026-10-15.**
  - Design gains a `[Create]` constructor and an `[Insert, Update]` upsert; a static `[Create]`, `params` and `IFactoryOnCancelled` are prose.
  - Parameter order: values, then `[Service]`, then `CancellationToken` is the standard for every method. Keith doubted that class operations accept other orders; the probe showed they do. He chose to state the rule and file #135.
