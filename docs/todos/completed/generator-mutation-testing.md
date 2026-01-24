# Generator Mutation Testing

**Date Created:** 2026-01-22
**Status:** Complete
**Purpose:** Validate that combination tests catch errors by introducing intentional bugs into the source generator

---

## Overview

This exercise validates that the combination testing approach (from `feat: implement combination testing with source generator`) effectively catches errors in the factory generator's code paths.

**Method:**
1. **Syntax bugs**: Break generated code syntax → verify compile errors occur
2. **Logical bugs**: Break logic → verify test failures occur

**Constraint:** Test updates should only fill coverage gaps, NOT mask intentional bugs.

---

## Prerequisites

### Step 0: Delete Deprecated Test Projects

Before running mutation testing, remove deprecated test projects from the solution to ensure only the new test projects (UnitTests + IntegrationTests) are validating the generator.

**Projects Removed:**
- [x] `FactoryGeneratorTests` (540 tests) - DEPRECATED (removed from solution 2026-01-23)
- [x] `RemoteFactory.AspNet.Tests` - DEPRECATED (removed from solution 2026-01-23)
- [x] `FactoryGeneratorSandbox` - DEPRECATED (removed from solution 2026-01-23)

**Baseline Test Count (after removal):**
- UnitTests: 381 tests
- IntegrationTests: 414 tests (3 skipped)
- Total: 795 tests

---

## Code Path Tracking

### ClassFactoryRenderer.cs

| # | Code Path | Syntax Bug | Syntax Result | Logic Bug | Logic Result | Notes |
|---|-----------|------------|---------------|-----------|--------------|-------|
| 1 | Factory interface (RenderFactoryInterface) | [x] | Build error | [x] | Build error | Interface name change caught |
| 2 | Event partial class (RenderEventPartialClass) | [x] | Build error | [x] | Build error | Delegate name change caught |
| 3 | Delegate rendering (RenderDelegate) | [x] | Build error | [x] | Build error | |
| 4 | Delegate property (RenderDelegateProperty) | [x] | Build error | [x] | Build error | |
| 5 | Constructor - local (RenderConstructors) | [x] | Build error | [x] | Test failed | |
| 6 | Constructor - remote (RenderConstructors) | [x] | Build error | [x] | Test failed | |
| 7 | Read - public method (RenderPublicMethod) | [x] | Build error | [x] | Test failed | |
| 8 | Read - remote method (RenderRemoteMethod) | [x] | Build error | [x] | Test failed | |
| 9 | Read - local method (RenderReadLocalMethod) | [x] | Build error | [x] | Test failed | |
| 10 | DoFactoryMethodCall - Bool variant | [x] | Build error | [x] | Test failed | |
| 11 | DoFactoryMethodCall - Async variant | [x] | Build error | [x] | Test failed | |
| 12 | DoFactoryMethodCall - AsyncNullable variant | [x] | Build error | [x] | Test failed | |
| 13 | Write - local method (RenderLocalMethod) | [x] | Build error | [x] | Test failed | |
| 14 | Save - public method (RenderSavePublicMethod) | [x] | Build error | [x] | Test failed | |
| 15 | Save - local method (RenderSaveLocalMethod) | [x] | Build error | [x] | Test failed | |
| 16 | Save - insert branch | [x] | Build error | [x] | Test failed (92) | IsNew inversion caught |
| 17 | Save - update branch | [x] | Build error | [x] | Test failed | |
| 18 | Save - delete branch | [x] | Build error | [x] | Test failed (134) | IsDeleted inversion caught |
| 19 | Save - explicit interface (IFactorySave) | [x] | Build error | [x] | Test failed | |
| 20 | Can - public method | [x] | Build error | [x] | Test failed | |
| 21 | Can - remote method (RenderCanRemoteMethod) | [x] | Build error | [x] | Test failed | |
| 22 | Can - local method (RenderCanLocalMethod) | [x] | Build error | [x] | Test failed | |
| 23 | Authorization checks (RenderAuthorizationChecks) | [x] | Build error | [x] | Test failed | |
| 24 | Auth method call (RenderAuthMethodCall) | [x] | Build error | [x] | Test failed (4) | HasAccess inversion caught |
| 25 | Service registrar - factory | [x] | Build error | [x] | Build error | |
| 26 | Service registrar - delegates | [x] | Build error | [x] | Test failed | |
| 27 | Event registration - local (RenderLocalEventRegistration) | [x] | Build error | [x] | Test failed | |
| 28 | Event registration - remote (RenderRemoteEventRegistration) | [x] | Build error | [x] | Test failed | |

