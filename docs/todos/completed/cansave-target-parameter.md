# Generate CanSave(T target) for Target-Parameterized Auth

**Status:** Complete
**Priority:** High
**Created:** 2026-04-06
**Last Updated:** 2026-04-06

---

## Problem

When an authorization method uses `AuthorizeFactoryOperation.Write` with a target type parameter (e.g., `bool CanWrite(IParamAuthOrder target)`), the generator suppresses CanSave generation entirely. The rationale was "Can* methods don't have access to the entity instance" — but for Save, the caller already has the entity in hand. This is inconsistent with how other parameterized auth methods work (e.g., `CanFetch(Guid orderId)` is generated and works fine).

## Solution

1. Always add `CanSave(T target, CancellationToken)` overload to `IFactorySave<T>` interface
2. Remove the target-parameter suppression in the generator for CanSave
3. `CanSave()` (parameterless) runs only non-target auth methods (role checks)
4. `CanSave(target)` runs ALL matching Write-scoped auth methods — both parameterless and target-parameterized, same pattern as CanFetch calling multiple auth methods

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-04-06
**Verdict:** APPROVED (with recommendations)

### Relevant Requirements Found

1. **Target parameter suppresses CanXxx generation** — `src/Design/CLAUDE-DESIGN.md` line 161-162, Quick Decisions Table: "Can auth methods receive the target entity? Yes -- on write operations (Insert/Update/Delete). Auth method `CanWrite(IEntity target)` inspects entity state; suppresses CanSave/CanInsert/CanUpdate/CanDelete generation." Also documented extensively in `ParamAuthOrderAuth.cs` lines 28-35, `ParamAuthOrder.cs` lines 16-26, and `docs/authorization.md` lines 294-300.

2. **Can* method guard derivation from auth methods** — `src/Design/CLAUDE-DESIGN.md` lines 523-554: Can* methods derive guard behavior from auth class methods, not factory methods. This pattern must be preserved for CanSave(target).

3. **CanSave aggregation** — `src/Design/CLAUDE-DESIGN.md` lines 43-50 (AuthorizedOrder.cs comments): CanSave aggregates auth methods from Insert, Update, Delete; distinct set. The plan's two-overload approach must correctly split the distinct set into target vs non-target auth methods.

