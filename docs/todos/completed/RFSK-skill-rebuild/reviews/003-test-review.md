## Test Review — RFSK-003 — [2026-10-16] — Round 1

**Verdict: APPROVED**
**Logs:** build PASSED, 0 errors (`reviews/003-build.log`; 2 warnings, both the WASM `NativeFileReference` notice from `OrderEntry.BlazorClient`), Design build PASSED, 0 warnings, 0 errors (`reviews/003-design-build.log`); tests: unit 871 passed, 0 failed; integration 664 passed, 0 failed, 5 skipped (`reviews/003-test.log`); Design 177 passed, 0 failed, 1 skipped (`reviews/003-design-test.log`). Both TFMs. No `[FAIL]` lines in either test log.

### Veto-tier
None.

### Callouts (≤ 5 Optional)
None. Every bullet is Required, and the plan adds no code path: the source diff is three `#region`/`#endregion` pairs and three comment edits.

### Tech-debt (untiered, ≤ 5, one line each)
- `Design.Client.Blazor` is compiled but no test runs it, so every region in it (`getting-started-client-program`, `skill-blazor-*`, and now `skill-command-*`, `skill-client-services`) is pinned by the same calls on `DesignClientServerContainers`, not by running the region's own lines.

### Could not verify
None.

### Theoretical (not triaged)
- `skill-client-services` shows `AddScoped<ITextScorer, TextScorer>()`, but the test containers register `ITextScorer` through `RegisterMatchingName`. You could delete the Program.cs line and every test would still pass. The behaviour the region shows (the client resolves `ITextScorer` for a bare `[Execute]`) is still asserted.

### Answers to the brief's questions
- **The `[integration]` bullet is honestly pinned.** `StaticFactoryTests.Execute_SendNotification_ReturnsResult` resolves `ExampleCommands.SendNotification` from the client scope, awaits it with two strings, and asserts `true`. That is the inject-then-await the two Home.razor regions show. `LocalExecuteTests.Execute_StaticFactory_WithoutRemote_RunsOnClient` and `Execute_ClassFactory_WithoutRemote_RunsOnClient` run a bare `[Execute]` on the client with a stand-in wire that throws on any call. They assert the scored results, which proves the client container resolved `ITextScorer`. The mirroring arrangement is the one `getting-started-client-program`, `skill-blazor-inject` and `skill-blazor-usage` (EXSET-015) already use. `DesignClientServerContainers` is what `design-snippets.md` means by "the real client/server containers". This is not a tier mismatch.
- **The fence counts are right.** I counted the fences and the embed markers myself: class-factory 11/11 embeds, interface-factory 6/6, static-factory 7/7 (5 cs + 2 razor), service-injection 7 = 6 embeds + 1 `// WRONG` (line 77), setup 9/9 (5 cs + 4 razor), anti-patterns 15 = 15 `// WRONG`. Every fence outside an embed opens with `// WRONG`. No other fence language appears (no XML package block is left).
- **The `[explicit-skip]` reasons are honest.** A grep finds none of the retired names (`IEmployeeRepository`, `IMyRepository`, `IDeptRepo`, `IAuthorizedRepository`, `SkillConsultation`, `SkillEmployee*`, `IConsultationFactory`, `EmployeeRepository`, `LazyLoad`). It finds no "N+1". The method-injection heading is "`[Service]` parameters: the tier that runs the method". Every remaining "repository" line is the corrected framing or the anti-pattern #1 WRONG block. Every remaining "server-only" line describes services or the `internal`-without-`[Remote]` row, not the injection title. The `SKILL.md`-link and ruling/generator bullets are properly left to code review.
- **No tests changed, and the three edits are comment-only.** `git diff RFSK...HEAD` touches no file under `Design.Tests` or `src/Tests`. The three edits drop `(R-C)` in `OrderLine.cs`, `(VisionAlignment R-I)` in `CustomerDirectory.cs`, and `(R-C)` in a `Home.razor` line comment. Both embeds of `collection-factory-basic` (`docs/factory-operations.md`, `advanced-patterns.md`) carry only the matching one-line change.

### Read report
- Read beyond the brief: `DesignClientServerContainers.ConfigureContainer`, to see how the test client registers `ITextScorer` (the Theoretical line); `git log -S skill-blazor-inject`, to date the precedent (EXSET-015, 60cd0d1).
- Named but unused: `reviews/001-test-review.md`, `reviews/002-test-review.md` (the precedent was confirmed in source instead); the Discovery Log (its one RFSK-001 entry bears on nothing here).
