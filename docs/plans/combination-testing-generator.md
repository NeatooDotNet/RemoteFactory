# Combination Testing Generator Implementation Plan

**Date:** 2026-01-22
**Related Todo:** [Factory Method Combination Testing Implementation](../todos/combination-testing-implementation.md)
**Status:** Ready for Implementation
**Last Updated:** 2026-01-22

---

## Overview

Implement comprehensive testing for all factory method combinations using a dedicated source generator. This approach replaces reflection-based tests in the deprecated FactoryGeneratorTests project with strongly-typed, compile-time verified test targets.

**Key Deliverables:**
- ~150 generated test target classes (compile verification)
- ~50 behavioral tests covering distinct code paths
- ~20 diagnostic tests verifying invalid combinations (expanded from original estimate)
- Zero reflection-based combination tests

---

## Approach

### Design Philosophy

1. **Compile-Time Verification**: If the project builds, all valid combinations are syntactically correct
2. **Behavioral Testing**: Explicit tests for each distinct code path in the RemoteFactory generator
3. **Diagnostic Validation**: Tests that verify invalid combinations emit proper diagnostics
4. **JSON-Driven Generation**: Single source of truth for combination dimensions

### Architecture Decision: Single Test Project (IntegrationTests)

**Chosen Approach: All combination tests in IntegrationTests**

After analyzing the generator placement question, the cleanest architecture places all combination testing in `RemoteFactory.IntegrationTests`:

**Why not split between UnitTests and IntegrationTests?**
1. Source generators run during compilation of the project that references them
2. If targets are generated in UnitTests, IntegrationTests would need to either:
   - Reference UnitTests as a project (anti-pattern for test projects)
   - Have its own duplicate generator reference (redundant generation)
3. A shared TestTargets library adds unnecessary project complexity

**Why IntegrationTests is the right choice:**
1. **Single generator reference**: CombinationTestGenerator only referenced once
2. **Full testing capability**: Can test Local, Server, and Remote modes in one place
3. **ClientServerContainers available**: Round-trip serialization tests are straightforward
4. **Local tests remain fast**: Can use simple inline container setup (ServerContainerBuilder-style) for non-remote tests
5. **Conceptual coherence**: Combination testing validates the full factory pipeline

**Test execution time concern addressed:**
- Local/Server mode tests use lightweight inline containers (no serialization overhead)
- Only Remote mode tests pay the ClientServerContainers cost
- ~50 behavioral tests will not significantly impact CI time
- Generated targets compile once; tests execute quickly

### Generator Pipeline Design

```
CombinationDimensions.json  -->  CombinationGenerator  -->  Generated .cs files
         |                              |
         v                              v
   (Embedded Resource)           IIncrementalGenerator
                                        |
                                        v
                            RemoteFactory.IntegrationTests.csproj
                                        |
                                        v
                               Compiled test targets
                                        |
                                        v
                               Behavioral test classes
```

---

## Design

### CombinationTestGenerator Project Structure

```
src/Tests/CombinationTestGenerator/
├── CombinationTestGenerator.csproj       # netstandard2.0 (Roslyn requirement)
├── CombinationDimensions.json            # Embedded resource - combination definitions
├── CombinationGenerator.cs               # IIncrementalGenerator entry point
├── Models/
│   ├── CombinationDimension.cs           # Operation/ReturnType/Parameter enums
│   ├── CombinationInfo.cs                # Single valid combination
│   ├── InvalidCombination.cs             # Invalid combination with diagnostic
│   └── ConfigurationRoot.cs              # Root deserialization type
├── Generation/
│   ├── TargetClassGenerator.cs           # Generates test target classes
│   └── MetadataGenerator.cs              # Generates CombinationMetadata.cs
└── Templates/
    └── CodeTemplates.cs                  # String templates for code generation
```

### JSON Configuration Schema

The JSON configuration in the todo provides a good foundation. Here is the refined schema:

