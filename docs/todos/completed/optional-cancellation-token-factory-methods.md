# Optional CancellationToken on Generated Factory Methods

**Priority:** High
**Category:** Feature Enhancement
**Created:** 2026-01-11
**Status:** Not Started

## Problem

When a domain method has a required CancellationToken parameter:

```csharp
[Insert]
public async Task Insert([Service] IDbContext db, CancellationToken cancellationToken)
{
    await db.SaveChangesAsync(cancellationToken);
}
```

The generated factory's SaveDelegate requires CancellationToken:

```csharp
services.AddScoped<SaveDelegate>(cc =>
{
    var factory = cc.GetRequiredService<OrderFactory>();
    return (IOrder target, CancellationToken cancellationToken) => factory.LocalSave(target, cancellationToken);
});
```

This breaks Neatoo's `EntityBase.Save()` method (no token) which internally calls the factory save. The entity can't satisfy the required CancellationToken.

**Current workaround:** Users must add `= default` to their domain method CancellationToken parameters.

## CancellationToken Is a Special Case

**Important:** This feature introduces a deliberate deviation from RemoteFactory's normal signature-matching behavior.

### Normal Behavior: Signature Matching

Normally, the generated `Save` method signature exactly mirrors the union of parameters from `Insert`, `Update`, and `Delete` domain methods. The factory acts as a pass-through - what you define on the domain method is what appears on the factory.

### Special Case: CancellationToken Always Present

CancellationToken breaks this rule. The generated factory methods **always** accept an optional CancellationToken, regardless of whether the domain method declares one:

- **Create/Fetch methods**: Always have `CancellationToken cancellationToken = default`
- **Save method**: Always has `CancellationToken cancellationToken = default`
- **Insert/Update/Delete (Local methods)**: Always accept the token from Save

### Why This Special Case Exists

1. **"Optional" means optional for the caller, not optional for functionality.** If a consumer passes a CancellationToken, it MUST be honored - cancelling the HTTP request, aborting server-side work (to the extent possible), and propagating through the infrastructure.

2. **The token is always meaningful.** Even when the domain method doesn't accept a CancellationToken, the token still:
   - Cancels the HTTP request on the client
   - Triggers `HttpContext.RequestAborted` on the server
   - Short-circuits factory infrastructure before/after the domain method call
   - Invokes `IFactoryOnCancelled` lifecycle hooks

3. **Consumer control matters.** The caller of the factory should always have the ability to cancel operations. Whether the domain method chooses to observe the token is a separate concern from whether the caller can request cancellation.

## Proposed Solution

Generated factory methods should **always** accept an optional CancellationToken (`= default`), regardless of whether the domain method requires one, has it optional, or omits it entirely.

### Domain → Factory Signature Transformation

| Domain method signature | Generated factory signature |
|-------------------------|----------------------------|
| `Insert(CancellationToken ct)` (required) | `Save(..., CancellationToken ct = default)` (optional) |
| `Insert(CancellationToken ct = default)` (optional) | `Save(..., CancellationToken ct = default)` (optional) |
| `Insert()` (no token) | `Save(..., CancellationToken ct = default)` (optional) |

The domain method's token requirement is an internal detail. The factory always provides a consistent optional token API.

### Generated Interface

```csharp
public interface IOrderFactory
{
    Task<IOrder?> Fetch(CancellationToken cancellationToken = default);
    Task<IOrder?> Save(IOrder target, CancellationToken cancellationToken = default);
    // ...
}
```

### Generated Implementation

The token is always accepted. Whether it's passed to the domain method depends on the domain method's signature:

```csharp
// If domain method HAS CancellationToken parameter - pass it through
public async Task<Authorized<IOrder>> LocalInsert(IOrder target, CancellationToken cancellationToken)
{
    return await DoFactoryMethodCallAsync(cTarget, FactoryOperation.Insert,
        () => cTarget.Insert(dbContext, cancellationToken));  // Token reaches domain
}

// If domain method does NOT have CancellationToken - token still used by infrastructure
public async Task<Authorized<IOrder>> LocalInsert(IOrder target, CancellationToken cancellationToken)
{
    // cancellationToken is still passed to DoFactoryMethodCallAsync for:
    // - Early cancellation checks
    // - HTTP layer cancellation (remote operations)
    // - IFactoryOnCancelled callbacks
    return await DoFactoryMethodCallAsync(cTarget, FactoryOperation.Insert,
        () => cTarget.Insert(dbContext));  // Domain doesn't see token, but infra does
}
```

### SaveDelegate Registration

```csharp
services.AddScoped<SaveDelegate>(cc =>
{
    var factory = cc.GetRequiredService<OrderFactory>();
    return (IOrder target, CancellationToken cancellationToken) => factory.LocalSave(target, cancellationToken);
});
```

The delegate always accepts the token. The token flows through the factory infrastructure even when the domain method doesn't accept it.

## Token Flow Summary

| Scenario | Factory accepts token? | HTTP honors token? | Domain receives token? |
|----------|----------------------|-------------------|----------------------|
| Domain has `CancellationToken` param | Yes (always) | Yes | Yes |
| Domain has no `CancellationToken` param | Yes (always) | Yes | No |
| Caller passes `default` | Yes (always) | N/A (not cancelled) | Depends on domain |

## Benefits

1. **Consistent API** - Factory methods always have the same signature pattern
2. **Consumer control** - Callers can always request cancellation
3. **Full infrastructure support** - Token honored at HTTP and factory layers regardless of domain method
4. **Backward compatible** - Existing domain methods without CancellationToken continue to work
5. **Opt-in depth** - Domain methods can choose whether to observe cancellation internally

## Implementation Notes

In the generator, when building the method call expression:
1. Always generate factory methods with `CancellationToken cancellationToken = default`
2. Always pass `cancellationToken` to `DoFactoryMethodCallAsync` (or equivalent)
3. Check if the domain method has a CancellationToken parameter
4. If yes, include `cancellationToken` in the domain method invocation
5. If no, omit it from the domain method invocation (but infrastructure still uses it)

## Test Cases

- [ ] Domain method with required CancellationToken - token passed to domain
- [ ] Domain method with optional CancellationToken (`= default`) - token passed to domain
- [ ] Domain method without CancellationToken - infrastructure uses token, domain doesn't receive it
- [ ] Factory.Save() called without token - uses default, no cancellation
- [ ] Factory.Save(token) with cancellation - HTTP cancelled even if domain doesn't take token
- [ ] Remote operation cancelled - server receives RequestAborted even if domain doesn't take token
- [ ] EntityBase.Save() (no token) works with generated factory
- [ ] IFactoryOnCancelled invoked when token cancelled (domain method has no token param)

## References

- Discovered in Neatoo while adding CancellationToken to Person example
- Related: `docs/todos/completed/cancellation-token-support.md`
