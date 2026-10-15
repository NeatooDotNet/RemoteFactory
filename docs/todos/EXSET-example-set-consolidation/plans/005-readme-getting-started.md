# README, getting started, framing pages

**Plan #:** 005
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4, AC-6
**Status:** Done
**Last Updated:** 2026-10-14
**Plan-review opt-in:** Yes (a new Design shape later plans embed; the first plan to move embeds; rulings D14, D15, D18, R-C, R-G, R-O)
**Code-review opt-in:** Yes (README and four framing pages; a new Design operation and store lookup)
**Branch:** EXSET-005-readme-getting-started
**PR:** #130

---

## Scope

Re-point every C# block on `README.md`, `docs/getting-started.md`, `docs/the-problem.md`, `docs/client-server-architecture.md` and `docs/decision-guide.md` to `src/Design`, in Design's Order domain (R-O):
- the five `readme-*` regions, which are defined twice today (reference app and `src/docs/samples`) and render twice;
- the four `getting-started-*` regions;
- the four hand-written blocks on the framing pages.

Add the one shape Design lacks for them: entity duality (R-G). `OrderLine` gains a `[Remote, Fetch]` by line id, so a screen can load one line as a root while the line stays a child of `Order`.

Delete `src/docs/samples`, and the reference-app sources and region markers these embeds replace.

Fix the audit findings on the five pages:
- the persistence-routing headline and "Data Mapper Factory" (D15);
- `[Remote]` as "runs on the server" and the decision tree's "called from client → `[Remote]`" (D18);
- the decision guide's missing read-model and context branches (C-29);
- `.NET 8`;
- "seven operations" and the `Event` operation;
- "fall back to remote calls";
- "no separate repository layer";
- the "not-registered" DI-exception wording;
- the child "internal interface" (R-C);
- private setters;
- unreassigned `Save` results;
- hand-written generated-interface illustrations.

Does NOT touch:
- other `docs/` pages (006–011);
- the skill's prose (Todo 2). The skill's one embed of `getting-started-client-program` (`references/setup.md`) is re-pointed by the same region;
- `src/Design/README.md` and the rest of the reference app (EXSET-012);
- `AuthorizedOrder` (EXSET-009).

---

## Intent

- A reader who opens the README, or follows getting started, sees one compiled, tested Order aggregate and the client code that uses it, each once. The code is interface-first, with public setters and reassigned saves.
- The README and the framing pages say what RemoteFactory is for (D15): with the object library loaded on both tiers, the seam needs no DTOs and no controllers; persistence routing is one use of that.
- A reader of the decision guide finds a home for a read model and a context, and reads `[Remote]` as "a client entry point", not "runs on the server".
- `src/docs/samples` is gone, and no README region has a second definition.

---

## Framework & Architectural Alignment

- D15 (the point is the seam with no DTOs or controllers; persistence routing is one use), D18 (`[Remote]` marks a client entry point; once on the server it does nothing different), D14 (a root's Create is local), R-C (a child's Create is public and local; its other operations are internal), R-G (an entity can be a root and a child; the factory-method signature is the difference), R-H/R-I (read model, `bool` Fetch), D13 (context), R-M (public `IsNew`/`IsDeleted`), R-O (migrated regions take the Order domain).
- `.claude/rules/design-snippets.md`: regions are tight, globally unique, pinned by a Design test, and a migrating region's old definition is deleted in the same change. Placeholders come in pairs. mdsnippets' log is kept outside the tree, followed by a `git diff --stat` check.
- EXSET-002–004 shapes are the substrate: `Order`, `OrderLine`, `IOrderDbContext`, `OrderSummary`, `OrderCheckout`, `OpenDraft`, `IProductCatalog`, `ExampleCommands`.

---

## Constraints & Invariants

- Every C# block on the five pages is an embed from `src/Design` or a short block labelled WRONG (R-A). Nothing on them is embedded from `src/docs/reference-app` or `src/docs/samples` afterwards.
- Each region keeps its name, so `references/setup.md`'s embed re-points by itself, and its old definition is deleted in the same change. The one exception is `getting-started-employee-model`: it no longer shows an employee, so it is renamed together with its only embed (getting-started.md) and the old definition is deleted.
- Regions are slices: tight around signatures and bodies, with Design's DESIGN DECISION and GENERATOR BEHAVIOR remarks just above each region, still beside the code they explain. A page that needs the same lines reuses the region rather than defining a second one.
- Every embedded region's behavior is pinned by a Design test on the real client/server containers, with two exceptions that are build-pinned (Keith, 2026-10-14): `Design.Server/Program.cs`'s middleware and `Design.Client.Blazor/Program.cs`'s registration. Both are top-level statements no test runs, and Design.Tests cannot reference the WASM client. The server's registration region lives in `ServerServices`, which `DesignServerCompositionTests` runs.
- A client-usage region uses only what a client assembly can call (Design.Tests has `InternalsVisibleTo`).
- `OrderLine`'s root Fetch is the only new operation:
  - it is `[Remote]` because a client calls it directly;
  - a missing line is a false Fetch (R-I);
  - the line's child operations (`Create` public; `Fetch(row)`, Insert, Update internal) are unchanged, and the Order cascade's tests stay green.
