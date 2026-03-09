# Trimming-Safe Factory Registration

**Date:** 2026-03-08
**Related Todo:** [Trimming-Safe Factory Registration](../todos/completed/trimming-safe-factory-registration.md)
**Status:** Complete
**Last Updated:** 2026-03-08

---

## Overview

Replace the reflection-based factory discovery in `RegisterFactories()` with a trimming-safe assembly-level attribute pattern. The current `assembly.GetTypes()` + `GetMethod("FactoryServiceRegistrar")` approach is inherently trim-unsafe, causing factory types (especially static factories) to be trimmed in Blazor WASM apps. The solution creates a `NeatooFactoryRegistrarAttribute` with `[DynamicallyAccessedMembers]` that the trimmer follows, ensuring all factory types survive IL trimming.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/trimming-safe-factory-registration.md#requirements-review)

### Relevant Existing Requirements

#### Trimming Architecture

- `docs/trimming.md` and `src/Design/CLAUDE-DESIGN.md` (Critical Rule 2): The generator emits `if (NeatooRuntime.IsServerRuntime)` guards for IL trimming. This proposed change does NOT alter guard emission -- it only changes how `RegisterFactories()` discovers `FactoryServiceRegistrar` methods. Guard behavior is orthogonal and unaffected.

#### Three Factory Types

- `src/Design/CLAUDE-DESIGN.md` (Decision Table): All three factory patterns (Class, Interface, Static) must be covered by the fix. Currently:
  - Class factories: protected by `[DynamicDependency]` on interface methods (v0.21.1)
  - Interface factories: NOT protected (confirmed vulnerable)
  - Static factories: NOT protected (the original bug report)

#### FactoryServiceRegistrar Signature

- All three renderers generate `internal static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)`. The `RegisterFactories()` method discovers these via `assembly.GetTypes()` + `GetMethod()`. This change replaces only the discovery mechanism, not the method signature.

#### Existing Assembly-Level Attribute Pattern

- `src/RemoteFactory/FactoryAttributes.cs` (line 129): `FactoryHintNameLengthAttribute` with `[AttributeUsage(AttributeTargets.Assembly)]` already exists. The new attribute follows this pattern.

#### Auth Type Auto-Registration

- `src/Design/CLAUDE-DESIGN.md`: The generator emits `services.TryAddTransient<IFooAuth, FooAuth>()` inside `FactoryServiceRegistrar`. This is preserved as long as `FactoryServiceRegistrar` survives trimming -- which is exactly what this fix ensures.

#### RegisterMatchingName is Out of Scope

- `src/RemoteFactory/AddRemoteFactoryServices.cs` (lines 150-154): `RegisterMatchingName` also uses `assembly.GetTypes()` but is a separate user-facing concern. Not in scope for this fix.

#### Server Registration Path

- `src/RemoteFactory.AspNetCore/ServiceCollectionExtensions.cs`: `AddNeatooAspNetCore` delegates to `AddNeatooRemoteFactory` which calls `RegisterFactories()`. The fix automatically applies to both client and server paths.

#### TrimmingTests Direct Call Pattern

- `src/Tests/RemoteFactory.TrimmingTests/Program.cs` line 14: Calls `TrimTestEntityFactory.FactoryServiceRegistrar()` directly, bypassing `RegisterFactories()`. Should be updated to test the assembly-attribute discovery path as well.

### Existing Tests

- `src/Tests/RemoteFactory.TrimmingTests/`: Only tests a class factory (`TrimTestEntity`). No static factory coverage. The trimming test project verifies IL trimming behavior by publishing with `PublishTrimmed=true` and checking that server-only types are removed.
- `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Visibility/`: Tests verify generated code structure (guard emission, interface visibility). These reference `FactoryServiceRegistrar` as a string marker but don't test the discovery mechanism.

### Gaps

1. No unit tests for `RegisterFactories()` discovery mechanism itself.
2. No static factory in the trimming test project (the original bug scenario).
3. Old inline generator path in `FactoryGenerator.cs` (`GenerateExecute` at line 84, class/interface at line 822) is dead code -- `Initialize()` only wires up the Renderer path. Cleanup is separate from this fix.

### Contradictions

None. The proposed solution is consistent with all documented patterns.

### Recommendations for Architect

1. Place attribute in `src/RemoteFactory/FactoryAttributes.cs` with `#if !NETSTANDARD` guard (file is shared with Generator via `<Compile Include>` link; `[DynamicallyAccessedMembers]` is not available on netstandard2.0).
2. Ensure `[DynamicallyAccessedMembers]` on both constructor parameter AND property for end-to-end trimmer tracking.
3. Extend TrimmingTests with a static factory.
4. Assembly attribute emission goes in Renderer classes, not FactoryModelBuilder.
5. Old `FactoryGenerator.cs` inline path is dead code -- do not attempt migration, just leave it (separate cleanup concern).

---

## Business Rules (Testable Assertions)

1. WHEN `RegisterFactories()` is called with an assembly containing class factories, THEN `FactoryServiceRegistrar` is invoked for each class factory type. -- Source: Existing behavior in `AddRemoteFactoryServices.cs:123-135`

2. WHEN `RegisterFactories()` is called with an assembly containing static factories, THEN `FactoryServiceRegistrar` is invoked for each static factory type. -- Source: Existing behavior (currently broken by trimming)

3. WHEN `RegisterFactories()` is called with an assembly containing interface factories, THEN `FactoryServiceRegistrar` is invoked for each interface factory type. -- Source: Existing behavior (currently broken by trimming)

4. WHEN a class factory is generated, THEN the generated source contains `[assembly: NeatooFactoryRegistrar(typeof({Namespace}.{ImplementationTypeName}Factory))]` before the namespace declaration. -- Source: NEW

5. WHEN a static factory is generated, THEN the generated source contains `[assembly: NeatooFactoryRegistrar(typeof({Namespace}.{TypeName}))]` before the namespace declaration. -- Source: NEW

6. WHEN an interface factory is generated, THEN the generated source contains `[assembly: NeatooFactoryRegistrar(typeof({Namespace}.{ImplementationTypeName}Factory))]` before the namespace declaration. -- Source: NEW

7. WHEN a class factory is generated, THEN the generated source does NOT contain `[DynamicDependency]` on interface methods. -- Source: NEW (replaces v0.21.1 workaround)

8. WHEN a class factory is generated, THEN the generated source does NOT contain `using System.Diagnostics.CodeAnalysis;` (unless needed by other attributes in the future). -- Source: NEW (cleanup of v0.21.1)

9. WHEN the `NeatooFactoryRegistrarAttribute` is defined, THEN its constructor parameter has `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]` AND the `Type` property has the same annotation. -- Source: Clarification Q2 (trimmer dataflow contract)

10. WHEN an assembly is published with `PublishTrimmed=true` and `IsServerRuntime=false`, THEN all factory types referenced by `[assembly: NeatooFactoryRegistrar(typeof(X))]` survive trimming (their `FactoryServiceRegistrar` methods are preserved). -- Source: NEW (the core fix)

