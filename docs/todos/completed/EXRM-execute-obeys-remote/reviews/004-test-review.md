# EXRM-004 — Test Review (Step 5 gate)

**Plan:** [`plans/004-contract-in-design-docs-skill.md`](../plans/004-contract-in-design-docs-skill.md)
**Reviewer:** `test-reviewer` · **Round:** 1 · **Date:** 2026-09-08 · **Budget:** tight
**Object:** the plan's Acceptance (8 bullets) and Test Evidence map at implementation commit `472bd8b` (plan commit `1eb7f76`)
**Verdict:** **CLEAN**

The reviewer's report arrived in two parts (one truncation, then a capped resend); reproduced in full below.

---

## Logs

`004-build-main.log` / `004-build-design.log` / `004-build-refapp.log` — all "Build succeeded", 0 Error(s), no `error CS`. Tests: UnitTests **779/779** per TFM (net9.0 + net10.0), IntegrationTests **626 passed + 5 skipped = 631** per TFM, Design.Tests **103/103** per TFM, reference app **42/42** per TFM. 0 failed anywhere. Counts move by exactly +2 unit / +1 Design against the plan's baselines. The three new tests are listed by name as Passed on both TFMs in `004-test-main-new.log` and `004-test-design-new.log`.

## Veto-tier

None. `git diff 1eb7f76..472bd8b -- src/Tests src/Design/Design.Tests` is +146/−3 across four files; all three deleted lines are comments (`StaticFactoryTests.cs` two `///` lines replaced by a longer note, `NF0105Tests.cs` one `//` line). No assertion, case, or expected value was removed or changed. `NF0102Tests.cs` is absent from the diff, and the only production-source change under `src/Generator`/`src/RemoteFactory` outside comments and XML docs is the one NF0102 `description:` string — verified by diffing with comment lines filtered out.

## Must-cover (plan-related)

None. All four Must bullets (1, 2, 3, 8) are pinned at their declared tier by evidence the reviewer independently reproduced.

## Should-cover

None. Bullets 5, 6 (Should, `[explicit-skip]` doc prose) have live cites the reviewer spot-read; bullet 7 (Should, `[integration]`) is pinned by a real server-container test; bullet 4 (Could, `[unit]`) is pinned by a test over a shape that provably generates. No plan-introduced code path has a live caller and no test — the only new executable code is `ArchiveOnServer` (tested) and the reference-app sample `TallyCommand._Total` (no caller; a docs snippet).

## Tech-debt (untiered)

1. `plans/004-…md` Test Evidence bullet 1 broke its 8 sweep hits into categories that sum to 9 ("four … qualified sentences" is three); the hit list itself is complete and correct.
2. `NF0105_RemotePublicStaticCreate_NoDiagnostic` and its precedent both filter to one diagnostic id, so neither would notice a generator crash (Roslyn reports that as `CS8785`); adding `Assert.Empty(errors)` or a `GeneratedTrees` presence check would close it for both at once.

## Answers

**Q1 — `ClassExecute_InternalStaticWithoutRemote_IsGuardedAndNotPromoted` (`InternalVisibilityTests.cs:300-362`).** Not vacuous, and the fragile-looking `IndexOf("}", interfaceStart)` is self-guarding here. `RenderInterfaceMethodSignature` (`ClassFactoryRenderer.cs:136-159`) emits `{prefix}{returnType} {Name}({parameters});` with no brace in any signature, so the first `}` after the header is the interface's closing brace. More importantly, every `DoesNotContain` is paired with a `Contains` over the *same* block for the *same* member — `Contains("Task<ExecVis> RunRemote(")` at `:342` passing proves the block reaches the last of the four members, so a truncation would surface as a failure, not a silent pass.

**Q1b — the `WrapperBlock` anchor.** `"public Task<ExecVis> Local{name}("` is exactly what `RenderLocalMethodOpening` (`ClassFactoryRenderer.cs:377-407`) emits: `sb.AppendLine($"        {modifiers} {returnType} Local{uniqueName}({parameters})")` with `modifiers: "public"` from `RenderClassExecuteLocalMethod` (`:833-844`) and `returnType` from `GetReturnType(method, includeTask: true, includeAuth: true)` — `Task<ExecVis>` for these no-auth methods. It cannot collide with the public method, which is `public virtual` (`RenderClassExecutePublicMethod`, `:807`). The `Assert.True(start >= 0, …)` at `:353` is reachable and is what fires if the anchor ever stops matching — it is inside a local function called unconditionally three times, so a renderer change to the modifier or return type turns this test red rather than green-and-empty. The block runs from the wrapper declaration to the `Local{name}Core(` call, which is exactly where `RenderLocalMethodOpening` (`:394-399`) emits the `IsServerRuntime` throw, so the boundaries are right.