- No `[Remote]` on anything but entry points; interface-first; no `new` on a `[Factory]` type outside its own factory methods.
- `Design.Server` composition still resolves every service.
- mdsnippets swallows no section: `git diff --stat` shows no file whose deletions exceed the edit.
- Both solutions are green.

---

## Steps

1. Duality: add `OrderLine`'s `[Remote, Fetch]` by line id, through a store lookup of one line row.
   - The remarks state R-G, and that a line loaded alone is read-only: it has no root save path.
   - The statements this makes false are rewritten: `OrderLine.cs:15-21, :131-136`; `CLAUDE-DESIGN.md:18, :380, :844, :1099`; Anti-Pattern 7's hand-written `Product`, which becomes `OrderLine`.
   - A test from the client fetches a line whose id it took from a fetched order (seed ids are random), and gets null for a missing id.
2. Probe mdsnippets for nested regions, then add Design regions. The names are globally unique (`skill-entity-duality` is taken):
   - **Domain model:** slices of `Order.cs` (interface; Create, Fetch, OpenDraft; the writes), reused by README, getting started and `the-problem.md:43`.
   - **Client usage:** in a Design test on the client container. It opens a draft, adds a line through the line factory, reassigns `order = await factory.Save(order)`, and fetches. README and getting started reuse it.
   - **Server setup:** the registration in `ServerServices` and the middleware in `Design.Server/Program.cs`.
   - **Client setup:** registration in `Design.Client.Blazor/Program.cs`, reordered so the `ITextScorer` block sits outside the region.
   - **Framing pages:** an `OrderLine` child slice (public Create, internal row operations) beside the root slices, and the duality slice.
3. Re-point the embeds on the five pages to those regions. Delete:
   - `src/docs/samples`;
   - the reference app's `ReadmeSamples.cs`;
   - `Samples/GettingStarted/*`, together with `ServerProgramSample.cs`, `ClientProgramSample.cs` and `SerializationConfigSample.cs`, which reference `EmployeeModel`. The last of these hosts the orphan `getting-started-serialization-config`;
   - the remaining region markers on reference-app files that stay until 012.

   Then build `EmployeeManagement.sln` once, since neither solution nor CI includes it.
4. README:
   - the D15 headline;
   - drop the auth "full example" for a link to the authorization page;
   - take out `.NET 8`;
   - the operation list without `Event`;
   - events described as transactional handlers plus the relay, not fire-and-forget (`:566`);
   - "the same `Employee` class executes" (`:15, :17`) restated for the Order domain.
5. Getting started and the problem:
   - D15 framing at every cited line (`getting-started.md:3, :110, :207, :215, :219`; `the-problem.md:5, :7, :22, :42, :86, :98, :103`), with no "persistence routing" headline and no "seven operations";
   - the repository and Employee prose matched to Design (`getting-started.md:13, :47, :110, :200-203`; `the-problem.md:67`);
   - serializable types stated per R-F, not "types with `[Factory]`" (`getting-started.md:239`);
   - no "client-side relay methods" on handler classes (`getting-started.md:43`);
   - the "before" controller block labelled WRONG;
   - the "With RemoteFactory" block (`the-problem.md:43`) embedded from the domain-model slices.
6. Client–server architecture:
   - `:3` and `:5`, `:140` reframed (D15);
   - `:39-44` states when `[Remote]` applies: the call must cross to the server, and a local Create, a `Can*` check and a bare `[Execute]` do not (C-03, U-02);
   - the root/child block from Design, with the child's public Create (R-C) and no "internal interface" claim (`:73, :80, :93, :99, :102`);
   - the duality block from Design;
   - the DI-exception paragraph (`:57-60`) gives both outcomes with their conditions:
     - the guard is emitted only for `internal` or `[Remote]` methods, and throws only when `IsServerRuntime` is false (it defaults to true);
     - otherwise a server-only service missing from the client's DI gives the standard resolution error;
     - an `internal` member cannot be called from another assembly at all.
