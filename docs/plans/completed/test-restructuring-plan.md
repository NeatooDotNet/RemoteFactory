# Test Restructuring Implementation Plan

**Date:** 2026-01-22
**Related Todo:** [Restructure Test Projects](../todos/restructure-test-projects.md)
**Status:** In Progress
**Last Updated:** 2026-01-22 (Phase 4 Complete)

---

## Overview

Comprehensive plan to restructure FactoryGeneratorTests into two focused test projects:
1. **RemoteFactory.UnitTests** - Fast unit tests with explicit method calls (no reflection), testing factory behavior in Server mode
2. **RemoteFactory.IntegrationTests** - Serialization round-trip tests with ClientServerContainers pattern

**Out of Scope:** The existing `RemoteOnlyTests` projects (Client, Server, Domain, Integration) remain unchanged. These test the `FactoryMode.RemoteOnly` scenario with extern aliases and separate assemblies.

---

## Approach

### Design Principles

1. **No Reflection in Tests**: Replace `method.Invoke()` with strongly-typed method calls
2. **Infrastructure Reflection OK**: Assembly scanning for DI registration is acceptable
3. **Behavior Testing**: Create `[Factory]` decorated classes and test their behavior (not code snapshots)
4. **Clear Separation**: Unit tests = Server mode execution | Integration tests = Client→Server serialization round-trips

### Unit Test Scope

Unit tests verify factory behavior in **Server mode only**:
- Methods with `[Remote]` attribute generate async interface methods (void → Task, bool → Task<bool>)
- Factory methods are callable and execute correctly
- Service injection via `[Service]` works
- No serialization round-trip testing

### Integration Test Scope

Integration tests verify **client→server serialization**:
- Use `ClientServerContainers.Scopes()` pattern with client and server containers
- Test that objects serialize correctly through `NeatooJsonSerializer`
- Validate `RemoteRequestDto`/`RemoteResponseDto` round-trips
- Test both Ordinal and Named serialization formats

### Test Migration Strategy

| Current File | Destination | Reason |
|--------------|-------------|--------|
| `ReadTests.cs` | UnitTests | Local factory operations, no `[Remote]` |
| `WriteTests.cs` | UnitTests | Local factory operations, no `[Remote]` |
| `RemoteReadTests.cs` | IntegrationTests | Uses `[Remote]`, requires serialization testing |
| `RemoteWriteTests.cs` | IntegrationTests | Uses `[Remote]`, requires serialization testing |
| `OrdinalSerializationTests.cs` | IntegrationTests | Serialization round-trip |
| `RecordSerializationTests.cs` | IntegrationTests | Serialization round-trip |
| `DiagnosticsTests.cs` | UnitTests/Diagnostics | Generator diagnostics (Roslyn) - ROOT LEVEL |
| `ExecuteTests.cs` | UnitTests | Static `[Execute]` methods |
| `AuthorizationTests.cs` | Both | Local auth → UnitTests, Remote auth → IntegrationTests |
| `LogicalModeTests.cs` | UnitTests/Logical | Logical mode specific - needs LogicalContainerBuilder |

| Current Pattern | New Pattern |
|-----------------|-------------|
| Reflection-based enumeration (`GetMethods().Where(...)`) | Explicit test per method signature |
| Single Theory with MemberData + MethodInfo | Multiple Facts with direct calls |
| Full ClientServerContainers in unit tests | Minimal ServerContainerBuilder (Server mode) |
| Mixed namespaces | Organized by feature area |

---

## Design

### RemoteFactory.UnitTests Structure

Tests factory behavior in **Server mode** (no serialization round-trips), with dedicated **Logical mode** tests.

