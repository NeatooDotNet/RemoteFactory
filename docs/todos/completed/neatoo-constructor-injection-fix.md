# Fix Ordinal Serialization for Types with Constructor Dependencies

## Overview

RemoteFactory 10.2.0+ generates ordinal serialization code that uses object initializer syntax, which breaks for Neatoo domain objects that require constructor-injected services.

**Status:** READY FOR RELEASE (Fix implemented and tested)

**Blocking:** Neatoo cannot upgrade to RemoteFactory 10.4.0

**Priority:** HIGH

---

## Problem Statement

### Current Generated Code (Broken for Neatoo)

```csharp
// Generated FromOrdinalArray - uses object initializer
public static object FromOrdinalArray(object?[] values)
{
    return new ContactPhone  // ❌ FAILS - no parameterless constructor
    {
        Id = (System.Guid)values[0]!,
        PhoneNumber = (string?)values[1],
        PhoneType = (string?)values[2]
    };
}

// Generated JsonConverter.Read - same issue
return new ContactPhone
{
    Id = prop0!,
    PhoneNumber = prop1,
    PhoneType = prop2
};
```

### Neatoo Domain Object Pattern

All Neatoo domain objects require constructor-injected services:

```csharp
internal partial class ContactPhone : EntityBase<ContactPhone>, IContactPhone
{
    public ContactPhone(IEntityBaseServices<ContactPhone> services) : base(services) { }

    public partial Guid Id { get; set; }
    public partial string? PhoneNumber { get; set; }
    public partial string? PhoneType { get; set; }
}
```

### Root Cause

The ordinal serialization generator assumes types can be instantiated via:
1. Parameterless constructor with object initializer, OR
2. Primary constructor (records)

It doesn't account for types that:
- Have non-empty constructors with DI-resolved parameters
- Cannot be instantiated without a service provider

---

## Impact

- **97 build errors** in Neatoo when upgrading to RemoteFactory 10.4.0
- All `[Factory]` types that inherit from Neatoo base classes are affected
- Blocks adoption of new RemoteFactory features (CancellationToken support, logging)

---

## Solution Options

### Option A: Skip Ordinal Generation for Types with Non-Empty Constructors

**Approach:** Detect constructors with parameters and skip generating ordinal serialization interfaces/methods.

**Pros:**
- Simple to implement
- No breaking changes for existing POCOs
- Clear semantic: "ordinal serialization is for simple DTOs"

**Cons:**
- Neatoo types won't benefit from ordinal serialization performance
- Inconsistent behavior based on constructor signature

**Implementation:**
```csharp
// In OrdinalSerializationGenerator
private bool ShouldGenerateOrdinalSerialization(INamedTypeSymbol type)
{
    // Skip if any constructor has non-service parameters
    var constructors = type.Constructors.Where(c => !c.IsStatic);

    foreach (var ctor in constructors)
    {
        // Check if all parameters are [Service] attributed or have defaults
        var hasRequiredNonServiceParams = ctor.Parameters.Any(p =>
            !p.HasExplicitDefaultValue &&
            !p.GetAttributes().Any(a => a.AttributeClass?.Name == "ServiceAttribute"));

        if (hasRequiredNonServiceParams)
            return false;
    }

    return true;
}
```

### Option B: Use Factory Delegates for Instantiation (Recommended)

**Approach:** Generate ordinal deserialization that uses the existing factory infrastructure.

**Pros:**
- Neatoo types can use ordinal serialization
- Works with any constructor signature
- Consistent with RemoteFactory's factory pattern

**Cons:**
- More complex generator changes
- Requires `IServiceProvider` access during deserialization

**Implementation:**
```csharp
// Generated code uses factory delegate
public static object FromOrdinalArray(object?[] values, IServiceProvider services)
{
    var instance = services.GetRequiredService<ContactPhone>();
    instance.Id = (System.Guid)values[0]!;
    instance.PhoneNumber = (string?)values[1];
    instance.PhoneType = (string?)values[2];
    return instance;
}

// JsonConverter uses DI-aware pattern
internal sealed class ContactPhoneOrdinalConverter : JsonConverter<ContactPhone>
{
    public override ContactPhone? Read(...)
    {
        // Get from JsonSerializerOptions or context
        var services = options.GetServiceProvider();
        var instance = services.GetRequiredService<ContactPhone>();

        // ... read properties and set them
        instance.Id = prop0!;
        instance.PhoneNumber = prop1;
        instance.PhoneType = prop2;

        return instance;
    }
}
```

### Option C: Attribute to Opt-Out

**Approach:** Add `[SuppressOrdinalSerialization]` attribute for types that can't use it.

**Pros:**
- Explicit opt-out
- No detection logic needed

**Cons:**
- Requires Neatoo to add attribute to all base classes or teach users to add it
- Doesn't solve the problem, just hides it

---

## Recommended Solution: Option A (Short-term) + Option B (Long-term)

