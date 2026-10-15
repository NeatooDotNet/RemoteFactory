# Save operation and interfaces reference

**Plan #:** 008
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4, AC-5
**Status:** In Progress
**Last Updated:** 2026-10-15
**Plan-review opt-in:** No. The pages re-point to Design shapes that 002–007 built and tested. Design gains only one region on Order and one test file.
**Code-review opt-in:** Yes. Both pages are rewritten against the generator's Save routing and the library's interfaces.
**Branch:** EXSET-008-save-interfaces
**PR:** —

---

## Scope

Re-point every C# block on `docs/save-operation.md` (8 embeds, 4 hand-written fences) and `docs/interfaces-reference.md` (12 embeds, 18 hand-written fences) to `src/Design`, or turn it into prose. No other page or the skill embeds these 20 regions.

**Design gains:**
- a `save-ifactorysavemeta` region on Order's two state properties;
- `Design.Tests/FactoryTests/SaveTests.cs`. Its routing test is the `save-testing` region, and that test's client calls are the `save-usage` region. Its other tests pin the Save outcomes the page states.

**Reused Design regions:** `operations-insert`, `-update`, `-delete` and `-insert-update` on the save page; `operations-lifecycle-onstart` and `-oncomplete` on the interfaces page.

**Becomes prose,** per I-12's precedent (006, 007): the library interface signatures, the `IAspAuthorize` decorator (authorization is EXSET-009), the hand-implemented ordinal value objects (R-F: a value object is a plain record), the events interfaces' contracts, and optimistic concurrency.

**Removed:**
- `IFactoryCore<T>`, which the library no longer has (T-13);
- the LazyLoad section and its summary row (R-K, AC-5);
- "Save vs explicit methods" (T-03: the write methods are not on the interface);
- the `save-generated` comment block, which duplicates the page's table;
- the private-setter and `[JsonInclude]` guidance (R-M).

**Findings to fix:**
- C-12: manual checks in write methods (R-E);
- C-20: concrete-typed callers and casts;
- C-21: method-injected services stored in fields;
- I-01 and U-12: the `IsNew`/`IsDeleted` shape (R-M);
- I-07: Save not reassigned;
- T-03: explicit Insert/Update/Delete;
- T-11: the 409 claim (the library maps no exception to 409);
- T-12: the save table's authorized new-and-deleted (#104) and missing-method outcomes;
- T-13: `IFactoryCore<T>`;
- U-03: the cascade on plain classes;
- D15: Save is one use of the seam, not "the persistence routing heart";
- the interfaces page's ordinal order ("alphabetical, base first"; 013 established own properties first, then the base's).

Delete the reference-app sources. That is all 14 files that define `save-*` or `interfaces-*` regions; the 11 regions nobody embeds go with them.

Does NOT touch:
- `trimming.md`'s `IFactorySaveMeta` section (EXSET-011);
- `authorization.md` (EXSET-009);
- `factory-events.md` (EXSET-011);
- the skill's prose (Todo 2).

---

## Intent

- A reader learning Save sees Order's real state properties, its writes with the R-D cascade, a client saving and reassigning, and a test of the routing. Each is compiled and tested.
- The save table states every outcome the generator produces, including the throws and #104's current behaviour.
- The interfaces page is a reference of what the library actually exposes: each interface's members in prose, who implements it, and where the full page is.

---

## Framework & Architectural Alignment

- **Rulings:**
  - D15;
  - R-D;
  - R-E;
  - R-F;
  - R-K;
  - R-M;
  - I-12's prose precedent;
  - statement 6: each object writes its own row.
- **`.claude/rules/design-snippets.md`.**
- **007's statements of hook timing and save naming,** which this page repeats rather than restates differently.

---

## Constraints & Invariants

- **Embeds.** Every C# block on both pages is an embed from `src/Design` or a short WRONG block.
- **Shared regions keep their output.** The new `save-ifactorysavemeta` region adds no line to any existing embed.
- **Tests run through the client containers.** The routing test saves through the client factory and reassigns each result (I-07).
- **Known bugs.** A test that exposes #104 is written naturally and skipped as a known bug, never worked around.
- **The whole still works:** both solutions green, mdsnippets swallows nothing, the reference app builds.

---

## Steps

1. **Design:**
   - the region on Order's `IsNew`/`IsDeleted`;
   - `SaveTests` through the client containers:
     - **routing:** Save inserts, updates and deletes, with each result reassigned;
     - **new and deleted:** returns null;
     - **#104:** an authorized new-and-deleted Save should return null, but throws `NotAuthorizedException`. Written naturally, skipped as a known bug.
2. **The save page:**
   - re-point the embeds;
   - rewrite the intro (D15), the state section (R-M), routing with the full outcome table (T-12), the cascade (R-D), partial operations (`CustomerPreferences`), return values, concurrency (T-11) and validation (R-E).
3. **The interfaces page:** the hook embeds, prose signatures, ordinal order and the events contracts; remove LazyLoad and `IFactoryCore`.
4. **Delete the reference-app sources,** then build `EmployeeManagement.sln`.
5. **Verify and record:** mdsnippets, both solutions, and the ledger outcomes.

