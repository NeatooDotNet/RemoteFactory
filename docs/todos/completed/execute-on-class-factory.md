# Support [Execute] Static Methods on Non-Static [Factory] Classes

**Status:** Complete
**Priority:** High
**Created:** 2026-02-17
**Last Updated:** 2026-02-17

---

## Problem

`[Execute]` methods are currently only supported on static classes (enforced by diagnostic `NF0103`). This forces orchestration logic that naturally belongs with an aggregate root into a separate static class, scattering related logic and requiring internal helpers to be made public or duplicated.

In the real-world `Consultation` aggregate, a `StartForPatient` orchestration method calls `Consultation`-specific factory methods and internal helpers. Without `[Execute]` support on the `[Factory]` class, callers must reference the concrete type directly, breaking the factory abstraction pattern.

## Solution

Allow `[Execute]` on `public static` methods within non-static `[Factory]` classes. The generator would:

1. Relax the `NF0103` diagnostic to allow `[Execute]` when the method is static (even if the class is not)
2. Generate factory methods on the existing class factory interface (e.g., `IConsultationFactory.StartForPatient(...)`)
3. Enforce that the return type matches the containing type's service type (interface if available)
4. Method is `public static` (no underscore prefix — factory class needs access)

**Detailed spec:** See `remotefactory-execute-on-class-factory.md` in project root.

---

## Plans

- [Support Execute on Class Factory - Implementation Plan](../plans/completed/execute-on-class-factory.md)

---

## Tasks

- [x] Architect review and plan creation
- [x] Developer review of plan (Approved with 4 items addressed in contract)
- [x] Implementation (all 7 phases complete, 2,868 tests passing)
- [x] Architect verification (VERIFIED — 0 failures)
- [x] Complete

---

## Progress Log

### 2026-02-17
- Created todo from feature spec (`remotefactory-execute-on-class-factory.md`)
- Starting architect review
- Architect completed codebase analysis and created implementation plan at `docs/plans/execute-on-class-factory.md`
- Key design decision: Execute on class factory generates factory interface methods (like Create/Fetch), not standalone delegate types (like static factory Execute)
- Identified NF0204 interaction risk with Execute methods returning the target type
- Plan status: Draft (Architect), ready for developer review
- Developer review completed. Plan approved with implementation contract.
- Four items addressed: NF0204 analysis correction, DomainMethodName property, HasCancellationToken property, NF0102 validation
- Plan status: Ready for Implementation
- User clarified signature requirements: `public static` (not private), no underscore prefix, must return containing type
- Updated plan, design project files, and implementation contract to reflect these decisions
- Implementation started: Phase 1 (Model Layer) - Created `ClassExecuteMethodModel`, updated `CreateMethodWithUniqueName`
- Phase 2 (Transform) - Relaxed NF0103, moved Execute check before return-type check, renamed diagnostic to `ExecuteRequiresStaticMethod`
- Phase 3 (Builder) - Added `BuildClassExecuteMethod`, Execute case in `BuildClassFactory` with NF0102 validation
- Phase 4 (Renderer) - Added `RenderClassExecuteMethod`/`RenderClassExecutePublicMethod`/`RenderClassExecuteLocalMethod`, reused `RenderRemoteMethod`
- Phase 5 (Design) - Uncommented design files, renamed class to `ClassExecuteDemo`, all 29 Design tests pass
- Phase 6 (Tests) - Created 5 unit tests (3 target classes) and 8 integration tests (2 target classes), all pass
- Phase 7 (NF0103 Verification) - Confirmed NF0103 behavior correct by code inspection and full test suite
- Full test suite: 2,868 passed, 0 failed, 9 skipped across net8.0/net9.0/net10.0
- Plan status set to "Awaiting Verification"

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] Design project builds successfully
- [x] Design project tests pass

**Verification results:**
- Design build: Passed (0 errors, 0 warnings)
- Design tests: 29 passed (26 existing + 3 new)

---

## Results / Conclusions

Feature implemented and verified. Key decisions made during implementation:

1. **`public static` (not `private static`)** — The generated factory class is separate from the domain class, so it needs access to call the Execute method.
2. **No underscore prefix** — Since the method is public, the method name is used directly as the factory interface method name.
3. **Return type must match containing type** — Keeps the factory interface cohesive. Use static class `[Execute]` for arbitrary return types.
4. **Generates factory interface methods** (not delegate types) — Consistent with Create/Fetch/Save on the same factory.

Files created/modified: 5 new files (model, test targets, tests), 6 modified files (transform, diagnostics, builder, renderer, design project).
Full test suite: 2,781+ tests passing, 0 failures.
