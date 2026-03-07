# Exploration: IL Trimming Feature Switches for Remote-Only Code Separation

**Date:** 2026-03-03
**Related Todo:** [Explore IL Trimming Feature Switches](../todos/completed/explore-trimming-remote-only.md)
**Status:** Complete
**Last Updated:** 2026-03-07

---

## Overview

This is an **exploration/feasibility plan**, not a production feature implementation. The goal is to determine whether .NET 9+'s `[FeatureSwitchDefinition]` attribute can be used in RemoteFactory's generated factory code to allow the IL trimmer to remove server-only methods and their transitive dependencies (e.g., EF Core, repository implementations) from Blazor WASM published output -- without requiring developers to split assemblies.

The exploration must answer four key questions from the todo:
1. Can the generator emit `if (NeatooRuntime.IsServerRuntime)` guards around server-only method calls?
2. Does the trimmer reliably remove guarded code AND its transitive dependencies?
3. Does anything in Neatoo's base classes (virtual methods, interface dispatch) defeat member-level trimming?
4. What is the minimum .NET version requirement?

---

## Business Rules (Observable Trimming Behaviors)

These are framed as observable outcomes rather than traditional business rules, since this is a trimming feasibility study. Each assertion describes what the IL trimmer SHOULD do if the approach is viable.

### Feature Switch Foundation

1. WHEN a static boolean property `NeatooRuntime.IsServerRuntime` is annotated with `[FeatureSwitchDefinition("Neatoo.RemoteFactory.IsServerRuntime")]` AND the application sets `<RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime" Value="false" Trim="true" />`, THEN the IL trimmer treats `NeatooRuntime.IsServerRuntime` as the constant `false`.

2. WHEN `NeatooRuntime.IsServerRuntime` is treated as `false` by the trimmer, THEN code inside `if (NeatooRuntime.IsServerRuntime) { ... }` blocks is recognized as dead code and removed from the published output.

### Generated Code Guards -- Class Factory Pattern

3. WHEN a generated class factory's `LocalCreate` method wraps its entity method invocation (e.g., `target.Create(...)`) and service resolution (e.g., `GetRequiredService<IOrderRepository>()`) inside an `if (NeatooRuntime.IsServerRuntime)` guard, AND `IsServerRuntime=false`, THEN the `LocalCreate` method body becomes empty (or throws), and the trimmer removes references to the entity's `[Remote]` method and its `[Service]` parameter types from the published assembly.

4. WHEN a generated class factory's `LocalInsert`, `LocalUpdate`, and `LocalDelete` methods (Write methods, only generated in `FactoryMode.Full`) are similarly guarded, AND `IsServerRuntime=false`, THEN the trimmer removes references to server-only services like `IOrderRepository` from the published assembly.

5. WHEN a generated class factory's `LocalSave` method calls `LocalInsert`/`LocalUpdate`/`LocalDelete` inside an `if (NeatooRuntime.IsServerRuntime)` guard, AND `IsServerRuntime=false`, THEN the trimmer removes the Save routing logic and its transitive dependency chain.

### Generated Code Guards -- Static Factory Pattern

6. WHEN a generated static factory's local delegate registration (the lambda that calls the private `_MethodName(...)` method with resolved services) is wrapped in an `if (NeatooRuntime.IsServerRuntime)` guard, AND `IsServerRuntime=false`, THEN the trimmer removes the reference to the private static method and its server-only `[Service]` parameter types.

### Generated Code Guards -- Interface Factory Pattern

7. WHEN a generated interface factory's `LocalMethodName` method is wrapped in an `if (NeatooRuntime.IsServerRuntime)` guard, AND `IsServerRuntime=false`, THEN the trimmer removes the local method body and its dependency on the server-side implementation type.

### Transitive Dependency Removal

8. WHEN all call sites that reference a server-only type (e.g., `IOrderRepository`, `DbContext`) are removed by the trimmer via dead-code elimination, AND no other reachable code references that type, THEN the server-only type and its own dependencies are also removed from the published output.

### Virtual Method / Interface Dispatch Concern

9. WHEN `FactoryBase<T>.DoFactoryMethodCall(...)` is virtual AND the generated factory class overrides or calls it with a lambda containing server-only code, THEN the trimmer MAY preserve the lambda body because virtual dispatch prevents static dead-code analysis. This would defeat the trimming goal. **This is a key risk to validate.**

10. WHEN `FactoryCore<T>` implements `IFactoryCore<T>` with virtual methods, AND the factory lambda is passed through this interface, THEN the trimmer MAY preserve the lambda regardless of the feature switch guard placement. **This is the second key risk.**

### Backward Compatibility

11. WHEN `NeatooRuntime.IsServerRuntime` is not configured (no `RuntimeHostConfigurationOption` in the project), THEN the property returns `true` at runtime (via `AppContext.TryGetSwitch` defaulting to `true`), and all code paths execute normally -- no behavioral change.

12. WHEN the feature switch approach is implemented, AND the application does NOT use trimming (`PublishTrimmed=false`), THEN the runtime behavior is identical to the current behavior -- the `if` guards always evaluate to `true` and the JIT compiles the full code path.

### Inline Pipeline (FactoryBase Redesign)

14. WHEN the generator inlines the `FactoryCore<T>` pipeline logic directly into generated `LocalMethod` bodies (no `DoFactoryMethodCall` call, no lambda capture, no virtual dispatch), AND the method body is wrapped in `if (NeatooRuntime.IsServerRuntime)`, AND `IsServerRuntime=false`, THEN the trimmer can statically determine all type references within the guard are dead code and remove them, because the entire call graph is linear with no opaque dispatch boundaries.

15. WHEN the generator inlines the pipeline and the entity implements `IFactoryOnStart` or `IFactoryOnComplete`, THEN the inline code performs `is IFactoryOnStart` / `is IFactoryOnComplete` type checks directly (same as `FactoryCore<T>` does today), preserving identical lifecycle hook behavior without virtual dispatch.

16. WHEN the inline pipeline redesign is adopted, THEN `IFactoryCore<T>` extensibility (custom DI registration of `IFactoryCore<T>` for per-type interception) is no longer available. Users who need per-type before/after behavior should implement `IFactoryOnStart` / `IFactoryOnComplete` / `IFactoryOnCancelled` on their entities instead.

### FactoryMode.RemoteOnly Interaction

13. WHEN an assembly uses `[assembly: FactoryMode(FactoryMode.RemoteOnly)]`, THEN `LocalMethod` methods are NOT generated at all (current behavior). The feature switch approach targets `FactoryMode.Full` assemblies where Local methods exist but should be trimmable. These two mechanisms are complementary, not competing.

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Feature switch property basics | `NeatooRuntime.IsServerRuntime` with switch set to `false`, `Trim="true"` | Rule 1, 2 | Property treated as constant `false`; guarded branches removed from IL |
| 2 | Class factory Create trimming | `Order` with `[Remote, Create]` method taking `[Service] IOrderLineListFactory`; published with `IsServerRuntime=false` | Rule 3, 8 | `LocalCreate` body trimmed; `IOrderLineListFactory` removed if unreferenced elsewhere |
| 3 | Class factory Save/Write trimming | `Order` with `[Remote, Insert]` taking `[Service] IOrderRepository`; published with `IsServerRuntime=false` | Rule 4, 5, 8 | `LocalInsert`/`LocalUpdate`/`LocalDelete` bodies trimmed; `IOrderRepository` removed |
| 4 | Static factory Execute trimming | Static class with `[Remote, Execute]` private method taking `[Service] IMyService`; published with `IsServerRuntime=false` | Rule 6, 8 | Local delegate registration trimmed; `IMyService` removed if unreferenced |
| 5 | Interface factory Local trimming | `[Factory] interface IMyRepository` with `GetAllAsync()`; published with `IsServerRuntime=false` | Rule 7, 8 | `LocalGetAllAsync` body trimmed; server implementation type removed |
| 6 | Virtual method defeats trimming (original architecture) | Same as Scenario 2 but lambda passes through `DoFactoryMethodCall` virtual chain | Rule 9, 10 | **Risk scenario**: trimmer may NOT remove lambda body. Prototype must verify. |
| 7 | No feature switch configured | Same code, but no `RuntimeHostConfigurationOption` in project file | Rule 11, 12 | Full runtime behavior preserved; `IsServerRuntime` returns `true`; all code paths execute |
| 8 | RemoteOnly mode unaffected | Assembly with `[assembly: FactoryMode(FactoryMode.RemoteOnly)]` | Rule 13 | No Local methods generated; feature switch is irrelevant for this mode |
| 9 | Inline pipeline Create trimming | Same as Scenario 2 but with inlined pipeline (no DoFactoryMethodCall), `IsServerRuntime=false` | Rule 14, 8 | `LocalCreate` body trimmed; all server-only types removed. No virtual dispatch to defeat the trimmer. |
| 10 | Inline pipeline with lifecycle hooks | Entity implements `IFactoryOnCompleteAsync`; inlined pipeline includes `is IFactoryOnCompleteAsync` check; `IsServerRuntime=true` | Rule 15 | Lifecycle hooks invoked correctly, identical behavior to current `FactoryCore<T>` pipeline |
| 11 | IFactoryCore extensibility removed | User registers custom `IFactoryCore<Order>` in DI with inline pipeline redesign | Rule 16 | Custom `IFactoryCore<Order>` is ignored; user must use lifecycle hook interfaces instead |

---

## Approach

### Strategy: Feature-Switch-Guarded Generated Code

The core approach is:

1. **Add `NeatooRuntime` class** to the `Neatoo.RemoteFactory` library with a `[FeatureSwitchDefinition]`-annotated static boolean property.

2. **Modify the source generator** to emit `if (NeatooRuntime.IsServerRuntime)` guards around server-only code in `LocalMethod` bodies.

