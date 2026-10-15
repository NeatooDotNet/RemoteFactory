# EXSET-005 code review — 2026-10-14

**Reviewer:** code-reviewer (per-plan, Step 5; `fa99a75` against `d74c684`). **Verdict:** CLEAN. No veto-tier findings.

**Build.** 0 errors in all three builds:
- Main solution: 2 warnings, both the OrderEntry WASM `NativeFileReference` notice (`005-build.log`).
- Design solution: 0 warnings (`005-design-build.log`).
- Reference app: 0 warnings (`005-refapp-build.log`).

**Tests.** 0 failed on both TFMs: unit 835, integration 644 + 5 skipped (Showcase performance and RelayTiming, pre-existing), Design 141. The Design count rose by exactly the four tests the plan added (137 → 141): three in `OrderLineDualityTests` and one in `OrderUsageTests`. So the Design run exercised this change.

The deliverable landed as scoped:

- **`OrderLine`'s root Fetch (R-G, R-I).**
  - `OrderLine.cs:187-201` is `[Remote, Fetch] internal Task<bool> Fetch(Guid id, [Service] IOrderDbContext db)`. It returns false when `FindLineAsync` finds nothing, and otherwise maps through the shared `LoadFrom`.
  - The child operations are unchanged in signature and visibility: public `Create`, internal `Fetch(row)`, `Insert` and `Update`. The row `Fetch` now calls the same `LoadFrom`.
  - `ClassFactoryRenderer.cs:123-124` promotes a `[Remote] internal` member to `public` on the interface, so the client can call `Fetch(Guid)`. The row `Fetch` and `Save` keep their `internal` modifier.
  - `IOrderDbContext.FindLineAsync` has one implementer (`InMemoryOrderDbContext`). It reads committed rows under the lock, like `FindAsync`.
  - The tests take the line id from a fetched order, count exactly one remote call, get null for an unknown id, and save a quantity change through the cascade into the store.
- **Regions and moved remarks.**
  - There are eight regions, each defined once. Every embed's line range matches its `#region`/`#endregion` lines in source. Nothing on the five pages, or in `setup.md`, is embedded from the reference app.
  - The XML `<remarks>` moved into one line-comment block above each region. None of their content was lost: `Order.cs`'s D14, R-I, routing, R-D and missing-row rationale, `OrderLine.cs`'s R-C and DID-NOT-DO notes, and both `Program.cs` files' DESIGN DECISION blocks are all still there.
  - `Design.Client.Blazor/Program.cs` keeps `ITextScorer` outside `getting-started-client-program`, as Keith ruled.
- **The five pages.**
  - The only C# fence outside an embed is `the-problem.md:27`, which is labelled WRONG.
  - The D15 headline is on README:3-5 and `the-problem.md:7`. Persistence routing appears only as "one use of that".
  - `client-server-architecture.md` and the decision guide frame `[Remote]` as "does the call cross" and list the local shapes (C-03, U-02).
  - `client-server-architecture.md:65-67` matches the library on the guard: `ClassFactoryRenderer.cs:393-399` emits it only for `internal` or `[Remote]` methods, and `NeatooRuntime.cs:13-16` defaults to `true`. One sentence on that page is wrong (callout 3).
  - Both setup regions name `typeof(IOrder).Assembly`. The client region has no `RegisterMatchingName` "fall back" line (T-20).
- **Deletions.**
  - `src/docs/samples` is gone. No rendered page, source file, solution, project, workflow or `mdsnippets.json` names it; the remaining references are in VisionAlignment, the rule file and todos.
  - Every `<!-- snippet: -->` marker under `docs/`, `README.md` and `skills/` resolves to a region in `src/`. The only unmatched names are `name` and `x`, which are prose mentions on `SKILL.md:121` and in completed todos.
  - The five retired names are neither defined nor embedded anywhere.
  - The three reference-app files that stay until EXSET-012 lost only their `#region`/`#endregion` lines.
- **Skill.** `setup.md:107`'s `getting-started-client-program` embed re-pointed by itself to Design's region. Its surrounding prose made no claim about the removed `RegisterMatchingName` or serialization options lines.
- **Ledger.** All 21 rows the plan names are recorded. 19 of them match the diff. T-17 misstates its outcome (callout 1), and T-22's outcome leaves out the README regions that 005 retired.

**Sacred tests.** No file under `src/Tests` or `src/Design/Design.Tests` was modified. The only test changes are the two new files, and `AggregateTests` is unchanged and green.

