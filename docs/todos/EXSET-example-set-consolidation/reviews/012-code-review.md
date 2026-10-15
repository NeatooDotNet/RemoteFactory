## Code Review — EXSET-012 — 2026-10-15

**Verdict: APPROVED**
**Logs:** build PASSED — `reviews/012-build-main.log` (0 errors, 2 warnings: the known `OrderEntry.BlazorClient` wasm `NativeFileReference` notice), `reviews/012-build-design.log` (0 errors, 0 warnings); tests 0 failed — `reviews/012-test-main.log` (UnitTests 855/855 and IntegrationTests 657 + 7 skipped, both TFMs), `reviews/012-test-design.log` (Design.Tests 175 + 1 skipped of 176, both TFMs); `reviews/012-mdsnippets.log` ends `Finished` with no missing-snippet line, and all 31 extracting files are under `src/Design`.

Commit `78838e8`, diffed with `git diff EXSET...HEAD`.

### Direction & shape

The deliverable landed as scoped. `src/docs/reference-app` (59 files, including its `EmployeeManagement.Tests` project), `scripts/extract-snippets.ps1` and `CODING_STANDARDS.md` are deleted. The reference app's tests go with the code they tested. That project was in neither solution and not in CI, and R-A / AC-6 rule it out, so no sacred test was weakened. Outside `docs/todos`, `docs/plans` and `docs/release-notes`, `git grep -i` for the deleted paths and names finds only `.claude/rules/design-snippets.md:9`, which records the deletion, and `docs/VisionAlignment.md`, the ruling record.

The rewritten `CLAUDE.md` statements hold up against the code:
- **Runtime enforcement.** `ClassFactoryRenderer.RenderLocalMethodOpening` emits the `!NeatooRuntime.IsServerRuntime` → `InvalidOperationException` guard only when `isServerOnly` (`method.IsInternal || method.IsRemote`). `NeatooRuntime.IsServerRuntime` defaults to `true`. Services resolve through `GetRequiredService` (`ClassFactoryRenderer.cs:852, 1660`).
- **Entity duality.** It matches `OrderLine.cs:146` `internal void Fetch(OrderLineRow row)` and `:191-192` `[Remote, Fetch] internal async Task<bool> Fetch(Guid id, ...)`.
- **Root `[Create]`.** It matches D14 and `Order.cs:239` `public void Create`.
- **Method injection.** It follows VisionAlignment §2 item 4 word for word in substance.

The `CLAUDE-DESIGN.md` changes:
- The private-setter reason matches `FactoryGenerator.Types.cs:441-446, 506-517`: ordinal properties need a public, internal or init setter.
- The LazyLoad rows now point to R-K.
- The checklist is uniformly `[x]`.

The agent files, the rule file and `SKILL.md:119, 137` name Design. The ledger's 012 shares (C-01, C-28, I-02, I-04, T-15, T-16, T-22, U-02, U-09 and the retired-with-source rows) describe what the diff did.

### Veto-tier

None.

### Callouts

1. **`src/Design/README.md:45-56, 65-71` · High · `Affects: AC-4` · `Reachable by:` `CLAUDE.md:30` tells every reader "When in doubt … read the Design code first", and this README is that directory's landing page; its Class Factory block is the first C# in it.** The plan's amendment says the README's stale statements are corrected, but its first two code blocks are untouched hand-written C#, and they contradict documented rules:
   - The Class Factory block has `[Remote, Create] public void Create(...)`, `[Remote, Fetch] public` and `[Remote, Insert] public`. `CLAUDE.md` says "`[Remote] public` is a compile-time error (NF0105)". D14 makes a root `[Create]` local. Design's real `Order` is `internal partial class Order : IOrder, …` (`Order.cs:82`).
   - The Interface Factory block names `IExampleRepository` / `ExampleDto`. Neither exists anywhere in `src/Design` any more: C-27 / #112 replaced them with `IProductCatalog`.

   The rewritten bullet at `:103` ("a child's Create is `public` and local") sits beneath a block showing the opposite for the root. The audit never read `src/Design/**`, so no ledger row carries this; it is the I-04 / U-02 / C-27 class of finding at an unaudited site. One option is to replace both blocks with prose plus pointers to `Order.cs` / `AllPatterns.cs`, since R-A allows hand-written C# only as WRONG blocks.

2. **`.claude/agents/remotefactory-architect.md:12` · High · `Affects: AC-4` (plan Acceptance bullet 2, C-28) · `Reachable by:` every spawn of the `remotefactory-architect` agent, whose system prompt opens with this sentence.** It still frames the library as "RemoteFactory's data mapper factory pattern". Plan bullet 2 requires that `.claude/agents` "carry none of … C-28's statements". The Test Evidence grep was case-sensitive (`Data Mapper`) and so missed the lowercase form; `git grep -i "data mapper" -- .claude` finds it. One option is D15's wording, as in `CLAUDE.md:7`.

