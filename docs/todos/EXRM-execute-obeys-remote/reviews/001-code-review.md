# EXRM-001 — Code Review (per-plan, opt-in)

**Date:** 2026-09-08
**Reviewer:** code-reviewer (budget: tight)
**Object:** the working-tree diff against arc `EXRM` plus five new files; `reviews/001-build.log`, `reviews/001-test.log`

## Verdict: CLEAN

**Direction and shape.** The flag is derived once in the static delegate builder with the same three-way rule as the read builder, carried on the delegate model, and consumed only in the static registrar renderer. Class rendering is untouched, as scoped.

**Guarded branch is byte-identical to v1.8.1.** With `guarded` the indent is 20 spaces and every emitted line reproduces the old literal exactly; the service-assignment join separator equals the old 24-space one. Confirmed in the generated `LocalExecuteTargetFactory.g.cs`.

**Eliding the mode blocks is safe.** A factory with only local-only delegates leaves the mode parameter unused; C# does not warn on unused parameters, consistent with the green build under `TreatWarningsAsErrors`.

**Wire refusal is correctly placed and not vacuous.** `ServiceAssemblies` pre-seeds its cache with every type of every registered assembly by full name, and requests carry the delegate's full name, so the lookup genuinely resolves the local-only type and the check fires before deserialization. The thrown message is character-for-character the serializer's own unknown-delegate text, so the refusal does not leak. No new reflection: the same lookup already ran later in the same request. Registry state is a concurrent dictionary with idempotent add.

**Sacred test intent preserved and strengthened.** `ExecuteBehaviorTests` gained ten assertions and lost none; every pre-existing client-scope Execute test targets a `*Remote` delegate, so nothing went vacuous when the weld dropped.

**Veto-tier:** none.

## Callouts and dispositions

| # | Finding | Affects | Priority | Disposition |
|---|---|---|---|---|
| 1 | Local-scope zero-count assertions cannot fail: only the client collection registers the wire stand-in, so a Logical-scope counter is structurally zero (six assertions) | AC-2 | Must | **Punched inline** — comments added in `ExecuteBehaviorTests` (local region) and `LocalExecuteTests.RemoteExecute_LogicalScope_RunsLocally_NoWire` stating that the proof is resolution-and-execution in a container with no wire implementation; the counter documents the expectation |
| 2 | `LocalOnlyDelegateRegistry.Register` is public and unguarded; any referencing assembly could mark a `[Remote]` delegate local-only | AC-2 | Must | **Accepted** — generated code in consumer assemblies forces public visibility; same precedent as `DtoConstructorRegistry`, cited in the plan |

**Theoretical (not triaged):** the registry is process-global and never cleared (already Dismissed on the todo from the test review); a local-only delegate whose registrar never ran would miss the refusal but is then absent from server DI too.