```json
{
  "$schema": "combination-dimensions.schema.json",
  "operations": [
    {
      "name": "Create",
      "attribute": "[Create]",
      "validReturnTypes": ["Void", "Bool", "Task", "TaskBool", "TResult", "TaskTResult"],
      "validParameters": ["None", "Single", "Multiple", "Service", "Mixed", "CancellationToken"],
      "validExecutionModes": ["Local", "Remote"],
      "signatureTypes": ["Constructor", "Static"],
      "generateTestTargets": true
    }
  ],
  "returnTypes": {
    "Void": { "csharpType": "void", "taskVariant": null },
    "Bool": { "csharpType": "bool", "taskVariant": "Task<bool>" },
    "Task": { "csharpType": "Task", "taskVariant": null },
    "TaskBool": { "csharpType": "Task<bool>", "taskVariant": null },
    "TResult": { "csharpType": "{ServiceType}", "taskVariant": "Task<{ServiceType}>" },
    "TaskTResult": { "csharpType": "Task<{ServiceType}>", "taskVariant": null }
  },
  "parameters": {
    "None": [],
    "Single": [{ "type": "int", "name": "id" }],
    "Multiple": [{ "type": "int", "name": "id" }, { "type": "string", "name": "name" }],
    "Service": [{ "type": "IService", "name": "service", "attribute": "[Service]" }],
    "Mixed": [{ "type": "int", "name": "id" }, { "type": "IService", "name": "service", "attribute": "[Service]" }],
    "CancellationToken": [{ "type": "CancellationToken", "name": "ct" }]
  },
  "invalidCombinations": [
    {
      "operation": "Execute",
      "returnType": "Void",
      "diagnostic": "NF0102",
      "reason": "Execute methods must return Task<T>"
    },
    {
      "operation": "Execute",
      "constraint": "non_static_class",
      "diagnostic": "NF0103",
      "reason": "Execute methods require static class"
    },
    {
      "operation": "Insert",
      "returnType": "TResult",
      "diagnostic": "NF0204",
      "reason": "Write operations should not return target type"
    }
  ],
  "authorizationModes": [
    { "name": "None", "attributes": [] },
    { "name": "ClassAuth", "attributes": ["[AuthorizeFactory<{AuthType}>]"] },
    { "name": "MethodAuth", "methods": ["CanCreate", "CanFetch", "CanInsert", "CanUpdate", "CanDelete"] }
  ]
}
```

### Generated Test Target Naming Convention

Pattern: `Comb_{Operation}_{ReturnType}_{Parameters}_{ExecutionMode}`

Examples:
- `Comb_Create_Void_None_Local` - Constructor create, void, no params, local
- `Comb_Create_TaskTResult_Service_Remote` - Static create, Task<T>, service param, remote
- `Comb_Insert_Bool_Mixed_Local` - Insert, bool, int + service params, local
- `Comb_Execute_TaskTResult_Single_Remote` - Execute, Task<T>, single param (always remote)

### Generated File Organization

```
src/Tests/RemoteFactory.IntegrationTests/
├── Generated/
│   └── CombinationTargets/
│       ├── CreateCombinations.g.cs       # ~30 classes
│       ├── FetchCombinations.g.cs        # ~30 classes
│       ├── WriteCombinations.g.cs        # ~60 classes (Insert/Update/Delete)
│       ├── ExecuteCombinations.g.cs      # ~20 classes
│       ├── EventCombinations.g.cs        # ~15 classes
│       └── CombinationMetadata.g.cs      # Enumeration API (documentation only)
├── TestContainers/
│   └── LocalContainerBuilder.cs          # NEW - Lightweight container for local tests
└── FactoryGenerator/
    └── Combinations/                      # NEW - Behavioral tests
        ├── CreateCombinationTests.cs
        ├── FetchCombinationTests.cs
        ├── WriteCombinationTests.cs
        ├── ExecuteCombinationTests.cs
        ├── EventCombinationTests.cs
        ├── AuthCombinationTests.cs
        └── InvalidCombinationDiagnosticTests.cs
```

### CombinationMetadata.g.cs Design

**Clarification**: This generated file is for **documentation and categorization only**. Tests will NOT enumerate over these types using reflection. Behavioral tests are explicit methods targeting specific generated types.