3. **Build a trimming verification project** that publishes a Blazor WASM app with the feature switch set to `false`, then inspects the output to verify server-only types are absent.

### Why Feature Switch (Not RemoteOnly Mode)

`FactoryMode.RemoteOnly` already skips generating Local methods entirely in client assemblies. But the problem described in the todo is about **shared assemblies** -- a single Domain assembly referenced by both client and server. In this case, the assembly needs `FactoryMode.Full` (server needs Local methods), but the client publish should be able to trim them away.

The feature switch bridges this gap: generate all code, but let the trimmer remove server-only paths at publish time.

---

## Design

### 1. NeatooRuntime Class (Library Addition)

```csharp
// In src/RemoteFactory/NeatooRuntime.cs
namespace Neatoo.RemoteFactory;

public static class NeatooRuntime
{
    [FeatureSwitchDefinition("Neatoo.RemoteFactory.IsServerRuntime")]
    public static bool IsServerRuntime =>
        AppContext.TryGetSwitch("Neatoo.RemoteFactory.IsServerRuntime", out bool isEnabled)
            ? isEnabled
            : true; // Default: server runtime (no behavioral change without explicit opt-in)
}
```

**Key decisions:**
- Default to `true` (server mode) so existing apps are unaffected
- The switch name `Neatoo.RemoteFactory.IsServerRuntime` follows .NET convention
- `FeatureSwitchDefinitionAttribute` is only available in .NET 9+, requiring conditional compilation:
  - net8.0: Property exists but without the attribute; trimming won't optimize it
  - net9.0+: Attribute applied; trimmer can optimize

### 2. Generator Changes: Guard Placement

The generator currently produces Local methods like:

```csharp
public Task<Order> LocalCreate(string customerName, CancellationToken cancellationToken = default)
{
    var target = ServiceProvider.GetRequiredService<Order>();
    var lineListFactory = ServiceProvider.GetRequiredService<IOrderLineListFactory>();
    return Task.FromResult(DoFactoryMethodCall(target, FactoryOperation.Create,
        () => target.Create(customerName, lineListFactory)));
}
```

With the feature switch guard, it would become:

```csharp
public Task<Order> LocalCreate(string customerName, CancellationToken cancellationToken = default)
{
    if (!NeatooRuntime.IsServerRuntime)
        throw new InvalidOperationException("Server-only method called in non-server runtime.");

    var target = ServiceProvider.GetRequiredService<Order>();
    var lineListFactory = ServiceProvider.GetRequiredService<IOrderLineListFactory>();
    return Task.FromResult(DoFactoryMethodCall(target, FactoryOperation.Create,
        () => target.Create(customerName, lineListFactory)));
}
```

**Critical concern: Lambda capture through virtual dispatch.** The call to `DoFactoryMethodCall(target, operation, () => target.Create(...))` passes a lambda through a virtual method chain (`FactoryBase<T>.DoFactoryMethodCall` -> `IFactoryCore<T>.DoFactoryMethodCall`). Virtual dispatch creates an opaque boundary for the trimmer -- it cannot determine at trim time whether the lambda's body will execute.

**Two possible guard placement strategies to prototype:**

**Strategy A: Guard at the top of LocalMethod (shown above)**
- Simple change, minimal generator modification
- Risk: Trimmer may still preserve the lambda body because it appears in the method's IL regardless of the `if` guard
- If the trimmer does constant-fold the `if` and removes the entire method body, this works

**Strategy B: Guard around individual service resolutions and the method call separately**
```csharp
public Task<Order> LocalCreate(string customerName, CancellationToken cancellationToken = default)
{
    if (NeatooRuntime.IsServerRuntime)
    {
        var target = ServiceProvider.GetRequiredService<Order>();
        var lineListFactory = ServiceProvider.GetRequiredService<IOrderLineListFactory>();
        return Task.FromResult(DoFactoryMethodCall(target, FactoryOperation.Create,
            () => target.Create(customerName, lineListFactory)));
    }
    throw new InvalidOperationException("Server-only method called in non-server runtime.");
}
```
- More explicit about the dead-code boundary
- Same virtual dispatch concern for the lambda

**Strategy C: Avoid passing through FactoryBase virtual methods entirely in guarded path**
```csharp
public Task<Order> LocalCreate(string customerName, CancellationToken cancellationToken = default)
{
    if (NeatooRuntime.IsServerRuntime)
    {
        var target = ServiceProvider.GetRequiredService<Order>();
        var lineListFactory = ServiceProvider.GetRequiredService<IOrderLineListFactory>();
        return Task.FromResult(DoFactoryMethodCall(target, FactoryOperation.Create,
            () => target.Create(customerName, lineListFactory)));
    }
    // When not server runtime, just make the remote call
    return RemoteCreate(customerName, cancellationToken);
}
```
- Interesting alternative: fall through to Remote when not server
- But this changes LocalMethod semantics (currently Local always executes locally)
- Would need careful analysis for side effects

**The prototype must test Strategy A first** (simplest) and fall back to alternatives only if the trimmer doesn't properly eliminate the guarded code.

### 3. Virtual Method Dispatch Risk Analysis

Current architecture:

```
Generated OrderFactory.LocalCreate(...)
  -> FactoryBase<Order>.DoFactoryMethodCall(target, op, () => target.Create(...))  [virtual]
    -> IFactoryCore<Order>.DoFactoryMethodCall(target, op, lambda)  [interface dispatch]
      -> FactoryCore<Order>.DoFactoryMethodCall(target, op, lambda)  [virtual]
        -> lambda.Invoke()  // Actually calls target.Create(...)
```

**Risk**: The IL trimmer uses static analysis. Virtual methods and interface implementations are typically preserved because the trimmer cannot determine at trim-time which concrete type will be resolved. The lambda containing `target.Create(customerName, lineListFactory)` is captured as a `Func<>` delegate and passed into this virtual chain. Even if the `if (NeatooRuntime.IsServerRuntime)` guard removes the call to `DoFactoryMethodCall`, the trimmer needs to determine that the lambda's referenced types (`IOrderLineListFactory`) are unreachable.

**Mitigation approaches to explore in prototype:**
- `[DynamicallyAccessedMembers]` annotations to help the trimmer
- `[RequiresUnreferencedCode]` to suppress warnings
- Sealed classes or devirtualization hints
- Whether the trimmer constant-folds the feature switch BEFORE analyzing reachability (which would make the guard sufficient)

### 4. Minimum .NET Version

