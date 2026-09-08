# New Contract in Design, Docs, Skill, Diagnostics

**Plan #:** 004
**Date:** 2026-09-08
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-5, AC-6
**Status:** Draft
**Last Updated:** 2026-09-08
**Plan-review opt-in:** — (declared at Step 2; name `business-requirements-reviewer`, documented rules change)
**Code-review opt-in:** — (declared at Step 2)
**Branch:** exrm-004-contract-in-design-docs-skill — cut from the arc at Step 2
**PR:** —

---

## Scope

Replace the always-remote contract everywhere it is stated with the `[Remote]`-follows rule: the Design source-of-truth comments and the CLAUDE-DESIGN rules table, the published attributes and trimming pages through their reference-app snippets, the skill references, the attribute XML docs, and the NF0102 description that justifies the `Task` rule by remoteness; and re-examine the NF0105 static-factory exemption, keeping it with a stated reason or narrowing it. Does not cut release notes (EXRM-005) or add behavior.

---

## Intent

_(Step 2)_

## Framework & Architectural Alignment

_(Step 2)_

## Constraints & Invariants

_(Step 2)_

## Steps

_(Step 2)_

## Acceptance

_(Step 2)_

## Current State (Pre-Flight)

_(Step 3)_

## Punchlist

-

## Test Evidence

_(after implementation)_

## Gate Record

_(Step 5)_

## Plan Amendments

_(append-only)_

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
- `src/Design/CLAUDE-DESIGN.md:40, 364-366, 374, 944-948, 1074` (the `:374` cite `AllPatterns.cs:340-347` is stale, now `383-404`); `ClassFactoryWithExecute.cs:16-37`; `src/Design/README.md:73-88`; `Design.Client.Blazor/Pages/Home.razor:91-97`.
- `src/Generator/DiagnosticDescriptors.cs:55, 59` (NF0105 message, for AC-6).
- Reference app: `EmployeeManagement.Application/Samples/Attributes/ExecuteSamples.cs:13-20` is the only live bare `[Execute]`, and it takes a server-only `[Service]`, so it becomes a client-side failure under the new rule; every other Execute snippet already carries `[Remote]`.
- Leave alone: interface-factory "always remote" text and `AuthorizeFactoryOperation.Execute` scope text (different concept).

NF0105 rationale on record (`docs/plans/completed/remote-requires-internal.md:232`): class-factory `[Execute]` must be `public static`, so `[Execute]` is excluded from NF0105; the static-class shape is separately exempt because `BuildStaticFactory` runs no NF0105 check.