```csharp
// Generated by CombinationTestGenerator
// NOTE: This file is for documentation and categorization purposes.
// Tests should NOT iterate over these types using reflection.
namespace RemoteFactory.IntegrationTests.Generated;

public static class CombinationMetadata
{
    /// <summary>
    /// All generated combination target types. For documentation only.
    /// </summary>
    public static IReadOnlyList<Type> AllTargetTypes { get; } = new[]
    {
        typeof(Comb_Create_Void_None_Local),
        typeof(Comb_Create_Bool_Single_Remote),
        // ... all generated types
    };

    public static IReadOnlyList<Type> CreateTargets { get; } = /* filtered */;
    public static IReadOnlyList<Type> FetchTargets { get; } = /* filtered */;
    public static IReadOnlyList<Type> WriteTargets { get; } = /* filtered */;
    public static IReadOnlyList<Type> ExecuteTargets { get; } = /* filtered */;
    public static IReadOnlyList<Type> EventTargets { get; } = /* filtered */;

    public static IReadOnlyList<Type> RemoteTargets { get; } = /* filtered */;
    public static IReadOnlyList<Type> LocalTargets { get; } = /* filtered */;
}
```

### LocalContainerBuilder for Non-Remote Tests

To avoid ClientServerContainers overhead for local/server mode tests, add a lightweight builder:

```csharp
// src/Tests/RemoteFactory.IntegrationTests/TestContainers/LocalContainerBuilder.cs
public sealed class LocalContainerBuilder
{
    private readonly ServiceCollection _services = new();

    public LocalContainerBuilder(NeatooFactory mode = NeatooFactory.Server)
    {
        _services.AddNeatooRemoteFactory(mode,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            Assembly.GetExecutingAssembly());
        _services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        RegisterFactoryTypes();
    }

    public LocalContainerBuilder WithService<TInterface, TImpl>() { /* ... */ }
    public IServiceProvider Build() => _services.BuildServiceProvider();
    // ... similar to ServerContainerBuilder in UnitTests
}
```

### Behavioral Test Design

Tests should cover distinct code paths, not every combination. Key code paths to test:

**Read Operations (Create/Fetch):**
1. Constructor create (new T())
2. Static factory create (T.Create())
3. Instance fetch method
4. Bool return type (null on false)
5. Task return type
6. With parameters
7. With service injection
8. Remote attribute (uses ClientServerContainers)
9. With CancellationToken

**Write Operations (Insert/Update/Delete):**
1. Insert routing (IsNew=true)
2. Update routing (IsNew=false, IsDeleted=false)
3. Delete routing (IsDeleted=true)
4. Bool return type
5. Task return type
6. Task<bool> return type
7. With parameters
8. With service injection
9. Remote attribute (uses ClientServerContainers)
10. With CancellationToken
11. Insert-only scenario
12. Update/Delete-only scenario

**Execute Operations:**
1. Basic execute
2. With parameters
3. With service injection
4. Nullable return
5. With CancellationToken

**Event Operations:**
1. Void event
2. Task event
3. With parameters
4. With service injection
5. With CancellationToken

**Event Testing Clarification:**
Events in RemoteFactory generate delegate types rather than factory methods. Testing validates:
- Delegate signature generation (correct parameters, return type)
- Event handler invocation through the generated delegate
- CancellationToken propagation (required per NF0404)
- Service injection in event handlers

### Diagnostic Test Design

Diagnostic tests verify the RemoteFactory generator emits proper diagnostics for invalid combinations.

**Complete Diagnostic Coverage (Expanded):**

| Test | Diagnostic | Description |
|------|------------|-------------|
| Execute_VoidReturn | NF0102 | Execute must return Task |
| Execute_NonStaticClass | NF0103 | Execute requires static class |
| StaticClass_NotPartial | NF0101 | Static class must be partial |
| Insert_ReturnsTargetType | NF0204 | Write cannot return target type |
| FactoryMethod_NotStatic | NF0201 | Factory method must be static (in static class) |
| AuthMethod_WrongReturnType | NF0202 | Auth must return bool/string/Task |
| AmbiguousSaveOperations | NF0203 | Ambiguous save operations (Insert+Update+Delete conflict) |
| Event_ReturnsValue | NF0401 | Event must return void or Task |
| Event_NotInFactoryClass | NF0402 | Event method must be in a class with [Factory] |
| Event_NoDataParams | NF0403 | Event has no data parameters (warning) |
| Event_NoCancellationToken | NF0404 | Event requires CancellationToken |

