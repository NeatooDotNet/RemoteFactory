## Close-Out Audit — 2026-10-15

**Grade: C** — one veto-tier finding is open: the EXSET-006 park candidates (V-1). Every Acceptance Criterion traces to the tree, and the build and tests are green.

Judged against the iterative-todo 0.11.x vocabulary the todo ran under: Must / Should / Could, and Follow-on lines parked at the sweep. Naming differences from 0.12.0 are not findings.

### Acceptance Criteria Trace
| AC | Word | Evidence (file:method:line) | Holds? | If not: parked as? |
|---|---|---|---|---|
| AC-1 | Must | `.github/workflows/build.yml:93-106`: "Build Design projects" and "Test Design projects" run `src/Design/Design.sln` in Release (with trx) after the main solution's steps at `:75-89`. | Yes. The YAML is in place, but no CI run has executed it yet (see Could not verify). | — |
| AC-2 | Must | **Context:** `Design.Domain/Contexts/OrderCheckout.cs`. `[Remote, Fetch] Fetch(orderId)` holds the `IOrder`; its verbs are client-side; `SaveAsync` saves through the root. Pinned by `ReadModelAndContextTests.OrderCheckout_*`.<br>**Read model:** `ReadModels/OrderSummary.cs`, Fetch-only. Pinned by `OrderSummary_FetchedThroughClient_ArrivesWithLineRecords`.<br>**Domain-service interface factory:** `Services/CustomerDirectory.cs:262-282` (`[Factory] [AuthorizeFactory<ICustomerDirectoryAuth>] public interface ICustomerDirectory`) and `FactoryPatterns/AllPatterns.cs:229` (`IProductCatalog`). Pinned by `InterfaceFactoryTests`, `InterfaceFactoryAuthorizationTests`.<br>**R-D cascade with public child Create:** `Entities/OrderLine.cs:132-133` has a public local `[Create]`. `OrderLineList.Update` at `:314-339` removes deleted rows, adds new ones and calls `lineFactory.Save(line, row)`. `Order.Insert`/`Update` at `Aggregates/Order.cs:274-300` call `lineListFactory.Save`. Pinned by `AggregateTests.Order_EditLinesFromClient_SavesExactlyThoseChanges:310` (client-side add, remove and edit; the store holds exactly those rows).<br>**Create-or-fetch:** `Order.OpenDraft` at `Order.cs:363-370` is a `[Remote, Execute]` that calls `FetchDraft` and falls back to `Create`. Pinned by `CreateOrFetchTests`.<br>**Value objects:** `ValueObjects/Money.cs` is a plain record (no `[Factory]`). Beside it, `ValueObjects/TaxRate.cs` is a `[Factory]` record whose `[Remote, Fetch]` takes `[Service] ITaxTable`. Pinned by `ValueObjectTests`. | Yes | — |
| AC-3 | Must | 32 markdown files (`docs/` excluding todos, plans and release-notes; `README.md`; `skills/RemoteFactory/`) hold 142 embed markers naming 74 regions.<br>Each region is defined exactly once in the tracked tree, always under `src/Design/`. `skill-blazor-inject` is a `begin-snippet` at `Design.Client.Blazor/Pages/Home.razor:17`.<br>Every rendered "snippet source" href points under `/src/Design/`.<br>The 8 hand fences on `docs/` pages are all short blocks labelled WRONG: `factory-events.md:511,527,545`, `interface-factory.md:134,150`, `service-injection.md:91`, `the-problem.md:27`, `trimming.md:403`. | Yes | — |
| AC-4 | Must | `audit-ledger.md` has 86 rows. Their IDs (C-01..30, I-01..15, T-01..29, U-01..12) match the 86 IDs in `Neatoo/docs/todos/consistency-audit/audit-remotefactory.md` exactly, and every row carries an outcome.<br>Spot-checked against the tree:<br>• `CODING_STANDARDS.md` is gone (T-15).<br>• `CLAUDE.md:150,154,162,164` state the tier rule (I-02), D14 (U-02), R-G (U-09) and both enforcement outcomes (T-16).<br>• No `TryFetch`, `IFactoryCore`, `DataPortal_`, "seven operations", "persistence routing", "Data Mapper" or 409 claim remains in any doc page, README or CLAUDE.md.<br>• `trimming.md:425-427` states R-M (U-12).<br>The T2-only rows (C-08, C-17, C-25, I-15, T-05, T-08, T-21, T-23, T-28) have no hit outside the skill. | Yes, with callout C-1 (one stale count left in VisionAlignment §7). | — |
| AC-5 | Should | A grep for `lazyload\|ILazyLoadFactory` over the published doc pages, `README.md` and `skills/` returns nothing. The only hits are `docs/VisionAlignment.md` (the R-K ruling, a working document) and `docs/plans/completed/` (history, excluded from mdsnippets). `CLAUDE-DESIGN.md:405` answers deferred loading with R-K. | Yes | — |
| AC-6 | Must | `src/docs` does not exist. `git ls-files src/docs` is empty, and no `.sln`, `.csproj`, `.props`, `.json` or `.yml` references it. | Yes | — |
| AC-7 | Could | `.claude/rules/design-snippets.md` (path-scoped frontmatter: `src/Design/**`, `docs/**/*.md`, `README.md`, `skills/RemoteFactory/**`) names Design as the snippet source. `.gitattributes:9` has `*.md text eol=lf`, and `git check-attr eol` reports `lf` for `.md` files. | Yes | — |

