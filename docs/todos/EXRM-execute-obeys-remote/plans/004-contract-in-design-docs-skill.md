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

Recon (2026-09-08): known old-contract sites are `docs/attributes-reference.md:189`, `skills/RemoteFactory/references/static-factory.md:87`, `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:368-371`, `src/Generator/DiagnosticDescriptors.cs:35` (NF0102), and `docs/plans/combination-testing-generator.md:168`. The reference app already carries a bare `TransferEmployee` beside a `[Remote]` variant in `EmployeeManagement.Application/Samples/Attributes/ExecuteSamples.cs`. NF0105 never fires for any Execute because every Execute is static (`Types.cs:581`, `FactoryModelBuilder.cs:195`), pinned by `NF0105Tests.cs:161`; class-level Execute must be public static, so the exemption almost certainly stays with a reason. The docs-recon inventory is attached at Step 2.
