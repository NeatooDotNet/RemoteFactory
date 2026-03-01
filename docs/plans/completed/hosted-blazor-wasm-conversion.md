# Convert All Applications to Hosted Blazor WebAssembly

**Date:** 2026-02-28
**Related Todo:** [Convert All Applications to Hosted Blazor WebAssembly](../todos/completed/convert-to-hosted-blazor-wasm.md)
**Status:** Complete
**Last Updated:** 2026-02-28

---

## Overview

Convert all four Blazor WebAssembly application groups from standalone to hosted WASM. In hosted mode, the ASP.NET Core server project hosts the Blazor WASM client -- a single `dotnet run` starts everything, the client uses relative URLs (no hardcoded BaseAddress), and CORS configuration is unnecessary because client and server share the same origin.

This also requires updating user-facing documentation and skill content that currently instructs users to configure a BaseAddress URL and CORS.

---

## Approach

For each of the four project groups (Design, Person, OrderEntry, Reference App), apply the same pattern:

1. **Server `.csproj`**: Add a `<ProjectReference>` to the client project with hosted-WASM-specific attributes
2. **Server `Program.cs`**: Add `app.UseBlazorFrameworkFiles()` and `app.MapFallbackToFile("index.html")`, remove CORS
3. **Client `Program.cs`**: Replace hardcoded BaseAddress with `builder.HostEnvironment.BaseAddress` for the keyed HttpClient
4. **Server `launchSettings.json`**: Update to launch browser (the server now serves the client)
5. **Client `launchSettings.json`**: Remove (or leave as-is -- the client is no longer run independently)
6. **Remove `DevServer` package references** from client `.csproj` files (the server hosts the client instead)
7. **Add `Microsoft.AspNetCore.Components.WebAssembly.Server` package** to `Directory.Packages.props` and server `.csproj` files

After the code changes, update documentation and skill files to reflect the new pattern.

---

## Design

### Hosted Blazor WASM Pattern (.NET 8/9/10)

The standard hosted WASM pattern requires these pieces:

**Server `.csproj`:**
```xml
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" />
<ProjectReference Include="..\Client\Client.csproj" />
```

Note: Unlike .NET 6/7 hosted WASM templates, .NET 8+ does NOT require special `ReferenceOutputAssembly="false"` or `SetTargetFramework` attributes on the client ProjectReference. The server just needs a normal project reference to the client so the build output includes the client's `wwwroot/_framework` files.

**Server `Program.cs`:**
```csharp
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();
// ... other middleware ...
app.MapFallbackToFile("index.html");
```

**Client `Program.cs`:**
```csharp
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
```

### Multi-Targeting Concern

All client projects multi-target `net8.0;net9.0;net10.0` (inherited from `Directory.Build.props`). The server projects also multi-target. When the server references the client, the build system must resolve the correct TFM. Since both projects share the same `TargetFrameworks`, this should resolve naturally -- the server builds each TFM and references the matching client TFM output.

However, `Microsoft.AspNetCore.Components.WebAssembly.Server` is a framework-provided package in .NET 8+, meaning it comes from the ASP.NET Core shared framework. For Web SDK projects targeting `net8.0;net9.0;net10.0`, this package needs framework-conditional versioning similar to how the client projects handle `Microsoft.AspNetCore.Components.WebAssembly`.

### DevServer Package Removal

Client projects currently reference `Microsoft.AspNetCore.Components.WebAssembly.DevServer`, which provides a development server for standalone WASM. In hosted mode, the ASP.NET Core server serves the client, so this package is no longer needed. Removing it avoids confusion and slightly reduces the dependency graph.

---

## Implementation Steps

### Phase 1: Shared Package Configuration

1. Add `Microsoft.AspNetCore.Components.WebAssembly.Server` to `src/Directory.Packages.props` with framework-conditional versions:
   - Version `8.0.11` for net8.0
   - Version `9.0.5` for net9.0
   - Version `10.0.0-preview.7.25380.108` for net10.0

### Phase 2: Design Project Conversion (Source of Truth)

The Design project is the source of truth for RemoteFactory patterns, so it must be converted first and verified thoroughly.

**Files to modify:**

| File | Change |
|------|--------|
| `src/Design/Design.Server/Design.Server.csproj` | Add `Microsoft.AspNetCore.Components.WebAssembly.Server` package reference (framework-conditional). Add `<ProjectReference>` to `Design.Client.Blazor`. |
| `src/Design/Design.Server/Program.cs` | Add `app.UseBlazorFrameworkFiles()`, `app.UseStaticFiles()`, `app.MapFallbackToFile("index.html")`. Remove `builder.Services.AddCors()` and `app.UseCors(...)`. Update the DESIGN SOURCE OF TRUTH comments to reflect hosted WASM pattern. |
| `src/Design/Design.Client.Blazor/Program.cs` | Change keyed HttpClient BaseAddress from `new Uri("http://localhost:5000/")` to `new Uri(builder.HostEnvironment.BaseAddress)`. Remove the comment about "COMMON MISTAKE: Not setting the BaseAddress" since it no longer applies. Update DESIGN SOURCE OF TRUTH comments. |
| `src/Design/Design.Client.Blazor/Design.Client.Blazor.csproj` | Remove `Microsoft.AspNetCore.Components.WebAssembly.DevServer` package references (both framework-conditional blocks). |
| `src/Design/Design.Server/Properties/launchSettings.json` | Set `"launchBrowser": true` and consider updating the URL to match the port. |
| `src/Design/README.md` | Update "Running the Projects" section -- replace "Run Server" + "Run Blazor Client" with a single "Run the Application" instruction pointing to Design.Server. |
| `src/Design/CLAUDE-DESIGN.md` | Update the "Client Setup (Blazor WASM)" section to show `builder.HostEnvironment.BaseAddress` instead of hardcoded URL. Update the "Running the Projects" section. |

**Verification:** Build `Design.sln`, run `dotnet test` on Design.Tests, manually verify Design.Server serves the Blazor client.

### Phase 3: Person Example Conversion

**Files to modify:**

