# Static-Factory Delegates Obey [Remote]; Weld Removed

**Plan #:** 001
**Date:** 2026-09-08
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-2
**Status:** Draft
**Last Updated:** 2026-09-08
**Plan-review opt-in:** — (declared at Step 2; generated public API shape changes, so expect Yes)
**Code-review opt-in:** — (declared at Step 2; behavior-changing, so expect Yes)
**Branch:** exrm-001-static-delegates-obey-remote — cut from the arc at Step 2
**PR:** —

---

## Scope

Remove the generator's forced-remote treatment of `[Execute]` and give the static-factory delegate path a real remote flag: the delegate model carries it, the builder derives it from `[Remote]` the way class methods already do, the registrar renders a remote registration only for remote delegates, and a bare delegate gets one unguarded local registration in every factory mode while remote delegates keep the guarded v1.8.1 shape. The server-side delegate handler refuses a request naming a bare delegate, so `[Remote]` is not decorative at the wire either (user decision 2026-09-08, folded in here as a Should bullet under AC-1). Preserve the intent of existing tests that assumed always-remote: the combination generator emits `[Remote]` for its Remote-mode Execute targets, and any bare test target that is invoked from a client scope gains `[Remote]` only with the user's recorded sign-off. Does not add local-mode coverage (EXRM-002), change class-level rendering (none is needed: its public method, local guard, and registrar filters already branch on the flag), touch the trimming harness, or edit documentation prose.

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

Recon (2026-09-08): the weld is `FactoryGenerator.Types.cs:582` and `:629` (the latter is dead for Execute; record primary ctors are Create-only). `ExecuteDelegateModel` has no remote flag; `BuildExecuteDelegate` reads neither `[Remote]` nor auth; `StaticFactoryRenderer.RenderFactoryServiceRegistrar` emits both registrations per factory mode with no per-delegate branch, and the local registration is always guarded. `FactoryEntryCall.RunAsync` tolerates a missing scheduler, so an unguarded local delegate is safe on a Remote-mode client. `CombinationDimensions.json` allows only Remote for Execute and `GenerateExecuteClassBody` never writes `[Remote]`, so the five `Comb_Execute_*_Remote` targets go vacuous the moment the weld is removed unless the generator emits `[Remote]` for them.
