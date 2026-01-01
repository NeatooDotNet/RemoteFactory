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

- [ ] `RecordDeclarationSyntax` is handled by the predicate in `FactoryGenerator.cs`
- [ ] `TypeInfo.IsRecord` is set correctly for record types
- [ ] `TypeInfo.HasPrimaryConstructor` correctly detects primary constructors
- [ ] NF0205 is emitted when `[Create]` is on a non-record or record without primary constructor
- [ ] NF0206 is emitted for `record struct`
- [ ] Generated factory interface has correct method signatures for records
- [ ] Generated factory implementation creates records correctly at runtime
- [ ] `[Create]` on positional record type generates Create method using primary constructor

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

Tests should follow the established patterns in `FactoryGeneratorTests/`:
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

| Test | Description |
|------|-------------|
| `RecordWithFactoryAttribute_GeneratesFactory` | Basic `[Factory]` on record generates expected code |
| `PositionalRecordWithCreateOnType_GeneratesCreateMethod` | `[Create]` on positional record works |
| `RecordStruct_EmitsNF0206` | `record struct` produces diagnostic |
| `CreateOnNonRecord_EmitsNF0205` | `[Create]` on class produces diagnostic |
| `CreateOnRecordWithoutPrimaryConstructor_EmitsNF0205` | `[Create]` on record without params produces diagnostic |
| `RecordWithSuppressFactory_NoGeneration` | `[SuppressFactory]` suppresses generation |
| `GenericRecord_Rejected` | Generic records rejected (existing behavior) |
| `SealedRecord_Works` | Sealed records generate correctly |
| `RecordWithServiceInPrimaryConstructor_Works` | `[Service]` in positional params |

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

| Test | Description |
|------|-------------|
| `Create_PositionalRecord_ReturnsInstance` | Create via primary constructor |
| `Create_RecordWithService_InjectsService` | Service injection works |
| `Fetch_Record_ReturnsInstance` | Fetch static method works |
| `Fetch_RecordAsync_ReturnsInstance` | Async fetch works |
| `Create_ExplicitConstructor_ReturnsInstance` | Explicit `[Create]` on constructor |
| `Create_DerivedRecord_ReturnsInstance` | Record inheritance works |
| `Record_HasValueEquality` | Created records have value equality |
| `Create_SealedRecord_ReturnsInstance` | Sealed record creation |
| `Create_RecordWithDefaults_UsesDefaults` | Default parameter values |
| `Create_RecordWithDefaults_OverridesDefaults` | Override default values |
| `Create_RecordWithExtraProps_InitPropsWork` | Additional init properties |
| `Create_NestedRecords_Works` | Records containing records |
| `Create_RecordWithCollection_Works` | Collections in records |

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

| Test | Description |
|------|-------------|
| `RemoteFetch_SimpleRecord_RoundTrips` | Simple record survives serialization |
| `RemoteFetch_RecordWithMultipleProperties_RoundTrips` | All properties preserved |
| `RemoteCreate_Record_ParametersSerializeCorrectly` | Create parameters serialize |
| `RemoteFetch_RecordWithNullableProperty_HandlesNull` | Nullable props handle null |
| `RemoteFetch_NestedRecords_RoundTrips` | Records containing records |
| `RemoteFetch_RecordWithCollectionProperty_RoundTrips` | Collections in records |
| `Serialization_RecordEquality_PreservedAfterRoundTrip` | Value equality after deserialize |

**Serialization Edge Cases:**

| Test | Description |
|------|-------------|
| `Serialization_RecordWithDefaultValues_PreservesDefaults` | Default param values |
| `Serialization_RecordWithRequiredMembers_Works` | C# 11 required members |
| `Serialization_RecordWith_Expression_NotAffected` | `with` expressions local-only |
| `Serialization_LargeRecord_HandlesSize` | Large property count |

#### 2.4 Client-Server Container Tests

Tests that explicitly validate the two-container model:

