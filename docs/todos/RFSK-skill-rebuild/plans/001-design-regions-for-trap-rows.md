# Design regions the trap rows embed

**Plan #:** 001
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-2, AC-3
**Status:** Draft — pre-flight done
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (no public API, no behaviour change; region markers and their tests only)
**Code-review opt-in:** No (mechanical; the test-reviewer gate checks that each region is pinned)
**Branch:** RFSK-001-design-regions-for-trap-rows
**PR:** —

---

## Scope

Give `src/Design` the regions five trap rows need and no region holds today: the context a screen binds to (`Contexts/OrderCheckout.cs`, T6, T14, T18), the read model as a whole class (`ReadModels/OrderSummary.cs`, T20), a class-level `[Execute]` with and without `[Remote]` (`FactoryPatterns/ClassFactoryWithExecute.cs`, T19), the root and child operations of one entity side by side (`Entities/OrderLine.cs`, T15), and the server-only `FetchDraft` behind `OpenDraft` (`Aggregates/Order.cs`, T3). Each region is tight around what its row shows, carries a unique prefixed name, and is pinned by a Design test on the real containers; existing regions keep their names. Does NOT embed anything (002–004), rename types, or change behaviour.

---

## Intent

- When 002 writes the trap table, every row has a region to embed; no row falls back to prose or to a hand-written block, and no later plan has to come back to Design for a shape it already has.
- The eleven rows' examples all come from the same Orders domain, so a reader sees one world: the order, its lines, its checkout, its summary.
- The regions show the row's point and nothing else: a reader of T19 sees three placements as a table, not a page of trimming remarks.

---

## Framework & Architectural Alignment

- R-A and `.claude/rules/design-snippets.md`: regions are the only source of C# in the skill; names are globally unique and prefixed; a region is tight around the code it shows; every region's behaviour is pinned by a Design test on the real client/server containers.
- VisionAlignment section 4, the rows this plan serves: T3 (D11), T6 (D12, D13), T14 (statement 4, constructor injection is both tiers), T15 (D23, R-G), T18 (statement 4), T19 (statement 3, `[Execute]` obeys `[Remote]`), T20 (D15, R-H).
- CLAUDE.md "Adding a new embed": this plan adds regions only; no marker pair is written in any markdown.

---

## Constraints & Invariants

- No existing region is renamed, moved or widened; `readme-domain-model`, `orderline-child`, `orderline-duality`, `operations-class-execute` and `aspnetcore-cancellation` render exactly as before, so no page in `docs/`, `README.md` or the skill changes except attribution lines.
- Nesting is allowed (as `readme-domain-model` nests the five operations); a region may wrap an existing one.
- No type, member or test is renamed; no behaviour changes; the Design tests that exist today stay green and unchanged unless a region shows something none of them pins.
- `mdsnippets` runs clean with its log outside the tree, and `git diff --stat` after it shows attribution-line drift only.
- Both solutions build and test green on net9.0 and net10.0, output to files.

---

## Steps

1. Wrap the checkout context so one block shows what T6, T14 and T18 need: constructor-injected factories, the `[Remote, Fetch]` that calls the root's own `Fetch` as a local call on the server, verbs that make no server call, and the `SaveAsync` that reassigns what the root's `Save` returns.
2. Region the Blazor page's checkout usage as the caller side of the context, beside the existing `skill-blazor-usage`.
3. Wrap the read model as a whole class, declaration and properties through `Fetch`, keeping `aspnetcore-cancellation` nested inside so the ASP.NET Core page's embed is unchanged.
4. Region the three placements on `ClassExecuteDemo` so a page can show them as the table the file's own comment describes, with the long trimming remarks outside the region.
5. Wrap `OrderLine`'s child operations and its root `Fetch` in one block, moving the explanatory comment between them above the block so the embed reads as two signatures side by side.
6. Region `FetchDraft` so `OpenDraft`'s embed can be followed by what it calls, and the T3 row shows the server-only `[Fetch]` the client cannot reach.
7. For each region, name the Design test that pins what it shows; where a region shows something no test pins, add the assertion to the existing test class rather than a new file.
8. Build and test both solutions, run `mdsnippets`, and confirm the diff is regions, tests and attribution lines only.