| # | Callout | Affects | Disposition |
|---|---|---|---|
| 1 | `audit-ledger.md:72` gives T-17's outcome as "005: none on 005's pages". The audit cites `docs/getting-started.md:219` and `docs/the-problem.md:103`, which are both 005's pages, and this diff removed "seven" from both. A reader of the ledger sees no fix where one landed. Separately, `:77` says T-22 is "none on 005's pages". The finding's sites (`SKILL.md`, `CLAUDE.md`) are indeed elsewhere, but its diagnosis names `readme-domain-model`, `readme-full-example` and `readme-client-usage`, and 005 replaced or retired all three. The T-22 point is minor. Confidence High (T-17). Reachable by: the close-out audit's AC-4 trace, which reads these outcomes. One option: "fixed on 005's pages (getting started, the problem); factory-operations.md:7 left for 007" | AC-4 · Must | |
| 2 | `docs/decision-guide.md:99` labels the `IFactorySaveMeta` "NO" branch "read-only: a read model or a context". A context is not read-only. The same page's pattern tree (`:46-47`) and `OrderCheckout.cs:5-15` say its verbs change the held order and save it through the order's root. What is true is that the context does not implement `IFactorySaveMeta` itself. Confidence High. Reachable by: a reader of the decision guide deciding whether a context needs `IFactorySaveMeta`. One option: "NO (a read model, or a context that saves through the root it holds)" | AC-4 · Must (C-29, T-03) | |
| 3 | `docs/client-server-architecture.md:69` closes the T-16 paragraph with "All of these are discovered when the method is called, or when the client is published — the domain assembly compiles either way." That contradicts its own third bullet (`:67`), where calling an `internal` member from another assembly is a compile-time error. "When the client is published" is not when anything is discovered, either: in a client published with the switch off, the guard throws at call time. Confidence High. Reachable by: a reader of the page's "What Happens If You Get It Wrong" section | AC-4 · Must (T-16) | |
| 4 | Three of Design's own statements about `OrderLine` are now false or incomplete. Plan-review callout 4 did not list their lines, so they were not rewritten: `CLAUDE-DESIGN.md:730-750`, Critical Rule 1 ("[Remote] is ONLY for Aggregate Root Entry Points"), labels a hand-written `OrderLine` "CORRECT: Child entity does NOT have [Remote]", but `OrderLine.cs` now has a `[Remote, Fetch]`, and that label is the type-based reasoning Anti-Pattern 7 (`:595-597`) calls the mistake; `:373` answers "Should this method be [Remote]?" with "Only aggregate root entry points", citing "`Order.cs` vs `OrderLine.cs`" as the contrast; `:1211`'s file table describes `OrderLine.cs` without the root Fetch. Confidence High (`:740`), Medium (`:373`, `:1211`). Reachable by: an agent or reader consulting CLAUDE-DESIGN's Critical Rules, which CLAUDE.md names as Design's quick reference | AC-4 · Must (U-09, R-G) | |
| 5 | `README.md:194-204` shows `services.AddNeatooAspNetCore(typeof(IOrder).Assembly)` and then "and add its endpoint to the pipeline:" over a `Program` that calls `builder.Services.AddDesignServer()`. README never says that `AddDesignServer` is Design's own method and already contains the first call. Getting started explains it (`getting-started.md:57`); README does not. A reader copying both blocks either registers twice or looks for an `AddDesignServer` that does not exist. Confidence Medium. Reachable by: a README reader following "Getting Started" without opening the walkthrough. One option: one clause after the first embed, as getting started has | AC-3 · Must | |

Theoretical (not triaged):

- The line's "read-only" remark (`OrderLine.cs:180-182`) says "To change it, fetch its order and save through the root". `IOrderLine` and `OrderLineRow` expose no order id, so a screen holding only a root-fetched line cannot find its order from it.
- The Save Routing bullets on `getting-started.md` read "`IsNew = false` → Update" and "`IsDeleted = true` → Delete" as if they did not overlap. Save-routing detail is EXSET-008's page.
- `client-server-architecture.md`'s list of reasons a root Create can need the server ("tenant defaults, a sequence") omits D14's `[AuthorizeFactory]` case, which Design's own `Order.cs` remark includes.
- `skills/RemoteFactory/references/setup.md` now shows an Order client registration beside Employee server samples (`:28`, `:55`, `:89`). Those embeds belong to EXSET-006 and EXSET-010.
- `readme-domain-model` calls the private `LoadFrom` and `MapTo`, which no embed shows. This follows from Keith's slice ruling (2026-10-14).

Read beyond the brief:

- `ClassFactoryRenderer.cs:111-135`, the interface promotion, and `:340-405`, the guard emission;
- `NeatooRuntime.cs`, the switch default;
- `Contexts/OrderCheckout.cs`, the context's verbs and save;
- `ReadModels/OrderSummary.cs`;
- `Services/CustomerDirectory.cs:258-260`, `ICustomerDirectory` as `[Factory]`;
- `DesignServerCompositionTests.cs:72`, which calls `AddDesignServer`;
- `005-plan-review.md`, for callout 4's line list;
- the audit rows C-01, C-19, I-01, I-12, T-09, T-16, T-17, T-22 and U-02 in `Neatoo/docs/todos/consistency-audit/audit-remotefactory.md`.

Named but unused:

- `CLAUDE-DESIGN.md` was read through the diff and targeted greps for `OrderLine` and `[Remote]`, not in full.
- The generated `OrderLineFactory.g.cs` is not on disk (`Design.Domain/Generated` is empty), so the promotion was traced in the renderer instead.
- There is no calibration file and no 005 test-review file.