```csharp
public class RecordClientServerTests
{
    [Fact]
    public async Task ClientContainer_CallsServerContainer_ForRemoteFetch()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var clientFactory = scopes.client.ServiceProvider
            .GetRequiredService<IRemoteRecordFactory>();

        // Act - this goes through MakeSerializedServerStandinDelegateRequest
        var result = await clientFactory.FetchRemote("test");

        // Assert - result was created on server, serialized, deserialized on client
        Assert.NotNull(result);
        Assert.Equal("Remote-test", result.Name);
    }

    [Fact]
    public async Task LocalContainer_DoesNotSerialize_ForRemoteFetch()
    {
        // Arrange
        var scopes = ClientServerContainers.Scopes();
        var localFactory = scopes.local.ServiceProvider
            .GetRequiredService<IRemoteRecordFactory>();

        // Act - local container executes directly without serialization
        var result = await localFactory.FetchRemote("test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Remote-test", result.Name);
    }

    [Fact]
    public async Task ServerContainer_ReceivesCorrectParameters_AfterSerialization()
    {
        // Test that complex parameters survive the serialization round-trip
    }
}
```

#### 2.5 Integration Tests (RemoteFactory.AspNet.Tests)

Add HTTP-level tests using `WebApplicationFactory<Program>`:

| Test | Description |
|------|-------------|
| `HttpPost_CreateRecord_ReturnsRecord` | HTTP Create endpoint |
| `HttpPost_FetchRecord_ReturnsRecord` | HTTP Fetch endpoint |
| `HttpPost_RecordWithAuth_Authorized` | Authorization works |
| `HttpPost_RecordWithAuth_Denied` | Authorization denial works |

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

### Test Files (New)

| File | Purpose |
|------|---------|
| `src/Tests/FactoryGeneratorTests/Factory/RecordTests.cs` | Core record operation tests (Create, Fetch) |
| `src/Tests/FactoryGeneratorTests/Factory/RecordSerializationTests.cs` | Client/server serialization round-trip tests |
| `src/Tests/FactoryGeneratorTests/Factory/RecordClientServerTests.cs` | Two-container model validation |
| `src/Tests/FactoryGeneratorTests/Diagnostics/RecordDiagnosticTests.cs` | NF0205, NF0206 diagnostic tests |
| `src/Tests/RemoteFactory.AspNet.Tests/RecordHttpTests.cs` | HTTP integration tests for records |

### Test Objects (New)

| File | Purpose |
|------|---------|
| `src/Tests/FactoryGeneratorTests/Factory/RecordTestObjects.cs` | Test record definitions (SimpleRecord, FetchableRecord, etc.) |

### Documentation

| File | Changes |
|------|---------|
| `docs/concepts/*.md` | Document record support |
| `docs/diagnostics/NF0205.md` | Document new diagnostic |
| `docs/diagnostics/NF0206.md` | Document new diagnostic |

## Resolved Decisions

1. **`[Create]` on positional records**: Allow `[Create]` on the record type declaration. It applies to the primary constructor. **Already implemented.**

2. **Primary constructor detection**: Check if `RecordDeclarationSyntax.ParameterList` is non-null and has parameters. **Already implemented in `TypeInfo` constructor.**

3. **Validation**: Emit NF0205 if `[Create]` is on a type that is not a record with a primary constructor. **Diagnostic already defined.**

4. **Implementation status**: Record support is already implemented in the generator. Focus is on verification and comprehensive testing.

5. **`record struct` exclusion**: Explicitly not supported via NF0206 diagnostic. Value types don't fit the client/server serialization model.

## Success Criteria

### Generator Functionality
- [ ] `[Factory]` attribute works on `record` types
- [ ] `[Create]` on positional record type generates Create method using primary constructor
- [ ] `[Create]` on explicit constructor works as with classes
- [ ] `[Fetch]` operations work as expected on records
- [ ] Generated factory interfaces and implementations are correct
- [ ] NF0205 diagnostic emitted for invalid `[Create]` on type
- [ ] NF0206 diagnostic emitted for `record struct`

### Serialization & Client/Server
- [ ] Records serialize correctly through `NeatooJsonSerializer`
- [ ] Records round-trip correctly through two-container model (client→server→client)
- [ ] Record value equality preserved after deserialization
- [ ] Records with nested records serialize correctly
- [ ] Records with collections serialize correctly
- [ ] `[Remote]` attribute works on record Fetch methods

### Test Coverage
- [ ] All diagnostic tests pass (NF0205, NF0206)
- [ ] All unit tests pass (RecordTests.cs)
- [ ] All serialization tests pass (RecordSerializationTests.cs)
- [ ] All client/server container tests pass (RecordClientServerTests.cs)
- [ ] All HTTP integration tests pass (RecordHttpTests.cs)
- [ ] All existing tests continue to pass (no regressions)
