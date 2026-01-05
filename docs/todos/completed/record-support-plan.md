# Record Support Plan for RemoteFactory

## Overview

Add C# record support to RemoteFactory, enabling records (particularly Value Objects) to make round trips from client to database through the generated factory infrastructure.

## Motivation

C# records are ideal for DDD Value Objects:
- **Value-based equality** - Built-in structural comparison
- **Immutability** - `init` properties and positional syntax
- **Concise syntax** - `record Address(string Street, string City)`
- **With expressions** - Easy to create modified copies

Use cases for factory-enabled records:
- Fetch reference data (currencies, countries, status codes)
- Create value objects with server-side validation
- Complex value object construction requiring database lookups

## Current State

**UPDATE: Record support is already partially implemented in the generator!**

The generator already handles `RecordDeclarationSyntax`:

```csharp
// FactoryGenerator.cs:21-22 - Already includes records
predicate: static (s, _) => s is ClassDeclarationSyntax or RecordDeclarationSyntax
```

Existing implementation in `FactoryGenerator.Types.cs`:
- `TypeInfo.IsRecord` property (line 220)
- `TypeInfo.HasPrimaryConstructor` property (line 221)
- Record struct detection in `TypeInfo` constructor (lines 43-80)
- `TypeFactoryMethodInfo` constructor for primary constructors (lines 302-332)
- `MethodInfo` constructor for record primary constructor parameters (lines 484-505)

Diagnostics already defined in `DiagnosticDescriptors.cs`:
- NF0205: `[Create]` on type requires record with primary constructor
- NF0206: `record struct` not supported

**What's needed: Verification and comprehensive testing.**

## Implementation Plan

### Phase 1: Generator Verification and Gap Analysis

Since record support is already implemented, Phase 1 focuses on **verification** and identifying any gaps.

#### 1.1 Verification Checklist

Run through these checks to verify existing implementation:

- [x] `RecordDeclarationSyntax` is handled by the predicate in `FactoryGenerator.cs`
- [x] `TypeInfo.IsRecord` is set correctly for record types
- [x] `TypeInfo.HasPrimaryConstructor` correctly detects primary constructors
- [x] NF0205 is emitted when `[Create]` is on a non-record or record without primary constructor
- [x] NF0206 is emitted for `record struct`
- [x] Generated factory interface has correct method signatures for records
- [x] Generated factory implementation creates records correctly at runtime
- [x] `[Create]` on positional record type generates Create method using primary constructor

**Status: COMPLETE** - All verification items confirmed via comprehensive tests.

#### 1.2 Roslyn Type Hierarchy (Reference)

`ClassDeclarationSyntax` and `RecordDeclarationSyntax` share a common base class:

```
BaseTypeDeclarationSyntax
    └── TypeDeclarationSyntax (abstract)
            ├── ClassDeclarationSyntax (sealed)
            ├── InterfaceDeclarationSyntax (sealed)
            ├── RecordDeclarationSyntax (sealed)
            └── StructDeclarationSyntax (sealed)
```

**Note**: `ParameterList` (for primary constructors) is specific to `RecordDeclarationSyntax`.

#### 1.3 Existing Implementation Details

**TypeInfo (FactoryGenerator.Types.cs)**:
```csharp
// Lines 220-221
public bool IsRecord { get; }
public bool HasPrimaryConstructor { get; }
```

**TypeFactoryMethodInfo for Primary Constructors (lines 302-332)**:
- Second constructor overload creates factory methods for primary constructors
- Extracts parameters from `recordSyntax.ParameterList`

**MethodInfo for Record Parameters (lines 484-505)**:
- Constructor specifically handles extracting parameters from record primary constructor

#### 1.4 Gap Analysis - Potential Issues to Verify

1. **Service injection in primary constructor**: Does `[Service]` work in positional parameters?
   ```csharp
   [Factory]
   [Create]
   public record RecordWithService(string Name, [Service] IService Service);
   ```

2. **Mixed parameters**: Constructor params + additional `init` properties
   ```csharp
   [Factory]
   [Create]
   public record MixedRecord(string Name)
   {
       public DateTime CreatedAt { get; init; } = DateTime.Now;
   }
   ```

3. **Record inheritance**: Derived records with base constructor calls
   ```csharp
   [Factory]
   [Create]
   public record DerivedRecord(string Name, int Value, string Extra)
       : BaseRecord(Name, Value);
   ```

