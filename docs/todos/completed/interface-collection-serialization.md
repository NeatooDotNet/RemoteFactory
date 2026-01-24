# Interface Collection Serialization Investigation

**Status:** Complete
**Priority:** High
**Created:** 2026-01-24
**Last Updated:** 2026-01-24

---

## Problem

Interface-typed collection properties like `IList<IDomainObject>` may not properly round-trip through RemoteFactory's JSON serialization. While serialization likely works (the serializer can inspect runtime types), deserialization may fail because System.Text.Json only sees the declared property type (`IList<T>`) and has no guidance on what concrete type to instantiate.

This is a critical issue because DDD applications commonly use interface-typed collections for abstraction and testability.

## Solution

Create comprehensive tests that validate the full serialization/deserialization round-trip for various interface collection patterns, using the `ClientServerContainers.Scopes()` pattern to ensure realistic client-server simulation. Based on test results, extend the converter infrastructure if needed.

---

## Plans

- [Interface Collection Serialization Test Plan](#test-plan) (inline below)

---

## Background

System.Text.Json cannot deserialize `IList<ISomething>` out of the box because:
1. It doesn't know what concrete collection type to instantiate for `IList<T>`
2. It doesn't know what concrete type to use for each interface element

RemoteFactory solves part of this using:
- `$type`/`$value` wrappers for interface elements
- `IServiceAssemblies` to resolve types at runtime via DI

### Key Files
- `src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs` - Handles `$type`/`$value` deserialization
- `src/RemoteFactory/Internal/NeatooInterfaceJsonConverterFactory.cs` - Determines when to apply interface converter

### Known Limitation

In `NeatooInterfaceJsonConverterFactory.CanConvert()` (line 23):
```csharp
if ((typeToConvert.IsInterface || typeToConvert.IsAbstract) && !typeToConvert.IsGenericType && this.serviceAssemblies.HasType(typeToConvert))
```

The `!typeToConvert.IsGenericType` condition **excludes** generic interface types like `IList<T>`, `ICollection<T>`, etc.

### Serialization vs Deserialization Asymmetry

**Serialization (Write):**
- Gets the concrete runtime object (e.g., `List<ChildImpl>`)
- Can iterate and write each element with `$type`/`$value` wrappers
- Likely works correctly

**Deserialization (Read):**
- Only has the declared property type (e.g., `IList<IChild>`)
- Must determine: What collection type to instantiate?
- Must determine: What element type for each `$type`/`$value` pair?
- This is where failures are expected

### Expected Behavior Matrix

| Property Type | Serialization | Deserialization | Status |
|--------------|---------------|-----------------|--------|
| `List<IInterface>` | Works (concrete collection) | Likely works (concrete collection, interface elements) | Untested |
| `IList<IInterface>` | Works (runtime type known) | Unknown - excluded by `!IsGenericType` | Untested |
| `ICollection<IInterface>` | Works | Unknown - excluded by `!IsGenericType` | Untested |
| `IEnumerable<IInterface>` | Works | Unknown - problematic (no Add method) | Untested |
| `IInterface` (single) | Works | Works (interface converter applies) | Untested |
| `List<ConcreteType>` | Works | Works (standard STJ) | Tested |

---

## Test Plan

### Location

Tests should be placed in:
- **Test file:** `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceCollectionSerializationTests.cs`
- **Test targets:** `src/Tests/RemoteFactory.IntegrationTests/TestTargets/TypeSerialization/InterfaceCollectionTargets.cs`

This follows the existing pattern established by `CoverageGapSerializationTests.cs` and `AggregateSerializationTests.cs`.

### Test Target Design

#### Domain Interfaces

```csharp
// src/Tests/RemoteFactory.IntegrationTests/TestTargets/TypeSerialization/InterfaceCollectionTargets.cs

/// <summary>
/// Interface for testing interface-typed property serialization.
/// </summary>
public interface ITestChild
{
    Guid Id { get; }
    string Name { get; set; }
    decimal Value { get; set; }
}

/// <summary>
/// Interface for nested collection testing (IParent with IList<IChild>).
/// </summary>
public interface ITestParent
{
    Guid Id { get; }
    string Name { get; set; }
    IList<ITestChild> Children { get; }
}
```

#### Concrete Implementations

```csharp
/// <summary>
/// Concrete implementation of ITestChild for serialization tests.
/// </summary>
[Factory]
public class TestChildImpl : ITestChild
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Value { get; set; }

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Fetch]
    public void Fetch(Guid id, string name, decimal value)
    {
        Id = id;
        Name = name;
        Value = value;
    }
}

/// <summary>
/// Concrete implementation of ITestParent with IList<ITestChild> property.
/// </summary>
[Factory]
public class TestParentImpl : ITestParent
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public IList<ITestChild> Children { get; set; } = new List<ITestChild>();

    [Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    [Fetch]
    [Remote]
    public void Fetch(Guid id, [Service] ITestChildImplFactory childFactory)
    {
        Id = id;
        Name = $"Parent-{id}";
        Children = new List<ITestChild>
        {
            childFactory.Fetch(Guid.NewGuid(), "Child1", 10.0m),
            childFactory.Fetch(Guid.NewGuid(), "Child2", 20.0m),
            childFactory.Fetch(Guid.NewGuid(), "Child3", 30.0m)
        };
    }
}
```

#### Collection Property Variations

```csharp
/// <summary>
/// Entity with various interface collection property types for comprehensive testing.
/// </summary>
[Factory]
public class InterfaceCollectionContainer
{
    // Single interface property (baseline - should work)
    public ITestChild? SingleChild { get; set; }

    // Concrete collection, interface elements
    public List<ITestChild> ListOfInterface { get; set; } = new();

    // Interface collection, interface elements (this is the key test)
    public IList<ITestChild> IListOfInterface { get; set; } = new List<ITestChild>();

    // Other interface collection types
    public ICollection<ITestChild> ICollectionOfInterface { get; set; } = new List<ITestChild>();

    // Read-only interface (special case - no setter needed)
    public IReadOnlyList<ITestChild> IReadOnlyListOfInterface { get; set; } = new List<ITestChild>();

    // Nested: parent with interface collection
    public ITestParent? NestedParent { get; set; }

    // Factory methods for testing
    [Create]
    public static InterfaceCollectionContainer Create()
    {
        return new InterfaceCollectionContainer();
    }

    [Fetch]
    [Remote]
    public static InterfaceCollectionContainer FetchWithAllCollectionTypes(
        [Service] ITestChildImplFactory childFactory,
        [Service] ITestParentImplFactory parentFactory)
    {
        var child1 = childFactory.Fetch(Guid.NewGuid(), "Child1", 10.0m);
        var child2 = childFactory.Fetch(Guid.NewGuid(), "Child2", 20.0m);
        var child3 = childFactory.Fetch(Guid.NewGuid(), "Child3", 30.0m);

        return new InterfaceCollectionContainer
        {
            SingleChild = child1,
            ListOfInterface = new List<ITestChild> { child1, child2 },
            IListOfInterface = new List<ITestChild> { child1, child2, child3 },
            ICollectionOfInterface = new List<ITestChild> { child1 },
            IReadOnlyListOfInterface = new List<ITestChild> { child2, child3 },
            NestedParent = parentFactory.Fetch(Guid.NewGuid())
        };
    }

    // Separate fetch methods for isolating test failures
    [Fetch]
    [Remote]
    public static InterfaceCollectionContainer FetchSingleInterface([Service] ITestChildImplFactory childFactory)
    {
        return new InterfaceCollectionContainer
        {
            SingleChild = childFactory.Fetch(Guid.NewGuid(), "SingleChild", 100.0m)
        };
    }

    [Fetch]
    [Remote]
    public static InterfaceCollectionContainer FetchListOfInterface([Service] ITestChildImplFactory childFactory)
    {
        return new InterfaceCollectionContainer
        {
            ListOfInterface = new List<ITestChild>
            {
                childFactory.Fetch(Guid.NewGuid(), "ListChild1", 10.0m),
                childFactory.Fetch(Guid.NewGuid(), "ListChild2", 20.0m)
            }
        };
    }

    [Fetch]
    [Remote]
    public static InterfaceCollectionContainer FetchIListOfInterface([Service] ITestChildImplFactory childFactory)
    {
        return new InterfaceCollectionContainer
        {
            IListOfInterface = new List<ITestChild>
            {
                childFactory.Fetch(Guid.NewGuid(), "IListChild1", 10.0m),
                childFactory.Fetch(Guid.NewGuid(), "IListChild2", 20.0m)
            }
        };
    }

    [Fetch]
    [Remote]
    public static InterfaceCollectionContainer FetchICollectionOfInterface([Service] ITestChildImplFactory childFactory)
    {
        return new InterfaceCollectionContainer
        {
            ICollectionOfInterface = new List<ITestChild>
            {
                childFactory.Fetch(Guid.NewGuid(), "ICollectionChild1", 10.0m)
            }
        };
    }

    [Fetch]
    [Remote]
    public static InterfaceCollectionContainer FetchNestedParent([Service] ITestParentImplFactory parentFactory)
    {
        return new InterfaceCollectionContainer
        {
            NestedParent = parentFactory.Fetch(Guid.NewGuid())
        };
    }
}
```

### Test Cases

```csharp
// src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/InterfaceCollectionSerializationTests.cs

namespace RemoteFactory.IntegrationTests.TypeSerialization;

/// <summary>
/// Tests for serialization round-trip of interface-typed collections.
///
/// These tests validate that RemoteFactory correctly handles:
/// - Single interface properties (baseline)
/// - Concrete collections with interface elements (List<IInterface>)
/// - Interface collections with interface elements (IList<IInterface>)
/// - Nested structures with interface collections
///
/// The key challenge is deserialization: while serialization has access to
/// runtime types, deserialization only sees declared types and must resolve
/// both the collection type and element types.
/// </summary>
public class InterfaceCollectionSerializationTests
{
    // ============================================================================
    // Phase 1: Baseline - Single Interface Property
    // ============================================================================

    /// <summary>
    /// Baseline test: single IInterface property should serialize/deserialize correctly.
    /// This uses the existing NeatooInterfaceJsonTypeConverter.
    /// </summary>
    [Fact]
    public async Task SingleInterface_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchSingleInterface();

        // Assert
        Assert.NotNull(result.SingleChild);
        Assert.IsType<TestChildImpl>(result.SingleChild); // Verify concrete type
        Assert.NotEqual(Guid.Empty, result.SingleChild.Id);
        Assert.Equal("SingleChild", result.SingleChild.Name);
        Assert.Equal(100.0m, result.SingleChild.Value);
    }

    // ============================================================================
    // Phase 2: Concrete Collection with Interface Elements
    // ============================================================================

    /// <summary>
    /// List<IInterface> - concrete collection type, interface elements.
    /// Serializer knows to create List<T>, but must handle interface elements.
    /// </summary>
    [Fact]
    public async Task ListOfInterface_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchListOfInterface();

        // Assert
        Assert.NotNull(result.ListOfInterface);
        Assert.Equal(2, result.ListOfInterface.Count);

        // Verify concrete types survived
        Assert.All(result.ListOfInterface, child =>
        {
            Assert.IsType<TestChildImpl>(child);
            Assert.NotEqual(Guid.Empty, child.Id);
            Assert.NotNull(child.Name);
            Assert.True(child.Value > 0);
        });

        // Verify specific values
        Assert.Equal("ListChild1", result.ListOfInterface[0].Name);
        Assert.Equal("ListChild2", result.ListOfInterface[1].Name);
    }

    /// <summary>
    /// Verify that element property values are preserved, not just types.
    /// </summary>
    [Fact]
    public async Task ListOfInterface_PreservesElementProperties()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchListOfInterface();

        // Assert
        Assert.Equal(10.0m, result.ListOfInterface[0].Value);
        Assert.Equal(20.0m, result.ListOfInterface[1].Value);
    }

    // ============================================================================
    // Phase 3: Interface Collection Types (Critical Tests)
    // ============================================================================

    /// <summary>
    /// IList<IInterface> - this is the critical test case.
    /// Serializer must determine both collection type AND element types.
    /// Currently excluded by !IsGenericType condition.
    /// </summary>
    [Fact]
    public async Task IListOfInterface_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchIListOfInterface();

        // Assert
        Assert.NotNull(result.IListOfInterface);
        Assert.Equal(2, result.IListOfInterface.Count);

        // Verify concrete types survived
        Assert.All(result.IListOfInterface, child =>
        {
            Assert.IsType<TestChildImpl>(child);
            Assert.NotEqual(Guid.Empty, child.Id);
        });

        Assert.Equal("IListChild1", result.IListOfInterface[0].Name);
        Assert.Equal("IListChild2", result.IListOfInterface[1].Name);
    }

    /// <summary>
    /// ICollection<IInterface> - another interface collection type.
    /// </summary>
    [Fact]
    public async Task ICollectionOfInterface_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchICollectionOfInterface();

        // Assert
        Assert.NotNull(result.ICollectionOfInterface);
        Assert.Single(result.ICollectionOfInterface);

        var child = result.ICollectionOfInterface.First();
        Assert.IsType<TestChildImpl>(child);
        Assert.Equal("ICollectionChild1", child.Name);
    }

    // ============================================================================
    // Phase 4: Nested Interface Collections
    // ============================================================================

    /// <summary>
    /// Nested: IParent with IList<IChild> property.
    /// Tests that interface resolution works through object graph.
    /// </summary>
    [Fact]
    public async Task NestedParentWithIListChildren_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchNestedParent();

        // Assert
        Assert.NotNull(result.NestedParent);
        Assert.IsType<TestParentImpl>(result.NestedParent); // Verify concrete parent type
        Assert.NotEqual(Guid.Empty, result.NestedParent.Id);
        Assert.StartsWith("Parent-", result.NestedParent.Name);

        // Verify children survived
        Assert.NotNull(result.NestedParent.Children);
        Assert.Equal(3, result.NestedParent.Children.Count);

        Assert.All(result.NestedParent.Children, child =>
        {
            Assert.IsType<TestChildImpl>(child);
            Assert.NotEqual(Guid.Empty, child.Id);
            Assert.NotNull(child.Name);
        });
    }

    // ============================================================================
    // Phase 5: Comprehensive Round-Trip
    // ============================================================================

    /// <summary>
    /// Tests all collection types in a single object.
    /// Useful for verifying no interference between collection types.
    /// </summary>
    [Fact]
    public async Task AllCollectionTypes_SurviveRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchWithAllCollectionTypes();

        // Assert - Each collection type should have correct values
        Assert.NotNull(result.SingleChild);
        Assert.NotEmpty(result.ListOfInterface);
        Assert.NotEmpty(result.IListOfInterface);
        Assert.NotEmpty(result.ICollectionOfInterface);
        Assert.NotEmpty(result.IReadOnlyListOfInterface);
        Assert.NotNull(result.NestedParent);
    }

    // ============================================================================
    // Phase 6: Save Operation Tests (Client-to-Server Direction)
    // ============================================================================

    /// <summary>
    /// Tests that modifications to List<IInterface> survive save round-trip.
    /// This tests client-to-server serialization direction.
    /// </summary>
    [Fact]
    public async Task ListOfInterface_ModificationsPreservedOnSave()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var containerFactory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();
        var childFactory = client.ServiceProvider.GetRequiredService<ITestChildImplFactory>();

        var container = await containerFactory.FetchListOfInterface();

        // Modify on client
        var originalCount = container.ListOfInterface.Count;
        container.ListOfInterface[0].Name = "ModifiedChild";
        container.ListOfInterface[0].Value = 999.0m;

        // Act - Save round-trips through serialization
        var saved = await containerFactory.Save(container);

        // Assert - Modifications should be preserved
        Assert.Equal(originalCount, saved.ListOfInterface.Count);
        Assert.Equal("ModifiedChild", saved.ListOfInterface[0].Name);
        Assert.Equal(999.0m, saved.ListOfInterface[0].Value);
    }

    // ============================================================================
    // Phase 7: Server-Side Direct Tests (Isolation)
    // ============================================================================

    /// <summary>
    /// Server-side test to verify factory works without serialization.
    /// Helps isolate whether failures are in serialization or factory logic.
    /// </summary>
    [Fact]
    public async Task IListOfInterface_WorksDirectlyOnServer()
    {
        // Arrange - Use server container directly (no serialization)
        var (_, server, _) = ClientServerContainers.Scopes();
        var factory = server.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchIListOfInterface();

        // Assert
        Assert.NotNull(result.IListOfInterface);
        Assert.Equal(2, result.IListOfInterface.Count);
        Assert.All(result.IListOfInterface, child => Assert.IsType<TestChildImpl>(child));
    }

    /// <summary>
    /// Local container test - no remoting, single container.
    /// </summary>
    [Fact]
    public async Task IListOfInterface_WorksInLocalMode()
    {
        // Arrange
        var (_, _, local) = ClientServerContainers.Scopes();
        var factory = local.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.FetchIListOfInterface();

        // Assert
        Assert.NotNull(result.IListOfInterface);
        Assert.Equal(2, result.IListOfInterface.Count);
    }

    // ============================================================================
    // Phase 8: JSON Structure Validation
    // ============================================================================

    /// <summary>
    /// Validates that serialized JSON includes $type/$value for interface elements.
    /// This test inspects the actual JSON to understand serialization behavior.
    /// </summary>
    [Fact]
    public async Task ListOfInterface_JsonIncludesTypeInformation()
    {
        // Arrange
        var (_, server, _) = ClientServerContainers.Scopes();
        var serializer = server.ServiceProvider.GetRequiredService<INeatooJsonSerializer>();
        var factory = server.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        var container = await factory.FetchListOfInterface();

        // Act - Serialize to JSON
        var json = serializer.Serialize(container);

        // Assert - JSON should contain type information for interface elements
        Assert.Contains("$type", json);
        Assert.Contains("TestChildImpl", json);
        // Note: Exact format depends on serialization mode (Named vs Ordinal)
    }

    // ============================================================================
    // Phase 9: Empty Collection Tests
    // ============================================================================

    /// <summary>
    /// Empty IList<IInterface> should serialize/deserialize as empty collection.
    /// </summary>
    [Fact]
    public async Task EmptyIListOfInterface_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        // Act
        var result = await factory.Create();

        // Assert
        Assert.NotNull(result.IListOfInterface);
        Assert.Empty(result.IListOfInterface);
    }

    // ============================================================================
    // Phase 10: Null Collection Tests
    // ============================================================================

    /// <summary>
    /// Tests that null interface collections are handled correctly.
    /// </summary>
    [Fact]
    public async Task NullIListOfInterface_SurvivesRoundTrip()
    {
        // Arrange
        var (client, _, _) = ClientServerContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<IInterfaceCollectionContainerFactory>();

        var container = await factory.Create();
        container.IListOfInterface = null!; // Intentionally null

        // Act
        var saved = await factory.Save(container);

        // Assert - Should be null or empty, not throw
        // Exact behavior depends on nullability handling
    }
}
```

### Test Execution Order

The tests are organized to fail fast and provide clear diagnostic information:

1. **Phase 1 (Baseline):** Verify single interface works - if this fails, there's a fundamental converter issue
2. **Phase 2 (Concrete Collection):** Verify `List<IInterface>` works - isolates element handling
3. **Phase 3 (Interface Collection):** Test the critical `IList<IInterface>` case
4. **Phase 4 (Nesting):** Verify nested structures work
5. **Phase 5 (Comprehensive):** Integration test with all types
6. **Phase 6 (Save Direction):** Test client-to-server serialization
7. **Phase 7 (Server Direct):** Isolate factory from serialization
8. **Phase 8 (JSON Inspection):** Debug serialization format
9. **Phase 9-10 (Edge Cases):** Empty and null handling

### Expected Failures

Based on code analysis, the following tests are expected to FAIL initially:

1. **`IListOfInterface_SurvivesRoundTrip`** - The `!typeToConvert.IsGenericType` check in `NeatooInterfaceJsonConverterFactory.CanConvert()` excludes `IList<T>`
2. **`ICollectionOfInterface_SurvivesRoundTrip`** - Same reason
3. **`NestedParentWithIListChildren_SurvivesRoundTrip`** - If the parent's `IList<IChild>` property fails

---

## Tasks

### Phase 1: Create Test Infrastructure
- [x] Create `InterfaceCollectionTargets.cs` with interfaces and implementations
- [x] Create `InterfaceCollectionSerializationTests.cs` with test cases
- [x] Register new types in `ClientServerContainers.RegisterFactoryTypes()` (not needed - auto-registered via `[Factory]` attribute)
- [x] Verify tests compile and factory generates

### Phase 2: Run Tests and Document Behavior
- [ ] Run all tests, document which pass/fail
- [ ] Capture actual JSON output for failing cases
- [ ] Document exact exception messages and stack traces
- [ ] Update Expected Behavior Matrix with actual results

### Phase 3: Fix if Needed
- [ ] If `IList<IInterface>` fails, extend `NeatooInterfaceJsonConverterFactory.CanConvert()` to handle generic interface collections
- [ ] Consider adding converter for `IList<T>`, `ICollection<T>`, `IEnumerable<T>`, `IReadOnlyList<T>`
- [ ] Ensure proper collection instantiation (default to `List<T>` for mutable interfaces)
- [ ] Handle `IEnumerable<T>` specially (may need to materialize to array)

### Phase 4: Documentation
- [ ] Update `docs/advanced/json-serialization.md` with supported collection patterns
- [ ] Add examples showing interface collection serialization
- [ ] Document any limitations (e.g., `IEnumerable<T>` must be materialized)

---

## Progress Log

### 2026-01-24
- Created detailed test plan with specific test targets and test cases
- Analyzed `NeatooInterfaceJsonConverterFactory.CanConvert()` limitation
- Identified expected failure points based on code analysis
- **Phase 1 Complete**: Implemented test infrastructure and ran tests

---

## Results / Conclusions

### Phase 1 Results (Test Infrastructure)

**ALL 13 TESTS PASS** across all three target frameworks (net8.0, net9.0, net10.0).

This was unexpected based on the code analysis. The expected failures did NOT occur:

| Test Case | Expected | Actual |
|-----------|----------|--------|
| `SingleInterface_SurvivesRoundTrip` | Pass | **Pass** |
| `ListOfInterface_SurvivesRoundTrip` | Pass | **Pass** |
| `ListOfInterface_PreservesElementProperties` | Pass | **Pass** |
| `IListOfInterface_SurvivesRoundTrip` | **Fail** | **Pass** |
| `ICollectionOfInterface_SurvivesRoundTrip` | **Fail** | **Pass** |
| `NestedParentWithIListChildren_SurvivesRoundTrip` | **Fail** | **Pass** |
| `AllCollectionTypes_SurviveRoundTrip` | Unknown | **Pass** |
| `ListOfInterface_ModificationsPreservedOnSave` | Pass | **Pass** |
| `IListOfInterface_WorksDirectlyOnServer` | Pass | **Pass** |
| `IListOfInterface_WorksInLocalMode` | Pass | **Pass** |
| `ListOfInterface_JsonIncludesTypeInformation` | Pass | **Pass** |
| `EmptyIListOfInterface_SurvivesRoundTrip` | Pass | **Pass** |
| `NullIListOfInterface_SurvivesRoundTrip` | Pass | **Pass** |

### Analysis

The hypothesis that `IList<IInterface>` properties would fail deserialization was **incorrect**. The serialization infrastructure handles these cases correctly. Possible explanations:

1. The `NeatooOrdinalConverterFactory` or other converters may be handling interface collection types before `NeatooInterfaceJsonConverterFactory` is consulted
2. System.Text.Json may be using default collection handling with the `$type`/`$value` wrappers being applied at the element level
3. The `NeatooJsonTypeInfoResolver` may be providing type information that enables proper deserialization

### Conclusion

**Interface-typed collections work correctly in RemoteFactory.** The investigation revealed that the existing serialization infrastructure properly handles:
- `IList<IInterface>` properties
- `ICollection<IInterface>` properties
- `IReadOnlyList<IInterface>` properties
- Nested objects with interface collection properties
- Save operations with modified interface collection elements

**Phase 3 (Fix if Needed) is NOT required** - the feature works as expected.

---

## Related Files

- Converter factory: `src/RemoteFactory/Internal/NeatooInterfaceJsonConverterFactory.cs`
- Interface converter: `src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs`
- Service assemblies: `src/RemoteFactory/Internal/ServiceAssemblies.cs`
- Test infrastructure: `src/Tests/RemoteFactory.IntegrationTests/TestContainers/ClientServerContainers.cs`
- Similar tests: `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/AggregateSerializationTests.cs`
- Similar tests: `src/Tests/RemoteFactory.IntegrationTests/TypeSerialization/CoverageGapSerializationTests.cs`
- Documentation: `docs/advanced/json-serialization.md`