```
src/Tests/RemoteFactory.UnitTests/
├── RemoteFactory.UnitTests.csproj
├── TestContainers/
│   ├── ServerContainerBuilder.cs       # NeatooFactory.Server - default for unit tests
│   └── DiagnosticTestHelper.cs         # CSharpGeneratorDriver helper
│
├── Diagnostics/                         # ROOT LEVEL - Generator diagnostics (Roslyn)
│   ├── NF0101Tests.cs                   # Static class not partial
│   ├── NF0102Tests.cs                   # Execute not returning Task
│   ├── RecordDiagnosticTests.cs
│   └── DuplicateSaveGeneratorTest.cs
│
├── Logical/                             # DEDICATED NAMESPACE - Logical mode
│   ├── LogicalContainerBuilder.cs       # NeatooFactory.Logical
│   ├── LogicalModeTests.cs              # IFactorySave, factory.Save
│   ├── LogicalComparisonTests.cs        # Logical vs Server equivalence
│   └── TestTargets/
│       └── LogicalModeTargets.cs
│
├── FactoryGenerator/
│   ├── Read/
│   │   ├── LocalCreateTests.cs         # [Create] without [Remote]
│   │   ├── LocalFetchTests.cs          # [Fetch] without [Remote]
│   │   ├── RemoteCreateTests.cs        # [Create][Remote] - Server mode execution
│   │   ├── RemoteFetchTests.cs         # [Fetch][Remote] - Server mode execution
│   │   └── ServiceInjectionTests.cs    # [Service] parameter injection
│   ├── Write/
│   │   ├── LocalWriteTests.cs          # [Insert/Update/Delete] without [Remote]
│   │   ├── RemoteWriteTests.cs         # [Insert/Update/Delete][Remote] - Server mode
│   │   └── SaveMethodGenerationTests.cs
│   ├── Authorization/
│   │   ├── LocalAuthTests.cs           # Authorization without [Remote]
│   │   └── AuthOperationFlagsTests.cs
│   ├── Events/
│   │   ├── EventDelegateGenerationTests.cs
│   │   └── EventParameterFilteringTests.cs
│   ├── Execute/
│   │   ├── ExecuteMethodTests.cs
│   │   └── ExecuteWithServiceTests.cs
│   ├── Parameters/
│   │   ├── CancellationTokenTests.cs
│   │   ├── NullableParameterTests.cs
│   │   ├── ParamsArrayTests.cs
│   │   └── ComplexParameterTests.cs
│   └── Records/
│       ├── RecordFactoryGenerationTests.cs
│       └── RecordWithConstructorTests.cs
│
├── TestTargets/                         # Organized by naming convention
│   ├── Read/
│   │   ├── CreateTarget_Void_NoParams.cs
│   │   ├── CreateTarget_Bool_ServiceParam.cs
│   │   └── FetchTarget_Task_IntParam.cs
│   ├── Write/
│   │   ├── InsertTarget_Void_NoParams.cs
│   │   └── WriteTarget_Bool_CancellationToken.cs
│   ├── Authorization/
│   │   └── AuthTarget_RemoteCreate_Policy.cs
│   └── Events/
│       └── EventTarget_Delegate_Handler.cs
│
└── Generated/
```

### RemoteFactory.IntegrationTests Structure

Tests **client→server serialization round-trips** using `ClientServerContainers.Scopes()`.

```
src/Tests/RemoteFactory.IntegrationTests/
├── RemoteFactory.IntegrationTests.csproj
├── Infrastructure/
│   ├── ClientServerContainers.cs       # Moved from FactoryGeneratorTests
│   ├── ServerServiceProvider.cs
│   ├── MakeSerializedServerStandinDelegateRequest.cs
│   ├── TestHostApplicationLifetime.cs
│   └── TestLoggerProvider.cs
├── FactoryRoundTrip/
│   ├── RemoteReadTests.cs              # [Create][Remote], [Fetch][Remote] end-to-end
│   ├── RemoteWriteTests.cs             # [Insert/Update/Delete][Remote] end-to-end
│   └── RemoteAuthTests.cs              # Remote authorization end-to-end
├── TypeSerialization/
│   ├── OrdinalSerializationTests.cs    # Ordinal format round-trips
│   ├── NamedSerializationTests.cs      # Named format round-trips
│   ├── RecordSerializationTests.cs     # Record-specific tests
│   ├── ComplexTypeSerializationTests.cs
│   ├── NullableSerializationTests.cs
│   ├── AggregateSerializationTests.cs
│   ├── ValidationSerializationTests.cs
│   └── ReflectionFreeSerializationTests.cs
├── Events/
│   └── RemoteEventIntegrationTests.cs
├── TestObjects/
│   ├── FactoryRoundTripTargets.cs      # Entities with [Remote] operations
│   ├── SerializationTestRecords.cs
│   ├── SerializationTestAggregates.cs
│   └── SerializationTestServices.cs
└── Generated/
```