7. Decision guide:
   - the `[Remote]` tree asks "does this call need to cross to the server?" and names the local shapes a client calls (local Create, `Can*`, bare `[Execute]`) (D18, C-03);
   - `:21`'s "routed from client" is restated;
   - the pattern table and tree gain a read model and a context (C-29), with Design's types as examples, and drop the reference-app names;
   - "explicit Insert/Update/Delete calls" goes (T-03);
   - the events tree stays: it is current guidance.
8. Run mdsnippets with its log outside the tree, check the diff, build and test both solutions, and record ledger outcomes. The rows are C-01, C-03, C-19, C-28, C-29, I-01, I-07, I-11, I-12, I-13, T-03, T-09, T-14, T-16, T-17, T-20, T-22, U-02, U-04, U-06 and U-09: what 005 fixes on its pages, "none on 005's pages" where a row does not appear there, and later owners keep theirs.

---

## Acceptance

- [x] From the client, a single order line loads as a root by its id, null when missing, while the order cascade still loads and saves it as a child `[integration]` · Must
- [x] The regions the five pages embed are pinned by Design tests on the client/server containers: the model slices, client usage and server registration `[integration]` · Must
- [x] The middleware and client-registration regions compile in both solutions' builds `[explicit-skip: top-level statements no test runs; Keith 2026-10-14]` · Must
- [x] Every C# block on the five pages is an embed from `src/Design` or a WRONG block, and no region is defined twice `[explicit-skip: grep of the five pages' fences and markers, and of all #region names]` · Must
- [x] `src/docs/samples` is deleted, and no rendered page, source file, solution or `mdsnippets.json` references it; the reference app still builds `[explicit-skip: filesystem, scoped grep, EmployeeManagement.sln build]` · Must
- [x] The five pages carry none of the findings' text this plan lists: persistence-routing framing, `[Remote]`-runs-on-server or called-from-client, `.NET 8`, seven operations, `Event` operation, fall back to remote, no repository layer, `[Factory]`-only serialization, client-side relay methods, child internal interface, private setters, unreassigned saves, Employee/repository prose `[explicit-skip: grep of the five pages]` · Must
- [x] The decision guide gives a read model and a context their own branch `[explicit-skip: page review]` · Should
- [x] mdsnippets ran clean with no swallowed section; both solutions green `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

- **README.md:** five embeds, each rendering twice. Every region is defined in the reference app (`EmployeeManagement.Domain/Samples/ReadmeSamples.cs` :9, :95, :124, :130, :140) and in `src/docs/samples/ReadmeSamples.cs` (:13, :98, :121, :135, :153).
  - The samples copy has `[Remote] public` instance methods, which give NF0105.
  - Both copies have `private set` on `Id`/`IsNew` and an `IPersonRepository`.
  - The client usage does not reassign `Save`.
  - `:3` "Data Mapper Factory"; `:560` lists `Event` as an operation; `:566` "Fire-and-forget domain events"; `:573` ".NET 8.0 (LTS)"; `:15, :17` "the same `Employee` class executes client-side and server-side".
- **getting-started.md:** four embeds, from reference-app `Server.WebApi/Program.cs:8`, `Client.Blazor/Program.cs:11`, `Aggregates/Employee.cs:32` and `Samples/GettingStarted/EmployeeModelUsage.cs:5`.
  - The usage uses `EmployeeModel`, a different type from the model shown.
  - Two `Save(fetched)` calls are unreassigned.
  - The model region's first line is "`[Remote]` executes on server". The client region says "fall back to remote calls", and `skills/RemoteFactory/references/setup.md:107` also embeds it.
  - Text: `:3, :110, :207` persistence routing; `:110` "No separate repository layer"; `:219` "All seven persistence operations".
  - The orphan region `getting-started-serialization-config` is at `Server.WebApi/Samples/SerializationConfigSample.cs:10`.
- **the-problem.md:** no embeds.
  - `:27` hand-written controller/DTO "before" block (not labelled); `:43` hand-written `Employee` with `[Remote, Insert/Update/Delete]`.
  - `:7` "persistence routing engine"; `:103` "seven persistence operations"; `:67` repository.
- **client-server-architecture.md:** no embeds.
  - `:82` hand-written `IOrder`/`Order`/`OrderLine`, with `[Remote, Create]`, the child's `[Create] internal` and "Generated: internal interface IOrderLineFactory" (`:93, :99, :102`).
  - `:107` hand-written `Product` with `[Remote, Fetch]` and `[Fetch] FetchAsChild` (duality).
  - `:57-60` "No service for type 'IEmployeeRepository' has been registered"; `:73, :80` the internal-interface rule.
  - `:35` already frames `[Remote]` as a client entry point.
- **decision-guide.md:** no C# blocks.
  - `:11-16` the `[Remote]` tree asks "called directly from client code?". `:21` "routed from client → `[Remote, Insert]`".
  - `:29-43` the pattern table and tree have no read model or context. `:31` "Aggregate roots and entities with lifecycle", with `Invoice`, `IOrderQueryService` and `PromoteCommand` as examples.
  - `:90` "explicit Insert/Update/Delete calls"; `:138-144` the events tree (manual `Task.Run` + `CreateScope` for external systems) is current guidance, not a finding.
- **`src/docs/samples`:** `Neatoo.Docs.Samples.csproj` and `ReadmeSamples.cs`. It is not in either solution and is embedded only by README.
- **Design:** no `#region` anywhere in `src/Design`.
  - `Order` (interface `IOrder`, local Create, `[Remote]` Fetch/Insert/Update/Delete, `OpenDraft`) and `OrderLine` (public Create; internal `Fetch(row)`, Insert, Update) have public setters.
  - `IOrderDbContext` finds an order by id or a customer's draft; it has no line lookup.
  - `Design.Server/Program.cs` calls `AddDesignServer()`, which is `AddNeatooAspNetCore` plus services in `ServerServices.cs`, with no explicit serialization options.
  - `Design.Client.Blazor/Program.cs` has `AddNeatooRemoteFactory(Remote, asm)`, `ITextScorer`, and a keyed HttpClient; it has no `RegisterMatchingName`.
  - `AllPatterns.cs:232` "Executed on the server, called from the client" (C-01; Design comment).

