# EXSET-005 plan review — 2026-10-14

**Reviewer:** plan-reviewer. **Verdict:** APPROVED. No veto-tier findings.

Pass A: Keith's two rulings are respected. D14 (`Order.Create` local), R-C (the line's Create stays public; the new Fetch is `[Remote] internal`) and R-I (a false Fetch; Update throws) all hold.

Pass B reality check:
- **Overload naming.** The generator names the second `Fetch` `Fetch1` (`FactoryModelBuilder.cs:934-1005`), so `IOrderLineFactory` gains a public `Fetch(Guid)` beside the internal `Fetch(OrderLineRow)`. `OrderLineList` still binds to the row overload.
- **Store.** `IOrderDbContext` has one implementer.
- **Embeds.** `readme-*` and `getting-started-*` regions are embedded only by README, `getting-started.md` and `setup.md:107`.
- **Names.** No colliding names in `src/Tests` or `src/Generator`.

## Callouts (all affect a Must criterion)

| # | Finding | Reachable by | Triage |
|---|---|---|---|
| 1 | Finding text the plan does not list: C-03 `client-server-architecture.md:44`, U-02 `:40`; C-28 `the-problem.md:5, :22, :42, :86, :98`, `client-server-architecture.md:5, :140`, `getting-started.md:215`; I-11 `getting-started.md:239` ("types with `[Factory]`", against R-F); T-09 `getting-started.md:43` (client-side relay methods); Employee/repository prose at `getting-started.md:13, :200-203`, `client-server-architecture.md:3` | grepping only the Current State lines | **Amend**: Current State and Steps 5–6 name them; Acceptance bullet 5's grep covers them |
| 2 | "Is this a client entry point? → `[Remote]`" still sends a local Create, a `Can*` check and a bare `[Execute]` to `[Remote]` (C-03's diagnosis) | Step 7 as worded | **Amend**: the tree asks "does the call need to cross to the server?" and lists the local shapes the client calls; the same at `client-server-architecture.md:39-44` |
| 3 | Setup regions: `Design.Server/Program.cs` holds only `AddDesignServer()` and `UseNeatoo()`; registration is in `ServerServices.cs`; no test runs either `Program.cs`, and Design.Tests cannot reference the WASM client, whose harness registration differs | Step 2's setup regions | **Ask** → Keith: the server registration region lives in `ServerServices`, pinned by the composition test; the middleware and client `Program` regions are build-pinned (explicit-skip); the client `Program.cs` is reordered so the `ITextScorer` block sits outside the region |
| 4 | The root Fetch makes Design's own statements false: `OrderLine.cs:15-21, :131-136`; `CLAUDE-DESIGN.md:18, :380, :844, :1099`, and Anti-Pattern 7 (a hand-written `Product`) | Step 1 editing only the new method's remarks | **Amend**: Step 1 names them |
| 5 | T-16's replacement must state the conditions: the guard is emitted only for `internal`/`[Remote]` methods and fires only when `IsServerRuntime` is false (default true); otherwise a missing server service gives the DI error; an `internal` member is not callable from another assembly | Step 6's "(the server-only guard, or a missing server service)" | **Amend**: Step 6 states both outcomes with their conditions |
| 6 | Deleting the getting-started samples breaks the reference app: `typeof(EmployeeModel)` in `ServerProgramSample.cs:21`, `ClientProgramSample.cs:16`, `SerializationConfigSample.cs:22, :27`; `EmployeeManagement.sln` is in neither solution nor CI | Step 3 | **Amend**: delete those three files with `EmployeeModel.cs`, and build `EmployeeManagement.sln` once |
| 7 | Region size and overlap: an `IOrder`/`Order` region is ~300 lines of remarks; README and getting started want the same lines; root and child are in different files; nesting has no precedent | Step 2 | **Ask** → Keith: slice regions around signatures and bodies, with the remarks just above each region; pages reuse regions; probe mdsnippets for nesting first |
| 8 | `the-problem.md:43` ("With RemoteFactory" `Employee`) has no region | Step 5 | **Amend**: it reuses the domain-model slices |
| 9 | Seeded line ids are random (`SeedRow` uses `Guid.NewGuid()`) | Step 1's test | **Amend**: the test gets a line id from a fetched order |
| 10 | "Nothing references it" cannot hold repo-wide (VisionAlignment, the rule file, todos, `scripts/extract-snippets.ps1`'s dead path) | verifying bullet 4 | **Amend**: the grep's scope is rendered pages, sources, solutions and `mdsnippets.json` |
| 11 | `decision-guide.md:138-141` (manual `Task.Run` + `CreateScope`) is current guidance; T-09 does not cite it | Step 7 | **Amend**: the events tree stays; Current State drops it as a finding |

## Theoretical (not triaged)

- `AllPatterns.cs:232` "Executed on the server, called from the client" is accurate for an interface-factory method.
- `InternalsVisibleTo Design.Tests` lets a client-usage region in a test compile against internals a real client cannot call.
- `skill-entity-duality` is taken (reference app, embedded by `advanced-patterns.md:189`): the new region needs another name.
- A line fetched as a root has no root save path; the remarks may say so.
- `SKILL.md:121` has a bare opening snippet marker in prose (pre-existing; not 005's pages).