---

## Acceptance

- [ ] The checkout context loads with its order in one call, is null for a missing order, its verbs run on the client, and its save goes through the root and reassigns `[integration]`
- [ ] The order summary read model arrives on the client with its line records and is null for a missing order `[integration]`
- [ ] On `ClassExecuteDemo`, the `[Remote]` execute crosses to the server, the bare one runs on the calling tier, and the `internal` one runs on the server only `[integration]`
- [ ] An order line loads as a root by its id, is null when no order holds it, and stays a child of its order `[integration]`
- [ ] `OpenDraft` returns the customer's existing draft when one exists and a new unsaved order when none does `[integration]`
- [ ] Every new region name is unique across the repository and `mdsnippets` runs clean with no existing embed changed except attribution lines `[explicit-skip: tooling check, no test tier]`
- [ ] Both solutions build and test green on both target frameworks `[explicit-skip: meta-bullet]`

---

## Current State (Pre-Flight)

Walked 2026-10-16 on `main` at `249baa3`; nothing edited.

- `Contexts/OrderCheckout.cs` (124 lines, CRLF): no region. `IOrderCheckout` 52–58; `[Factory] internal partial class OrderCheckout` 63–123 with the constructor taking `IOrderFactory` and `IOrderLineFactory` at 69–73, `[Remote, Fetch] internal Task<bool> Fetch(int orderId)` at 85–94 calling `_orderFactory.Fetch`, `AddProduct` 99–102, `Submit` 107–110, `SaveAsync` 118–122 reassigning `Order`. Doc comments sit between the members; a region over 63–123 carries them.
- `Design.Client.Blazor/Pages/Home.razor` (CRLF): `#region skill-blazor-usage` at 177 inside `@code`, so `#region` works there; the checkout usage `StartCheckout`, `AddFoundProduct`, `SubmitCheckout`, `SaveCheckout` is at 273–323, no region.
- `ReadModels/OrderSummary.cs`: `#region aspnetcore-cancellation` wraps only the `Fetch` method at 87–107; the class declaration is at 63–70, `OrderSummaryLine` record at 50, `IOrderSummary` at 38–45.
- `FactoryPatterns/ClassFactoryWithExecute.cs` (194 lines): no region. `RunCommand` `[Remote, Execute] public static` at 119–124, `ScoreLocally` `[Execute] public static` at 152–157, `ArchiveOnServer` `[Execute] internal static` at 187–192. Each is preceded by a long `<remarks>` block (78–118, 129–151, 162–186), so a single region over the three would carry about 100 lines of remarks; per-method regions starting at the attribute line exclude them.
- `Entities/OrderLine.cs` (374 lines): `#region orderline-child` 128–170 (public `[Create]`, internal `[Fetch](row)`, `[Insert](row)`, `[Update](row)`; no `[Delete]`, the list removes rows); comment block 171–186; `#region orderline-duality` 187–201 (`[Remote, Fetch] internal Task<bool> Fetch(Guid id, [Service] IOrderDbContext db)`). `collection-factory-basic` 283–351 is the list and is not touched.
- `Aggregates/Order.cs`: `FetchDraft` `[Fetch] internal async Task<bool>` at 327–339, no region; `#region operations-class-execute` 362–371 wraps `OpenDraft`; the D11 remarks at 345–361 sit between them.
- Tests that already pin the shapes, all on `DesignClientServerContainers`: `ReadModelAndContextTests` (five tests: summary with line records, summary null, checkout one call, checkout null, verbs on client and save through root); `ClassFactoryExecuteTests` (client/server, local mode, `Execute_InternalStaticWithoutRemote_RunsOnServer`); `LocalExecuteTests.Execute_ClassFactory_WithoutRemote_RunsOnClient` and `Execute_WithRemote_StillTriesTheWire`; `OrderLineDualityTests` (root by id, null, stays a child); `CreateOrFetchTests` (`OpenDraft` fetches the draft; creates a new unsaved order). No new test is expected; step 7 confirms rather than adds.
- `mdsnippets` tolerates a region no file embeds (every region today is embedded, so this is the first unused set until 002); the run must be checked for that, not assumed.