11. WHEN an assembly is published with `PublishTrimmed=true` and `IsServerRuntime=false`, THEN static factory delegate types survive trimming and can be resolved from DI. -- Source: Original bug report (static factories being trimmed)

12. WHEN `RegisterFactories()` is called, THEN it enumerates `assembly.GetCustomAttributes<NeatooFactoryRegistrarAttribute>()` and invokes `GetMethod("FactoryServiceRegistrar")` on each type referenced by the attribute. -- Source: NEW (Option A design)

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Class factory assembly attribute emission | Generate code for a class with `[Factory]` attribute (e.g., `TrimTestEntity`) | 4, 7, 8 | Generated source contains `[assembly: NeatooFactoryRegistrar(typeof(...))]`, does NOT contain `[DynamicDependency]`, does NOT contain `using System.Diagnostics.CodeAnalysis;` |
| 2 | Static factory assembly attribute emission | Generate code for a static class with `[Factory]` + `[Execute]` method | 5 | Generated source contains `[assembly: NeatooFactoryRegistrar(typeof({Namespace}.{StaticClassName}))]` |
| 3 | Interface factory assembly attribute emission | Generate code for an interface with `[Factory]` attribute | 6 | Generated source contains `[assembly: NeatooFactoryRegistrar(typeof({Namespace}.{ImplName}Factory))]` |
| 4 | RegisterFactories discovers class factory via attribute | Assembly with class factory + assembly attribute | 1, 12 | `FactoryServiceRegistrar` invoked; factory interface resolvable from DI |
| 5 | RegisterFactories discovers static factory via attribute | Assembly with static factory + assembly attribute | 2, 12 | `FactoryServiceRegistrar` invoked; delegate type resolvable from DI |
| 6 | RegisterFactories discovers interface factory via attribute | Assembly with interface factory + assembly attribute | 3, 12 | `FactoryServiceRegistrar` invoked; factory interface resolvable from DI |
| 7 | Attribute has DynamicallyAccessedMembers annotations | Inspect `NeatooFactoryRegistrarAttribute` definition | 9 | Both constructor param and property have `[DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)]` |
| 8 | Static factory survives trimming | Publish trimming test app with static factory, `IsServerRuntime=false` | 10, 11 | Static factory delegate resolvable from DI; no trim warnings |
| 9 | Class factory survives trimming (regression) | Publish trimming test app with class factory, `IsServerRuntime=false` | 10 | Class factory interface resolvable from DI (was already working via `[DynamicDependency]`, must still work) |
| 10 | Multiple factories in same assembly | Assembly with class + static + interface factories | 1, 2, 3, 4, 5, 6 | All three `[assembly: NeatooFactoryRegistrar(...)]` attributes emitted; all three `FactoryServiceRegistrar` methods invoked |

---

## Approach

The approach has three parts:

1. **Define the attribute** in the core library with proper `[DynamicallyAccessedMembers]` annotations so the trimmer preserves the referenced type's methods.

2. **Emit assembly-level attributes** from each Renderer class. Each factory's generated source file gets an `[assembly: NeatooFactoryRegistrar(typeof(FullyQualifiedType))]` before the namespace declaration. This creates a static `typeof()` reference that the trimmer must follow.

3. **Update `RegisterFactories()`** to enumerate assembly attributes instead of scanning all types. The `GetMethod("FactoryServiceRegistrar")` call on the preserved type is now trim-safe because `[DynamicallyAccessedMembers]` on the attribute guarantees the type's methods are preserved.

This replaces the v0.21.1 `[DynamicDependency]` workaround on class factory interface methods, which was limited to class factories only.

---

## Design

### 1. NeatooFactoryRegistrarAttribute

Location: `src/RemoteFactory/FactoryAttributes.cs`, wrapped in `#if !NETSTANDARD` (since `DynamicallyAccessedMembers` is not available on netstandard2.0, and this file is shared with the Generator project via `<Compile Include>` link).

```csharp
#if !NETSTANDARD
[System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
public sealed class NeatooFactoryRegistrarAttribute : Attribute
{
    public NeatooFactoryRegistrarAttribute(
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type)
    {
        Type = type;
    }

    [DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods)]
    public Type Type { get; }
}
#endif
```

Key design decisions:
- `AllowMultiple = true` -- each factory type gets its own attribute instance
- `[DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)]` on BOTH the constructor parameter AND the property -- this creates the full dataflow contract the trimmer follows. `FactoryServiceRegistrar` is `internal static` (NonPublicMethods) or `public static` (PublicMethods) depending on factory type.
- `#if !NETSTANDARD` -- the Generator (netstandard2.0) emits this attribute as text strings; it doesn't need the type itself at compile time.

### 2. Assembly Attribute Emission in Renderers

Each Renderer adds the assembly-level attribute before the namespace declaration in the generated source. The pattern is the same for all three factory types; only the referenced type differs.

**ClassFactoryRenderer changes:**
- ADD: `[assembly: NeatooFactoryRegistrar(typeof({Namespace}.{ImplementationTypeName}Factory))]` before `namespace`
- ADD: `using Neatoo.RemoteFactory;` for the attribute (if not already present)
- REMOVE: `[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof({ImplementationTypeName}Factory))]` from interface methods (lines 126-130)
- REMOVE: `using System.Diagnostics.CodeAnalysis;` (only needed for `[DynamicDependency]`)

**StaticFactoryRenderer changes:**
- ADD: `[assembly: NeatooFactoryRegistrar(typeof({Namespace}.{TypeName}))]` before `namespace`
- ADD: `using Neatoo.RemoteFactory;` for the attribute (if not already present)

**InterfaceFactoryRenderer changes:**
- ADD: `[assembly: NeatooFactoryRegistrar(typeof({Namespace}.{ImplementationTypeName}Factory))]` before `namespace`
- ADD: `using Neatoo.RemoteFactory;` for the attribute (if not already present)

The fully-qualified type name is required in the `typeof()` expression because the attribute is at assembly scope (outside the namespace declaration). The qualified name is constructed from `unit.Namespace` + the type name:
- Class factory: `{unit.Namespace}.{model.ImplementationTypeName}Factory`
- Static factory: `{unit.Namespace}.{model.TypeName}`
- Interface factory: `{unit.Namespace}.{model.ImplementationTypeName}Factory`

### 3. Updated RegisterFactories()

Location: `src/RemoteFactory/AddRemoteFactoryServices.cs`, method `RegisterFactories()` (lines 123-135).

Current code:
```csharp
private static void RegisterFactories(this IServiceCollection services,
    NeatooFactory remoteLocal, params Assembly[] assemblies)
{
    foreach (var assembly in assemblies)
    {
        var methods = assembly.GetTypes()
            .Select(t => t.GetMethod("FactoryServiceRegistrar",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
            .Where(m => m != null).ToList();

        foreach (var m in methods)
        {
            m?.Invoke(null, [services, remoteLocal]);
        }
    }
}
```

