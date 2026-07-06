# TRIM-006 — Incremental-generator caching regression test

**Plan #:** 006
**Status:** Draft
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** TBD at draft
**Related Todo:** [../todo.md](../todo.md)

## Scope

Add a driver-level regression test for the generator's incremental caching, closing the project-wide hole plan-review B1 (TRIM-001) exposed: the pipeline cache boundary lives on the transform-output records (`TypeInfo` / `TypeFactoryMethodInfo` / `MethodInfo`), and a non-`EquatableArray` field added there silently breaks caching for every consumer with **no failing test** — `DiagnosticTestHelper.RunGenerator` runs the generator exactly once and never asserts cached steps. The test should run the driver twice with `GeneratorDriverOptions`/`WithTrackingIncrementalGeneratorSteps`, apply an unrelated edit between runs, and assert the factory-generation steps report `Cached`/`Unchanged` — guarding all current and future transform-output fields (including TRIM-001's `DtoPreserveTypes`). Surfaced by the TRIM-001 test-review gate as pre-existing tech debt (2026-07-06). Does NOT change generator behavior.