3. **`src/Design/CLAUDE-DESIGN.md:1110` · Medium · `Affects: AC-4` · `Reachable by:` anyone consulting Design Debt on whether to support private setters; the section exists "to prevent re-proposal".** The new reason ends "reaching it would take reflection", and Reconsider When still reads "If .NET adds source-generator-accessible private member access". `[UnsafeAccessor]` (`UnsafeAccessorKind.Method` on `set_X`) has been in .NET since 8. It is reflection-free and trim-safe, and generated code in both supported TFMs (net9.0, net10.0) can use it. The first half of the reason (the ordinal converter skips non-accessible setters) is accurate. The "would take reflection" clause and the trigger are not. Whether R-M's public-setter doctrine makes the row moot is the orchestrator's call.

4. **`CLAUDE.md:7` · Medium · `Affects: AC-4` (C-28) · `Reachable by:` a reader of the project overview looking for the generated endpoint.** "it generates the factories, the serialization and the endpoint at compile time" is wrong about the endpoint. The endpoint is a library route, `app.MapPost("/api/neatoo", …)` in `RemoteFactory.AspNetCore/WebApplicationExtensions.cs:54`; the generator emits the delegates it dispatches to. `README.md:5` (EXSET-005) has the same phrasing, so a fix here would need to match it there.

### Could not verify

- That the trimmer actually removes an `internal` method's body when `IsServerRuntime=false` (`CLAUDE.md` runtime-enforcement bullet 2). I traced it to the renderer's design comment (guard outside the async state machine, TRIM-009), not to a trimmed publish log for this commit.

### Theoretical (not triaged)

- `CLAUDE.md:92` says non-`skill-*` regions "are shared with `docs/` and `README.md`". `order-class-declaration` and `ordercommands-declaration` (added in EXSET-015) are embedded only by the skill.
- `CLAUDE.md` "Key files" lists `Design.Domain`, `Design.Client.Blazor` and `Design.Server`, not `Design.Tests`, which supplies 10 of the 31 snippet source files in `012-mdsnippets.log`.
- `SKILL.md:137`, the sentence rewritten here, still tells editors to hand-write "partial examples", against R-A and `CLAUDE.md:90`. The plan's Amendments defer it to Todo 2 deliberately, but the Follow-on list does not carry it.

### Read report

- **Read beyond the brief:**
  - `src/Generator/Renderer/ClassFactoryRenderer.cs:330-430`
  - `src/RemoteFactory/NeatooRuntime.cs`
  - `src/Generator/FactoryGenerator.Types.cs:407-517`
  - `src/Design/Design.Domain/Entities/OrderLine.cs` (signatures)
  - `Aggregates/Order.cs` (Create, declaration)
  - `ValueObjects/Money.cs` and `TaxRate.cs`
  - `src/RemoteFactory.AspNetCore/WebApplicationExtensions.cs:54`
  - `.claude/agents/*` (grep)
  - skill `snippet:` markers versus docs (grep)
  - the full `src/Design/README.md`
- **Named but unused:** the audit rows beyond the eight cited (the ledger's retired-with-source rows were checked against the deletion only).

### Findings

| # | Finding | Tier | Confidence | Affects | Reachable by | Triage |
|---|---|---|---|---|---|---|
| 1 | `src/Design/README.md:45-56, 65-71`: hand-written Class Factory block shows `[Remote, Create/Fetch/Insert] public` (NF0105, D14) and Interface Factory block names the removed `IExampleRepository`/`ExampleDto` (C-27, #112), in a README the plan says it corrected | Callout | High | AC-4 | Design's landing page, which `CLAUDE.md:30` sends readers to | Amend: all four hand-written blocks are now Design embeds (`order-class-declaration`, `operations-fetch-instance`, `interface-factory-contract`, `ordercommands-declaration`, `operations-execute`, `save-ifactorysavemeta`); the routing list and serialization bullets match the generator |
| 2 | `.claude/agents/remotefactory-architect.md:12`: "RemoteFactory's data mapper factory pattern" (C-28); plan bullet 2's case-sensitive grep missed it | Callout | High | AC-4 (C-28) | Every `remotefactory-architect` spawn | Amend: the architect agent states D15's framing; Test Evidence row 2's grep is now case-insensitive and also covers README.md |
| 3 | `src/Design/CLAUDE-DESIGN.md:1110`: "reaching it would take reflection" and the Reconsider When trigger; `[UnsafeAccessor]` (.NET 8+) gives reflection-free private setter access in both TFMs | Callout | Medium | AC-4 | A reader of Design Debt deciding on private-setter support | Amend: the reason says the converter writes only properties it can set; Reconsider When names `[UnsafeAccessor]` if R-M is revisited |
| 4 | `CLAUDE.md:7`: "generates … the endpoint at compile time"; the endpoint is the library's `MapPost("/api/neatoo")` (`WebApplicationExtensions.cs:54`); `README.md:5` matches | Callout | Medium | AC-4 (C-28) | A reader of the project overview | Amend: `CLAUDE.md:7` and `README.md:5` say the library maps the endpoint |
