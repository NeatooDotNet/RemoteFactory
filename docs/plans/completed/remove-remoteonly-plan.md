# Remove RemoteOnly Factory Mode

**Date:** 2026-03-07
**Related Todo:** [Remove RemoteOnly Factory Mode](../../todos/completed/remove-remoteonly.md)
**Status:** Complete
**Last Updated:** 2026-03-08

---

## Overview

Remove the entire `FactoryMode` compile-time concept from RemoteFactory. This includes the `FactoryMode` enum, `FactoryModeAttribute`, all generator branching on `RemoteOnly` vs `Full`, the `FactoryGenerationUnit.Mode` property, the `FactoryText.Mode` property, the `TypeInfo.FactoryMode` property, the `GetFactoryMode()` detection method, the `RemoteOnlyTests` test project, and all documentation/skill/example references. After removal, the generator always produces the full code path (what was previously `FactoryMode.Full`). IL Trimming via `NeatooRuntime.IsServerRuntime` is the replacement mechanism for client bundle size reduction.

---

## Business Requirements Context

**Source:** [Todo Requirements Review](../todos/remove-remoteonly.md#requirements-review)

### Relevant Existing Requirements

#### Business Rules

- **Req 1 (FactoryMode is compile-time; NeatooFactory is runtime):** The `FactoryMode` enum (`Full`, `RemoteOnly`) in `src/RemoteFactory/FactoryAttributes.cs:148-162` is purely a compile-time generator concept. The runtime `NeatooFactory` enum (`Server`, `Remote`, `Logical`) in `src/RemoteFactory/AddRemoteFactoryServices.cs:10-26` is completely separate and must remain untouched. -- Relevance: This removal ONLY affects the compile-time `FactoryMode` enum. The runtime `NeatooFactory` must not be modified.

- **Req 2 (IL Trimming replaces RemoteOnly):** `NeatooRuntime.IsServerRuntime` (`src/RemoteFactory/NeatooRuntime.cs`) with `[FeatureSwitchDefinition]` provides the same client bundle size reduction that `RemoteOnly` provided. Documented in completed exploration plan at `docs/plans/completed/explore-trimming-remote-only.md`. -- Relevance: Removing RemoteOnly is safe because the IL Trimming approach is already implemented and tested.

- **Req 3 (Generator branches on FactoryMode in multiple locations):** All RemoteOnly branches produce a subset of what Full produces. After removal, only the Full code path remains. -- Relevance: Removal simplifies by eliminating conditional branches, not by adding new code paths.

- **Req 4 (Design Source of Truth references FactoryMode):** `src/Design/CLAUDE-DESIGN.md` line 670 and `src/Design/Design.Client.Blazor/AssemblyAttributes.cs` must be updated. -- Relevance: Design projects must reflect the removal.

- **Req 10 (FactoryMode is in the public NuGet API surface):** `FactoryMode` enum and `FactoryModeAttribute` ship in the `Neatoo.RemoteFactory` NuGet package. Removing them is a breaking change. -- Relevance: Requires major version bump and migration guide.

#### Existing Tests

- **RemoteOnlyTests project** (`src/Tests/RemoteOnlyTests/`): 3 sub-projects (Client, Server, Integration) that specifically test the RemoteOnly compile-time mode. Behavioral coverage (client-server round-trip for Create, Fetch, Save) is already provided by `src/Tests/RemoteFactory.IntegrationTests/` which uses the Full mode with the two-container pattern. -- Relevance: This project is removed entirely; no test coverage is lost.

- **TrimmingTests project** (`src/Tests/RemoteFactory.TrimmingTests/`): Standalone console app that verifies IL Trimming correctly removes server-only types when `NeatooRuntime.IsServerRuntime=false`. Uses `PublishTrimmed=true`, `TrimMode=full`, and `RuntimeHostConfigurationOption`. -- Relevance: This project already validates IL Trimming as the replacement mechanism for RemoteOnly. No changes needed to this project. It confirms that the "just use Full mode + IL Trimming" path works.

- **Design.Tests** (`src/Design/Design.Tests/`): 26 tests covering all three factory patterns. These use Full mode. -- Relevance: Must continue passing after removal. These validate the "Full path only" behavior.

- **RemoteFactory.UnitTests and IntegrationTests**: All existing tests use Full mode. -- Relevance: Must continue passing.

### Gaps

1. **No migration guide exists.** Consumers currently using `[assembly: FactoryMode(FactoryMode.RemoteOnly)]` need documented migration: remove the attribute, add IL Trimming configuration instead.

2. **`FactoryModeOption` naming inconsistency.** Several docs and reference app samples use `FactoryModeOption.RemoteOnly` instead of `FactoryMode.RemoteOnly`. Verified this is a documentation error -- `FactoryModeOption` does not exist as a type in the codebase. All references are in comments within `src/docs/reference-app/` sample files and in doc markdown. These are cleaned up as part of this removal.

### Contradictions

None. The reviewer found no contradictions between this removal and any documented design pattern or anti-pattern.

### Recommendations for Architect

1. Remove the entire FactoryMode concept (enum + attribute + all branching), not just the RemoteOnly value.
2. Update Design.Client.Blazor to demonstrate IL Trimming instead.
3. Remove all "Trimming vs RemoteOnly" framing from docs.
4. Major version bump required.

---

## Business Rules (Testable Assertions)

Since this is a removal task, the business rules define what must be true AFTER the removal -- both that things work correctly and that removed concepts are truly gone.

### Generator Output Rules (Post-Removal)

1. WHEN a class factory with `[Remote]` methods is compiled after removal, THEN the generator EMITS both a local constructor (taking `IServiceProvider`) and a remote constructor (taking `IServiceProvider, IMakeRemoteDelegateRequest`). Expected: Both constructors present in generated code. -- Source: Req 3 (Full mode behavior is the only remaining behavior)

2. WHEN a class factory with `[Remote]` methods is compiled after removal, THEN the generator EMITS `LocalMethod` implementations for all methods (including remote ones). Expected: `LocalCreate`, `LocalFetch`, `LocalInsert`, `LocalUpdate`, `LocalDelete`, `LocalSave` all present. -- Source: Req 3

3. WHEN an interface factory is compiled after removal, THEN the generator EMITS both local and remote constructors AND `LocalMethod` implementations for all methods. Expected: Both constructors and all LocalMethod bodies present. -- Source: Req 3

4. WHEN a static factory with `[Execute]` methods is compiled after removal, THEN the generator EMITS local delegate registrations inside the `if(remoteLocal == NeatooFactory.Server)` block. Expected: Local registrations present. -- Source: Req 3

5. WHEN a class factory's `FactoryServiceRegistrar` is generated after removal, THEN entity registrations (`AddTransient<ImplementationType>()`), `IFactorySave<T>` registrations, and delegate service registrations are always emitted (without any compile-time mode guard). Expected: All registrations present unconditionally. -- Source: Req 3

6. WHEN a class factory has `[Remote]` methods with `[Event]` attributes, THEN the generator EMITS local event registrations. Expected: Local event registrations present. -- Source: Req 3

7. WHEN `MakeRemoteDelegateRequest` field is generated in any factory class after removal, THEN it is declared as nullable (`IMakeRemoteDelegateRequest?`). Expected: Nullable declaration (the Full mode pattern). -- Source: Req 3 (Full mode used nullable; RemoteOnly used non-nullable)

### Compilation Rules

8. WHEN the solution is built after removal, THEN there are zero references to `FactoryMode`, `FactoryModeAttribute`, `FactoryModeOption`, or `RemoteOnly` in any non-historical source file (generator, core library, tests, design projects, examples, reference app compilable code). Expected: Zero matches in active source. -- Source: NEW

9. WHEN the solution is built after removal, THEN the `NeatooFactory` enum (`Server`, `Remote`, `Logical`) in `AddRemoteFactoryServices.cs` is unchanged. Expected: Enum definition identical to current. -- Source: Req 1

10. WHEN the solution is built after removal, THEN `NeatooRuntime.IsServerRuntime` in `NeatooRuntime.cs` is unchanged. Expected: Feature switch definition identical to current. -- Source: Req 2

### Pipeline Model Rules

11. WHEN `FactoryGenerationUnit` is constructed after removal, THEN it has no `Mode` property or parameter. Expected: No `FactoryMode` concept in the model. -- Source: Req 3, Gap 1 (reviewer recommended full removal)

12. WHEN `FactoryText` is constructed after removal (legacy code path), THEN it has no `Mode` property or parameter. Expected: No `FactoryMode` concept in FactoryText. -- Source: Req 3

13. WHEN `TypeInfo` is constructed after removal, THEN it has no `FactoryMode` property and does not call `GetFactoryMode()`. Expected: No FactoryMode detection in TypeInfo. -- Source: Req 3

### Test Rules

14. WHEN `dotnet test src/Neatoo.RemoteFactory.sln` is run after removal, THEN all tests pass with zero failures. Expected: 100% pass rate. -- Source: Req 6 (RemoteOnly tests removed; remaining tests unaffected)

15. WHEN the `RemoteOnlyTests` directory is checked after removal, THEN it does not exist. Expected: Directory deleted. -- Source: Req 6

16. WHEN the solution file is checked after removal, THEN it contains no references to `RemoteOnlyTests`. Expected: Zero matches. -- Source: Req 6

### Documentation Rules

17. WHEN `docs/factory-modes.md` is read after removal, THEN it describes only the three runtime modes (Server, Remote, Logical) with no mention of compile-time FactoryMode or RemoteOnly. Expected: No "Compile-Time Modes" section. -- Source: Req 7

18. WHEN `docs/trimming.md` is read after removal, THEN the "Trimming vs RemoteOnly" section is removed and IL Trimming is presented as the sole mechanism for client bundle reduction. Expected: No RemoteOnly comparison. -- Source: Req 7, Recommendation 5

19. WHEN Design.Client.Blazor's `AssemblyAttributes.cs` is read after removal, THEN it either does not exist or demonstrates IL Trimming configuration (not FactoryMode). Expected: No `[assembly: FactoryMode(...)]`. -- Source: Req 4, Recommendation 3

20. WHEN `CLAUDE-DESIGN.md` line 670 area is read after removal, THEN it no longer references `AssemblyAttributes.cs` for FactoryMode configuration. Expected: Updated or removed reference. -- Source: Req 4

### OrderEntry Example Rules

21. WHEN the `OrderEntry.Domain.Client` directory is checked after removal, THEN it does not exist. Expected: Directory deleted. -- Source: NEW (project existed solely for RemoteOnly)

22. WHEN the `OrderEntry.Domain.Server` directory is checked after removal, THEN it does not exist. Expected: Directory deleted. -- Source: NEW (counterpart to Domain.Client)

23. WHEN `OrderEntry.Domain/OrderEntry.Domain.csproj` is checked after removal, THEN it exists as a proper project with `<ProjectReference>` to `Generator`, `RemoteFactory`, and `OrderEntry.Ef`. Expected: New project file exists and compiles domain files directly. -- Source: NEW (Concern 8 resolution -- Option (a))

24. WHEN the OrderEntry domain files (`Order.cs`, `OrderLine.cs`, `OrderLineList.cs`) are read after removal, THEN they contain no `#if CLIENT`, `#if !CLIENT`, `#else`, or `#endif` conditional compilation guards, and no throw-only placeholder methods. Expected: Zero conditional compilation guards; all methods are real implementations. -- Source: NEW (Concern 8 resolution)

25. WHEN `OrderEntry.BlazorClient.csproj` is read after removal, THEN its ProjectReference points to `OrderEntry.Domain` (not `OrderEntry.Domain.Client`). Expected: Direct domain reference. -- Source: NEW

26. WHEN the solution file is checked after removal, THEN it contains no `OrderEntry.Domain.Client` or `OrderEntry.Domain.Server` project entries, and contains an `OrderEntry.Domain` project entry. Expected: Old entries removed, new entry added. -- Source: NEW

### Test Scenarios

| # | Scenario | Inputs / State | Rule(s) | Expected Result |
|---|----------|---------------|---------|-----------------|
| 1 | Class factory generates both constructors | Build any class with `[Factory]` + `[Remote]` methods | Rule 1 | Generated code has local ctor (IServiceProvider) and remote ctor (IServiceProvider, IMakeRemoteDelegateRequest) |
| 2 | Class factory generates all LocalMethods | Build class with `[Remote] [Fetch]` method | Rule 2 | `LocalFetch` method body present in generated code |
| 3 | Interface factory generates full code | Build interface with `[Factory]` | Rule 3 | Both constructors + LocalMethod bodies present |
| 4 | Static factory local registrations present | Build static class with `[Execute]` method | Rule 4 | Local delegate registration inside Server block |
| 5 | FactoryServiceRegistrar has entity registration | Build class with `[Factory]` + instance method (not constructor/static) | Rule 5 | `services.AddTransient<T>()` present unconditionally |
| 6 | MakeRemoteDelegateRequest is nullable | Build any factory with `[Remote]` methods | Rule 7 | Field declared as `IMakeRemoteDelegateRequest?` (nullable) |
| 7 | No FactoryMode references in source | `grep -r "FactoryMode" src/ --include="*.cs"` excluding completed docs/todos | Rule 8 | Zero matches |
| 8 | NeatooFactory enum untouched | Read `AddRemoteFactoryServices.cs` | Rule 9 | Enum identical: Server, Remote, Logical |
| 9 | All tests pass | `dotnet test src/Neatoo.RemoteFactory.sln` | Rule 14 | Zero failures |
| 10 | RemoteOnlyTests removed from solution | Read solution file | Rule 16 | No RemoteOnlyTests references |
| 11 | Design.Client.Blazor no FactoryMode | Read AssemblyAttributes.cs | Rule 19 | No `[assembly: FactoryMode(...)]` |
| 12 | Factory-modes doc is runtime-only | Read `docs/factory-modes.md` | Rule 17 | No compile-time modes section |
| 13 | OrderEntry.Domain.Client deleted | Check directory existence | Rule 21 | Directory does not exist |
| 14 | OrderEntry.Domain.Server deleted | Check directory existence | Rule 22 | Directory does not exist |
| 15 | OrderEntry.Domain.csproj exists | Check `OrderEntry.Domain/OrderEntry.Domain.csproj` | Rule 23 | Project file exists with references to Generator, RemoteFactory, and OrderEntry.Ef |
| 16 | Domain files have no conditional compilation | Read `Order.cs`, `OrderLine.cs`, `OrderLineList.cs` | Rule 24 | Zero `#if CLIENT` / `#if !CLIENT` / `#else` / `#endif` guards; no throw-only placeholders |
| 17 | BlazorClient references Domain directly | Read `OrderEntry.BlazorClient.csproj` | Rule 25 | ProjectReference to `OrderEntry.Domain` |
| 18 | Solution has no OrderEntry Client/Server | Read solution file | Rule 26 | Zero matches for OrderEntry.Domain.Client/Server; has OrderEntry.Domain entry |

---

## Approach

This is a pure removal/simplification task. The strategy is:

1. **Remove from inside out**: Start with the generator (where FactoryMode branches live), then the model/pipeline types, then the public API types, then the test project, then docs/examples/skills.

2. **Every FactoryMode conditional becomes the Full-mode branch**: Where the generator has `if (mode == FactoryMode.RemoteOnly) { ... } else { /* Full mode */ ... }`, remove the entire `if/else` and keep only the Full-mode body. Where it has `if (mode == FactoryMode.Full) { ... }`, remove the guard and always execute the body.

3. **Remove FactoryMode from method signatures**: All renderer methods that accept `FactoryMode mode` as a parameter drop that parameter. The builder stops passing `typeInfo.FactoryMode`. The model types lose the `Mode` property.

4. **Remove public API surface**: Delete the `FactoryMode` enum and `FactoryModeAttribute` class from `FactoryAttributes.cs`.

5. **Delete RemoteOnlyTests**: Remove the directory tree and solution file references.

6. **Update documentation**: Remove all RemoteOnly references from docs, skills, and examples.

---

## Design

### Generator Changes

#### Renderers

All three renderers (`ClassFactoryRenderer`, `InterfaceFactoryRenderer`, `StaticFactoryRenderer`) have methods that accept a `FactoryMode mode` parameter. After removal:

**ClassFactoryRenderer.cs** (most changes):
- `RenderFactoryClass(sb, model, mode)` -> `RenderFactoryClass(sb, model)`: Remove field nullability branch (line 178); always use nullable `IMakeRemoteDelegateRequest?`
- `RenderConstructors(sb, model, mode)` -> `RenderConstructors(sb, model)`: Remove RemoteOnly branch (line 265); always generate both constructors (Full mode path)
- `RenderReadMethod(sb, method, model, mode)` -> `RenderReadMethod(sb, method, model)`: Remove `mode == FactoryMode.Full || !method.IsRemote` guard (line 332); always call `RenderReadLocalMethod`
- `RenderLocalMethod(sb, method, model, mode)` -> `RenderLocalMethod(sb, method, model)`: Remove mode guard for write methods (line 214)
- `RenderClassExecuteMethod(sb, method, model, mode)` -> `RenderClassExecuteMethod(sb, method, model)`: Remove mode guard (line 772); always call `RenderClassExecuteLocalMethod`
- `RenderSaveMethod(sb, method, model, mode)` -> `RenderSaveMethod(sb, method, model)`: Remove mode guard (line 1020); always call `RenderSaveLocalMethod`
- `RenderCanMethod(sb, method, model, mode)` -> `RenderCanMethod(sb, method, model)`: Remove mode guard (line 1307); always call `RenderCanLocalMethod`
- `RenderFactoryServiceRegistrar(sb, model, mode)` -> `RenderFactoryServiceRegistrar(sb, model)`: Remove `mode == FactoryMode.Full` guards (lines 1501, 1511, 1554); always emit entity registration, IFactorySave registration, delegate registrations, and local event registrations

**InterfaceFactoryRenderer.cs**:
- `RenderFactoryClass(sb, model, mode)` -> `RenderFactoryClass(sb, model)`: Remove field nullability branch (line 93); always use nullable
- `RenderConstructors(sb, model, mode)` -> `RenderConstructors(sb, model)`: Remove RemoteOnly branch (line 156); always generate both constructors
- `RenderMethod(sb, method, model, mode)` -> `RenderMethod(sb, method, model)`: Remove mode guard (line 235); always call `RenderLocalMethod`
- `RenderFactoryServiceRegistrar(sb, model, mode)` -> `RenderFactoryServiceRegistrar(sb, model)`: Remove mode guard (line 468); always emit delegate registrations

**StaticFactoryRenderer.cs**:
- `RenderFactoryServiceRegistrar(sb, model, mode)` -> `RenderFactoryServiceRegistrar(sb, model)`: Remove `mode == FactoryMode.Full` guard (line 132); always emit local registrations

#### FactoryGenerator.cs (Legacy Code Path)

The legacy code path in `FactoryGenerator.cs` (used for the old-style generation) also branches on FactoryMode:
- Lines 212, 237: `typeInfo.FactoryMode == FactoryMode.Full` guards around local method/event registrations -- remove guards, always emit
- Line 738: `new FactoryText(typeInfo.FactoryMode)` -- remove FactoryMode parameter
- Lines 784-800: Constructor generation branch for RemoteOnly vs Full -- remove RemoteOnly branch, keep Full
- Lines 957-971: `GetFactoryMode()` method -- delete entirely

#### FactoryGenerator.Types.cs (Legacy Types)

- Line 226: `this.FactoryMode = GetFactoryMode(semanticModel)` -- remove
- Line 294: `public FactoryMode FactoryMode { get; }` -- remove
- Lines 1033, 1041, 1053, 1330: `classText.Mode == FactoryMode.Full` guards -- remove guards, always execute body
- Lines 1819-1824: `FactoryText.Mode` property and constructor parameter -- remove

### Model Changes

**FactoryGenerationUnit.cs**: Remove `Mode` property and `FactoryMode mode` constructor parameter. Update all `new FactoryGenerationUnit(...)` calls in `FactoryModelBuilder.cs`.

**FactoryModelBuilder.cs**: Remove `mode: typeInfo.FactoryMode` from all three `Build*Factory` methods (lines 102, 154, 282).

### Public API Changes

**FactoryAttributes.cs** (lines 145-200): Delete the `FactoryMode` enum and `FactoryModeAttribute` class entirely.

### Solution Changes

**Neatoo.RemoteFactory.sln**: Remove all RemoteOnlyTests project entries and their build configurations.

### File Deletions

- `src/Tests/RemoteOnlyTests/` -- entire directory tree (3 sub-projects)
- `src/Design/Design.Client.Blazor/AssemblyAttributes.cs` -- delete (the file's only purpose was to demonstrate FactoryMode)

### Documentation Changes

Files requiring RemoteOnly content removal or rewrite:
- `docs/factory-modes.md` -- Remove "Compile-Time Modes" section
- `docs/trimming.md` -- Remove "Trimming vs RemoteOnly" comparison
- `docs/attributes-reference.md` -- Remove `[assembly: FactoryMode]` entries
- `docs/decision-guide.md` -- Remove "Full vs RemoteOnly Mode?" and "IL Trimming or RemoteOnly?" sections
- `docs/service-injection.md` -- Remove brief RemoteOnly mention
- `docs/events.md` -- Remove brief RemoteOnly mention
- `README.md` -- Remove FactoryMode/RemoteOnly from feature list and examples

### Skills Changes

- `skills/RemoteFactory/SKILL.md` -- Remove RemoteOnly from quick decisions table
- `skills/RemoteFactory/references/setup.md` -- Remove "[assembly: FactoryMode] for Client Assemblies" section
- `skills/RemoteFactory/references/trimming.md` -- Remove "Trimming vs RemoteOnly" section

### OrderEntry Example Changes

The OrderEntry example uses a Client/Server assembly split pattern where `OrderEntry.Domain.Client` exists specifically for `[assembly: FactoryMode(FactoryMode.RemoteOnly)]` and `OrderEntry.Domain.Server` is the server counterpart. After removal, this split is unnecessary -- a single `OrderEntry.Domain` project replaces both. IL Trimming via `NeatooRuntime.IsServerRuntime` handles client bundle reduction at publish time.

**Key structural change:** `OrderEntry.Domain/` is currently a bare directory of `.cs` files (no `.csproj`) that `Domain.Client` and `Domain.Server` link to via `<Compile Include>`. A new `OrderEntry.Domain.csproj` must be created to compile these files directly.

**Handling `#if CLIENT` / `#if !CLIENT` guards:** Three domain files use conditional compilation:

- **`Order.cs`**: Has `#if CLIENT` (lines 45-78) with throw-only placeholder methods, and `#if !CLIENT` / `#else` (lines 80-209) with full server implementations. After removal: **delete** the entire `#if CLIENT` block (placeholder methods), **keep** the server-side implementations, **remove** the `#if !CLIENT` / `#else` / `#endif` guard lines. The `using OrderEntry.Ef;` and `using Microsoft.EntityFrameworkCore;` directives (currently behind `#if !CLIENT`) become unconditional.

- **`OrderLine.cs`**: Has `#if !CLIENT` (lines 49-62) wrapping a server-only `Fetch(OrderLineEntity entity)` method. The `[Create]` constructor is outside any guard. After removal: **keep** the Fetch method, **remove** the `#if !CLIENT` / `#endif` guard lines. The `using OrderEntry.Ef;` directive becomes unconditional.

- **`OrderLineList.cs`**: Has `#if !CLIENT` (lines 62-79) wrapping a server-only `Fetch(ICollection<OrderLineEntity> entities, [Service] IOrderLineFactory lineFactory)` method. After removal: **keep** the Fetch method, **remove** the `#if !CLIENT` / `#endif` guard lines. The `using OrderEntry.Ef;` directive becomes unconditional.

**EF reference handling:** After removing the guards, the domain files have unconditional `using OrderEntry.Ef;` references. The new `OrderEntry.Domain.csproj` **must** include a `<ProjectReference>` to `OrderEntry.Ef`. This means `OrderEntry.BlazorClient` will transitively receive the EF dependency at compile time. This is acceptable because:
1. IL Trimming removes unused EF types from the published client bundle
2. The `[Service]` method-injection pattern ensures EF services are never registered in the client DI container
3. This is the intentional design shift: compile-time isolation is replaced by publish-time trimming

**New `OrderEntry.Domain.csproj` contents:**
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
    <CompilerGeneratedFilesOutputPath>Generated</CompilerGeneratedFilesOutputPath>
    <!-- Suppress warnings for example code: CA1852 (seal type), CS0067 (unused event) -->
    <NoWarn>$(NoWarn);CA1852;CS0067</NoWarn>
  </PropertyGroup>

  <ItemGroup>
    <Compile Remove="$(CompilerGeneratedFilesOutputPath)/**/*.cs" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\Generator\Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
    <ProjectReference Include="..\..\..\RemoteFactory\RemoteFactory.csproj" />
    <!-- Domain code directly references OrderEntry.Ef types in [Service]-injected method bodies -->
    <ProjectReference Include="..\OrderEntry.Ef\OrderEntry.Ef.csproj" />
  </ItemGroup>

  <!-- Only delete Generated folder for first TFM to preserve files across multi-target builds -->
  <Target Name="PreBuild" BeforeTargets="BeforeBuild" Condition="'$(TargetFramework)' == 'net9.0'">
    <RemoveDir Directories="$(CompilerGeneratedFilesOutputPath)" />
  </Target>

</Project>
```

**File operations:**
- `src/Examples/OrderEntry/OrderEntry.Domain.Client/` -- Delete entire project directory
- `src/Examples/OrderEntry/OrderEntry.Domain.Server/` -- Delete entire project directory
- `src/Examples/OrderEntry/OrderEntry.Domain/OrderEntry.Domain.csproj` -- **Create new** (see contents above)
- `src/Examples/OrderEntry/OrderEntry.Domain/Order.cs` -- Remove `#if CLIENT` block (delete placeholder methods lines 45-79), remove `#if !CLIENT` / `#else` / `#endif` guard lines, make `using OrderEntry.Ef;` and `using Microsoft.EntityFrameworkCore;` unconditional, remove the `#region` / `#endregion` if they reference client-server-separation, update class comment
- `src/Examples/OrderEntry/OrderEntry.Domain/OrderLine.cs` -- Remove `#if !CLIENT` / `#endif` guard lines, make `using OrderEntry.Ef;` unconditional, remove `#region` / `#endregion` if they reference client-server-separation, update class comment
- `src/Examples/OrderEntry/OrderEntry.Domain/OrderLineList.cs` -- Remove `#if !CLIENT` / `#endif` guard lines, make `using OrderEntry.Ef;` unconditional
- `src/Examples/OrderEntry/OrderEntry.Domain.Client/AssemblyAttributes.cs` -- Deleted with the project directory (contains `[assembly: FactoryMode(FactoryMode.RemoteOnly)]`)
- `src/Examples/OrderEntry/OrderEntry.BlazorClient/OrderEntry.BlazorClient.csproj` -- Update ProjectReference from `OrderEntry.Domain.Client` to `OrderEntry.Domain`
- `src/Examples/OrderEntry/OrderEntry.BlazorClient/Program.cs` -- Update comment from "RemoteOnly factories from Domain.Client" to reflect new architecture
- `src/Examples/OrderEntry/OrderEntry.BlazorClient/Pages/Home.razor` -- Rewrite architecture description: remove `FactoryMode.RemoteOnly` mentions, remove `OrderEntry.Domain.Client` / `OrderEntry.Domain.Server` references, describe single `OrderEntry.Domain` project with IL Trimming
- `src/Examples/OrderEntry/OrderEntry.Server/OrderEntry.Server.csproj` -- Update ProjectReference from `OrderEntry.Domain.Server` to `OrderEntry.Domain`. Remove the `ReferenceOutputAssembly="false"` comment about "transitive type conflicts between Domain.Client and Domain.Server" (no longer relevant). The `ReferenceOutputAssembly="false"` on the BlazorClient reference itself should stay (it prevents the Blazor client assembly from being loaded as a .NET reference). Remove the separate `OrderEntry.Ef` ProjectReference since it is now transitively included through `OrderEntry.Domain`.
- `src/Examples/OrderEntry/OrderEntry.Server/Program.cs` -- Update comment from "Domain.Server assembly" to "Domain assembly"
- `src/Neatoo.RemoteFactory.sln` -- Remove `OrderEntry.Domain.Client` and `OrderEntry.Domain.Server` project entries. Add `OrderEntry.Domain` project entry.

### Reference App Changes (12 files)

Files with `FactoryMode`/`RemoteOnly`/`FactoryModeOption` content requiring edits or deletion:

1. `src/docs/reference-app/EmployeeManagement.Domain/AssemblyAttributes.cs` -- Remove FactoryModeOption comments
2. `src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AssemblyAttributeSamples.cs` -- Remove FactoryModeOption references
3. `src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs` -- Remove RemoteOnly content from snippet; update to show only runtime modes
4. `src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs` -- Remove `modes-remoteonly-example` snippet region (lines 123-128); keep `modes-full-example` and `modes-logical-example`
5. `src/docs/reference-app/EmployeeManagement.Domain/Samples/FactoryModes/GeneratedCodeIllustrations.cs` -- Rename or update; the `FactoryModes` directory name is about runtime modes (Server/Remote/Logical) not compile-time FactoryMode, so the directory may stay but RemoteOnly content must be removed
6. `src/docs/reference-app/EmployeeManagement.Domain/Samples/ReadmeSamples.cs` -- Remove FactoryModeOption reference
7. `src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs` -- Remove RemoteOnly references
8. `src/docs/reference-app/EmployeeManagement.Tests/Samples/FactoryModes/LogicalModeTestingSample.cs` -- Check for RemoteOnly content (namespace uses `FactoryModes` which is about runtime modes; may need no content changes)
9. `src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/CompleteSetupExamples.cs` -- Remove RemoteOnly setup content
10. `src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/FactoryModeConfigurationSamples.cs` -- Check for RemoteOnly content
11. `src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/FactoryModes/FullModeServerExample.cs` -- Check for RemoteOnly references
12. `src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/FactoryModes/ServerModeConfigurationSample.cs` -- Check for RemoteOnly references

Note: The `FactoryModes/` directory names in the reference app correspond to RUNTIME modes (Server/Remote/Logical), not the compile-time `FactoryMode` enum. These directories stay but any RemoteOnly compile-time content within them must be removed.

### Design Project Changes

- `src/Design/CLAUDE-DESIGN.md` -- Update line 670 area to remove AssemblyAttributes.cs/FactoryMode reference
- `src/Design/Design.Client.Blazor/AssemblyAttributes.cs` -- Delete

---

## Implementation Steps

### Phase 1: Generator Core Removal

1. Remove `FactoryMode` enum and `FactoryModeAttribute` from `src/RemoteFactory/FactoryAttributes.cs`
2. Remove `Mode` property from `FactoryGenerationUnit` record; update constructor
3. Remove `mode:` argument from all `FactoryModelBuilder.Build*Factory` methods
4. Remove `FactoryMode` property from `TypeInfo` (FactoryGenerator.Types.cs line 294)
5. Remove `GetFactoryMode()` method from `FactoryGenerator.cs` (lines 957-971)
6. Remove `FactoryMode` assignment from `TypeInfo` constructor (FactoryGenerator.Types.cs line 226)
7. Remove `Mode` property from `FactoryText` class and its constructor parameter (FactoryGenerator.Types.cs lines 1819-1824)

### Phase 2: Renderer Simplification

8. Simplify `ClassFactoryRenderer`: Remove all `FactoryMode mode` parameters and `mode ==` conditionals. Keep only the Full-mode code paths. Update all call sites within the renderer.
9. Simplify `InterfaceFactoryRenderer`: Same treatment.
10. Simplify `StaticFactoryRenderer`: Same treatment.

### Phase 3: Legacy Code Path Cleanup

11. Simplify `FactoryGenerator.cs` legacy code path: Remove FactoryMode guards at lines 212, 237, 738, 784-800.
12. Simplify `FactoryGenerator.Types.cs` legacy factory method classes: Remove `classText.Mode` conditionals at lines 1033, 1041, 1053, 1330.

### Phase 4: Verify Generator Changes Compile

13. Build `src/Neatoo.RemoteFactory.sln` -- verify zero compiler errors
14. Run `dotnet test src/Neatoo.RemoteFactory.sln` -- verify all existing tests pass

### Phase 5: Remove RemoteOnlyTests and Collapse OrderEntry Example

15. Remove `src/Tests/RemoteOnlyTests/` directory tree
16. Remove RemoteOnlyTests project entries from `src/Neatoo.RemoteFactory.sln`
17. Delete `src/Examples/OrderEntry/OrderEntry.Domain.Client/` directory
18. Delete `src/Examples/OrderEntry/OrderEntry.Domain.Server/` directory
19. Create `src/Examples/OrderEntry/OrderEntry.Domain/OrderEntry.Domain.csproj` -- new project that compiles domain files directly, with references to Generator (as analyzer), RemoteFactory, and OrderEntry.Ef (see Design section for exact contents)
20. Update `src/Examples/OrderEntry/OrderEntry.Domain/Order.cs`:
    - Remove `#if CLIENT` block entirely (delete throw-only placeholder methods, lines 45-79)
    - Remove `#else` line (line 80)
    - Remove `#endif` line (line 209)
    - Remove `#if !CLIENT` line around `using` directives (line 5) and `#endif` (line 8)
    - Make `using OrderEntry.Ef;` and `using Microsoft.EntityFrameworkCore;` unconditional
    - Remove the `#region docs:concepts/client-server-separation:order-entity` / `#endregion` markers (or update the region name)
    - Update class summary comment to remove "Client: Placeholder methods" reference
21. Update `src/Examples/OrderEntry/OrderEntry.Domain/OrderLine.cs`:
    - Remove `#if !CLIENT` (line 49) and `#endif` (line 62) guard lines around Fetch method
    - Remove `#if !CLIENT` (line 5) and `#endif` (line 7) guard lines around `using OrderEntry.Ef;`
    - Make `using OrderEntry.Ef;` unconditional
    - Remove `#region` / `#endregion` markers (or update the region name)
    - Update class summary comment to remove "Simple entity with local [Create] - runs on both client and server" if misleading
22. Update `src/Examples/OrderEntry/OrderEntry.Domain/OrderLineList.cs`:
    - Remove `#if !CLIENT` (line 62) and `#endif` (line 79) guard lines around Fetch method
    - Remove `#if !CLIENT` (line 5) and `#endif` (line 7) guard lines around `using OrderEntry.Ef;`
    - Make `using OrderEntry.Ef;` unconditional
23. Update `src/Neatoo.RemoteFactory.sln`:
    - Remove `OrderEntry.Domain.Client` and `OrderEntry.Domain.Server` project entries
    - Add `OrderEntry.Domain` project entry
24. Update `src/Examples/OrderEntry/OrderEntry.BlazorClient/OrderEntry.BlazorClient.csproj` -- change ProjectReference from `OrderEntry.Domain.Client` to `OrderEntry.Domain`
25. Update `src/Examples/OrderEntry/OrderEntry.Server/OrderEntry.Server.csproj`:
    - Change ProjectReference from `OrderEntry.Domain.Server` to `OrderEntry.Domain`
    - Remove the `ReferenceOutputAssembly="false"` comment about "transitive type conflicts" (no longer relevant)
    - Remove the separate `OrderEntry.Ef` ProjectReference (now transitive through Domain)
26. Update `src/Examples/OrderEntry/OrderEntry.BlazorClient/Program.cs` -- update comment from "RemoteOnly factories from Domain.Client" to reflect single Domain project
27. Update `src/Examples/OrderEntry/OrderEntry.BlazorClient/Pages/Home.razor` -- rewrite architecture description: remove `FactoryMode.RemoteOnly`, `OrderEntry.Domain.Client`, `OrderEntry.Domain.Server` references; describe single `OrderEntry.Domain` with IL Trimming
28. Update `src/Examples/OrderEntry/OrderEntry.Server/Program.cs` -- update comment from "Domain.Server assembly" to "Domain assembly"
29. Build and test to confirm clean removal

### Phase 6: Update Design Projects

30. Delete `src/Design/Design.Client.Blazor/AssemblyAttributes.cs`
31. Update `src/Design/CLAUDE-DESIGN.md` to remove the FactoryMode reference at line 670
32. Build Design projects and run Design.Tests to verify

### Phase 7: Update Documentation, Skills, Examples, Reference App

33. Update `docs/factory-modes.md` -- remove Compile-Time Modes section
34. Update `docs/trimming.md` -- remove "Trimming vs RemoteOnly" comparison
35. Update `docs/attributes-reference.md` -- remove `[assembly: FactoryMode]` entries
36. Update `docs/decision-guide.md` -- remove RemoteOnly decision sections
37. Update `docs/service-injection.md` -- remove RemoteOnly mention
38. Update `docs/events.md` -- remove RemoteOnly mention
39. Update `README.md` -- remove FactoryMode/RemoteOnly references
40. Update `skills/RemoteFactory/references/setup.md` -- remove "[assembly: FactoryMode] for Client Assemblies" section (this is the only skill file with FactoryMode/RemoteOnly content; SKILL.md and trimming.md have zero matches)
41. Update all 12 reference app files -- remove FactoryModeOption/RemoteOnly/compile-time FactoryMode content. The `modes-remoteonly-example` snippet region in `FactoryModesSamples.cs` must be removed. `FactoryModes/` directories (about runtime modes) may stay but must have compile-time RemoteOnly content removed.
42. Run mdsnippets if any snippet-sourced content was affected (the `modes-remoteonly-example` region is snippeted into docs)
43. Build reference app to verify

### Phase 8: Final Verification

44. Full solution build: `dotnet build src/Neatoo.RemoteFactory.sln`
45. Full test run: `dotnet test src/Neatoo.RemoteFactory.sln`
46. Grep verification: Confirm zero FactoryMode/RemoteOnly references in active source (excluding completed docs/todos/plans and `FactoryModes/` directory names that refer to runtime modes)

---

## Acceptance Criteria

- [ ] `FactoryMode` enum and `FactoryModeAttribute` class do not exist in `FactoryAttributes.cs`
- [ ] `FactoryGenerationUnit` has no `Mode` property
- [ ] `TypeInfo` has no `FactoryMode` property
- [ ] `FactoryText` has no `Mode` property
- [ ] `GetFactoryMode()` method does not exist
- [ ] All three renderers have no `FactoryMode` parameters or conditionals
- [ ] Legacy code path in `FactoryGenerator.cs` has no FactoryMode branching
- [ ] `src/Tests/RemoteOnlyTests/` directory does not exist
- [ ] Solution file has no RemoteOnlyTests references
- [ ] `src/Examples/OrderEntry/OrderEntry.Domain.Client/` directory does not exist
- [ ] `src/Examples/OrderEntry/OrderEntry.Domain.Server/` directory does not exist
- [ ] `src/Examples/OrderEntry/OrderEntry.Domain/OrderEntry.Domain.csproj` exists with ProjectReferences to Generator, RemoteFactory, and OrderEntry.Ef
- [ ] OrderEntry domain files (`Order.cs`, `OrderLine.cs`, `OrderLineList.cs`) have no `#if CLIENT` / `#if !CLIENT` conditional compilation guards
- [ ] OrderEntry domain files have no throw-only placeholder methods
- [ ] Solution file has no `OrderEntry.Domain.Client` or `OrderEntry.Domain.Server` references
- [ ] Solution file has an `OrderEntry.Domain` project entry
- [ ] `OrderEntry.BlazorClient.csproj` references `OrderEntry.Domain` (not Domain.Client)
- [ ] `NeatooFactory` enum is unchanged (Server, Remote, Logical)
- [ ] `NeatooRuntime.IsServerRuntime` is unchanged
- [ ] `src/Design/Design.Client.Blazor/AssemblyAttributes.cs` is deleted
- [ ] `CLAUDE-DESIGN.md` no longer references FactoryMode
- [ ] All docs (`factory-modes.md`, `trimming.md`, `attributes-reference.md`, `decision-guide.md`, `service-injection.md`, `events.md`, `README.md`) have no RemoteOnly/FactoryMode references
- [ ] `skills/RemoteFactory/references/setup.md` has no RemoteOnly/FactoryMode references
- [ ] All 12 reference app files have no compile-time FactoryMode/RemoteOnly/FactoryModeOption content
- [ ] Solution builds with zero errors
- [ ] All tests pass with zero failures
- [ ] Grep for `FactoryMode|FactoryModeOption` in active `.cs` source returns zero matches (excluding `FactoryModes/` directory names which refer to runtime modes)

---

## Dependencies

- None. This is a self-contained removal task with no external dependencies.

---

## Risks / Considerations

1. **Breaking Change**: Any consumer using `[assembly: FactoryMode(FactoryMode.RemoteOnly)]` will get a compile error after upgrading. Migration: remove the attribute, optionally add IL Trimming configuration. Must be documented in release notes with migration guide.

2. **Generated Code Differences**: After removal, assemblies that previously used RemoteOnly will now have Full-mode generated code (additional local methods, both constructors). This is functionally correct -- the runtime DI registration (`NeatooFactory.Remote`) still controls which constructor is used. The extra generated code is trimmed by IL Trimming if configured.

3. **FactoryModeOption Documentation Error**: The `FactoryModeOption` type referenced in docs/samples never existed -- it was always `FactoryMode`. This error is cleaned up as part of the removal, not a separate concern.

4. **Legacy Code Path**: `FactoryGenerator.cs` and `FactoryGenerator.Types.cs` contain a legacy code generation path (pre-model/renderer refactor). This code also branches on FactoryMode and must be updated. The legacy path must be kept functional because it is still used for some generation scenarios. Specifically, the `FactoryText` constructor becomes parameterless after removing the `FactoryMode mode = FactoryMode.Full` default parameter; the call site at line 738 (`new FactoryText(typeInfo.FactoryMode)`) becomes simply `new FactoryText()`.

5. **FactoryMode.Logical in Design Comments**: `Design.Client.Blazor/AssemblyAttributes.cs` line 26 mentions `FactoryMode.Logical` as an available mode, but `Logical` was never a value in the `FactoryMode` enum (it exists only in the `NeatooFactory` runtime enum). This is another documentation error that is cleaned up by deleting the file.

6. **OrderEntry Example Collapse**: The `OrderEntry.Domain.Client` and `OrderEntry.Domain.Server` projects exist specifically to demonstrate the RemoteOnly client/server assembly split pattern. After FactoryMode removal, this split is unnecessary. Both projects are deleted. A new `OrderEntry.Domain.csproj` is created to compile the domain files directly. The `#if CLIENT` / `#if !CLIENT` conditional compilation guards are removed from the domain files. The `[Service]` method-injection pattern already ensures EF types are server-only at runtime. The existing `RemoteFactory.TrimmingTests` project validates IL Trimming as the replacement mechanism.

7. **InterfaceFactoryRenderer guard difference**: `InterfaceFactoryRenderer.RenderMethod` at line 235 uses `if (mode == FactoryMode.Full)` (no `|| !method.IsRemote` fallback), unlike the class factory equivalents. This is correct for interface factories where all methods are inherently remote. After removal, the guard disappears and LocalMethod is always emitted, which is the correct Full-mode behavior. No special handling needed.

8. **EF Transitive Dependency in BlazorClient**: After the OrderEntry collapse, `OrderEntry.Domain` references `OrderEntry.Ef`, which means `OrderEntry.BlazorClient` transitively receives EF Core packages at compile time. This is intentional -- the design shift replaces compile-time isolation with publish-time trimming. The EF types are trimmed from the published client bundle because they are only used in `[Service]`-injected method bodies that are dead code on the client. This is the same pattern validated by `RemoteFactory.TrimmingTests`.

---

## Architectural Verification

**Scope Table:**

| Component | Affected? | Change Type |
|-----------|-----------|-------------|
| `src/RemoteFactory/FactoryAttributes.cs` | Yes | Delete enum + attribute |
| `src/RemoteFactory/NeatooRuntime.cs` | No | Untouched |
| `src/RemoteFactory/AddRemoteFactoryServices.cs` | No | Untouched (NeatooFactory enum) |
| `src/Generator/Renderer/ClassFactoryRenderer.cs` | Yes | Remove FactoryMode params + conditionals |
| `src/Generator/Renderer/InterfaceFactoryRenderer.cs` | Yes | Remove FactoryMode params + conditionals |
| `src/Generator/Renderer/StaticFactoryRenderer.cs` | Yes | Remove FactoryMode params + conditionals |
| `src/Generator/Model/FactoryGenerationUnit.cs` | Yes | Remove Mode property |
| `src/Generator/Builder/FactoryModelBuilder.cs` | Yes | Remove mode: arguments |
| `src/Generator/FactoryGenerator.cs` | Yes | Remove GetFactoryMode(), legacy FactoryMode guards |
| `src/Generator/FactoryGenerator.Types.cs` | Yes | Remove TypeInfo.FactoryMode, FactoryText.Mode, classText.Mode guards |
| `src/Tests/RemoteOnlyTests/` | Yes | Delete entirely |
| `src/Examples/OrderEntry/OrderEntry.Domain.Client/` | Yes | Delete entirely (existed solely for RemoteOnly) |
| `src/Examples/OrderEntry/OrderEntry.Domain.Server/` | Yes | Delete entirely (counterpart to Domain.Client) |
| `src/Examples/OrderEntry/OrderEntry.Domain/OrderEntry.Domain.csproj` | Yes | **Create new** (replaces Client+Server split) |
| `src/Examples/OrderEntry/OrderEntry.Domain/*.cs` (3 domain files) | Yes | Remove `#if CLIENT`/`#if !CLIENT` guards, make `using` unconditional |
| `src/Examples/OrderEntry/` (BlazorClient, Server -- 5 files) | Yes | Update ProjectReferences, comments, Razor descriptions |
| `src/Design/Design.Client.Blazor/AssemblyAttributes.cs` | Yes | Delete |
| `src/Design/CLAUDE-DESIGN.md` | Yes | Update reference |
| `docs/` (7 files) | Yes | Remove RemoteOnly content |
| `skills/RemoteFactory/references/setup.md` | Yes | Remove FactoryMode section (only skill file with content) |
| `src/docs/reference-app/` (12 files) | Yes | Remove compile-time FactoryMode/RemoteOnly content |

**Breaking Changes:** Yes -- `FactoryMode` enum and `FactoryModeAttribute` are removed from the public API surface of the `Neatoo.RemoteFactory` NuGet package. Consumers using `[assembly: FactoryMode(FactoryMode.RemoteOnly)]` will get compile errors and must remove the attribute.

**Codebase Analysis:**
- Confirmed `FactoryModeOption` is a documentation-only error; no such type exists in compilable code
- Confirmed `NeatooFactory` (runtime) is completely separate from `FactoryMode` (compile-time); no shared references
- Confirmed all RemoteOnly guards produce a strict subset of Full mode output; removing guards always results in strictly more generated code, never less
- Confirmed the Design.Client.Blazor `AssemblyAttributes.cs` comment about `FactoryMode.Logical` is incorrect (Logical exists only in the NeatooFactory runtime enum)

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1-4: Generator Core + Renderers + Verify | developer | Yes | Core generator changes are interconnected; need accumulated context. ~15 files modified. | None |
| Phase 5: Remove RemoteOnlyTests + Collapse OrderEntry | developer | No | Follow-on from Phase 1-4; same agent can delete files, create new project, update domain files, fix project references. ~15 files (3 deleted directories, 1 new csproj, 3 domain files edited, 5 project/content files updated, solution file). | Phase 1-4 |
| Phase 6: Update Design Projects | developer | No | Small follow-on; same agent context. | Phase 5 |
| Phase 7: Documentation + Skills + Reference App | developer | Yes | Fresh context for doc-focused work; no dependency on generator details. ~20 files (7 docs + 1 skill + 12 ref-app). | Phase 6 (build must pass first) |
| Phase 8: Final Verification | developer | No | Resume from Phase 7 agent; run final builds/tests/grep. | Phase 7 |

**Parallelizable phases:** None -- each phase depends on the prior phase's changes being committed.

**Notes:** Phases 1-6 should be done by one agent invocation (start fresh, then resume through phases 5-6). Phase 5 is now the largest phase due to the OrderEntry restructuring (creating new csproj, editing 3 domain files, updating 5 project/content files). Phase 7 (documentation) benefits from a fresh agent because it is a different kind of work (editing markdown vs editing C#). Phase 8 is a quick verification pass by the same agent.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-03-07
**Concerns Addressed:** 2026-03-07
**Re-Reviewed:** 2026-03-07
**Concern 8 Resolved:** 2026-03-07
**Approved:** 2026-03-07

### Assertion Trace Verification

| Rule # | Implementation Path (method/condition) | Expected Result | Matches Rule? | Notes |
|--------|---------------------------------------|-----------------|---------------|-------|
| 1 | `ClassFactoryRenderer.RenderConstructors(sb, model, mode)` at line 263: currently branches on `mode == FactoryMode.RemoteOnly` (line 265). Plan removes the RemoteOnly branch (lines 265-279), leaving only the `else` block (lines 283-311) which generates both local ctor (`IServiceProvider`) and remote ctor (`IServiceProvider, IMakeRemoteDelegateRequest`). | Both constructors present | Yes | Removal of RemoteOnly branch leaves only the Full-mode code path that generates both ctors. Confirmed in source. |
| 2 | `ClassFactoryRenderer.RenderReadMethod(sb, method, model, mode)` at line 320: guard `mode == FactoryMode.Full \|\| !method.IsRemote` (line 332) controls `RenderReadLocalMethod`. Plan removes guard, always calls `RenderReadLocalMethod`. Same pattern at: `RenderClassExecuteMethod` (line 772), `RenderSaveMethod` (line 1020), `RenderCanMethod` (line 1307), and `RenderLocalMethod` for WriteMethodModel called from `RenderFactoryClass` (line 214). | All LocalMethod variants present | Yes | Five distinct call sites each have the same `mode == FactoryMode.Full \|\| !method.IsRemote` guard. Removing each guard means LocalMethod is always emitted. |
| 3 | `InterfaceFactoryRenderer.RenderConstructors(sb, model, mode)` at line 154: branches on `mode == FactoryMode.RemoteOnly` (line 156). `RenderMethod(sb, method, model, mode)` at line 223: guard `mode == FactoryMode.Full` (line 235) gates `RenderLocalMethod`. Plan removes RemoteOnly branch in ctors and removes Full guard in RenderMethod. | Both constructors + LocalMethod bodies present | Yes | Same pattern as class factory. Removal of RemoteOnly branch leaves both ctors; removal of Full guard means LocalMethod always emitted. |
| 4 | `StaticFactoryRenderer.RenderFactoryServiceRegistrar(sb, model, mode)` at line ~128: guard `mode == FactoryMode.Full` (line 132) wraps local delegate + event registrations inside the `if(remoteLocal == NeatooFactory.Logical \|\| NeatooFactory.Server)` runtime block. Plan removes the compile-time guard. | Local delegate registrations present inside Server block | Yes | After removing the `mode == FactoryMode.Full` guard at line 132, the `foreach` loops for local delegates and events always execute within the runtime `NeatooFactory.Server/Logical` block. |
| 5 | `ClassFactoryRenderer.RenderFactoryServiceRegistrar(sb, model, mode)` at line 1493: three `mode == FactoryMode.Full` guards at lines 1501, 1511, 1554. Line 1501: `mode == FactoryMode.Full && model.RequiresEntityRegistration` gates `AddTransient<ImplementationType>()`. Line 1511: `mode == FactoryMode.Full && model.HasDefaultSave` gates `IFactorySave<T>` registration. Line 1554: `mode == FactoryMode.Full` gates delegate service registrations. Plan removes the `mode == FactoryMode.Full` part, leaving only `model.RequiresEntityRegistration`, `model.HasDefaultSave`, and unconditional delegate registrations. | All registrations present unconditionally (modulo model flags) | Yes | After removal, entity registration depends only on `model.RequiresEntityRegistration`, save registration only on `model.HasDefaultSave`, and delegate registrations are unconditional. Correct. |
| 6 | `ClassFactoryRenderer.RenderFactoryServiceRegistrar` event registration at lines 1569-1591: local event registrations are inside the runtime `if(remoteLocal == NeatooFactory.Logical \|\| NeatooFactory.Server)` block (line 1581). There is NO compile-time `FactoryMode` guard on these lines. However, in the legacy path: `FactoryGenerator.cs` line 237 has `typeInfo.FactoryMode == FactoryMode.Full` guarding `eventMethods.AppendLine(eventResult.EventRegistration)`. Plan removes the legacy guard. | Local event registrations present | Yes | The new renderer path already emits event registrations without a FactoryMode guard. The legacy path guard at line 237 is removed. Both paths will emit local event registrations. |
| 7 | `ClassFactoryRenderer.RenderFactoryClass(sb, model, mode)` at line 165: field declaration branches on `mode == FactoryMode.RemoteOnly` (line 178). RemoteOnly emits non-nullable `IMakeRemoteDelegateRequest`; else emits nullable `IMakeRemoteDelegateRequest?`. Plan removes RemoteOnly branch, keeping only the `else` block. Same pattern in `InterfaceFactoryRenderer.RenderFactoryClass` at line 93. | Nullable declaration `IMakeRemoteDelegateRequest?` | Yes | Removing the `if (mode == FactoryMode.RemoteOnly)` branch and keeping the `else` always produces the nullable declaration. |
| 8 | Plan Step 40 / Phase 8: grep verification. Implementation removes `FactoryMode` enum and `FactoryModeAttribute` from `FactoryAttributes.cs`, all generator references, all test/doc/skill references, all OrderEntry references (6 files), all 12 reference-app files. | Zero matches in active source | Yes | Plan now enumerates all affected files: generator core, OrderEntry (6 files), reference-app (12 files), docs (7), skills (1). |
| 9 | Plan leaves `AddRemoteFactoryServices.cs` untouched (Scope Table: "No" for this file). The `NeatooFactory` enum at lines 10-26 is never referenced by FactoryMode removal code. | NeatooFactory enum identical | Yes | No code changes touch this file. Confirmed NeatooFactory has no relation to FactoryMode. |
| 10 | Plan leaves `NeatooRuntime.cs` untouched (Scope Table: "No"). `NeatooRuntime.IsServerRuntime` with `[FeatureSwitchDefinition]` at lines 12-16. | Feature switch definition identical | Yes | No code changes touch this file. |
| 11 | `FactoryGenerationUnit.cs` line 15: constructor parameter `FactoryMode mode`. Line 34: `public FactoryMode Mode { get; }`. Plan removes both. `FactoryModelBuilder.cs` passes `mode: typeInfo.FactoryMode` at lines 102, 154, 282; plan removes these arguments. | No FactoryMode concept in model | Yes | Removing the parameter, property, and all three call sites eliminates FactoryMode from the pipeline model. |
| 12 | `FactoryGenerator.Types.cs` lines 1819-1824: `FactoryText.Mode` property and constructor parameter `FactoryMode mode = FactoryMode.Full`. `FactoryGenerator.cs` line 738: `new FactoryText(typeInfo.FactoryMode)`. Plan removes Mode property, constructor parameter, and the argument at line 738. | No FactoryMode concept in FactoryText | Yes | Removing the property, parameter, and call site eliminates FactoryMode from FactoryText. The `new FactoryText()` constructor call would need no arguments (or could remain parameterless since `mode` had a default). |
| 13 | `FactoryGenerator.Types.cs` line 226: `this.FactoryMode = GetFactoryMode(semanticModel)`. Line 294: `public FactoryMode FactoryMode { get; }`. `FactoryGenerator.cs` lines 957-971: `GetFactoryMode()` method. Plan removes all three. | No FactoryMode detection in TypeInfo | Yes | Removing the property, assignment, and detection method eliminates FactoryMode from TypeInfo. |
| 14 | Existing tests in `RemoteFactory.UnitTests`, `RemoteFactory.IntegrationTests`, and `Design.Tests` all use Full mode (the default). Removing the FactoryMode concept means Full mode is the only path, so no test behavior changes. `RemoteOnlyTests` is deleted entirely. | 100% pass rate | Yes | Since Full mode is the only path and all remaining tests already use Full mode, no behavioral change occurs. |
| 15 | Plan Phase 5, Step 15: `rm -rf src/Tests/RemoteOnlyTests/`. Directory confirmed to exist. | Directory deleted | Yes | Straightforward directory deletion. |
| 16 | Plan Phase 5, Step 16: Remove RemoteOnlyTests project entries from `src/Neatoo.RemoteFactory.sln`. Solution file confirmed to contain 4 RemoteOnlyTests entries (solution folder + 3 projects). | Zero matches | Yes | All 4 entries must be removed from the solution file. |
| 17 | Plan Phase 7, Step 21: Update `docs/factory-modes.md` to remove Compile-Time Modes section. File confirmed to exist with 12 FactoryMode/RemoteOnly references. | No compile-time modes section | Yes | Content removal from markdown. |
| 18 | Plan Phase 7, Step 22: Update `docs/trimming.md` to remove "Trimming vs RemoteOnly" section. File confirmed to exist with 8 references. | No RemoteOnly comparison | Yes | Content removal from markdown. |
| 19 | Plan Phase 6, Step 18: Delete `src/Design/Design.Client.Blazor/AssemblyAttributes.cs`. File confirmed to exist with `[assembly: FactoryMode(FactoryMode.RemoteOnly)]`. | No `[assembly: FactoryMode(...)]` | Yes | File deletion. Plan also needs to verify the Design.Client.Blazor project still builds without this file (it should -- the file is not referenced by any .cs code). |
| 20 | Plan Phase 6, Step 19: Update `src/Design/CLAUDE-DESIGN.md` at line 670 to remove `AssemblyAttributes.cs` reference. Line 670 confirmed: `Design.Client.Blazor/AssemblyAttributes.cs \| Assembly-level [FactoryMode] configuration`. | Updated or removed reference | Yes | Remove the table row at line 670. |
| 21 | Plan Phase 5, Step 17: Delete `src/Examples/OrderEntry/OrderEntry.Domain.Client/` directory. Directory confirmed to exist. | Directory deleted | Yes (UNBLOCKED) | **Concern 8 resolved**: Plan now creates a new `OrderEntry.Domain.csproj` (Step 19) before this deletion matters. Domain.Client is safely deletable. |
| 22 | Plan Phase 5, Step 18: Delete `src/Examples/OrderEntry/OrderEntry.Domain.Server/` directory. Directory confirmed to exist. | Directory deleted | Yes (UNBLOCKED) | **Concern 8 resolved**: Same. Domain.Server is safely deletable after new Domain.csproj is created. |
| 23 | Plan Phase 5, Step 19: Create `src/Examples/OrderEntry/OrderEntry.Domain/OrderEntry.Domain.csproj` with references to Generator (analyzer), RemoteFactory, and OrderEntry.Ef. Steps 20-22: Remove `#if CLIENT`/`#if !CLIENT` guards from domain files. | New project exists; domain files compile; no conditional compilation guards | Yes (NEW) | **Concern 8 resolved via Option (a)**: New csproj replaces the Client+Server split. EF reference is required because domain files have unconditional `using OrderEntry.Ef;` after guard removal. `[Service]` method-injection ensures EF is server-only at runtime; IL Trimming removes it at publish time. |
| 24 | Plan Phase 5, Step 24: Update `OrderEntry.BlazorClient.csproj` ProjectReference from `OrderEntry.Domain.Client` to `OrderEntry.Domain`. | ProjectReference to `OrderEntry.Domain` | Yes (UNBLOCKED) | **Concern 8 resolved**: `OrderEntry.Domain.csproj` now exists (created in Step 19). ProjectReference is valid. |
| 25 | Plan Phase 5, Step 23: Remove `OrderEntry.Domain.Client` and `OrderEntry.Domain.Server` entries from solution file. Add `OrderEntry.Domain` entry. | Old entries removed; new entry added | Yes (UNBLOCKED) | **Concern 8 resolved**: Solution file operations are straightforward now that Domain.csproj exists. |
| 26 | Plan Phase 5, Steps 20-22: Domain files `Order.cs`, `OrderLine.cs`, `OrderLineList.cs` have no `#if CLIENT`/`#if !CLIENT` guards after edit. | Zero conditional compilation guards; all methods are real implementations | Yes (NEW) | `Order.cs`: throw-only CLIENT placeholders deleted, server implementations kept. `OrderLine.cs`/`OrderLineList.cs`: Fetch methods unconditionally present. |

### Concerns

**Concern 1 (BLOCKING): OrderEntry example project entirely missing from the plan.**
RESOLVED (Round 1): Plan updated to include OrderEntry example in Scope Table, Implementation Steps, and Acceptance Criteria.

**Concern 2 (BLOCKING): Reference-app files drastically undercounted.**
RESOLVED (Round 1): Plan updated from 4 to 12 reference-app files. All 12 files enumerated in the Design section under "Reference App Changes".

**Concern 3 (Non-blocking): Skill file count overstated.**
RESOLVED (Round 1): Plan corrected. Only `skills/RemoteFactory/references/setup.md` has FactoryMode/RemoteOnly content.

**Concern 4 (Non-blocking): InterfaceFactoryRenderer.RenderMethod has a subtly different guard than the class factory.**
ACKNOWLEDGED (Round 1): Added to Risks section (item 7). No special handling needed.

**Concern 5 (Non-blocking): OrderEntry.Domain.Client project may need architectural decision.**
SUPERSEDED BY CONCERN 8 (Round 2): The architect's proposed collapse approach has a structural flaw (see Concern 8).

**Concern 6 (Non-blocking): Legacy `FactoryText` constructor default parameter.**
RESOLVED (Round 1): Plan's Risks section (item 4) now explicitly states the constructor becomes parameterless.

**Concern 7 (Non-blocking): `InterfaceFactoryRenderer.RenderFactoryServiceRegistrar` delegate registration guard.**
ACKNOWLEDGED (Round 1): No plan changes needed.

**Concern 8 (BLOCKING -- NEW in Round 2): OrderEntry collapse approach is structurally unsound.**
RESOLVED (Round 3 -- Architect): Option (a) selected. Plan updated to create a new `OrderEntry.Domain.csproj`, remove `#if CLIENT`/`#if !CLIENT` guards from domain files, and add `OrderEntry.Ef` as a project reference. The `[Service]` method-injection pattern ensures EF types are server-only at runtime; IL Trimming removes them from published client bundles. See updated "OrderEntry Example Changes" section, Rules 21-26, Scenarios 13-18, and Implementation Steps 15-29 for full details.

Original concern details: `OrderEntry.Domain` was a bare directory of `.cs` files (no `.csproj`). Both `Domain.Client` and `Domain.Server` linked to these files via `<Compile Include>`. Three domain files used `#if CLIENT`/`#if !CLIENT` guards. The `#if !CLIENT` sections referenced `OrderEntry.Ef` types. Resolution: create `OrderEntry.Domain.csproj` with EF reference, remove all conditional compilation guards, delete throw-only placeholder methods.

---

## Implementation Contract

**Created:** 2026-03-07
**Approved by:** developer agent (Round 3 review)

### Verification Acceptance Criteria

- [ ] `dotnet build src/Neatoo.RemoteFactory.sln` -- zero errors
- [ ] `dotnet test src/Neatoo.RemoteFactory.sln` -- zero failures
- [ ] Grep for `FactoryMode|FactoryModeOption` in active `.cs` source returns zero matches (excluding `FactoryModes/` directory names which refer to runtime modes, and excluding `docs/todos/completed/`, `docs/plans/completed/`)
- [ ] `NeatooFactory` enum in `AddRemoteFactoryServices.cs` is byte-identical to pre-implementation state
- [ ] `NeatooRuntime.IsServerRuntime` in `NeatooRuntime.cs` is byte-identical to pre-implementation state

### Test Scenario Mapping

| Scenario # | Test Method | Notes |
|------------|-------------|-------|
| 1-6 | Verified by existing tests (Design.Tests + IntegrationTests + UnitTests) | Generated code correctness validated by existing test suite -- these tests exercise Full mode, which is the only remaining path |
| 7 | Manual grep verification | `grep -r "FactoryMode" src/ --include="*.cs"` excluding completed docs/plans |
| 8 | Visual inspection of `AddRemoteFactoryServices.cs` | NeatooFactory enum unchanged (Server, Remote, Logical) |
| 9 | `dotnet test` output | All tests pass with zero failures |
| 10 | Visual inspection of solution file | No RemoteOnlyTests references |
| 11 | Visual inspection of Design.Client.Blazor | No AssemblyAttributes.cs file |
| 12 | Visual inspection of factory-modes.md | No compile-time modes section |
| 13 | Directory existence check | `OrderEntry.Domain.Client/` does not exist |
| 14 | Directory existence check | `OrderEntry.Domain.Server/` does not exist |
| 15 | File existence + content check | `OrderEntry.Domain/OrderEntry.Domain.csproj` exists with `<ProjectReference>` to Generator (analyzer), RemoteFactory, and OrderEntry.Ef |
| 16 | File content grep | `Order.cs`, `OrderLine.cs`, `OrderLineList.cs` have zero `#if CLIENT` / `#if !CLIENT` / `#else` / `#endif` guards; no throw-only placeholder methods |
| 17 | File content check | `OrderEntry.BlazorClient.csproj` has `<ProjectReference Include="..\OrderEntry.Domain\OrderEntry.Domain.csproj" />` |
| 18 | Solution file inspection | No `OrderEntry.Domain.Client` or `OrderEntry.Domain.Server` entries; has `OrderEntry.Domain` entry |

### In Scope

**Phase 1-4 (Generator Core):**
- `src/RemoteFactory/FactoryAttributes.cs` -- delete `FactoryMode` enum (lines 148-162) and `FactoryModeAttribute` class (lines 164-200)
- `src/Generator/Model/FactoryGenerationUnit.cs` -- remove `Mode` property and `FactoryMode mode` constructor parameter
- `src/Generator/Builder/FactoryModelBuilder.cs` -- remove `mode: typeInfo.FactoryMode` from 3 call sites (lines 102, 154, 282)
- `src/Generator/FactoryGenerator.cs` -- delete `GetFactoryMode()` method (lines 957-971); remove FactoryMode guards at lines 212, 237, 738, 784-800
- `src/Generator/FactoryGenerator.Types.cs` -- remove `TypeInfo.FactoryMode` property (line 294) and assignment (line 226); remove `FactoryText.Mode` property and constructor parameter (lines 1819-1824); remove `classText.Mode` conditionals at lines 1033, 1041, 1053, 1330
- `src/Generator/Renderer/ClassFactoryRenderer.cs` -- remove all `FactoryMode mode` parameters and `mode ==` conditionals (8 methods affected)
- `src/Generator/Renderer/InterfaceFactoryRenderer.cs` -- remove all `FactoryMode mode` parameters and `mode ==` conditionals (4 methods affected)
- `src/Generator/Renderer/StaticFactoryRenderer.cs` -- remove `FactoryMode mode` parameter and `mode ==` conditional (1 method affected)

**Phase 5 (RemoteOnlyTests + OrderEntry):**
- `src/Tests/RemoteOnlyTests/` -- delete entire directory tree
- `src/Examples/OrderEntry/OrderEntry.Domain.Client/` -- delete entire directory
- `src/Examples/OrderEntry/OrderEntry.Domain.Server/` -- delete entire directory
- `src/Examples/OrderEntry/OrderEntry.Domain/OrderEntry.Domain.csproj` -- create new (see plan Design section for exact contents)
- `src/Examples/OrderEntry/OrderEntry.Domain/Order.cs` -- remove `#if CLIENT` block (lines 5-8, 45-79), `#else` (line 80), `#endif` (line 209); make usings unconditional; update comments
- `src/Examples/OrderEntry/OrderEntry.Domain/OrderLine.cs` -- remove `#if !CLIENT` / `#endif` guards (lines 5-7, 49, 62); make usings unconditional
- `src/Examples/OrderEntry/OrderEntry.Domain/OrderLineList.cs` -- remove `#if !CLIENT` / `#endif` guards (lines 5-7, 62, 79); make usings unconditional
- `src/Examples/OrderEntry/OrderEntry.BlazorClient/OrderEntry.BlazorClient.csproj` -- update ProjectReference to `OrderEntry.Domain`
- `src/Examples/OrderEntry/OrderEntry.BlazorClient/Program.cs` -- update comment
- `src/Examples/OrderEntry/OrderEntry.BlazorClient/Pages/Home.razor` -- rewrite architecture description
- `src/Examples/OrderEntry/OrderEntry.Server/OrderEntry.Server.csproj` -- update ProjectReference to `OrderEntry.Domain`; remove `OrderEntry.Ef` reference (now transitive); update comment about transitive conflicts
- `src/Examples/OrderEntry/OrderEntry.Server/Program.cs` -- update comment
- `src/Neatoo.RemoteFactory.sln` -- remove RemoteOnlyTests (4 entries); remove OrderEntry.Domain.Client and OrderEntry.Domain.Server (2 entries); add OrderEntry.Domain (1 entry)

**Phase 6 (Design Projects):**
- `src/Design/Design.Client.Blazor/AssemblyAttributes.cs` -- delete
- `src/Design/CLAUDE-DESIGN.md` -- remove line 670 table row

**Phase 7 (Documentation + Skills + Reference App):**
- `docs/factory-modes.md` -- remove Compile-Time Modes section
- `docs/trimming.md` -- remove "Trimming vs RemoteOnly" comparison
- `docs/attributes-reference.md` -- remove `[assembly: FactoryMode]` entries
- `docs/decision-guide.md` -- remove RemoteOnly decision sections
- `docs/service-injection.md` -- remove RemoteOnly mention
- `docs/events.md` -- remove RemoteOnly mention
- `README.md` -- remove FactoryMode/RemoteOnly references
- `skills/RemoteFactory/references/setup.md` -- remove "[assembly: FactoryMode] for Client Assemblies" section
- 12 reference-app files (see plan Design section for per-file dispositions)
- Run mdsnippets if `modes-remoteonly-example` snippet region was affected

### Out of Scope

- `NeatooFactory` enum (Server, Remote, Logical) in `src/RemoteFactory/AddRemoteFactoryServices.cs` -- must NOT be modified
- `NeatooRuntime.IsServerRuntime` in `src/RemoteFactory/NeatooRuntime.cs` -- must NOT be modified
- Historical files in `docs/todos/completed/` and `docs/plans/completed/` -- leave RemoteOnly references as-is (historical records)
- `FactoryModes/` directory names in reference-app (these refer to RUNTIME modes, not compile-time FactoryMode) -- directories stay, only compile-time RemoteOnly content within files is removed
- Release notes creation -- separate task after implementation
- `src/Examples/Person/` -- no FactoryMode references (verified by grep)

### Verification Gates

| Gate | Trigger | Criteria |
|------|---------|----------|
| Gate 1 | After Phase 4 (generator changes) | `dotnet build src/Neatoo.RemoteFactory.sln` zero errors; `dotnet test src/Neatoo.RemoteFactory.sln` zero failures |
| Gate 2 | After Phase 5 (RemoteOnlyTests + OrderEntry) | Build + test pass; OrderEntry.Domain compiles; solution file has correct entries |
| Gate 3 | After Phase 6 (Design projects) | Build + test pass; Design.Tests pass |
| Gate 4 | After Phase 8 (final) | Build + test pass; grep verification zero matches; all acceptance criteria checked |

### Stop Conditions

If any occur, STOP and report immediately:
- Out-of-scope test failure (any test not directly related to FactoryMode removal starts failing)
- Unexpected FactoryMode reference in code not identified in this plan
- `NeatooFactory` enum or `NeatooRuntime.IsServerRuntime` accidentally modified
- OrderEntry domain files fail to compile after `#if CLIENT` guard removal (unexpected EF dependency issue)
- Generated code differs functionally from pre-removal Full mode output (not just structurally -- the generated code should be identical to what was previously generated with FactoryMode.Full)

---

## Implementation Progress

**Started:** 2026-03-07
**Developer:** remotefactory-developer (Claude Code)

**[Milestone 1]:** Generator Core + Renderers
- [x] FactoryAttributes.cs cleaned -- removed `FactoryMode` enum (lines 148-162) and `FactoryModeAttribute` class (lines 164-200)
- [x] FactoryGenerationUnit.cs cleaned -- removed `Mode` property and constructor parameter
- [x] FactoryModelBuilder.cs cleaned -- removed `mode:` arguments from all three Build methods
- [x] TypeInfo + FactoryText cleaned -- removed `FactoryMode` property from TypeInfo, `Mode` from FactoryText
- [x] GetFactoryMode() removed -- deleted method from FactoryGenerator.cs (lines 957-971)
- [x] ClassFactoryRenderer simplified -- removed `mode` parameter and all FactoryMode conditionals
- [x] InterfaceFactoryRenderer simplified -- removed `mode` parameter and all FactoryMode conditionals
- [x] StaticFactoryRenderer simplified -- removed `mode` parameter and all FactoryMode conditionals
- [x] Legacy code path simplified -- removed all FactoryMode guards in FactoryGenerator.cs and FactoryGenerator.Types.cs
- [x] **Verification**: build succeeded (0 errors), all tests passed (478 unit + 476 integration per TFM)

**[Milestone 2]:** RemoteOnlyTests + OrderEntry + Design Projects
- [x] RemoteOnlyTests directory deleted
- [x] OrderEntry.Domain.Client directory deleted
- [x] OrderEntry.Domain.Server directory deleted
- [x] OrderEntry.Domain.csproj created (new project replacing Client+Server split)
- [x] OrderEntry domain files: `#if CLIENT`/`#if !CLIENT` guards removed, `using` directives unconditional
- [x] OrderEntry.BlazorClient references and content updated
- [x] OrderEntry.Server references and content updated
- [x] Solution file cleaned (RemoteOnlyTests + Domain.Client/Server removed, Domain added)
- [x] Design.Client.Blazor/AssemblyAttributes.cs deleted
- [x] CLAUDE-DESIGN.md updated
- [x] **Verification**: build succeeded (0 errors), all tests passed (478 unit + 476 integration + 29 design per TFM)

**[Milestone 3]:** Documentation + Skills + Reference App
- [x] All 7 doc files updated (factory-modes.md, trimming.md, attributes-reference.md, decision-guide.md, service-injection.md, events.md, README.md)
- [x] `skills/RemoteFactory/references/setup.md` updated -- removed "[assembly: FactoryMode] for Client Assemblies" section
- [x] Reference app files updated: AssemblyAttributes.cs, AssemblyAttributeSamples.cs, FactoryModeAttributes.cs, FactoryModesSamples.cs, GeneratedCodeIllustrations.cs, ReadmeSamples.cs (both reference-app and docs/samples), TestingSamples.cs, CompleteSetupExamples.cs
- [x] Reference app files verified no changes needed: LogicalModeTestingSample.cs, FactoryModeConfigurationSamples.cs, FullModeServerExample.cs, ServerModeConfigurationSample.cs
- [x] mdsnippets not installed; snippet regions removed from source and markdown manually; remaining snippet line number references are cosmetic only
- [x] **Verification**: build succeeded (0 errors), all tests passed (478 unit + 476 integration + 29 design + 47 reference-app per TFM), grep verification zero matches for compile-time FactoryMode/FactoryModeOption/RemoteOnly in active .cs source

---

## Completion Evidence

**Reported:** 2026-03-08 (All phases complete)

### All Milestones Complete (Phases 1-8)

- **Tests Passing (Final):**
  - `dotnet test src/Neatoo.RemoteFactory.sln` -- 0 failures
    - RemoteFactory.UnitTests (net9.0): 478 passed, 0 failed
    - RemoteFactory.UnitTests (net10.0): 478 passed, 0 failed
    - RemoteFactory.IntegrationTests (net9.0): 476 passed, 3 skipped (pre-existing performance test skips), 0 failed
    - RemoteFactory.IntegrationTests (net10.0): 476 passed, 3 skipped, 0 failed
  - `dotnet test src/Design/Design.sln` -- 0 failures
    - Design.Tests (net9.0): 29 passed, 0 failed
    - Design.Tests (net10.0): 29 passed, 0 failed
  - `dotnet test src/docs/reference-app/EmployeeManagement.sln` -- 0 failures
    - EmployeeManagement.Tests (net9.0): 47 passed, 0 failed
    - EmployeeManagement.Tests (net10.0): 47 passed, 0 failed
- **Build Status:** `dotnet build src/Neatoo.RemoteFactory.sln` -- 0 errors (only pre-existing SQLite WASM warning from OrderEntry.BlazorClient)
- **Grep Verification:**
  - `FactoryMode` enum, `FactoryModeAttribute` class, `GetFactoryMode()` method: zero matches in all `.cs` files
  - `FactoryModeOption`: zero matches in all `.cs` files
  - Remaining `FactoryMode` string occurrences in `.cs` source are all either: (a) `FactoryModel` class names (false positives), (b) `FactoryModeAttributes.cs` filename references in comments (runtime mode file), or (c) `FactoryModeConfigurationSamples` class name (runtime mode class)
  - `RemoteOnly` string: only occurrence is in test method name `ShowcaseRead_CreateRemoteOnlyClientFail` which tests runtime Remote mode behavior (not the compile-time concept)
- **NeatooFactory enum:** Unchanged (Server, Remote, Logical) in `AddRemoteFactoryServices.cs`
- **NeatooRuntime.IsServerRuntime:** Unchanged with `[FeatureSwitchDefinition]` in `NeatooRuntime.cs`
- **All Contract Items:** Confirmed 100% complete

### Files Modified (Phases 1-6)

**Phase 1 -- Remove FactoryMode enum, attribute, and model types:**
- `src/RemoteFactory/FactoryAttributes.cs` -- removed FactoryMode enum and FactoryModeAttribute class
- `src/Generator/Model/FactoryGenerationUnit.cs` -- removed Mode property and constructor parameter
- `src/Generator/Builder/FactoryModelBuilder.cs` -- removed mode: arguments from Build methods

**Phase 2 -- Simplify renderers:**
- `src/Generator/Renderer/ClassFactoryRenderer.cs` -- removed mode parameter and all FactoryMode conditionals
- `src/Generator/Renderer/InterfaceFactoryRenderer.cs` -- removed mode parameter and all FactoryMode conditionals
- `src/Generator/Renderer/StaticFactoryRenderer.cs` -- removed mode parameter and all FactoryMode conditionals

**Phase 3 -- Legacy code path cleanup:**
- `src/Generator/FactoryGenerator.cs` -- removed GetFactoryMode(), FactoryMode guards in GenerateExecute/GenerateInterfaceFactory
- `src/Generator/FactoryGenerator.Types.cs` -- removed FactoryMode from TypeInfo/FactoryText, guards in ReadFactoryMethod/WriteFactoryMethod

**Phase 5 -- Remove RemoteOnlyTests + Collapse OrderEntry:**
- Deleted: `src/Tests/RemoteOnlyTests/` (entire directory tree)
- Deleted: `src/Examples/OrderEntry/OrderEntry.Domain.Client/` (entire directory)
- Deleted: `src/Examples/OrderEntry/OrderEntry.Domain.Server/` (entire directory)
- Created: `src/Examples/OrderEntry/OrderEntry.Domain/OrderEntry.Domain.csproj`
- Modified: `src/Examples/OrderEntry/OrderEntry.Domain/Order.cs` -- removed #if CLIENT block and guards
- Modified: `src/Examples/OrderEntry/OrderEntry.Domain/OrderLine.cs` -- removed #if !CLIENT guards
- Modified: `src/Examples/OrderEntry/OrderEntry.Domain/OrderLineList.cs` -- removed #if !CLIENT guards
- Modified: `src/Examples/OrderEntry/OrderEntry.BlazorClient/OrderEntry.BlazorClient.csproj` -- updated ProjectReference
- Modified: `src/Examples/OrderEntry/OrderEntry.Server/OrderEntry.Server.csproj` -- updated ProjectReference, removed redundant EF ref
- Modified: `src/Examples/OrderEntry/OrderEntry.BlazorClient/Program.cs` -- updated comment
- Modified: `src/Examples/OrderEntry/OrderEntry.BlazorClient/Pages/Home.razor` -- rewrote architecture description
- Modified: `src/Examples/OrderEntry/OrderEntry.Server/Program.cs` -- updated comment
- Modified: `src/Neatoo.RemoteFactory.sln` -- removed 5 project entries, added 1 new

**Phase 6 -- Update Design projects:**
- Deleted: `src/Design/Design.Client.Blazor/AssemblyAttributes.cs`
- Modified: `src/Design/CLAUDE-DESIGN.md` -- removed FactoryMode reference at line 670

---

## Documentation

**Agent:** developer (no separate documentation agent)
**Completed:** 2026-03-08

### Expected Deliverables

- [x] `docs/factory-modes.md` -- Removed Compile-Time Modes section
- [x] `docs/trimming.md` -- Removed "Trimming vs RemoteOnly" comparison
- [x] `docs/attributes-reference.md` -- Removed FactoryMode entries from Quick Lookup and Assembly-Level Attributes
- [x] `docs/decision-guide.md` -- Removed "Full vs RemoteOnly Mode?" and "IL Trimming or RemoteOnly?" sections
- [x] `docs/service-injection.md` -- Changed "RemoteOnly mode" to "Remote mode" in error message section
- [x] `docs/events.md` -- Changed "Server (Full) mode" to "Server mode" and "RemoteOnly mode" to "Remote mode"
- [x] `README.md` -- Removed FactoryMode/RemoteOnly from feature list and client assembly mode snippet; updated doc links
- [x] `skills/RemoteFactory/references/setup.md` -- Removed "[assembly: FactoryMode] for Client Assemblies" section
- [x] `src/docs/reference-app/` -- Updated 8 files, verified 4 files need no changes
- [x] `src/Design/CLAUDE-DESIGN.md` -- Updated in Phase 6 (already complete)
- [x] Skill updates: setup.md only (SKILL.md and trimming.md confirmed zero matches)
- [x] Sample updates: Reference-app files + `src/docs/samples/ReadmeSamples.cs` (extra file found during grep)

### Files Updated

**Phase 7 -- Published docs:**
- Modified: `docs/factory-modes.md` -- removed Compile-Time Modes section (lines 59-91), updated solution structure and Next Steps
- Modified: `docs/trimming.md` -- removed "Trimming vs RemoteOnly" section, updated Next Steps
- Modified: `docs/attributes-reference.md` -- removed `[assembly: FactoryMode]` from Quick Lookup table and Assembly-Level Attributes section
- Modified: `docs/decision-guide.md` -- removed "Full vs RemoteOnly Mode?" and "IL Trimming or RemoteOnly?" sections, updated Next Steps
- Modified: `docs/service-injection.md` -- changed "RemoteOnly mode" to "Remote mode"
- Modified: `docs/events.md` -- changed "Server (Full) mode" to "Server mode", "RemoteOnly mode" to "Remote mode"
- Modified: `README.md` -- removed RemoteOnly from feature list, removed client assembly mode snippet section, updated doc links

**Phase 7 -- Skills:**
- Modified: `skills/RemoteFactory/references/setup.md` -- removed "[assembly: FactoryMode] for Client Assemblies" section

**Phase 7 -- Reference app:**
- Modified: `src/docs/reference-app/EmployeeManagement.Domain/AssemblyAttributes.cs` -- removed FactoryModeOption comments
- Modified: `src/docs/reference-app/EmployeeManagement.Domain/Samples/Attributes/AssemblyAttributeSamples.cs` -- removed attributes-factorymode region
- Modified: `src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs` -- removed modes-remoteonly-config region
- Modified: `src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs` -- removed modes-remoteonly-example region
- Modified: `src/docs/reference-app/EmployeeManagement.Domain/Samples/FactoryModes/GeneratedCodeIllustrations.cs` -- removed modes-remoteonly-generated region
- Modified: `src/docs/reference-app/EmployeeManagement.Domain/Samples/ReadmeSamples.cs` -- removed readme-client-assembly-mode region
- Modified: `src/docs/samples/ReadmeSamples.cs` -- removed readme-client-assembly-mode region
- Modified: `src/docs/reference-app/EmployeeManagement.Tests/Samples/TestingSamples.cs` -- removed RemoteOnlyModeExample class
- Modified: `src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/CompleteSetupExamples.cs` -- renamed RemoteOnlyModeClientSetup to RemoteModeClientSetup
- No changes needed: LogicalModeTestingSample.cs, FactoryModeConfigurationSamples.cs, FullModeServerExample.cs, ServerModeConfigurationSample.cs (runtime mode content only)

---

## Architect Verification

**Verified:** 2026-03-08
**Verdict:** VERIFIED

**Independent build results:**
- `dotnet build src/Neatoo.RemoteFactory.sln` -- Build succeeded, 0 errors, 1 warning (pre-existing SQLite WASM warning in OrderEntry.BlazorClient)

**Independent test results:**
- RemoteFactory.UnitTests (net9.0): 478 passed, 0 failed
- RemoteFactory.UnitTests (net10.0): 478 passed, 0 failed
- RemoteFactory.IntegrationTests (net9.0): 476 passed, 3 skipped (pre-existing performance test skips), 0 failed
- RemoteFactory.IntegrationTests (net10.0): 476 passed, 3 skipped, 0 failed
- Design.Tests (net9.0): 29 passed, 0 failed
- Design.Tests (net10.0): 29 passed, 0 failed
- EmployeeManagement.Tests (net9.0): 47 passed, 0 failed
- EmployeeManagement.Tests (net10.0): 47 passed, 0 failed

**Grep verification (zero matches for compile-time FactoryMode in active .cs source):**
- `FactoryModeAttribute` in src/*.cs: zero matches (only false-positive filename references to runtime `FactoryModeAttributes.cs`)
- `FactoryModeOption` in src/*.cs: zero matches
- `GetFactoryMode` in src/*.cs: zero matches
- `RemoteOnly` in src/*.cs: only `ShowcaseRead_CreateRemoteOnlyClientFail` test method name (tests runtime Remote mode, not compile-time concept)
- `enum FactoryMode` in FactoryAttributes.cs: zero matches (enum removed)
- `FactoryMode` in Generator/ (excluding FactoryModel false positives): zero matches

**Design match verification:**
- FactoryMode enum and FactoryModeAttribute removed from FactoryAttributes.cs: CONFIRMED
- FactoryGenerationUnit.Mode property removed: CONFIRMED
- FactoryText.Mode property removed: CONFIRMED (zero FactoryMode matches in FactoryGenerator.Types.cs)
- GetFactoryMode() method removed from FactoryGenerator.cs: CONFIRMED
- All generator conditionals removed from ClassFactoryRenderer, InterfaceFactoryRenderer, StaticFactoryRenderer: CONFIRMED
- RemoteOnlyTests directory deleted: CONFIRMED
- OrderEntry.Domain.csproj created, Domain.Client and Domain.Server deleted: CONFIRMED
- #if CLIENT guards removed from Order.cs, OrderLine.cs, OrderLineList.cs: CONFIRMED (zero matches)
- Design.Client.Blazor/AssemblyAttributes.cs deleted: CONFIRMED
- CLAUDE-DESIGN.md updated (zero FactoryMode/RemoteOnly matches): CONFIRMED
- Solution file cleaned (RemoteOnlyTests removed, Domain.Client/Server removed, Domain added): CONFIRMED
- Published docs clean (factory-modes.md, trimming.md, attributes-reference.md, decision-guide.md, service-injection.md, events.md, README.md): CONFIRMED
- Skills clean (zero RemoteOnly/FactoryMode matches): CONFIRMED
- NeatooFactory enum unchanged (Server, Remote, Logical in AddRemoteFactoryServices.cs): CONFIRMED
- NeatooRuntime.IsServerRuntime unchanged (with [FeatureSwitchDefinition] in NeatooRuntime.cs): CONFIRMED

**Issues found:** None

---

## Requirements Verification

**Reviewer:** business-requirements-reviewer
**Verified:** 2026-03-08
**Verdict:** REQUIREMENTS SATISFIED

### Requirements Compliance

| Requirement | Status | Evidence |
|-------------|--------|----------|
| Req 1: NeatooFactory enum untouched | Satisfied | Read `src/RemoteFactory/AddRemoteFactoryServices.cs` lines 10-26: `NeatooFactory` enum has Server, Remote, Logical -- identical to pre-implementation. Zero modifications to this file. |
| Req 2: IL Trimming mechanism unchanged | Satisfied | Read `src/RemoteFactory/NeatooRuntime.cs` lines 10-17: `NeatooRuntime.IsServerRuntime` with `[FeatureSwitchDefinition("Neatoo.RemoteFactory.IsServerRuntime")]` -- identical to pre-implementation. Zero modifications to this file. |
| Req 3: Full code path remains (generator output) | Satisfied | Traced through `ClassFactoryRenderer.RenderConstructors()` (line 250): both local ctor (`IServiceProvider`) and remote ctor (`IServiceProvider, IMakeRemoteDelegateRequest`) always generated, no FactoryMode conditional. `IMakeRemoteDelegateRequest?` nullable at line 176 (Full mode pattern). `RenderFactoryServiceRegistrar()` (line 1448): entity registration gated only by `model.RequiresEntityRegistration`, delegate registrations unconditional, event registrations gated only by runtime `NeatooFactory` enum. Grep for `FactoryMode` in Generator/ returned zero matches (only `FactoryModel` class name false positives). Grep for `FactoryMode` in `FactoryGenerator.Types.cs` returned zero matches. `FactoryGenerationUnit.cs` has no `Mode` property. |
| Req 4: Design Source of Truth updated | Satisfied | `src/Design/Design.Client.Blazor/AssemblyAttributes.cs` deleted (file does not exist). `src/Design/CLAUDE-DESIGN.md` has zero `FactoryMode` or `RemoteOnly` matches (verified by grep). Design Files table at line 655 no longer references AssemblyAttributes.cs. All three factory patterns, all 9 anti-patterns, all critical rules, and the design debt table are intact and unmodified. |
| Req 10: Breaking change -- FactoryMode removed from public API | Satisfied | Read `src/RemoteFactory/FactoryAttributes.cs`: file ends at line 144 with `FactoryHintNameLengthAttribute`. No `FactoryMode` enum or `FactoryModeAttribute` class exists. The `FactoryMode` and `FactoryModeAttribute` types have been completely removed from the public API surface. Release notes creation was explicitly documented as out-of-scope for this plan (separate task). |

### Additional Verifications

| Check | Status | Evidence |
|-------|--------|----------|
| FactoryMode references in active .cs source | Clean | Grep for `FactoryMode` in `src/RemoteFactory/` returned zero matches. Grep for `RemoteOnly` in `src/**/*.cs` returned one match: test method name `ShowcaseRead_CreateRemoteOnlyClientFail` (tests runtime Remote mode behavior, not compile-time concept). |
| RemoteOnlyTests removed | Satisfied | Glob for `src/Tests/RemoteOnlyTests/**` returned no files. |
| OrderEntry Domain.Client/Server deleted | Satisfied | Glob for `src/Examples/OrderEntry/OrderEntry.Domain.Client/**` and `OrderEntry.Domain.Server/**` both returned no files. |
| OrderEntry.Domain.csproj created correctly | Satisfied | Read `src/Examples/OrderEntry/OrderEntry.Domain/OrderEntry.Domain.csproj`: has ProjectReferences to Generator (as Analyzer), RemoteFactory, and OrderEntry.Ef. |
| OrderEntry domain files -- no conditional compilation | Satisfied | Grep for `#if CLIENT\|#if !CLIENT\|#else\|#endif` in OrderEntry.Domain returned zero matches. Read `Order.cs`, `OrderLine.cs`, `OrderLineList.cs`: all have unconditional `using OrderEntry.Ef;`, no throw-only placeholder methods, all methods are real implementations. |
| OrderEntry.BlazorClient references Domain directly | Satisfied | Read `OrderEntry.BlazorClient.csproj` line 18: `<ProjectReference Include="..\OrderEntry.Domain\OrderEntry.Domain.csproj" />` |
| OrderEntry.Server references Domain (not Domain.Server) | Satisfied | Read `OrderEntry.Server.csproj` line 15: `<ProjectReference Include="..\OrderEntry.Domain\OrderEntry.Domain.csproj" />`. No separate OrderEntry.Ef reference (now transitive through Domain). |
| Solution file clean | Satisfied | Grep for `RemoteOnly\|OrderEntry.Domain.Client\|OrderEntry.Domain.Server` in solution file returned zero matches. Grep for `OrderEntry.Domain` found exactly one entry pointing to the new `OrderEntry.Domain.csproj`. |
| Published docs clean | Satisfied | Grep for `\bFactoryMode\b` in `docs/factory-modes.md`, `docs/trimming.md`, `docs/attributes-reference.md`, `docs/decision-guide.md`, `docs/service-injection.md`, `docs/events.md`, `README.md` all returned zero matches. `docs/factory-modes.md` describes only the three runtime modes (Server, Remote, Logical). |
| Skills clean | Satisfied | Grep for `RemoteOnly\|FactoryModeAttribute\|FactoryModeOption` in `skills/` returned zero matches. |
| Reference-app clean | Satisfied | Grep for `\bFactoryMode\b` in `src/docs/reference-app/**/*.cs` returned zero matches. The `FactoryModeAttributes.cs` filename contains "FactoryMode" as substring but its content is exclusively runtime mode configurations (Server, Remote, Logical). |
| Design.Tests pass (29 tests per TFM) | Satisfied | Architect independently verified: Design.Tests net9.0 29 passed, net10.0 29 passed. |
| Key behavioral contracts preserved | Satisfied | OrderEntry example follows Critical Rule 1: `[Remote]` only on `Order` (aggregate root); `OrderLine` and `OrderLineList` have no `[Remote]` (child entities). Anti-Pattern 6 (missing `partial`): OrderEntry domain classes lack `partial` keyword but this is a pre-existing condition in the OrderEntry example, not introduced by this change. |

### Unintended Side Effects

None found. The implementation is a clean removal that:
1. Does not modify `NeatooFactory` (runtime enum) or `NeatooRuntime.IsServerRuntime` (IL trimming mechanism)
2. Does not alter any generated code behavior -- the Full mode code path is the only path that remains, and all existing tests exercise Full mode
3. Does not modify any Design project files beyond deleting the `AssemblyAttributes.cs` and updating the `CLAUDE-DESIGN.md` reference table
4. Does not violate any documented pattern, anti-pattern, or design debt entry
5. The OrderEntry restructuring (new Domain.csproj with EF reference) follows the documented pattern: `[Service]` method-injection ensures EF types are server-only at runtime; IL Trimming removes them from published client bundles (same mechanism validated by `RemoteFactory.TrimmingTests`)

### Issues Found

None.