---

## Acceptance

- [x] Save through the client inserts, updates and deletes, and each result is reassigned `[integration]` · Must
- [x] The save table's outcomes are pinned:
  - new and deleted returns null;
  - with no `[Delete]`, marking the object deleted throws;
  - authorized new-and-deleted is #104, as a skipped known-bug test.

  `[integration]` · Should
- [x] On both pages:
  - every C# block is an embed from Design or WRONG;
  - no LazyLoad and no `IFactoryCore`;
  - none of the findings' text remains;
  - the reference-app sources are gone, and the reference app builds.

  `[explicit-skip: greps; EmployeeManagement.sln build]` · Must
- [x] The interfaces page's member descriptions and ordinal order match the library source `[explicit-skip: source-cited review]` · Should
- [x] mdsnippets clean; both solutions green `[explicit-skip: meta]` · Must

---

## Current State (Pre-Flight)

- **The generator's Save** (`ClassFactoryRenderer.cs:1111-1175`) branches on `IsDeleted` first:
  - **Deleted:** new and deleted returns the default; deleted and stored goes to Delete.
  - **New:** goes to Insert.
  - **Otherwise:** goes to Update.

  A missing method throws `NotImplementedException` in its branch. With no `[Delete]`, that includes new and deleted.
- **With authorization:**
  - the default is an `Authorized<T>` with `HasAccess` false, so `Save` throws `NotAuthorizedException` for new and deleted, and for a `bool` write that returns false (#104, open);
  - `TrySave` returns it.
- **`IFactoryCore<T>`** is not in `src/RemoteFactory`; the generator inlines the pipeline.
- **No exception maps to 409** anywhere in the library or AspNetCore.
- **Custom ordinal types:** `NeatooOrdinalConverterFactory` honours a hand-implemented `IOrdinalSerializable`, with `IOrdinalConverterProvider<T>`. Design's value objects are plain records (R-F), so the page describes the interfaces as generated for `[Factory]` types.
- **Order:** `Insert` does not touch `IsNew`; its complete hook clears it. `IOrder : IFactorySaveMeta`.

---

## Punchlist

---

## Test Evidence

| Bullet | Pinned by | Tier |
|---|---|---|
| 1. Routing through the client | `SaveTests.Save_RoutesOnIsNewAndIsDeleted` (the `save-usage` region: Insert, Update and Delete through the client, each result reassigned; the order is gone after the Delete); `Save_InsertThenUpdate_WritesTheOneRow` (the `save-testing` region: each save reaches the server's row) | integration |
| 2. Outcomes | `SaveTests.Save_NewAndDeleted_ReturnsNull` (null, and no draft stored). The missing-method throw: the library's `PartialSaveTests` (`:196`, `:289`, `:358`; see Amendments). #104: `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull`, skipped as a known bug. Run unskipped, it failed on both TFMs with `NotAuthorizedException: Authorization denied for operation Save on IAuthorizedOrder; no reason was supplied.` | integration |
| 3. Pages | explicit-skip: both pages have no fence outside a `snippet:`/`endSnippet` pair. The 11 embeds come from Order, CustomerPreferences and SaveTests. No LazyLoad, `IFactoryCore`, `[JsonInclude]` or 409 text remains. The 14 reference-app files are deleted; `reviews/008-refapp-build.log` (0 errors) | explicit-skip |
| 4. Interfaces page | explicit-skip: members as printed from `src/RemoteFactory/*.cs` and `AspAuthorize.cs`; `IFactorySave<T>` from `ClassFactoryRenderer.cs:165`, `:1245-1321`, `:1571`; ordinal order as 013 established | explicit-skip |
| 5. Meta | explicit-skip: `reviews/008-mdsnippets.log`. README, getting started, the-problem and factory-operations change only in attribution lines. Logs: `008-build.log`, `008-test.log`, `008-design-build.log`, `008-design-test.log` (unit 839; integration 646 + 7 skips; Design 166 + 1 known-bug skip, up 4; both TFMs) | explicit-skip |

---

## Gate Record

---

## Plan Amendments

- **2026-10-15 · Implementation:**
  - The planned Design test of the missing-`[Delete]` throw is dropped. `ICustomerPreferences` offers no way to mark the object deleted, which is right for a type with no Delete, so a caller cannot reach that branch without a cast (C-20). The throw is pinned by the library's `PartialSaveTests`, and the page says a type with no Delete should not offer deletion.
  - `trimming.md's` link to the removed private-setter section now points to `#ifactorysavemeta`. The section itself is EXSET-011's, and is on the todo punchlist.
  - The interfaces page keeps `### IFactoryEvents` and `### IFactoryEventRelay` headings, because `attributes-reference.md` and the v1.1.0 notes link to them. `lazyload.md's` link to `#ilazyloadfactory` goes with that page (EXSET-011).

---

## Notes

- **Ran unattended** under Keith's standing instruction (2026-10-15): take all recommendations, and stop only for genuine feedback. These defaults follow the 006 and 007 precedents:
  - library signatures become prose;
  - concurrency is prose with no Design example, because the happy path has none;
  - `IAspAuthorize` is left to EXSET-009.