### Veto-Tier Findings
**V-1 · Untraced deferral: the EXSET-006 park candidates were never parked or dismissed on the todo.** Area 6.5. Confidence: High.

- **The candidates.** `plans/006-aspnetcore-modes-serialization.md:291-294`, under Notes, reads: "**Candidates to park as issues.** These are library behaviour, not this plan's to change; Keith decides at the gate." It lists three:
  - logger categories: `RemoteFactory.Trace` sits outside the prefix, the `Remote`/`Authorization` constants are unused, and the client HTTP call has no logger;
  - the static logger factory `UseNeatoo` sets, which outlives its host;
  - the client never reading `X-Neatoo-Format`.
- **The plan review sent the static there.** `reviews/006-plan-review.md` callout 2 was dispositioned Amend with the words "The static joins the park candidates".
- **What the Gate Record says.** `plans/006-...md:246` reads: "documented on the page as current behaviour, and no issue is filed for them." The documentation part holds: `aspnetcore-integration.md:108,254` documents the header and the category.
- **What is missing:**
  - no Dismissed row on `todo.md`;
  - no Parked issue (a search of every issue finds none on these topics);
  - no Discovery Log "Stop → Keith" entry recording that Keith made the call the plan reserved for him;
  - PR #132's "Deliberately open" list omits them.
- **Why it is veto-tier.** A deferral addressed to Keith was resolved by the loop and lives only in a plan's Gate Record. The static-logger-factory lifetime is a latent library defect that a plan review measured, and it is now tracked nowhere.
- **What must change.** The three candidates need a traceable disposition on the todo: a Parked issue number or a Dismissed row, with who decided.

### Callouts (Must in full; ≤ 5 Should/Could)
**C-1 · Punchlist row 1 is checked, but VisionAlignment §7 still says "162 embeds".** Area 6.6 / AC trace. Confidence: High. `Affects: AC-4`.