**Note**: NF0103, NF0203, NF0401-0404 are currently missing from UnitTests diagnostic coverage. This plan adds them.

### Integration with Existing Test Infrastructure

The generated targets will use:
- `LocalContainerBuilder` (new) for local/server mode tests
- `ClientServerContainers.Scopes()` for remote round-trip tests
- `DiagnosticTestHelper` (from UnitTests) for diagnostic tests

```csharp
// Example local mode test - lightweight, no serialization
[Fact]
public async Task Create_Constructor_VoidReturn_NoParams_Local()
{
    var provider = new LocalContainerBuilder()
        .WithService<IService, ServiceImpl>()
        .Build();

    var factory = provider.GetRequiredService<IComb_Create_Void_None_LocalFactory>();
    var result = await factory.Create();

    Assert.NotNull(result);
    Assert.True(result.OperationCalled);
}

// Example remote mode test - full round-trip
[Fact]
public async Task Create_Remote_SerializesCorrectly()
{
    var (server, client, _) = ClientServerContainers.Scopes();

    var factory = client.GetRequiredService<IComb_Create_TaskTResult_Single_RemoteFactory>();
    var result = await factory.Create(42);

    Assert.NotNull(result);
    Assert.Equal(42, result.ReceivedIntParam);
}
```

---

## Implementation Steps

### Phase 1: Generator Infrastructure (2-3 days)

1. **Create CombinationTestGenerator.csproj**
   ```xml
   <Project Sdk="Microsoft.NET.Sdk">
     <PropertyGroup>
       <TargetFramework>netstandard2.0</TargetFramework>
       <IsRoslynComponent>true</IsRoslynComponent>
       <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
       <LangVersion>latest</LangVersion>
     </PropertyGroup>
     <ItemGroup>
       <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.3.0" PrivateAssets="all" />
       <PackageReference Include="System.Text.Json" Version="6.0.0" />
     </ItemGroup>
     <ItemGroup>
       <EmbeddedResource Include="CombinationDimensions.json" />
       <None Remove="CombinationDimensions.json" />
     </ItemGroup>
   </Project>
   ```

2. **Define Model Classes** (equatable for incremental caching)
   - `CombinationDimension.cs` - Operation enum, return type enum, parameter enum
   - `CombinationInfo.cs` - Fully equatable combination record
   - `InvalidCombination.cs` - Invalid combination with diagnostic
   - `ConfigurationRoot.cs` - Root JSON deserialization

3. **Implement CombinationGenerator.cs**
   - Parse embedded JSON resource
   - Generate cross-product of valid combinations
   - Exclude invalid combinations
   - Emit target classes and metadata

4. **Create LocalContainerBuilder.cs** in IntegrationTests
   - Lightweight container builder for local/server mode tests
   - Mirrors ServerContainerBuilder pattern from UnitTests

5. **Reference Generator from IntegrationTests**
   ```xml
   <ItemGroup>
     <ProjectReference Include="..\CombinationTestGenerator\CombinationTestGenerator.csproj"
                       OutputItemType="Analyzer"
                       ReferenceOutputAssembly="false" />
   </ItemGroup>
   ```

### Phase 2: Behavioral Tests - Read Operations (1 day)

Create `CreateCombinationTests.cs` with ~8 explicit tests:
- Basic create (constructor) - uses LocalContainerBuilder
- Static create - uses LocalContainerBuilder
- Task return - uses LocalContainerBuilder
- Bool return (null on false) - uses LocalContainerBuilder
- With parameters - uses LocalContainerBuilder
- With service injection - uses LocalContainerBuilder
- Remote create - uses ClientServerContainers
- With CancellationToken - uses LocalContainerBuilder