| File | Change |
|------|--------|
| `src/Examples/Person/Person.Server/Person.Server.csproj` | Add `Microsoft.AspNetCore.Components.WebAssembly.Server` package reference (framework-conditional). Add `<ProjectReference>` to `Person.Client`. |
| `src/Examples/Person/Person.Server/Program.cs` | Add `app.UseBlazorFrameworkFiles()`, `app.UseStaticFiles()`, `app.MapFallbackToFile("index.html")`. Remove `builder.Services.AddCors()` and `app.UseCors(...)`. |
| `src/Examples/Person/Person.Client/Program.cs` | Change keyed HttpClient BaseAddress from `new Uri("http://localhost:5183/")` to `new Uri(builder.HostEnvironment.BaseAddress)`. |
| `src/Examples/Person/Person.Client/Person.Client.csproj` | Remove `Microsoft.AspNetCore.Components.WebAssembly.DevServer` package references. |
| `src/Examples/Person/Person.Server/Properties/launchSettings.json` | Set `"launchBrowser": true`. Clean up IIS Express profile if desired. |

**Verification:** Build via main solution, verify Person.Server serves the Blazor client.

### Phase 4: OrderEntry Example Conversion

**Files to modify:**

| File | Change |
|------|--------|
| `src/Examples/OrderEntry/OrderEntry.Server/OrderEntry.Server.csproj` | Add `Microsoft.AspNetCore.Components.WebAssembly.Server` package reference (framework-conditional). Add `<ProjectReference>` to `OrderEntry.BlazorClient`. |
| `src/Examples/OrderEntry/OrderEntry.Server/Program.cs` | Add `app.UseBlazorFrameworkFiles()`, `app.UseStaticFiles()`, `app.MapFallbackToFile("index.html")`. Remove `builder.Services.AddCors()` and `app.UseCors(...)`. |
| `src/Examples/OrderEntry/OrderEntry.BlazorClient/Program.cs` | Change keyed HttpClient BaseAddress from `new Uri("http://localhost:5184/")` to `new Uri(builder.HostEnvironment.BaseAddress)`. |
| `src/Examples/OrderEntry/OrderEntry.BlazorClient/OrderEntry.BlazorClient.csproj` | Remove `Microsoft.AspNetCore.Components.WebAssembly.DevServer` package references. |

**Verification:** Build via main solution, verify OrderEntry.Server serves the Blazor client.

### Phase 5: Reference App (EmployeeManagement) Conversion

This project is the source for MarkdownSnippets code extraction. Changes here propagate to docs and skill files.

**Files to modify:**

| File | Change |
|------|--------|
| `src/docs/reference-app/EmployeeManagement.Server.WebApi/EmployeeManagement.Server.WebApi.csproj` | Add `Microsoft.AspNetCore.Components.WebAssembly.Server` package reference (framework-conditional). Add `<ProjectReference>` to `EmployeeManagement.Client.Blazor`. |
| `src/docs/reference-app/EmployeeManagement.Server.WebApi/Program.cs` | Add `app.UseBlazorFrameworkFiles()`, `app.UseStaticFiles()`, `app.MapFallbackToFile("index.html")`. Remove `builder.Services.AddCors()` and `app.UseCors(...)`. The `#region getting-started-server-program` snippet may need to be updated to include the hosted WASM middleware. |
| `src/docs/reference-app/EmployeeManagement.Client.Blazor/Program.cs` | Change keyed HttpClient BaseAddress from `new Uri(serverBaseAddress)` to `new Uri(builder.HostEnvironment.BaseAddress)`. Remove the `serverBaseAddress` variable. The `#region getting-started-client-program` snippet will change. |
| `src/docs/reference-app/EmployeeManagement.Client.Blazor/EmployeeManagement.Client.Blazor.csproj` | Remove `Microsoft.AspNetCore.Components.WebAssembly.DevServer` package references. |
| `src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/CorsConfigurationSamples.cs` | This sample demonstrates CORS for standalone WASM. It should be updated to note that CORS is only needed when client and server are on different origins (non-hosted scenarios). The `#region aspnetcore-cors` will change, which propagates to docs and skill via MarkdownSnippets. |
| `src/docs/reference-app/EmployeeManagement.Client.Blazor/Samples/ClientProgramSample.cs` | Update to use `builder.HostEnvironment.BaseAddress` instead of a passed `serverBaseAddress`. |
| `src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs` | Update the `modes-remote-config` region to show `builder.HostEnvironment.BaseAddress` instead of a hardcoded `serverUrl`. |
| `src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs` | Update the `modes-remoteonly-example` region to show `builder.HostEnvironment.BaseAddress`. |
| `src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/CompleteSetupExamples.cs` | Update `RemoteOnlyModeClientSetup.Configure()` to use `builder.HostEnvironment.BaseAddress` instead of a `serverUrl` parameter. |

**Verification:** Build `EmployeeManagement.sln`, run its tests, verify Server.WebApi serves the Blazor client.

### Phase 6: Documentation and Skill Updates

After code changes are complete and MarkdownSnippets regions are updated, run `mdsnippets` to regenerate embedded code blocks.

**Documentation files to update:**

| File | Change |
|------|--------|
| `docs/getting-started.md` | The `getting-started-client-program` snippet will auto-update via mdsnippets. Review surrounding text: remove mention of "pointed at the server" for HttpClient. Simplify the "Project Structure" section to note hosted WASM (single startup). |
| `docs/factory-modes.md` | The `modes-remote-config` snippet will auto-update. Review surrounding text. |
| `docs/aspnetcore-integration.md` | The `aspnetcore-cors` snippet will auto-update. Rewrite the "CORS Configuration" section to note it is only needed for non-hosted deployments. |
| `docs/client-server-architecture.md` | No snippet changes, but review for any mention of "two separate projects" or BaseAddress configuration. |

**Skill files to update:**

| File | Change |
|------|--------|
| `skills/RemoteFactory/references/setup.md` | The `aspnetcore-cors` and `getting-started-client-program` snippets will auto-update via mdsnippets. Review and update the "CORS Configuration (for Blazor WASM)" section header and surrounding text. Update the hardcoded BaseAddress example at line 150. |

**CLAUDE.md updates:**