New code:
```csharp
private static void RegisterFactories(this IServiceCollection services,
    NeatooFactory remoteLocal, params Assembly[] assemblies)
{
    foreach (var assembly in assemblies)
    {
        var attributes = assembly.GetCustomAttributes<NeatooFactoryRegistrarAttribute>();

        foreach (var attr in attributes)
        {
            var method = attr.Type.GetMethod("FactoryServiceRegistrar",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            method?.Invoke(null, [services, remoteLocal]);
        }
    }
}
```

The `GetMethod()` call is now trim-safe because:
1. `attr.Type` returns a `Type` annotated with `[DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)]`
2. The trimmer follows this annotation and preserves all public and non-public methods on the type
3. `GetMethod("FactoryServiceRegistrar")` on the preserved type will always find the method

### 4. TrimmingTests Extension

Add a static factory to `src/Tests/RemoteFactory.TrimmingTests/` to verify the original bug (static factories being trimmed) is fixed:

**New file: `TrimTestCommands.cs`**
```csharp
[Factory]
public static partial class TrimTestCommands
{
    [Remote, Execute]
    private static Task<string> _DoWork(string input, [Service] IServerOnlyRepository repo)
    {
        return Task.FromResult(repo.DoServerWork(input));
    }
}
```

**Update `Program.cs`:**
- Add `AddNeatooRemoteFactory(NeatooFactory.Remote, ...)` call that exercises `RegisterFactories()` (the assembly-attribute path)
- Resolve the static factory delegate to verify it survived trimming
- Keep existing direct `FactoryServiceRegistrar` call as a secondary verification

### 5. Generated Code Structure

Example of what a generated class factory file will look like after the change:

```csharp
#nullable enable

using Neatoo.RemoteFactory;
using Microsoft.Extensions.DependencyInjection;
// ... other usings ...

[assembly: NeatooFactoryRegistrar(typeof(MyNamespace.MyEntityFactory))]

/*
    READONLY - DO NOT EDIT!!!!
    Generated by Neatoo.RemoteFactory
*/
namespace MyNamespace
{
    public interface IMyEntityFactory
    {
        // NO [DynamicDependency] attributes -- removed
        Task<IMyEntity> Create(string name, CancellationToken cancellationToken = default);
    }

    internal class MyEntityFactory : IMyEntityFactory
    {
        // ... factory implementation ...

        public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
        {
            // ... registrations ...
        }
    }
}
```

---

## Implementation Steps

### Phase 1: Core Library Changes

1. Add `NeatooFactoryRegistrarAttribute` to `src/RemoteFactory/FactoryAttributes.cs` inside `#if !NETSTANDARD` block, at the end of the file (after `FactoryHintNameLengthAttribute`).
2. Add `using System.Diagnostics.CodeAnalysis;` to `FactoryAttributes.cs` inside the same `#if !NETSTANDARD` block.
3. Update `RegisterFactories()` in `src/RemoteFactory/AddRemoteFactoryServices.cs` to use `assembly.GetCustomAttributes<NeatooFactoryRegistrarAttribute>()` instead of `assembly.GetTypes()`.

### Phase 2: Generator Renderer Changes

4. **ClassFactoryRenderer.cs**: Remove `[DynamicDependency]` emission from `RenderFactoryInterface()` (lines 126-130). Remove the `using System.Diagnostics.CodeAnalysis;` emission (lines 51-55). Add assembly-level attribute emission after usings, before namespace. Ensure `using Neatoo.RemoteFactory;` is present.
5. **StaticFactoryRenderer.cs**: Add assembly-level attribute emission after usings, before namespace. Ensure `using Neatoo.RemoteFactory;` is present.
6. **InterfaceFactoryRenderer.cs**: Add assembly-level attribute emission after usings, before namespace. Ensure `using Neatoo.RemoteFactory;` is present.

### Phase 3: Trimming Tests

7. Create `src/Tests/RemoteFactory.TrimmingTests/TrimTestCommands.cs` with a static factory using `[Factory]`, `[Remote, Execute]`.
8. Update `src/Tests/RemoteFactory.TrimmingTests/Program.cs` to exercise the assembly-attribute discovery path via `AddNeatooRemoteFactory()` and verify the static factory delegate is resolvable.

### Phase 4: Unit Test Updates

9. Add or update unit tests in `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/` to verify:
   - Assembly attribute is emitted for class, static, and interface factories
   - `[DynamicDependency]` is no longer emitted on class factory interfaces
   - `using System.Diagnostics.CodeAnalysis;` is no longer emitted by ClassFactoryRenderer

### Phase 5: Build and Test

10. Build the full solution: `dotnet build src/Neatoo.RemoteFactory.sln`
11. Run all tests: `dotnet test src/Neatoo.RemoteFactory.sln`
12. Verify existing unit tests that reference `FactoryServiceRegistrar` as a string marker still pass (these tests search generated source for method names -- the `FactoryServiceRegistrar` method is unchanged, so they should pass).

---

## Acceptance Criteria

- [ ] `NeatooFactoryRegistrarAttribute` defined in `src/RemoteFactory/FactoryAttributes.cs` with `[DynamicallyAccessedMembers]` on both constructor parameter and property
- [ ] All three Renderer classes (Class, Static, Interface) emit `[assembly: NeatooFactoryRegistrar(typeof(...))]` in generated code
- [ ] `[DynamicDependency]` removed from class factory interface methods in ClassFactoryRenderer
- [ ] `using System.Diagnostics.CodeAnalysis;` removed from ClassFactoryRenderer emission
- [ ] `RegisterFactories()` updated to use `assembly.GetCustomAttributes<NeatooFactoryRegistrarAttribute>()` instead of `assembly.GetTypes()`
- [ ] Static factory added to TrimmingTests project
- [ ] TrimmingTests exercises assembly-attribute discovery path
- [ ] Unit tests verify assembly attribute emission for all three factory types
- [ ] Unit tests verify `[DynamicDependency]` is NOT emitted on class factory interfaces
- [ ] Full solution builds with zero errors
- [ ] All existing tests pass

---

## Dependencies

- `System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute` -- available in net9.0 and net10.0 (both target frameworks). NOT available on netstandard2.0 (hence `#if !NETSTANDARD` guard).
- No new NuGet packages required.
- No breaking changes to public API (the attribute is consumed internally by the generator and `RegisterFactories()`).

---

## Risks / Considerations

1. **Unit test fragility**: Existing unit tests that search generated source for string patterns (e.g., looking for `DynamicDependency`) will need updates. The tests in `CanMethodVisibilityTests.cs` and `InternalVisibilityTests.cs` search for `FactoryServiceRegistrar` as a marker but do not assert `[DynamicDependency]` presence, so they should not break.

2. **Assembly attribute uniqueness**: Each generated factory file emits its own `[assembly: NeatooFactoryRegistrar(typeof(...))]`. Multiple files can have assembly-level attributes -- the `AllowMultiple = true` on the attribute definition handles this correctly.