Create `FetchCombinationTests.cs` with ~6 explicit tests:
- Basic fetch - uses LocalContainerBuilder
- Task return - uses LocalContainerBuilder
- Nullable return - uses LocalContainerBuilder
- With parameters - uses LocalContainerBuilder
- With service injection - uses LocalContainerBuilder
- Remote fetch - uses ClientServerContainers

### Phase 3: Behavioral Tests - Write Operations (1 day)

Create `WriteCombinationTests.cs` with ~12 explicit tests:
- Insert routing (IsNew=true)
- Update routing (IsNew=false, IsDeleted=false)
- Delete routing (IsDeleted=true)
- Bool return (null on false)
- Task return
- Task<bool> return
- With parameters
- With service injection
- Remote write - uses ClientServerContainers
- With CancellationToken
- Insert-only scenario
- Update/Delete-only scenario

### Phase 4: Behavioral Tests - Execute and Event (1 day)

Create `ExecuteCombinationTests.cs` with ~7 explicit tests:
- Basic execute
- With parameters
- With service injection
- Nullable return
- With CancellationToken
- Multiple parameters
- Mixed params (business + service)

Create `EventCombinationTests.cs` with ~9 explicit tests:
- Void event (delegate invocation)
- Task event (async delegate)
- With parameters
- With service injection
- With CancellationToken
- Static class event
- Remote event - uses ClientServerContainers
- Multiple parameters
- Delegate naming convention verification

### Phase 5: Authorization Tests (1 day)

Create `AuthCombinationTests.cs` with ~6 explicit tests:
- Read authorization
- Write authorization
- Execute authorization
- Denied authorization
- Remote authorization - uses ClientServerContainers
- Multiple operations

### Phase 6: Diagnostic Tests (1 day) - EXPANDED

Create `InvalidCombinationDiagnosticTests.cs` with ~20 tests covering ALL diagnostics:

**Currently tested (verify still working):**
- NF0101: Static class not partial
- NF0102: Execute void return
- NF0201: Factory method not static
- NF0202: Auth wrong return type
- NF0204: Write returns target type
- NF0205: (verify exists)
- NF0206: (verify exists)

**Add tests for (currently missing):**
- NF0103: Execute non-static class
- NF0203: Ambiguous save operations
- NF0401: Event returns value (not void/Task)
- NF0402: Event not in [Factory] class
- NF0403: Event no data params (warning)
- NF0404: Event missing CancellationToken

### Phase 7: Remove Reflection Tests (0.5 day)

Delete reflection-based test methods from FactoryGeneratorTests. **Use method names as primary identifiers** (line numbers are secondary reference only, verified 2026-01-22):

| File | Method Name | Approximate Lines |
|------|-------------|-------------------|
| RemoteWriteTests.cs | RemoteWrite (reflection variant) | ~558-641 |
| WriteTests.cs | WriteDataMapperTest | ~508-578 |
| ReadTests.cs | ReadFactory (reflection variant) | ~422-473 |
| RemoteReadTests.cs | RemoteReadFactoryTest | ~390-443 |
| FactoryOnStartCompleteTests.cs | ReadFactoryTest | ~50-97 |
| MixedWriteTests.cs | MixedReturnTypeWriteDataMapperTest | ~505-560 |
| ReadAuthTests.cs | Multiple reflection methods | ~341-625 |
| WriteAuthTests.cs | Multiple reflection methods | ~538-809 |
| RemoteWriteAuthTests.cs | Multiple reflection methods | ~538-925 |

### Phase 8: Documentation (0.5 day)

- Update CLAUDE.md with combination testing section
- Create `docs/testing/combination-testing.md`
- Generate combination matrix documentation from JSON

---

## Acceptance Criteria

- [ ] CombinationTestGenerator builds and references correctly from IntegrationTests
- [ ] ~150 test target classes generate successfully in IntegrationTests/Generated/
- [ ] All generated targets compile with RemoteFactory generator
- [ ] CombinationMetadata.g.cs provides enumeration API (documentation only)
- [ ] LocalContainerBuilder exists for lightweight local/server tests
- [ ] ~50 behavioral tests pass on net8.0, net9.0, net10.0
- [ ] ~20 diagnostic tests pass (expanded coverage)
- [ ] Zero reflection in behavioral test code
- [ ] All deprecated reflection-based tests removed from FactoryGeneratorTests
- [ ] Generated files tracked in git (Generated/ folder)
- [ ] Test execution time < 30 seconds for combination tests
- [ ] Documentation complete with combination matrix