| File | Change |
|------|--------|
| `CLAUDE.md` (project root) | Update the "Client Setup (Blazor WASM)" code example to use `builder.HostEnvironment.BaseAddress`. |
| `src/Design/CLAUDE-DESIGN.md` | Update the "Client Setup (Blazor WASM)" section. |

**Process:**
1. Make all code and sample changes (Phases 1-5)
2. Run `mdsnippets` from repository root to regenerate embedded snippets in docs and skill files
3. Review auto-generated changes in docs and skill files
4. Make manual prose updates to docs and skill files as identified above

---

## Acceptance Criteria

- [ ] All four server projects host their respective Blazor WASM clients
- [ ] `dotnet build src/Neatoo.RemoteFactory.sln` succeeds
- [ ] `dotnet test src/Neatoo.RemoteFactory.sln` -- all tests pass
- [ ] `dotnet build src/Design/Design.sln` succeeds
- [ ] `dotnet test src/Design/Design.sln` -- all tests pass
- [ ] `dotnet build src/docs/reference-app/EmployeeManagement.sln` succeeds
- [ ] `dotnet test src/docs/reference-app/EmployeeManagement.sln` -- all tests pass
- [ ] Each server project, when run with `dotnet run`, serves the Blazor client at its root URL
- [ ] No hardcoded `localhost:XXXX` BaseAddress URLs remain in client Program.cs files
- [ ] No CORS configuration remains in any server Program.cs file
- [ ] No `Microsoft.AspNetCore.Components.WebAssembly.DevServer` references remain in client .csproj files
- [ ] Documentation and skill files reflect the hosted WASM pattern (no BaseAddress URL configuration guidance for hosted scenarios)
- [ ] `mdsnippets` has been run and embedded code blocks are current

---

## Dependencies

- `Microsoft.AspNetCore.Components.WebAssembly.Server` package must be available for all three target frameworks (8.0, 9.0, 10.0)
- `mdsnippets` CLI must be available for regenerating documentation snippets

---

## Risks / Considerations

### 1. Multi-Targeting with Server-Client ProjectReference

The server projects multi-target `net8.0;net9.0;net10.0`, as do the client projects. When one multi-targeting project references another, MSBuild must resolve matching TFMs. Since both sides use identical `TargetFrameworks`, this should work naturally. However, Blazor WASM projects have special build behavior (linking, tree-shaking) that could interact unexpectedly with multi-targeting. If issues arise, the fallback is to add `SetTargetFramework` attributes to the ProjectReference.

### 2. EmployeeManagement.Server.WebApi Has Explicit TargetFrameworks

The `EmployeeManagement.Server.WebApi.csproj` explicitly sets `<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>` rather than inheriting from `Directory.Build.props`. This is fine but means changes to the central props won't automatically propagate. The client project (`EmployeeManagement.Client.Blazor`) also has explicit `TargetFrameworks`. Ensure both match.

### 3. DevServer Removal May Affect Independent Client Debugging

Removing `Microsoft.AspNetCore.Components.WebAssembly.DevServer` means the client project can no longer be run independently with `dotnet run`. This is intentional -- in hosted mode, you always run the server. However, developers accustomed to the standalone pattern should be aware.

### 4. MarkdownSnippets Propagation

Several code changes in the reference app (`EmployeeManagement`) are in `#region` blocks that are extracted by MarkdownSnippets. After code changes, `mdsnippets` must be run to update the docs and skill files. If the snippet regions change name or location, the corresponding markdown files will have stale anchors.

### 5. CORS Documentation Nuance

CORS is not needed for hosted WASM (same origin), but it IS still needed for non-hosted deployments (e.g., separate client and server in production with different domains). The documentation should clarify this nuance rather than simply removing CORS guidance. The CORS sample should remain but be contextualized as "for non-hosted deployments."

### 6. Design.Server Port Mismatch

The Design.Client.Blazor currently uses `http://localhost:5000/` as BaseAddress, but Design.Server's launchSettings.json shows port `5085`. After conversion, this mismatch becomes irrelevant since the client uses `builder.HostEnvironment.BaseAddress`. However, this suggests the current standalone setup was already broken, which is further motivation for this conversion.

### 7. Person.Server Single-Target vs Multi-Target

The `Person.Server.csproj` comment says "Inherits TargetFramework (net9.0)" suggesting it may single-target despite the global props setting multi-target. Need to verify what it actually resolves to. If it single-targets net9.0, referencing the multi-targeting Person.Client client could require `SetTargetFramework` on the ProjectReference. The developer should check this during implementation.

### 8. Test Impact Assessment

The existing tests (unit tests, integration tests, Design tests) use the `ClientServerContainers` pattern which simulates client/server communication through DI containers, not HTTP. These tests should be unaffected by the hosted WASM conversion since they don't depend on actual HTTP endpoints or CORS. The conversion is purely about how the Blazor client is served and configured at the application level.

---

## Architectural Verification

**Scope Table:**

| Component | Affected? | Nature of Change |
|-----------|-----------|-----------------|
| Server .csproj files (x4) | Yes | Add package ref + client project ref |
| Server Program.cs files (x4) | Yes | Add hosted WASM middleware, remove CORS |
| Client Program.cs files (x4) | Yes | Use HostEnvironment.BaseAddress |
| Client .csproj files (x4) | Yes | Remove DevServer package |
| Directory.Packages.props | Yes | Add WebAssembly.Server package versions |
| Reference app sample files (~5) | Yes | Update BaseAddress/serverUrl patterns |
| Documentation pages (~4) | Yes | Updated prose + auto-updated snippets |
| Skill files (1) | Yes | Auto-updated snippets + manual prose |
| CLAUDE.md files (2) | Yes | Update code examples |
| Design README (1) | Yes | Update run instructions |
| Solution files | No | No changes needed |
| Generator | No | Not affected |
| RemoteFactory core library | No | Not affected |
| RemoteFactory.AspNetCore | No | Not affected |
| Unit/Integration tests | No | Use DI container simulation, not HTTP |

**Design Project Verification:**
- N/A -- this is a configuration/deployment change, not a code generation change. The Design project's factory patterns, tests, and generated code are unaffected. The changes are limited to `Program.cs` and `.csproj` files.