---

## Punchlist

- 

---

## Test Evidence

The flip step was not run: temporarily breaking production code to watch a test fail was refused by the session's permission classifier. The plan adds region markers only, and every cited test except the new one was gated in EXSET. The new control's failure mode is its own assertion: without `[Remote]`, `RunCommand` runs locally and no exception is thrown.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Checkout: one call, null when missing, verbs on client, save through root | Required | `[integration]` | `Design.Tests.FactoryTests.ReadModelAndContextTests.OrderCheckout_LoadsWithItsOrder_InOneCall`, `…OrderCheckout_ReturnsNull_WhenOrderDoesNotExist`, `…OrderCheckout_VerbsRunOnClient_AndSaveGoesThroughTheRoot` | ✓ |
| Summary: line records, null when missing | Required | `[integration]` | `…ReadModelAndContextTests.OrderSummary_FetchedThroughClient_ArrivesWithLineRecords`, `…OrderSummary_ReturnsNull_WhenOrderDoesNotExist` | ✓ |
| `ClassExecuteDemo`: `[Remote]` crosses, bare runs on caller, `internal` on server | Required | `[integration]` | `…LocalExecuteTests.Execute_ClassFactory_WithRemote_TriesTheWire` (new), `…LocalExecuteTests.Execute_ClassFactory_WithoutRemote_RunsOnClient`, `…ClassFactoryExecuteTests.Execute_InternalStaticWithoutRemote_RunsOnServer`; the client-side guard is pinned at `[unit]` in `RemoteFactory.UnitTests…InternalVisibilityTests`, because `IsServerRuntime` is process-wide | ✓ |
| Order line: root by id, null, stays a child | Required | `[integration]` | `…OrderLineDualityTests.OrderLine_FetchedAsARoot_ByItsId`, `…OrderLine_FetchedAsARoot_IsNull_WhenNoOrderHoldsIt`, `…OrderLine_StaysAChild_OfItsOrder` | ✓ |
| `OpenDraft`: existing draft, else new unsaved | Required | `[integration]` | `…CreateOrFetchTests.OpenDraft_FetchesTheCustomersDraft_WhenOneExists`, `…OpenDraft_CreatesANewUnsavedOrder_WhenTheCustomerHasNoDraft` | ✓ |
| Region names unique; mdsnippets clean, attribution drift only | Required | `[explicit-skip: tooling check]` | all 8 regions extracted; 7 markdown files changed, 0 lines besides `snippet source` attributions | ✓ |
| Both solutions green, both TFMs | Required | `[explicit-skip: meta-bullet]` | `reviews/001-*.log`: unit 871, integration 664 (5 skipped), Design 177 (1 skipped). One net9.0 unit failure, `CanMethod_Public_ReturnsAuthorized_WhenAllowed`, is the #100 flake; green on re-run (`001-unit-rerun.log`) | ✓ |

---

## Gate Record

- 

---

## Plan Amendments

### 2026-10-16 — One control test added; regions named

- **Section affected:** Steps 7, Current State
- **Original said:** "No new test is expected; step 7 confirms rather than adds."
- **What changed:** `LocalExecuteTests.Execute_ClassFactory_WithRemote_TriesTheWire` added. The existing control used the static `ExampleCommands`, so nothing showed the class-level `[Remote]` `RunCommand` crossing. Regions: `skill-context-checkout`, `skill-context-usage`, `skill-readmodel` (wraps `aspnetcore-cancellation`), `skill-execute-remote`, `skill-execute-local`, `skill-execute-internal`, `skill-orderline-root-and-child` (wraps `orderline-child` and `orderline-duality`; the duality comment moved above the block), `skill-order-fetchdraft`.
- **Why:** the T19 row claims the crossing for a class factory; the test now pins it.
- **Discovery Log:** 2026-10-16 / RFSK-001

---

## Notes

- Loaded: `.claude/rules/design-snippets.md`, CLAUDE.md "Skill Code Samples", VisionAlignment sections 2 and 4. The project declares no design skill for Step 2.
- Unattended from here (todo header).
