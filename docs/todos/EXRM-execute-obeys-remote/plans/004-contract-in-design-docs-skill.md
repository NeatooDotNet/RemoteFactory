# New Contract in Design, Docs, Skill, Diagnostics

**Plan #:** 004
**Date:** 2026-09-08
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-5, AC-6
**Status:** In Progress
**Last Updated:** 2026-09-08
**Plan-review opt-in:** Yes — the plan rewrites a documented contract at every home it has (Design, CLAUDE-DESIGN, published docs, skill, XML docs), which is exactly the contradiction surface `business-requirements-reviewer` holds a veto over. `business-requirements-reviewer` only — user decision 2026-09-08
**Code-review opt-in:** No — user decision 2026-09-08 (orchestrator proposed No): prose and comment edits, one descriptor description, one test; the only candidate behavior change is an NF0105 narrowing under AC-6, which is a stop-and-ask and would flip this to Yes by amendment
**Branch:** exrm-004-contract-in-design-docs-skill — cut from the arc at Step 2
**PR:** —

---

## Scope

Replace the always-remote contract everywhere it is still stated with the `[Remote]`-follows rule, and state that rule once at a home every other page points at: the published attributes and trimming pages and the operations, decision, authorization, and client-server pages that lean on them; the skill's references and Quick Decisions; the attribute XML docs; the NF0102 description that justifies the `Task` rule by remoteness; and the Design side that EXRM-002/003 did not reach — `CLAUDE-DESIGN.md`'s rules table, the Design README and Blazor home page, and one Design test comment. The published bare-`[Execute]` example becomes a compiled reference-app snippet, authored fresh in the Domain samples with a dependency the client can resolve; the reference app's existing bare `[Execute]`, which was written as a server-side command, takes `[Remote]` so it stops being a client-side failure. The plan pins the visibility rule for a bare `internal static [Execute]` on a class factory, and re-examines the NF0105 static exemption — including its actual breadth, which is every static method rather than `[Execute]` — recording the decision where the exemption lives. Does not cut release notes or bump the version (EXRM-005), add behavior, edit historical plan archives under `docs/plans/`, or touch the interface-factory always-remote text, which is a different rule (NF0106).

---

## Intent

- AC-5 asks that the Design projects, the published docs, and the skill state the new contract with the decorative claim gone. Three plans have made `[Execute]` obey `[Remote]` and measured it; the docs a user reads still say the opposite in four places, and the attribute's own IntelliSense says it in a fifth.
- A reader should find the rule stated once, in the attributes reference, and every other page consistent with it — not four paraphrases that drift. The trimming page's guard table, which today has no `[Execute]` rows, is where a WASM author looks first for "will this body ship", so it carries the rule by shape.
- The only bare `[Execute]` in the reference app takes a server-only repository — it was written as a server command before the contract changed, so under the new rule it is the wrong shape for a published example (it is not even registered on the client, which loads the Domain assembly only), and `[Remote]` is what it always meant. The docs' bare example must be one that actually runs on the client, with a service the client resolves, and it must compile, which means a new reference-app snippet rather than hand-written markdown.
- The two diagnostics that mention remoteness say the wrong thing for the wrong reason: NF0102 justifies `Task<T>` by remote execution, which is no longer why the rule exists, and NF0105's static exemption has never had a stated reason. AC-6 asks that the exemption be examined rather than inherited.
- Two rows pulled down from the todo's Punchlist ride here because this plan edits the files they name anyway: the NF0102 wording and the reference-app sample; the third is the visibility pin EXRM-002's plan review moved here.

---

## Framework & Architectural Alignment