#### 1.5 Diagnostic Definitions (Already Exist)

**NF0205**: `[Create]` on type requires record with primary constructor
**NF0206**: `record struct` not supported

Both are defined in `DiagnosticDescriptors.cs`.

### Phase 2: Test Coverage

**Status: MOSTLY COMPLETE** - Core tests implemented, edge cases remaining.

Tests follow the established patterns in `FactoryGeneratorTests/`:
- Use `FactoryTestBase<TFactory>` for client/server container setup
- Use `ClientServerContainers.Scopes()` for isolated DI containers
- Use Theory/MemberData for parameterized testing across containers

#### 2.1 Unit Tests - Generator Validation (FactoryGeneratorTests/Diagnostics/)

Create new test file: `RecordDiagnosticTests.cs`

Diagnostic tests use `CSharpGeneratorDriver` pattern from existing `DiagnosticsTests.cs`:

```csharp
[Fact]
public void NF0205_CreateOnNonRecord_ReportsDiagnostic()
{
    var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]  // Invalid - not a record
    public class NotARecord
    {
        public string Name { get; set; }
    }
}";
    var (diagnostics, _, _) = RunGenerator(source);
    var nf0205Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0205");
    Assert.NotNull(nf0205Diagnostic);
    Assert.Equal(DiagnosticSeverity.Error, nf0205Diagnostic.Severity);
}

[Fact]
public void NF0206_RecordStruct_ReportsDiagnostic()
{
    var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    [Create]
    public record struct ValueRecord(string Name);
}";
    var (diagnostics, _, _) = RunGenerator(source);
    var nf0206Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "NF0206");
    Assert.NotNull(nf0206Diagnostic);
}
```

| Test | Description | Status |
|------|-------------|--------|
| `ValidRecord_PositionalWithCreate_GeneratesFactory` | Basic `[Factory]` on record generates expected code | ✅ |
| `NF0205_CreateOnRecordWithPrimaryConstructor_NoDiagnostic` | `[Create]` on positional record works | ✅ |
| `NF0206_RecordStruct_ReportsDiagnostic` | `record struct` produces diagnostic | ✅ |
| `NF0205_CreateOnNonRecord_Class_ReportsDiagnostic` | `[Create]` on class produces diagnostic | ✅ |
| `NF0205_CreateOnRecordWithoutPrimaryConstructor_ReportsDiagnostic` | `[Create]` on record without params produces diagnostic | ✅ |
| `Record_WithSuppressFactory_NoGeneration` | `[SuppressFactory]` suppresses generation | ✅ |
| `GenericRecord_Rejected` | Generic records rejected (existing behavior) | ✅ |
| `ValidRecord_SealedWithCreate_GeneratesFactory` | Sealed records generate correctly | ✅ |
| `ValidRecord_WithService_GeneratesFactory` | `[Service]` in positional params | ✅ |
| `ValidRecord_WithDefaults_GeneratesFactory` | Default parameter values | ✅ |
| `ValidRecord_WithFetch_GeneratesFactory` | Fetch operations on records | ✅ |
| `NF0205_CreateOnConstructor_Class_NoDiagnostic` | Explicit constructor on class | ✅ |
| `NF0205_CreateOnConstructor_RecordWithoutPrimaryConstructor_NoDiagnostic` | Explicit constructor on record | ✅ |
| `AbstractRecord_Rejected` | Abstract records filtered out | ✅ |

**File: `RecordDiagnosticTests.cs` - COMPLETE (14 tests)**

#### 2.2 Unit Tests - Record Operations (FactoryGeneratorTests/Factory/)

Create new test file: `RecordTests.cs`

**Test Objects:**