### StaticFactoryRenderer.cs

| # | Code Path | Syntax Bug | Syntax Result | Logic Bug | Logic Result | Notes |
|---|-----------|------------|---------------|-----------|--------------|-------|
| 29 | Delegate rendering (RenderDelegate) | [x] | Build error | [x] | Build error | |
| 30 | Event delegate (RenderEventDelegate) | [x] | Build error | [x] | Build error | |
| 31 | Remote delegate registration (RenderRemoteDelegateRegistration) | [x] | Build error | [x] | Test failed | |
| 32 | Local delegate registration (RenderLocalDelegateRegistration) | [x] | Build error | [x] | Test failed | |
| 33 | Remote event registration (RenderRemoteEventRegistration) | [x] | Build error | [x] | Test failed | |
| 34 | Local event registration (RenderLocalEventRegistration) | [x] | Build error | [x] | Test failed | |

### OrdinalRenderer.cs

| # | Code Path | Syntax Bug | Syntax Result | Logic Bug | Logic Result | Notes |
|---|-----------|------------|---------------|-----------|--------------|-------|
| 35 | Converter class (RenderConverterClass) | [x] | Build error | [x] | Test failed | |
| 36 | Read method (RenderReadMethod) | [x] | Build error | [x] | Test failed | |
| 37 | Write method (RenderWriteMethod) | [x] | Build error | [x] | Test failed | |
| 38 | Partial class - PropertyNames | [x] | Build error | [x] | Test failed | FILLED: OrdinalMetadataTests |
| 39 | Partial class - ToOrdinalArray | [x] | Build error | [x] | Test failed | |
| 40 | Partial class - FromOrdinalArray | [x] | Build error | [x] | Test failed | FILLED: OrdinalMetadataTests |
| 41 | Partial class - CreateOrdinalConverter | [x] | Build error | [x] | Test failed | |
| 42 | Construction - primary constructor | [x] | Build error | [x] | Test failed | |
| 43 | Construction - object initializer | [x] | Build error | [x] | Test failed (8) | Caught by LogicalModeTests |

### InterfaceFactoryRenderer.cs

| # | Code Path | Syntax Bug | Syntax Result | Logic Bug | Logic Result | Notes |
|---|-----------|------------|---------------|-----------|--------------|-------|
| 44 | Factory interface (RenderFactoryInterface) | [x] | Build error | [x] | Build error | |
| 45 | Delegate rendering (RenderDelegate) | [x] | Build error | [x] | Build error | |
| 46 | Delegate property (RenderDelegateProperty) | [x] | Build error | [x] | Build error | |
| 47 | Constructor - local | [x] | Build error | [x] | Test failed | |
| 48 | Constructor - remote | [x] | Build error | [x] | Test failed | |
| 49 | Public method (RenderPublicMethod) | [x] | Build error | [x] | Test failed | |
| 50 | Remote method (RenderRemoteMethod) | [x] | Build error | [x] | Test failed | |
| 51 | Local method (RenderLocalMethod) | [x] | Build error | [x] | Test failed | |
| 52 | Can method special handling | [x] | Build error | [x] | Test failed | |
| 53 | Authorization checks | [x] | Build error | [x] | Test failed | |
| 54 | Service registrar | [x] | Build error | [x] | Test failed | |

---

## Gap Summary

### Coverage Gaps Found (bugs NOT caught)

