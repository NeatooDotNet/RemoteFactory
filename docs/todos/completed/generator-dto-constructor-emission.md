# Generator-Emitted DTO Constructor Lambdas for IL Trimming

**Status:** Complete
**Priority:** High
**Created:** 2026-03-25
**Last Updated:** 2026-03-30

---

## Problem

`NeatooJsonSerializer.Deserialize<T>()` fails for plain DTO classes under Blazor WASM IL trimming. The trimmer strips constructor metadata that STJ needs for reflection-based discovery.

GitHub issue: https://github.com/NeatooDotNet/RemoteFactory/issues/48

The v0.23.2 fix (Activator.CreateInstance fallback in NeatooJsonTypeInfoResolver) did NOT resolve this — `Activator.CreateInstance` also fails when the trimmer strips the constructor. The only way to preserve constructors is a static reference the trimmer can trace: `() => new Dto()`.

### What we've confirmed doesn't work

1. **`[DynamicallyAccessedMembers]`** — .NET runtime team called it "false sense of hope" (dotnet/runtime#52268)
2. **`Activator.CreateInstance` fallback** — also fails under trimming; the constructor metadata it needs is the same metadata the trimmer strips

### What should work

Generator-emitted `() => new Dto()` lambdas. The source generator already knows all DTO return types at compile time. Static constructor references survive the trimmer.

### Prior work

- [Completed todo (v0.23.2 attempt)](completed/fix-dto-trimming-deserialization.md)
- [Completed plan (Activator approach)](../plans/completed/fix-dto-trimming-design.md)
- Original architect plan (before rewrite) had the right direction: `DtoConstructorRegistry` with generator-emitted registrations
- Developer review caught that DTO discovery must happen in `FactoryModelBuilder` (where `ITypeSymbol` is available), not in renderers (where return types are strings)

### Key files

- `src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs` — needs to consume registered constructors
- `src/Generator/` — needs to discover DTO return types and emit registration code

## Solution

Have the source generator emit explicit `() => new Dto()` lambdas for non-Neatoo DTO return types discovered in factory method signatures. Register these in a static `DtoConstructorRegistry` that `NeatooJsonTypeInfoResolver` checks before DI fallback. DTO discovery must happen in `FactoryModelBuilder` (at the Roslyn `ITypeSymbol` level), not in renderers.

---

## Clarifications

**Architect confirmed Ready (2026-03-25)**

No clarifying questions. One refinement: the developer's prior review said DTO discovery must happen in `FactoryModelBuilder`, but the architect identified that `ITypeSymbol` analysis actually needs to happen one step earlier — in the transform phase that produces `TypeFactoryMethodInfo` (in `FactoryGenerator.Types.cs`). By the time `FactoryModelBuilder` runs, `ReturnType` is already a string.

---

## Requirements Review

**Reviewer:** business-requirements-reviewer
**Reviewed:** 2026-03-25
**Verdict:** APPROVED

### Relevant Requirements Found

1. **Trimming-Safe Factory Registration Pattern** (`src/Design/CLAUDE-DESIGN.md`, "Trimming-Safe Factory Registration" section; `docs/trimming.md`, "Factory Type Preservation" section). The generator already emits `[assembly: NeatooFactoryRegistrar(typeof(X))]` assembly attributes with `[DynamicallyAccessedMembers]` to create static references the IL trimmer can trace. The proposed `DtoConstructorRegistry` with generator-emitted `() => new Dto()` lambdas follows the exact same trimming-survival strategy: static references at compile time that the trimmer preserves. This is a proven pattern in the codebase.

2. **Auth Type Auto-Registration Precedent** (`src/Design/CLAUDE-DESIGN.md`, "Auth Type Auto-Registration for Trimming" section). The generator already emits explicit `services.TryAddTransient<IFooAuth, FooAuth>()` registrations in `FactoryServiceRegistrar` to create static references the trimmer preserves. The DTO constructor registration follows the same principle: generator discovers types at compile time and emits static references that survive trimming.

3. **NeatooJsonTypeInfoResolver CreateObject Pattern** (`src/RemoteFactory/Internal/NeatooJsonTypeInfoResolver.cs:29-48`). The resolver already sets `CreateObject` for (a) DI-registered types via `ServiceProvider.GetRequiredService(type)` and (b) plain DTOs via `Activator.CreateInstance(type)`. The todo proposes replacing the `Activator.CreateInstance` fallback (line 47) with a registry lookup. This is a direct replacement of the existing code path, not a new architectural pattern.

4. **RecordBypassConverterFactory Exclusion** (`src/RemoteFactory/Internal/RecordBypassConverterFactory.cs:36-57`). Types with parameterized constructors and no public parameterless constructor are claimed by `RecordBypassConverterFactory` and serialized without `$id`/`$ref` metadata. The DTO constructor registry must only register types with public parameterless constructors (plain DTOs like `ExampleDto`), not records. The existing `RecordBypassConverterFactory.CanConvert()` detection rule (`!hasParameterlessCtor && hasParameterizedCtor`) defines the boundary. Anti-Pattern 9 (`src/Design/CLAUDE-DESIGN.md`) reinforces this: records are handled separately from mutable types.

5. **Interface Factory Returns Non-Neatoo Types** (`src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:204-230`; `src/Design/CLAUDE-DESIGN.md`, Quick Decisions Table: "Can Interface Factory return a record? Yes"). Interface factories can return plain DTOs (`ExampleDto`) and records (`ExampleRecordResult`). The DTO discovery scope includes return types from all three factory patterns (interface methods, [Execute] return types, [Fetch]/[Create] return types that aren't the factory's own type).

6. **Generator Pipeline Architecture** (`src/Generator/FactoryGenerator.Types.cs:676-688`; `src/Generator/FactoryGenerator.Transform.cs:75-261`). The `MethodInfo` base class stores `ReturnType` as a string (line 676: `this.ReturnType = methodSymbol.ReturnType.ToString()`). The `ITypeSymbol` is available during `TypeFactoryMethods()` in `FactoryGenerator.Transform.cs` (line 81: `methodSymbol.ReturnType`), but by the time the `FactoryModelBuilder` runs, only the string representation survives. The architect's clarification correctly identifies this: DTO type analysis (determining whether a return type is a plain DTO with a public parameterless constructor vs. a record or Neatoo type) must happen in the transform phase where `ITypeSymbol` is accessible.

7. **FactoryGenerationUnit as Pipeline Output** (`src/Generator/Model/FactoryGenerationUnit.cs:9-27`). This is a sealed record that flows through the incremental generator pipeline. Any new data (list of discovered DTO types) must be added here with proper value equality semantics (the existing `EquatableArray<T>` pattern from `TypeInfo`).

8. **No Reflection Policy** (`CLAUDE.md` global instructions). The project has a "No Reflection Without Approval" policy. The proposed solution eliminates the `Activator.CreateInstance` reflection call in `NeatooJsonTypeInfoResolver.cs:47` and replaces it with a compile-time-generated static lambda. This is a net improvement in reflection posture.

9. **Design Project as Source of Truth** (`CLAUDE.md`). The Design project already has the exact DTO that triggers this bug (`Design.Domain.FactoryPatterns.ExampleDto`). The reproduction was confirmed using the Design project's Blazor WASM client (Progress Log, 2026-03-25). After the fix, the Design project tests (`InterfaceFactoryTests.InterfaceFactory_GetAllAsync_ReturnsDataFromServer` etc.) must continue to pass, and the published Blazor WASM scenario must work without `DeserializeNoConstructor` errors.

10. **Private Setter Prohibition Does Not Apply to DTOs** (`src/Design/CLAUDE-DESIGN.md`, Anti-Pattern 4; Design Debt table: "Private setter support"). The private setter rule applies to `[Factory]`-annotated entity properties that go through ordinal serialization. Plain DTOs like `ExampleDto` use standard STJ deserialization (via `CreateObject` + property setters), not the ordinal serializer. The proposed change does not alter this boundary.

### Gaps

1. **DTO Discovery Scope Definition**: No existing documented requirement defines exactly which return types qualify as "DTOs that need constructor registration." The architect must establish criteria:
   - Must have a public parameterless constructor (exclude records claimed by `RecordBypassConverterFactory`)
   - Must NOT be a Neatoo `[Factory]`-annotated type (those are already DI-registered)
   - Must NOT be a primitive, string, collection, or framework type
   - Must NOT be already registered in the DI container (the existing `IServiceProviderIsService` check handles those at runtime, but the generator cannot know DI registrations at compile time)
   - What about generic types like `IReadOnlyList<ExampleDto>`? The DTO inside the generic needs registration, but the collection does not.

2. **Static Factory [Execute] Return Types**: The Design project demonstrates `ExampleCommands._SendNotification` returning `Task<bool>`. If a static factory returned `Task<SomeDto>`, that DTO would also need registration. The architect should confirm that all three factory patterns (class, interface, static) are scanned for DTO return types.

3. **Registry Initialization Timing**: The `DtoConstructorRegistry` must be populated before the first deserialization attempt. The existing `FactoryServiceRegistrar` pattern is called during `AddNeatooRemoteFactory()` / `AddNeatooAspNetCore()`. The architect must define whether DTO constructor registration happens in the same `FactoryServiceRegistrar` method or through a separate mechanism (e.g., a static initializer or a separate assembly attribute).

4. **Cross-Assembly DTO Types**: If a DTO type is defined in a different assembly than the factory that returns it, the generator for the factory's assembly can still see it (via `ITypeSymbol` from the compilation), but can only emit `new Dto()` if that type is accessible. The architect should address whether cross-assembly DTOs are in scope.

### Contradictions

None found. The proposed approach is fully aligned with existing patterns:
- It extends the proven trimming-safe registration pattern (assembly attributes, static references)
- It removes an `Activator.CreateInstance` call (improving the reflection posture)
- It does not conflict with any documented anti-pattern, design debt item, or design decision
- No Design Debt table entry is being violated (none of the five debt items relate to DTO constructor registration)

### Recommendations for Architect

1. **Follow the Auth Type Registration Precedent**: The `FactoryServiceRegistrar` already emits explicit DI registrations for auth types discovered at compile time. Use the same pattern for DTO constructor lambdas: emit registration calls in the generated `FactoryServiceRegistrar` method so they execute during `AddNeatooRemoteFactory()`.

2. **DTO Discovery Must Happen in Transform Phase**: As the architect already identified, `ITypeSymbol` analysis must happen in `FactoryGenerator.Transform.cs` (specifically in the `TypeFactoryMethods` method or `MethodInfo` constructor at line 676-688 of `FactoryGenerator.Types.cs`), where `methodSymbol.ReturnType` is still an `ITypeSymbol`. The extracted DTO type information (fully qualified name) must then flow through `TypeInfo` -> `FactoryModelBuilder` -> `FactoryGenerationUnit` to the renderers.

3. **Exclude Records**: Use the same detection rule as `RecordBypassConverterFactory`: types with no public parameterless constructor and at least one parameterized constructor are records/immutable types. Only register types that have a public parameterless constructor and are not Neatoo types or DI-registered services.

4. **Unwrap Generic Collection Types**: For return types like `IReadOnlyList<ExampleDto>` or `Task<List<SomeDto>>`, the generator must unwrap generic type arguments to discover the inner DTO type. The `MethodInfo` constructor already unwraps `Task<T>` (line 680-688); a similar unwrapping for collection types (`IReadOnlyList<T>`, `List<T>`, `IEnumerable<T>`, etc.) is needed.

5. **Maintain Design Project as Verification**: `ExampleDto` in `AllPatterns.cs` is the exact type that triggered the bug. After the fix, the `InterfaceFactoryTests` must still pass in the two DI container test mode, AND the published Blazor WASM Design client must successfully deserialize `ExampleDto` without `DeserializeNoConstructor` errors.

6. **Replace, Don't Supplement, the Activator.CreateInstance Path**: The `else if` branch in `NeatooJsonTypeInfoResolver.cs:38-48` should be replaced with the registry lookup, not kept alongside it. The `Activator.CreateInstance` fallback provides a false sense of safety since it fails under trimming. If the registry has no entry, the type was not discovered by the generator and should fall through to STJ's default behavior (which will produce a clear error if the constructor was trimmed).

---

## Plans

- [Generator-Emitted DTO Constructor Lambdas Plan](../plans/generator-dto-constructor-plan.md)

---

## Tasks

- [x] Architect comprehension check (Step 2)
- [x] Business requirements review (Step 3) — APPROVED
- [x] Architect plan and design (Step 4)
- [x] Developer review (Step 5) — Approved
- [x] Implementation (Step 7) — Complete
- [x] Verification (Step 8) — Architect VERIFIED, Requirements SATISFIED
- [x] Documentation (Step 9) — CLAUDE-DESIGN.md updated, docs/trimming.md updated

---

## Progress Log

### 2026-03-30
- Completed verification: Architect VERIFIED (2142 tests passed, 7/7 scenarios verified, 9/9 acceptance criteria met)
- Requirements verification: REQUIREMENTS SATISFIED (all 13 requirements traced, no violations)
- Documentation: Updated `CLAUDE-DESIGN.md` (new DTO Constructor Registry section, quick decisions, design debt). Updated `docs/trimming.md` (nested DTO limitation note).
- Todo marked Complete. Nested DTO gap tracked separately.

### 2026-03-31
- **Nested DTO gap found in zTreatment production deployment.** The generator emits `DtoConstructorRegistry.Register<AdminUserListItem>()` but NOT `DtoConstructorRegistry.Register<AdminClinicAssignment>()`. `AdminClinicAssignment` is a property (`List<AdminClinicAssignment> Clinics`) on `AdminUserListItem` — a nested DTO that the generator doesn't discover.
- The generator must walk DTO property types recursively to find nested DTOs that also need constructor registration. Current scope only covers direct return types from `[Factory]` methods.
- Error in production: `DeserializeNoConstructor, JsonConstructorAttribute, zTreatment.DomainModels.Admin.AdminClinicAssignment`
- Generated code at `AdminCommandsFactory.g.cs` lines 162-167 registers 5 top-level DTOs but misses `AdminClinicAssignment`
- **This is a scope gap in the existing plan's DTO discovery logic (Gap #1 and Recommendation #4).** The unwrapping of generic collection types is implemented, but the recursive walk into DTO properties is not.

### 2026-03-25
- Created todo after confirming v0.23.2 Activator.CreateInstance fix did not resolve the issue under trimming
- Carrying forward learnings from prior attempt: DynamicallyAccessedMembers insufficient, Activator insufficient, need generator-emitted static constructor references
- Developer's prior review identified that DTO discovery must use ITypeSymbol in FactoryModelBuilder, not string-based return types in renderers
- **Reproduced the bug** using Design project:
  1. Fixed `index.html` script tag (was `blazor.webassembly#[.{fingerprint}].js`, changed to `blazor.webassembly.js`)
  2. `dotnet publish src/Design/Design.Server/Design.Server.csproj -c Release -f net10.0 -o ./publish-test`
  3. Run `dotnet Design.Server.dll --urls "http://localhost:5199"` from publish-test/
  4. Navigate to http://localhost:5199, click "Get All Items"
  5. Error: `DeserializeNoConstructor, JsonConstructorAttribute, Design.Domain.FactoryPatterns.ExampleDto`
- This is the exact reproduction of issue #48 — `ExampleDto` constructor metadata stripped by trimmer
- **Documentation deliverable (Step 9):** Add a "DTO Serialization and Trimming" section to `docs/trimming.md` explaining why `IsTrimmable=true` + `DefaultJsonTypeInfoResolver` breaks plain DTO deserialization, and how the generator solves it. Key point: normal Blazor apps don't hit this because their assemblies aren't trimmed — we hit it because we intentionally trim to remove server-only business logic from the client, which is a core value proposition of RemoteFactory.

---

## Completion Verification

Before marking this todo as Complete, verify:

- [x] All builds pass
- [x] All tests pass

**Verification results:**
- Build: 0 errors, all projects build (net9.0 + net10.0)
- Tests: 2142 passed, 0 failed, 6 skipped (pre-existing)

---

## Results / Conclusions

Implementation complete. The source generator now discovers plain DTO return types at compile time and emits `DtoConstructorRegistry.Register<T>(() => new T())` lambdas in `FactoryServiceRegistrar`. `NeatooJsonTypeInfoResolver` uses the registry instead of `Activator.CreateInstance` (which was removed). Records are correctly excluded. All three factory patterns (class, interface, static) are scanned. Generic collections and nullable types are unwrapped to discover inner DTOs.

**Known limitation (tracked separately):** Nested DTOs — DTO properties that are themselves DTOs (e.g., `List<AdminClinicAssignment>` on `AdminUserListItem`) — are not discovered recursively. Only direct return types and their generic type arguments from factory method signatures are registered. This was discovered in zTreatment production (2026-03-31) and documented in CLAUDE-DESIGN.md Design Debt table.