```csharp
// Positional record with [Create] on type
[Factory]
[Create]
public record SimpleRecord(string Name, int Value);

// Positional record with service dependency
[Factory]
[Create]
public record RecordWithService(string Name, [Service] ITestService Service);

// Record with Fetch operation
[Factory]
[Create]
public record FetchableRecord(string Id, string Data)
{
    [Fetch]
    public static FetchableRecord FetchById(string id)
        => new FetchableRecord(id, $"Fetched-{id}");

    [Fetch]
    public static Task<FetchableRecord> FetchByIdAsync(string id)
        => Task.FromResult(new FetchableRecord(id, $"AsyncFetched-{id}"));
}

// Record with explicit constructor (not positional)
[Factory]
public record ExplicitConstructorRecord
{
    public string Name { get; init; }

    [Create]
    public ExplicitConstructorRecord(string name) => Name = name;
}

// Record inheriting from another record
[Factory]
[Create]
public record DerivedRecord(string Name, int Value, string Extra)
    : SimpleRecord(Name, Value);

// Remote record operations
[Factory]
[Create]
public record RemoteRecord(string Name)
{
    [Fetch]
    [Remote]
    public static RemoteRecord FetchRemote(string name)
        => new RemoteRecord($"Remote-{name}");
}

// Sealed record
[Factory]
[Create]
public sealed record SealedRecord(string Value);

// Record with default parameter values
[Factory]
[Create]
public record RecordWithDefaults(string Name = "default", int Value = 0);

// Record with required keyword (C# 11+)
[Factory]
[Create]
public record RecordWithRequired(string Name)
{
    public required string RequiredProp { get; init; }
}

// Record with additional init properties beyond primary constructor
[Factory]
[Create]
public record RecordWithExtraProps(string Name)
{
    public string ComputedProp => $"Hello, {Name}";
    public DateTime CreatedAt { get; init; } = DateTime.Now;
}

// Record with nested record property
[Factory]
[Create]
public record OuterRecord(string Name, InnerRecord Inner);

[Factory]
[Create]
public record InnerRecord(string Value);

// Record with collection property
[Factory]
[Create]
public record RecordWithCollection(string Name, List<string> Items);
```

**Test Cases (Local Container):**

| Test | Description | Status |
|------|-------------|--------|
| `SimpleRecord_Create_ReturnsInstance_Client/Local` | Create via primary constructor | ✅ |
| `RecordWithService_Create_InjectsService_Client/Local` | Service injection works | ✅ |
| `FetchableRecord_FetchById_ReturnsInstance` | Fetch static method works | ✅ |
| `FetchableRecord_FetchByIdAsync_ReturnsInstance` | Async fetch works | ✅ |
| `ExplicitConstructorRecord_Create_ReturnsInstance` | Explicit `[Create]` on constructor | ✅ |
| `Create_DerivedRecord_ReturnsInstance` | Record inheritance works | ⏳ TODO |
| `SimpleRecord_HasValueEquality` | Created records have value equality | ✅ |
| `SealedRecord_Create_ReturnsInstance` | Sealed record creation | ✅ |
| `RecordWithDefaults_Create_WithValues_UsesProvidedValues` | Default parameter values | ✅ |
| `RecordWithExtraProps_Create_SetsComputedProp` | Additional init properties | ✅ |
| `OuterRecord_Create_WithNestedRecord_ReturnsInstance` | Records containing records | ✅ |
| `RecordWithCollection_Create_WithItems_ReturnsInstance` | Collections in records | ✅ |
| `RecordWithNullable_Create_WithNullDescription_ReturnsInstance` | Nullable properties | ✅ |
| `ComplexRecord_Create_WithAllProperties_ReturnsInstance` | Multiple property types | ✅ |
| `RecordWithServiceFetch_FetchWithService_InjectsService` | Service in fetch method | ✅ |
| `GeneratedFactory_SimpleRecord_HasCreateMethod` | Factory interface validation | ✅ |
| `GeneratedFactory_FetchableRecord_HasFetchMethods` | Factory method validation | ✅ |

**File: `RecordTests.cs` - MOSTLY COMPLETE (22 tests, 1 TODO: DerivedRecord)**

#### 2.3 Serialization Round-Trip Tests (FactoryGeneratorTests/Factory/)

Create new test file: `RecordSerializationTests.cs`

These tests validate records survive the client→server→client serialization cycle using the two DI container approach.

**Test Pattern (following `RemoteWriteTests.cs`):**