- `[FeatureSwitchDefinitionAttribute]` was introduced in .NET 9
- IL trimmer support for `[FeatureSwitchDefinition]` requires .NET 9+ SDK
- .NET 8 apps cannot use this feature (the attribute doesn't exist in BCL)

**Conditional compilation approach:**
```csharp
#if NET9_0_OR_GREATER
    [FeatureSwitchDefinition("Neatoo.RemoteFactory.IsServerRuntime")]
#endif
    public static bool IsServerRuntime => ...
```

This means:
- net8.0: Feature switch is a no-op; property is a regular boolean. Trimming won't remove guarded code.
- net9.0+: Full trimmer integration.

### 5. Trimming Verification Project

Create a test project that:
1. Defines a domain class with `[Factory]`, `[Remote]` methods, and server-only `[Service]` dependencies
2. References a "server-only" package/type (simulating EF Core or a repository)
3. Publishes as Blazor WASM with:
   ```xml
   <PublishTrimmed>true</PublishTrimmed>
   <RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime"
                                    Value="false" Trim="true" />
   ```
4. Inspects the published output using `dotnet-ildasm` or assembly size analysis
5. Verifies the server-only types are NOT present in the trimmed output

---

## Implementation Steps

### Phase 1: NeatooRuntime Class + Minimal Generator Change (Prototype)

1. Add `NeatooRuntime.cs` to `src/RemoteFactory/` with the feature switch property
2. Modify `ClassFactoryRenderer.RenderReadLocalMethod()` to wrap the method body in `if (NeatooRuntime.IsServerRuntime)` guard
3. Modify `ClassFactoryRenderer.RenderLocalMethod()` (for Write methods) similarly
4. Modify `ClassFactoryRenderer.RenderSaveLocalMethod()` similarly
5. Verify existing tests still pass (guards default to `true`)

### Phase 2: Trimming Verification Project

1. Create `src/Tests/RemoteFactory.TrimmingTests/` (or similar) as a Blazor WASM project
2. Define a simple domain class with `[Factory]`, `[Remote, Create]`, and a server-only `[Service]` dependency
3. Define a "server-only" type that the `[Service]` parameter references
4. Configure the project with `PublishTrimmed=true` and the feature switch
5. Publish and inspect the output to see if server-only types are removed

### Phase 3: Virtual Dispatch Investigation

1. If Phase 2 shows the trimmer preserves server-only types through virtual dispatch:
   a. Try sealing `FactoryCore<T>` or specific methods
   b. Try alternative guard placement strategies (B, C from Design section)
   c. Try `[DynamicDependency]` or trimmer XML configuration
   d. Document which approach (if any) allows full transitive trimming

### Phase 4: Extend to All Factory Patterns (If Phases 1-3 Succeed)

1. Apply guards to `StaticFactoryRenderer` local delegate registrations
2. Apply guards to `InterfaceFactoryRenderer.RenderLocalMethod()`
3. Verify trimming works for all three patterns
4. Document findings and any pattern-specific limitations

### Phase 5: Documentation and Decision

1. Document prototype results
2. Answer the four key questions from the todo
3. Recommend: proceed to production feature, modify approach, or abandon
4. If proceeding, create a separate production implementation plan

---

## Acceptance Criteria

- [ ] `NeatooRuntime.IsServerRuntime` property exists with `[FeatureSwitchDefinition]` on net9.0+
- [ ] Generator emits feature-switch-guarded Local methods for class factories
- [ ] All existing tests pass with the guarded code (guards default to `true`)
- [ ] Trimming verification project demonstrates whether server-only types are removed
- [ ] Virtual method dispatch risk is characterized (does it defeat trimming or not?)
- [ ] All four key questions from the todo are answered with evidence
- [ ] Findings documented with specific trimmer output evidence

---

## Dependencies

- .NET 9+ SDK (for `[FeatureSwitchDefinitionAttribute]` and trimmer integration)
- Blazor WASM project template (for trimming verification)
- `dotnet publish` with trimming enabled (for output inspection)
- Potentially `ILSpy` or `dotnet-ildasm` for assembly inspection

---

## Risks / Considerations

### High Risk: Virtual Method Dispatch Defeating Trimming (Rules 9, 10)

The core architecture passes entity method lambdas through `FactoryBase<T>` virtual methods and `IFactoryCore<T>` interface dispatch. This creates an opaque boundary for the trimmer. If the trimmer cannot see through this indirection to determine that the lambda body is dead code, then the feature switch approach may be insufficient for transitive dependency removal.

**Mitigation:** The prototype's primary job is to test this. If virtual dispatch defeats trimming, the architecture may need:
- A trimmer-friendly code path that bypasses the virtual chain when guarded
- Devirtualization via sealed classes
- Alternative approaches (compile-time elimination via MSBuild targets instead of trimmer)

### Medium Risk: net8.0 Support Limitation (Rule 1)

`[FeatureSwitchDefinitionAttribute]` is .NET 9+ only. net8.0 applications cannot benefit from this feature. This is acceptable since:
- net8.0 reaches end of support November 2026
- The feature switch is an optimization, not a correctness requirement
- net8.0 users can still use `FactoryMode.RemoteOnly` for assembly splitting

### Low Risk: Backward Compatibility (Rules 11, 12)

The default value of `IsServerRuntime` is `true`, so no existing behavior changes. Applications that don't configure the feature switch get identical behavior. This is inherently backward-compatible.

### Low Risk: FactoryMode.RemoteOnly Interaction (Rule 13)

`RemoteOnly` assemblies don't generate Local methods at all, so the feature switch guards have nothing to wrap. The two mechanisms are complementary: `RemoteOnly` is a compile-time decision; feature switches are a publish-time optimization.

### Consideration: Generated Code Size

Adding `if (NeatooRuntime.IsServerRuntime)` guards to every Local method increases the generated code size slightly. For the prototype, this is irrelevant. For a production implementation, the impact should be measured.

### Consideration: Trimming Warnings

The existing RemoteFactory codebase uses reflection in `HandleRemoteDelegateRequest` (e.g., `DynamicInvoke`, `GetProperty`). These will produce trimmer warnings. The prototype should note any warnings but does not need to resolve them -- they're a separate concern from the feature switch feasibility.

---

## Architectural Verification

**Scope Table:**

| Component | Affected by Feature Switch? | Current State |
|-----------|----------------------------|---------------|
| `NeatooRuntime` class | New -- must be created | Does not exist |
| `ClassFactoryRenderer` Local methods | Yes -- guards needed | Generates without guards |
| `ClassFactoryRenderer` Write methods | Yes -- guards needed | Generates without guards |
| `ClassFactoryRenderer` Save methods | Yes -- guards needed | Generates without guards |
| `StaticFactoryRenderer` local registrations | Yes (Phase 4) | Generates without guards |
| `InterfaceFactoryRenderer` local methods | Yes (Phase 4) | Generates without guards |
| `FactoryBase<T>` | Risk: virtual dispatch | 7 virtual methods |
| `FactoryCore<T>` / `IFactoryCore<T>` | Risk: interface + virtual | Interface dispatch + virtuals |
| `FactoryMode.RemoteOnly` | Not affected | Skips Local generation entirely |
| `Neatoo.RemoteFactory.AspNetCore` | Not affected | Server-only package |

**Design Project Verification:** N/A for exploration -- Design projects don't need modification for the prototype.

**Breaking Changes:** No. The feature switch defaults to `true`, preserving all existing behavior.

**Codebase Analysis:**

Files examined and key findings:

1. **`src/Generator/Renderer/ClassFactoryRenderer.cs`** (1280+ lines)
   - `RenderReadLocalMethod()` (line ~353): Generates `LocalCreate`/`LocalFetch` -- directly calls entity methods and resolves services via `ServiceProvider.GetRequiredService<T>()`
   - `RenderLocalMethod()` (line ~540): Generates `LocalInsert`/`LocalUpdate`/`LocalDelete` -- similarly resolves services
   - `RenderSaveLocalMethod()` (line ~684): Generates `LocalSave` -- routes to `LocalInsert`/`LocalUpdate`/`LocalDelete`
   - `RenderFactoryServiceRegistrar()` (line ~1079): Registers delegates that call Local methods
   - Guard placement: Each `Local*` method is the right target for guards

2. **`src/Generator/Renderer/StaticFactoryRenderer.cs`** (317 lines)
   - `RenderLocalDelegateRegistration()` (line ~178): Generates lambdas that call the private static `_MethodName()` and resolve services
   - Guard placement: The local delegate registration lambda

3. **`src/Generator/Renderer/InterfaceFactoryRenderer.cs`** (538 lines)
   - `RenderLocalMethod()` (line ~274): Generates local methods that resolve the server implementation via DI
   - Guard placement: The local method body

4. **`src/RemoteFactory/FactoryBase.cs`** (60 lines)
   - 7 virtual methods (`DoFactoryMethodCall` variants)
   - All delegate to `IFactoryCore<T>` -- two levels of indirection
   - **KEY RISK**: Virtual + interface dispatch may prevent trimmer from analyzing lambda reachability

5. **`src/RemoteFactory/Internal/FactoryCore.cs`** (351 lines)
   - Implements `IFactoryCore<T>` with all-virtual methods
   - Invokes lambdas passed from generated factory Local methods
   - Contains `is IFactoryOnStart`, `is IFactoryOnComplete` runtime type checks -- these use interface dispatch, another trimmer concern

6. **Generated output example** (`Design.Domain.Aggregates.OrderFactory.g.cs`)
   - Confirmed pattern: `LocalCreate` calls `ServiceProvider.GetRequiredService<IOrderLineListFactory>()` then `DoFactoryMethodCall(target, op, () => target.Create(...))`
   - `LocalInsert`/`LocalUpdate`/`LocalDelete` call `ServiceProvider.GetRequiredService<IOrderRepository>()` then `DoFactoryMethodCallAsync(cTarget, op, () => cTarget.Insert(repository))`
   - `LocalSave` routes to `LocalInsert`/`LocalUpdate`/`LocalDelete` based on `IsNew`/`IsDeleted`
   - **All these are the targets for feature switch guards**

7. **`src/RemoteFactory/FactoryAttributes.cs`** (200 lines)
   - `FactoryMode.RemoteOnly` already exists for compile-time elimination
   - Feature switch is complementary -- for `Full` mode assemblies where trimming happens at publish time

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: NeatooRuntime + Generator Guards | developer | Yes | Independent, focused on generator changes and library addition | None |
| Phase 2: Trimming Verification Project | developer | Yes | Different domain (project setup, publish, inspection); benefits from clean context | Phase 1 |
| Phase 3: Virtual Dispatch Investigation | developer | No (resume Phase 2) | Continues from Phase 2 findings; needs context from verification results | Phase 2 |
| Phase 4: All Factory Patterns | developer | No (resume Phase 3) | Extension of Phase 1 patterns; needs Phase 3 findings | Phase 1, 3 |
| Phase 5: Documentation + Decision | architect | Yes | Analysis and recommendation based on all findings | Phase 1-4 |

**Parallelizable phases:** None -- each phase depends on prior findings.

**Notes:** Phases 3-4 may be unnecessary if Phase 2 conclusively answers the feasibility question (either positively or negatively). The architect should evaluate after Phase 2 whether to proceed, pivot, or stop.

---

## Alternative Architecture: Trimmer-Friendly FactoryBase Redesign

### What DoFactoryMethodCall Does Today

The `FactoryBase<T>` / `IFactoryCore<T>` / `FactoryCore<T>` architecture exists to provide a **cross-cutting pipeline** around entity factory method invocations. The current call chain is:

```
Generated OrderFactory.LocalCreate(...)
  1. Resolves services via ServiceProvider.GetRequiredService<T>()
  2. Calls FactoryBase<T>.DoFactoryMethodCall(operation, () => target.Create(...))     [virtual]
     3. Validates lambda is non-null
     4. Delegates to IFactoryCore<T>.DoFactoryMethodCall(operation, lambda)             [interface dispatch]
        5. FactoryCore<T>.DoFactoryMethodCall(operation, lambda)                        [virtual]
           a. Logs operation start (with correlation ID)
           b. Starts Stopwatch
           c. For Write variants: calls IFactoryOnStart/IFactoryOnStartAsync if target implements it
           d. Invokes lambda.Invoke()  -->  target.Create(customerName, lineListFactory)
           e. For Write variants: checks IFactoryOnComplete/IFactoryOnCompleteAsync on target
           f. For Read variants: checks IFactoryOnComplete on the returned target
           g. Logs operation complete (with elapsed time)
           h. Catches exceptions: logs failure, re-throws
           i. For async variants: catches OperationCanceledException separately, invokes IFactoryOnCancelled/IFactoryOnCancelledAsync
```

**The value provided by this architecture:**

| Concern | Implementation | Where |
|---------|---------------|-------|
| **Structured logging** | Log start/complete/failed with correlation ID and elapsed time | `FactoryCore<T>` |
| **Lifecycle hooks** | `IFactoryOnStart`, `IFactoryOnComplete`, `IFactoryOnCancelled` (sync + async variants) | `FactoryCore<T>` |
| **Bool result handling** | When domain method returns `bool`, `false` means "don't return the target" (returns `default`) | `DoFactoryMethodCallBool*` variants |
| **Extensibility via DI** | Users can register a custom `IFactoryCore<T>` to intercept all factory calls for type `T` | Open generic DI registration |
| **Extensibility via inheritance** | Users can subclass `FactoryCore<T>` to add before/after behavior | Virtual methods on `FactoryCore<T>` |
| **Null checking** | `ArgumentNullException.ThrowIfNull` on the lambda | `FactoryBase<T>` |

**The indirection layers:**

- `FactoryBase<T>` (abstract): 7 virtual methods. Each does `ArgumentNullException.ThrowIfNull` then delegates to `IFactoryCore<T>`. The virtuality here serves **no current purpose** -- the generated factory classes never override these methods. This layer exists purely as a pass-through.

- `IFactoryCore<T>` (interface): 7 methods. Decouples the factory from the core implementation, enabling DI-based replacement. This is the **extensibility seam** -- users can register their own `IFactoryCore<T>` implementation for specific types.

- `FactoryCore<T>` (class): 7 virtual methods. Contains the actual logging, lifecycle hooks, and lambda invocation. Virtual so users can subclass and override.

**Important observation about the Interface Factory pattern:** Interface factories (`InterfaceFactoryRenderer`) do **NOT** use `FactoryBase<T>` or `IFactoryCore<T>` at all. Their `LocalMethod` implementations call `target.MethodName(...)` directly. They get no lifecycle hooks, no logging, no timing. This means the virtual dispatch problem already does not exist for the Interface Factory pattern.

**Important observation about the Static Factory pattern:** Static factories (`StaticFactoryRenderer`) also do **NOT** use `FactoryBase<T>` or `IFactoryCore<T>`. Their local delegate registrations call the domain method directly. No lifecycle hooks, no logging, no timing. The virtual dispatch problem does not exist for this pattern either.

**Conclusion: The virtual dispatch problem only affects the Class Factory pattern.** The Class Factory pattern is, however, the most common and most complex pattern (Create, Fetch, Save with Insert/Update/Delete routing).

### Current Extensibility Usage

From codebase analysis:

- **Custom `FactoryCore<T>` subclasses**: Found only in unit tests (`TrackingFactoryCore_Sync`, `TrackingAsyncFactoryCore_Read`, etc.). No examples in Design projects, integration tests, or example apps. The tests verify the extensibility mechanism works, but no production code uses it.

- **Custom `IFactoryCore<T>` implementations via DI**: The open-generic registration (`services.AddScoped(typeof(IFactoryCore<>), typeof(FactoryCore<>))`) allows per-type overrides, but no examples of this pattern exist in the codebase outside tests.

- **`FactoryBase<T>` virtual method overrides**: The generated factory classes inherit from `FactoryBase<T>` but never override `DoFactoryMethodCall`. The virtual methods on `FactoryBase<T>` are **dead extensibility** -- they exist but cannot be used because the generated classes are `internal`.

- **Lifecycle hooks**: `Order` in the Design project implements `IFactoryOnStartAsync` and `IFactoryOnCompleteAsync`. This is the primary production use case for the `FactoryCore` pipeline.

### Proposed Alternative Design: Inline Pipeline in Generated Code

**Core idea:** Instead of delegating to `FactoryBase<T>` -> `IFactoryCore<T>` -> `FactoryCore<T>` via virtual/interface dispatch, the generator emits the pipeline logic directly into each `LocalMethod`. The lambda never crosses a virtual dispatch boundary.

#### Design A: Full Inline (Recommended)

The generator emits the complete pipeline inline in each `LocalMethod`:

```csharp
// Generated: OrderFactory.LocalCreate (Read pattern -- no target parameter)
public async Task<Order> LocalCreate(string customerName, CancellationToken cancellationToken = default)
{
    if (!NeatooRuntime.IsServerRuntime)
        throw new InvalidOperationException("Server-only method called in non-server runtime.");

    var logger = ServiceProvider.GetService<ILogger<OrderFactory>>();
    var correlationContext = ServiceProvider.GetService<ICorrelationContext>();
    var correlationId = correlationContext?.CorrelationId;

    logger?.FactoryOperationStarted(correlationId, FactoryOperation.Create, "Order");
    var sw = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        var target = ServiceProvider.GetRequiredService<Order>();
        var lineListFactory = ServiceProvider.GetRequiredService<IOrderLineListFactory>();

        await target.Create(customerName, lineListFactory);

        if (target is IFactoryOnComplete factoryOnComplete)
        {
            factoryOnComplete.FactoryComplete(FactoryOperation.Create);
        }

        sw.Stop();
        logger?.FactoryOperationCompleted(correlationId, FactoryOperation.Create, "Order", sw.ElapsedMilliseconds);
        return target;
    }
    catch (Exception ex)
    {
        sw.Stop();
        logger?.FactoryOperationFailed(correlationId, FactoryOperation.Create, "Order", ex.Message, ex);
        throw;
    }
}
```

For the Write pattern (Insert/Update/Delete), the generated code includes `IFactoryOnStart`/`IFactoryOnStartAsync` and `IFactoryOnCancelled`/`IFactoryOnCancelledAsync`:

```csharp
// Generated: OrderFactory.LocalInsert (Write pattern -- has target parameter)
public async Task<Order?> LocalInsert(Order target, CancellationToken cancellationToken = default)
{
    if (!NeatooRuntime.IsServerRuntime)
        throw new InvalidOperationException("Server-only method called in non-server runtime.");

    var cTarget = (Order) target ?? throw new Exception("...");
    var logger = ServiceProvider.GetService<ILogger<OrderFactory>>();
    var correlationContext = ServiceProvider.GetService<ICorrelationContext>();
    var correlationId = correlationContext?.CorrelationId;

    logger?.FactoryOperationStarted(correlationId, FactoryOperation.Insert, "Order");
    var sw = System.Diagnostics.Stopwatch.StartNew();

    try
    {
        if (cTarget is IFactoryOnStart factoryOnStart)
        {
            factoryOnStart.FactoryStart(FactoryOperation.Insert);
        }
        if (cTarget is IFactoryOnStartAsync factoryOnStartAsync)
        {
            await factoryOnStartAsync.FactoryStartAsync(FactoryOperation.Insert);
        }

        var repository = ServiceProvider.GetRequiredService<IOrderRepository>();
        await cTarget.Insert(repository);

        if (cTarget is IFactoryOnComplete factoryOnComplete)
        {
            factoryOnComplete.FactoryComplete(FactoryOperation.Insert);
        }
        if (cTarget is IFactoryOnCompleteAsync factoryOnCompleteAsync)
        {
            await factoryOnCompleteAsync.FactoryCompleteAsync(FactoryOperation.Insert);
        }

        sw.Stop();
        logger?.FactoryOperationCompleted(correlationId, FactoryOperation.Insert, "Order", sw.ElapsedMilliseconds);
        return cTarget;
    }
    catch (OperationCanceledException)
    {
        sw.Stop();
        logger?.FactoryOperationCancelled(correlationId, FactoryOperation.Insert, "Order");

        if (cTarget is IFactoryOnCancelled factoryOnCancelled)
        {
            factoryOnCancelled.FactoryCancelled(FactoryOperation.Insert);
        }
        if (cTarget is IFactoryOnCancelledAsync factoryOnCancelledAsync)
        {
            await factoryOnCancelledAsync.FactoryCancelledAsync(FactoryOperation.Insert);
        }

        throw;
    }
    catch (Exception ex)
    {
        sw.Stop();
        logger?.FactoryOperationFailed(correlationId, FactoryOperation.Insert, "Order", ex.Message, ex);
        throw;
    }
}
```

**Why this works for trimming:**

The call graph is now fully static and linear:
```
LocalCreate(...)
  -> ServiceProvider.GetRequiredService<Order>()          [static type reference]
  -> ServiceProvider.GetRequiredService<IOrderLineListFactory>()  [static type reference]
  -> target.Create(customerName, lineListFactory)          [direct call, no lambda]
  -> target is IFactoryOnComplete                          [type check, no dispatch indirection]
```

There is no lambda capture, no `Func<T>` delegate, no virtual method, and no interface dispatch between the feature switch guard and the server-only code. When `NeatooRuntime.IsServerRuntime` is constant-folded to `false`, the entire method body after the guard becomes dead code with a fully analyzable dependency graph. The trimmer can statically determine that `IOrderLineListFactory`, `IOrderRepository`, and the entity's server-only methods are unreachable.

#### Design B: Inline with Optional FactoryCore Delegate (Considered, Not Recommended)

This alternative preserves the extensibility seam by having the generator check for a custom `IFactoryCore<T>` at runtime:

```csharp
public async Task<Order> LocalCreate(string customerName, CancellationToken cancellationToken = default)
{
    if (!NeatooRuntime.IsServerRuntime)
        throw new InvalidOperationException("...");

    var customCore = ServiceProvider.GetService<IFactoryCore<Order>>();
    if (customCore != null)
    {
        // Fall back to virtual dispatch path for custom cores
        var target = ServiceProvider.GetRequiredService<Order>();
        var lineListFactory = ServiceProvider.GetRequiredService<IOrderLineListFactory>();
        return await customCore.DoFactoryMethodCallAsync(FactoryOperation.Create,
            () => { target.Create(customerName, lineListFactory); return Task.FromResult(target); });
    }

    // Inline path (trimmer-friendly)
    // ... same as Design A ...
}
```

**Why this is NOT recommended:**
- Doubles the generated code for every method
- The `customCore != null` branch still contains the lambda + virtual dispatch, meaning the trimmer cannot remove it
- Defeats the purpose: the very types we want trimmed are referenced in the fallback branch

#### Design C: Static Helper Methods (Considered, Not Recommended)

Move the pipeline logic to static helper methods in the library instead of generating it inline:

```csharp
// In library:
public static class FactoryPipeline
{
    public static T Execute<T>(IServiceProvider sp, FactoryOperation op, Func<T> call) { ... }
}

// In generated code:
return FactoryPipeline.Execute<Order>(ServiceProvider, FactoryOperation.Create,
    () => { target.Create(...); return target; });
```

**Why this is NOT recommended:**
- Still passes a lambda as `Func<T>`, which the trimmer must analyze through generic methods
- Generic methods with `Func<T>` parameters are problematic for trimmer analysis
- Marginal improvement over the current architecture

### Breaking Changes

| Change | Impact | Severity |
|--------|--------|----------|
| `FactoryBase<T>` removed or drastically simplified | Generated factories no longer inherit from it; custom subclasses of generated factories (impossible today since they are `internal`) would break | **Low** -- generated factories are `internal`, users cannot subclass them |
| `IFactoryCore<T>` extensibility mechanism removed | Users who registered custom `IFactoryCore<T>` implementations via DI lose the ability to intercept factory calls | **Medium** -- the feature exists and is documented via tests, but no known production usage. The Design project does NOT use custom `IFactoryCore<T>`. |
| `FactoryCore<T>` subclassing no longer affects generated factories | Users who subclassed `FactoryCore<T>` and registered it as `IFactoryCore<T>` lose interception | **Medium** -- same as above |
| `FactorySaveBase<T>` requires adjustment | Currently provides default `IFactorySave<T>` implementation; would need to be retained or moved to generated code | **Low** -- generated code already provides the Save routing; only the base class default would change |
| Generated code size increases | Each `LocalMethod` now contains the full pipeline (~30-50 lines) instead of a single `DoFactoryMethodCall` call (~3 lines) | **Low** -- more generated code, but it is IL that gets trimmed away on the client anyway |
| Logger resolution changes | Currently `FactoryCore<T>` takes `ILogger<FactoryCore<T>>` via constructor; inline code would use `ServiceProvider.GetService<ILogger<XxxFactory>>()` | **Low** -- log category name changes from `FactoryCore<Order>` to `OrderFactory` |

### Proposed Migration for FactoryCore Extensibility

The `IFactoryCore<T>` extensibility mechanism provides before/after interception of factory calls. If this capability needs to be preserved (even though it has no known production usage), consider a replacement mechanism:

**Option 1: Lifecycle hooks are sufficient.** The `IFactoryOnStart`/`IFactoryOnComplete`/`IFactoryOnCancelled` interfaces already provide the same before/after semantics and are checked inline (via `is` type checks). Users who need per-type interception can implement these interfaces on their entities. This is the **recommended approach** -- it already works and is trimmer-friendly.

**Option 2: Middleware-style pipeline.** If use cases emerge that require intercepting factory calls without modifying the entity (e.g., cross-cutting auditing), a future feature could add middleware-style hooks registered via DI. These would be resolved and invoked inline in generated code, avoiding virtual dispatch through base classes. This is a **future enhancement** if needed.

### Impact on Each Factory Pattern

| Pattern | Current Virtual Dispatch? | Impact of Redesign |
|---------|--------------------------|-------------------|
| **Class Factory** | Yes -- all `LocalMethod` calls go through `FactoryBase<T>` -> `IFactoryCore<T>` -> `FactoryCore<T>` | **Primary target.** Generated code inlines the pipeline. Eliminates the trimming risk entirely. |
| **Static Factory** | No -- local delegates call domain methods directly | **No change needed.** Already trimmer-friendly with feature switch guards around the delegate registration. |
| **Interface Factory** | No -- `LocalMethod` calls domain methods directly | **No change needed.** Already trimmer-friendly with feature switch guards around the local method body. |

### Updated Risk Assessment

| Risk | Original Assessment | With Redesign |
|------|-------------------|---------------|
| Virtual method dispatch defeating trimming (Rules 9, 10) | **High** -- core architecture passes lambdas through virtual chain | **Eliminated** -- no virtual dispatch, no lambda capture. Entity methods are called directly in generated code. |
| Feature switch constant-folding | Medium -- depends on trimmer implementation | **Unchanged** -- still the foundation; prototype must verify .NET 9+ trimmer handles `[FeatureSwitchDefinition]` correctly |
| Transitive dependency removal | Medium -- depends on trimmer's ability to trace through dead code | **Improved** -- all type references are direct `GetRequiredService<T>()` calls within dead code blocks, giving the trimmer a clear dependency graph |
| net8.0 limitation | Low -- acceptable | **Unchanged** |
| Backward compatibility | Low -- defaults preserve behavior | **Increased to Medium** -- `IFactoryCore<T>` extensibility is removed. Mitigated by the fact that lifecycle hooks cover the same use cases. |
| Generated code size | N/A | **New: Low** -- each `LocalMethod` grows from ~5 lines to ~35-50 lines, but this is generated code and is trimmed away on client side |

### Updated Implementation Phases

If the redesign is adopted, the implementation phases change:

#### Phase 1: NeatooRuntime Class (Unchanged)
Add `NeatooRuntime.cs` with `[FeatureSwitchDefinition]`.

#### Phase 2: Generator Redesign for Class Factory Pattern (Replaces original Phase 1 generator changes)
1. Modify `ClassFactoryRenderer.RenderReadLocalMethod()` to emit inline pipeline (logging, lifecycle hooks, direct method call, exception handling) instead of calling `DoFactoryMethodCall`.
2. Modify `ClassFactoryRenderer.RenderLocalMethod()` (Write methods) similarly, including `IFactoryOnStart`/`IFactoryOnCancelled` handling.
3. Wrap all inline pipeline code in `if (NeatooRuntime.IsServerRuntime)` guard.
4. Remove `FactoryBase<T>` as the base class from generated factories. Generated factories become standalone classes (or inherit from a minimal base for `IFactorySave<T>` support).
5. Verify all existing tests pass.

#### Phase 3: Trimming Verification (Same as original Phase 2)
Build the verification project and confirm the trimmer removes server-only types.

#### Phase 4: Deprecation Assessment
1. Determine if `FactoryBase<T>`, `IFactoryCore<T>`, and `FactoryCore<T>` should be deprecated, made internal, or removed.
2. Update unit tests that test `FactoryCore<T>` extensibility (these test an internal mechanism, not user-facing behavior).
3. Document the migration path for anyone using custom `IFactoryCore<T>` implementations.

#### Phase 5: Static and Interface Factory Guards (Same as original Phase 4)
Add feature switch guards to `StaticFactoryRenderer` and `InterfaceFactoryRenderer`. These patterns already avoid virtual dispatch, so they only need the `if (NeatooRuntime.IsServerRuntime)` guard.

#### Phase 6: Documentation and Decision (Same as original Phase 5)

### Generator Complexity Impact

The redesign **increases** generator complexity in one dimension and **decreases** it in another:

**Increased complexity:**
- The generator must emit ~35-50 lines per `LocalMethod` instead of ~5 lines.
- The generator needs knowledge of which lifecycle hook interfaces (`IFactoryOnStart`, `IFactoryOnComplete`, etc.) to check for, and the Read vs. Write distinction for which hooks apply.
- The generator must handle 7 method signature variants (sync/async, with/without target, bool/non-bool) that `FactoryCore` currently handles uniformly.

**Decreased complexity:**
- No need to generate `IFactoryCore<T>` constructor parameter and `base(factoryCore)` calls.
- No need to select between `DoFactoryMethodCall` / `DoFactoryMethodCallAsync` / `DoFactoryMethodCallBool` / `DoFactoryMethodCallBoolAsync` etc. -- the generator directly emits the appropriate code for each method signature.
- The generated class hierarchy is simpler: no `FactoryBase<T>` inheritance.

**Net assessment:** The generator's rendering methods become more verbose but more straightforward. The logic that was hidden in 7 virtual method variants in `FactoryCore<T>` becomes explicit in the generator's rendering logic. This is a modest increase in generator complexity, offset by the elimination of a complex runtime type hierarchy.

---

## Developer Review

**Status:** Approved (with concerns noted)
**Reviewed:** 2026-03-03

### Codebase Verification Summary

The following plan claims were independently verified against the source code:

1. **FactoryBase<T> has 7 virtual pass-through methods** -- CONFIRMED. `src/RemoteFactory/FactoryBase.cs` (60 lines) contains exactly 7 `protected virtual` methods, each doing `ArgumentNullException.ThrowIfNull` (except 2 that omit it) then delegating to `this.FactoryCore.DoFactoryMethodCall*()`. None are overridden by generated factory classes.

2. **IFactoryCore<T> extensibility used only in tests** -- CONFIRMED. Custom `IFactoryCore<T>` registrations found only in `RemoteFactory.UnitTests/FactoryGenerator/Core/FactoryCoreTests.cs`, `FactoryCoreAsyncTests.cs`, and one performance test that re-registers the default. Zero usage in Design projects, Examples, or integration tests.

3. **Interface and Static factories don't use FactoryBase<T>** -- CONFIRMED. `InterfaceFactoryRenderer.cs` and `StaticFactoryRenderer.cs` contain zero references to `FactoryBase`, `DoFactoryMethodCall`, or `IFactoryCore`. Interface factories call `target.MethodName(...)` directly. Static factories call `TypeName.MethodName(...)` directly.

4. **Lifecycle hooks usage** -- CONFIRMED. `Order` in Design project implements `IFactoryOnStartAsync` and `IFactoryOnCompleteAsync`. No test targets implement lifecycle hooks. The lifecycle hook interfaces are:
   - `IFactoryOnStart` / `IFactoryOnStartAsync` -- called BEFORE method execution, only in **Write** variants (with target parameter)
   - `IFactoryOnComplete` / `IFactoryOnCompleteAsync` -- called AFTER method execution, in **both** Read and Write variants
   - `IFactoryOnCancelled` / `IFactoryOnCancelledAsync` -- called on `OperationCanceledException`, only in **async Write** variants

5. **Read vs Write lifecycle hook distinction** -- VERIFIED from FactoryCore.cs:
   - Read variants (no target param): Only `IFactoryOnComplete` on the **result**. No Start, no Cancelled, no async lifecycle hooks.
   - Sync Write variants: `IFactoryOnStart` before, `IFactoryOnComplete` after. No async hooks, no Cancelled.
   - Async Write variants: All 6 lifecycle hooks (Start/StartAsync before, Complete/CompleteAsync after, Cancelled/CancelledAsync on OperationCanceledException).
   - Bool Write variants: Same as non-bool, plus `if (!succeeded) return default` before `IFactoryOnComplete`.

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `NeatooRuntime.IsServerRuntime` property (Design Section 1): annotated with `[FeatureSwitchDefinition("Neatoo.RemoteFactory.IsServerRuntime")]`, body uses `AppContext.TryGetSwitch()` defaulting to `true`. When project sets `<RuntimeHostConfigurationOption Include="Neatoo.RemoteFactory.IsServerRuntime" Value="false" Trim="true" />`, .NET 9+ trimmer treats the property as constant `false`. | Property treated as constant `false` by trimmer | Yes | This is a documented .NET 9+ trimmer behavior per `FeatureSwitchDefinitionAttribute` docs. The implementation follows the standard pattern. Must verify empirically in Phase 2. |
| 2 | Inline pipeline code wraps method body in `if (!NeatooRuntime.IsServerRuntime) throw ...;` (Design A, line 519-520 of plan). When `IsServerRuntime` is constant-folded to `false`, the condition `!false` is `true`, so the `throw` executes and the rest of the method body (after the guard) is dead code. Alternatively in the `if (NeatooRuntime.IsServerRuntime) { ... }` form, the entire block body is dead code. | Guarded branches removed from IL | Yes | Standard dead-code elimination after constant folding. The key is that there is NO virtual dispatch or lambda capture between the guard and the type references. |
| 3 | Inline `LocalCreate` method (Design A, plan lines 517-551): Guard `if (!NeatooRuntime.IsServerRuntime) throw ...;` at top. Below it: `ServiceProvider.GetRequiredService<Order>()`, `ServiceProvider.GetRequiredService<IOrderLineListFactory>()`, `await target.Create(customerName, lineListFactory)`, `is IFactoryOnComplete` check. All type references (`Order`, `IOrderLineListFactory`) are direct static references within the dead code block. No lambda, no virtual dispatch. | `LocalCreate` body becomes dead code; `IOrderLineListFactory` reference removed | Yes | The inline pipeline eliminates the lambda/virtual dispatch boundary that was the key risk. All type references are in the linear code path. |
| 4 | Inline `LocalInsert` method (Design A, plan lines 558-620): Same guard pattern. Service resolution `ServiceProvider.GetRequiredService<IOrderRepository>()` and call `cTarget.Insert(repository)` are direct references within the guarded block. Same applies to `LocalUpdate` and `LocalDelete`. | Write method bodies trimmed; `IOrderRepository` removed | Yes | Same reasoning as Rule 3. Direct static references within dead code block. |
| 5 | `RenderSaveLocalMethod` in `ClassFactoryRenderer.cs` (line 684): `LocalSave` routes to `LocalInsert`/`LocalUpdate`/`LocalDelete` based on `IsNew`/`IsDeleted`. With inline pipeline, the `LocalSave` method itself calls the inlined `LocalInsert`/`LocalUpdate`/`LocalDelete` methods. If `LocalSave` is also wrapped in an `if (NeatooRuntime.IsServerRuntime)` guard, the calls to the Local* methods are within dead code. | Save routing logic and transitive dependencies trimmed | Yes, with caveat | The plan's inline examples show guards on LocalInsert/LocalUpdate/LocalDelete individually but do NOT show whether LocalSave itself gets a guard. LocalSave calls these methods, so if LocalSave is NOT guarded, it would keep references to the guarded methods alive even though their bodies are dead. **The guard must also be on LocalSave.** The plan mentions this in Step 5 of Phase 1 ("Modify `ClassFactoryRenderer.RenderSaveLocalMethod()` similarly") so this is covered. |
| 6 | `StaticFactoryRenderer.RenderLocalDelegateRegistration()` (line 178): Currently generates `services.AddTransient<TypeName.DelegateName>(cc => { return (...) => { var svc = cc.GetRequiredService<IMyService>(); return TypeName.MethodName(svc); }; });`. With feature switch guard: wrap the lambda registration in `if (NeatooRuntime.IsServerRuntime) { services.AddTransient<...>(...); }`. The `GetRequiredService<IMyService>()` call and `TypeName.MethodName()` call are within the guarded block. | Local delegate registration trimmed; `IMyService` removed | Yes | Static factories already avoid virtual dispatch. The guard placement is straightforward around the entire `services.AddTransient` call. |
| 7 | `InterfaceFactoryRenderer.RenderLocalMethod()` (line 274): Currently generates `var target = ServiceProvider.GetRequiredService<ServiceTypeName>(); return target.MethodName(...)`. With feature switch guard: wrap the method body in `if (NeatooRuntime.IsServerRuntime)`. `ServiceTypeName` and the implementation type are direct references within the guarded block. | `LocalMethodName` body trimmed; server implementation removed | Yes | Interface factories already avoid virtual dispatch. Guard placement is straightforward. |
| 8 | Transitive dependency removal is a trimmer capability, not a code generation concern. When all call sites referencing `IOrderRepository` (or any server-only type) are within dead code blocks, and no other reachable code references that type, the trimmer removes the type and its dependencies. | Server-only types and their dependencies removed | Yes (conditional) | This depends on the trimmer's ability to trace transitive dependencies. The inline pipeline ensures all references to server-only types are within dead code blocks. The trimmer SHOULD remove them, but this must be empirically verified in Phase 2. Edge case: if `IOrderRepository` is also referenced in DI registration code (e.g., `services.AddScoped<IOrderRepository, OrderRepository>()`), those registrations must ALSO be guarded or in a server-only assembly. |
| 9 | In the **original** architecture (not the inline redesign): `FactoryBase<T>.DoFactoryMethodCall()` is virtual (confirmed in `FactoryBase.cs` line 16), lambda `() => target.Create(...)` is passed through `IFactoryCore<T>` interface dispatch to `FactoryCore<T>` virtual method. Trimmer cannot statically determine lambda body is dead code across virtual boundaries. | Trimmer MAY preserve lambda body despite feature switch guard | Yes (risk confirmed) | This is the key risk the exploration must validate. The inline pipeline (Design A) eliminates this risk entirely by removing the virtual dispatch chain. |
| 10 | In the **original** architecture: `IFactoryCore<T>` interface dispatch (confirmed in `FactoryCore.cs` line 12) with `FactoryCore<T>` virtual implementation (line 31). Lambda passed through interface dispatch cannot be statically analyzed. | Trimmer MAY preserve lambda regardless of guard | Yes (risk confirmed) | Same risk as Rule 9, second layer of indirection. Both risks are eliminated by the inline pipeline. |
| 11 | `NeatooRuntime.IsServerRuntime` property body (Design Section 1): `AppContext.TryGetSwitch("Neatoo.RemoteFactory.IsServerRuntime", out bool isEnabled) ? isEnabled : true`. When no `RuntimeHostConfigurationOption` is configured, `AppContext.TryGetSwitch` returns `false` (switch not found), so the ternary evaluates to `true` (default). | Property returns `true`; all code paths execute normally | Yes | The `AppContext.TryGetSwitch` returns `false` when switch is not found, and `out` parameter is `false`. The ternary `? isEnabled : true` correctly falls through to `true`. |
| 12 | When `PublishTrimmed=false`, the trimmer does not run. The `if (NeatooRuntime.IsServerRuntime)` guards are evaluated at runtime by JIT. `NeatooRuntime.IsServerRuntime` returns `true` (per Rule 11 default). The `if (true)` guard executes the full code path. | Runtime behavior identical to current behavior | Yes | No trimmer means no constant folding; JIT evaluates the condition at runtime. |
| 13 | `FactoryMode.RemoteOnly` handling in `ClassFactoryRenderer.cs` (line 146): `if (mode == FactoryMode.RemoteOnly)` skips rendering of Local methods entirely. With `RemoteOnly`, there are no `LocalCreate`/`LocalInsert`/etc. methods generated, so feature switch guards have nothing to wrap. | No Local methods generated; feature switch irrelevant | Yes | The two mechanisms target different scenarios: `RemoteOnly` = compile-time elimination for client-only assemblies; feature switch = publish-time elimination for shared assemblies. |
| 14 | Inline pipeline (Design A, plan lines 516-551 and 558-620): No `DoFactoryMethodCall` call. No lambda capture (`Func<T>` or `Action`). No `FactoryBase<T>` virtual method. No `IFactoryCore<T>` interface dispatch. All type references (`Order`, `IOrderLineListFactory`, `IOrderRepository`) are direct `GetRequiredService<T>()` calls and direct method invocations within the `if (NeatooRuntime.IsServerRuntime)` block. Call graph: `LocalCreate -> GetRequiredService<Order>() -> GetRequiredService<IOrderLineListFactory>() -> target.Create(...) -> is IFactoryOnComplete check`. Entirely linear, no opaque dispatch. | Trimmer can statically determine all type references are dead code | Yes | This is the core insight of the redesign. By eliminating the virtual dispatch chain, the trimmer gets a clear, linear dependency graph. |
| 15 | Inline pipeline lifecycle hooks (Design A, plan lines 573-576, 585-592): `if (cTarget is IFactoryOnStart factoryOnStart) { factoryOnStart.FactoryStart(operation); }` / `if (cTarget is IFactoryOnStartAsync factoryOnStartAsync) { await factoryOnStartAsync.FactoryStartAsync(operation); }` -- same `is` type checks as `FactoryCore<T>` uses (verified in `FactoryCore.cs` lines 148, 227-235, 240-248). | Lifecycle hooks invoked identically to current FactoryCore pipeline | Yes, with concern | The inline code in Design A correctly replicates the lifecycle hooks for the **async Write** variant. However, see Concern #2 below about incomplete coverage of all 7 variants and the Read pattern's different lifecycle hook set. |
| 16 | Inline pipeline removes `FactoryBase<T>` -> `IFactoryCore<T>` -> `FactoryCore<T>` chain. Custom `IFactoryCore<T>` registrations via DI are no longer resolved by generated factory code. `FactoryCore<T>` open-generic registration becomes unused. | IFactoryCore<T> extensibility no longer available | Yes | Verified: custom `IFactoryCore<T>` usage exists only in unit tests (`FactoryCoreTests.cs`, `FactoryCoreAsyncTests.cs`). No production usage in Design, Examples, or integration tests. Migration path: `IFactoryOnStart`/`IFactoryOnComplete`/`IFactoryOnCancelled` interfaces on entities. |

### Test Scenario Verification

| # | Scenario | Verification Against Design | Matches Expected? |
|---|----------|---------------------------|-------------------|
| 1 | Feature switch property basics | `NeatooRuntime` class design (Section 1) correctly annotates with `[FeatureSwitchDefinition]` and uses `AppContext.TryGetSwitch`. Empirical verification needed. | Yes -- must verify empirically |
| 2 | Class factory Create trimming | Inline pipeline LocalCreate (Design A) wraps `GetRequiredService<IOrderLineListFactory>()` in guard. No virtual dispatch. Trimmer should remove. | Yes -- must verify empirically |
| 3 | Class factory Save/Write trimming | Inline pipeline LocalInsert/LocalUpdate/LocalDelete (Design A) wraps `GetRequiredService<IOrderRepository>()` in guard. LocalSave also guarded. | Yes -- must verify empirically |
| 4 | Static factory Execute trimming | Guard around `services.AddTransient<DelegateName>(cc => ...)` in `RenderLocalDelegateRegistration`. Service resolution inside lambda within guard. | Yes -- must verify empirically |
| 5 | Interface factory Local trimming | Guard around `var target = ServiceProvider.GetRequiredService<ServiceTypeName>(); return target.Method(...)` in `RenderLocalMethod`. Direct call, no dispatch. | Yes -- must verify empirically |
| 6 | Virtual method defeats trimming (original architecture) | Original architecture passes lambda through `FactoryBase<T>.DoFactoryMethodCall` (virtual) -> `IFactoryCore<T>.DoFactoryMethodCall` (interface) -> `FactoryCore<T>.DoFactoryMethodCall` (virtual). Confirmed in `FactoryBase.cs` and `FactoryCore.cs`. | Yes -- this is the risk scenario the exploration validates |
| 7 | No feature switch configured | `NeatooRuntime.IsServerRuntime` body: `AppContext.TryGetSwitch(..., out isEnabled) ? isEnabled : true`. No switch configured -> `TryGetSwitch` returns false -> ternary returns `true`. All code paths execute. | Yes |
| 8 | RemoteOnly mode unaffected | `ClassFactoryRenderer.cs` line 146: `if (mode == FactoryMode.RemoteOnly)` skips Local method rendering. No Local methods means no guards needed. | Yes |
| 9 | Inline pipeline Create trimming | Same as Scenario 2 but explicitly tests the inline path (no DoFactoryMethodCall). Design A shows linear call graph within guard. | Yes -- must verify empirically |
| 10 | Inline pipeline with lifecycle hooks | Design A Write inline code (lines 558-620) shows `is IFactoryOnStart`, `is IFactoryOnStartAsync`, `is IFactoryOnComplete`, `is IFactoryOnCompleteAsync`, `is IFactoryOnCancelled`, `is IFactoryOnCancelledAsync` checks matching `FactoryCore.cs` `DoFactoryMethodCallAsync(T target, ...)` method (lines 217-279). With `IsServerRuntime=true`, the inline code executes and lifecycle hooks fire. | Yes |
| 11 | IFactoryCore extensibility removed | Inline pipeline does not resolve `IFactoryCore<T>` from DI. Custom registrations are ignored. Verified: no production usage of custom `IFactoryCore<T>`. | Yes |

### Concerns

#### Concern 1: Plan Shows Only 2 of 7 FactoryCore Variants in Inline Examples

The plan's inline code examples (Design A) show only:
- **Read async variant** (LocalCreate returning `Task<Order>` with `await target.Create(...)`)
- **Write async variant** (LocalInsert returning `Task<Order?>` with `await cTarget.Insert(repository)`)

The `FactoryCore<T>` actually has **7 distinct method signatures** with different lifecycle hook combinations:

1. **Sync Read** (`Func<T>`): Only `IFactoryOnComplete` on result. No async hooks.
2. **Async Read** (`Func<Task<T>>`): Only `IFactoryOnComplete` on result. No async hooks.
3. **Async Nullable Read** (`Func<Task<T?>>`): Only `IFactoryOnComplete` on result. No async hooks.
4. **Sync Write** (`Action`): `IFactoryOnStart` before, `IFactoryOnComplete` after. No async hooks, no Cancelled.
5. **Sync Write Bool** (`Func<bool>`): Same as #4 plus `if (!succeeded) return default` before `IFactoryOnComplete`.
6. **Async Write** (`Func<Task>`): All 6 lifecycle hooks. `OperationCanceledException` handling.
7. **Async Write Bool** (`Func<Task<bool>>`): All 6 lifecycle hooks plus `if (!succeeded) return default`.

The plan acknowledges this in the "Generator Complexity Impact" section ("7 method signature variants") but does not provide inline examples for all of them. This is acceptable for an exploration plan -- the two examples demonstrate the pattern, and the remaining 5 follow the same structure. However, during implementation, the generator must correctly emit the right lifecycle hooks for each variant.

**Verdict:** Not a blocking concern. The pattern is clear. The developer implementing Phase 2 must reference `FactoryCore.cs` for the exact lifecycle hook combinations per variant.

#### Concern 2: Read Pattern Inline Example -- Missing `IFactoryOnCompleteAsync` Consideration

The plan's Read inline example (lines 536-539) checks `is IFactoryOnComplete` but does NOT check `is IFactoryOnCompleteAsync`. Looking at `FactoryCore.cs`, the Read variants (lines 48-136) indeed only check `IFactoryOnComplete` (sync), NOT `IFactoryOnCompleteAsync`. So the plan's example is **correct** for the Read pattern. The current `FactoryCore` Read methods do not invoke `IFactoryOnCompleteAsync` even in async Read variants.

**Verdict:** Not a concern -- the plan matches the existing behavior.

#### Concern 3: Authorization Checks Not Shown in Inline Examples

The plan's inline code examples do not include authorization checks (`RenderAuthorizationChecks`). The current `RenderReadLocalMethod` (line 364) and `RenderLocalMethod` for writes (line 551) both render authorization checks before the method body. The inline pipeline must preserve these.

**Verdict:** Not a blocking concern. Authorization is orthogonal to the trimming guard and happens before the guarded block. The plan's examples are simplified for clarity. The guard should wrap the code AFTER authorization, or wrap everything including authorization (since auth types would also be server-only in some cases).

#### Concern 4: DI Registration Code May Defeat Trimming

Rule 8's trace notes that if server-only types are referenced in DI registration code (e.g., `services.AddScoped<IOrderRepository, OrderRepository>()` in `Program.cs`), those references must also be guarded or in a server-only assembly. The trimming verification project (Phase 2) must account for this -- the DI registrations for server-only services should be in a server-only configuration path.

**Verdict:** Not a plan flaw. This is a configuration concern for the trimming verification project. Phase 2 should structure DI registrations to ensure server-only registrations are not in the published assembly.

#### Concern 5: `InvokingFactoryOnComplete` / `InvokingFactoryOnStart` Log Messages

The current `FactoryCore<T>` emits `logger.InvokingFactoryOnComplete(TypeName)` and `logger.InvokingFactoryOnStart(TypeName)` log messages (Debug level) before invoking lifecycle hooks. The plan's inline examples omit these log calls. The inline code should include them for behavioral parity.

**Verdict:** Minor. These are Debug-level log messages. The implementation should include them for completeness, but their absence would not affect correctness.

#### Concern 6: FactorySaveBase Impact

`FactorySaveBase<T>` (in `src/RemoteFactory/FactorySaveBase.cs`) inherits from `FactoryBase<T>` and implements `IFactorySave<T>`. Generated factories that have Save methods inherit from `FactorySaveBase<T>` instead of `FactoryBase<T>` (see `ClassFactoryRenderer.cs` line 135: `var baseClass = model.HasDefaultSave ? "FactorySaveBase" : "FactoryBase"`). The plan's breaking changes table mentions `FactorySaveBase<T>` but the inline redesign does not detail how Save method generation changes with respect to `IFactorySave<T>` interface implementation.

With the inline pipeline, generated factories would no longer need `FactoryBase<T>` or `FactorySaveBase<T>` as base classes. However, `IFactorySave<T>` is still needed as a marker interface for Save routing. The generated factory would need to implement `IFactorySave<T>` directly instead of inheriting it from `FactorySaveBase<T>`.

**Verdict:** Minor implementation detail. The plan acknowledges this ("FactorySaveBase requires adjustment") but the inline pipeline does not need to inherit from any base class -- it can implement `IFactorySave<T>` directly. This should be noted in the implementation contract.

#### Concern 7: Exploration Scope vs Production Scope

This is an exploration plan, not a production implementation. The inline pipeline (Design A) is presented as the recommended architecture, but the plan's implementation phases still start with the **original** guard placement (Phase 1: "Modify `ClassFactoryRenderer.RenderReadLocalMethod()` to wrap the method body in `if (NeatooRuntime.IsServerRuntime)` guard") before testing the inline approach. The Updated Implementation Phases section (after the Alternative Architecture) replaces this with the inline approach.

There are two sets of implementation phases in the plan: the original (lines 269-307) and the updated (lines 726-750). The updated phases should be the definitive ones.

**Verdict:** The plan should clarify which phases are authoritative. The "Updated Implementation Phases" section (lines 726-750) should be marked as the authoritative implementation plan, and the original phases (lines 269-307) should be marked as superseded. Currently both exist without clear precedence.

### Agent Phasing Review

The phasing is practical:

- **Phase 1 (NeatooRuntime + Generator Guards, Fresh Agent):** Correct to start fresh. This is generator modification work.
- **Phase 2 (Trimming Verification, Fresh Agent):** Correct. Different domain (project setup, publish inspection). Clean context is appropriate.
- **Phase 3 (Virtual Dispatch Investigation, Resume Phase 2):** Correct to resume. Needs Phase 2 results.
- **Phase 4 (All Factory Patterns, Resume Phase 3):** Correct. Extension of earlier work.
- **Phase 5 (Documentation + Decision, Fresh Architect):** Correct. Analysis phase.

The note about Phases 3-4 potentially being unnecessary is important and correct. If Phase 2 proves the inline pipeline works, Phase 3 (virtual dispatch investigation) becomes unnecessary since the inline pipeline eliminates virtual dispatch entirely.

However, there is a phasing ambiguity: the "Updated Implementation Phases" (after the Alternative Architecture section) lists 6 phases, while the Agent Phasing table lists 5 phases matching the original phases. **The Agent Phasing table should be updated to match the Updated Implementation Phases.**

### Verdict: **Approved** (with concerns noted above)

The plan is sound for an exploration/feasibility study. The core insight -- that inline pipeline eliminates the virtual dispatch problem -- is correct and well-supported by codebase analysis. The concerns are implementation details that can be addressed during Phase 2.

---

## Implementation Contract

**Approved by:** Developer review (2026-03-03)
**Authoritative Phases:** The "Updated Implementation Phases" (Alternative Architecture section, lines 726-750) are authoritative. The original phases (lines 269-307) are superseded by the redesign.

### In Scope

1. **NeatooRuntime class creation** (`src/RemoteFactory/NeatooRuntime.cs`)
   - `[FeatureSwitchDefinition]` property with `#if NET9_0_OR_GREATER` conditional
   - Default to `true` via `AppContext.TryGetSwitch`

2. **Generator modifications for inline pipeline (Class Factory pattern)**
   - Modify `ClassFactoryRenderer.RenderReadLocalMethod()` to inline the FactoryCore pipeline (logging, lifecycle hooks, exception handling, direct method call) instead of calling `DoFactoryMethodCall`
   - Modify `ClassFactoryRenderer.RenderLocalMethod()` (Write methods) similarly with all 6 lifecycle hooks for async variants
   - Modify `ClassFactoryRenderer.RenderSaveLocalMethod()` to add guard
   - Wrap all inline pipeline code in `if (NeatooRuntime.IsServerRuntime)` guard with `throw InvalidOperationException` else branch
   - Handle all 7 FactoryCore method signature variants correctly (sync/async, read/write, bool/non-bool, nullable)
   - Preserve authorization checks (RenderAuthorizationChecks) -- these should be BEFORE the guard (auth runs on both client and server) or INSIDE the guard (if auth uses server-only types). Implementation should follow existing pattern.
   - Include `InvokingFactoryOnComplete`/`InvokingFactoryOnStart` log messages for behavioral parity

3. **Remove FactoryBase<T> / IFactoryCore<T> from generated factory inheritance**
   - Generated factory classes no longer inherit from `FactoryBase<T>` or `FactorySaveBase<T>`
   - Generated factory classes implement `IFactorySave<T>` directly (for Save-capable factories)
   - Generated factory constructors no longer take `IFactoryCore<T>` parameter

4. **Trimming verification project** (`src/Tests/RemoteFactory.TrimmingTests/` or similar)
   - Blazor WASM project with `PublishTrimmed=true`
   - Feature switch configured with `RuntimeHostConfigurationOption`
   - Domain class with `[Factory]`, `[Remote, Create]` method, server-only `[Service]` dependency
   - "Server-only" type simulating EF Core / repository
   - Publish and inspect output for server-only type absence

5. **Feature switch guards for Static and Interface factory patterns** (if Phase 2 succeeds)
   - Guard around `RenderLocalDelegateRegistration` in `StaticFactoryRenderer`
   - Guard around `RenderLocalMethod` body in `InterfaceFactoryRenderer`

6. **Documentation of findings** -- answer the 4 key questions with evidence

### Out of Scope

1. **Production-quality implementation** -- this is a prototype/exploration
2. **Resolving existing trimmer warnings** (reflection in `HandleRemoteDelegateRequest`, etc.)
3. **Modifying Design projects** -- Design project verification is N/A for this exploration
4. **Performance benchmarking** -- generated code size impact measurement deferred
5. **Middleware-style pipeline replacement** for IFactoryCore extensibility (future enhancement)
6. **net8.0 trimming support** -- `[FeatureSwitchDefinition]` is .NET 9+ only; net8.0 is out of scope

### Verification Gates

| Gate | Check | Action if Fails |
|------|-------|----------------|
| After Phase 1 (NeatooRuntime + generator changes) | All existing tests pass (`dotnet test src/Neatoo.RemoteFactory.sln`) | Fix generator output; do not proceed to Phase 2 |
| After Phase 2 (trimming verification) | Published Blazor WASM output does NOT contain server-only types | If types present: investigate guard placement. If virtual dispatch is the cause, Phase 3 is needed. |
| After Phase 2 (negative result) | If trimmer does NOT remove server-only types | Document findings, answer key questions, report to architect for decision |
| After Phase 4 (all patterns) | Static and Interface factory trimming also verified | If any pattern fails, document which and why |

### Stop Conditions

1. **Phase 2 proves trimming infeasible** -- If the .NET 9+ trimmer cannot constant-fold `[FeatureSwitchDefinition]` properties even with the inline pipeline (no virtual dispatch), STOP and document findings. This would mean the feature switch approach is fundamentally unsuitable.

2. **Out-of-scope test failures** -- If tests outside `FactoryGenerator/Core/` (the FactoryCore extensibility tests) fail after generator changes, STOP and report. The FactoryCore tests themselves ARE expected to fail/need modification since the inline pipeline removes the FactoryCore dependency.

3. **Reflection usage needed** -- If the implementation requires adding reflection to make trimming work, STOP and ask for approval.

4. **Breaking change discovered beyond IFactoryCore** -- If the inline pipeline breaks user-facing functionality beyond the IFactoryCore extensibility mechanism (which has no production usage), STOP and escalate to architect.

### Test Scenario Mapping

| Scenario # | Test Approach | Phase |
|-----------|---------------|-------|
| 1 (Feature switch basics) | Publish-and-inspect: verify trimmer constant-folds the property | Phase 2 |
| 2 (Class factory Create trimming) | Publish-and-inspect: verify `IOrderLineListFactory` absent from trimmed output | Phase 2 |
| 3 (Class factory Save/Write trimming) | Publish-and-inspect: verify `IOrderRepository` absent from trimmed output | Phase 2 |
| 4 (Static factory Execute trimming) | Publish-and-inspect: verify `IMyService` absent from trimmed output | Phase 4 |
| 5 (Interface factory Local trimming) | Publish-and-inspect: verify server implementation absent from trimmed output | Phase 4 |
| 6 (Virtual method defeats trimming) | Publish-and-inspect with **original** architecture (before inline redesign) -- may be skipped if inline pipeline is implemented first | Phase 3 (optional) |
| 7 (No feature switch configured) | xUnit test: verify `NeatooRuntime.IsServerRuntime` returns `true` when no switch configured | Phase 1 |
| 8 (RemoteOnly mode unaffected) | Existing tests: verify FactoryMode.RemoteOnly still works | Phase 1 (existing tests) |
| 9 (Inline pipeline Create trimming) | Same as Scenario 2 (inline pipeline IS the implementation) | Phase 2 |
| 10 (Inline pipeline lifecycle hooks) | xUnit test: verify lifecycle hooks fire correctly with inline pipeline (use Design project's `Order` which implements `IFactoryOnStartAsync`/`IFactoryOnCompleteAsync`) | Phase 1 |
| 11 (IFactoryCore extensibility removed) | Verify FactoryCore tests fail or are updated. Document the breaking change. | Phase 1 |

### Expected Test Modifications

The following test files are IN SCOPE for modification because they directly test the `IFactoryCore<T>` extensibility mechanism being removed:

- `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Core/FactoryCoreTests.cs` -- Tests custom `IFactoryCore<T>` registration. Will fail with inline pipeline. Should be updated or marked as testing deprecated functionality.
- `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Core/FactoryCoreAsyncTests.cs` -- Tests custom `IFactoryCore<T>` async overrides. Same as above.
- `src/Tests/RemoteFactory.UnitTests/TestTargets/Core/FactoryCoreTargets.cs` -- Test targets for above.

All other test files are OUT OF SCOPE for modification.

---

## Implementation Progress

[Filled during implementation]

---

## Completion Evidence

[Developer fills this section, then sets status to "Awaiting Verification" and STOPS]

---

## Documentation

**Agent:** [pending]
**Completed:** [pending]

### Expected Deliverables

- [ ] Findings document answering the four key questions (may be in this plan's Results section)
- [ ] If feasible: production implementation plan reference (separate plan)
- [ ] Skill updates: N/A for exploration
- [ ] Sample updates: N/A for exploration

### Files Updated

[Documentation agent fills this after completing work]

---

## Architect Verification

[Architect fills this section after independently verifying the developer's work]