---

## Punchlist

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| A line loads as a root by id from the client, null when missing; the cascade still loads and saves it as a child | Must | `[integration]` | `Design.Tests.FactoryTests.OrderLineDualityTests.OrderLine_FetchedAsARoot_ByItsId` (id from a fetched order; one call; values match), `OrderLine_FetchedAsARoot_IsNull_WhenNoOrderHoldsIt`, `OrderLine_StaysAChild_OfItsOrder` (row-loaded through the order, quantity change saved through the cascade and in the store); the cascade's existing `AggregateTests` unchanged and green | ✓ |
| Embedded model slices, client usage and server registration pinned on the containers | Must | `[integration]` | `order-interface`, `readme-domain-model`: `AggregateTests` (Create local, Fetch, Insert/Update/Delete through `Save`), `CreateOrFetchTests`; `orderline-child`: `AggregateTests`' line tests and `OrderLine_StaysAChild_OfItsOrder`; `orderline-duality`: `OrderLineDualityTests`; `readme-client-usage`: `OrderUsageTests.Client_OpensADraft_AddsALine_AndSaves` runs the region itself (two calls; saved order and line in the store); `readme-server-setup`: `DesignServerCompositionTests` (calls `AddDesignServer`) | ✓ |
| Middleware and client-registration regions compile | Must | explicit-skip | `reviews/005-design-build.log` (`Design.Server`, `Design.Client.Blazor`: 0 errors, 0 warnings) | ✓ |
| Every C# block on the five pages is a Design embed or WRONG; no region defined twice | Must | explicit-skip | Fence scan outside embeds: one block, `the-problem.md:27`, labelled WRONG. Every embed's source is `src/Design` (README 6, getting started 6, the problem 1, client-server-architecture 2). Each of the plan's 8 regions is defined once; the 5 retired names (`readme-client-setup`, `readme-full-example`, `getting-started-usage`, `getting-started-employee-model`, `getting-started-serialization-config`) are defined and embedded nowhere | ✓ |
| `src/docs/samples` deleted, unreferenced; the reference app builds | Must | explicit-skip | Directory gone; no rendered page, source, solution, `mdsnippets.json` or workflow names it (scoped grep); `EmployeeManagement.sln` built, 0 errors | ✓ |
| The five pages carry none of the findings' text | Must | explicit-skip | grep of the five pages: the remaining "persistence routing" hits are D15's "one use of that" (README:5, the-problem:7); `client-server-architecture.md:35` quotes "runs on the server" to deny it; no unreassigned `Save` | ✓ |
| The decision guide gives a read model and a context their own branch | Should | explicit-skip | `decision-guide.md` pattern table and tree: entity, read model (`OrderSummary`), context (`OrderCheckout`) | ✓ |
| mdsnippets clean, no swallowed section; both solutions green | Must | explicit-skip | mdsnippets exit 0; `git diff --stat` touches only the edited pages, `setup.md` (re-pointed embed) and the plan's files. `reviews/005-build.log` (0 errors; 2 OrderEntry WASM warnings), `005-test.log` (835 unit, 644 integration + 5 pre-existing skips), `005-design-build.log` (0/0), `005-design-test.log` (141); both TFMs | ✓ |

