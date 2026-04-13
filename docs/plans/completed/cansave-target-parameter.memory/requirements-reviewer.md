# Requirements Reviewer — CanSave Target Parameter

Last updated: 2026-04-06
Current step: Post-Implementation Verification (Mode 2) — Completed

## Key Context

- This plan reverses a documented design decision (target params suppress CanSave generation)
- The reversal is justified: CanSave is unique among Can* methods because the caller has the entity in hand
- CanInsert/CanUpdate/CanDelete remain suppressed (correct — called before operation, entity not yet available at call site)
- The `IFactorySave<T>` interface change is technically breaking but acceptable (same rationale as prior add-cansave-to-ifactorysave todo)
- No Design Debt table conflict — suppression was never listed as a deliberate deferral

## Mistakes to Avoid

- Do not confuse type-matched parameters (Guid in CanFetch) with target parameters (IEntity in CanWrite). They use different flags (`IsTarget`) in the generator.
- The plan's analogy "same pattern as CanFetch calling multiple auth methods" is slightly imprecise — CanFetch works because Guid is NOT a target param. But the underlying principle (Can* calls all matching auth methods) is the same.

## User Corrections

None yet.

## Pre-Design Review

**Verdict:** APPROVED (with recommendations)
**Date:** 2026-04-06

(See todo file Requirements Review section for full pre-design findings.)

## Requirements Verification

**Verdict:** REQUIREMENTS SATISFIED
**Date:** 2026-04-06

### Compliance Table

| # | Requirement | Source | Status | Notes |
|---|------------|--------|--------|-------|
| 1 | IFactorySave<T> has both CanSave overloads | Plan Rule 6-7, IFactorySave.cs | Satisfied | Interface has `CanSave(CancellationToken)` and `CanSave(T target, CancellationToken)` — verified at src/RemoteFactory/IFactorySave.cs lines 29, 40 |
| 2 | Generator produces two CanSave overloads for class factories with target-param auth | Plan Rule 1-2, FactoryModelBuilder.cs | Satisfied | AddCanMethods (line 813-851) creates parameterless CanSave (non-target auth only) and target CanSave (all auth). AssignUniqueNames updated to handle Can* overloading (signature-based dedup, line 896-936) |
| 3 | CanSave(target) calls ALL Write-scoped auth methods | Plan Rule 3 | Satisfied | targetCanSave built with full authMethods list (line 841-847). Integration test CanSave_WithTarget_ReturnsFalse_WhenStatusLocked validates target auth is called. Design test ParamAuth_CanSaveTarget_ReturnsFalse_WhenNonTargetAuthFails validates non-target auth is also called |
| 4 | CanSave() parameterless calls only non-target auth | Plan Rule 4 | Satisfied | parameterlessCanSave built with nonTargetAuthMethods (line 820-833). Design test ParamAuth_CanSaveParameterless_ReturnsTrue passes even though target auth would fail on "Locked" entity (entity not passed) |
| 5 | CanSave() parameterless returns Authorized(true) when no non-target auth exists | Plan Rule 5 | Satisfied | Integration test CanSave_Parameterless_ReturnsTrue (AuthTargetParamTests) validates this — AuthWithTargetParam has no non-target Write method, so parameterless CanSave returns true |
| 6 | CanInsert/CanUpdate/CanDelete remain suppressed | Plan (unchanged) | Satisfied | Integration test CanInsert_CanUpdate_CanDelete_NotGenerated_OnInterface (line 344-353) explicitly verifies via reflection. Generator code at line 853: `continue` exits the loop for non-Save methods with target auth |
| 7 | AuthorizedOrder behavior unchanged | Plan Rule 8 | Satisfied | AuthorizationTests.cs unchanged. Renderer renders `Authorized(true)` for target overload when no target auth exists (line 1268-1274). All 68 Design tests pass |
| 8 | IFactorySave<T> explicit interface implementations always rendered | Pre-design recommendation #2 | Satisfied | RenderCanSaveExplicitInterfaceMethod (line 1225-1290) handles all four cases: parameterless with/without auth, target with/without auth. Null cases return Authorized(true) |
| 9 | IsTarget flag preserved for filtering | Pre-design recommendation #3 | Satisfied | nonTargetAuthMethods filtered by `!am.Parameters.Any(p => p.IsTarget)` at line 821. Target CanSave uses full authMethods list |
| 10 | Design project updated as source of truth | CLAUDE.md requirement | Satisfied | ParamAuthOrderAuth.cs fully updated with new comments (lines 28-39, 96-130). ParamAuthOrder.cs updated (lines 16-29). 8 new Design tests in ParamAuthorizationTests.cs covering all plan scenarios |
| 11 | Client-server round-trip tested | Plan Scenario 9 | Satisfied | ParamAuth_CanSaveTarget_ThroughClientServer_WhenStatusLocked test (line 522-542) validates CanSave(target) across serialization boundary |

### Unintended Side Effects

1. **Interface factory pattern not updated**: `AddCanMethodsForInterface` (line 716-791) still fully suppresses Can* generation when target auth is present (line 732-734: `continue`). This means interface factories with target-param auth won't get CanSave overloads. This is a gap in plan Step 4 ("Modify AddCanMethodsForInterface — Same logic as step 3") but does NOT affect the IFactorySave<T> contract (interface factories don't implement IFactorySave<T>). No test targets exist for this scenario.

2. **CLAUDE-DESIGN.md Quick Decisions Table is stale**: Lines 161-162 still say target parameters "suppresses CanSave/CanInsert/CanUpdate/CanDelete generation" and "CanSave needs the entity but runs before Save; auth is checked inside Save() instead". This contradicts the new behavior where CanSave IS generated. The Design project source code (ParamAuthOrderAuth.cs, ParamAuthOrder.cs) is correctly updated, so the authoritative source is correct, but the quick reference is misleading.

3. **docs/authorization.md is stale**: Lines 294-300 "CanXxx Suppression" section still says CanSave is suppressed when target params exist. This contradicts the implementation.

4. **docs/interfaces-reference.md is stale (pre-existing)**: Lines 343-347 show IFactorySave<T> without any CanSave methods. This was flagged in pre-design review as pre-existing but is now further out of date.

### Issues Found

**No blocking violations.** The implementation correctly satisfies all behavioral requirements established by the plan. The stale documentation issues (CLAUDE-DESIGN.md, docs/authorization.md, docs/interfaces-reference.md) are documentation debt, not requirements violations — the Design project code (which is the single source of truth per CLAUDE.md) is correctly updated.

The interface factory gap (AddCanMethodsForInterface not updated) is a partial implementation of the plan but does not violate any existing requirement since no interface factory with target-param auth exists in the codebase.