---

## Dependencies

- Neatoo.RemoteFactory library
- Neatoo.Generator source generator
- xUnit v3
- Microsoft.CodeAnalysis.CSharp (for CombinationTestGenerator)
- System.Text.Json (for JSON parsing in CombinationTestGenerator)
- Existing test infrastructure (ClientServerContainers in IntegrationTests)

---

## Risks / Considerations

### Technical Risks

1. **Generator Complexity**: Two source generators in the build pipeline
   - Mitigation: CombinationTestGenerator only outputs to IntegrationTests, separate from main Generator
   - Risk Level: Medium

2. **Incremental Caching**: All model types must be equatable
   - Mitigation: Use records or implement IEquatable<T> with proper GetHashCode
   - Risk Level: Low

3. **JSON Parsing in netstandard2.0**: Limited System.Text.Json support
   - Mitigation: Use JsonSerializer with compatible options, avoid newer APIs
   - Risk Level: Low

4. **Compile Time Impact**: 150 generated files may slow builds
   - Mitigation: Efficient incremental generator, only regenerate on JSON change
   - Risk Level: Low

5. **Path Length Limits**: Generated file paths on Windows
   - Mitigation: Keep class names concise, use short namespace
   - Risk Level: Low

### Design Risks

1. **Combination Explosion**: Cross-product generates too many targets
   - Mitigation: Prune combinations that don't add coverage value
   - Risk Level: Medium

2. **Missing Code Paths**: Behavioral tests don't cover edge cases
   - Mitigation: Explicit test design based on generator code analysis
   - Risk Level: Medium

3. **Maintenance Burden**: JSON schema changes require generator updates
   - Mitigation: Document schema, validate JSON at generation time
   - Risk Level: Low

---

## Architectural Verification

**Generator Constraints:**
- [x] Solution works within netstandard2.0 (CombinationTestGenerator targets netstandard2.0)
- [x] Uses IIncrementalGenerator for performance

**Equatability:**
- [x] CombinationInfo and related types must implement proper equality for pipeline caching

**Serialization:**
- [x] Generated targets include ICombinationTarget interface for verification
- [x] Targets designed for LocalContainerBuilder and ClientServerContainers testing

**Testing:**
- [x] ClientServerContainers pattern used for remote combination tests
- [x] LocalContainerBuilder used for local/server mode tests (new)

**Multi-Target:**
- [x] Generated targets work across net8.0, net9.0, net10.0

**Backward Compatibility:**
- [x] No changes to RemoteFactory library or Generator
- [x] New test infrastructure only

**API Ergonomics:**
- [x] JSON configuration is human-readable
- [x] Generated code follows existing naming conventions

**Documentation:**
- [x] Combination matrix derivable from JSON
- [x] Documentation update included in plan

---

## Developer Review

**Status:** Approved

**Re-Review Date:** 2026-01-22

**Summary:** All previous concerns have been addressed. The revised plan is ready for implementation.

---

### Previous Concerns (All Addressed)

#### 1. Diagnostic Code Discrepancy - ADDRESSED

**Original**: Phase 6 diagnostic tests should explicitly cover missing diagnostics (NF0103, NF0203, NF0401-0404).

**Resolution**: Phase 6 expanded to ~20 tests covering ALL diagnostics including NF0103, NF0203, NF0401, NF0402, NF0403, NF0404.

#### 2. Plan Places Behavioral Tests in Wrong Location - ADDRESSED

**Original**: Some combination tests require ClientServerContainers which is in IntegrationTests.

**Resolution**: Architectural decision made to place ALL combination testing in IntegrationTests. Added LocalContainerBuilder for lightweight local/server mode tests. This eliminates cross-project generator complexity.

#### 3. CombinationMetadata Uses typeof() Arrays - ADDRESSED