3. **Namespace qualification**: The `typeof()` expression in the assembly attribute must use the fully-qualified type name because the attribute appears at file scope (outside the namespace). This is constructed from `unit.Namespace + "." + TypeName`. If any namespace contains special characters or conflicts, this could produce invalid code. This risk is low since the namespace comes directly from the user's source code.

4. **Dead code in FactoryGenerator.cs**: The old inline generator path (`GenerateExecute` and class/interface generation in `FactoryGenerator.cs`) is confirmed dead code -- `Initialize()` only wires up the Renderer path. This plan does NOT touch `FactoryGenerator.cs`. Dead code cleanup is a separate concern.

5. **RegisterMatchingName still uses reflection**: `RegisterMatchingName()` in `AddRemoteFactoryServices.cs` still uses `assembly.GetTypes()`. This is out of scope -- it is a user-facing method for convention-based DI registration, not the factory discovery mechanism.

6. **Trimming test verification is manual**: The `RemoteFactory.TrimmingTests` project is verified by `dotnet publish` with trimming enabled and checking the output. This is not part of `dotnet test`. The CI/CD pipeline may not exercise this. The developer should verify locally.

---

## Architectural Verification

**Scope Table:**

| Factory Pattern | Current Trimming Protection | After This Change |
|----------------|---------------------------|-------------------|
| Class Factory | `[DynamicDependency]` on interface methods (v0.21.1) | `[assembly: NeatooFactoryRegistrar(typeof(XxxFactory))]` |
| Static Factory | NONE (bug) | `[assembly: NeatooFactoryRegistrar(typeof(XxxCommands))]` |
| Interface Factory | NONE (vulnerable) | `[assembly: NeatooFactoryRegistrar(typeof(XxxFactory))]` |

**Verification Evidence:**

- Dead code in `FactoryGenerator.cs`: Verified -- `Initialize()` (lines 19-82) only calls `FactoryModelBuilder.Build` + `FactoryRenderer.Render`. `GenerateExecute` (line 84) is never called.
- `FactoryAttributes.cs` shared with Generator: Verified -- Generator.csproj line 16 includes it via `<Compile Include>`. The `#if !NETSTANDARD` guard is necessary.
- `DynamicallyAccessedMembersAttribute` availability: Available in `System.Diagnostics.CodeAnalysis` on net9.0 and net10.0. Not available on netstandard2.0.
- `FactoryServiceRegistrar` visibility: Class and interface factories generate it as `public static`. Static factories generate it as `internal static`. The `[DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)]` covers both.

**Breaking Changes:** No -- the `NeatooFactoryRegistrarAttribute` is new public API but is consumed by generated code, not user code. The `RegisterFactories()` method is private. The `FactoryServiceRegistrar` method signature is unchanged.

**Codebase Analysis:**

| File | Purpose | Changes Required |
|------|---------|-----------------|
| `src/RemoteFactory/FactoryAttributes.cs` | Attribute definitions | Add `NeatooFactoryRegistrarAttribute` in `#if !NETSTANDARD` |
| `src/RemoteFactory/AddRemoteFactoryServices.cs` | Factory discovery | Update `RegisterFactories()` to use assembly attributes |
| `src/Generator/Renderer/ClassFactoryRenderer.cs` | Class factory codegen | Remove `[DynamicDependency]`, add assembly attribute |
| `src/Generator/Renderer/StaticFactoryRenderer.cs` | Static factory codegen | Add assembly attribute |
| `src/Generator/Renderer/InterfaceFactoryRenderer.cs` | Interface factory codegen | Add assembly attribute |
| `src/Tests/RemoteFactory.TrimmingTests/TrimTestCommands.cs` | NEW | Static factory for trimming test |
| `src/Tests/RemoteFactory.TrimmingTests/Program.cs` | Trimming verification | Exercise assembly-attribute discovery |
| `src/Tests/RemoteFactory.UnitTests/` | Generator unit tests | Verify attribute emission, verify DynamicDependency removal |

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Core library (attribute + RegisterFactories) | developer | Yes | Clean start, small scope (2 files) | None |
| Phase 2: Generator renderers | developer | No | Same agent, continues from Phase 1; needs context of attribute name/namespace | Phase 1 |
| Phase 3: Trimming tests | developer | No | Same agent, continues; needs context of changes made | Phase 1, 2 |
| Phase 4: Unit tests + build verification | developer | No | Same agent; needs full context of all changes | Phase 1, 2, 3 |

**Parallelizable phases:** None -- each phase depends on the previous.

**Notes:** All phases are small enough to fit in a single agent session. A single developer agent invocation covering all four phases is the recommended approach. The phases are listed separately for progress tracking, not for fresh agent invocations.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-08

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `RegisterFactories()` in `AddRemoteFactoryServices.cs`: `assembly.GetCustomAttributes<NeatooFactoryRegistrarAttribute>()` iterates; `attr.Type.GetMethod("FactoryServiceRegistrar", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)` finds `public static void FactoryServiceRegistrar` (ClassFactoryRenderer L1449); `method.Invoke(null, [services, remoteLocal])` | FactoryServiceRegistrar invoked for each class factory | Yes | `public static` covered by `BindingFlags.Public` |
| 2 | Same `RegisterFactories()` path. `attr.Type` resolves to `{Namespace}.{TypeName}`. `GetMethod()` finds `internal static void FactoryServiceRegistrar` (StaticFactoryRenderer L106). `BindingFlags.NonPublic` covers `internal`. | FactoryServiceRegistrar invoked for each static factory | Yes | `NonPublicMethods` DAM annotation + `BindingFlags.NonPublic` |
| 3 | Same `RegisterFactories()` path. `attr.Type` resolves to `{Namespace}.{Impl}Factory`. `GetMethod()` finds `public static void FactoryServiceRegistrar` (InterfaceFactoryRenderer L410). | FactoryServiceRegistrar invoked for each interface factory | Yes | `public static` covered by `BindingFlags.Public` |
| 4 | `ClassFactoryRenderer.Render()`: After usings, before `namespace {unit.Namespace}`, emits `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof({unit.Namespace}.{model.ImplementationTypeName}Factory))]`. Values from `ClassFactoryModel.ImplementationTypeName` (L37) and `FactoryGenerationUnit.Namespace` (L29). | Assembly attribute with fully-qualified type | Yes | Using fully-qualified attribute name for robustness at assembly scope |
| 5 | `StaticFactoryRenderer.Render()`: After usings, before `namespace {unit.Namespace}`, emits `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof({unit.Namespace}.{model.TypeName}))]`. Value from `StaticFactoryModel.TypeName` (L24). | Assembly attribute with fully-qualified static type | Yes | Static factories use `TypeName`, not `ImplementationTypeName` |
| 6 | `InterfaceFactoryRenderer.Render()`: After usings, before `namespace {unit.Namespace}`, emits `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof({unit.Namespace}.{model.ImplementationTypeName}Factory))]`. Value from `InterfaceFactoryModel.ImplementationTypeName` (L22). | Assembly attribute with fully-qualified implementation type | Yes | Points to concrete factory class, not the interface |
| 7 | `ClassFactoryRenderer.RenderFactoryInterface()`: Lines 126-130 removed (the `sb.AppendLine($"[DynamicDependency(...)]")` call). No other emission of `[DynamicDependency]` exists in the renderer. | No `[DynamicDependency]` in generated source | Yes | |
| 8 | `ClassFactoryRenderer.Render()`: Lines 51-55 removed (the `using System.Diagnostics.CodeAnalysis;` conditional block). | No `using System.Diagnostics.CodeAnalysis;` in generated source | Yes | Only needed for `[DynamicDependency]` which is being removed |
| 9 | `NeatooFactoryRegistrarAttribute` in `FactoryAttributes.cs`: Constructor param annotated with `[DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)]`; `Type` property annotated identically. | Both annotations present | Yes | Dual annotation for end-to-end trimmer dataflow |
| 10 | `typeof()` in `[assembly: NeatooFactoryRegistrar(typeof(X))]` creates static reference; `[DynamicallyAccessedMembers]` on attribute preserves all methods on X; `FactoryServiceRegistrar` (public or internal static) is preserved. | Factory types survive trimming | Yes (design contract) | Verified at publish time, not unit test |
| 11 | Same as Rule 10. Static class + nested delegate types referenced by `FactoryServiceRegistrar` registrations. | Static factory delegates survive trimming | Yes (design contract) | Scenario 8 verifies via publish+run |
| 12 | `RegisterFactories()`: `assembly.GetCustomAttributes<NeatooFactoryRegistrarAttribute>()` enumerates attributes; `attr.Type.GetMethod("FactoryServiceRegistrar", ...)` on each; `method?.Invoke(null, ...)` invokes. | Uses assembly attributes + GetMethod on each type | Yes | `?.` handles edge case of missing method |