---

## Gate Record

- Round 1 (2026-10-14): test-reviewer CLEAN, no callouts. Its two tech-debt notes are dismissed: `scripts/extract-snippets.ps1` has a dead `docs/samples` path that nothing calls, and `VisionAlignment.md:69` is the as-found inventory. code-reviewer CLEAN with 5 callouts, all punched:
  - T-17 and T-22 ledger outcomes corrected;
  - the decision guide no longer calls a context read-only;
  - the DI paragraph's closing sentence matches its third bullet;
  - CLAUDE-DESIGN's Critical Rule 1, its `[Remote]` quick-decision row and the `OrderLine.cs` file row now show the root Fetch;
  - README says what `AddDesignServer()` is.

  The punches are prose only; no code changed after the green logs. `reviews/005-test-review.md`, `reviews/005-code-review.md`. Done.

---

## Plan Amendments

### 2026-10-14 — Plan-review callouts folded in

- **Section affected:** Constraints, Steps 1–7, Acceptance, Current State
- **What changed:** callouts 1–11 of `reviews/005-plan-review.md`:
  - the uncited finding lines are named in Steps 4–6;
  - the `[Remote]` question is "does the call cross to the server";
  - Design's own `OrderLine` statements are rewritten with the root Fetch;
  - T-16 gives both outcomes with their conditions;
  - the `EmployeeModel`-dependent samples go and `EmployeeManagement.sln` is built once;
  - regions are slices that pages reuse;
  - `the-problem.md:43` is embedded;
  - the duality test takes its line id from a fetched order;
  - the samples grep is scoped;
  - the events tree stays.
- **Why:** detail against the code and the audit; intent unchanged. Callouts 3 and 7 were Keith's to decide (Notes).
- **Discovery Log:** none

### 2026-10-14 — Regions as built

- **Section affected:** Constraints (region names), Step 2
- **Original said:** each region keeps its name, and `getting-started-employee-model` is renamed.
- **What changed:**
  - Eight regions. Two are new for the order: `order-interface` and `readme-domain-model` (Create, Fetch and the writes in `Order.cs`). Two are new for the line: `orderline-child` and `orderline-duality`.
  - Four keep their names, now defined in Design: `readme-client-usage`, `readme-server-setup`, `getting-started-server-program` and `getting-started-client-program`.
  - Pages that need the same lines reuse a region. Five names are therefore retired, with their definitions deleted and nothing embedding them: `readme-client-setup`, `readme-full-example`, `getting-started-usage`, `getting-started-employee-model` and `getting-started-serialization-config`. No region is renamed.
  - `Order.cs`'s `FetchDraft` and `OpenDraft` moved below the writes. The rationale for Create, Fetch and the writes now sits in one comment block directly above `readme-domain-model`, and `OrderLine.cs` and both `Program.cs` files follow the same pattern.
  - nesting proved to work (the inner markers are stripped from the outer block), but no region needed it.
- **Why:** reuse leaves one definition per region and removes the need for a rename.
- **Discovery Log:** none

---

## Notes

- 2026-10-14, Keith:
  - README shows the aggregate and a screen. The domain model is `IOrder`/`Order` with its operations. Client usage opens a draft, adds a line through `IOrderLineFactory`, and reassigns `order = await factory.Save(order)`. The auth "full example" goes, with a link to the authorization page; EXSET-009 owns `AuthorizedOrder`.
  - Entity duality is `OrderLine` with a `[Remote, Fetch]` by line id, a root for a line-detail screen while it stays a child of `Order`.
- 2026-10-14, Keith, on plan-review callouts 3 and 7:
  - Regions are slices around signatures and bodies, with the remarks just above each region. README and getting started reuse them.
  - The `Program.cs` middleware and client-registration regions are build-pinned (explicit-skip). The server registration region lives in `ServerServices`, which the composition test runs.