### Test Target Naming Convention

Test target classes follow a consistent naming pattern to make their purpose clear:

**Pattern:** `{Operation}Target_{ReturnType}_{ParameterVariation}`

**Examples:**

| Class Name | Tests |
|------------|-------|
| `CreateTarget_Void_NoParams` | `[Create]` that returns void with no parameters |
| `CreateTarget_Bool_ServiceParam` | `[Create]` that returns bool with `[Service]` parameter |
| `FetchTarget_Task_IntParam` | `[Fetch]` that returns Task with int parameter |
| `InsertTarget_Void_CancellationToken` | `[Insert]` with CancellationToken parameter |
| `WriteTarget_Bool_MultipleParams` | Write operations returning bool with multiple parameters |

**Naming Rules:**
1. **Operation**: `Create`, `Fetch`, `Insert`, `Update`, `Delete`, `Execute`, or compound names like `RemoteCreate`
2. **ReturnType**: `Void`, `Bool`, `Task`, `Int`, `String`, `Entity`, `Nullable`, etc.
3. **ParameterVariation**: `NoParams`, `IntParam`, `ServiceParam`, `MultipleParams`, `CancellationToken`, `Nullable`, `Params`, etc.

**When to use multiple targets vs. one target with multiple methods:**
- **Multiple targets**: When testing different return types or fundamentally different method signatures
- **Single target**: When testing variations of the same operation (e.g., Insert with different parameter combinations)

### Container Builder Pattern (Unit Tests)

Unit tests use a simple `ServerContainerBuilder` for Server mode execution (no client/server split):

```csharp
// ServerContainerBuilder.cs - For UnitTests
public class ServerContainerBuilder
{
    private readonly ServiceCollection _services = new();

    public ServerContainerBuilder()
    {
        _services.AddNeatooRemoteFactory(
            NeatooFactory.Server,  // Server mode - executes locally
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            Assembly.GetExecutingAssembly()
        );
        _services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
    }

    public ServerContainerBuilder WithService<TInterface, TImpl>()
        where TImpl : class, TInterface
        where TInterface : class
    {
        _services.AddScoped<TInterface, TImpl>();
        return this;
    }

    public IServiceProvider Build() => _services.BuildServiceProvider();
}
```

### LogicalContainerBuilder Pattern (Unit Tests - Logical Mode)

Logical mode tests live in their own namespace (`Logical/`) because they test a fundamentally different execution path. Logical mode combines client-side factory interfaces with local method execution (no serialization round-trip).

**When to use which builder:**

| Builder | NeatooFactory Mode | Use For |
|---------|-------------------|---------|
| `ServerContainerBuilder` | `NeatooFactory.Server` | Most unit tests - direct server-side execution |
| `LogicalContainerBuilder` | `NeatooFactory.Logical` | Tests verifying Logical mode behavior (IFactorySave, factory.Save with [Remote] methods) |

**Why Logical mode needs dedicated tests:**
- Logical mode uses `[Remote]` methods but executes them locally
- The `IFactorySave<T>` interface behaves differently in Logical vs Server mode
- Regression risk: Logical mode bugs can go undetected if only tested with Server mode

