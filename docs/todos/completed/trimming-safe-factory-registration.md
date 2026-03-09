# Trimming-Safe Factory Registration

**Status:** Complete
**Priority:** High
**Created:** 2026-03-08
**Last Updated:** 2026-03-08


---

## Problem

Factory classes are being trimmed during IL trimming in Blazor WASM apps. The v0.21.1 fix (emitting `[DynamicDependency]` on every class factory interface method) solved class factories, but static factories are still trimmed because they don't have factory interfaces — they use nested delegate types, and `[DynamicDependency]` is not valid on delegate declarations (CS0592).

The root cause affects all factory types: `RegisterFactories()` in `AddRemoteFactoryServices.cs` discovers `FactoryServiceRegistrar` methods via `assembly.GetTypes()` + `GetMethod()` reflection, which is inherently trim-unsafe.

## Solution

Replace the reflection-based factory discovery with a trimming-safe pattern:

1. Create a `NeatooFactoryRegistrarAttribute` (assembly-level, with `[DynamicallyAccessedMembers]`) in the library
2. Generate `[assembly: NeatooFactoryRegistrar(typeof(X))]` per factory — the `typeof()` is a static reference the trimmer follows
3. Update `RegisterFactories()` to enumerate assembly attributes instead of `assembly.GetTypes()`
4. Apply to all factory types (class, static, interface) — replacing the `[DynamicDependency]` on interface methods from v0.21.1

Key files:
- `src/RemoteFactory/AddRemoteFactoryServices.cs` — reflection-based `RegisterFactories()` (line 123-132)
- `src/Generator/Renderer/ClassFactoryRenderer.cs` — current `[DynamicDependency]` on interface methods
- `src/Generator/Renderer/StaticFactoryRenderer.cs` — no trimming protection currently
- `src/Generator/Renderer/InterfaceFactoryRenderer.cs` — needs checking for same issue

---

## Clarifications

### Q1: Old generator path in FactoryGenerator.cs (Architect)
**Q:** Is `FactoryGenerator.cs` still active alongside the Renderer classes, or have the Renderers fully replaced it?
**A:** Both paths are active. `FactoryGenerator.cs` has inline `FactoryServiceRegistrar` generation at lines 264 (static factories) and 822 (class/interface factories) using `context.AddSource()`. The Renderer classes also generate via `spc.AddSource()`. **User decision: Fully replace the old inline generation path. The assembly attribute should only be emitted from the Renderer classes, and the old FactoryGenerator.cs inline path should be removed or migrated.**

### Q2: Attribute design — Option A vs B (Architect)
**Q:** Should the attribute just hold the type reference (Option A, simpler, `RegisterFactories()` still uses `GetMethod()`), or fully eliminate reflection (Option B)?
**A:** Option A is sufficient. The dotnet-runtime-debugger agent confirmed that `[DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)]` on both the constructor parameter AND the property creates a dataflow contract the trimmer follows. The downstream `GetMethod()` call becomes trim-safe with no warnings. The annotation must appear on both sides (constructor param and property) for end-to-end trimmer tracking.

### Q3: `using System.Diagnostics.CodeAnalysis;` cleanup (Architect)
**Q:** Should we remove the `using System.Diagnostics.CodeAnalysis;` from ClassFactoryRenderer when removing `[DynamicDependency]`?
**A:** Yes, do the cleanup. Remove it since it was only added for `[DynamicDependency]`.

### Q4: InterfaceFactoryRenderer vulnerability (Architect)
**Q:** Does InterfaceFactoryRenderer have the same trimming problem as static factories?
**A:** Yes. The dotnet-runtime-debugger agent confirmed interface factories ARE vulnerable. The trimmer preserves the type and its interface-contract methods via DI, but `FactoryServiceRegistrar` is `internal static`, satisfies no interface, and has no static callers — so it gets trimmed. All three factory types need the fix.

**Architect confirmed: Ready to proceed.**

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-08
**Verdict:** APPROVED

### Relevant Requirements Found

1. **Trimming architecture (feature switch guards).** `docs/trimming.md` and `src/Design/CLAUDE-DESIGN.md` (Critical Rule 2, "Factory Method Visibility Controls Guard Emission and Trimming") document that the generator emits `if (NeatooRuntime.IsServerRuntime)` guards and that these are the mechanism for IL trimming. The proposed change does not alter guard emission — it only changes how `RegisterFactories()` discovers the `FactoryServiceRegistrar` methods. The guard behavior is orthogonal and unaffected.