```csharp
public class RecordSerializationTests : FactoryTestBase<IRemoteRecordFactory>
{
    // MemberData provides both Client and Local containers
    public static IEnumerable<object[]> RemoteRecordFactoryTest_Client
        => new[] { new object[] { ClientServerContainers.Scopes().client
            .ServiceProvider.GetRequiredService<IRemoteRecordFactory>() } };

    public static IEnumerable<object[]> RemoteRecordFactoryTest_Local
        => new[] { new object[] { ClientServerContainers.Scopes().local
            .ServiceProvider.GetRequiredService<IRemoteRecordFactory>() } };

    [Theory]
    [MemberData(nameof(RemoteRecordFactoryTest_Client))]
    [MemberData(nameof(RemoteRecordFactoryTest_Local))]
    public async Task RemoteFetch_Record_SerializesCorrectly(IRemoteRecordFactory factory)
    {
        // Act - calls through serialization layer for Client container
        var result = await factory.FetchRemote("test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Remote-test", result.Name);
    }
}
```

**Serialization Test Cases:**

| Test | Description | Status |
|------|-------------|--------|
| `RemoteFetch_SimpleRecord_RoundTrips` | Simple record survives serialization | ✅ |
| `Create_ComplexRecord_AllTypesSerialize` | All properties preserved | ✅ |
| `ServerContainer_ReceivesCorrectParameters_AfterSerialization` | Create parameters serialize | ✅ |
| `Create_RecordWithNullValue_NullSerializes` | Nullable props handle null | ✅ |
| `Create_NestedRecords_BothRecordsSerialize` | Records containing records | ✅ |
| `Create_RecordWithCollection_CollectionSerializes` | Collections in records | ✅ |
| `Serialization_RecordEquality_PreservedAfterRoundTrip` | Value equality after deserialize | ✅ |
| `RemoteFetchAsync_SimpleRecord_RoundTrips` | Async remote fetch | ✅ |
| `Create_RecordWithEmptyCollection_EmptyCollectionSerializes` | Empty collections | ✅ |
| `Serialization_DifferentRecords_NotEqual` | Inequality verification | ✅ |

**File: `RecordSerializationTests.cs` - COMPLETE (15 tests)**

**Serialization Edge Cases:**

| Test | Description | Status |
|------|-------------|--------|
| `Serialization_RecordWithDefaultValues_PreservesDefaults` | Default param values | ✅ (via ComplexRecord) |
| `Serialization_RecordWithRequiredMembers_Works` | C# 11 required members | ⏳ TODO |
| `Serialization_RecordWith_Expression_NotAffected` | `with` expressions local-only | N/A (not applicable) |
| `Serialization_LargeRecord_HandlesSize` | Large property count | ✅ (via ComplexRecord) |

#### 2.4 Client-Server Container Tests

**Status: COMPLETE** - All client/server container tests implemented.

Tests that explicitly validate the two-container model:

| Test | Description | Status |
|------|-------------|--------|
| `ClientContainer_RemoteFetch_SerializesCorrectly` | Client goes through serialization | ✅ |
| `LocalContainer_RemoteFetch_ExecutesDirectly` | Local executes directly | ✅ |
| `ClientContainer_RemoteAsyncFetch_SerializesCorrectly` | Async remote fetch | ✅ |
| `ServerContainer_ReceivesCorrectParameters_AfterSerialization` | Complex params serialize | ✅ |

**File: `RecordSerializationTests.cs` (RecordClientServerTests class) - COMPLETE (4 tests)**

```csharp
// Example test pattern implemented:
public class RecordClientServerTests
{
    [Fact]
    public async Task ClientContainer_RemoteFetch_SerializesCorrectly()
    {
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.ServiceProvider
            .GetRequiredService<IRemoteRecordFactory>();

        var result = await clientFactory.FetchRemote("test");

        Assert.NotNull(result);
        Assert.Equal("Remote-test", result.Name);
    }
    // ... additional tests
}
```

#### 2.5 Integration Tests (RemoteFactory.AspNet.Tests)

**Status: TODO** - HTTP integration tests not yet created.

Add HTTP-level tests using `WebApplicationFactory<Program>`:

| Test | Description | Status |
|------|-------------|--------|
| `HttpPost_CreateRecord_ReturnsRecord` | HTTP Create endpoint | ⏳ TODO |
| `HttpPost_FetchRecord_ReturnsRecord` | HTTP Fetch endpoint | ⏳ TODO |
| `HttpPost_RecordWithAuth_Authorized` | Authorization works | ⏳ TODO |
| `HttpPost_RecordWithAuth_Denied` | Authorization denial works | ⏳ TODO |