| # | Code Path | Bug Type | Expected Error | Actual Result | Status |
|---|-----------|----------|----------------|---------------|--------|
| 38 | PropertyNames | Logic | Test failure | All tests passed | **FILLED** |
| 40 | FromOrdinalArray | Logic | Test failure | All tests passed | **FILLED** |

### Test Updates Made (to fill gaps only)

| # | Test File | Change Made | Reason |
|---|-----------|-------------|--------|
| 38 | OrdinalMetadataTests.cs | Added PropertyNames_ContainsCorrectPropertyNamesInOrder and 3 related tests | Validates PropertyNames contains correct names in order |
| 40 | OrdinalMetadataTests.cs | Added FromOrdinalArray_CreatesInstanceWithCorrectValues and 4 related tests | Validates FromOrdinalArray correctly deserializes |

---

## Progress

- **Total Code Paths:** 54
- **Syntax Tests Completed:** 54 / 54
- **Logic Tests Completed:** 54 / 54
- **Gaps Found:** 2
- **Gaps Filled:** 2
- **Tests Added:** 9 (OrdinalMetadataTests)

---

## Completion Criteria

- [x] Deprecated test projects deleted (Step 0)
- [x] All 54 code paths tested with syntax bugs
- [x] All 54 code paths tested with logic bugs
- [x] All gaps documented in Gap Summary
- [x] Any test updates documented (gap fills only) - Completed 2026-01-23
- [x] Final summary written

---

## Final Results (2026-01-23)

### Overall Results (WITH gap-filling tests)

| Renderer | Code Paths | Syntax Detected | Logic Detected | Gaps |
|----------|------------|-----------------|----------------|------|
| ClassFactoryRenderer | 28 | 28/28 (100%) | 28/28 (100%) | 0 |
| StaticFactoryRenderer | 6 | 6/6 (100%) | 6/6 (100%) | 0 |
| OrdinalRenderer | 9 | 9/9 (100%) | 9/9 (100%) | 0 |
| InterfaceFactoryRenderer | 11 | 11/11 (100%) | 11/11 (100%) | 0 |
| **Total** | **54** | **54/54 (100%)** | **54/54 (100%)** | **0** |

### Comparison to Preliminary Results

| Metric | Preliminary (with deprecated) | Final (without deprecated) | Change |
|--------|------------------------------|---------------------------|--------|
| Syntax Detection | 53/54 (98%) | 54/54 (100%) | +1 improved |
| Logic Detection | 51/54 (94%) | 52/54 (96%) | +1 improved |
| Total Gaps | 4 | 2 | -2 (better) |

**Key Finding:** The preliminary results showed Code Path 43 (object initializer) as not detected, but the new tests (LogicalModeTests) DO catch this bug. The new test projects have better coverage in some areas.

### Gaps Filled (2026-01-23)

1. **Code Path 38 - PropertyNames**: Now detected by `OrdinalMetadataTests.PropertyNames_ContainsCorrectPropertyNamesInOrder`
2. **Code Path 40 - FromOrdinalArray**: Now detected by `OrdinalMetadataTests.FromOrdinalArray_CreatesInstanceWithCorrectValues`

### Gap Analysis (Historical)

Both gaps were in OrdinalRenderer and related to ordinal serialization metadata:

- **PropertyNames** is a static `string[]` property used for debugging/introspection. Originally no tests validated this metadata.
- **FromOrdinalArray** is a static method for creating instances from ordinal arrays. Originally no tests called this method directly (tests used the JSON converter instead).

**Resolution:** Added 9 new tests in `OrdinalMetadataTests.cs` that validate PropertyNames order and FromOrdinalArray deserialization.

### Detection Methods Observed

- **Build Errors (Compile Time)**: 100% syntax detection, many logic bugs also manifest as build errors
- **Test Failures (Runtime)**: Logic bugs in branches (insert/update/delete), authorization, constructors
- **Not Detected**: 2 metadata-related logic bugs in OrdinalRenderer

---

## Session Log

### Session 1 - 2026-01-22