**Q2.** `[Remote][Create] public static` is a live compiled shape (`src/Tests/RemoteFactory.IntegrationTests/Generated/CombinationTestGenerator/…/CreateCombinations.g.cs:40-58`), so `NF0105Tests.cs:186-208` passes because the exemption applies, not because generation failed.

**Q3.** `ClassFactoryExecuteTests.cs:196-211` resolves the factory from the **server** scope and asserts `"Archived: ledger 2026"`; `[integration]` is correct (DI containers rule out `[unit]`, and the round-trip half is unreachable by construction — no remote delegate), and the client-throw explanation is honest since `IsServerRuntime` is process-global, with the guard pinned at unit tier and the comment at `ClassFactoryWithExecute.cs:155-181` naming guard and non-promotion.

**Q4.** The reviewer's re-run returned exactly 8 hits, all accounted for: `docs/attributes-reference.md:205`, `skills/RemoteFactory/references/static-factory.md:87`, `skills/RemoteFactory/references/trimming.md:167` (qualified), `docs/trimming.md:39`, `src/Design/Design.Tests/FactoryTests/InterfaceFactoryTests.cs:25` and `src/Generator/Builder/FactoryModelBuilder.cs:434` (NF0106), `src/Tests/RemoteFactory.TrimmingTests/Program.cs:8`, `AssemblyAttributeEmissionTests.cs:311`; `docs/attributes-reference.md:203-207` and `skills/…/trimming.md:163-167` state the rule as `AllPatterns.cs:370-373` does.

**Q5.** The embed at `docs/attributes-reference.md:189-201` and `skills/RemoteFactory/references/static-factory.md:93-105` is byte-identical to `MinimalAttributesSamples.cs:99-107`; `ExecuteSamples.cs:18,27` are both `[Remote, Execute]`, and `MinimalAttributesSamples.cs:103` is the only bare `[Execute]` in the reference app.

**Q6.** `git diff 1eb7f76..472bd8b -- src/Tests src/Design/Design.Tests` is +146/−3 with all three deletions comment lines; no assertion changed.

## Read report

Beyond the brief: `ClassFactoryRenderer.cs:111-159` (Q1 brace risk); `CreateCombinations.g.cs:40-58` (Q2); `plan-template.md:69-73` (Q3 tiers); `docs/release-notes/v1.7.0.md` (sweep probe). Named but unused: `004-baseline-refapp-build.log`, `004-baseline-refapp-test.log`.

---

## Orchestrator disposition (2026-09-08)

- **Tech-debt 1 — fixed** in the plan's Test Evidence ("four" → "three"); the hit list was already complete.
- **Tech-debt 2 — worked inline, not carried.** Both `NF0105_RemotePublicStaticExecute_NoDiagnostic` (in scope: Step 8 re-examines NF0105, and this plan already edited its comment) and the new `NF0105_RemotePublicStaticCreate_NoDiagnostic` now run the generator directly and assert, beside "no NF0105", that `GeneratedTrees` is non-empty and the output compilation has no error-severity diagnostics — the repo's own crash-proofing idiom (`AssemblyAttributeEmissionTests`), chosen over a CS8785 check because `FactoryRenderer` swallows render exceptions into a `/* Error: */` comment, which surfaces as compile errors rather than a throw. This strengthens the existing test; nothing it asserted was removed. The new assertions went red on their first run — `CS0246: CancellationToken could not be found` in the generated file — which is a fixture requirement rather than a generator fault: generated factories name `Task` and `CancellationToken` unqualified and rely on the consumer's `ImplicitUsings`, which every consumer in this repo enables, and `AssemblyAttributeEmissionTests` already documents that an output-compilation check needs `using System.Threading;` in the fixture source. Both fixtures now carry it, with a comment saying why it is load-bearing; the reliance itself is recorded as a Dismissed row on the todo (pre-existing, serves no criterion). Main solution rebuilt and UnitTests re-run afterwards — see the plan's Gate Record for the r2 logs. The wider pattern (every other `_NoDiagnostic` test in `Diagnostics/` shares the weakness) serves no EXRM criterion and is not this plan's; noted here for the sweep or a successor, not punched.
- **No round 2 needed:** round 1 CLEAN with no must-cover or should-cover; the plan goes Done on this record.