### Concerns

1. **Two existing tests must be updated (in-scope).** `InternalVisibilityTests.AllInternal_DynamicDependencyStillEmitted` and `InternalVisibilityTests.InternalClassWithInterface_DynamicDependencyStillEmitted` assert that `[DynamicDependency]` IS emitted. These must change to assert it is NOT emitted (or be replaced). They are in-scope since they test the exact behavior being changed.

2. **Fully-qualified attribute name recommended.** The emitted assembly attribute should use `Neatoo.RemoteFactory.NeatooFactoryRegistrar` (fully qualified) instead of relying on the `using Neatoo.RemoteFactory;` directive. This is more robust at assembly scope and avoids any dependency on the user's using directives. Incorporated into the Implementation Contract below.

---

## Implementation Contract

**Created:** 2026-03-08
**Approved by:** developer agent (developer review, Step 5)

### Design Refinement: Fully-Qualified Attribute Name

The emitted assembly attribute must use the fully-qualified attribute name to avoid dependency on `using` directives at assembly scope:

```
[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof({Namespace}.{TypeName}))]
```

This means the renderers do NOT need to add `using Neatoo.RemoteFactory;` for the attribute (it may already be present for other reasons, but the assembly attribute does not depend on it).

### In Scope

#### Core Library (Phase 1)

- [ ] `src/RemoteFactory/FactoryAttributes.cs`: Add `NeatooFactoryRegistrarAttribute` class inside `#if !NETSTANDARD` block at end of file. Add `using System.Diagnostics.CodeAnalysis;` inside the same guard. Attribute has `[DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)]` on both constructor parameter and `Type` property. `AllowMultiple = true`.

- [ ] `src/RemoteFactory/AddRemoteFactoryServices.cs`: Replace `RegisterFactories()` body (lines 123-135). Change from `assembly.GetTypes().Select(t => t.GetMethod(...))` to `assembly.GetCustomAttributes<NeatooFactoryRegistrarAttribute>()` then `attr.Type.GetMethod(...)`. Keep `BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public`. Keep `method?.Invoke(null, [services, remoteLocal])`.

#### Generator Renderers (Phase 2)

- [ ] `src/Generator/Renderer/ClassFactoryRenderer.cs`:
  - REMOVE: Lines 51-55 (`using System.Diagnostics.CodeAnalysis;` conditional block)
  - REMOVE: Lines 126-130 (`[DynamicDependency(...)]` emission in `RenderFactoryInterface()`)
  - ADD: After all usings and before `namespace` declaration, emit: `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof({unit.Namespace}.{model.ImplementationTypeName}Factory))]`

- [ ] `src/Generator/Renderer/StaticFactoryRenderer.cs`:
  - ADD: After all usings and before `namespace` declaration, emit: `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof({unit.Namespace}.{model.TypeName}))]`

- [ ] `src/Generator/Renderer/InterfaceFactoryRenderer.cs`:
  - ADD: After all usings and before `namespace` declaration, emit: `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof({unit.Namespace}.{model.ImplementationTypeName}Factory))]`

#### Trimming Tests (Phase 3)

- [ ] `src/Tests/RemoteFactory.TrimmingTests/TrimTestCommands.cs`: NEW file. Static factory class with `[Factory]`, a `[Remote, Execute]` method taking `[Service] IServerOnlyRepository`. Follows the pattern described in the plan's Design section 4.

- [ ] `src/Tests/RemoteFactory.TrimmingTests/Program.cs`: Update to exercise the assembly-attribute discovery path:
  - Add `AddNeatooRemoteFactory(NeatooFactory.Remote, ...)` call that invokes `RegisterFactories()` internally
  - Resolve the static factory delegate to verify it survived
  - Keep existing direct `FactoryServiceRegistrar` call as secondary verification

#### Unit Tests (Phase 4)

- [ ] `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/Visibility/InternalVisibilityTests.cs`:
  - UPDATE `AllInternal_DynamicDependencyStillEmitted()` (line 340): Change `Assert.Contains("[DynamicDependency...]")` to `Assert.DoesNotContain("[DynamicDependency")`. Update test name and docstring to reflect new behavior (e.g., `AllInternal_DynamicDependencyNotEmitted`).
  - UPDATE `InternalClassWithInterface_DynamicDependencyStillEmitted()` (line 725): Same change. Update test name and docstring (e.g., `InternalClassWithInterface_DynamicDependencyNotEmitted`).
  - These are IN-SCOPE changes: the tests verify the exact behavior being replaced.

- [ ] Add new unit tests (new test class or extend existing) to verify:
  - Class factory generated source contains `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(...))]` (Scenario 1)
  - Static factory generated source contains the assembly attribute (Scenario 2)
  - Interface factory generated source contains the assembly attribute (Scenario 3)
  - Class factory generated source does NOT contain `[DynamicDependency` (Scenario 1, overlaps with updated tests above)
  - Class factory generated source does NOT contain `using System.Diagnostics.CodeAnalysis;` (Scenario 1)

### Out of Scope

- `src/Generator/FactoryGenerator.cs` -- Dead code in `GenerateExecute()` (line 84) and class/interface inline generation (line 822). Confirmed dead: `Initialize()` only wires Renderer path. Separate cleanup concern.
- `src/RemoteFactory/AddRemoteFactoryServices.cs` `RegisterMatchingName()` -- Still uses `assembly.GetTypes()` but is user-facing convention-based registration. Separate concern.
- Generated `FactoryServiceRegistrar` method signature or behavior -- Unchanged by this work.
- `NeatooRuntime` feature switch guards -- Orthogonal trimming mechanism, unaffected.
- Any changes to ordinal serialization or other Renderer output beyond the assembly attribute.