**Original**: Clarify whether tests will enumerate the metadata or if it's documentation only.

**Resolution**: Explicitly documented that CombinationMetadata is for **documentation and categorization only**. Tests will NOT iterate over types using reflection. All behavioral tests are explicit methods.

#### 4. Line Numbers for Deprecated Tests May Drift - ADDRESSED

**Original**: Use method names rather than line numbers as primary reference.

**Resolution**: Updated Phase 7 to use method names as primary identifiers with line numbers as secondary reference only.

#### 5. JSON Embedded Resource Configuration - ADDRESSED

**Original**: Ensure file is not also included as Content or None.

**Resolution**: Updated csproj example to include `<None Remove="..."/>` to prevent duplicate inclusion.

#### 6. Event Combination Testing Needs Clarification - ADDRESSED

**Original**: Clarify how events are tested (delegates vs factory methods).

**Resolution**: Added "Event Testing Clarification" section explaining that events generate delegates and how testing validates delegate signature generation, invocation, and CancellationToken propagation.

#### 7. Missing NF0402 Test - ADDRESSED

**Original**: Plan omitted NF0402 (EventRequiresFactoryClass).

**Resolution**: Added NF0402 to the diagnostic test list in the "Complete Diagnostic Coverage" table.

---

### New Verification (2026-01-22 Re-Review)

#### LocalContainerBuilder Pattern Verification

**Verified**: Similar patterns already exist in the codebase:
- `ServerContainerBuilder` in `RemoteFactory.UnitTests/TestContainers/`
- `LogicalContainerBuilder` in `RemoteFactory.UnitTests/TestContainers/`

Both follow the same fluent builder pattern with `WithService<TInterface, TImpl>()`, `Build()`, and `RegisterFactoryTypes()`. The proposed `LocalContainerBuilder` in IntegrationTests follows this established pattern and is a reasonable addition.

#### Diagnostic Coverage Completeness Check

**Verified all generator diagnostics:**

| Diagnostic | Description | In Plan | Test Exists |
|------------|-------------|---------|-------------|
| NF0101 | Class must be partial | Yes | Yes (UnitTests) |
| NF0102 | Execute must return Task | Yes | Yes (UnitTests) |
| NF0103 | Execute requires static class | Yes (add) | No |
| NF0104 | Hint name truncated | No* | No |
| NF0201 | Factory method must be static | Yes | Yes (UnitTests) |
| NF0202 | Auth wrong return type | Yes | Yes (UnitTests) |
| NF0203 | Ambiguous save operations | Yes (add) | No |
| NF0204 | Write returns target type | Yes | Yes (UnitTests) |
| NF0205 | Create requires record with primary ctor | Yes | Yes (UnitTests) |
| NF0206 | Record struct not supported | Yes | Yes (UnitTests) |
| NF0207 | Nested type ordinal skipped | No** | No |
| NF0301 | Method skipped no attribute | No*** | No |
| NF0401 | Event must return void/Task | Yes (add) | No |
| NF0402 | Event requires [Factory] class | Yes (add) | No |
| NF0403 | Event no data params (warning) | Yes (add) | No |
| NF0404 | Event missing CancellationToken | Yes (add) | No |

*NF0104 is an edge case for very long type names hitting file system limits - reasonable to exclude from combination testing.
**NF0207 is Info-level for nested types without ordinal serialization - informational only.
***NF0301 is opt-in debugging diagnostic (`isEnabledByDefault: false`) - not applicable for normal testing.

**Conclusion**: The plan appropriately focuses on Error and Warning diagnostics that affect normal usage. The omitted diagnostics are either edge-case infrastructure (NF0104), informational (NF0207), or opt-in debugging (NF0301).

#### Architecture Decision Verification

**Verified**: The decision to place all tests in IntegrationTests is sound because:
1. `ClientServerContainers` already exists in IntegrationTests
2. A single generator reference avoids cross-project complexity
3. `LocalContainerBuilder` addresses the lightweight testing concern
4. No anti-patterns (test project referencing test project)

---

### Final Assessment

**All concerns addressed. No new concerns identified.**