2. **Three factory types must all be covered.** `src/Design/CLAUDE-DESIGN.md` (Decision Table, Quick Reference) documents Class, Interface, and Static factory patterns. The todo correctly identifies that all three need the fix. Confirmed in the Renderer code:
   - `src/Generator/Renderer/ClassFactoryRenderer.cs` — currently emits `[DynamicDependency]` on interface methods (lines 126-130)
   - `src/Generator/Renderer/StaticFactoryRenderer.cs` — has no trimming protection (confirmed: no `DynamicDependency` anywhere in file)
   - `src/Generator/Renderer/InterfaceFactoryRenderer.cs` — has no trimming protection (confirmed: no `DynamicDependency` anywhere in file)

3. **`FactoryServiceRegistrar` is generated with consistent signatures.** All three renderers generate an `internal static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)` method. The `RegisterFactories()` method in `src/RemoteFactory/AddRemoteFactoryServices.cs` (line 127) discovers these via `assembly.GetTypes()` + `GetMethod("FactoryServiceRegistrar", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)`. The proposed change replaces only the discovery mechanism, not the generated method signature or behavior.

4. **Existing assembly-level attribute pattern.** `src/RemoteFactory/FactoryAttributes.cs` (line 129) already has `FactoryHintNameLengthAttribute` with `[AttributeUsage(AttributeTargets.Assembly)]`. The proposed `NeatooFactoryRegistrarAttribute` follows this established pattern. No contradiction.

5. **Auth type auto-registration for trimming.** `src/Design/CLAUDE-DESIGN.md` ("Auth Type Auto-Registration for Trimming") documents that the generator emits explicit `services.TryAddTransient<IFooAuth, FooAuth>()` in `FactoryServiceRegistrar`. This pattern is inside the `FactoryServiceRegistrar` method body, so it is preserved as long as the `FactoryServiceRegistrar` method itself survives trimming — which is exactly what the proposed fix ensures.

6. **`RegisterMatchingName` uses `assembly.GetTypes()` too.** `src/RemoteFactory/AddRemoteFactoryServices.cs` (lines 150-154) has a separate `RegisterMatchingName` method that also uses `assembly.GetTypes()`. The todo's scope correctly limits the fix to `RegisterFactories()` only. `RegisterMatchingName` is user-called for convention registration and is a separate concern documented in `docs/trimming.md` (lines 208-215).

7. **Server setup via `AddNeatooAspNetCore`.** `src/RemoteFactory.AspNetCore/ServiceCollectionExtensions.cs` (line 30) delegates to `services.AddNeatooRemoteFactory(NeatooFactory.Server, ...)`, which calls `RegisterFactories()`. The proposed change to `RegisterFactories()` will automatically apply to both client (`AddNeatooRemoteFactory`) and server (`AddNeatooAspNetCore`) registration paths. No additional changes needed.

8. **Design project test infrastructure.** `src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs` (line 230) calls `services.AddNeatooRemoteFactory(mode, serializationOptions, typeof(ExampleClassFactory).Assembly)`, which internally calls `RegisterFactories()`. The proposed change will be transparent to this call chain.

9. **TrimmingTests project.** `src/Tests/RemoteFactory.TrimmingTests/Program.cs` calls `TrimTestEntityFactory.FactoryServiceRegistrar(services, NeatooFactory.Remote)` directly (line 14), bypassing `RegisterFactories()`. This direct call pattern will continue to work since the `FactoryServiceRegistrar` method signature is unchanged. However, this test should be updated or extended to verify that the assembly-attribute-based discovery also works.

10. **Multi-targeting requirement.** `src/Directory.Build.props` targets net9.0 and net10.0. The proposed `NeatooFactoryRegistrarAttribute` and `[DynamicallyAccessedMembers]` are available in both target frameworks. `[FeatureSwitchDefinition]` (used by the existing trimming infrastructure) was introduced in .NET 9, confirming both TFMs are compatible.

11. **No Design Debt conflict.** The Design Debt table in `src/Design/CLAUDE-DESIGN.md` lists five deferred topics (private setter support, OR logic for AspAuthorize, automatic Remote detection, collection factory injection, IEnumerable serialization). None relate to factory registration or trimming discovery. No conflict.

### Gaps

1. **No existing unit tests for `RegisterFactories()`.** There are no tests in `src/Tests/RemoteFactory.UnitTests/` that verify the `RegisterFactories()` method's behavior. The architect should include tests that verify assembly-attribute-based discovery works for all three factory types (class, static, interface).