4. **IFactorySave<T> interface contract** — `src/RemoteFactory/IFactorySave.cs`: Currently has `Save(T, CancellationToken)` and `CanSave(CancellationToken)`. Adding `CanSave(T, CancellationToken)` is a breaking change for manual implementors (noted in plan's risks section). The prior `add-cansave-to-ifactorysave` todo (`docs/todos/completed/add-cansave-to-ifactorysave.md`) acknowledged this same breaking change category and deemed it acceptable since implementations are generator-created.

5. **Type-matched vs target parameters** — `ParamAuthOrderAuth.cs`: CanFetchOrder(Guid) uses type-matched parameters (non-target). CanWrite(IParamAuthOrder) uses target parameters. The generator currently distinguishes these with `p.IsTarget`. The plan correctly treats only target params as the CanSave parameter.

6. **Existing CanFetch pattern with multiple auth methods** — `ParamAuthOrderAuth.cs` lines 62-63: CanFetch calls both CanRead() (Read scope, parameterless) AND CanFetchOrder(Guid) (Fetch scope, parameterized). This validates the plan's approach: CanSave(target) should call both non-target and target Write-scoped auth methods.

7. **Published documentation** — `docs/authorization.md` lines 294-300 explicitly documents the suppression behavior under "CanXxx Suppression" section. This will need updating when the plan is implemented.

8. **AddCanMethods suppression code** — `src/Generator/Builder/FactoryModelBuilder.cs` lines 805-812 (class factory) and lines 728-735 (interface factory): These are the two suppression points that will be modified.

### Gaps

1. **No existing pattern for Can* methods receiving target entity** — This is a genuinely new generator capability. There is no existing model for a Can* method that receives the factory's entity type as a parameter. The architect must establish how the generator passes the target through the Can* method signature and into the auth method call, and how this integrates with the existing `BuildCanMethod` helper.

2. **No precedent for overloaded Can* methods** — Currently, each factory operation produces at most one Can* method. The plan proposes two CanSave overloads (parameterless and target-parameterized). The architect must handle overload disambiguation in `AssignUniqueNames` (FactoryModelBuilder.cs line 853) and ensure both appear correctly on the generated factory interface.

3. **IFactorySave<T>.CanSave(T, CancellationToken) explicit interface implementation for the no-auth case** — When auth is not configured, the current generator renders `IFactorySave<T>.CanSave(CancellationToken)` returning `Authorized(true)`. With the new overload, it must also render `IFactorySave<T>.CanSave(T, CancellationToken)` returning `Authorized(true)`.

### Contradictions

**None that warrant VETO.** The plan deliberately reverses a documented design decision (target parameter suppresses CanSave generation), but this reversal is justified:

1. The original rationale ("Can* methods don't have access to the entity instance") is correct for CanInsert/CanUpdate/CanDelete (called before the operation) but incorrect for CanSave specifically — the caller has the entity in hand when deciding whether to save.

2. The plan preserves the suppression for CanInsert, CanUpdate, and CanDelete — only CanSave gains the target overload.

3. The plan maintains backward compatibility: parameterless CanSave() continues to work for non-target auth, and the existing AuthorizedOrder pattern is explicitly unaffected (plan Rule 8, test scenario 8).

4. This is NOT in the Design Debt table — it was never identified as a deliberately deferred feature requiring a "Reconsider When" condition.

### Recommendations for Architect

1. **Update all documentation that references the suppression behavior** — CLAUDE-DESIGN.md Quick Decisions Table (lines 161-162), ParamAuthOrderAuth.cs comments (lines 28-35, 96-102), ParamAuthOrder.cs comments (lines 16-26), and `docs/authorization.md` "CanXxx Suppression" section (lines 294-300). The suppression still applies to CanInsert/CanUpdate/CanDelete but no longer to CanSave.

2. **Handle the no-auth case for the new overload** — `RenderCanSaveExplicitInterfaceMethod` must render `IFactorySave<T>.CanSave(T, CancellationToken)` returning `Authorized(true)` even when no auth is configured, to satisfy the interface contract.

3. **Preserve the `p.IsTarget` distinction** — The plan's approach of splitting auth methods into "non-target" (for parameterless CanSave) and "all" (for CanSave(target)) is consistent with how the generator already handles type-matched parameters. Keep using the existing `IsTarget` flag for filtering.

4. **Verify CanInsert/CanUpdate/CanDelete remain suppressed** — The plan correctly scopes this change to CanSave only. Ensure the implementation does not accidentally un-suppress CanInsert/CanUpdate/CanDelete for target-param auth.

5. **Docs interface reference is already stale** — `docs/interfaces-reference.md` shows `IFactorySave<T>` without even the existing `CanSave(CancellationToken)` method. This is a pre-existing documentation gap; the plan should update it to show both overloads.

---

## Plans

- [CanSave Target Parameter Support Plan](../plans/cansave-target-parameter.md)

---

## Tasks

- [x] Requirements review (Step 2) — APPROVED
- [x] Architect validation (Step 3) — Approved with concerns (all addressed)
- [x] Implementation (Step 4) — Complete
- [x] Developer code review (Step 5) — Approved (pre-existing auth triplication noted)
- [x] Verification (Step 6) — VERIFIED + REQUIREMENTS SATISFIED
- [x] Documentation (Step 7) — Complete (CLAUDE-DESIGN.md, authorization.md, interfaces-reference.md, skill file)

---

## Progress Log

### 2026-04-06
- Created todo from discussion about CanSave suppression behavior
- Identified the two suppression points in FactoryModelBuilder.cs (AddCanMethods line 805, AddCanMethodsForInterface line 729)
- User decided: always add CanSave(T target) to IFactorySave<T>, parameterless CanSave runs non-target auth only, CanSave(target) runs all matching auth methods
- Drafted plan: [CanSave Target Parameter Support](../plans/cansave-target-parameter.md)

---

## Completion Verification

Before marking this todo as Complete, verify:

- [ ] All builds pass
- [ ] All tests pass

**Verification results:**
- Build: Succeeded (0 errors)
- Tests: All pass — 532 unit (x2 TFMs), 543 integration (x2 TFMs), 68 Design (x2 TFMs)

---

## Results / Conclusions

Successfully implemented `CanSave(T target)` generation for target-parameterized authorization. The generator now produces two CanSave overloads when Write-scoped auth methods have target parameters:

- `CanSave()` (parameterless) — runs only non-target Write auth (role checks)
- `CanSave(target)` — runs ALL Write auth (non-target + target-parameterized)

CanInsert/CanUpdate/CanDelete remain correctly suppressed. Existing AuthorizedOrder behavior is unchanged. `AssignUniqueNames` was enhanced to support C# method overloading for CanMethods specifically.

**Pre-existing issue noted:** Auth method triplication in generated LocalCanSave (calls auth 3x due to `Distinct()` reference equality on record collections). Not introduced by this work — should be tracked separately.
