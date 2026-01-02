# Interface Collection Serialization Investigation

## Summary

Investigation into serialization support for interface-typed collections like `IList<IDomainObject>`. This is a unique value-add of RemoteFactory that is not possible with vanilla System.Text.Json.

## Background

System.Text.Json cannot deserialize `IList<ISomething>` out of the box because:
1. It doesn't know what concrete collection type to instantiate for `IList<T>`
2. It doesn't know what concrete type to use for each interface element

RemoteFactory solves this using:
- `$type`/`$value` wrappers for interface elements
- `IServiceAssemblies` to resolve types at runtime via DI

## Current Implementation

### Key Files
- `src/RemoteFactory/Internal/NeatooInterfaceJsonTypeConverter.cs` - Handles `$type`/`$value` deserialization
- `src/RemoteFactory/Internal/NeatooInterfaceJsonConverterFactory.cs` - Determines when to apply interface converter

### Known Limitation

In `NeatooInterfaceJsonConverterFactory.CanConvert()` (line 23):
```csharp
if ((typeToConvert.IsInterface || typeToConvert.IsAbstract) && !typeToConvert.IsGenericType && this.serviceAssemblies.HasType(typeToConvert))
```

The `!typeToConvert.IsGenericType` condition **excludes** generic interface types like `IList<T>`, `ICollection<T>`, etc.

### Expected Behavior Matrix

| Property Type | Expected Behavior | Status |
|--------------|-------------------|--------|
| `List<IDomainObject>` | Works - concrete collection, interface elements get `$type`/`$value` | Untested |
| `IList<IDomainObject>` | Unknown - excluded by `!IsGenericType` condition | Untested |
| `IDomainObject` (single) | Works - interface converter applies | Untested |
| `List<ConcreteType>` | Works - standard STJ handling | Tested |

## TODO

### Phase 1: Verify Current Behavior
- [ ] Create test for `List<IInterface>` (concrete collection of interface elements)
- [ ] Create test for `IList<IInterface>` (interface collection of interface elements)
- [ ] Create test for single interface property (`ICustomerModel Customer`)
- [ ] Document actual behavior vs expected behavior

### Phase 2: Fix if Needed
- [ ] If `IList<IInterface>` fails, extend converter factory to handle generic interface collections
- [ ] Consider handling for `ICollection<T>`, `IEnumerable<T>`, `IReadOnlyList<T>`, etc.

### Phase 3: Documentation
- [ ] Update `docs/advanced/json-serialization.md` with supported collection patterns
- [ ] Add examples showing interface collection serialization

## Test Pattern

Tests should use the two DI container approach (`ClientServerContainers.Scopes()`) to validate full serialization round-trips:

```csharp
// Example test structure
public class InterfaceCollectionSerializationTests
{
    [Theory]
    [MemberData(nameof(Factory_Client))]
    [MemberData(nameof(Factory_Local))]
    public async Task Property_ListOfInterfaces_SerializesCorrectly(ITestFactory factory)
    {
        // Create object with List<IChildModel> property
        // Verify children survive round-trip with correct concrete types
    }
}
```

## Related Files

- Existing serialization tests: `src/Tests/FactoryGeneratorTests/Factory/RecordSerializationTests.cs`
- Test infrastructure: `src/Tests/FactoryGeneratorTests/ClientServerContainers.cs`
- Documentation: `docs/advanced/json-serialization.md`