**Status:** Preliminary run completed, but included deprecated FactoryGeneratorTests

**Issue Discovered:** The deprecated `FactoryGeneratorTests` project (540 tests) was included in the test run. This may have caught bugs that the new test projects (UnitTests: 381, IntegrationTests: 414) would miss.

**Next Steps:**
1. Delete deprecated test projects
2. Re-run mutation testing with only new test projects
3. Document actual gaps in new test coverage

### Session 2 - 2026-01-23

**Status:** Complete validation with only new test projects

**Work Performed:**
1. Removed deprecated projects from solution (FactoryGeneratorTests, RemoteFactory.AspNet.Tests, FactoryGeneratorSandbox)
2. Verified clean build baseline (795 tests passing)
3. Systematically tested all 54 code paths across 4 renderers
4. Documented results in tables

**Key Findings:**
- Syntax detection: 100% (all bugs cause build errors)
- Logic detection: 96% (52/54 bugs caught by tests)
- 2 gaps found in OrdinalRenderer (PropertyNames, FromOrdinalArray)
- Code Path 43 (object initializer) now detected by new tests (was gap in preliminary)

**Conclusion:** The new test projects (UnitTests + IntegrationTests) provide 96% mutation testing coverage. The 2 gaps are in low-risk metadata methods and do not affect core functionality.

### Session 3 - 2026-01-23

**Status:** Gap-filling completed - 100% mutation testing coverage achieved

**Work Performed:**
1. Created new test file: `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Ordinal/OrdinalMetadataTests.cs`
2. Added 9 tests to fill the 2 coverage gaps:
   - 4 tests for PropertyNames validation (Gap #38)
   - 3 tests for FromOrdinalArray validation (Gap #40)
   - 2 tests for round-trip and consistency validation

**Verification:**
- Injected Bug #38 (reversed PropertyNames order): 3 tests failed as expected
- Injected Bug #40 (reversed FromOrdinalArray index): 4 tests failed as expected
- Reverted bugs: All 9 new tests pass
- Full test suite: All 390 UnitTests pass (was 381, +9 new)

**Final Results:**
- Syntax detection: 54/54 (100%)
- Logic detection: 54/54 (100%)
- All gaps filled

---

## Results / Conclusions

**Exercise Complete: 100% mutation testing coverage achieved**

The mutation testing exercise successfully validated the effectiveness of the combination testing approach introduced in commit `39ce0f2` (feat: implement combination testing with source generator).

### Key Accomplishments

1. **Perfect Detection Rate**: Achieved 100% detection for both syntax bugs (54/54) and logic bugs (54/54) across all 4 renderers
2. **Test Suite Quality**: The new test structure (UnitTests + IntegrationTests) proves to be more effective than the deprecated tests
3. **Gap Identification & Resolution**: Found and filled 2 coverage gaps in OrdinalRenderer metadata methods
4. **Test Growth**: Added 9 comprehensive tests to fill gaps, bringing total to 804 tests

### Validation Results by Renderer

- **ClassFactoryRenderer** (28 paths): 100% syntax, 100% logic - perfect coverage
- **StaticFactoryRenderer** (6 paths): 100% syntax, 100% logic - perfect coverage
- **OrdinalRenderer** (9 paths): 100% syntax, 100% logic - perfect after gap-filling
- **InterfaceFactoryRenderer** (11 paths): 100% syntax, 100% logic - perfect coverage

### Lessons Learned

1. **Combination testing works**: The systematic testing of all factory modes, authorization modes, and patterns provides comprehensive coverage
2. **Metadata requires explicit testing**: Low-level metadata properties (PropertyNames, FromOrdinalArray) need dedicated tests even if not used in typical workflows
3. **New > Old**: The restructured test projects (UnitTests/IntegrationTests) provide better coverage than the deprecated 540-test FactoryGeneratorTests project

### Recommendations

- Continue using combination testing approach for new features
- Always validate mutation testing coverage when adding new generator code paths
- Keep OrdinalMetadataTests as template for testing serialization metadata

---
