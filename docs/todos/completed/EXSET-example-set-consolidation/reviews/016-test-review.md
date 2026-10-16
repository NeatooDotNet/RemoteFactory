# EXSET-016 test review — 2026-10-15

## Round 1 — CONCERNS (1 veto-tier, 0 callouts)

**Reviewer:** test-reviewer, against `0428c12`, diffed from `30e1034`. Logs green:
- main build (`016-build-main.log`): 0 errors, 2 warnings. Both are the known OrderEntry WASM `NativeFileReference` notice (the map says "0 warnings"; see Theoretical).
- Design build (`016-build-design.log`) and reference app (`016-build-refapp.log`): 0 errors, 0 warnings.
- tests (`016-test-main.log`, `016-test-design.log`): unit 855 (852 at 011, plus the 3 `PreservePropertiesEmissionTests`), integration 657 + 7 skips, Design 175 + 1 skip, each on both TFMs. No failures. The skips are the known three `ShowcasePerformanceTests`, two `RelayTimingTests`, two `FactoryReferenceIdentityTests` (#133) and Design's #104.
- mdsnippets (`016-mdsnippets.log`): no error, exception or missing-snippet line; ends `Finished`.
- trimming gate (`016-ci-{publish,verify,run}.log`): win-x64 self-contained, no `-p` overrides. `016-ci-verify.log` is byte-identical to `016-trimfix-full-verify.log`; the run ends "All checks passed", with both `SaveMeta round trip` lines PASSED.

The logs are 16:58–16:59, after the fix commit (16:52) and before `0428c12` (17:01), which carries the csproj change; the CI-shaped publish therefore ran on the working tree holding it.

Notes per bullet:
- **1. C# blocks are embeds or WRONG (Must): not honest as cited.** See finding 1. The tagged fences check out: `trimming.md:369` (`trimming-event-nested-record`), `appendix/serialization.md:34` (`service-injection-constructor`) and `:81` (`getting-started-client-program`) sit inside snippet markers; `trimming.md:403` is labelled `// WRONG`. The other untagged fences are a diagram (`trimming.md:48`), exception text and a flow diagram (`record-reference-handling.md:9`, `:84`), and a name-to-type map (`appendix/serialization.md:95`, plan-review callout 7).
- **2. One `TrimMode` (Must): honest.** `trimming.md:153` says `TrimMode` needs no setting, under Blazor's `partial` with the domain `IsTrimmable`; checklist item 1 (`:231`) names `IsTrimmable`; `:276` agrees. That matches the experiment table and `016-trim-*-verify.log`.
- **3. `IsNew`/`IsDeleted` across a trimmed round trip, both formats (Must): honest.**
  - `SaveMetaRoundTripSmokeTest.Run` is called from `Program.cs` and adds a failed check on false. Per format, it builds its own `NeatooFactory.Remote` container with `NeatooSerializationOptions { Format }`, deserializes `{"IsNew":false,"IsDeleted":true}` into `TrimCtorSaveTarget`, re-serializes it and asserts the text holds `"IsNew":false` and `"IsDeleted":true`.
  - The values are the opposite of the defaults, so a trimmed setter (default left) and a trimmed getter (property dropped) each change the text. No getter or setter is called in harness code.
  - `TrimCtorSaveTarget`'s only constructor takes `IClientTallyPort`, so it gets no ordinal converter; `PreservePropertiesEmissionTests.ConstructorTakesAService_PreservePropertiesEmitted` pins that this shape gets `PreserveProperties<T>()` and no `CreateOrdinalConverter()`.
  - Red before the fix: `016-savemeta-unfixed-run.log` shows `got {}` in both formats. Green after, at `full` (`016-trimfix-full-run.log`), at `partial` + `IsTrimmable` (`016-trimfix-partial-trimmable-run.log`) and CI-shaped (`016-ci-run.log`).
  - Tier: publish-trimmed harness, as declared.
- **4. No LazyLoad in docs, README, skill (Should, Required): honest.** The grep over `docs`, `README.md` and `skills`, excluding release notes, todos, plans and `VisionAlignment.md`, leaves only `SKILL.md:4`'s "deferred loading", "lazy loading" triggers, kept by plan-review callout 9's triage.
- **5. Meta (Must): honest,** as the log list above.

**Sacred-test check: clean.** In `30e1034..HEAD -- src/Tests src/Design/Design.Tests`, the only removed line is `<TrimMode>full</TrimMode>` in the harness csproj, replaced by `partial` + `IsTrimmable=true` (plan-review callout 5, Plan Amendments). No assertion, case or marker was removed, and the absence gate's output under the new setting is identical to its output under `full`. `Program.cs` gained one check; the other changes are new files. `src/Design` changed only by one region pair in `FactoryEventHandlerPattern.cs`.

**Plan-introduced reachable paths:** the generator's new `else` branch in `ClassFactoryRenderer` (reached by every class factory with no ordinal converter) is pinned by `PreservePropertiesEmissionTests` (constructor-with-service and non-`partial` inputs, plus the converter-path negative) and end to end by the harness. `DtoConstructorRegistry.PreserveProperties<T>` is called only from that emission.

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|
| 1 | Veto | Bullet 1's grep (`'```csharp\|```cs$'`) checks tagged fences only. `trimming.md:439-442` is an untagged fence holding C#: the "Expected output" of the ILSpy recipe, `public bool IsNew { get; set; }` / `public bool IsDeleted { get; set; }`. It is neither an embed nor WRONG, and the plan's own Constraint says "a block that shows generated output becomes prose". The bullet is unproven as cited. Shape: explicit-skip grep widened to untagged fences, with that block either turned into prose or recorded in the map as tool output, not a C# block. | AC-3; Acceptance bullet 1 | Amend: the ILSpy "Expected output" fence is prose; Test Evidence row 1 now lists every fence, tagged or not, with its tag; round 2 requested |
| 2 | Tech-debt | CI no longer publishes the harness at `TrimMode=full`. A regression that shows only under `full` is now backed only by the gitignored `016-trimfix-full-*` logs. | — | Punch → Follow-on: a second CI publish of the harness at `TrimMode=full` (Could) |

Could not verify:
- `016-ci-publish.log` does not print the ILLink trim mode, so that the CI-shaped run used `partial` is inferred from the csproj in the working tree at 16:59, not read from the log. Both modes pass, so bullet 5 does not depend on it.

Theoretical (not triaged):
- The nested `[Factory]` shape that the page and `DtoConstructorRegistry`'s remarks name has no emission test. It takes the same `else` branch as the two shapes tested.
- `016-savemeta-unfixed-run.log` comes from an earlier version of the check: `016-savemeta-untrimmed-run.log`, from the same minute, fails Named on indented JSON, which the current whitespace compaction fixes. The current code was never run red. `{}` fails either version, so the check is still sensitive.
- Test Evidence 3 says "outbound and re-serialized inbound". The check is a single deserialize-then-serialize pass. That is the right shape, because building the object in code would root the setters, but the wording claims two directions.
- Test Evidence 5 says the builds had "0 warnings". The main build has the 2 known OrderEntry WASM warnings.

Read report:
- Read beyond the brief: `.github/workflows/build.yml` trimming step, to confirm CI passes no `TrimMode` override; `docs/trimming.md` and both appendix pages, to re-run bullets 1, 2 and 4; `015-test-review.md`, for the review format; and `011-factory-events.md`'s evidence, for the prior unit count (852).
- Named but unused: `016-trim-*.log` beyond the verify comparisons; `016-build.log`, `016-test.log`, `016-design-*.log`, `016-unit-*.log`, `016-refapp-build.log` (the logs the map does not cite).

## Round 2 — APPROVED (0 veto-tier, 0 callouts)

**Reviewer:** test-reviewer, against `17d0135`, diffed from `0428c12`. `git diff --stat 0428c12..17d0135 -- src` is empty: the fix commit touches only `docs/trimming.md`, both appendix pages, the plan, the todo and the review files. No source, test or snippet marker changed, so the round-1 logs still stand. Re-grepped: `016-test-main.log` unit 855 and integration 657 + 7 skipped on both TFMs, `016-test-design.log` 175 + 1 skipped on both, 0 failures; the three builds show `0 Error(s)`; `016-ci-run.log` ends "All checks passed".

**Veto 1: closed.** Every opening fence on the three pages, tagged or not:
- `docs/trimming.md`: untagged `:48` (publish tree diagram); `xml` `:66`, `:86`, `:100`, `:116`, `:132`, `:160`, `:177`, `:190`, `:339` (csproj and linker XML); `bash` `:209`, `:223`, `:433` (shell commands); `cs` `:369`, inside `trimming-event-nested-record` markers (`:367`/`:381`); `csharp` `:403`, whose first line is `// WRONG - IL2091 ...`. The ILSpy "Expected output" fence is gone and its content is now a prose sentence (`:437`).
- `docs/appendix/serialization.md`: `json` `:19` (wire shape); `cs` `:34` and `:81`, inside `service-injection-constructor` and `getting-started-client-program` markers; untagged `:95` (name-to-type map, not C#).
- `docs/appendix/record-reference-handling.md`: untagged `:9` (exception text) and `:84` (reference-flow diagram). Neither holds C#.

Each C# block is an embed or labelled WRONG, and no untagged fence holds C#.

**Test Evidence map: honest.**
- Row 1 now covers every fence and names the same C# and untagged fences listed above. It doesn't list the `xml`/`bash`/`json` fences one by one, but none of them is C#.
- Row 3's wording ("deserialized and serialized back") matches the single-pass check, which closes round-1 Theoretical 3.
- Row 5 names the main build's 2 OrderEntry warnings, which closes round-1 Theoretical 4.
- `trimming.md:425` now says the harness pins only the no-converter path and that the converter path keeps its accessors by its own reads and writes. That matches what `SaveMetaRoundTripSmokeTest` exercises.

**Sacred-test check: clean.** Nothing under `src` changed in this round.

| # | Tier | Finding | Affects | Triage |
|---|---|---|---|---|

No new findings.

Could not verify: none.

Theoretical (not triaged):
- `record-reference-handling.md:3`, rewritten in this round, reads "four serialization paths handles". That is a grammar slip in prose, not a test matter.

Read report:
- Read beyond the brief: none. The fence content on the three pages was read in full to place each fence.
- Named but unused: `016-code-review.md`. Its changes were prose only and none touched a fence.