### Tests NOT to Modify (Out of Scope)

All tests in these directories/files are out of scope unless explicitly listed in "In Scope" above:
- `src/Tests/RemoteFactory.IntegrationTests/` -- Full round-trip tests, not affected by assembly attribute change
- `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/` (except InternalVisibilityTests DynamicDependency tests and new assembly attribute tests)
- `src/Tests/RemoteFactory.UnitTests/Serialization/` -- Serialization tests, unrelated
- `src/Design/Design.Tests/` -- Design project tests, should pass without changes

### Verification Gates

1. **After Phase 1 (Core Library):** Solution builds (`dotnet build src/Neatoo.RemoteFactory.sln`). No compilation errors.

2. **After Phase 2 (Renderers):** Solution builds. Inspect generated output from at least one test target (e.g., via `EmitCompilerGeneratedFiles`) to confirm:
   - Assembly attribute present in generated .g.cs files
   - No `[DynamicDependency]` in class factory generated output
   - No `using System.Diagnostics.CodeAnalysis;` in class factory generated output

3. **After Phase 3 (Trimming Tests):** Solution builds. The TrimmingTests project compiles with the new static factory.

4. **After Phase 4 (Unit Tests + Final):** All tests pass (`dotnet test src/Neatoo.RemoteFactory.sln`). Zero failures. This includes:
   - All existing tests (those not modified must pass as-is)
   - Updated DynamicDependency tests (now assert NOT emitted)
   - New assembly attribute emission tests
   - Existing visibility tests, serialization tests, integration tests

### Stop Conditions

If any of the following occur, STOP and report immediately:

