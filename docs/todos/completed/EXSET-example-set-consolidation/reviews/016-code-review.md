## Code Review — EXSET-016 — 2026-10-15

**Verdict: APPROVED**
**Logs:** build PASSED. `016-build-main.log`: 0 errors, 2 warnings, both the SDK's wasm-workload `NativeFileReference` notice on `OrderEntry.BlazorClient`. `016-build-design.log` and `016-build-refapp.log`: 0 errors, 0 warnings. Tests: 0 failed. `016-test-main.log`: unit 855 and integration 657 passed with 7 skipped, on each TFM. `016-test-design.log`: 175 passed, 1 skipped, on each TFM. Trimming gate: `016-ci-publish.log` built; `016-ci-verify.log` says "Trimming verification passed."; `016-ci-run.log` says "All checks passed." `016-mdsnippets.log` finished clean.

Reviewed: `30e1034..0428c12` (3 commits).

### Direction & shape

The fix sits at the right seam.
- **Where it is emitted.** `ClassFactoryRenderer.RenderFactoryServiceRegistrar` (`src/Generator/Renderer/ClassFactoryRenderer.cs:1626-1634`) emits `PreserveProperties<T>()` as the `else` branch of `model.RegisterOrdinalConverter`. That is exactly the condition `FactoryModelBuilder.cs:308-313` computes.
- **What it roots.** The new API (`src/RemoteFactory/Internal/DtoConstructorRegistry.cs:59-62`) is a no-op whose type parameter carries `[DynamicallyAccessedMembers(PublicProperties | PublicFields)]`. That keeps accessors and fields, and no methods, so it cannot root `[Remote]` bodies by itself. The live gate measures this:
  - `TrimTestEntity` is not `partial`, so it now takes this branch (its generated registrar, line 190).
  - Its `ClassSyncBody_MARKER` and `ClassAsyncBody_MARKER` are still reported `ok` (absent) in `016-ci-verify.log:58-59`.
- **Compiled shapes.** The nested shape compiles: `NestedClassFactoryTests`' types emit it in UnitTests' generated output. The non-`partial` and constructor-service shapes are pinned by `PreservePropertiesEmissionTests` (3 tests; the unit count went from 852 to 855).
- **The harness check is honest.**
  - `SaveMetaRoundTripSmokeTest` deserializes `{"IsNew":false,"IsDeleted":true}` and compares only the re-serialized text. Its values are the opposite of the defaults, so losing either accessor changes the output.
  - `TrimCtorSaveTarget` is read nowhere else in the harness.
  - It is red before the fix (`016-savemeta-unfixed-run.log`, `{}` in both formats) and green after (`016-ci-run.log`).
- **The other changes.**
  - The csproj moves to `partial` + `IsTrimmable`, as the page prescribes.
  - The LazyLoad deletions leave no dangling link, region or type: a grep for `lazyload.md`, `LazyLoadSamples` and its types finds only untracked generated files.
  - `SKILL.md`'s defer-loading row now carries R-K's answer.