**Breaking Changes:** No -- this is an internal infrastructure change. The RemoteFactory API, generated code, and NuGet packages are unchanged. This only affects how example/reference applications are structured and how documentation guides users.

**Codebase Analysis:**

Files examined:
- All four server `.csproj`, `Program.cs`, and `launchSettings.json` files
- All four client `.csproj`, `Program.cs` files
- `src/Directory.Build.props` (TargetFrameworks: net8.0;net9.0;net10.0)
- `src/Directory.Packages.props` (no WebAssembly.Server yet)
- `src/Design/Design.sln`, `src/docs/reference-app/EmployeeManagement.sln`, `src/Neatoo.RemoteFactory.sln`
- All documentation pages for BaseAddress/CORS references
- Skill setup.md for BaseAddress/CORS references
- CLAUDE-DESIGN.md for BaseAddress references
- Reference app sample files referencing serverUrl/BaseAddress
- CorsConfigurationSamples.cs source file

Key findings:
1. All four project groups follow the same standalone pattern with hardcoded BaseAddress URLs
2. All four servers configure CORS with `AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()`
3. The Design.Client.Blazor uses port 5000 but the server runs on 5085 -- a latent configuration bug
4. `Microsoft.AspNetCore.Components.WebAssembly.Server` is not in Directory.Packages.props and needs framework-conditional versions
5. The reference app uses `#region` snippets that propagate to docs and skill via mdsnippets
6. CORS guidance appears in `docs/aspnetcore-integration.md`, `skills/RemoteFactory/references/setup.md`, and multiple sample files

---

## Agent Phasing

| Phase | Agent Type | Fresh Agent? | Rationale | Dependencies |
|-------|-----------|-------------|-----------|--------------|
| Phase 1: Package Config | developer | Yes | Small scoped change to one file | None |
| Phase 2: Design Project | developer | No | Resume from Phase 1 -- needs build context | Phase 1 |
| Phase 3: Person Example | developer | No | Resume -- same pattern, small change set | Phase 1 |
| Phase 4: OrderEntry Example | developer | No | Resume -- same pattern, small change set | Phase 1 |
| Phase 5: Reference App + Samples | developer | Yes | Fresh context for the most complex conversion (sample files, snippet regions) | Phase 1 |
| Phase 6: Docs and Skill Updates | developer | Yes | Fresh context for documentation-focused work. Run mdsnippets, then manual prose updates | Phase 5 |

**Parallelizable phases:** Phases 2, 3, and 4 are independent of each other (all depend only on Phase 1). They could theoretically run in parallel, but given they share the same pattern and are each small, sequential execution in a single agent is more practical.

**Notes:**
- Phases 1-4 can be handled by a single developer agent session since they are small, formulaic changes.
- Phase 5 is separated because the reference app has sample files with `#region` blocks and requires more careful editing.
- Phase 6 must run after Phase 5 because `mdsnippets` needs the updated sample code to generate correct snippets.

---

## Developer Review

**Status:** Approved
**Reviewed:** 2026-02-28

### Verdict: Approved (with corrections noted below)

I have independently verified every file referenced in the plan against the actual codebase. The plan is thorough, well-structured, and the approach is correct. The multi-targeting scenario has been validated with a proof-of-concept build (multi-target Web SDK server referencing multi-target BlazorWebAssembly client compiles successfully for all three frameworks). Below are corrections and clarifications that must be applied during implementation.

### Corrections Required

#### 1. Root CLAUDE.md Does NOT Need Updating (Plan Error)

The plan (Phase 6, CLAUDE.md updates table) says to update `CLAUDE.md` (project root) with a new client setup code example. However, the root `CLAUDE.md` contains NO BaseAddress, HttpClient, or client setup code examples. There is nothing to update. This line should be removed from the scope.

**`src/Design/CLAUDE-DESIGN.md`** does have a hardcoded `localhost:5000` URL in the "Client Setup (Blazor WASM)" section (line 476) and does need updating. This is correctly identified.

#### 2. Package Versioning Pattern: Follow Existing Convention

The plan says to add framework-conditional versions to `Directory.Packages.props`. The plan proposes Version `8.0.11`, `9.0.5`, and `10.0.0-preview.7.25380.108` as separate entries.

However, the existing csproj pattern does NOT use the `.v8`/`.v10` suffixed package names from `Directory.Packages.props`. Instead, csproj files use `VersionOverride` directly:

```xml
<!-- Pattern used in all client projects -->
<ItemGroup Condition="'$(TargetFramework)' == 'net8.0'">
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" VersionOverride="8.0.11" />
</ItemGroup>
<ItemGroup Condition="'$(TargetFramework)' == 'net9.0' Or '$(TargetFramework)' == 'net10.0'">
    <PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.Server" />
</ItemGroup>
```

**Implementation approach:** Add a single entry to `Directory.Packages.props`:
```xml
<PackageVersion Include="Microsoft.AspNetCore.Components.WebAssembly.Server" Version="9.0.5" />
```
Then use `VersionOverride="8.0.11"` in the net8.0 conditional block of each server csproj. This matches the pattern used by the existing client WebAssembly and DevServer references.

Note: The `.v8` / `.v10` entries already present in `Directory.Packages.props` for WebAssembly, DevServer, Mvc.Testing, and Metadata are dead entries (not referenced by any csproj). Adding more dead entries would be misleading. The developer should follow the actual pattern in use.

#### 3. Sample Files: Keep Generic Patterns Generic

The plan says to update these reference app sample files to use `builder.HostEnvironment.BaseAddress`:
- `FactoryModeAttributes.cs` (`modes-remote-config` region)
- `FactoryModesSamples.cs` (`modes-remoteonly-example` region)
- `CompleteSetupExamples.cs` (`RemoteOnlyModeClientSetup.Configure()`)

**These should NOT be changed.** These are generic configuration patterns showing how Remote mode works with ANY client (Blazor, MAUI, WPF, console). Using `builder.HostEnvironment.BaseAddress` would make them Blazor-specific. The `serverUrl` variable is intentionally generic. `CompleteSetupExamples.RemoteOnlyModeClientSetup.Configure()` takes `serverUrl` as a parameter -- changing this to `builder.HostEnvironment.BaseAddress` would require adding a WebAssemblyHostBuilder dependency, which doesn't belong in the Infrastructure project.

