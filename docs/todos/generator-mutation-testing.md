# Generator Mutation Testing

**Date Created:** 2026-01-22
**Status:** Not Started
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

**Projects to Remove:**
- [ ] `FactoryGeneratorTests` (540 tests) - DEPRECATED
- [ ] `RemoteFactory.AspNet.Tests` - DEPRECATED

**Why:** The initial run included deprecated `FactoryGeneratorTests` which has 540 tests. These tests may have caught bugs that the new test projects miss, inflating detection rates.

**Commands:**
```bash
# Remove from solution
dotnet sln src/Neatoo.RemoteFactory.sln remove src/Tests/FactoryGeneratorTests/FactoryGeneratorTests.csproj
dotnet sln src/Neatoo.RemoteFactory.sln remove src/Tests/RemoteFactory.AspNet.Tests/RemoteFactory.AspNet.Tests.csproj

# Delete project folders
rm -rf src/Tests/FactoryGeneratorTests
rm -rf src/Tests/RemoteFactory.AspNet.Tests
```

---

## Code Path Tracking

### ClassFactoryRenderer.cs

| # | Code Path | Syntax Bug | Syntax Result | Logic Bug | Logic Result | Notes |
|---|-----------|------------|---------------|-----------|--------------|-------|
| 1 | Factory interface (RenderFactoryInterface) | [ ] | | [ ] | | |
| 2 | Event partial class (RenderEventPartialClass) | [ ] | | [ ] | | |
| 3 | Delegate rendering (RenderDelegate) | [ ] | | [ ] | | |
| 4 | Delegate property (RenderDelegateProperty) | [ ] | | [ ] | | |
| 5 | Constructor - local (RenderConstructors) | [ ] | | [ ] | | |
| 6 | Constructor - remote (RenderConstructors) | [ ] | | [ ] | | |
| 7 | Read - public method (RenderPublicMethod) | [ ] | | [ ] | | |
| 8 | Read - remote method (RenderRemoteMethod) | [ ] | | [ ] | | |
| 9 | Read - local method (RenderReadLocalMethod) | [ ] | | [ ] | | |
| 10 | DoFactoryMethodCall - Bool variant | [ ] | | [ ] | | |
| 11 | DoFactoryMethodCall - Async variant | [ ] | | [ ] | | |
| 12 | DoFactoryMethodCall - AsyncNullable variant | [ ] | | [ ] | | |
| 13 | Write - local method (RenderLocalMethod) | [ ] | | [ ] | | |
| 14 | Save - public method (RenderSavePublicMethod) | [ ] | | [ ] | | |
| 15 | Save - local method (RenderSaveLocalMethod) | [ ] | | [ ] | | |
| 16 | Save - insert branch | [ ] | | [ ] | | |
| 17 | Save - update branch | [ ] | | [ ] | | |
| 18 | Save - delete branch | [ ] | | [ ] | | |
| 19 | Save - explicit interface (IFactorySave) | [ ] | | [ ] | | |
| 20 | Can - public method | [ ] | | [ ] | | |
| 21 | Can - remote method (RenderCanRemoteMethod) | [ ] | | [ ] | | |
| 22 | Can - local method (RenderCanLocalMethod) | [ ] | | [ ] | | |
| 23 | Authorization checks (RenderAuthorizationChecks) | [ ] | | [ ] | | |
| 24 | Auth method call (RenderAuthMethodCall) | [ ] | | [ ] | | |
| 25 | Service registrar - factory | [ ] | | [ ] | | |
| 26 | Service registrar - delegates | [ ] | | [ ] | | |
| 27 | Event registration - local (RenderLocalEventRegistration) | [ ] | | [ ] | | |
| 28 | Event registration - remote (RenderRemoteEventRegistration) | [ ] | | [ ] | | |

### StaticFactoryRenderer.cs

| # | Code Path | Syntax Bug | Syntax Result | Logic Bug | Logic Result | Notes |
|---|-----------|------------|---------------|-----------|--------------|-------|
| 29 | Delegate rendering (RenderDelegate) | [ ] | | [ ] | | |
| 30 | Event delegate (RenderEventDelegate) | [ ] | | [ ] | | |
| 31 | Remote delegate registration (RenderRemoteDelegateRegistration) | [ ] | | [ ] | | |
| 32 | Local delegate registration (RenderLocalDelegateRegistration) | [ ] | | [ ] | | |
| 33 | Remote event registration (RenderRemoteEventRegistration) | [ ] | | [ ] | | |
| 34 | Local event registration (RenderLocalEventRegistration) | [ ] | | [ ] | | |

### OrdinalRenderer.cs