- **The row.** Row 1 names `docs/VisionAlignment.md` "§1.1, §7 · done when the count matches".
- **What the sweep fixed.** Commit `8596dd9` (PR #153) corrected §1.1, line 68: "All 165 rendered blocks (155 embed markers)".
- **What it missed.** §7, line 238, still says "`src/docs/samples` deleted, and 162 embeds re-pointed".
- **The closure cell.** The row closes with "· the close-out sweep", which names no commit or PR.
- `Reachable by:` a reader of VisionAlignment §7, the rulings document that `CLAUDE.md`, the rules file and the todo's Sources of record all point to.

**C-2 · Discovery Log prose over budget: 6 of 11 entries exceed 60 words.** Area 6.9. Confidence: High. `Affects: AC-3`.

- The worst is "2026-10-15 · EXSET-009 · Amend" (the static `[AuthorizeFactory<T>]` fix), at 110 words.
- The others run 102, 89, 76, 68 and 65 words.
- `Reachable by:` a reader of the todo's Discovery Log.

### Could not verify
- **AC-1 in execution.** `build.yml` triggers only on push or PR to `main`, and `gh run list --branch EXSET` shows no run. The Design build and test steps have never executed in CI. `plans/001` says CI confirms this on the arc's PR into main. Local Release evidence: `reviews/001-design-build.log`, `001-design-test.log`.
- **Standing-order check 6.12.** The arc PR into main is not yet open, so I cannot check that its body lists every decision the loop took on a sentence attributed to Keith. V-1's decision is one such item.

### Theoretical (not triaged)
- Plans 001, 002 and 003 are Done, but their Acceptance checkboxes are still `- [ ]`. Their Test Evidence rows are all ✓, and plans 004–016 check theirs.
- `plans/012:35` excludes "`src/Examples` (Follow-on)", but the only Follow-on line on `src/Examples` is #150, which covers Person only. `src/Examples/OrderEntry` has no line, and I know of no finding against it.
- #112 (interface-factory examples expose a repository) is still open, while ledger C-27's outcome cites it as handled by EXSET-003. The skill's `interface-factory.md:137` still shows `IEmployeeRepository` (Todo 2 prose). Whether the arc closes #112 is not recorded.

### Build & Test
- **Build:** PASSED — `reviews/final-build.log`. Both solutions built: main, then `Design.sln` (Design.Domain, Client.Blazor, Server, Tests). 0 errors. Main has 2 warnings, the known `OrderEntry.BlazorClient` WASM `NativeFileReference` notice. Design has 0 warnings. The log was written 17:29, after HEAD `36a46ad` (17:28).
- **Tests:** 0 failed — `reviews/final-test.log`, on net9.0 and net10.0:

  | Suite | Passed | Skipped |
  |---|---|---|
  | Unit | 855 | 0 |
  | Integration | 657 | 7 |
  | Design | 175 | 1 |

  Every skip carries a reason:
  - 2 flaky relay-timing tests, skipped earlier by user decision;
  - 3 optional performance demos;
  - 2 known-bug regression tests for #133 (`FactoryReferenceIdentityTests`, added by EXSET-013 per Keith's ruling);
  - 1 known-bug test for #104 (`SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull`, new in EXSET-008 and written the natural way).

  No pre-existing test file was deleted. The assertion removals in modified Design tests are reshapes: local mode moved to the client/server path, Save results are reassigned, and `IExampleRepository` was renamed to `ICustomerDirectory`. All of them went through each plan's test review.
- **Test Evidence citations:** all 99 test-method names cited across the 16 Test Evidence sections exist in `src/`, as do the cited test classes, including `StaticAuthorizeFactoryTests` and `SaveMetaRoundTripSmokeTest`. Every Acceptance bullet has a row, and no `MISSING` row exists. Each Gate Record has at most two rounds.
- **Library changes are internal or additive:**
  - `IFactoryEventCollector.Clear()` is on an `internal` interface;
  - `PreserveProperties<T>` uses only a `[DynamicallyAccessedMembers]` annotation;
  - `HttpRequestException` now carries its `StatusCode`;
  - the static-factory authorization is rendered by the generator.

  No reflection was introduced.

### Container
- **Plans issued / cap:** 16 / 18. The initial split was 12, and the four re-splits (006→013, 009→014, 010→015, 011→016) are logged in the Discovery Log.
- **Status cells over budget:** 0. All read "Done".
- **Untraced deferrals:** EXSET-006's three park candidates (V-1). Every other "Open, by design" item in the Gate Records traces:
  - to a Plan Index row (009, 010, 011, 014, 015, 016);
  - to an issue (#104, #131, #133, #135, #141);
  - to a Follow-on line now parked (#148–#152);
  - or to Docs & Retro (the release-note lines);
  - 009's flag-based auth classes are a "Does NOT touch" scope line, not a deferral.
- **Prose over budget:** Discovery Log "EXSET-009 · Amend" (static `[AuthorizeFactory<T>]`), 110 words. The Goal is 94 words.
- **Open PRs into the arc:** None. #118–#147 and #134 for plans 001–016, plus #139 and #153, are all MERGED into `EXSET`. Every Plan Index PR number matches.
- **Must parked without demotion:** None.
- **Dispositions living only in a PR body:** None. V-1's disposition lives only in a plan's Gate Record, not on the todo.

### Parked (as the todo lists them)
- #148 · skill hand-written C# names retired sample types (Origin: EXSET-015)
- #149 · skill `polymorphic-hierarchy.md` Key Rule 4 states inheritance backwards (Origin: EXSET-014)
- #150 · Person example keeps the private-setter / `[JsonInclude]` / `[DynamicDependency]` recipe (Origin: EXSET-016)
- #151 · CI publishes the trimming harness only at `TrimMode=partial` (techdebt; Origin: EXSET-016)
- #152 · skill "Code Sample Sources" still says hand-write partial examples (Origin: EXSET-012)

All five are open, and each body opens with `Origin: EXSET-NNN · …`, checked with `gh issue view`.

### Read report
- **Read beyond the brief:**
  - `~/.claude/skills/iterative-todo/SKILL.md` (plan-review is opt-in, which settles that plans 010, 011, 012 and 015 have no plan-review file);
  - `reviews/006-plan-review.md` callout 2;
  - PR #132's body;
  - `gh issue list`, to search for the 006 candidates and check the referenced issues #104, #112, #114, #119, #128, #131, #133, #135, #138, #141;
  - `mdsnippets.json`;
  - the arc diff from merge-base `6c2e46b` for `src/Generator`, `src/RemoteFactory` and the modified test files;
  - `src/Tests/RemoteFactory.TrimmingTests` (`Program.cs`, `.csproj`) and `reviews/016-ci-run.log`.
- **Named but unused:** the per-plan `*.log` files other than `016-ci-run.log`, because the final logs supersede them. The individual code-review and test-review files were not re-read: their closures are summarised in each Gate Record, and I verified the cited tests exist rather than re-walking the reviews.

---

## Close-Out Re-Audit — 2026-10-15 (round 2)

**Grade: A** — V-1, the only round-1 veto, is resolved. Every Acceptance Criterion still traces to the tree, and the build and tests are green. Two items remain under Could not verify, both carried from round 1 and both about events that come later.

The calibration is the same as round 1: iterative-todo 0.11.x vocabulary (Must / Should / Could), with Follow-on lines parked at the sweep.

### Round-1 items verified
- **V-1 · resolved.** Changed in `b2d1bb2` (PR #157, MERGED into `EXSET` 2026-10-15T22:42Z).
  - #154 (logger categories), #155 (`UseNeatoo` static logger factory outlives the host) and #156 (client never reads `X-Neatoo-Format`) are all OPEN and labelled `techdebt`. `gh issue view` shows each body opening `Origin: EXSET-006 · …`. #155 cites `reviews/006-plan-review.md` callout 2.
  - `plans/006-aspnetcore-modes-serialization.md:246` (Gate Record, "Open, by design") now reads "parked at the close-out audit as #154, #155 and #156".
  - `todo.md:129` (Docs & Retro, **Parked**) and `todo.md:113` (Close-Out Audit) list all three.
  - The Notes at `plans/006:291-294` and the `UseNeatoo` static at `plans/006:93` now trace to these issues.
- **C-1 · resolved.** `docs/VisionAlignment.md:238` now says "every embed (155 markers, 165 rendered blocks) re-pointed", which matches §1.1 at `:68`. A grep finds no "162" left in the file. Punchlist row 1 (`todo.md:68`) now closes with "#153; section 7 in the audit fixes".
- **C-2 · resolved.** I recounted all 11 Discovery Log entries: 48–58 words each, and the longest is EXSET-011 Re-split at 58. Each trimmed entry keeps its `Serves AC-n`, its ruling and its issue or test name. The Goal is 94 words.
- **Theoretical (boxes) · resolved.** In plans 001–003 the word diff is 21 `[ ]` → `[x]` and nothing else. No plan has an unticked Acceptance box.
- **Theoretical (#112) · accepted as stated.** Ledger C-27 (`audit-ledger.md:37`) lists T2 among its owners. #112 stays open for the skill prose.

### Acceptance Criteria Trace
The arc diff since round 1 (`36a46ad..60445ee`) touches 7 markdown files and no source. Round 1's code evidence therefore stands unchanged. I re-checked what that diff could have affected.

| AC | Word | Evidence (file:method:line) | Holds? | If not: parked as? |
|---|---|---|---|---|
| AC-1 | Must | `.github/workflows/build.yml:93-106` runs `Design.sln` build and test in Release (unchanged). | Yes. Not yet executed by CI (see Could not verify). | — |
| AC-2 | Must | As in round 1: `Contexts/OrderCheckout.cs`, `ReadModels/OrderSummary.cs`, `Services/CustomerDirectory.cs:262-282`, `Entities/OrderLine.cs:132-133` + `OrderLineList.Update:314-339`, `Order.OpenDraft:363-370`, `ValueObjects/Money.cs` + `TaxRate.cs`. Pinned by tests that pass in `final-test.log` (Design 175). | Yes | — |
| AC-3 | Must | Recount: the 32 tracked doc/README/skill files hold 142 `snippet:` hits. One is prose (`skills/RemoteFactory/SKILL.md:119`, "`<!-- snippet: name -->`"), leaving 74 regions. Every rendered "snippet source" link points under `/src/Design/`; the only two non-Design matches are prose lines in `VisionAlignment.md:37,74`. | Yes | — |
| AC-4 | Must | `audit-ledger.md` has 86 rows, each with an outcome. C-1's stale count is fixed (`VisionAlignment.md:238`). | Yes | — |
| AC-5 | Should | Unchanged since round 1. #157 touched no doc page, README or skill file. | Yes | — |
| AC-6 | Must | `src/docs` is still absent from the tree. | Yes | — |
| AC-7 | Could | `.claude/rules/design-snippets.md` and `.gitattributes:9` are unchanged. | Yes | — |

### Veto-Tier Findings
None.

### Callouts (Must in full; ≤ 5 Should/Could)
None.

### Could not verify
- **AC-1 in execution.** `gh run list --branch EXSET` is still empty, because `build.yml` runs only for `main`. The Design steps first run on the arc's PR into main, as `plans/001` states.
- **Standing-order check 6.12.** The arc PR into main is not open yet (`gh pr list --head EXSET` is empty). Its body should list the loop's decisions on sentences attributed to Keith. Among them: EXSET-006's "Keith decides at the gate" (`plans/006:291`), resolved by parking #154–#156 at the close-out audit.

### Theoretical (not triaged)
- `todo.md:113,129` say the three 006 candidates were parked "at the close-out audit" but do not say who chose park over dismiss. The plan reserved that choice for Keith. The Step 7 triage makes him the decider by definition, so nothing is missing; the record just leaves it implicit.
- Carried from round 1: `plans/012:35` excludes "`src/Examples` (Follow-on)", but the only Follow-on line, #150, covers Person. `src/Examples/OrderEntry` has no line, and I know of no finding against it.
- `VisionAlignment.md:238` says all 155 markers were "re-pointed". 142 remain because some were retired with their pages (for example `references/lazyload.md`). The count matches recon, which is what Punchlist row 1 asked for.

### Build & Test
- **Build:** PASSED — `reviews/final-build.log`. Two `Build succeeded` blocks, 0 errors. The main solution has 2 warnings (the known OrderEntry WASM notice); Design has 0. The log is from 17:29, after `36a46ad`. The only later change is `b2d1bb2`, which is markdown only.
- **Tests:** 0 failed — `reviews/final-test.log`. On both net9.0 and net10.0: Unit 855/855, Integration 657 passed + 7 skipped, Design 175 passed + 1 skipped. The skips are the same reasoned set as in round 1.

### Container
- **Plans issued / cap:** 16 / 18. The four re-splits are logged.
- **Status cells over budget:** 0.
- **Untraced deferrals:** None. Every deferral phrase in `plans/*.md` now traces, including `plans/006:291` → #154–#156.
- **Prose over budget:** None. The longest Discovery Log entry is 58 words; the Goal is 94.
- **Open PRs into the arc:** None. #157 and #153 are MERGED.
- **Must parked without demotion:** None.
- **Dispositions living only in a PR body:** None.

### Parked (as the todo lists them)
- #148 · skill hand-written C# names retired sample types (Origin: EXSET-015)
- #149 · skill `polymorphic-hierarchy.md` Key Rule 4 states inheritance backwards (Origin: EXSET-014)
- #150 · Person example keeps the private-setter / `[JsonInclude]` / `[DynamicDependency]` recipe (Origin: EXSET-016)
- #151 · CI publishes the trimming harness only at `TrimMode=partial` (techdebt; Origin: EXSET-016)
- #152 · skill "Code Sample Sources" still says hand-write partial examples (Origin: EXSET-012)
- #154 · logger categories: `RemoteFactory.Trace` outside the prefix; `Remote`/`Authorization` unused (techdebt; Origin: EXSET-006)
- #155 · `UseNeatoo` stores the host's logger factory in a static that outlives the host (techdebt; Origin: EXSET-006)
- #156 · client never reads the `X-Neatoo-Format` response header (techdebt; Origin: EXSET-006)

All eight are OPEN, and `gh issue view` confirms each body opens with `Origin: EXSET-NNN · …`.

### Read report
- **Read beyond the brief:**
  - `git diff 36a46ad 60445ee`, the whole of PR #157;
  - `gh pr view 157`;
  - `gh issue view` for #112, #148–#152 and #154–#156;
  - `gh run list --branch EXSET`;
  - a re-grep of the embed markers and "snippet source" links across the 32 doc, README and skill files;
  - a deferral-phrase grep over `plans/*.md`.
- **Named but unused:** none. Per-plan logs and review files were not re-read, because #157 changed no source and round 1's checks of them stand.
