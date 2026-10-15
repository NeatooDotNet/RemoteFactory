# EXSET-005 test review — 2026-10-14

## Round 1 — CLEAN

**Reviewer:** test-reviewer (tight), against `fa99a75` diffed from `d74c684`. Logs green:
- main build: 0 errors, 2 warnings (the OrderEntry WASM native-reference notice);
- Design build: 0 errors, 0 warnings, and it includes `Design.Server` and `Design.Client.Blazor` on both TFMs;
- reference-app build: 0 errors, 0 warnings;
- tests: unit 835, integration 644 + 5 pre-existing skips, Design 141, each on both TFMs.

Design went from 137 to 141, which matches the four tests added. Unit and integration counts are unchanged. No failures.

Every Test Evidence row cites tests that exist, assert what the bullet says, and run at the declared tier:
- **Duality.** The three `OrderLineDualityTests` run on the client container.
  - `…ByItsId` takes the line id from a fetched order. It asserts exactly one remote call, the id, name, price and quantity, and `IsNew == false`.
  - `…IsNull…` asserts null for an id no order holds.
  - `…StaysAChild…` row-loads the line through its order, saves a quantity change through the root, and reads `5` back from the server store with `FindLineAsync`.
- **Client usage.** `OrderUsageTests` contains the `readme-client-usage` region itself. It asserts two remote calls, a saved order that is not new and has an id, and the stored row's customer and single line.
- **Model slices.** `AggregateTests` covers each operation in `readme-domain-model` on the client container: Create local with zero calls, Fetch and null Fetch, and Save routing to Insert, Update and Delete, plus the missing-row throws. `CreateOrFetchTests` covers OpenDraft. `orderline-child` is covered by `AggregateTests`' line tests and `…StaysAChild…`.
- **Server registration.** `readme-server-setup` is the `AddNeatooAspNetCore` line inside `AddDesignServer`. `DesignServerCompositionTests` calls that and runs the Order save path.

The map is honest. Rendered and source text match for all 16 embeds of the 8 regions (README, getting started, the problem, client-server architecture, and the skill's `setup.md`).

Sacred-test check clean: `fa99a75` adds two test files and modifies or deletes none.

Explicit-skip spot checks, against the files:
- **Fences.**
  - README: the only fences outside embeds are `bash`.
  - getting-started.md: `bash` blocks and a solution-tree block.
  - the-problem.md: the WRONG-labelled controller block at `:27`, and the generated-code diagram at `:130`.
  - client-server-architecture.md: the flow diagram at `:20`, and `xml`.
  - decision-guide.md: all text trees.
  - So the only C# outside an embed is the WRONG block at `the-problem.md:27`. Embed sources are all under `src/Design` (6 / 6 / 1 / 2).
- **Region uniqueness.** Each of the 8 regions has exactly one `#region` definition. The extra `git grep` hits for the two `getting-started-*-program` names are prose in `docs/plans/completed/hosted-blazor-wasm-conversion.md`. The 5 retired names are neither defined nor embedded anywhere.
- **`src/docs/samples`.** No tracked file remains and `src/docs/` now holds only `reference-app/`. Nothing outside `docs/todos` and `docs/plans` names it except the rule file and `VisionAlignment.md`'s ruling and inventory text. `mdsnippets.json` does not name it.
- **Finding text.** Grepping the five pages leaves exactly what the map says:
  - "Persistence routing … is one use of that" at `README.md:5` and `the-problem.md:7`;
  - `client-server-architecture.md:35`, which quotes "runs on the server" in order to deny it.
  - The remaining `Save(` calls are reassigned root saves or the void child-list `lineListFactory.Save` in the cascade.
- **Decision guide.** It has a read-model branch and a context branch (`:35`, `:44`, `:46`, `:99`), with `OrderSummary` and `OrderCheckout` as the examples.
- **Swallowed sections.** README keeps all 11 `##` sections. Its 444 deletions come from the double render and the dropped auth example.

Tech-debt:
- `scripts/extract-snippets.ps1` defaults to a root-level `docs/samples` with `docs:*` markers. That directory was removed in `87e1ecb`, and nothing calls the script. It is a different path from the `src/docs/samples` this plan deleted.
- The inventory row at `docs/VisionAlignment.md:69` still describes `src/docs/samples` as present ("1 file, 275 lines").

Could not verify: the mdsnippets exit status. No log of the run is on disk. Instead I checked that every rendered embed matches its region and that README keeps all its sections.

Theoretical: none.