| # | Code Path | Syntax Bug | Syntax Result | Logic Bug | Logic Result | Notes |
|---|-----------|------------|---------------|-----------|--------------|-------|
| 35 | Converter class (RenderConverterClass) | [ ] | | [ ] | | |
| 36 | Read method (RenderReadMethod) | [ ] | | [ ] | | |
| 37 | Write method (RenderWriteMethod) | [ ] | | [ ] | | |
| 38 | Partial class - PropertyNames | [ ] | | [ ] | | |
| 39 | Partial class - ToOrdinalArray | [ ] | | [ ] | | |
| 40 | Partial class - FromOrdinalArray | [ ] | | [ ] | | |
| 41 | Partial class - CreateOrdinalConverter | [ ] | | [ ] | | |
| 42 | Construction - primary constructor | [ ] | | [ ] | | |
| 43 | Construction - object initializer | [ ] | | [ ] | | |

### InterfaceFactoryRenderer.cs

| # | Code Path | Syntax Bug | Syntax Result | Logic Bug | Logic Result | Notes |
|---|-----------|------------|---------------|-----------|--------------|-------|
| 44 | Factory interface (RenderFactoryInterface) | [ ] | | [ ] | | |
| 45 | Delegate rendering (RenderDelegate) | [ ] | | [ ] | | |
| 46 | Delegate property (RenderDelegateProperty) | [ ] | | [ ] | | |
| 47 | Constructor - local | [ ] | | [ ] | | |
| 48 | Constructor - remote | [ ] | | [ ] | | |
| 49 | Public method (RenderPublicMethod) | [ ] | | [ ] | | |
| 50 | Remote method (RenderRemoteMethod) | [ ] | | [ ] | | |
| 51 | Local method (RenderLocalMethod) | [ ] | | [ ] | | |
| 52 | Can method special handling | [ ] | | [ ] | | |
| 53 | Authorization checks | [ ] | | [ ] | | |
| 54 | Service registrar | [ ] | | [ ] | | |

---

## Gap Summary

### Coverage Gaps Found (bugs NOT caught)

| # | Code Path | Bug Type | Expected Error | Actual Result | Action Required |
|---|-----------|----------|----------------|---------------|-----------------|
| | | | | | |

### Test Updates Made (to fill gaps only)

| # | Test File | Change Made | Reason |
|---|-----------|-------------|--------|
| | | | |

---

## Progress

- **Total Code Paths:** 54
- **Syntax Tests Completed:** 0 / 54
- **Logic Tests Completed:** 0 / 54
- **Gaps Found:** 0
- **Tests Updated:** 0

---

## Completion Criteria

- [ ] Deprecated test projects deleted (Step 0)
- [ ] All 54 code paths tested with syntax bugs
- [ ] All 54 code paths tested with logic bugs
- [ ] All gaps documented in Gap Summary
- [ ] Any test updates documented (gap fills only)
- [ ] Final summary written

---

## Preliminary Results (2026-01-22) - NEEDS RE-VALIDATION

**Note:** These results included the deprecated `FactoryGeneratorTests` (540 tests) which may have inflated detection rates. Results must be re-validated after removing deprecated projects.

### Preliminary Overall Results (WITH deprecated tests)

| Renderer | Code Paths | Syntax Detected | Logic Detected | Gaps |
|----------|------------|-----------------|----------------|------|
| ClassFactoryRenderer | 28 | 28/28 (100%) | 28/28 (100%) | 0 |
| StaticFactoryRenderer | 6 | 6/6 (100%) | 6/6 (100%) | 0 |
| OrdinalRenderer | 9 | 8/9 (89%) | 6/9 (67%) | 4 |
| InterfaceFactoryRenderer | 11 | 11/11 (100%) | 11/11 (100%) | 0 |
| **Total** | **54** | **53/54 (98%)** | **51/54 (94%)** | **4** |

### Preliminary Gaps Found (may change after re-validation)

1. **Code Path 38 - PropertyNames**: Logic bug not detected (metadata not validated)
2. **Code Path 40 - FromOrdinalArray**: Logic bug not detected (method not exercised)
3. **Code Path 43 - Object initializer**: Both syntax and logic bugs not detected (no test targets use this path)

### Detection Methods Observed

- **Build Errors (Compile Time)**: Most bugs detected
- **Test Failures (Runtime)**: Some logic bugs caught (auth inversion, DI registration, delete branch)
- **Not Detected**: 4 bugs in OrdinalRenderer

---

## Session Log

### Session 1 - 2026-01-22

**Status:** Preliminary run completed, but included deprecated FactoryGeneratorTests

**Issue Discovered:** The deprecated `FactoryGeneratorTests` project (540 tests) was included in the test run. This may have caught bugs that the new test projects (UnitTests: 381, IntegrationTests: 414) would miss.

**Next Steps:**
1. Delete deprecated test projects
2. Re-run mutation testing with only new test projects
3. Document actual gaps in new test coverage

---