2. **No integration test for static factory trimming.** The `src/Tests/RemoteFactory.TrimmingTests/` project only tests a class factory (`TrimTestEntity`). The architect should consider adding a static factory to this project to verify the fix addresses the original bug (static factories being trimmed).

3. **Generator pipeline dual-path clarification.** The todo's Q1 states "Both paths are active" (FactoryGenerator.cs inline and Renderer classes). However, inspection of `FactoryGenerator.cs` shows that `GenerateExecute` (line 84) and the class/interface generation (line 822) are defined but **not called from `Initialize()`**. The `Initialize()` method (lines 19-81) only wires up the Renderer path via `FactoryModelBuilder.Build` + `FactoryRenderer.Render`. The old inline methods appear to be dead code. The architect should verify this before attempting to "remove or migrate" the old path — it may already be dead.

4. **Published docs update needed.** `docs/trimming.md` (line 215) recommends `[DynamicDependency]` as a user-facing option for preserving types. If the library moves to assembly-level attributes internally, the docs should still accurately describe the available user-facing options. The internal mechanism change should not require docs updates, but if the `[DynamicDependency]` on factory interface methods is removed, verify this doesn't break any documented guidance.

### Contradictions

None found. The proposed solution is consistent with all documented patterns, rules, and design decisions.

### Recommendations for Architect

1. **Verify the old FactoryGenerator.cs inline path is dead code.** The `GenerateExecute` method (line 84) and class/interface generation (line 822) in `FactoryGenerator.cs` are not called from `Initialize()`. If they are indeed dead, the Q1 clarification to "remove the old inline path" is simply dead code cleanup, not a migration. Confirm before spending effort on migration.

2. **Place `NeatooFactoryRegistrarAttribute` in `src/RemoteFactory/FactoryAttributes.cs`.** This file already contains all factory-related attributes including the existing assembly-level `FactoryHintNameLengthAttribute`. Follow the established pattern.

3. **Ensure `[DynamicallyAccessedMembers]` is on both constructor parameter AND property.** The Q2 clarification specifies this dual annotation requirement. This is critical for end-to-end trimmer tracking.

4. **Extend TrimmingTests with a static factory.** The original bug specifically affects static factories. The trimming test project should include a static factory to prevent regression.

5. **The assembly attribute emission should be in the Renderer classes, not in `FactoryModelBuilder`.** Keep the Renderers as the sole code-generation path, consistent with the current architecture where `FactoryModelBuilder` builds models and Renderers produce output.

6. **Consider the `TrimTestEntity` direct call pattern.** `src/Tests/RemoteFactory.TrimmingTests/Program.cs` line 14 calls `FactoryServiceRegistrar` directly. This bypasses `RegisterFactories()` entirely. If the goal is to test the full registration pipeline, the test should be updated to call `AddNeatooRemoteFactory()` instead.

---

## Plans

- [Trimming-Safe Factory Registration Plan](../plans/completed/trimming-safe-factory-registration.md)

---

## Tasks

- [x] Architect comprehension check (Step 2)
- [x] Business requirements review (Step 3)
- [x] Architect plan creation & design (Step 4)
- [x] Developer review (Step 5)
- [x] Implementation (Step 7)
- [x] Verification (Step 8)
- [x] Documentation (Step 9)
- [x] Completion (Step 10)

---

## Progress Log

### 2026-03-08
- Created todo from bug report: static factories (e.g., TherapyCommands) trimmed in Blazor WASM apps
- v0.21.1 fix addressed class factories but not static factories
- dotnet-runtime-debugger agent confirmed `[DynamicDependency]` is not valid on delegates (CS0592)
- Agent recommended assembly-level attribute with `[DynamicallyAccessedMembers]` as the proper fix

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: Pass (Release mode, 0 errors)
- Tests: Pass (2,024 tests: 490×2 unit + 481×2 integration + 41×2 design, 0 failures)

---

## Results / Conclusions

Replaced the trim-unsafe `assembly.GetTypes()` factory discovery with a `NeatooFactoryRegistrarAttribute` assembly-level attribute pattern. All three factory types (class, static, interface) now emit `[assembly: NeatooFactoryRegistrar(typeof(X))]`, and `RegisterFactories()` enumerates these attributes instead of scanning types. The `[DynamicDependency]` workaround from v0.21.1 was removed. This fixes static factories being trimmed in Blazor WASM apps and makes the entire factory registration pipeline trim-safe.