**What SHOULD be updated:** The `ClientProgramSample.cs` and `EmployeeManagement.Client.Blazor/Program.cs` files, which ARE Blazor-specific. These are correctly identified in the plan.

#### 4. Person.Server Is Multi-Target (Risk #7 Resolved)

The plan flags Risk #7 about `Person.Server` possibly being single-target. The csproj comment says "Inherits TargetFramework (net9.0)" but this comment is stale. There is no nested `Directory.Build.props` and the csproj does not override `TargetFrameworks`. It inherits `net8.0;net9.0;net10.0` from the central `src/Directory.Build.props`. No `SetTargetFramework` will be needed.

#### 5. Missing Files: OrderEntry.Server and EmployeeManagement.Server.WebApi Have No launchSettings.json

The plan mentions updating `launchSettings.json` only for Design.Server and Person.Server. This is correct because OrderEntry.Server and EmployeeManagement.Server.WebApi do not have `launchSettings.json` files. The plan should note that these servers need launchSettings.json created to set `"launchBrowser": true` -- OR the developer can skip this since it's not strictly necessary for hosted WASM to work.

#### 6. Middleware Ordering Clarification

The plan says to add `app.UseBlazorFrameworkFiles()`, `app.UseStaticFiles()`, and `app.MapFallbackToFile("index.html")`. The ordering should be:

```csharp
app.UseBlazorFrameworkFiles();  // Before UseStaticFiles
app.UseStaticFiles();           // Serve wwwroot content
// ... other middleware (UseNeatoo, etc.) ...
app.MapFallbackToFile("index.html");  // LAST - catches unmatched routes
```

`UseBlazorFrameworkFiles` must come before `UseStaticFiles` (it configures the framework files provider). `MapFallbackToFile` must come after all route mappings. The plan's design section shows this correctly, but the implementation tables for each phase just list the three calls without specifying order relative to existing middleware (UseNeatoo). The developer should place `UseBlazorFrameworkFiles()` and `UseStaticFiles()` BEFORE `UseNeatoo()`, and `MapFallbackToFile("index.html")` AFTER all other middleware/routes.

#### 7. Person.Server Has Custom Middleware That Must Be Preserved

`Person.Server/Program.cs` has a custom middleware block (lines 21-31) that reads `UserRoles` from request headers. This must be preserved during conversion. The plan's Phase 3 table notes removing CORS and adding hosted WASM middleware but does not explicitly mention preserving this custom middleware. The developer must be careful not to disrupt it.

### Confirmed Claims

- All four project groups follow the standalone WASM pattern with hardcoded BaseAddress URLs
- All four servers configure wide-open CORS (`AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()`)
- Design.Client.Blazor uses port 5000 but Design.Server runs on 5085 (latent bug)
- `Microsoft.AspNetCore.Components.WebAssembly.Server` is NOT in the ASP.NET Core shared framework -- it must be added as a NuGet package (verified against net8.0, net9.0, net10.0 SDK installations)
- `MapFallbackToFile` IS available from the Web SDK without the extra package
- `UseBlazorFrameworkFiles` requires the `WebAssembly.Server` package
- Multi-target server referencing multi-target BlazorWebAssembly client builds and resolves TFMs correctly (verified with proof-of-concept build)
- No special `ReferenceOutputAssembly="false"` or `SetTargetFramework` attributes needed in .NET 8+
- Unit/integration tests use DI container simulation and are unaffected by this change
- Agent phasing is practical: Phases 1-4 are small and formulaic; Phase 5 (reference app) is correctly separated due to snippet complexity; Phase 6 (docs) correctly depends on Phase 5

### Non-blocking Observations

- The Design README says "Server runs at `http://localhost:5000`" but the actual launchSettings port is 5085. This should be corrected during the README update.
- Client launchSettings.json files (Design.Client.Blazor, Person.Client) can be deleted or left as-is. In hosted mode they're unused. Leaving them avoids accidental breakage if someone tries to run the client independently and gets a confusing error.

---

## Implementation Contract

**Created:** 2026-02-28
**Approved by:** developer agent (Opus 4.6)

### Design Project Acceptance Criteria

N/A -- no design project code generation changes.

### In Scope

#### Phase 1: Package Configuration
- [ ] Add single `Microsoft.AspNetCore.Components.WebAssembly.Server` Version `9.0.5` entry to `src/Directory.Packages.props`

#### Phase 2: Design Project Conversion
- [ ] `src/Design/Design.Server/Design.Server.csproj`: Add framework-conditional `WebAssembly.Server` package refs (VersionOverride for net8.0) + ProjectReference to `Design.Client.Blazor`
- [ ] `src/Design/Design.Server/Program.cs`: Add `UseBlazorFrameworkFiles()`, `UseStaticFiles()` (before `UseNeatoo()`), `MapFallbackToFile("index.html")` (after all routes); remove `AddCors()` and `UseCors()`
- [ ] `src/Design/Design.Client.Blazor/Program.cs`: Replace hardcoded `localhost:5000` with `builder.HostEnvironment.BaseAddress`; update COMMON MISTAKE comment
- [ ] `src/Design/Design.Client.Blazor/Design.Client.Blazor.csproj`: Remove both DevServer package reference blocks
- [ ] `src/Design/Design.Server/Properties/launchSettings.json`: Set `"launchBrowser": true`
- [ ] `src/Design/README.md`: Update "Running the Projects" section (single `dotnet run` on server); fix stale port reference (says 5000, actually 5085)
- [ ] `src/Design/CLAUDE-DESIGN.md`: Update "Client Setup (Blazor WASM)" section

#### Phase 3: Person Example Conversion
- [ ] `src/Examples/Person/Person.Server/Person.Server.csproj`: Add framework-conditional `WebAssembly.Server` package refs + ProjectReference to `Person.Client`
- [ ] `src/Examples/Person/Person.Server/Program.cs`: Add hosted WASM middleware; remove CORS; preserve custom `UserRoles` middleware
- [ ] `src/Examples/Person/Person.Client/Program.cs`: Replace hardcoded `localhost:5183` with `builder.HostEnvironment.BaseAddress`
- [ ] `src/Examples/Person/Person.Client/Person.Client.csproj`: Remove both DevServer package reference blocks
- [ ] `src/Examples/Person/Person.Server/Properties/launchSettings.json`: Update `launchUrl` from `weatherforecast` to empty/root; clean up IIS Express profile