```csharp
// LogicalContainerBuilder.cs - For Logical mode tests
public class LogicalContainerBuilder
{
    private readonly ServiceCollection _services = new();

    public LogicalContainerBuilder()
    {
        _services.AddNeatooRemoteFactory(
            NeatooFactory.Logical,  // Logical mode - client interface, local execution
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            Assembly.GetExecutingAssembly()
        );
        _services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
    }

    public LogicalContainerBuilder WithService<TInterface, TImpl>()
        where TImpl : class, TInterface
        where TInterface : class
    {
        _services.AddScoped<TInterface, TImpl>();
        return this;
    }

    public IServiceProvider Build() => _services.BuildServiceProvider();
}
```

**Logical Mode Test Examples:**

```csharp
// UnitTests/Logical/LogicalModeTests.cs
namespace RemoteFactory.UnitTests.Logical;

public class LogicalModeTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public LogicalModeTests()
    {
        _provider = new LogicalContainerBuilder()
            .WithService<IService, ServiceImpl>()
            .Build();
    }

    [Fact]
    public void IFactorySave_CanBeResolved_InLogicalMode()
    {
        var factorySave = _provider.GetService<IFactorySave<LogicalModeTarget>>();
        Assert.NotNull(factorySave);
    }

    [Fact]
    public async Task Factory_Save_Insert_ExecutesLocally()
    {
        var factory = _provider.GetRequiredService<ILogicalModeTargetFactory>();
        var entity = new LogicalModeTarget { IsNew = true };

        var result = await factory.Save(entity);

        Assert.True(result.InsertCalled);
    }

    public void Dispose() => (_provider as IDisposable)?.Dispose();
}
```

### ClientServerContainers Pattern (Integration Tests)

Integration tests use the existing `ClientServerContainers.Scopes()` pattern for serialization round-trips:

```csharp
// IntegrationTests use client/server containers
[Theory]
[MemberData(nameof(ClientServerData))]
public async Task RemoteCreate_SerializesCorrectly(string containerType, Func<IServiceProvider> getProvider)
{
    var provider = getProvider();
    var factory = provider.GetRequiredService<IRemoteReadDataMapperFactory>();

    var result = await factory.Create();  // Goes through serialization if client

    Assert.NotNull(result);
    Assert.True(result.CreateCalled);
}
```

### Reflection Elimination Pattern

**Before (Current - Reflection):**
```csharp
// RemoteWriteTests.cs - DON'T DO THIS
public static IEnumerable<object[]> RemoteWriteFactoryTest_Client()
{
    var factory = scopes.client.ServiceProvider.GetRequiredService<IRemoteWriteObjectFactory>();
    var methods = factory.GetType().GetMethods().Where(m => m.Name.StartsWith("Save"));
    foreach (var method in methods) yield return [method, factory];
}

[Theory]
[MemberData(nameof(RemoteWriteFactoryTest_Client))]
public async Task RemoteWrite(MethodInfo method, IRemoteWriteObjectFactory factory)
{
    result = method.Invoke(factory, [entity, default(CancellationToken)]);
}
```

**After (Unit Tests - Server Mode):**
```csharp
// UnitTests/FactoryGenerator/Write/LocalWriteTests.cs
public class LocalWriteTests : IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly IWriteTargetFactory _factory;

    public LocalWriteTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<ITestService, TestService>()
            .Build();
        _factory = _provider.GetRequiredService<IWriteTargetFactory>();
    }

    [Fact]
    public async Task Insert_Void_NoParams_CallsMethod()
    {
        var entity = new WriteTarget { IsNew = true };
        var result = await _factory.Save(entity);  // Strongly-typed, Server mode
        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }

    public void Dispose() => (_provider as IDisposable)?.Dispose();
}
```

**After (Integration Tests - Client/Server Round-Trip):**
```csharp
// IntegrationTests/FactoryRoundTrip/RemoteWriteTests.cs
public class RemoteWriteTests
{
    [Fact]
    public async Task RemoteInsert_ClientToServer_SerializesCorrectly()
    {
        var (client, server) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IRemoteWriteTargetFactory>();

        var entity = new RemoteWriteTarget { IsNew = true };
        var result = await factory.Save(entity);  // Goes through serialization

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
    }
}
```

---

## Implementation Steps

### Phase 1: RemoteFactory.UnitTests Foundation