### Phase 1: Skip Generation (Immediate Fix)

Implement Option A to unblock Neatoo upgrade. This is safe and non-breaking.

- [x] Detect non-empty constructors in ordinal generator
- [x] Skip `IOrdinalSerializable` generation for these types
- [x] Skip `FromOrdinalArray` generation
- [x] Skip `JsonConverter` generation
- [x] Add tests for detection logic (12 tests in ConstructorInjectionTests.cs)
- [ ] Release as patch (10.4.1)

### Phase 2: DI-Aware Serialization (Future)

Implement Option B for full ordinal serialization support with DI.

- [ ] Design `IServiceProvider` integration in JsonConverter
- [ ] Update generator to use factory pattern
- [ ] Add tests for DI-resolved types
- [ ] Release as minor (10.5.0)

---

## Detection Logic

A constructor requires "special instantiation" if ANY of these are true:

1. **Has parameters without defaults that aren't `[Service]`**
   ```csharp
   // Needs DI - can't use object initializer
   public Person(IPersonServices services) : base(services) { }
   ```

2. **Inherits from a base with non-empty constructor**
   ```csharp
   // Base requires services
   public class Person : EntityBase<Person> { }
   ```

3. **Is abstract or interface** (can't be instantiated directly)

### Types That CAN Use Object Initializer

```csharp
// Parameterless constructor
public class PersonDto { }

// Record with primary constructor (generated)
public record Person(string Name, int Age);

// All parameters have defaults
public class Config(string env = "dev") { }
```

---

## Additional Fix: IMakeRemoteDelegateRequest CancellationToken

RemoteFactory 10.4.0 added `CancellationToken` parameter to interface methods:

```csharp
// Old (Neatoo implements this)
Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters);

// New (10.4.0)
Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters, CancellationToken token);
```

### Fix

This is a breaking interface change. Options:

1. **Default parameter** (if C# supports on interface) - NOT SUPPORTED
2. **Overload with old signature** - Add to interface
3. **Extension method** - Can't override interface method

**Recommended:** Add overload methods that call the new signature with `default`:

```csharp
public interface IMakeRemoteDelegateRequest
{
    // New method with token
    Task<T> ForDelegate<T>(Type delegateType, object?[]? parameters, CancellationToken token);
    Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters, CancellationToken token);

    // Backward compatibility - call new method with default token
    Task<T> ForDelegate<T>(Type delegateType, object?[]? parameters)
        => ForDelegate<T>(delegateType, parameters, default);
    Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters)
        => ForDelegateNullable<T>(delegateType, parameters, default);
}
```

- [x] Add overload methods with default implementation
- [ ] Update Neatoo.UnitTest to implement new signatures
- [ ] Test both old and new calling patterns

---

## Tasks

### Ordinal Serialization Fix

- [x] Add constructor analysis to `OrdinalSerializationGenerator`
- [x] Skip generation for types with non-trivial constructors
- [x] Add unit tests for constructor detection (ConstructorInjectionTests.cs)
- [x] Add integration test with Neatoo-style types (CtorTestNeatooStyleEntity)
- [ ] Update documentation

### Interface Compatibility Fix

- [x] Add backward-compatible overloads to `IMakeRemoteDelegateRequest`
- [x] Backward compat via default interface implementations
- [x] All existing tests pass

### Release

- [x] Bump version to 10.5.0
- [x] Update release notes
- [x] Add AOT compatibility documentation
- [ ] Publish to NuGet
- [ ] Verify Neatoo builds with new package

---

## Test Cases

### Constructor Detection

| Type | Should Generate Ordinal? |
|------|-------------------------|
| `class Dto { }` | Yes |
| `record Person(string Name)` | Yes |
| `class Entity(IServices s) : Base(s)` | No |
| `class Configured(string env = "dev")` | Yes |
| `abstract class BaseEntity { }` | No |

### Backward Compatibility

```csharp
// Existing code must continue to work
await remoteDelegateRequest.ForDelegate<Person>(typeof(CreatePerson), args);

// New code can use cancellation
await remoteDelegateRequest.ForDelegate<Person>(typeof(CreatePerson), args, cts.Token);
```

---

## Files to Modify

| File | Change |
|------|--------|
| `src/Generator/OrdinalSerializationGenerator.cs` | Add constructor detection |
| `src/RemoteFactory/IMakeRemoteDelegateRequest.cs` | Add overload methods |
| `src/Tests/OrdinalSerializationTests.cs` | Add detection tests |
| `docs/release-notes/v10.4.1.md` | Document fixes |

---

## Success Criteria

- [ ] Neatoo solution builds with RemoteFactory 10.4.1+
- [ ] All existing RemoteFactory tests pass
- [ ] All existing Neatoo tests pass
- [ ] No breaking changes for existing POCOs using ordinal serialization
- [ ] Backward-compatible interface methods work