- The Design projects are the requirements source of truth (repo `CLAUDE.md`); the published docs and the skill restate what Design demonstrates. EXRM-002 and EXRM-003 already put the rule into `AllPatterns.cs` and `ClassFactoryWithExecute.cs` with the measured trimming result, so this plan writes the *derived* homes to agree with those two files, and verifies rather than re-edits them.
- The skill is self-contained: `SKILL.md` and `references/*.md` only, no `.cs` or `docs/*.md` references (repo `CLAUDE.md`). Compiled samples reach it and the docs through MarkdownSnippets from the reference app; anti-pattern and partial samples are hand-written by design.
- MarkdownSnippets pipeline: `skill-*` regions for skill-only samples, unprefixed regions shared with docs; `mdsnippets` from the repo root is the check that the tree is current.
- Diagnostic conventions: descriptors in `DiagnosticDescriptors.cs` with `description` as the long-form rationale; diagnostic tests under `RemoteFactory.UnitTests/Diagnostics` using `DiagnosticTestHelper`; generated-code visibility pinned in `FactoryGenerator/Visibility/InternalVisibilityTests.cs`, whose `AllInternal_GeneratedInterface_IsInternal` shape is the precedent for the pin.
- The rule the docs must state, as the arc has established it: `[Remote]` decides where an operation runs on every operation; a bare `[Execute]` — `private static` on a static factory, `public static` on a class factory — gets one unguarded local registration in every mode and resolves `[Service]` parameters from the container that runs the call; `[Remote, Execute]` keeps the remote delegate, the endpoint, and the guarded local half; `[AspAuthorize]` forces remote on both shapes and, on class factories, is enforced, while `[AuthorizeFactory]` runs alongside (EXRM-002; static factories render no enforcement, #91); and `internal static [Execute]` without `[Remote]` on a class factory is server-only like every other internal operation — guarded, trimmable, not promoted — because the renderer's server-only test is `internal` or `[Remote]` (plan-review M2).
- DDD vocabulary unexplained; comments say what the code does, not what the pattern means.

---

## Constraints & Invariants

- No behavior change. The generator, the library, and every test that exists today stay as they are; the only permitted source edits under `src/Generator` and `src/RemoteFactory` are descriptor text, comments, and XML docs — plus the AC-6 outcome if, and only if, the user approves a narrowing.
- Narrowing NF0105 is a stop-and-ask, not a keyboard decision: it widens a compile error's reach, so it is a public-API change EXRM-005 would have to carry. The default outcome is "kept, with the reason stated at its home", and the re-examination must address the exemption's real breadth (`IsStaticFactory` is `methodSymbol.IsStatic`, so a static `[Create]`/`[Fetch]` on a class factory is exempt too) rather than only the `[Execute]` case.
- The NF0102 rule is unchanged: a bare `[Execute]` returning anything but `Task`/`Task<T>` still fails it. Only the justification changes.
- Every doc sentence that states the contract agrees with `AllPatterns.cs` and `ClassFactoryWithExecute.cs` as they stand after EXRM-003. Where a page needs a fact those two files do not carry, the plan cites the harness or an EXRM-002 test rather than reasoning it.
- The skill stays self-contained after the edit: no reference to a repo path leaves the skill directory.
- After `mdsnippets` runs, the tree is clean — every embedded block matches its region, and no snippet is orphaned by the reference-app change.
- The new reference-app bare sample takes a `[Service]` the client resolves — the client registers the Domain assembly's `IX`/`X` pairs by name, so a pure-computation Domain service such as `ISalaryCalculator` qualifies (plan-review S1) — and does something a client would plausibly do locally, not a `[Remote]` body with the attribute dropped. It lives in one unprefixed region, shared by docs and skill per the region convention (M4). The existing `TransferEmployeeCommand` bare method gains `[Remote]` and nothing else. Everything compiles and the reference-app tests stay green.
- The Design sample of the server-only class-level shape is `internal static` without `[Remote]`, sits beside `RunCommand` and `ScoreLocally` so the three placements read as one table, and its comment names the guard; the docs' one sentence on that shape cites it (S3).
- Interface factories stay described as always-remote (NF0106) — that text is correct and is not this plan's.
- Historical plan archives under `docs/plans/` are not edited; one is recorded as dismissed on the todo.
- Both solutions and the reference app build and test green. Test counts move by exactly the tests this plan adds.

---

## Steps

1. State the contract once, in the attributes reference's `[Execute]` and `[Remote]` entries, as the rule every other page points at: a bare `[Execute]` (`private static` on a static factory, `public static` on a class factory) is local-only with local service resolution, `[Remote]` is the server entry point with the guarded local half, `internal static` without `[Remote]` on a class factory is server-only like any internal operation, and the static NF0105 exemption is stated with its reason. Retire the decorative paragraph and the unconditional guard claim.
2. Carry the rule into the trimming page: `[Execute]` rows in the guard table by shape and attribute — bare static, `[Remote]` static, bare `public static` class-level, `[Remote]` class-level, and `internal static` class-level as its own guarded row — with no sync/async axis (class-level `[Execute]` is emitted async unconditionally); and the static-factories bullet rewritten so guarded means `[Remote]`.
3. Carry it through the operations, decision, authorization, and client-server pages: the two-context intro says where each shape runs and why, and its "must be `public static`" sentences become "`public static` to run where the factory resolves, `internal static` for server-only"; the decision guide's `[Execute]` branch names local versus remote as a choice; the authorization page states the bare-`[Execute]` rule as "`[AspAuthorize]` forces remote on both shapes; on class factories the policy is also enforced, and `[AuthorizeFactory]` runs alongside", citing the generator's own note that static factories render no enforcement (#91), and placed so it cannot be read onto the static-factory snippet above it; and the architecture page's entry-point list stops implying every `[Execute]` crosses.
4. Author a genuine client-runnable bare `[Execute]` in the reference app's Domain samples, beside a `[Remote]` one so the page can show the pair, taking a Domain service the client resolves by name so the snippet shows local service resolution too; one unprefixed region the attributes and trimming pages embed — the published bare example is then compiled and mirrors the Design pair; and put `[Remote]` on the existing server-command bare sample, which is what it always meant (Punchlist row: reference app).
5. Bring the skill to the same rule: replace the decorative section, split the shape table's static **and** class-level rows by attribute, update the static-factory and class-factory references and the Quick Decisions table — whose class-Execute answers say "`public static` to run where the factory resolves, `internal static` for server-only" rather than visibility alone — and embed the new bare snippet through the same shared region the docs use, adding no prose reference outside the skill directory.
6. Attribute XML docs: the `[Execute]` remarks state the rule and drop the decorative paragraph; `[Remote]`, which has no XML doc today, gains one saying it decides where a method runs on every operation, requires `internal` on class factories, and is exempt on static methods.
7. NF0102's description justifies `Task`/`Task<T>` by the uniform API rather than remote execution (Punchlist row: NF0102), and the model builder's exemption comment names the reason NF0105 does not apply.
8. Re-examine the NF0105 static exemption against the new contract — what visibility means on each shape, and the exemption's real breadth — and record the outcome at its home in the generator, in the attributes reference, and in a diagnostic test that pins the decision; a narrowing is a stop-and-ask before any edit.
9. Pin the server-only shape — `internal static [Execute]` without `[Remote]` on a class factory: guarded, and not promoted on the factory interface — at unit tier beside the existing visibility tests, with `[Remote] internal static` promoted as the contrast; add the shape to the Design class-Execute sample with a Design test so the docs' one sentence on it is Design-backed; and state it at the visibility rule's home in the docs (Punchlist row: visibility).
10. Close the Design side: `CLAUDE-DESIGN.md`'s rules table gains the `[Execute]`/`[Remote]` rule with a current `AllPatterns.cs` cite and its "`[Remote]` requires `internal` — yes, always" row gains the static carve-out, the Design README and the Blazor home page's static-factory blurb say where `[Execute]` runs, the Design static-factory test comment names the client/server split it demonstrates; then run `mdsnippets`, and build and test both solutions and the reference app to log files.

---

## Acceptance

- [ ] The attributes reference, the trimming page, and the skill state that `[Execute]` obeys `[Remote]` on both shapes, and no published doc, skill file, Design comment, or attribute XML doc still calls `[Remote]` decorative, says bare `[Execute]` bodies are guarded or trimmed, or justifies NF0102 by remote execution `[explicit-skip: doc prose; the retired claims are verified absent by grep, recorded in Test Evidence]` · Must
- [ ] The published bare-`[Execute]` example is a compiled reference-app snippet taking a `[Service]` the client resolves, shown beside a `[Remote]` one, embedded in the docs and the skill from one shared region, and an `mdsnippets` run leaves the tree unchanged; the reference app carries no bare `[Execute]` that takes a server-only service `[explicit-skip: snippet pipeline; the mdsnippets run and a clean diff are the check]` · Must
- [ ] A bare `internal static [Execute]` on a class factory is server-only: its generated local method carries the `IsServerRuntime` guard and it is not promoted on the factory interface, while `[Remote] internal static [Execute]` on the same shape is guarded and promoted to public `[unit]` · Must
- [ ] The NF0105 static exemption is decided — kept with its reason stated at its home and in the attributes reference, or narrowed with the user's approval — and a diagnostic test pins the decision either way, including its breadth `[unit]` · Could
- [ ] The authorization page states the bare-`[Execute]` rule — `[AspAuthorize]` forces remote on both shapes and is enforced on class factories; `[AuthorizeFactory]` runs alongside — scoped so it cannot be read onto the adjacent static-factory snippet, in agreement with EXRM-002's class-shape pins and the #91 dismissal `[explicit-skip: doc prose; the behavior is pinned by EXRM-002]` · Should
- [ ] `CLAUDE-DESIGN.md` carries the rule in its rules table with a current `AllPatterns.cs` cite, and the Design README, the Blazor home page, and the Design static-factory test comment agree with it `[explicit-skip: Design prose; the behavior is pinned by EXRM-002's Design tests]` · Should
- [ ] A Design sample of the server-only class-level shape — `internal static [Execute]` without `[Remote]` — resolves and runs through the server container, and its comment names the guard and the non-promotion `[integration]` · Should
- [ ] Both solutions and the reference app build and test green, NF0102's existing tests are untouched and green, and the skill's prose and cross-references point nowhere outside its own directory — MarkdownSnippets' generated `<sup>` attribution links excepted, as they already are `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

Walked 2026-09-08 on `exrm-004-contract-in-design-docs-skill` @ `1eb7f76`. No surprise shifts the plan; no amendment.

- **Old-contract sites, verbatim on the branch:** `docs/attributes-reference.md:187` (guard stated unconditionally) and `:189` (decorative paragraph); `docs/trimming.md:35`; `skills/RemoteFactory/references/trimming.md:136-141` (shape table — static row `:138` and class-level row `:141` both "Yes, from v1.7.0") and `:161-163` (decorative section); `skills/RemoteFactory/references/static-factory.md:87`; `src/RemoteFactory/FactoryAttributes.cs:88-112` (`ExecuteAttribute` XML doc: trimming paragraph `:93-99`, decorative paragraph `:101-108`), `RemoteAttribute` `:26-32` with no XML doc; `src/Generator/DiagnosticDescriptors.cs:35` (NF0102 description); `src/Generator/Builder/FactoryModelBuilder.cs:194` (comment).
- **Design side already right:** `AllPatterns.cs:361-381` (the `[Remote]` note with the measured pair), `:425-463` (`_ScoreText`, the bare static sample), `ClassFactoryWithExecute.cs:116-149` (`ScoreLocally`). `StaticFactoryTests.cs:34-37` says "Registers delegate in DI for both client and server" — true of `[Remote]`, but names no split.
- **Renderer facts the pin rests on:** `ClassFactoryRenderer.cs:111-136` `RenderFactoryInterface` — the interface is `internal` only when `model.AllMethodsInternal` (`ClassFactoryModel.cs:62`); otherwise `needsInternalPrefix = !AllMethodsInternal && method.IsInternal && !isPromotedByRemote` (`:124`) puts `internal ` on the member (`:143-144`). `RenderClassExecuteLocalMethod` `:833-844` calls `RenderLocalMethodOpening(..., needsAsync: true, isServerOnly: method.IsInternal || method.IsRemote)`; the guard is emitted in the non-async `Local{X}` wrapper (`:394-399`), the body in `Local{X}Core`. `RenderClassExecutePublicMethod` `:807-829` is always `public virtual`. **No test target anywhere carries `internal static [Execute]` on a class factory** — the pin compiles that shape for the first time.
- **NF0105:** `FactoryModelBuilder.cs:194-195` `if (method.IsRemote && !method.IsInternal && !method.IsStaticFactory)`; `IsStaticFactory = methodSymbol.IsStatic` (`FactoryGenerator.Types.cs:581`), so every static method is exempt. `NF0105Tests.cs:161` pins `[Remote, Execute] public static` on a class factory only. `DiagnosticDescriptors.cs:50-59`: message and description are class-factory statements ("must be internal to enable IL trimming"). Rationale on record `docs/plans/completed/remote-requires-internal.md:230-252` — reasoned for `[Execute]`, implemented for `static`. No published rule states the broader scope (plan-review Q4).
- **NF0102:** `DiagnosticDescriptors.cs:28-35`; the accepted set is `Task` or `Task<T>` and stays; only the description's "designed for remote execution" changes.
- **Reference app:** `MinimalAttributesSamples.cs:88-96` is region `attributes-execute` (`PromoteCommand`, `[Remote, Execute]`), `:98-109` `attributes-remote`; namespace `EmployeeManagement.Domain.Samples.Attributes.Minimal`. `ISalaryCalculator`/`SalaryCalculator` at `ServiceInjectionSamples.cs:139-150` (namespace `…Samples.Services`); client and server both call `RegisterMatchingName(typeof(Employee).Assembly)` (`Client.Blazor/Program.cs:26`, `Server.WebApi/Program.cs:15`), so the pair resolves on both tiers. `ExecuteSamples.cs` (Application assembly — never loaded by the client): bare `_TransferEmployee` `:13-20`, `[Remote]` sibling `:22-40`, no test references it. Embed sites: `attributes-execute` → `docs/attributes-reference.md:173` only; `skill-static-execute-commands` → `skills/…/static-factory.md:11`. `mdsnippets` 28.0.1 installed globally.
- **Docs homes:** attributes-reference `:167-190` ([Execute] entry), `:255-275` ([Remote] entry — "Without `[Remote]`, methods execute locally" is already the generic rule), `:410-420` inheritance table (the `[Remote] | Yes` row is dismissed). trimming `:17-40`: class table `:22-26`, static bullet `:35`. factory-operations `:300-305` two-context table ("Class factory | `public static`"), `:345`, `:438-444` ("Method is **`public static`**"). decision-guide `:98-112`. authorization `:100`; `:155-172` — the multi-policy snippet is a **static** `[Remote, Execute]` (`AuthorizationSamples.cs:368-375`). client-server-architecture `:38-42` ("Execute operations initiated by the UI"), `:65-72` visibility table — the docs' visibility-rule home, whose `internal (no Remote)` row is where the class-level `[Execute]` sentence belongs, mirrored in trimming's `:22-26` table.
- **Skill homes:** `SKILL.md:63-64` (NF0105 row), `:72-73`; `class-factory.md:215-227`, `:280-284`, `:396-399`; `anti-patterns.md:186-207` (§8, `Task` vs `Task<T>` — rule unchanged, samples stay), `:336-338`; `static-factory.md:5-9`, `:87`, `:119-121`. All hand-written except the two snippets.
- **Design docs:** `CLAUDE-DESIGN.md:40` (static row), `:366-368` (rules table; `:368` "Yes, always"), `:374` (void row, stale cite), `:942-950` (§3 static signatures), `:1074` (checklist). `src/Design/README.md:73-88`; `Home.razor:86-92`.
- **Design test infrastructure:** `Design.Domain.csproj:30` `InternalsVisibleTo Design.Tests`, so a Design test can call an `internal` interface member. `DesignClientServerContainers.cs:293-294` applies `RegisterMatchingName(typeof(ExampleClassFactory).Assembly)` to every container, so `IExampleService` resolves on the test client as well; `IsServerRuntime` is process-global, so the guard's client-side throw is not observable in-process — the `[unit]` generated-code pin is what proves the guard. `ClassFactoryExecuteTests.cs` (3 tests) is the class-Execute home; `LocalExecuteTests.cs` (3 tests, `NoWireDelegateRequest`) the bare-Execute home.
- **Baselines** (per TFM, net9.0 and net10.0): UnitTests 777; IntegrationTests 631 (626 + 5 skipped); Design.Tests 102; reference app 42 (`reviews/004-baseline-refapp-build.log` "Build succeeded", `-test.log` 0 failed). Expected after: UnitTests +2 (the visibility pin, the NF0105 breadth pin), Design.Tests +1, the rest flat.

---

## Punchlist

Step 2 triage (2026-09-08): three of the five open todo-level rows lie in this plan's path and are pulled down; the `FileVersion` and `CLAUDE.md <VersionPrefix>` rows are the release step's and stay for EXRM-005.

- [ ] NF0102 description justifies the `Task` rule by remoteness · `src/Generator/DiagnosticDescriptors.cs:35` · done when the text no longer says "designed for remote execution" · AC-5 · Must (from the todo)
- [ ] Bare `[Execute] internal static` on a class factory yields an internal factory interface, the same visibility rule every other internal operation follows · the visibility rule's home in `docs/attributes-reference.md` and the Design comments · done when stated in one sentence and pinned at `[unit]` tier · AC-5 · Must (from the todo; EXRM-002 plan-review B4/B3)
- [ ] Reference app's only bare `[Execute]` takes a server-only `[Service]`, the wrong shape under the new contract · `src/docs/reference-app/EmployeeManagement.Application/Samples/Attributes/ExecuteSamples.cs:13-20` · done when it carries `[Remote]` and the docs embed a new bare sample instead (user decision 2026-09-08) · AC-5 · Must (from the todo)

---

## Test Evidence

_(after implementation)_

---

## Gate Record

_(Step 5)_

---

## Plan Amendments

_(append-only)_

### 2026-09-08 — Plan-review callouts amended before implementation

- **Section affected:** Framework Alignment bullet 5; Constraints; Steps 1, 2, 3, 4, 5, 9, 10; Acceptance bullets 2, 3, 5, 7 and a new bullet 8; Intent; Punchlist row 3; Notes.
- **Original said:** the rule was stated as "bare `[Execute]` = one unguarded local registration in every mode" with no visibility qualifier; Step 1 carried an inherited-`[Remote]`-over-`[Execute]` sentence; Step 5 embedded the new snippet through a `skill-*` region; bullet 7 asserted no path reference outside the skill; the authorization sentence claimed "runs alongside" for both shapes; the Notes said the Blazor client registers no domain services; Step 9 stated the internal rule from generator behavior alone.
- **What changed:** the rule is stated for `public static` (class) / `private static` (static), and `internal static` without `[Remote]` on a class factory is its own server-only case — guarded and not promoted — pinned at `[unit]` together and given a Design sample with a Design test (M2, S3); the inherited-`[Remote]` sentence is dropped and the inheritance-table row dismissed on the todo (M1); one unprefixed shared region (M4); bullet 7 scoped to prose and cross-references (M3); the authorization sentence scoped to "forces remote on both; enforced on class factories" with the #91 cite (M5); the new sample takes a client-resolved Domain service (S1); `CLAUDE-DESIGN.md:368` gets the static carve-out (S2); the "must be `public static`" sentences and the skill's class-level trimming row are named in Steps 3 and 5 (S4, S5).
- **Why:** `reviews/004-plan-review.md`; user decision 2026-09-08 (accept all as proposed).
- **Discovery Log:** 2026-09-08 / EXRM-004

---

## Notes

Recon (2026-09-08): known old-contract sites are `docs/attributes-reference.md:189`, `skills/RemoteFactory/references/static-factory.md:87`, `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:368-371`, `src/Generator/DiagnosticDescriptors.cs:35` (NF0102), and `docs/plans/combination-testing-generator.md:168`. The reference app already carries a bare `TransferEmployee` beside a `[Remote]` variant in `EmployeeManagement.Application/Samples/Attributes/ExecuteSamples.cs`. NF0105 never fires for any Execute because every Execute is static (`Types.cs:581`, `FactoryModelBuilder.cs:195`), pinned by `NF0105Tests.cs:161`; class-level Execute must be public static, so the exemption almost certainly stays with a reason. **Docs-recon inventory (2026-09-08, main @ eb63a94).** None of the old-contract statements is MarkdownSnippets-embedded; all are edited in place.

Old-contract statements that must change:
- `docs/attributes-reference.md:189` ("[Remote] is decorative on [Execute]") and `:187` (local registration always guarded).
- `docs/trimming.md:35` ("[Execute] delegate registrations are guarded", unconditional).
- `skills/RemoteFactory/references/trimming.md:161-163` (whole "decorative" section) and `:136-141` (table row "Static factory ([Execute]) | Yes").
- `skills/RemoteFactory/references/static-factory.md:87` ("[Remote] is decorative here").
- `src/RemoteFactory/FactoryAttributes.cs:102-108` (IntelliSense: decorative) and `:95-99` (guard stated unconditionally); `RemoteAttribute` at `:26-32` has no XML doc at all.
- `src/Generator/DiagnosticDescriptors.cs:35` (NF0102 "designed for remote execution"); `src/Generator/Builder/FactoryModelBuilder.cs:194` (comment "static factories exempt").
- `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:368-371`; `src/Design/Design.Tests/.../StaticFactoryTests.cs:34-37` ("Registers delegate in DI for both client and server").
- `docs/plans/combination-testing-generator.md:168` ("(always remote)", historical; one-line note).

New-contract homes:
- `docs/attributes-reference.md:169` ([Execute] blurb) and `:258` ([Remote] blurb, already generic); `:417` inheritance table — `[Remote]` is Inherited: Yes while `[Execute]` is No, so an inherited `[Remote]` over a redeclared `[Execute]` is a combination worth one sentence.
- `docs/trimming.md:20-26` guard table needs `[Execute]` rows; `docs/factory-operations.md:300-305, 438-444` (Execute two-context table); `docs/decision-guide.md:98-112`; `docs/authorization.md:100, 161-166` (AspAuthorize + Execute rule home; snippet source `AuthorizationSamples.cs:368-375`); `docs/client-server-architecture.md:42, 63`.
- `skills/RemoteFactory/SKILL.md:64, 72-73` (Quick Decisions); `references/class-factory.md:215-227, 399-400`; `references/static-factory.md:5-9, 121`; `references/anti-patterns.md:186-207, 336-337`.
- `src/Design/CLAUDE-DESIGN.md:40, 364-366, 374, 944-948, 1074` (the `:374` cite `AllPatterns.cs:340-347` is stale; the void-return block now sits at ~`402-409`); `ClassFactoryWithExecute.cs:16-37`; `src/Design/README.md:73-88`; `Design.Client.Blazor/Pages/Home.razor:91-97`.
- `src/Generator/DiagnosticDescriptors.cs:55, 59` (NF0105 message, for AC-6).
- Reference app: `EmployeeManagement.Application/Samples/Attributes/ExecuteSamples.cs:13-20` is the only live bare `[Execute]`, and it takes a server-only `[Service]`, so it becomes a client-side failure under the new rule; every other Execute snippet already carries `[Remote]`.
- Leave alone: interface-factory "always remote" text and `AuthorizeFactoryOperation.Execute` scope text (different concept).

NF0105 rationale on record (`docs/plans/completed/remote-requires-internal.md:232`): class-factory `[Execute]` must be `public static`, so `[Execute]` is excluded from NF0105; the static-class shape is separately exempt because `BuildStaticFactory` runs no NF0105 check.

Pulled in from the EXRM-002 plan review (B4/B3, 2026-09-08): the visibility pin moves here with Punchlist row 3 — pin at `[unit]` tier that a bare `internal static [Execute]` on a class factory is not promoted to public (a single-method target renders the whole interface internal, `ClassFactoryModel.cs:62`; otherwise the member carries the `internal` prefix) while `[Remote] internal static [Execute]` is promoted. Inventory note: EXRM-002 rewrites the decorative claim at `AllPatterns.cs:368-371` and EXRM-003 its trimming sentence; 004 verifies that site rather than editing it.

**Step 2 re-grep (2026-09-08, arc @ e9d270e, after EXRM-001/002/003 merged).** The Design side is largely done: `AllPatterns.cs:370-380` and `ClassFactoryWithExecute.cs:116-141` already state the rule with the measured trimming result. Still carrying the old contract, verbatim: `docs/attributes-reference.md:187,189`; `docs/trimming.md:35`; `skills/RemoteFactory/references/trimming.md:136-141,161-163`; `skills/RemoteFactory/references/static-factory.md:87`; `src/RemoteFactory/FactoryAttributes.cs:95-108`; `src/Generator/DiagnosticDescriptors.cs:35`; `src/Generator/Builder/FactoryModelBuilder.cs:194`. `CLAUDE-DESIGN.md` has no row stating the rule at all (`:40` static-factory row, `:374` NF0102 row with the stale cite). Every `[Execute]` snippet in docs and skill carries `[Remote]`; there is no compiled bare example anywhere a user reads. The reference-app Blazor client registers the Domain assembly's `IX`/`X` pairs by name (`Client.Blazor/Program.cs:26`, `RegisterMatchingName`) — the Step 2 draft said it registered none, which plan-review S1 corrected — so a client-runnable bare sample takes a pure-computation Domain service such as `ISalaryCalculator` (`ServiceInjectionSamples.cs:139`). The client loads only `typeof(Employee).Assembly`, so an Application-assembly factory is never registered client-side at all. `ExecuteSamples.cs` is not a snippet source today (its header points at `MinimalAttributesSamples.cs`) and nothing in the reference-app tests references `TransferEmployeeCommand`. The NF0105 exemption is `IsStaticFactory = methodSymbol.IsStatic` (`FactoryGenerator.Types.cs:581`), i.e. every static method, which is the breadth Step 8 must address. `mdsnippets` 28.0.1 is installed globally.

**User decisions at Step 2 (2026-09-08):** priorities confirmed as drafted; plan review by `business-requirements-reviewer` only; code review No; the reference app's existing bare `[Execute]` takes `[Remote]` (it was a server command) and a new bare sample is authored in the Domain samples as the docs' snippet — the orchestrator had proposed converting the existing one.