1. **Create Project**
   - Create `RemoteFactory.UnitTests.csproj` with multi-target
   - Add package references (xunit.v3, DI, Logging)
   - Add project references (RemoteFactory, Generator as Analyzer)
   - Configure `EmitCompilerGeneratedFiles: true`

2. **Create TestContainers Infrastructure**
   - `ServerContainerBuilder.cs` - Fluent builder for server-mode container
   - `LogicalContainerBuilder.cs` - Fluent builder for logical-mode container
   - `DiagnosticTestHelper.cs` - CSharpGeneratorDriver helper (moved from DiagnosticsTests)

3. **Create TestTargets**
   - Define clean test entities with all operation variations
   - Each entity tests specific return types and parameter combinations
   - Use meaningful names: `WriteTargetVoid`, `WriteTargetBool`, etc.

4. **Verify Generator Runs**
   - Build project, confirm `Generated/` folder populated
   - Verify factory interfaces available for test code

### Phase 2: Migrate Unit Tests

Unit tests verify factory behavior in **Server mode only** (no serialization).

**Read Tests (Local + Remote Server-Mode)**
- `LocalCreateTests.cs` - [Create] without [Remote]
- `LocalFetchTests.cs` - [Fetch] without [Remote]
- `RemoteCreateTests.cs` - [Create][Remote] executed in Server mode (verifies async generation)
- `RemoteFetchTests.cs` - [Fetch][Remote] executed in Server mode
- `ServiceInjectionTests.cs` - [Service] parameter injection

**Write Tests (Local + Remote Server-Mode)**
- `LocalWriteTests.cs` - [Insert/Update/Delete] without [Remote]
- `RemoteWriteTests.cs` - [Insert/Update/Delete][Remote] executed in Server mode
- `SaveMethodGenerationTests.cs` - Verify Save method generation

**Authorization Tests (Local Only)**
- `LocalAuthTests.cs` - Authorization without [Remote] (remote auth → IntegrationTests)
- `AuthOperationFlagsTests.cs` - Operation flag combinations

**Diagnostics Tests (~20 tests)**
- Move existing tests, split by diagnostic ID
- Already uses CSharpGeneratorDriver (no reflection in test code)

**Events Tests**
- Event delegate generation, parameter filtering

**Execute Tests**
- Static [Execute] method testing

**Parameters Tests**
- CancellationToken, Nullable, Params, Complex parameters

**Records Tests**
- Record factory generation (serialization tests → IntegrationTests)

### Phase 3: RemoteFactory.IntegrationTests Foundation

1. **Create Project**
   - Create `RemoteFactory.IntegrationTests.csproj` with multi-target
   - Same package/project references as UnitTests

2. **Move Infrastructure**
   - Extract classes from `ClientServerContainers.cs`:
     - `ClientServerContainers` itself
     - `ServerServiceProvider`
     - `MakeSerializedServerStandinDelegateRequest`
     - `TestHostApplicationLifetime`
   - Move `TestLoggerProvider.cs`
   - Update namespaces

3. **Create TestObjects**
   - Extract serialization test records from `RecordTestObjects.cs`
   - Extract aggregate test objects
   - Create test service implementations

### Phase 4: Migrate Integration Tests

Integration tests verify **client→server serialization round-trips**.

**FactoryRoundTrip/ (from FactoryGeneratorTests)**
- `RemoteReadTests.cs` - [Create][Remote], [Fetch][Remote] end-to-end
- `RemoteWriteTests.cs` - [Insert/Update/Delete][Remote] end-to-end
- `RemoteAuthTests.cs` - Remote authorization end-to-end