#### Phase 4: OrderEntry Example Conversion
- [ ] `src/Examples/OrderEntry/OrderEntry.Server/OrderEntry.Server.csproj`: Add framework-conditional `WebAssembly.Server` package refs + ProjectReference to `OrderEntry.BlazorClient`
- [ ] `src/Examples/OrderEntry/OrderEntry.Server/Program.cs`: Add hosted WASM middleware; remove CORS
- [ ] `src/Examples/OrderEntry/OrderEntry.BlazorClient/Program.cs`: Replace hardcoded `localhost:5184` with `builder.HostEnvironment.BaseAddress`
- [ ] `src/Examples/OrderEntry/OrderEntry.BlazorClient/OrderEntry.BlazorClient.csproj`: Remove both DevServer package reference blocks

#### Phase 5: Reference App (EmployeeManagement) Conversion
- [ ] `src/docs/reference-app/EmployeeManagement.Server.WebApi/EmployeeManagement.Server.WebApi.csproj`: Add framework-conditional `WebAssembly.Server` package refs + ProjectReference to `EmployeeManagement.Client.Blazor`
- [ ] `src/docs/reference-app/EmployeeManagement.Server.WebApi/Program.cs`: Add hosted WASM middleware (update `#region getting-started-server-program` to include it); remove CORS
- [ ] `src/docs/reference-app/EmployeeManagement.Client.Blazor/Program.cs`: Replace `serverBaseAddress` variable with `builder.HostEnvironment.BaseAddress` (update `#region getting-started-client-program`)
- [ ] `src/docs/reference-app/EmployeeManagement.Client.Blazor/EmployeeManagement.Client.Blazor.csproj`: Remove both DevServer package reference blocks
- [ ] `src/docs/reference-app/EmployeeManagement.Client.Blazor/Samples/ClientProgramSample.cs`: Update to use `builder.HostEnvironment.BaseAddress`
- [ ] `src/docs/reference-app/EmployeeManagement.Server.WebApi/Samples/AspNetCore/CorsConfigurationSamples.cs`: Add comment noting CORS is for non-hosted deployments only

