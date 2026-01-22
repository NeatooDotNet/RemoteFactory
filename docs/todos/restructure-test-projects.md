# Restructure Test Projects

**Status:** In Progress
**Priority:** High
**Created:** 2026-01-22
**Last Updated:** 2026-01-22 (Architect Review #2)

---

## Problem

The **FactoryGeneratorTests** project is a mess:
- Mixes unit tests with integration tests
- Uses reflection extensively for test enumeration (48 `method.Invoke()` occurrences)
- No clear separation between serialization tests and generator correctness tests
- Difficult to locate specific test categories
- Tests are slow due to unnecessary serialization overhead in unit tests

## Solution

Replace FactoryGeneratorTests with two well-organized test projects:

1. **RemoteFactory.UnitTests** - Fast unit tests in **Server mode only**. Tests factory behavior including `[Remote]` methods that become async, but no serialization round-trips.
2. **RemoteFactory.IntegrationTests** - Serialization round-trip tests using `ClientServerContainers` pattern. Tests client→server communication.

**Out of Scope:** The existing `RemoteOnlyTests` projects (Client, Server, Domain, Integration) remain unchanged. These test `FactoryMode.RemoteOnly` with extern aliases and separate assemblies.

This restructuring will:
- Eliminate reflection from tests (keep only infrastructure reflection)
- Improve test organization and discoverability
- Increase test coverage with explicit tests for each generated method signature
- Reduce CI execution time by separating fast unit tests from slower integration tests

---

## Plans

- [Test Restructuring Implementation Plan](../plans/test-restructuring-plan.md)

---

## Tasks

### Phase 1: RemoteFactory.UnitTests Project (Server Mode + Logical Mode)
- [ ] Create RemoteFactory.UnitTests.csproj with multi-target (net8.0;net9.0;net10.0)
- [ ] Create TestContainers infrastructure (ServerContainerBuilder, DiagnosticTestHelper)
- [ ] Create TestTargets folder with test entity objects (using naming convention)
- [ ] Migrate local Read tests (ReadTests.cs → LocalCreateTests, LocalFetchTests)
- [ ] Migrate local Write tests (WriteTests.cs → LocalWriteTests)
- [ ] Create Server-mode Remote tests (verify async generation works in Server mode)
- [ ] Migrate local Authorization tests
- [ ] Migrate Diagnostics tests to ROOT LEVEL (Diagnostics/)
- [ ] Migrate Events tests (generation, not serialization)
- [ ] Migrate Execute tests
- [ ] Migrate Parameters tests
- [ ] Migrate Records tests (factory generation only)
- [ ] Create Logical/ namespace with dedicated tests:
  - [ ] LogicalContainerBuilder.cs
  - [ ] LogicalModeTests.cs (IFactorySave, factory.Save)
  - [ ] LogicalComparisonTests.cs (Logical vs Server equivalence)

### Phase 2: RemoteFactory.IntegrationTests Project (Client→Server Round-Trips)
- [ ] Create RemoteFactory.IntegrationTests.csproj with multi-target
- [ ] Move ClientServerContainers infrastructure to new project
- [ ] Create TestObjects folder with test entities
- [ ] Create FactoryRoundTrip/ folder:
  - [ ] Migrate RemoteReadTests.cs (end-to-end factory operations)
  - [ ] Migrate RemoteWriteTests.cs (end-to-end factory operations)
  - [ ] Migrate Remote Authorization tests
- [ ] Create TypeSerialization/ folder:
  - [ ] Move OrdinalSerializationTests.cs
  - [ ] Move RecordSerializationTests.cs
  - [ ] Move AggregateSerializationTests.cs
  - [ ] Move ValidationSerializationTests.cs
  - [ ] Move ReflectionFreeSerializationTests.cs
- [ ] Move RemoteEventIntegrationTests.cs to Events/
- [ ] Add coverage for identified gaps (Dictionary, Enum, large objects)

### Phase 3: Cleanup
- [ ] Remove migrated test files from FactoryGeneratorTests
- [ ] Verify all tests pass on all target frameworks
- [ ] Update solution file with new projects
- [ ] Update CI/CD if needed

**Note:** RemoteOnlyTests projects are out of scope and remain unchanged.

---

## Progress Log

### 2026-01-22 (Architect Review #2)
- Added dedicated `Logical/` namespace for Logical mode tests
- Added `LogicalContainerBuilder` for `NeatooFactory.Logical` testing
- Moved Diagnostics to ROOT LEVEL (out of FactoryGenerator/)
- Added Test Target Naming Convention: `{Operation}Target_{ReturnType}_{ParameterVariation}`
- Added acceptance criteria: Coverage Parity, Namespace Pattern
- Added Logical Mode Regression Risk to considerations

### 2026-01-22 (Update)
- Clarified test scope after architect review:
  - **UnitTests**: Server mode only, tests factory behavior including `[Remote]` methods
  - **IntegrationTests**: Client→Server serialization round-trips
  - **RemoteOnlyTests**: Out of scope (existing projects unchanged)
- Updated migration strategy to separate local vs remote tests

### 2026-01-22
- Initial analysis of FactoryGeneratorTests completed
- Identified 45+ test files, 48 reflection usages
- Created architecture designs for both new test projects
- Established namespace structure and file organization
- Estimated ~270 explicit unit tests needed to replace reflection-based tests

---

## Results / Conclusions

_(To be filled upon completion)_