**File: `RecordHttpTests.cs` - NOT CREATED**

#### 2.6 Reflection-Based Validation Tests

Following the pattern from `WriteTests.cs`, validate all generated methods exist:

```csharp
[Fact]
public void GeneratedFactory_HasAllExpectedMethods()
{
    var factoryType = typeof(ISimpleRecordFactory);

    // Verify Create method exists with correct signature
    var createMethod = factoryType.GetMethod("Create");
    Assert.NotNull(createMethod);
    Assert.Equal(typeof(SimpleRecord), createMethod.ReturnType);

    var parameters = createMethod.GetParameters();
    Assert.Equal(2, parameters.Length);
    Assert.Equal("name", parameters[0].Name);
    Assert.Equal("value", parameters[1].Name);
}

[Fact]
public void GeneratedFactory_CreateMethods_AllReturnCorrectType()
{
    // Use reflection to invoke all Create* methods and validate return types
    var factoryType = typeof(ISimpleRecordFactory);
    var createMethods = factoryType.GetMethods()
        .Where(m => m.Name.StartsWith("Create"));

    foreach (var method in createMethods)
    {
        // Validate return type is or wraps SimpleRecord
    }
}
```

### Phase 3: Example and Documentation

#### 3.1 Add Record Examples

Create example Value Objects in the Person example:

```csharp
// Simple positional record with [Create] on type
[Factory]
[Create]
public record Address(string Street, string City, string State, string PostalCode);

// Record with Fetch operation
[Factory]
[Create]
public record Currency(string Code, string Name, string Symbol)
{
    [Fetch]
    public static async Task<Currency> FetchByCode(
        string code,
        [Service] ICurrencyService service)
    {
        return await service.GetByCode(code);
    }
}
```

#### 3.2 Update Documentation

- Add records section to concepts documentation
- Update quick start with record example
- Document any limitations or special considerations

### Phase 4: Edge Cases and Validation

#### 4.1 Diagnostics

Add new diagnostics:
- [ ] **NF0205**: `[Create]` on type requires record with primary constructor
- [ ] **NF0206**: `record struct` not supported (value types incompatible with RemoteFactory)

Existing diagnostics apply to records:
- [ ] Generic records rejected (existing generic type check)
- [ ] Abstract records rejected (existing abstract check)
- [ ] Sealed records work normally

#### 4.2 Special Cases