- **Embeds.** Every C# fence on the three pages is an mdsnippets embed or the one `// WRONG` IL2091 block (`trimming.md:403`). The new region `trimming-event-nested-record` is unique, and is pinned by `FactoryEventHandlerTests`.
- **Factual claims checked against code and found true:**
  - the converter order (`NeatooJsonSerializer.cs:80-104`; LazyLoad's converter, which sits between 2 and 3, is left out under R-K);
  - private setters left out of ordinal membership (`FactoryGenerator.Types.cs:439-445, 506-518`);
  - `RecordBypassConverterFactory`'s rule, which is based only on constructors (`RecordBypassConverterFactory.cs:36-58`);
  - Design's `IProductCatalog` return types, `IOrder.Lines`, and `Order` being on the converter path;
  - Neatoo's two converter types existing in the Neatoo repo.

### Veto-tier

None.

### Callouts

All five affect AC-4 (Must).

1. **Ctor-injected `[Factory]` classes do not get reference tracking in the Named format. Both appendix pages say all `[Factory]` classes do.** High confidence.
   - **Where:** `docs/appendix/serialization.md:54`; `docs/appendix/record-reference-handling.md:57` (the Path 3 "Types" line).
   - **Affects:** AC-4 (T-27; the plan review's callout 4).
   - **Reachable by:** a reader whose aggregate takes a constructor `[Service]` and follows `docs/serialization.md:81`'s workaround for #133 ("use the Named format"). Shared instances still arrive as copies, and a back reference throws "possible object cycle".
   - **Diagnosis:** `RecordBypassConverterFactory.CanConvert` claims any type with no public parameterless constructor (`RecordBypassConverterFactory.cs:36-58`). A ctor-injected class is such a type, so it takes Path 4 and is written with `ReferenceHandler = null` in both formats. The record-handling page says so itself at `:114` and `:128`, so it now contradicts its own Path 3 line. The harness shows the effect: the Named output of the ctor-injected `TrimCtorSaveTarget` carries no `$id` (`016-savemeta-untrimmed-run.log:13-16`), where a Path 3 object writes `"$id":"1"` (the plan review's `PlainMeta` probe).
   - One option: qualify both sentences to "with a parameterless constructor", as the `:123` table row already does.

2. **The appendix's opening still says "three-path".** High confidence.
   - **Where:** `docs/appendix/record-reference-handling.md:3`.
   - **Affects:** AC-4 (T-27).
   - **Reachable by:** anyone reading the page. Its first paragraph promises three paths, and the section below describes four (`:33`).
   - **Diagnosis:** the line was not touched. The ledger records T-27 as "fixed: the appendix describes four paths".

3. **The trimming page says the harness pins both paths. It pins only the path without a converter.** High confidence.
   - **Where:** `docs/trimming.md:425`.
   - **Affects:** AC-4 (I-01, U-12).
   - **Reachable by:** a reader of "What Keeps the Flags". The sentence follows two bullets that name the converter path and the `PreserveProperties` path, so "both paths" reads as both of them.
   - **Diagnosis:** `SaveMetaRoundTripSmokeTest` round-trips only `TrimCtorSaveTarget`, which has no converter. No harness code serializes `TrimSaveTarget` or reads its flags (grep over `src/Tests/RemoteFactory.TrimmingTests/*.cs`). The converter path for `IsNew`/`IsDeleted` was measured only in the plan review's scratch probe.
   - The plan's own Test Evidence states the narrower claim correctly. One option: "pins the path without a converter, in both formats".

4. **The page still credits DI registration with preserving `[Factory]` types. This fix exists because that is not enough.** Medium confidence.
   - **Where:** `docs/trimming.md:300` ("those are already preserved via DI registration") and `:310` ("entities are preserved via DI registration").
   - **Affects:** AC-4 (the plan's Intent: the page states what a publish keeps).
   - **Reachable by:** a reader reasoning about why an entity's properties survive. Before this plan, the same belief hid the `{}` defect.
   - **Diagnosis:** `AddTransient<T>` keeps constructors only (the plan review's reality check, `016-plan-review.md:36`). A `[Factory]` class's properties are kept by its ordinal converter or by the new `PreserveProperties<T>()`, as `:422-423` of the same page now says. Lines 300 and 310 predate this plan, but they sit on the page it rewrites, and the fix contradicts them.

5. **Test Evidence row 5 says the builds had "0 warnings". `016-build-main.log` has 2.** High confidence, low consequence.
   - **Where:** `plans/016-trimming-appendix-lazyload.md:125`.
   - **Affects:** AC-4 (the plan's Acceptance bullet 5 evidence).
   - **Reachable by:** the close-out audit, which reads Test Evidence as the record.
   - **Diagnosis:** both warnings are the wasm workload's `NativeFileReference` notice on `OrderEntry.BlazorClient` (net9.0 and net10.0). They come from the environment, not from 016's code. The row is still inaccurate as written.

### Could not verify

- **The ILSpy recipe** (`trimming.md:434-442`): its `obj/Release/net10.0/linked/` path and its expected `{ get; set; }` rendering. No Blazor WASM client was published or decompiled.
- **The nested shape on a trimmed client.** It is compile-verified (UnitTests' generated `PreserveProperties<DeepNestedFactory>()` and others), but no trimmed run or emission unit test covers it.
- **No 016 test-review file** exists in `reviews/`, and the brief named none, so I checked no test-review closures.

### Theoretical (not triaged)

- **Getter bodies now stay.** `PreserveProperties<T>()` keeps every public property getter body. A public computed property that reaches a server-only type would root that type on the client. No such case exists in the gate's targets, and the gate is green.
- **CI no longer publishes with `full`.** The page's "`TrimMode=full` also removes it" rests on the gitignored `016-trim-full-*` and `016-trimfix-full-*` logs, so a regression that shows only under `full` would go unseen.
- **Publish warnings for annotated members.** A `[Factory]` member marked `[RequiresUnreferencedCode]` would now draw an ILLink reflection-access warning at publish, through the DAM on `PreserveProperties<T>`.
- **The stated converter conditions are not the whole list.** `trimming.md:422` and `record-reference-handling.md:37` leave out "has a serialized property", "not a collection" and "a record has a primary constructor" (`FactoryModelBuilder.cs:308-313`). I found no `IFactorySaveMeta` shape in the gap; collections are covered by the Dismissed `OrderLineList` entry.
- **A superseded log shows a failure.** `016-savemeta-untrimmed-run.log` has a Named FAILED from an earlier revision of the smoke test: the indented output, before the whitespace compaction at `SaveMetaRoundTripSmokeTest.cs:57`. It is not cited as evidence, and there is no untrimmed control run of the final test code.

### Read report

- **Read beyond the brief:**
  - Generator: `FactoryModelBuilder.cs:280-340`, `FactoryGenerator.Types.cs:430-520`, `ClassFactoryRenderer.cs:1500-1640`.
  - Library: `NeatooJsonSerializer.cs:55-105`, `RecordBypassConverterFactory.cs:1-80`.
  - Harness: generated harness registrars, `verify-trimmed.sh` (grep), `TrimTestEntity.cs` (grep).
  - UnitTests generated output for the nested shapes.
  - Design: `AllPatterns.cs` (`IProductCatalog`), `Order.cs`.
  - Docs: `docs/serialization.md:47-81`.
  - Neatoo repo: a grep for the two converter types.
- **Named but unused:** `016-trim-partial-plain-*` beyond its listing; `docs/VisionAlignment.md` (the rulings are quoted in the plan and the plan review, and no finding depended on their exact text).

### Findings

| # | Finding | Tier | Confidence | Affects | Reachable by | Triage |
|---|---|---|---|---|---|---|
| 1 | `appendix/serialization.md:54` and `record-reference-handling.md:57` say `[Factory]` classes are reference-tracked in Named. Ctor-injected ones take Path 4 (`RecordBypassConverterFactory`) and are not, in either format | Callout | High | AC-4 (T-27) | A ctor-injected aggregate that follows `serialization.md:81`'s "use Named" workaround for #133 | Amend: both pages now say a `[Factory]` class with a parameterless constructor; `appendix/serialization.md` §3 states a ctor-injected class takes the record path and is untracked in either format |
| 2 | `record-reference-handling.md:3` still says "three-path"; the page now has four paths | Callout | High | AC-4 (T-27) | Anyone reading the page's first paragraph | Amend: line 3 says four serialization paths |
| 3 | `trimming.md:425` says the harness "pins both paths"; it pins only the path without a converter (`TrimCtorSaveTarget`) | Callout | High | AC-4 (I-01, U-12) | A reader of "What Keeps the Flags" | Amend: the page says the harness pins the second path; the converter's own reads and writes keep the first |
| 4 | `trimming.md:300, 310` say `[Factory]` types are "preserved via DI registration"; DI keeps constructors only, and properties are kept by the converter or `PreserveProperties` | Callout | Medium | AC-4 | A reader reasoning about why entity properties survive trimming | Amend: both sites now say DI keeps constructors and the converter or `PreserveProperties<T>()` keeps properties |
| 5 | Test Evidence row 5 says "0 warnings"; `016-build-main.log` has 2 (wasm-workload `NativeFileReference`, OrderEntry) | Callout | High | AC-4 (Acceptance bullet 5 evidence) | The close-out audit reading Test Evidence | Amend: Test Evidence row 5 names the 2 known `OrderEntry` wasm warnings |