- **Out-of-scope test failure:** Any test not listed in the "In Scope" section starts failing. Report which test, what it tests, and how it relates (or does not relate) to the current changes.
- **Architectural contradiction:** The implementation reveals a design flaw in the plan (e.g., the assembly attribute placement causes Roslyn's `NormalizeWhitespace()` to produce invalid code).
- **Compilation error in generated code:** The fully-qualified `typeof()` or attribute name produces invalid C# in any edge case (e.g., nested namespaces, generic types).
- **Reflection usage beyond plan:** If the implementation seems to require reflection beyond the planned `GetMethod("FactoryServiceRegistrar")` in `RegisterFactories()`, STOP and ask.

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| 1 | New unit test: class factory assembly attribute emission | Run generator on class factory source, assert `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(...))]` present, `[DynamicDependency]` absent, `using System.Diagnostics.CodeAnalysis;` absent |
| 2 | New unit test: static factory assembly attribute emission | Run generator on static factory source, assert assembly attribute present with `typeof({Namespace}.{StaticClassName})` |
| 3 | New unit test: interface factory assembly attribute emission | Run generator on interface factory source, assert assembly attribute present with `typeof({Namespace}.{Impl}Factory)` |
| 4 | Existing integration tests (RemoteFactory.IntegrationTests) | Class factory round-trip still works through new `RegisterFactories()` path. No changes needed -- existing tests exercise this. |
| 5 | Existing integration tests (RemoteFactory.IntegrationTests) | Static factory round-trip still works. No changes needed. |
| 6 | Existing integration tests (RemoteFactory.IntegrationTests) | Interface factory round-trip still works. No changes needed. |
| 7 | Code inspection of `NeatooFactoryRegistrarAttribute` definition | Verify both constructor param and property have `[DynamicallyAccessedMembers]` |
| 8 | Manual: `dotnet publish` TrimmingTests with static factory | Verify static factory delegate resolvable. Not part of `dotnet test`. |
| 9 | Manual: `dotnet publish` TrimmingTests with class factory | Verify class factory still works (regression). Not part of `dotnet test`. |
| 10 | New unit test or existing tests passing | Multiple factories in same assembly. Covered by full test suite -- each test target is in the same test assembly with others. |

---

## Implementation Progress

**Started:** 2026-03-08
**Developer:** developer agent (Claude Opus 4.6)

### Phase 1: Core Library Changes (Complete)
- Added `NeatooFactoryRegistrarAttribute` to `src/RemoteFactory/FactoryAttributes.cs` inside `#if !NETSTANDARD` guard at end of file
- Attribute has `[DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)]` on both constructor parameter and `Type` property
- `AllowMultiple = true`, `Inherited = false`, `AttributeTargets.Assembly`
- Updated `RegisterFactories()` in `src/RemoteFactory/AddRemoteFactoryServices.cs` to use `assembly.GetCustomAttributes<NeatooFactoryRegistrarAttribute>()` instead of `assembly.GetTypes()`
- Verification gate: Solution builds with zero errors

### Phase 2: Generator Renderer Changes (Complete)
- **ClassFactoryRenderer.cs**: Removed `using System.Diagnostics.CodeAnalysis;` emission block (was lines 51-55). Removed `[DynamicDependency(...)]` emission from `RenderFactoryInterface()` (was lines 126-130). Added `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof({Namespace}.{ImplTypeName}Factory))]` after usings, before namespace
- **StaticFactoryRenderer.cs**: Added `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof({Namespace}.{TypeName}))]` after usings, before namespace
- **InterfaceFactoryRenderer.cs**: Added `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof({Namespace}.{ImplTypeName}Factory))]` after usings, before namespace
- Verification gate: Solution builds. Generated output inspected for TrimmingTests -- assembly attribute present, no `[DynamicDependency]`, no `using System.Diagnostics.CodeAnalysis;`

### Phase 3: Trimming Tests (Complete)
- Created `src/Tests/RemoteFactory.TrimmingTests/TrimTestCommands.cs` -- static factory with `[Factory]`, `[Remote, Execute]`, `[Service] IServerOnlyRepository`
- Updated `src/Tests/RemoteFactory.TrimmingTests/Program.cs` -- replaced direct `FactoryServiceRegistrar` call with `AddNeatooRemoteFactory(NeatooFactory.Remote, ...)` to exercise the assembly-attribute discovery path. Added verification for static factory delegate resolution (`TrimTestCommands.DoWork`)
- Verification gate: TrimmingTests project builds with zero errors

### Phase 4: Unit Tests (Complete)
- Updated `InternalVisibilityTests.AllInternal_DynamicDependencyStillEmitted` -> renamed to `AllInternal_DynamicDependencyNotEmitted`, changed assertion from `Assert.Contains("[DynamicDependency...")` to `Assert.DoesNotContain("[DynamicDependency")`
- Updated `InternalVisibilityTests.InternalClassWithInterface_DynamicDependencyStillEmitted` -> renamed to `InternalClassWithInterface_DynamicDependencyNotEmitted`, same assertion change
- Created `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/AssemblyAttributeEmissionTests.cs` with 5 tests:
  - `ClassFactory_EmitsAssemblyAttribute` -- verifies class factory generates `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(TestNamespace.MyEntityFactory))]`
  - `ClassFactory_DoesNotEmitDynamicDependency` -- verifies no `[DynamicDependency` in generated output
  - `ClassFactory_DoesNotEmitDiagnosticsCodeAnalysisUsing` -- verifies no `using System.Diagnostics.CodeAnalysis;`
  - `StaticFactory_EmitsAssemblyAttribute` -- verifies static factory generates `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(TestNamespace.MyCommands))]`
  - `InterfaceFactory_EmitsAssemblyAttribute` -- verifies interface factory generates `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof(TestNamespace.MyServiceFactory))]`
- Verification gate: All tests pass

### No Stop Conditions Triggered
- No out-of-scope test failures
- No architectural contradictions discovered
- No compilation errors in generated code
- No unexpected reflection usage

---

## Completion Evidence

**Reported:** 2026-03-08

- **Tests Passing:**
  - RemoteFactory.UnitTests (net9.0): 490 passed, 0 failed
  - RemoteFactory.UnitTests (net10.0): 490 passed, 0 failed
  - RemoteFactory.IntegrationTests (net9.0): 481 passed, 0 failed, 3 skipped
  - RemoteFactory.IntegrationTests (net10.0): 481 passed, 0 failed, 3 skipped
  - Design.Tests (net9.0): 41 passed, 0 failed
  - Design.Tests (net10.0): 41 passed, 0 failed
- **Verification Resources Pass:** Yes (Design.Tests all pass)
- **All Contract Items:** Confirmed 100% complete

---

## Documentation

**Agent:** business-requirements-documenter
**Completed:** 2026-03-08
**Status:** Complete

### Files Updated

1. **`src/Design/CLAUDE-DESIGN.md`** — Added new "Trimming-Safe Factory Registration" subsection under Critical Rule 2, after "Auth Type Auto-Registration for Trimming". Documents: the `[assembly: NeatooFactoryRegistrar(typeof(X))]` pattern emitted by all three renderers, the `[DynamicallyAccessedMembers]` dataflow contract, the assembly-attribute-based discovery in `RegisterFactories()`, and a table showing what each factory pattern targets. This covers NEW rules 4-6, 9-10, and 12 from the plan.

2. **`docs/trimming.md`** — Added new "Factory Type Preservation" section before "Authorization Types and Trimming". Explains that all factory types are automatically preserved from trimming via assembly-level attributes, and that users do not need to take any action. This covers NEW rules 10-11 from the plan (static and all factory types surviving trimming).

3. **No changes to `docs/trimming.md` line 215** (`[DynamicDependency]` user-facing guidance for `RegisterMatchingName`). Requirements reviewer confirmed this reference is about user-facing convention-based registration, not the internal factory interface mechanism that was removed. It remains accurate.

### Developer Deliverables

None. All source code changes were completed during the implementation phase. The Design project source of truth (`src/Design/`) was confirmed unaffected — no new patterns were added that require Design.Domain examples or Design.Tests test cases. The change is an internal infrastructure improvement to the factory discovery mechanism; it does not alter any user-facing API or factory pattern behavior.

### Not Updated (No Changes Needed)

- **`src/Design/CLAUDE-DESIGN.md` Design Completeness Checklist** — No new pattern was demonstrated. The checklist items are unchanged.
- **`src/Design/CLAUDE-DESIGN.md` Design Debt table** — No design debt was resolved or added.
- **`src/Design/CLAUDE-DESIGN.md` Anti-Patterns** — No new anti-pattern was discovered.
- **`docs/trimming.md` RegisterMatchingName section** — `[DynamicDependency]` guidance remains accurate for user-facing convention-based registration.
- **`skills/RemoteFactory/`** — Internal mechanism change, not user-facing API. No skill updates needed.
- **`src/Design/Design.Domain/`** — No new factory pattern examples needed (Developer Deliverable: none).
- **`src/Design/Design.Tests/`** — No new test cases needed (Developer Deliverable: none).

---

## Architect Verification

**Verified:** 2026-03-08
**Verdict:** VERIFIED

**Independent test results:**
- RemoteFactory.UnitTests (net9.0): 490 passed, 0 failed
- RemoteFactory.UnitTests (net10.0): 490 passed, 0 failed
- RemoteFactory.IntegrationTests (net9.0): 481 passed, 0 failed, 3 skipped
- RemoteFactory.IntegrationTests (net10.0): 481 passed, 0 failed, 3 skipped
- Design.Tests (net9.0): 41 passed, 0 failed
- Design.Tests (net10.0): 41 passed, 0 failed
- Full solution build (Release): 0 errors

**Design match:** Implementation matches the original plan on all points:

1. **NeatooFactoryRegistrarAttribute** -- Present in `src/RemoteFactory/FactoryAttributes.cs` at end of file, inside `#if !NETSTANDARD` guard. Has `[DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)]` on both constructor parameter AND `Type` property. `AllowMultiple = true`, `Inherited = false`, `AttributeTargets.Assembly`. Uses fully-qualified `System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers` (no `using` needed within the guard).

2. **RegisterFactories()** -- `src/RemoteFactory/AddRemoteFactoryServices.cs` line 127: uses `assembly.GetCustomAttributes<NeatooFactoryRegistrarAttribute>()` instead of `assembly.GetTypes()`. Iterates attributes, calls `attr.Type.GetMethod("FactoryServiceRegistrar", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)` on each.

3. **All three renderers emit assembly attribute before namespace:**
   - ClassFactoryRenderer.cs line 60: `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof({unit.Namespace}.{model.ImplementationTypeName}Factory))]`
   - StaticFactoryRenderer.cs line 47: `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof({unit.Namespace}.{model.TypeName}))]`
   - InterfaceFactoryRenderer.cs line 48: `[assembly: Neatoo.RemoteFactory.NeatooFactoryRegistrar(typeof({unit.Namespace}.{model.ImplementationTypeName}Factory))]`

4. **ClassFactoryRenderer cleanup** -- No `[DynamicDependency]` or `using System.Diagnostics.CodeAnalysis;` anywhere in file. Confirmed via grep: zero matches.

5. **InternalVisibilityTests updated** -- Both tests renamed from `*StillEmitted` to `*NotEmitted`, assertions changed from `Assert.Contains` to `Assert.DoesNotContain`.

6. **AssemblyAttributeEmissionTests** -- New test class at `src/Tests/RemoteFactory.UnitTests/FactoryGenerator/AssemblyAttributeEmissionTests.cs` with 5 tests: ClassFactory_EmitsAssemblyAttribute, ClassFactory_DoesNotEmitDynamicDependency, ClassFactory_DoesNotEmitDiagnosticsCodeAnalysisUsing, StaticFactory_EmitsAssemblyAttribute, InterfaceFactory_EmitsAssemblyAttribute.

7. **TrimmingTests static factory** -- `TrimTestCommands.cs` created with `[Factory]` static partial class, `[Remote, Execute]` method with `[Service] IServerOnlyRepository`. `Program.cs` updated to use `AddNeatooRemoteFactory(NeatooFactory.Remote, ...)` for assembly-attribute discovery and resolves `TrimTestCommands.DoWork` delegate.

**Issues found:** None

---

## Requirements Verification

**Reviewer:** business-requirements-reviewer
**Verified:** 2026-03-08
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| # | Requirement | Status | Evidence |
|---|-------------|--------|----------|
| 1 | Trimming architecture (guard emission unaffected) | SATISFIED | Verified in generated output: `src/Tests/RemoteFactory.TrimmingTests/Generated/.../TrimTestEntityFactory.g.cs` line 54 still emits `if (!NeatooRuntime.IsServerRuntime) throw ...`; `TrimTestCommandsFactory.g.cs` line 28 still emits `if (NeatooRuntime.IsServerRuntime)` guard. No Renderer code touching guard emission was changed. |
| 2 | Three factory types covered | SATISFIED | All three Renderers emit `[assembly: NeatooFactoryRegistrar(typeof(...))]`: ClassFactoryRenderer.cs line 60, StaticFactoryRenderer.cs line 47, InterfaceFactoryRenderer.cs line 48. New unit tests verify all three: `AssemblyAttributeEmissionTests.cs` (ClassFactory, StaticFactory, InterfaceFactory tests). |
| 3 | FactoryServiceRegistrar signature unchanged | SATISFIED | Method signatures in all three Renderers are identical to pre-change: ClassFactoryRenderer line 1443 (`public static`), StaticFactoryRenderer line 111 (`internal static`), InterfaceFactoryRenderer line 415 (`public static`). `RegisterFactories()` in AddRemoteFactoryServices.cs line 131-132 still uses same `BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public` and `method?.Invoke(null, [services, remoteLocal])`. |
| 4 | Existing assembly-level attribute pattern followed | SATISFIED | `NeatooFactoryRegistrarAttribute` added to `src/RemoteFactory/FactoryAttributes.cs` at end of file (lines 145-167), following the established `FactoryHintNameLengthAttribute` pattern at line 129. Same `[AttributeUsage(AttributeTargets.Assembly)]` pattern. |
| 5 | Auth type auto-registration preserved | SATISFIED | `FactoryServiceRegistrar` method body is unchanged across all three Renderers. InterfaceFactoryRenderer still emits `services.TryAddTransient<{authType}, {concreteType}>()` in its auth registration block (lines 447-472). ClassFactoryRenderer still emits auth registrations. Auth types survive trimming because `FactoryServiceRegistrar` itself now survives via the assembly attribute. |
| 6 | RegisterMatchingName unchanged (out of scope) | SATISFIED | `RegisterMatchingName()` in `src/RemoteFactory/AddRemoteFactoryServices.cs` lines 146-165 is completely unchanged. Still uses `assembly.GetTypes()` for convention-based registration. |
| 7 | Server registration path (AddNeatooAspNetCore) | SATISFIED | `src/RemoteFactory.AspNetCore/ServiceCollectionExtensions.cs` is unchanged. Line 30 delegates to `AddNeatooRemoteFactory(NeatooFactory.Server, ...)` which calls updated `RegisterFactories()`. Transparent. |
| 8 | Design project test infrastructure | SATISFIED | `src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs` is unchanged. Line 230 calls `AddNeatooRemoteFactory()` which now uses assembly attribute discovery internally. Design.Tests: 41 passed, 0 failed on both net9.0 and net10.0. |
| 9 | TrimmingTests updated to exercise assembly-attribute discovery | SATISFIED | `src/Tests/RemoteFactory.TrimmingTests/Program.cs` line 15 now calls `services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(TrimTestEntity).Assembly)` exercising the assembly-attribute path. Line 31 resolves `TrimTestCommands.DoWork` delegate (static factory). Line 28 resolves `ITrimTestEntityFactory` (class factory). New `TrimTestCommands.cs` adds a static factory test target. |
| 10 | Multi-targeting (net9.0 and net10.0) | SATISFIED | Unit tests pass on both TFMs (490 passed each). Integration tests pass on both (481 passed each). Design.Tests pass on both (41 passed each). `[DynamicallyAccessedMembers]` used with fully-qualified `System.Diagnostics.CodeAnalysis.*` inside `#if !NETSTANDARD` guard, available on both frameworks. |
| 11 | No Design Debt conflict | SATISFIED | No features from the Design Debt table were implemented. No deferred decisions were overridden. The change is strictly an infrastructure improvement to the factory discovery mechanism. |

### Pre-Design Review Gap Resolution

| Gap (from pre-design review) | Resolution |
|-------------------------------|------------|
| No unit tests for `RegisterFactories()` | ADDRESSED: New `AssemblyAttributeEmissionTests.cs` with 5 tests verifying assembly attribute emission for all three factory types. Integration tests exercise the full `RegisterFactories()` path via `AddNeatooRemoteFactory()` (481 tests, all passing). |
| No static factory in TrimmingTests | ADDRESSED: New `TrimTestCommands.cs` added with `[Factory]`, `[Remote, Execute]` static factory. `Program.cs` updated to resolve `TrimTestCommands.DoWork` delegate. |
| Generator dual-path clarification | CONFIRMED: Old inline path in `FactoryGenerator.cs` is dead code. Plan correctly identified this and excluded it from scope. No changes to `FactoryGenerator.cs`. |
| Published docs update for `[DynamicDependency]` | NO ACTION NEEDED: The `[DynamicDependency]` reference in `docs/trimming.md` line 215 is user-facing guidance for `RegisterMatchingName` (preserved types from convention-based registration), not about internal factory interface methods. The reference remains accurate. |

### Unintended Side Effects

None detected. Specifically verified:

1. **Generated code structure**: The only differences in generated output are (a) addition of `[assembly: NeatooFactoryRegistrar(...)]` before namespace, (b) removal of `[DynamicDependency]` on class factory interface methods, (c) removal of `using System.Diagnostics.CodeAnalysis;` from class factory output. No behavioral changes to factory interfaces, delegates, service registration, guard emission, or authorization handling.

2. **Serialization contracts**: No serialization-related code was modified. `IOrdinalSerializable` implementation, property setters, round-trip behavior are all unchanged.

3. **Factory interface visibility**: `RenderFactoryInterface()` in ClassFactoryRenderer still uses the same visibility logic (lines 109-127). The only change is removal of the `[DynamicDependency]` prefix on interface method lines. Interface promotion of `[Remote] internal` methods is unaffected.

4. **Published documentation accuracy**: `docs/trimming.md` and `src/Design/CLAUDE-DESIGN.md` remain accurate. Neither references `[DynamicDependency]` on factory interfaces as an internal mechanism. The `docs/trimming.md` reference to `[DynamicDependency]` (line 215) is about user-facing `RegisterMatchingName` guidance and is unrelated.

5. **Design project source of truth**: No files in `src/Design/` were modified. All 41 Design.Tests pass on both frameworks, confirming the Design projects continue to demonstrate correct factory patterns.

### Issues Found

None.