Handle:
- [ ] Records with required members (C# 11+)
- [ ] Records with default parameter values
- [ ] Nested records
- [ ] Records with `with` expression methods (ensure factory doesn't use them)
- [ ] Records with computed properties
- [ ] Records with collection properties

#### 4.3 Serialization Considerations

**System.Text.Json Serialization of Records**:
- Records with positional parameters use constructor-based deserialization
- The `[JsonConstructor]` attribute may be needed for records with multiple constructors
- Verify that generated factory-created records serialize correctly via `NeatooJsonSerializer`

**Polymorphic Serialization**:
- When a record implements an interface (e.g., `ISimpleRecord`), ensure `$type` discriminator works
- Test that records derived from other records serialize correctly
- Verify that the base record properties are included in derived record serialization

**DI Container Registration**:
Records are automatically registered in `ClientServerContainers.RegisterIfAttribute()` via:
```csharp
if (t.GetCustomAttribute<FactoryAttribute>() != null)
{
    services.AddScoped(t);
}
```
This works for records since they are reference types with the `[Factory]` attribute.

## Files to Modify

### Generator Changes (Verification/Minor Fixes Only)

**Already Implemented** - verify and fix gaps if found:

| File | Status | Notes |
|------|--------|-------|
| `src/Generator/FactoryGenerator.cs` | ✅ Done | Already includes `RecordDeclarationSyntax` |
| `src/Generator/FactoryGenerator.Transform.cs` | ✅ Done | Already handles records |
| `src/Generator/FactoryGenerator.Types.cs` | ✅ Done | Has `IsRecord`, `HasPrimaryConstructor` |
| `src/Generator/DiagnosticDescriptors.cs` | ✅ Done | NF0205, NF0206 already defined |

### Test Files

| File | Purpose | Status |
|------|---------|--------|
| `src/Tests/FactoryGeneratorTests/Factory/RecordTests.cs` | Core record operation tests (Create, Fetch) | ✅ Created |
| `src/Tests/FactoryGeneratorTests/Factory/RecordSerializationTests.cs` | Client/server serialization round-trip tests | ✅ Created |
| `src/Tests/FactoryGeneratorTests/Factory/RecordClientServerTests.cs` | Two-container model validation | ✅ (in RecordSerializationTests.cs) |
| `src/Tests/FactoryGeneratorTests/Diagnostics/RecordDiagnosticTests.cs` | NF0205, NF0206 diagnostic tests | ✅ Created |
| `src/Tests/RemoteFactory.AspNet.Tests/RecordHttpTests.cs` | HTTP integration tests for records | ⏳ TODO |

### Test Objects

| File | Purpose | Status |
|------|---------|--------|
| `src/Tests/FactoryGeneratorTests/Factory/RecordTestObjects.cs` | Test record definitions | ✅ Created |

### Documentation

| File | Changes | Status |
|------|---------|--------|
| `docs/examples/Records/` | Record examples | ⏳ TODO |
| `docs/diagnostics/NF0205.md` | Document new diagnostic | ⏳ TODO |
| `docs/diagnostics/NF0206.md` | Document new diagnostic | ⏳ TODO |

## Resolved Decisions

1. **`[Create]` on positional records**: Allow `[Create]` on the record type declaration. It applies to the primary constructor. **Already implemented.**

2. **Primary constructor detection**: Check if `RecordDeclarationSyntax.ParameterList` is non-null and has parameters. **Already implemented in `TypeInfo` constructor.**

3. **Validation**: Emit NF0205 if `[Create]` is on a type that is not a record with a primary constructor. **Diagnostic already defined.**

4. **Implementation status**: Record support is already implemented in the generator. Focus is on verification and comprehensive testing.

5. **`record struct` exclusion**: Explicitly not supported via NF0206 diagnostic. Value types don't fit the client/server serialization model.

## Success Criteria

### Generator Functionality
- [x] `[Factory]` attribute works on `record` types
- [x] `[Create]` on positional record type generates Create method using primary constructor
- [x] `[Create]` on explicit constructor works as with classes
- [x] `[Fetch]` operations work as expected on records
- [x] Generated factory interfaces and implementations are correct
- [x] NF0205 diagnostic emitted for invalid `[Create]` on type
- [x] NF0206 diagnostic emitted for `record struct`

### Serialization & Client/Server
- [x] Records serialize correctly through `NeatooJsonSerializer`
- [x] Records round-trip correctly through two-container model (client→server→client)
- [x] Record value equality preserved after deserialization
- [x] Records with nested records serialize correctly
- [x] Records with collections serialize correctly
- [x] `[Remote]` attribute works on record Fetch methods

### Test Coverage
- [x] All diagnostic tests pass (NF0205, NF0206) - 14 tests
- [x] All unit tests pass (RecordTests.cs) - 22 tests
- [x] All serialization tests pass (RecordSerializationTests.cs) - 15 tests
- [x] All client/server container tests pass (RecordClientServerTests.cs) - 4 tests
- [x] All HTTP integration tests pass (RecordHttpTests.cs) - 8 tests passing
- [x] All existing tests continue to pass (no regressions)

### Remaining Work
- [x] Create RecordHttpTests.cs (HTTP integration tests) - 8 tests passing
- [x] Add record examples to docs/examples - RecordTests.cs with 6 tests
- [x] Create docs/diagnostics/NF0205.md
- [x] Create docs/diagnostics/NF0206.md

### Known Limitations (Discovered Through Testing)

These limitations were discovered when attempting to add test cases:

1. **Record Inheritance (CS0108)**
   - Issue: The generator's ordinal serialization helpers are generated without `new` keyword
   - Affected: `PropertyNames`, `PropertyTypes`, `ToOrdinalArray`, `FromOrdinalArray`, `CreateOrdinalConverter`
   - Workaround: Don't use `[Factory]` on both base and derived records
   - Documented in: `RecordTestObjects.cs`

2. **C# 11 Required Members (CS9035)**
   - Issue: The generator calls constructors without setting required properties in object initializers
   - Affected: Records/classes with `required` keyword on properties
   - Workaround: Use positional records or avoid `required` members
   - Documented in: `RecordTestObjects.cs`

These should be tracked as future generator enhancements.
