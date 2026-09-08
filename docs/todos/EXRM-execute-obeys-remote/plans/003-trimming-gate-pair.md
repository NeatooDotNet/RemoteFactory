# Trimming Gate Measures Absent and Present Pair

**Plan #:** 003
**Date:** 2026-09-08
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3
**Status:** Draft
**Last Updated:** 2026-09-08
**Plan-review opt-in:** — (declared at Step 2)
**Code-review opt-in:** — (declared at Step 2)
**Branch:** exrm-003-trimming-gate-pair — cut from the arc at Step 2
**PR:** —

---

## Scope

Extend the trimmed-client gate so it measures the pair rather than infers it: a bare `[Execute]` target on each shape whose body marker is asserted present on the trimmed client, beside the existing `[Remote, Execute]` targets asserted absent, with delegate and factory resolution checks so a dead feature cannot pass as a clean trim. Does not change generator emission or documentation.

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

Recon (2026-09-08): `verify-trimmed.sh` defines only `check_absent`; the one body-literal PRESENT control is the "Trimming verification app completed" string, searched by the shared `present()` helper in both encodings. TRIM-008 once asserted markers present as a deliberate tripwire, so the shape has precedent. Every harness port is registered inside `if (NeatooRuntime.IsServerRuntime)` in `Program.cs`, so a bare leg needs a service registered outside that guard or none at all. `ValidateOnBuild = true` in the harness: confirm an unguarded delegate registration does not trip it on a client publish.

Pointer from the EXRM-002 plan review (B5, 2026-09-08): the last sentence of the note at `src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:368-371` ("The guard is what makes the body trimmable, not the attribute") is the finding this plan measures. EXRM-002 rewrites only the decorative claim beside it; 003 rewrites the trimming sentence once the pair is measured.