**TypeSerialization/**
- `OrdinalSerializationTests.cs` - All ordinal/named format tests
- `RecordSerializationTests.cs` - Record round-trip tests
- `AggregateSerializationTests.cs` - Aggregate with children
- `ValidationSerializationTests.cs` - Validation metadata
- `ReflectionFreeSerializationTests.cs` - AOT converter tests

**Events/**
- `RemoteEventIntegrationTests.cs` - Event delegate serialization

**Add Gap Coverage**
- Dictionary serialization tests
- Enum serialization tests
- Large object graph tests
- Edge case string tests

### Phase 5: Cleanup

1. Remove migrated files from FactoryGeneratorTests
2. Verify remaining tests in FactoryGeneratorTests still needed
3. Update solution file
4. Run full test suite on all frameworks
5. Update CI/CD configuration

---

## Acceptance Criteria

- [ ] Zero `MethodInfo.Invoke()` in test code (both projects)
- [ ] All generated factory method signatures have explicit tests
- [ ] Tests pass on net8.0, net9.0, net10.0
- [ ] **UnitTests**: Server mode only, no ClientServerContainers, no serialization
- [ ] **UnitTests/Logical**: Logical mode tests with LogicalContainerBuilder
- [ ] **IntegrationTests**: Client→Server round-trips, ClientServerContainers preserved
- [ ] **RemoteOnlyTests**: Unchanged (out of scope)
- [ ] Generated files output to `Generated/` and committed to git
- [ ] CI/CD discovers and runs all tests
- [ ] Test execution time reduced for unit tests (no serialization overhead)
- [ ] **Coverage Parity**: Every test case from original reflection-based tests has an explicit counterpart
- [ ] **Namespace Pattern**: All test classes follow `RemoteFactory.{UnitTests|IntegrationTests}.{Feature}` pattern

---

## Dependencies

- Neatoo.RemoteFactory library
- Neatoo.Generator source generator
- xUnit v3
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.CodeAnalysis.CSharp (for diagnostic tests)

---

## Risks / Considerations

1. **Test Count Explosion**: ~270 explicit tests vs ~15 reflection-based tests
   - Mitigation: Better maintainability, clearer failures, easier debugging

2. **Test Entity Maintenance**: Many test target classes needed
   - Mitigation: Organize in dedicated TestTargets folder, document patterns

3. **Coverage Regression**: Missing test cases during migration
   - Mitigation: Track each FactoryGeneratorTests file migrated, verify test count

4. **Generated Code Path Limits**: Windows path length issues
   - Mitigation: Keep namespace/class names reasonable, monitor Generated/ folder

5. **Parallel Work Conflicts**: Multiple developers working on migration
   - Mitigation: Assign clear phases to individuals, use feature branches

6. **Logical Mode Regression Risk**: Logical mode has historically been a source of subtle bugs
   - Mitigation: Dedicated `Logical/` namespace ensures Logical mode tests are not accidentally skipped or merged with Server mode tests
   - `LogicalComparisonTests.cs` catches behavioral drift between modes
   - Clear visibility when Logical mode behavior diverges from Server mode

---

## Estimated Effort

| Phase | Estimated Duration |
|-------|-------------------|
| Phase 1: UnitTests Foundation | 1-2 days |
| Phase 2: Migrate Unit Tests | 8-10 days |
| Phase 3: IntegrationTests Foundation | 1 day |
| Phase 4: Migrate Integration Tests | 2-3 days |
| Phase 5: Cleanup | 1 day |
| **Total** | **13-17 days** |

---

## Architectural Verification

**Three Patterns Analysis:**
- Standalone: N/A (test project, not production code)
- Inline Interface: Uses generated factory interfaces
- Inline Class: Uses `[Factory]` decorated test classes

**Breaking Changes:** No - Adding new test projects, not modifying existing code

**Pattern Consistency:** Follows existing xUnit + generated factory patterns

**Codebase Analysis:**
- Examined: ClientServerContainers.cs, FactoryTestBase.cs, RemoteWriteTests.cs, OrdinalSerializationTests.cs, DiagnosticsTests.cs
- Patterns found: Theory/MemberData, reflection enumeration, container scoping

---

## Developer Review

**Status:** Not Started

**Concerns:** None yet

---

## Implementation Contract

**In Scope:**
- [ ] Create RemoteFactory.UnitTests project
- [ ] Create RemoteFactory.IntegrationTests project
- [ ] Migrate local factory tests to UnitTests (Server mode)
- [ ] Migrate remote serialization tests to IntegrationTests
- [ ] Remove migrated code from FactoryGeneratorTests
- [ ] Update solution and CI/CD

**Out of Scope:**
- Modifying RemoteFactory library code
- Modifying Generator code
- Adding new generator features
- Changing existing test behavior (only structure)
- **RemoteOnlyTests projects** - The existing `src/Tests/RemoteOnlyTests/` projects (Client, Server, Domain, Integration) remain unchanged. These test `FactoryMode.RemoteOnly` with extern aliases and are a separate concern.

---

## Implementation Progress

**Phase 1:** Create UnitTests Foundation - **COMPLETE**
- [x] Create csproj (RemoteFactory.UnitTests.csproj with net8.0;net9.0;net10.0)
- [x] Create TestContainers (ServerContainerBuilder, LogicalContainerBuilder, DiagnosticTestHelper)
- [x] Create TestTargets (Read/CreateTargets.cs, Read/FetchTargets.cs, Write/WriteTargets.cs)
- [x] Create Shared/Services.cs (IService, Service, ISecondaryService, SecondaryService)
- [x] Create AssemblyAttributes.cs (FactoryHintNameLength 120)
- [x] Create FactoryGenerator/Read/LocalCreateTests.cs (11 tests)
- [x] Create FactoryGenerator/Read/LocalFetchTests.cs (10 tests)
- [x] Create FactoryGenerator/Write/LocalWriteTests.cs (18 tests)
- [x] **Verification**: Project builds, Generated/ populated, 39 tests pass on all TFMs

**Phase 2:** Migrate Unit Tests (Server Mode) - **IN PROGRESS**
- [x] Local read tests (ReadTests.cs) - Migrated to LocalCreateTests.cs, LocalFetchTests.cs
- [x] Local write tests (WriteTests.cs) - Migrated to LocalWriteTests.cs
- [ ] Remote read/write in Server mode (verify async generation)
- [ ] Local authorization tests
- [ ] Diagnostics tests (ROOT LEVEL)
- [ ] Events tests
- [ ] Execute tests
- [ ] Parameters tests
- [ ] Records tests (factory generation only, not serialization)
- [ ] Logical mode tests (dedicated namespace with LogicalContainerBuilder)
- [ ] **Verification**: All unit tests pass

**Phase 3:** Create IntegrationTests Foundation - **COMPLETE**
- [x] Create csproj (RemoteFactory.IntegrationTests.csproj with net8.0;net9.0;net10.0)
- [x] Move ClientServerContainers infrastructure (adapted for new project)
- [x] Create TestObjects (RoundTripTargets.cs)
- [x] Create initial round-trip tests (RemoteCreate 3, RemoteFetch 2, RemoteSave 4 = 9 tests)
- [x] **Verification**: Project builds, 9 tests pass on all TFMs

**Phase 4:** Migrate Integration Tests (Client→Server Round-Trips) - **COMPLETE**
- [x] FactoryRoundTrip tests (RemoteCreateRoundTripTests, RemoteFetchRoundTripTests, RemoteSaveRoundTripTests)
- [x] TypeSerialization tests (Ordinal, Record, Aggregate, Validation, ReflectionFree)
- [x] Event integration tests (RemoteEventIntegrationTests)
- [x] Authorization tests (AuthorizationEnforcementTests)
- [ ] Gap coverage tests (Dictionary, Enum, large objects)
- [x] **Verification**: 146 integration tests pass on all TFMs

**Phase 5:** Cleanup - IN PROGRESS
- [ ] Remove migrated files from FactoryGeneratorTests (deferred - not removing yet per instructions)
- [x] Update solution file (already includes new projects)
- [x] Full test run on all frameworks (all tests passing)
- [ ] **Verification**: CI/CD passes

---

## Completion Evidence

_(Required before marking complete)_

- **Tests Passing:** [Output or screenshot]
- **Generated Code Sample:** [Snippet showing factories work]
- **All Checklist Items:** [Confirmed 100% complete]