#### Phase 5: Files NOT to Modify (Generic Patterns)
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs` -- generic `serverUrl` pattern is correct
- `src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModesSamples.cs` -- generic `serverUrl` pattern is correct
- `src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/CompleteSetupExamples.cs` -- generic `serverUrl` parameter is correct

#### Phase 6: Documentation and Skill Updates
- [ ] Run `mdsnippets` from repository root
- [ ] `docs/getting-started.md`: Review auto-updated snippets; update surrounding prose about client configuration
- [ ] `docs/factory-modes.md`: Review auto-updated snippets (modes-remote-config will NOT change since we're keeping generic `serverUrl`)
- [ ] `docs/aspnetcore-integration.md`: Review auto-updated CORS snippet; rewrite CORS section header and prose for non-hosted scenario context
- [ ] `skills/RemoteFactory/references/setup.md`: Review auto-updated snippets; update CORS section header; update hardcoded BaseAddress example at line 150
- [ ] `src/Design/CLAUDE-DESIGN.md`: Already handled in Phase 2
- [ ] `src/Design/README.md`: Already handled in Phase 2

### Explicitly Out of Scope

- Root `CLAUDE.md` -- contains no BaseAddress/CORS references (plan error corrected)
- Generator changes
- RemoteFactory core library changes
- RemoteFactory.AspNetCore changes
- NuGet package version or structure changes
- Test infrastructure changes (ClientServerContainers)
- Adding new tests (existing tests validate correctness)
- Generic sample files (FactoryModeAttributes.cs, FactoryModesSamples.cs, CompleteSetupExamples.cs)
- Creating launchSettings.json for OrderEntry.Server or EmployeeManagement.Server.WebApi (not necessary)

### Verification Gates

1. After Phase 1: `dotnet restore src/Neatoo.RemoteFactory.sln` succeeds
2. After Phase 2: `dotnet build src/Design/Design.sln` and `dotnet test src/Design/Design.sln` pass
3. After Phases 3-4: `dotnet build src/Neatoo.RemoteFactory.sln` and `dotnet test src/Neatoo.RemoteFactory.sln` pass
4. After Phase 5: `dotnet build src/docs/reference-app/EmployeeManagement.sln` and `dotnet test src/docs/reference-app/EmployeeManagement.sln` pass
5. After Phase 6: `mdsnippets` runs without errors; all three solutions build and test successfully
6. Final sweep: No hardcoded localhost URLs in client Program.cs files; No CORS in server Program.cs files; No DevServer in client .csproj files

### Stop Conditions

If any occur, STOP and report:
- Out-of-scope test failure (any test in RemoteFactory.UnitTests, RemoteFactory.IntegrationTests, or existing Design.Tests that was passing before)
- Multi-targeting ProjectReference resolution failure
- `mdsnippets` failure or stale snippet anchor (region name changed/removed)
- Build failure in any of the three solutions after respective phase
- Person.Server custom middleware (UserRoles header) breaks during conversion

---

## Implementation Progress

**Started:** 2026-02-28
**Developer:** Claude Opus 4.6

**Milestone 1:** Package Configuration + Design Project
- [x] Add WebAssembly.Server to Directory.Packages.props (Version 9.0.5, single entry)
- [x] Convert Design Server/Client (UseBlazorFrameworkFiles, UseStaticFiles before UseNeatoo; MapFallbackToFile after; HostEnvironment.BaseAddress; removed CORS and DevServer)
- [x] Update Design README and CLAUDE-DESIGN.md (fixed stale port 5000 -> 5085, single `dotnet run` instructions, hosted WASM client setup)
- [x] **Verification**: Design.sln builds (0 warnings, 0 errors) and tests pass (29 passed x 3 TFMs)

**Milestone 2:** Person + OrderEntry Examples
- [x] Convert Person Server/Client (preserved UserRoles header middleware; removed CORS and DevServer; cleaned up launchSettings.json IIS Express profile and weatherforecast launchUrl)
- [x] Convert OrderEntry Server/Client (required `ReferenceOutputAssembly="false"` on BlazorClient ProjectReference to prevent transitive type conflict between OrderEntry.Domain.Client and OrderEntry.Domain.Server; removed CORS and DevServer)
- [x] **Verification**: Neatoo.RemoteFactory.sln builds (0 warnings, 0 errors) and all tests pass (UnitTests: 450 x 3 TFMs, IntegrationTests: 466+3skipped x 3 TFMs, RemoteOnlyTests: 19 x 3 TFMs)

**Milestone 3:** Reference App + Samples
- [x] Convert EmployeeManagement Server/Client (UseBlazorFrameworkFiles, UseStaticFiles before UseNeatoo; MapFallbackToFile after; HostEnvironment.BaseAddress; removed CORS and DevServer; added WebAssembly.Server package refs with VersionOverride for net8.0; added ProjectReference to Client.Blazor; updated #region getting-started-server-program and #region getting-started-client-program)
- [x] Update sample files: ClientProgramSample.cs (removed serverBaseAddress parameter, use HostEnvironment.BaseAddress); CorsConfigurationSamples.cs (added comments noting CORS is for non-hosted deployments only)
- [x] Did NOT modify generic pattern files (FactoryModeAttributes.cs, FactoryModesSamples.cs, CompleteSetupExamples.cs) per Correction #3
- [x] **Verification**: EmployeeManagement.sln builds (0 warnings, 0 errors) and tests pass (48 passed x 3 TFMs)

**Milestone 4:** Documentation
- [x] Run mdsnippets (213 snippets extracted, 0 warnings, 0 errors)
- [x] Update docs prose (getting-started.md -- hosted WASM project structure and server description; aspnetcore-integration.md -- CORS section recontextualized for non-hosted deployments; factory-modes.md -- no changes needed, generic `serverUrl` pattern correctly preserved)
- [x] Update skill prose (setup.md -- CORS section header and prose updated for non-hosted context; hardcoded BaseAddress example replaced with `builder.HostEnvironment.BaseAddress`)
- [x] Root CLAUDE.md -- confirmed no changes needed (Correction #1: no BaseAddress/CORS references exist)
- [x] Confirmed CLAUDE-DESIGN.md and README.md correctly updated in Phase 2
- [x] **Verification**: mdsnippets ran twice with no stale anchors; all three solutions build (0 warnings, 0 errors) and all tests pass

---

## Completion Evidence

**Reported:** 2026-02-28

- **Tests Passing:**
  - `dotnet test src/Neatoo.RemoteFactory.sln`: UnitTests 450 x 3 TFMs (0 failures), IntegrationTests 466+3skipped x 3 TFMs (0 failures), RemoteOnlyTests 19 x 3 TFMs (0 failures). Note: 1 transient failure in `EventTrackerTests.Track_RunningTask_IncrementsPendingCount` on net10.0 -- passes when re-run in isolation (timing-related flaky test, pre-existing, unrelated to this work).
  - `dotnet test src/Design/Design.sln`: 29 passed x 3 TFMs (0 failures)
  - `dotnet test src/docs/reference-app/EmployeeManagement.sln`: 48 passed x 3 TFMs (0 failures)
- **Design Projects Compile:** Yes -- Design.sln builds with 0 warnings, 0 errors
- **All Contract Items:** All Phase 6 contract items completed:
  - [x] mdsnippets run (twice -- second run confirms no stale anchors)
  - [x] `docs/getting-started.md` -- snippets auto-updated; prose updated for hosted WASM project structure and server configuration
  - [x] `docs/factory-modes.md` -- reviewed; `modes-remote-config` correctly unchanged (generic `serverUrl` pattern preserved per Correction #3)
  - [x] `docs/aspnetcore-integration.md` -- CORS snippet auto-updated; section header and prose rewritten for non-hosted deployment context
  - [x] `skills/RemoteFactory/references/setup.md` -- snippets auto-updated; CORS section header and prose updated for non-hosted context; hardcoded `localhost:5000` BaseAddress replaced with `builder.HostEnvironment.BaseAddress`
  - [x] `src/Design/CLAUDE-DESIGN.md` -- verified correct from Phase 2 (hosted WASM section with `HostEnvironment.BaseAddress`)
  - [x] `src/Design/README.md` -- verified correct from Phase 2 (single `dotnet run` instructions, correct port 5085)
  - [x] Root `CLAUDE.md` -- confirmed no changes needed (Correction #1)
- **Final Sweep:**
  - No hardcoded `localhost:XXXX` BaseAddress URLs in any client Program.cs files
  - No CORS configuration (`AddCors`/`UseCors`) in any server Program.cs files
  - No `Microsoft.AspNetCore.Components.WebAssembly.DevServer` references in any client .csproj files

---

## Documentation

**Agent:** developer (no dedicated documentation agent)
**Completed:** 2026-02-28

### Expected Deliverables

- [x] `docs/getting-started.md` -- updated client configuration prose
- [x] `docs/factory-modes.md` -- reviewed, no prose changes needed (generic patterns preserved)
- [x] `docs/aspnetcore-integration.md` -- recontextualized CORS section for non-hosted scenarios
- [x] `skills/RemoteFactory/references/setup.md` -- updated CORS and client setup sections
- [x] `CLAUDE.md` (root) -- confirmed no changes needed (Correction #1: no BaseAddress/CORS references)
- [x] `src/Design/CLAUDE-DESIGN.md` -- verified correct from Phase 2
- [x] `src/Design/README.md` -- verified correct from Phase 2
- [x] Skill updates: Yes (via mdsnippets + manual prose)
- [x] Sample updates: Yes (reference app sample files -- completed in Phase 5)

### Files Updated

Phase 6 documentation changes:

- `docs/getting-started.md` -- Auto-updated snippets (`getting-started-server-program` now shows hosted WASM middleware; `getting-started-client-program` now shows `HostEnvironment.BaseAddress`). Manual prose: updated Project Structure section to note hosted WASM; updated Server Configuration intro; updated Client Configuration intro to remove "pointed at the server"; added note about `UseBlazorFrameworkFiles()` and `MapFallbackToFile()`.
- `docs/aspnetcore-integration.md` -- Auto-updated CORS snippet (now includes non-hosted deployment comment). Manual prose: rewrote CORS section header to "CORS Configuration (Non-Hosted Deployments)"; added explanatory paragraph about when CORS is/isn't needed; updated trailing prose from "Place CORS..." to "When CORS is needed, place it...".
- `docs/factory-modes.md` -- No changes (correctly verified that `modes-remote-config` snippet was not updated since generic `serverUrl` was preserved per Correction #3).
- `skills/RemoteFactory/references/setup.md` -- Auto-updated snippets (`aspnetcore-cors` and `getting-started-client-program`). Manual prose: rewrote CORS section header to "CORS Configuration (Non-Hosted Deployments Only)" with explanatory paragraph; replaced hardcoded `https://localhost:5000/` BaseAddress example with `builder.HostEnvironment.BaseAddress` at line 150.

