# TRIM-004 — Trimming harness pass/fail semantics + CI gate

**Plan #:** 004
**Status:** Draft
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** TBD at draft
**Related Todo:** [../todo.md](../todo.md)

## Scope

Give the publish-trimmed harness enforceable pass/fail semantics and make CI run it. Today `RemoteFactory.TrimmingTests` sits outside `Neatoo.RemoteFactory.sln`, is never published or executed by `.github/workflows/build.yml`, and its `Program.cs` / smoke tests print `FAILED` lines to the console but always exit 0 — so the trimmed-repro acceptance signals that TRIM-001/002/003 depend on would verify nothing in CI. Convert failure paths to a non-zero process exit (aggregated across all checks so one failure doesn't mask others), add a CI step that publishes the trimmed exe (Release; RID matching the ubuntu runner) and runs it, and settle solution membership at draft time (the csproj is deliberately single-TFM net9.0 with `PublishTrimmed=true`, which the solution-wide multi-targeted build/test may not tolerate as-is). Does NOT add new trimming test cases — those land with TRIM-001/002/003.