Ready for implementation with the following notes:
- The developer should use method names (not line numbers) when identifying deprecated tests to delete
- The `LocalContainerBuilder` should follow the pattern of existing container builders
- CombinationMetadata is documentation-only; tests must not iterate over types using reflection

---

## User Decisions (2026-01-22)

**Q1: Will two source generators be required if tests are split between UnitTests and IntegrationTests?**

**A1**: No. A single generator reference in IntegrationTests is sufficient. Splitting would require either:
- UnitTests being referenced by IntegrationTests (anti-pattern)
- Duplicate generator references (redundant)
- A shared TestTargets library (unnecessary complexity)

**Q2: Should we do this all in integration tests since it's more involved?**

**A2**: Yes. All combination tests will go in IntegrationTests because:
- Single generator reference, single location
- ClientServerContainers available for remote tests
- LocalContainerBuilder (new) keeps local tests lightweight
- Test execution time is acceptable (~50 tests, only remote tests pay serialization cost)
- Eliminates architectural complexity

---

## Implementation Contract

**In Scope:**
- [ ] Create CombinationTestGenerator project (netstandard2.0)
- [ ] Define CombinationDimensions.json schema
- [ ] Implement IIncrementalGenerator
- [ ] Generate ~150 test target classes to IntegrationTests/Generated/
- [ ] Generate CombinationMetadata.g.cs (documentation only)
- [ ] Create LocalContainerBuilder.cs in IntegrationTests
- [ ] Create ~50 behavioral tests in IntegrationTests
- [ ] Create ~20 diagnostic tests (expanded coverage)
- [ ] Remove deprecated reflection-based tests from FactoryGeneratorTests
- [ ] Update documentation

**Out of Scope:**
- Modifying RemoteFactory library code
- Modifying Neatoo.Generator code
- Adding new factory operations
- Changing existing test behavior (only adding new tests)
- Authorization test targets with `[Authorize]` ASP.NET Core attribute (requires additional setup)
- Modifying RemoteFactory.UnitTests project structure

---

## Implementation Progress

**Phase 1:** Generator Infrastructure - NOT STARTED
- [ ] Create CombinationTestGenerator.csproj
- [ ] Define model classes (equatable)
- [ ] Create CombinationDimensions.json
- [ ] Implement CombinationGenerator.cs
- [ ] Create LocalContainerBuilder.cs in IntegrationTests
- [ ] Reference generator from IntegrationTests
- [ ] **Verification**: Generated files appear in IntegrationTests/Generated/

**Phase 2:** Behavioral Tests - Read - NOT STARTED
- [ ] CreateCombinationTests.cs (~8 tests)
- [ ] FetchCombinationTests.cs (~6 tests)
- [ ] **Verification**: Tests pass

**Phase 3:** Behavioral Tests - Write - NOT STARTED
- [ ] WriteCombinationTests.cs (~12 tests)
- [ ] **Verification**: Tests pass

**Phase 4:** Behavioral Tests - Execute/Event - NOT STARTED
- [ ] ExecuteCombinationTests.cs (~7 tests)
- [ ] EventCombinationTests.cs (~9 tests)
- [ ] **Verification**: Tests pass

**Phase 5:** Authorization Tests - NOT STARTED
- [ ] AuthCombinationTests.cs (~6 tests)
- [ ] **Verification**: Tests pass

**Phase 6:** Diagnostic Tests - NOT STARTED
- [ ] InvalidCombinationDiagnosticTests.cs (~20 tests, expanded coverage)
- [ ] **Verification**: Tests pass, all diagnostics covered

**Phase 7:** Remove Reflection Tests - NOT STARTED
- [ ] Delete deprecated test methods (by method name)
- [ ] **Verification**: No test regressions

**Phase 8:** Documentation - NOT STARTED
- [ ] Update CLAUDE.md
- [ ] Create combination-testing.md
- [ ] **Verification**: Documentation complete

---

## Completion Evidence

_(Required before marking complete)_

- **Tests Passing:** [Output or screenshot]
- **Generated Code Sample:** [Snippet showing targets compile in IntegrationTests]
- **All Checklist Items:** [Confirmed 100% complete]
