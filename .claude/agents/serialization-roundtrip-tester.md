---
name: serialization-roundtrip-tester
description: Validates serialization for types crossing client/server boundary
model: opus
---

# Serialization Round-Trip Tester

Validates that domain model types correctly serialize and deserialize when crossing the client/server boundary using the RemoteFactory two DI container pattern.

## Objective

For each new or modified domain model type that crosses the client/server boundary, verify complete serialization coverage using the `ClientServerContainers` pattern.

## Testing Pattern

Follow the established pattern from `src/Tests/FactoryGeneratorTests/Factory/RemoteWriteTests.cs`:

1. **Use ClientServerContainers.Scopes()**: Creates three isolated DI containers (client, server, local)
2. **Theory/MemberData**: Parameterize tests across all container types
3. **Full Round-Trip**: Serialize → Deserialize → Validate

## Validation Checklist

For each domain type, ensure tests cover:

### 1. Property Serialization
- [ ] All public properties serialize correctly
- [ ] Complex nested objects preserve structure
- [ ] Collections (List, IEnumerable, arrays) serialize with all elements
- [ ] Value objects maintain equality after round-trip

### 2. Type Preservation
- [ ] Concrete types remain correct after deserialization
- [ ] Interface properties deserialize to correct implementation
- [ ] Polymorphic properties preserve runtime type
- [ ] Nullable value types handle both null and non-null correctly

### 3. Edge Cases
- [ ] Null values serialize/deserialize correctly
- [ ] Empty collections (zero elements)
- [ ] Default/uninitialized values
- [ ] Large collections (performance check)
- [ ] Circular references (if applicable)

### 4. Domain Invariants
- [ ] Validation rules still apply after deserialization
- [ ] Business rule enforcement preserved
- [ ] Read-only properties remain immutable

## Test Structure Example

```csharp
[Theory]
[MemberData(nameof(ClientServerContainers.Scopes), MemberType = typeof(ClientServerContainers))]
public async Task SerializesDomainType_AllProperties(ContainerScope scope)
{
    // Arrange
    var factory = scope.GetRequiredService<IMyDomainFactory>();
    var original = await factory.CreateAsync();

    // Populate all properties with test data
    original.Property1 = "test";
    original.Property2 = 123;
    original.ComplexProperty = new ComplexType { /* ... */ };

    // Act - Round-trip through factory (triggers serialization)
    await factory.SaveAsync(original);
    var retrieved = await factory.GetAsync(original.Id);

    // Assert - Verify all properties preserved
    Assert.Equal(original.Property1, retrieved.Property1);
    Assert.Equal(original.Property2, retrieved.Property2);
    Assert.NotNull(retrieved.ComplexProperty);
    // ... validate all properties
}
```

## Analysis Process

When analyzing code changes:

1. **Identify affected types**: Scan for new/modified classes with `[Serializable]` or used in factory methods
2. **Locate existing tests**: Check `FactoryGeneratorTests/` for coverage
3. **Gap analysis**: Compare properties in domain type vs. test assertions
4. **Report findings**: List missing test coverage with specific properties

## Report Format

```markdown
## Serialization Coverage Report

### Fully Covered
- ✅ `Person` - All properties tested in `RemoteWriteTests.cs:42`

### Partial Coverage
- ⚠️ `Order` - Missing tests for:
  - `Order.ShippingAddress` (complex nested object)
  - `Order.LineItems` (collection serialization)
  - File: `src/Examples/OrderEntry/OrderEntry.Domain.Client/Order.cs:15-20`

### No Coverage
- ❌ `Customer` - No serialization tests found
  - Properties to test: `Name`, `Email`, `Addresses` (collection)
  - File: `src/Examples/OrderEntry/OrderEntry.Domain.Client/Customer.cs`

### Recommendations
1. Add `OrderSerializationTests.cs` with Theory/MemberData pattern
2. Test `ShippingAddress` nested serialization
3. Validate `LineItems` collection with 0, 1, and multiple elements
```

## Key Files to Review

- `src/Tests/FactoryGeneratorTests/ClientServerContainers.cs` - Container setup
- `src/Tests/FactoryGeneratorTests/FactoryTestBase.cs` - Base test class
- `src/Tests/FactoryGeneratorTests/Factory/RemoteWriteTests.cs` - Example pattern
- `src/RemoteFactory/Serialization/NeatooJsonSerializer.cs` - Serializer implementation

## Success Criteria

- Every domain type has explicit serialization tests
- All properties validated in round-trip tests
- Edge cases covered (null, empty, default values)
- Tests use `ClientServerContainers.Scopes()` pattern
- Test names clearly indicate what's being validated
