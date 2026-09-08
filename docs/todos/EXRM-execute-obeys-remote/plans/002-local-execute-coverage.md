# Local Execute Proven on Both Shapes, Plus Auth

**Plan #:** 002
**Date:** 2026-09-08
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-2, AC-4
**Status:** Draft
**Last Updated:** 2026-09-08
**Plan-review opt-in:** — (declared at Step 2)
**Code-review opt-in:** — (declared at Step 2)
**Branch:** exrm-002-local-execute-coverage — cut from the arc at Step 2
**PR:** —

---

## Scope

Prove the new contract from the client side for both shapes: the combination generator gains a Local execution mode for Execute and the Execute behavior tests gain local legs with a wire-crossing negative control; class-level bare `[Execute]` gets client round-trip tests beside the existing `[Remote, Execute]` ones; the interplay of `[AspAuthorize]` and `[AuthorizeFactory]` with a bare class-level `[Execute]` is pinned; and the Design projects gain bare `[Execute]` samples with tests, as this repo's requirements verification. Does not change the generator beyond what EXRM-001 landed, and does not touch documentation prose or the trimming harness.

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

Recon (2026-09-08): the pattern for "ran locally on the client" is `Showcase/ShowcaseReadTests.cs`: a bare `[Create]` taking `[Service] IServerOnlyService` whose body is `Assert.Fail()`, asserted to throw `InvalidOperationException` from the client (negative form), and a bare `[Create]` taking `[Service] IService` resolved on the client through `RegisterMatchingName` (positive form). `ClassExecuteRoundTripTests` and `Design.Tests/ClassFactoryExecuteTests` are all `[Remote, Execute]` and become AC-2 evidence. Static factories carry no authorization path in the builder today, so AC-4 is class-level; `[AspAuthorize]` has no Execute precedent in any test (`AspAuthorizeTestObj.cs` covers Create/Insert only) and the AspNetCore test library is consumed by no test project.
