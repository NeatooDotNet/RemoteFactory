# What if they try to define params in their factory method?

**Priority:** Low
**Category:** Investigation
**Created:** 2026-01-12
**Status:** Completed ✅

## Question

What happens if a user defines a factory method with a `params` parameter?

```csharp
[Fetch]
public void Fetch(params int[] ids)
{
    // ...
}
```

## Investigation Results

- [x] Does the generator handle this correctly? **YES**
- [x] Does serialization work for params arrays? **YES**
- [x] Should this be explicitly supported, blocked, or documented? **SUPPORTED**

## Implementation Summary

The generator now fully supports `params` parameters. The implementation:

1. **Detects `params` modifier** via `MethodParameterInfo.IsParams` property
2. **Reorders parameters** so CancellationToken comes BEFORE `params` (C# requirement)
3. **Preserves `params` modifier** in generated interfaces, delegates, and methods

### Generated Signature Pattern

```csharp
// Domain method:
[Create]
public void Create(params int[] ids) { }

// Generated factory interface:
void Create(CancellationToken cancellationToken = default, params int[] ids);
```

### Calling the Factory

Due to C# rules, when an optional parameter comes before `params`, you must explicitly provide it:

```csharp
// Valid calls:
factory.Create(default, 1, 2, 3);           // Pass 'default' for CT
factory.Create(myToken, 1, 2, 3);           // Pass explicit CT
factory.Create();                            // No params (empty array)

// Invalid (won't compile):
factory.Create(1, 2, 3);  // C# tries to bind first arg to CancellationToken
```

### Files Changed

- `FactoryGenerator.Types.cs`: Added `IsParams` property, updated parameter ordering methods
- `ParamsParameterTests.cs`: Added comprehensive tests for params support

### Tests

12 tests covering:
- Local execution with variadic syntax
- Remote execution with serialization
- Mixed params (regular + params parameters)
- Empty params arrays
- Explicit CancellationToken passing
- CancellationToken flow when domain method has BOTH CT and params