---

## Architect Verification

**Verified:** 2026-02-28
**Verdict:** VERIFIED

### Independent Test Results

All builds and tests run independently by the architect. Results match the developer's claims exactly.

| Solution | Build | Tests |
|----------|-------|-------|
| `src/Neatoo.RemoteFactory.sln` | 0 warnings, 0 errors | UnitTests: 450 x 3 TFMs (0 failures); IntegrationTests: 466+3skipped x 3 TFMs (0 failures); RemoteOnlyTests: 19 x 3 TFMs (0 failures) |
| `src/Design/Design.sln` | 0 warnings, 0 errors | 29 passed x 3 TFMs (0 failures) |
| `src/docs/reference-app/EmployeeManagement.sln` | 0 warnings, 0 errors | 48 passed x 3 TFMs (0 failures) |

Zero test failures across all solutions and all target frameworks.

### Design Match - Spot-Check Results

**Server Program.cs files (all 4 verified):**
- `UseBlazorFrameworkFiles()` before `UseStaticFiles()` before `UseNeatoo()` before `MapFallbackToFile("index.html")` -- correct middleware ordering in all 4 servers
- CORS (`AddCors`/`UseCors`) removed from all 4 servers -- confirmed via grep (zero matches)
- Design.Server: extensive DESIGN SOURCE OF TRUTH comments updated for hosted WASM
- Person.Server: custom UserRoles middleware (lines 21-31) preserved intact between `UseStaticFiles()` and `UseNeatoo()`
- OrderEntry.Server: database creation block preserved, hosted WASM middleware added correctly
- EmployeeManagement.Server.WebApi: `#region getting-started-server-program` updated to include hosted WASM middleware

**Client Program.cs files (all 4 verified):**
- All use `builder.HostEnvironment.BaseAddress` -- confirmed via grep (zero matches for hardcoded localhost BaseAddress)
- Design.Client.Blazor: DESIGN SOURCE OF TRUTH comments updated, hardcoded `localhost:5000` replaced
- Person.Client: `localhost:5183` replaced with `HostEnvironment.BaseAddress`
- OrderEntry.BlazorClient: `localhost:5184` replaced with `HostEnvironment.BaseAddress`
- EmployeeManagement.Client.Blazor: `serverBaseAddress` variable removed, `#region getting-started-client-program` updated

**Server .csproj files (all 4 verified):**
- All have `Microsoft.AspNetCore.Components.WebAssembly.Server` with framework-conditional versioning (VersionOverride="8.0.11" for net8.0, no override for net9.0/net10.0)
- All have ProjectReference to their respective client project
- OrderEntry.Server uses `ReferenceOutputAssembly="false"` on BlazorClient ref to prevent transitive type conflict -- reasonable approach to the Domain.Client/Domain.Server duality

**Client .csproj files (all 4 verified):**
- DevServer removed from all 4 -- confirmed via grep (zero matches for DevServer in any .csproj)

**Generic sample files NOT modified (verified):**
- `FactoryModeAttributes.cs` -- zero diff (git diff confirms unchanged)
- `FactoryModesSamples.cs` -- zero diff
- `CompleteSetupExamples.cs` -- zero diff

### Final Sweep Results

| Check | Method | Result |
|-------|--------|--------|
| Hardcoded localhost BaseAddress in client Program.cs | `grep localhost.*BaseAddress **/Program.cs` | Zero matches |
| AddCors/UseCors in server Program.cs | `grep AddCors\|UseCors **/*Server*/Program.cs` | Zero matches |
| DevServer in client .csproj | `grep DevServer **/*.csproj` | Zero matches |

### Documentation Verification

| File | Status | Notes |
|------|--------|-------|
| `docs/getting-started.md` | Updated | Snippets auto-updated; prose reflects hosted WASM project structure and middleware |
| `docs/aspnetcore-integration.md` | Updated | CORS section recontextualized as "Non-Hosted Deployments"; explanatory paragraph added |
| `docs/factory-modes.md` | Correctly unchanged | Generic `serverUrl` pattern preserved per Correction #3 |
| `skills/RemoteFactory/references/setup.md` | Updated | CORS header "Non-Hosted Deployments Only"; `HostEnvironment.BaseAddress` in client setup snippet and manual example |
| `src/Design/README.md` | Updated | Single "Run the Application" section; correct port 5085; hosted WASM description |
| `src/Design/CLAUDE-DESIGN.md` | Updated | "Client Setup (Hosted Blazor WASM)" section uses `HostEnvironment.BaseAddress` |
| `CLAUDE.md` (root) | NOT modified | Confirmed zero diff -- correct per Correction #1 |

### Corrections Compliance

All 7 developer review corrections were followed:
1. Root CLAUDE.md not touched (no BaseAddress/CORS references exist)
2. Package versioning follows existing VersionOverride convention
3. Generic sample files left unchanged
4. Person.Server confirmed multi-target (Risk #7 resolved)
5. No launchSettings.json created for OrderEntry.Server or EmployeeManagement.Server.WebApi
6. Middleware ordering correct in all servers
7. Person.Server UserRoles middleware preserved
